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
        public string Warning { get; private set; }
        private string file;

        public CachedSound(string audioFileName)
        {
            file = audioFileName;
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

        // I wrote this and and then noticed WaveFormatConversionStream exists, so I tried using it.
        // But it didn't work for some files (e.g. an 8khz wave), so I am sticking with my code for now.
        private float[] convertToMono44100(List<float> src, WaveFormat srcFormat)
        {
            int srcSampleCount = src.Count / srcFormat.Channels;
            float destSamplesPerSourceSample = (float)44100 / srcFormat.SampleRate;
            
            // Kinda' hacky, but when the sample rates don't mesh nicely, the result isn't always good.
            // So we mark it and warn the user.
            if (destSamplesPerSourceSample != Math.Floor(destSamplesPerSourceSample))
            {
                Warning = "WARNING: All sounds must have a sample rate of 44100Hz.  " + file + " was " + srcFormat.SampleRate + "Hz instead.  It was automatically converted to 44100, but some distortion may result.  Consider using Audacity (or similar) to convert the file to 44100Hz.";
            }

            int destSampleCount = (int)Math.Floor(srcSampleCount * destSamplesPerSourceSample);
            float[] dest = new float[destSampleCount];

            for (int iDest = 0; iDest < destSampleCount; iDest++)
            {
                // We just interpolate between samples even though I'm sure there's a better DSP way.
                float srcSampleNum = iDest / destSamplesPerSourceSample;
                int srcSampleWholeNumber = (int)Math.Truncate(srcSampleNum);
                float srcSampleFraction = srcSampleNum - srcSampleWholeNumber;
                
                int iSrc = srcSampleWholeNumber * srcFormat.Channels;
                int iSrcNext = (srcSampleWholeNumber + 1) * srcFormat.Channels;
                
                float srcValue = src[iSrc];
                float srcValueNext = iSrcNext < src.Count ? src[iSrcNext] : srcValue;

                dest[iDest] = (srcValue * (1 - srcSampleFraction)) + (srcValueNext * srcSampleFraction);
            }

            return dest;
        }
    }
}