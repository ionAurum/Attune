using System;
using System.Collections.Generic;

namespace Attune
{
    /// <summary>
    /// The Kite Breath's ribbons and the Hum Flyer's rings (ruling A33, a slow loop): marks at
    /// different heights drift right to left past the flyer, which stays at ProbeX, at a slow and
    /// steady Speed. A mark is reached if the flyer's height is within Reach of it as it passes.
    /// One missed comes round again on the next pass, so missing costs nothing (ruling A11). Every
    /// mark reached, a round is done; Reset starts a new one. Distances are in screen widths,
    /// heights 0 (the ground) to 1 (the top of the sky). Plain C#, so the tests can drive it.
    /// </summary>
    public sealed class DriftCourse
    {
        private readonly float[] _heights;
        private readonly bool[] _reached;

        /// <summary>
        /// A course of marks at these heights, spacing apart round the loop (at least far enough that a
        /// mark is never on the screen twice), with the flyer probeX across the screen.
        /// </summary>
        public DriftCourse(IReadOnlyList<float> heights, float spacing, float probeX)
        {
            _heights = new float[Math.Max(1, heights.Count)];
            for (int i = 0; i < heights.Count; i++)
            {
                _heights[i] = Math.Min(1f, Math.Max(0f, heights[i]));
            }

            _reached = new bool[_heights.Length];
            Spacing = Math.Max(spacing, 1.5f / _heights.Length);
            ProbeX = probeX;
            Length = Spacing * _heights.Length;
        }

        /// <summary>How far apart the marks are.</summary>
        public float Spacing { get; }

        /// <summary>Where the flyer is, across the screen.</summary>
        public float ProbeX { get; }

        /// <summary>How far past the flyer the first mark starts: most of a spacing, so it is on the screen from the start.</summary>
        public float EnteringAt => Spacing * 0.9f;

        /// <summary>How long the loop is: a mark comes round again after this far.</summary>
        public float Length { get; }

        /// <summary>How fast the marks drift, in screen widths a second.</summary>
        public float Speed { get; set; } = 0.07f;

        /// <summary>How near the flyer's height must be to a mark's to reach it.</summary>
        public float Reach { get; set; } = 0.08f;

        /// <summary>How far the marks have drifted, round the loop.</summary>
        public float Drifted { get; private set; }

        /// <summary>How many marks there are.</summary>
        public int Count => _heights.Length;

        /// <summary>How many have been reached this round.</summary>
        public int Reached { get; private set; }

        /// <summary>True once every mark has been reached this round.</summary>
        public bool AllReached => Reached == Count;

        /// <summary>A mark's height.</summary>
        public float Height(int mark) => _heights[mark];

        /// <summary>True if a mark has been reached this round.</summary>
        public bool IsReached(int mark) => _reached[mark];

        /// <summary>How far a mark still has to drift to reach the flyer, 0 to Length.</summary>
        public float Ahead(int mark)
        {
            float ahead = (EnteringAt + mark * Spacing - Drifted) % Length;
            return ahead < 0f ? ahead + Length : ahead;
        }

        /// <summary>Where a mark is across the screen, in widths: a mark just past the flyer is to its left, not round at the far right.</summary>
        public float ScreenX(int mark)
        {
            float ahead = Ahead(mark);
            return ahead > Length - ProbeX - 0.25f ? ProbeX + ahead - Length : ProbeX + ahead;
        }

        /// <summary>The mark that will next pass the flyer and isn't reached yet, or −1 if every one is.</summary>
        public int Next
        {
            get
            {
                int next = -1;
                for (int i = 0; i < Count; i++)
                {
                    if (!_reached[i] && (next < 0 || Ahead(i) < Ahead(next)))
                    {
                        next = i;
                    }
                }

                return next;
            }
        }

        /// <summary>
        /// dt seconds of drift with the flyer at height: returns the mark reached in that time, or −1.
        /// A mark passing out of reach is simply passed by.
        /// </summary>
        public int Advance(float dt, float height)
        {
            float step = Math.Max(0f, Speed * dt);
            if (step <= 0f)
            {
                return -1;
            }

            int reached = -1;
            for (int i = 0; i < Count; i++)
            {
                if (!_reached[i] && Ahead(i) <= step && Math.Abs(height - _heights[i]) <= Reach)
                {
                    _reached[i] = true;
                    Reached++;
                    reached = i;
                }
            }

            Drifted = (Drifted + step) % Length;
            return reached;
        }

        /// <summary>A new round: no mark reached. The marks drift on from where they are.</summary>
        public void Reset()
        {
            Array.Clear(_reached, 0, _reached.Length);
            Reached = 0;
        }
    }
}
