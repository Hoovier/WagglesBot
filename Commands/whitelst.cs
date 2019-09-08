using CoreWaggles;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class Stick : ModuleBase<SocketCommandContext>
{
    [Command("isnsfw")]
    [Alias("woona", "nsfw")]
    public async Task IsnsfwAsync()
    {
        //This checks the safeChannels List, despite it being named safechannels it is a list of NSFW enabled channels.
        if (Global.safeChannels.ContainsKey(Context.Channel.Id))
        {
            await ReplyAsync("This channel is NSFW.");
        }
        else
            await ReplyAsync("This channel is not NSFW.");
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
                    if (Global.safeChannels.ContainsKey(channel.Id))
                    {
                        await ReplyAsync("Channel already in whitelist! To remove try ``~wlist remove #channel``");
                    }
                    else
                    {
                        Global.safeChannels.Add(channel.Id, Context.Guild.Id);
                        string channelObject = JsonConvert.SerializeObject(Global.safeChannels);
                        var path = "JSONstorage/wlist.JSON";
                        if (File.Exists(path))
                        {
                            File.WriteAllText(path, channelObject);
                        }
                        else
                            await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");
                        await ReplyAsync($"Added {channel.Name} to NSFW-enabled channels.");
                    }
                    break;
                }
            case "remove":
                {
                    if (!Global.safeChannels.ContainsKey(channel.Id))
                    {
                        await ReplyAsync("Channel not in whitelist.");
                    }
                    else
                    {
                        Global.safeChannels.Remove(channel.Id);
                        var path = "JSONstorage/wlist.JSON";
                        string channelObject = JsonConvert.SerializeObject(Global.safeChannels);
                        if (File.Exists(path))
                        {
                            File.WriteAllText(path, channelObject);
                        }
                        else
                            await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");

                        await ReplyAsync($"Removed {channel.Name} from NSFW-enabled channels.");
                    }
                    break;
                }
            case "list":
                {
                    string response = "Contents: ";
                    foreach (KeyValuePair<ulong, ulong> entry in Global.safeChannels)
                    {
                        // do something with entry.Value or entry.Key
                        if (entry.Value == Context.Guild.Id)
                        {
                            response = $"{response} \n<#{entry.Key}> ";
                        }
                    }
                    await ReplyAsync(response);
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
        string added = "added: ";
        string notadded = "not added";
        switch (arg)
        {
            case "list":
                {
                    string response = "Contents: ";
                    foreach (KeyValuePair<ulong, ulong> entry in Global.safeChannels)
                    {
                        // do something with entry.Value or entry.Key
                        if (entry.Value == Context.Guild.Id)
                        {
                            response = $"{response} \n<#{entry.Key}> ";
                        }
                    }
                    await ReplyAsync(response);
                    break;
                }
            case "all":
                {
                    foreach (SocketGuildChannel channel in Context.Guild.TextChannels)
                    {
                        if (Global.safeChannels.ContainsKey(channel.Id))
                        {
                            notadded = $"{notadded} \n<#{channel.Id}> ";
                        }
                        else
                        {
                            Global.safeChannels.Add(channel.Id, Context.Guild.Id);
                            added = $"{added} \n<#{channel.Id}> ";
                        }
                    }
                    string channelObject = JsonConvert.SerializeObject(Global.safeChannels);
                    var path = "JSONstorage/wlist.JSON";
                    if (File.Exists(path))
                    {
                        File.WriteAllText(path, channelObject);
                        await ReplyAsync("Succesfully found and wrote to the file!");
                        Dictionary<ulong, ulong> newList = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(File.ReadAllText(path));
                        await ReplyAsync($"I have {newList.Count} channels in my saved file!");
                    }
                    else
                        await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");
                    await ReplyAsync($"{notadded} \n{added}");
                    break;
                }
            default:
                {
                    await ReplyAsync("Did you forget an action argument? I can ``wlist list``, ``wlist all``.");
                    break;
                }
        }
    }
    [Command("todo")]
    public async Task TodoAsync(string check, [Remainder] string thingToAdd)
    {
        switch (check)
        {
            case "add":
                {
                    Global.todo.Add(thingToAdd);
                    await ReplyAsync($"Added: ``{thingToAdd}``");
                    var path = "JSONstorage/List.JSON";
                    string channelObject = JsonConvert.SerializeObject(Global.todo);
                    File.WriteAllText(path, channelObject);
                    break;
                }
            case "list":
                {
                    string response = "Contents: ";
                    foreach (string any in Global.todo)
                    {
                        response = $"{response} \n{any}";
                    }
                    await ReplyAsync(response);
                    break;
                }
            case "remove":
                {
                    if (Global.todo.Contains(thingToAdd))
                    {
                        Global.todo.Remove(thingToAdd);
                        await ReplyAsync($"Removed: ``{thingToAdd}``");
                    }
                    else
                    {
                        await ReplyAsync($"Thats not on my list!");
                    }
                    break;
                }
            default:
                {
                    await ReplyAsync("Did you forget an action argument? I can ``add``, ``remove``, or even ``list``.");
                    break;
                }
        }
    }
}
