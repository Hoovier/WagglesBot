using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class todoList : ModuleBase<SocketCommandContext>
    {

        [Command("addtask")]
        public async Task addTask(string name, [Remainder] string description)
        {
            //add a task to list
            DBTransaction.addTask(Context.User.Id, name, description);
            await ReplyAsync("Task added!");
        }
        [Command("removetask")]
        public async Task removeTask(string name)
        {
            //remove task from list
            await ReplyAsync(DBTransaction.removeTask(Context.User.Id, name));
        }
        [Command("tasks")]
        public async Task listTasks()
        {
            //list all tasks
            await ReplyAsync(DBTransaction.listTasks(Context.User.Id, Context.User.Username));
        }

        [Command("tasks")]
        public async Task listTasks(SocketGuildUser user)
        {
            //overload to handle looking at other users lists
            await ReplyAsync(DBTransaction.listTasks(user.Id, user.Username));
        }
        
        [Command("gettask")]
        public async Task getTask(string name)
        {
            await ReplyAsync(DBTransaction.getTask(Context.User.Id, name));
        }
    }
}
