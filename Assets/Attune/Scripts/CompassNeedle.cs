using System.Collections.Generic;
using Attune.Signals;

namespace Attune
{
    /// <summary>
    /// The Compass Sweep's needle (design, Demos, 1): scanning in the world. It sweeps clockwise
    /// over the headings the boat can sail, skipping those onto land or off the map, and waits after
    /// one full sweep with no press; the next press wakes it rather than sailing. After a step it
    /// stays on that heading, so pressing again repeats the step (ruling A31). Its speed is the
    /// profile's scan step.
    /// </summary>
    public sealed class CompassNeedle
    {
        private readonly List<int> _open = new();

        /// <summary>A needle stepping with scanner, which it sets to wait after one sweep and to stay where it picked.</summary>
        public CompassNeedle(Scanner scanner)
        {
            Scanner = scanner;
            Scanner.Rounds = 1;
            Scanner.FromTop = false;
        }

        /// <summary>The scanner behind it.</summary>
        public Scanner Scanner { get; }

        /// <summary>The headings it sweeps, clockwise from north.</summary>
        public IReadOnlyList<int> Open => _open;

        /// <summary>The heading it points at, or −1 when no heading is open.</summary>
        public int Heading => Scanner.Index >= 0 && Scanner.Index < _open.Count ? _open[Scanner.Index] : -1;

        /// <summary>True when it has swept once with no press and waits for one.</summary>
        public bool IsWaiting => Scanner.IsPaused;

        /// <summary>
        /// The boat has moved, or the map is new: sweeps the headings now open, starting on stayOn if
        /// it is open, else on the next open heading clockwise from it. Awake.
        /// </summary>
        public void Refresh(CompassMap map, int stayOn)
        {
            _open.Clear();
            for (int heading = 0; heading < CompassMap.Headings; heading++)
            {
                if (map.IsOpen(heading))
                {
                    _open.Add(heading);
                }
            }

            int start = 0;
            for (int step = 0; step < CompassMap.Headings; step++)
            {
                int index = _open.IndexOf(CompassMap.Wrap(stayOn + step));
                if (index >= 0)
                {
                    start = index;
                    break;
                }
            }

            Scanner.Reset(_open.Count, start);
        }

        /// <summary>Lets dt seconds pass, at stepSeconds a heading; returns true if the needle moved.</summary>
        public bool Advance(float dt, float stepSeconds)
        {
            Scanner.StepSeconds = stepSeconds;
            return Scanner.Advance(dt);
        }

        /// <summary>A press: the heading to sail, or −1 if it woke the waiting needle (or none is open).</summary>
        public int Press()
        {
            int picked = Scanner.Press();
            return picked >= 0 && picked < _open.Count ? _open[picked] : -1;
        }
    }
}
