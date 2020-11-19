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
            await ReplyAsync("Success! Reacting with ``" + emoji + "`` will give the user the ``" + role.Name + "`` role!");
        }

        [Command("removerole")]
        public async Task removeRole(Discord.IRole role)
        {
            //remove a task from the database!
            string result = DBTransaction.removeRole(role.Id);
            await ReplyAsync(result);
        }

        [Command("listrole")]
        [Alias("listroles", "lr")]

        public async Task listRoles()
        {
            string response = "**__Role Reactions for this server:__** \n";
            Dictionary<ulong, string> result = DBTransaction.listRoles(Context.Guild.Id);
            if(result.ContainsKey(0))
            {
                await ReplyAsync(response + result[0]);
                return;
            }
            foreach(KeyValuePair<ulong, string> pair in result)
            {
                //get role name!
                response = response + Context.Guild.GetRole(pair.Key).Name + " " + pair.Value + "\n";
            }
            await ReplyAsync(response);
        }
        [Command("rolehelp")]
        [Alias("rh", "roles")]
         public async Task rolehelp()
        {
            await ReplyAsync("**Associated Commands:** \n```~addrole <@role> <:emoji:> <messageID of target message>\n~removerole <@role>\n~listroles```");
        }
    }
}
