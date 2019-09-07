using Discord.Commands;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

// Gets Globals
using CoreWaggles;

using DerpiRoot = WagglesBot.Modules.DerpibooruResponse.Rootobject;
using DerpiSearch = WagglesBot.Modules.DerpibooruResponse.Search;

public class DerpibooruCommands : ModuleBase<SocketCommandContext>
{
    // The Derpibooru API endpoint.
    public readonly string baseURL = "https://derpibooru.org/search.json";
    
    // Available sorting methods for Derpibooru.
    // "score" is the default "unsorted" value from the prior version of this command.
    public readonly string[] sortingOptions = {"created_at", "wilson", "relevance", "random", "score"};

    public CommandService _command { get; set; }

    [Group("reverse")]
    [Alias("r")]
    public class ReverseCommand : ModuleBase<SocketCommandContext> {
        // The Derpibooru Reverse Image Search endpoint.
        public readonly string reverseURL = "https://derpibooru.org/search/reverse.json?scraper_url=";

        [Command]
        public async Task NoArgs() {
            if (Global.miscLinks.ContainsKey(Context.Channel.Id)) {
                await UrlArg(Global.miscLinks[Context.Channel.Id]);
            } else {
                await ReplyAsync("There are no links in my memory! Post one first!");
            }
        }

        [Command]
        public async Task UrlArg([Remainder]string url) {
            // Broadcasts "User is typing..." message to Discord channel.
            await Context.Channel.TriggerTypingAsync();

            // Validate that the user input a URL.
            if (string.IsNullOrEmpty(url)) {
                await ReplyAsync("No URL given, please provide a valid URL!");
                return;
            } else if (DerpibooruCommands.IsReachableUrl(url)) {
                await ReplyAsync("Invalid or inaccessible URL: `" + url + "`\nPlease try again, or contact Hoovier!");
                return;
            }

            // If we have a URL, then make a scraper request.
            DerpiRoot DerpiResponse = DerpibooruCommands.MakeDerpiRequest($"{this.reverseURL}{url}");

            if (DerpiResponse.Search.Length == 0) {
                await ReplyAsync("Could not find the image on Derpibooru.org!");
            } else {
                // Select the first, or only, result.
                DerpiSearch element = DerpiResponse.Search[0];

                // Determine if Channel allows NSFW content or not, and if the image is safe.
                bool safeOnly = !Global.safeChannels.ContainsKey(Context.Channel.Id) && !Context.IsPrivate;
                bool isImageSafe = DerpibooruCommands.GetImageRating(element).Equals("safe");

                if (safeOnly && !isImageSafe) {
                    // If there is a NSFW artwork searched on a SFW channel, do not display result and inform the user.
                    await ReplyAsync("Result found, but is NSFW. Please enable NSFW on this channel to view result, or ask again on an approved channel or private message.");
                } else {
                    // Display the image link if allowed!
                    await ReplyAsync("Result found on Derpibooru here: https://derpibooru.org/" + element.id);
                }
            } 
        }
    }

    public static bool IsReachableUrl(string url) {
        try
        {
            //Creating the HttpWebRequest
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            //Getting the Web Response.
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //Returns TRUE if the Status code == 200
            response.Close();
            return (response.StatusCode == HttpStatusCode.OK);
        }
        catch
        {
            //Any exception will returns false.
            return false;
        }
    }

    public static string GetImageRating(DerpiSearch element) {
        // All known Derpibooru Rating Tags.
        string[] ratings = {
            "safe", 
            "suggestive", 
            "questionable", 
            "explicit", 
            "semi-grimdark", 
            "grimdark", 
            "grotesque",
            };

        // Get Image tags, cross reference with Rating Tags.
        foreach (string tag in element.tags.Split(",")) {
            foreach (string rate in ratings) {
                // Return the image rating if a rating tag matches.
                if (tag.Trim().Equals(rate)) {
                    return rate;
                }
            }
        }
        
        // Return "Unrated" if an image is somehow missing a rating.
        // All caps because it shouldn't be possible/exist in Derpi's systems.
        return "UNRATED";
    }

    public static async Task<string> MakeDerpiRequestRaw(string url) {
        // Set Handler to allow for GZIP or Deflate compressed HTTP responses.
        HttpClientHandler handler = new HttpClientHandler();
        handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

        // Set up the HTTP Client for fielding the Derpibooru JSON API.
        using (HttpClient client = new HttpClient(handler)) {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode){
                return await response.Content.ReadAsStringAsync();
            }
            return string.Empty;
        }
    }

    public static DerpiRoot MakeDerpiRequest(string url) {
        // Fetch the raw JSON.
        string DerpiJson = MakeDerpiRequestRaw(url).Result;
        // Return the deserialized object version.
        return Newtonsoft.Json.JsonConvert.DeserializeObject<DerpiRoot>(DerpiJson);
    }
}