using NAudio.CoreAudioApi;
using System.Threading;

namespace RealStereo.Balancing.Speaker
{
    class VolumeFader
    {
        private WorkerThread workerThread;
        private volatile float[] targetVolumes;
        private Thread thread;

        public VolumeFader(WorkerThread workerThread)
        {
            this.workerThread = workerThread;
        }

        /// <summary>
        /// Set the target volume for all channels.
        /// The volume will then fade to the new target.
        /// </summary>
        /// <param name="channels">Target volume for all channels.</param>
        public void Set(float[] channels)
        {
            targetVolumes = channels;

            if (thread == null || !thread.IsAlive)
            {
                thread = new Thread(Run);
                thread.Start();
            }
        }

        /// <summary>
        /// Stops the thread and so the volume fading.
        /// </summary>
        public void Cancel()
        {
            if (thread != null && thread.IsAlive)
            {
                thread.Interrupt();
            }
        }

        /// <summary>
        /// Main thread function. Fades the volume for all channels to the target volume.
        /// </summary>
        private void Run()
        {
            try
            {
                while (true)
                {
                    MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
                    bool allAtTarget = true;

                    for (int i = 0; i < targetVolumes.Length; i++)
                    {
                        if (i == 0)
                        {
                            float current = outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar;

                            // check if the target volume is already reached
                            if (current >= targetVolumes[i] - 0.0075 && current <= targetVolumes[i] + 0.0075)
                            {
                                continue;
                            }

                            allAtTarget = false;

                            if (current > targetVolumes[i])
                            {
                                outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar = current - 0.01f;
                            }
                            else
                            {
                                outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar = current + 0.01f;
                            }
                        }
                    }

                    if (allAtTarget)
                    {
                        break;
                    }

                    Thread.Sleep(30);
                }
            }
            catch (ThreadInterruptedException)
            {}
        }
    }
}
