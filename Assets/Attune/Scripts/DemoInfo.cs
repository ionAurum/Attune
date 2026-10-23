using Attune.Signals;

namespace Attune
{
    /// <summary>One demo, as the hub shows it (design, Demos): its name, a line about it, the channels it needs and its scene.</summary>
    public sealed class DemoInfo
    {
        /// <summary>A demo.</summary>
        public DemoInfo(string id, string name, string line, DemoNeeds needs, string milestone, string scene = DemoCatalogue.StubScene,
            bool switchExempt = false)
        {
            Id = id;
            Name = name;
            Line = line;
            Needs = needs;
            Milestone = milestone;
            Scene = scene;
            SwitchExempt = switchExempt;
        }

        /// <summary>A short name for code and the feedback file: "CompassSweep".</summary>
        public string Id { get; }

        /// <summary>The name players see.</summary>
        public string Name { get; }

        /// <summary>One line about it, for its tile.</summary>
        public string Line { get; }

        /// <summary>The channels it needs.</summary>
        public DemoNeeds Needs { get; }

        /// <summary>The milestone it is built in.</summary>
        public string Milestone { get; }

        /// <summary>The scene it plays in: the stub until it is built.</summary>
        public string Scene { get; }

        /// <summary>True if the Switch source need not play it yet (ruling A18: the Point demos).</summary>
        public bool SwitchExempt { get; }

        /// <summary>True until the demo has a scene of its own.</summary>
        public bool IsStub => Scene == DemoCatalogue.StubScene;
    }
}
