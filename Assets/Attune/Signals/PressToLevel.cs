using System;

namespace Attune.Signals
{
    /// <summary>
    /// Press → Level (design, Adapters): holding ramps the level up, taking RiseSeconds from 0 to 1,
    /// and releasing lets it fall, taking FallSeconds from 1 to 0. A switch can fly the kite.
    /// </summary>
    public sealed class PressToLevel
    {
        /// <summary>An adapter with these rates.</summary>
        public PressToLevel(float riseSeconds = 1.5f, float fallSeconds = 2.5f)
        {
            RiseSeconds = riseSeconds;
            FallSeconds = fallSeconds;
        }

        /// <summary>How long holding takes to raise the level from 0 to 1.</summary>
        public float RiseSeconds { get; set; }

        /// <summary>How long the level takes to fall from 1 to 0 once released.</summary>
        public float FallSeconds { get; set; }

        /// <summary>The level now, 0 to 1.</summary>
        public float Level { get; private set; }

        /// <summary>dt seconds with the press down or not; returns the new level.</summary>
        public float Update(bool down, float dt)
        {
            if (dt <= 0f)
            {
                return Level;
            }

            float seconds = Math.Max(1e-3f, down ? RiseSeconds : FallSeconds);
            float step = dt / seconds;
            Level = Math.Min(1f, Math.Max(0f, Level + (down ? step : -step)));
            return Level;
        }

        /// <summary>Drops the level to 0, as when the source changes.</summary>
        public void Reset() => Level = 0f;
    }
}
