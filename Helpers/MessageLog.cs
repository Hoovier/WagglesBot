using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWaggles
{
    class MessageLog
    {
        const ulong MESSAGE_DELETED = 0;
        public ulong[] ids;
        private uint pos;

        public MessageLog()
        {
            ids = new ulong[3];
            pos = 0;
        }
        public void addElement(ulong newID)
        {
            switch(pos)
            {
                //if pos is 0 or 1, allow it to go to pos+1 in array
                case 0:
                case 1:
                    pos++;
                    ids[pos] = newID; //add the element to array
                    return;
                //if pos is 2, instead of incrementing, set to 0
                case 2:
                    pos = 0;
                    ids[pos] = newID;
                    return;
            }
        }
        public ulong getLastElement()
        {
            //save element from current pos, which makes it the newest
            ulong lastElem = ids[pos];
            //determine where to move pos to.
            switch (pos)
            {
                case 0:
                case 1:
                    ids[pos] = MESSAGE_DELETED;//set current pos to MESSAGE_DELETED
                    pos++;//move to next spot
                    return lastElem;
                case 2:
                default:
                    ids[pos] = MESSAGE_DELETED;
                    pos = 0;
                    return lastElem;
            }
        }

    }
}
