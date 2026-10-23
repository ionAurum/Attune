using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>
    /// M5's demos played as rules (design, Demos 4 and 5; ruling A33): the marks drifting past the
    /// flyer, which reaches one only at its height and meets a missed one again, and the flyer's
    /// height, which rises quickly and falls gently for the kite.
    /// </summary>
    public class DriftTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void AMarkIsReachedOnlyAtItsHeightAndComesRoundAgain()
        {
            var course = new DriftCourse(new[] { 0.3f, 0.7f, 0.5f }, 0.6f, 0.25f) { Speed = 0.2f, Reach = 0.05f };
            Assert.That(course.Length, Is.EqualTo(1.8f).Within(1e-5f));
            Assert.That(course.Next, Is.EqualTo(0), "the first mark comes first");

            int reached = Fly(course, 0.3f, course.Ahead(0) / course.Speed + 0.1f);
            Assert.That(reached, Is.EqualTo(0));
            Assert.That(course.IsReached(0), Is.True);
            Assert.That(course.Next, Is.EqualTo(1));

            // The next mark passes at the wrong height: passed by, not reached.
            Fly(course, 0.3f, course.Ahead(1) / course.Speed + 0.1f);
            Assert.That(course.IsReached(1), Is.False, "0.3 is too far from 0.7");
            Assert.That(course.Next, Is.EqualTo(2));
            Fly(course, 0.5f, course.Ahead(2) / course.Speed + 0.1f);
            Assert.That(course.Next, Is.EqualTo(1), "the missed one is all that is left");

            Fly(course, 0.72f, course.Ahead(1) / course.Speed + 0.1f);
            Assert.That(course.IsReached(1), Is.True, "it came round again, and 0.72 is near enough");
            Assert.That(course.AllReached, Is.True);
            Assert.That(course.Next, Is.EqualTo(-1));

            course.Reset();
            Assert.That(course.Reached, Is.EqualTo(0));
            Assert.That(course.AllReached, Is.False);
        }

        [Test]
        public void AMarkJustPassedIsLeftOfTheFlyerNotRoundAtTheFarSide()
        {
            var course = new DriftCourse(new[] { 0.5f, 0.5f, 0.5f, 0.5f }, 0.5f, 0.25f) { Speed = 0.1f };
            Assert.That(course.ScreenX(0), Is.EqualTo(0.25f + 0.45f).Within(1e-5f), "the first starts most of a spacing away, on the screen");
            Fly(course, 0f, course.Ahead(0) / course.Speed + 1f);
            Assert.That(course.ScreenX(0), Is.EqualTo(0.15f).Within(0.01f), "a tenth of a width past the flyer");
            Assert.That(course.IsReached(0), Is.False, "the ground is nowhere near 0.5");
        }

        [Test]
        public void TheMarksAreNeverOnTheScreenTwice()
        {
            var course = new DriftCourse(new[] { 0.5f, 0.5f }, 0.3f, 0.25f);
            Assert.That(course.Length, Is.GreaterThanOrEqualTo(1.5f));
        }

        [Test]
        public void TheKiteRisesQuicklyAndFallsGently()
        {
            var kite = new FlightHeight(riseSeconds: 0.5f, fallSeconds: 2f);
            Assert.That(kite.Update(1f, 0.25f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(kite.Update(1f, 1f), Is.EqualTo(1f), "it stops at the level");
            Assert.That(kite.Update(0f, 0.5f), Is.EqualTo(0.75f).Within(1e-5f), "it falls a quarter of the sky in half a second");
            Assert.That(kite.Update(0.7f, 1f), Is.EqualTo(0.7f), "and stops at the level on the way down");
            kite.Update(0.4f, 0f);
            Assert.That(kite.Height, Is.EqualTo(0.7f), "no time, no change");
        }

        [Test]
        public void AScriptedLevelCurveTouchesEveryRibbon()
        {
            // The Kite Breath's Check (design, Demos, 4), as rules: aim for each ribbon as the last one passes.
            var course = new DriftCourse(new[] { 0.35f, 0.6f, 0.45f, 0.8f, 0.55f, 0.25f }, 0.45f, 0.25f) { Speed = 0.07f, Reach = 0.09f };
            var kite = new FlightHeight(0.6f, 3f);
            float seconds = 0f;
            while (!course.AllReached && seconds < 2f * course.Length / course.Speed)
            {
                int next = course.Next;
                kite.Update(next >= 0 ? course.Height(next) : 0f, Frame);
                course.Advance(Frame, kite.Height);
                seconds += Frame;
            }

            Assert.That(course.AllReached, Is.True, $"{course.Reached} of {course.Count} in {seconds:0} s");
            Assert.That(seconds, Is.LessThan(course.Length / course.Speed), "within one pass of the loop");
        }

        // Flies at height for seconds; returns the last mark reached, or -1.
        private static int Fly(DriftCourse course, float height, float seconds)
        {
            int reached = -1;
            for (float t = 0f; t < seconds; t += Frame)
            {
                int mark = course.Advance(Frame, height);
                if (mark >= 0)
                {
                    reached = mark;
                }
            }

            return reached;
        }
    }
}
