using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class ReminderCommands : ModuleBase<SocketCommandContext>
    {
        [Command("remindme")]
        [Alias("remind me", "rm", "addreminder")]
        public async Task AddReminderAsync(string title, [Remainder] string interval)
        {
            string[] arrayOfTime = interval.Split(" ");
            int time = 0;
            int hours = 0, mins = 0;
            string temp = "";
            foreach (var item in arrayOfTime)
            {
                if (!(item.Contains("H") || item.Contains("h") || item.Contains("M") || item.Contains("m")))
                {
                    await ReplyAsync("That's not gonna work! Make sure to format your response like ``~remindme \"to water the plants\" 2H 42M`` or ``~remindme homework 23h 55m``!");
                    return;
                }
                if (item.Contains("H") || item.Contains("h"))
                {
                    temp = item.Trim(new char[] { 'H', 'h' });
                    hours = int.Parse(temp);
                    time = time + (hours * 60);
                }
                else if (item.Contains("M") || item.Contains("m"))
                {
                    temp = item.Trim(new char[] { 'M', 'm' });
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
                try
                {
                    DBTransaction.AddReminder(Context.User.Id, Context.Guild.Id, title, time, DateTime.Now.ToString("yyyy-MM-dd.HH:mm:ss"));
                    await ReplyAsync("I'll remind you in " + hours + " hour(s) and " + mins + " minute(s)!");
                }
                catch (SQLiteException ex)
                {
                    int errID = DBTransaction.getErrorID(ex.Message);
                    switch (errID)
                    {
                        //if its a foreign key problem, server is not in the Servers Table, so add it and then try again
                        case 0:
                            Console.WriteLine("Foreign key failure when adding Reminder!");
                            await ReplyAsync("Sorry, something went wrong! Contact Hoovier!");
                            
                            break;
                        //if its a duplicate problem, 
                        case 1:
                            Console.WriteLine("Attempted to add Reminder, user already had one Reminder with that name! Server: " + Context.Guild.Name);
                            await ReplyAsync("You already have a Reminder with that title in this server!");
                            break;
                        //unknown error, spit out an error for me. 69 for obvious reasons.
                        case 69:
                            Console.WriteLine("SQL Error: " + ex.Message + "\nErrorNum:" + ex.ErrorCode);
                            await ReplyAsync("Something went wrong, contact Hoovier with error code: " + ex.ErrorCode);
                            break;
                    }
                }
                }
        }

        [Command("reminders")]
        public async Task ListRemindersAsync()
        {
            List<ReminderObject> reminders = DBTransaction.getReminders(Context.User.Id);
            string response = "**__Scheduled Reminders:__**\n";

            foreach (ReminderObject item in reminders)
            {

                DateTime timeAdded = DateTime.Parse(item.timeAdded);
                DateTime now = DateTime.Now;
                //get the amount of time passed since the reminder was added!
                TimeSpan span = now - timeAdded;
                //amount of minutes * 600000000 = ticks for constructor.
                //this holds the amount of time that needs to have passed from when the reminder was added to when it expires
                TimeSpan timeTilReminder = new TimeSpan((long)item.timeInterval * 600000000);
                //this gets the difference between how much time should pass, and how much has actually passed.
                TimeSpan timePassed = timeTilReminder - span;

                response = response + "**" + item.title + ":** in " + timePassed.Hours + " Hours and " + timePassed.Minutes + " Minutes.\n";
            }
            await ReplyAsync(response);
        }

        [Command("removeReminder")]
        public async Task RemoveReminderAsync(string title)
        {
            int rows = DBTransaction.removeReminder(title, Context.Guild.Id, Context.User.Id);
            if(rows == 1)
            {
                await ReplyAsync("Reminder removed!");
            }
            else if(rows == 0)
            {
                await ReplyAsync("No reminder removed, maybe no reminder with that title exists! ");
            }
            else
            {
                await ReplyAsync("Something went wrong, ask Hoovier for help! Error: " + rows);
            }
        }
        }
}
