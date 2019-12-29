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
        public async Task setNick(string srch)
        {
            //url for use, explicit images and inserts provided tags straight in.
            string url = $"https://e621.net/post/index.json?tags={srch}+rating:e&limit=5";
            string respond;

            using (HttpClient client = new HttpClient())
            {
                //userAgent info
                string info = "WagglesBot/1.0 (by Hoovier)";
                string type = "application/json";
                client.BaseAddress = new Uri(url);

                //random client things, not super sure if all of it is needed apart from UserAgent stuff.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.UserAgent.Clear();
                //important useragent things, not allowed through without this
                client.DefaultRequestHeaders.UserAgent.ParseAdd(info);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(type));
                HttpResponseMessage response = await client.GetAsync(String.Empty);

                if (response.IsSuccessStatusCode)
                {
                    respond = await response.Content.ReadAsStringAsync();
                    List<e621.Image> response1 = JsonConvert.DeserializeObject<List<e621.Image>>(respond);
                    await ReplyAsync(response1.ElementAt(1).file_url);
                }
                else
                {
                    respond = string.Empty;
                    await ReplyAsync("Status code was failure. " + response.StatusCode);
                }
            }
        }
    }
}
