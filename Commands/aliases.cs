using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            aliases.updateExcomm();
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
                aliases.updateExcomm(); //updates excomm file
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
                aliases.updateExcomm(); //updates excomm file
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

        // Used to make sure the saved file is always the same as one in memory
        public static void updateExcomm()
        {
            string path = "JSONstorage/extraComms.JSON";
            //this check doesnt work, and I dont know why
            //the if statement always evaluates to true
            //new path is correct, but still should have failed in the past
            if (File.Exists(path))
            {
                //serializes dictionary
                string excommJSON = JsonConvert.SerializeObject(Global.excomm);
                //writes it to file
                File.WriteAllText(path, excommJSON);
            }
            else
            {
                Console.WriteLine("Error writing to extraComms.JSON!");
            }
        }
    }
}
