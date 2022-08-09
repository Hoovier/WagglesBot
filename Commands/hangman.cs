using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CoreWaggles.Commands
{
    public class hangman : ModuleBase<SocketCommandContext>
    {
        [Command("hangman start")]
        [Alias("hm", "hm start")]
        public async Task hangmanStart()
        {
            if (Global.hmGameDic.ContainsKey(Context.Channel.Id))
            {
                await hmgameStop(Context.Channel.Id);
            }
            List<string> poke = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(@"pokemoneng.json"));
            int max = poke.Count;
            Random rand = new Random();
            string pokemonChosen = poke[rand.Next(max)];

            Global.hmGameDic.Add(Context.Channel.Id, new hmObject());
            Global.hmGameDic[Context.Channel.Id].solution = pokemonChosen.ToUpper();
            Global.hmGameDic[Context.Channel.Id].generateHMboard();

            await ReplyAsync("Pokemon Hangman has started, guess one letter at a time, or guess the whole pokemon name to win instantly!");
            Global.hmGameDic[Context.Channel.Id].message = await Context.Channel.SendMessageAsync(Global.hmGameDic[Context.Channel.Id].hmBoard);

        }

        [Command("hm stop")]
        [Alias("hangman stop")]
        public async Task hmgameStop()
        {
            if (Global.hmGameDic.ContainsKey(Context.Channel.Id))
            {
                await hmgameStop(Context.Channel.Id);
            }
            await ReplyAsync("Stopped game.");
        }

        public async Task hmgameStop(ulong id)
        {
            Global.hmGameDic.Remove(id);
        }

       
    }
}

