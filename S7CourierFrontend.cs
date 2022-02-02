using System;
using System.Collections.Generic;
using System.Text;

namespace S7Courier
{
    // S7Courier frontend appliance
    // Coding by kjurlina. Have a lot of fun

    class S7CourierFrontend
    {
        // Global variables
        S7CourierSupervisor Supervisor = null;
        List<string> NewMessages = null;
        List<string> Subscribers = null;
        int SendInterval = 10;
        bool CheckedFlag;

        // Constructor
        public S7CourierFrontend(S7CourierSupervisor supervisor)
        {
            // Assign supervisor to local instance
            Supervisor = supervisor;

            // Initialize flags
            CheckedFlag = false;
        }

        // Check new messages and send to subscribers
        public void Send()
        {
            try
            {
                while (true)
                {
                    // Get new messages IDs from runtime table
                    if (DateTime.Now.Second % SendInterval == 0 & !CheckedFlag)
                    {
                        // First get new (unqueued) messages
                        NewMessages = Supervisor.GetNewMessages();
                        if (NewMessages.Count > 0)
                        {
                            // Read table of subscribers
                            Subscribers = Supervisor.GetSubscribers();

                            // Now loop trough all new messages and find subscribers
                            foreach (string m in NewMessages)
                            {
                                string[] Message = m.Split(" :: ");
                                foreach (string s in Subscribers)
                                {
                                    string[] Subscriber = s.Split(" :: ");
                                    // When subscriber is found copy message to queue and rise runtime flag
                                    if (Message[1] == Subscriber[2])
                                    {
                                        Supervisor.InsertIntoQueueTable(Subscriber[1], Message[0]);
                                        Supervisor.ToLogFile("Message ID=" + Message[0] + " for subscriber ID=" + Subscriber[1] + " has been queued"); 
                                    }
                                }
                            }
                        }

                        // Set periodic flat
                        CheckedFlag = true;
                    }
                    else if (DateTime.Now.Second % SendInterval != 0)
                    {
                        CheckedFlag = false;
                    }

                    // If there are new messages
                }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Send method internal error");
                return;
            }
        }
    }
}
