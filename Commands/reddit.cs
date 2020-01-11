using Discord.Commands;
using Reddit;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class reddit : ModuleBase<SocketCommandContext>
    {
        [Command("reddit")]
        [Alias("red")]
        public async Task redditSearch(string sub, string sort)
        {
            await Context.Channel.TriggerTypingAsync();
            //try to get posts from provided subreddit, if it fails tell users that the subreddit doesn't exist.
            try
            {
                //get posts from Reddit API
                SubredditPosts posts = Global.reddit.Subreddit(sub).Posts;
                //depending on sorting, put that set of posts into the global post collection.
                switch (sort)
                {
                    case "top":
                        Global.redditDictionary[Context.Channel.Id] = posts.Top;
                        break;
                    case "hot":
                        Global.redditDictionary[Context.Channel.Id] = posts.Hot;
                        break;
                    case "new":
                        Global.redditDictionary[Context.Channel.Id] = posts.New;
                        break;
                    default:
                        await ReplyAsync("Invalid sorting argument! Try Hot/New/Top.");
                        return;
                }
            }
            catch
            {
                await ReplyAsync("That is not a valid subreddit!");
                //make the dictionary have an empty list, so other commands dont return images from previous search.
                Global.redditDictionary[Context.Channel.Id] = new List<Post>();
                return;
            }
            Random rand = new Random();
            //maximum amount of posts that can be used from cache
            int maxPosts = Global.redditDictionary[Context.Channel.Id].Count;
            Global.redditIndex = rand.Next(0, maxPosts - 1);
            //get the post at given index and store it here for faster use
            Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
            //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
            if(chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                await ReplyAsync("This result is NSFW, and this channel is not whitelisted! Try making another search or going to the next result using ~rnext");
                return;
            }
            //if the post is a selfpost, give link to post instead of url like a linkpost
            if (chosen.Listing.IsSelf)
                await ReplyAsync("https://www.reddit.com/" + chosen.Permalink);
            //if its a linkpost, give url that is being linked.
            else
                await ReplyAsync("Cached " + maxPosts + " posts!\n" + ((LinkPost)chosen).URL);
        }
        [Command("rnext")]
        [Alias("rn", "rednext")]
        public async Task redditNext()
        {
            await redditNextMulti(1);
        }

        [Command("rnext")]
        [Alias("rn", "rednext")]
        public async Task redditNextMulti(int amount)
        {
            await Context.Channel.TriggerTypingAsync();
            //if there is not entry in dictionary for this channel, tell user
            if(amount  < 1 || amount > 5)
            {
                await ReplyAsync("Pick a number between 1 and 5!");
                return;
            }
            if(!Global.redditDictionary.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("Try running a reddit search using ~reddit <subreddit> <hot/new/top> first!");
                return;
            }
            if(Global.redditDictionary[Context.Channel.Id].Count == 0)
            {
                await ReplyAsync("No results!");
                return;
            }
            if (Global.redditDictionary[Context.Channel.Id].Count == 1)
            {
                //leave index unchanged so that same result will be spit out
                await ReplyAsync("Only one result in search!");
            }
            //only run if previous if fails, if there is more than one result in dictionary
            else if (Global.redditDictionary[Context.Channel.Id].Count < Global.redditIndex + 1)
            {
                Global.redditIndex = 0;
            }
            //if all of the above ifs fail, increment index, as usual.
            else
            {
                string response = $"Posting {amount} links:\n";
                for(int counter = 0; counter < amount; counter++)
                {
                    //increment redditIndex so we can grab the next post in cache
                    Global.redditIndex++;
                    //store post at given index
                    Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
                    //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
                    if (chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
                    {
                        response = response + "NSFW Post\n";
                    }
                    else
                    {
                        if (chosen.Listing.IsSelf)
                            response = response + "https://www.reddit.com/" + chosen.Permalink + "\n";
                        else
                            response = response + ((LinkPost)chosen).URL + "\n";
                    }
                }
                await ReplyAsync(response);
            }
        }

        [Command("rinfo")]
        [Alias("rtags", "redinfo", "ri")]
        public async Task redditInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Global.redditDictionary.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("Try running a reddit search using ~reddit <subreddit> <hot/new/top> first!");
                return;
            }
            Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
            //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
            if (chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                await ReplyAsync("This result is NSFW, and this channel is not whitelisted! Try making another search or going to the next result using ~rnext");
                return;
            }
            //grab random information about post and format it for user.
            string response = "**" + chosen.Title + "** \nPoster: " + chosen.Author + "\nUpvotes: " 
                + chosen.Score + " Comments: " + chosen.Listing.NumComments + "\nhttps://www.reddit.com/" + chosen.Permalink; 
            await ReplyAsync(response);
        }
    }
}
