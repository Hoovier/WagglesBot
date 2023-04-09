using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWaggles
{
    public class ReminderObject
    {
        public string timeAdded;
        public string title;
        public int timeInterval;
        public ulong userID;
        public ulong serverID;
        public ReminderObject(string _timeAdded, string _title, int _timeInterval, ulong _userID, ulong _serverID)
        {
            timeAdded = _timeAdded;
            title = _title;
            timeInterval = _timeInterval;
            userID = _userID;
            serverID = _serverID;
        }
    }
}
