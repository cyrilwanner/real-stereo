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
        private MMDevice outputAudioDevice;
        private IWavePlayer outWavePlayer;

        public TestTone(MMDevice outputAudioDevice)
        {
            sineSignalGenerator = new SignalGenerator()
            {
                Gain = 1,
                Frequency = 2000,
                Type = SignalGeneratorType.Sin
            };
            this.outputAudioDevice = outputAudioDevice;
        }

        public void Play(int seconds, EventHandler<StoppedEventArgs> PlaybackStopped = null)
        {
            if (outputAudioDevice == null || sineSignalGenerator == null)
            {
                return;
            }

            outWavePlayer = new WasapiOut(outputAudioDevice, AudioClientShareMode.Shared, false, 250); // High latency (big buffer) of 250, since our camera detection uses so much CPU
            outWavePlayer.PlaybackStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                outWavePlayer.Dispose();
            });
            if (PlaybackStopped != null)
            {
                outWavePlayer.PlaybackStopped += PlaybackStopped;
            }
            outWavePlayer.Init(sineSignalGenerator.Take(TimeSpan.FromSeconds(seconds)));
            outWavePlayer.Play();
        }

        public void Stop()
        {
            if (outWavePlayer == null)
            {
                return;
            }
            outWavePlayer.Stop();
        }
    }
}
