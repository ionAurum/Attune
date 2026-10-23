using System;

namespace Attune.Signals
{
    /// <summary>
    /// Static: Steer ↔ Level (design, Adapters). A one-sided Level from a Steer, or a Steer centred
    /// on half the level.
    /// </summary>
    public static class SteerLevel
    {
        /// <summary>The Level from one side of a Steer: side +1 reads right, −1 left; the other side is 0.</summary>
        public static float ToLevel(float steer, int side = 1) => Math.Min(1f, Math.Max(0f, steer * Math.Sign(side == 0 ? 1 : side)));

        /// <summary>The Steer from a Level: half the level is centre, 0 is full left and 1 full right.</summary>
        public static float ToSteer(float level) => Math.Min(1f, Math.Max(-1f, level * 2f - 1f));
    }
}
