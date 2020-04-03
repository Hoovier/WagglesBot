using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreWaggles.Commands
{
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
            string insertString = "";
            //for all users in server, add them to Users DB, and also add them to relational table that tells us what server they are in
            foreach (SocketGuildUser user in listOfUsers)
            {
                insertString = insertString +  $"INSERT INTO Users VALUES({user.Id}, '{user.Username}') ON CONFLICT(ID) DO NOTHING; " +
                $"INSERT INTO In_Server VALUES({user.Id}, {serverID}) ON CONFLICT DO NOTHING; ";
            }
            //count how many rows are affected, hopefully the same amount of users in the server. 
            //might divide by 2 in future since users are inserted into 2 tables each
            //leverage premade function to run SQL
            int count = insertData(insertString);
            return count;
        }

        //this adds a server to Servers Table, to keep track of servers that bot is in, and for the In_Server table to have a valid foreign key for serverID
        public static void addOrUpdateServer(ulong serverID, string name)
        {
            string insertString = $"INSERT INTO Servers VALUES({serverID}, '{name}') " +
                    $"ON CONFLICT(ID) DO UPDATE SET Name = '{name}';";
            //leverage premade function to run SQL
            insertData(insertString);
        }
        //just runs SQL from two functions above to DB
        private static int insertData(string statement)
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
    }
}
