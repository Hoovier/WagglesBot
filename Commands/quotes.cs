using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CoreWaggles;
using System.Text.RegularExpressions;
using System.IO;

namespace CoreWaggles.Commands
{
    public class quotes : ModuleBase<SocketCommandContext>
    {
        [Command("authorize")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AddUserPerm(SocketGuildUser user)
        {
            DBTransaction.insertData($"INSERT INTO Authorized_Users VALUES({user.Id}, 'Quotes', {Context.Guild.Id});");
            await ReplyAsync("Added " + user.Username);
        }

        [Command("deauthorize")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemoveUserPerm(SocketGuildUser user)
        {
            DBTransaction.insertData($"DELETE FROM Authorized_Users WHERE UserID = {user.Id} AND Permission = 'Quotes' AND ServerID = {Context.Channel.Id};");
            await ReplyAsync("Removed " + user.Username);
        }

        [Command("addquote")]
        public async Task addQuote(SocketGuildUser user, [Remainder]string quote)
        {
            //check to see if user is in table for authorized users
            if (checkUserPermission(user))
            {
                DBTransaction.addQuote(user.Id, quote, Context.Guild.Id);
                await ReplyAsync("Added Quote!");
                return;
            }
            //didnt have perm, report that!
                await ReplyAsync("You are not allowed to do that! Have an admin give you permission by using ```~authorize <User>```");
        }

        [Command("quote")]
        public async Task pickQuote(SocketGuildUser user)
        {
            string response = DBTransaction.pickQuote(Context.Guild.Id, user.Id);
            await ReplyAsync(response);
        }
        [Command("quote")]
        public async Task pickQuoteNoArg()
        {
            //pass no user so it can pick from any
            string response = DBTransaction.pickQuote(Context.Guild.Id, 0);
            await ReplyAsync(response);
        }
        [Command("searchquote")]
        public async Task searchQuotes(SocketGuildUser user)
        {
            string response = DBTransaction.listQuoteFromUser(Context.Guild.Id, user.Id, user.Username);
            //check to see if response is too long, and if it is just send it as a txt file.
            if(response.Length > 1999)
            {
                using (var textDoc = new StreamWriter(@"message.txt"))
                {
                    textDoc.Write(response);
                }
                await Context.Channel.SendFileAsync(@"message.txt");
                return;
            }
            await ReplyAsync(response);
        }

        [Command("removequote")]
        public async Task remQuote(SocketGuildUser user, int index)
        {
            if(!checkUserPermission(user))
            {
                await ReplyAsync("You are not allowed to do that! Have an admin give you permission by using ```~authorize <User>```");
                return;
            }

            string response = DBTransaction.removeQuote(Context.Guild.Id, user.Id, index);
            await ReplyAsync(response);
        }
        //this command is for stealing quotes already in SweetieBot and having Waggles remove the indexes and process them fast, little user input needed.
        [Command("parsequote")]
        public async Task parsequote(SocketGuildUser user, [Remainder]string quotes)
        {
            if (!checkUserPermission(user))
            {
                await ReplyAsync("You are not allowed to do that! Have an admin give you permission by using ```~authorize <User>```");
                return;
            }

            int counter = 0;
            string response = " Responses Added: \n", currString;
            string[] chosen = quotes.Split("\n");
            //look for a number and a "." I.E ("26." or "5.")
            Regex rx = new Regex(@"\d+\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (string res in chosen)
            {
                MatchCollection matches = rx.Matches(res);
                if (matches.Count > 0)
                {
                    counter++;
                    //delete the number and . from quote and then stick it into DB
                    currString = res.Replace(matches[0].Groups[0].Value, "");
                   DBTransaction.addQuote(user.Id, currString, Context.Guild.Id);
                    response = response + currString + "\n";
                }
            }
            await ReplyAsync(counter + response);
        }

        private bool checkUserPermission(SocketGuildUser user)
        {
            //check to see if user is in table for authorized users
            if (DBTransaction.canModifyQuotes(user.Id, "Quotes"))
            {
                return true;
            }
            //if they are not, just check to see if they have permission to modify channels, run anyway if they do
            else if ((user as IGuildUser).GuildPermissions.ManageChannels)
            {
                return true;
            }
            //didnt fit any of the past two, dont allow
            return false;

        }

    }
}
