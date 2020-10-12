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
    //if the id is set to 0, it has already been deleted.
    const ulong MESSAGE_DELETED = 0;
    public CommandService _command { get; set; }
    [Command("kick")]
    [RequireUserPermission(GuildPermission.KickMembers)]

    public async Task kickAsync(SocketGuildUser name, [Remainder] string reason)
    {
        string nameactual = name.Username;
        await name.KickAsync(reason);
        await Context.Channel.SendMessageAsync($"Kicked {nameactual} for {reason} ");

    }
    [Command("eww")]
    public async Task ewwAsync()
    {
        ulong channel = Context.Channel.Id;

        //if the dictionary doesnt have this channel in its memory that means she has not sent a message here yet.
        if (!Global.lastMessage.ContainsKey(channel))
        {
            await ReplyAsync("No messages in my memory to delete!");
            return;
        }

        ulong lastID = Global.lastMessage[channel].getLastElement();
        
        //if she has already deleted all three messages in her memory, it will be set to MESSAGE_DELETED, so just tell user that it did not work.
        if (lastID == MESSAGE_DELETED)
        {
            await Context.Message.AddReactionAsync(new Emoji("❌"));
            return;
        }

        await Context.Channel.GetMessageAsync(lastID).Result.DeleteAsync();
        await Context.Message.AddReactionAsync(new Emoji("✔️"));
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
