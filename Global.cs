using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WagglesBot.Modules;

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
        internal static int searched;
        // The Featured Derpibooru Image, and the Timestamp to track when it was last stored.
        internal static int featuredId = 0;
        internal static long featuredLastFetch = 0;
       internal static void checkURL(string srch, ulong id)
        {
            string requestUrl = $"https://derpibooru.org/search.json?q=id:{srch}+AND+safe&filter_id=164610&sf=score&sd=desc&perpage=50&page=";
            string shortened = srch;
            if (srch.Contains("?"))
            {
                string pattern = @"(\d+)+\?";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.', '?' });
                links[id] = shortened; 
            }
            else if (srch.Contains("booru"))
            {
                string pattern = @"(\d+)";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.', '?' });
                links[id] = shortened;
            }

            else if (srch.Contains("https"))
            {
                string pattern = @"(\d+)+\.";
                Match result = Regex.Match(srch, pattern);
                shortened = result.Value.Trim(new Char[] { ' ', '.' });
                links[id] = shortened;
            }
            else{ }
        }
    }

}
