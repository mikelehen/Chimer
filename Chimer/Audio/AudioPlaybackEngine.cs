namespace Chimer.Audio
{
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using System;

    class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            //outputDevice = new WaveOutEvent();
            //outputDevice = new DirectSoundOut();
            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 500);

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
        }
        
        public void PlaySound(CachedSound sound, int channel)
        {
            AddMixerInput(new CachedSoundSampleProvider(sound), channel);
        }

        private void AddMixerInput(ISampleProvider input, int channel)
        {
            mixer.AddMixerInput(new MonoToChannelSampleProvider(input, channel, mixer.WaveFormat));
        }

        public void Dispose()
        {
            outputDevice.Dispose();
        }
    }
}