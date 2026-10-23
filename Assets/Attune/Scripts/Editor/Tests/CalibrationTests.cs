using System.Collections.Generic;
using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>Calibration, normalising and the dead zone (design, Calibration).</summary>
    public class CalibrationTests
    {
        [Test]
        public void SteerNormalisesEachSideToItsOwnReach()
        {
            // A small tilt one way and a big one the other are both full input.
            AxisCalibration axis = AxisCalibration.Make("Steer", rest: 0.1f, low: 0f, high: 0.6f);
            Assert.That(axis.NormaliseSteer(0.6f, 0f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(axis.NormaliseSteer(0f, 0f), Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(axis.NormaliseSteer(0.35f, 0f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(axis.NormaliseSteer(0.05f, 0f), Is.EqualTo(-0.5f).Within(1e-5f));
            Assert.That(axis.NormaliseSteer(0.1f, 0f), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void NormalisingClampsPastTheReach()
        {
            AxisCalibration axis = AxisCalibration.Make("Steer", 0f, -0.2f, 0.2f);
            Assert.That(axis.NormaliseSteer(5f, 0f), Is.EqualTo(1f));
            Assert.That(axis.NormaliseSteer(-5f, 0f), Is.EqualTo(-1f));
            Assert.That(axis.NormaliseLevel(-5f, 0f), Is.EqualTo(0f));
            Assert.That(axis.NormaliseLevel(5f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void LevelIsOneSidedFromRest()
        {
            AxisCalibration axis = AxisCalibration.Make("Level", rest: 0.02f, low: 0f, high: 0.42f);
            Assert.That(axis.NormaliseLevel(0.22f, 0f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(axis.NormaliseLevel(0.01f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void ADeadZoneIsZeroInsideAndStretchesTheRest()
        {
            Assert.That(AxisCalibration.ApplyDeadZone(0.05f, 0.1f), Is.EqualTo(0f));
            Assert.That(AxisCalibration.ApplyDeadZone(-0.1f, 0.1f), Is.EqualTo(0f));
            Assert.That(AxisCalibration.ApplyDeadZone(1f, 0.1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(AxisCalibration.ApplyDeadZone(-1f, 0.1f), Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(AxisCalibration.ApplyDeadZone(0.55f, 0.1f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void NoiseAtRestWidensTheDeadZone()
        {
            // Noise of 0.05 on a reach of 0.25 each way is 20%: wider than the profile's 10%.
            AxisCalibration axis = AxisCalibration.Make("Steer", 0f, -0.25f, 0.25f, noise: 0.05f);
            Assert.That(axis.EffectiveDeadZone(0.1f), Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(axis.NormaliseSteer(0.045f, 0.1f), Is.EqualTo(0f));
            Assert.That(axis.EffectiveDeadZone(0.3f), Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void AnAxisWithNoRangeReadsZero()
        {
            AxisCalibration axis = AxisCalibration.Make("Steer", 0.5f, 0.5f, 0.5f);
            Assert.That(axis.NormaliseSteer(0.9f, 0f), Is.EqualTo(0f));
            Assert.That(axis.NormaliseLevel(0.9f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void RestIsTheMeanAndNoiseHowFarItWanders()
        {
            var samples = new List<float>();
            for (int i = 0; i < 100; i++)
            {
                samples.Add(0.3f + (i % 2 == 0 ? 0.01f : -0.01f));
            }

            Assert.That(Calibrate.Rest(samples, out float rest, out float noise), Is.True);
            Assert.That(rest, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(noise, Is.EqualTo(0.01f).Within(1e-4f));
            Assert.That(Calibrate.Rest(new List<float>(), out _, out _), Is.False);
        }

        [Test]
        public void ReachIgnoresASpike()
        {
            // The player leans to about 0.4, and one sample jumps to 2.
            var samples = new List<float>();
            for (int i = 0; i < 50; i++)
            {
                samples.Add(0.4f);
            }

            samples.Add(2f);
            Assert.That(Calibrate.Reach(samples, rest: 0f, noise: 0.01f, side: 1, minSpan: 0.01f, out float reach), Is.True);
            Assert.That(reach, Is.EqualTo(0.4f).Within(1e-4f));
        }

        [Test]
        public void ReachOnTheLowSideIsBelowRest()
        {
            var samples = new List<float> { -0.2f, -0.3f, -0.3f, -0.3f, 0.1f };
            Assert.That(Calibrate.Reach(samples, 0f, 0f, -1, 0.01f, out float reach), Is.True);
            Assert.That(reach, Is.LessThan(-0.2f));
        }

        [Test]
        public void AReachInsideTheNoiseDoesNotCount()
        {
            // Nothing beyond three times the noise: the step saw no movement, and the axis keeps what it had.
            var samples = new List<float> { 0.02f, 0.03f, 0.025f };
            Assert.That(Calibrate.Reach(samples, 0f, noise: 0.02f, side: 1, minSpan: 0.001f, out float reach), Is.False);
            Assert.That(reach, Is.EqualTo(0f));
            Assert.That(Calibrate.Reach(new List<float> { -0.5f }, 0f, 0f, 1, 0.01f, out _), Is.False);
        }

        [Test]
        public void TimingClassifiesPressesByDuration()
        {
            var timing = new TimingValues { HoldSeconds = 0.8f, LongHoldSeconds = 2f };
            Assert.That(timing.Classify(0.1f), Is.EqualTo(PressKind.Tap));
            Assert.That(timing.Classify(0.79f), Is.EqualTo(PressKind.Tap));
            Assert.That(timing.Classify(0.8f), Is.EqualTo(PressKind.Hold));
            Assert.That(timing.Classify(1.99f), Is.EqualTo(PressKind.Hold));
            Assert.That(timing.Classify(2f), Is.EqualTo(PressKind.LongHold));
        }

        [Test]
        public void ClampKeepsTimingSane()
        {
            var timing = new TimingValues { HoldSeconds = 3f, LongHoldSeconds = 1f, PressOnLevel = 0.3f, PressOffLevel = 0.6f, DeadZone = 2f };
            timing.Clamp();
            Assert.That(timing.LongHoldSeconds, Is.GreaterThan(timing.HoldSeconds));
            Assert.That(timing.PressOffLevel, Is.LessThan(timing.PressOnLevel));
            Assert.That(timing.DeadZone, Is.LessThanOrEqualTo(0.9f));
        }

        [Test]
        public void PressTrackerClassifiesOnReleaseAndCounts()
        {
            var timing = new TimingValues { HoldSeconds = 0.8f, LongHoldSeconds = 2f };
            var press = new PressTracker();
            Assert.That(press.Down(1.0), Is.True);
            Assert.That(press.Down(1.1), Is.False, "a held key reported again is the same press");
            Assert.That(press.KindIfReleased(2.0, timing), Is.EqualTo(PressKind.Hold));
            Assert.That(press.HeldSeconds(2.0), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(press.Up(3.5, timing), Is.EqualTo(PressKind.LongHold));
            Assert.That(press.Up(3.6, timing), Is.Null);
            press.Down(4.0);
            Assert.That(press.Up(4.2, timing), Is.EqualTo(PressKind.Tap));
            Assert.That(press.Count, Is.EqualTo(2));
            press.Down(5.0);
            press.Cancel();
            Assert.That(press.IsDown, Is.False);
            Assert.That(press.Count, Is.EqualTo(2), "a cancelled press isn't counted");
        }
    }
}
