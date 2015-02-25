using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace DMPDistributedDBBackend
{
    public class DatabaseDriver
    {
        private DatabaseConnection databaseConnection;
        private Dictionary<ReferenceID, string> pairMatch = new Dictionary<ReferenceID, string>();
        private Dictionary<string, TrackingObject> trackingObjects = new Dictionary<string, TrackingObject>();

        public DatabaseDriver(DatabaseConnection databaseConnection)
        {
            this.databaseConnection = databaseConnection;
            SQLCleanupDatabase();
        }

        public void HandleConnect(string serverID, int clientID, string remoteAddress, int remotePort)
        {
            //Don't care
        }

        public void HandleReport(string serverID, int clientID, ReportingMessage reportMessage)
        {
            ReferenceID thisReference = new ReferenceID(serverID, clientID);
            if (!pairMatch.ContainsKey(thisReference))
            {
                pairMatch.Add(thisReference, reportMessage.serverHash);
                if (!trackingObjects.ContainsKey(reportMessage.serverHash))
                {
                    trackingObjects.Add(reportMessage.serverHash, new TrackingObject());
                    SQLConnect(reportMessage, trackingObjects[reportMessage.serverHash]);
                }
                TrackingObject trackingObject = trackingObjects[reportMessage.serverHash];
                trackingObject.referenceCount++;
                Console.WriteLine(reportMessage.serverHash + " references: " + trackingObject.referenceCount);
            }
            SQLReport(reportMessage, trackingObjects[reportMessage.serverHash]);
        }

        public void HandleDisconnect(string serverID, int clientID)
        {
            ReferenceID thisReference = new ReferenceID(serverID, clientID);
            if (pairMatch.ContainsKey(thisReference))
            {
                string serverHash = pairMatch[thisReference];
                TrackingObject trackingObject = trackingObjects[serverHash];
                trackingObject.referenceCount--;
                Console.WriteLine(serverHash + " references: " + trackingObjects[serverHash]);
                if (trackingObject.referenceCount == 0)
                {
                    SQLDisconnect(serverHash, trackingObject);
                    trackingObjects.Remove(serverHash);
                }
                pairMatch.Remove(thisReference);
            }
        }

        private void SQLConnect(ReportingMessage reportMessage, TrackingObject trackingObject)
        {
            string initSqlQuery = "CALL gameserverinit(@serverhash, @namex, @descriptionx, @gameportx, @gameaddressx, @protocolx, @programversion, @maxplayersx, @modcontrolx, @modcontrolshax, @gamemodex, @cheatsx, @warpmodex, @universex, @bannerx, @homepagex, @httpportx, @adminx, @teamx, @locationx, @fixedipx);";
            Dictionary<string, object> parameters = reportMessage.GetParameters();
            try
            {
                databaseConnection.ExecuteNonReader(initSqlQuery, parameters);
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: Ignoring error on connection (add server), error: " + e.Message);
            }
            string playerSqlQuery = "CALL gameserverplayer(@hash, @player, '1')";
            foreach (string connectedPlayer in reportMessage.players)
            {
                Console.WriteLine("Player " + connectedPlayer + " joined " + reportMessage.serverHash);
                Dictionary<string, object> playerParams = new Dictionary<string, object>();
                playerParams["@hash"] = reportMessage.serverHash;
                playerParams["@player"] = connectedPlayer;
                try
                {
                    databaseConnection.ExecuteNonReader(playerSqlQuery, playerParams);
                }
                catch (Exception e)
                {
                    Console.WriteLine("WARNING: Ignoring error on connection (add player), error: " + e.Message);
                }
                trackingObject.players = reportMessage.players;
            }

        }

        private void SQLReport(ReportingMessage reportMessage, TrackingObject trackingObject)
        {
            //Take all the currently connected players and remove the players that were connected already to generate a list of players to be added
            List<string> addList = new List<string>(reportMessage.players);
            foreach (string player in trackingObject.players)
            {
                if (addList.Contains(player))
                {
                    addList.Remove(player);
                }
            }
            //Take all the old players connected and remove the players that are connected already to generate a list of players to be removed
            List<string> removeList = new List<string>(trackingObject.players);
            foreach (string player in reportMessage.players)
            {
                if (removeList.Contains(player))
                {
                    removeList.Remove(player);
                }
            }
            //Add new players
            foreach (string player in addList)
            {
                Console.WriteLine("Player " + player + " joined " + reportMessage.serverHash);
                Dictionary<string, object> playerParams = new Dictionary<string, object>();
                playerParams["hash"] = reportMessage.serverHash;
                playerParams["player"] = player;
                string sqlQuery = "CALL gameserverplayer(@hash ,@player, '1')";
                try
                {
                    databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                }
                catch (Exception e)
                {
                    Console.WriteLine("WARNING: Ignoring error on report (add player), error: " + e.Message);
                }
            }
            //Remove old players
            foreach (string player in removeList)
            {
                Console.WriteLine("Player " + player + " left " + reportMessage.serverHash);
                Dictionary<string, object> playerParams = new Dictionary<string, object>();
                playerParams["hash"] = reportMessage.serverHash;
                playerParams["player"] = player;
                string sqlQuery = "CALL gameserverplayer(@hash ,@player, '0')";
                try
                {
                    databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                }
                catch (Exception e)
                {
                    Console.WriteLine("WARNING: Ignoring error on report (remove player), error: " + e.Message);
                }
            }
        }

        private void SQLDisconnect(string serverHash, TrackingObject trackingObject)
        {
            //Remove old players
            foreach (string player in trackingObject.players)
            {
                Dictionary<string, object> playerParams = new Dictionary<string, object>();
                playerParams["hash"] = serverHash;
                playerParams["player"] = player;
                string sqlQuery = "CALL gameserverplayer(@hash ,@player, '0')";
                try
                {
                    databaseConnection.ExecuteNonReader(sqlQuery, playerParams);
                }
                catch (Exception e)
                {
                    Console.WriteLine("WARNING: Ignoring error on disconnect (remove player), error: " + e.Message);
                }
            }
            Dictionary<string, object> offlineParams = new Dictionary<string, object>();
            offlineParams["@hash"] = serverHash;
            string mySql = "CALL gameserveroffline(@hash)";
            try
            {
                databaseConnection.ExecuteNonReader(mySql, offlineParams);
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: Ignoring error on disconnect (remove server), error: " + e.Message);
            }
        }

        public void SQLCleanupDatabase()
        {
            try
            {
                databaseConnection.ExecuteNonReader("CALL gameserverscleanup()");
            }
            catch (Exception e)
            {
                Console.WriteLine("WARNING: Ignoring error on cleanup, error: " + e.Message);
            }
        }

        private struct ReferenceID
        {
            public readonly string serverID;
            public readonly int clientID;

            public ReferenceID(string serverID, int clientID)
            {
                this.serverID = serverID;
                this.clientID = clientID;
            }

            public override bool Equals(object obj)
            {
                if (obj is ReferenceID)
                {
                    ReferenceID rhs = (ReferenceID)obj;
                    return (serverID == rhs.serverID && clientID == rhs.clientID);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return (serverID + clientID).GetHashCode();
            }
        }
    }
}

