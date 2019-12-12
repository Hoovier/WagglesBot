using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{

    public class WittyCommands : ModuleBase<SocketCommandContext>
    {
        [Command("witty add")]
        public async Task addWitty(string name, string match, double probability, params String[] responses)
        {
            if (probability > 1.0 || probability < 0.0)
            {
                await ReplyAsync("Pick a probability between 1.0 and 0");
                return;
            }

            WittyObject temp = new WittyObject(name, match, probability, responses.ToList());
            if (!Global.wittyDictionary.ContainsKey(Context.Guild.Id))
            {
                Global.wittyDictionary.Add(Context.Guild.Id, new List<WittyObject>());
            }
            Global.wittyDictionary[Context.Guild.Id].Add(temp);
            await ReplyAsync("Added Witty with name: " + temp.name);
        }
        [Command("witty remove")]
        public async Task RemWitty(string name)
        {
            WittyObject temp;
            foreach (WittyObject wit in Global.wittyDictionary[Context.Guild.Id])
            {
                if (wit.name == name)
                {
                    Global.wittyDictionary[Context.Guild.Id].Remove(wit);
                    await ReplyAsync("removed " + name + "!");
                    return;
                }
            }
            await ReplyAsync("Could not find that Witty, try '~witty list' to see see all wittys!");
        }
            
        [Command("witty list")]
        public async Task listWitty()
        {
            if (!Global.wittyDictionary.ContainsKey(Context.Guild.Id))
            {
                await ReplyAsync("No wittys registered in this server yet, use '~help witty' to find out how to add one!");
                return;
            }
            else
            {
                string allWitties = Global.wittyDictionary[Context.Guild.Id].Count + " Witties: \n";
                foreach (WittyObject wit in Global.wittyDictionary[Context.Guild.Id])
                {
                    allWitties = allWitties + wit.name + "\n";
                }
                await ReplyAsync(allWitties);
            }
        }
        [Command("witty get")]
        public async Task getWitty(string name)
        {
            if (!Global.wittyDictionary.ContainsKey(Context.Guild.Id))
            {
                await ReplyAsync("No wittys registered in this server yet, use '~help witty' to find out how to add one!");
                return;
            }
            else
            {
                foreach (WittyObject wit in Global.wittyDictionary[Context.Guild.Id])
                {
                    if (wit.name == name)
                    {
                        string response = "**Name:** " + wit.name + "\n**Regex:** " + wit.trigger + "\n**Probability:** " + wit.probability + "\n**Responses:** \n";
                        foreach (string words in wit.responses)
                            response = response + words + "\n";
                        await ReplyAsync(response);
                        return;
                    }
                }
            }
            await ReplyAsync("Witty by that name not found! Try ~witty list to get all witty names!");
        }
        [Command("wittydesc")]
        public async Task describe()
        {
            await ReplyAsync(Global.wittyDictionary[Context.Guild.Id].First().responses.Count.ToString());
        }
    }
}
