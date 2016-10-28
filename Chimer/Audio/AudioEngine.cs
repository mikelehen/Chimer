namespace Chimer.Audio
{
    using NAudio.CoreAudioApi;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using System;
    using System.Diagnostics;
    using System.Text;

    enum PassThroughMode
    {
        Off, Auto, On
    }

    struct InputStatus
    {
        public int Volume;
        public bool Muted;
    }

    class AudioEngine : IDisposable
    {
        private readonly WasapiOut wavePlayer;
        private readonly WasapiCapture waveIn;
        private readonly MixingSampleProvider mixer;
        private readonly BufferedWaveProvider passThruBufferedProvider;
        private readonly MutingSampleProvider mutedInputChannel;
        private readonly int outputLatency;
        private readonly int inputThreshold;

        // Delay to wait after the input threshold was last exceeded before re-muting.
        private const int INPUT_MUTE_DELAY_SECONDS = 5;

        public AudioEngine(int sampleRate = 44100, string inputDeviceId = null, string outputDeviceId = null, int inputLatency = 0, int outputLatency = 0, int inputVolume = 100, int inputThreshold = 5)
        {
            var outputDevice = getDeviceForId(outputDeviceId, DataFlow.Render);
            var inputDevice = getDeviceForId(inputDeviceId, DataFlow.Capture);
            Logger.Log("Using " + (outputDeviceId == null ? "default" : "configured") + 
                " output device " + outputDevice.ID + " (" + outputDevice.FriendlyName + ")");
            Logger.Log("Using " + (inputDeviceId == null ? "default" : "configured") +
                " input device " + inputDevice.ID + " (" + inputDevice.FriendlyName + ")");

            this.outputLatency = outputLatency;
            this.inputThreshold = inputThreshold;

            if (outputLatency == 0)
            {
                wavePlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
            }
            else
            {
                wavePlayer = new WasapiOut(outputDevice, AudioClientShareMode.Exclusive, useEventSync: true, latency: outputLatency);
            }
            int channels = outputDevice.AudioClient.MixFormat.Channels;
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
            mixer.ReadFully = true;
            wavePlayer.Init(mixer);

            waveIn = new WasapiCapture(inputDevice, useEventSync: true, audioBufferMillisecondsLength: inputLatency);
            if (inputLatency == 0)
            {
                waveIn.ShareMode = AudioClientShareMode.Shared;
            }
            else
            {
                waveIn.ShareMode = AudioClientShareMode.Exclusive;
                waveIn.WaveFormat = new WaveFormatExtensible(44100, 16, 2);
            }

            waveIn.DataAvailable += onInputDataAvailable;

            passThruBufferedProvider = new BufferedWaveProvider(waveIn.WaveFormat);
            SampleChannel inputChannel = new SampleChannel(passThruBufferedProvider);

            this.mutedInputChannel = new MutingSampleProvider(inputChannel,
                inputThreshold / 100.0f, inputVolume / 100.0f, TimeSpan.FromSeconds(INPUT_MUTE_DELAY_SECONDS));
            mutedInputChannel.VolumeMeter += (sender, volume) => {
                if (InputStatusChange != null)
                {
                    var inputState = new InputStatus() { Muted = mutedInputChannel.IsMuted, Volume = (int)(100 * volume) };
                    InputStatusChange(this, inputState);
                }
            };

            MultiplexingSampleProvider multiplexer = new MultiplexingSampleProvider(
                new ISampleProvider[] { mutedInputChannel },
                channels);
            for(int i = 0; i < channels; i++)
            {
                multiplexer.ConnectInputToOutput(0, i);
            }
            mixer.AddMixerInput(multiplexer);

            if (inputVolume > 0)
            {
                waveIn.StartRecording();
            }

            wavePlayer.Play();
        }

        public event EventHandler<InputStatus> InputStatusChange;

        public PassThroughMode PassThroughMode
        {
            set
            {
                if (value == PassThroughMode.Off)
                {
                    if (waveIn.CaptureState == CaptureState.Capturing)
                    {
                        waveIn.StopRecording();
                    }
                }
                else
                {
                    if (value == PassThroughMode.Auto)
                    {
                        mutedInputChannel.VolumeThreshold = inputThreshold / 100.0f;
                    }
                    else
                    {
                        Debug.Assert(value == PassThroughMode.On);
                        mutedInputChannel.VolumeThreshold = 0;
                    }

                    if (waveIn.CaptureState == CaptureState.Stopped)
                    {
                        waveIn.StartRecording();
                    }
                }
            }
        }

        private DateTime lastSkipMessage = DateTime.MinValue;
        private void onInputDataAvailable(object sender, WaveInEventArgs e)
        {
            // If we have a specific target latency, try not to overfill the output, which would create extra latency.
            if (outputLatency == 0 || passThruBufferedProvider.BufferedDuration.TotalMilliseconds <= 2 * outputLatency)
            {
                passThruBufferedProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }
            else
            {
                if (DateTime.Now.Subtract(lastSkipMessage).TotalMilliseconds > 500)
                {
                    Logger.Log("Skipping pass-through input samples to let output catch up. If you see this constantly, you may want to increase your outputLatency.");
                    lastSkipMessage = DateTime.Now;
                }
            }
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
            wavePlayer.Stop();
            wavePlayer.Dispose();

            waveIn.StopRecording();
            waveIn.Dispose();
        }

        public static void LogAvailableDevices()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendLine("\r\n======= AVAILABLE OUTPUT DEVICES =======");
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach(var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                b.AppendLine(device.ID + " : " + device.FriendlyName);
            }
            b.AppendLine("========================================");

            b.AppendLine("\r\n======== AVAILABLE INPUT DEVICES ========");
            foreach (var device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                b.AppendLine(device.ID + " : " + device.FriendlyName);
            }
            b.AppendLine("=========================================");

            Logger.Log(b.ToString());
        }

        private MMDevice getDeviceForId(string id, DataFlow dataFlow)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            if (id == null)
            {
                return deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
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