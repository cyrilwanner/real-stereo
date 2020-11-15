using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Image = System.Windows.Controls.Image;
using Point = System.Drawing.Point;

namespace RealStereo
{
    public class WorkerThread
    {
        private Thread thread;
        private Dictionary<Image, Camera> cameras;
        private bool isBalancing = false;
        private bool cancelled = false;
        private MMDevice outputAudioDevice;
        private MMDevice inputAudioDevice;
        private VolumeInterpolation volumeInterpolation;

        private static int FPS = 10;

        public WorkerThread(ref Dictionary<Image, Camera> cameras)
        {
            this.cameras = cameras;

            thread = new Thread(Run);
            thread.Start();
        }

        public event EventHandler<ResultReadyEventArgs> ResultReady;

        public void SetBalancing(bool isBalancing)
        {
            this.isBalancing = isBalancing;
        }

        public void SetOutputAudioDevice(MMDevice outputAudioDevice)
        {
            this.outputAudioDevice = outputAudioDevice;
        }

        public MMDevice GetOutputAudioDevice()
        {
            return outputAudioDevice;
        }

        public void SetInputAudioDevice(MMDevice inputAudioDevice)
        {
            this.inputAudioDevice = inputAudioDevice;
        }

        public MMDevice GetInputAudioDevice()
        {
            return inputAudioDevice;
        }

        public bool IsBalancing()
        {
            return isBalancing;
        }

        public ref Dictionary<Image, Camera> GetCameras()
        {
            return ref cameras;
        }

        public void SetVolumeInterpolation(VolumeInterpolation volumeInterpolation)
        {
            this.volumeInterpolation = volumeInterpolation;
        }

        public VolumeInterpolation GetVolumeInterpolation()
        {
            return volumeInterpolation;
        }

        public void Stop()
        {
            cancelled = true;
            thread.Join();
        }

        protected virtual void OnResultReady(WorkerResult result)
        {
            if (ResultReady != null)
            {
                ResultReadyEventArgs args = new ResultReadyEventArgs();
                args.Result = result;

                ResultReady(this, args);
            }
        }

        private void Run()
        {
            while (!cancelled)
            {
                DoWork();

                Thread.Sleep(1000 / FPS);
            }
        }

        private void DoWork()
        {
            WorkerResult result = new WorkerResult(cameras.Keys.Count);
            Point coordinates = new Point(0, 0);
            bool applyCoordinates = true;

            for (int i = 0; i < cameras.Keys.Count; i++)
            {
                Image image = cameras.Keys.ElementAt(i);
                Camera camera = cameras[image];

                camera.Process(isBalancing);
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
            }

            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => OnResultReady(result));
            }
        }
    }

    public class ResultReadyEventArgs : EventArgs
    {
        public WorkerResult Result { get; set; }
    }
}
