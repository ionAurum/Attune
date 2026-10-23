namespace Attune.Signals
{
    /// <summary>
    /// Static: the Keyboard zones source's zones (design, Sources), from where a key sits on the
    /// board: x from 0 (the left edge of the main block) to 1 (its right edge; keys right of it are
    /// past 1), y from 0 (the function row) to 1 (the space bar's row). The halves give Steer, the
    /// quadrants the four gestures.
    /// </summary>
    public static class KeyboardZones
    {
        /// <summary>The four gestures, in scanning order.</summary>
        public static readonly string[] Quadrants = { TopLeft, TopRight, BottomLeft, BottomRight };

        /// <summary>The top-left quadrant's gesture.</summary>
        public const string TopLeft = "Top left";

        /// <summary>The top-right quadrant's gesture.</summary>
        public const string TopRight = "Top right";

        /// <summary>The bottom-left quadrant's gesture.</summary>
        public const string BottomLeft = "Bottom left";

        /// <summary>The bottom-right quadrant's gesture.</summary>
        public const string BottomRight = "Bottom right";

        /// <summary>Where the halves divide: between 6 and 7, T and Y, G and H, B and N on a standard board (B's middle is at 0.45, Y's at 0.467).</summary>
        public const float Split = 0.46f;

        /// <summary>Where the top and bottom divide: the Q row is top, the A row bottom.</summary>
        public const float Middle = 0.5f;

        /// <summary>A key's half: −1 left, +1 right.</summary>
        public static int Half(float x) => x < Split ? -1 : 1;

        /// <summary>A key's quadrant, as its gesture.</summary>
        public static string Quadrant(float x, float y)
        {
            bool top = y < Middle;
            if (Half(x) < 0)
            {
                return top ? TopLeft : BottomLeft;
            }

            return top ? TopRight : BottomRight;
        }
    }
}
