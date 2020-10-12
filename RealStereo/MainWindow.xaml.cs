using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;
using Point = System.Drawing.Point;

namespace RealStereo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<Image, Camera> cameras = new Dictionary<Image, Camera>();
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            // create HOG descriptor
            PeopleDetector peopleDetector = new PeopleDetector();

            // register cameras
            cameras.Add(camera1, new Camera(0, peopleDetector));
            cameras.Add(camera2, new Camera(1, peopleDetector));

            Loaded += new RoutedEventHandler(StartCameras);
        }

        private void StartCameras(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(UpdateCoordinates);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
        }

        private void UpdateCoordinates(object sender, EventArgs e)
        {
            Point coordinates = new Point(0, 0);

            for (int i = 0; i < cameras.Keys.Count; i++)
            {
                Image image = cameras.Keys.ElementAt(i);
                Camera camera = cameras[image];

                camera.Process();
                image.Source = camera.GetFrame();
                Point? cameraCoordinates = camera.GetCoordinates(i % 2 == 0 ? Orientation.Horizontal : Orientation.Vertical);

                // if a camera is not ready or didn't detect a person, cancel coordinates calculation
                if (cameraCoordinates == null)
                {
                    return;
                }

                coordinates.X = Math.Max(coordinates.X, cameraCoordinates.Value.X);
                coordinates.Y = Math.Max(coordinates.Y, cameraCoordinates.Value.Y);
            }

            coordinatesTextBlock.Text = "Point(" + coordinates.X + ", " + coordinates.Y + ")";
        }
    }
}
