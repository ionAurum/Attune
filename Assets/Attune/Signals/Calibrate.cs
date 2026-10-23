using System;
using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// Static: turns the samples of a calibration's steps into an axis (design, Calibration). Rest
    /// (keep still for 2 seconds) gives the rest position and the noise; Reach (go as far as is
    /// comfortable for 3 seconds, each way that applies) gives each end. A step that saw nothing
    /// usable says so, and the axis keeps what it had.
    /// </summary>
    public static class Calibrate
    {
        /// <summary>Of the samples on one side of rest, the share beyond which a reach is taken: a spike past it doesn't count.</summary>
        public const float ReachPercentile = 0.9f;

        /// <summary>A reach must be this many times the noise from rest to count.</summary>
        public const float NoiseMultiple = 3f;

        /// <summary>The rest position (the mean) and the noise (the 95th percentile of the distance from it); false with no samples.</summary>
        public static bool Rest(IReadOnlyList<float> samples, out float rest, out float noise)
        {
            rest = 0f;
            noise = 0f;
            if (samples == null || samples.Count == 0)
            {
                return false;
            }

            double sum = 0;
            foreach (float sample in samples)
            {
                sum += sample;
            }

            rest = (float)(sum / samples.Count);
            var distances = new List<float>(samples.Count);
            foreach (float sample in samples)
            {
                distances.Add(Math.Abs(sample - rest));
            }

            noise = Percentile(distances, 0.95f);
            return true;
        }

        /// <summary>
        /// The comfortable reach on one side of rest (side +1 above, −1 below): the 90th percentile of
        /// how far the samples on that side went. False if none went past the noise, minSpan
        /// included, so a step the player skipped or couldn't do leaves the axis as it was.
        /// </summary>
        public static bool Reach(IReadOnlyList<float> samples, float rest, float noise, int side, float minSpan, out float reach)
        {
            reach = rest;
            if (samples == null || samples.Count == 0 || side == 0)
            {
                return false;
            }

            var distances = new List<float>();
            foreach (float sample in samples)
            {
                float distance = (sample - rest) * Math.Sign(side);
                if (distance > 0f)
                {
                    distances.Add(distance);
                }
            }

            if (distances.Count == 0)
            {
                return false;
            }

            float far = Percentile(distances, ReachPercentile);
            if (far < Math.Max(minSpan, noise * NoiseMultiple))
            {
                return false;
            }

            reach = rest + far * Math.Sign(side);
            return true;
        }

        /// <summary>
        /// The comfortable range of an axis measured as two held values (the Hum source: "the lowest
        /// and highest comfortable hum"): the median of the samples while the player held the low
        /// end, and of those while they held the high end. False if either step heard fewer than
        /// minSamples, or the two are less than minSpan apart, so the axis keeps what it had.
        /// </summary>
        public static bool Range(IReadOnlyList<float> lowSamples, IReadOnlyList<float> highSamples, int minSamples, float minSpan,
            out float low, out float high)
        {
            low = high = 0f;
            if (lowSamples == null || highSamples == null || lowSamples.Count < Math.Max(1, minSamples) ||
                highSamples.Count < Math.Max(1, minSamples))
            {
                return false;
            }

            low = Percentile(new List<float>(lowSamples), 0.5f);
            high = Percentile(new List<float>(highSamples), 0.5f);
            return high - low >= minSpan;
        }

        /// <summary>The value below which share (0 to 1) of the values lie, interpolated; 0 for none.</summary>
        public static float Percentile(List<float> values, float share)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            values.Sort();
            float position = Math.Min(1f, Math.Max(0f, share)) * (values.Count - 1);
            int below = (int)Math.Floor(position);
            int above = Math.Min(values.Count - 1, below + 1);
            float t = position - below;
            return values[below] + (values[above] - values[below]) * t;
        }
    }
}
