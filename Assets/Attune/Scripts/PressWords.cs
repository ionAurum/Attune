namespace Attune
{
    /// <summary>
    /// How the hints name a source's presses when it scans the menus: a switch's "Tap" and
    /// "Long hold", a breath's "Puff" and "Long breath", a hum's "Hum" and "Long hum".
    /// </summary>
    public sealed class PressWords
    {
        /// <summary>A switch's words.</summary>
        public static readonly PressWords Switch = new("Tap", "Long hold", "Let go", "Keep holding");

        /// <summary>A short press, as a hint starts with it: "Tap", "Puff".</summary>
        public readonly string Tap;

        /// <summary>A Long hold: "Long hold", "Long breath".</summary>
        public readonly string LongHold;

        /// <summary>Ending a press: "Let go", "Stop".</summary>
        public readonly string LetGo;

        /// <summary>Going on with a press: "Keep holding", "Keep going".</summary>
        public readonly string Keep;

        /// <summary>The words for one source.</summary>
        public PressWords(string tap, string longHold, string letGo, string keep)
        {
            Tap = tap;
            LongHold = longHold;
            LetGo = letGo;
            Keep = keep;
        }
    }
}
