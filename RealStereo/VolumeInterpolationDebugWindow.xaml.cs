using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RealStereo
{
    /// <summary>
    /// Interaction logic for VolumeInterpolationDebugWindow.xaml
    /// </summary>
    public partial class VolumeInterpolationDebugWindow : Window
    {
        private VolumeInterpolation volumeInterpolation;
        private int speakerIndex;

        public VolumeInterpolationDebugWindow()
        {
            InitializeComponent();
        }

        public void SetVolumeInterpolation(VolumeInterpolation volumeInterpolation)
        {
            this.volumeInterpolation = volumeInterpolation;
        }

        public void SetSpeakerIndex(int speakerIndex)
        {
            this.speakerIndex = speakerIndex;
        }

        public void Draw()
        {
            int scale = volumeInterpolation.GetScale();
            double[] minMax = GetMinMax();

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
        }

        private double[] GetMinMax()
        {
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
    }
}
