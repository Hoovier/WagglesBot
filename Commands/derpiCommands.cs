﻿using CoreWaggles;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WagglesBot.Modules;
using DerpiRevRoot = WagglesBot.Modules.DerpiReverseResponse.RootObject;
using DerpiRevSearch = WagglesBot.Modules.DerpiReverseResponse.Image;
// Alias for Derpibooru Response objects.
using DerpiRoot = WagglesBot.Modules.DerpibooruResponse.Rootobject;
using DerpiFeatured = WagglesBot.Modules.DerpibooruResponse.FeaturedObject;
using DerpiSearch = WagglesBot.Modules.DerpibooruResponse.Image;
using CoreWaggles.Commands;
using Discord.Rest;
using Discord;

public class DerpibooruComms : ModuleBase<SocketCommandContext>
{
    // TODO: Make a better endpoint reference repository.
    // The Derpibooru API endpoint.
    public readonly string baseURL = "https://derpibooru.org/api/v1/json/search/images";
    // The Derpibooru Reverse Image Search endpoint.
    public readonly string reverseURL = "https://derpibooru.org/api/v1/json/search/reverse?url=";

    // Available sorting methods for Derpibooru.
    // "score" is the default "unsorted" value from the prior version of this command.
    public readonly string[] sortingOptions = {"created_at", "wilson", "relevance", "random", "score"};

    public CommandService _command { get; set; }
    
    [Command("reverse")]
    [Alias("r")]
    public async Task DerpiReverse([Remainder]string url) {
         // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Validate that the user input a URL.
        if (string.IsNullOrEmpty(url)) {
            await ReplyAsync("No URL given, please provide a valid URL!");
            return;
        } else if (DerpiHelper.IsReachableUrl(url)) {
            await ReplyAsync("Invalid or inaccessible URL: `" + url + "`\nPlease try again, or contact Hoovier!");
            return;
        }

        // If we have a URL, then make a scraper request.
        // Deserialize (from JSON to DerpibooruResponse.RootObject) the Derpibooru search results.
        string DerpiJson = Get.ReverseSearch(this.reverseURL + url).Result;
        DerpiRevRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRevRoot>(DerpiJson);

        // Convert Search Array to a List, to use List functionality.
        List<DerpiRevSearch> imageList = DerpiResponse.images;
        if (imageList.Count == 0) {
            await ReplyAsync("Could not find the image on Derpibooru.org!");
        } else {
            // Get search result element. First element if there are more than one.
            DerpiRevSearch element =  imageList.First();
            // Determine if Channel allows NSFW content or not.
            bool safeOnly = !DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate;
            
            if (safeOnly && !DerpiHelper.IsElementSafe(element.tags)) {
                // If there is a NSFW artwork searched on a SFW channel, do not display result and inform the user.
                await ReplyAsync("Result found, but is NSFW. Please enable NSFW on this channel to view result, or ask again on an approved channel or private message.");
            } else {
                // Display the image link if allowed!
                string derpiURL = "https://derpibooru.org/" + imageList.First().id;
                await ReplyAsync("Result found on Derpibooru here: " + derpiURL);
                Global.LastDerpiID[Context.Channel.Id] = imageList.First().id.ToString();
            }
        }
    }
    [Command("reverse")]
    [Alias("r")]
    public async Task DerpiReverseNoArg()
    {
        if (Global.miscLinks.ContainsKey(Context.Channel.Id))
        {
            await DerpiReverse(Global.miscLinks[Context.Channel.Id]);
        }
        else
        {
            await ReplyAsync("There are no links in my memory! Post one first!");
        }
    }
    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiDefault([Remainder]string search) {
        // Regular Derpi search should just be the Sorted one with a default sorting option.
        await DerpiMaster(false, 4, search, "1");
    }

    [Command("dpage")]
    [Alias("dp")]
    public async Task DerpiPage(int page, [Remainder] string search)
    {
        // Regular Derpi search should just be the Sorted one with a default sorting option.
        await DerpiMaster(false, 4, search, page.ToString());
    }

