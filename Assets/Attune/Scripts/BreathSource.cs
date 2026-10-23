using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// The Breath source (design, Sources): the microphone's loudness. Breathing out at it raises
    /// Level, calibrated from the room (Rest) to a comfortable breath (Reach); a puff over the
    /// threshold is a press (Level → Press). Its raw Level is the loudness in dBFS. With the
    /// profile's Noisy room on, it ignores anything under three times the room's loudness (ruling
    /// A35). It listens while it is the active source, and its presses scan the menus (ruling A36).
    /// </summary>
    public sealed class BreathSource : InputSource
    {
        private static readonly CalibrationAxis[] _axes =
        {
            new("Level", "breathe out")
            {
                Ready = "First stay quiet for {0:0} seconds, so I can hear the room. Then breathe out at the microphone, " +
                        "as hard as is comfortable: not as hard as you can.",
                Still = "Stay quiet",
                StillDetail = "Listening to the room.",
                HighLine = "Breathe out, as hard as is comfortable",
                ReachDetail = "Keep it going.",
                Missed = "I didn't hear a breath over the room, so your breath is as it was. ",
            },
        };

        private static readonly PressWords _words = new("Puff", "Long breath", "Stop", "Keep going");

        private readonly float[] _window = new float[8192];
        private readonly AxisCalibration _raised = new();

        /// <inheritdoc/>
        public override string Id => SourceIds.Breath;

        /// <inheritdoc/>
        public override string Name => "Breath";

        /// <inheritdoc/>
        public override string Description => "Breathe out at the microphone: how hard is the level, and a puff is a press. The menus scan: puff to choose, a long breath to go back.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Level;

        /// <inheritdoc/>
        public override bool IsAvailable => MicrophoneInput.HasDevice;

        /// <inheritdoc/>
        public override bool UsesMicrophone => true;

        /// <inheritdoc/>
        public override bool ScansMenus => true;

        /// <inheritdoc/>
        public override PressWords Words => _words;

        /// <inheritdoc/>
        public override IReadOnlyList<CalibrationAxis> Axes => _axes;

        /// <inheritdoc/>
        public override AttuneTuning.Smoothing? Smoothing => AttuneTuning.Current.BreathSmoothing;

        /// <inheritdoc/>
        public override AxisCalibration DefaultCalibration(string axis)
        {
            AttuneTuning tuning = AttuneTuning.Current;
            return AxisCalibration.Make(axis, tuning.BreathQuietDb, tuning.BreathQuietDb, tuning.BreathFullDb);
        }

        /// <inheritdoc/>
        public override AxisCalibration InPlay(AxisCalibration axis, Profile profile) =>
            profile != null && profile.NoisyRoom ? axis.RaisedRest(LoudnessDetector.Times(AttuneTuning.Current.NoisyRoomTimes), _raised) : axis;

        /// <inheritdoc/>
        public override void End() => MicrophoneInput.Release();

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            MicrophoneInput.Pump(dt);
            float decibels = LoudnessDetector.Silence;
            if (MicrophoneInput.IsOn)
            {
                int want = Mathf.Clamp(Mathf.RoundToInt(MicrophoneInput.SampleRate * AttuneTuning.Current.BreathWindowSeconds), 64, _window.Length);
                int count = MicrophoneInput.Latest(_window, want);
                decibels = LoudnessDetector.Decibels(_window, 0, count);
            }

            reading.Level = decibels;
            reading.Loudness = decibels;
            reading.Pitch = 0f;
        }
    }
}
