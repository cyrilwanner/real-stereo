using AForge.Video.DirectShow;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Image = System.Windows.Controls.Image;

namespace RealStereo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<Image, Camera> cameras = new Dictionary<Image, Camera>();
        private WorkerThread workerThread;
        private FilterInfoCollection videoDevices;
        private Dictionary<string, int> videoDeviceNameIndexDictionary = new Dictionary<string, int>();
        private bool isBalancing = false;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(StartWorkerThread);
            Loaded += new RoutedEventHandler(LoadCameras);
        }

        private void StartWorkerThread(object sender, RoutedEventArgs e)
        {
            workerThread = new WorkerThread(ref cameras);
            workerThread.ResultReady += ResultReady;
        }

        private void LoadCameras(object sender, RoutedEventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < videoDevices.Count; i++)
            {
                String deviceName = videoDevices[i].Name;
                String initialDeviceName = deviceName;
                int deviceNumber = 1;

                while (videoDeviceNameIndexDictionary.ContainsKey(deviceName))
                {
                    deviceNumber++;
                    deviceName = initialDeviceName + " " + deviceNumber;
                }

                videoDeviceNameIndexDictionary.Add(deviceName, i);
                camera1ComboBox.Items.Add(new string(deviceName));
                camera2ComboBox.Items.Add(new string(deviceName));
            }
        }

        private void ResultReady(object sender, ResultReadyEventArgs e)
        {
            if (e.Result.GetCoordinates() != null)
            {
                coordinatesTextBlock.Text = "Point(" + e.Result.GetCoordinates().X + ", " + e.Result.GetCoordinates().Y + ")";
            }

            if (e.Result.GetFrames() != null)
            {
                for (int i = 0; i < e.Result.GetFrames().Length; i++)
                {
                    cameras.Keys.ElementAt(i).Source = e.Result.GetFrames()[i];
                }
            }
        }

        private void ToggleBalancing(object sender, RoutedEventArgs e)
        {

            isBalancing = !isBalancing;
            workerThread.SetBalancing(isBalancing);

            startBalancingButton.Content = (isBalancing ? "Stop" : "Start") + " Balancing";
        }

        private void UpdateChannelLevelList()
        {
            if (channelLevelsPanel == null)
            {
                return;
            }

            channelLevelsPanel.Children.Clear();

            if (audioOutputComboBox.SelectedItem is MMDevice)
            {
                MMDevice audioOut = (MMDevice) audioOutputComboBox.SelectedItem;

                for (int channelIndex = 0; channelIndex < audioOut.AudioEndpointVolume.Channels.Count; channelIndex++)
                {
                    Label label = new Label();
                    if (AudioChannelMap.Map.ContainsKey(channelIndex))
                    {
                        label.Content = AudioChannelMap.Map[channelIndex];
                    } else
                    {
                        label.Content = "Channel " + (channelIndex + 1);
                    }
                    label.Margin = new Thickness(0, channelIndex > 0 ? 5 : 0, 0, 0);
                    channelLevelsPanel.Children.Add(label);

                    ProgressBar progressBar = new ProgressBar();
                    progressBar.Value = audioOut.AudioEndpointVolume.Channels[channelIndex].VolumeLevelScalar * 100;
                    channelLevelsPanel.Children.Add(progressBar);
                }
            }

            UpdateChannelLevels();
        }

        private void UpdateChannelLevels()
        {
            if (audioOutputComboBox.SelectedItem is MMDevice)
            {
                MMDevice audioOut = (MMDevice)audioOutputComboBox.SelectedItem;

                for (int channelIndex = 0; channelIndex < audioOut.AudioEndpointVolume.Channels.Count; channelIndex++)
                {
                    ProgressBar progressBar = (ProgressBar) VisualTreeHelper.GetChild(channelLevelsPanel, channelIndex * 2 + 1);

                    progressBar.Value = audioOut.AudioEndpointVolume.Channels[channelIndex].VolumeLevelScalar * 100;

                    if (progressBar.Value >= 90)
                    {
                        progressBar.Foreground = Brushes.Red;
                    }
                    else if (progressBar.Value >= 70)
                    {
                        progressBar.Foreground = Brushes.Orange;
                    }
                    else
                    {
                        progressBar.Foreground = Brushes.Green;
                    }
                }
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

                cameras[camera] = new Camera(videoDeviceNameIndexDictionary[comboBox.SelectedItem as string], new PeopleDetector());
            } else
            {
                cameras.Remove(camera);
                camera.Source = null;
            }
        }

        private void audioDeviceComboBox_DropDownOpened(object sender, EventArgs e)
        {
            MMDeviceEnumerator audioDeviceEnumerator = new MMDeviceEnumerator();
            MMDeviceCollection audioDeviceCollection;
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox == audioOutputComboBox)
            {
                audioDeviceCollection = audioDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            } else
            {
                audioDeviceCollection = audioDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            }

            object selectedItem = comboBox.SelectedItem;
            comboBox.Items.Clear();
            comboBox.Items.Add("None");
            foreach (MMDevice audioDevice in audioDeviceCollection)
            {
                comboBox.Items.Add(audioDevice);
                if (selectedItem.ToString() == audioDevice.ToString())
                {
                    comboBox.SelectedItem = audioDevice;
                }
            }
            if (comboBox.SelectedItem == null)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void audioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            MMDevice audioDevice = e.AddedItems.Count > 0 && e.AddedItems[0] is MMDevice ? (MMDevice)e.AddedItems[0] : null;

            if (comboBox == audioOutputComboBox)
            {
                UpdateChannelLevelList();
            }
            else
            {
                // Input device
            }
        }

        private void EditConfiguration(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow window = new ConfigurationWindow();
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = Left + Width;
            window.Top = Top;
            window.Height = Height;
            window.ShowDialog();
        }
    }
}
