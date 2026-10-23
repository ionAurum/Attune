using System;

namespace Attune.Signals
{
    /// <summary>
    /// What a demo can ask for and a source can supply (design, The input layer, Channels). Flags,
    /// so a set of them is one value.
    /// </summary>
    [Flags]
    public enum Channels
    {
        /// <summary>No channel.</summary>
        None = 0,

        /// <summary>0 to 1, rest is 0: breath strength, hum pitch, a lean one way.</summary>
        Level = 1 << 0,

        /// <summary>−1 to 1, rest is 0: head tilt, a keyboard half, mouse x around its start.</summary>
        Steer = 1 << 1,

        /// <summary>A position on the screen, 0 to 1 on each axis.</summary>
        Point = 1 << 2,

        /// <summary>Down and up, classified as Tap, Hold or Long hold by how long it was held.</summary>
        Press = 1 << 3,

        /// <summary>A named event from the player's own set: a keyboard zone, an expression.</summary>
        Gesture = 1 << 4,

        /// <summary>One "do it" event: a Tap, a finished dwell, a scan pick.</summary>
        Select = 1 << 5,
    }
}
