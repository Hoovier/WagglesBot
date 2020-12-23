using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class mateCommands : ModuleBase<SocketCommandContext>
    {
        [Command("gf grant")]
        [Alias("gf give")]
        public async Task grantGFtoUser(SocketGuildUser user)
        {
            string stuff = DBTransaction.getServerMate(Context.Guild.Id);
            if ( stuff != "NONE")
            {
                string[] mateArray = stuff.Split(',');
                await ReplyAsync("Sorry! I'm with " + mateArray[0] + "! (" + mateArray[1] + ")");
                return;
            }

            DateTime localDate = DateTime.Now;
            string timeNow = localDate.ToString("yyyy-MM-dd.HH:mm:ss");
            //add user to DB, putting the current time for the TimeMated as well as for last message received.
            DBTransaction.InsertMate(user.Id, Context.Guild.Id, timeNow, timeNow);
            await ReplyAsync("Hey " + user.Username + "~");
            
        }

        [Command("callme")]
        [Alias("cm")]
        public async Task setMateNick(string nick)
        {
            string stuff = DBTransaction.getServerMate(Context.Guild.Id);
            if(stuff == "NONE")
            {
                await ReplyAsync("I'm not with anyone!");
                return;
            }
            string[] mateArr = stuff.Split(',');
            if(Context.User.Id.ToString() != mateArr[2])
            {
                await ReplyAsync("Sorry! You're not " + mateArr[1] + "!");
                return;
            }

            DBTransaction.setMateNick(Context.Guild.Id, nick);
            await ReplyAsync("Okay! I'll call you " + nick + "!");
        }

        [Command("gf steal")]
        public async Task stealMate()
        {
            DateTime localDate = DateTime.Now;
            string dateString = localDate.ToString("yyyy-MM-dd.HH:mm:ss");
            string timestamp = DBTransaction.getTimeMated(Context.Guild.Id);
            //figure out how long its been since last mate
            DateTime stamp = DateTime.ParseExact(timestamp, "yyyy-MM-dd.HH:mm:ss", CultureInfo.InvariantCulture);
            TimeSpan span = localDate - stamp;

            if (span.TotalHours > 23)
            {
                string timeNow = localDate.ToString("yyyy-MM-dd.HH:mm:ss");
                DBTransaction.InsertMate(Context.User.Id, Context.Guild.Id, timeNow, timeNow);
                await ReplyAsync("Okay! You seem like fun, tell me what to call you with ~callme <nickname>!");
                return;
            }
            else
            {
                string mateInfo = DBTransaction.getServerMate(Context.Guild.Id);
                string[] mateArr = mateInfo.Split(",");
                await ReplyAsync("Hmm.. no.. I like " + mateArr[1] + " too much!");
            }

        }

        [Command("absence")]
        public async Task testabsences(string length)
        {
            string[] lines = System.IO.File.ReadAllLines($@"Commands\MateResponses\{length}Absence.txt");
            Random rand = new Random();
            int chosen = rand.Next(lines.Length);
            await ReplyAsync(lines[chosen]);
        }
    }
}
