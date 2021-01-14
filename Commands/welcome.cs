using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class welcome : ModuleBase<SocketCommandContext>
    {
        [Command("wm edit")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task editWelcomeMessage(string choice, [Remainder]string msg)
        {
            int affectedRows = 0;
            if(choice == "welcome")
            {
                affectedRows = DBTransaction.setWelcomeMessage(Context.Guild.Id, msg);
                if (affectedRows == 1)
                {
                    await ReplyAsync("Changed welcome message!");
                }
                else
                {
                    await ReplyAsync("No message was changed, try running ~wm setup!");
                }
                return;
            }
            else if(choice == "confirmation")
            {
                affectedRows = DBTransaction.setConfirmationMessage(Context.Guild.Id, msg);
                if (affectedRows == 1)
                {
                    await ReplyAsync("Changed confirmation message!");
                }
                else
                {
                    await ReplyAsync("No message was changed, try running ~wm setup!");
                }
                return;
            }

                await ReplyAsync("Invalid choice! Choose to edit the **welcome** or **confirmation** message!");
                return;
        }
        [Command("wm get")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task getwelcomeInfo()
        {
            //0channel, 1welcome, 2post
            string[] welcomeInfo = DBTransaction.getWelcomeInfo(Context.Guild.Id);
            if(welcomeInfo[0] == "NONE")
            {
                await ReplyAsync("No welcome info for this server!");
                return;
            }
            var chan = Context.Guild.GetChannel(Convert.ToUInt64(welcomeInfo[0]));
            string response = $"Welcoming Channel: #{chan} \nWelcome Message: **{welcomeInfo[1]}**\nConfirmation Message: **{welcomeInfo[2]}**" +
                $"\nRole:{Context.Guild.GetRole(Convert.ToUInt64(welcomeInfo[3])).Name}";
            await ReplyAsync(response);
        }

        [Command("wm setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task wmSetup(ITextChannel channel, IRole role)
        {
            DBTransaction.InsertWelcomeInfo(channel.Id, Context.Guild.Id, role.Id);
            await ReplyAsync("Initialized welcome info with channel ID: " + channel.Id);
        }

        [Command("wm disable")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task wmDisable()
        {
            int rowsAffected = DBTransaction.RemoveWelcomeInfo(Context.Guild.Id);
            if (rowsAffected == 1)
            {
                await ReplyAsync("Disabled welcome messages in this server. Welcome and confirmation messages have been deleted.");
            }
            else
            {
                await ReplyAsync("No welcome message to delete!");
            }
        }
    }
}
