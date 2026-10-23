namespace Attune
{
    /// <summary>
    /// One axis a source calibrates (design, Calibration): its name, the words for each way it
    /// reaches, and, for a source that isn't moved (breath, a hum), the lines that say what to do
    /// instead of moving.
    /// </summary>
    public sealed class CalibrationAxis
    {
        /// <summary>The axis a range axis's Rest step is kept as: the room's loudness, in dBFS.</summary>
        public const string Quiet = "Quiet";

        /// <summary>An axis; lowWord null for a one-sided axis, which reaches only upward.</summary>
        public CalibrationAxis(string axis, string highWord, string lowWord = null)
        {
            Axis = axis;
            HighWord = highWord;
            LowWord = lowWord;
        }

        /// <summary>The axis: "Steer", "Level".</summary>
        public string Axis { get; }

        /// <summary>The way above rest, in the instruction: "right", "louder".</summary>
        public string HighWord { get; }

        /// <summary>The way below rest, or null for a one-sided axis.</summary>
        public string LowWord { get; }

        /// <summary>True if it reaches both ways from rest.</summary>
        public bool IsTwoSided => LowWord != null;

        /// <summary>
        /// True if the two reaches are the ends of its range, and rest is only the room (the Hum
        /// source: its lowest comfortable note is 0, its highest 1, and the Rest step measures the
        /// room's loudness, kept as the source's Quiet axis).
        /// </summary>
        public bool IsRange { get; set; }

        /// <summary>What the Ready step says, with {0} for the Rest step's seconds; null for the moving kind.</summary>
        public string Ready { get; set; }

        /// <summary>What the Rest step asks: "Stay quiet"; null for "Keep still".</summary>
        public string Still { get; set; }

        /// <summary>What the Rest step says it is doing: "Listening to the room."; null for the moving kind.</summary>
        public string StillDetail { get; set; }

        /// <summary>What the low Reach step asks: "Hum your lowest comfortable note"; null for the moving kind.</summary>
        public string LowLine { get; set; }

        /// <summary>What the high Reach step asks: "Breathe out, as hard as is comfortable"; null for the moving kind.</summary>
        public string HighLine { get; set; }

        /// <summary>What a Reach step says while it measures: "Keep it going."; null for "Hold it there.".</summary>
        public string ReachDetail { get; set; }

        /// <summary>What Done says if a Reach step saw nothing: "I didn't hear a breath, so it is as it was. "; null for the moving kind.</summary>
        public string Missed { get; set; }
    }
}
