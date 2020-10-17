using AForge.Video.DirectShow;
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
        private PeopleDetector peopleDetector;
        private FilterInfoCollection videoDevices;
        private Dictionary<string, int> videoDeviceNameIndexDictionary = new Dictionary<string, int>();

        public MainWindow()
        {
            InitializeComponent();

            // create HOG descriptor
            peopleDetector = new PeopleDetector();

            Loaded += new RoutedEventHandler(LoadAudioDevices);
            Loaded += new RoutedEventHandler(LoadCameras);
            Loaded += new RoutedEventHandler(StartCameras);
        }

        private void LoadAudioDevices(object sender, RoutedEventArgs e)
        {

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
