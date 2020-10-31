
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace RealStereo
{
    public class ConfigurationStepCamera : ConfigurationStep
    {
        private ConfigurationManager manager;
        private WorkerThread workerThread;
        private bool initialBalancingValue;
        private bool isActiveStep = false;
        private Point lastCoordinates;
        private long lastMove = 0;
        private List<Point> coordinates = new List<Point>();

        private static int MOVE_THRESHOLD = 30;
        private static int STAND_STILL_TIME = 3;

        public ConfigurationStepCamera(ConfigurationManager manager, ref WorkerThread workerThread)
        {
            this.manager = manager;
            this.workerThread = workerThread;
        }

        public void Start()
        {
            workerThread.ResultReady += ResultReady;
            initialBalancingValue = workerThread.IsBalancing();
            
            if (!initialBalancingValue)
            {
                workerThread.SetBalancing(true);
            }

            manager.SetInstructions("Go to the position and stand still");
            isActiveStep = true;
        }

        public void Cancel()
        {
            workerThread.SetBalancing(initialBalancingValue);
            workerThread.ResultReady -= ResultReady;
            isActiveStep = false;
        }

        public Configuration Finish(Configuration currentConfiguration)
        {
            int x = 0;
            int y = 0;

            foreach (Point coordinate in coordinates)
            {
                x += coordinate.X;
                y += coordinate.Y;
            }

            currentConfiguration.Coordinates = new Point(x / coordinates.Count, y / coordinates.Count);
            return currentConfiguration;
        }

        private void ResultReady(object sender, ResultReadyEventArgs e)
        {
            if (e.Result.GetCoordinates() != null)
            {
                if (DidMove(e.Result.GetCoordinates()))
                {
                    if (lastMove > 0)
                    {
                        lastMove = 0;
                        manager.SetInstructions("Go to the position and stand still");

                        if (!isActiveStep)
                        {
                            manager.Cancel();
                            manager.SetError("You moved during configuration. Restarting position.");

                            Task.Delay(3000).ContinueWith(_ =>
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => manager.Start());
                            });
                        }
                    }
                }
                else
                {
                    if (lastMove == 0)
                    {
                        lastMove = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        manager.SetInstructions("Calculating coordinates");
                        coordinates.Clear();
                    }
                    else if (isActiveStep && DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMove >= STAND_STILL_TIME * 1000)
                    {
                        manager.NextStep();
                        isActiveStep = false;
                    }
                    else if (isActiveStep)
                    {
                        coordinates.Add(e.Result.GetCoordinates());
                    }
                }

                lastCoordinates = e.Result.GetCoordinates();
            }
        }

        private bool DidMove(Point coordinates)
        {
            if (lastCoordinates == null)
            {
                return true;
            }

            if (coordinates.X >= lastCoordinates.X + MOVE_THRESHOLD || coordinates.X <= lastCoordinates.X - MOVE_THRESHOLD)
            {
                return true;
            }

            if (coordinates.Y >= lastCoordinates.Y + MOVE_THRESHOLD || coordinates.Y <= lastCoordinates.Y - MOVE_THRESHOLD)
            {
                return true;
            }

            return false;
        }
    }
}
