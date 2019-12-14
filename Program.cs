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

namespace WagglesBot
{

    class Program
    {
        static void Main(string[] args) => new Program().runBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        public readonly string todoPath = "JSONstorage/List.JSON";
        public readonly string safeChannelsPath = "JSONstorage/wlist.JSON";
        public readonly string extraCommsPath = "JSONstorage/extraComms.JSON";
        public readonly string WittysPath = "JSONstorage/Wittys.JSON";

        public async Task runBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            Global.todo = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(this.todoPath));
            Global.safeChannels = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(File.ReadAllText(this.safeChannelsPath));
            Global.excomm = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(this.extraCommsPath));
            Global.wittyDictionary = JsonConvert.DeserializeObject<Dictionary<ulong, List<WittyObject>>>(File.ReadAllText(this.WittysPath));
            //Waggles = 0, Mona = 1
            string[] keys = System.IO.File.ReadAllLines("Keys.txt");
            string botToken = keys[1];
            
            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);
            // event subscriptions
            _client.Log += Log;
            _client.ReactionAdded += OnReactionAdded;
            // _client.UserJoined += AnnounceJoinedUser;
            await _client.StartAsync();

            await Task.Delay(-1);
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
            if (reaction.MessageId == Global.MessageIdToTrack && reaction.UserId != 480105212485435392)
            {
                if (reaction.Emote.Name == "🎉")
                {


                    var clock = "**__Fun page__** \n **Boop**: Boop a user! \n **Pony**: Brings up a random image of me. \n **Click**: Count! Adds one every time it runs. \n **Say**: Make me say whatever you want. \n **Is**: Ask me a question and I'll guess! \n **Joke**: I'll tell you a joke. ";
                    await Global.MessageTotrack.ModifyAsync(msg => msg.Content = clock);
                    IEmote cloc = new Emoji("🎉");
                    await Global.MessageTotrack.RemoveReactionAsync(cloc, reaction.User.Value);
                }

                else if (reaction.Emote.Name == "🏠")
                {
                    var cloke = "**__Hi there! I'm Waggles, let me show you what I can do!__** \n I don't wanna cloud your view, so I made a machine to help me help you. All you have to do is react with one of the following emojis to get its corresponding page. \n :wrench: - My admin tools! \n :tada: - The funnest commands! \n :house: - Takes you to the homepage. \n :horse: - Derpibooru commands!";
                    await Global.MessageTotrack.ModifyAsync(msg => msg.Content = cloke);

                    IEmote clock = new Emoji("🏠");
                    await Global.MessageTotrack.RemoveReactionAsync(clock, reaction.User.Value);



                }
                else if (reaction.Emote.Name == "🐴")
                {
                    var cloke = "**__Derpibooru page__** \n **Derpi**: Allows you to run a derpi query, returns only safe results unless the channel is marked NSFW. \n **Derpist**: Returns the amount of images matching your query. \n **Derpitags**: Returns the tags of a linked or saved image. \n **Next**: Returns another image of last derpi search. \n **Artist**: Returns the artist(s) page(s) of a linked or saved image.";
                    await Global.MessageTotrack.ModifyAsync(msg => msg.Content = cloke);

                    IEmote clock = new Emoji("🐴");
                    await Global.MessageTotrack.RemoveReactionAsync(clock, reaction.User.Value);
                }
                else if (reaction.Emote.Name == "🔧")
                {
                    var cloke = "**__Admin page__** \n **Ban**: Ban a user! \n **kick**: Kick a user. \n **wlist**: add, remove, or view the list of channels that allow nsfw posts from derpibooru searches. \n **alias**: add, remove, view, or edit the list of aliases.";
                    await Global.MessageTotrack.ModifyAsync(msg => msg.Content = cloke);

                    IEmote clock = new Emoji("🔧");
                    await Global.MessageTotrack.RemoveReactionAsync(clock, reaction.User.Value);



                }

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
                    Global.links[context.Channel.Id] = derpiID.ToString();
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
                if (Global.excomm.ContainsKey(message.Content.Trim('~')))
                {
                   await _commands.ExecuteAsync(context, Global.excomm[message.Content.Trim('~')]);
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
                WittyProcessor.Process(context, message.Content);
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