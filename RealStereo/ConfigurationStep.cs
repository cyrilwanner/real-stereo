
namespace RealStereo
{
    interface ConfigurationStep
    {
        public void Start();
        public PointConfiguration Finish(PointConfiguration currentConfiguration);
        public void Cancel();
    }
}
