using Discord.Commands;
using System;
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
    
    public readonly string baseURL = "https://derpibooru.org/search.json";

    public readonly string[] sortingOptions = {"created_at", "wilson", "relevance", "random%3A1096362"};

    public CommandService _command { get; set; }
    
    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiDefault([Remainder]string search) {
        // Regular Derpi search should just be the Sorted one with a default sorting option.
        await DerpiSorted(-1, search);
    }

    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiSorted(int Sort, [Remainder]string search) {
        // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Choose sorting method from the available list.
        // "score" is the default "unsorted" value from the prior version of this command.
        string sortParam = (Sort >= 0 && Sort < this.sortingOptions.Length) ? this.sortingOptions[Sort] : "score";

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
        string requestUrl = DerpiHelper.buildDerpiUrl(this.baseURL, queryParams);
        
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

            // Get all tags and try to locate artist tag.
            string[] artistTags = randomElement.tags.Split(',');
            artistTags = Array.FindAll(artistTags, tag => tag.Contains("artist:"));

            // Parse artist tag results.
            string artistResults;
            switch (artistTags.Length) {
                case 0:
                    bool isScreencap = randomElement.tags.Contains("screencap");
                    artistResults = isScreencap ? "" : "\nProblem finding artist"; break;
                case 1:
                    artistResults = "\n" + artistTags[0].TrimStart(); break;
                default:
                    artistResults = "\n" + String.Join(" ", artistTags); break;
            }

            // Get the full image URL in the format "//derpicdn.net/img/view/YYYY/M/d/IMAGE_ID.png"
            string imgLink = randomElement.representations.full;

            // TODO: REMOVE COMMENTS BELOW THIS IF ABOVE WORKS.
            // Format Date and build image result link.
            // string createdDate = randomElement.created_at.Date.ToString("yyyy/M/d");
            // string imgLink = $"https://derpicdn.net/img/view/{createdDate}/{randomElement.id}.{randomElement.original_format}";

            await ReplyAsync($"https:{imgLink}{artistResults}");

        } catch {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }

    [Command("next")]
    [Alias("n")]
    public async Task DerpiantoherAsync()
    {
        await Context.Channel.TriggerTypingAsync();
        string individualquery;
        individualquery = Global.searchesD[Context.Channel.Id];
 
        DerpiRoot firstImages = JsonConvert.DeserializeObject<DerpiRoot>(individualquery);
        List<DerpiSearch> allimages = new List<DerpiSearch>();
        allimages.AddRange(firstImages.Search.ToList());
        
        if (allimages.Count == 0)
        {
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            return;
        }
        if(Global.searched + 1 > allimages.Count())
        {
            Global.searched = 0;
        }
        int rd = Global.searched++;
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
        else if (results.Length > 1 && results.Length < 10)
        {
            for (int counter = 0; (counter < results.Length); counter++)
            {
                sb.Append(results[counter]);
            }
            newresults = sb.ToString();
        }
        else if (results.Length > 10)
        {
            newresults = "Too many artist to list.";
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
            await ReplyAsync($"{cliky} \n{newresults}");
        }
        else if (allimages.Count < 1)
        {

            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
        }
    }
    [Command("next")]
    [Alias("n")]
    public async Task DerpianotheroverloadAsync(int chose)
    {
        await Context.Channel.TriggerTypingAsync();

       
        string individualquery;



        individualquery = Global.searchesD[Context.Channel.Id];

        DerpiRoot firstImages = JsonConvert.DeserializeObject<DerpiRoot>(individualquery);
        List<DerpiSearch> allimages = new List<DerpiSearch>();
        allimages.AddRange(firstImages.Search.ToList());

        if (allimages.Count == 0)
        {
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            return;
        }
        if (Global.searched + 1 > allimages.Count())
        {
            Global.searched = 0;
        }
        int rd = chose;
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
        else if (results.Length > 1 && results.Length < 10)
        {
            for (int counter = 0; (counter < results.Length); counter++)
            {
                sb.Append(results[counter]);
            }
            newresults = sb.ToString();
        }
        else if (results.Length > 10)
        {
            newresults = "Too many artist to list.";
        }
        if (allimages.Count > 0)
        {
            //var newresults = results[0].TrimStart();

            pony = pony.Date;
            string pony4 = pony.ToString("yyyy/M/d");

            var pony2 = allimages.ElementAt(rd).representations.full;
            string cliky = $"https://derpicdn.net/img/view/{pony4}/{idofimg}.{filetype} ";
            //pony2 = $"https:{pony2}";\n ugly link: {pony2}
            // await ReplyAsync($"{count} matching images found! {cliky} and {pony2}");
            await ReplyAsync($"{cliky} \n{newresults}");
        }
        else if (allimages.Count < 1)
        {

            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
        }
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
 */
public class DerpiHelper {
    // TODO: make this a method of the DerpibooruComms class or find if it is more generic across command files.
    //Takes a base URL, and appends query parameters to it.
    public static string buildDerpiUrl(string url, IDictionary<string, string> queryParams) {
        // Takes the parameter pairs such as "perpage" and "50", and pairs them into strings "perpage=50".
        string[] queryPairs = queryParams.Select(entry => $"{entry.Key}={entry.Value}").ToArray();
        
        // Joins together the above parameter pairs into one string such as "perpage=50&page=6".
        string paramString = string.Join("&", queryPairs);

        // Detect if the current URL has a query string.
        // If so, append with "&", else append with "?" to start it off.
        return $"{url}{(url.Contains("?") ? "&" : "?")}{paramString}";
    }

}
