using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// Static: which channels a source reaches, directly or through the adapters (design, The input
    /// layer, Adapters), and how each one is reached.
    /// </summary>
    public static class ChannelRoutes
    {
        /// <summary>Every channel, in the order the design lists them.</summary>
        public static readonly Channels[] All =
        {
            Channels.Level, Channels.Steer, Channels.Point, Channels.Press, Channels.Gesture, Channels.Select,
        };

        /// <summary>The channels a source supplying supplied can give, directly or through adapters.</summary>
        public static Channels Reachable(Channels supplied)
        {
            Channels reached = supplied;
            while (true)
            {
                Channels next = reached;
                if (Has(reached, Channels.Press))
                {
                    next |= Channels.Level | Channels.Select | Channels.Gesture;
                }

                if (Has(reached, Channels.Level))
                {
                    next |= Channels.Press | Channels.Steer;
                }

                if (Has(reached, Channels.Steer))
                {
                    next |= Channels.Level;
                }

                if (Has(reached, Channels.Point))
                {
                    next |= Channels.Select;
                }

                if (next == reached)
                {
                    return reached;
                }

                reached = next;
            }
        }

        /// <summary>
        /// How one channel is had from a source supplying supplied, in a few words for the hub and
        /// the overlay: "direct", "hold to raise"; null if it can't be.
        /// </summary>
        public static string How(Channels channel, Channels supplied)
        {
            if (Has(supplied, channel))
            {
                return "direct";
            }

            Channels reachable = Reachable(supplied);
            if (!Has(reachable, channel))
            {
                return null;
            }

            switch (channel)
            {
                case Channels.Level:
                    return Has(supplied, Channels.Press) || !Has(supplied, Channels.Steer) ? "hold to raise" : "from steer";
                case Channels.Steer:
                    return "from level";
                case Channels.Press:
                    return "level over a threshold";
                case Channels.Gesture:
                    return "scan the gestures";
                case Channels.Select:
                    return Has(reachable, Channels.Press) ? "a tap" : "dwell";
                default:
                    return null;
            }
        }

        /// <summary>The names of the channels in a set, in the design's order: "Point, Press".</summary>
        public static string Names(Channels set)
        {
            var names = new List<string>();
            foreach (Channels channel in All)
            {
                if (Has(set, channel))
                {
                    names.Add(channel.ToString());
                }
            }

            return string.Join(", ", names);
        }

        /// <summary>True if set holds channel.</summary>
        public static bool Has(Channels set, Channels channel) => (set & channel) != 0;
    }
}
