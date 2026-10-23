using System.Collections.Generic;

namespace Attune.Signals
{
    /// <summary>
    /// Press → Gesture by scanning (ruling A18): the player's gestures highlight one at a time and a
    /// Tap sends the highlighted one, so one switch can play a Gesture demo.
    /// </summary>
    public sealed class GestureScan
    {
        private readonly List<string> _gestures = new();

        /// <summary>Scans gestures with scanner's timing.</summary>
        public GestureScan(IEnumerable<string> gestures, Scanner scanner)
        {
            _gestures.AddRange(gestures);
            Scanner = scanner;
            Scanner.Reset(_gestures.Count);
        }

        /// <summary>The scanner stepping through the gestures.</summary>
        public Scanner Scanner { get; }

        /// <summary>The gestures, in scanning order.</summary>
        public IReadOnlyList<string> Gestures => _gestures;

        /// <summary>The highlighted gesture, or null with none.</summary>
        public string Highlighted => Scanner.Index >= 0 && Scanner.Index < _gestures.Count ? _gestures[Scanner.Index] : null;

        /// <summary>Lets dt seconds pass; returns true if the highlight moved.</summary>
        public bool Advance(float dt) => Scanner.Advance(dt);

        /// <summary>A Tap: the gesture it sends, or null when the scan was paused (the Tap wakes it) or empty.</summary>
        public string Tap()
        {
            int picked = Scanner.Press();
            return picked >= 0 && picked < _gestures.Count ? _gestures[picked] : null;
        }
    }
}
