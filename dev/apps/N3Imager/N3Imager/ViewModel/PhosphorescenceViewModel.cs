using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using N3Imager.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ViewModelLib;

namespace N3Imager.ViewModel
{
    class CamPhosResults
    {
        public ulong StartTimeStamp;
        public List<PtGreyCameraImage> Images;

        public CamPhosResults(ulong startTime, List<PtGreyCameraImage> images)
        {
            StartTimeStamp = startTime;
            Images = images.ToList();
        }
    }

    class PhosphorescenceViewModel : ViewModelBase, IDropTarget
    {
        StatusViewModel _statusVM;
        Stack<DateTime> _timestamps = new Stack<DateTime>();

        List<BitmapSource> _phosImageList = new List<BitmapSource>();
        List<string> _imageFilePathList = new List<string>();

        public PhosphorescenceViewModel(StatusViewModel svm)

        {
            base.DisplayName = "PhosphorescenceViewModel";

            _statusVM = svm;

            PreviousPhosImageCmd = new RelayCommand(param => PreviousPhosImage());
            NextPhosImageCmd = new RelayCommand(param => NextPhosImage());
            CommandSaveAll = new RelayCommand(param => Save(true));

            PixelSize = 0;

        }


        #region properties
        

        

        public string ImageFileName
        {
            get
            {
                string path = "";
                try
                {
                    System.IO.Path.GetFullPath(_imageFilePathList[PhosImageListIndex - 1]);
                    return System.IO.Path.GetFileName(_imageFilePathList[PhosImageListIndex - 1]);
                }
                catch
                {
                    if (PhosImageListIndex > 0)
                        path = _imageFilePathList[PhosImageListIndex - 1];
                }

                return path;
            }
            
        }
        public string ImageFilePath
        {
            get
            {
                try
                {
                    return _imageFilePathList[PhosImageListIndex - 1];
                }
                catch { }

                return "";
            }
        }

        private RelayCommand _mouseMoveCommand;
        public RelayCommand MouseMoveCommand
        {
            get
            {
                if (_mouseMoveCommand == null) _mouseMoveCommand = new RelayCommand(param => MouseMove((MouseEventArgs)param));
                return _mouseMoveCommand;
            }
            set { _mouseMoveCommand = value; }
        }

        private RelayCommand _mouseUpCommand;
        public RelayCommand MouseUpCommand
        {
            get
            {
                if (_mouseUpCommand == null) _mouseUpCommand = new RelayCommand(param => MouseUp((MouseEventArgs)param));
                return _mouseUpCommand;
            }
            set { _mouseUpCommand = value; }
        }

        private RelayCommand _mouseDownCommand;
        public RelayCommand MouseDownCommand
        {
            get
            {
                if (_mouseDownCommand == null) _mouseDownCommand = new RelayCommand(param => MouseDown((MouseEventArgs)param));
                return _mouseDownCommand;
            }
            set { _mouseDownCommand = value; }
        }

        public RelayCommand PreviousPhosImageCmd { get; set; }
        public RelayCommand NextPhosImageCmd { get; set; }
        public RelayCommand CommandSaveAll { get; set; }

        int _phosImageListIndex = 0;
        public int PhosImageListIndex
        {
            get { return _phosImageListIndex; }
            set
            {
                if (value > 0 && value <= _phosImageList.Count)
                {
                    _phosImageListIndex = value;
                    ImageProcessor.GetHSL(_phosImageList[_phosImageListIndex - 1],
                        out _imageL, out _imageS, out _imageH);
                    _roiMat = null;
                    PhosImage = _phosImageList[_phosImageListIndex - 1];
                    OnPropertyChanged("PhosImageListIndex");
                    OnPropertyChanged("ImageFilePath");
                    OnPropertyChanged("ImageFileName");
                    OnPropertyChanged("ImageL");
                    OnPropertyChanged("ImageS");
                    OnPropertyChanged("ImageH");
                }
            }
        }
        BitmapSource _phosImage;
        public BitmapSource PhosImage
        {
            get
            {
                return _phosImage;
            }
            set
            {
                _phosImage = value;
                OnPropertyChanged("PhosImage");
                if (_roiMat != null)
                {
                    ImageProcessor.GetHSL(_roiMat, out _selectionL, out _selectionS, out _selectionH);
                }
                else
                {
                    _selectionL = _selectionS = _selectionH = 0;
                }
                OnPropertyChanged("SelectionL");
                OnPropertyChanged("SelectionS");
                OnPropertyChanged("SelectionH");
            }
            
        }

