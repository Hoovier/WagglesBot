using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{

   public class wordGame : ModuleBase<SocketCommandContext>
    {
        [Command("wg start")]
        [Alias("word start", "wordgame start")]

        public async Task wordgameStart()
        {
            if(Global.wordGameDic.ContainsKey(Context.Channel.Id))
            {
                await wordgameStop(Context.Channel.Id);
            }
            Random rand = new Random();
            //pick a random line from the 8letter.txt
            int lineNum = rand.Next(40160);
            //read single line from file
            string chosen = File.ReadLines("8letter.txt").Skip(lineNum).First();
            //scramble it
            Console.WriteLine("Solution: " + chosen);
            string scrambled = new string(chosen.ToCharArray().OrderBy(s => (rand.Next(2) % 2) == 0).ToArray());
            Global.wordGameDic.Add(Context.Channel.Id, new wgObject());

            Global.wordGameDic[Context.Channel.Id].letters = scrambled;

            Global.wordGameDic[Context.Channel.Id].Solution = chosen;

            await ReplyAsync("Word game has started, letters can be used more than once, one point per letter. Say a 7+ letter word to win instantly!");
            Global.wordGameDic[Context.Channel.Id].message = await Context.Channel.SendMessageAsync("Scrambled: " + String.Join(" ", scrambled.ToList()) + "\n**Answers:**\n");
        }

        [Command("wg scores")]
        [Alias("word score", "wscore", "wgscore")]
        public async Task printWGScore()
        {
            string scoreboard = "**Scores:**\n";
            foreach (KeyValuePair<ulong, int> item in Global.wordGameDic[Context.Channel.Id].scoreCard)
            {
                scoreboard = scoreboard + DBTransaction.getUserFromID(item.Key) + ": " + item.Value + "\n";
            }
            await ReplyAsync(scoreboard);
        }

        [Command("wg stop")]
        [Alias("word stop")]
        public async Task wordgameStop()
        {
            if(Global.wordGameDic.ContainsKey(Context.Channel.Id))
            {
                await wordgameStop(Context.Channel.Id);
            }
            await ReplyAsync("Stopped game.");
        }

        public async Task wordgameStop(ulong id)
        {
            Global.wordGameDic.Remove(id);
        }


    }

    
}
