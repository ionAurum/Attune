using System;

namespace Attune.Signals
{
    /// <summary>
    /// Static: how loud a stretch of sound is (design, Sources: Breath is the microphone's loudness),
    /// as its RMS in decibels below full scale (dBFS: 0 is the loudest a sample can be). The stretch's
    /// own mean is taken out first, so a microphone's DC offset doesn't read as sound. Decibels make a
    /// breath's range even: calibration's Rest is the room, and Reach the comfortable breath.
    /// </summary>
    public static class LoudnessDetector
    {
        /// <summary>The quietest it reports: digital silence, or no sound at all.</summary>
        public const float Silence = -100f;

        /// <summary>The loudness of count samples of buffer from start, in dBFS; Silence for none.</summary>
        public static float Decibels(float[] buffer, int start, int count)
        {
            if (buffer == null || count <= 0 || start < 0 || start + count > buffer.Length)
            {
                return Silence;
            }

            double mean = 0;
            for (int i = start; i < start + count; i++)
            {
                mean += buffer[i];
            }

            mean /= count;
            double sum = 0;
            for (int i = start; i < start + count; i++)
            {
                double centred = buffer[i] - mean;
                sum += centred * centred;
            }

            return FromRms((float)Math.Sqrt(sum / count));
        }

        /// <summary>An RMS amplitude in dBFS, no quieter than Silence.</summary>
        public static float FromRms(float rms) => rms <= 1e-5f ? Silence : Math.Max(Silence, 20f * (float)Math.Log10(rms));

        /// <summary>A dBFS value back to an RMS amplitude.</summary>
        public static float ToRms(float decibels) => (float)Math.Pow(10.0, decibels / 20.0);

        /// <summary>How many decibels louder a sound times as loud in amplitude is: 3 → about 9.5 dB.</summary>
        public static float Times(float factor) => factor <= 0f ? 0f : 20f * (float)Math.Log10(factor);
    }
}
