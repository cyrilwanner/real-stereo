using AForge.Video.DirectShow;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private PeopleDetector peopleDetector;
        private FilterInfoCollection videoDevices;

        public MainWindow()
        {
            InitializeComponent();

            // create HOG descriptor
            peopleDetector = new PeopleDetector();

            Loaded += new RoutedEventHandler(LoadCameras);
            Loaded += new RoutedEventHandler(StartCameras);
        }

        private void LoadCameras(object sender, RoutedEventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo camera in videoDevices)
            {
                camera1ComboBox.Items.Add(camera.Name);
                camera2ComboBox.Items.Add(camera.Name);
            }
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

        private void camera1ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = videoDevices
                .OfType<FilterInfo>()
                .Select((Value, Index) => new { Value, Index })
                .Single(i => i.Value.Name == e.AddedItems[0] as string);

            camera2ComboBox.Items.Clear();
            LoadCameras(null, null);
            camera2ComboBox.Items.RemoveAt(device.Index);
            cameras[camera1] = new Camera(device.Index, peopleDetector);
        }

        private void camera2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = videoDevices
                .OfType<FilterInfo>()
                .Select((Value, Index) => new { Value, Index })
                .Single(i => i.Value.Name == e.AddedItems[0] as string);

            camera1ComboBox.Items.Clear();
            LoadCameras(null, null);
            camera1ComboBox.Items.RemoveAt(device.Index);
            cameras[camera2] = new Camera(device.Index, peopleDetector);
        }
    }
}
