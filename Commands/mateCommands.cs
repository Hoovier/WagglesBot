using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            if (stuff != "NONE")
            {
                string[] mateArray = stuff.Split(',');
                await ReplyAsync("Sorry! I'm with " + mateArray[0] + "! (" + mateArray[1] + ")");
                return;
            }
            //initialize react chances with default values
            Global.MateMessageReactChance[Context.Guild.Id] = 70;
            Global.MateHeartReactChance[Context.Guild.Id] = 25;
            await dumpmates();

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
            if (stuff == "NONE")
            {
                await ReplyAsync("I'm not with anyone!");
                return;
            }
            string[] mateArr = stuff.Split(',');
            if (Context.User.Id.ToString() != mateArr[2])
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
        [Command("addresponse")]
        public async Task addresponse(string type, [Remainder] string response)
        {
            if (type != "random" && type != "short" && type != "medium" && type != "long")
            {
                await ReplyAsync("Sorry! Choose one of the following response types: short, medium, long, random!");
                return;
            }
            //if its any of the files ending in absence, just append it to that one, so not random
            if (type != "random")
            {
                System.IO.File.AppendAllText($@"Commands/MateResponses/{type}Absence.txt", Environment.NewLine + response);
                await ReplyAsync("Added **" + response + "** to the **" + type + "Absence** responses!");
            }
            else
            {
                System.IO.File.AppendAllText(@"Commands/MateResponses/randomResponse.txt", Environment.NewLine + response);
                await ReplyAsync("Added **" + response + "** to the **random** responses!");
            }
        }

        [Command("removeresponse")]
        public async Task remResponse(string type, int index)
        {
            if (type != "random" && type != "short" && type != "medium" && type != "long")
            {
                await ReplyAsync("Sorry! Choose one of the following response types: short, medium, long, random!");
                return;
            }
            //if its any of the files ending in absence, just append it to that one, so not random
            if (type != "random")
            {
                List<string> lines = System.IO.File.ReadAllLines($@"Commands/MateResponses/{type}Absence.txt").ToList();
                string resp = lines[index - 1];
                lines.RemoveAt(index - 1);
                System.IO.File.WriteAllLines($@"Commands/MateResponses/{type}Absence.txt", lines.ToArray());
                await ReplyAsync("Removed **" + resp + "** from the **" + type + "Absence** responses!");
            }
            else
            {
                List<string> lines = System.IO.File.ReadAllLines(@"Commands/MateResponses/randomResponse.txt").ToList();
                string resp = lines[index - 1];
                lines.RemoveAt(index - 1);
                System.IO.File.WriteAllLines(@"Commands/MateResponses/randomResponse.txt", lines.ToArray());
                await ReplyAsync("Removed **" + resp + "** from the **Random** responses!");
            }
            
        }

        [Command("getresponses")]
        public async Task getResponses(string type)
        {
            if (type != "random" && type != "short" && type != "medium" && type != "long")
            {
                await ReplyAsync("Sorry! Choose one of the following response types: short, medium, long, random!");
                return;
            }
            //if its any of the files ending in absence, just append it to that one, so not random
            if (type != "random")
            {
                string[] lines = System.IO.File.ReadAllLines($@"Commands/MateResponses/{type}Absence.txt");
                await ReplyAsync("**" + type + " responses:**\n" + string.Join("\n", lines));
            }
            else
            {
                string[] lines = System.IO.File.ReadAllLines($@"Commands/MateResponses/randomResponse.txt");
                await ReplyAsync("**" + type + " responses:**\n" + string.Join("\n", lines));
            }
        }

        //sets and saves chances to file
        [Command("dump mates")]
        public async Task dumpmates()
        {
            Global.MateHeartReactChance[Context.Guild.Id] = 25;
            Global.MateMessageReactChance[Context.Guild.Id] = 70;
            string heart = JsonConvert.SerializeObject(Global.MateHeartReactChance);
            string messReact = JsonConvert.SerializeObject(Global.MateMessageReactChance);
            System.IO.File.WriteAllText(@"Commands/MateResponses/heart.JSON", heart );
            System.IO.File.WriteAllText(@"Commands/MateResponses/mess.JSON", messReact);
        }
        private async Task dumpmateFromJSON()
        {
            string heart = JsonConvert.SerializeObject(Global.MateHeartReactChance);
            string messReact = JsonConvert.SerializeObject(Global.MateMessageReactChance);
            System.IO.File.WriteAllText(@"Commands/MateResponses/heart.JSON", heart);
            System.IO.File.WriteAllText(@"Commands/MateResponses/mess.JSON", messReact);
        }

        [Command("setmatechance")]

        public async Task setMateChance(string type, int value)
        {
            if(value < 0 || value > 100)
            {
                await ReplyAsync("Sorry! Enter a value between 1-100!");
                return;
            }
            else if (type != "heart" && type != "message")
            {
                await ReplyAsync("Sorry! Pick a valid chance type such as **heart** or **message**!");
                return;
            }
            else
            {
                if(type == "heart")
                {
                    Global.MateHeartReactChance[Context.Guild.Id] = value;
                    await ReplyAsync("Success! Set Heart reaction chance to " + value);
                }
                else
                {
                    Global.MateMessageReactChance[Context.Guild.Id] = value;
                    await ReplyAsync("Success! Set random message reaction chance to " + value);
                }
            }
            await dumpmateFromJSON();
        }

        [Command("getmatechances")]
        public async Task getChances()
        {
            await ReplyAsync("Heart React Chance: " + Global.MateHeartReactChance[Context.Guild.Id] + "\nRandom Message Chance: " 
                + Global.MateMessageReactChance[Context.Guild.Id]);
        }



        [Command("absence")]
        public async Task testabsences(string length)
        {
            string[] lines = System.IO.File.ReadAllLines($@"Commands/MateResponses/{length}Absence.txt");
            Random rand = new Random();
            int chosen = rand.Next(lines.Length);
            await ReplyAsync(lines[chosen]);
        }
    }
}
