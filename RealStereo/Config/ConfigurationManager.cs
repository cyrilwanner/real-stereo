using RealStereo.Balancing;
using RealStereo.Config.Steps;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace RealStereo.Config
{
    public class ConfigurationManager
    {
        private ConfigurationStep[] steps;
        private int currentStep = 0;
        private List<PointConfiguration> configurations = new List<PointConfiguration>();
        private PointConfiguration currentConfiguration = new PointConfiguration();
        private TextBlock instructionsText;
        private Border instructionsBox;
        private ProgressBar audioInputDeviceVolume;
        private StackPanel positions;
        private Button saveButton;
        private bool terminated = false;

        public ConfigurationManager(ref WorkerThread workerThread, TextBlock instructionsText, Border instructionsBox, ProgressBar audioInputDeviceVolume, StackPanel positions, Button saveButton)
        {
            steps = new ConfigurationStep[] {
                new ConfigurationStepCamera(this, ref workerThread),
                new ConfigurationStepSpeaker(this, ref workerThread),
            };

            this.instructionsText = instructionsText;
            this.instructionsBox = instructionsBox;
            this.audioInputDeviceVolume = audioInputDeviceVolume;
            this.positions = positions;
            this.saveButton = saveButton;
        }

        /// <summary>
        /// Start a new configuration.
        /// </summary>
        public void Start()
        {
            steps[currentStep].Start();
        }

        /// <summary>
        /// Proceed to the next configuration step.
        /// If it is already the last step, configuration will start over at a new position.
        /// </summary>
        public void NextStep()
        {
            currentConfiguration = steps[currentStep].Finish(currentConfiguration);

            if (currentStep + 1 < steps.Length)
            {
                currentStep++;
                Start();
            }
            else
            {
                NextPosition();
            }
        }

        /// <summary>
        /// Proceed to the next position.
        /// </summary>
        private void NextPosition()
        {
            // update UI
            List<CheckBox> checkboxes = positions.Children.OfType<CheckBox>().ToList();
            checkboxes[configurations.Count].IsChecked = true;

            // start next position
            configurations.Add(currentConfiguration);
            currentStep = 0;
            currentConfiguration = new PointConfiguration();
            Start();

            // add new UI checkbox if needed
            if (checkboxes.Count == configurations.Count)
            {
                saveButton.IsEnabled = true;

                CheckBox checkbox = new CheckBox();
                checkbox.IsChecked = false;
                checkbox.IsEnabled = false;
                checkbox.Content = "Middle " + (checkboxes.Count - 3);
                positions.Children.Add(checkbox);
            }
        }

        /// <summary>
        /// Cancel the current configuration and all steps.
        /// </summary>
        public void Cancel()
        {
            foreach (ConfigurationStep step in steps)
            {
                step.Cancel();
            }

            currentStep = 0;
        }

        /// <summary>
        /// Sets the instruction text.
        /// </summary>
        /// <param name="text">Instructions.</param>
        public void SetInstructions(string text)
        {
            instructionsBox.BorderBrush = Brushes.Green;
            instructionsText.Text = text;
        }

        /// <summary>
        /// Sets the error text.
        /// </summary>
        /// <param name="text">Error.</param>
        public void SetError(string text)
        {
            instructionsBox.BorderBrush = Brushes.Red;
            instructionsText.Text = text;
        }

        /// <summary>
        /// Sets the input audio device volume.
        /// </summary>
        /// <param name="value">Volume.</param>
        public void SetAudioInputDeviceVolume(float value)
        {
            audioInputDeviceVolume.Value = value;
        }

        /// <summary>
        /// Get all configured positions.
        /// </summary>
        /// <returns>Configured positions.</returns>
        public List<PointConfiguration> GetConfigurations()
        {
            return configurations;
        }

        /// <summary>
        /// Terminates the configuration.
        /// </summary>
        public void Terminate()
        {
            terminated = true;
        }

        /// <summary>
        /// Whether the configuration is terminated or not.
        /// </summary>
        /// <returns></returns>
        public bool IsTerminated()
        {
            return terminated;
        }
    }
}
