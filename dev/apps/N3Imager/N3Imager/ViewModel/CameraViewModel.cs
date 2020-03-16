using N3Imager.Model;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ViewModelLib;

namespace N3Imager.ViewModel
{
    class CameraViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;
        Stack<DateTime> _timestamps = new Stack<DateTime>();

        PtGreyCamera _ptGreyCamera;
        ConcurrentQueue<PtGreyCameraImage> _ptGreyImageQueue = null;
        System.Threading.AutoResetEvent _ptGreyStopImageThreadEvent;
        bool _cameraConnected;
        uint _phosImagesToCollect;
        List<PtGreyCameraImage> _phosImages = new List<PtGreyCameraImage>();
        System.Threading.ManualResetEvent _phosMeasureCompletionEvent = new System.Threading.ManualResetEvent(false);
        ulong _phosStartTimestamp;

        BackgroundWorker bwCameraConnector = new BackgroundWorker();

        public CameraViewModel(StatusViewModel svm)
        {
            base.DisplayName = "CameraViewModel";
            _statusVM = svm;

            CommandResetSettings = new RelayCommand(param => ResetSettings());
            CommandSave = new RelayCommand(param => Save());

            _cameraConnected = false;
            _phosImagesToCollect = 0;
            bwCameraConnector.DoWork += ConnectCameraDoWork;
            bwCameraConnector.RunWorkerCompleted += ConnectCameraCompleted;
            bwCameraConnector.RunWorkerAsync();
        }

        public RelayCommand CommandResetSettings { get; set; }
        public RelayCommand CommandSave { get; set; }

        #region Properties

        object _cameraImageLock = new object();
        WriteableBitmap _cameraImage;
        public WriteableBitmap CameraImage
        {
            get
            {
                lock (_cameraImageLock)
                {
                    return _cameraImage;
                }
            }
            set
            {
                lock (_cameraImageLock)
                {
                    _cameraImage = value;
                    OnPropertyChanged("CameraImage");
                }
            }
           
        }

        public System.Drawing.Bitmap CameraBitmap
        {
            get { return BitmapFromWriteableBitmap(CameraImage); }
        }
        
        
        long _imageWidth, _imageHeight;
        public long ImageWidth
        {
            get { return _imageWidth; }
            set
            {
                _imageWidth = value;
                OnPropertyChanged("ImageWidth");
            }
        }
        public long ImageHeight
        {
            get { return _imageHeight; }
            set
            {
                _imageHeight = value;
                OnPropertyChanged("ImageHeight");
            }
        }

        double _shutter;
        public double Shutter
        {
            get
            {
                return _shutter;
            }
            set
            {
                if (SetShutterTime(value))
                {
                    _shutter = value;
                    Properties.Settings.Default.ShutterDefault = value;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("Shutter");
                }
            }
        }

        double _gain;
        public double Gain
        {
            get { return _gain; }
            set
            {
                if (_ptGreyCamera.SetGain(value))
                {
                    _gain = value;
                    Properties.Settings.Default.GainDefault = value;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("Gain");
                }
            }
        }

        double _frameRate;
        public double FrameRate
        {
            get { return _frameRate; }
            set
            {
                if (_ptGreyCamera.SetFrameRate(value))
                {
                    _frameRate = value;
                    OnPropertyChanged("FrameRate");
                }
            }
        }

        double _wbRed;
        public double WBred
        {
            get { return _wbRed; }
            set
            {
                if (_ptGreyCamera.SetWhiteBalanceRed(value))
                {
                    _wbRed = value;
                    
                    OnPropertyChanged("WBred");
                }
            }
        }

        double _wbBlue;
        public double WBblue
        {
            get { return _wbBlue; }
            set
            {
                if (_ptGreyCamera.SetWhiteBalanceBlue(value))
                {
                    _wbBlue = value;
                    
                    OnPropertyChanged("WBblue");
                }
            }
        }

        bool _ready;
        public bool Ready
        {
            get { return _ready; }
            set
            {
                _ready = value;
                OnPropertyChanged("Ready");
            }
        }

