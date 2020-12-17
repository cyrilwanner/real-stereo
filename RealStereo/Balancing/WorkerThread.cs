using NAudio.CoreAudioApi;
using RealStereo.Balancing.Speaker;
using RealStereo.Balancing.Tracking;
using RealStereo.Ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Image = System.Windows.Controls.Image;
using Point = System.Drawing.Point;

namespace RealStereo.Balancing
{
    public class WorkerThread
    {
        private static readonly int Fps = 10;

        private Thread thread;
        private Dictionary<Image, Camera> cameras;
        private bool isBalancing = false;
        private bool isCalibrating = false;
        private MMDevice outputAudioDevice;
        private MMDevice inputAudioDevice;
        private VolumeInterpolation volumeInterpolation;
        private VolumeFader volumeFader;

        public WorkerThread(ref Dictionary<Image, Camera> cameras)
        {
            this.cameras = cameras;

            volumeFader = new VolumeFader(this);

            thread = new Thread(Run);
            thread.Start();
        }

        public event EventHandler<ResultReadyEventArgs> ResultReady;

        /// <summary>
        /// Set whether the worker thread should balance the speakers.
        /// This will enable people detection and speaker balancing.
        /// </summary>
        /// <param name="isBalancing"></param>
        public void SetBalancing(bool isBalancing)
        {
            this.isBalancing = isBalancing;
        }

        /// <summary>
        /// Set whether the worker thread should calibrate the equipment.
        /// This will enable people detection but not speaker balancing.
        /// </summary>
        /// <param name="isCalibrating"></param>
        public void SetCalibrating(bool isCalibrating)
        {
            this.isCalibrating = isCalibrating;
        }

        /// <summary>
        /// Sets the audio device used for balancing.
        /// </summary>
        /// <param name="outputAudioDevice">Output audio device.</param>
        public void SetOutputAudioDevice(MMDevice outputAudioDevice)
        {
            this.outputAudioDevice = outputAudioDevice;
        }

        /// <summary>
        /// Gets the audio deviced used for balancing.
        /// </summary>
        /// <returns>Output audio device.</returns>
        public MMDevice GetOutputAudioDevice()
        {
            return outputAudioDevice;
        }

        /// <summary>
        /// Sets the audio device used for recording during configuration.
        /// </summary>
        /// <param name="inputAudioDevice">Input audio device</param>
        public void SetInputAudioDevice(MMDevice inputAudioDevice)
        {
            this.inputAudioDevice = inputAudioDevice;
        }

        /// <summary>
        /// Gets the audio device used for recording.
        /// </summary>
        /// <returns>Input audio device.</returns>
        public MMDevice GetInputAudioDevice()
        {
            return inputAudioDevice;
        }

        /// <summary>
        /// Returns whether the worker thread is currently balancing.
        /// </summary>
        /// <returns></returns>
        public bool IsBalancing()
        {
            return isBalancing;
        }

        /// <summary>
        /// Returns whether the worker thread is currently calibrating.
        /// </summary>
        /// <returns></returns>
        public bool IsCalibrating()
        {
            return isCalibrating;
        }

        /// <summary>
        /// Get the selected cameras.
        /// </summary>
        /// <returns></returns>
        public ref Dictionary<Image, Camera> GetCameras()
        {
            return ref cameras;
        }

        /// <summary>
        /// Set the volume interpolation instance.
        /// </summary>
        /// <param name="volumeInterpolation"></param>
        public void SetVolumeInterpolation(VolumeInterpolation volumeInterpolation)
        {
            this.volumeInterpolation = volumeInterpolation;
        }

        /// <summary>
        /// Get the volume interpolation instance.
        /// </summary>
        /// <returns></returns>
        public VolumeInterpolation GetVolumeInterpolation()
        {
            return volumeInterpolation;
        }

        /// <summary>
        /// Stop the whole worker thread. This will abort all pending work.
        /// </summary>
        public void Stop()
        {
            thread.Interrupt();
        }

        /// <summary>
        /// Calls the event listeners when a new result is ready.
        /// </summary>
        /// <param name="result">New result.</param>
        protected virtual void OnResultReady(WorkerResult result)
        {
            if (ResultReady != null)
            {
                ResultReadyEventArgs args = new ResultReadyEventArgs();
                args.Result = result;

                ResultReady(this, args);
            }
        }

        /// <summary>
        /// Main thread method.
        /// </summary>
        private void Run()
        {
            try
            {
                while (true)
                {
                    DoWork();

                    Thread.Sleep(1000 / Fps);
                }
            }
            catch (ThreadInterruptedException)
            {
                volumeFader.Cancel();
            }
        }

        /// <summary>
        /// Combines the people tracking and speaker balancing.
        /// It first processes the next camera frames and receives the coordinates of the detected people.
        /// Afterwards, the speaker will be balanced.
        /// It also updates the UI Windows and triggers new result events.
        /// </summary>
        private void DoWork()
        {
            WorkerResult result = new WorkerResult(cameras.Keys.Count);
            Point coordinates = new Point(0, 0);
            bool applyCoordinates = true;

            // process all cameras
            for (int i = 0; i < cameras.Keys.Count; i++)
            {
                Image image = cameras.Keys.ElementAt(i);
                Camera camera = cameras[image];

                // process next frame and detect people/coordinates if needed
                camera.Process(isBalancing || isCalibrating);
                result.SetFrame(i, camera.GetFrame());
                Point? cameraCoordinates = camera.GetCoordinates(i % 2 == 0 ? Orientation.Horizontal : Orientation.Vertical);

                // if a camera is not ready or didn't detect a person, cancel coordinates calculation
                if (cameraCoordinates == null)
                {
                    applyCoordinates = false;
                }
                else
                {
                    coordinates.X = Math.Max(coordinates.X, cameraCoordinates.Value.X);
                    coordinates.Y = Math.Max(coordinates.Y, cameraCoordinates.Value.Y);
                }
            }

            if (applyCoordinates)
            {
                result.SetCoordinates(coordinates);

                if (isBalancing)
                {
                    float[] channels = new float[outputAudioDevice.AudioEndpointVolume.Channels.Count];
                    for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
                    {
                        channels[i] = (float)volumeInterpolation.GetVolumeForPositionAndSpeaker(coordinates.X, coordinates.Y, i);
                    }
                    volumeFader.Set(channels);
                }
            }

            // dispatch the result ready event and update the main window
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => {
                    OnResultReady(result);
                    if (isBalancing)
                    {
                        if (Application.Current.MainWindow is MainWindow)
                        {
                            ((MainWindow)Application.Current.MainWindow).UpdateChannelLevels();
                        }
                    }
                });
            }
        }
    }

    public class ResultReadyEventArgs : EventArgs
    {
        public WorkerResult Result { get; set; }
    }
}
