using RealStereo.Balancing;
using RealStereo.Balancing.Speaker;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RealStereo.Ui
{
    public partial class VolumeInterpolationDebugWindow : Window
    {
        private WorkerThread workerThread;
        private int speakerIndex;
        private Rectangle currentPosition;

        public VolumeInterpolationDebugWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the current worker thread.
        /// </summary>
        /// <param name="workerThread">Worker thread.</param>
        public void SetWorkerThread(WorkerThread workerThread)
        {
            this.workerThread = workerThread;
        }

        /// <summary>
        /// Set the currently selected speaker.
        /// </summary>
        /// <param name="speakerIndex">Speaker index.</param>
        public void SetSpeakerIndex(int speakerIndex)
        {
            this.speakerIndex = speakerIndex;
        }

        /// <summary>
        /// Draw the interpolation results.
        /// </summary>
        public void Draw()
        {
            VolumeInterpolation volumeInterpolation = workerThread.GetVolumeInterpolation();
            int scale = volumeInterpolation.GetScale();
            double[] minMax = GetMinMax();

            // draw result for all coordinates
            for (int x = 0; x < volumeInterpolation.Values.GetLength(0); x++)
            {
                for (int y = 0; y < volumeInterpolation.Values.GetLength(1); y++)
                {
                    Rectangle rect = new Rectangle();
                    rect.Width = scale;
                    rect.Height = scale;
                    byte intensity = (byte)(255 - (255 / (minMax[1] - minMax[0])) * (volumeInterpolation.Values[x, y, speakerIndex, 0] - minMax[0]));
                    rect.Fill = new SolidColorBrush(Color.FromRgb(intensity, intensity, 255));

                    canvas.Children.Add(rect);
                    Canvas.SetLeft(rect, x * scale);
                    Canvas.SetTop(rect, (volumeInterpolation.Values.GetLength(1) - y) * scale);
                }
            }

            // add the current position
            currentPosition = new Rectangle();
            currentPosition.Width = scale;
            currentPosition.Height = scale;
            currentPosition.Fill = Brushes.Orange;
            canvas.Children.Add(currentPosition);
            Canvas.SetLeft(currentPosition, -10);
            Canvas.SetTop(currentPosition, -10);

            workerThread.ResultReady += ResultReady;
        }

        /// <summary>
        /// Gets the global minimum and maximum of the interpolation.
        /// </summary>
        /// <returns>Array where minimum is stored at index 0 and maximum at index 1.</returns>
        private double[] GetMinMax()
        {
            VolumeInterpolation volumeInterpolation = workerThread.GetVolumeInterpolation();
            double[] minMax = new double[2] { 1000, -1000 };

            for (int x = 0; x < volumeInterpolation.Values.GetLength(0); x++)
            {
                for (int y = 0; y < volumeInterpolation.Values.GetLength(1); y++)
                {
                    double value = volumeInterpolation.Values[x, y, speakerIndex, 0];

                    if (value < minMax[0])
                    {
                        minMax[0] = value;
                    }

                    if (value > minMax[1])
                    {
                        minMax[1] = value;
                    }
                }
            }

            return minMax;
        }

        /// <summary>
        /// Update the current position when it changes.
        /// </summary>
        /// <param name="sender">Worker thread.</param>
        /// <param name="e">Event arguments.</param>
        private void ResultReady(object sender, ResultReadyEventArgs e)
        {
            if (e.Result.GetCoordinates().HasValue)
            {
                VolumeInterpolation volumeInterpolation = workerThread.GetVolumeInterpolation();
                int scale = volumeInterpolation.GetScale();
                Canvas.SetLeft(currentPosition, volumeInterpolation.MapCoordinate(e.Result.GetCoordinates().Value.X) * scale);
                Canvas.SetTop(currentPosition, (volumeInterpolation.Values.GetLength(1) - volumeInterpolation.MapCoordinate(e.Result.GetCoordinates().Value.Y)) * scale);
            }
        }

        /// <summary>
        /// Stop listening on position updates when the window closes.
        /// </summary>
        /// <param name="sender">Window.</param>
        /// <param name="e">Event arguments.</param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            workerThread.ResultReady -= ResultReady;
        }
    }
}
