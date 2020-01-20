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
    }
}
