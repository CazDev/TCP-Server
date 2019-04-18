using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace TCP_Server
{
    class Program
    {
        public static bool CheckingReceive = true;
        public static bool CheckingSend = true;

        public static Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static Socket handler;

        public static bool CheckingListening = true;

        static void Main()
        {

            StartTCPListener();

        }

        public static void StartTCPListener()
        {
            string data = "";

            byte[] bytes = new Byte[1024];

            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 8390);

            Console.WriteLine( "Started listening on: " + ipAddress.ToString());

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (CheckingListening)
                {

                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.
                    handler = listener.Accept();
                    data = null;

                    Thread t = new Thread(new ThreadStart(StartTCPSender));
                    t.Start();

                    CheckingReceive = true;
                    Console.WriteLine("Client connection accepted.");

                    // An incoming connection needs to be processed.
                    while (CheckingReceive)
                    {
                        try
                        {

                            bytes = new byte[1024];
                            int bytesRec = handler.Receive(bytes);
                            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                            Console.WriteLine("");
                            Console.WriteLine($"FromClient@{handler.LocalEndPoint}: " + data);

                            if (data.IndexOf("#") > -1)
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Error recieving data.");
                            }

                            data = "";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            CheckingReceive = false;
                        }
                    }

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes("Client connection closed.");

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            CheckingSend = false;

            Console.WriteLine("Press ENTER to exit.");
            Console.Read();
            Environment.Exit(0);

        }

        static void StartTCPSender()
        {
            while (CheckingSend)
            {
                try
                {
                    Console.WriteLine("");
                    Console.Write($"ToClient@{handler.LocalEndPoint}: ");
                    string SendMsg = Console.ReadLine();

                    byte[] byData = System.Text.Encoding.ASCII.GetBytes(SendMsg);
                    handler.Send(byData);

                    if (SendMsg.Contains("/sendfile"))
                    {
                        string string1 = String.Format("Sending...", Environment.NewLine);
                        byte[] preBuf = Encoding.ASCII.GetBytes(string1);
                        
                        string string2 = String.Format("", Environment.NewLine);
                        byte[] postBuf = Encoding.ASCII.GetBytes(string2);

                        string dir = SendMsg.Replace("/sendfile ", "");

                        Console.WriteLine("Sending {0} with buffers to the host.{1}", dir, Environment.NewLine);
                        handler.SendFile(dir, preBuf, postBuf , TransmitFileOptions.UseSystemThread);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    CheckingSend = false;
                }
            }
        }

    }
}