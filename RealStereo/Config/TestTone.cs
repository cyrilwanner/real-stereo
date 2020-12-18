using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace RealStereo.Config
{
    class TestTone
    {
        private SignalGenerator sineSignalGenerator;
        private MMDevice outputAudioDevice;
        private MMDevice inputAudioDevice;
        private IWavePlayer outWavePlayer;
        private WasapiCapture capture;
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

        /// <summary>
        /// Play a test tone for the given amount of time on the selected audio output devie.
        /// </summary>
        /// <param name="seconds">Test tone length in seconds.</param>
        /// <param name="PlaybackStopped">Event handler that will be called once the tone has stopped.</param>
        public void Play(int seconds, EventHandler<StoppedEventArgs> PlaybackStopped = null)
        {
            captureSamples.Clear();
            capture = new WasapiCapture(inputAudioDevice);

            // define a low- & high pass filter to reduce background noise
            BiQuadFilter lowPassFilter = BiQuadFilter.LowPassFilter(capture.WaveFormat.SampleRate, 2100, 1);
            BiQuadFilter highPassFilter = BiQuadFilter.HighPassFilter(capture.WaveFormat.SampleRate, 1900, 1);

            // when data is recorded, apply the filters and store the sample
            capture.DataAvailable += new EventHandler<WaveInEventArgs>(delegate (object o, WaveInEventArgs e)
            {
                for (int i = 0; i < e.BytesRecorded; i += 4)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, i);
                    sample = lowPassFilter.Transform(sample);
                    sample = highPassFilter.Transform(sample);
                    captureSamples.Add(sample);
                }
            });

            // define output with a high latency (big buffer) of 250, since our camera detection uses so much CPU
            outWavePlayer = new WasapiOut(outputAudioDevice, AudioClientShareMode.Shared, false, 250);
            outWavePlayer.PlaybackStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                capture.Dispose();
                outWavePlayer.Dispose();
            });

            if (PlaybackStopped != null)
            {
                outWavePlayer.PlaybackStopped += PlaybackStopped;
            }

            outWavePlayer.Init(sineSignalGenerator.Take(TimeSpan.FromSeconds(seconds)));
            outWavePlayer.Play();
            capture.StartRecording();
        }

        /// <summary>
        /// Stop the test tone before before it is played for the specified amount of time.
        /// </summary>
        public void Stop()
        {
            if (outWavePlayer == null)
            {
                return;
            }

            outWavePlayer.Stop();
        }

        /// <summary>
        /// Calculates the average recorded volume.
        /// </summary>
        /// <returns>Average recorded volume.</returns>
        public float GetAverageCaptureVolume()
        {
            int count = captureSamples.Count;
            float sum = 0;

            for (int i = 0; i < count; i++)
            {
                if (captureSamples[i] < 0)
                {
                    sum -= captureSamples[i];
                }
                else
                {
                    sum += captureSamples[i];
                }
            }

            return sum / count;
        }
    }
}
