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
        private Dictionary<string, int> videoDeviceNameIndexDictionary = new Dictionary<string, int>();

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
            for (int i = 0; i < videoDevices.Count; i++)
            {
                videoDeviceNameIndexDictionary.Add(videoDevices[i].Name, i);
                camera1ComboBox.Items.Add(new string(videoDevices[i].Name));
                camera2ComboBox.Items.Add(new string(videoDevices[i].Name));
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

        private void cameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBox otherComboBox = comboBox == camera1ComboBox ? camera2ComboBox : camera1ComboBox;
            Image camera = comboBox == camera1ComboBox ? camera1 : camera2;
            Image otherCamera = comboBox == camera1ComboBox ? camera2 : camera1;

            if (comboBox.SelectedItem as string != "None")
            {
                if (otherComboBox.SelectedItem as string == comboBox.SelectedItem as string)
                {
                    cameras.Remove(otherCamera);
                    otherCamera.Source = null;
                    otherComboBox.SelectedItem = "None";
                }

                cameras[camera] = new Camera(videoDeviceNameIndexDictionary[comboBox.SelectedItem as string], peopleDetector);
            } else
            {
                cameras.Remove(camera);
                camera.Source = null;
            }
        }
    }
}
