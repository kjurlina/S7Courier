using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace S7Courier
{
    // S7Courier backend appliance
    // Coding by kjurlina. Have a lot of fun

    class S7CourierBackend
    {
        // Global variables
        S7CourierSupervisor Supervisor = null;
        TcpListener Listener = null;
        IPAddress IPAddress;
        int Port;

        // Constructor
        public S7CourierBackend(S7CourierSupervisor supervisor)
        {
            // Assign supervisor to local instance
            Supervisor = supervisor;

            // Find IP address and corresponding port
            IPAddress = IPAddress.Parse(Supervisor.GetIPAddres());
            Port = Int32.Parse(Supervisor.GetPort());
        }

        // Handle connection
        public void Listen()
        {
            try
            {
                // Construct and start server on configured port
                Listener = new TcpListener(IPAddress, Port);
                Listener.Start();

                // Say something to log file
                Supervisor.ToLogFile("Entering listening loop...");

                // Enter the listening loop
                while (true)
                {
                    // Wait for incoming connection
                    TcpClient Client = Listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(RecordMessages, Client);
                }
            }
            catch (SocketException e)
            {
                Supervisor.ToLogFile("SocketException: {0}" + e);
                Supervisor.ToLogFile("Connection (socket) error. It's probably taken. Exiting application");
            }
            finally
            {
                // Stop listening for new clients.
                Listener.Stop();
            }
        }

        // Record received messages into runtime database
        private void RecordMessages(object obj)
        {
            // General purpose variables
            var Client = (TcpClient)obj;
            Byte[] Bytes = new Byte[1024];
            string[] Msg = new string[8];
            bool WD = false;
            int i, j, k;

            // Log incoming connection
            Supervisor.ToLogFile("Connection from client " + ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString() + " accepted...");

            // Get a stream object for reading and writing
            NetworkStream Stream = Client.GetStream();
            Array.Clear(Bytes, 0, Bytes.Length);

            // Loop to receive all the data sent by the client
            try
            {
                while ((i = Stream.Read(Bytes, 0, Bytes.Length)) != 0)
                {

                    // Reset watchdog variable and other variables
                    WD = false;
                    j = k = 0;

                    // Receive and interpret data (compose sane messages for database storage)
                    char[] chars = Encoding.ASCII.GetChars(Bytes);

                    for (j = 0; j <= 7; j++)
                    {
                        // 128 is default message length (sent from PLC)
                        k = j * 128;
                        int index = chars[k + 1];
                        // By S7 spec, first two characters are maximum and acutual string length
                        // Here, they can be neglected because we have fixed buffer length (1024)
                        Msg[j] = new string(chars, k, index + 2);
                        Msg[j] = Msg[j].Remove(0, 2);
                        WD = WD | Supervisor.CheckMessageSanity(Msg[j]);
                        // If received string is sane, put it to database
                        if (Supervisor.CheckMessageSanity(Msg[j]))
                        {
                            Supervisor.InsertIntoRuntimeTable(Msg[j]);
                            Supervisor.ToLogFile("Message number " + j.ToString() + " from buffer written to database");
                        }
                        else if (Msg[j].Length > 0)
                        {
                            Supervisor.ToLogFile("Received message \"" + Msg[j] + "\": is not properly formatted. Writing to database skipped");
                        }
                    }

                    Array.Clear(Bytes, 0, Bytes.Length);
                    Array.Clear(chars, 0, chars.Length);

                    // Prepare and send feedback
                    string sfbk = "RCV_" + DateTime.Now.ToString();
                    char[] cfbk = sfbk.ToCharArray();
                    byte[] fbk = Encoding.ASCII.GetBytes(cfbk);
                    Array.Resize(ref fbk, 24);
                    Stream.Write(fbk, 0, fbk.Length);
                }

                Supervisor.ToLogFile("Client " + ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString() + " disconnected");
            }
            catch (Exception ex)
            {
                if (WD == true)
                {
                    // Report that client is disconnected
                    Supervisor.ToLogFile("Client " + ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString() + " disconnected");
                }
                else
                {
                    // Report that something wnet wrong
                    // Message "was aborted by the software in your host machine" is not being logged
                    // This exception is normal disconnection routine from remote S7 OUC client
                    if (!ex.Message.Contains("was aborted by the software in your host machine"))
                    {
                        Supervisor.ToLogFile(ex.Message);
                    }
                    Supervisor.ToLogFile("Something went wrong. There are no sane messages in received buffer...");
                    WD = false;
                }
            }
        }
    }

}
