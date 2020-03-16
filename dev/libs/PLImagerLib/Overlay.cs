using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLImagerLib
{
    public class Overlay
    {
        public bool Test()
        {
            var wl = Cv2.ImRead(@"P:\Projects\N3 Imaging\Images\Testing\whiteLight.bmp");
            var edge = Cv2.ImRead(@"P:\Projects\N3 Imaging\Images\Testing\edgeMask.bmp", ImreadModes.GrayScale);
            Mat merged;

            var res = MaskOutline(wl, edge, out merged);

            Cv2.ImShow("white light", wl);
            Cv2.ImShow("edge mask", edge);
            Cv2.ImShow("merged", merged);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            return res;

        }


        public bool Add(Mat whiteLightImage, Mat edgeMaskImage, out Mat mergedImage)
        {
            bool result = false;
            mergedImage = Mat.Zeros(whiteLightImage.Size(), whiteLightImage.Type());

            try
            {
                Cv2.Add(whiteLightImage, edgeMaskImage, mergedImage);

                result = true;
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        public bool MaskOutline(Mat whiteLightImage, Mat edgeMaskImage, out Mat mergedImage)
        {
            bool result = false;
            mergedImage = whiteLightImage.Clone();

            try
            {
                //convert white mask outlines to purple color outlines
                MatOfByte matb = new MatOfByte(edgeMaskImage);
                var indexerGray = matb.GetIndexer();

                MatOfByte3 mat3 = new MatOfByte3(mergedImage);
                var indexerColor = mat3.GetIndexer();

                for (int y = 0; y < edgeMaskImage.Height; y++)
                {
                    for (int x = 0; x < edgeMaskImage.Width; x++)
                    {
                        byte edge = indexerGray[y, x];
                        if (edge == 255)//white
                        {
                            indexerColor[y, x] = new Vec3b(255, 128, 255);//purple
                        }
                    }
                }
                               

                result = true;
            }
            catch (Exception ex)
            {

            }

            return result;
        }
    }
}
