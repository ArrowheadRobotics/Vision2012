// Simple Player sample application
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2006-2011
// contacts@aforgenet.com
//

//TODO: Change back to const top
//TODO: Add my name -- Nathan S.
//Modified by permision by Corin Rypkema for the FIRST robotics competition.  corinryp@gmail.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Sockets;

using AForge;
using AForge.Video;
using AForge.Imaging;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using AForge;
using AForge.Math.Geometry;
using System.Net;

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Media;
using System.Threading;


namespace Player
{
    public partial class MainForm : Form
    {
        //Declare top level prototypes
        List<IntPoint> cornide;
        private Stopwatch stopWatch = null;
        SolidBrush brush = new SolidBrush(Color.Red);

        List<IntPoint> bestcorn;
        Bitmap global_bitmap;
        static string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "log.txt");
        System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, true);
        //inital value
        bool filter_high = false;
        int red = 150;
        int blue = 150;
        int green = 150;
        int red_max = 255;
        int blue_max = 255;
        int green_max = 255;
        int blob_size = 10;
        int cropx1 = 00;
        int cropx2 = 0;
        int cropy1 = 0;
        int cropy2 = 000;
        bool slowed = false;
        int slowrate = 50;
        double dist;
        double dist_previous = -1;
        int frame = 0;

        // Class constructor
        public MainForm()
        {

            this.KeyDown += new KeyEventHandler(OnKeyUp);//key server
            //this.MouseDown += new MouseEventHandler(OnMouseClick);
            InitializeComponent();
            CenterToScreen();//centers the form on the current screen

        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            file.Close();
            CloseCurrentVideoSource();
        }
        // "Exit" menu item clicked
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

            this.Close();

        }
        // Open local video capture device
        private void localVideoCaptureDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VideoCaptureDeviceForm form = new VideoCaptureDeviceForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {

                // create video source

                VideoCaptureDevice videoSource = form.VideoDevice;
                // open it

                OpenVideoSource(videoSource);

            }

        }
        // Open video file using DirectShow
        private void openVideofileusingDirectShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // create video source

                FileVideoSource fileSource = new FileVideoSource(openFileDialog.FileName);// open it
                OpenVideoSource(fileSource);

            }

        }

        // Open JPEG URL

        private void openJPEGURLToolStripMenuItem_Click(object sender, EventArgs e)
        {

            URLForm form = new URLForm();

            form.Description = "Enter URL of an updating JPEG from a web camera:";

            form.URLs = new string[]//predef form

				{

					"http://195.243.185.195/axis-cgi/jpg/image.cgi?camera=1",

				};




            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // create video source
                JPEGStream jpegSource = new JPEGStream(form.URL);
                // open it
                OpenVideoSource(jpegSource);
            }

        }




        // Open MJPEG URL

        private void openMJPEGURLToolStripMenuItem_Click(object sender, EventArgs e)
        {

            URLForm form = new URLForm();

            form.Description = "Enter URL of an MJPEG video stream:";

            form.URLs = new string[]

				{//predef form

					"http://10.7.6.19/axis-cgi/mjpg/video.cgi?resolution=640x480",

					"http://10.xx.yy.zz/axis-cgi/mjpg/video.cgi?resolution=640x480",
                    "http://wc2.dartmouth.edu/axis-cgi/mjpg/video.cgi?resolution=640x480",

				};


            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // create video source
                MJPEGStream mjpegSource = new MJPEGStream(form.URL);
                // open it
                OpenVideoSource(mjpegSource);
            }

        }




        // Open video source
        private void OpenVideoSource(IVideoSource source)
        {
            try
            {
                // set busy cursor
                this.Cursor = Cursors.WaitCursor;
                // stop current video source
                CloseCurrentVideoSource();
                // start new video source
                videoSourcePlayer.VideoSource = source;
                videoSourcePlayer.BackColor = Color.Black;
                videoSourcePlayer.Start();
                // reset stop watch
                stopWatch = null;
                // start timer
                timer.Start();
                this.Cursor = Cursors.VSplit;

                //Thread myThread = new Thread(new ThreadStart(cog(videoSourcePlayer)));

            }
            catch
            {
                statusStrip.Text = "Error connecting to server.";
                CloseCurrentVideoSource();
                timer.Stop();
            }
        }

        private ThreadStart cog(AForge.Controls.VideoSourcePlayer videoSourcePlayer)
        {
            while (true)
            {
                int framer = 0;
                framer++;
                if (framer % 15 == 0)
                {
                    videoSourcePlayer.SignalToStop();
                    videoSourcePlayer.Start();
                }
            }

        }
        // Close video source if it is running
        private void CloseCurrentVideoSource()
        {
            if (videoSourcePlayer.VideoSource != null)
            {
                videoSourcePlayer.SignalToStop();
                // wait ~ 3 seconds
                for (int i = 0; i < 30; i++)
                {
                    if (!videoSourcePlayer.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }
                if (videoSourcePlayer.IsRunning)
                {
                    videoSourcePlayer.Stop();
                }
                videoSourcePlayer.VideoSource = null;
            }
        }




        ///////
        //HERE
        //////
        // New frame received by the player
        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            frame++;
            if (frame % 5 == 0)
            {

                Console.WriteLine("Dropped a Frame");
                frame++;
                return;
            }
            global_bitmap = image;




            if (slowed)
                System.Threading.Thread.Sleep(slowrate);
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;

            DateTime now = DateTime.Now;
            Graphics g = Graphics.FromImage(image);
            image = ProcessImage(image);
            // paint current time

            try//TODO REMOVE THIS SECTION. NOT NEEDED
            {//find corners
                IntPoint[] arraycorners = cornide.ToArray();//convert to an array of intpoints

                if (((732 / avdisty(arraycorners)) + .222) < ((989 / avdistx(arraycorners)) + 3.37))
                {
                    dist = ((732 / avdisty(arraycorners)) + .222);//from our graph
                }
                else
                {
                    dist = ((989 / avdistx(arraycorners)) + 3.37);//from our graph
                }

                if (dist < 70 && (dist < (dist_previous * 1.5) && dist > (dist_previous * .5)))//LIMIT excessive shots.
                {
                    g.DrawString((arraycorners[0].ToString() + " = " + arraycorners[1].ToString() + " = " + arraycorners[2].ToString() + " = " + arraycorners[3].ToString() + " = " + "DISTANCE: " + dist), this.Font, brush, new PointF(5, 5));
                    dist_previous = dist;
                    Server.SetData(dist, 0); ;

                    //Console.WriteLine(dist);//TODO fix this
                }
                else if (dist_previous == -1)//TODO WHAT is this?
                {
                    dist_previous = dist;
                    g.DrawString("Hello", this.Font, brush, new PointF(5, 5));//hello
                }
                else
                {
                    g.DrawString("ERROR", this.Font, brush, new PointF(5, 5));//print error if too far
                    dist_previous = dist;
                }
            }
            catch (Exception e)
            {
                // g.DrawString("exception", this.Font, brush, new PointF(5, 5));//exception
            }

            brush.Dispose();
            g.Dispose();


        }

        private double avdisty(IntPoint[] corns)
        {//TODO absolute values
            double ret = 0;
            IntPoint one = corns[0];
            IntPoint two = corns[1];
            IntPoint three = corns[2];
            IntPoint four = corns[3];
            double y_average_a = ((double)one.Y + (double)two.Y) / 2;
            double y_average_b = ((double)three.Y + (double)four.Y) / 2;
            ret = abs(y_average_b - y_average_a);
            return ret;
        }

        private double abs(double p)//(c) Nadatan Industries
        {
            if (p < 0)
            {
                return -1 * p;
            }
            else
            {
                return p;
            }
        }
        private double avdistx(IntPoint[] corns)
        {//TODO absolute values
            double ret = 0;
            IntPoint one = corns[0];
            IntPoint two = corns[1];
            IntPoint three = corns[2];
            IntPoint four = corns[3];
            double x_average_a = ((double)one.X + (double)four.X) / 2;
            double x_average_b = ((double)two.X + (double)three.X) / 2;
            ret = x_average_b - x_average_a;
            return ret;
        }
        // On timer event - gather statistics
        private void timer_Tick(object sender, EventArgs e)
        {
            IVideoSource videoSource = videoSourcePlayer.VideoSource;
            if (videoSource != null)
            {
                // get number of frames since the last timer tick
                int framesReceived = videoSource.FramesReceived;
                if (stopWatch == null)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();
                }
                else
                {
                    stopWatch.Stop();
                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    fpsLabel.Text = fps.ToString("F2") + " fps | " + Server.data[0];
                    if (slowed)
                        fpsLabel.Text = fpsLabel.Text + " | Slow Mode | Delayed " + slowrate + " msec";
                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }
        private Bitmap ProcessImage(Bitmap bitmap)
        {///CROP SYSTEM


            //if (cropx1 != 0 && cropy1 != 0 && cropx2 != 0 && cropy2 != 0 && Worker.selected_square != 0)
            //{
            //    Crop crop_filter = new Crop(new Rectangle(cropx1, cropy1, cropx2, cropy2));
            //     bitmap= crop_filter.Apply(bitmap);
            //    Console.WriteLine(">Crop Cutter:> Cropped\n({0},{1}),({2},{3})", cropx1, cropy1, cropx2, cropy2);
            //}

            //if (Worker.selected_square == 0) 
            //{
            //    Crop crop_filter = new Crop(new Rectangle(0, 0, bitmap.Width,bitmap.Height));
            //    bitmap = crop_filter.Apply(bitmap);
            //    //Console.WriteLine(">Crop Cutter:> RESET");
            //}
            //// step 2 - locating objects

            if (cropx1 == 0 && cropy1 == 0 && cropx2 == 0 && cropy2 == 0)
            {
                cropx1 = 0;
                cropy1 = 0;
                cropx2 = bitmap.Width;
                cropy2 = bitmap.Height;

            }
            Bitmap old = bitmap;
            Crop crop_filter = new Crop(new Rectangle(cropx1, cropy1, cropx2, cropy2));

            bitmap = crop_filter.Apply(bitmap);




            // lock image
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            //  ////invert
            // Invert inv = new Invert();
            //  // //apply the filter
            //inv.ApplyInPlace(bitmapData);


            //// create filter
            //HSLFiltering filter = new HSLFiltering();
            //// set color ranges to keep
            //filter.Hue = new IntRange(90, 300);
            //filter.Saturation = new Range(0.0f, .5f);
            //filter.Luminance = new Range(0f, 1);
            //// apply the filter
            //filter.ApplyInPlace(bitmapData);



            //EuclideanColorFiltering filter = new EuclideanColorFiltering();
            //// set center colol and radius
            //filter.CenterColor = new RGB(209, 206, 196);
            //filter.Radius = 150;
            //// apply the filter
            //filter.ApplyInPlace(bitmapData);

            //// create filter
            ChannelFiltering filter = new ChannelFiltering();
            //// set channels' ranges to keep

            Invert inv = new Invert();

            //// //apply the filter

            inv.ApplyInPlace(bitmapData);

            if (red_max > 255)
            {
                red_max = 255;
                Console.WriteLine("1THRESHOLD OVERIDE");
            }
            if (blue_max > 255)
            {
                blue_max = 255;
                Console.WriteLine("2THRESHOLD OVERIDE");
            }
            if (green_max > 255)
            {
                green_max = 255;
                Console.WriteLine("3THRESHOLD OVERIDE");
            }
            if (red > 255)
            {
                red = 254;
                Console.WriteLine("4THRESHOLD OVERIDE");
            }
            if (blue > 255)
            {
                blue = 254;
                Console.WriteLine("5THRESHOLD OVERIDE");
            }
            if (green > 255)
            {
                green = 254;
                Console.WriteLine("6THRESHOLD OVERIDE");
            }

            if (red < 0)
            {
                red = 1;
                Console.WriteLine("7THRESHOLD UNDERIDE");
            }
            if (blue < 0)
            {
                blue = 1;
                Console.WriteLine("8THRESHOLD UNDERIDE");
            }
            if (green < 0)
            {
                green = 1;
                Console.WriteLine("9THRESHOLD UNDERIDE");
            }


            if (red_max < 0)
            {
                red_max = 255;
                Console.WriteLine("10THRESHOLD UNDERIDE");
            }
            if (blue_max < 0)
            {
                blue_max = 255;
                Console.WriteLine("11THRESHOLD UNDERIDE");
            }
            if (green_max < 0)
            {
                green_max = 255;
                Console.WriteLine("13THRESHOLD UNDERIDE");
            }

            if (red_max < red)
            {
                Console.WriteLine("14THRESHOLD UNION ERROR");
                red = 0;
                red_max = 255;
            }

            if (blue_max < blue)
            {
                blue = 0;
                Console.WriteLine("15THRESHOLD UNION ERROR");
                blue_max = 255;
            }
            if (green_max < green)
            {
                green = 0;
                green_max = 255;
                Console.WriteLine("16THRESHOLD UNION ERROR");
            }

            file.WriteLine("R: " + red + " R_M: " + red_max);
            file.WriteLine("G: " + green + " G_M: " + green_max);
            file.WriteLine("B: " + blue + " B_M: " + blue_max);
            file.WriteLine("EOE");
            //file.Close();
            filter.Red = new IntRange(red, red_max);
            filter.Green = new IntRange(green, green_max);
            filter.Blue = new IntRange(blue, blue_max);
            //// apply the filter
            filter.ApplyInPlace(bitmapData);



            ////invert


            //edge

            //CannyEdgeDetector edge_filter = new CannyEdgeDetector();
            //// apply the filter
            //edge_filter.ApplyInPlace(bitmapData);



            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = blob_size * 3;
            blobCounter.MinWidth = blob_size * 4;


            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bitmap.UnlockBits(bitmapData);



            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            Graphics g = Graphics.FromImage(bitmap);
            Pen yellowPen = new Pen(Color.Yellow, 2); // circles
            Pen redPen = new Pen(Color.Red, 2);       // quadrilateral
            Pen brownPen = new Pen(Color.Brown, 2);   // quadrilateral with known sub-type
            Pen greenPen = new Pen(Color.Green, 2);   // known triangle
            Pen bluePen = new Pen(Color.Blue, 2);     // triangle
            double curdist = 0;
            double leastdist = 100;
            //List<IntPoint> bestcorn;//promoted to super
            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners; // the list of x,y coordinates.  corners(list)->corner(intpoint)->[x,y]

                // is triangle or quadrilateral




                if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                {
                    cornide = corners;
                    // get sub-type

                    PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);
                    Pen pen;
                    if (subType == PolygonSubType.Rectangle || subType == PolygonSubType.Trapezoid || subType == PolygonSubType.Parallelogram || subType == PolygonSubType.Rhombus || subType == PolygonSubType.Square || subType == PolygonSubType.Unknown)
                    {

                        if (corners.Count == 4)
                        {
                            pen = redPen;
                            IntPoint[] array_of_corners = corners.ToArray();//array_of_corners is now an array of corners
                            if (((732 / avdisty(array_of_corners)) + .222) < ((989 / avdistx(array_of_corners)) + 3.37))
                            {
                                curdist = ((732 / avdisty(array_of_corners)) + .222);//from our graph
                            }
                            else
                            {
                                curdist = ((989 / avdistx(array_of_corners)) + 3.37);//from our graph
                            }

                            //if (Worker.selected_square == 1)
                            //{
                            //    Console.WriteLine("called\n");
                            //    IntPoint one = array_of_corners[0];
                            //    IntPoint two = array_of_corners[3];
                            //    cropx1 = one.X - 5;
                            //    cropy1 = one.Y - 5;
                            //    cropx2 = two.X + 5;
                            //    cropy2 = two.Y + 5;
                            //}
                            //else if (i == Worker.selected_square - 1 && Worker.selected_square != 1)
                            //{
                            //    Console.WriteLine("called\n");
                            //    IntPoint one = array_of_corners[0];
                            //    IntPoint two = array_of_corners[3];
                            //    cropx1 = one.X - 5;
                            //    cropy1 = one.Y - 5;
                            //    cropx2 = two.X + 5;
                            //    cropy2 = two.Y + 5;
                            //}
                            //if (Worker.selected_square == 0)
                            //{
                            //    cropx1 = 0;
                            //    cropy1 = 0;
                            //    cropx2 = 0;
                            //    cropy2 = 0;
                            //}

                            if (Worker.selected_square == 0)//PREDATOR VISION
                            {

                                if (curdist < leastdist)
                                {
                                    //  Console.WriteLine((abs(array_of_corners[0].Y - array_of_corners[1].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)));
                                    //if ((abs(array_of_corners[0].Y - array_of_corners[3].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)) < 1.25 && (abs(array_of_corners[0].Y - array_of_corners[3].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)) > .5)
                                    if (true)
                                    {


                                        leastdist = curdist;
                                        if (leastdist < 55 && leastdist > 2)
                                        {

                                            double angle = (array_of_corners[0].X + array_of_corners[3].X) / 2;
                                            Console.WriteLine("0: " + array_of_corners[0].X + " 1: " + array_of_corners[1].X + " 2: " + array_of_corners[2].X + " 3: " + array_of_corners[3].X);
                                            Server.SetData(((array_of_corners[0].X + array_of_corners[1].X) / 2), 1);
                                            Console.WriteLine("PREDATOR: " + Server.GetData(1));
                                            Console.WriteLine(leastdist);
                                            Server.SetData(leastdist, 0);

                                            bestcorn = corners;//superify?                                        
                                            //g.DrawPolygon(pen, ToPointsArray(bestcorn));//leave here as a comment

                                            IntPoint ones = array_of_corners[0];
                                            PointF string_draw = new PointF(ones.X, ones.Y);
                                            SolidBrush tbrush = new SolidBrush(Color.Red);
                                            try
                                            {
                                                if (string_draw.X != 0 && string_draw.Y != 0)
                                                {
                                                    g.DrawString(n.ToString(), this.Font, tbrush, string_draw);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("{0} in {1} ({2},{3})", e.Message, e.StackTrace, string_draw.X, string_draw.Y);
                                            }

                                        }

                                    }
                                }
                                try
                                {

                                    Graphics global_graphics_predator = Graphics.FromImage(bitmap);
                                    Pen penc = new Pen(Color.YellowGreen, 2);


                                    //g.DrawPolygon(penc, ToPointsArray(bestcorn));
                                    global_graphics_predator.DrawPolygon(penc, ToPointsArray(bestcorn));
                                    global_graphics_predator.DrawLine(penc, new System.Drawing.Point(320, 1), new System.Drawing.Point(320, 479));
                                    //bitmap = global_bitmap;


                                }
                                catch (Exception e)
                                {
                                }

                                yellowPen.Dispose();
                                redPen.Dispose();
                                greenPen.Dispose();
                                bluePen.Dispose();
                                brownPen.Dispose();
                                g.Dispose();



                                return bitmap;
                            }
                            else
                            {

                                int sel_least_dist = Worker.selected_square;
                                sel_least_dist *= 3;//threefeet
                                sel_least_dist -= 1;
                                int sel_max_dist = sel_least_dist + 5;
                                Console.WriteLine(sel_least_dist);
                                Console.WriteLine(sel_max_dist);
                                //Console.WriteLine(sel_least_dist + " " + sel_max_dist);



                                if (curdist < leastdist && curdist > sel_least_dist && curdist < sel_max_dist)
                                {
                                    //  Console.WriteLine((abs(array_of_corners[0].Y - array_of_corners[1].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)));
                                    //if ((abs(array_of_corners[0].Y - array_of_corners[3].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)) < 1.25 && (abs(array_of_corners[0].Y - array_of_corners[3].Y)) / (abs(array_of_corners[0].X - array_of_corners[1].X)) > .5)
                                    if (true)//todo
                                    {


                                        leastdist = curdist;
                                        if (leastdist < 55 && leastdist > 2)
                                        {

                                            // Console.WriteLine("0: " + array_of_corners[0].X + " 1: " + array_of_corners[1].X + " 2: " + array_of_corners[2].X + " 3: " + array_of_corners[3].X);

                                            Server.SetData(((array_of_corners[0].X + array_of_corners[1].X) / 2), 1);
                                            Console.WriteLine(Server.GetData(1));
                                            //Console.WriteLine(leastdist);
                                            Server.SetData(leastdist, 0);
                                            bestcorn = corners;//superify?                                        
                                            //g.DrawPolygon(pen, ToPointsArray(bestcorn));//leave here as a comment

                                            IntPoint ones = array_of_corners[0];
                                            PointF string_draw = new PointF(ones.X, ones.Y);
                                            SolidBrush tbrush = new SolidBrush(Color.Red);
                                            try
                                            {
                                                if (string_draw.X != 0 && string_draw.Y != 0)
                                                {
                                                    g.DrawString(n.ToString(), this.Font, tbrush, string_draw);
                                                    // glob.DrawLine(penc, new System.Drawing.Point(10, 10), new System.Drawing.Point(20, 20));
                                                    //g.DrawLine(pen, new System.Drawing.Point(10, 10), new System.Drawing.Point(20, 20));
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("{0} in {1} ({2},{3})", e.Message, e.StackTrace, string_draw.X, string_draw.Y);
                                            }

                                        }

                                    }
                                }
                            }

                            //pen = (corners.Count == 4) ? redPen : bluePen;
                        }//TODO get rid of anything that ISNT drawn, eg corner
                    }

                    //else
                    //{

                    //    pen = (corners.Count == 4) ? brownPen : greenPen;

                    //}


                }

            }
            //Console.WriteLine(leastdist); how dod oyo

            //double leastdist = 0;
            //for (int i = 0, n = blobs.Length; i < n; i++)
            //{
            //    try
            //    {
            //        List<IntPoint> filterlist;
            //        filterlist = cornide;

            //        IntPoint[] filter_corners = filterlist.ToArray();

            //        double currentdist = 0;
            //        for (int corner_count = 0; corner_count < filter_corners.Length; corner_count++)
            //        {

            //            IntPoint one_f = filter_corners[corner_count];
            //            corner_count++;
            //            IntPoint two_f = filter_corners[corner_count];
            //            corner_count++;
            //            IntPoint three_f = filter_corners[corner_count];
            //            corner_count++;
            //            IntPoint four_f = filter_corners[corner_count];


            //            double y_average_a = ((double)one_f.Y + (double)two_f.Y) / 2;
            //            double y_average_b = ((double)three_f.Y + (double)four_f.Y) / 2;
            //            double averaged_dist = y_average_b - y_average_a;
            //            currentdist = ((732 / averaged_dist) + .222);//from our graph
            //            if (currentdist <= leastdist)
            //            {
            //                leastdist = currentdist;
            //                Pen pencil = redPen;

            //                g.DrawPolygon(pencil, ToPointsArray(filterlist));
            //            }

            //        }


            //    }
            //    catch
            //    {
            //        //do nothING
            //    }

            //}
            //Console.WriteLine(leastdist);



            try
            {

                Graphics global_graphics = Graphics.FromImage(global_bitmap);
                Pen penc = new Pen(Color.YellowGreen, 2);
                {
                    //g.DrawPolygon(penc, ToPointsArray(bestcorn));
                    global_graphics.DrawPolygon(penc, ToPointsArray(bestcorn));
                    global_graphics.DrawLine(penc, new System.Drawing.Point(320, 1), new System.Drawing.Point(320, 479));

                    bitmap = global_bitmap;
                }
            }
            catch (Exception e)
            {
            }
            try
            {
                g.DrawLine(redPen, 220, 1, 220, 100);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.InnerException);

            }
            yellowPen.Dispose();
            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            brownPen.Dispose();

            g.Dispose();



            return bitmap;

        }

        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)//BLOB
        {

            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];




            for (int i = 0, n = points.Count; i < n; i++)
            {

                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);

            }




            return array;

        }


        public Bitmap ConvertToGrayscale(Bitmap source)//Not used
        {

            Bitmap bm = new Bitmap(source.Width, source.Height);

            for (int y = 0; y < bm.Height; y++)
            {

                for (int x = 0; x < bm.Width; x++)
                {

                    Color c = source.GetPixel(x, y);

                    int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);

                    bm.SetPixel(x, y, Color.FromArgb(luma, luma, luma));

                }

            }

            return bm;

        }
        public void OnKeyUp(object sender, KeyEventArgs e)//Key handler
        {
            try
            {
                switch (e.KeyCode.ToString().ToLower())
                {
                    case "-":
                        Worker.selected_square = -1;
                        break;
                    case "q":
                        slowed = !slowed;
                        break;
                    case "w":
                        if (slowrate - 10 > 0)
                            slowrate -= 10;
                        break;
                    case "e":
                        slowrate += 10;
                        break;
                    //NEW CROP
                    case "j":
                        cropx1 += 10;
                        break;
                    case "l":
                        cropx1 -= 10;
                        break;
                    case "i":
                        cropy1 += 10;
                        break;
                    case "k":
                        cropy1 -= +10;
                        break;
                    case "left":
                        cropx2 += 10;
                        break;
                    case "right":
                        cropx2 -= 10;
                        break;
                    case "up":
                        cropy2 += 10;
                        break;
                    case "down":
                        cropy2 -= +10;
                        break;
                    case "f":

                        blob_size++;
                        Console.WriteLine("Blob size: " + blob_size);
                        break;
                    case "v":
                        if (blob_size > 1)
                        {
                            blob_size--;

                            Console.WriteLine("Blob size: " + blob_size);
                        }
                        else
                        {
                            Console.WriteLine("Blob size underide");
                        }

                        break;
                    case "space":
                        filter_high = !filter_high;
                        Console.WriteLine("Keys set high: " + filter_high);
                        break;
                    case "a":
                        if (filter_high) red_max++;
                        if (!filter_high) red++;
                        Console.WriteLine("RED: " + red + " Red_Max:" + red_max);
                        break;

                    case "d":
                        if (filter_high) blue_max++;
                        if (!filter_high) blue++;
                        Console.WriteLine("Blue: " + blue + " Blue_Max:" + blue_max);
                        break;

                    case "s":
                        if (filter_high) green_max++;
                        if (!filter_high) green++;
                        Console.WriteLine("Greed: " + green + " Green:" + green_max);

                        break;

                    case "z":
                        if (filter_high) red_max--;
                        if (!filter_high) red--;
                        Console.WriteLine("RED: " + red + " Red_Max:" + red_max);
                        break;

                    case "c":
                        if (filter_high) blue_max--;
                        if (!filter_high) blue--;
                        Console.WriteLine("Blue: " + blue + " Blue_Max:" + blue_max);

                        break;


                    case "x":
                        if (filter_high) green_max--;
                        if (!filter_high) green--;
                        Console.WriteLine("Green: " + green + " Green Max:" + green_max);

                        break;


                    default:
                        try
                        {
                            String e_string = e.KeyCode.ToString();
                            Console.WriteLine(">KeyServer:> Key recieved: " + e_string);
                            char[] e_char = e_string.ToCharArray();
                            Worker.selected_square = int.Parse(e_char[1].ToString());
                            Console.WriteLine(">SquareSelector:> Square selected: " + Worker.selected_square);
                        }
                        catch (Exception err)
                        {
                        }
                        break;
                }
            }
            catch
            {
            }
        }

        //public void OnMouseClick(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == System.Windows.Forms.MouseButtons.Left)
        //    {
        //        cropx1 = e.Location.X - 5;
        //        cropy1 = e.Location.Y - 5;
        //        cropx2 = e.Location.X + 100;
        //        cropy2 = e.Location.Y + 100;

        //    }
        //}

        private void MainForm_Load(object sender, System.EventArgs e)
        {

        }



    }



}