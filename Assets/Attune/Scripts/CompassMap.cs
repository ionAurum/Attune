using System.Collections.Generic;

namespace Attune
{
    /// <summary>
    /// The Compass Sweep's rules (design, Demos, 1): a grid of sea and land, a boat that moves one
    /// cell along any of eight headings onto sea, and flags to reach in any order. Row 0 is the top
    /// (north). Plain C#, so the tests can play it.
    /// </summary>
    public sealed class CompassMap
    {
        /// <summary>How many headings the needle sweeps.</summary>
        public const int Headings = 8;

        /// <summary>The headings' names, clockwise from north.</summary>
        public static readonly string[] HeadingNames =
        {
            "North", "North-east", "East", "South-east", "South", "South-west", "West", "North-west",
        };

        /// <summary>
        /// The island: '.' sea, '#' land, 'B' the boat's start, '1' to '9' the flags. Every row the
        /// same length.
        /// </summary>
        public static readonly string[] Island =
        {
            ". . . . . . . . . . . . .",
            ". 1 . . . # # . . . . 2 .",
            ". . . . # # # # . . . . .",
            ". . . # # # # # # . . . .",
            ". . # # # # # # # . 3 . .",
            ". 5 . . # # # # . . . . .",
            ". . . . . # # . . 4 . . .",
            "B . . . . . . . . . . . .",
        };

        private static readonly int[] _dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] _dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        private readonly bool[,] _land;
        private readonly List<(int X, int Y)> _flags = new();
        private readonly List<bool> _reached = new();

        /// <summary>A map from rows of cells (Island's format; spaces are ignored).</summary>
        public CompassMap(IReadOnlyList<string> rows)
        {
            var cells = new List<string>();
            foreach (string row in rows)
            {
                cells.Add(row.Replace(" ", ""));
            }

            Height = cells.Count;
            Width = Height > 0 ? cells[0].Length : 0;
            _land = new bool[Width, Height];
            var flags = new SortedDictionary<char, (int, int)>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width && x < cells[y].Length; x++)
                {
                    char c = cells[y][x];
                    if (c == '#')
                    {
                        _land[x, y] = true;
                    }
                    else if (c == 'B')
                    {
                        Boat = (x, y);
                    }
                    else if (c >= '1' && c <= '9')
                    {
                        flags[c] = (x, y);
                    }
                }
            }

            foreach ((int, int) flag in flags.Values)
            {
                _flags.Add(flag);
                _reached.Add(false);
            }
        }

        /// <summary>Cells across.</summary>
        public int Width { get; }

        /// <summary>Cells down.</summary>
        public int Height { get; }

        /// <summary>Where the boat is.</summary>
        public (int X, int Y) Boat { get; private set; }

        /// <summary>The flags, in their number's order.</summary>
        public IReadOnlyList<(int X, int Y)> Flags => _flags;

        /// <summary>How many flags have been reached this round.</summary>
        public int FlagsReached
        {
            get
            {
                int count = 0;
                foreach (bool reached in _reached)
                {
                    count += reached ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>True once every flag has been reached this round.</summary>
        public bool AllReached => _flags.Count > 0 && FlagsReached == _flags.Count;

        /// <summary>One step along heading: the change in x and in y (y down).</summary>
        public static (int X, int Y) Offset(int heading) => (_dx[Wrap(heading)], _dy[Wrap(heading)]);

        /// <summary>A heading in 0 to 7.</summary>
        public static int Wrap(int heading) => ((heading % Headings) + Headings) % Headings;

        /// <summary>True if a flag has been reached this round.</summary>
        public bool IsReached(int flag) => flag >= 0 && flag < _reached.Count && _reached[flag];

        /// <summary>True for a land cell.</summary>
        public bool IsLand(int x, int y) => IsInside(x, y) && _land[x, y];

        /// <summary>True for a sea cell on the map: somewhere the boat can be.</summary>
        public bool IsSea(int x, int y) => IsInside(x, y) && !_land[x, y];

        /// <summary>True if the boat can take a step along heading: onto sea, on the map.</summary>
        public bool IsOpen(int heading)
        {
            (int dx, int dy) = Offset(heading);
            return IsSea(Boat.X + dx, Boat.Y + dy);
        }

        /// <summary>
        /// Moves the boat one step along heading, if that is open. Returns true if it moved; flag is
        /// the flag it reached by moving (one not reached before this round), or −1.
        /// </summary>
        public bool Step(int heading, out int flag)
        {
            flag = -1;
            if (!IsOpen(heading))
            {
                return false;
            }

            (int dx, int dy) = Offset(heading);
            Boat = (Boat.X + dx, Boat.Y + dy);
            for (int i = 0; i < _flags.Count; i++)
            {
                if (!_reached[i] && _flags[i] == Boat)
                {
                    _reached[i] = true;
                    flag = i;
                }
            }

            return true;
        }

        /// <summary>Starts a new round: every flag to reach again.</summary>
        public void ResetFlags()
        {
            for (int i = 0; i < _reached.Count; i++)
            {
                _reached[i] = false;
            }
        }

        /// <summary>The fewest steps from the boat to (x, y), as headings; null if the sea doesn't reach it.</summary>
        public List<int> PathTo(int x, int y)
        {
            if (!IsSea(x, y))
            {
                return null;
            }

            var from = new Dictionary<(int, int), ((int, int) Cell, int Heading)>();
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(Boat);
            from[Boat] = (Boat, -1);
            while (queue.Count > 0)
            {
                (int X, int Y) at = queue.Dequeue();
                if (at == (x, y))
                {
                    var path = new List<int>();
                    while (at != Boat)
                    {
                        ((int, int) cell, int heading) = from[at];
                        path.Insert(0, heading);
                        at = cell;
                    }

                    return path;
                }

                for (int heading = 0; heading < Headings; heading++)
                {
                    (int X, int Y) next = (at.X + _dx[heading], at.Y + _dy[heading]);
                    if (IsSea(next.X, next.Y) && !from.ContainsKey(next))
                    {
                        from[next] = (at, heading);
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
        }

        private bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    }
}
