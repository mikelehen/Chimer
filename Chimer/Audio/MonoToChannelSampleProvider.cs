namespace Chimer.Audio
{
    using NAudio.Wave;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class MonoToChannelSampleProvider : ISampleProvider
    {
        private ISampleProvider srcProvider;
        private int targetChannel;
        private WaveFormat destFormat;

        public MonoToChannelSampleProvider(ISampleProvider srcProvider, int targetChannel, WaveFormat destFormat)
        {
            this.srcProvider = srcProvider;
            this.targetChannel = targetChannel;
            this.destFormat = destFormat;

            Debug.Assert(srcProvider.WaveFormat.Channels == 1);
        }

        public int Read(float[] dest, int offset, int count)
        {
            int srcSampleCountMax = count / destFormat.Channels;
            float[] src = new float[srcSampleCountMax];
            int srcSampleCount = srcProvider.Read(src, 0, srcSampleCountMax);

            int destSampleCount = srcSampleCount * destFormat.Channels;

            for (int iDest = 0; iDest < destSampleCount; iDest++)
            {
                if (iDest % destFormat.Channels == targetChannel)
                {
                    dest[iDest + offset] = src[iDest / destFormat.Channels];
                }
                else
                {
                    dest[iDest + offset] = 0;
                }
            }

            return destSampleCount;
        }

        public WaveFormat WaveFormat
        {
            get { return this.destFormat; }
        }
    }
}