using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageRoot = CoreWaggles.e621.RootObject;
using ImageList = System.Collections.Generic.List<CoreWaggles.e621.Post>;



namespace CoreWaggles.Commands
{
    public class e621Commands : ModuleBase<SocketCommandContext>
    {
        //array for sorting options, https://e621.net/help/show/cheatsheet#sorting
        //-id is default, newest first, id is reverse
        public readonly string[] sortingOptions = { "-id", "id", "score", "favcount", "random" };

        [Command("e")]
        [Alias("e621", "e6")]
        public async Task e621SearchSort(int sort, [Remainder]string srch)
        {
            await Context.Channel.TriggerTypingAsync();
            if(srch.Contains("loli") || srch.Contains("foalcon") || srch.Contains("cub"))
            {
                await ReplyAsync("Those search terms(loli, foalcon, or cub) are not allowed! Try again!");
                return;
            }
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
            if (!DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate)
            {
                url = $"https://e621.net/posts.json?tags=order:{sortingOptions[sort]}+{srch}+rating:s&limit=50";
            }
            else
            {
                url = $"https://e621.net/posts.json?tags=order:{sortingOptions[sort]}+{srch}+-cub+-loli+-foalcon&limit=50";
            }
            string respond = e621.getJSON(url).Result;
            if (respond == "failure")
            {
                await ReplyAsync("An error occurred! " + url);
                return;
            }
            ImageList responseList = JsonConvert.DeserializeObject<ImageRoot>(respond).posts;
            if (responseList.Count == 0)
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            else
            {
                Global.e621Searches[Context.Channel.Id] = respond;
                Random rand = new Random();
                Global.e621SearchIndex[Context.Channel.Id] = rand.Next(0, responseList.Count);
                e621.Post chosenImage = responseList[Global.e621SearchIndex[Context.Channel.Id]];
                await ReplyAsync(chosenImage.file.url + "\n" + string.Join(",", chosenImage.tags.artist));
            }
        }
        [Command("e")]
        [Alias("e621", "e6")]
        public async Task e621Search([Remainder] string srch)
        {
            //run same command as sorted one, but pass default option as arg
            await e621SearchSort(0, srch);
        }
        [Command("en")]
        [Alias("enext", "ne")]
        public async Task e621Next()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageRoot>(Global.e621Searches[Context.Channel.Id]).posts;
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
                if (responseList.Count == Global.e621SearchIndex[Context.Channel.Id])
                    Global.e621SearchIndex[Context.Channel.Id] = 0;
                Global.e621SearchIndex[Context.Channel.Id]++;
                await ReplyAsync(responseList.ElementAt(Global.e621SearchIndex[Context.Channel.Id]).file.url);
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
        }
        //basically a copy of ~redditnext
        [Command("en")]
        [Alias("enext", "ne")]
        public async Task e621NextSpecific(int amount)
        {
            await Context.Channel.TriggerTypingAsync();
            string response = $"Posting {amount} links:\n";
            //check user provided amount
            if (amount < 1 || amount > 5)
            {
                await ReplyAsync("Pick a number between 1 and 5!");
                return;
            }
            //if dictionary has an entry for channel, proceed
            if (Global.e621Searches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageRoot>(Global.e621Searches[Context.Channel.Id]).posts;
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
                else if(responseList.Count < (Global.e621SearchIndex[Context.Channel.Id] + amount))
                {
                    await ReplyAsync("Reached end of results, resetting index. Use ~enext to start again.");
                    Global.e621SearchIndex[Context.Channel.Id] = 0;
                    return;
                }
                //if all fail, proceed!
                else
                {
                    //loop through user provided amount
                    for(int counter = 0; counter < amount; counter++)
                    {
                        if(responseList.Count < Global.e621SearchIndex[Context.Channel.Id] + 1)
                        {
                            await ReplyAsync("Reached end of results, resetting index. Use ~enext to start again.");
                            Global.e621SearchIndex[Context.Channel.Id] = 0;
                        }
                        //if everythings fine, increase index by 1
                        else
                        {
                            Global.e621SearchIndex[Context.Channel.Id]++;
                        }
                        response = response + responseList[Global.e621SearchIndex[Context.Channel.Id]].file.url + "\n";
                    }
                }

                await ReplyAsync(response);
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
                ImageList responseList = JsonConvert.DeserializeObject<ImageRoot>(Global.e621Searches[Context.Channel.Id]).posts;
                e621.Post chosen = responseList.ElementAt(Global.e621SearchIndex[Context.Channel.Id]);
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
        public static string Builde621Tags(e621.Post img)
        {
            // Adds commas to tags, for easier reading.
            //put the artist and general tags List together, so I can Join them all at once.
            List<string> allTagsList = img.tags.character;
            allTagsList.AddRange(img.tags.general);
            string tagString = String.Join(", ", allTagsList);
            // Create string with ratings and artist tags
            string artistAndRating = ratings[img.rating.ToString()] + "**Artist(s):** " + string.Join(", ", img.tags.artist);

            //return string with lots of formatting!
            return "**Info:** " + artistAndRating + "\n\n" + "All tags: \n```" + tagString + "```";
        }
    }
}
