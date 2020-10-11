using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
            timer.Tick += new EventHandler(TimerTick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            foreach (KeyValuePair<Image, Camera> entry in cameras)
            {
                entry.Value.Process();
                entry.Key.Source = entry.Value.GetFrame();
            }
        }
    }
}
