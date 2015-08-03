namespace Chimer.Audio
{
    using NAudio.CoreAudioApi;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using System;
    using System.Text;

    class AudioEngine : IDisposable
    {
        private readonly IWavePlayer wavePlayer;
        private readonly IWaveIn waveIn;
        private readonly MixingSampleProvider mixer;
        private readonly BufferedWaveProvider passThruBufferedProvider;

        public AudioEngine(int sampleRate = 44100, string inputDeviceId = null, string outputDeviceId = null)
        {
            LogAvailableDevices();
            var outputDevice = getDeviceForId(outputDeviceId);
            var inputDevice = getDeviceForId(inputDeviceId);
            Logger.Log("Using " + (outputDeviceId == null ? "default" : "configured") + 
                " audio device " + outputDevice.ID + " (" + outputDevice.FriendlyName + ") for output.");
            Logger.Log("Using " + (inputDeviceId == null ? "default" : "configured") +
                " audio device " + inputDevice.ID + " (" + inputDevice.FriendlyName + ") for input.");

            wavePlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 500);
            int channels = outputDevice.AudioClient.MixFormat.Channels;
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
            mixer.ReadFully = true;
            wavePlayer.Init(mixer);

            waveIn = new WasapiCapture(inputDevice, useEventSync: true);
            waveIn.DataAvailable += onInputDataAvailable;

            passThruBufferedProvider = new BufferedWaveProvider(waveIn.WaveFormat);
            MultiplexingSampleProvider multiplexer = new MultiplexingSampleProvider(
                new ISampleProvider[] { new SampleChannel(passThruBufferedProvider) }, 
                channels);
            for(int i = 0; i < channels; i++)
            {
                multiplexer.ConnectInputToOutput(0, i);
            }
            mixer.AddMixerInput(multiplexer);

            waveIn.StartRecording();
            wavePlayer.Play();
        }

        private void onInputDataAvailable(object sender, WaveInEventArgs e)
        {
            passThruBufferedProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
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
            wavePlayer.Dispose();
            waveIn.Dispose();
        }

        private void LogAvailableDevices()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("\r\n==== AVAILABLE OUTPUT DEVICES ====");
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach(var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                b.AppendLine(device.ID + " : " + device.FriendlyName);
            }
            b.AppendLine("=================================");

            b.AppendLine("\r\n==== AVAILABLE INPUT DEVICES ====");
            foreach (var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
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