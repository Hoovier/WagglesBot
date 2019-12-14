using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreWaggles
{
    public class WittyObject
    {
        public string name;
        public string trigger;
        public double probability;
        public List<string> responses;
        public WittyObject(string name, string trigger, double probability, List<string> responses)
        {
            this.name = name;
            this.trigger = trigger;
            this.probability = probability;
            this.responses = responses;
        }
    }
    public class WittyProcessor
    { 
        public static void Process(SocketCommandContext context, string message)
        {
            Random rand = new Random();
            //pick number between 0 and 100
            int prob = rand.Next(0, 100);
            //iterate through witties, see if a wittys regex matches the message
            foreach (WittyObject wit in Global.wittyDictionary[context.Guild.Id])
            {
                //if random number is less than probability * 100, run checks
                // EX. prob = 37, wit.probability * 100 = 40
                //60% chance prob is greater than, 40% chance its less than
                if (prob < wit.probability * 100)
                {
                     
                    Regex rx = new Regex(wit.trigger);
                    //if message fits the regex
                    if (rx.IsMatch(message))
                    {
                        //pick one of the registered responses!
                        int chosen = rand.Next(0, wit.responses.Count);
                        string response = wit.responses.ElementAt(chosen);
                        //send response
                        context.Channel.SendMessageAsync(response);
                    }
                }
            }
        }
    }
}
