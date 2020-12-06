namespace RealStereo.Config.Steps
{
    interface ConfigurationStep
    {
        public void Start();
        public PointConfiguration Finish(PointConfiguration currentConfiguration);
        public void Cancel();
    }
}
