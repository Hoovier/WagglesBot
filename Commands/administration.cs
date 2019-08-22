using CoreWaggles;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class administration : ModuleBase<SocketCommandContext>
{
    public CommandService _command { get; set; }
    [Command("kick")]
    [RequireUserPermission(GuildPermission.KickMembers)]

    public async Task kickAsync(SocketGuildUser name, [Remainder] string reason)
    {
        string nameactual = name.Username;
        await name.KickAsync(reason);
        await Context.Channel.SendMessageAsync($"Kicked {nameactual} for {reason} ");

    }

    [Command("s")]
    public async Task SSSAsync()
    {
        int links = Global.links.Count;
        int strings = Global.searchesD.Count;
        Dictionary<ulong, ulong> newList = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(File.ReadAllText("wlist.JSON"));
        ulong clonk = newList.ElementAt(1).Key;
        await ReplyAsync($"I'm holding {links} ID's and {strings} search strings! My whitelist has {Global.safeChannels.Count} entries! \n random channel <@{clonk}>");
    }

    [Command("write")]


    public async Task WriteAsync()
    {

        var path = "List.JSON";
        if (File.Exists(path))
        {
            await ReplyAsync(Global.todo[1]);
        }
        else
            await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");


    }
    
}
public class Ban : ModuleBase<SocketCommandContext>
{

    [Command("ban")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task PingAsync(SocketGuildUser name, [Remainder] string reason)
    {
        string nameactual = name.Username;
        await Context.Guild.AddBanAsync(name, 0, reason);
        await Context.Channel.SendMessageAsync($"Banned {nameactual} for {reason} ");

    }
}