    [Command("derpi")]
    [Alias("d")]
    public async Task DerpiSorted(int Sort, [Remainder]string search) {
        await DerpiMaster(false, Sort, search, "1");
    }

    // "Master" Derpibooru/~derpi searching method. Not a discord-accessible method.
    private async Task DerpiMaster(bool artistAsLink, int Sort, string search, string page) {
        // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Validate that a valid sorting option was chosen.
        if (Sort < 0 || Sort >= this.sortingOptions.Length) {
            await ReplyAsync("Invalid sorting option: " + Sort + ". Please try again");
            return;
        }
        // Choose sorting method from the available list.
        string sortParam = this.sortingOptions[Sort];

        // Set up the base query parameters.
        // "q" is our search terms
        // "filter_id" is the filter against content banned by Discord TOS.
        Dictionary<string,string> queryParams = new Dictionary<string,string>() {
            {"filter_id", "178065"},
            {"sf", sortParam}, 
            {"sd", "desc"}, 
            {"per_page", "50"},
            {"page", page},
        };

        // If the channel is not on the list of NSFW enabled channels do not allow NSFW results.
        // the second part checks if the command was executed in DMS, DM channels do not have to be added to the NSFW enabled list.
        // In DMs the first check will fail, and so will the second, allowing for nsfw results to be displayed.
        bool safeOnly = !DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate;

        // Add search to the parameters list, with "AND safe" if it failed the NSFW check.
        queryParams.Add("q", safeOnly ? $"{search}+AND+safe" : search);

        // Build the full request URL.
        string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, queryParams);
        
        // Global.DerpiSearchCache is a dictionary-based cache with the last search result in that channel, if applicable.
        // Always stores results globally for other commands like ~next to keep track.
        Global.DerpiSearchCache[Context.Channel.Id] = Get.Derpibooru(requestUrl).Result;

