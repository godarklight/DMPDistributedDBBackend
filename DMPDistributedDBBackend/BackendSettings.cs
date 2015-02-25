using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

namespace DMPDistributedDBBackend
{
    public class BackendSettings
    {
        public string dbHost = "localhost";
        public int dbPort = 3306;
        public string dbUsername = "USERNAME";
        public string dbPassword = "PASSWORD";
        public string dbDatabase = "DATABASE";
        public List<string> reporters = new List<string>();

        public void LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                reporters.Add("d-mp.org:9003");
                reporters.Add("godarklight.info.tm:9003");
                SaveToFile(fileName);
            }
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            dbHost = xmlDoc.DocumentElement.GetElementsByTagName("dbHost")[0].InnerText;
            dbPort = Int32.Parse(xmlDoc.DocumentElement.GetElementsByTagName("dbPort")[0].InnerText);
            dbUsername = xmlDoc.DocumentElement.GetElementsByTagName("dbUsername")[0].InnerText;
            dbPassword = xmlDoc.DocumentElement.GetElementsByTagName("dbPassword")[0].InnerText;
            dbDatabase = xmlDoc.DocumentElement.GetElementsByTagName("dbDatabase")[0].InnerText;
            reporters.Clear();
            foreach (XmlNode endpointNode in xmlDoc.DocumentElement.GetElementsByTagName("reporter"))
            {
                reporters.Add(endpointNode.InnerText);
            }
        }

        public void SaveToFile(string fileName)
        {
            string newFile = fileName + ".new";
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement settingsElement = xmlDoc.CreateElement("settings");
            xmlDoc.AppendChild(settingsElement);
            //Settings
            XmlComment dbComment = xmlDoc.CreateComment("MySQL database Settings");
            settingsElement.AppendChild(dbComment);
            XmlElement dbHostElement = xmlDoc.CreateElement("dbHost");
            dbHostElement.InnerText = dbHost;
            settingsElement.AppendChild(dbHostElement);
            XmlElement dbPortElement = xmlDoc.CreateElement("dbPort");
            dbPortElement.InnerText = dbPort.ToString();
            settingsElement.AppendChild(dbPortElement);
            XmlElement dbUsernameElement = xmlDoc.CreateElement("dbUsername");
            dbUsernameElement.InnerText = dbUsername;
            settingsElement.AppendChild(dbUsernameElement);
            XmlElement dbPasswordElement = xmlDoc.CreateElement("dbPassword");
            dbPasswordElement.InnerText = dbPassword;
            settingsElement.AppendChild(dbPasswordElement);
            XmlElement dbDatabaseElement = xmlDoc.CreateElement("dbDatabase");
            dbDatabaseElement.InnerText = dbDatabase;
            settingsElement.AppendChild(dbDatabaseElement);

            XmlComment xmlComment = xmlDoc.CreateComment("Specify reporters to connect to. You may use dns:port, ipv4:port, or [ipv6]:port format");
            settingsElement.AppendChild(xmlComment);
            foreach (string reporter in reporters)
            {
                XmlElement remoteReporterElement = xmlDoc.CreateElement("reporter");
                remoteReporterElement.InnerText = reporter;
                settingsElement.AppendChild(remoteReporterElement);
            }

            //Save
            xmlDoc.Save(newFile);
            File.Move(newFile, fileName);
        }

        public string GetConnectionString()
        {
            return "Server=" + dbHost + "; Port=" + dbPort + "; Database=" + dbDatabase + "; Uid=" + dbUsername + "; Pwd=" + dbPassword + ";";
        }
    }
}

