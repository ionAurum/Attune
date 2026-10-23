using System.Collections.Generic;
using Attune.Signals;

namespace Attune
{
    /// <summary>
    /// Static: the ten demos in the hub's order (design, Demos), and the pitch path (ruling A13).
    /// A built demo plays in its own scene, named by its Id; until then it plays in the stub scene,
    /// which shows what the input layer reads.
    /// </summary>
    public static class DemoCatalogue
    {
        /// <summary>The hub's scene.</summary>
        public const string HubScene = "Hub";

        /// <summary>The scene every demo plays in until it has its own.</summary>
        public const string StubScene = "DemoStub";

        /// <summary>Every demo, in the hub's order.</summary>
        public static readonly IReadOnlyList<DemoInfo> All = new List<DemoInfo>
        {
            new("CompassSweep", "Compass Sweep", "A needle sweeps; a press sends the boat that way.",
                new DemoNeeds(Channels.Press), "M4", "CompassSweep"),
            new("KeyboardGarden", "Keyboard Garden", "Plant in four beds; hold to grow.",
                new DemoNeeds(Channels.Gesture, Channels.Press), "M4", "KeyboardGarden"),

            // Ruling A29: a source that points dwells; one that only presses scans the rows and cells.
            new("DwellBuilder", "Dwell Builder", "Build by resting on blocks, or by scanning. No clicks.",
                new DemoNeeds(Channels.Point | Channels.Press), "M4", "DwellBuilder"),
            new("KiteBreath", "Kite Breath", "Breathe out to lift the kite.",
                new DemoNeeds(Channels.Level), "M5", "KiteBreath"),
            new("HumFlyer", "Hum Flyer", "Hum higher to climb, lower to dive.",
                new DemoNeeds(Channels.Level), "M5", "HumFlyer"),
            new("HeadLeanSailing", "Head-Lean Sailing", "Lean or tilt to round the buoys.",
                new DemoNeeds(Channels.Steer), "M6"),
            new("TremorLoom", "Tremor Loom", "Draw threads; the wobble becomes the weave.",
                new DemoNeeds(Channels.Point | Channels.Steer), "M8"),
            new("ExpressionSpells", "Expression Spells", "Two gestures make a spell to light a lantern.",
                new DemoNeeds(Channels.Gesture), "M7"),
            new("LighthouseKeeper", "Lighthouse Keeper", "Guide ships home with the beam.",
                new DemoNeeds(Channels.Point, Channels.Select), "M6", switchExempt: true),
            new("EchoRoom", "Echo Room", "Find the chime by its echoes.",
                new DemoNeeds(Channels.Steer, Channels.Press), "M8"),
        };

        /// <summary>The pitch path after the hub (ruling A13).</summary>
        public static readonly IReadOnlyList<string> Pitch = new[] { "CompassSweep", "KiteBreath", "HeadLeanSailing", "ExpressionSpells" };

        /// <summary>A demo by Id, or null.</summary>
        public static DemoInfo Find(string id)
        {
            foreach (DemoInfo demo in All)
            {
                if (demo.Id == id)
                {
                    return demo;
                }
            }

            return null;
        }

        /// <summary>The demo after this one: on the pitch path while playing the pitch, else in the hub's order; null after the last.</summary>
        public static DemoInfo After(DemoInfo demo, bool pitch)
        {
            if (demo == null)
            {
                return null;
            }

            if (pitch)
            {
                for (int i = 0; i < Pitch.Count - 1; i++)
                {
                    if (Pitch[i] == demo.Id)
                    {
                        return Find(Pitch[i + 1]);
                    }
                }

                return null;
            }

            for (int i = 0; i < All.Count - 1; i++)
            {
                if (All[i] == demo)
                {
                    return All[i + 1];
                }
            }

            return null;
        }
    }
}
