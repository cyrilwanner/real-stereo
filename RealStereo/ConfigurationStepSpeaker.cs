
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RealStereo
{
    public class ConfigurationStepSpeaker : ConfigurationStep
    {
        private ConfigurationManager manager;
        private WorkerThread workerThread;
        private TestTone testTone;
        private int speakerStep;
        private Dictionary<int, float> originalChannelVolume = new Dictionary<int, float>();
        Dictionary<int, float[]> volumes = new Dictionary<int, float[]>();
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
            originalChannelVolume.Clear();
            speakerStep = 1;
            for (int i = 0; i < outputAudioDevice.AudioEndpointVolume.Channels.Count; i++)
            {
                originalChannelVolume.Add(i, outputAudioDevice.AudioEndpointVolume.Channels[i].VolumeLevelScalar);
            }
            MuteAllChannels();

            testTone = new TestTone(outputAudioDevice, inputAudioDevice);
            manager.SetInstructions("Calibrating speakers channel " + (int) (speakerStep / 2) + " - Step 1");
            outputAudioDevice.AudioEndpointVolume.Channels[0].VolumeLevelScalar = originalChannelVolume[0];
            testTone.Play(2, new EventHandler<StoppedEventArgs>(TestToneStopped));
            // Detect volume for this channel
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

        public Configuration Finish(Configuration currentConfiguration)
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
            if (!volumes.ContainsKey((int)(speakerStep - 1) / 2))
            {
                volumes[(int)(speakerStep - 1) / 2] = new float[2];
            }
            volumes[(int) (speakerStep-1) / 2][(speakerStep-1) % 2] = testTone.GetAverageCaptureVolume();

            AudioEndpointVolumeChannel audioEndpointVolume = outputAudioDevice.AudioEndpointVolume.Channels[channelIndex];
            if (speakerStep % 2 == 0)
            {
                // New channel

                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex];
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 1");
                testTone.Play(2, new EventHandler<StoppedEventArgs>(TestToneStopped));
            } else
            {
                // Half volume same channel

                audioEndpointVolume.VolumeLevelScalar = originalChannelVolume[channelIndex] / 2;
                speakerStep++;
                manager.SetInstructions("Calibrating speakers channel " + channelIndex + " - Step 2");
                testTone.Play(2, new EventHandler<StoppedEventArgs>(TestToneStopped));
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
