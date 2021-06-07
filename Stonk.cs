using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWaggles
{
    public class Stonk
    {
        public string Name;
        public int NumberOfShares;
        public int Price;
        public ulong ServerID;
        public Stonk(string name, int numberOfShares, int price, ulong serverID)
        {
            Name = name;
            NumberOfShares = numberOfShares;
            Price = price;
            ServerID = serverID;
        }
    }
}
