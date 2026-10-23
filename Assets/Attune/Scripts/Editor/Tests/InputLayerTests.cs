using System.Collections.Generic;
using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>
    /// The Unity side (design, Checks): profiles round-trip through the store, CSV rows escape commas
    /// and quotes, and every demo's needs can be met by the Switch source through the adapters, but
    /// for the Point demos (ruling A18).
    /// </summary>
    public class InputLayerTests
    {
        // A store over a dictionary, so the test writes nothing to this machine's settings.
        private static ProfileStore MemoryStore(Dictionary<string, string> values) =>
            new(key => values.TryGetValue(key, out string value) ? value : null, (key, value) => values[key] = value);

        [Test]
        public void AProfileRoundTripsWithItsSourceCalibrationAndTiming()
        {
            var values = new Dictionary<string, string>();
            ProfileStore store = MemoryStore(values);
            Profile made = store.Create();
            made.Source = SourceIds.Pointer;
            made.SwitchControl = "<Mouse>/leftButton";
            made.Timing.ScanStepSeconds = 0.9f;
            made.Timing.ScanFromTop = false;
            made.SetCalibration(SourceIds.Pointer, AxisCalibration.Make("Steer", 0.4f, 0.3f, 0.55f, 0.01f, true));
            store.Save(made);
            store.CurrentId = made.Id;

            ProfileStore again = MemoryStore(values);
            Profile loaded = again.Load(again.CurrentId);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo("Player 1"));
            Assert.That(loaded.Source, Is.EqualTo(SourceIds.Pointer));
            Assert.That(loaded.SwitchControl, Is.EqualTo("<Mouse>/leftButton"));
            Assert.That(loaded.Timing.ScanStepSeconds, Is.EqualTo(0.9f));
            Assert.That(loaded.Timing.ScanFromTop, Is.False);
            AxisCalibration steer = loaded.Calibration(SourceIds.Pointer, "Steer");
            Assert.That(steer, Is.Not.Null);
            Assert.That(steer.Rest, Is.EqualTo(0.4f));
            Assert.That(steer.High, Is.EqualTo(0.55f));
            Assert.That(steer.IsSet, Is.True);
            Assert.That(loaded.Calibration(SourceIds.Gamepad, "Steer"), Is.Null);
        }

        [Test]
        public void ProfilesAreListedAndNumberedInTurn()
        {
            var values = new Dictionary<string, string>();
            ProfileStore store = MemoryStore(values);
            store.Create();
            store.Create("  Sam ");
            Assert.That(store.Ids, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(values[ProfileStore.ListKey], Is.EqualTo("1,2"));
            Assert.That(store.Load(2).Name, Is.EqualTo("Sam"));
            Assert.That(values.ContainsKey(ProfileStore.KeyPrefix + "2"), Is.True);
            Assert.That(store.All().Count, Is.EqualTo(2));
        }

        [Test]
        public void ACalibrationReplacesTheOldOneForTheSameAxis()
        {
            var profile = new Profile();
            profile.SetCalibration(SourceIds.Gamepad, AxisCalibration.Make("Steer", 0f, -1f, 1f));
            profile.SetCalibration(SourceIds.Gamepad, AxisCalibration.Make("Steer", 0.1f, -0.5f, 0.6f));
            Assert.That(profile.Calibrations.Count, Is.EqualTo(1));
            Assert.That(profile.Calibrations[0].Axes.Count, Is.EqualTo(1));
            Assert.That(profile.Calibration(SourceIds.Gamepad, "Steer").Rest, Is.EqualTo(0.1f));
        }

        [Test]
        public void ABrokenProfileDoesNotLoad()
        {
            var values = new Dictionary<string, string> { [ProfileStore.KeyPrefix + "1"] = "{not json", [ProfileStore.ListKey] = "1,x,,1" };
            ProfileStore store = MemoryStore(values);
            Assert.That(store.Ids, Is.EqualTo(new[] { 1 }));
            Assert.That(store.Load(1), Is.Null, "it logs a warning and is left as it is");
            Assert.That(store.Load(7), Is.Null);
        }

        [Test]
        public void CsvEscapesCommasQuotesAndLineBreaks()
        {
            Assert.That(FeedbackLog.Escape("Fun"), Is.EqualTo("Fun"));
            Assert.That(FeedbackLog.Escape("rest 0.1, reach 0.3"), Is.EqualTo("\"rest 0.1, reach 0.3\""));
            Assert.That(FeedbackLog.Escape("the \"big\" one"), Is.EqualTo("\"the \"\"big\"\" one\""));
            Assert.That(FeedbackLog.Escape("two\nlines"), Is.EqualTo("\"two\nlines\""));
            Assert.That(FeedbackLog.Escape(" padded"), Is.EqualTo("\" padded\""));
            Assert.That(FeedbackLog.Escape(null), Is.EqualTo(""));
        }

        [Test]
        public void ACsvRowSplitsBackIntoItsFields()
        {
            string[] fields = { "2026-10-23T10:00:00+01:00", "Jo, the tester", "Compass Sweep", "Switch", "12.5", "4", "Fun", "say \"hi\"", "0.1" };
            string row = FeedbackLog.Row(fields);
            Assert.That(FeedbackLog.Split(row), Is.EqualTo(fields));
            Assert.That(FeedbackLog.Row(FeedbackLog.Columns).Split(',').Length, Is.EqualTo(9));
        }

        [Test]
        public void TheSwitchCanPlayEveryDemoButThePointOnes()
        {
            // Ruling A18: Press → Gesture by scanning brings in the Garden and the Spells. Ruling A29: the Dwell
            // Builder scans its rows and cells; the Lighthouse Keeper is still exempt.
            foreach (DemoInfo demo in DemoCatalogue.All)
            {
                bool playable = demo.Needs.CanBeMetBy(Channels.Press);
                if (demo.SwitchExempt)
                {
                    Assert.That(playable, Is.False, demo.Name + " is exempt because a switch can't give Point");
                    Assert.That(demo.Needs.Missing(Channels.Press), Is.EqualTo(Channels.Point), demo.Name);
                }
                else
                {
                    Assert.That(playable, Is.True, demo.Name + " must be playable by one switch");
                }
            }

            Assert.That(DemoCatalogue.All.Count, Is.EqualTo(10));
        }

        [Test]
        public void EveryDemoCanBePlayedBySomeM2Source()
        {
            Channels[] sources =
            {
                Channels.Press | Channels.Steer | Channels.Gesture, // Keyboard zones
                Channels.Point | Channels.Steer | Channels.Press, // Pointer
                Channels.Press, // Switch
                Channels.Steer | Channels.Point | Channels.Press, // Gamepad
            };
            foreach (DemoInfo demo in DemoCatalogue.All)
            {
                bool any = false;
                foreach (Channels source in sources)
                {
                    any |= demo.Needs.CanBeMetBy(source);
                }

                Assert.That(any, Is.True, demo.Name);
            }
        }

        [Test]
        public void ThePitchPathRunsInOrderAndEnds()
        {
            DemoInfo demo = DemoCatalogue.Find(DemoCatalogue.Pitch[0]);
            var path = new List<string>();
            while (demo != null)
            {
                path.Add(demo.Id);
                demo = DemoCatalogue.After(demo, pitch: true);
            }

            Assert.That(path, Is.EqualTo(DemoCatalogue.Pitch));
            Assert.That(DemoCatalogue.After(DemoCatalogue.All[0], pitch: false), Is.EqualTo(DemoCatalogue.All[1]));
            Assert.That(DemoCatalogue.After(DemoCatalogue.All[9], pitch: false), Is.Null);
        }

        [Test]
        public void KeyboardKeysFallInTheirZones()
        {
            Assert.That(KeyboardZones.Quadrant(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.Q).x,
                KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.Q).y), Is.EqualTo(KeyboardZones.TopLeft));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.T).x), Is.EqualTo(-1));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.Y).x), Is.EqualTo(1));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.G).x), Is.EqualTo(-1));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.H).x), Is.EqualTo(1));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.B).x), Is.EqualTo(-1));
            Assert.That(KeyboardZones.Half(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.N).x), Is.EqualTo(1));
            Vector2Assert(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.Enter), KeyboardZones.BottomRight);
            Vector2Assert(KeyboardZonesSource.Place(UnityEngine.InputSystem.Key.LeftArrow), KeyboardZones.BottomRight);
            Assert.That(KeyboardZonesSource.IsReserved(UnityEngine.InputSystem.Key.Escape), Is.True);
            Assert.That(KeyboardZonesSource.IsReserved(UnityEngine.InputSystem.Key.F2), Is.True);
        }

        private static void Vector2Assert(UnityEngine.Vector2 place, string quadrant) =>
            Assert.That(KeyboardZones.Quadrant(place.x, place.y), Is.EqualTo(quadrant));
    }
}
