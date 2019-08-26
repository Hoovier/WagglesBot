using Discord.Commands;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WagglesBot.Modules;
using System.Text.RegularExpressions;
using CoreWaggles;

// Alias for Derpibooru Response objects.
using DerpiRoot = WagglesBot.Modules.DerpibooruResponse.Rootobject;
using DerpiSearch = WagglesBot.Modules.DerpibooruResponse.Search;

public class DerpibooruComms : ModuleBase<SocketCommandContext>
{
    // TODO: Make a better endpoint reference repository.
    // The Derpibooru API endpoint.
    public readonly string baseURL = "https://derpibooru.org/search.json";
    // The Derpibooru Reverse Image Search endpoint.
    public readonly string reverseURL = "https://derpibooru.org/search/reverse.json?scraper_url=";

    // Available sorting methods for Derpibooru.
    // "score" is the default "unsorted" value from the prior version of this command.
    public readonly string[] sortingOptions = {"created_at", "wilson", "relevance", "random%3A1096362", "score"};

    public CommandService _command { get; set; }
    
    [Command("reverse")]
    [Alias("r")]
    public async Task DerpiReverse([Remainder]string url) {
         // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Validate that the user input a URL.
        if (string.IsNullOrEmpty(url)) {
            await ReplyAsync("No URL given, please provide a valid URL!");
            return;
        } else if (DerpiHelper.ValidateUrl(url)) {
            await ReplyAsync("Invalid or inaccessible URL: `" + url + "`\nPlease try again, or contact Hoovier!");
            return;
        }

        // If we have a URL, then make a scraper request.
        // Deserialize (from JSON to DerpibooruResponse.RootObject) the Derpibooru search results.
        string DerpiJson = Get.Derpibooru($"{this.reverseURL}{url}").Result;
        DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(DerpiJson);

        // Convert Search Array to a List, to use List functionality.
        List<DerpiSearch> imageList = DerpiResponse.Search.ToList();
        if (imageList.Count == 0) {
            await ReplyAsync("Could not find the image on Derpibooru.org!");
        } else {
            // Get search result element. First element if there are more than one.
            DerpiSearch element =  imageList.First();
            // Determine if Channel allows NSFW content or not.
            bool safeOnly = !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate;
            
            if (safeOnly && !DerpiHelper.IsElementSafe(element)) {
                // If there is a NSFW artwork searched on a SFW channel, do not display result and inform the user.
                await ReplyAsync("Result found, but is NSFW. Please enable NSFW on this channel to view result, or ask again on an approved channel or private message.");
            } else {
                // Display the image link if allowed!
                string derpiURL = "https://derpibooru.org/" + imageList.First().id;
                await ReplyAsync("Result found on Derpibooru here: " + derpiURL);
            }
        }
    }

    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiDefault([Remainder]string search) {
        // Regular Derpi search should just be the Sorted one with a default sorting option.
        await DerpiMaster(false, 5, search);
    }

    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiSorted(int Sort, [Remainder]string search) {
        await DerpiMaster(false, Sort, search);
    }

    // "Master" Derpibooru/~derpi searching method. Not a discord-accessible method.
    public async Task DerpiMaster(bool artistAsLink, int Sort, string search) {
        // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Validate that a valid sorting option was chosen.
        if (Sort < 0 || Sort >= this.sortingOptions.Length) {
            await ReplyAsync("Invalid sorting option: " + Sort + ". Please try again");
            return;
        }
        // Choose sorting method from the available list.
        string sortParam = this.sortingOptions[Sort];

        // Set up the base query parameters.
        // "q" is our search terms
        // "filter_id" is the filter against content banned by Discord TOS.
        Dictionary<string,string> queryParams = new Dictionary<string,string>() {
            {"filter_id", "164610"},
            {"sf", sortParam}, 
            {"sd", "desc"}, 
            {"perpage", "50"},
            {"page", "1"},
        };

        // If the channel is not on the list of NSFW enabled channels do not allow NSFW results.
        // the second part checks if the command was executed in DMS, DM channels do not have to be added to the NSFW enabled list.
        // In DMs the first check will fail, and so will the second, allowing for nsfw results to be displayed.
        bool safeOnly = !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate;

        // Add search to the parameters list, with "AND safe" if it failed the NSFW check.
        queryParams.Add("q", safeOnly ? $"{search}+AND+safe" : search);

        // Build the full request URL.
        string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, queryParams);
        
