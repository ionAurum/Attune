using System;

namespace Attune
{
    /// <summary>
    /// The check's test sound, made as it plays in place of the microphone (MicrophoneInput.Use):
    /// a hum-like tone with two overtones, white noise like a breath or a room, or silence, changed
    /// at any moment. The check needs no microphone (design, Checks), yet runs the real Breath and
    /// Hum pipeline on this.
    /// </summary>
    public sealed class TestToneFeed : AudioFeed
    {
        private const int Rate = 48000;

        private readonly Random _random = new(7);
        private double _owed, _phase;
        private float _hz, _amplitude, _noise;

        /// <inheritdoc/>
        public override int SampleRate => Rate;

        /// <inheritdoc/>
        public override bool IsLive => false;

        /// <inheritdoc/>
        public override string Label => "Test sound playing";

        /// <summary>From now on, a tone of hz at amplitude, with noise of its own amplitude on top.</summary>
        public void Tone(float hz, float amplitude, float noise = 0f)
        {
            _hz = hz;
            _amplitude = amplitude;
            _noise = noise;
        }

        /// <summary>From now on, white noise of amplitude alone.</summary>
        public void Noise(float amplitude) => Tone(0f, 0f, amplitude);

        /// <summary>From now on, silence.</summary>
        public void Silence() => Tone(0f, 0f, 0f);

        /// <inheritdoc/>
        public override bool Start()
        {
            _owed = 0;
            return true;
        }

        /// <inheritdoc/>
        public override void Stop() => _owed = 0;

        /// <inheritdoc/>
        public override void Advance(float dt) => _owed += Math.Max(0f, dt) * Rate;

        /// <inheritdoc/>
        public override int Read(float[] into)
        {
            int count = (int)Math.Min(_owed, into.Length);
            double step = 2 * Math.PI * _hz / Rate;
            for (int i = 0; i < count; i++)
            {
                _phase = (_phase + step) % (2 * Math.PI * 6);
                double tone = _hz > 0f ? _amplitude * (Math.Sin(_phase) + Math.Sin(2 * _phase) / 2 + Math.Sin(3 * _phase) / 3) : 0;
                into[i] = (float)(tone + _noise * (_random.NextDouble() * 2 - 1));
            }

            _owed -= count;
            return count;
        }
    }
}
