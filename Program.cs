﻿using System;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Reflection;
using CoreWaggles;
using System.IO;
using CoreWaggles.Commands;
using System.Threading;
using CoreWaggles.Services;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using Discord.Rest;
using System.Timers;
using System.Xml.XPath;
using System.Xml;
using System.Linq;

namespace WagglesBot
{

    class Program
    {
        static void Main(string[] args) => new Program().runBotAsync().GetAwaiter().GetResult();


        public async Task runBotAsync()
        {
            //Reddit Token stuff!
            string[] RedditTokens = System.IO.File.ReadAllLines("redditTokens.txt");
            Global.reddit = new Reddit.RedditClient(RedditTokens[0], RedditTokens[1], RedditTokens[2], RedditTokens[3]);
            string heart = System.IO.File.ReadAllText(@"Commands/MateResponses/heart.JSON");
            string mess = System.IO.File.ReadAllText(@"Commands/MateResponses/mess.JSON");
            Global.MateHeartReactChance = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(heart);
            Global.MateMessageReactChance = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(mess);
            //Waggles = 0, Mona = 1
            string[] keys = System.IO.File.ReadAllLines("Keys.txt");
            string botToken = keys[1];
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                var cmd = services.GetRequiredService<CommandService>();

                client.Log += Log;
                client.JoinedGuild += OnJoinedGuild;
                client.UserJoined += Client_UserJoined;
                client.Ready += async () =>
                {
                    Console.WriteLine("Logged in on Discord as: " + client.CurrentUser.Username);
                    await client.SetGameAsync("with Mona");
                    Console.WriteLine("In " + client.Guilds.Count + " servers!");
                    Console.WriteLine("Logged into Reddit as: " + Global.reddit.Account.Me.Name);
                    Console.WriteLine(File.Exists("WagglesDB.db") ? "Database found!" : "ERROR Database not found!");
                };
                client.ReactionRemoved += Client_ReactionRemoved;
                client.ReactionAdded += async (Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) =>
                {
                    if (Global.redditDictionary.ContainsKey(reaction.Channel.Id))
                    {
                        if (reaction.MessageId == Global.redditDictionary[reaction.Channel.Id].redditIDtoTrack && !reaction.User.Value.IsBot)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Rnext button!");
                            await cmd.ExecuteAsync(Global.redditDictionary[reaction.Channel.Id].redContext, "rnext 5", services);
                        }
                    }
                   if (Global.e621Searches.ContainsKey(reaction.Channel.Id))
                    {
                        if (reaction.MessageId == Global.e621MessageToTrack[reaction.Channel.Id] && !reaction.User.Value.IsBot)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Enext button!");
                            await cmd.ExecuteAsync(Global.e621Context[reaction.Channel.Id], "enext 5", services);
                        }
                    }
                    if (Global.DerpiSearchCache.ContainsKey(reaction.Channel.Id))
                    {
                        if (Global.derpiMessageToTrack.ContainsKey(reaction.Channel.Id))
                        {
                            if (reaction.MessageId == Global.derpiMessageToTrack[reaction.Channel.Id] && !reaction.User.Value.IsBot)
                            {
                                Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Dnext button!");
                                await cmd.ExecuteAsync(Global.derpiContext[reaction.Channel.Id], "next 5", services);
                            }
                        }
                    }
                    
                    SocketGuildChannel chanl = reaction.Channel as SocketGuildChannel;
                    string mateString = DBTransaction.getServerMate(chanl.Guild.Id);
                    var mesg = reaction.Channel.GetMessageAsync(reaction.MessageId);

