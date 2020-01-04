using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageList = System.Collections.Generic.List<CoreWaggles.e621.Image>;


namespace CoreWaggles.Commands
{
   public class e621Commands : ModuleBase<SocketCommandContext>
    {
        [Command("e")]
        public async Task e621Search([Remainder] string srch)
        {
            //url for use, explicit images and inserts provided tags straight in.
            string url = $"https://e621.net/post/index.json?tags={srch}+rating:s&limit=50";
            string respond = e621.getJSON(url).Result;
            if (respond == "failure")
            {
               await ReplyAsync("An error occurred!");
                return;
            }
            ImageList responseList = JsonConvert.DeserializeObject<ImageList>(respond);
            if (responseList.Count == 0)
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            else 
            {
                Global.e621Searches[Context.Channel.Id] = respond;
                Random rand = new Random();
                Global.e621SearchIndex = rand.Next(0, 49);
                await ReplyAsync(responseList.ElementAt(Global.e621SearchIndex).file_url);
            }
        }
        [Command("en")]
        [Alias("enext", "ne")]
        public async Task e621Next()
        {
            if(Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.e621Searches[Context.Channel.Id]);
                if (responseList.Count == Global.e621SearchIndex)
                    Global.e621SearchIndex = 0;
                Global.e621SearchIndex++;
                await ReplyAsync(responseList.ElementAt(Global.e621SearchIndex).file_url);
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
            
        }
    }
}
