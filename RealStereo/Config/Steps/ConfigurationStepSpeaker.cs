using NAudio.CoreAudioApi;
using NAudio.Wave;
using RealStereo.Balancing;
using System;
using System.Collections.Generic;

namespace RealStereo.Config.Steps
{
    public class ConfigurationStepSpeaker : ConfigurationStep
    {
        private static int TEST_TONE_LENGTH = 2;
        private static float TEST_TONE_SCALING_TARGET = 0.5f;
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

        public void Start()
        {
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            MMDevice inputAudioDevice = workerThread.GetInputAudioDevice();
            isCanceled = false;
            volumes = new Dictionary<int, float[]>();
            originalChannelVolume.Clear();
            speakerStep = 1;
            for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
            {
                originalChannelVolume.Add(i, outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar);
            }

            testTone = new TestTone(outputAudioDevice, inputAudioDevice);

            if (volumeScalingFactor == 0)
            {
                manager.SetInstructions("Calibrating max volume");
                testTone.Play(TEST_TONE_LENGTH, delegate (object sender, StoppedEventArgs e)
                {
                    float maxVolume = testTone.GetAverageCaptureVolume();
                    volumeScalingFactor = TEST_TONE_SCALING_TARGET / maxVolume;
                    manager.SetAudioInputDeviceVolume(maxVolume * volumeScalingFactor);

                    MuteAllChannels();
                    manager.SetInstructions("Calibrating speakers channel " + (speakerStep / 2) + " - Step 1");
                    outputAudioDevice.AudioEndpointVolume.Channels[0].VolumeLevelScalar = originalChannelVolume[0];
                    testTone.Play(TEST_TONE_LENGTH, new EventHandler<StoppedEventArgs>(TestToneStopped));
                });
            }
            else
            {
                MuteAllChannels();
                manager.SetInstructions("Calibrating speakers channel " + (speakerStep / 2) + " - Step 1");
                outputAudioDevice.AudioEndpointVolume.Channels[0].VolumeLevelScalar = originalChannelVolume[0];
                testTone.Play(TEST_TONE_LENGTH, new EventHandler<StoppedEventArgs>(TestToneStopped));
            }
        }

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

        private void TestToneStopped(object sender, StoppedEventArgs e)
        {
            if (isCanceled)
            {
                return;
            }
            MMDevice outputAudioDevice = workerThread.GetOutputAudioDevice();
            if (speakerStep >= outputAudioDevice.AudioEndpointVolume.Channels.Count * 2)
            {
                manager.NextStep();
                return;
            }

            MuteAllChannels();
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
                // New channel

                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex];
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 1");
                testTone.Play(TEST_TONE_LENGTH, new EventHandler<StoppedEventArgs>(TestToneStopped));
            } else
            {
                // Half volume same channel

                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex] / 2;
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 2");
                testTone.Play(TEST_TONE_LENGTH, new EventHandler<StoppedEventArgs>(TestToneStopped));
                // Detect volume for this channel
            }
        }

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
