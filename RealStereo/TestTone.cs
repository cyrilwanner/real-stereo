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
        public static void play(MMDevice audioDevice, int seconds, EventHandler<StoppedEventArgs> PlaybackStopped = null)
        {
            ISampleProvider sine = new SignalGenerator()
            {
                Gain = 0.1,
                Frequency = 2000,
                Type = SignalGeneratorType.Sin
            }.Take(TimeSpan.FromSeconds(seconds));
            IWavePlayer waveOut = new WasapiOut(audioDevice, AudioClientShareMode.Shared, false, 50);
            waveOut.Init(sine);
            waveOut.Play();
            waveOut.PlaybackStopped += new EventHandler<StoppedEventArgs>(delegate (object o, StoppedEventArgs e)
            {
                waveOut.Dispose();
            });
            if (PlaybackStopped != null)
            {
                waveOut.PlaybackStopped += PlaybackStopped;
            }
        }
    }
}
