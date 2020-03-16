using N3Imager.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ViewModelLib;

namespace N3Imager.ViewModel
{
    public delegate void Callback(object sender, object  result);

    class MeasurementViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;
        Stack<DateTime> _timestamps = new Stack<DateTime>();
        bool _camPhosActive = false;
        CamPhosResults _camPhosResults;

        MainWindowViewModel _mainVM;
        CameraViewModel _cameraVM;
        PhosphorescenceViewModel _resultVM;
        AnalyzerViewModel _analyzerVM;
        enum MEASUREMENT_MODE { NONE = -1, WHITE_LIGHT, N3_FL, PHOS, DP_FL, DP_FL2, DARK_BG }
        MEASUREMENT_MODE _measurementMode = MEASUREMENT_MODE.NONE;

        string _saveFolderName = "";

        int indexUV;

        List<System.Drawing.Bitmap> imgs_White_Light = null;
        List<System.Drawing.Bitmap> imgs_N3_FL = null;
        List<System.Drawing.Bitmap> imgs_PHOS = null;
        List<System.Drawing.Bitmap> imgs_DP_FL = null;
        List<System.Drawing.Bitmap> imgs_DP_FL2 = null;
        List<System.Drawing.Bitmap> imgs_DARK_BG = null;

        public MeasurementViewModel(MainWindowViewModel mvm, 
            StatusViewModel svm, CameraViewModel cvm, PhosphorescenceViewModel pvm, AnalyzerViewModel avm)
        {
            base.DisplayName = "MeasurementViewModel";
            _mainVM = mvm;
            _statusVM = svm;
            _cameraVM = cvm;
            _resultVM = pvm;
            _analyzerVM = avm;

            ArduinoConnected = false;

            CommandEnd = new RelayCommand(param => End(), cc => _camPhosActive);
            CommandStartDarkBG = new RelayCommand(param => StartDarkBG(), cc => !_camPhosActive);
            CommandStartLed = new RelayCommand(param => StartLed(), cc => !_camPhosActive);
            CommandStartN3Fluorescence = new RelayCommand(param => StartN3Fluorescence(), cc => !_camPhosActive);
            CommandStartDeepUVFluorescence = new RelayCommand(param => StartDeepUVFluorescence(int.Parse(param.ToString())), cc => !_camPhosActive);
            CommandStartPhosphorescence = new RelayCommand(param => StartPhosphorescence(), cc => ArduinoConnected && !_camPhosActive);
            CommandSaveSettings = new RelayCommand(param => ShowSaveSettings());

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += ConnectToArduino;
            bw.RunWorkerCompleted += ConnectToArduinoCompleted;
            bw.RunWorkerAsync();
        }

        public RelayCommand CommandEnd { get; set; }
        public RelayCommand CommandStartDarkBG { get; set; }
        public RelayCommand CommandStartLed { get; set; }
        public RelayCommand CommandStartN3Fluorescence { get; set; }
        public RelayCommand CommandStartDeepUVFluorescence { get; set; }
        public RelayCommand CommandStartPhosphorescence { get; set; }
        public RelayCommand CommandSaveSettings { get; set; }



