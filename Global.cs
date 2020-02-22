using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Reddit;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
namespace CoreWaggles
{
    internal static class Global
    {
        internal static ulong MessageIdToTrack { get; set; }

        internal static RestUserMessage MessageTotrack { get; set; }
        private static int ploxs = 0;
        internal static int Ploxrs { get { return ploxs; } set { ploxs = ++ploxs; } }
        internal static List<string> todo = new List<string>();
        internal static Dictionary<ulong, ulong> safeChannels = new Dictionary<ulong, ulong>();
        internal static Dictionary<ulong, string> searchesD = new Dictionary<ulong, string>();
        internal static Dictionary<ulong, string> links = new Dictionary<ulong, string>();
        internal static Dictionary<string, string> excomm = new Dictionary<string, string>();
        internal static Dictionary<ulong, string> miscLinks = new Dictionary<ulong, string>();
        internal static Dictionary<ulong, List<WittyObject>> wittyDictionary = new Dictionary<ulong, List<WittyObject>>();

        internal static RedditClient reddit = new RedditClient();
        internal static Dictionary<ulong, RedditHelper> redditDictionary = new Dictionary<ulong, RedditHelper>();
        internal static int searched;
        // The Featured Derpibooru Image, and the Timestamp to track when it was last stored.
        internal static int featuredId = 0;
        internal static long featuredLastFetch = 0;
        //holds the JSON for the last e621 search in the channel
        internal static Dictionary<ulong, string> e621Searches = new Dictionary<ulong, string>();
        //holds the index of last used element of JSON array in cache
        //probably come up with a better name for this
        internal static int e621SearchIndex;

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
        //for holding the directorys that will be set and checked by checkDirsArePresent()
        private static string serverDir;
        private static string targetDir;

        public static void checkDirsArePresent(ulong serverID, ulong channelID, string channelName)
        {
            //set directorys
            serverDir = @"C:\Users\javie\source\repos\CoreWaggles\CoreWaggles\savedImages\" + serverID + @"\";
            targetDir = @"C:\Users\javie\source\repos\CoreWaggles\CoreWaggles\savedImages\" + serverID + @"\" + channelName + "-" + channelID + @"\";
            //if the server folder does not exist, neither will the targetDir, so make both
            if (!Directory.Exists(serverDir))
            {
                Directory.CreateDirectory(serverDir);
                Directory.CreateDirectory(targetDir);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Server and Channel directorys did not exist! Creating now.");
                Console.ResetColor();
            }
            //if the server directory exists, make sure targetDir exists or make it
            else if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Channel directory did not exist! Creating now.");
                Console.ResetColor();
            }
        }
        public static void downloadAttOrEmb(string url, SocketUserMessage message )
        {
            // Get the file extension from the link.
            //   First, remove any URL query parameters. (Such as https://www.example.com/image.png?v=2)
            //   Second, grab the file extension if one exists.
            // @see: https://stackoverflow.com/a/23229959
            Uri linkUri = new Uri(url);
            string linkPath = linkUri.GetLeftPart(UriPartial.Path);
            string linkExt = Path.GetExtension(linkPath).ToLower();
            //filename will be the current date, including seconds and milliseconds to prevent files being named the same thing
            string filename = DateTime.Now.ToString("yyyy_MM-dd-hh-mm-ss-fff");
            //wrap in try-catch cause the webclient might fail to download.
            try
            {
                //reuse code that @Max wrote for ~save command
                switch (linkExt)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".webm":
                    case ".gif":
                        // Instantiate a WebClient, download the file, then automatically dispose of the client when finished.
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(url, $@"{targetDir}{filename}{linkExt}");
                        }
                        Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{message.Channel.Name}]");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Saved an embedded/attached link from " + message.Author.Username + " at " + filename);
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
