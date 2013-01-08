using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Player
{
    class Worker
    {
       
       public static int selected_square;//1,2,3,4,5,6,7,8,9 = squares :D




        //0 means default, show all squares
        public Worker()
        {
            
        }

        public void startTCP()
        {
            for (; ; ) {
                Server.ServerStuff(); // you will NEVER have to call this function EVER AGAIN as long as this thread exists
            }
        }


        public void startVI()
        {
            Application.Run(new MainForm());
        }

    }
}
