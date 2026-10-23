using System;

namespace Attune
{
    /// <summary>
    /// How high the kite or the flyer is, 0 (the ground) to 1 (the top), following the Level: it
    /// climbs towards it taking RiseSeconds for the whole sky, and sinks towards it taking
    /// FallSeconds. The kite rises quickly and falls gently when the breath stops (design, Demos, 4);
    /// the flyer climbs and dives at one rate. Plain C#, so the tests can drive it.
    /// </summary>
    public sealed class FlightHeight
    {
        /// <summary>A height that follows the Level at these rates.</summary>
        public FlightHeight(float riseSeconds, float fallSeconds)
        {
            RiseSeconds = riseSeconds;
            FallSeconds = fallSeconds;
        }

        /// <summary>How long climbing the whole sky takes.</summary>
        public float RiseSeconds { get; set; }

        /// <summary>How long sinking the whole sky takes.</summary>
        public float FallSeconds { get; set; }

        /// <summary>The height now.</summary>
        public float Height { get; private set; }

        /// <summary>dt seconds following level; returns the new height.</summary>
        public float Update(float level, float dt)
        {
            float target = Math.Min(1f, Math.Max(0f, level));
            if (dt <= 0f)
            {
                return Height;
            }

            if (target > Height)
            {
                Height = Math.Min(target, Height + dt / Math.Max(1e-3f, RiseSeconds));
            }
            else
            {
                Height = Math.Max(target, Height - dt / Math.Max(1e-3f, FallSeconds));
            }

            return Height;
        }

        /// <summary>Puts it at height at once.</summary>
        public void Place(float height) => Height = Math.Min(1f, Math.Max(0f, height));
    }
}
