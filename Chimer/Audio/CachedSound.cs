namespace Chimer.Audio
{
    using NAudio.Wave;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public CachedSound(string audioFileName)
        {
            WaveFormat = new WaveFormat(44100, 1);
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                var srcFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = convertToMono44100(wholeFile, srcFormat);
            }
        }

        private float[] convertToMono44100(List<float> src, WaveFormat srcFormat)
        {
            if (srcFormat.SampleRate > 44100 || (44100 % srcFormat.SampleRate != 0))
            {
                throw new Exception("Wave files must be 44100 bits per second or a divisor of 44100.");
            }

            int divisor = 44100 / srcFormat.SampleRate;
            int srcSampleCount = src.Count / srcFormat.Channels;
            float[] dest = new float[(srcSampleCount - 1) * divisor]; // subtract the last sample, since it doesn't have anything to interpolate to.

            for (int iDest = 0; iDest < dest.Length; iDest++)
            {
                // We just interpolate between samples even though I'm sure there's a better DSP way.
                int srcSampleNum = iDest / divisor;
                int iSrc = srcSampleNum * srcFormat.Channels;
                int iSrcNext = (srcSampleNum + 1) * srcFormat.Channels;
                float value = src[iSrc];
                float valueNext = src[iSrcNext];
                float amountOfNextValue = (iDest % divisor) / divisor;

                dest[iDest] = (value * (1 - amountOfNextValue)) + (valueNext * amountOfNextValue);
            }

            return dest;
        }
    }
}