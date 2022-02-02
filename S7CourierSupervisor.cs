using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace S7Courier
{
    // S7Courier supervisor and side kick
    // Coding by kjurlina. Have a lot of fun

    class S7CourierSupervisor
    {
        // Global class variables
        private string ConfigFilePath;
        private string LogFilePath;
        private string DbFilePath;
        private string SeparationString;
        private string SQLiteConnectionString;
        private string SQLiteQueryString;

        private string[] ConfigFileContent;
        private string[] ConfigLineContent;
        private int ConfigFileNumberOfLines;
        private bool Verbose = false;

        int i;

        // Constructor
        public S7CourierSupervisor()
        {
            // Create configuration & log file names & paths
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                ConfigFilePath = @".\config\S7CourierConfig.txt";
                LogFilePath = @".\logs\S7CourierLog.txt";
                DbFilePath = @".\db\S7CourierDB.sqlite";
                SeparationString = " :: ";
                SQLiteConnectionString = "Data Source =" + DbFilePath + ";Version=3;";
                SQLiteQueryString = "";
            }
            else if (Environment.OSVersion.Platform.ToString().Contains("Linux"))
            {
                ConfigFilePath = @".\config\S7CourierConfig.txt";
                LogFilePath = @".\logs\S7CourierLog.txt";
                DbFilePath = @".\db\S7CourierDB.sqlite";
                SeparationString = " :: ";
                SQLiteConnectionString = "Data Source =" + DbFilePath + ";Version=3;";
                SQLiteQueryString = "";
            }
            else
            {
                ConfigFilePath = "";
                LogFilePath = "";
                DbFilePath = "";
                SeparationString = "";
                SQLiteConnectionString = "Data Source =" + DbFilePath + ";Version=3;";
                SQLiteQueryString = "";
            }
        }

        // Log file functions
        public bool CheckLogFileExistence()
        {
            // Check if config file exists
            return File.Exists(LogFilePath);
        }

        public long ChecklLogFileSize()
        {
            // Check log file size
            long LogFileSize = new FileInfo(LogFilePath).Length;
            return LogFileSize;
        }

        public void CreateLogFile()
        {
            Directory.CreateDirectory("logs");
            var fileStream = File.Create(LogFilePath);
            fileStream.Close();
            ToLogFile("Application has started with new log file");
        }

        public void ArchiveLogFile()
        {
            // Put some info into existing file (the one that will be archived)
            ToLogFile("Log file size is too big. It will be archived and new one will be created");


            // Save file and extend name with current timestamp
            string ArchiveLogFilePath;
            string LogFileTS = DateTime.Now.Year.ToString() + "_" +
                               DateTime.Now.Month.ToString() + "_" +
                               DateTime.Now.Day.ToString() + "_" +
                               DateTime.Now.Hour.ToString() + "_" +
                               DateTime.Now.Minute.ToString() + "_" +
                               DateTime.Now.Second.ToString();

            ArchiveLogFilePath = @".\logs\S7CourierLog_" + LogFileTS + ".txt";

            // Create current log file archive copy
            File.Copy(LogFilePath, ArchiveLogFilePath);
            File.Delete(LogFilePath);
        }

        // Configuration file functions
        public bool CheckConfigFileExistence()
        {
            // Check if config file exists
            return File.Exists(ConfigFilePath);
        }

        public bool ReadConfigFile()
        {
            // This method to read configuration file content
            ConfigFileContent = File.ReadAllLines(ConfigFilePath);

            if (ConfigFileContent.Length > 0)
            {
                // Get number of configuration file lines
                ConfigFileNumberOfLines = File.ReadAllLines(ConfigFilePath).Length;
                // Read verbose mode
                Verbose = GetVerboseMode();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool GetVerboseMode()
        {
            // This method to get verbose mode from config file content
            try
            {
                i = 0;
                while (i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        ToLogFile("There is nothing about verbose mode in configuration file. We will assume it's off");
                        return false;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "Verbose")
                    {
                        ToConsole("Verbose mode is on. All future messages will be mirrored to console");
                        ToLogFile("Verbose mode is on. All future messages will be mirrored to console");
                        return true;
                    }
                    i++;
                }

                return false;
            }
            catch (Exception ex)
            {
                ToLogFile("Something went wrong with determination of verbose mode. We will assume it's off");
                ToLogFile(ex.Message);
                return false;
            }
        }

        public string GetIPAddres()
        {
            // This method to get IP address from config file content
            i = 0;
            while (i <= ConfigFileNumberOfLines)
            {
                if (i >= ConfigFileNumberOfLines)
                {
                    ToLogFile("Address not found in configuration file");
                    return "";
                }

                ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                if (ConfigLineContent[0] == "IP_Address")
                {
                    ToLogFile("Configured IP Address is " + ConfigLineContent[1]);
                    break;
                }
                i++;
            }

            return (ConfigLineContent[1]);
        }

        public string GetPort()
        {
            // This method to get IP address from config file content
            i = 0;
            while (i <= ConfigFileNumberOfLines)
            {
                if (i >= ConfigFileNumberOfLines)
                {
                    ToLogFile("Port not found in configuration file");
                    return "";
                }

                ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                if (ConfigLineContent[0] == "Port")
                {
                    ToLogFile("Configured port number is " + ConfigLineContent[1]);
                    break;
                }
                i++;
            }

            return (ConfigLineContent[1]);
        }

        // SQLite functions
        public bool CheckDbExits()
        {
            // Check if SQLite database file exists         
            return File.Exists(DbFilePath);
        }

        public void CreateDatabase()
        {
            // Create database in given path
            SQLiteConnection.CreateFile(DbFilePath);

            // Output message to log file
            ToLogFile("New database has been created");
        }

        public bool CheckDatabaseTableExists(string table)
        {
            // Check if database table exists
            SQLiteQueryString = "SELECT name FROM sqlite_master WHERE type = 'table'";
            List<String> Result = ExecuteSQLiteReader(SQLiteQueryString);

            foreach (string SingleRow in Result)
            {
                if (SingleRow == table)
                {
                    return true;
                }
            }

            return false;
        }

        public void CreateDatabaseTable(string table)
        {
            // Create database table
            // Construct query string depending on case (RT, Subscribers, MessageQueue)
            switch (table)
            {
                case "S7CourierRT":
                    SQLiteQueryString = "CREATE TABLE " + table + " (FacilityID varchar(16), Timestamp varchar(24), Message varchar(96), Queued varchar(8))";
                    break;
                case "S7CourierSubscribers":
                    SQLiteQueryString = "CREATE TABLE " + table + " (SubscriberID varchar(128), FacilityID varchar(16), Address varchar(16))";
                    break;
                case "S7CourierMessageQueue":
                    SQLiteQueryString = "CREATE TABLE " + table + " (SubscriberID varchar(128), MessageID varchar(16))";
                    break;
                default:
                    SQLiteQueryString = "";
                    break;
            }
            
            ExecuteSQLiteWriter(SQLiteQueryString);
            ToLogFile("Database table " + table + " has been created");
        }

        public bool CheckMessageSanity(string msg)
        {
            // Reeived message sanity check
            // It must contain three parts separated by ::
            // If yes, then facility ID length must be >= 15, timestamp >= 14 and message >= 1
            string[] Msg = msg.Split("::");
            if (Msg.Length == 3)
            {
                return (Msg[0].Length >= 15 & Msg[1].Length >= 14 & Msg[2].Length >= 1);
            }
            else
            {
                return false;
            }
        }

        public void InsertIntoRuntimeTable(string msg)
        {
            // Method to insert new message into runtime database
            // First split imput string and extract important variables
            string[] Msg = msg.Split("::");
            SQLiteQueryString = "INSERT INTO S7CourierRT(FacilityID, Timestamp, Message, Queued) VALUES('" + Msg[0] + "','" + Msg[1] + "','" + Msg[2] + "','False')";
            ExecuteSQLiteWriter(SQLiteQueryString);
        }

        public void InsertIntoQueueTable(string subID, string msgID)
        {
            // This method puts message into queue table
            SQLiteQueryString = "INSERT INTO S7CourierMessageQueue(SubscriberID, MessageID) VALUES('" + subID + "','" + msgID + "')";
            ExecuteSQLiteWriter(SQLiteQueryString);
            // Modify runtime table flag "queued"
            SQLiteQueryString = "UPDATE S7CourierRT SET Queued = 'True' WHERE ROWID = " + msgID;
            ExecuteSQLiteWriter(SQLiteQueryString);

        }

        public List<string> GetNewMessages()
        {
            // Check runtime table for unsent messages
            // Data from reader will come as string array
            // Thus there must be some nasty text handling
            SQLiteQueryString = "SELECT rowid, * FROM S7CourierRT WHERE Queued = 'False'";
            List<string> Result = ExecuteSQLiteReader(SQLiteQueryString);         
            return Result;
        }

        public List<string> GetSubscribers()
        {
            // Check subscribers table for all current message subscribers
            SQLiteQueryString = "SELECT rowid, * FROM S7CourierSubscribers";
            List<string> Result = ExecuteSQLiteReader(SQLiteQueryString);
            return Result;
        }

        public List<string> GetMessageByRowID(string rowid)
        {
            // Get message from RT table by message ID
            SQLiteQueryString = "SELECT * FROM S7CourierRT WHERE ROWID = " + rowid;
            List<string> Result = ExecuteSQLiteReader(SQLiteQueryString);

            return Result;
        }

        private void ExecuteSQLiteWriter(string query)
        {
            // Execute SQLite database writing command
            string SQLiteQuery = query;
            using (SQLiteConnection SQLiteConnection = new SQLiteConnection(SQLiteConnectionString))
            {
                SQLiteConnection.Open();
                using (SQLiteCommand SQLiteCommand = new SQLiteCommand(SQLiteQuery, SQLiteConnection))
                {
                    SQLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private List<string> ExecuteSQLiteReader(string query)
        {
            // Execute SQLite database reading command
            string SQLiteQuery = query;
            string SingleRow = "";
            List<String> Result = new List<String>();

            using (SQLiteConnection SQLiteConnection = new SQLiteConnection(SQLiteConnectionString))
            {
                SQLiteConnection.Open();
                using (SQLiteCommand SQLiteCommand = new SQLiteCommand(SQLiteQuery, SQLiteConnection))
                {
                    using (SQLiteDataReader SQLiteDataReader = SQLiteCommand.ExecuteReader())
                    {
                        while (SQLiteDataReader.Read())
                        {
                            for (i = 0; i <= SQLiteDataReader.FieldCount - 1; i++)
                            {
                                SingleRow += SQLiteDataReader.GetValue(i).ToString();
                                
                                // If this is not last column add data separator
                                if (i < SQLiteDataReader.FieldCount - 1)
                                {
                                    SingleRow += SeparationString;
                                }
                            }
                            Result.Add(SingleRow);
                            SingleRow = "";
                        }
                    }
                }
            }

            return Result;
        }

        // Logging functions
        public void ToConsole(string message)
        {
            // Output message to console
            string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
            Console.WriteLine(MessageTS + " :: " + message);
        }

        public void ToLogFile(string message)
        {
            // Output message to log file
            string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
            using (StreamWriter sw = File.AppendText(LogFilePath))
            {
                sw.WriteLine(MessageTS + " :: " + message);
                if (Verbose)
                {
                    Console.WriteLine(MessageTS + " :: " + message);
                }
            }
        }

    }
}
