using System;
using System.IO;
using Attune.Signals;

namespace Attune
{
    /// <summary>
    /// Samples played as if from the microphone, at their own rate and in real time, looping or
    /// once: the mic file (design, Checks: -attuneMicFile feeds a recording through the real Breath
    /// and Hum pipeline), the check's test sounds, and silence in a self-test, which never opens the
    /// real microphone. The samples stay in memory; nothing is written.
    /// </summary>
    public sealed class SampleFeed : AudioFeed
    {
        private readonly float[] _samples;
        private readonly int _rate;
        private readonly bool _loop;
        private readonly string _label;
        private double _owed; // samples due but not yet read
        private int _at;

        /// <summary>A feed of samples at sampleRate, with the indicator's label.</summary>
        public SampleFeed(float[] samples, int sampleRate, bool loop, string label)
        {
            _samples = samples ?? new float[0];
            _rate = Math.Max(1000, sampleRate);
            _loop = loop;
            _label = label;
        }

        /// <inheritdoc/>
        public override int SampleRate => _rate;

        /// <inheritdoc/>
        public override bool IsLive => false;

        /// <inheritdoc/>
        public override string Label => _label;

        /// <summary>True once a feed that doesn't loop has played every sample.</summary>
        public bool IsFinished => !_loop && _at >= _samples.Length;

        /// <summary>The mic file at path, looping; null with problem saying why if it can't be read.</summary>
        public static SampleFeed FromFile(string path, out string problem)
        {
            problem = null;
            try
            {
                if (!WavReader.TryRead(File.ReadAllBytes(path), out float[] samples, out int rate, out problem))
                {
                    return null;
                }

                return new SampleFeed(samples, rate, true, "Mic file playing");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                              exception is ArgumentException || exception is NotSupportedException)
            {
                problem = exception.Message;
                return null;
            }
        }

        /// <summary>Silence: a self-test's microphone, so the check never opens the real one.</summary>
        public static SampleFeed Silence() => new(new float[4800], 48000, true, "Test silence");

        /// <inheritdoc/>
        public override bool Start()
        {
            _owed = 0;
            _at = 0;
            return true;
        }

        /// <inheritdoc/>
        public override void Stop() => _owed = 0;

        /// <inheritdoc/>
        public override void Advance(float dt) => _owed += Math.Max(0f, dt) * _rate;

        /// <inheritdoc/>
        public override int Read(float[] into)
        {
            int count = (int)Math.Min(_owed, into.Length);
            if (count <= 0 || _samples.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < count; i++)
            {
                if (_at >= _samples.Length)
                {
                    if (!_loop)
                    {
                        into[i] = 0f;
                        continue;
                    }

                    _at = 0;
                }

                into[i] = _samples[_at++];
            }

            _owed -= count;
            return count;
        }
    }
}
