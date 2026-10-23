using System;
using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>The One Euro filter (design, Calibration, Smooth).</summary>
    public class FilterTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void TheFirstSamplePassesThrough()
        {
            var filter = new OneEuroFilter();
            Assert.That(filter.Filter(0.7f, Dt), Is.EqualTo(0.7f));
        }

        [Test]
        public void ItSmoothsTremorAtRest()
        {
            // A still hand with a 6 Hz tremor of ±0.05: the filtered value wanders far less.
            var filter = new OneEuroFilter(minCutoff: 1f, beta: 0.05f);
            float worst = 0f;
            for (int i = 0; i < 600; i++)
            {
                float tremor = 0.05f * (float)Math.Sin(2 * Math.PI * 6 * i * Dt);
                float value = filter.Filter(0.5f + tremor, Dt);
                if (i > 120)
                {
                    worst = Math.Max(worst, Math.Abs(value - 0.5f));
                }
            }

            Assert.That(worst, Is.LessThan(0.02f));
        }

        [Test]
        public void ItKeepsLagLowWhenMovingFast()
        {
            // A fast sweep: with beta the filter keeps up far better than with none.
            float Lag(float beta)
            {
                var filter = new OneEuroFilter(1f, beta);
                float value = 0f;
                for (int i = 0; i < 30; i++)
                {
                    value = filter.Filter(i * 0.05f, Dt);
                }

                return 29 * 0.05f - value;
            }

            Assert.That(Lag(2f), Is.LessThan(Lag(0f) * 0.5f));
        }

        [Test]
        public void ItSettlesOnAStep()
        {
            var filter = new OneEuroFilter(1f, 0.1f);
            filter.Filter(0f, Dt);
            float value = 0f;
            for (int i = 0; i < 300; i++)
            {
                value = filter.Filter(1f, Dt);
            }

            Assert.That(value, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void ResetForgetsTheHistory()
        {
            var filter = new OneEuroFilter();
            filter.Filter(0f, Dt);
            filter.Filter(0f, Dt);
            filter.Reset();
            Assert.That(filter.Filter(0.9f, Dt), Is.EqualTo(0.9f));
        }

        [Test]
        public void ATimeStepOfZeroChangesNothing()
        {
            var filter = new OneEuroFilter();
            filter.Filter(0.2f, Dt);
            Assert.That(filter.Filter(0.9f, 0f), Is.EqualTo(0.2f));
        }
    }
}
