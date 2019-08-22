using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


   [Group("boop")]
        public class Boop : ModuleBase<SocketCommandContext>
        {

            [Command]
            public async Task boop1ArgAsync(SocketGuildUser name)
            {

                string[] freshboops;

                freshboops = new string[]
                {
                "https://derpicdn.net/img/view/2016/8/3/1216326.png", "https://derpicdn.net/img/view/2017/11/4/1577548.png", "https://derpicdn.net/img/view/2017/9/6/1529072.gif", "https://derpicdn.net/img/view/2015/2/1/819255.gif"
                };
                Random rand = new Random();

                int pick = rand.Next(freshboops.Length);


                if (name.Username != Context.User.Username)
                {
                    await ReplyAsync($"{Context.User.Mention} booped {name.Username}!");



                }
                else
                {
                    string freshestBoops = freshboops[pick];
                    await ReplyAsync(freshestBoops);

                }



            }

            [Command(RunMode = RunMode.Async)]
            public async Task boopNoArgsAsync()
            {

                int count = Context.Guild.DownloadedMemberCount;
                var groupOfUsers = Context.Guild.Users;

                int cooks = new Random().Next(0, count);
                var userCheck = groupOfUsers.ElementAt(cooks);

                while (userCheck.Username == Context.User.Username)
                {
                    cooks = new Random().Next(0, count);
                    userCheck = groupOfUsers.ElementAt(cooks);
                    Console.WriteLine("I totally picked the same person!");

                }


                await ReplyAsync($"{Context.User.Mention} booped {groupOfUsers.ElementAt(cooks).Username}!");

            }
        }
    