        // Deserialize (from JSON to DerpibooruResponse.RootObject) the Derpibooru search results.
        DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.DerpiSearchCache[Context.Channel.Id]);

        // Actual request an. Try-catch to softly catch exceptions.
        try {
            // Convert Search Array to a List, to use List functionality.
            List<DerpiSearch> imageList = DerpiResponse.images;
            if (imageList.Count == 0) {
                await ReplyAsync("No results! The tag may be misspelled, the chosen page number might be too high, or the results could be filtered out due to channel!");
                return;
            }
            // Get random number generator and random entry.
            var rng = new Random();
            int rand = rng.Next(imageList.Count);
            Global.DerpibooruSearchIndex[Context.Channel.Id] = rand + 1;
            DerpiSearch randomElement = imageList.ElementAt(rand);

            // Add image ID to Global.links.
            // TODO: Describe where this is used better?
            Global.LastDerpiID[Context.Channel.Id] = randomElement.id.ToString();
            RestUserMessage msg = await Context.Channel.SendMessageAsync(
                DerpiHelper.BuildDiscordResponse(randomElement, artistAsLink, !safeOnly)
            );
            await msg.AddReactionAsync(new Emoji("▶"));
            //set random info for running ~enext through emoji reactions.
            Global.derpiContext[Context.Channel.Id] = Context;
            Global.derpiMessageToTrack[Context.Channel.Id] = msg.Id;

            

        } catch {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }

    // Cycles through the page of ~derpi results.
    [Command("next")]
    [Alias("n")]
    public async Task DerpiNext()
    {       
        DerpiRoot DerpiResponse;

        // Check if an a "~derpi" search has been made in this channel yet.
        // We need to deserialize this in order to get the result set size.
        // TODO: Store the result size globally? To avoid deserializing so soon.
        if (Global.DerpiSearchCache.ContainsKey(Context.Channel.Id)) {
            DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.DerpiSearchCache[Context.Channel.Id]);
        } else {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) to get some results before I can hoof you over more silly!");
            return;
        }

        // If ~next goes off the globally cached page, loop around the beginning again.
        if(Global.DerpibooruSearchIndex[Context.Channel.Id] + 1 > DerpiResponse.images.Count()) {
            Global.DerpibooruSearchIndex[Context.Channel.Id] = 0;
        }

        // The `~next` command is just used again
        await DerpiNextPick(1);
    }

    // Post the given amount of images!
    [Command("next")]
    [Alias("n")]
    public async Task DerpiNextPick(int amount)
    {
        await Context.Channel.TriggerTypingAsync();
        string response = $"Posting {amount} links:\n";
        //prevents the useless "posting 1 links" thing.
        if (amount == 1)
        {
            response = "";
        }
        

        DerpiRoot DerpiResponse;

        // Check if an a "~derpi" search has been made in this channel yet.
        if (Global.DerpiSearchCache.ContainsKey(Context.Channel.Id)) {
            DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Global.DerpiSearchCache[Context.Channel.Id]);
        } else {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) to get some results before I can hoof you over more silly!");
            return;
        }
       
        if (DerpiResponse.images.Count() == 0) 
        {
            // No Results Message.
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            return;
        }
        else if(DerpiResponse.images.Count < amount)
        {
            response = "Not enough images to post, posting all \n";
            amount = DerpiResponse.images.Count;
        }
        else if (DerpiResponse.images.Count() == 1) {
            // Only a single result, no pagination.
            await ReplyAsync("Only a single result in this image set.\n");
            // No return, let it continue to parsing the image below.
        } 
        
        //check user provided amount
        if (amount < 1 || amount > 5)
        {
            await ReplyAsync("Pick a number between 1 and 5!");
            return;
        }

        // If ~next goes off the globally cached page, loop around the beginning again.
        if (Global.DerpibooruSearchIndex[Context.Channel.Id] + amount > DerpiResponse.images.Count()) {
            Global.DerpibooruSearchIndex[Context.Channel.Id] = 0;
        }
        for (int counter = 0; counter < amount; counter++)
        {
            if (DerpiResponse.images.Count < Global.DerpibooruSearchIndex[Context.Channel.Id] +1)
            {
                await ReplyAsync("Reached end of results, resetting index. Use ~enext to start again.");
                Global.DerpibooruSearchIndex[Context.Channel.Id] = 0;
            }
            //if everythings fine, increase index by 1
            else
            {
                response = response + DerpiHelper.BuildDiscordResponse(DerpiResponse.images[Global.DerpibooruSearchIndex[Context.Channel.Id]]) + "\n";
                Global.DerpibooruSearchIndex[Context.Channel.Id]++;
            }
            
        }

        // Add image ID to Global.links.
        // TODO: Describe where this is used better?
        Global.LastDerpiID[Context.Channel.Id] = DerpiResponse.images[Global.DerpibooruSearchIndex[Context.Channel.Id] - 1].id.ToString();

        RestUserMessage msg = await Context.Channel.SendMessageAsync(response);
        await msg.AddReactionAsync(new Emoji("▶"));
        //set random info for running ~enext through emoji reactions.
        Global.derpiContext[Context.Channel.Id] = Context;
        Global.derpiMessageToTrack[Context.Channel.Id] = msg.Id;
    }

    // Gets the number of search results for a query.
    [Command("derpist")]
    [Alias("st", "count", "amount")]
    public async Task DerpistAsync([Remainder]string search)
    {
        await Context.Channel.TriggerTypingAsync();

        // Same base query as ~derpi, discounting sorting and asking for less results.
        Dictionary<string,string> queryParams = new Dictionary<string,string>() {
            {"filter_id", "178065"},
            {"per_page", "1"},
            {"page", "1"},
            {"q", search},
        };

        // Build the full request URL.
        string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, queryParams);

        // Make the request, and parse the JSON into a C#-friendly object.
        DerpiRoot results = JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru(requestUrl).Result);

        await ReplyAsync($"Total results: {results.total}");
    }

    [Command("artist")]
    [Alias("a")]
    public async Task ArtistAsync([Remainder]string srch)
    {
        // Get image ID from art URL.
        int imageID = Global.ExtractBooruId(srch);
        // Get Derpibooru search on this specific ID. Display artist as link!
        await DerpiMaster(true, this.sortingOptions.Length - 1, $"id:{imageID}", "1");
    }
    
    [Command("artist")]
    [Alias("a")]
    public async Task ArtistNoLink()
    {
        await Context.Channel.TriggerTypingAsync();

        // Get last Derpibooru Image ID from global cache.
        int imageID;
        
        // Check if an a "~derpi" search has been made in this channel yet.
        if (!Global.LastDerpiID.ContainsKey(Context.Channel.Id)) {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) to get some results before I can hoof you over more silly!");
            return;
        }

        // Try to parse the global cache into an integer ID.
        if (int.TryParse(Global.LastDerpiID[Context.Channel.Id], out imageID)) {
            string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, new Dictionary<string,string>() {
                {"filter_id", "178065"},
                {"q", $"id:{imageID}"},
            });
            string DerpiJson = Get.Derpibooru(requestUrl).Result;
            DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(DerpiJson);

            if (DerpiResponse.images.Count == 0) {
                // Given ID does not exist.
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
            } else {
                // Get artist Tag Link(s) and print image URL as well.
                bool safeOnly = !DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate;
                string artistLinks = DerpiHelper.BuildArtistTags(DerpiResponse.images[0], true, !safeOnly);
                await ReplyAsync($"https://derpibooru.org/{imageID}\n{artistLinks}");
            }
        } else {
            // Can't get an ID from cache.
            await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
        }
    }
    
    // Get tags for the image in global cache.
    // Not a single Derpibooru call made, all non-cache cases return a helpful message.
    [Command("derpitags")]
    [Alias("dt", "tags", "tag")]
    public async Task DerpiTagsNoLink() {
        // Trigger "user is typing..." message.
        await Context.Channel.TriggerTypingAsync();

        // Check if an a "~derpi" search has been made in this channel yet.
        if (Global.LastDerpiID.ContainsKey(Context.Channel.Id)) {
            // Handmade link + the channel ID to get the last stored ID.
            string builtLink = "https://derpibooru.org/api/v1/json/search/images?q=id%3A" + Global.LastDerpiID[Context.Channel.Id] + "&filter_id=56027";
            // Retrieves JSON and saves as string.
            string JSONresponse = Get.Derpibooru(builtLink).Result;
            // Finally makes the derpibooru object to leverage DerpiTagsDisplay().
            DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(JSONresponse);

            // Check if the ID existed or yielded any results.
            if (DerpiResponse.images.Count < 1) {
                await ReplyAsync("No results found, please call `~derpi` again or post another link!");
            } else {
                await DerpiTagsDisplay(DerpiResponse.images[0]);
            }
        } else {
            await ReplyAsync("You need to call `~derpi` (`~d` for short) or post a link before I can hoof over the tags!");
        }
    }

    // Get tags for the provided image ID or URL.
    [Command("derpitags")]
    [Alias("dt", "tags", "tag")]
    public async Task DerpiTags([Remainder]string search) {
         // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Image ID placeholder.
        int imageID;

        // First, check if search term is an image ID. An integer.
        if (int.TryParse(search, out imageID)) {
            // Continue as normal, imageID is stored in the variable.
        } 
        // Second, if not an integer, test if it is a valid Booru URL.
        else if (Global.IsBooruUrl(search)) {
            // 1. Attempt to parse Integer ID from Booru URL.
            imageID = Global.ExtractBooruId(search);
            // 2a. Unparseable, quit early and apologize to user.
            if (imageID == -1) {
                await ReplyAsync("Sorry, I couldn't understand the URL you gave me. Please try again with another URL, or contact Hoovier if something is wrong.");
                await ReplyAsync("If you think this was in error, please contact Hoovier with the request you made.");
                return;
            }
            // 2b. Continue as normal, imageID is stored in the variable.
        }
        // Lastly, if not a valid Booru URL or ID, output a gentle error message.
        else {
            await ReplyAsync("Sorry, I couldn't understand the URL you gave me. Please try again with another URL, or contact Hoovier if something is wrong.");
            return;
        }

        // If you reach here, the "else" return case didn't happen and we can query Derpibooru.
        // We can use imageID to uniformly query DerpiBooru.
        string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, new Dictionary<string,string>() {
            {"filter_id", "56027"},
            {"q", $"id:{imageID}"},
        });
        string DerpiJson = Get.Derpibooru(requestUrl).Result;
        DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(DerpiJson);

        if (DerpiResponse.images.Count == 0) {
            await ReplyAsync("No results! The ID or URL provided might have had a typo or is deleted no longer exists.");
            await ReplyAsync("If you think this was in error, please contact Hoovier with the request you made.");
        } else {
            await DerpiTagsDisplay(DerpiResponse.images[0]);
        }
    }

    // Formatter for the results of both ~dt variants. Not a discord-accessible method.
    private async Task DerpiTagsDisplay(DerpiSearch element) {
        // Get "Info" block, made up of the Artists list and Image Rating.
        string artists = DerpiHelper.BuildArtistTags(element);
        string rating = DerpiHelper.GetImageRating(element.tags);

        // Display `~dt` info block and all image tags.
        await ReplyAsync($"Info: **{rating}, {artists}**\n\nAll tags: ```{String.Join(", ",element.tags)}```");
    }

    [Command("imfeelinglucky")]
    [Alias("lucky")]
    public async Task DerpiLuckySearch([Remainder]string search) {
        await DerpiLuckyMulti(1, search);
    }

    [Command("imfeelinglucky")]
    [Alias("lucky")]
    public async Task DerpiLuckyMulti(int num, [Remainder]string search) {
        // Broadcasts "User is typing..." message to Discord channel.
        await Context.Channel.TriggerTypingAsync();

        // Limit ~lucky amounts.
        if (num < 1 || num > 5) {
            await ReplyAsync("You need to pick a number bigger than 0 and no more than 5");
            return;
        }

        // Set up the base query parameters.
        // Sorted randomly, gets "num" amount of items!
        Dictionary<string,string> queryParams = new Dictionary<string,string>() {
            {"filter_id", "178065"},
            {"sf", "random"}, 
            {"sd", "desc"}, 
            {"per_page", num.ToString()},
            {"page", "1"},
        };

        // If the channel is not on the list of NSFW enabled channels do not allow NSFW results.
        // the second part checks if the command was executed in DMS, DM channels do not have to be added to the NSFW enabled list.
        // In DMs the first check will fail, and so will the second, allowing for nsfw results to be displayed.
        bool safeOnly = !DBTransaction.isChannelWhitelisted(Context.Channel.Id) && !Context.IsPrivate;

        // Add search to the parameters list, with "AND safe" if it failed the NSFW check.
        queryParams.Add("q", safeOnly ? $"{search}+AND+safe" : search);

        // Build the full request URL.
        string requestUrl = DerpiHelper.BuildDerpiUrl(this.baseURL, queryParams);

        // Deserialize (from JSON to DerpibooruResponse.RootObject) the Derpibooru search results.
        DerpiRoot DerpiResponse = JsonConvert.DeserializeObject<DerpiRoot>(Get.Derpibooru(requestUrl).Result);

        // Actual request an. Try-catch to softly catch exceptions.
        try {
            if (DerpiResponse.images.Count == 0) {
                await ReplyAsync("No results! The tag may be misspelled, or the results could be filtered out due to channel!");
                return;
            }
            else if (DerpiResponse.images.Count < num) {
                await ReplyAsync($"Not enough results to post {num}, but here is what we have found!");
            }

            // Print all results of the search!
            // Sorted randomly by Derpibooru already, and will be between 1-5 elements.
            string message = $"Listing {DerpiResponse.images.Count} results\n";
            message += String.Join("\n", 
                DerpiResponse.images.Select(
                    element => element.representations.full));

            await ReplyAsync(message);

        } catch {
            await ReplyAsync("Sorry! Something went wrong, your search terms are probably incorrect.");
            return;
        }
    }

    [Command("featured")]
    [Alias("f")]
    public async Task DerpiFeatured() {
        string requestUrl = "https://derpibooru.org/api/v1/json/images/featured";
        //use DerpiFeatured cause this API endpoint just returns a single image not a list of them!
        DerpiFeatured DerpiResponse = JsonConvert.DeserializeObject<DerpiFeatured>(Get.Derpibooru(requestUrl).Result);
        // Return the stored Featured Image.
        await ReplyAsync("https://derpibooru.org/" + DerpiResponse.image.id);
        Global.LastDerpiID[Context.Channel.Id] = DerpiResponse.image.id.ToString();
        
    }
}

