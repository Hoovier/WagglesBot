using Discord;
using Discord.Commands;
using Discord.Rest;
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
            await redditSearchMulti(sub, sort, 1);
        }
        [Command("reddit")]
        [Alias("red")]
        public async Task redditSearchMulti(string sub, string sort, int amount)
        {
            await Context.Channel.TriggerTypingAsync();
            //try to get posts from provided subreddit, if it fails tell users that the subreddit doesn't exist.
            try
            {
                //if dictionary doesnt have a entry for it, add it
                if (!Global.redditDictionary.ContainsKey(Context.Channel.Id))
                    Global.redditDictionary.Add(Context.Channel.Id, new RedditHelper());
                //get posts from Reddit API
                SubredditPosts posts = Global.reddit.Subreddit(sub).Posts;
                //depending on sorting, put that set of posts into the global post collection.
                switch (sort)
                {
                    case "top":
                        Global.redditDictionary[Context.Channel.Id].postDictionary = posts.Top;
                        break;
                    case "hot":
                        Global.redditDictionary[Context.Channel.Id].postDictionary = posts.Hot;
                        break;
                    case "new":
                        Global.redditDictionary[Context.Channel.Id].postDictionary = posts.New;
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
                Global.redditDictionary[Context.Channel.Id].postDictionary = new List<Post>();
                return;
            }
            Random rand = new Random();
            //maximum amount of posts that can be used from cache
            int maxPosts = Global.redditDictionary[Context.Channel.Id].getAmount();
            Global.redditDictionary[Context.Channel.Id].index = rand.Next(0, maxPosts - 1);
            //get the post at given index and store it here for faster use
            Post chosen = Global.redditDictionary[Context.Channel.Id].getChosenPost();
            //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
            if(chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                await ReplyAsync("This result is NSFW, and this channel is not whitelisted! Try making another search or going to the next result using ~rnext");
                return;
            }
            await ReplyAsync("Cached " + maxPosts + " posts!");
            await redditNextMulti(amount);
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
            if(Global.redditDictionary[Context.Channel.Id].getAmount() == 0)
            {
                await ReplyAsync("No results!");
                return;
            }
            if (Global.redditDictionary[Context.Channel.Id].getAmount() == 1)
            {
                //leave index unchanged so that same result will be spit out
                await ReplyAsync("Only one result in search!");

            }
            //only run if previous if fails, if there is more than one result in dictionary
            else if (Global.redditDictionary[Context.Channel.Id].getAmount() < Global.redditDictionary[Context.Channel.Id].index + amount)
            {
                await ReplyAsync("Reached end of results, resetting index. Use ~rnext to start again.");
                Global.redditDictionary[Context.Channel.Id].index = 0;
            }
            //if all of the above ifs fail, increment index, as usual.
            else
            {
                string response = $"Posting {amount} links:\n";
                for(int counter = 0; counter < amount; counter++)
                {
                    //increment redditIndex so we can grab the next post in cache
                    if (Global.redditDictionary[Context.Channel.Id].getAmount() < Global.redditDictionary[Context.Channel.Id].index + 1)
                    {
                        await ReplyAsync("Exceeded cache, looping back to beginning!");
                        Global.redditDictionary[Context.Channel.Id].index = 0;
                    }
                    else
                    {
                        Global.redditDictionary[Context.Channel.Id].index++;
                    }
                    //store post at given index
                    Post chosen = Global.redditDictionary[Context.Channel.Id].getChosenPost();
                    //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
                    if (chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
                    {
                        response = response + "NSFW Post\n";
                    }
                    else
                    {
                        if (chosen.Listing.IsSelf)
                            response = response + "https://www.reddit.com" + chosen.Permalink + "\n";
                        else
                            response = response + ((LinkPost)chosen).URL + "\n";
                    }
                }
                RestUserMessage msg =  await Context.Channel.SendMessageAsync(response);
                await msg.AddReactionAsync(new Emoji("▶"));
                //set random info for running ~rnext through emoji reactions.
                Global.redditDictionary[Context.Channel.Id].setTrackingData(msg.Id, msg, Context);
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
            Post chosen = Global.redditDictionary[Context.Channel.Id].getChosenPost();
            //if result is nsfw, and channel is not whitelisted, AND its not a DM, tell them and dont post anything.
            if (chosen.NSFW && !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                await ReplyAsync("This result is NSFW, and this channel is not whitelisted! Try making another search or going to the next result using ~rnext");
                return;
            }
            //grab random information about post and format it for user.
            string response = "**" + chosen.Title + "** \nPoster: " + chosen.Author + "\nUpvotes: " 
                + chosen.Score + " Comments: " + chosen.Listing.NumComments + "\nhttps://www.reddit.com" + chosen.Permalink; 
            await ReplyAsync(response);
        }
        [Command("rinfo")]
        [Alias("rtags", "redinfo", "ri")]
        public async Task redditInfoOverloaded(string link)
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Global.redditDictionary.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("Try running a reddit search using ~reddit <subreddit> <hot/new/top> first!");
                return;
            }
           //if the channel is not nsfw or private, call function with the false bool
            if (!Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate)
            {
                await ReplyAsync(Global.redditDictionary[Context.Channel.Id].getInfo(link, false));
                return;
            }
            //if it is nsfw, let the function know that
            await ReplyAsync(Global.redditDictionary[Context.Channel.Id].getInfo(link, true));
        }
    }
}
