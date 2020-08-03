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
            //Waggles = 0, Mona = 1
            string[] keys = System.IO.File.ReadAllLines("Keys.txt");
            string botToken = keys[1];
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += Log;
                client.JoinedGuild += OnJoinedGuild;
                client.Ready += async () =>
                {
                    Console.WriteLine("Logged in on Discord as: " + client.CurrentUser.Username);
                    await client.SetGameAsync("with Mona");
                    Console.WriteLine("In " + client.Guilds.Count + " servers!");
                    Console.WriteLine("Logged into Reddit as: " + Global.reddit.Account.Me.Name);
                    Console.WriteLine(File.Exists("WagglesDB.db") ? "Database found!" : "ERROR Database not found!");
                };
                client.ReactionAdded += OnReactionAdded;
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
                else if (reaction.MessageId == Global.redditDictionary[reaction.Channel.Id].redditIDtoTrack && !reaction.User.Value.IsBot)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Rnext button!");
                    // await _commands.ExecuteAsync(Global.redditDictionary[reaction.Channel.Id].redContext, "rnext 5");
                }


        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }


    }
}