/**
    Derpi Utility class to aggregate common functionality.
    TODO: make this a method of the DerpibooruComms class or find if it is more generic across command files.
 */
public class DerpiHelper {

    // Pick a random element from a List.
    // Todo: Remove? Make more IEnumerable/generic for Array/Linq usage?
    public static List<T> GetRandomElements<T>(IEnumerable<T> list, int elementsCount) {
        return list.OrderBy(arg => Guid.NewGuid()).Take(elementsCount).ToList();
    }

    /// <summary>Takes a base URL, and appends query parameters to it.</summary>
    /// <param name="url">A base URL string. May also have existing parameters.</param>
    /// <param name="queryParams">A Dictionary of Key-Value pairs for HTML paramters.</param>
    /// <returns>The fully built search URL.</returns>
    public static string BuildDerpiUrl(string url, IDictionary<string, string> queryParams) {
        // Takes the parameter pairs such as "perpage" and "50", and pairs them into strings "perpage=50".
        string[] queryPairs = queryParams.Select(entry => $"{entry.Key}={entry.Value}").ToArray();
        
        // Joins together the above parameter pairs into one string such as "perpage=50&page=6".
        string paramString = string.Join("&", queryPairs);

        // Detect if the current URL has a query string.
        // If so, append with "&", else append with "?" to start it off.
        return $"{url}{(url.Contains("?") ? "&" : "?")}{paramString}";
    }

