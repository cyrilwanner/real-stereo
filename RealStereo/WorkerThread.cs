using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Image = System.Windows.Controls.Image;
using Point = System.Drawing.Point;

namespace RealStereo
{
    class WorkerThread
    {
        private Thread thread;
        private Dictionary<Image, Camera> cameras;
        private bool isBalancing = false;

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
            while (true)
            {
                DoWork();

                Thread.Sleep(1000 / FPS);
            }
        }

        private void DoWork()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            WorkerResult result = new WorkerResult(cameras.Keys.Count);
            Point coordinates = new Point(0, 0);
            bool applyCoordinates = true;

            CountdownEvent activeThreads = new CountdownEvent(cameras.Keys.Count);

            for (int i = 0; i < cameras.Keys.Count; i++)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    object[] args = state as object[];
                    Dictionary<Image, Camera> cameras = args[0] as Dictionary<Image, Camera>;
                    int index = (int) args[1];

                    Image image = cameras.Keys.ElementAt(index);
                    Camera camera = cameras[image];

                    camera.Process(isBalancing);
                    result.SetFrame(index, camera.GetFrame());
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

                    activeThreads.Signal();
                }, new object[] { cameras, i });
            }

            activeThreads.Wait();

            if (applyCoordinates)
            {
                result.SetCoordinates(coordinates);
            }

            stopwatch.Stop();
            //System.Diagnostics.Debug.WriteLine(stopwatch.ElapsedMilliseconds);

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
