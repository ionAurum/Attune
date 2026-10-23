using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// The targets a dwell can select, as rectangles on the screen (0 to 1 on each axis, y up), with
    /// a rest zone: a border RestBorder wide around the screen's edge where nothing is ever a target
    /// (design, Dwell Builder: the border is a rest zone).
    /// </summary>
    public sealed class DwellField
    {
        private readonly List<(int Id, float X, float Y, float Width, float Height)> _targets = new();

        /// <summary>How wide the rest zone along every edge is, as a share of the screen (0 for none).</summary>
        public float RestBorder { get; set; }

        /// <summary>Adds a target; a target added later is on top of one added earlier.</summary>
        public void Add(int id, float x, float y, float width, float height) => _targets.Add((id, x, y, width, height));

        /// <summary>Takes every target away.</summary>
        public void Clear() => _targets.Clear();

        /// <summary>True if the point (x, y) is in the rest zone or off the screen.</summary>
        public bool IsRest(float x, float y) =>
            x < RestBorder || y < RestBorder || x > 1f - RestBorder || y > 1f - RestBorder;

        /// <summary>The target under (x, y), or DwellTimer.None in the rest zone, off the screen or over nothing.</summary>
        public int TargetAt(float x, float y)
        {
            if (IsRest(x, y))
            {
                return DwellTimer.None;
            }

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                var target = _targets[i];
                if (x >= target.X && x <= target.X + target.Width && y >= target.Y && y <= target.Y + target.Height)
                {
                    return target.Id;
                }
            }

            return DwellTimer.None;
        }
    }
}
