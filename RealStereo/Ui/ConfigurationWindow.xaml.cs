using RealStereo.Balancing;
using RealStereo.Config;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RealStereo.Ui
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

        /// <summary>
        /// Initializes the configuration window.
        /// </summary>
        /// <param name="workerThread">Used worker thread.</param>
        public void InitConfiguration(ref WorkerThread workerThread)
        {
            manager = new ConfigurationManager(ref workerThread, instructionsText, instructionsBox, audioInputDeviceVolume, positions, saveButton);
            this.workerThread = workerThread;
            UpdateStartButton();
        }

        /// <summary>
        /// Updates the enabled state of the start button.
        /// Requires all devices to be selected and a unique name for an enabled button.
        /// </summary>
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

        /// <summary>
        /// Handles a click on the cancel button.
        /// Will cancel the whole configuration.
        /// </summary>
        /// <param name="sender">Button.</param>
        /// <param name="e">Event arguments.</param>
        private void CancelConfiguration(object sender, object e)
        {
            manager.Cancel();
            manager.Terminate();
            Close();
        }

        /// <summary>
        /// Handles a click on the start button.
        /// Will start the configuration.
        /// </summary>
        /// <param name="sender">Button.</param>
        /// <param name="e">Event arguments.</param>
        private void StartConfiguration(object sender, RoutedEventArgs e)
        {
            startCalibrationButton.IsEnabled = false;
            positions.Visibility = Visibility.Visible;
            instructionsBox.Visibility = Visibility.Visible;
            audioInputDeviceVolume.Visibility = Visibility.Visible;
            manager.Start();
        }

        /// <summary>
        /// Handles the window closing event.
        /// Will stop the configuration.
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">Event arguments</param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (manager != null)
            {
                manager.Cancel();
                manager.Terminate();
            }
        }

        /// <summary>
        /// Handles a click on the save button.
        /// Will save the configuration to the file.
        /// </summary>
        /// <param name="sender">Button.</param>
        /// <param name="e">Event arguments.</param>
        private void SaveConfiguration(object sender, RoutedEventArgs e)
        {
            manager.Cancel();
            Configuration.GetInstance().Rooms.Add(roomNameTextBox.Text, manager.GetConfigurations());
            Configuration.GetInstance().Save();
            manager.Terminate();
            Close();
        }

        /// <summary>
        /// Handles a change in the room name textbox.
        /// Will update the enabled state of the start button based on the uniqueness of the name.
        /// </summary>
        /// <param name="sender">Text box</param>
        /// <param name="e">Event arguments.</param>
        private void RoomNameChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStartButton();
        }
    }
}
