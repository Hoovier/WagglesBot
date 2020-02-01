using Discord.Commands;
using Discord.Rest;
using Reddit;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWaggles
{
    class RedditHelper
    {
        public int index;
        public RestUserMessage redditMessageToTrack;
        public ulong redditIDtoTrack;
        public SocketCommandContext redContext;
        public List<Post> postDictionary = new List<Post>();
        public int getAmount()
        {
            return postDictionary.Count;
        }
        public Post getChosenPost()
        {
            return postDictionary[index];
        }
        public void setTrackingData(ulong id, RestUserMessage msg, SocketCommandContext context)
        {
            this.redditIDtoTrack = id;
            this.redditMessageToTrack = msg;
            this.redContext = context;
        }
        public string getInfo(string link, bool isNSFW)
        {
            foreach(Post post in postDictionary)
            {
                if (!post.Listing.IsSelf)
                {
                    if (((LinkPost)post).URL == link)
                    {
                        if (isNSFW || post.NSFW == false)
                        {
                            return ("**" + post.Title + "** \nPoster: " + post.Author + "\nUpvotes: "
                        + post.Score + " Comments: " + post.Listing.NumComments + "\nhttps://www.reddit.com" + post.Permalink);
                        }
                        else if (isNSFW == false && post.NSFW)
                        {
                            return "Sorry, this link is nsfw!";
                        }
                    }
                }
            }
            return "No matching link found, sorry! Are you in the right channel?";
        }
    }
}
