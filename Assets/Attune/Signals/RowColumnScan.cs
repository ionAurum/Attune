using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// Press → Select by row-column scanning (ruling A29): the rows highlight one at a time and a
    /// press picks the highlighted row; then its items highlight one at a time and a press picks
    /// one. A row whose items go a whole round with no press gives way to the rows again, at that
    /// row. The rows' scanner carries the profile's timing (pausing, from the top); the items scan
    /// at the same speed. The Dwell Builder's switch play uses it.
    /// </summary>
    public sealed class RowColumnScan
    {
        private readonly List<int> _lengths = new();
        private int _row = -1;

        /// <summary>Scans rows of these lengths with the rows' scanner's timing, from the first row.</summary>
        public RowColumnScan(IEnumerable<int> rowLengths, Scanner rows)
        {
            _lengths.AddRange(rowLengths);
            Rows = rows;
            Items = new Scanner(rows.StepSeconds, 1, true);
            Rows.Reset(_lengths.Count);
        }

        /// <summary>The scanner stepping through the rows: its timing is the profile's.</summary>
        public Scanner Rows { get; }

        /// <summary>The scanner stepping through a picked row's items: one round, then back to the rows.</summary>
        public Scanner Items { get; }

        /// <summary>True once a row is picked and its items are scanned.</summary>
        public bool InRow { get; private set; }

        /// <summary>The highlighted row, or the picked one while its items are scanned; −1 with none.</summary>
        public int Row => InRow ? _row : Rows.Index;

        /// <summary>The highlighted item in the picked row, or −1 while the rows are scanned.</summary>
        public int Item => InRow ? Items.Index : -1;

        /// <summary>True when the rows have paused for lack of a press; the next press wakes them.</summary>
        public bool IsPaused => !InRow && Rows.IsPaused;

        /// <summary>Lets dt seconds pass; returns true if the highlight moved.</summary>
        public bool Advance(float dt)
        {
            if (!InRow)
            {
                return Rows.Advance(dt);
            }

            Items.StepSeconds = Rows.StepSeconds;
            bool moved = Items.Advance(dt);
            if (Items.IsPaused)
            {
                // A whole round of the row with no press: back to the rows, at this row.
                InRow = false;
                Rows.Reset(_lengths.Count, _row);
                return true;
            }

            return moved;
        }

        /// <summary>
        /// A press. Returns true with the row and item it picked; false when it picked a row (its
        /// items are scanned next), woke the paused rows, or there was nothing to pick.
        /// </summary>
        public bool Press(out int row, out int item)
        {
            row = -1;
            item = -1;
            if (_lengths.Count == 0)
            {
                return false;
            }

            if (!InRow)
            {
                int picked = Rows.Press();
                if (picked < 0 || _lengths[picked] <= 0)
                {
                    return false;
                }

                _row = picked;
                InRow = true;
                Items.Reset(_lengths[picked]);
                return false;
            }

            item = Items.Press();
            row = _row;
            InRow = false; // the rows go on from the top or from this row, as Rows.Press left them
            return item >= 0;
        }
    }
}
