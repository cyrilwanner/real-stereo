using System.Collections.Generic;
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

        public ConfigurationManager(ref WorkerThread workerThread, TextBlock instructionsText, Border instructionsBox)
        {
            steps = new ConfigurationStep[] {
                new ConfigurationStepCamera(this, ref workerThread),
                new ConfigurationStepSpeaker(this),
            };

            this.instructionsText = instructionsText;
            this.instructionsBox = instructionsBox;
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
                configurations.Add(currentConfiguration);
                NextPosition();
            }
        }

        private void NextPosition()
        {
            currentStep = 0;
            currentConfiguration = new Configuration();
            Start();
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
    }
}
