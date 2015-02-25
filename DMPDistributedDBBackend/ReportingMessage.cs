using System;
using System.Collections.Generic;

namespace DMPDistributedDBBackend
{
    public class ReportingMessage
    {
        public string serverHash;
        public string serverName;
        public string description;
        public int gamePort;
        public string gameAddress;
        public int protocolVersion;
        public string programVersion;
        public int maxPlayers;
        public int modControl;
        public string modControlSha;
        public int gameMode;
        public bool cheats;
        public int warpMode;
        public long universeSize;
        public string banner;
        public string homepage;
        public int httpPort;
        public string admin;
        public string team;
        public string location;
        public bool fixedIP;
        public string[] players;

        public static ReportingMessage FromBytesBE(byte[] inputBytes)
        {
            ReportingMessage returnMessage = new ReportingMessage();
            using (MessageStream2.MessageReader mr = new MessageStream2.MessageReader(inputBytes))
            {
                returnMessage.serverHash = mr.Read<string>();
                returnMessage.serverName = mr.Read<string>();
                returnMessage.description = mr.Read<string>();
                returnMessage.gamePort = mr.Read<int>();
                returnMessage.gameAddress = mr.Read<string>();
                returnMessage.protocolVersion = mr.Read<int>();
                returnMessage.programVersion = mr.Read<string>();
                returnMessage.maxPlayers = mr.Read<int>();
                returnMessage.modControl = mr.Read<int>();
                returnMessage.modControlSha = mr.Read<string>();
                returnMessage.gameMode = mr.Read<int>();
                returnMessage.cheats = mr.Read<bool>();
                returnMessage.warpMode = mr.Read<int>();
                returnMessage.universeSize = mr.Read<long>();
                returnMessage.banner = mr.Read<string>();
                returnMessage.homepage = mr.Read<string>();
                returnMessage.httpPort = mr.Read<int>();
                returnMessage.admin = mr.Read<string>();
                returnMessage.team = mr.Read<string>();
                returnMessage.location = mr.Read<string>();
                returnMessage.fixedIP = mr.Read<bool>();
                returnMessage.players = mr.Read<string[]>();
            }
            return returnMessage;
        }

        public Dictionary<string, object> GetParameters()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["@serverhash"] = serverHash;
            parameters["@namex"] = serverName;
            if (serverName.Length > 255)
            {
                serverName = serverName.Substring(0, 255);
            }
            parameters["@descriptionx"] = description;
            parameters["@gameportx"] = gamePort;
            parameters["@gameaddressx"] = gameAddress;
            parameters["@protocolx"] = protocolVersion;
            parameters["@programversion"] = programVersion;
            parameters["@maxplayersx"] = maxPlayers;
            parameters["@modcontrolx"] = modControl;
            parameters["@modcontrolshax"] = modControlSha;
            parameters["@gamemodex"] = gameMode;
            parameters["@cheatsx"] = cheats;
            parameters["@warpmodex"] = warpMode;
            parameters["@universex"] = universeSize;
            parameters["@bannerx"] = banner;
            parameters["@homepagex"] = homepage;
            parameters["@httpportx"] = httpPort;
            parameters["@adminx"] = admin;
            parameters["@teamx"] = team;
            parameters["@locationx"] = location;
            parameters["@fixedipx"] = fixedIP;
            return parameters;
        }
    }
}

