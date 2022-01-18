using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class Currency : ModuleBase<SocketCommandContext>
    {

        [Command("bonus")]
        [Alias("daily", "gimme", "giv", "gibplz")]
        public async Task DailyAsync()
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("Please run this command within the server of your choice!");
                return;
            }
            string timestamp = DBTransaction.getMoneyTimeStamp(Context.User.Id, Context.Guild.Id);
            DateTime localDate = DateTime.Now;
            string dateString = localDate.ToString("yyyy-MM-dd.HH:mm:ss");
            //if there is no record of this user, insert into the table!
            if (timestamp == "NONE")
            {
                DBTransaction.addUsertoMoney(Context.User.Id, 250, Context.Guild.Id, dateString);
                await ReplyAsync("Youve joined the rat race with a grant of 250 Bits! Come back every 8 hours for more!");
                return;
            }
            //figure out how long its been since the last ~daily
            DateTime stamp = DateTime.ParseExact(timestamp, "yyyy-MM-dd.HH:mm:ss", CultureInfo.InvariantCulture);
            TimeSpan span = localDate - stamp;

            if (span.TotalHours > 1)
            {
                DBTransaction.giveMoney(Context.User.Id, 100, Context.Guild.Id, dateString);
                string bal = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
                await ReplyAsync("Congrats youve been gifted 100 Bits, your new balance is " + bal);
            }
            else
            {
                double remaining = Math.Round(60 - span.TotalMinutes);
                if (remaining < 61)
                {
                    await ReplyAsync("Sorry, wait " + remaining.ToString() + " Minutes!");
                }
                else
                {
                    int hours = (int)remaining / 60;
                    int mins = (int)remaining % 60;
                    await ReplyAsync("Sorry, wait " + hours + " hours, " + mins + " minutes for your next bonus!");
                }

            }
        }

        [Command("balance")]
        [Alias("bal")]

        public async Task BalanceAsync()
        {
            if (Context.IsPrivate)
            {
                string response = DBTransaction.getMoneyBalanceDMs(Context.User.Id);
                await ReplyAsync(response);
                return;
            }
            string bal = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (bal == "NONE")
            {
                await ReplyAsync("You are poor and destitute, not a cent to your name! Use ~bonus to beg for some.");
            }
            else
            {
                await ReplyAsync(bal + " Bits!");
            }
        }
        [Command("balance")]
        [Alias("bal")]
        public async Task BalanceAsync(SocketGuildUser user)
        {
            if (Context.IsPrivate)
            {
                string response = DBTransaction.getMoneyBalanceDMs(user.Id);
                await ReplyAsync(response);
                return;
            }
            string bal = DBTransaction.getMoneyBalance(user.Id, Context.Guild.Id);
            if (bal == "NONE")
            {
                await ReplyAsync("They are poor and destitute, not a cent to their name! Consider gifting them some cash, or paying them with ~pay!");
            }
            else
            {
                await ReplyAsync(bal + " Bits!");
            }
        }

        [Command("leaders")]
        [Alias("leaderboard")]
        public async Task MoneyLeadersAsync()
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("Sorry, this command is for server use only.");
                return;
            }
            string leaderString = DBTransaction.getMoneyLeaders(Context.Guild.Id);

            await ReplyAsync(leaderString);
        }

        [Command("pay")]
        public async Task PayAsync(SocketGuildUser user, long amount)
        {
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE")
            {
                return;
            }
            long senderBal = long.Parse(balanceString);
            if (amount > senderBal || amount <= 0)
            {
                await ReplyAsync("Sorry, your balance of " + senderBal + " Bits is too low!");
                return;
            }
            if (DBTransaction.payMoney(user.Id, amount, Context.Guild.Id) == 1)
            {
                DBTransaction.payMoney(Context.User.Id, -amount, Context.Guild.Id);
                await ReplyAsync("Payment succesful! Your balance is now " + (senderBal - amount) + " Bits!");
            }
        }
        [Command("bet")]
        public async Task BetAsync(long amount)
        {
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE" || balanceString == "0")
            {
                await ReplyAsync("You dont have any money to bet!");
                return;
            }
            if (amount < 2)
            {
                await ReplyAsync("You have to bet more than that! Cheapskate.");
                return;
            }
            
            long senderBal = long.Parse(balanceString);
            if (amount > senderBal || amount <= 0)
            {
                await ReplyAsync("Sorry, your balance of " + senderBal + " Bits is too low!");
                return;
            }
            Random rand = new Random();
            int chosenNum = rand.Next(0, 100);
            int rowsAffected = 0;
            //give it a 40% chance to succeed!
            if (chosenNum < 41)
            {
                rowsAffected = DBTransaction.payMoney(Context.User.Id, amount, Context.Guild.Id);
                if (rowsAffected == 1)
                {
                    await ReplyAsync("Congrats! You won " + amount + " Bits, bringing your balance to " + (long.Parse(balanceString) + amount) + "Bits!");
                }
                else
                {
                    await ReplyAsync("An error occurred! Contact Hoovier!");
                }
            }
            else
            {
                rowsAffected = DBTransaction.payMoney(Context.User.Id, -amount, Context.Guild.Id);
                if (rowsAffected == 1)
                {
                    await ReplyAsync("Oof. You lost " + amount + " Bits, bringing your balance to " + (long.Parse(balanceString) - amount) + "Bits!");
                }
                else
                {
                    await ReplyAsync("An error occurred! Contact Hoovier!");
                }
            }
        }

        [Command("slots")]
        public async Task playSlots(long amount)
        {
            //check if user has the bits
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE" || balanceString == "0")
            {
                await ReplyAsync("You dont have any money to bet!");
                return;
            }
            if (amount < 2)
            {
                await ReplyAsync("You have to bet more than that! Cheapskate.");
                return;
            }
            long senderBal = long.Parse(balanceString);
            if (amount > senderBal || amount <= 0)
            {
                await ReplyAsync("Sorry, your balance of " + senderBal + " Bits is too low!");
                return;
            }
            //hard codes the emojis that will correspond with every random number
            Dictionary<int, string> emojiDic = new Dictionary<int, string>
            {
                {0, "<:momoderp:670375029741060103>" },
                {1, "<:stare:785151828114931732>" },
                {2, "<:rymnut:644961603619651584>" },
                {3, ":star:" },
                {4, "<:FloDelet:767858750761467904>" }
            };
            int rowsAffected;
            List<int> chosenNumbers = new List<int>();
            Random rand = new Random();
            string response = "";
            for (int i = 0; i < 3; i++)
            {
                //gets a random number and stores it in the list
                chosenNumbers.Add(rand.Next(5));
                //gets random number generated from list.
                response += emojiDic[chosenNumbers[i]] + " ";
            }
            //all match
            if (chosenNumbers[0] == chosenNumbers[1] && chosenNumbers[2] == chosenNumbers[1])
            {
                await ReplyAsync(response);
                rowsAffected = DBTransaction.payMoney(Context.User.Id, amount * 3, Context.Guild.Id);
                await ReplyAsync("3 matches! You win " + (amount * 3) + " Bits, bringing your balance to " + (long.Parse(balanceString) + (amount * 3)) + "Bits!");
                return;
            }
            // if 0 matches 1, if 1 matches 2, or 0 matches 2, but not all three
            else if (chosenNumbers[0] == chosenNumbers[1] || chosenNumbers[2] == chosenNumbers[1] || chosenNumbers[0] == chosenNumbers[2])
            {
                await ReplyAsync(response);
                rowsAffected = DBTransaction.payMoney(Context.User.Id, amount, Context.Guild.Id);
                await ReplyAsync("2 matches! You win " + amount + " Bits, bringing your balance to " + (long.Parse(balanceString) + amount) + "Bits!");
                return;
            }
            else
            {
                await ReplyAsync(response);
                rowsAffected = DBTransaction.payMoney(Context.User.Id, -amount, Context.Guild.Id);
                await ReplyAsync("Whoops, you lost " + amount + "Bits, bringing your balance to " + (long.Parse(balanceString) - amount) + "Bits!");
            }
        }

        [Command("lootbox")]
        [Alias("lb")]

        public async Task lootbox()
        {
            await ReplyAsync("Purchasable Lootboxes:\n**Stone:** 100 Bits```0-200 Bit Prizes```**Iron:** 250 Bits\n```100-400 Bit Prizes```**Gold**: 1000 Bits\n```250-2000 Bit Prizes```");
        }

        [Command("lootbox")]
        [Alias("lb")]
        public async Task lootbox(string option)
        {
            //check if user has the bits
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE" || balanceString == "0")
            {
                await ReplyAsync("You dont have any money!");
                return;
            }
            long senderBal = long.Parse(balanceString);
            if ((100 > senderBal && option == "stone") || (250 > senderBal && option == "iron") || (1000 > senderBal && option == "gold"))
            {
                await ReplyAsync("Sorry, your balance of " + senderBal + " Bits is too low!");
                return;
            }
            Random rand = new Random();
            long prize = 0;
            if(option == "stone")
            {
                prize = rand.Next(200);
                await ReplyAsync("You paid 100 Bits for a Stone Lootbox and got " + prize + "Bits bringing your balance to " + (senderBal - 100 + prize));
                DBTransaction.payMoney(Context.User.Id, prize-100, Context.Guild.Id);
            }
            else if (option == "iron")
            {
                prize = rand.Next(100,400); 
                await ReplyAsync("You paid 250 Bits for a Iron Lootbox and got " + prize + "Bits bringing your balance to " + (senderBal - 250 + prize));
                DBTransaction.payMoney(Context.User.Id, prize - 250, Context.Guild.Id);
            }
            else if (option == "gold")
            {
                prize = rand.Next(250,2000);
                await ReplyAsync("You paid 1000 Bits for a Gold Lootbox and got " + prize + "Bits bringing your balance to " + (senderBal - 1000 + prize));
                DBTransaction.payMoney(Context.User.Id, prize - 1000, Context.Guild.Id);
            }
            else
            {
                await ReplyAsync("Sorry, you can choose Stone, Iron, or Gold lootboxes only!");
            }
        }

        [Command("lootboxtest")]
        [Alias("lbtest")]
        public async Task lootboxSim(int stoneMin, int stoneMax, int ironMin, int IronMax, int goldMin, int goldMax, int iterations)
        {
            Random rand = new Random();
            int stonetotal = 0, irontotal = 0, goldtotal = 0;

            for (int i = 0; i < iterations; i++)
            {
                stonetotal = stonetotal + rand.Next(stoneMin, stoneMax);
                irontotal = irontotal + rand.Next(ironMin, IronMax);
                goldtotal = goldtotal + rand.Next(goldMin, goldMax);
            }
            await ReplyAsync($"**Stone:** {stoneMin}-{stoneMax}\n**Iron:** {ironMin}-{IronMax}\n**Gold:** {goldMin}-{goldMax}\n**{iterations} iterations run:**\n**StoneSpent:** {iterations * 100} " +
                $"**TotalStone Winnings:** {stonetotal} **StoneAVG:** {stonetotal / iterations}\n**IronSpent:** {iterations * 250} **TotalIron Winnings:** {irontotal} **IronAVG:** {irontotal / iterations}\n" +
                $"**GoldSpent:** {iterations * 1000} **TotalGold Winnings:** {goldtotal} **GoldAVG:** {goldtotal / iterations}");
        }

        }
}
