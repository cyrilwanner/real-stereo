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
            manager = new ConfigurationManager(ref workerThread, instructionsText, instructionsBox);

            // if all devices are set, enable the start button
            if (workerThread.GetCameras().Keys.Count >= 2)
            {
                startCalibrationButton.IsEnabled = true;
            }
        }

        private void CancelConfiguration(object sender, RoutedEventArgs e)
        {
            manager.Cancel();
            Close();
        }

        private void StartConfiguration(object sender, RoutedEventArgs e)
        {
            startCalibrationButton.IsEnabled = false;
            positions.Visibility = Visibility.Visible;
            instructionsBox.Visibility = Visibility.Visible;
            manager.Start();
        }
    }
}
