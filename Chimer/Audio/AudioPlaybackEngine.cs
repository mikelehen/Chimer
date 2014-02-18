namespace Chimer.Audio
{
    using NAudio.CoreAudioApi;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using System;
    using System.Text;

    class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

        public AudioPlaybackEngine(int sampleRate = 44100, string deviceId = null)
        {
            LogAvailableDevices();
            var device = getDeviceForId(deviceId);
            Logger.Log("Using " + (deviceId == null ? "default" : "configured") + 
                " audio device " + device.ID + " (" + device.FriendlyName + ")");

            outputDevice = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: true, latency: 500);

            int channels = device.AudioClient.MixFormat.Channels;
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
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

        private void LogAvailableDevices()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("\r\n==== AVAILABLE SOUND DEVICES ====");
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach(var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                b.AppendLine(device.ID + " : " + device.FriendlyName);
            }
            b.AppendLine("=================================");
            Logger.Log(b.ToString());
        }

        private MMDevice getDeviceForId(string id)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            if (id == null)
            {
                return deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            else
            {
                try
                {
                    MMDevice device = deviceEnumerator.GetDevice(id);
                    if (device.State != DeviceState.Active) 
                        throw new Exception();
                    return device;
                }
                catch (Exception)
                {
                    throw new Exception("Couldn't find audio device with id '" + id + "'");
                }
            }
        }
    }
}