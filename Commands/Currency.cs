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
            if(Context.IsPrivate)
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
            
            if(span.TotalHours > 8)
            {
                DBTransaction.giveMoney(Context.User.Id, 100, Context.Guild.Id, dateString);
                string bal = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
                await ReplyAsync("Congrats youve been gifted 100 Bits, your new balance is " + bal);
            }
            else
            {
                double remaining = Math.Round(480 - span.TotalMinutes);
                if(remaining < 61)
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

        public async Task BalanceAsync()
        {
            if (Context.IsPrivate)
            {
                string response = DBTransaction.getMoneyBalanceDMs(Context.User.Id);
                await ReplyAsync(response);
                return;
            }
            string bal = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if(bal == "NONE")
            {
                await ReplyAsync("You are poor and destitute, not a cent to your name! Use ~bonus to beg for some.");
            }
            else
            {
                await ReplyAsync(bal + " Bits!");
            }
        }
        [Command("balance")]
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

        [Command("pay")]
        public async Task PayAsync(SocketGuildUser user, int amount)
        {
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE")
            {
                return;
            }
            int senderBal = int.Parse(balanceString);
            if(amount > senderBal || amount <= 0)
            {
                await ReplyAsync("Sorry, your balance of " + senderBal + " Bits is too low!");
                return;
            }
            if(DBTransaction.payMoney(user.Id, amount, Context.Guild.Id) == 1)
            {
                DBTransaction.payMoney(Context.User.Id, -amount, Context.Guild.Id);
                await ReplyAsync("Payment succesful! Your balance is now " + (senderBal - amount) + " Bits!");
            }
        }
        [Command("bet")]
        public async Task BetAsync(int amount)
        {
            string balanceString = DBTransaction.getMoneyBalance(Context.User.Id, Context.Guild.Id);
            if (balanceString == "NONE" || balanceString == "0")
            {
                await ReplyAsync("You dont have any money to bet!");
                return;
            }
            if (amount < 1)
            {
                await ReplyAsync("You have to bet more than that! Cheapskate.");
                return;
            }
            int senderBal = int.Parse(balanceString);
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
                //this makes sure that no matter how low they bet they will always get a prize if they win.
                if(amount == 1)
                {
                    amount = 2;
                }
                rowsAffected = DBTransaction.payMoney(Context.User.Id, amount / 2, Context.Guild.Id);
                if (rowsAffected == 1)
                {
                    await ReplyAsync("Congrats! You won " + (amount / 2) + " Bits, bringing your balance to " + (int.Parse(balanceString) + (amount / 2)) + "Bits!");
                }
                else
                {
                    await ReplyAsync("An error occurred! Contact Hoovier!");
                }
            }
            else
            {
                rowsAffected = DBTransaction.payMoney(Context.User.Id, -(amount / 2), Context.Guild.Id);
                if (rowsAffected == 1)
                {
                    await ReplyAsync("Oof. You lost " + (amount / 2) + " Bits, bringing your balance to " + (int.Parse(balanceString) - (amount / 2)) + "Bits!");
                }
                else
                {
                    await ReplyAsync("An error occurred! Contact Hoovier!");
                }
            }

        }
    }
}
