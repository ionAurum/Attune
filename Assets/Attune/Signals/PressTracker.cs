namespace Attune.Signals
{
    /// <summary>
    /// One press channel's state: whether it is down, for how long, and what releasing it would make
    /// of it. A second Down while down, or an Up while up, is ignored, so a source that reports a
    /// held key every frame is fine.
    /// </summary>
    public sealed class PressTracker
    {
        private double _downAt;

        /// <summary>True while the press is held.</summary>
        public bool IsDown { get; private set; }

        /// <summary>How many presses have ended since the last ResetCount.</summary>
        public int Count { get; private set; }

        /// <summary>The press started at time (seconds); returns true if it wasn't already down.</summary>
        public bool Down(double time)
        {
            if (IsDown)
            {
                return false;
            }

            IsDown = true;
            _downAt = time;
            return true;
        }

        /// <summary>The press ended at time: what it was, or null if it wasn't down.</summary>
        public PressKind? Up(double time, TimingValues timing)
        {
            if (!IsDown)
            {
                return null;
            }

            IsDown = false;
            Count++;
            return timing.Classify((float)(time - _downAt));
        }

        /// <summary>Drops a held press without classifying it, as when the source changes under it.</summary>
        public void Cancel() => IsDown = false;

        /// <summary>How long the press has been held at time; 0 when it isn't down.</summary>
        public float HeldSeconds(double time) => IsDown ? (float)(time - _downAt) : 0f;

        /// <summary>What releasing at time would make of the press, or null when it isn't down.</summary>
        public PressKind? KindIfReleased(double time, TimingValues timing) => IsDown ? timing.Classify(HeldSeconds(time)) : (PressKind?)null;

        /// <summary>Starts counting presses again from 0.</summary>
        public void ResetCount() => Count = 0;
    }
}
