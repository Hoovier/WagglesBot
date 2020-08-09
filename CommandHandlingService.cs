﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using CoreWaggles.Commands;

namespace CoreWaggles.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if(message.Author.Id == _discord.CurrentUser.Id)
            {
                //initialize MessageLog object if it doesnt exist.
                if (!Global.lastMessage.ContainsKey(message.Channel.Id))
                    Global.lastMessage.Add(message.Channel.Id, new MessageLog());

                //add message to stack
                Global.lastMessage[message.Channel.Id].addElement(message.Id);
            }
            //this checks to see if a message has embeds or attachments, and downloads all of them.
            if (!message.Author.IsBot && message.Attachments.Count > 0)
            {
                foreach (Attachment att in message.Attachments)
                {
                    //spams up console, but fun for notifications Console.WriteLine("Saved an attachment!");
                    Global.miscLinks[message.Channel.Id] = att.Url;
                }
                return;
            }

            // We case-insensitive search and compare key phrases of the message.
            string lowerCaseMessage = message.Content.ToLower();

            // If URL posted is a Derpibooru URL, extract the ID and save to `links` cache.
            if (Global.IsBooruUrl(lowerCaseMessage))
            {
                // Grab context for Channel info.
                var contextInfo = new SocketCommandContext(_discord, message);
                // Extract the Derpibooru image ID.
                int derpiID = Global.ExtractBooruId(message.Content);
                // If an ID is able to be parsed out, add it to the `links` cache for the Channel.
                if (derpiID != -1)
                {
                    Global.LastDerpiID[contextInfo.Channel.Id] = derpiID.ToString();
                }
            }
            // If a URL that is NOT booru related, then just save to `miscLinks` cache.
            else if (lowerCaseMessage.Contains("https://") && !Global.IsBooruUrl(lowerCaseMessage))
            {
                var contextInfo = new SocketCommandContext(_discord, message);
                Global.miscLinks[contextInfo.Channel.Id] = message.Content;
            }

            // This value holds the offset where the prefix ends
            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            if (!message.HasCharPrefix('~', ref argPos) || message.HasStringPrefix("~~", ref argPos) || message.Author.Id == 141016540240805888)
            {
                //throw message into witty processor if it doesnt match a command.
                DBTransaction.processWitty(context, message.Content);

                if ((message.Content.ToLower().Contains("lewd") || message.Content.ToLower().Contains("sexuals")) && message.Content.ToLower().Contains("rym"))
                {
                    await context.Channel.SendMessageAsync("Please don't! <:tears:409771767410851845>");
                }
                if ((message.Content.ToLower().Contains("here's wonderwall") || message.Content.ToLower().Contains("heres wonderwall")))
                {
                    await context.Channel.SendMessageAsync("https://www.youtube.com/watch?v=bx1Bh8ZvH84");
                }
                if ((message.Content.ToLower().Contains("alexa play despacito") || message.Content.ToLower().Contains("thats so sad") || message.Content.ToLower().Contains("that's so sad")))
                {
                    await context.Channel.SendMessageAsync("https://www.youtube.com/watch?v=kJQP7kiw5Fk");
                }
                return;
            }
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            await _commands.ExecuteAsync(context, argPos, _services);
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            //command was executed, log it to console.
            Console.WriteLine($"[{DateTime.Now.ToString("h:mm:ss")} #{context.Channel.Name}] \n{context.Message.Author}: {context.Message}");

            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
            {
                //get GuildID first, cause DM channels dont have one and cause errors!
                ulong guildID = context.Channel is IPrivateChannel ? 0 : context.Guild.Id;

                //if the command is in DB, it will return actual command desired, otherwise returns string.empty and fails the check
                string AliasedCommandCheck = DBTransaction.getAliasedCommand(context.Message.Content.Trim('~'), guildID, true);
                if (AliasedCommandCheck != string.Empty)
                {
                    await _commands.ExecuteAsync(context, AliasedCommandCheck, _services);
                    return;
                }
                await context.Channel.SendMessageAsync("Command not found, try ~help");
                return;
            }

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
        }
    }
}
