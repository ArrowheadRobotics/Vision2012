using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Media;

namespace Player
{
    public class Test
    {
       
        static void Main()
        {
            //ThreadStart job = new ThreadStart(ThreadJob);
            //Thread thread = new Thread(job);
            //thread.Start();
            Console.WriteLine("WELCOME TO RAPTOR v.1.2\n\n CYBERHAWKS706\n\n\n\n\n");
            SoundPlayer simpleSound = new SoundPlayer(@"c:\start.wav");
            simpleSound.Play();
         
            
           //space
            Worker myWorker = new Worker();
            //more whitespace
            Thread myThread = new Thread(new ThreadStart(myWorker.startTCP));
            Thread myThread_two = new Thread(new ThreadStart(myWorker.startVI));
           
            myThread_two.Start();
            myThread.Start();
     
        }

        //static void ThreadJob()
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
                
        //    }
        //}
    }
}
 
