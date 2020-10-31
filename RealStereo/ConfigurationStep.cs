
namespace RealStereo
{
    interface ConfigurationStep
    {
        public void Start();
        public Configuration Finish(Configuration currentConfiguration);
        public void Cancel();
    }
}
