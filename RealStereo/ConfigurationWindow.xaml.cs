using System.Windows;

namespace RealStereo
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private ConfigurationManager manager;

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        public void InitConfiguration(ref WorkerThread workerThread)
        {
            manager = new ConfigurationManager(ref workerThread, instructionsText, instructionsBox, audioInputDeviceVolume);

            // if all devices are set, enable the start button
            if (workerThread.GetCameras().Keys.Count >= 2 && workerThread.GetOutputAudioDevice() != null && workerThread.GetInputAudioDevice() != null)
            {
                startCalibrationButton.IsEnabled = true;
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
    }
}
