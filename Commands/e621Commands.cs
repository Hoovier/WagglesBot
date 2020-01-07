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
        //array for sorting options, https://e621.net/help/show/cheatsheet#sorting
        //-id is default, newest first, id is reverse
        public readonly string[] sortingOptions = { "-id", "id", "score", "favcount", "random" };

        [Command("e")]
        [Alias("e621", "e6")]
        public async Task e621Search([Remainder] string srch)
        {
            //run same command as sorted one, but pass default option as arg
            await e621SearchSort(0, srch);
        }
        [Command("e")]
        [Alias("e621", "e6")]
        public async Task e621SearchSort(int sort, string srch)
        {
            await Context.Channel.TriggerTypingAsync();
            //if sort integer is too small or big, give help
            if (sort - 1 > sortingOptions.Length || sort < 0)
            {
                await ReplyAsync("Sorting integers are: \n```0:Default, newest to oldest. \n1:Oldest to newest. \n2:Score. \n3:FavCount. \n4:Random.```" +
                    "\nManual sorting can be done by using this chart https://e621.net/help/show/cheatsheet#sorting and inserting it into a search as a tag. " +
                    "\nEx: Overriding default search order with mpixels order.```~e order:mpixels horse```");
                return;
            }
            //url for use, explicit images and inserts provided tags straight in.
            string url;
            //if channel is not on the explicit channels list,
            if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                url = $"https://e621.net/post/index.json?tags=order:{sortingOptions[sort]}+{srch}+rating:s&limit=50";
            }
            else
            {
                url = $"https://e621.net/post/index.json?tags=order:{sortingOptions[sort]}+{srch}+-cub&limit=50";
            }
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
                Global.e621SearchIndex = rand.Next(0, responseList.Count);
                e621.Image chosenImage = responseList[Global.e621SearchIndex];
                await ReplyAsync(chosenImage.file_url + "\n" + string.Join(",", chosenImage.artist));
            }
        }
        [Command("en")]
        [Alias("enext", "ne")]
        public async Task e621Next()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.e621Searches[Context.Channel.Id]);
                if (responseList.Count == 0)
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
        [Command("en")]
        [Alias("enext", "ne")]
        public async Task e621NextSpecific(int index)
        {
            await Context.Channel.TriggerTypingAsync();
            if (Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.e621Searches[Context.Channel.Id]);
                if (responseList.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }
                if (responseList.Count == 1)
                {
                    await ReplyAsync("Only one result to show! \n" + responseList.ElementAt(0));
                    return;
                }
                if (responseList.Count < index)
                {
                    await ReplyAsync("Thats too big, choose a number between 0-" + (responseList.Count - 1));
                    return;
                }
                Global.e621SearchIndex = index + 1;
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
            await Context.Channel.TriggerTypingAsync();
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
