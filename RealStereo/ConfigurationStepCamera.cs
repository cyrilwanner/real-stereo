
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
        private bool initialized = false;
        private bool isActiveStep = false;
        private Point? lastCoordinates;
        private long lastMove = 0;
        private List<Point> coordinates = new List<Point>();

        private static int MOVE_THRESHOLD = 60;
        private static int STAND_STILL_TIME = 3;

        public ConfigurationStepCamera(ConfigurationManager manager, ref WorkerThread workerThread)
        {
            this.manager = manager;
            this.workerThread = workerThread;
        }

        public void Start()
        {
            if (initialized)
            {
                // reset state before the next position
                Cancel();
            }
            else
            {
                initialized = true;
            }

            manager.SetInstructions("Go to the position and stand still");
            isActiveStep = true;
            lastMove = 0;
            lastCoordinates = null;

            Task.Delay(3000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (manager.IsTerminated())
                    {
                        return;
                    }

                    workerThread.ResultReady += ResultReady;
                    if (!initialBalancingValue)
                    {
                        initialBalancingValue = workerThread.IsBalancing();
                    }
                    workerThread.SetCalibrating(true);
                    workerThread.SetBalancing(false);
                });
            });
        }

        public void Cancel()
        {
            workerThread.SetCalibrating(false);
            workerThread.SetBalancing(initialBalancingValue);
            workerThread.ResultReady -= ResultReady;
            isActiveStep = false;
        }

        public PointConfiguration Finish(PointConfiguration currentConfiguration)
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
            if (e.Result.GetCoordinates().HasValue)
            {
                Point currentCoordinates = e.Result.GetCoordinates().Value;
                if (DidMove(currentCoordinates))
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
                                if (manager.IsTerminated())
                                {
                                    return;
                                }

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
                        coordinates.Add(currentCoordinates);
                    }
                }

                lastCoordinates = currentCoordinates;
            }
        }

        private bool DidMove(Point coordinates)
        {
            if (!lastCoordinates.HasValue)
            {
                return true;
            }

            if (coordinates.X >= lastCoordinates.Value.X + MOVE_THRESHOLD || coordinates.X <= lastCoordinates.Value.X - MOVE_THRESHOLD)
            {
                return true;
            }

            if (coordinates.Y >= lastCoordinates.Value.Y + MOVE_THRESHOLD || coordinates.Y <= lastCoordinates.Value.Y - MOVE_THRESHOLD)
            {
                return true;
            }

            return false;
        }
    }
}
