using System;
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
                        if (reaction.MessageId == Global.derpiMessageToTrack[reaction.Channel.Id] && !reaction.User.Value.IsBot)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Dnext button!");
                            await cmd.ExecuteAsync(Global.derpiContext[reaction.Channel.Id], "next 5", services);
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
                        bool addedCorrectly = DBTransaction.addQuote(message.Author.Id, message.Content, Guild);
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


    }
}