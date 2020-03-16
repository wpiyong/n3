using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace N3Imager.Model
{
    class ImageProcessor
    {
        public static void GetHSL(BitmapSource bsrc, out uint l, out uint s, out uint h)
        {
            Mat src = BitmapSourceConverter.ToMat(bsrc);
            GetHSL(src, out l, out s, out h);
        }

        public static void GetHSL(Mat src, out uint l, out uint s, out uint h)
        {
            //Cv2.ImShow("src", src);
            //Cv2.WaitKey();
            //Cv2.DestroyAllWindows();

            Mat src_hls = new Mat();
            Cv2.CvtColor(src, src_hls, ColorConversionCodes.BGR2HLS);

            var src_hls_split = Cv2.Split(src_hls);
            Mat H = src_hls_split[0].Clone(), L, S;
            H.ConvertTo(H, MatType.CV_16UC1);
            H = 2 * H;//opencv H is from 0 .. 180
            L = (100 * src_hls_split[1]) / 255;//L and S range is from 0..255
            S = (100 * src_hls_split[2]) / 255;//L and S range is from 0..255

            l = (uint)Cv2.Mean(L)[0];
            s = (uint)Cv2.Mean(S)[0];
            h = (uint)Cv2.Mean(H)[0];
        }
    }
}