        // Global.searchesD is a dictionary-based cache with the last search result in that channel, if applicable.
        // Always stores results globally for other commands like ~next to keep track.
        Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}").Result;

        // Deserialize (from JSON to DerpibooruResponse.RootObject) the Derpibooru search results.
        DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);

        // Actual request an. Try-catch to softly catch exceptions.
        try {
            // Convert Search Array to a List, to use List functionality.
            List<DerpiSearch> imageList = DerpiResponse.Search.ToList();
            if (imageList.Count == 0) {
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                return;
            }
            // Get random number generator and random entry.
            var rng = new Random();
            int rand = rng.Next(imageList.Count);
            Global.searched = rand + 1;
            DerpiSearch randomElement = imageList.ElementAt(rand);

            // Add image ID to Global.links.
            // TODO: Describe where this is used better?
            Global.links[Context.Channel.Id] = randomElement.id;

            await ReplyAsync(
                DerpiHelper.BuildDiscordResponse(randomElement, artistAsLink, !safeOnly)
            );

        } catch {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }

    // Cycles through the page of ~derpi results.
    [Command("next")]
    [Alias("n")]
    public async Task DerpiNext()
    {       
        DerpiRoot DerpiResponse;

        // Check if an a "~derpi" search has been made in this channel yet.
        // We need to deserialize this in order to get the result set size.
        // TODO: Store the result size globally? To avoid deserializing so soon.
        if (Global.searchesD.ContainsKey(Context.Channel.Id)) {
            DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
        } else {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) to get some results before I can hoof you over more silly!");
            return;
        }

        // If ~next goes off the globally cached page, loop around the beginning again.
        if(Global.searched + 1 > DerpiResponse.Search.Count()) {
            Global.searched = 0;
        }

        // The `~next` command is basically `~next {CurrentIndex + 1}`, lets treat it that way.
        await DerpiNextPick(++Global.searched);
    }

    // Allows you to select an item on the page of results by index number.
    [Command("next")]
    [Alias("n")]
    public async Task DerpiNextPick(int index)
    {
        await Context.Channel.TriggerTypingAsync();

        DerpiRoot DerpiResponse;

        // Check if an a "~derpi" search has been made in this channel yet.
        if (Global.searchesD.ContainsKey(Context.Channel.Id)) {
            DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
        } else {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) to get some results before I can hoof you over more silly!");
            return;
        }
       
        if (DerpiResponse.Search.Count() == 0) {
            // No Results Message.
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            return;
        } else if (index < 0 || index >= DerpiResponse.Search.Count()) {
            // Out of Bounds Message.
            await ReplyAsync("Your selection is out of range! Valid values are between 0 and " + (DerpiResponse.Search.Count() - 1));
            return;
        }

        // If ~next goes off the globally cached page, loop around the beginning again.
        if(Global.searched + 1 > DerpiResponse.Search.Count()) {
            Global.searched = 0;
        }

        // Get element at specified index.
        DerpiSearch chosenElement = DerpiResponse.Search.ElementAt(index);

        // Add image ID to Global.links.
        // TODO: Describe where this is used better?
        Global.links[Context.Channel.Id] = chosenElement.id;

        await ReplyAsync(
            DerpiHelper.BuildDiscordResponse(chosenElement)
        );
    }

    [Command("derpist")]
    [Alias("st")]
    public async Task DerpistAsync([Remainder]string srch)
    {
        await Context.Channel.TriggerTypingAsync();
        string requestUrl;
        if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
        {
            requestUrl =
                      $"https://derpibooru.org/search.json?q={srch}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


        }

        else
        {
            requestUrl =
                       $"https://derpibooru.org/search.json?q={srch}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
        }
        DerpiRoot firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}1").Result);
        int count = firstImages.Total;
        List<DerpiSearch> allimages = new List<DerpiSearch>();
        allimages.AddRange(firstImages.Search.ToList());


        await ReplyAsync($"Total results: {count}");
        return;

    }
    [Command("artist")]
    [Alias("a")]
    public async Task ArtistAsync([Remainder]string srch)
        {
            await Context.Channel.TriggerTypingAsync();
       
            string shortened = srch;
            string requestUrl = $"https://derpibooru.org/search.json?q={srch}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
            if (srch.Contains("https"))
            {

                string pattern = @"(\d+)+\.";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.' });
                ulong chanelid = 480105955552395285;
                if (Context.Channel.Id == chanelid)
                {
                    requestUrl =
                              $"https://derpibooru.org/search.json?q=id:{shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                }

                else
                {
                    requestUrl =
                               $"https://derpibooru.org/search.json?q=id:{shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }


            }
            else
            {
                await ReplyAsync("Non-url detected, trying now.");
                if (!Global.safeChannels.ContainsKey(Context.Channel.Id))
                {
                    requestUrl =
                              $"https://derpibooru.org/search.json?q={shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                }

                else
                {
                    requestUrl =
                               $"https://derpibooru.org/search.json?q={shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }

            }
            int count =
                JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}1").Result)
                    .Total;
            DerpiRoot firstImages =
                     JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}1").Result);
            List<DerpiSearch> allimages = new List<DerpiSearch>();
            allimages.AddRange(firstImages.Search.ToList());
            var rand = new Random();
            if (allimages.Count == 0)
            {
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                return;
            }
            int rd = rand.Next(allimages.Count);
            var pony = allimages.ElementAt(rd).created_at;
            var filetype = allimages.ElementAt(rd).original_format;
            var idofimg = allimages.ElementAt(rd).id;
        if (Global.links.ContainsKey(Context.Channel.Id))
        {
            Global.links[Context.Channel.Id] = idofimg;
        }
        else
        {
            Global.links.Add(Context.Channel.Id, idofimg);
        }
        string arrsting = allimages.ElementAt(rd).tags;
            string[] arrstingchoose = arrsting.Split(',');
            var sb = new System.Text.StringBuilder();
            string newresults = "Problem finding artist";
            var results = Array.FindAll(arrstingchoose, s => s.Contains("artist:"));
            // if (results.Length == 1)
            //{
            //  newresults = results[0].TrimStart();
            //}
            if (results.Length > 0)
            {
                for (int counter = 0; (counter < results.Length); counter++)
                {
                    sb.Append($"https://derpibooru.org/tags/{results[counter].Replace("-", "-dash-").Replace(":", "-colon-").TrimStart()} ");
                }
                newresults = sb.ToString();
            }

            if (allimages.Count > 0)
            {
                //var newresults = results[0].TrimStart();
                pony = pony.Date;
                string pony4 = pony.ToString("yyyy/M/d");

                var pony2 = allimages.ElementAt(rd).representations.full;
                string cliky = $"https://derpibooru.org/{idofimg}";
                //pony2 = $"https:{pony2}";\n ugly link: {pony2}
                // await ReplyAsync($"{count} matching images found! {cliky} and {pony2}");
                await ReplyAsync($"{cliky} \n{newresults}");
            }
            else if (allimages.Count < 1)
            {

                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            }
        }
    
        [Command("artist")]
        [Alias("a")]
        public async Task Nolink()
        {
            await Context.Channel.TriggerTypingAsync();
        
        string requestUrl;
        requestUrl = $"https://derpibooru.org/search.json?q=id:{Global.links[Context.Channel.Id]}&filter_id=164610";
        int count =
               JsonConvert.DeserializeObject<DerpiRoot>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}").Result)
                   .Total;
            DerpiRoot firstImages =
                     JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}").Result);
            List<DerpiSearch> allimages = new List<DerpiSearch>();
            allimages.AddRange(firstImages.Search.ToList());

            if (allimages.Count == 0)
            {
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                return;
            }
            var rand = new Random();
            int rd = rand.Next(allimages.Count);
            var pony = allimages.ElementAt(rd).created_at;
            var filetype = allimages.ElementAt(rd).original_format;
            var idofimg = allimages.ElementAt(rd).id;

        if (Global.links.ContainsKey(Context.Channel.Id))
        {
            Global.links[Context.Channel.Id] = idofimg;
        }
        else
        {
            Global.links.Add(Context.Channel.Id, idofimg);
        }
        string arrsting = allimages.ElementAt(rd).tags;
            string[] arrstingchoose = arrsting.Split(',');
            var sb = new System.Text.StringBuilder();
            string newresults = "Problem finding artist";
            var results = Array.FindAll(arrstingchoose, s => s.Contains("artist:"));
            // if (results.Length == 1)
            //{
            //  newresults = results[0].TrimStart();
            //}
            if (results.Length > 0)
            {
                for (int counter = 0; (counter < results.Length); counter++)
                {
                    sb.Append($"https://derpibooru.org/tags/{results[counter].Replace("-", "-dash-").Replace(":", "-colon-").TrimStart().Replace(" ", "+") } ");
                }
                newresults = sb.ToString();
            }

            if (allimages.Count > 0)
            {
                //var newresults = results[0].TrimStart();
                pony = pony.Date;
                string pony4 = pony.ToString("yyyy/M/d");

                var pony2 = allimages.ElementAt(rd).representations.full;
                string cliky = $"https://derpibooru.org/{idofimg}";
                //pony2 = $"https:{pony2}";\n ugly link: {pony2}
                // await ReplyAsync($"{count} matching images found! {cliky} and {pony2}");
                await ReplyAsync($"{cliky} \n{newresults}");
            }
            else if (allimages.Count < 1)
            {

                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            }
        }
    
        
        [Group("derpitags")]
        [Alias("dt")]
        public class Repsonsetest : ModuleBase<SocketCommandContext>
        {
            [Command]
            async Task Nolink()
            {
                await Context.Channel.TriggerTypingAsync();
            string requestUrl;
            requestUrl =$"https://derpibooru.org/search.json?q=id:{Global.links[Context.Channel.Id]}&filter_id=164610";
            int count =
                   JsonConvert.DeserializeObject<DerpiRoot>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}").Result)
                       .Total;
                DerpiRoot firstImages =
                         JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}").Result);
                List<DerpiSearch> allimages = new List<DerpiSearch>();
                allimages.AddRange(firstImages.Search.ToList());
                var lop = allimages.ElementAt(0).tags;
                string arrsting = allimages.ElementAt(0).tags;
                string[] arrstingchoose = arrsting.Split(',');
                var sb = new System.Text.StringBuilder();
                var results = Array.FindAll(arrstingchoose, s => s.Contains("artist:"));
                int rightIndex = 0;
                var ratingIndexplicit = Array.FindIndex(arrstingchoose, s => s.Contains("explicit"));
                var ratingIndexsafe = Array.FindIndex(arrstingchoose, s => s.Contains("safe"));
                var ratingIndexquesti = Array.FindIndex(arrstingchoose, s => s.Contains("questionable"));
                var ratingIndexsuggestive = Array.FindIndex(arrstingchoose, s => s.Contains("suggestive"));
                string newresults = "Problem finding artist";
                if (ratingIndexplicit != -1)
                {
                    rightIndex = ratingIndexplicit;
                }
                else if (ratingIndexsafe != -1)
                {
                    rightIndex = ratingIndexsafe;
                }
                else if (ratingIndexsuggestive != -1)
                {
                    rightIndex = ratingIndexsuggestive;
                }
                else if (ratingIndexquesti != -1)
                {
                    rightIndex = ratingIndexquesti;
                }

                if (results.Length == 1)
                {
                    newresults = results[0].TrimStart();
                }
                else if (results.Length > 1)
                {
                    newresults = string.Join(",", results);


                }

                await ReplyAsync($"Info: **{arrstingchoose[rightIndex].TrimStart(' ')}, {newresults}** \n \n All tags: ```{lop.TrimStart(' ')}```");
                return;


            }
            [Command]
            public async Task RegitagsAsync([Remainder]string srch)

            {
                await Context.Channel.TriggerTypingAsync();
                string requestUrl = $"https://derpibooru.org/search.json?q=id:{srch}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                string shortened = srch;
            if(srch.Contains("?"))
            {
                string pattern = @"(\d+)+\?";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.', '?' });
                ulong chanelid = 480105955552395285;
                if (Context.Channel.Id == chanelid)
                {
                    requestUrl =
                              $"https://derpibooru.org/search.json?q=id:{shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                }

                else
                {
                    requestUrl =
                               $"https://derpibooru.org/search.json?q=id:{shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }
            }
            else if(srch.Contains("booru"))
            {
                string pattern = @"(\d+)";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.', '?' });
                ulong chanelid = 480105955552395285;
                if (Context.Channel.Id == chanelid)
                {
                    requestUrl =
                              $"https://derpibooru.org/search.json?q=id:{shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                }

                else
                {
                    requestUrl =
                               $"https://derpibooru.org/search.json?q=id:{shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }
            }

            else    if (srch.Contains("https"))
                {
                    string pattern = @"(\d+)+\.";
                    Match result = Regex.Match(srch, pattern);
                    shortened = result.Value.Trim(new Char[] { ' ', '.' });
                    ulong chanelid = 480105955552395285;
                    if (Context.Channel.Id == chanelid)
                    {
                        requestUrl =
                                  $"https://derpibooru.org/search.json?q=id:{shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                    }

                    else
                    {
                        requestUrl =
                                   $"https://derpibooru.org/search.json?q=id:{shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                    }

                }
                else
                {
                    await ReplyAsync("Non-url detected, trying now.");
                    ulong chanelid = 480105955552395285;
                    if (Context.Channel.Id == chanelid)
                    {
                        requestUrl =
                                  $"https://derpibooru.org/search.json?q={shortened}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";


                    }

                    else
                    {
                        requestUrl =
                                   $"https://derpibooru.org/search.json?q={shortened}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                    }

                }
                DerpiRoot firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru($"{requestUrl}1").Result);
                int count = firstImages.Total;
                List<DerpiSearch> allimages = new List<DerpiSearch>();
                allimages.AddRange(firstImages.Search.ToList());
                var lop = allimages.ElementAt(0).tags;
                string arrsting = allimages.ElementAt(0).tags;
                string[] arrstingchoose = arrsting.Split(',');
                var sb = new System.Text.StringBuilder();
                var results = Array.FindAll(arrstingchoose, s => s.Contains("artist:"));
                int rightIndex = 0;
                var ratingIndexplicit = Array.FindIndex(arrstingchoose, s => s.Contains("explicit"));
                var ratingIndexsafe = Array.FindIndex(arrstingchoose, s => s.Contains("safe"));
                var ratingIndexquesti = Array.FindIndex(arrstingchoose, s => s.Contains("questionable"));
                var ratingIndexsuggestive = Array.FindIndex(arrstingchoose, s => s.Contains("suggestive"));
                string newresults = "Problem finding artist";
                if (ratingIndexplicit != -1)
                {
                    rightIndex = ratingIndexplicit;
                }
                else if (ratingIndexsafe != -1)
                {
                    rightIndex = ratingIndexsafe;
                }
                else if (ratingIndexsuggestive != -1)
                {
                    rightIndex = ratingIndexsuggestive;
                }
                else if (ratingIndexquesti != -1)
                {
                    rightIndex = ratingIndexquesti;
                }

                if (results.Length == 1)
                {
                    newresults = results[0].TrimStart();
                }
                else if (results.Length > 1)
                {
                    newresults = string.Join(",", results);


                }

                await ReplyAsync($"Info: **{arrstingchoose[rightIndex].TrimStart(' ')}, {newresults}** \n \n All tags: ```{lop.TrimStart(' ')}```");
                return;

            }
        }
    [Group("imfeelinglucky")]
    [Alias("lucky")]
    public class Resonsetest : ModuleBase<SocketCommandContext>
    {
        [Command]
        public async Task luck(int num, [Remainder]string srch)
        {
            await Context.Channel.TriggerTypingAsync();
            string requestUrl;
            int count;
            int paige = 420;
            if(num == 0 || num > 5)
            {
                await ReplyAsync("You need to pick a number bigger than 0 and no more than 5");
                return;
            }
            
            try
            {
                DerpiRoot firstImages;
                if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
                {
                    requestUrl = $"https://derpibooru.org/search.json?q={srch}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }

                else
                {
                    requestUrl = $"https://derpibooru.org/search.json?q={srch}&filter_id=164610&sf=random&sd=desc&perpage=50&page=";
                }

                if (Global.searchesD.ContainsKey(Context.Channel.Id))
                {
                    try
                    {
                        string tempsrch = Get.Derpibooru($"{requestUrl}1").Result;
                        int amt = JsonConvert.DeserializeObject<DerpiRoot>(tempsrch)
                           .Total;
                        int pages = amt / 50;
                        var randim = new Random();
                        paige = randim.Next(pages);
                        Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}{paige}").Result;
                        firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
                        count = firstImages.Total;
                    }
                    catch
                    {
                        await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
                        return;
                    }

                }
                else
                {
                    string tempsrch = Get.Derpibooru($"{requestUrl}1").Result;
                    int amt = JsonConvert.DeserializeObject<DerpiRoot>(tempsrch).Total;
                    int pages = amt / 50;
                    var randim = new Random();
                    paige = randim.Next(pages);
                    Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}{paige}").Result);
                    count = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]).Total;
                    firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
                }

                List<DerpiSearch> allimages = new List<DerpiSearch>();
                allimages.AddRange(firstImages.Search.ToList());
                var rand = new Random();
                if (allimages.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }
                int rd = rand.Next(allimages.Count);
                List<int> chosen = new List<int>();
                if(num > allimages.Count)
                {
                   await ReplyAsync("Not enough results to post " + num);
                    return;
                }
                string added = $"Listing {num} results";
                if (allimages.Count > num)
                {
                    for (int counting = 0; counting < num; counting++) {
                        rd = rand.Next(allimages.Count);
                        while (chosen.Contains(rd))
                        {
                            rd = rand.Next(allimages.Count);
                        }
                        chosen.Add(rd);
                        added = $"{added}\nhttps://derpicdn.net/img/view/{allimages.ElementAt(rd).created_at.Date.ToString("yyyy/M/d")}/{allimages.ElementAt(rd).id}.{allimages.ElementAt(rd).original_format}";
                    }
                }
                Global.searched = rd + 1;
                

                
                

                if (allimages.Count > 0)
                {
                    //var newresults = results[0].TrimStart();

                   
                    await ReplyAsync(added);


                }
                else if (allimages.Count < 1)
                {

                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                }
            }
            catch
            {
                await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
                return;
            }
        }
        [Command]
        public async Task luckmulti([Remainder]string srch)
        {
            await Context.Channel.TriggerTypingAsync();
            string requestUrl;
            int count;
            int paige = 420;
            try
            {
                DerpiRoot firstImages;
                if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
                {
                    requestUrl = $"https://derpibooru.org/search.json?q={srch}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }

                else
                {
                    requestUrl = $"https://derpibooru.org/search.json?q={srch}&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
                }

                if (Global.searchesD.ContainsKey(Context.Channel.Id))
                {
                    try
                    {
                        string tempsrch = Get.Derpibooru($"{requestUrl}1").Result;
                        int amt = JsonConvert.DeserializeObject<DerpiRoot>(tempsrch)
                           .Total;
                        int pages = amt / 50;
                        var randim = new Random();
                        paige = randim.Next(pages);
                        Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}{paige}").Result;
                        firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
                        count = firstImages.Total;
                    }
                    catch
                    {
                        await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
                        return;
                    }

                }
                else
                {
                    string tempsrch = Get.Derpibooru($"{requestUrl}1").Result;
                    int amt = JsonConvert.DeserializeObject<DerpiRoot>(tempsrch)
                       .Total;
                    int pages = amt / 50;
                    var randim = new Random();
                    paige = randim.Next(pages);
                    Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}{paige}").Result);
                    count = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]).Total;
                    firstImages = JsonConvert.DeserializeObject<DerpiRoot>(Global.searchesD[Context.Channel.Id]);
                }

                List<DerpiSearch> allimages = new List<DerpiSearch>();
                allimages.AddRange(firstImages.Search.ToList());
                var rand = new Random();
                if (allimages.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }
                int rd = rand.Next(allimages.Count);

                Global.searched = rd + 1;
                var pony = allimages.ElementAt(rd).created_at;
                var filetype = allimages.ElementAt(rd).original_format;
                var idofimg = allimages.ElementAt(rd).id;

                if (Global.links.ContainsKey(Context.Channel.Id))
                {
                    Global.links[Context.Channel.Id] = idofimg;
                }
                else
                {
                    Global.links.Add(Context.Channel.Id, idofimg);
                }
                string arrsting = allimages.ElementAt(rd).tags;
                string[] arrstingchoose = arrsting.Split(',');
                var sb = new System.Text.StringBuilder();
                string newresults = "Problem finding artist";
                var results = Array.FindAll(arrstingchoose, s => s.Contains("artist:"));
                if (results.Length == 1)
                {
                    newresults = results[0].TrimStart();
                }
                else if (results.Length > 1)
                {
                    for (int counter = 0; (counter < results.Length); counter++)
                    {
                        sb.Append(results[counter]);
                    }
                    newresults = sb.ToString();
                }

                if (allimages.Count > 0)
                {
                    //var newresults = results[0].TrimStart();

                    pony = pony.Date;
                    string pony4 = pony.ToString("yyyy/M/d");

                    var pony2 = allimages.ElementAt(rd).representations.full;
                    string cliky = $"https://derpicdn.net/img/view/{pony4}/{idofimg}.{filetype}";
                    //pony2 = $"https:{pony2}";\n ugly link: {pony2}
                    // await ReplyAsync($"{count} matching images found! {cliky} and {pony2}");
                    await ReplyAsync($"{cliky} \n:paintbrush:{newresults} :page_facing_up:Found on page {paige}");


                }
                else if (allimages.Count < 1)
                {

                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                }
            }
            catch
            {
                await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
                return;
            }
        }
    }

}

