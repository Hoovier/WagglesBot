using Discord.Rest;
using Reddit;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CoreWaggles
{
    internal static class Global
    {
        //dictionary that is ChannelID:MessageID
        internal static Dictionary<ulong, ulong> MessageIdToTrack = new Dictionary<ulong, ulong>();
        //channelID:MessageObject
        internal static Dictionary<ulong, RestUserMessage> MessageTotrack = new Dictionary<ulong, RestUserMessage>();

        internal static Dictionary<ulong, string> DerpiSearchCache = new Dictionary<ulong, string>();
        internal static Dictionary<ulong, string> LastDerpiID = new Dictionary<ulong, string>();
        internal static Dictionary<ulong, string> miscLinks = new Dictionary<ulong, string>();

        //Array of messages for deletion, stores at max 3 per channel
        internal static Dictionary<ulong, MessageLog> lastMessage = new Dictionary<ulong, MessageLog>();

        internal static RedditClient reddit = new RedditClient();
        internal static Dictionary<ulong, RedditHelper> redditDictionary = new Dictionary<ulong, RedditHelper>();
        //index of current image in channel
        internal static Dictionary<ulong, int> DerpibooruSearchIndex = new Dictionary<ulong, int>();
        // The Featured Derpibooru Image, and the Timestamp to track when it was last stored.
        internal static int featuredId = 0;
        internal static long featuredLastFetch = 0;
        //holds the JSON for the last e621 search in the channel
        internal static Dictionary<ulong, string> e621Searches = new Dictionary<ulong, string>();
        //holds the index of last used element of JSON array in cache
        //probably come up with a better name for this
        internal static Dictionary<ulong, int> e621SearchIndex = new Dictionary<ulong, int>();

        // Test if string is indeed a Derpibooru or valid URL.
        // Quick validation, is this in either Derpi* domain, and can it possibly contain a valid ID?
        public static bool IsBooruUrl(string search) {
            return (search.Contains("derpicdn.net") || search.Contains("derpibooru.org")) && search.Any(char.IsDigit);
        }

        // Assumes the URL is a valid DerpiBooru domain.
        // Returns the Image ID, if possible, from the URL.
        public static int ExtractBooruId(string url) {
            // Test case Regex Patterns.
            string[] patterns = {       
                // Easy case, a direct "derpibooru.org/IMAGE_ID" URL.
                @"(?i)derpibooru.org\/(\d+)",
                // Straight-foward case, direct full-size "derpicdn.net/----/IMAGE_ID.XYZ" URL.
                // Support .png, .gif, .webm, or any other URL's derpi supports.
                @"(?i)derpicdn.net\/.*?(\d+)\.[a-z]{3,4}",
                // "Sized" representations, like ".../IMAGE_ID/tiny_thumb.XYZ"
                @"(?i)derpicdn.net\/.*?(\d+)\/[a-z_]+\.[a-z0-9]{3,4}",
                // "Full", really long image name. ".../IMAGE_ID__safe_tag1+tag2+tag3.XYZ"
                @"(?i)derpicdn.net\/.*?(\d+)__",
            };
            Match match;
            foreach (string pattern in patterns) {
                // The match data is stored in "match", then tested for success.
                if ((match = Regex.Match(url, pattern)).Success) {
                // If the pattern matches, exist early with the value of "match".
                // Groups[0] is always the value of the initial string. Groups[1] is the first match.
                return int.Parse(match.Groups[1].Value);
                }
            }

            // If we make it here, throw -1 as an invalid result flag.
            return -1;
        }
    }
}
