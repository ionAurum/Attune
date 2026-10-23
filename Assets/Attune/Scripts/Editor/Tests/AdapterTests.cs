using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>Every adapter (design, Adapters), the routes between channels, and the keyboard zones.</summary>
    public class AdapterTests
    {
        [Test]
        public void LevelToPressHasHysteresis()
        {
            var adapter = new LevelToPress(0.5f, 0.35f);
            Assert.That(adapter.Update(0.4f), Is.EqualTo(0));
            Assert.That(adapter.Update(0.5f), Is.EqualTo(1));
            Assert.That(adapter.IsDown, Is.True);
            Assert.That(adapter.Update(0.45f), Is.EqualTo(0), "wavering under the threshold holds the press");
            Assert.That(adapter.Update(0.36f), Is.EqualTo(0));
            Assert.That(adapter.Update(0.3f), Is.EqualTo(-1));
            Assert.That(adapter.Update(0.45f), Is.EqualTo(0), "it must reach the threshold again");
        }

        [Test]
        public void ABreathPuffIsOnePress()
        {
            var adapter = new LevelToPress(0.5f, 0.35f);
            var press = new PressTracker();
            var timing = new TimingValues();
            float[] puff = { 0f, 0.2f, 0.55f, 0.48f, 0.52f, 0.44f, 0.3f, 0.1f, 0f };
            int presses = 0;
            for (int i = 0; i < puff.Length; i++)
            {
                int edge = adapter.Update(puff[i]);
                if (edge > 0)
                {
                    press.Down(i * 0.05);
                }
                else if (edge < 0 && press.Up(i * 0.05, timing) == PressKind.Tap)
                {
                    presses++;
                }
            }

            Assert.That(presses, Is.EqualTo(1));
        }

        [Test]
        public void PressToLevelRampsUpAndFalls()
        {
            var adapter = new PressToLevel(riseSeconds: 2f, fallSeconds: 4f);
            adapter.Update(true, 1f);
            Assert.That(adapter.Level, Is.EqualTo(0.5f).Within(1e-5f));
            adapter.Update(true, 5f);
            Assert.That(adapter.Level, Is.EqualTo(1f), "it stops at 1");
            adapter.Update(false, 1f);
            Assert.That(adapter.Level, Is.EqualTo(0.75f).Within(1e-5f));
            adapter.Update(false, 10f);
            Assert.That(adapter.Level, Is.EqualTo(0f), "it stops at 0");
        }

        [Test]
        public void SteerAndLevelConvert()
        {
            Assert.That(SteerLevel.ToLevel(0.6f), Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(SteerLevel.ToLevel(-0.6f), Is.EqualTo(0f), "the other side is 0");
            Assert.That(SteerLevel.ToLevel(-0.6f, -1), Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(SteerLevel.ToSteer(0.5f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(SteerLevel.ToSteer(0f), Is.EqualTo(-1f));
            Assert.That(SteerLevel.ToSteer(1f), Is.EqualTo(1f));
        }

        [Test]
        public void ASwitchReachesEverythingButPoint()
        {
            Channels reached = ChannelRoutes.Reachable(Channels.Press);
            Assert.That(reached, Is.EqualTo(Channels.Press | Channels.Level | Channels.Steer | Channels.Select | Channels.Gesture));
        }

        [Test]
        public void BreathReachesPressAndSelect()
        {
            Channels reached = ChannelRoutes.Reachable(Channels.Level);
            Assert.That(ChannelRoutes.Has(reached, Channels.Press), Is.True);
            Assert.That(ChannelRoutes.Has(reached, Channels.Select), Is.True);
            Assert.That(ChannelRoutes.Has(reached, Channels.Steer), Is.True);
            Assert.That(ChannelRoutes.Has(reached, Channels.Point), Is.False);
        }

        [Test]
        public void APointerOnlySourceSelectsByDwell()
        {
            Assert.That(ChannelRoutes.Reachable(Channels.Point), Is.EqualTo(Channels.Point | Channels.Select));
            Assert.That(ChannelRoutes.How(Channels.Select, Channels.Point), Is.EqualTo("dwell"));
            Assert.That(ChannelRoutes.How(Channels.Select, Channels.Press), Is.EqualTo("a tap"));
            Assert.That(ChannelRoutes.How(Channels.Level, Channels.Press), Is.EqualTo("hold to raise"));
            Assert.That(ChannelRoutes.How(Channels.Gesture, Channels.Press), Is.EqualTo("scan the gestures"));
            Assert.That(ChannelRoutes.How(Channels.Point, Channels.Press), Is.Null);
            Assert.That(ChannelRoutes.How(Channels.Press, Channels.Press), Is.EqualTo("direct"));
        }

        [Test]
        public void NeedsAreMetByAnyOfTheirChannels()
        {
            var loom = new DemoNeeds(Channels.Point | Channels.Steer);
            Assert.That(loom.CanBeMetBy(Channels.Steer), Is.True);
            Assert.That(loom.CanBeMetBy(Channels.Point), Is.True);
            Assert.That(loom.CanBeMetBy(Channels.Press), Is.True, "Press → Level → Steer");

            var builder = new DemoNeeds(Channels.Point, Channels.Select);
            Assert.That(builder.CanBeMetBy(Channels.Press), Is.False);
            Assert.That(builder.Missing(Channels.Press), Is.EqualTo(Channels.Point));
            Assert.That(builder.ToString(), Is.EqualTo("Point, Select"));
            Assert.That(loom.ToString(), Is.EqualTo("Steer or Point"));
        }

        [Test]
        public void KeyboardZonesSplitIntoHalvesAndQuadrants()
        {
            Assert.That(KeyboardZones.Half(0.1f), Is.EqualTo(-1));
            Assert.That(KeyboardZones.Half(0.9f), Is.EqualTo(1));
            Assert.That(KeyboardZones.Half(1.3f), Is.EqualTo(1), "keys right of the main block are on the right");
            Assert.That(KeyboardZones.Quadrant(0.1f, 0.2f), Is.EqualTo(KeyboardZones.TopLeft));
            Assert.That(KeyboardZones.Quadrant(0.8f, 0.2f), Is.EqualTo(KeyboardZones.TopRight));
            Assert.That(KeyboardZones.Quadrant(0.1f, 0.8f), Is.EqualTo(KeyboardZones.BottomLeft));
            Assert.That(KeyboardZones.Quadrant(0.8f, 0.8f), Is.EqualTo(KeyboardZones.BottomRight));
        }
    }
}
