using MediaFoundation;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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
        private Dictionary<string, int> videoDeviceNameIndexDictionary = new Dictionary<string, int>();
        private bool isBalancing = false;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(StartWorkerThread);
        }

        private void StartWorkerThread(object sender, RoutedEventArgs e)
        {
            workerThread = new WorkerThread(ref cameras);
            workerThread.ResultReady += ResultReady;
        }

        private void ResultReady(object sender, ResultReadyEventArgs e)
        {
            if (e.Result.GetCoordinates().HasValue)
            {
                coordinatesTextBlock.Text = "Point(" + e.Result.GetCoordinates().Value.X + ", " + e.Result.GetCoordinates().Value.Y + ")";
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

        private void cameraComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            videoDeviceNameIndexDictionary.Clear();
            object selectedItem = comboBox.SelectedItem;
            comboBox.Items.Clear();
            comboBox.Items.Add("None");

            IMFActivate[] devices;
            HResult hr = MF.EnumVideoDeviceSources(out devices);
            hr.ThrowExceptionOnError();
            for (int i = 0; i < devices.Length; i++)
            {
                string friendlyName;
                hr = devices[i].GetAllocatedString(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, out friendlyName);
                hr.ThrowExceptionOnError();

                int deviceNumber = 1;
                while (videoDeviceNameIndexDictionary.ContainsKey(friendlyName))
                {
                    deviceNumber++;
                    friendlyName = friendlyName + " " + deviceNumber;
                }

                videoDeviceNameIndexDictionary.Add(friendlyName, i);
                comboBox.Items.Add(friendlyName);
                if (selectedItem.ToString() == friendlyName)
                {
                    comboBox.SelectedItem = friendlyName;
                }
            }
            if (comboBox.SelectedItem == null)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void cameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.SelectedItem == null)
            {
                return;
            }
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
            ComboBox comboBox = (ComboBox)sender;
            MMDeviceEnumerator audioDeviceEnumerator = new MMDeviceEnumerator();
            MMDeviceCollection audioDeviceCollection;

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
            if (comboBox.SelectedItem == null)
            {
                return;
            }
            MMDevice audioDevice = comboBox.SelectedItem is MMDevice ? (MMDevice)comboBox.SelectedItem : null;
            if (audioDevice == null)
            {
                return;
            }

            if (comboBox == audioOutputComboBox)
            {
                UpdateChannelLevelList();
                workerThread.SetOutputAudioDevice(audioDevice);
            }
            else
            {
                workerThread.SetInputAudioDevice(audioDevice);
            }
        }

        private void roomComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            object selectedItem = comboBox.SelectedItem;
            comboBox.Items.Clear();
            comboBox.Items.Add("None");
            comboBox.SelectedIndex = 0;

            foreach (string roomName in Configuration.GetInstance().Rooms.Keys)
            {
                comboBox.Items.Add(roomName);

                if (roomName == selectedItem.ToString())
                {
                    comboBox.SelectedItem = roomName;
                }
            }

            if (selectedItem == null)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void roomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (startBalancingButton == null)
            {
                return;
            }

            ComboBox comboBox = sender as ComboBox;
            if (comboBox.SelectedItem == null || comboBox.SelectedItem.ToString() == "None")
            {
                startBalancingButton.IsEnabled = false;
            }
            else
            {
                startBalancingButton.IsEnabled = true;
                Configuration.GetInstance().SelectedRoom = comboBox.SelectedItem.ToString();
                Configuration.GetInstance().Save();
            }
        }

        private void EditConfiguration(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow window = new ConfigurationWindow();
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = Left + Width;
            window.Top = Top;
            window.Height = Height;
            window.InitConfiguration(ref workerThread);
            window.ShowDialog();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            workerThread.Stop();
            Application.Current.Shutdown();
        }
    }
}
