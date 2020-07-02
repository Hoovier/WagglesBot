﻿using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreWaggles.Commands
{
    //todo: Handle Errors relating to foreign keys, such as ServerID not in table, or UserID not being in User table!
    //possibly move the actual SQL into command specific file instead of piling all SQL stuff into DBTransaction.
    //get rid of repeated code!
    //expand upon Authorization table to include more permissions for waggles commands and user granting
    internal static class DBTransaction
    {
        //cs is connection string for DB
        private static readonly string cs = @"URI=file:WagglesDB.db; foreign keys=true;";

        public static int getErrorID(string errMessage)
        {
            if (errMessage.Contains("FOREIGN KEY"))
                return 0;
            else if (errMessage.Contains("UNIQUE"))
                return 1;
            else
                return 69;
        }
        public static int updateUserList(List<SocketGuildUser> listOfUsers, ulong serverID)
        {
            int rowsAffected = 0;
            //for all users in server, add them to Users DB, and also add them to relational table that tells us what server they are in
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                foreach (SocketGuildUser user in listOfUsers)
                {
                    cmd.CommandText = $"INSERT INTO Users VALUES(@UserID, @Username) ON CONFLICT(ID) DO NOTHING; INSERT INTO In_Server VALUES(@UserID, @ServerID) ON CONFLICT DO NOTHING; ";
                    cmd.Parameters.AddWithValue("@UserID", user.Id);
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@ServerID", serverID);
                    if (cmd.ExecuteNonQuery() == 2)
                    {
                        rowsAffected++;
                    }
                }
            }
            
            return rowsAffected;
        }

        //this adds a server to Servers Table, to keep track of servers that bot is in, and for the In_Server table to have a valid foreign key for serverID
        public static void addOrUpdateServer(ulong serverID, string name)
        {
            //Use prepared statement in order to assure the correct insertion and prevent SQL injection.
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                    cmd.CommandText = $"INSERT INTO Servers VALUES({serverID}, @serverName) " +
                    $"ON CONFLICT(ID) DO UPDATE SET Name = '@serverName';";
                    cmd.Parameters.AddWithValue("@serverName", name);
            }
        }
        //just runs SQL from two functions above to DB
        public static int insertData(string statement)
        {
            int rowsChanged = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();

            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = statement;
                rowsChanged = cmd.ExecuteNonQuery();
            }
            return rowsChanged;
        }

        //function that allows users to run raw SQL,usage to be restricted to Hoovier and LazyReader.
        public static string runSQL(string sql)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            string response = "```";
            using var commd = new SQLiteCommand(sql, con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            //prepares response string by casting all columns to string regardless of type
            while (rdr.Read())
            {
                for(int counter = 0; counter < rdr.FieldCount; counter++)
                {
                    response = response + " |" + rdr[counter].ToString() + "| \t";
                }
                response = response + "\n";
            }
            response = response + "```";
            return response;
        }

        public static string addAliasedCommand(string name, string command, ulong serverID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "INSERT INTO Aliased_Commands VALUES(@name, @command, @serverID);";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@command", command);
                cmd.Parameters.AddWithValue("@serverID", serverID);
                rowsAffected = cmd.ExecuteNonQuery();
            }

            //pretty self explanatory, if one row is affected, command is added
            //if no rows are affected, theres a problem
            //if multiple rows are affected, panic!
            if (rowsAffected == 1)
                return "Succesfully added aliased command!";
            if (rowsAffected == 0)
                return "An error occured, no aliased command added";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        public static string removeAliasedCommand(string name, ulong serverID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "DELETE FROM Aliased_Commands WHERE Name=@name AND ServerID=@serverID";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@serverID", serverID);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully removed aliased command!";
            if (rowsAffected == 0)
                return "An error occured, no aliased command removed. Does the command exist or is the command name misspelled?";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        public static string editAliasedCommand(string name, string newCommand, ulong serverID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "UPDATE Aliased_Commands SET Command=@newCommand WHERE Name=@name AND ServerID=@serverID";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@serverID", serverID);
                cmd.Parameters.AddWithValue("@newCommand", newCommand);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully edited aliased command!";
            if (rowsAffected == 0)
                return "An error occured, no aliased command edited. Does the command exist or is the command name misspelled?";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        //scanningMessage is a bool that allows me to repurpose the same code to return data in form that waggles can use to check incoming commands
        public static string getAliasedCommand(string name, ulong serverID, bool scanningMessage)
        {
            string response = "No command found!";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand("SELECT Command FROM Aliased_Commands WHERE Name=@name AND ServerID=@serverID", con);
            commd.Parameters.AddWithValue("@name", name);
            commd.Parameters.AddWithValue("@serverID", serverID);
            using SQLiteDataReader rdr = commd.ExecuteReader();

            if (scanningMessage)
            {
                //if there is data there, it will return it, otherwise return empty string
                if (rdr.Read())
                    return rdr.GetString(0);
                else
                    return string.Empty;
            }

            while (rdr.Read())
            {
                response = "Alias - Command ```Name: " + name + " - Command: " + rdr.GetString(0) + "```";   
            }
            return response;
        }

        public static string listAliasedCommands(ulong serverID)
        {
            string response = "Alias - Command ```";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand("SELECT Name, Command FROM Aliased_Commands WHERE ServerID=@serverID", con);
            commd.Parameters.AddWithValue("@serverID", serverID);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                response = response + rdr.GetString(0) + " - " + rdr.GetString(1) + "\n";
            }
            if (response == "Alias - Command ```")
                return "No Commands for this server found!";
            else
                return response + "```";
        }

        public static string addWitty(string name, string match, ulong serverID, double probability, List<string> responses)
        {
            int rowsAffected = 0;
            string witID;
            using var con = new SQLiteConnection(cs);
            con.Open();
            //get max
            using var commd = new SQLiteCommand("SELECT MAX(WittyID) FROM Wittys", con);
            {
                using SQLiteDataReader rdr = commd.ExecuteReader();
                rdr.Read();
                witID = (rdr.GetInt64(0) + 1).ToString();
            }
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "INSERT INTO Wittys(Name, Trigger, Probability, ServerID, WittyID) VALUES(@Name, @Trigger, @Probability, @ServerID, " +
                    witID + ");";
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Trigger", match);
                cmd.Parameters.AddWithValue("@Probability", probability);
                cmd.Parameters.AddWithValue("@ServerID", serverID);
                rowsAffected = cmd.ExecuteNonQuery();
                    foreach(string response in responses)
                    {
                        //multiple Inserts, dont know if this affects performance but its the only way to do prepared statements
                        cmd.CommandText = "INSERT INTO Responses VALUES(@Response, @WittyID);";
                        cmd.Parameters.AddWithValue("@Response", response);
                        cmd.Parameters.AddWithValue("@WittyID", witID);
                        rowsAffected = cmd.ExecuteNonQuery();
                    }
            }
            return "Added Witty!";
        }
        public static string removeWitty(string name, ulong serverID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM Wittys WHERE Name=@name AND ServerID=@serverID";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@serverID", serverID);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully removed Witty!";
            if (rowsAffected == 0)
                return "An error occured, no witty removed. Does the witty exist or is the witty name misspelled?";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        public static string listWitty(ulong serverID)
        {
            string response = "Wittys ```\n";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Name FROM Wittys WHERE ServerID={serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                //Console.WriteLine(response);
                response = response + rdr.GetString(0) + "\n";
            }
            if (response == "Wittys ```")
                return "No Wittys for this server found!";
            else
                return response + "```";
        }
        public static string getWitty(string name, ulong serverID)
        {
            string response = "No Witty found with that name!";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand("SELECT Name, Trigger, Probability FROM Wittys WHERE Name=@name AND ServerID=@serverID", con);
            commd.Parameters.AddWithValue("@name", name);
            commd.Parameters.AddWithValue("@serverID", serverID);
            using SQLiteDataReader rdr = commd.ExecuteReader();

            while (rdr.Read())
            {
                response = "```Name: " + rdr.GetString(0) + "\nTrigger: " + rdr.GetString(1) +  "\nProbability: " + rdr.GetDouble(2) + "```";
            }
            return response;
        }

        public static void processWitty(SocketCommandContext context, string message)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand("SELECT WittyID, Trigger, Probability FROM Wittys WHERE ServerID=" + context.Guild.Id, con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            Random rand = new Random();
            //pick number between 0 and 100
            int prob = rand.Next(0, 100);
            //iterate through witties, see if a wittys regex matches the message
            while (rdr.Read())
            {
                //if random number is less than probability * 100, run checks
                // EX. prob = 37, wit.probability * 100 = 40
                //60% chance prob is greater than, 40% chance its less than
                if (prob < rdr.GetDouble(2) * 100)
                {
                    string response = "";
                    Regex rx = new Regex(rdr.GetString(1));
                    //if message fits the regex
                    if (rx.IsMatch(message))
                    {
                        //pick one of the registered responses!
                        //get how many responses there are
                        using var responseCommand = new SQLiteCommand("SELECT count(Response) FROM Responses WHERE WittyID = " + rdr.GetInt32(0), con);
                        using SQLiteDataReader responseCount = responseCommand.ExecuteReader();
                        //get first row
                        responseCount.Read();
                        int numOfResponses = responseCount.GetInt32(0);
                        //close down reader so that responseCommand can be reused
                        responseCount.Close();
                        //get responses
                        responseCommand.CommandText = "SELECT Response FROM Responses WHERE WittyID = " + rdr.GetInt32(0);
                        using SQLiteDataReader responses = responseCommand.ExecuteReader();
                        //get index of chosen response
                        int chosen = rand.Next(0, numOfResponses);
                        //loop through responses and move reader along until we reach desired index
                        for(int counter = 0; counter <= chosen; counter++)
                        {
                            responses.Read();
                            if (counter == chosen)
                            {
                                response = responses.GetString(0);
                                context.Channel.SendMessageAsync(response);
                                //return here or else itll keep making multiple matches
                                return;
                            }
                        }
                    }
                }
            }
        }

        public static void addChannelToWhitelist(ulong channelID, ulong serverID, string channelName)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "INSERT INTO Whitelist VALUES(@ChannelID, @ServerID, @ChannelName);";
                cmd.Parameters.AddWithValue("@ChannelID", channelID);
                cmd.Parameters.AddWithValue("@ServerID", serverID);
                cmd.Parameters.AddWithValue("@ChannelName", channelName);
                cmd.ExecuteNonQuery();
            }
        }

        public static string removeChannelWhitelist(ulong channelID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "DELETE FROM Whitelist WHERE ChannelID = @ChannelID;";
                cmd.Parameters.AddWithValue("@ChannelID", channelID);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully removed channel from Whitelist!";
            if (rowsAffected == 0)
                return "An error occured, no channel removed. Channel was not possibly already in Whitelist.";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        public static string listWhitelistedChannels(ulong serverID)
        {
            string response = "Whitelisted Channels ```\n";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT ChannelName FROM Whitelist WHERE ServerID={serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            //same idea as listWitty, just loop through all the responses and spit out names of whitelisted channels!
            while (rdr.Read())
            {
                response = response + rdr.GetString(0) + "\n";
            }
            if (response == "Whitelisted Channels ```\n")
                return "No Whitelisted Channels for this server found!";
            else
                return response + "```";
        }
        public static bool isChannelWhitelisted(ulong channelID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //select count of random attribute, since they all should be unique, it shouldnt matter
            using var commd = new SQLiteCommand($"SELECT count(ChannelName) FROM Whitelist WHERE ChannelID ={channelID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                //if the count is 1, that means it is nsfw, otherwise itll return false!
                return rdr.GetInt32(0) == 1;
            }
            //if it reaches here, who knows what happened!
            return false;
        }

        public static bool canModifyQuotes(ulong userID, string permission)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT count(UserID) FROM Authorized_Users WHERE Permission ='{permission}' AND UserID = {userID};", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                //if the count is 1 then we know that user is allowed to modify quotes!
                return rdr.GetInt32(0) == 1;
            }
            //if it reaches here, who knows what happened!
            return false;
        }
        public static bool quoteExists(ulong userID, string message, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT COUNT(Message) FROM Quotes WHERE ServerID={serverID} AND UserID = {userID} AND message = @Message", con);
            commd.Parameters.AddWithValue("@Message", message);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfQuotes = rdr.GetInt32(0);
            rdr.Close();
            //if number is 1, itll return as true, and if it doesnt, itll be false
            return !(numberOfQuotes == 0);
        }
        public static bool addQuote(ulong UserID, string message, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            try
            {
                using var cmd = new SQLiteCommand(con);
                {
                    cmd.CommandText = $"INSERT INTO Quotes VALUES(@Message, {UserID}, {serverID});";
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch(SQLiteException ex)
            {
                return false;
            }
        }


        public static string pickQuote(ulong serverID, ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //by default make string to add to SQL when a user is specified
            string userString = " AND UserID = " + userID.ToString();
            //if 0 is passed, assume any user allowed!
            if(userID == 0)
            {
                userString = "";
            }

            using var commd = new SQLiteCommand($"SELECT COUNT(Message) FROM Quotes WHERE ServerID={serverID}" + userString, con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfQuotes = rdr.GetInt32(0);
            if (numberOfQuotes == 0)
            {
                return "No quotes to show!";
            }
            rdr.Close();
            int chosenIndex = 0, counter = 0;
            Random rand = new Random();
            chosenIndex = rand.Next(0, numberOfQuotes);
            commd.CommandText = $"SELECT Username, Message FROM Quotes INNER JOIN Users ON Quotes.UserID = Users.ID WHERE ServerID={serverID}" + userString;
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                if (counter == chosenIndex)
                {

                    return "**[" + msgs.GetString(0) + "]:** " + msgs.GetString(1);
                }
                counter++;
            }
            return "An error occured";
        }

        public static string listQuoteFromUser(ulong serverID, ulong userID, string username)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int index = 1;
            using var commd = new SQLiteCommand($"SELECT COUNT(Message) FROM Quotes WHERE ServerID={serverID} AND UserID = {userID} ORDER BY rowid", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfQuotes = rdr.GetInt32(0);
            if (numberOfQuotes == 0)
            {
                return "No quotes to show!";
            }
            rdr.Close();

            string response = $"**__Quotes from {username}__** \n";
            commd.CommandText = $"SELECT Message FROM Quotes  WHERE ServerID={serverID} AND UserID = {userID} ORDER BY rowid";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                response = response + "**" + index + ".** " + msgs.GetString(0) + "\n";
                index++;
            }
            return response;
        }
        public static string removeQuote(ulong serverID, ulong userID, int index)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();

            using var commd = new SQLiteCommand($"SELECT COUNT(Message) FROM Quotes WHERE ServerID={serverID} AND UserID = {userID} ORDER BY rowid", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfQuotes = rdr.GetInt32(0);
            if (numberOfQuotes < index)
            {
                return "Not a valid quote index! Try 1-" + numberOfQuotes;
            }
            rdr.Close();
            //set rowid to 10000 so that if loop to find rowid fails, the SQL wont delete another quote. Assuming we dont have 10k quotes in there.
            int counter = 1, rowsAffected = 0, rowid = 10000;

            commd.CommandText = $"SELECT rowid FROM Quotes WHERE ServerID={serverID} AND UserID = {userID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                if (counter == index)
                {
                    //save rowid for later
                    rowid = msgs.GetInt32(0);
                    //once rowid is found, break out of loop!
                    break;
                }
                counter++;
            }
            msgs.Close();
            //use insertData cause it does the same thing as what we want to do here, despite name.
            rowsAffected = insertData("DELETE FROM Quotes WHERE rowid = " + rowid);
            if (rowsAffected == 1)
                    return "Succesfully removed quote!";
            if (rowsAffected == 0)
                    return "An error occured, no quote was removed.";
            else
                    return "ERROR! Code: " + rowsAffected;
        }
        public static void addTask(ulong UserID, string name, string description)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO TODO_Tasks VALUES({UserID}, @name, @description, datetime('now'));";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.ExecuteNonQuery();
            }
        }
        public static string removeTask(ulong userID, string name)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"DELETE FROM TODO_Tasks WHERE ListOwner= {userID} AND TaskName= @name;";
                cmd.Parameters.AddWithValue("@name", name);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully removed task from list!";
            if (rowsAffected == 0)
                return "An error occured, no task removed. Maybe the task doesn't exist!";
            else
                return "ERROR! Code: " + rowsAffected;
        }
        public static string listTasks(ulong userID, string username)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int index = 1;
            using var commd = new SQLiteCommand($"SELECT COUNT(TaskName) FROM TODO_Tasks WHERE ListOwner={userID} ORDER BY TimeAdded", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfTasks = rdr.GetInt32(0);
            if (numberOfTasks == 0)
            {
                return "No tasks to show!";
            }
            rdr.Close();

            string response = $"**__Tasks for {username}__** \n";
            commd.CommandText = $"SELECT TaskName, TaskDesc FROM TODO_Tasks WHERE ListOwner={userID} ORDER BY TimeAdded";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                response = response + "**" + index + ". " + msgs.GetString(0) +":** "+ msgs.GetString(1) +  "\n";
                index++;
            }
            return response;
        }
        public static string getTask(ulong userID, string taskName)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int index = 1;
            using var commd = new SQLiteCommand($"SELECT COUNT(TaskName) FROM TODO_Tasks WHERE ListOwner={userID} AND TaskName= @taskname", con);
            commd.Parameters.AddWithValue("@taskname", taskName);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfTasks = rdr.GetInt32(0);
            if (numberOfTasks == 0)
            {
                return "No tasks to show!";
            }
            rdr.Close();

            string response = $"**__Task - Description__** \n";
            commd.CommandText = $"SELECT TaskName, TaskDesc FROM TODO_Tasks WHERE ListOwner={userID} AND TaskName= @taskname";
            commd.Parameters.AddWithValue("@taskname", taskName);
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                response = response + "**" + index + ". " + msgs.GetString(0) + ":** " + msgs.GetString(1) + "\n";
                index++;
            }
            return response;
        }
    }
}
