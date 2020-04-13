using CoreWaggles.Commands;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace CoreWaggles
{
    //TODO: Make aliases server specific!
    public class aliases : ModuleBase<SocketCommandContext>
    {
        [Command("alias add")]
        public async Task setCommand(string name, [Remainder] string command)
        {
            try
            {
                string dbResponse = DBTransaction.addAliasedCommand(name, command, Context.Guild.Id);
                await ReplyAsync(dbResponse);
            }
            catch(SQLiteException ex)
            {
                int errID = DBTransaction.getErrorID(ex.Message);
                switch (errID)
                {
                    //if its a foreign key problem, server is not in the Servers Table, so add it and then try again
                    case 0:
                        Console.WriteLine("ServerID does not exist in DB, failed FOREIGN KEY check. Trying to add Server to DB now.");
                        DBTransaction.addOrUpdateServer(Context.Guild.Id, Context.Guild.Name);
                        await setCommand(name, command);
                        Console.WriteLine("Success!?");
                        break;
                    //if its a duplicate problem, 
                    case 1:
                        Console.WriteLine("Attempted to add Alias, server already had one Alias with that name! Server: " + Context.Guild.Name);
                        await ReplyAsync("You already have an Alias with that name in this server!");
                        break;
                    //unknown error, spit out an error for me. 69 for obvious reasons.
                    case 69:
                        Console.WriteLine("SQL Error: " + ex.Message + "\nErrorNum:" + ex.ErrorCode);
                        await ReplyAsync("Something went wrong, contact Hoovier with error code: " + ex.ErrorCode);
                        break;
                }
            }
        }

        [Command("alias remove")]
        public async Task remAlias(string name)
        {
            //has error handling based on amount of rows returned in DBTransaction already
            string dbResponse = DBTransaction.removeAliasedCommand(name, Context.Guild.Id);
            await ReplyAsync(dbResponse);
        }
        [Command("alias edit")]
        public async Task editalias(string name, string newcom)
        {
            //has error handling based on amount of rows returned in DBTransaction already
            string dbResponse = DBTransaction.editAliasedCommand(name, newcom, Context.Guild.Id);
                await ReplyAsync(dbResponse);
        }
        [Command("alias get")]
        public async Task getAlias(string name)
        {
            string dbResponse = DBTransaction.getAliasedCommand(name, Context.Guild.Id, false);
                await ReplyAsync(dbResponse);
        }
        [Command("alias list")]
        public async Task listAlias()
        {
            string dbResponse = DBTransaction.listAliasedCommands(Context.Guild.Id);
            //if the list of aliases is too big, post as .txt document
            if(dbResponse.Length > 1999)
            {
                using(var textDoc = new StreamWriter(@"message.txt"))
                {
                    textDoc.Write(dbResponse);
                }
                await Context.Channel.SendFileAsync(@"message.txt");
                return;
            }

            await ReplyAsync(dbResponse);
        }
    }
}
