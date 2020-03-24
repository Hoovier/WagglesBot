using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

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
                
              await  ReplyAsync(ex.ErrorCode.ToString());
            }
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
