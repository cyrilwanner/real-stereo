using RealStereo.Balancing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace RealStereo.Config.Steps
{
    public class ConfigurationStepCamera : ConfigurationStep
    {
        private static readonly int MoveThreshold = 60;
        private static readonly int StandStillTime = 3;

        private ConfigurationManager manager;
        private WorkerThread workerThread;
        private bool initialBalancingValue;
        private bool initialized = false;
        private bool isActiveStep = false;
        private Point? lastCoordinates;
        private long lastMove = 0;
        private List<Point> coordinates = new List<Point>();

        public ConfigurationStepCamera(ConfigurationManager manager, ref WorkerThread workerThread)
        {
            this.manager = manager;
            this.workerThread = workerThread;
        }

        /// <summary>
        /// Start the camera configuration step.
        /// The person has to stand still for the defined amount of time until the coordinates are calculated.
        /// </summary>
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

            // give the person 5s time to go to the next position
            Task.Delay(5000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (manager.IsTerminated())
                    {
                        return;
                    }

                    workerThread.ResultReady += ResultReady;
                });
            });

            if (!initialBalancingValue)
            {
                initialBalancingValue = workerThread.IsBalancing();
            }
            workerThread.SetCalibrating(true);
            workerThread.SetBalancing(false);
        }

        /// <summary>
        /// Cancel the camera configuration step.
        /// </summary>
        public void Cancel()
        {
            workerThread.SetCalibrating(false);
            workerThread.SetBalancing(initialBalancingValue);
            workerThread.ResultReady -= ResultReady;
            isActiveStep = false;
        }

        /// <summary>
        /// Finishes the camera configuration step by returning the calculated coordiantes.
        /// </summary>
        /// <param name="currentConfiguration">Current configuration.</param>
        /// <returns>Current configuration with coordinates set.</returns>
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

        /// <summary>
        /// Process the worker result by extracting the coordinates and checking if a person has moved.
        /// </summary>
        /// <param name="sender">Worker thread.</param>
        /// <param name="e">Event arguments.</param>
        private void ResultReady(object sender, ResultReadyEventArgs e)
        {
            if (e.Result.GetCoordinates().HasValue)
            {
                Point currentCoordinates = e.Result.GetCoordinates().Value;

                // check if the person did move since the last result
                if (DidMove(currentCoordinates))
                {
                    // reset the countdown if it already started
                    if (lastMove > 0)
                    {
                        lastMove = 0;
                        manager.SetInstructions("Go to the position and stand still");

                        // if the configuration is currently in a different step, completely cancel the active step and start again
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
                        // if the person did not move and no countdown is set, start one
                        lastMove = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        manager.SetInstructions("Calculating coordinates");
                        coordinates.Clear();
                    }
                    else if (isActiveStep && DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMove >= StandStillTime * 1000)
                    {
                        // proceed to the next step if the person stood still long enough
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

        /// <summary>
        /// Check if the person did move since the last result.
        /// </summary>
        /// <param name="coordinates">Current coordinates.</param>
        /// <returns>Whether the person has moved or not.</returns>
        private bool DidMove(Point coordinates)
        {
            if (!lastCoordinates.HasValue)
            {
                return true;
            }

            if (coordinates.X >= lastCoordinates.Value.X + MoveThreshold || coordinates.X <= lastCoordinates.Value.X - MoveThreshold)
            {
                return true;
            }

            if (coordinates.Y >= lastCoordinates.Value.Y + MoveThreshold || coordinates.Y <= lastCoordinates.Value.Y - MoveThreshold)
            {
                return true;
            }

            return false;
        }
    }
}
