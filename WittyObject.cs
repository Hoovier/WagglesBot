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
            int prob = rand.Next(0, 100);
            foreach (WittyObject wit in Global.wittyDictionary[context.Guild.Id])
            {
                if (prob < wit.probability * 100)
                {
                     
                    Regex rx = new Regex(wit.trigger);
                    if (rx.IsMatch(message))
                    {
                        int chosen = rand.Next(0, wit.responses.Count);
                        string response = wit.responses.ElementAt(chosen);
                        context.Channel.SendMessageAsync(response);
                    }
                }
            }
        }
    }
}
