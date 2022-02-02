using System;
using System.Threading;
using System.Threading.Tasks;

namespace S7Courier
{
    // S7 message concentrator and courier
    // Coding by kjurlina
    // Have a lot of fun

    class S7Courier
    {
        // Main routine
        static void Main(string[] args)
        {
            // Global project variables
            S7CourierSupervisor Supervisor = null;
            S7CourierBackend Backend = null;
            S7CourierFrontend Frontend = null;

            // Create local supervisor instance and event handler for application closing event
            try
            {
                Supervisor = new S7CourierSupervisor();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, e) => CurrentDomain_ProcessExit(sender, e, Supervisor));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            // First check if log file exists. If not, create one
            try
            {
                if (!Supervisor.CheckLogFileExistence())
                {
                    // Create log file
                    Supervisor.CreateLogFile();
                }
                else if (Supervisor.ChecklLogFileSize() > 1048576)
                {
                    // If log file is too big archive it and create new one
                    Supervisor.ArchiveLogFile();
                    Supervisor.CreateLogFile();
                }
                else
                {
                    Supervisor.ToLogFile("Application started with existing log file");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Something went wrong with log file. Exiting application");
                return;
            }

            // Check if configuration file exists. If not, exit application
            try
            {
                if (!Supervisor.CheckConfigFileExistence())
                {
                    Supervisor.ToLogFile("Configuration file does not exist. Exiting application");
                    return;
                }
                else
                {
                    Supervisor.ReadConfigFile();
                    Supervisor.ToLogFile("Configuration file loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with configuration file. Exiting application");
                return;
            }

            // Check if database file exists. If not, create one
            try
            {
                if (!Supervisor.CheckDbExits())
                {
                    Supervisor.CreateDatabase();
                }
                else
                {
                    Supervisor.ToLogFile("Database checked and OK");
                }

            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with database file. Exiting application");
                return;
            }

            // If database exists, check if there are tables. If some of them are missing, create it
            try
            {
                if (!Supervisor.CheckDatabaseTableExists("S7CourierRT"))
                {
                    Supervisor.CreateDatabaseTable("S7CourierRT");
                }
                if (!Supervisor.CheckDatabaseTableExists("S7CourierSubscribers"))
                {
                    Supervisor.CreateDatabaseTable("S7CourierSubscribers");
                }
                if (!Supervisor.CheckDatabaseTableExists("S7CourierMessageQueue"))
                {
                    Supervisor.CreateDatabaseTable("S7CourierMessageQueue");
                }

            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with database runtime table. Exiting application");
                return;
            }

            // Create backend instance
            try
            {
                Backend = new S7CourierBackend(Supervisor);
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile("Could not create backend intance. Exiting application " + ex.Message);
            }

            // Create frontend instance
            try
            {
                Frontend = new S7CourierFrontend(Supervisor);
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile("Could not create frontend instance. Exiting application " + ex.Message);
            }

            // Multithreading for backend/frontend appliances
            try
            {
                var BackendTask = Task.Factory.StartNew(() => Backend.Listen());                
                var FrontendTask = Task.Factory.StartNew(() => Frontend.Send());

                Task.WaitAny(BackendTask, FrontendTask);

                if (BackendTask.IsCompleted & !FrontendTask.IsCompleted)
                {
                    Supervisor.ToLogFile("Backend application shut down unexpectedly");
                }
                else if (!BackendTask.IsCompleted & FrontendTask.IsCompleted)
                {
                    Supervisor.ToLogFile("Frontend application shut down unexpectedly");
                }
                else
                {
                    Supervisor.ToLogFile("Both backend and frontend application shut down unexpectedly");
                }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile("Error in frontend/backend multithreading " + ex.Message);
            }
        }

        // Process exit routine
        static void CurrentDomain_ProcessExit(object sender, EventArgs e, S7CourierSupervisor supervisor)
        {
            supervisor.ToLogFile("Application closed");
            supervisor.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