        Mat _roiMat = null;
        uint _imageL, _imageS, _imageH, _selectionL, _selectionS, _selectionH;
        public uint ImageL
        {
            get { return _imageL; }
        }
        public uint ImageH
        {
            get { return _imageH; }
        }
        public uint ImageS
        {
            get { return _imageS; }
        }
        public uint SelectionL
        {
            get { return _selectionL; }
        }
        public uint SelectionS
        {
            get { return _selectionS; }
        }

        public uint SelectionH
        {
            get { return _selectionH; }
        }


        uint _pixelSize;
        public uint PixelSize
        {
            get { return _pixelSize; }
            set
            {
                _pixelSize = value;
                OnPropertyChanged("PixelSize");
            }
        }

        bool _isDragging = false;
        public bool IsDragging
        {
            get { return _isDragging; }
            set
            {
                _isDragging = value;
                OnPropertyChanged("IsDragging");
            }
        }
        int _roiX, _roiY, _roiWidth, _roiHeight;
        public int RoiX
        {
            get { return _roiX; }
            set
            {
                _roiX = value;
                OnPropertyChanged("RoiX");
            }
        }
        public int RoiY
        {
            get { return _roiY; }
            set
            {
                _roiY = value;
                OnPropertyChanged("RoiY");
            }
        }
        public int RoiWidth
        {
            get { return _roiWidth; }
            set
            {
                _roiWidth = value;
                OnPropertyChanged("RoiWidth");
            }
        }
        public int RoiHeight
        {
            get { return _roiHeight; }
            set
            {
                _roiHeight = value;
                OnPropertyChanged("RoiHeight");
            }
        }
        #endregion


        System.Windows.Point _anchorPoint = new System.Windows.Point();
        System.Windows.Point _imagePixelPos = new System.Windows.Point();
        private void MouseDown(MouseEventArgs e)
        {
            var image = e.Source as Image;
            if (image == null)
                return;

            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(image);
            _imagePixelPos = e.GetPosition((IInputElement)image);
            _anchorPoint = e.GetPosition((IInputElement)parentObject);
            IsDragging = true;
            RoiX = (int)_anchorPoint.X;
            RoiY = (int)_anchorPoint.Y;
        }
        private void MouseMove(MouseEventArgs e)
        {
            var image = e.Source as Image;
            if (image != null && IsDragging)
            {
                DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(image);
                var pos = e.GetPosition((IInputElement)parentObject);
                RoiWidth = (int)Math.Abs(pos.X - _anchorPoint.X);
                RoiHeight = (int)Math.Abs(pos.Y - _anchorPoint.Y);
            }
        }
        private void MouseUp(MouseEventArgs e)
        {
            IsDragging = false;

            var image = e.Source as Image;
            if (image != null)
            {
                var originalPhosImage = _phosImageList[PhosImageListIndex - 1];
                var pixelMousePositionX = _imagePixelPos.X * originalPhosImage.PixelWidth / image.ActualWidth;
                var pixelMousePositionY = _imagePixelPos.Y * originalPhosImage.PixelHeight / image.ActualHeight;
                var pixelWidth = RoiWidth * originalPhosImage.PixelWidth / image.ActualWidth;
                var pixelHeight = RoiHeight * originalPhosImage.PixelHeight / image.ActualHeight;

                if (pixelHeight > 0 && pixelWidth > 0)
                {
                    Mat src = BitmapSourceConverter.ToMat(originalPhosImage);

                    int rectX = (int)Math.Round(pixelMousePositionX);
                    if (rectX < 0)
                        rectX = 0;
                    if (rectX + pixelWidth > originalPhosImage.PixelWidth)
                        rectX = (int)(originalPhosImage.PixelWidth - pixelWidth);
                    int rectY = (int)Math.Round(pixelMousePositionY);
                    if (rectY < 0)
                        rectY = 0;
                    if (rectY + pixelHeight > originalPhosImage.PixelHeight)
                        rectY = (int)(originalPhosImage.PixelHeight - pixelHeight);

                    var roi = new OpenCvSharp.Rect(rectX, rectY,
                            (int)pixelWidth, (int)pixelHeight);
                    _roiMat = new Mat(src, roi).Clone();

                    int rectThick = (int)Math.Round(0.01 * originalPhosImage.PixelWidth, 0);
                    if (originalPhosImage.PixelHeight < originalPhosImage.PixelWidth)
                        rectThick = (int)Math.Round(0.01 * originalPhosImage.PixelHeight, 0);

                    Cv2.Rectangle(src, new OpenCvSharp.Point(rectX, rectY),
                        new OpenCvSharp.Point(pixelWidth + rectX, pixelHeight + rectY),
                        new Scalar(0, 0, 255, 255), rectThick);

                    //Cv2.NamedWindow("src", WindowMode.Normal);
                    //Cv2.ImShow("src", src);
                    //Cv2.ResizeWindow("src", 400, 300);
                    //Cv2.WaitKey();
                    //Cv2.DestroyAllWindows();
                    PhosImage = BitmapSourceConverter.ToBitmapSource(src);
                }
            }

            RoiX = RoiY = RoiWidth = RoiHeight = 0;


        }


        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
            dropInfo.Effects = dragFileList.Any(item =>
            {
                var extension = System.IO.Path.GetExtension(item);
                return extension != null && 
                    ( extension.ToUpper().Equals(".BMP") || extension.ToUpper().Equals(".JPG"));
            }) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>().ToList();
            if (dragFileList.Any(item =>
                {
                    var extension = System.IO.Path.GetExtension(item);
                    return extension != null &&
                        (extension.ToUpper().Equals(".BMP") || extension.ToUpper().Equals(".JPG"));
                }) == false)
                return;

