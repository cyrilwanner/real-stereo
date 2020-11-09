using System.Windows;

namespace RealStereo
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private ConfigurationManager manager;
        private WorkerThread workerThread;

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        public void InitConfiguration(ref WorkerThread workerThread)
        {
            manager = new ConfigurationManager(ref workerThread, instructionsText, instructionsBox, audioInputDeviceVolume, positions, saveButton);
            this.workerThread = workerThread;
            UpdateStartButton();
        }

        private void UpdateStartButton()
        {
            if (workerThread.GetCameras().Keys.Count >= 2 && workerThread.GetOutputAudioDevice() != null && workerThread.GetInputAudioDevice() != null && roomNameTextBox.Text.Trim().Length > 0 && !Configuration.GetInstance().Rooms.ContainsKey(roomNameTextBox.Text))
            {
                startCalibrationButton.IsEnabled = true;
            }
            else
            {
                startCalibrationButton.IsEnabled = false;
            }
        }

        private void CancelConfiguration(object sender, object e)
        {
            manager.Cancel();
            Close();
        }

        private void StartConfiguration(object sender, RoutedEventArgs e)
        {
            startCalibrationButton.IsEnabled = false;
            positions.Visibility = Visibility.Visible;
            instructionsBox.Visibility = Visibility.Visible;
            audioInputDeviceVolume.Visibility = Visibility.Visible;
            manager.Start();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (manager != null)
            {
                manager.Cancel();
            }
        }

        private void SaveConfiguration(object sender, RoutedEventArgs e)
        {
            manager.Cancel();
            Configuration.GetInstance().Rooms.Add(roomNameTextBox.Text, manager.GetConfigurations());
            Configuration.GetInstance().Save();
            Close();
        }

        private void RoomNameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateStartButton();
        }
    }
}
