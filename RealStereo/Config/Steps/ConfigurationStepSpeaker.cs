using NAudio.CoreAudioApi;
using NAudio.Wave;
using RealStereo.Balancing;
using System;
using System.Collections.Generic;

namespace RealStereo.Config.Steps
{
    public class ConfigurationStepSpeaker : ConfigurationStep
    {
        private static readonly int TestToneLength = 2;
        private static readonly float TestToneScalingTarget = 0.5f;

        private ConfigurationManager manager;
        private WorkerThread workerThread;
        private TestTone testTone;
        private int speakerStep;
        private float volumeScalingFactor = 0;
        private Dictionary<int, float> originalChannelVolume = new Dictionary<int, float>();
        Dictionary<int, float[]> volumes;
        private bool isCanceled;

        public ConfigurationStepSpeaker(ConfigurationManager manager, ref WorkerThread workerThread)
        {
            this.manager = manager;
            this.workerThread = workerThread;
        }

        /// <summary>
        /// Start the speaker configuration step.
        /// A test tone will be played on all speakers and the volume recorded.
        /// </summary>
        public void Start()
        {
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            MMDevice inputAudioDevice = workerThread.GetInputAudioDevice();
            isCanceled = false;
            volumes = new Dictionary<int, float[]>();
            originalChannelVolume.Clear();
            speakerStep = 1;

            // get the current channel volumes to reset it when the configuration is done
            for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
            {
                originalChannelVolume.Add(i, outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar);
            }

            testTone = new TestTone(outputAudioDevice, inputAudioDevice);

            // if this is the first time this step gets executed, record the volume level and calculate a target to scale it to
            if (volumeScalingFactor == 0)
            {
                manager.SetInstructions("Calibrating max volume");
                testTone.Play(TestToneLength, delegate (object sender, StoppedEventArgs e)
                {
                    float maxVolume = testTone.GetAverageCaptureVolume();
                    volumeScalingFactor = TestToneScalingTarget / maxVolume;
                    manager.SetAudioInputDeviceVolume(maxVolume * volumeScalingFactor);

                    MuteAllChannels();
                    manager.SetInstructions("Calibrating speakers channel " + (speakerStep / 2) + " - Step 1");
                    outputAudioDevice.AudioEndpointVolume.Channels[0].VolumeLevelScalar = originalChannelVolume[0];
                    testTone.Play(TestToneLength, new EventHandler<StoppedEventArgs>(TestToneStopped));
                });
            }
            else
            {
                // start with first channel
                MuteAllChannels();
                manager.SetInstructions("Calibrating speakers channel " + (speakerStep / 2) + " - Step 1");
                outputAudioDevice.AudioEndpointVolume.Channels[0].VolumeLevelScalar = originalChannelVolume[0];
                testTone.Play(TestToneLength, new EventHandler<StoppedEventArgs>(TestToneStopped));
            }
        }

        /// <summary>
        /// Cancel the speaker configuration step.
        /// </summary>
        public void Cancel()
        {
            if (testTone == null)
            {
                return;
            }
            isCanceled = true;
            testTone.Stop();
            foreach(int i in originalChannelVolume.Keys)
            {
                workerThread.GetOutputAudioDevice().AudioEndpointVolume.Channels[i].VolumeLevelScalar = originalChannelVolume[i];
            }
        }

        /// <summary>
        /// Finishes the speaker configuration step by returning the recorded channel volumes.
        /// </summary>
        /// <param name="currentConfiguration">Current configuration.</param>
        /// <returns>Current configuration with recorded channel volumes.</returns>
        public PointConfiguration Finish(PointConfiguration currentConfiguration)
        {
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
            {
                outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar = originalChannelVolume[i];
            }

            currentConfiguration.Volumes = volumes;
            return currentConfiguration;
        }

        /// <summary>
        /// Test tone stopped event.
        /// Calculates the recorded volume and proceeds to the next channel/speaker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestToneStopped(object sender, StoppedEventArgs e)
        {
            if (isCanceled)
            {
                return;
            }

            // if all speakers are configured, proceed to the next step
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            if (speakerStep >= outputAudioDevice.AudioEndpointVolume.Channels.Count * 2)
            {
                manager.NextStep();
                return;
            }

            // mute all channels to be ready for the next step
            MuteAllChannels();

            // store the average recorded volume
            int channelIndex = speakerStep / 2;
            if (!volumes.ContainsKey((speakerStep - 1) / 2))
            {
                volumes[(speakerStep - 1) / 2] = new float[3];
                volumes[(speakerStep - 1) / 2][2] = originalChannelVolume[(speakerStep - 1) / 2];
            }

            float volume = testTone.GetAverageCaptureVolume() * volumeScalingFactor;
            manager.SetAudioInputDeviceVolume(volume);
            volumes[(speakerStep-1) / 2][(speakerStep-1) % 2] = volume;

            AudioEndpointVolumeChannel audioEndpointVolume = outputAudioDevice.AudioEndpointVolume.Channels[channelIndex];
            if (speakerStep % 2 == 0)
            {
                // configure the next channel
                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex];
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 1");
                testTone.Play(TestToneLength, new EventHandler<StoppedEventArgs>(TestToneStopped));
            } else
            {
                // repeat the recording with half of the volume to check how much a volume change influences the recorded volume
                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex] / 2;
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 2");
                testTone.Play(TestToneLength, new EventHandler<StoppedEventArgs>(TestToneStopped));
            }
        }

        /// <summary>
        /// Mute all channels of the current output audio device.
        /// </summary>
        private void MuteAllChannels()
        {
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
            {
                outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar = 0;
            }
        }
    }
}
