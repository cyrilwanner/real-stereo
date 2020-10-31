
namespace RealStereo
{
    public class ConfigurationStepSpeaker : ConfigurationStep
    {
        private ConfigurationManager manager;

        public ConfigurationStepSpeaker(ConfigurationManager manager)
        {
            this.manager = manager;
        }

        public void Start()
        {
            manager.SetInstructions("Calibrating speakers");
        }

        public void Cancel()
        {

        }

        public Configuration Finish(Configuration currentConfiguration)
        {
            return currentConfiguration;
        }
    }
}
