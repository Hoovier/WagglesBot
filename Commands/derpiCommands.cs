
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WagglesBot.Modules;
using System.Text.RegularExpressions;
using CoreWaggles;


public class DerpibooruComms : ModuleBase<SocketCommandContext>
{
    public CommandService _command { get; set; }

    

    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiAsync([Remainder]string srch)
    {
        await Context.Channel.TriggerTypingAsync();
        string requestUrl;
        int count;
        try
        {
            DerpibooruResponse.Rootobject firstImages;
            // if the channel is not on the list of NSFW enabled channels do not allow NSFW results.
            // the second part checks if the command was executed in DMS, DM channels do not have to be added to the NSFW enabled list.
            // In DMs the first check will fail, and so will the second, allowing for nsfw results to be displayed.
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
                //searchesD is a dictionary with the last search result in that channel, if applicable
                //this adds the new search results, in json format, to the dictionary and then gives the deserialized form to a local variable. 
                try
                {
                    Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}1").Result;
                    count =
                   JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
                       .Total;
                    firstImages =
                            JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
                }
                catch
                {
                    await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
                    return;
                }

            }
            else
            {
                Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}1").Result);
                count = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]).Total;
                firstImages = JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
            }

            List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
                await ReplyAsync($"{cliky} \n{newresults}");


            }
            else if (allimages.Count < 1)
            {

                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            }
        }
        catch {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }
    
    [Command("derpi")]
    [Alias("d")]
    
    public async Task DerpispecificAsync(int Sort, [Remainder]string srch)
    {
        
        await Context.Channel.TriggerTypingAsync();
        string requestUrl;
        
        string result = "score";
        int count;
        
        DerpibooruResponse.Rootobject firstImages;
        switch (Sort)
        {
            case 0:
                result = "created_at";
                break;
            case 1:
                result = "wilson";
                break;
            case 2:
                result = "relevance";
                break;
            case 3:
                result = "random%3A1096362";
                break;
        }
        if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
        {
            requestUrl =
                      $"https://derpibooru.org/search.json?q={srch}+AND+safe&filter_id=164610&sf={result}&sd=desc&perpage=50&page=";


        }

        else
        {
            requestUrl =
                       $"https://derpibooru.org/search.json?q={srch}&filter_id=164610&sf={result}&sd=desc&perpage=50&page=";
        }
        if(Global.searchesD.ContainsKey(Context.Channel.Id))
        {
            Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}1").Result;
            count =
           JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
               .Total;
            firstImages =
                    JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
        }
        else
        {
            Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}1").Result);
            count =
           JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
               .Total;
            firstImages =
                    JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
        }
        
        List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
        if(Global.links.ContainsKey(Context.Channel.Id))
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
    public async Task DerpiantoherAsync()
    {
        await Context.Channel.TriggerTypingAsync();
        string individualquery;
        individualquery = Global.searchesD[Context.Channel.Id];
 
        DerpibooruResponse.Rootobject firstImages = JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(individualquery);
        List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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

        DerpibooruResponse.Rootobject firstImages =
                 JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(individualquery);
        List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
        int count =
            JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}1").Result)
                .Total;
        DerpibooruResponse.Rootobject firstImages =
                 JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Get.Derpibooru($"{requestUrl}1").Result);
        List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
                JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}1").Result)
                    .Total;
            DerpibooruResponse.Rootobject firstImages =
                     JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Get.Derpibooru($"{requestUrl}1").Result);
            List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
               JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}").Result)
                   .Total;
            DerpibooruResponse.Rootobject firstImages =
                     JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Get.Derpibooru($"{requestUrl}").Result);
            List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
                   JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}").Result)
                       .Total;
                DerpibooruResponse.Rootobject firstImages =
                         JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Get.Derpibooru($"{requestUrl}").Result);
                List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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

                int count =
                    JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(WagglesBot.Modules.Get.Derpibooru($"{requestUrl}1").Result)
                        .Total;
                DerpibooruResponse.Rootobject firstImages =
                         JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Get.Derpibooru($"{requestUrl}1").Result);
                List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
                DerpibooruResponse.Rootobject firstImages;
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
                        int amt = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(tempsrch)
                           .Total;
                        int pages = amt / 50;
                        var randim = new Random();
                        paige = randim.Next(pages);
                        Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}{paige}").Result;
                        count =
                       JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
                           .Total;
                        firstImages =
                                JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
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
                    int amt = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(tempsrch)
                       .Total;
                    int pages = amt / 50;
                    var randim = new Random();
                    paige = randim.Next(pages);
                    Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}{paige}").Result);
                    count = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]).Total;
                    firstImages = JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
                }

                List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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
                DerpibooruResponse.Rootobject firstImages;
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
                        int amt = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(tempsrch)
                           .Total;
                        int pages = amt / 50;
                        var randim = new Random();
                        paige = randim.Next(pages);
                        Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}{paige}").Result;
                        count =
                       JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
                           .Total;
                        firstImages =
                                JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
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
                    int amt = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(tempsrch)
                       .Total;
                    int pages = amt / 50;
                    var randim = new Random();
                    paige = randim.Next(pages);
                    Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}{paige}").Result);
                    count = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]).Total;
                    firstImages = JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
                }

                List<DerpibooruResponse.Search> allimages = new List<DerpibooruResponse.Search>();
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





 


