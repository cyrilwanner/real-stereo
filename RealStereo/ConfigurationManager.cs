using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace RealStereo
{
    public class ConfigurationManager
    {
        private ConfigurationStep[] steps;
        private int currentStep = 0;
        private List<Configuration> configurations = new List<Configuration>();
        private Configuration currentConfiguration = new Configuration();
        private TextBlock instructionsText;
        private Border instructionsBox;
        private ProgressBar audioInputDeviceVolume;
        private StackPanel positions;

        public ConfigurationManager(ref WorkerThread workerThread, TextBlock instructionsText, Border instructionsBox, ProgressBar audioInputDeviceVolume, StackPanel positions)
        {
            steps = new ConfigurationStep[] {
                new ConfigurationStepCamera(this, ref workerThread),
                new ConfigurationStepSpeaker(this, ref workerThread),
            };

            this.instructionsText = instructionsText;
            this.instructionsBox = instructionsBox;
            this.audioInputDeviceVolume = audioInputDeviceVolume;
            this.positions = positions;
        }

        public void Start()
        {
            steps[currentStep].Start();
        }

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

        private void NextPosition()
        {
            // update UI
            List<CheckBox> checkboxes = positions.Children.OfType<CheckBox>().ToList();
            checkboxes[configurations.Count].IsChecked = true;

            // start next position
            configurations.Add(currentConfiguration);
            currentStep = 0;
            currentConfiguration = new Configuration();
            Start();

            // add new UI checkbox if needed
            if (checkboxes.Count == configurations.Count)
            {
                CheckBox checkbox = new CheckBox();
                checkbox.IsChecked = false;
                checkbox.IsEnabled = false;
                checkbox.Content = "Middle " + (checkboxes.Count - 3);
                positions.Children.Add(checkbox);
            }
        }

        public void Cancel()
        {
            foreach (ConfigurationStep step in steps)
            {
                step.Cancel();
            }

            currentStep = 0;
        }

        public void SetInstructions(string text)
        {
            instructionsBox.BorderBrush = Brushes.Green;
            instructionsText.Text = text;
        }

        public void SetError(string text)
        {
            instructionsBox.BorderBrush = Brushes.Red;
            instructionsText.Text = text;
        }

        public void SetAudioInputDeviceVolume(float value)
        {
            audioInputDeviceVolume.Value = value;
        }
    }
}
