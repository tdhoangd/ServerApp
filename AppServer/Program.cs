using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;


/**
 * Server Assumption: the server wont run too long, so numbers can be stored in a 
 * dictionary, and searching faster than read from file. 
 */


namespace AppServer
{
    class Program
    {
        static  object myLock = new object();
        static Dictionary<int, Socket> list_clients = new Dictionary<int, Socket>();
        static Dictionary<int, int> list_nums = new Dictionary<int, int>();
        static List<int> list_new_uniques = new List<int>();
        static int DUP_COUNT = 0;

        static int count = 1;
        static bool terminateFlag = false;
        static int MAX_CONNECT = 5;
        static readonly string FILE_PATH = "numbers.log";

        public static void StartServer() 
        {
            // establish local endpoint and create TCP/IP socket
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());  
            IPAddress ipAddress = ipHostInfo.AddressList[0];  
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 4000);  
            Socket server = new Socket(ipAddress.AddressFamily,  
                SocketType.Stream, ProtocolType.Tcp ); 

            //create or empty log file
            File.WriteAllText(FILE_PATH, String.Empty);

            // Bind the socket to the local endpoint and
            // listen for incoming connections
            try {
                server.Bind(localEndPoint);
                server.Listen(10);
                Console.WriteLine("Waiting for connection...");

                // Create new thread to handle report each 10s
                Thread reportThread = new Thread(PrintReport);
                reportThread.Start();

                int num;
                while (true)
                {
                    Socket client = server.Accept();
                    num = list_clients.Count;
                    bool flag;
                    lock (myLock) flag = terminateFlag;

                    if (num >= MAX_CONNECT || (flag == true && num > 0)) {
                        // reject new connnection if number of concurrent connection reach max
                        // or server still in process of terminating clients 
                        byte[] msg = Encoding.ASCII.GetBytes("busy");
                        client.Send(msg);
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    } else {
                        byte[] msg = Encoding.ASCII.GetBytes("available");
                        client.Send(msg);

                        // start new thread to handle communication between server and new client
                        lock (myLock) list_clients.Add(count, client);
                        lock (myLock) terminateFlag = false;
                        Thread thread = new Thread(HandleClient);
                        thread.Start(count);
                        count++;
                    }
                }

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }


        }


        public static void PrintReport()
        {
            while (true)
            {
                Thread.Sleep(10000); // wait 10 second

                lock (myLock)
                {
                    // print report
                    try {
                        long sum = 0;
                        foreach (int numVal in list_new_uniques){
                            sum += numVal;
                        }
                        Console.WriteLine("Received {0} unique numbers, {1} duplicates. Unique total: {2}",
                                     list_new_uniques.Count, DUP_COUNT, sum);

                    } catch (Exception e) {
                        Console.WriteLine($"Summation Exception: {e}");
                    }

                    // reset count
                    list_new_uniques.Clear();
                    DUP_COUNT = 0;
                }
            }
        }

        public static void HandleClient(object count)
        {
            int id = (int) count;
            Socket client; 

            lock (myLock) client = list_clients[id];

            // waiting for incoming message
            byte[] bytes = new byte[1024];
            int bytesRec = client.Receive(bytes);

            if (bytesRec != 0) {
                string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                data = data.TrimEnd(Environment.NewLine.ToCharArray());

                if (Object.Equals(data, "terminate")) {
                    lock (myLock) terminateFlag = true;
                } else if (ValidNumber(data)) 
                {
                    int numVal = Int32.Parse(data);

                    // update log file
                    lock (myLock)
                    {
                        if (terminateFlag != true) {
                            // update log file, 
                            if (!list_nums.ContainsKey(numVal)) {
                                File.AppendAllText(FILE_PATH, data + Environment.NewLine);
                                list_nums.Add(numVal, 1);
                                list_new_uniques.Add(numVal);
                            } else {
                                DUP_COUNT++;
                            }

                        }
                    }
                }
                 
            }

            lock (myLock) list_clients.Remove(id);
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }


        public static bool ValidNumber(string data) {
            bool retVal = true; 

            // check input is digit only
            foreach (char c in data) 
            {
                if (c < '0' || c > '9')
                {
                    retVal = false;
                    break;
                }
            }

            retVal &= data.Length == 9;

            return retVal;
        }

        static void Main()
        {
            StartServer();
        }
    }
}
