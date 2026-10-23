using System.Collections.Generic;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// What a source read from its device this frame, in the device's own units and before
    /// calibration. InputRouter hands one to the active source each frame and makes channels of it.
    /// </summary>
    public sealed class SourceReading
    {
        /// <summary>The Level axis, raw.</summary>
        public float Level;

        /// <summary>The Steer axis, raw.</summary>
        public float Steer;

        /// <summary>Point, 0 to 1 on each axis, y up.</summary>
        public Vector2 Point = new(0.5f, 0.5f);

        /// <summary>True while the press is held.</summary>
        public bool PressDown;

        /// <summary>The gestures made this frame.</summary>
        public readonly List<string> Gestures = new();

        /// <summary>
        /// True if the source hears nothing to measure Level by this frame (the Hum source between
        /// notes): InputRouter lets Level glide down instead (ruling A34).
        /// </summary>
        public bool LevelSilent;

        /// <summary>A microphone source's loudness, in dBFS, for calibration and the overlay.</summary>
        public float Loudness = Signals.LoudnessDetector.Silence;

        /// <summary>A microphone source's pitch, in hertz; 0 for none.</summary>
        public float Pitch;

        /// <summary>Clears what lasts only a frame.</summary>
        public void BeginFrame()
        {
            Gestures.Clear();
            LevelSilent = false;
        }
    }
}