    /// <summary>Validate that a given string is a URL and test if it is reachable by Waggles.</summary>
    /// <param name="url">A string that may or may not be a URL.</param>
    /// <returns>TRUE if the string is a reachable URL, FALSE if it isn't.</returns>
    /// <see>https://stackoverflow.com/a/3808841</see>
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

    // Takes a Derpibooru singular search result node and returns a string message to be sent to Discord.
    /// <summary>Builds the artist tag section of most Derpibooru results.</summary>
    /// <param name="element">A Derpibooru Search Result Element.</param>
    /// <param name="artistAsLink">Whether or not to render it as a list of tags, or a list of links.</param>
    /// <param name="NSFW">Only valid if "artistAsLink" is true, then we need to flag if we are showing SFW results or not.</param>
    /// <returns>A string response consisting of the image element itself, and an artist tags block.</returns>
    public static string BuildDiscordResponse(DerpiSearch element, bool artistAsLink = false, bool NSFW = false) {

        // Get the full image URL in the format "//derpicdn.net/img/view/YYYY/M/d/IMAGE_ID.png"
        // Prepend the protocol, HTTPS, to the incomplete URL representation.
        string results = element.representations.full;

        //if its an ~artist command, respond with derpibooru/id link instead of derpicdn one
        if (artistAsLink)
        {
             results = "https://derpibooru.org/" + element.id;
        }

        // Get the artist block.
        string artistBlock = DerpiHelper.BuildArtistTags(element, artistAsLink, NSFW);

        // Add a newline in between if there are any results.
        if (!String.IsNullOrEmpty(artistBlock)) {
            artistBlock = "\n" + artistBlock;
        }

        return results + artistBlock;
    }

