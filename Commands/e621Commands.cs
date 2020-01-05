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
                if(responseList.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }
                if (responseList.Count == 1)
                {
                    await ReplyAsync("Only one result to show! \n" + responseList.ElementAt(0));
                    return;
                }
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
        [Command("etags")]
        [Alias("et", "e621tags")]
        public async Task e621Tags()
        {
            if (Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.e621Searches[Context.Channel.Id]);
                e621.Image chosen = responseList.ElementAt(Global.e621SearchIndex);
                if (responseList.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }
               
                await ReplyAsync(e621Helper.Builde621Tags(chosen));
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
        }
    }
    public class e621Helper
    {
        //This dictionary returns the string corresponding to the rating member in an Image object.
        //ex: ratings[s] returns "Safe,"
        public readonly static Dictionary<string, string> ratings = new Dictionary<string, string>() 
        { { "s", "Safe, " }, { "q", "Questionable, " }, { "e", "Explicit, " } };

        /// <summary>Takes an e621.Image object and returns formatted tag string.</summary>
        /// <param name="img">An e621.Image object that will have tags extracted</param>
        /// <returns>Formatted tag and artist string.</returns>
        public static string Builde621Tags(e621.Image img)
        {
            // Adds commas to tags, for easier reading.
            string tagString = img.tags.Replace(" ", ", ");
            // Create string with ratings and artist tags
            string artistAndRating = ratings[img.rating.ToString()] + "**Artist(s):** " + string.Join(",", img.artist);

            //return string with lots of formatting!
            return "**Info:** " + artistAndRating + "\n\n" + "All tags: \n```" + tagString + "```";
        }
    }
}
