using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>The dwell timer and its rest zone, the scanner's timing, and scanning the gestures (design, Adapters, Scanning).</summary>
    public class SelectionTests
    {
        [Test]
        public void ADwellSelectsOnceAfterItsTime()
        {
            var dwell = new DwellTimer(1f);
            int selected = DwellTimer.None;
            int times = 0;
            for (int i = 0; i < 200; i++)
            {
                int now = dwell.Update(7, 0.01f);
                if (now != DwellTimer.None)
                {
                    selected = now;
                    times++;
                }
            }

            Assert.That(selected, Is.EqualTo(7));
            Assert.That(times, Is.EqualTo(1), "resting on after a select doesn't select again");
        }

        [Test]
        public void MovingAwayCancelsADwell()
        {
            var dwell = new DwellTimer(1f);
            dwell.Update(3, 0f);
            dwell.Update(3, 0.9f);
            Assert.That(dwell.Progress, Is.EqualTo(0.9f).Within(1e-5f));
            dwell.Update(4, 0.05f);
            Assert.That(dwell.Progress, Is.EqualTo(0f));
            Assert.That(dwell.Update(4, 0.5f), Is.EqualTo(DwellTimer.None));
            Assert.That(dwell.Update(3, 0.6f), Is.EqualTo(DwellTimer.None), "coming back starts again");
        }

        [Test]
        public void LeavingAndComingBackSelectsAgain()
        {
            var dwell = new DwellTimer(0.5f);
            dwell.Update(1, 0f);
            Assert.That(dwell.Update(1, 0.6f), Is.EqualTo(1));
            dwell.Update(DwellTimer.None, 0.1f);
            dwell.Update(1, 0f);
            Assert.That(dwell.Update(1, 0.6f), Is.EqualTo(1));
        }

        [Test]
        public void ATargetThePointArrivesOnMustBeLeftFirst()
        {
            var dwell = new DwellTimer(0.5f);
            dwell.Settle(4);
            Assert.That(dwell.Update(4, 2f), Is.EqualTo(DwellTimer.None), "resting where it arrived never selects");
            Assert.That(dwell.Progress, Is.EqualTo(0f));
            dwell.Update(5, 0f);
            Assert.That(dwell.Update(5, 0.6f), Is.EqualTo(5), "another target selects as usual");
            dwell.Settle(DwellTimer.None);
            dwell.Update(4, 0f);
            Assert.That(dwell.Update(4, 0.6f), Is.EqualTo(4), "settling on nothing holds nothing back");
        }

        [Test]
        public void TheRestZoneNeverSelects()
        {
            // A target reaching into the border: resting on its part in the rest zone places nothing.
            var field = new DwellField { RestBorder = 0.1f };
            field.Add(5, 0f, 0f, 0.5f, 0.5f);
            var dwell = new DwellTimer(0.5f);
            int selected = DwellTimer.None;
            for (int i = 0; i < 100; i++)
            {
                int now = dwell.Update(field.TargetAt(0.05f, 0.3f), 0.05f);
                if (now != DwellTimer.None)
                {
                    selected = now;
                }
            }

            Assert.That(selected, Is.EqualTo(DwellTimer.None));
            Assert.That(field.TargetAt(0.3f, 0.3f), Is.EqualTo(5));
            Assert.That(field.TargetAt(0.7f, 0.7f), Is.EqualTo(DwellTimer.None), "over nothing");
            Assert.That(field.TargetAt(-0.2f, 0.5f), Is.EqualTo(DwellTimer.None), "off the screen");
        }

        [Test]
        public void ALaterTargetIsOnTop()
        {
            var field = new DwellField();
            field.Add(1, 0f, 0f, 1f, 1f);
            field.Add(2, 0.4f, 0.4f, 0.2f, 0.2f);
            Assert.That(field.TargetAt(0.5f, 0.5f), Is.EqualTo(2));
            Assert.That(field.TargetAt(0.1f, 0.1f), Is.EqualTo(1));
        }

        [Test]
        public void TheScannerStepsOnItsTimer()
        {
            var scanner = new Scanner(stepSeconds: 1f, rounds: 0);
            scanner.Reset(3);
            Assert.That(scanner.Index, Is.EqualTo(0));
            Assert.That(scanner.Advance(0.99f), Is.False);
            Assert.That(scanner.Advance(0.02f), Is.True);
            Assert.That(scanner.Index, Is.EqualTo(1));
            scanner.Advance(1f);
            Assert.That(scanner.Index, Is.EqualTo(2));
            scanner.Advance(1f);
            Assert.That(scanner.Index, Is.EqualTo(0), "it wraps round");
            scanner.Advance(2.5f);
            Assert.That(scanner.Index, Is.EqualTo(2), "a long frame takes every step in it");
        }

        [Test]
        public void APressPicksTheHighlightedOption()
        {
            var scanner = new Scanner(1f, 0, fromTop: true);
            scanner.Reset(4);
            scanner.Advance(2.1f);
            Assert.That(scanner.Press(), Is.EqualTo(2));
            Assert.That(scanner.Index, Is.EqualTo(0), "from the top again");

            scanner.FromTop = false;
            scanner.Advance(1.1f);
            Assert.That(scanner.Press(), Is.EqualTo(1));
            Assert.That(scanner.Index, Is.EqualTo(1), "carrying on from the pick");
        }

        [Test]
        public void TheScanPausesAfterItsRoundsAndAPressWakesIt()
        {
            var scanner = new Scanner(1f, rounds: 2, fromTop: true);
            scanner.Reset(3);
            scanner.Advance(5.5f); // six highlights: two rounds
            Assert.That(scanner.IsPaused, Is.False);
            scanner.Advance(0.6f);
            Assert.That(scanner.IsPaused, Is.True);
            Assert.That(scanner.Advance(10f), Is.False, "paused, it waits");
            Assert.That(scanner.Press(), Is.EqualTo(-1), "the press wakes it and picks nothing");
            Assert.That(scanner.IsPaused, Is.False);
            Assert.That(scanner.Index, Is.EqualTo(0));
        }

        [Test]
        public void AScanWithNoOptionsDoesNothing()
        {
            var scanner = new Scanner(1f);
            scanner.Reset(0);
            Assert.That(scanner.Index, Is.EqualTo(-1));
            Assert.That(scanner.Advance(5f), Is.False);
            Assert.That(scanner.Press(), Is.EqualTo(-1));
        }

        [Test]
        public void ResetCanStartPartWay()
        {
            var scanner = new Scanner(1f);
            scanner.Reset(5, 3);
            Assert.That(scanner.Index, Is.EqualTo(3));
            scanner.Reset(5, 9);
            Assert.That(scanner.Index, Is.EqualTo(0));
        }

        [Test]
        public void ScanningTheGesturesSendsTheHighlightedOne()
        {
            var scan = new GestureScan(KeyboardZones.Quadrants, new Scanner(1f, 0, true));
            Assert.That(scan.Highlighted, Is.EqualTo(KeyboardZones.TopLeft));
            scan.Advance(1.05f);
            Assert.That(scan.Highlighted, Is.EqualTo(KeyboardZones.TopRight));
            Assert.That(scan.Tap(), Is.EqualTo(KeyboardZones.TopRight));
            scan.Advance(3.05f);
            Assert.That(scan.Tap(), Is.EqualTo(KeyboardZones.BottomRight));
        }

        [Test]
        public void APausedGestureScanWakesOnATap()
        {
            var scan = new GestureScan(new[] { "Smile", "Brow raise" }, new Scanner(1f, 1, true));
            scan.Advance(5f);
            Assert.That(scan.Scanner.IsPaused, Is.True);
            Assert.That(scan.Tap(), Is.Null);
            Assert.That(scan.Tap(), Is.EqualTo("Smile"));
        }
    }
}
