using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class reactionRoles : ModuleBase<SocketCommandContext>
    {
        [Command("addrole")]
        public async Task addRole(Discord.IRole role, string emoji, ulong messageID)
        {
            //add a task to list
            DBTransaction.setReactionRole(role.Id, Context.Guild.Id, emoji, messageID);
            await ReplyAsync(role.Id + " " + emoji);
        }
    }
}
