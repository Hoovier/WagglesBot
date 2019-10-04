using CoreWaggles;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles

    /*
     * Poll = user provided list of users, taken in as a SocketUser[] players
     * contestants & ballot = list of candidates/people running
     * 
     */
{
    public class election : ModuleBase<SocketCommandContext>
    {
        [Command("election add")]
        public async Task addCompetitors(params SocketUser[] players)
        {
            string added = "Added: ";
            string notAdded = "Not Added(Already in race): ";
            foreach(SocketUser user in players)
            {
                //checks if any of the users(Poll) to be added is already running
                if (Global.contestants.Contains(user))
                {
                    notAdded = notAdded + " " + user.Username;
                }
                else
                {
                    Global.contestants.Add(user);
                    added = added + " " + user.Username; 
                }
            }
            
            
            await ReplyAsync(added + "\n" + notAdded);
        }

        [Command("election join")] //allows a user to add themself to contestants
        public async Task addSelf()
        {
            if (Global.contestants.Contains(Context.Message.Author))
            {
                await ReplyAsync("Already in the running!");
            }
            else
            {
                Global.contestants.Add(Context.Message.Author);
                await ReplyAsync("Joined succesfully!");
            }
        }

        [Command("election list")]
        public async Task listRunners()
        {
            string runners = "Runners: ";
            foreach(SocketUser user in Global.contestants)
            {
                runners = runners + " " + user.Username + ",";
            }
            //removes the last comma in string
            string fixedString = runners.Remove(runners.Length - 1);
           await ReplyAsync(fixedString);
        }
        [Command("vote")]
        public async Task voteAsync(params SocketUser[] players)
        {
            //checks if all the players in this vote are on the ballot/running for office
            bool ballotMatches = matchLists(players);
            //if user already voted and the new Poll provided matches the ballot, modify vote
            if (Global.votes.ContainsKey(Context.Message.Author) && ballotMatches)
            {
                await ReplyAsync("Already voted! Modifying vote...");
                Global.votes[Context.Message.Author] = new List<SocketUser>(players);
                await getVotes();
            }
            //if the Poll matches the Ballot, put in a new vote into records
            else if (ballotMatches)
            {
                Global.votes[Context.Message.Author] = new List<SocketUser>(players);
                await getVotes();
            }
            //if at least one of the users on the Poll doesnt match Ballot, error!
            else if (ballotMatches == false)
            {
                await ReplyAsync("At least one of the users provided is not running!");
            }
            //I hope this one never happens, but who knows!
            else
                await ReplyAsync("Something went wrong, ask Hoovier for help!(TBH if you get this error, he probably wont know either.)");
        }
        [Command("results")]
        //this function is going to calculate all the votes put together
        //so far all of the votes are seperated from one another, this one combines all.
        public async Task resultsAsync()
        {
            int points;
            if (Global.votes.Count == 0)
            {
                await ReplyAsync("Sorry! There are no votes yet, try voting using ~vote <users>");
            }
            else
            {
                string response = "**Results:** \n";
                //temporary dictionary to hold Contestant:Points information
                Dictionary<SocketUser, int> scores = new Dictionary<SocketUser, int>();
                //this will iterate through the dictionary of Votes
                //that dictionary looks like a User and a List of Users on the ballot, in descending order             
                //Voter:Poll I.E. Hoovier:Star,Lazy,Swoots
                foreach (KeyValuePair<SocketUser, List<SocketUser>> pair in Global.votes)
                {
                    //my primitive point award is based on the first user on List to get max points, lowering by 1 for each successive user
                    points = Global.contestants.Count;
                    //this will iterate through list of Users, the Poll provided by the Value in Votes.
                    foreach (SocketUser user in pair.Value)
                    {
                        //not sure if this is... necessary, but I saw glitches a couple months back when trying to add a value to a nonexistent key
                        if (!scores.ContainsKey(user))
                        {
                            scores.Add(user, points);
                            points--;
                        }
                        else //this awards the user in our temporary dictionary the points, for use later
                        {
                            scores[user] += points;
                            points--;
                        }  
                    }
                }
                //iterate through scores dictionary, and post key:value
                foreach (SocketUser contestant in scores.Keys)
                {
                    response = $"{response}{contestant.Username} {scores[contestant]}\n";
                }
                await ReplyAsync(response);
            }
        }

        [Command("getvote")]
        public async Task getVotes()
        {
            //if someone has voted already
            if (Global.votes.ContainsKey(Context.Message.Author))
            {
                await getResults(Context.Message.Author);
            }
            else
            {
                await ReplyAsync("You haven't voted yet!");
            }
        }
        [Command("getvote")]
        //overloaded function for seeing how other users voted
        public async Task getVotes(SocketUser user)
        {
            if (Global.votes.ContainsKey(user))
            {
                await getResults(Context.Message.Author);
            }
            else
            {
                await ReplyAsync("They haven't voted yet!");
            }
        }


        private async Task getResults(SocketUser user)
        { 
        string response = $"**{Context.Message.Author.Username}'s Votes:** \n";
        int amount = Global.contestants.Count;
            //gets array of users from Votes, that correspond to the users Poll
        foreach (SocketUser contestant in Global.votes[Context.Message.Author])
        {
            //calculates points in same way, reducing by one in every iteration
            response = $"{response}{contestant.Username} {amount}\n";
            amount--;
        }
        await ReplyAsync(response);
        }
  
        private bool matchLists(SocketUser[] ballot)
        {
            bool flag = true;
            foreach(SocketUser user in ballot)
            {
                if (!Global.contestants.Contains(user))
                    flag = false;
            }
            return flag;
        }
}
   
}
