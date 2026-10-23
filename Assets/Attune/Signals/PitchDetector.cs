using System;

namespace Attune.Signals
{
    /// <summary>
    /// The pitch of a hum (design, Sources: Hum is the microphone's pitch), by YIN (de Cheveigné and
    /// Kawahara, 2002): the lag at which the sound best matches itself is its period. It looks at the
    /// latest SamplesNeeded samples, and finds pitches from MinHz to MaxHz. A stretch with no clear
    /// period (breath, noise, silence) has no pitch. Semitones turns a pitch into the Hum source's
    /// units, so a step up the scale is the same size anywhere in a voice's range.
    /// </summary>
    public sealed class PitchDetector
    {
        private readonly float[] _difference;

        /// <summary>A detector for sound at sampleRate, finding pitches from minHz to maxHz.</summary>
        public PitchDetector(int sampleRate, float minHz = 70f, float maxHz = 1000f, float threshold = 0.15f)
        {
            SampleRate = Math.Max(1000, sampleRate);
            MinHz = Math.Max(20f, minHz);
            MaxHz = Math.Max(MinHz * 2f, maxHz);
            Threshold = threshold;
            MaxLag = (int)Math.Ceiling(SampleRate / MinHz);
            MinLag = Math.Max(2, (int)Math.Floor(SampleRate / MaxHz));
            Window = Math.Max(MaxLag, (int)Math.Ceiling(SampleRate * 0.02f));
            _difference = new float[MaxLag + 2];
        }

        /// <summary>The sound's samples a second.</summary>
        public int SampleRate { get; }

        /// <summary>The lowest pitch it finds, in hertz.</summary>
        public float MinHz { get; }

        /// <summary>The highest pitch it finds, in hertz.</summary>
        public float MaxHz { get; }

        /// <summary>How unlike itself the sound may be at its period and still have a pitch, 0 to 1: lower is stricter.</summary>
        public float Threshold { get; set; }

        /// <summary>The longest period it looks for, in samples.</summary>
        public int MaxLag { get; }

        /// <summary>The shortest period it looks for, in samples.</summary>
        public int MinLag { get; }

        /// <summary>How many samples each comparison covers.</summary>
        public int Window { get; }

        /// <summary>How many of the latest samples it needs.</summary>
        public int SamplesNeeded => Window + MaxLag + 1;

        /// <summary>
        /// The pitch of the latest SamplesNeeded of count samples of buffer from start, in hertz, or 0
        /// if it has none (or there are too few samples). clarity is how clearly periodic it is, 0 to 1.
        /// </summary>
        public float Detect(float[] buffer, int start, int count, out float clarity)
        {
            clarity = 0f;
            if (buffer == null || count < SamplesNeeded || start < 0 || start + count > buffer.Length)
            {
                return 0f;
            }

            int from = start + count - SamplesNeeded;

            // The difference at each lag, then normalised by its running mean (YIN steps 2 and 3).
            double running = 0;
            _difference[0] = 1f;
            for (int lag = 1; lag <= MaxLag + 1; lag++)
            {
                double sum = 0;
                for (int j = 0; j < Window; j++)
                {
                    double delta = buffer[from + j] - buffer[from + j + lag];
                    sum += delta * delta;
                }

                running += sum;
                _difference[lag] = running <= 1e-12 ? 1f : (float)(sum * lag / running);
            }

            // The first dip under the threshold, followed down to its bottom (step 4); else no pitch.
            int best = -1;
            float lowest = 1f;
            for (int lag = MinLag; lag <= MaxLag; lag++)
            {
                lowest = Math.Min(lowest, _difference[lag]);
                if (_difference[lag] < Threshold)
                {
                    while (lag + 1 <= MaxLag && _difference[lag + 1] < _difference[lag])
                    {
                        lag++;
                    }

                    best = lag;
                    break;
                }
            }

            if (best < 0)
            {
                clarity = Math.Max(0f, 1f - lowest);
                return 0f;
            }

            clarity = Math.Max(0f, 1f - _difference[best]);

            // Between samples, by a parabola through the bottom and its neighbours (step 5).
            float period = best;
            if (best > 1 && best < MaxLag + 1)
            {
                float a = _difference[best - 1], b = _difference[best], c = _difference[best + 1];
                float bend = a - 2f * b + c;
                if (bend > 1e-9f)
                {
                    period = best + 0.5f * (a - c) / bend;
                }
            }

            return period <= 0f ? 0f : SampleRate / period;
        }

        /// <summary>A pitch in semitones above 55 Hz (the A two octaves below middle A); 0 Hz and below give 0.</summary>
        public static float Semitones(float hz) => hz <= 0f ? 0f : 12f * (float)(Math.Log(hz / 55.0) / Math.Log(2.0));

        /// <summary>Semitones above 55 Hz back to hertz.</summary>
        public static float Hertz(float semitones) => 55f * (float)Math.Pow(2.0, semitones / 12.0);
    }
}