        #endregion

        #region default_settings

        readonly double DEFAULT_SHUTTER_TIME = 300;
        readonly double DEFAULT_GAIN = 1;
        readonly double DEFAULT_WB_RED = 1.11;
        readonly double DEFAULT_WB_BLUE = 2.24;

        void ResetSettings()
        {
            if (_ptGreyCamera.DefaultSettings())
            {
                Gain = DEFAULT_GAIN;
                Shutter = DEFAULT_SHUTTER_TIME;
                WBred = DEFAULT_WB_RED;
                WBblue = DEFAULT_WB_BLUE;

                _ptGreyCamera.StartVideo();
            }
            else
            {
                MessageBox.Show("Failed to restore camera settings", "Camera Error");
            }
        }

        #endregion

        #region connect_camera
        void ConnectCameraDoWork(object sender, DoWorkEventArgs e)
        {
            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Trying to connect to camera..." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);

            _ptGreyImageQueue = new ConcurrentQueue<PtGreyCameraImage>();
            _ptGreyCamera = new PtGreyCamera();
            string message;
            if (_ptGreyCamera.Open(_ptGreyImageQueue, out message))
            {
                _ptGreyStopImageThreadEvent = new System.Threading.AutoResetEvent(false);

                if (_ptGreyCamera.DefaultSettings())
                {
                    Gain = Properties.Settings.Default.GainDefault;
                    Shutter = Properties.Settings.Default.ShutterDefault;
                    WBred = Properties.Settings.Default.WBRed;
                    WBblue = Properties.Settings.Default.WBBlue;

                    _ptGreyCamera.StartVideo();
                    
                }
                else
                    message = "Error: Could not load default camera settings";

                e.Result = message;
            }
            else
            {
                e.Result = "Error: " + message;
            }

        }

        void ConnectCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ts = _timestamps.Pop();
            var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
            _statusVM.Messages.Remove(sm);
            _statusVM.Busy--;

