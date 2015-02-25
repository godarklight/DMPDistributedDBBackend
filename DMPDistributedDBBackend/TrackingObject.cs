using System;

namespace DMPDistributedDBBackend
{
    public class TrackingObject
    {
        public int referenceCount = 0;
        public string[] players = new string[0];
    }
}