        #region Properties
        bool _ArduinoConnected;
        public bool ArduinoConnected
        {
            get { return _ArduinoConnected; }
            set
            {
                _ArduinoConnected = value;
                OnPropertyChanged("ArduinoConnected");
                Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));

            }
        }
        #endregion


        #region connect_arduino

        void ConnectToArduino(object sender, DoWorkEventArgs e)
        {
            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Trying to connect to arduino..." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);
            ArduinoConnected = false;

            e.Result = null;

            try
            {
                if (Arduino.Connect())
                {
                    e.Result = "Arduino";
                }
            }
            catch
            {
                e.Result = null;
            }
        }

        void ConnectToArduinoCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ts = _timestamps.Pop();
            var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
            _statusVM.Messages.Remove(sm);
            _statusVM.Busy--;

            if (e.Result != null)
            {
                _statusVM.Messages.Add(new StatusMessage { Timestamp = DateTime.Now, Message = "Connected to " + e.Result.ToString() });
                ArduinoConnected = true;
            }
            else
            {
                var res = MessageBox.Show("Failed to connect to Arduino", "Arduino connection error, retry?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += ConnectToArduino;
                    bw.RunWorkerCompleted += ConnectToArduinoCompleted;
                    bw.RunWorkerAsync();
                }
                else
                {
                    var sts = new StatusMessage
                    {
                        Timestamp = DateTime.Now,
                        Message = "Arduino connect error!"
                    };
                    _statusVM.Messages.Add(sts);
                }
            }
        }

        #endregion

        void ShowSaveSettings()
        {
            var saveDlg = new View.SaveFolderWindow(_saveFolderName, "");
            saveDlg.ShowDialog();
            if (!saveDlg.SaveFolderNameSet)
                return;
            _saveFolderName = saveDlg.SaveFolderName;
            GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
        }

        void StartLed()
        {
            if (CheckSaveFileExists("_WL.bmp"))
            {
                var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_WL.bmp");
                saveDlg.ShowDialog();
                if (!saveDlg.SaveFolderNameSet)
                    return;
                _saveFolderName = saveDlg.SaveFolderName;
                GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
            }

            var res = MessageBox.Show("Please confirm that the white light is on and then press OK to continue.", "White Light On",
                MessageBoxButton.OKCancel);

            if (res == MessageBoxResult.Cancel)
                return;

            Callback WhenMeasurementEnds = WhiteLightCompleteCallback;
            _camPhosResults = new CamPhosResults(0, new List<PtGreyCameraImage>());
            string error = "";

            List<double> shutterTimes = Properties.Settings.Default.WhiteLightMeasurementShutterTimes.Split(',').
                Select(r => Convert.ToDouble(r)).ToList();
            
            _camPhosActive = _cameraVM.StartWhiteLight(shutterTimes, Properties.Settings.Default.WhiteLightMeasurementGain,
                    WhenMeasurementEnds, out error);

            if (!_camPhosActive)
            {
                MessageBox.Show(error, "Image Capture not started");
                return;
            }

            _measurementMode = MEASUREMENT_MODE.WHITE_LIGHT;

            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture is active." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);

        }


        void WhiteLightCompleteCallback(object sender, object result)
        {
            _measurementMode = MEASUREMENT_MODE.NONE;
            MessageBox.Show("Please turn off the white light", "White Light Off");

            if (Object.ReferenceEquals(sender.GetType(), _cameraVM.GetType()))
            {
                _camPhosActive = false;
                _camPhosResults = (CamPhosResults)result;
            }

            if (!_camPhosActive)
                ShowWhiteLightResults();
        }

        void ShowWhiteLightResults()
        {
            if (_camPhosResults.Images.Count > 0)
            {
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;

                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture completed." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);

                imgs_White_Light = _camPhosResults.Images.Select(p => p.Image).ToList();
                SaveData(imgs_White_Light, "_WL.bmp");

                _analyzerVM.AddResults(ref imgs_White_Light, ResultsType.WHITE_LIGHT);
                _resultVM.LoadCameraImages(_camPhosResults);
                _mainVM.TabIndex = 1;

            }
            else
            {
                MessageBox.Show("Failed to collect all data", "Error");
            }

        }

        void StartDarkBG()
        {
            if (CheckSaveFileExists("_DB.bmp"))
            {
                var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_DB.bmp");
                saveDlg.ShowDialog();
                if (!saveDlg.SaveFolderNameSet)
                    return;
                _saveFolderName = saveDlg.SaveFolderName;
                GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
            }

            var res = MessageBox.Show("Please confirm that the all light is off and then press OK to continue.", "Light Off",
                MessageBoxButton.OKCancel);

            if (res == MessageBoxResult.Cancel)
                return;

            Callback WhenMeasurementEnds = DarkBGCompleteCallback;
            _camPhosResults = new CamPhosResults(0, new List<PtGreyCameraImage>());
            string error = "";

            //TODO: property settings
            List<List<double>> shutterTimesDarkBG = new List<List<double>>();
            
            List<double> shutterTimesFL = Properties.Settings.Default.N3FlMeasurementShutterTimes.Split(',').
                Select(r => Convert.ToDouble(r)).ToList();
            List<double> shutterTimesDP = Properties.Settings.Default.DeepUVFlMeasurementShutterTimes.Split(',').
                Select(r => Convert.ToDouble(r)).ToList();

            shutterTimesDarkBG.Add(shutterTimesFL);
            shutterTimesDarkBG.Add(shutterTimesDP);

            List<double> gainsDarkBG = new List<double>();
            gainsDarkBG.Add(Properties.Settings.Default.N3FlMeasurementGain);
            gainsDarkBG.Add(Properties.Settings.Default.PhosMeasurementGain);

            _camPhosActive = _cameraVM.StartDarkBG(shutterTimesDarkBG, gainsDarkBG,
                    WhenMeasurementEnds, out error);

            if (!_camPhosActive)
            {
                MessageBox.Show(error, "Image Capture not started");
                return;
            }

            _measurementMode = MEASUREMENT_MODE.DARK_BG;

            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture is active." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);

        }


        void DarkBGCompleteCallback(object sender, object result)
        {
            _measurementMode = MEASUREMENT_MODE.NONE;

            if (Object.ReferenceEquals(sender.GetType(), _cameraVM.GetType()))
            {
                _camPhosActive = false;
                _camPhosResults = (CamPhosResults)result;
            }

            if (!_camPhosActive)
                ShowDarkBGResults();
        }

        void ShowDarkBGResults()
        {
            if (_camPhosResults.Images.Count > 0)
            {
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;

                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture completed." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);

                imgs_DARK_BG = _camPhosResults.Images.Select(p => p.Image).ToList();
                SaveData(imgs_DARK_BG, "_DB.bmp");

                _analyzerVM.AddResults(ref imgs_DARK_BG, ResultsType.DARK_BG);
                _resultVM.LoadCameraImages(_camPhosResults);
                _mainVM.TabIndex = 1;

            }
            else
            {
                MessageBox.Show("Failed to collect all data", "Error");
            }

        }


        void StartN3Fluorescence()
        {
            if (CheckSaveFileExists("_FL.bmp"))
            {
                var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_FL.bmp");
                saveDlg.ShowDialog();
                if (!saveDlg.SaveFolderNameSet)
                    return;
                _saveFolderName = saveDlg.SaveFolderName;
                GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
            }

            var res = MessageBox.Show("Please confirm that the long wave UV is on and then press OK to continue.", "Long Wave UV On",
                MessageBoxButton.OKCancel);

            if (res == MessageBoxResult.Cancel)
                return;

            Callback WhenMeasurementEnds = N3FluorescenceCompleteCallback;
            _camPhosResults = new CamPhosResults(0, new List<PtGreyCameraImage>());
            string error = "";

            List<double> shutterTimes = Properties.Settings.Default.N3FlMeasurementShutterTimes.Split(',').
                Select(r => Convert.ToDouble(r)).ToList();
            
            _camPhosActive = _cameraVM.StartN3Fluorescence(shutterTimes, Properties.Settings.Default.N3FlMeasurementGain,
                    WhenMeasurementEnds, out error);

            if (!_camPhosActive)
            {
                MessageBox.Show(error, "Image Capture not started");
                return;
            }

            _measurementMode = MEASUREMENT_MODE.N3_FL;

            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture is active." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);
        }

        void N3FluorescenceCompleteCallback(object sender, object result)
        {
            _measurementMode = MEASUREMENT_MODE.NONE;
            MessageBox.Show("Please turn off the long wave UV", "Long Wave UV Off"); 

            if (Object.ReferenceEquals(sender.GetType(), _cameraVM.GetType()))
            {
                _camPhosActive = false;
                _camPhosResults = (CamPhosResults)result;
            }

            if (!_camPhosActive)
                ShowN3FluorescenceResults();
        }

        void ShowN3FluorescenceResults()
        {
            if (_camPhosResults.Images.Count > 0)
            {
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;

                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture completed." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);

                imgs_N3_FL = _camPhosResults.Images.Select(p => p.Image).ToList();
                SaveData(imgs_N3_FL, "_FL.bmp");

                _analyzerVM.AddResults(ref imgs_N3_FL, ResultsType.N3_FL);
                _resultVM.LoadCameraImages(_camPhosResults);
                _mainVM.TabIndex = 1;

            }
            else
            {
                MessageBox.Show("Failed to collect all data", "Error");
            }

        }

        void StartDeepUVFluorescence(int index)
        {
            indexUV = index;
            if (index == 0)
            {
                if (CheckSaveFileExists("_UV.bmp"))
                {
                    var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_UV.bmp");
                    saveDlg.ShowDialog();
                    if (!saveDlg.SaveFolderNameSet)
                        return;
                    _saveFolderName = saveDlg.SaveFolderName;
                    GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
                }
            } 
            else if(index == 1)
            {
                if (CheckSaveFileExists("_UV2.bmp"))
                {
                    var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_UV2.bmp");
                    saveDlg.ShowDialog();
                    if (!saveDlg.SaveFolderNameSet)
                        return;
                    _saveFolderName = saveDlg.SaveFolderName;
                }
            }

            //var res = MessageBox.Show("Please confirm that the deep UV is on and then press OK to continue.", "Deep UV On",
            //    MessageBoxButton.OKCancel);

            //if (res == MessageBoxResult.Cancel)
            //    return;
            // TODO: arduino light on
            if (!Arduino.FluorescenceOn())
            {
                _cameraVM.AbortDeepUVFluorescence();

                MessageBox.Show("Serial Port Error... You may have to reset the camera.", "Aborting...");

                return;
            }

            Callback WhenMeasurementEnds = DeepUVFluorescenceCompleteCallback;
            _camPhosResults = new CamPhosResults(0, new List<PtGreyCameraImage>());
            string error = "";
            // TODO: settings for deep uv
            List<double> shutterTimes = Properties.Settings.Default.DeepUVFlMeasurementShutterTimes.Split(',').
                Select(r => Convert.ToDouble(r)).ToList();

            _camPhosActive = _cameraVM.StartDeepUVFluorescence(shutterTimes, Properties.Settings.Default.DeepUVFlMeasurementGain, 
                    WhenMeasurementEnds, out error);

            if (!_camPhosActive)
            {
                MessageBox.Show(error, "Image Capture not started");
                return;
            }

            if (index == 0)
            {
                _measurementMode = MEASUREMENT_MODE.DP_FL;
            }
            else if (index == 1)
            {
                _measurementMode = MEASUREMENT_MODE.DP_FL2;
            }

            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture is active." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);
        }

        void DeepUVFluorescenceCompleteCallback(object sender, object result)
        {
            _measurementMode = MEASUREMENT_MODE.NONE;
            //MessageBox.Show("Please turn off the Deep UV", "Deep Off");

            // TODO: arduino light off
            if (!Arduino.End())
            {
                MessageBox.Show("Serial Port Error... You may have to reset the camera for next measurement.", "...");
            }

            if (Object.ReferenceEquals(sender.GetType(), _cameraVM.GetType()))
            {
                _camPhosActive = false;
                _camPhosResults = (CamPhosResults)result;
            }

            if (!_camPhosActive)
                ShowDeepUVFluorescenceResults();
        }

        void ShowDeepUVFluorescenceResults()
        {
            if (_camPhosResults.Images.Count > 0)
            {
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;

                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Image capture completed." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);

                if (indexUV == 0)
                {
                    imgs_DP_FL = _camPhosResults.Images.Select(p => p.Image).ToList();
                    SaveData(imgs_DP_FL, "_UV.bmp");
                    _analyzerVM.AddResults(ref imgs_DP_FL, ResultsType.DP_FL);
                } else if(indexUV == 1)
                {
                    imgs_DP_FL2 = _camPhosResults.Images.Select(p => p.Image).ToList();
                    SaveData(imgs_DP_FL2, "_UV2.bmp");
                    _analyzerVM.AddResults(ref imgs_DP_FL2, ResultsType.DP_FL2);
                }
                _resultVM.LoadCameraImages(_camPhosResults);
                _mainVM.TabIndex = 1;

            }
            else
            {
                MessageBox.Show("Failed to collect all data", "Error");
            }

        }


        void StartPhosphorescence()
        {
            if (CheckSaveFileExists("_PHOS.bmp"))
            {
                var saveDlg = new View.SaveFolderWindow(_saveFolderName, "_PHOS.bmp");
                saveDlg.ShowDialog();
                if (!saveDlg.SaveFolderNameSet)
                    return;
                _saveFolderName = saveDlg.SaveFolderName;
                GlobalVariables.globalSettings.SaveFolderName = saveDlg.SaveFolderName;
            }
            
            string error = "";
            Callback WhenMeasurementEnds = PhosphorescenceMeasurementCompleteCallback;
            _camPhosResults = new CamPhosResults(0, new List<PtGreyCameraImage>());

            _camPhosActive = _cameraVM.StartPhosphorescenceMeasurement(Properties.Settings.Default.PhosMeasurementStartDelayUs,
                Properties.Settings.Default.PhosMeasurementImageCount,
                Properties.Settings.Default.PhosMeasurementShutterTime, Properties.Settings.Default.PhosMeasurementGain,
                WhenMeasurementEnds, out error);

            if (!_camPhosActive)
            {
                MessageBox.Show(error, "Phosphorescence measurement not started");
                return;
            }

            if (!Arduino.PhosphorescenceOn())
            {
                _cameraVM.AbortPhosMeasurement();

                MessageBox.Show("Serial Port Error... You may have to reset the camera.", "Aborting...");
            }
            else
            {
                _measurementMode = MEASUREMENT_MODE.PHOS;
                _statusVM.Busy++;
                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Phosphorescence measurement is active." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);
            }

        }

        void PhosphorescenceMeasurementCompleteCallback(object sender, object result)
        {
            _measurementMode = MEASUREMENT_MODE.NONE;
            if (Object.ReferenceEquals(sender.GetType(), _cameraVM.GetType()))
            {
                _camPhosActive = false;
                _camPhosResults = (CamPhosResults)result;
            }
            
            if (!_camPhosActive)
                ShowPhosResults();
        }

        void ShowPhosResults()
        {
            if (_camPhosResults.Images.Count > 0)
            {
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;

                DateTime timestamp = DateTime.Now;
                var sts = new StatusMessage { Timestamp = timestamp, Message = "Phosphorescence measurement completed." };
                _statusVM.Messages.Add(sts);
                _timestamps.Push(timestamp);

                imgs_PHOS = _camPhosResults.Images.Select(p => p.Image).ToList();
                SaveData(imgs_PHOS, "_PHOS.bmp");

                _analyzerVM.AddResults(ref imgs_PHOS, ResultsType.PHOS);
                _resultVM.LoadCameraImages(_camPhosResults);
                _mainVM.TabIndex = 1;
                
            }
            else
            {
                MessageBox.Show("Failed to collect all data", "Error");
            }

            if (!Arduino.End())
            {
                MessageBox.Show("Serial Port", "Error");
            }
        }

        void End()
        {
            if (_camPhosActive)
            {
                switch(_measurementMode)
                {
                    case MEASUREMENT_MODE.WHITE_LIGHT:
                        _cameraVM.AbortWhiteLight();
                        break;
                    case MEASUREMENT_MODE.N3_FL:
                        _cameraVM.AbortN3Fluorescence();
                        break;
                    case MEASUREMENT_MODE.PHOS:
                        _cameraVM.AbortPhosMeasurement();
                        break;
                    default:
                        break;
                }
                _measurementMode = MEASUREMENT_MODE.NONE;
                _camPhosActive = false;
                Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
                var ts = _timestamps.Pop();
                var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
                _statusVM.Messages.Remove(sm);
                _statusVM.Busy--;
            }
        }

        bool CheckSaveFileExists(string ext)
        {
            if (_saveFolderName == "")
                return true;

            var root = GlobalVariables.globalSettings.SaveFolderPath + @"\" + _saveFolderName;
            if (System.IO.Directory.Exists(root))
            {
                string fileName = new System.IO.DirectoryInfo(root).Name;
                var fileNameSearchStr = fileName + "_*" + ext;
                System.IO.DirectoryInfo hdDirectoryInWhichToSearch =
                    new System.IO.DirectoryInfo(root);
                var filesInDir = hdDirectoryInWhichToSearch.GetFiles(fileNameSearchStr);
                return filesInDir.Count() > 0;
            }

            return false;

        }

        void SaveData(List<System.Drawing.Bitmap> images, string ext)
        {
            try
            {
                var root = GlobalVariables.globalSettings.SaveFolderPath + @"\" + _saveFolderName;
                System.IO.Directory.CreateDirectory(root);

                string fileName = new System.IO.DirectoryInfo(root).Name;
                var filePath = root + @"\" + fileName + "_";
               

                for (int i = 0; i < images.Count; i++)
                {
                    using (var bmp = new System.Drawing.Bitmap(images[i]))
                    {
                        bmp.Save(filePath + i + ext);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error saving images", "Auto save error");
            }
        }


        #region dispose

        private bool _disposed = false;

        void Close()
        {
            if (_ArduinoConnected)
                Arduino.End();
        }

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
                    Close();
                }
                _disposed = true;
            }
        }

        #endregion
    }
}
