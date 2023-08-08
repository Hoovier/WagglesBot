using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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

        //dictionary DB
        private static readonly string dictionaryCS = @"URI=file:Dictionary.db; foreign keys=true;";

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
                for (int counter = 0; counter < rdr.FieldCount; counter++)
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
                foreach (string response in responses)
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
                response = "```Name: " + rdr.GetString(0) + "\nTrigger: " + rdr.GetString(1) + "\nProbability: " + rdr.GetDouble(2) + "```";
            }
            return response;
        }
        public static bool userInExclusionList(ulong userID, ulong serverID)
        {
            //if the user exists in the exclusion table, do not respond to them.
            using var conExclusion = new SQLiteConnection(cs);
            conExclusion.Open();
            using var ExclusionCommand = new SQLiteCommand("SELECT * FROM Witty_Exclusions WHERE ServerID=" + serverID + " AND UserID=" + userID, conExclusion);
            using SQLiteDataReader exrdr = ExclusionCommand.ExecuteReader();
            return exrdr.HasRows;
        }

        public static void processWitty(SocketCommandContext context, string message)
        {
            if(userInExclusionList(context.User.Id, context.Guild.Id))
            {
                return;
            }

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
                        for (int counter = 0; counter <= chosen; counter++)
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

        public static int addWittyExclusion(ulong userID, ulong serverID)
        {
            if(userInExclusionList(userID, serverID))
            {
                return 0;
            }
            using var con = new SQLiteConnection(cs);
            con.Open();
            int effected = 0;
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "INSERT INTO Witty_Exclusions VALUES(@ServerID, @UserID);";
                cmd.Parameters.AddWithValue("@ServerID", serverID);
                cmd.Parameters.AddWithValue("@UserID", userID);
                effected = cmd.ExecuteNonQuery();
            }
            return effected;
        }

        public static int removeWittyExclusion(ulong userID, ulong serverID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = "DELETE FROM Witty_Exclusions WHERE ServerID = @ServerID AND UserID = @UserID;";
                cmd.Parameters.AddWithValue("@ServerID", serverID);
                cmd.Parameters.AddWithValue("@UserID", userID);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            return rowsAffected;

        }

        public static string listWittyExclusions(ulong serverID)
        {
            string response = "Excluded Users: ```\n";
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Users.Username FROM Witty_Exclusions INNER JOIN Users on Users.ID = Witty_Exclusions.UserID WHERE ServerID ={serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            //same idea as listWitty, just loop through all the responses and spit out names of whitelisted channels!
            while (rdr.Read())
            {
                response = response + rdr.GetString(0) + "\n";
            }
            if (response == "Excluded Users: ```\n")
                return "No excluded users for this server found!";
            else
                return response + "```";
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
            catch (SQLiteException ex)
            {
                return false;
            }
        }

        public static List<string> pickQuotesList(ulong serverID, ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT COUNT(Message) FROM Quotes WHERE ServerID={serverID} AND UserID = {userID} ORDER BY rowid LIMIT 5", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfQuotes = rdr.GetInt32(0);
            if (numberOfQuotes == 0)
            {
                return new List<string>();
            }
            rdr.Close();

           
            commd.CommandText = $"SELECT Message FROM Quotes  WHERE ServerID={serverID} AND UserID = {userID} ORDER BY rowid";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            List<string> quotesList = new List<string>();
            while (msgs.Read())
            {
                quotesList.Add(msgs.GetString(0));
                
            }
            return quotesList;

        }

        public static string pickQuote(ulong serverID, ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //by default make string to add to SQL when a user is specified
            string userString = " AND UserID = " + userID.ToString();
            //if 0 is passed, assume any user allowed!
            if (userID == 0)
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

        public static string pickQuoteRaw(ulong serverID, ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //by default make string to add to SQL when a user is specified
            string userString = " AND UserID = " + userID.ToString();
            //if 0 is passed, assume any user allowed!
            if (userID == 0)
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

                    return msgs.GetString(1);
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
                response = response + "**" + index + ". " + msgs.GetString(0) + ":** " + msgs.GetString(1) + "\n";
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
        public static string getMoneyBalanceDMs(ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT COUNT(ServerID) FROM In_Server WHERE UserID={userID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfServers = rdr.GetInt32(0);
            if (numberOfServers == 0)
            {
                return "NONE";
            }
            rdr.Close();

            string response = "Server: Balance\n";
            string bal = "0";
            commd.CommandText = $"SELECT NAME, ServerID FROM In_Server JOIN Servers ON ServerID = ID WHERE UserID = {userID};";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                bal = getMoneyBalance(userID, (ulong)msgs.GetInt64(1));
                if (bal == "NONE")
                { bal = "0"; }
                response = response + "**" + msgs.GetString(0) + ":** " + bal + " Bits\n";
            }
            return response;
        }
        public static void addUsertoMoney(ulong UserID, int amount, ulong serverID, string timestamp)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //Adds users to table with set money as well as a timestamp to determine when they can run their next ~daily
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO Money(UserID, Amount, ServerID, TimeStamp) VALUES({UserID}, {amount}, {serverID}, @timestamp);";
                cmd.Parameters.AddWithValue("@timestamp", timestamp);
                cmd.ExecuteNonQuery();
            }
        }
        public static void giveMoney(ulong UserID, int amount, ulong serverID, string timestamp)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //Adds users to table with set money as well as a timestamp to determine when they can run their next ~daily
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Money SET Amount = Amount + {amount}, TimeStamp = @timestamp WHERE UserID = {UserID} AND ServerID = {serverID};";
                cmd.Parameters.AddWithValue("@timestamp", timestamp);
                cmd.ExecuteNonQuery();
            }
        }

        public static int payMoney(ulong userID, long amount, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int rowsAffected = 0;
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Money SET Amount = Amount + {amount} WHERE UserID = {userID} AND ServerID = {serverID};";
                rowsAffected = cmd.ExecuteNonQuery();
            }
            return rowsAffected;
        }
        public static string getMoneyTimeStamp(ulong userID, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(TimeStamp) FROM Money WHERE UserID={userID} AND ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfTimestamps = rdr.GetInt32(0);
            if (numberOfTimestamps == 0)
            {
                return "NONE";
            }
            rdr.Close();
            commd.CommandText = $"SELECT TimeStamp FROM Money WHERE UserID={userID} AND ServerID= {serverID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            return msgs.GetString(0);
        }

        public static string getMoneyBalance(ulong userID, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(Amount) FROM Money WHERE UserID={userID} AND ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            long numberOfMoney = rdr.GetInt64(0);
            if (numberOfMoney == 0)
            {
                return "NONE";
            }
            rdr.Close();
            commd.CommandText = $"SELECT Amount FROM Money WHERE UserID={userID} AND ServerID= {serverID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            return msgs.GetInt64(0).ToString();
        }

        public static string getMoneyLeaders( ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            string board = "**Leaderboard:**\n```";
            using var commd = new SQLiteCommand($"SELECT Amount, Users.Username FROM Money JOIN Users ON Users.ID = Money.UserID WHERE ServerID={serverID} GROUP BY Users.Username ORDER BY Amount DESC LIMIT 10", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while(rdr.Read())
            {
                board = board + rdr.GetString(1)  + ": " + rdr.GetInt64(0) + " Bits\n";
            }

            return board + "```";
        }

        public static void setReactionRole(ulong roleID, ulong serverID, string emojiName, ulong messageID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //Adds a reaction and role pair to the DB
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO Reaction_Roles(RoleID, ServerID, Emoji, MessageID) VALUES({roleID}, {serverID}, @emoji, @messageID);";
                cmd.Parameters.AddWithValue("@emoji", emojiName);
                cmd.Parameters.AddWithValue("@messageID", messageID);
                cmd.ExecuteNonQuery();
            }
        }
        public static ulong reactionRoleExists(ulong messageID, string emojiName)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT RoleID FROM Reaction_Roles WHERE MessageID={messageID} AND Emoji = '{emojiName}'", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            if (!rdr.HasRows)
            {
                return 0;
            }
            ulong roleID = (ulong)rdr.GetInt64(0);
            rdr.Close();
            //return the roleID so bot can turn it into a role!
            return roleID;
        }

        public static string removeRole(ulong roleID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"DELETE FROM Reaction_Roles WHERE RoleID= {roleID};";
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
                return "Succesfully removed role from list!";
            if (rowsAffected == 0)
                return "An error occured, no role removed. Maybe the role was never saved!";
            else
                return "ERROR! Code: " + rowsAffected;
        }

        public static Dictionary<ulong, string> listRoles(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            Dictionary<ulong, string> temp = new Dictionary<ulong, string>();
            using var commd = new SQLiteCommand($"SELECT RoleID, Emoji FROM Reaction_Roles WHERE ServerID={serverID}", con);
            string response = $"**__Role Reactions for this server:__** \n";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            while (msgs.Read())
            {
                temp.Add((ulong)msgs.GetInt64(0), msgs.GetString(1));
            }

            if (temp.Count == 0)
            {
                temp.Add(0, "No Roles to show!");
            }
            return temp;
        }

        public static int InsertMate(ulong userID, ulong serverID, string lastTimeStamp, string timeMated)
        {
            insertData("DELETE FROM Mates WHERE ServerID = " + serverID + ";");
            //leverage existing function
            string Mate = $"INSERT INTO Mates(UserID, ServerID, LastMessageSent, TimeMated, ChosenNick) VALUES({userID}, {serverID}, '{lastTimeStamp}', '{timeMated}', 'Mi amor');";
            return insertData(Mate);
        }

        public static string getServerMate(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(UserID) FROM Mates WHERE  ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfMates = rdr.GetInt32(0);
            if (numberOfMates == 0)
            {
                return "NONE";
            }
            rdr.Close();
            commd.CommandText = $"SELECT Users.Username, Mates.ChosenNick, Mates.UserID FROM Mates join Users on Mates.UserID = Users.ID WHERE ServerID={serverID};";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            return msgs.GetString(0) + "," + msgs.GetString(1) + "," + msgs.GetInt64(2);
        }

        public static void setMateNick(ulong ServerID, string name)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Mates SET ChosenNick = @name WHERE ServerID = {ServerID};";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }
        }
        public static void setLastMateMessageTime(ulong ServerID)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd.HH:mm:ss");
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Mates SET LastMessageSent = '{now}' WHERE ServerID = {ServerID};";
                cmd.ExecuteNonQuery();
            }
        }

        public static string getTimeMated(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(UserID) FROM Mates WHERE  ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfMates = rdr.GetInt32(0);
            if (numberOfMates == 0)
            {
                return "NONE";
            }
            rdr.Close();
            commd.CommandText = $"SELECT TimeMated FROM Mates WHERE ServerID= {serverID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            return msgs.GetString(0);
        }
        //this function will check if its been more than a certain amount of time since last message from mate
        public static string TimeSinceLastMateMessage(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(UserID) FROM Mates WHERE  ServerID= {serverID}", con);
            commd.CommandText = $"SELECT LastMessageSent FROM Mates WHERE ServerID= {serverID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            DateTime stamp = DateTime.ParseExact(msgs.GetString(0), "yyyy-MM-dd.HH:mm:ss", CultureInfo.InvariantCulture);
            TimeSpan span = DateTime.Now - stamp;
            string length = "NONE"; //default no value for txt lookup
            //if too short, dont send anything
            if (span.TotalMinutes <= 20)
            { return "NONE"; }

            //if its been less than 2 hours but more than 20 minutes, do shortAbsence
            else if (span.TotalHours < 2 && span.TotalMinutes > 20)
            {
                length = "short";
            }
            //if it reaches here, and its been less than 8 hours but more than 2, pick a response to give!
            else if (span.TotalHours <= 8)
            {
                length = "medium";
            }
            else
            {
                length = "long";
            }
            string[] lines = System.IO.File.ReadAllLines($@"Commands/MateResponses/{length}Absence.txt");
            Random rand = new Random();
            int chosen = rand.Next(lines.Length);
            //return random line from array of responses.
            return lines[chosen];

        }

        public static void InsertWelcomeInfo(ulong channelID, ulong serverID, ulong roleID)
        {
            insertData($"INSERT INTO Welcome_Info(ChannelID, ServerID, RoleId, WelcomeMessage, PostMessage) " +
                $"VALUES({channelID}, {serverID}, {roleID}, 'Hi! Welcome to the server! Please click the reaction below to confirm that you are 18 or over!', 'You have confirmed!')");
        }
        public static int RemoveWelcomeInfo(ulong serverID)
        {
            int rowsAff = insertData($"DELETE FROM Welcome_Info WHERE serverID={serverID}");
            return rowsAff;
        }

        public static int setWelcomeMessage(ulong ServerID, string message)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int amount = 0;
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Welcome_Info SET WelcomeMessage = @message WHERE ServerID = {ServerID};";
                cmd.Parameters.AddWithValue("@message", message);
                amount = cmd.ExecuteNonQuery();
            }
            return amount;
        }

        public static int setConfirmationMessage(ulong ServerID, string message)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            int amount = 0;
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Welcome_Info SET PostMessage = @message WHERE ServerID = {ServerID};";
                cmd.Parameters.AddWithValue("@message", message);
                amount = cmd.ExecuteNonQuery();
            }
            return amount;
        }

        public static string[] getWelcomeInfo(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(ChannelID) FROM Welcome_Info WHERE  ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfWelcomes = rdr.GetInt32(0);
            if (numberOfWelcomes == 0)
            {
                string[] temp = { "NONE" };
                return temp;
            }
            rdr.Close();
            commd.CommandText = $"SELECT ChannelID, WelcomeMessage, PostMessage, RoleID FROM Welcome_Info WHERE ServerID={serverID};";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            string[] tempArr = { msgs.GetInt64(0).ToString(), msgs.GetString(1), msgs.GetString(2), msgs.GetInt64(3).ToString() };
            return tempArr;
        }

        public static ulong getWelcomeUser(ulong serverID, ulong userID, ulong messageID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(UserID) FROM Welcome_Users WHERE ServerID= {serverID} AND UserID = {userID} AND MessageID= {messageID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfUsers = rdr.GetInt32(0);
            if (numberOfUsers == 0)
            {
                ulong temp = 0;
                return temp;
            }
            rdr.Close();
            commd.CommandText = $"SELECT MessageID FROM Welcome_Users WHERE ServerID= {serverID} AND UserID = {userID} AND MessageID= {messageID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            ulong tempArr = (ulong)msgs.GetInt64(0);
            return tempArr;
        }

        public static void InsertWelcomeUser(ulong userID, ulong serverID, ulong msgID)
        {
            insertData($"INSERT INTO Welcome_Users(UserID, MessageID, ServerID) VALUES({userID}, {msgID}, {serverID})");
        }

        public static void RemoveWelcomeUser(ulong userID, ulong serverID)
        {
            insertData($"DELETE FROM Welcome_Users WHERE ServerID={serverID} AND UserID={userID}");
        }

        public static void AddReminder(ulong userID, ulong serverID, string title, int interval, string timeAdded)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //Adds a reminder to the table
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO Reminders(UserID, ServerID, Title, TimeInterval, TimeAdded) VALUES({userID}, {serverID}, @title, @interval, @timeAdded);";
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@interval", interval);
                cmd.Parameters.AddWithValue("@timeAdded", timeAdded);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<ReminderObject> getReminders()
        {
            using var con = new SQLiteConnection(cs);
            List<ReminderObject> temp = new List<ReminderObject>();
            con.Open();
            using var commd = new SQLiteCommand($"SELECT TimeAdded, Title, TimeInterval, UserID, ServerID FROM Reminders;", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(new ReminderObject(rdr.GetString(0), rdr.GetString(1), rdr.GetInt32(2), (ulong)rdr.GetInt64(3), (ulong)rdr.GetInt64(4)));
            }
            return temp;
        }
        public static List<ReminderObject> getReminders(ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            List<ReminderObject> temp = new List<ReminderObject>();
            con.Open();
            using var commd = new SQLiteCommand($"SELECT TimeAdded, Title, TimeInterval, UserID, ServerID FROM Reminders WHERE UserID = {userID};", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(new ReminderObject(rdr.GetString(0), rdr.GetString(1), rdr.GetInt32(2), (ulong)rdr.GetInt64(3), (ulong)rdr.GetInt64(4)));
            }
            return temp;
        }
        public static int removeReminder(string title, ulong serverID, ulong userID)
        {
            int rowsAffected = 0;
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"DELETE FROM Reminders WHERE title= @title AND ServerID = {serverID} AND UserID= {userID};";
                cmd.Parameters.AddWithValue("@title", title);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            if (rowsAffected == 1)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Succesfully removed stored reminder!");
                Console.ResetColor();
                return rowsAffected;
            }
            if (rowsAffected == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to remove stored reminder!");
                Console.ResetColor();
                return rowsAffected;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Something went wrong with removing a reminder!!");
                Console.ResetColor();
                return rowsAffected;
            }
        }

        public static void addStonk(string name, int numOfShares, int price, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO Stonks VALUES(@name, @shares, {price}, {serverID});";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@shares", numOfShares);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Stonk> getStonkObj(ulong serverID)
        {
            List<Stonk> temp = new List<Stonk>();
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Name, NumberOfShares, Price FROM Stonks WHERE ServerID = {serverID};", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(new Stonk(rdr.GetString(0), rdr.GetInt32(1), rdr.GetInt32(2), serverID));
            }
            return temp;
        }

        public static string getStonks(ulong serverID)
        {
            Dictionary<string, int> availStonks = getPurchasedStonks(serverID);
            int availableStonks = 0;
            using var con = new SQLiteConnection(cs);
            string response = "``Current Stonks:``\n";
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Name, NumberOfShares, Price FROM Stonks WHERE ServerID = {serverID};", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                if (availStonks.ContainsKey(rdr.GetString(0)))
                {
                    availableStonks = rdr.GetInt32(1) - availStonks[rdr.GetString(0)];
                }
                else
                {
                    availableStonks = rdr.GetInt32(1);
                }
                response = response + $"__{rdr.GetString(0)}__\n" + $"**Max:** {rdr.GetInt32(1)}".PadRight(15, ' ') + $"**Available for purchase:** {availableStonks}".PadRight(40, ' ') + $" **Price:** ${rdr.GetInt32(2)}\n";
            }
            return response;
        }

        public static Dictionary<string, int> getPurchasedStonks(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            Dictionary<string, int> temp = new Dictionary<string, int>();
            con.Open();
            using var commd = new SQLiteCommand($"SELECT StonkName, SUM(NumOfShares) FROM Stonk_Record WHERE ServerID = {serverID} GROUP BY StonkName;", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(rdr.GetString(0), rdr.GetInt32(1));
            }
            return temp;
        }

        public static List<string> getStonkInfo(string name)
        {
            using var con = new SQLiteConnection(cs);
            List<string> temp = new List<string>();
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Name, NumberOfShares, Price FROM Stonks WHERE Name = @name;", con);
            commd.Parameters.AddWithValue("@name", name);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                temp.Add(rdr.GetString(0));
                temp.Add((rdr.GetInt32(1)).ToString());
                temp.Add((rdr.GetInt32(2)).ToString());
            }
            return temp;
        }

        public static void editStonkShares(string name, int numOfShares)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Stonks SET NumberOfShares = @shares WHERE Name = @name ;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@shares", numOfShares);
                cmd.ExecuteNonQuery();
            }
        }

        public static void editStonkPrice(string name, int price, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"UPDATE Stonks SET Price = @price WHERE Name = @name AND ServerID = {serverID} ;";
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }
        }

        public static void addStonkPurchase(string name, int numOfShares, ulong userID, ulong serverID, string date)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
                var command = con.CreateCommand();
                command.CommandText =
                $"INSERT INTO Stonk_Record(StonkName, UserID, ServerID, NumOfShares) VALUES(@name, @userID, @serverID, @shares) ON CONFLICT(UserID, ServerID, StonkName) DO UPDATE SET NumOfShares= NumOfShares + {numOfShares};";
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@userID", userID);
                command.Parameters.AddWithValue("@serverID", serverID);
                command.Parameters.AddWithValue("@shares", numOfShares);
                command.ExecuteNonQuery();
        }

        public static List<int> getMaxShares(string name, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            List<int> temp = new List<int>();
            using var commd = new SQLiteCommand($"SELECT NumberOfShares, Price FROM Stonks WHERE Name=@name AND ServerID={serverID};", con);
            commd.Parameters.AddWithValue("@name", name);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            if (rdr.Read())
            {
                temp.Add(rdr.GetInt32(0));
                temp.Add(rdr.GetInt32(1));

            }
            else
            {
                return temp;
            }
            rdr.Close();
            int purchasedShares = 0;
            commd.CommandText = "SELECT NumOfShares FROM Stonk_Record WHERE StonkName = @name AND ServerID = @serverID";
            commd.Parameters.AddWithValue("@name", name);
            commd.Parameters.AddWithValue("@serverID", serverID);
            using SQLiteDataReader reader = commd.ExecuteReader();
            while(reader.Read())
            {
                purchasedShares = purchasedShares + reader.GetInt32(0);
            }
            temp.Add(purchasedShares);
            return temp;
        }

        public static string getOwnedStonks(ulong userID, ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            string response = "``Owned Stonks:``\n";
            con.Open();
            using var commd = new SQLiteCommand($"SELECT StonkName, NumOfShares FROM Stonk_Record WHERE UserID = {userID} AND ServerID = {serverID} GROUP BY StonkName", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                if (rdr.GetInt32(1) != 0)
                {
                    response = response + $"**{rdr.GetString(0)}** Owned: {rdr.GetInt32(1)}\n";
                }
            }
            return response;
        }
        public static bool hasEnoughStonk(ulong userID, ulong serverID, string stonkName, int amount)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT NumOfShares FROM Stonk_Record WHERE UserID = {userID} AND ServerID = {serverID} AND StonkName= @name GROUP BY StonkName", con);
            commd.Parameters.AddWithValue("@name", stonkName);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            while (rdr.Read())
            {
                if(rdr.GetInt32(0) >= amount)
                {
                    return true;
                }
            }
            return false;
        }

        public static void sellStonk(ulong userID, ulong serverID, string stonkName, int amount)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"UPDATE Stonk_Record SET NumOfShares = NumOfShares - {amount} WHERE StonkName = @name AND ServerID = {serverID} AND UserID = {userID}", con);
            commd.Parameters.AddWithValue("@name", stonkName);
            commd.ExecuteNonQuery();
        }

        public static void stonkConfigSetup(ulong serverID, ulong channelID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            //use prepared statement to make sure user provided data doesn't cause issues
            using var cmd = new SQLiteCommand(con);
            {
                cmd.CommandText = $"INSERT INTO StonkConfig(ServerID, ChannelID) VALUES({serverID}, {channelID}) ON CONFLICT(ServerID) DO UPDATE SET ChannelID={channelID};";
                cmd.ExecuteNonQuery();
            }
        }

        public static ulong getStonkChannel(ulong serverID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Count(ServerID) FROM StonkConfig WHERE ServerID= {serverID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            int numberOfChannels = rdr.GetInt32(0);
            if (numberOfChannels == 0)
            {
                return 0;
            }
            rdr.Close();
            commd.CommandText = $"SELECT ChannelID FROM StonkConfig WHERE ServerID= {serverID}";
            using SQLiteDataReader msgs = commd.ExecuteReader();
            msgs.Read();
            return (ulong)msgs.GetInt64(0);
        }

        public static string getUserFromID(ulong userID)
        {
            using var con = new SQLiteConnection(cs);
            con.Open();
            using var commd = new SQLiteCommand($"SELECT Username FROM Users WHERE ID={userID}", con);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            rdr.Read();
            string username = rdr.GetString(0);
            return username;
          
        }

        public static string getWordDefinition(string word)
        {
            string response = "Definitions:";
            int counter = 0;
            using var con = new SQLiteConnection(dictionaryCS);
            con.Open();
            using var commd = new SQLiteCommand("SELECT word, definition FROM entries WHERE word=@word", con);
            commd.Parameters.AddWithValue("@word", word);
            using SQLiteDataReader rdr = commd.ExecuteReader();
            if(!rdr.HasRows)
            {
                return "No definitions found. Is your word a plural or past tense? IE try 'Run' instead of 'running'";
            }
            while (rdr.Read() && counter < 11 && response.Length < 1800)
            {
                counter++;
                response = response + "\n**" + rdr.GetString(0) + " -** " + rdr.GetString(1).Replace("\n", "").Replace("  ", " ");
            }
            return response;
        }
    }
}
