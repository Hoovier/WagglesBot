using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageList = System.Collections.Generic.List<CoreWaggles.danbooru.Image>;
using Newtonsoft.Json;
using System.Linq;

namespace CoreWaggles.Commands
{
    public class danbooruCommands : ModuleBase<SocketCommandContext>
    {
        [Command("dan")]
        [Alias("danbooru", "danb")]
        public async Task danbooruSearchSort([Remainder]string srch)
        {
            await Context.Channel.TriggerTypingAsync();
            //url for use, explicit images and inserts provided tags straight in.
            string url;
            //if channel is not on the explicit channels list,
            if (!DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate)
            {
                url = $"https://danbooru.donmai.us/posts.json?tags={srch}+Rating%3Asafe&limit=50";
            }
            else
            {
                url = $"https://danbooru.donmai.us/posts.json?tags={srch}&limit=50";
            }
            string respond = danbooru.getJSON(url).Result;
            if (respond == "failure")
            {
                await ReplyAsync("An error occurred! It is possible your search returned 0 results or the results are filtered out due to channel.");
                return;
            }
            ImageList responseList = JsonConvert.DeserializeObject<ImageList>(respond);
            if (responseList.Count == 0)
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            else
            {
                Global.danbooruSearches[Context.Channel.Id] = respond;
                Random rand = new Random();
                Global.danbooruSearchIndex[Context.Channel.Id] = rand.Next(0, responseList.Count);
                danbooru.Image chosenImage = responseList[Global.danbooruSearchIndex[Context.Channel.Id]];
                while(chosenImage.file_url == null)
                {
                    Global.danbooruSearchIndex[Context.Channel.Id] = rand.Next(0, responseList.Count);
                    chosenImage = responseList[Global.danbooruSearchIndex[Context.Channel.Id]];
                }
                
                await ReplyAsync(chosenImage.file_url + "\n" + chosenImage.tag_string_artist.Replace(" ", ", "));
            }
        }

        //basically a copy of ~redditnext
        [Command("dn")]
        [Alias("danext", "dnext")]
        public async Task danbooruNext()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Global.danbooruSearches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.danbooruSearches[Context.Channel.Id]);
                if (responseList.Count - 1 == Global.danbooruSearchIndex[Context.Channel.Id])
                {
                    Global.danbooruSearchIndex[Context.Channel.Id] = 0;
                }
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
                
                Global.danbooruSearchIndex[Context.Channel.Id]++;
                while (responseList.ElementAt(Global.danbooruSearchIndex[Context.Channel.Id]).file_url == null)
                {
                    Global.danbooruSearchIndex[Context.Channel.Id]++;
                }
                await ReplyAsync(responseList.ElementAt(Global.danbooruSearchIndex[Context.Channel.Id]).file_url + "\n" 
                    + responseList.ElementAt(Global.danbooruSearchIndex[Context.Channel.Id]).tag_string_artist);
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
        }
        //basically a copy of ~redditnext
        [Command("dn")]
        [Alias("dnext", "danext")]
        public async Task danbooruNextSpecific(int amount)
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
            if (Global.danbooruSearches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.danbooruSearches[Context.Channel.Id]);
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
                else if (responseList.Count < (Global.danbooruSearchIndex[Context.Channel.Id] + amount))
                {
                    await ReplyAsync("Reached end of results, resetting index. Use ~dnext to start again.");
                    Global.danbooruSearchIndex[Context.Channel.Id] = 0;
                    return;
                }
                //if all fail, proceed!
                else
                {
                    //loop through user provided amount
                    for (int counter = 0; counter < amount; counter++)
                    {
                        if (responseList.Count < Global.danbooruSearchIndex[Context.Channel.Id] + 1)
                        {
                            await ReplyAsync("Reached end of results, resetting index. Use ~dnext to start again.");
                            Global.danbooruSearchIndex[Context.Channel.Id] = 0;
                        }
                        //if everythings fine, increase index by 1
                        else
                        {
                            Global.danbooruSearchIndex[Context.Channel.Id]++;
                        }
                        while (responseList.ElementAt(Global.danbooruSearchIndex[Context.Channel.Id]).file_url == null)
                        {
                            Global.danbooruSearchIndex[Context.Channel.Id]++;
                        }
                        response = response + responseList[Global.danbooruSearchIndex[Context.Channel.Id]].file_url + "\n";
                    }
                }

                await ReplyAsync(response);
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
        }
        [Command("dantags")]
        [Alias("dant", "danboorutags")]
        public async Task danbooruTags()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Global.danbooruSearches.ContainsKey(Context.Channel.Id))
            {
                ImageList responseList = JsonConvert.DeserializeObject<ImageList>(Global.danbooruSearches[Context.Channel.Id]);
                danbooru.Image chosen = responseList.ElementAt(Global.danbooruSearchIndex[Context.Channel.Id]);
                if (responseList.Count == 0)
                {
                    await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                    return;
                }

                await ReplyAsync(BuildDanbooruTags(chosen));
            }
            else
            {
                await ReplyAsync("You have to make a search first! Try running ~e <tag(s)>");
            }
        }
        //This dictionary returns the string corresponding to the rating member in an Image object.
        //ex: ratings[s] returns "Safe,"
        public readonly static Dictionary<string, string> ratings = new Dictionary<string, string>()
        { { "s", "Safe, " }, { "q", "Questionable, " }, { "e", "Explicit, " } };
        /// <summary>Takes a danbooru.Image object and returns formatted tag string.</summary>
        /// <param name="img">A danbooru.Image object that will have tags extracted</param>
        /// <returns>Formatted tag and artist string.</returns>
        public static string BuildDanbooruTags(danbooru.Image img)
        {
            // Adds commas to tags, for easier reading.
            //put the artist and general tags string together.
            string allTagsstring = img.tag_string_general + " " + img.tag_string_copyright + " " + img.tag_string_character;
            string tagString = allTagsstring.Replace(" ", ", ");
            // Create string with ratings and artist tags
            string artistAndRating = ratings[img.rating.ToString()] + "**Artist(s):** " + img.tag_string_artist;

            //return string with lots of formatting!
            return "**Info:** " + artistAndRating + "\n\n" + "All tags: \n```" + tagString + "```";
        }

    }
}
