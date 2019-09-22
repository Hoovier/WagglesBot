using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles
{
        //TODO: Make aliases server specific!
        public class aliases : ModuleBase<SocketCommandContext>
        {
        [Command("alias add")]
        public async Task setCommand(string name, [Remainder] string command)
        {
            //Adds new command to extraCommands dictionary
            Global.excomm[name] = command;
            //saves excomm dictionary to file
            Global.updateExcomm();
            await ReplyAsync("Succesfully found and wrote to the file!");
        }

        [Command("alias remove")]
        public async Task remAlias(string name)
        {
            //checks if dictionary has the command in it
            if (Global.excomm.ContainsKey(name))
            {
                string tempcomm = Global.excomm[name]; //used to store command that is deleted temporarily
                Global.excomm.Remove(name);
                Global.updateExcomm(); //updates excomm file
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
                Global.updateExcomm(); //updates excomm file
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