            if (e.Result.ToString().Substring(0, 6) != "Error:")
            {
                Ready = true;
                
                _statusVM.Messages.Add(new StatusMessage { Timestamp = DateTime.Now, Message = "Connected to " + e.Result.ToString() });
                _cameraConnected = true;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += PtGreyImageReceiveDoWork;
                bw.WorkerReportsProgress = true;
                bw.ProgressChanged += PtGreyImageReceiveProgressChangedEvent;
                bw.RunWorkerAsync();

            }
            else
            {
                var res = MessageBox.Show(e.Result.ToString(), "Camera connection error, retry?" , MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    bwCameraConnector.RunWorkerAsync();
                }
                else
                {
                    var sts = new StatusMessage { Timestamp = DateTime.Now, Message = "Camera connect error!" };
                    _statusVM.Messages.Add(sts);
                }
            }
            
            
        }
        #endregion

        #region camera image callback


        void PtGreyImageReceiveDoWork(object sender, DoWorkEventArgs e)
        {
            PtGreyCameraImage item;
            while (!_ptGreyStopImageThreadEvent.WaitOne(1))
            {
                if (_ptGreyImageQueue.TryPeek(out item))
                    ((BackgroundWorker)sender).ReportProgress(0);
            }
        }

        void PtGreyImageReceiveProgressChangedEvent(object sender, ProgressChangedEventArgs e)
        {
            PtGreyCameraImage item = null;
            if (_ptGreyImageQueue.TryDequeue(out item))
            {
                System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)item.Image;
                System.Drawing.Imaging.BitmapData data = null;
                try
                {
                    if (bmp.Width != ImageWidth || bmp.Height != ImageHeight)
                    {
                        ImageWidth = bmp.Width;
                        ImageHeight = bmp.Height;

                        lock (_cameraImageLock)
                        {
                            //changing this property to a new reference now
                            _cameraImage = new System.Windows.Media.Imaging.WriteableBitmap((int)ImageWidth, (int)ImageHeight,
                                   96,
                                   96, System.Windows.Media.PixelFormats.Bgr24, null);
                        }
                        //since this is happening after the original reference was bound to the UI
                        //we have to notify UI of new reference
                        CameraImage = _cameraImage;
                    }

                    lock (_cameraImageLock)
                    {
                        data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, (int)_cameraImage.Width, (int)_cameraImage.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                        _cameraImage.WritePixels(new System.Windows.Int32Rect(0, 0, (int)_cameraImage.Width, (int)_cameraImage.Height),
                                    data.Scan0, (int)(_cameraImage.Width * 3 * _cameraImage.Height), data.Stride);
                    }

                    if (_phosImagesToCollect > 0)
                    {
                        _phosImages.Add(
                            new PtGreyCameraImage
                            {
                                FrameId = item.FrameId,
                                TimeStamp = item.TimeStamp,
                                Image = CameraBitmap
                            }
                            );

                        if (--_phosImagesToCollect == 0)
                            _phosMeasureCompletionEvent.Set();
                    }
                }
                finally
                {
                    if (data != null)
                        bmp.UnlockBits(data);

                    bmp.Dispose();
                }

            }
        }

        #endregion

        public bool SetBinning(bool enable)
        {
            bool result = false;
            try
            {
                _ptGreyCamera.StopVideo();

                result = _ptGreyCamera.SetBinning(enable);

                _ptGreyCamera.StartVideo();
                
            }
            catch
            {
            }

            return result;
        }

        public bool SetShutterTime(double timeMs)
        {
            return _ptGreyCamera.SetShutterTime(timeMs * 1000);
        }
        public bool SetGain(double gain)
        {
            return _ptGreyCamera.SetGain(gain);
        }

        #region phos_capture
        struct BWArgument { public Callback measurementEnd; public int waitTime; }

        public bool StartPhosphorescenceMeasurement(long delay, uint count, double shutterTimeMs, double gain,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";
            
            try
            {
                if (!_cameraConnected)
                    throw new Exception("Camera not connected");

                Ready = false;


                //stop streaming camera
                _ptGreyCamera.StopVideo();

                if (!_ptGreyCamera.SetBinning(true))
                    throw new Exception("Could not enable binning");

                if (!SetShutterTime(shutterTimeMs))
                    throw new Exception("Could not change shutter");

                if (!SetGain(gain))
                    throw new Exception("Could not change gain");

                //increase frame buffer count
                if (!_ptGreyCamera.SetStreamBufferCount(50))
                    throw new Exception("Could not set frame buffer count");

                //do this before we change trigger mode
                _phosStartTimestamp = _ptGreyCamera.GetImageTimeStamp();

                //empty phos image buffer
                _phosImages.Clear();

                //enable external trigger with count + delay
                if (!_ptGreyCamera.PhosTriggerMode(true, count, delay))
                    throw new Exception("Could not set trigger mode");
                
                //set phos count
                _phosImagesToCollect = count;

                //start video
                _ptGreyCamera.StartVideo();

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                arg.waitTime = (int)(5000 + (count * Shutter + delay));


                _phosMeasureCompletionEvent.Reset();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForPhosCompletion;
                bw.RunWorkerCompleted += PhosMeasurementComplete;
                bw.RunWorkerAsync(arg);

                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Ready = true;
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.PhosTriggerMode(false, 0, 0);
                _ptGreyCamera.SetStreamBufferCount(1);
                _ptGreyCamera.SetBinning(false);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
            }

            return result;
        }

        public bool AbortPhosMeasurement()
        {
            try
            {
                Ready = true;
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.PhosTriggerMode(false, 0, 0);
                _ptGreyCamera.SetStreamBufferCount(1);
                _ptGreyCamera.SetBinning(false);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
                return true;
            }
            catch
            {
                return false;
            }
        }
        void WaitForPhosCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch(Exception ex)
            {
                
            }
        }

        void PhosMeasurementComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;
            if (!Ready)//not aborted
            {
                Ready = true;
                _phosMeasureCompletionEvent.Reset();

                //stop video
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;

                //restore normal trigger mode
                _ptGreyCamera.PhosTriggerMode(false, 0, 0);

                //rest frame buffer count 
                _ptGreyCamera.SetStreamBufferCount(1);

                _ptGreyCamera.SetBinning(false);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);

                //start video
                _ptGreyCamera.StartVideo();

                measurementEnds(this, new CamPhosResults(_phosStartTimestamp, _phosImages));
            }
        }



        #endregion

        #region n3fluorescence

        public bool StartN3Fluorescence(List<double> shutterTimesMs, double gain,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";

            try
            {
                if (!_cameraConnected)
                    throw new Exception("Camera not connected");

                Ready = false;

                //stop streaming camera
                _ptGreyCamera.StopVideo();

                if (!_ptGreyCamera.SetBinning(true))
                    throw new Exception("Could not enable binning");

                if (!_ptGreyCamera.SetGain(gain))
                    throw new Exception("Could not change gain");

                //increase frame buffer count
                if (!_ptGreyCamera.SetStreamBufferCount(50))
                    throw new Exception("Could not set frame buffer count");

                //sequencer off
                if (!_ptGreyCamera.SetSequencerMode(false))
                    throw new Exception("Could not disable sequencer");
                //sequencer config on
                if (!_ptGreyCamera.SetSequencerConfigurationMode(true))
                    throw new Exception("Could not enable sequencer config");
                //set sequencer 
                for (int sequenceNumber = 0; sequenceNumber < shutterTimesMs.Count; sequenceNumber++)
                {
                    if (!_ptGreyCamera.SetSingleSequence(sequenceNumber, shutterTimesMs.Count-1,
                        shutterTimesMs[sequenceNumber]*1000))
                    {
                        throw new Exception("Could not set sequence");
                    }
                }

                //sequencer config off
                if (!_ptGreyCamera.SetSequencerConfigurationMode(false))
                    throw new Exception("Could not disable sequencer config");
                //sequencer on
                if (!_ptGreyCamera.SetSequencerMode(true))
                    throw new Exception("Could not enable sequencer");

                //do this before we change trigger mode
                _phosStartTimestamp = _ptGreyCamera.GetImageTimeStamp();

                //empty phos image buffer
                _phosImages.Clear();

                //set phos count
                _phosImagesToCollect = (uint)shutterTimesMs.Count;

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                arg.waitTime = (int)(5000 + (shutterTimesMs.Sum()));


                _phosMeasureCompletionEvent.Reset();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForN3FluorescenceCompletion;
                bw.RunWorkerCompleted += N3FluorescenceComplete;
                bw.RunWorkerAsync(arg);

                //start video
                _ptGreyCamera.StartVideo();

                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
            }

            return result;
        }

        public bool AbortN3Fluorescence()
        {
            try
            {
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.SetStreamBufferCount(1);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
                return true;
            }
            catch
            {
                return false;
            }
        }
        void WaitForN3FluorescenceCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch (Exception ex)
            {

            }
        }

        void N3FluorescenceComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;
            if (!Ready)//not aborted
            {
                Ready = true;
                _phosMeasureCompletionEvent.Reset();

                //stop video
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;

                //rest frame buffer count 
                _ptGreyCamera.SetStreamBufferCount(1);

                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);

                _ptGreyCamera.SetBinning(false);

                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);

                //start video
                _ptGreyCamera.StartVideo();

                measurementEnds(this, new CamPhosResults(_phosStartTimestamp, _phosImages));
            }
        }



        #endregion

        #region deepuvfluorescence

        public bool StartDeepUVFluorescence(List<double> shutterTimesMs, double gain,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";

            try
            {
                if (!_cameraConnected)
                    throw new Exception("Camera not connected");

                Ready = false;

                //stop streaming camera
                _ptGreyCamera.StopVideo();

                if (!_ptGreyCamera.SetBinning(true))
                    throw new Exception("Could not enable binning");

                if (!_ptGreyCamera.SetGain(gain))
                    throw new Exception("Could not change gain");

                //increase frame buffer count
                if (!_ptGreyCamera.SetStreamBufferCount(50))
                    throw new Exception("Could not set frame buffer count");

                //sequencer off
                if (!_ptGreyCamera.SetSequencerMode(false))
                    throw new Exception("Could not disable sequencer");
                //sequencer config on
                if (!_ptGreyCamera.SetSequencerConfigurationMode(true))
                    throw new Exception("Could not enable sequencer config");
                //set sequencer 
                for (int sequenceNumber = 0; sequenceNumber < shutterTimesMs.Count; sequenceNumber++)
                {
                    if (!_ptGreyCamera.SetSingleSequence(sequenceNumber, shutterTimesMs.Count - 1,
                        shutterTimesMs[sequenceNumber] * 1000))
                    {
                        throw new Exception("Could not set sequence");
                    }
                }

                //sequencer config off
                if (!_ptGreyCamera.SetSequencerConfigurationMode(false))
                    throw new Exception("Could not disable sequencer config");
                //sequencer on
                if (!_ptGreyCamera.SetSequencerMode(true))
                    throw new Exception("Could not enable sequencer");

                //do this before we change trigger mode
                _phosStartTimestamp = _ptGreyCamera.GetImageTimeStamp();

                //empty phos image buffer
                _phosImages.Clear();

                //set phos count
                _phosImagesToCollect = (uint)shutterTimesMs.Count;

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                arg.waitTime = (int)(5000 + (shutterTimesMs.Sum()));


                _phosMeasureCompletionEvent.Reset();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForDeepUVFluorescenceCompletion;
                bw.RunWorkerCompleted += DeepUVFluorescenceComplete;
                bw.RunWorkerAsync(arg);

                //start video
                _ptGreyCamera.StartVideo();

                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
            }

            return result;
        }

        public bool AbortDeepUVFluorescence()
        {
            try
            {
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.SetStreamBufferCount(1);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
                return true;
            }
            catch
            {
                return false;
            }
        }
        void WaitForDeepUVFluorescenceCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch (Exception ex)
            {

            }
        }

        void DeepUVFluorescenceComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;
            if (!Ready)//not aborted
            {
                Ready = true;
                _phosMeasureCompletionEvent.Reset();

                //stop video
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;

                //rest frame buffer count 
                _ptGreyCamera.SetStreamBufferCount(1);

                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);

                _ptGreyCamera.SetBinning(false);

                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);

                //start video
                _ptGreyCamera.StartVideo();

                measurementEnds(this, new CamPhosResults(_phosStartTimestamp, _phosImages));
            }
        }



        #endregion

        #region white_light

        public bool StartWhiteLight(List<double> shutterTimesMs, double gain,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";

            try
            {
                if (!_cameraConnected)
                    throw new Exception("Camera not connected");

                Ready = false;

                //stop streaming camera
                _ptGreyCamera.StopVideo();

                if (!_ptGreyCamera.SetBinning(false))
                    throw new Exception("Could not disable binning");

                if (!_ptGreyCamera.SetGain(gain))
                    throw new Exception("Could not change gain");

                //increase frame buffer count
                if (!_ptGreyCamera.SetStreamBufferCount(50))
                    throw new Exception("Could not set frame buffer count");

                //sequencer off
                if (!_ptGreyCamera.SetSequencerMode(false))
                    throw new Exception("Could not disable sequencer");
                //sequencer config on
                if (!_ptGreyCamera.SetSequencerConfigurationMode(true))
                    throw new Exception("Could not enable sequencer config");
                //set sequencer 
                for (int sequenceNumber = 0; sequenceNumber < shutterTimesMs.Count; sequenceNumber++)
                {
                    if (!_ptGreyCamera.SetSingleSequence(sequenceNumber, shutterTimesMs.Count - 1,
                        shutterTimesMs[sequenceNumber] * 1000))
                    {
                        throw new Exception("Could not set sequence");
                    }
                }

                //sequencer config off
                if (!_ptGreyCamera.SetSequencerConfigurationMode(false))
                    throw new Exception("Could not disable sequencer config");
                //sequencer on
                if (!_ptGreyCamera.SetSequencerMode(true))
                    throw new Exception("Could not enable sequencer");

                //do this before we change trigger mode
                _phosStartTimestamp = _ptGreyCamera.GetImageTimeStamp();

                //empty phos image buffer
                _phosImages.Clear();

                //set phos count
                _phosImagesToCollect = (uint)shutterTimesMs.Count;

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                arg.waitTime = (int)(5000 + (shutterTimesMs.Sum()));


                _phosMeasureCompletionEvent.Reset();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForN3FluorescenceCompletion;
                bw.RunWorkerCompleted += N3FluorescenceComplete;
                bw.RunWorkerAsync(arg);

                //start video
                _ptGreyCamera.StartVideo();

                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
            }

            return result;
        }

        public bool AbortWhiteLight()
        {
            try
            {
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.SetStreamBufferCount(1);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
                return true;
            }
            catch
            {
                return false;
            }
        }
        void WaitForWhiteLightCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch (Exception ex)
            {

            }
        }

        void WhiteLightComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;
            if (!Ready)//not aborted
            {
                Ready = true;
                _phosMeasureCompletionEvent.Reset();

                //stop video
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;

                //rest frame buffer count 
                _ptGreyCamera.SetStreamBufferCount(1);

                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);

                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);

                //start video
                _ptGreyCamera.StartVideo();

                measurementEnds(this, new CamPhosResults(_phosStartTimestamp, _phosImages));
            }
        }

        #endregion

        #region dark_background

        public bool StartDarkBG(List<List<double>> shutterTimesMs, List<double> gains,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";

            try
            {
                if (!_cameraConnected)
                    throw new Exception("Camera not connected");

                Ready = false;

                //stop streaming camera
                _ptGreyCamera.StopVideo();

                if (!_ptGreyCamera.SetBinning(true))
                    throw new Exception("Could not disable binning");
                // TODO: move gain setting into singlesequence
                //if (!_ptGreyCamera.SetGain(gain))
                //    throw new Exception("Could not change gain");

                //increase frame buffer count
                if (!_ptGreyCamera.SetStreamBufferCount(50))
                    throw new Exception("Could not set frame buffer count");

                //sequencer off
                if (!_ptGreyCamera.SetSequencerMode(false))
                    throw new Exception("Could not disable sequencer");
                //sequencer config on
                if (!_ptGreyCamera.SetSequencerConfigurationMode(true))
                    throw new Exception("Could not enable sequencer config");

                if(shutterTimesMs.Count != gains.Count)
                {
                    throw new Exception("Error in dark background settings");
                }
                //set sequencer 
                int sequenceNumber = 0;
                double shutterTimesMsTotal = 0;
                int sequenceNumberTotal = 0;
                for (int i = 0; i < shutterTimesMs.Count; i++)
                {
                    sequenceNumberTotal += shutterTimesMs[i].Count;
                }
                for (int i = 0; i < shutterTimesMs.Count; i++)
                {
                    for (int m = 0; m < shutterTimesMs[i].Count; m++)
                    {
                        if (_ptGreyCamera.SetSingleSequence(sequenceNumber, sequenceNumberTotal - 1, gains[i],
                            shutterTimesMs[i][m] * 1000))
                        {
                            sequenceNumber++;
                            shutterTimesMsTotal += shutterTimesMs[i][m];
                        } else
                        {
                            throw new Exception("Could not set sequence");
                        }
                    }
                }


                //sequencer config off
                if (!_ptGreyCamera.SetSequencerConfigurationMode(false))
                    throw new Exception("Could not disable sequencer config");
                //sequencer on
                if (!_ptGreyCamera.SetSequencerMode(true))
                    throw new Exception("Could not enable sequencer");

                //do this before we change trigger mode
                _phosStartTimestamp = _ptGreyCamera.GetImageTimeStamp();

                //empty phos image buffer
                _phosImages.Clear();

                //set phos count
                _phosImagesToCollect = (uint)sequenceNumber;

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                //arg.waitTime = (int)(5000 + (shutterTimesMs.Sum()));
                arg.waitTime = (int)(5000 + (shutterTimesMsTotal));


                _phosMeasureCompletionEvent.Reset();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForN3FluorescenceCompletion;
                bw.RunWorkerCompleted += N3FluorescenceComplete;
                bw.RunWorkerAsync(arg);

                //start video
                _ptGreyCamera.StartVideo();

                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _ptGreyCamera.SetBinning(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
            }

            return result;
        }

        public bool AbortDarkBG()
        {
            try
            {
                Ready = true;
                _ptGreyCamera.StopVideo();
                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);
                _phosImagesToCollect = 0;
                _phosMeasureCompletionEvent.Set();
                _ptGreyCamera.SetStreamBufferCount(1);
                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);
                _ptGreyCamera.StartVideo();
                return true;
            }
            catch
            {
                return false;
            }
        }
        void WaitForDarkBGCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch (Exception ex)
            {

            }
        }

        void DarkBGComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;
            if (!Ready)//not aborted
            {
                Ready = true;
                _phosMeasureCompletionEvent.Reset();

                //stop video
                _ptGreyCamera.StopVideo();
                _phosImagesToCollect = 0;

                //rest frame buffer count 
                _ptGreyCamera.SetStreamBufferCount(1);

                _ptGreyCamera.SetSequencerConfigurationMode(false);
                _ptGreyCamera.SetSequencerMode(false);

                SetShutterTime(Properties.Settings.Default.ShutterDefault);
                SetGain(Properties.Settings.Default.GainDefault);

                //start video
                _ptGreyCamera.StartVideo();

                measurementEnds(this, new CamPhosResults(_phosStartTimestamp, _phosImages));
            }
        }

        #endregion

        #region save
        private System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        void Save()
        {
            var img = BitmapFromWriteableBitmap(CameraImage);
            
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += SaveDoWork;
            bw.RunWorkerCompleted += SaveCompleted;
            bw.RunWorkerAsync(img);
        }

        void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;
            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Saving image..." };
            _statusVM.Messages.Add(sts);
            _timestamps.Push(timestamp);

            try
            {
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "JPG file (*.jpg)|*.jpg|BMP file (*.bmp)|*.bmp";
                if (saveDlg.ShowDialog() == true)
                {
                    using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((System.Drawing.Bitmap)(e.Argument)))
                    {
                        if (Path.GetExtension(saveDlg.FileName).ToUpper().Contains("JPG"))
                        {
                            bmp.Save(saveDlg.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        else
                        {
                            bmp.Save(saveDlg.FileName);
                        }
                    }
                    e.Result = true;
                }
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
            var ts = _timestamps.Pop();
            var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
            _statusVM.Messages.Remove(sm);
            _statusVM.Busy--;

            string message = "Image not saved";

            if ((bool)e.Result == true)
                message = "Image saved";

            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = message };
            _statusVM.Messages.Add(sts);
            Task.Delay(2000).ContinueWith(t => RemoveSaveStatusMessage(timestamp));

        }

        void RemoveSaveStatusMessage(DateTime ts)
        {
            var sm = _statusVM.Messages.Where(s => s.Timestamp == ts).First();
            _statusVM.Messages.Remove(sm);
        }

        #endregion

        #region dispose

        private bool _disposed = false;

        void CloseCamera()
        {
            if (_ptGreyCamera != null)
                _ptGreyCamera.Close();

            if (_ptGreyStopImageThreadEvent != null)
                _ptGreyStopImageThreadEvent.Set();
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
                    CloseCamera();
                }
                _disposed = true;
            }
        }

        #endregion

    }
}
