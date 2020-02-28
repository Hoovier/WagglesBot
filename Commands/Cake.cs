using CoreWaggles;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WagglesBot.Modules;

public class Cakes : ModuleBase<SocketCommandContext>
{
    [Command("cake")]
    [Alias("c")]

    public async Task DabsAsync(int Sort, [Remainder]string srch)
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
        if (Context.User.Id == 346275493965856769)
        {
            requestUrl = $"https://https://derpibooru.org/api/v1/json/search/images?q={srch}&sf={result}&sd=desc&perpage=50&page=";
        }

        else
        {
            if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                requestUrl =
                          $"https://derpibooru.org/api/v1/json/search/images?q={srch}+AND+safe&filter_id=178065&sf=score&sd=desc&perpage=50&page=";


            }

            else
            {
                requestUrl =
                           $"https://derpibooru.org/api/v1/json/search/images?q={srch}&filter_id=178065&sf=score&sd=desc&perpage=50&page=";
            }
        }
        if (Global.searchesD.ContainsKey(Context.Channel.Id))
        {
            Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}1").Result;
            count =
           JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
               .total;
            firstImages =
                    JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
        }
        else
        {
            Global.searchesD.Add(Context.Channel.Id, Get.Derpibooru($"{requestUrl}1").Result);
            count =
           JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
               .total;
            firstImages =
                    JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
        }
        //can probably remove the allImages variable, derpi response is already in a list in new API
        List<DerpibooruResponse.Image> allimages = new List<DerpibooruResponse.Image>();
        allimages.AddRange(firstImages.images);
        var rand = new Random();
        if (allimages.Count == 0)
        {
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            return;
        }

        int rd = rand.Next(allimages.Count);
        var pony = allimages.ElementAt(rd).created_at;
        var filetype = allimages.ElementAt(rd).format;
        var idofimg = allimages.ElementAt(rd).id;
        if (Global.links.ContainsKey(Context.Channel.Id))
        {
            Global.links[Context.Channel.Id] = idofimg.ToString();
        }
        else
        {
            Global.links.Add(Context.Channel.Id, idofimg.ToString());
        }

        string[] arrsting = allimages.ElementAt(rd).tags.ToArray();
        
        var sb = new System.Text.StringBuilder();
        string newresults = "Problem finding artist";
        var results = Array.FindAll(arrsting, s => s.Contains("artist:"));
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
    [Command("cookie")]
    [Alias("c")]
    public async Task DerpiAsync([Remainder]string srch)
    {
        await Context.Channel.TriggerTypingAsync();
        string requestUrl;
        string result = "score";
        int count;
        try
        {
            DerpibooruResponse.Rootobject firstImages;
            if (Context.User.Id == 346275493965856769)
            {
                requestUrl = $"https://derpibooru.org/api/v1/json/search/images?q={srch}&sf={result}&sd=desc&perpage=50&page=";
            }

            else
            {
                if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
                {
                    requestUrl =
                              $"https://derpibooru.org/api/v1/json/search/images?q={srch}+AND+safe&filter_id=178065&sf=score&sd=desc&perpage=50&page=";


                }

                else
                {
                    requestUrl =
                               $"https://derpibooru.org/api/v1/json/search/images?q={srch}&filter_id=178065&sf=score&sd=desc&perpage=50&page=";
                }
            }
            if (Global.searchesD.ContainsKey(Context.Channel.Id))
            {
                try
                {
                    Global.searchesD[Context.Channel.Id] = Get.Derpibooru($"{requestUrl}1").Result;
                    count =
                   JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id])
                       .total;
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
                count = JsonConvert.DeserializeObject<WagglesBot.Modules.DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]).total;
                firstImages = JsonConvert.DeserializeObject<DerpibooruResponse.Rootobject>(Global.searchesD[Context.Channel.Id]);
            }

            List<DerpibooruResponse.Image> allimages = new List<DerpibooruResponse.Image>();
            allimages.AddRange(firstImages.images);
            var rand = new Random();
            if (allimages.Count == 0)
            {
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                return;
            }
            int rd = rand.Next(allimages.Count);
            Global.searched = rd + 1;
            var pony = allimages.ElementAt(rd).created_at;
            var filetype = allimages.ElementAt(rd).format;
            var idofimg = allimages.ElementAt(rd).id;

            if (Global.links.ContainsKey(Context.Channel.Id))
            {
                Global.links[Context.Channel.Id] = idofimg.ToString();
            }
            else
            {
                Global.links.Add(Context.Channel.Id, idofimg.ToString());
            }
            string[] arrsting = allimages.ElementAt(rd).tags.ToArray();
            var sb = new System.Text.StringBuilder();
            string newresults = "Problem finding artist";
            var results = Array.FindAll(arrsting, s => s.Contains("artist:"));
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
        catch
        {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }
}

