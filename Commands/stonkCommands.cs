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
        public async Task addStonk(string name, int numOfShares, int price )
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
        public async Task editStonkshares(string name, int numOfShares)
        {
            if (Context.User.Id != 223651215337193472)
            {
                await ReplyAsync("Only Hoovier can run this command!");
            }
            else
            {
                DBTransaction.editStonkShares(name, numOfShares);
                await ReplyAsync($"Edited ``{name}`` stonk!");
            }

        }
        [Command("editstonk price")]
        public async Task editStonkprice(string name, int price)
        {
            if (Context.User.Id != 223651215337193472)
            {
                await ReplyAsync("Only Hoovier can run this command!");
            }
            else
            {
                DBTransaction.editStonkPrice(name, price);
                await ReplyAsync($"Edited ``{name}`` stonk!");
            }

        }

        [Command("stonks")]
        public async Task postStonk()
        {
                await ReplyAsync(DBTransaction.getStonks());
        }

        [Command("mystonks")]
        public async Task myStonks()
        {
            await ReplyAsync(DBTransaction.getOwnedStonks(Context.User.Id, Context.Guild.Id));
        }


        [Command("buystonk")]
        public async Task buyStonk(string stonk, int amount)
        {
            //0 = MaxNumOfShares
            //1 = stonkPrice
            //2 = OwnedShares
            List<int> stonkInfo = DBTransaction.getMaxShares(stonk, Context.Guild.Id);
            int balance = int.Parse(DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id));

            if (stonkInfo.Count == 0)
            {
                await ReplyAsync("Sorry, there is no stock with that name on the market right now!");
            }
            else if (stonkInfo[2] + amount > stonkInfo[0])
            {
                await ReplyAsync("Sorry! Not enough stonks to complete your purchase!");
            }
            else if (stonkInfo[1] * amount > balance)
            {
                await ReplyAsync("Sorry! You don't have enough Bits for this. The price for " + amount + "shares of this stonk is " + stonkInfo[1] * amount);
            }
            else
            {
                DateTime localDate = DateTime.Now;
                string dateString = localDate.ToString("yyyy-MM-dd.HH:mm:ss");
                DBTransaction.addStonkPurchase(stonk, amount, Context.User.Id, Context.Guild.Id, dateString);
                await ReplyAsync("Stonks purchased! Your new balance is " + (balance - (stonkInfo[1] * amount)) + " Bits!");
            }
            
        }

        [Command("sellstonk")]
        public async Task sellStonk(string name, int amount)
        {
            //1 = name, 2 = numOfShares, 3 = price
            List<string> stonkInfo = DBTransaction.getStonkInfo(name);
            int balance = int.Parse(DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id));
            bool enoughStonks = DBTransaction.hasEnoughStonk(Context.User.Id, Context.Guild.Id, name, amount);
            if(enoughStonks)
            {
                DBTransaction.sellStonk(Context.User.Id, Context.Guild.Id, name, amount);
                DBTransaction.payMoney(Context.User.Id, amount * int.Parse(stonkInfo[2]), Context.Guild.Id);
                await ReplyAsync("Your stonks were sold and you made " + amount * int.Parse(stonkInfo[2]) + " bits!");
            }
            else
            {
                await ReplyAsync("Sorry! You dont have enough shares of that stonk to complete this transaction.");
            }
        }


    }
}
