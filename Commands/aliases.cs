using CoreWaggles.Commands;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoreWaggles
{
    //TODO: Make aliases server specific!
    public class aliases : ModuleBase<SocketCommandContext>
    {
        [Command("alias add")]
        public async Task setCommand(string name, [Remainder] string command)
        {
            string dbResponse = DBTransaction.addAliasedCommand(name, command, Context.Guild.Id);
            await ReplyAsync(dbResponse);
        }

        [Command("alias remove")]
        public async Task remAlias(string name)
        {
            string dbResponse = DBTransaction.removeAliasedCommand(name, Context.Guild.Id);
            await ReplyAsync(dbResponse);
        }
        [Command("alias edit")]
        public async Task editalias(string name, string newcom)
        {
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
