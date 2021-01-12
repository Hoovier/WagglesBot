using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class reactionRoles : ModuleBase<SocketCommandContext>
    {
        [Command("addrole")]
        [RequireUserPermission(ChannelPermission.ManageRoles)]
        public async Task addRole(Discord.IRole role, string emoji, ulong messageID)
        {
            //try to give user role in order to test if perms are present
            IGuildUser user = (IGuildUser)Context.Message.Author;
            try
            {
                //if user has it, remove then add back, this prevents removal of roles from people who already had a role
                if(user.RoleIds.Contains(role.Id))
                {
                    await user.RemoveRoleAsync(role);
                    await user.AddRoleAsync(role);
                }
                //if they dont, add it and then remove again
                else
                {
                    await user.AddRoleAsync(role);
                    await user.RemoveRoleAsync(role);
                }
                Console.WriteLine("Successful Role addition test. Permissions ok.");
                //add a task to list
                DBTransaction.setReactionRole(role.Id, Context.Guild.Id, emoji, messageID);
                await ReplyAsync("Success! Reacting with ``" + emoji + "`` will give the user the ``" + role.Name + "`` role!");
            }
            catch
            {
                await ReplyAsync("Sorry! Something went wrong! Make sure that I have permission to modify roles and that my role is higher than the selected one!");
            }
            
        }

        [Command("removerole")]
        [RequireUserPermission(ChannelPermission.ManageRoles)]
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
