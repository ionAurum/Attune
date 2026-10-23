using System;

namespace Attune.Signals
{
    /// <summary>
    /// A Level that glides down while its source hears nothing to measure (ruling A34: the Hum
    /// source between notes). While the source hears something the level is what it hears; once it
    /// goes silent the level falls from where it was, taking fallSeconds from 1 to 0, as Press → Level
    /// does. Rest stays 0, so a source that stops sounding comes back to rest.
    /// </summary>
    public sealed class LevelGlide
    {
        /// <summary>The level now, 0 to 1.</summary>
        public float Level { get; private set; }

        /// <summary>True while the source is silent and the level gliding (and before it first hears anything).</summary>
        public bool IsGliding { get; private set; } = true;

        /// <summary>One frame of dt seconds: silent, or hearing level. Returns the level now.</summary>
        public float Update(bool silent, float level, float dt, float fallSeconds)
        {
            if (silent)
            {
                IsGliding = true;
                if (dt > 0f)
                {
                    Level = Math.Max(0f, Level - dt / Math.Max(1e-3f, fallSeconds));
                }

                return Level;
            }

            IsGliding = false;
            Level = Math.Min(1f, Math.Max(0f, level));
            return Level;
        }

        /// <summary>Back to rest, silent: as when the source changes.</summary>
        public void Reset()
        {
            Level = 0f;
            IsGliding = true;
        }
    }
}