    /// <summary>Builds the artist tag section of most Derpibooru results.</summary>
    /// <param name="element">A Derpibooru Search Result Element.</param>
    /// <param name="artistAsLink">Whether or not to render it as a list of tags, or a list of links.</param>
    /// <param name="NSFW">Only valid if "artistAsLink" is true, then we need to flag if we are showing SFW results or not.</param>
    /// <returns>A string, either a list of artist names, or a list of URLs to artist tagged works.</returns>
    public static string BuildArtistTags(DerpiSearch element, bool artistAsLink = false, bool NSFW = false) {
        string artistResult;

        // Get a distilled list of artist tags from the full tag listing.
        string[] artistTags = element.tags.ToArray();
        artistTags = Array.FindAll(artistTags, tag => tag.Contains("artist:"));

        // Safety Suffix ("AND safe") filter.
        string safetySuffix = NSFW ? String.Empty : "+AND+safe";

        // Parse artist tag response accordingly.
        switch (artistTags.Length) {
            case 0:
                // Unedited screencaps uploaded have no artist tags.
                bool isScreencap = element.tags.Contains("screencap");
                artistResult = isScreencap ? String.Empty : "Problem finding artist";
                break;
            case 1:
                artistResult = artistTags[0].TrimStart();
                if (artistAsLink) {
                    artistResult = $"https://derpibooru.org/search?q={artistResult}{safetySuffix}&filter_id=178065";
                }
                break;
            default:
                artistResult = artistAsLink 
                    ? String.Join("\n", artistTags.Select(t => $"https://derpibooru.org/search?q={t.Trim()}{safetySuffix}&filter_id=178065"))
                    : String.Join(' ', artistTags);
                break;
        }

        return artistResult;
    }

