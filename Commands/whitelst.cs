using CoreWaggles;
using CoreWaggles.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;


public class Wlist : ModuleBase<SocketCommandContext>
{
    [Command("isnsfw")]
    [Alias("woona", "nsfw")]
    public async Task IsnsfwAsync()
    {
        string isIt = DBTransaction.isChannelWhitelisted(Context.Channel.Id) ? "is" : "is not";
        await ReplyAsync($"This channel {isIt} NSFW.");
    }

    [Command("stab")]
    public async Task StabAsync(SocketGuildUser name)
    {
        await ReplyAsync($"{Context.User.Mention} stabbed {name.Username}!");
    }

    [Command("wlist")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task WlistAsync(string action, SocketGuildChannel channel)
    {
        switch (action)
        {
            case "add":
                {
                    try
                    {
                        DBTransaction.addChannelToWhitelist(channel.Id, Context.Guild.Id, channel.Name);
                        await ReplyAsync("Added to Whitelist!");
                    }
                    catch(SQLiteException ex)
                    {
                        switch (ex.ErrorCode)
                        {
                            case 19:
                                Console.WriteLine("Attempted to add channel to Whitelist, it already was in. Server: " + Context.Guild.Name);
                                await ReplyAsync("That channel was already in the Whitelist!");
                                break;
                            default:
                                Console.WriteLine("SQL Error: " + ex.Message + "\nErrorNum:" + ex.ErrorCode);
                                await ReplyAsync("Something went wrong, contact Hoovier with error code: " + ex.ErrorCode);
                                break;
                        }
                    }
                    break;
                }
            case "remove":
                {
                    string dbResponse = DBTransaction.removeChannelWhitelist(channel.Id);
                    await ReplyAsync(dbResponse);
                    break;
                }
            case "list":
                {
                    string dbResponse = DBTransaction.listWhitelistedChannels(Context.Guild.Id);
                    await ReplyAsync(dbResponse);
                    break;
                }
            default:
                {
                    await ReplyAsync("Did you forget an action argument? I can ``wlist add #channel``, ``wlist remove #channel``, or even ``wlist list all``.");
                    break;
                }
        }
    }
    [Command("wlist")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task WlistnoargAsync(string arg)
    {
        switch (arg)
        {
            case "list":
                {
                    string dbResponse = DBTransaction.listWhitelistedChannels(Context.Guild.Id);
                    await ReplyAsync(dbResponse);
                    break;
                }
            case "all":
                {
                    foreach (SocketGuildChannel channel in Context.Guild.TextChannels)
                    {
                        try
                        {
                            DBTransaction.addChannelToWhitelist(channel.Id, Context.Guild.Id, channel.Name);
                        }
                        catch (SQLiteException ex)
                        {
                            switch (ex.ErrorCode)
                            {
                                case 19:
                                    Console.WriteLine("Attempted to add channel to Whitelist, it already was in. Server: " + Context.Guild.Name);
                                   //no need to reply cause we dont want to spam chat with channels already in
                                    break;
                                default:
                                    Console.WriteLine("SQL Error: " + ex.Message + "\nErrorNum:" + ex.ErrorCode);
                                    await ReplyAsync("Something went wrong, contact Hoovier with error code: " + ex.ErrorCode);
                                    break;
                            }
                        }
                    }
                    await WlistnoargAsync("list");
                    break;
                }
            default:
                {
                    await ReplyAsync("Did you forget an action argument? I can ``wlist list``, ``wlist all``.");
                    break;
                }
        }
    }
   
}
