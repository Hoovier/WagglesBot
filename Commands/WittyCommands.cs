﻿using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{

    public class WittyCommands : ModuleBase<SocketCommandContext>
    {
        public readonly string path = "JSONstorage/Wittys.JSON";
        [Command("witty add")]
        public async Task addWitty(string name, string match, double probability, params String[] responses)
        {
            //Makes sure probability, when multiplied by 100, will be valid.
            //EX. .40 * 100 = 40% chance of happening.
            if (probability > 1.0 || probability < 0.0)
            {
                await ReplyAsync("Pick a probability between 1.0 and 0");
                return;
            }
            //call wittyobject constructor with given values
            WittyObject temp = new WittyObject(name, match, probability, responses.ToList());

            //error handling for null wittyDictionary List objects.
            if (!Global.wittyDictionary.ContainsKey(Context.Guild.Id))
            {
                Global.wittyDictionary.Add(Context.Guild.Id, new List<WittyObject>());
            }
            //adds witty to associated server wide dictionary
            Global.wittyDictionary[Context.Guild.Id].Add(temp);
            //write object JSON info to file
            string channelObject = JsonConvert.SerializeObject(Global.wittyDictionary);
            if (File.Exists(path))
            {
                File.WriteAllText(path, channelObject);
            }
            else
            {
                await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");
                return;
            }
            await ReplyAsync("Added Witty with name: " + temp.name);
        }
        [Command("witty remove")]
        public async Task RemWitty(string name)
        {
            //iterate through List of witties, compare names to given name.
            foreach (WittyObject wit in Global.wittyDictionary[Context.Guild.Id])
            {
                if (wit.name == name)
                {
                    Global.wittyDictionary[Context.Guild.Id].Remove(wit);
                    await ReplyAsync("removed " + name + "!");
                    //write witties to file
                    string channelObject = JsonConvert.SerializeObject(Global.wittyDictionary);
                    if (File.Exists(path))
                    {
                        File.WriteAllText(path, channelObject);
                    }
                    else
                    {
                        await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");
                        return;
                    }
                    return;
                }
            }
            //only reaches this point if entire list does not match.
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
        //more to be added later!
        [Command("wittydesc")]
        public async Task describe()
        {
            string response = Global.wittyDictionary[Context.Guild.Id].Count + " Wittys in my memory. \n";
            await ReplyAsync(response);
        }
    }
}