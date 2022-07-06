using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Discord.WebSocket;

namespace CoreWaggles.Commands
{

    public class WittyCommands : ModuleBase<SocketCommandContext>
    {
        public readonly string path = "JSONstorage/Wittys.JSON";
        [Command("witty add")]
        public async Task addWitty(string name, string match, double probability, params String[] responses)
        {
            //Makes sure probability, when multiplied by 100, will be valid.
            //EX. .40 * 100 = 40% chance of happening.
            if (probability > 1.0 || probability < 0.0)
            {
                await ReplyAsync("Pick a probability between 1.0 and 0");
                return;
            }
            try
            {
                string dbResponse = DBTransaction.addWitty(name, match, Context.Guild.Id, probability, responses.ToList());
                await ReplyAsync(dbResponse);
            }
            catch (SQLiteException ex)
            {
                switch(ex.ErrorCode)
                {
                    case 19:
                        Console.WriteLine("Attempted to add Witty, server already had one witty with that name! Server: " + Context.Guild.Name);
                        await ReplyAsync("You already have a witty with that name!");
                        break;
                    default:
                        Console.WriteLine("SQL Error: " + ex.Message + "\nErrorNum:" + ex.ErrorCode);
                        await ReplyAsync("Something went wrong, contact Hoovier with error code: " + ex.ErrorCode);
                        break;
                }
            }
        }
        [Command("witty exclude")]
        [Alias("witty ignore")]
        [RequireUserPermission(Discord.GuildPermission.ManageChannels)]
        public async Task ignoreUserWitty(SocketGuildUser user)
        {
            int returncode = DBTransaction.addWittyExclusion(user.Id, Context.Guild.Id);
            if(returncode > 0)
            {
                await ReplyAsync("User added to witty exclusion list, ignored from all wittys.");
            }
            else
            {
                await ReplyAsync("User not added to exclusion, maybe they are already in the list?");
            }
        }

        [Command("witty include")]
        [RequireUserPermission(Discord.GuildPermission.ManageChannels)]
        public async Task includeUserWitty(SocketGuildUser user)
        {
            int returncode = DBTransaction.removeWittyExclusion(user.Id, Context.Guild.Id);
            if (returncode > 0)
            {
                await ReplyAsync("User removed from witty exclusion list");
            }
            else
            {
                await ReplyAsync("Something went wrong, user not removed from exclusion.");
            }
        }

        [Command("wittyexclusions")]
        [Alias("witty exclusions")]
        public async Task listWittyExclusions()
        {
            await ReplyAsync(DBTransaction.listWittyExclusions(Context.Guild.Id));
        }


        [Command("witty remove")]
        public async Task RemWitty(string name)
        {
            string DBresponse = DBTransaction.removeWitty(name, Context.Guild.Id);
            //only reaches this point if entire list does not match.
            await ReplyAsync(DBresponse);
        }
            
        [Command("witty list")]
        public async Task listWitty()
        {
            string DBresponse = DBTransaction.listWitty(Context.Guild.Id);
            await ReplyAsync(DBresponse);
        }
        [Command("witty get")]
        public async Task getWitty(string name)
        {
            string DBresponse = DBTransaction.getWitty(name, Context.Guild.Id);
            await ReplyAsync(DBresponse);
        }
    }
}
