namespace Attune.Signals
{
    /// <summary>
    /// Level → Press (design, Adapters): crossing OnLevel upward starts a press, falling below
    /// OffLevel ends it. The gap between them (hysteresis) stops a level that wavers at the threshold
    /// from flickering. A breath puff becomes a switch.
    /// </summary>
    public sealed class LevelToPress
    {
        /// <summary>An adapter with these thresholds.</summary>
        public LevelToPress(float onLevel = 0.5f, float offLevel = 0.35f)
        {
            OnLevel = onLevel;
            OffLevel = offLevel;
        }

        /// <summary>The level a press starts at.</summary>
        public float OnLevel { get; set; }

        /// <summary>The level a press ends below.</summary>
        public float OffLevel { get; set; }

        /// <summary>True while the level holds the press down.</summary>
        public bool IsDown { get; private set; }

        /// <summary>A new level: +1 if the press started, −1 if it ended, 0 if nothing changed.</summary>
        public int Update(float level)
        {
            if (!IsDown && level >= OnLevel)
            {
                IsDown = true;
                return 1;
            }

            if (IsDown && level < OffLevel)
            {
                IsDown = false;
                return -1;
            }

            return 0;
        }

        /// <summary>Lets go of a held press without reporting it, as when the source changes.</summary>
        public void Reset() => IsDown = false;
    }
}