/**
    Derpi Utility class to aggregate common functionality.
    TODO: make this a method of the DerpibooruComms class or find if it is more generic across command files.
 */
public class DerpiHelper {

    /// <summary>Takes a base URL, and appends query parameters to it.</summary>
    /// <param name="url">A base URL string. May also have existing parameters.</param>
    /// <param name="queryParams">A Dictionary of Key-Value pairs for HTML paramters.</param>
    /// <returns>The fully built search URL.</returns>
    public static string BuildDerpiUrl(string url, IDictionary<string, string> queryParams) {
        // Takes the parameter pairs such as "perpage" and "50", and pairs them into strings "perpage=50".
        string[] queryPairs = queryParams.Select(entry => $"{entry.Key}={entry.Value}").ToArray();
        
        // Joins together the above parameter pairs into one string such as "perpage=50&page=6".
        string paramString = string.Join("&", queryPairs);

        // Detect if the current URL has a query string.
        // If so, append with "&", else append with "?" to start it off.
        return $"{url}{(url.Contains("?") ? "&" : "?")}{paramString}";
    }

    /// <summary>Validate that a given string is a URL.</summary>
    /// <param name="url">A string that may or may not be a URL.</param>
    /// <returns>TRUE if the string is a reachable URL, FALSE if it isn't.</returns>
    /// <see>https://stackoverflow.com/a/3808841</see>
    public static bool ValidateUrl(string url) {
        try
        {
            //Creating the HttpWebRequest
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            //Getting the Web Response.
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //Returns TRUE if the Status code == 200
            response.Close();
            return (response.StatusCode == HttpStatusCode.OK);
        }
        catch
        {
            //Any exception will returns false.
            return false;
        }
    }

