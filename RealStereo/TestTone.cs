using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;

namespace RealStereo
{
    class TestTone
    {
        private SignalGenerator sineSignalGenerator;
        private MMDevice outputAudioDevice, inputAudioDevice;
        private IWavePlayer outWavePlayer;
        private WasapiCapture wasapiCapture;
        private readonly List<float> captureSamples = new List<float>();

        public TestTone(MMDevice outputAudioDevice, MMDevice inputAudioDevice)
        {
            sineSignalGenerator = new SignalGenerator()
            {
                Gain = 1,
                Frequency = 2000,
                Type = SignalGeneratorType.Sin
            };
            this.outputAudioDevice = outputAudioDevice;
            this.inputAudioDevice = inputAudioDevice;
        }

        public void Play(int seconds, EventHandler<StoppedEventArgs> PlaybackStopped = null)
        {
            captureSamples.Clear();
            wasapiCapture = new WasapiCapture(inputAudioDevice);
            wasapiCapture.DataAvailable += new EventHandler<WaveInEventArgs>(delegate (object o, WaveInEventArgs e)
            {
                for (int i = 0; i < e.BytesRecorded; i += 4)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, i);
                    captureSamples.Add(sample);
                }
            });

            outWavePlayer = new WasapiOut(outputAudioDevice, AudioClientShareMode.Shared, false, 250); // High latency (big buffer) of 250, since our camera detection uses so much CPU
            outWavePlayer.PlaybackStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                wasapiCapture.Dispose();
                outWavePlayer.Dispose();
            });
            if (PlaybackStopped != null)
            {
                outWavePlayer.PlaybackStopped += PlaybackStopped;
            }
            outWavePlayer.Init(sineSignalGenerator.Take(TimeSpan.FromSeconds(seconds)));
            outWavePlayer.Play();
            wasapiCapture.StartRecording();
        }

        public void Stop()
        {
            if (outWavePlayer == null)
            {
                return;
            }
            outWavePlayer.Stop();
        }

        public float GetAverageCaptureVolume()
        {
            int count = captureSamples.Count;
            float sum = 0;
            for (int i = 0; i < count; i++)
            {
                if (captureSamples[i] < 0)
                {
                    sum -= captureSamples[i];
                } else
                {
                    sum += captureSamples[i];
                }
            }
            return sum / count;
        }
    }
}