    // TODO: ADD BETTER DOCUMENT/SUMMARY.
    // Check if a Derpibooru Search result is SFW, by way of scanning for a "safe" tag.
    public static bool IsElementSafe(List<string> tags) {
        return DerpiHelper.GetImageRating(tags).Equals("safe");
    }

    // Get Derpibooru Image Rating.
    public static string GetImageRating(List<string> tags) {
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
        foreach (string tag in tags) {
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
}

// TODO:
//  -- Make this better!
//  -- Add more test cases for all Unit-Functions! (that aren't internet-dependent)
//  -- Add documentation!
//  -- Always add more test cases to cover edge cases!
// This isn't just a class to test functions once, but necessary whenever we update
// code! To ensure we never update the logic in such a way that it breaks something.
public class DerpiTest {
    // Test for the DerpiHelper.ExtractBooruId() method.
    // TODO: Name this better, make functions for each test case instead of one monolithic one.
    public void ExtractBooruIdTest() {
        string[] strings = {
            // Page url itself
            "https://derpibooru.org/2131160", 
            // Full, really long titled URL
            "//derpicdn.net/img/view/2019/8/30/2131160__safe_screencap_apple+bloom_carrot+top_golden+harvest_lightning+bolt_parasol_white+lightning_hearts+and+hooves+day+%28episode%29_banner_blushin.png", 
            // "Sized" representation. Works with thumb, small, medium, etc
            "//derpicdn.net/img/2019/8/30/2131160/thumb_tiny.png", 
            // Actual full-sized image short URL
            "//derpicdn.net/img/view/2019/8/30/2131160.png",
            // Example of invalid input, should return -1 at the end.
            "score",
            // Same thing, but with a .webm sample for pesky 4-character extensions.
            "//derpicdn.net/img/view/2019/8/30/2131011__safe_screencap_applejack_fluttershy_pinkie+pie_twilight+sparkle_sonic+rainboom+%28episode%29_animated_blinking_bouncing_cheering_cloudsdale_cu.webm",
            "//derpicdn.net/img/2019/8/30/2131011/thumb_small.webm",
            "//derpicdn.net/img/view/2019/8/30/2131011.webm",
            // Or even file extensions with numeric characters in them!
            "//derpicdn.net/img/2019/8/30/2131011/full.mp4",
        };

        foreach (string str in strings) {
            Console.WriteLine (Global.ExtractBooruId(str));
        }
        Console.WriteLine("Test complete. ");
    } 
}
