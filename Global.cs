﻿using Discord.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WagglesBot.Modules;
using System.IO;
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
        internal static void updateExcomm()
        {
            //used to make sure the saved file is always the same as one in memory
            string path = "JSONstorage/extraComms.JSON";
            //this check doesnt work, and I dont know why
            //the if statement always evaluates to true
            //new path is correct, but still should have failed in the past
            if (File.Exists(path))
            {
                //serializes dictionary
                string excommJSON = JsonConvert.SerializeObject(excomm);
                //writes it to file
                File.WriteAllText(path, excommJSON);
            }
            else
            {
                Console.WriteLine("Error writing to extraComms.JSON!");
            }
        }
    }

}
