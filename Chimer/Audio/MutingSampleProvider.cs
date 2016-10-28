using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimer.Audio
{
    class MutingSampleProvider : ISampleProvider
    {
        private const int VolumeMeterSamplesPerSecond = 3;

        private readonly ISampleProvider source;
        private readonly float volumeThreshold;
        private readonly float volumeMultiplier;
        private readonly TimeSpan unmutePeriod;

        private float maxVolumeMeasurement = 0;
        private int samplesUntilVolumeMeasurement = 0;

        private bool muted = true;
        private int samplesUntilRemute = 0;

        public MutingSampleProvider(ISampleProvider source, float volumeThreshold, float volumeMultiplier, TimeSpan unmutePeriod)
        {
            this.source = source;
            this.volumeThreshold = volumeThreshold;
            this.volumeMultiplier = volumeMultiplier;
            this.unmutePeriod = unmutePeriod;
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return source.WaveFormat;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                if (this.samplesUntilVolumeMeasurement <= 0)
                {
                    if (this.VolumeMeter != null)
                    {
                        this.VolumeMeter(this, this.maxVolumeMeasurement);
                        this.maxVolumeMeasurement = 0;
                    }
                    this.samplesUntilVolumeMeasurement = (source.WaveFormat.SampleRate * source.WaveFormat.Channels) / VolumeMeterSamplesPerSecond;
                }
                this.maxVolumeMeasurement = Math.Max(this.maxVolumeMeasurement, buffer[i]);
                this.samplesUntilVolumeMeasurement--;

                if (buffer[i] > volumeThreshold)
                {
                    this.muted = false;
                    this.samplesUntilRemute = source.WaveFormat.SampleRate * source.WaveFormat.Channels * (int)this.unmutePeriod.TotalSeconds;
                }
                else if (!this.muted)
                {
                    this.samplesUntilRemute--;
                    if (this.samplesUntilRemute <= 0)
                    {
                        this.muted = true;
                    }
                }
                else
                {
                    buffer[i] = 0;
                }

                buffer[i] *= this.volumeMultiplier;
            }

            return samplesRead;
        }

        public event EventHandler<float> VolumeMeter;

        public bool IsMuted
        {
            get { return this.muted; }
        }
    }
}