                    if (!mateString.Contains("NONE"))
                    {
                        string[] mateInfo = mateString.Split(",");
                        Random rand = new Random();

                        if (mateInfo[2] == mesg.Result.Author.Id.ToString() && reaction.Emote.Name == "💖" && !reaction.User.Value.IsBot && rand.Next(100) > 50)
                        {
                            await reaction.Channel.SendMessageAsync(reaction.User.Value.Mention + " Woah woah woah! Thats *my* " + mateInfo[1] + "! Hands off.");
                        }
                    }
                    
                    
                    await OnReactionAdded(cache, channel, reaction);
                };
                services.GetRequiredService<CommandService>().Log += Log;


                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, botToken);
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                //here we make a timer and make it wait to trigger every 10 seconds!
                System.Timers.Timer aTimer = new System.Timers.Timer(10000);
                System.Timers.Timer Stonktimer = new System.Timers.Timer(3600000);
                //this is the actual function that runs when the time runs out
                aTimer.Elapsed += async (object sender, ElapsedEventArgs e) => 
                {
                    //get all reminders!
                    List<ReminderObject> listOfReminders = DBTransaction.getReminders();

                    foreach (ReminderObject item in listOfReminders)
                    {
                        DateTime timeAdded = DateTime.Parse(item.timeAdded);
                        DateTime now = DateTime.Now;
                        //get the amount of time passed since the reminder was added!
                        TimeSpan span = now - timeAdded;
                        //if enough time has passed to be equal to or greater than the specified time interval, go
                        if (span.TotalMinutes >= item.timeInterval)
                        {
                            //gets the server, uses that to get the user, and then finally sends the message
                            try
                            {
                                await client.GetGuild(item.serverID).GetUser(item.userID).GetOrCreateDMChannelAsync().Result.SendMessageAsync(item.title);
                            }
                            catch
                            {
                                Console.WriteLine("Reminder message failed! User: " + item.userID + "Server: " + item.serverID +  " Reminder: " + item.title);
                            }
                            DBTransaction.removeReminder(item.title, item.serverID, item.userID);
                        }
                    }
                };
                Stonktimer.Elapsed += async (object sender, ElapsedEventArgs e) =>
                {

                    //squeeze in OICO check, cause its also on an hour interval
                    string oico = Oico();
                    if (oico != "NONE")
                    {
                        await client.GetGuild(606504338143182868).GetTextChannel(758138741318484039).SendMessageAsync(oico);
                    }
                    //Stonks price check
                    Random rand = new Random();
                    //foreach server, iterate through and change the price of each stonk.
                    foreach (var server in client.Guilds)
                    {
                        string response = "Stonk Updates:";
                        List<Stonk> stonks = DBTransaction.getStonkObj(server.Id);
                        foreach (Stonk stonk in stonks)
                        {
                            int oldPrice = stonk.Price;
                            int newprice;
                            //if the stonk price drops to 50 or less, skyrocket the price to restart its trip down.
                            if (oldPrice < 51)
                            {
                                double increasePercentage = rand.Next(0, 101) * .1;

                                newprice = oldPrice + (int)(oldPrice * increasePercentage);
                            }
                            //otherwise use algorithm from stackexchange
                            else
                            {
                                double volatility = .50;
                                double randDouble = rand.NextDouble();
                                double percentageChange = 2 * volatility * randDouble;
                                if (percentageChange > volatility)
                                {
                                    percentageChange -= (2 * volatility);
                                }
                                double priceChange = oldPrice * percentageChange;
                                newprice = Convert.ToInt32(oldPrice + priceChange);
                            }
                            //change the stonk price in the database
                            DBTransaction.editStonkPrice(stonk.Name, newprice, stonk.ServerID);
                            //construct response
                            response = response + "\n__" + stonk.Name + "__ **Old Price:** " + oldPrice + " **New Price:** " + newprice;
                        }
                        ulong channelID = DBTransaction.getStonkChannel(server.Id);

                        //if there is no Stonk channel in the config, do not post anything.
                        if (channelID != 0) {

                            await client.GetGuild(server.Id).GetTextChannel(channelID).SendMessageAsync(response);
                            }
                    }
                };
                Stonktimer.AutoReset = true;
                Stonktimer.Enabled = true;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
                await Task.Delay(Timeout.Infinite);
            }
        }

        private async Task Client_UserJoined(SocketGuildUser arg)
        {
            //0channel, 1welcome, 2post
            string[] welcomeInfo = DBTransaction.getWelcomeInfo(arg.Guild.Id);
            if(!(welcomeInfo[0] == "NONE"))
            {
                //just in case user has joined earlier without reacting, clear it out
                DBTransaction.RemoveWelcomeUser(arg.Id, arg.Guild.Id);
                ITextChannel channel = (ITextChannel)arg.Guild.GetChannel(Convert.ToUInt64(welcomeInfo[0]));
                var msg = await channel.SendMessageAsync(arg.Mention + " has joined the server.\n" + welcomeInfo[1]);
                await msg.AddReactionAsync(new Emoji("✅"));
                List<SocketGuildUser> temp = new List<SocketGuildUser>();
                temp.Add(arg);
                DBTransaction.updateUserList(temp, arg.Guild.Id);
                DBTransaction.InsertWelcomeUser(arg.Id, arg.Guild.Id, msg.Id);
            }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }

        private async Task OnJoinedGuild(SocketGuild arg)
        {
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                try
                {
                    SocketUser use = client.GetUser(223651215337193472);
                    await use.SendMessageAsync("I joined a new server named: " + arg.Name + "\n" + "Owner info: " + arg.Owner.Username + arg.Owner.Discriminator);
                }
                catch
                {
                    Console.WriteLine("I joined a new server named: " + arg.Name + "\n" + "Owner info: " + arg.Owner.Username + arg.Owner.Discriminator);
                }
            }

        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "⭐")
            {
                var message = await cache.DownloadAsync();
                //get guildID for DB
                var chnl = message.Channel as SocketGuildChannel;
                var Guild = chnl.Guild.Id;

                int count = 0;
                IEmote star = new Emoji("⭐");
                count = message.Reactions[star].ReactionCount;
                if (count > 1)
                {
                    //if quote doesnt exist, add it
                    if (DBTransaction.quoteExists(message.Author.Id, message.Content, Guild) == false)
                    {

                        string messageToAdd = message.Content;
                        //check if the message has a image attached
                        if (message.Attachments.Count > 0)
                        {
                            //combine message with url to message
                            messageToAdd = messageToAdd + "\n" + message.Attachments.ElementAt(0).Url;
                            await message.AddReactionAsync(new Emoji("📷"));
                        }
                        bool addedCorrectly = DBTransaction.addQuote(message.Author.Id, messageToAdd, Guild);
                        
                        if (addedCorrectly)
                        {
                            await message.AddReactionAsync(new Emoji("🔖"));
                            Console.WriteLine("Added quote through reactions for: " + message.Author.Username + ": " + message.Content + " " + addedCorrectly);
                        }
                        else
                        {
                            Console.WriteLine("Failed to add quote through reactions for: " + message.Author.Username + ": " + message.Content + " " + addedCorrectly);
                            await message.AddReactionAsync(new Emoji("❌"));
                        }
                    }
                }
            }
            //for welcome message CHECKMARK
            if (reaction.Emote.Name == "✅")
            {
                var message = await cache.DownloadAsync();
                //get guildID for DB
                var chnl = message.Channel as SocketGuildChannel;
                var Guild = chnl.Guild.Id;
                //0channel, 1welcome, 2post
                string[] WelcomeInfo = DBTransaction.getWelcomeInfo(Guild);
                if(WelcomeInfo[0] != "NONE")
                {
                    //returns 0 if the reacting user isnt a target
                    ulong messageID = DBTransaction.getWelcomeUser(Guild, reaction.UserId, message.Id);
                    if(messageID != 0)
                    {
                        var reactionUser = reaction.User.Value as IGuildUser;
                        await channel.SendMessageAsync(reactionUser.Mention + " has been confirmed.\n" + WelcomeInfo[2]);
                        await reactionUser.AddRoleAsync(chnl.Guild.GetRole(Convert.ToUInt64(WelcomeInfo[3])));
                        DBTransaction.RemoveWelcomeUser(reactionUser.Id, chnl.Guild.Id);
                    }
                }
            }

            //for role adding through reactions!
            ulong reactionRole = DBTransaction.reactionRoleExists(reaction.MessageId, reaction.Emote.ToString());
            if (reactionRole != 0)
            {
                var message = await cache.DownloadAsync();
                //get guild to find role!
                var chnl = message.Channel as SocketGuildChannel;
                var reactionUser = reaction.User.Value as IGuildUser;
                Console.WriteLine("Giving role to " + reaction.User.Value.Username + "!");
                try
                {
                    await reactionUser.AddRoleAsync(chnl.Guild.GetRole(reactionRole));
                    await reactionUser.SendMessageAsync("Hi " + reactionUser.Username + "! I gave you the " + chnl.Guild.GetRole(reactionRole).Name + " role!");
                }
                catch
                {
                    await reactionUser.SendMessageAsync("Sorry, something went wrong, I am either unable to modify roles or the chosen role is above me in settings. Contact an admin or Hoovier#4192 for assistance!");
                    Console.WriteLine("Something went wrong giving out a role! Server: " + chnl.Guild.Name);
                }
            }

            //checks to see if its in the dictionary first!
            if (Global.MessageIdToTrack.ContainsKey(channel.Id))
            {
                if (reaction.MessageId == Global.MessageIdToTrack[channel.Id] && !reaction.User.Value.IsBot)
                {
                    if (reaction.Emote.Name == "🎉")
                    {
                        var clock = "**__Fun page__** \n **Boop**: Boop a user! \n **Pony**: Brings up a random image of me. \n " +
                            "**Click**: Count! Adds one every time it runs. \n **Say**: Make me say whatever you want. " +
                            "\n **Is**: Ask me a question and I'll guess! \n **Joke**: I'll tell you a joke. ";
                        await Global.MessageTotrack[channel.Id].ModifyAsync(msg => msg.Content = clock);
                        IEmote cloc = new Emoji("🎉");
                        await Global.MessageTotrack[channel.Id].RemoveReactionAsync(cloc, reaction.User.Value);
                    }

                    else if (reaction.Emote.Name == "🏠")
                    {
                        var cloke = "**__Hi there! I'm Waggles, let me show you what I can do!__** \n " +
                            "I don't wanna cloud your view, so I made a machine to help me help you. All you have to do is react with one of the following emojis to get its corresponding page. " +
                            "\n :wrench: - My admin tools! \n :tada: - The funnest commands! \n :house: - Takes you to the homepage. \n :horse: - Derpibooru commands!";
                        await Global.MessageTotrack[channel.Id].ModifyAsync(msg => msg.Content = cloke);
                        IEmote clock = new Emoji("🏠");
                        await Global.MessageTotrack[channel.Id].RemoveReactionAsync(clock, reaction.User.Value);
                    }
                    else if (reaction.Emote.Name == "🐴")
                    {
                        var cloke = "**__Derpibooru page__** \n **Derpi**: Allows you to run a derpi query, returns only safe results unless the channel is marked NSFW. " +
                            "\n **Derpist**: Returns the amount of images matching your query. \n **Derpitags**: Returns the tags of a linked or saved image. " +
                            "\n **Next**: Returns another image of last derpi search. \n **Artist**: Returns the artist(s) page(s) of a linked or saved image.";
                        await Global.MessageTotrack[channel.Id].ModifyAsync(msg => msg.Content = cloke);
                        IEmote clock = new Emoji("🐴");
                        await Global.MessageTotrack[channel.Id].RemoveReactionAsync(clock, reaction.User.Value);
                    }
                    else if (reaction.Emote.Name == "🔧")
                    {
                        var cloke = "**__Admin page__** \n **Ban**: Ban a user! \n **kick**: Kick a user. \n " +
                            "**wlist**: add, remove, or view the list of channels that allow nsfw posts from derpibooru searches. \n " +
                            "**alias**: add, remove, view, or edit the list of aliases.";
                        await Global.MessageTotrack[channel.Id].ModifyAsync(msg => msg.Content = cloke);
                        IEmote clock = new Emoji("🔧");
                        await Global.MessageTotrack[channel.Id].RemoveReactionAsync(clock, reaction.User.Value);
                    }
                }
            }
            }

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            //for role removal after user unclicks a reaction.
            ulong reactionRole = DBTransaction.reactionRoleExists(reaction.MessageId, reaction.Emote.ToString());
            if (reactionRole != 0)
            {
                var message = await cache.DownloadAsync();
                //get guild to find role!
                var chnl = message.Channel as SocketGuildChannel;
                var reactionUser = reaction.User.Value as IGuildUser;
                Console.WriteLine("Removing role from " + reaction.User.Value.Username + "!");
                try
                {
                    await reactionUser.RemoveRoleAsync(chnl.Guild.GetRole(reactionRole));
                }
                catch
                {
                    await reactionUser.SendMessageAsync("Sorry, something went wrong, I am either unable to modify roles or the chosen role is above me in settings. Contact an admin or Hoovier#4192 for assistance!");
                    return;
                }
                await reactionUser.SendMessageAsync("Hi " + reactionUser.Username + "! I removed the " + chnl.Guild.GetRole(reactionRole).Name + " role!");
            }
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private string Oico()
        {

            // URL to oico's youtube video feed.
            string oico = "https://www.youtube.com/feeds/videos.xml?channel_id=UC7y4zXXFxr6pBw-5kuYNrig";
            // URL to video. Self explanatory.
            string urlQuery = "string(//ns:entry[1]/ns:link/@href)";
            // Video timestamp. ISO-8601 format.
            // Format: YYYY-MM-DD[T]HH:MM:SS[Z]
            // [T] is a literal "T" character to separate date and time.
            // [Z] is the timezone offset. Example, CA is UTC-7, so [Z] becomes -07:00.
            // Example: 2021-06-08T02:51:18+00:00
            string dateQuery = "string(//ns:entry[1]/ns:published)";

            try
            {
                // Load the remote XML file to an XPath Document [Allows for smart parsing.]
                XPathDocument document = new XPathDocument(oico);
                // Gets a "navigator" to find items within the parsed document.
                XPathNavigator navigator = document.CreateNavigator();

                // XML Namespace manager (for reasons).
                // Give a usable alias ("ns") to the Atom namespace used.
                XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
                manager.AddNamespace("ns", "http://www.w3.org/2005/Atom");

                // Set up XPath Queries and add the namespace context to them.
                XPathExpression query = navigator.Compile(dateQuery);
                query.SetContext(manager);
                var latestVideoUpload = navigator.Evaluate(query).ToString();
                var fakeDate = System.IO.File.ReadAllText("TXTResources/oicoTimeStamp.txt"); //Yesterdays Oico vid published.

                // Compare `latestVideoUpload` with your cached/stored Oico date.
                if (String.Compare(latestVideoUpload, fakeDate) > 0)
                {
                    // If newest video in feed is actually new, then fetch the URL and toss it to Discord!
                    query = navigator.Compile(urlQuery);
                    query.SetContext(manager);
                    var latestVideoUrl = navigator.Evaluate(query).ToString();

                    // Don't forget to update your cached date!
                    File.WriteAllText("TXTResources/oicoTimeStamp.txt", latestVideoUpload);
                    //return new URL to where it was called
                    return latestVideoUrl;
                }
            }
            catch
            {
                // If unable to open Oico's feed, then print the error and then resume operating as if no video was posted.
                Console.WriteLine("Unable to access Oico's XML video feed. Most likely a 404.");
                return "NONE";
            }
            
            return "NONE";
            // Do nothing if the feed hasn't changed.
        }


    }
}