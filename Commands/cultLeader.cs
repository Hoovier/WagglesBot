using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class cultLeader : ModuleBase<SocketCommandContext>
    {
        [Command("nick")]
        [RequireUserPermission(GuildPermission.ManageNicknames)]
        public async Task setNick(SocketGuildUser oldUser, [Remainder] string nick)
        { 
            await oldUser.ModifyAsync(user => user.Nickname = nick);
            await ReplyAsync("Changed name to " + nick);
        }
    }
}