    // Takes a Derpibooru singular search result node and returns a string message to be sent to Discord.
    /// <summary>Builds the artist tag section of most Derpibooru results.</summary>
    /// <param name="element">A Derpibooru Search Result Element.</param>
    /// <param name="artistAsLink">Whether or not to render it as a list of tags, or a list of links.</param>
    /// <param name="NSFW">Only valid if "artistAsLink" is true, then we need to flag if we are showing SFW results or not.</param>
    /// <returns>A string response consisting of the image element itself, and an artist tags block.</returns>
    public static string BuildDiscordResponse(DerpiSearch element, bool artistAsLink = false, bool NSFW = false) {
        
        // Get the full image URL in the format "//derpicdn.net/img/view/YYYY/M/d/IMAGE_ID.png"
        // Prepend the protocol, HTTPS, to the incomplete URL representation.
        string results = "https:" + element.representations.full;

        // Get the artist block.
        string artistBlock = DerpiHelper.BuildAristTags(element, artistAsLink, NSFW);

        // Add a newline in between if there are any results.
        if (!String.IsNullOrEmpty(artistBlock)) {
            artistBlock = "\n" + artistBlock;
        }

        return results + artistBlock;
    }

    /// <summary>Builds the artist tag section of most Derpibooru results.</summary>
    /// <param name="element">A Derpibooru Search Result Element.</param>
    /// <param name="artistAsLink">Whether or not to render it as a list of tags, or a list of links.</param>
    /// <param name="NSFW">Only valid if "artistAsLink" is true, then we need to flag if we are showing SFW results or not.</param>
    /// <returns>A string, either a list of artist names, or a list of URLs to artist tagged works.</returns>
    public static string BuildAristTags(DerpiSearch element, bool artistAsLink, bool NSFW = false) {
        string artistResult;

        // Get a distilled list of artist tags from the full tag listing.
        string[] artistTags = element.tags.Split(',');
        artistTags = Array.FindAll(artistTags, tag => tag.Contains("artist:"));

        // Parse artist tag response accordingly.
        switch (artistTags.Length) {
            case 0:
                // Unedited screencaps uploaded have no artist tags.
                bool isScreencap = element.tags.Contains("screencap");
                artistResult = isScreencap ? String.Empty : "Problem finding artist";
                break;
            case 1:
                artistResult = artistTags[0].TrimStart();
                if (artistAsLink) {
                    artistResult = $"https://derpibooru.org/search?q={artistResult}";
                    artistResult += NSFW ? String.Empty : "+AND+safe";
                }
                break;
            default:
                artistResult = String.Join(' ', artistTags); break;
        }

        return artistResult;
    }

    // TODO: ADD BETTER DOCUMENT/SUMMARY.
    // Check if a Derpibooru Search result is SFW, by way of scanning for a "safe" tag.
    public static bool IsElementSafe(DerpiSearch element) {
        return element.tags.Split(',').Any("safe".Contains);
    }

}
