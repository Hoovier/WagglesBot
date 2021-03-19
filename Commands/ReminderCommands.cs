using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class ReminderCommands : ModuleBase<SocketCommandContext>
    {
        [Command("remindme")]
        public async Task AddReminderAsync(string title, [Remainder]string interval)
        {
            string[] arrayOfTime = interval.Split(" ");
            int time = 0;
            int hours = 0, mins = 0;
            string temp = "";
            foreach (var item in arrayOfTime)
            {
                if(!(item.Contains("H") || item.Contains("h") || item.Contains("M") || item.Contains("m")))
                {
                    await ReplyAsync("That's not gonna work! Make sure to format your response like ``2H 42M`` or ``23h 55m``!");
                    return;
                }
                if(item.Contains("H") || item.Contains("h"))
                {
                    temp = item.Trim(new char[] { 'H', 'h' });
                    hours = int.Parse(temp);
                    time = time + (hours * 60);
                }
                else if(item.Contains("M") || item.Contains("m"))
                {
                    temp = item.Trim(new char[] { 'M', 'm'});
                    mins = int.Parse(temp);
                    time = time + mins;
                }

            }
            if (time < 1)
            {
                await ReplyAsync("Sorry, your reminder has to be longer than 1 minute at least! I'm not a time traveler.");
            }
            else
            {
                DBTransaction.AddReminder(Context.User.Id, Context.Guild.Id, title, time, DateTime.Now.ToString("yyyy-MM-dd.HH:mm:ss"));
                await ReplyAsync("I'll remind you in " + hours + " hour(s) and " + mins + " minute(s)!");
            }
        }
    }
}
