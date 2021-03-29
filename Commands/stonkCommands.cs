using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class stonkCommands : ModuleBase<SocketCommandContext>
    {
        [Command("addstonk")]
        public async Task addStonk(string name, int numOfShares, double price )
        {
            if(Context.User.Id != 223651215337193472)
            {
                await ReplyAsync("Only Hoovier can run this command!");
            }
            else
            {
                DBTransaction.addStonk(name, numOfShares, price);
                await ReplyAsync($"Added ``{name}`` stonk!");
            }

        }
        [Command("editstonk shares")]
        public async Task editStonk(string name, int numOfShares)
        {
            if (Context.User.Id != 223651215337193472)
            {
                await ReplyAsync("Only Hoovier can run this command!");
            }
            else
            {
                DBTransaction.editStonk(name, numOfShares);
                await ReplyAsync($"Edited ``{name}`` stonk!");
            }

        }
        [Command("editstonk price")]
        public async Task editStonk(string name, double price)
        {
            if (Context.User.Id != 223651215337193472)
            {
                await ReplyAsync("Only Hoovier can run this command!");
            }
            else
            {
                DBTransaction.editStonk(name, price);
                await ReplyAsync($"Edited ``{name}`` stonk!");
            }

        }

        [Command("stonks")]
        public async Task postStonk()
        {
                await ReplyAsync(DBTransaction.getStonks());
        }
    }
}
