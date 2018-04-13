using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace AppClient
{
    class Program
    {
        static public int MAX_SLEEP_TIME = 1000;

        public static void StartClient(string input) {
            try {
                // establish a TCP/IP socket 
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());  
                IPAddress ipAddress = ipHostInfo.AddressList[0];  
                IPEndPoint remoteEP = new IPEndPoint(ipAddress,4000);  
                Socket sender = new Socket(ipAddress.AddressFamily, 
                    SocketType.Stream, ProtocolType.Tcp );  

                // Connect to server
                try {
                    sender.Connect(remoteEP);
                   
                    // Receive the response from server
                    byte[] bytes = new byte[1024];  
                    int bytesRec = sender.Receive(bytes);  
                    string response = Encoding.ASCII.GetString(bytes,0,bytesRec);
                    Console.WriteLine($"[SERVER]: {response}");
                    if (Object.Equals(response, "busy")) {
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close(); 
                        return;
                    }

                    System.Threading.Thread.Sleep(MAX_SLEEP_TIME);

                    // send number 
                    if (InputCheck(input)) {
                        // Encode the data and send through the socket
                        byte[] msg = Encoding.ASCII.GetBytes(input + Environment.NewLine);
                        int bytesSent = sender.Send(msg);
                        Console.WriteLine("Message sent.");
                    }
                         
                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                } catch (ArgumentNullException ane) {  
                    Console.WriteLine("ArgumentNullException : {0}",ane.ToString());  
                } catch (SocketException se) {  
                    Console.WriteLine("SocketException : {0}",se.ToString());  
                } catch (Exception e) {  
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());  
                }           
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

        // validata input is 
        public static bool InputCheck(string input) {
            bool ret = false;
            if (Object.Equals(input, "terminate")) {
                ret = true;
            } else {

                // check input is digit only
                bool isDigitOnly = true;
                foreach (char c in input) {
                    if (c < '0' || c > '9') {
                        isDigitOnly = false;
                        break;
                    }
                }             
                if (isDigitOnly && input.Length == 9) {
                    ret = true;
                }            
            }
            return ret;
        }

        static void Main(string[] args)
        {
            if (args.Length > 0) {
                StartClient(args[0]);
            } else {
                Random rnd = new Random();
                
                while (true) 
                {
                    int number = rnd.Next(0, 999999999);
                    StartClient(number.ToString().PadLeft(9, '0'));
                }

            } 
        }
        
    }
}
