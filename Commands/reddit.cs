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
                return;
            }
            Random rand = new Random();
            //maximum amount of posts that can be used from cache
            int maxPosts = Global.redditDictionary[Context.Channel.Id].Count;
            Global.redditIndex = rand.Next(0, maxPosts - 1);
            //get the post at given index and store it here for faster use
            Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
            //if the post is a selfpost, give link to post instead of url like a linkpost
            if (chosen.Listing.IsSelf)
                await ReplyAsync("https://www.reddit.com/" + chosen.Permalink);
            //if its a linkpost, give url that is being linked.
            else
                await ReplyAsync("Cached " + maxPosts + " posts!\n" + ((LinkPost)chosen).URL);
        }

        [Command("rnext")]
        [Alias("rn")]
        public async Task redditNext()
        {
            await Context.Channel.TriggerTypingAsync();
            //if there is not entry in dictionary for this channel, tell user
            if(!Global.redditDictionary.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("Try running a reddit search using ~reddit <subreddit> <hot/new/top> first!");
                return;
            }
            //increment redditIndex so we can grab the next post in cache
            Global.redditIndex++;
            //store post at given index
            Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
            if (chosen.Listing.IsSelf)
                await ReplyAsync("https://www.reddit.com/" + chosen.Permalink);
            else
                await ReplyAsync(((LinkPost)chosen).URL);
        }

        [Command("rinfo")]
        [Alias("rtags", "rt", "ri")]
        public async Task redditInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Global.redditDictionary.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("Try running a reddit search using ~reddit <subreddit> <hot/new/top> first!");
                return;
            }
            Post chosen = Global.redditDictionary[Context.Channel.Id][Global.redditIndex];
            //grab random information about post and format it for user.
            string response = "**" + chosen.Title + "** \nPoster: " + chosen.Author + "\nUpvotes: " 
                + chosen.Score + " Comments: " + chosen.Listing.NumComments + "\nhttps://www.reddit.com/" + chosen.Permalink; 
            await ReplyAsync(response);
        }
    }
}
