namespace Attune.Signals
{
    /// <summary>
    /// Press → Select by scanning (design, Scanning): options highlight one at a time, StepSeconds
    /// each, and a press picks the highlighted one. After Rounds rounds with no press it pauses, and
    /// the next press wakes it rather than picking. After a pick or a pause it starts from the top
    /// again if FromTop, else carries on from where it was. The menus' ScanDriver and the demos'
    /// scanning in the world (the Compass Sweep's needle) both use it.
    /// </summary>
    public sealed class Scanner
    {
        private float _sinceStep;
        private int _shown; // highlights shown since the scan started, woke or picked, the current one included

        /// <summary>A scanner with these timings and no options yet.</summary>
        public Scanner(float stepSeconds = 1.5f, int rounds = 3, bool fromTop = true)
        {
            StepSeconds = stepSeconds;
            Rounds = rounds;
            FromTop = fromTop;
        }

        /// <summary>How long each option stays highlighted.</summary>
        public float StepSeconds { get; set; }

        /// <summary>Rounds with no press before pausing; 0 never pauses.</summary>
        public int Rounds { get; set; }

        /// <summary>Start from the first option after a pick or a pause.</summary>
        public bool FromTop { get; set; }

        /// <summary>How many options there are.</summary>
        public int Count { get; private set; }

        /// <summary>The highlighted option, or −1 with none.</summary>
        public int Index { get; private set; } = -1;

        /// <summary>True when the scan has paused for lack of a press; the next press wakes it.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>How far through the current step it is, 0 to 1.</summary>
        public float StepProgress => StepSeconds <= 0f ? 1f : System.Math.Min(1f, _sinceStep / StepSeconds);

        /// <summary>Starts scanning count options from the first (or from start), awake.</summary>
        public void Reset(int count, int start = 0)
        {
            Count = count < 0 ? 0 : count;
            Index = Count == 0 ? -1 : start < 0 || start >= Count ? 0 : start;
            IsPaused = false;
            _sinceStep = 0f;
            _shown = Count == 0 ? 0 : 1;
        }

        /// <summary>Lets dt seconds pass; returns true if the highlight moved.</summary>
        public bool Advance(float dt)
        {
            if (Count == 0 || IsPaused || StepSeconds <= 0f)
            {
                return false;
            }

            bool moved = false;
            _sinceStep += dt;
            while (_sinceStep >= StepSeconds)
            {
                _sinceStep -= StepSeconds;
                if (Rounds > 0 && _shown >= Rounds * Count)
                {
                    IsPaused = true;
                    _sinceStep = 0f;
                    return moved;
                }

                Index = (Index + 1) % Count;
                _shown++;
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// A press: returns the option it picked, and scanning goes on from the top or from there;
        /// or −1 if there are none, or the scan was paused, which the press wakes.
        /// </summary>
        public int Press()
        {
            if (Count == 0)
            {
                return -1;
            }

            if (IsPaused)
            {
                Restart(Index);
                return -1;
            }

            int picked = Index;
            Restart(picked);
            return picked;
        }

        private void Restart(int from)
        {
            IsPaused = false;
            _sinceStep = 0f;
            _shown = 1;
            Index = FromTop ? 0 : from;
        }
    }
}
