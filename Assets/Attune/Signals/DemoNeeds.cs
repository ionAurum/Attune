using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// The channels a demo needs (design, Channels). Each need is a set of channels any one of which
    /// will do: the Tremor Loom needs Point or Steer, the Echo Room needs Steer and Press.
    /// </summary>
    public sealed class DemoNeeds
    {
        private readonly Channels[] _needs;

        /// <summary>Each argument is one need, met by any one of its channels.</summary>
        public DemoNeeds(params Channels[] needs)
        {
            _needs = needs ?? new Channels[0];
        }

        /// <summary>The needs, each a set of channels any one of which meets it.</summary>
        public IReadOnlyList<Channels> Needs => _needs;

        /// <summary>Every channel named in any need.</summary>
        public Channels Named
        {
            get
            {
                Channels all = Channels.None;
                foreach (Channels need in _needs)
                {
                    all |= need;
                }

                return all;
            }
        }

        /// <summary>True if a source supplying supplied can play the demo, through adapters if need be.</summary>
        public bool CanBeMetBy(Channels supplied) => Missing(supplied) == Channels.None;

        /// <summary>The needs a source supplying supplied can't meet, as one set; None if it can play the demo.</summary>
        public Channels Missing(Channels supplied)
        {
            Channels reachable = ChannelRoutes.Reachable(supplied);
            Channels missing = Channels.None;
            foreach (Channels need in _needs)
            {
                if ((need & reachable) == 0)
                {
                    missing |= need;
                }
            }

            return missing;
        }

        /// <summary>The needs in words: "Point or Steer, Press".</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (Channels need in _needs)
            {
                parts.Add(ChannelRoutes.Names(need).Replace(", ", " or "));
            }

            return string.Join(", ", parts);
        }
    }
}
