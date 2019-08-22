using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles
{
        public class aliases : ModuleBase<SocketCommandContext>
        {
            [Command("alias add")]
        public async Task setCommand(string name, [Remainder] string command)
        {
            Global.excomm[name] = command;
            string pony = JsonConvert.SerializeObject(Global.excomm);
            var path = "extraComms.JSON";
            if (File.Exists(path))
            {
                File.WriteAllText(path, pony);
                await ReplyAsync("Succesfully found and wrote to the file!");

            }
            else
                await ReplyAsync($"Could not find file at {Directory.GetCurrentDirectory()}");
            
        }

        [Command("alias remove")]
        public async Task remAlias(string name)
        {
            if (Global.excomm.ContainsKey(name))
            {
                string tempcomm = Global.excomm[name];
                Global.excomm.Remove(name);
                await ReplyAsync($"Removed alias {name}: {tempcomm}");
            }
            else
                await ReplyAsync("Sorry, thats not an alias in my memory!");
        }
        [Command("alias edit")]
        public async Task editalias(string name, string newcom)
        {
            if (Global.excomm.ContainsKey(name))
            {
                string tempcomm = Global.excomm[name];
                Global.excomm[name] = newcom;
                await ReplyAsync($"Replaced alias {tempcomm} with {newcom}");
            }
            else
                await ReplyAsync("Sorry, thats not an alias in my memory!");
        }
        [Command("alias get")]
        public async Task getAlias(string name)
        {
            if (Global.excomm.ContainsKey(name))
            {
               
                await ReplyAsync($"Command:```{name} - {Global.excomm[name]}```");
            }
            else
                await ReplyAsync("Sorry, thats not an alias in my memory!");
        }
        [Command("alias list")]
        public async Task listAlias()
        {
            string listOfNames = "**Aliases** \n```";
            
            foreach (KeyValuePair<string,string> i in Global.excomm)
            {
                listOfNames = $"{listOfNames}{i.Key}: {i.Value} \n";
            }
            listOfNames = $"{listOfNames}```";
            await ReplyAsync(listOfNames);
        }
    }
    }
