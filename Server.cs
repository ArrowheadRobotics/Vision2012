using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;           
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Media;

namespace Player
{
    public static class Server
    {
        public static double[] data = {-1,-1,-1}; // dist, to be determined, to be determined
        private static int port = 1180;
        public static bool dflag = true;
        private static Socket s;
        private static TcpListener myList;
        private static bool inUse = false;
        public static void ServerStuff()
        {
            if (inUse)
            {
                Console.WriteLine("SERVER ERROR: ALREADY LISTENING IN OTHER THREAD. ABORTING...");
                return;
            }
            else
            {
                inUse = true;
            }
            myList = new TcpListener(IPAddress.Any, port);
            myList.Start();
            Console.WriteLine("Starting at port: " + port);
            Console.WriteLine("The local End point is: " +
                              myList.LocalEndpoint);
            Console.WriteLine("Waiting for a connection.....");
            s = myList.AcceptSocket();
            Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

            SoundPlayer simpleSound2 = new SoundPlayer(@"c:\inmessage.wav");
            simpleSound2.Play();
            simpleSound2.Dispose();
            
            try
            {
                while (dflag)
                {
                    byte[] b = new byte[100];
                    s.Receive(b);
                    short pick = Convert.ToInt16(b[0]);
                    if ((pick > 0) && (pick != 127))
                    {
                        Console.WriteLine("Got {0}", pick);
                        ASCIIEncoding asen = new ASCIIEncoding();
                        try
                        {
                            Console.WriteLine("SENDING: " + data[pick - 1].ToString());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        s.Send(asen.GetBytes(data[pick - 1].ToString()));
                    }
                    else if (pick == 127)
                    {
                        inUse = false;
                        break;
                    }
                }
            }
            catch { inUse = false; }
            s.Close();
            myList.Stop();
        }
        public static void SetData(double sdata, int id)
        {
            data[id] = sdata;
        }
        public static double GetData(int id)
        {
            return data[id];
        }
        public static void SetPort(int p)
        {
            port = p;
        }
    }
}