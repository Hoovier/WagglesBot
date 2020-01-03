using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
   public class e621Commands : ModuleBase<SocketCommandContext>
    {
        [Command("e")]
        public async Task e621Search(string srch)
        {
            //url for use, explicit images and inserts provided tags straight in.
            string url = $"https://e621.net/post/index.json?tags={srch}+rating:e&limit=50";
            string respond = e621.getJSON(url).Result;
            if (respond == "failure")
            {
               await ReplyAsync("An error occurred!");
                return;
            }
            List<e621.Image> imageList = JsonConvert.DeserializeObject<List<e621.Image>>(respond);
            if (imageList.Count == 0)
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            else 
            {
                Random rand = new Random();
                int chose = rand.Next(0, 49);
                await ReplyAsync(imageList.ElementAt(chose).file_url);
            }
        }
    }
}
