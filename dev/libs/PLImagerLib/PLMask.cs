using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLImagerLib
{
    public class PLMask
    {
        //1ms FL = 40 binary threshold

        public bool Test()
        {
            var rootDir = @"P:\Projects\N3 Imaging\Images\06272018_16natural_4_syn_binning\SWUV lamp phos";
            var files = Directory.GetFiles(rootDir, "*.bmp", SearchOption.TopDirectoryOnly);
            Mat combinedMask = null;
            Mat whiteLight = Cv2.ImRead(@"P:\Projects\N3 Imaging\Images\06272018_16natural_4_syn_binning\whitelight.bmp");
            foreach (var file in files)
            {
                var fl = Cv2.ImRead(file);
                Mat mask;
            
                var res = PlMask(fl, 20, out mask);
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
            List<Mat> bigContours = new List<Mat>();
            foreach(var contour in contours)
            {
                if (Cv2.ContourArea(contour) > 400)
                    bigContours.Add(contour);
            }

            combinedMask = Mat.Zeros(combinedMask.Size(), MatType.CV_8UC1);
            Cv2.DrawContours(combinedMask, bigContours, -1, Scalar.White);

            //get centers of contours
            for( int i = 0; i < bigContours.Count; i++)
            {
                var c = bigContours[i];
                var m = c.Moments(true);
                var x = m.M10 / m.M00;
                var y = m.M01 / m.M00;
                Cv2.DrawContours(whiteLight, bigContours, i, new Scalar(255, 0, 0), 4);
                Cv2.PutText(whiteLight, "F", new Point(x, y), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 4);

            }

            Cv2.ImShow("mask", combinedMask);
            Cv2.ImShow("whiteLight", whiteLight);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            return true;
        }

        public bool PlMask(Mat plImage, int threshold, out Mat edgeMask)
        {
            bool result = false;
            edgeMask = new Mat();

            try
            {
                Mat gray = new Mat();
                Cv2.CvtColor(plImage, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(gray, edgeMask, threshold, 255, ThresholdTypes.Binary);

                Cv2.ImShow("edgeMask", edgeMask);
                Cv2.WaitKey(0);

                Mat[] contours;
                var hierarchy = new List<Point>();
                Cv2.FindContours(edgeMask, out contours, OutputArray.Create(hierarchy), RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);

                var hulls = new List<Mat>();
                for (int j = 0; j < contours.Length; j++)
                {
                    Mat hull = new Mat();
                    Cv2.ConvexHull(contours[j], hull);
                    hulls.Add(hull);
                }

                edgeMask = Mat.Zeros(edgeMask.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(edgeMask, hulls, -1, Scalar.White);

                Cv2.ImShow("edgeMask", edgeMask);
                Cv2.WaitKey(0);
                Cv2.DestroyAllWindows();

                result = true;
            }
            catch(Exception ex)
            {

            }

            return result;
        }
    }
}
