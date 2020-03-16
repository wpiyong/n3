using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhosFinder
{
    public partial class Form1 : Form
    {
        List<string> files;
        int fileIndex = -1;
        Rectangle rect;
        System.Drawing.Point mouseDownPoint = System.Drawing.Point.Empty;
        System.Drawing.Point mousePoint = System.Drawing.Point.Empty;
        int imageWidth, imageHeight;
        Mat image;

        public Form1()
        {
            InitializeComponent();
        }

        private double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        private void buttonSelectImage_Click(object sender, EventArgs e)
        {
            labelB.Text = "B: ";
            labelG.Text = "G: ";
            labelR.Text = "R: ";
            Cv2.DestroyAllWindows();
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    files = new DirectoryInfo(Path.GetDirectoryName(openFileDialog1.FileName)).GetFiles()
                        .Where(f => f.Extension.ToUpper() == ".PNG" || f.Extension.ToUpper() == ".BMP"
                                    || f.Extension.ToUpper() == ".JPG")
                                                  .OrderBy(f => f.Name)
                                                  .Select(f => f.FullName).ToList();

                    if (files != null && files.Count > 0)
                    {
                        fileIndex = files.IndexOf(openFileDialog1.FileName);
                        var bmp = new Bitmap(files[fileIndex]);
                        image = new Mat(files[fileIndex]);
                        imageWidth = bmp.Width;
                        imageHeight = bmp.Height;
                        labelFilename.Text = files[fileIndex];
                        pictureBoxImage.Image = bmp;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

#if FALSE
        private void buttonSelectImage_Click2(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<OpenCvSharp.Point> rois = new List<OpenCvSharp.Point>(new OpenCvSharp.Point[] {
                        new OpenCvSharp.Point(35,90),new OpenCvSharp.Point(400,90),new OpenCvSharp.Point(850,90),new OpenCvSharp.Point(1290,90),
                        new OpenCvSharp.Point(35,400),new OpenCvSharp.Point(400,400),new OpenCvSharp.Point(850,400),new OpenCvSharp.Point(1290,400),
                        new OpenCvSharp.Point(35,800),new OpenCvSharp.Point(400,800),new OpenCvSharp.Point(850,800),new OpenCvSharp.Point(1290,800),
                        new OpenCvSharp.Point(35,1230),new OpenCvSharp.Point(400,1230),new OpenCvSharp.Point(850,1230),new OpenCvSharp.Point(1290,1230)
                    });
                    var files = Directory.GetFiles(Path.GetDirectoryName(openFileDialog1.FileName), "*.png").ToList();
                    if (files != null && files.Count > 0)
                    {
                        int index = files.IndexOf(openFileDialog1.FileName);
                        //using (var win = new Window())
                        //using (var win1 = new Window())
                        {
                            while (true)
                            {
                                Debug.WriteLine(files[index]);
                                Mat img = new Mat(files[index]);//load color image
                                Mat gray = new Mat(), laplacian = new Mat(), sobel = new Mat(), canny = new Mat();
                                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                                //Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);
                                Cv2.NamedWindow("img", WindowMode.Normal);
                                Cv2.ImShow("img", img);
                                
                                foreach (var pt in rois)
                                {
                                    Rect roi = new Rect(pt, new OpenCvSharp.Size(200, 200));
                                    Mat original = img.Clone(roi);
                                    Mat gray2 = gray.Clone(roi);
                                    Cv2.Canny(gray2, canny, 100, 300, 3);
                                  

                                    Mat[] contours1;
                                    var hierarchy1 = new List<OpenCvSharp.Point>();
                                    Cv2.FindContours(canny, out contours1, OutputArray.Create(hierarchy1),
                                        RetrievalModes.External,
                                        ContourApproximationModes.ApproxSimple);

                                    var hulls1 = new List<Mat>();
                                    for (int j = 0; j < contours1.Length; j++)
                                    {
                                        Mat hull = new Mat();
                                        Cv2.ConvexHull(contours1[j], hull);
                                        if (hull.ContourArea() > 100)
                                            hulls1.Add(hull);
                                    }

                                    if (hulls1.Count > 0)
                                    {
                                        Mat drawing = Mat.Zeros(canny.Size(), MatType.CV_8UC1);
                                        Cv2.DrawContours(drawing, hulls1, -1, Scalar.White, -1);
                                        //Cv2.NamedWindow(pt.ToString() + " hulls", WindowMode.Normal);
                                        //Cv2.ImShow(pt.ToString() + " hulls", drawing);

                                        List<OpenCvSharp.Point> points = new List<OpenCvSharp.Point>();
                                        foreach (Mat hull in hulls1)
                                        {
                                            int m2Count = (hull.Rows % 2 > 0) ? hull.Rows + 1 : hull.Rows;
                                            OpenCvSharp.Point[] p2 = new OpenCvSharp.Point[m2Count];
                                            hull.GetArray(0, 0, p2);
                                            Array.Resize(ref p2, hull.Rows);

                                            points.AddRange(p2.ToList());
                                        }
                                        Mat finalHull = new Mat();
                                        Cv2.ConvexHull(InputArray.Create(points), finalHull);

                                        if (finalHull.ContourArea() > 1000)
                                        {
                                            List<Mat> finalHulls = new List<Mat>();
                                            finalHulls.Add(finalHull);
                                            Mat mask = Mat.Zeros(drawing.Size(), MatType.CV_8UC1);
                                            Cv2.DrawContours(mask, finalHulls, -1, Scalar.White, -1);

                                            //Cv2.ImShow(pt.ToString() + " mask", mask);
                                            //Cv2.ImShow(pt.ToString() + " original", original);
                                            Mat masked = Mat.Zeros(original.Size(), original.Type()) ;
                                            original.CopyTo(masked, mask);
                                            Cv2.ImShow(pt.ToString() + " masked", masked);
                                            
                                        }
                                    }


                                }



                                int c = Cv2.WaitKey(0);

                                if (c == 'n' && index < files.Count - 1)
                                    index++;
                                else if (c == 'p' && index > 0)
                                    index--;
                                else if (c == 27)
                                    break;
                                else
                                    index = 0;

                                Window.DestroyAllWindows();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void buttonSelectImage1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<OpenCvSharp.Point> rois = new List<OpenCvSharp.Point>(new OpenCvSharp.Point[] {
                        new OpenCvSharp.Point(35,90),new OpenCvSharp.Point(400,90),new OpenCvSharp.Point(850,90),new OpenCvSharp.Point(1290,90),
                        new OpenCvSharp.Point(35,400),new OpenCvSharp.Point(400,400),new OpenCvSharp.Point(850,400),new OpenCvSharp.Point(1290,400),
                        new OpenCvSharp.Point(35,800),new OpenCvSharp.Point(400,800),new OpenCvSharp.Point(850,800),new OpenCvSharp.Point(1290,800),
                        new OpenCvSharp.Point(35,1230),new OpenCvSharp.Point(400,1230),new OpenCvSharp.Point(850,1230),new OpenCvSharp.Point(1290,1230)
                    });
                    var files = Directory.GetFiles(Path.GetDirectoryName(openFileDialog1.FileName), "*.png").ToList();
                    if (files != null && files.Count > 0)
                    {
                        int index = files.IndexOf(openFileDialog1.FileName);
                        //using (var win = new Window())
                        //using (var win1 = new Window())
                        {
                            while (true)
                            {
                                ListBox listBoxTemp = new ListBox();
                                listBoxTemp.Items.Clear();
                                Debug.WriteLine(files[index]);
                                Mat img = new Mat(files[index]);//load color image
                                Mat gray = new Mat(), laplacian = new Mat(), sobel = new Mat(), canny = new Mat();
                                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                                //Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);

                                Cv2.Canny(gray, canny,100, 300, 3);
                                Cv2.NamedWindow("img", WindowMode.Normal);
                                Cv2.NamedWindow("hulls", WindowMode.Normal);
                                Cv2.ImShow("img", img);

                                Mat[] contours1;
                                var hierarchy1 = new List<OpenCvSharp.Point>();
                                Cv2.FindContours(canny, out contours1, OutputArray.Create(hierarchy1),
                                    RetrievalModes.External,
                                    ContourApproximationModes.ApproxSimple);

                                var hulls1 = new List<Mat>();
                                for (int j = 0; j < contours1.Length; j++)
                                {
                                    Mat hull = new Mat();
                                    Cv2.ConvexHull(contours1[j], hull);
                                    if (hull.ContourArea() > 100)
                                        hulls1.Add(hull);
                                }

                                if (hulls1.Count > 0)
                                {
                                    Mat drawing = Mat.Zeros(canny.Size(), MatType.CV_8UC1);
                                    Cv2.DrawContours(drawing, hulls1, -1, Scalar.White, -1);
                                    
                                    Cv2.ImShow("hulls", drawing);
                                }

                                    goto wait;

                                Cv2.Laplacian(gray, laplacian, MatType.CV_32F);

                                foreach(var pt in rois)
                                {
                                    Rect roi = new Rect(pt, new OpenCvSharp.Size(200,200));
                                    Mat original = img.Clone(roi);
                                    Mat temp = laplacian.Clone(roi);
                                    MatOfFloat matf = new MatOfFloat(temp);
                                    var indexer = matf.GetIndexer();
                                    List<double> sums =new List<double>();
                                    double sum = 0;
                                    for (int y = 0; y < 25; y++)
                                    {
                                        for (int x = 0; x < 25; x++)
                                        {
                                            float val = indexer[y, x];
                                            if (val >= 1)
                                                sum += val;                                            
                                        }
                                    }
                                    sums.Add(sum);
                                    sum = 0;
                                    for (int y = 0; y < 25; y++)
                                    {
                                        for (int x = temp.Width - 25; x < temp.Width; x++)
                                        {
                                            float val = indexer[y, x];
                                            if (val >= 1)
                                                sum += val;
                                        }
                                    }
                                    sums.Add(sum);
                                    sum = 0;
                                    for (int y = temp.Height - 25; y < temp.Height; y++)
                                    {
                                        for (int x = temp.Width - 25; x < temp.Width; x++)
                                        {
                                            float val = indexer[y, x];
                                            if (val >= 1)
                                                sum += val;
                                        }
                                    }
                                    sums.Add(sum);
                                    sum = 0;
                                    for (int y = temp.Height - 25; y < temp.Height; y++)
                                    {
                                        for (int x = 0; x < 25; x++)
                                        {
                                            float val = indexer[y, x];
                                            if (val >= 1)
                                                sum += val;
                                        }
                                    }
                                    sums.Add(sum);
                                    sum = 0;
                                    for (int y = 88; y < 113; y++)
                                    {
                                        for (int x = 88; x < 113; x++)
                                        {
                                            float val = indexer[y, x];
                                            if (val >= 1)
                                                sum += val;
                                        }
                                    }
                                    sums.Add(sum);

                                    double avg = sums.Average();
                                    double stdev = CalculateStdDev(sums);
                                    int upper = (int)(avg + 1.5 * stdev);
                                    int lower = (int)(avg - 1.5 * stdev);

                                    listBoxTemp.Items.Add(pt.ToString() + ": " + String.Join(",", sums.ToArray())
                                        + "," + upper +","+lower);
                                    if (sums[4] > upper || sums[4] < lower)
                                    {
                                        //Cv2.ImShow(pt.ToString(), temp);
                                        temp.ConvertTo(temp, MatType.CV_8UC1);

                                        Mat [] contours;
                                        var hierarchy = new List<OpenCvSharp.Point>();
                                        Cv2.FindContours(temp, out contours, OutputArray.Create(hierarchy), 
                                            RetrievalModes.External,
                                            ContourApproximationModes.ApproxSimple);
                                        
                                        var hulls = new List<Mat>();
                                        for (int j = 0; j < contours.Length; j++)
                                        {
                                            Mat hull = new Mat();
                                            Cv2.ConvexHull(contours[j], hull);
                                            if (hull.ContourArea() > 1000)
                                                hulls.Add(hull);
                                        }
                                        
                                        if (hulls.Count > 0)
                                        {
                                            Mat drawing = Mat.Zeros(temp.Size(), MatType.CV_8UC1);
                                            Cv2.DrawContours(drawing, contours, -1, Scalar.White, -1);
                                            Cv2.ImShow(pt.ToString() + " hulls", drawing);
                                            
                                            /*
                                            List<OpenCvSharp.Point> points = new List<OpenCvSharp.Point>();
                                            foreach (Mat hull in hulls)
                                            {
                                                int m2Count = (hull.Rows % 2 > 0) ? hull.Rows + 1 : hull.Rows;
                                                OpenCvSharp.Point[] p2 = new OpenCvSharp.Point[m2Count];
                                                hull.GetArray(0, 0, p2);
                                                Array.Resize(ref p2, hull.Rows);

                                                points.AddRange(p2.ToList());
                                            }
                                            Mat finalHull = new Mat();
                                            Cv2.ConvexHull(InputArray.Create(points), finalHull);

                                            if (finalHull.ContourArea() > 1000)
                                            {
                                                List<Mat> finalHulls = new List<Mat>();
                                                finalHulls.Add(finalHull);
                                                Mat mask = Mat.Zeros(temp.Size(), MatType.CV_8UC1);
                                                Cv2.DrawContours(mask, finalHulls, -1, Scalar.White, -1);

                                                Cv2.ImShow(pt.ToString(), temp);
                                                Cv2.ImShow(pt.ToString() + " original", original);
                                                Cv2.ImShow(pt.ToString() + " mask", mask);
                                                //Mat masked = Mat.Zeros(original.Size(), original.Type()) ;
                                                //masked.CopyTo(original, mask);
                                                //Cv2.ImShow(pt.ToString() + " masked", masked);
                                            }
                                            */
                                        }
                                        
                                    }
                                }


                            wait:

                                int c = Cv2.WaitKey(0);

                                if (c == 'n' && index < files.Count - 1)
                                    index++;
                                else if (c == 'p' && index > 0)
                                    index--;
                                else if (c == 27)
                                    break;
                                else
                                    index = 0;

                                Window.DestroyAllWindows();
                            }
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
#endif
        private void buttonNext_Click(object sender, EventArgs e)
        {
            labelB.Text = "B: ";
            labelG.Text = "G: ";
            labelR.Text = "R: ";
            Cv2.DestroyAllWindows();
            if (files != null && files.Count > 0)
            {
                if (fileIndex < files.Count - 1)
                {
                    var bmp = new Bitmap(files[++fileIndex]);
                    imageWidth = bmp.Width;
                    imageHeight = bmp.Height;
                    image = new Mat(files[fileIndex]);
                    labelFilename.Text = files[fileIndex];
                    pictureBoxImage.Image = bmp;
                }
                else
                    MessageBox.Show("last file");
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            Cv2.DestroyAllWindows();
            labelB.Text = "B: ";
            labelG.Text = "G: ";
            labelR.Text = "R: ";
            if (files != null && files.Count > 0)
            {
                if (fileIndex > 0)
                {
                    var bmp = new Bitmap(files[--fileIndex]);
                    imageWidth = bmp.Width;
                    imageHeight = bmp.Height;
                    image = new Mat(files[fileIndex]);
                    labelFilename.Text = files[fileIndex];
                    pictureBoxImage.Image = bmp;
                }
                else
                    MessageBox.Show("first file");
            }
        }

        static public Rectangle GetRectangle(System.Drawing.Point p1, System.Drawing.Point p2)
        {
            return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
        }

        private void pictureBoxImage_MouseDown(object sender, MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mousePoint = e.Location;
        }

        
        private void pictureBoxImage_MouseMove(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                pictureBoxImage.Refresh();
                using (Graphics g = pictureBoxImage.CreateGraphics())
                {
                    rect = GetRectangle(mousePoint, e.Location);
                    g.DrawRectangle(Pens.Red, rect);
                }
            }
        }

        private void pictureBoxImage_MouseUp(object sender, MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Debug.WriteLine(rect);
            if (image != null && rect.Width > 0 && rect.Height > 0)
            { 
                Rect roi = new Rect(rect.X * imageWidth / pictureBoxImage.Width, 
                    rect.Y * imageHeight / pictureBoxImage.Height,
                    rect.Width * imageWidth / pictureBoxImage.Width,
                    rect.Height * imageHeight / pictureBoxImage.Height
                    );
                Mat roi_image = image.Clone(roi);
                //Cv2.ImShow("crop", roi_image);

                Mat[] channels;
                Cv2.Split(roi_image, out channels);
                labelB.Text = "B: " + Math.Round(Cv2.Mean(channels[0])[0], 2).ToString();
                labelG.Text = "G: " + Math.Round(Cv2.Mean(channels[1])[0], 2).ToString();
                labelR.Text = "R: " + Math.Round(Cv2.Mean(channels[2])[0], 2).ToString();
                rect = new Rectangle();
            }
        }
    }
}
