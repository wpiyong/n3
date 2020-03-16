using SpinnakerNET;
using SpinnakerNET.GenApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace N3Imager.Model
{
    class PtGreyCameraImage
    {
        public ulong FrameId { get; set; }
        public ulong TimeStamp { get; set; }
        public System.Drawing.Bitmap Image { get; set; }
    }

    class PtGreyCamera
    {
        ManagedSystem system = null;
        IList<IManagedCamera> camList = null;
        IManagedCamera managedCamera = null;
        INodeMap nodeMap = null;
        ImageEventListener imageEventListener = null;
        ConcurrentQueue<PtGreyCameraImage> imageQueue = null;
        bool _videoMode = false;

        class ImageEventListener : ManagedImageEvent
        {
            ConcurrentQueue<PtGreyCameraImage> _imageQueue = null;

            public ImageEventListener(ConcurrentQueue<PtGreyCameraImage> imageQueue)
            {
                _imageQueue = imageQueue;
            }


            override protected void OnImageEvent(ManagedImage image)
            {
                //Debug.WriteLine("OnImageEvent");

                try
                {
                    if (!image.IsIncomplete)
                    {
                        // Convert image
                        using (var temp = image.Convert(PixelFormatEnums.BGR8))
                        {
                            if (_imageQueue.Count <= 10)
                            {
                                _imageQueue.Enqueue(
                                    new PtGreyCameraImage
                                    {
                                        FrameId = image.FrameID,
                                        TimeStamp = image.TimeStamp,
                                        Image = new System.Drawing.Bitmap(temp.bitmap)
                                    }
                                    );
                            }
                            else
                                Debug.WriteLine("Dropped frame");
                        }

                    }
                    image.Release();
                }
                catch (SpinnakerException ex)
                {
                    Debug.WriteLine("Error: {0}", ex.Message);
                }
                catch (Exception ex1)
                {
                    Debug.WriteLine("Error: {0}", ex1.Message);
                }
                finally
                {
                    // Must manually release the image to prevent buffers on the camera stream from filling up
                    //image.Release();
                }
            }


        }

        

        public bool Open(ConcurrentQueue<PtGreyCameraImage> imageQ, out string message)
        {
            bool result = false;
            message = "";

            system = new ManagedSystem();

            // Retrieve list of cameras from the system
            camList = system.GetCameras();

            // Finish if there are no cameras
            if (camList.Count != 1)
            {
                foreach (IManagedCamera mc in camList)
                    mc.Dispose();

                // Clear camera list before releasing system
                camList.Clear();

                // Release system
                system.Dispose();
                message = "Camera count is " + camList.Count;
            }
            else
            {
                try
                {
                    managedCamera = camList[0];

                    if (managedCamera.TLDevice.DeviceDisplayName != null && managedCamera.TLDevice.DeviceDisplayName.IsReadable)
                    {
                        message = managedCamera.TLDevice.DeviceDisplayName.ToString();
                    }

                    // Initialize camera
                    managedCamera.Init();

                    // Retrieve GenICam nodemap
                    nodeMap = managedCamera.GetNodeMap();

                    imageQueue = imageQ;
                    result = true;

                }
                catch (SpinnakerException ex)
                {
                    Debug.WriteLine("Error: {0}", ex.Message);
                    message = ex.Message;
                    result = false; ;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    result = false;
                }
            }

            return result;

        }

        bool RestoreDefaultSettings()
        {
            bool result = false;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        managedCamera.UserSetSelector.Value = UserSetSelectorEnums.Default.ToString();
                        managedCamera.UserSetLoad.Execute();
                        result = true;
                        break;
                    }
                    catch(SpinnakerException s)
                    {
                        managedCamera.AcquisitionMode.Value = AcquisitionModeEnums.Continuous.ToString();
                        managedCamera.BeginAcquisition();
                        System.Threading.Thread.Sleep(500);
                        managedCamera.EndAcquisition();
                    }
                }

                //TODO: stream buffer default count mode to manual
                // Set stream buffer Count Mode to manual
                // Retrieve Stream Parameters device nodemap
                INodeMap sNodeMap = managedCamera.GetTLStreamNodeMap();
                IEnum streamBufferCountMode = sNodeMap.GetNode<IEnum>("StreamBufferCountMode");
                if (streamBufferCountMode == null || !streamBufferCountMode.IsWritable)
                {
                    return false;
                }

                IEnumEntry streamBufferCountModeManual = streamBufferCountMode.GetEntryByName("Manual");
                if (streamBufferCountModeManual == null || !streamBufferCountModeManual.IsReadable)
                {
                    return false;
                }

                streamBufferCountMode.Value = streamBufferCountModeManual.Value;

            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool SetStreamBufferCount(long count)
        {
            try
            {
                //StreamDefaultBufferCount is the number of images to buffer on PC
                //default is 10
                INodeMap sNodeMap = managedCamera.GetTLStreamNodeMap();
                IInteger streamNode = sNodeMap.GetNode<IInteger>("StreamDefaultBufferCount");
                if (streamNode == null || !streamNode.IsWritable)
                {
                    return false;
                }

                streamNode.Value = count;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool DefaultSettings()
        {
            bool result = false;
            try
            {
                StopVideo();
                if (RestoreDefaultSettings())
                {
                    if (!SetStreamBufferCount(1))
                        return false;

                    //shutter, gain, wb off
                    managedCamera.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();
                    managedCamera.ExposureMode.Value = ExposureModeEnums.Timed.ToString();

                    managedCamera.GainAuto.Value = GainAutoEnums.Off.ToString();

                    SetProprtyEnabledSetting("Saturation", false);
                    managedCamera.BalanceWhiteAuto.Value = BalanceWhiteAutoEnums.Off.ToString();

                    if (EnableChunkData())
                        result = true;
                }

            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool SetSequencerMode(bool enable)
        {
            bool result = false;

            try
            {
                IEnum iSequencerMode = nodeMap.GetNode<IEnum>("SequencerMode");
                IEnumEntry iSequencerModeOn = iSequencerMode.GetEntryByName("On");
                IEnumEntry iSequencerModeOff = iSequencerMode.GetEntryByName("Off");
                

                if (enable)
                {
                    //
                    // Turn sequencer mode on
                    // 
                    // *** NOTES ***
                    // After sequencer mode has been turned on, the camera will 
                    // begin using the saved states in the order that they were set. 
                    //
                    // *** LATER ***
                    // Once all images have been captured, disable the sequencer 
                    // in order to restore the camera to its initial state.
                    //
                    if (iSequencerMode.Value.Int != iSequencerModeOn.Value)
                    {
                        if (!iSequencerMode.IsWritable)
                            throw new Exception("entry - SequencerMode 'On'");

                        iSequencerMode.Value = iSequencerModeOn.Value;
                    }
                    
                }
                else
                {

                    //
                    // Turn sequencer mode back off
                    //
                    // *** NOTES ***
                    // Between uses, it is best to disable the sequencer until it 
                    // is once again required.
                    //
                    if (iSequencerMode.Value.Int != iSequencerModeOff.Value)
                    {
                        if (!iSequencerMode.IsWritable)
                            throw new Exception("entry - SequencerMode 'Off'");

                        iSequencerMode.Value = iSequencerModeOff.Value;
                    }

                    
                }
                result = true;
                
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool SetSequencerConfigurationMode(bool enable)
        {
            bool result = false;

            try
            {
                IEnum iSequencerConfigurationMode = nodeMap.GetNode<IEnum>("SequencerConfigurationMode");
                if (iSequencerConfigurationMode == null || !iSequencerConfigurationMode.IsWritable)
                {
                    throw new Exception("node - SequencerConfigurationMode");
                }

                if (enable)
                {
                    //
                    // Turn configuration mode on
                    //
                    // *** NOTES ***
                    // Once sequencer mode is off, enabling sequencer configuration 
                    // mode allows for the setting of individual sequences.
                    // 
                    // *** LATER ***
                    // Before sequencer mode is turned back on, sequencer
                    // configuration mode must be turned off.
                    //
                    IEnumEntry iSequencerConfigurationModeOn = iSequencerConfigurationMode.GetEntryByName("On");
                    if (iSequencerConfigurationModeOn == null || !iSequencerConfigurationModeOn.IsReadable)
                    {
                        throw new Exception("entry - SequencerConfigurationMode 'On'");
                    }

                    iSequencerConfigurationMode.Value = iSequencerConfigurationModeOn.Value;
                }
                else
                {
                    //
                    // Turn configuration mode off
                    //
                    // *** NOTES ***
                    // Once all desired states have been set, turn sequencer 
                    // configuration mode off in order to turn sequencer mode on.
                    //
                    IEnumEntry iSequencerConfigurationModeOff = iSequencerConfigurationMode.GetEntryByName("Off");
                    if (iSequencerConfigurationModeOff == null || !iSequencerConfigurationModeOff.IsReadable)
                    {
                        throw new Exception("entry - SequencerConfigurationMode 'Off'");
                    }

                    iSequencerConfigurationMode.Value = iSequencerConfigurationModeOff.Value;
                }
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool SetSingleSequence(int sequenceNumber, int finalSequenceNumber, double exposureTimeToSet)
        {
            bool result = false;

            try
            {
                //
                // Select the current sequence
                //
                // *** NOTES ***
                // Select the index of the sequence to be set.
                //
                // *** LATER ***
                // The next state - i.e. the state to be linked to -
                // also needs to be set before saving the current state.
                //
                IInteger iSequencerSetSelector = nodeMap.GetNode<IInteger>("SequencerSetSelector");
                if (iSequencerSetSelector == null || !iSequencerSetSelector.IsWritable)
                {
                    throw new Exception("Unable to select state. Aborting...\n");
                }

                iSequencerSetSelector.Value = sequenceNumber;


                //
                // Set desired settings for the current state
                //
                
                // Set exposure time; exposure time recorded in microseconds
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsWritable)
                {
                    throw new Exception("Unable to set exposure time. Aborting...\n");
                }

                iExposureTime.Value = exposureTimeToSet;

                

                //
                // Set the trigger type for the current state
                //
                // *** NOTES ***
                // It is a requirement of every state to have its trigger 
                // source set. The trigger source refers to the moment when the 
                // sequencer changes from one state to the next.
                //
                IEnum iSequencerTriggerSource = nodeMap.GetNode<IEnum>("SequencerTriggerSource");
                if (iSequencerTriggerSource == null || !iSequencerTriggerSource.IsWritable)
                {
                    throw new Exception("Unable to set trigger source (enum retrieval). Aborting...\n");
                }

                IEnumEntry iSequencerTriggerSourceFrameStart = iSequencerTriggerSource.GetEntryByName("FrameStart");
                if (iSequencerTriggerSourceFrameStart == null || iSequencerTriggerSourceFrameStart.IsWritable)
                {
                    throw new Exception("Unable to set trigger source (entry retrieval). Aborting...\n");
                }

                iSequencerTriggerSource.Value = iSequencerTriggerSourceFrameStart.Value;


                //
                // Set the next state in the sequence
                //
               

                IInteger iSequencerSetNext = nodeMap.GetNode<IInteger>("SequencerSetNext");
                if (iSequencerSetNext == null || !iSequencerSetNext.IsWritable)
                {
                    throw new Exception("Unable to set next state. Aborting...\n");
                }

                if (sequenceNumber == finalSequenceNumber)
                {
                    iSequencerSetNext.Value = 0;
                }
                else
                {
                    iSequencerSetNext.Value = sequenceNumber + 1;
                }


                //
                // Save current state
                //
                // *** NOTES ***
                // Once all appropriate settings have been configured, make 
                // sure to save the state to the sequence. Notice that these 
                // settings will be lost when the camera is power-cycled.
                //
                ICommand iSequencerSetSave = nodeMap.GetNode<ICommand>("SequencerSetSave");
                if (iSequencerSetSave == null || !iSequencerSetSave.IsWritable)
                {
                    throw new Exception("Unable to save state. Aborting...\n");
                }

                iSequencerSetSave.Execute();

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool SetSingleSequence(int sequenceNumber, int finalSequenceNumber, double gain, double exposureTimeToSet)
        {
            bool result = false;

            try
            {
                //
                // Select the current sequence
                //
                // *** NOTES ***
                // Select the index of the sequence to be set.
                //
                // *** LATER ***
                // The next state - i.e. the state to be linked to -
                // also needs to be set before saving the current state.
                //
                IInteger iSequencerSetSelector = nodeMap.GetNode<IInteger>("SequencerSetSelector");
                if (iSequencerSetSelector == null || !iSequencerSetSelector.IsWritable)
                {
                    throw new Exception("Unable to select state. Aborting...\n");
                }

                iSequencerSetSelector.Value = sequenceNumber;


                //
                // Set desired settings for the current state
                //

                // Set exposure time; exposure time recorded in microseconds
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsWritable)
                {
                    throw new Exception("Unable to set exposure time. Aborting...\n");
                }

                iExposureTime.Value = exposureTimeToSet;

                // TODO: set gain
                // Set gain; gain recorded in decibels
                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsWritable)
                {
                    throw new Exception("Unable to set gain. Aborting...\n");
                }

                iGain.Value = gain;

                //
                // Set the trigger type for the current state
                //
                // *** NOTES ***
                // It is a requirement of every state to have its trigger 
                // source set. The trigger source refers to the moment when the 
                // sequencer changes from one state to the next.
                //
                IEnum iSequencerTriggerSource = nodeMap.GetNode<IEnum>("SequencerTriggerSource");
                if (iSequencerTriggerSource == null || !iSequencerTriggerSource.IsWritable)
                {
                    throw new Exception("Unable to set trigger source (enum retrieval). Aborting...\n");
                }

                IEnumEntry iSequencerTriggerSourceFrameStart = iSequencerTriggerSource.GetEntryByName("FrameStart");
                if (iSequencerTriggerSourceFrameStart == null || iSequencerTriggerSourceFrameStart.IsWritable)
                {
                    throw new Exception("Unable to set trigger source (entry retrieval). Aborting...\n");
                }

                iSequencerTriggerSource.Value = iSequencerTriggerSourceFrameStart.Value;


                //
                // Set the next state in the sequence
                //


                IInteger iSequencerSetNext = nodeMap.GetNode<IInteger>("SequencerSetNext");
                if (iSequencerSetNext == null || !iSequencerSetNext.IsWritable)
                {
                    throw new Exception("Unable to set next state. Aborting...\n");
                }

                if (sequenceNumber == finalSequenceNumber)
                {
                    iSequencerSetNext.Value = 0;
                }
                else
                {
                    iSequencerSetNext.Value = sequenceNumber + 1;
                }


                //
                // Save current state
                //
                // *** NOTES ***
                // Once all appropriate settings have been configured, make 
                // sure to save the state to the sequence. Notice that these 
                // settings will be lost when the camera is power-cycled.
                //
                ICommand iSequencerSetSave = nodeMap.GetNode<ICommand>("SequencerSetSave");
                if (iSequencerSetSave == null || !iSequencerSetSave.IsWritable)
                {
                    throw new Exception("Unable to save state. Aborting...\n");
                }

                iSequencerSetSave.Execute();

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }


        public void StartVideo()
        {
            try
            {
                if (managedCamera != null && _videoMode == false)
                {
                    // Set acquisition mode to continuous
                    managedCamera.AcquisitionMode.Value = AcquisitionModeEnums.Continuous.ToString();
                    
                    // Configure image events
                    imageEventListener = new ImageEventListener(imageQueue);
                    managedCamera.RegisterEvent(imageEventListener);

                    // Begin acquiring images
                    managedCamera.BeginAcquisition();

                    _videoMode = true;
                }
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
        }

        public void StopVideo()
        {
            try
            {
                if (managedCamera != null)
                {
                    // End acquisition
                    managedCamera.EndAcquisition();

                    managedCamera.UnregisterEvent(imageEventListener);

                    //clear queue
                    PtGreyCameraImage item;
                    while (imageQueue.TryDequeue(out item))
                    {
                        // do nothing
                    }

                    _videoMode = false;
                }
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
        }

        public ulong GetImageTimeStamp()
        {
            try
            {
                ICommand iTimestampLatch = nodeMap.GetNode<ICommand>("TimestampLatch");
                iTimestampLatch.Execute();
                IInteger iTimestamp = nodeMap.GetNode<IInteger>("TimestampLatchValue");
                return iTimestamp.Value < 0 ? 0 : (ulong)iTimestamp.Value;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                
            }

            return 0;
        }

        public void Close()
        {
            if (camList != null)
            {
                foreach (IManagedCamera mc in camList)
                {
                    StopVideo();

                    // Deinitialize camera
                    mc.DeInit();

                    mc.Dispose();
                }

                camList.Clear();
            }

            if (system != null)
                system.Dispose();
        }


        public bool SetShutterTime(double value)
        {
            bool result = false;

            try
            {
                managedCamera.ExposureTime.Value = value;//microseconds
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetShutterTime()
        {
            return managedCamera.ExposureTime.Value;//microseconds
        }

        public bool SetGain(double value)
        {
            bool result = false;

            try
            {
                managedCamera.Gain.Value = value;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetGain()
        {
            return managedCamera.Gain.Value;
        }

        public bool SetFrameRate(double value)
        {
            bool result = false;

            try
            {
                managedCamera.AcquisitionFrameRate.Value = value;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetFrameRate()
        {
            return managedCamera.AcquisitionFrameRate.Value;
        }

        public bool SetWhiteBalanceRed(double wbRed)
        {
            bool result = false;

            try
            {
                managedCamera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
                managedCamera.BalanceRatio.Value = wbRed;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetWhiteBalanceRed()
        {
            managedCamera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
            return (double)managedCamera.BalanceRatio.Value;
        }

        public bool SetWhiteBalanceBlue(double wbBlue)
        {
            bool result = false;

            try
            {
                managedCamera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
                managedCamera.BalanceRatio.Value = wbBlue;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetWhiteBalanceBlue()
        {
            managedCamera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
            return (double)managedCamera.BalanceRatio.Value;
        }


        bool EnableChunkData()
        {
            bool result = true;

            try
            {
                managedCamera.ChunkSelector.Value = ChunkSelectorEnums.FrameID.ToString();
                managedCamera.ChunkEnable.Value = true;

                managedCamera.ChunkSelector.Value = ChunkSelectorEnums.Timestamp.ToString();
                managedCamera.ChunkEnable.Value = true;

                managedCamera.ChunkModeActive.Value = true;
                #region chunkenable_using_node
                /*
                //
                // Activate chunk mode
                //
                // *** NOTES ***
                // Once enabled, chunk data will be available at the end of the 
                // payload of every image captured until it is disabled. Chunk 
                // data can also be retrieved from the nodemap.
                //
                IBool iChunkModeActive = nodeMap.GetNode<IBool>("ChunkModeActive");
                iChunkModeActive.Value = true;

                
                //
                // Enable all types of chunk data
                //
                // *** NOTES ***
                // Enabling chunk data requires working with nodes: 
                // "ChunkSelector" is an enumeration selector node and 
                // "ChunkEnable" is a boolean. It requires retrieving the 
                // selector node (which is of enumeration node type), selecting
                // the entry of the chunk data to be enabled, retrieving the 
                // corresponding boolean, and setting it to true. 
                //
                // In this example, all chunk data is enabled, so these steps 
                // are performed in a loop. Once this is complete, chunk mode
                // still needs to be activated.
                //
                // Retrieve selector node
                IEnum iChunkSelector = nodeMap.GetNode<IEnum>("ChunkSelector");

                // Retrieve entries
                EnumEntry[] entries = iChunkSelector.Entries;

                for (int i = 0; i < entries.Length; i++)
                {
                    // Select entry to be enabled
                    IEnumEntry iChunkSelectorEntry = entries[i];

                    // Go to next node if problem occurs
                    if (!iChunkSelectorEntry.IsAvailable || !iChunkSelectorEntry.IsReadable)
                    {
                        continue;
                    }

                    iChunkSelector.Value = iChunkSelectorEntry.Value;

                    // Retrieve corresponding boolean
                    IBool iChunkEnable = nodeMap.GetNode<IBool>("ChunkEnable");

                    // Enable the boolean, thus enabling the corresponding chunk
                    // data
                    if (iChunkEnable == null)
                    {

                    }
                    else if (iChunkEnable.Value)
                    {
                    }
                    else if (iChunkEnable.IsWritable)
                    {
                        iChunkEnable.Value = true;
                    }
                    else
                    {
                    }
                    
                }
                */
                #endregion

            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }

            return result;
        }


        public bool PhosTriggerMode(bool enable, uint count, long delay_us)
        {
            bool result = false;
            try
            {
                //hardware gpio 0, rising edge, count = num images

                //
                // Ensure trigger mode off
                //
                // *** NOTES ***
                // The trigger must be disabled in order to configure whether 
                // the source is software or hardware.
                //
                managedCamera.TriggerMode.Value = TriggerModeEnums.Off.ToString();

                
                if (enable)
                {
                    //order of setting matters, set trigger selector first
                    //set number of images to capture after single trigger
                    managedCamera.TriggerSelector.Value = TriggerSelectorEnums.FrameBurstStart.ToString();
                    managedCamera.AcquisitionBurstFrameCount.Value = count;

                    managedCamera.TriggerDelay.Value = delay_us;//microsecs

                    //
                    // Select trigger source
                    //
                    // *** NOTES ***
                    // The trigger source must be set to hardware or software while 
                    // trigger mode is off.
                    //
                    managedCamera.TriggerSource.Value = TriggerSourceEnums.Line0.ToString();
                    managedCamera.TriggerActivation.Value = TriggerActivationEnums.RisingEdge.ToString();

                    

                    //
                    // Turn trigger mode on
                    //
                    // *** LATER ***
                    // Once the appropriate trigger source has been set, turn 
                    // trigger mode back on in order to retrieve images using the 
                    // trigger.
                    //
                    managedCamera.TriggerMode.Value = TriggerModeEnums.On.ToString();
                }

                result = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

            return result;
        }

        public bool SetBinning(bool enable)
        {
            bool result = true;
            try
            {
                managedCamera.BinningSelector.Value = BinningSelectorEnums.All.ToString();
                managedCamera.BinningHorizontalMode.Value = BinningHorizontalModeEnums.Sum.ToString();
                managedCamera.BinningVerticalMode.Value = BinningVerticalModeEnums.Sum.ToString();
                managedCamera.BinningHorizontal.Value = enable ? 2 : 1;
                managedCamera.BinningVertical.Value = enable ? 2 : 1;
                if (!enable)
                    result = ResetImageDimensions();
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }

            return result;
        }

        bool ResetImageDimensions()
        {
            bool result = true;
            try
            {
                managedCamera.Width.Value = managedCamera.SensorWidth.Value;
                managedCamera.Height.Value = managedCamera.SensorHeight.Value;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }

            return result;
        }

        public void SetProprtyEnabledSetting(string property, bool enabled)
        {
            try
            {
                BoolNode boolNode;
                if (property == "Gamma")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("GammaEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", (object)property);
                    }
                }
                else if (property == "Sharpness")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SharpnessEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", (object)property);
                    }
                }
                else if (property == "Hue")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("HueEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", (object)property);
                    }
                }
                else if (property == "Saturation")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SaturationEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", (object)property);
                    }
                }
                else if (property == "FrameRate")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("AcquisitionFrameRateEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", (object)property);
                    }
                }
                else
                {
                    Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not implemented", (object)property);
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetProprtyEnabledSetting " + property + " exceptoin: " + e.Message);
            }
        }
    }
}
