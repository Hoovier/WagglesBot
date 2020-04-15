using System;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.RegularExpressions;
using CoreWaggles;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using CoreWaggles.Commands;

namespace WagglesBot
{

    class Program
    {
        static void Main(string[] args) => new Program().runBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public async Task runBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            //Reddit Token stuff!
            string[] RedditTokens = System.IO.File.ReadAllLines("redditTokens.txt");
            Global.reddit = new Reddit.RedditClient(RedditTokens[0], RedditTokens[1], RedditTokens[2], RedditTokens[3]);
            //Waggles = 0, Mona = 1
            string[] keys = System.IO.File.ReadAllLines("Keys.txt");
            string botToken = keys[1];
            
            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);
            // event subscriptions
            _client.Log += Log;
            _client.ReactionAdded += OnReactionAdded;
            
            //_client.Ready += OnReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.Ready += OnReady;
            // _client.UserJoined += AnnounceJoinedUser;
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task OnReady()
        {
            Console.WriteLine("Logged in on Discord as: " + _client.CurrentUser.Username);
            await _client.SetGameAsync("with Mona");
            Console.WriteLine("Logged into Reddit as: " + Global.reddit.Account.Me.Name);
            Console.WriteLine(File.Exists("WagglesDB.db") ? "Database found!" : "ERROR Database not found!");
            Console.WriteLine("In " + _client.Guilds.Count + " servers!");
        }

        private async Task OnJoinedGuild(SocketGuild arg)
        {
            try
            {
                SocketUser use = _client.GetUser(223651215337193472);
                await use.SendMessageAsync("I joined a new server named: " + arg.Name + "\n" + "Owner info: " + arg.Owner.Username + arg.Owner.Discriminator);
            }
            catch
            {
                Console.WriteLine("I joined a new server named: " + arg.Name + "\n" + "Owner info: " + arg.Owner.Username + arg.Owner.Discriminator);
            }

        }



        /* private async Task AnnounceJoinedUser(SocketGuildUser arg)
         {

             var channel = _client.GetChannel(480105955552395285) as SocketTextChannel; // Gets the channel to send the message in
             await channel.SendMessageAsync($"Hi there {arg.Username}!"); //Welcomes the new user
             IEmote emote = channel.Guild.Emotes.First(e => e.Name == "rymwave");



             await channel.SendMessageAsync($"{emote}");
         }
         */
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //checks to see if its in the dictionary first!
            if (Global.MessageIdToTrack.ContainsKey(channel.Id))
            {
                if (reaction.MessageId == Global.MessageIdToTrack[channel.Id] && reaction.UserId != _client.CurrentUser.Id)
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
            else if (reaction.MessageId == Global.redditDictionary[reaction.Channel.Id].redditIDtoTrack && reaction.UserId != _client.CurrentUser.Id)
            {
                Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{reaction.Channel.Name}] \n{reaction.User.Value.Username}: Clicked Rnext button!");
                await _commands.ExecuteAsync(Global.redditDictionary[reaction.Channel.Id].redContext, "rnext 5");
            }

        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }


        private async Task HandleCommandAsync(SocketMessage arg)
        {
           
            var message = arg as SocketUserMessage;
            //for tracking messages from bot
            if(message.Author.Id == _client.CurrentUser.Id)
            {
                //initialize MessageLog object if it doesnt exist.
                if (!Global.lastMessage.ContainsKey(message.Channel.Id))
                    Global.lastMessage.Add(message.Channel.Id, new MessageLog());

                //add message to stack
                Global.lastMessage[message.Channel.Id].addElement(message.Id);
            }

            if (message is null || message.Author.Id == _client.CurrentUser.Id) return;

            int argPos = 0;

            if (message.Author.Id == 141016540240805888 && message.HasStringPrefix("~", ref argPos))
            {
                var context = new SocketCommandContext(_client, message);
                await context.Channel.SendMessageAsync("Youre not my supervisor!");
            }
          

            // We case-insensitive search and compare key phrases of the message.
            string lowerCaseMessage = message.Content.ToLower();
          
            // If URL posted is a Derpibooru URL, extract the ID and save to `links` cache.
            if (Global.IsBooruUrl(lowerCaseMessage))
            {
                // Grab context for Channel info.
                var context = new SocketCommandContext(_client, message);
                // Extract the Derpibooru image ID.
                int derpiID = Global.ExtractBooruId(message.Content);
                // If an ID is able to be parsed out, add it to the `links` cache for the Channel.
                if (derpiID != -1) {
                    Global.LastDerpiID[context.Channel.Id] = derpiID.ToString();
                }
            }
            // If a URL that is NOT booru related, then just save to `miscLinks` cache.
            else if(lowerCaseMessage.Contains("https://") && !Global.IsBooruUrl(lowerCaseMessage))
            {
                var context = new SocketCommandContext(_client, message);
                Global.miscLinks[context.Channel.Id] = message.Content;
            }

            if (message.HasStringPrefix("~", ref argPos) && message.Author.Id != 141016540240805888 && !message.HasStringPrefix("~~", ref argPos))
            {
                var context = new SocketCommandContext(_client, message);
                Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{context.Channel.Name}] \n{message.Author.Username}: {message.Content}");
                //get GuildID first, cause DM channels dont have one and cause errors!
                ulong guildID;
                if(context.IsPrivate)
                {
                    //set to 0 so itll wont match any servers!
                    guildID = 0;
                }
                else
                {
                    guildID = context.Guild.Id;
                }
                //if the command is in DB, it will return actual command desired, otherwise returns string.empty and fails the check
                string AliasedCommandCheck = DBTransaction.getAliasedCommand(message.Content.Trim('~'), guildID, true);
                if ( AliasedCommandCheck != string.Empty)
                {
                   await _commands.ExecuteAsync(context, AliasedCommandCheck);
                }
                else
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        Console.WriteLine(result.ErrorReason);
                        await context.Channel.SendMessageAsync("Sorry! Seems like I messed up, here's where it all went wrong: **" + result.ErrorReason + "**");
                    }
                }
            }
            else
            {
                var context = new SocketCommandContext(_client, message);
                DBTransaction.processWitty(context, message.Content);
            }

            if ((message.Content.ToLower().Contains("lewd") || message.Content.ToLower().Contains("sexuals")) && message.Content.ToLower().Contains("rym"))
            {
                var context = new SocketCommandContext(_client, message);
                await context.Channel.SendMessageAsync("Please don't! <:tears:409771767410851845>");
                
            }
            if ((message.Content.ToLower().Contains("here's wonderwall") || message.Content.ToLower().Contains("heres wonderwall")) )
            {
                var context = new SocketCommandContext(_client, message);
                await context.Channel.SendMessageAsync("https://www.youtube.com/watch?v=bx1Bh8ZvH84");
                
            }
            if ((message.Content.ToLower().Contains("alexa play despacito") || message.Content.ToLower().Contains("thats so sad") || message.Content.ToLower().Contains("that's so sad")))
            {
                var context = new SocketCommandContext(_client, message);
                await context.Channel.SendMessageAsync("https://www.youtube.com/watch?v=kJQP7kiw5Fk");

            }

        }

    }
}