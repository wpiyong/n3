using N3Imager.View;
using N3Imager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ViewModelLib;
using System.ComponentModel;
using ImageProcessorLib;
using System.IO;

namespace N3Imager.ViewModel
{
    public enum ResultsType
    {
        WHITE_LIGHT = 0,
        N3_FL,
        PHOS,
        DP_FL,
        DP_FL2,
        DARK_BG
    }

    public class Result
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
    }

    class AnalyzerViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;

        List<System.Drawing.Bitmap> imgs_White_Light = new List<System.Drawing.Bitmap>();
        List<System.Drawing.Bitmap> imgs_N3_FL = new List<System.Drawing.Bitmap>();
        List<System.Drawing.Bitmap> imgs_PHOS = new List<System.Drawing.Bitmap>();
        List<System.Drawing.Bitmap> imgs_DP_FL = new List<System.Drawing.Bitmap>();
        List<System.Drawing.Bitmap> imgs_DP_FL2 = new List<System.Drawing.Bitmap>();
        List<System.Drawing.Bitmap> imgs_DARK_BG = new List<System.Drawing.Bitmap>();

        List<System.Drawing.Bitmap> currImgList = null;

        System.Windows.Shapes.Rectangle tmpRect = new System.Windows.Shapes.Rectangle();
        TextBlock tmpText = new TextBlock();
        System.Windows.Point currentPoint;
        double rectW, rectH;
        List<Rectangle> RectMarkers = new List<Rectangle>();
        List<TextBlock> TextMarkers = new List<TextBlock>();

        List<string> Descriptions = new List<string>();
        List<string> Comments = new List<string>();
        List<List<N3Results>> N3ResultsListList = new List<List<N3Results>>();

        public AnalyzerViewModel(StatusViewModel svm)
        {
            base.DisplayName = "AnalyzerViewModel";
            _statusVM = svm;

            MeasurementTypes = new ObservableCollection<string>() { "WHITE_LIGHT", "N3_FL", "PHOS", "DP_FL", "DP_FL2", "DARK_BG" };
            ResultList = new ObservableCollection<Result>();

            CommandPreviousItem = new RelayCommand(param => PreviousItem());
            CommandNextItem = new RelayCommand(param => NextItem());
            CommandAddMask = new RelayCommand(param => AddMask());
            CommandRemoveMasks = new RelayCommand(param => RemoveMasks());
            CommandAnalyze = new RelayCommand(param => Analyze());
            CommandRemoveLastOne = new RelayCommand(param => RemoveLastOne());
            CommandSave = new RelayCommand(param => Save());
        }

        public RelayCommand CommandAddMask { get; set; }
        public RelayCommand CommandRemoveMasks { get; set; }
        public RelayCommand CommandRemoveLastOne { get; set; }
        public RelayCommand CommandAnalyze { get; set; }
        public RelayCommand CommandPreviousItem { get; set; }
        public RelayCommand CommandNextItem { get; set; }
        public RelayCommand CommandSave { get; set; }

        #region properties

        public ObservableCollection<string> MeasurementTypes { get; }
        public ObservableCollection<Result> ResultList { get; set; }

        public int MaskCounts
        {
            get
            {
                return RectMarkers.Count;
            }
        }

        string _measurementType;
        public string MeasurementType
        {
            get
            {
                return _measurementType;
            }
            set
            {
                _measurementType = value;
                OnPropertyChanged("MeasurementType");
                MeasurementTypeChangedHandler(_measurementType);
            }
        }

        BitmapSource _currImage;
        public BitmapSource CurrImage
        {
            get
            {
                return _currImage;
            }
            set
            {
                _currImage = value;
                OnPropertyChanged("CurrImage");
            }
        }

        double _width;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged("Width");
                }
            }
        }

        double _height;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged("Height");
                }
            }
        }

        int _numItems;
        public int NumItems
        {
            get
            {
                return _numItems;
            }
            set
            {
                _numItems = value;
                OnPropertyChanged("NumItems");
            }
        }

        int _currentItem;
        public int CurrentItem
        {
            get
            {
                return _currentItem;
            }
            set
            {
                _currentItem = value;
                OnPropertyChanged("CurrentItem");
            }
        }

        #endregion

        void Save()
        {
            try
            {
                var root = GlobalVariables.globalSettings.SaveFolderPath + @"\" + GlobalVariables.globalSettings.SaveFolderName;
                System.IO.Directory.CreateDirectory(root);

                string fileName = "results.csv";
                var filePath = root + @"\" + fileName;

                // hsl data saving for debugging
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}",
                        "Sample#", "Description", "Comment",
                        "FL_H", "FL_S", "FL_L", "Time(ms)",
                        "PHOS_H", "PHOS_S", "PHOS_L", "PHOS_delay", "", "", "", "",
                        "UV_H", "UV_S", "UV_L", "Time(ms)",
                        "UV239_H", "UV239_S", "UV239_L", "Time(ms)");
                    writer.WriteLine(line);
                    if (N3ResultsListList.Count != ResultList.Count)
                    {
                        MessageBox.Show("Diamonds counts error.", "Error");
                        return;
                    }
                    for (int i = 0; i < N3ResultsListList.Count; i++)
                    {
                        Result r = ResultList[i];
                        line = string.Format("{0},{1},{2},", r.Index.ToString(), r.Description, r.Comment);

                        //FL
                        List<N3Results> item = N3ResultsListList[i];
                        N3Results elem = item[0];
                        line += string.Format("{0},{1},{2},{3},", elem.h.ToString(), elem.s.ToString(), elem.l.ToString(), elem.time.ToString());

                        //PHOS
                        {
                            elem = item[1];
                            List<double> delays = elem.delays;

                            line += string.Format("{0},{1},{2},{3},{4},{5},{6},{7},",
                                elem.h.ToString(), elem.s.ToString(), elem.l.ToString(), delays[0].ToString(), delays[1].ToString(), delays[2].ToString(), delays[3].ToString(), delays[4].ToString());
                        }

                        //UV
                        elem = item[2];
                        line += string.Format("{0},{1},{2},{3},", elem.h.ToString(), elem.s.ToString(), elem.l.ToString(), elem.time.ToString());

                        //UV2
                        elem = item[3];
                        line += string.Format("{0},{1},{2},{3}", elem.h.ToString(), elem.s.ToString(), elem.l.ToString(), elem.time.ToString());

                        writer.WriteLine(line);
                        writer.Flush();
                    }
                }
                MessageBox.Show("Save finished");
            } catch(Exception ex)
            {
                MessageBox.Show("Error saving debug info: " + ex.Message, "Error");
            }

        }

        void Analyze()
        {
            if(RectMarkers.Count == 0 && tmpRect.Width == 0)
            {
                MessageBox.Show("Please create mask for analyzing.", "Warning");
                return;
            }

            if (imgs_White_Light.Count == 0 || imgs_N3_FL.Count == 0 ||
                imgs_PHOS.Count == 0 || imgs_DP_FL.Count == 0 ||
                imgs_DP_FL2.Count == 0 || imgs_DARK_BG.Count == 0)
            {
                MessageBox.Show("Please finish all the image captures.", "Warning");
                return;
            }

            Console.WriteLine("Analyzing...");
            //double x = Canvas.GetLeft(RectMarkers[0]);
            //double y = Canvas.GetTop(RectMarkers[0]);
            //Console.WriteLine("Rect location: {0}, {1}", x, y);

            Descriptions.Clear();
            Comments.Clear();
            ResultList.Clear();
            N3ResultsListList.Clear();

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += Analyzing_doWork;
            bw.RunWorkerCompleted += Analyzing_completed;
            bw.RunWorkerAsync();
        }

        void Analyzing_doWork(object sender, DoWorkEventArgs e)
        {
            ImageAnalyzer analyzer = new ImageAnalyzer_N3Imager();
            List<System.Drawing.Bitmap> imgs = imgs_N3_FL.Concat(imgs_PHOS).ToList();
            imgs.AddRange(imgs_DP_FL);
            imgs.AddRange(imgs_DP_FL2);

            List<System.Drawing.Rectangle> rects = new List<System.Drawing.Rectangle>();

            for (int i = 0; i < RectMarkers.Count; i++)
            {
                Rectangle rect = RectMarkers[i];
                double x = 0;
                Application.Current.Dispatcher.Invoke(() => x = Canvas.GetLeft(rect));
                double y = 0;
                Application.Current.Dispatcher.Invoke(() => y = Canvas.GetTop(rect));

                System.Drawing.Rectangle r = new System.Drawing.Rectangle();
                Application.Current.Dispatcher.Invoke(()=>r = new System.Drawing.Rectangle((int)(x * 0.5), (int)(y * 0.5), (int)(rect.Width * 0.5), (int)(rect.Height * 0.5)));
                rects.Add(r);
            }

            if(rects.Count == 0)
            {
                double x = 0;
                Application.Current.Dispatcher.Invoke(() => x = Canvas.GetLeft(tmpRect));
                double y = 0;
                Application.Current.Dispatcher.Invoke(() => y = Canvas.GetTop(tmpRect));

                System.Drawing.Rectangle r = new System.Drawing.Rectangle();
                Application.Current.Dispatcher.Invoke(() => r = new System.Drawing.Rectangle((int)(x * 0.5), (int)(y * 0.5), (int)(tmpRect.Width * 0.5), (int)(tmpRect.Height * 0.5)));
                rects.Add(r);
            }

            e.Result = analyzer.Get_Description(ref imgs, ref imgs_DARK_BG, ref rects, out Descriptions, out Comments, out N3ResultsListList);
        }

        void Analyzing_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((bool)e.Result == true)
            {
                Console.WriteLine("analyzing completed");
                for (int i = 0; i < Descriptions.Count; i++)
                {
                    Result r = new Result() { Index = i+1, Comment = Comments[i], Description = Descriptions[i] };
                    ResultList.Add(r);
                }

                // todo: update the mask color
                for(int i = 0; i < ResultList.Count; i++)
                {
                    Result r = ResultList[i];
                    if (RectMarkers.Count == 0)
                    {
                        // tmpRect
                        if (r.Comment == "Natural")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Cyan);
                        } else if (r.Comment == "Refer")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Magenta);
                        }
                        else if (r.Comment == "HPHT")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else if (r.Comment == "CVD")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.LawnGreen);
                        }
                        else if (r.Comment == "CZ")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Salmon);
                        }
                        else if (r.Comment == "Natural(DeepUV)")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Brown);
                        }
                        else if (r.Comment == "Natural(shortPHOS)")
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Red);
                        } else
                        {
                            tmpRect.Fill = new SolidColorBrush(Colors.Black);
                        }
                    } else
                    {
                        Rectangle rec = RectMarkers[i];
                        if (r.Comment == "Natural")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Cyan);
                        }
                        else if (r.Comment == "Refer")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Magenta);
                        }
                        else if (r.Comment == "HPHT")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else if (r.Comment == "CVD")
                        {
                            rec.Fill = new SolidColorBrush(Colors.LawnGreen);
                        }
                        else if (r.Comment == "CZ")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Salmon);
                        }
                        else if (r.Comment == "Natural(DeepUV)")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Brown);
                        }
                        else if (r.Comment == "Natural(shortPHOS)")
                        {
                            rec.Fill = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            //rec.Fill = new SolidColorBrush(Colors.Black);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("analyzing failed!");
            }
        }

        void AddMask()
        {
            if(tmpRect.ActualWidth != 0 && tmpRect.ActualHeight != 0)
            {
                RectMarkers.Add(tmpRect);

                MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                double x = Canvas.GetLeft(tmpRect);
                double y = Canvas.GetTop(tmpRect);
                tmpText = DrawCanvas.Text(x, y, (TextMarkers.Count + 1).ToString(), true, Brushes.Yellow, mv.CanvasResult, false);
                TextMarkers.Add(tmpText);

                tmpRect = new Rectangle();
                tmpText = new TextBlock();

                OnPropertyChanged("MaskCounts");
            }
        }

        public void AddResults(ref List<System.Drawing.Bitmap> imgs, ResultsType resType)
        {
            if(resType == ResultsType.WHITE_LIGHT)
            {
                imgs_White_Light = imgs;
            }
            else if (resType == ResultsType.N3_FL)
            {
                imgs_N3_FL = imgs;
            }
            else if (resType == ResultsType.PHOS)
            {
                imgs_PHOS = imgs;
            }
            else if (resType == ResultsType.DP_FL)
            {
                imgs_DP_FL = imgs;
            }
            else if (resType == ResultsType.DP_FL2)
            {
                imgs_DP_FL2 = imgs;
            }
            else if (resType == ResultsType.DARK_BG)
            {
                imgs_DARK_BG = imgs;
            }
        }

        public void InitCurrentImageListSelection()
        {
            if(currImgList == null)
            {
                MeasurementType = MeasurementTypes[0];
            }
        }

        void PreviousItem()
        {
            if (CurrentItem > 1)
            {
                CurrentItem--;
                CurrImage = Convert(currImgList[CurrentItem - 1]);
            }
        }

        void NextItem()
        {
            if (CurrentItem < NumItems)
            {
                CurrentItem++;
                CurrImage = Convert(currImgList[CurrentItem - 1]);
            }
        }

        public void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition((Canvas)sender);
                Console.WriteLine("selected point: {0}, {1}", currentPoint.X, currentPoint.Y);
            }
        }

        public void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && currentPoint.X != 0 && currentPoint.Y != 0 )
            {
                System.Windows.Point pos = e.GetPosition((Canvas)sender);
                rectW = pos.X - currentPoint.X;
                rectH = pos.Y - currentPoint.Y;
            }
        }

        public void MouseUpHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (rectW != 0 && rectH != 0 && currentPoint.X != 0 && currentPoint.Y != 0)
                {
                    if (rectW < 0)
                    {
                        currentPoint.X = currentPoint.X + rectW;
                        rectW = -rectW;
                    }

                    if (rectH < 0)
                    {
                        currentPoint.Y = currentPoint.Y + rectH;
                        rectH = -rectH;
                    }
                    RemoveTempMarker();
                    MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    tmpRect = DrawCanvas.Rect(currentPoint.X, currentPoint.Y, (int)rectW, (int)rectH, Brushes.Red, mv.CanvasResult, 0.3);
                    
                    currentPoint = default(Point);
                    rectW = 0;
                    rectH = 0;
                }
            }
        }

        void RemoveMasks(int index = 0)
        {
            if(index == 0)
            {
                ResultList.Clear();
            }
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (index == 0)
            {
                mv.CanvasResult.Children.Remove(tmpRect);
            }
            while (index < RectMarkers.Count)
            {
                mv.CanvasResult.Children.Remove(RectMarkers[RectMarkers.Count - 1]);
                RectMarkers.RemoveAt(RectMarkers.Count - 1);

                mv.CanvasResult.Children.Remove(TextMarkers[TextMarkers.Count - 1]);
                TextMarkers.RemoveAt(TextMarkers.Count - 1);

                OnPropertyChanged("MaskCounts");
            }
        }
        
        void RemoveLastOne()
        {
            if (RectMarkers.Count > 0)
            {
                int index = RectMarkers.Count - 1;
                RemoveMasks(index);
            }
        }

        void RemoveTempMarker()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mv.CanvasResult.Children.Remove(tmpRect);
            mv.CanvasResult.Children.Remove(tmpText);
        }

        void MeasurementTypeChangedHandler(string measType)
        {
            if(measType == ResultsType.WHITE_LIGHT.ToString())
            {
                currImgList = imgs_White_Light;
            } else if (measType == ResultsType.N3_FL.ToString())
            {
                currImgList = imgs_N3_FL;
            }
            else if (measType == ResultsType.PHOS.ToString())
            {
                currImgList = imgs_PHOS;
            }
            else if (measType == ResultsType.DP_FL.ToString())
            {
                currImgList = imgs_DP_FL;
            }
            else if (measType == ResultsType.DP_FL2.ToString())
            {
                currImgList = imgs_DP_FL2;
            }
            else if (measType == ResultsType.DARK_BG.ToString())
            {
                currImgList = imgs_DARK_BG;
            }

            if (currImgList != null)
            {
                NumItems = currImgList.Count;
                CurrentItem = 1;
                CurrImage = Convert(currImgList[0]);
                if(measType == ResultsType.WHITE_LIGHT.ToString() && Width == 0)
                {
                    Width = CurrImage.PixelWidth;
                    Height = CurrImage.PixelHeight;
                }
            }
            else
            {
                NumItems = 0;
                CurrentItem = 0;
                CurrImage = null;
            }
        }

        BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        #region dispose

        private bool _disposed = false;

        protected override void OnDispose()
        {
            ViewModelDispose(true);
            GC.SuppressFinalize(this);
        }

        void ViewModelDispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }
                _disposed = true;
            }
        }

        #endregion
    }
}
