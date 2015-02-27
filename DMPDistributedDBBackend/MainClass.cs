using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DMPDistributedDBBackend
{
    public class MainClass
    {
        //Current clients for command printing
        private static DatabaseClient databaseClient;
        private static DatabaseDriver databaseDriver;
        private static Thread mainThread;
        private static string currentEndpoint;

        public static void Main()
        {
            MainClass mainClass = new MainClass();
            mainThread = new Thread(new ThreadStart(mainClass.Run));
            mainThread.Start();
            InteractiveConsole();
        }

        /* TODO: Divert all Console.WriteLine calls.
        private static void DetectConsole()
        {
            bool consoleOK = true;
            try
            {
                Console.WriteLine();
            }
            catch
            {
                consoleOK = false;
            }
            if (consoleOK)
            {
                InteractiveConsole();
            }
            else
            {
                NoConsole();
            }
        }
        */

        private static void InteractiveConsole()
        {
            bool running = true;
            while (running)
            {
                string line = Console.ReadLine().ToLower();
                bool handled = false;
                if (line == "q")
                {
                    handled = true;
                    running = false;
                    mainThread.Abort();
                }
                if (line == "p")
                {
                    handled = true;
                    DatabaseDriver thisDriver = databaseDriver;
                    if (thisDriver != null)
                    {
                        thisDriver.PrintServers();
                    }
                }
                if (line == "r")
                {
                    handled = true;
                    DatabaseClient thisClient = databaseClient;
                    if (thisClient != null)
                    {
                        thisClient.Disconnect();
                    }
                }
                if (line == "d")
                {
                    handled = true;
                    string displayEndpoint = currentEndpoint;
                    if (displayEndpoint != null)
                    {
                        Console.WriteLine("Currently connected to " + displayEndpoint);
                    }
                }
                if (!handled)
                {
                    Console.WriteLine("Commands: q for quit, p for print server/relay tree, r for reconnect to the network, d to display the current remote endpoint");
                }
            }
        }

        private static void NoConsole()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        public void Run()
        {
            BackendSettings backendSettings = new BackendSettings();
            backendSettings.LoadFromFile("settings.xml");
            while (true)
            {
                TcpClient currentConnection = null;
                foreach (string connectionString in backendSettings.reporters)
                {
                    Console.WriteLine("Selecting reporter " + connectionString);
                    foreach (IPEndPoint endpoint in FindEndpoint(connectionString))
                    {
                        Console.WriteLine("Connecting to " + endpoint);
                        try
                        {
                            TcpClient newClient = new TcpClient(endpoint.AddressFamily);
                            IAsyncResult ar = newClient.BeginConnect(endpoint.Address, endpoint.Port, null, null);
                            if (ar.AsyncWaitHandle.WaitOne(5000))
                            {
                                if (newClient.Connected)
                                {
                                    newClient.EndConnect(ar);
                                    currentConnection = newClient;
                                    currentEndpoint = connectionString;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Connection failed to " + endpoint.Address + " port " + endpoint.Port);
                                    newClient.Close();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Connection failed to " + endpoint.Address + " port " + endpoint.Port);
                                newClient.Close();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Connection failed to " + endpoint.Address + " port " + endpoint.Port);
                        }
                    }
                    if (currentConnection != null)
                    {
                        break;
                    }
                }
                if (currentConnection == null)
                {
                    Console.WriteLine("Failed to connect to all endpoints. Waiting 60 seconds.");
                }
                else
                {
                    Console.WriteLine("Connected!");
                    DatabaseConnection databaseConnection = new DatabaseConnection(backendSettings);
                    databaseDriver = new DatabaseDriver(databaseConnection);
                    databaseClient = new DatabaseClient(currentConnection, databaseDriver);
                    databaseClient.Run();
                    Console.WriteLine("Disconnected! Reconnecting in 60 seconds...");
                    databaseClient = null;
                    databaseDriver = null;
                    currentEndpoint = null;
                }
                Thread.Sleep(60000);
            }
        }

        private IPEndPoint[] FindEndpoint(string connectionString)
        {
            if (!connectionString.Contains(":") || connectionString.EndsWith("]"))
            {
                connectionString += ":9003";
            }
            string ipPart = connectionString.Substring(0, connectionString.LastIndexOf(":"));
            string portPart = connectionString.Substring(connectionString.LastIndexOf(":") + 1);
            int portInt = 9003;
            if (!Int32.TryParse(portPart, out portInt))
            {
                return new IPEndPoint[0];
            }
            //IPv6 literal
            if (ipPart.StartsWith("[") && ipPart.EndsWith("]"))
            {
                ipPart = ipPart.Substring(1, ipPart.Length - 2);
            }
            IPAddress ipAddr= null;
            if (IPAddress.TryParse(ipPart, out ipAddr))
            {
                IPEndPoint[] retVal = new IPEndPoint[1];
                retVal[0] = new IPEndPoint(ipAddr, portInt);
                return retVal;
            }
            try
            {
                IAsyncResult ar = Dns.BeginGetHostEntry(ipPart, null, null);
                if (ar.AsyncWaitHandle.WaitOne(5000))
                {
                    IPHostEntry hostEntry = Dns.EndGetHostEntry(ar);
                    IPEndPoint[] retVal = new IPEndPoint[hostEntry.AddressList.Length];
                    for (int i = 0; i < hostEntry.AddressList.Length; i++)
                    {
                        retVal[i] = new IPEndPoint(hostEntry.AddressList[i], portInt);
                    }
                    return retVal;
                }
            }
            catch
            {
                //Don't care
            }
            return new IPEndPoint[0];
        }
    }
}

