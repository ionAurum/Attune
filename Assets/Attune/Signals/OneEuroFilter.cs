using System;

namespace Attune.Signals
{
    /// <summary>
    /// The One Euro filter (Casiez, Roussel and Vogel, 2012) for one axis: a low-pass filter whose
    /// cutoff rises with speed, so it smooths tremor when the input moves slowly and keeps lag low
    /// when it moves fast. MinCutoff is the smoothing at rest (lower is smoother), Beta how fast the
    /// cutoff rises with speed (higher is less lag).
    /// </summary>
    public sealed class OneEuroFilter
    {
        private bool _started;
        private float _value, _speed;

        /// <summary>A filter with these settings.</summary>
        public OneEuroFilter(float minCutoff = 1f, float beta = 0.1f, float derivativeCutoff = 1f)
        {
            MinCutoff = minCutoff;
            Beta = beta;
            DerivativeCutoff = derivativeCutoff;
        }

        /// <summary>The cutoff at rest, in hertz.</summary>
        public float MinCutoff { get; set; }

        /// <summary>How much the cutoff rises per unit of speed.</summary>
        public float Beta { get; set; }

        /// <summary>The cutoff of the speed's own smoothing, in hertz.</summary>
        public float DerivativeCutoff { get; set; }

        /// <summary>The last filtered value.</summary>
        public float Value => _value;

        /// <summary>Filters a new sample taken dt seconds after the last; the first sample passes through.</summary>
        public float Filter(float sample, float dt)
        {
            if (!_started)
            {
                _started = true;
                _value = sample;
                _speed = 0f;
                return _value;
            }

            if (dt <= 0f)
            {
                return _value;
            }

            float rawSpeed = (sample - _value) / dt;
            _speed += Alpha(DerivativeCutoff, dt) * (rawSpeed - _speed);
            float cutoff = MinCutoff + Beta * Math.Abs(_speed);
            _value += Alpha(cutoff, dt) * (sample - _value);
            return _value;
        }

        /// <summary>Forgets the history: the next sample passes through.</summary>
        public void Reset() => _started = false;

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * (float)Math.PI * Math.Max(1e-4f, cutoff));
            return 1f / (1f + tau / dt);
        }
    }
}
