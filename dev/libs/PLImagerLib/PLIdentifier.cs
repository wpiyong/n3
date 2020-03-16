using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PLImagerLib
{
    public class PLIdentifier
    {

        public bool TestFl()
        {

            //get fl contours
            var rootDir = @"P:\Projects\N3 Imaging\Images\09042018_jewerly\test";
            var files = Directory.GetFiles(rootDir, "*.bmp", SearchOption.TopDirectoryOnly);
            Mat combinedFlMask = null;
            foreach (var file in files)
            {
                Mat fl = Cv2.ImRead(file);
                Mat fl_hls = new Mat();
                Cv2.CvtColor(fl, fl_hls, ColorConversionCodes.BGR2HLS);

                var fl_hls_split = Cv2.Split(fl_hls);
                Mat H = fl_hls_split[0].Clone(), L, S;
                H.ConvertTo(H, MatType.CV_16UC1);

                H = 2 * H;//opencv H is from 0 .. 180
                L = (100 * fl_hls_split[1])/ 255;//L and S range is from 0..255
                S = (100 * fl_hls_split[2]) / 255;//L and S range is from 0..255

                var l = (uint)Cv2.Mean(L)[0];
                var s = (uint)Cv2.Mean(S)[0];
                var h = (uint)Cv2.Mean(H)[0];

                Debug.WriteLine(H.Dump());

                var L_vector_ptr = (L.Reshape(1, L.Rows * L.Cols)).Data;
                var L_vector = new byte[L.Rows * L.Cols];
                Marshal.Copy(L_vector_ptr, L_vector, 0, L.Rows * L.Cols);

                //Debug.WriteLine(string.Join("\n", L_vector));

                var jenksBreaks = 
                    JenksFisher.CreateJenksFisherBreaksArray(L_vector.Select(p => (double)p).ToList(), 4);

                var jenksBreaks1 = JenksFisher.getJenksBreaks(L_vector.Select(p => (double)p).ToList(), 4);

                Mat threshold = new Mat();
                Cv2.InRange(fl_hls, new Scalar(0, 0, 0), new Scalar(255, jenksBreaks[2], 255), threshold);
                Cv2.ImShow("threshold", threshold);
                Cv2.WaitKey();
                Cv2.DestroyAllWindows();

                //Debug.WriteLine(fl_hls_split[1].Dump());
            }

                        
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            return true;
        }

        public bool Test()
        {
            var plMask = new PLMask();
            Mat whiteLight = Cv2.ImRead(@"P:\Projects\N3 Imaging\Images\09042018_jewerly\WhiteLight\whitelight.bmp");

            //get phos contours
            var rootDir = @"P:\Projects\N3 Imaging\Images\09042018_jewerly\Phos";
            var files = Directory.GetFiles(rootDir, "*.bmp", SearchOption.TopDirectoryOnly);
            Mat combinedMask = null;
            foreach (var file in files)
            {
                var pl = Cv2.ImRead(file);
                Mat mask;

                var res = plMask.PlMask(pl, 20, out mask);
                if (combinedMask == null)
                    combinedMask = Mat.Zeros(mask.Size(), mask.Type());

                Cv2.Add(mask, combinedMask, combinedMask);
            }
            Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                    new OpenCvSharp.Size(9, 9),
                            new OpenCvSharp.Point(2, 2));
            Cv2.Dilate(combinedMask, combinedMask, element);

            //find contours on this mask
            Mat[] contours;
            var hierarchy = new List<Point>();
            Cv2.FindContours(combinedMask, out contours, OutputArray.Create(hierarchy), RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            //remove small size contours
            List<Mat> phosContours = new List<Mat>();
            foreach (var contour in contours)
            {
                if (Cv2.ContourArea(contour) > 400)
                    phosContours.Add(contour);
            }

            //Mat phosMask = Mat.Zeros(combinedMask.Size(), MatType.CV_8UC1);
            //Cv2.DrawContours(phosMask, phosContours, -1, Scalar.White, -1);//filled contours

            //get centers of contours
            for (int i = 0; i < phosContours.Count; i++)
            {
                var c = phosContours[i];
                var m = c.Moments(true);
                var x = m.M10 / m.M00;
                var y = m.M01 / m.M00;
                Cv2.DrawContours(whiteLight, phosContours, i, new Scalar(255, 0, 0), 4);
                Cv2.PutText(whiteLight, "P", new Point(x, y), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 4);

            }


            //get fl contours
            rootDir = @"P:\Projects\N3 Imaging\Images\09042018_jewerly\SW Fl";
            files = Directory.GetFiles(rootDir, "*.bmp", SearchOption.TopDirectoryOnly);
            Mat combinedFlMask = null;
            foreach (var file in files)
            {
                var pl = Cv2.ImRead(file);
                Mat mask;

                var res = plMask.PlMask(pl, 20, out mask);
                if (combinedFlMask == null)
                    combinedFlMask = Mat.Zeros(mask.Size(), mask.Type());

                Cv2.Add(mask, combinedFlMask, combinedFlMask);
            }

            Cv2.ImShow("combinedFlMask", combinedFlMask);
            Cv2.WaitKey(0);
            Cv2.DestroyWindow("combinedFlMask");

            element = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                    new OpenCvSharp.Size(9, 9),
                            new OpenCvSharp.Point(2, 2));
            Cv2.Dilate(combinedFlMask, combinedFlMask, element);

            //find contours on this mask
            Cv2.FindContours(combinedFlMask, out contours, OutputArray.Create(hierarchy), RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            //remove small size contours
            List<Mat> flContours = new List<Mat>();
            foreach (var contour in contours)
            {
                if (Cv2.ContourArea(contour) > 400)
                    flContours.Add(contour);
            }

            //Mat flMask = Mat.Zeros(combinedFlMask.Size(), MatType.CV_8UC1);
            //Cv2.DrawContours(flMask, flContours, -1, Scalar.White, -1);//filled contours
            //check for intersection with phos contours
            //if no intersection then label them
            for (int i = 0; i < flContours.Count; i++)
            {
                Mat blankFl = Mat.Zeros(whiteLight.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(blankFl, flContours, i, Scalar.White, -1);
                bool phos = false;
                for(int j = 0; j < phosContours.Count; j++)
                {
                    Mat blankPhos = Mat.Zeros(whiteLight.Size(), MatType.CV_8UC1);
                    Cv2.DrawContours(blankPhos, phosContours, j, Scalar.White, -1);
                    Mat intersection = new Mat();
                    Cv2.BitwiseAnd(blankFl, blankPhos, intersection);
                    //Cv2.ImShow("blankFl", blankFl);
                    //Cv2.ImShow("blankPhos", blankPhos);
                    //Cv2.ImShow("intersection", intersection);
                    //Cv2.WaitKey();
                    if (intersection.Sum()[0] > 0)
                    {
                        phos = true;
                        break;
                    }
                }

                if (!phos)
                {
                    var c = flContours[i];
                    var m = c.Moments(true);
                    var x = m.M10 / m.M00;
                    var y = m.M01 / m.M00;
                    Cv2.DrawContours(whiteLight, flContours, i, new Scalar(255, 0, 0), 4);
                    Cv2.PutText(whiteLight, "F", new Point(x, y), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 4);
                }

            }

            Cv2.ImShow("whiteLight", whiteLight);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            return true;
        }
    }
}
