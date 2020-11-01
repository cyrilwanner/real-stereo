using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealStereo
{
    class TestTone
    {
        private SignalGenerator sineSignalGenerator;
        private MMDevice outputAudioDevice, inputAudioDevice;
        private IWavePlayer outWavePlayer;
        private WasapiCapture wasapiCapture;
        private List<float> captureSamples = new List<float>();

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
            wasapiCapture.RecordingStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                wasapiCapture.Dispose();
            });
            wasapiCapture.DataAvailable += new EventHandler<WaveInEventArgs>(delegate (object o, WaveInEventArgs e)
            {
                int increaseBy = (wasapiCapture.WaveFormat.BitsPerSample / 8) * wasapiCapture.WaveFormat.Channels;
                for (int i = 0; i < e.BytesRecorded; i += increaseBy)
                {
                    float scaledSample = 0;
                    switch(wasapiCapture.WaveFormat.BitsPerSample)
                    {
                        case 32:
                            uint intSample = (uint)((e.Buffer[i + 3] << 24) | (e.Buffer[i+2] << 16) | (e.Buffer[i+1] << 8) | e.Buffer[i]);
                            scaledSample = intSample / 4294967295f; /// 2147483648f;
                            break;
                        case 24:
                            uint int24Sample = (uint)((e.Buffer[i + 2] << 16) | (e.Buffer[i + 1] << 8) | e.Buffer[i]);
                            scaledSample = int24Sample / 16777216f; /// 8388608f;
                            break;
                        case 16:
                            ushort shortSample = (ushort)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                            scaledSample = shortSample / 65535f;
                            break;
                        case 8:
                            byte byteSample = (byte)e.Buffer[i];
                            scaledSample = byteSample / 255f;
                            break;
                    }
                    captureSamples.Add(scaledSample);
                }
            });

            outWavePlayer = new WasapiOut(outputAudioDevice, AudioClientShareMode.Shared, false, 250); // High latency (big buffer) of 250, since our camera detection uses so much CPU
            outWavePlayer.PlaybackStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                wasapiCapture.StopRecording();
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
            int countZeros = 0;
            float sum = 0;
            for (int i = 0; i < count; i++)
            {
                if (captureSamples[i] == 0)
                {
                    countZeros++;
                }
                sum += captureSamples[i];
            }
            return sum / (count-countZeros);
        }
    }
}