            _phosImageList = new List<BitmapSource>();
            _imageFilePathList = new List<string>();
            for (int i = 0; i < dragFileList.Count; i++)
            {
                //handle file drop in data context
                _phosImageList.Add(new BitmapImage(new Uri(dragFileList[i])));
            }

            if (_phosImageList.Count > 0)
            {
                _imageFilePathList = dragFileList.ToList();
                PhosImageListIndex = 1;
            }
            else
            {
                PhosImageListIndex = 0;
            }
        }

        public void LoadCameraImages(CamPhosResults results)
        {
            _phosImageList = new List<BitmapSource>();
            _imageFilePathList = new List<string>();
            for (int i = 0; i < results.Images.Count; i++)
            {
                //handle file drop in data context
                _phosImageList.Add(BitmapToBitmapSource(results.Images[i].Image));
                _imageFilePathList.Add("Frame Id:" + results.Images[i].FrameId +
                    ", Timestamp delta [ms]:" + Math.Round((results.Images[i].TimeStamp - results.StartTimeStamp)/1000000d, 0));
            }

            if (_phosImageList.Count > 0)
            {
                PhosImageListIndex = 1;
            }
            else
            {
                PhosImageListIndex = 0;
            }
        }

        BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
           
            var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }


        void PreviousPhosImage()
        {
            if (PhosImageListIndex > 1)
                PhosImageListIndex = PhosImageListIndex - 1;
        }
        void NextPhosImage()
        {
            if (PhosImageListIndex < _phosImageList.Count)
                PhosImageListIndex = PhosImageListIndex + 1;
        }



        void Save(bool all = false)
        {

            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Saving..." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += SaveDoWork;
            bw.RunWorkerCompleted += SaveCompleted;
            bw.RunWorkerAsync(all);
        }

        void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;

            try
            {
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "JPG file (*.jpg)|*.jpg|BMP file (*.bmp)|*.bmp";
                if (saveDlg.ShowDialog() == true)
                {
                    var filePath = saveDlg.FileName;
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    var fileExt = System.IO.Path.GetExtension(filePath);


                    for (int i = 0; i < _phosImageList.Count; i++)
                    {
                        if (i > 0)
                        {
                            fileName += i;
                            filePath = System.IO.Path.GetDirectoryName(filePath) + @"\" + fileName + fileExt;
                        }
                        using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                        {
                            
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                BitmapEncoder encoder = new BmpBitmapEncoder();
                                if (fileExt.ToUpper().Equals(".JPG"))
                                {
                                    encoder = new JpegBitmapEncoder();
                                }
                                encoder.Frames.Add(BitmapFrame.Create(_phosImageList[i]));
                                encoder.Save(fileStream);
                            });
                            
                        }

                    }
                    
                    
                }

                e.Result = true;

            }
            catch (Exception ex)
            {
                e.Result = false;
            }
            finally
            {

            }

        }

        void SaveCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            string message = "Not saved";

            if ((bool)e.Result == true)
            {
                message = "Saved";
            }

            MessageBox.Show(message, "Complete");

            var ts = _timestamps.Pop();
            var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
            _statusVM.Messages.Remove(sm);
            _statusVM.Busy--;
        }

    }
}
