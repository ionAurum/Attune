using System;
using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// The Hum source (design, Sources): the microphone's pitch. Level runs from the player's lowest
    /// comfortable note (0) to their highest (1), measured in semitones so each step of the scale
    /// counts the same; any hum is also a press (ruling A34). Between notes it hears nothing to
    /// measure, so InputRouter lets Level glide down (ruling A34). A sound counts as a hum only if
    /// it has a clear pitch and is louder than the room, which calibration's Rest step measures and
    /// keeps as the Quiet axis. It listens while it is the active source, and its presses scan the
    /// menus (ruling A36).
    /// </summary>
    public sealed class HumSource : InputSource
    {
        /// <summary>The axis that holds the room's loudness, in dBFS, from calibration's Rest step.</summary>
        public const string QuietAxis = CalibrationAxis.Quiet;

        private static readonly CalibrationAxis[] _axes =
        {
            new("Level", "your highest comfortable note", "your lowest comfortable note")
            {
                IsRange = true,
                Ready = "First stay quiet for {0:0} seconds, so I can hear the room. Then hum your lowest comfortable note, " +
                        "then your highest: comfortable, not as far as you can.",
                Still = "Stay quiet",
                StillDetail = "Listening to the room.",
                LowLine = "Hum your lowest comfortable note",
                HighLine = "Now your highest comfortable note",
                ReachDetail = "Hold it.",
            },
        };

        private static readonly PressWords _words = new("Hum", "Long hum", "Stop", "Keep going");

        private readonly float[] _window = new float[4096];
        private readonly float[] _recent = new float[3]; // the last three pitches, for a median that drops a stray octave
        private PitchDetector _detector;
        private Profile _profile;
        private float _semitones, _sinceNote = float.PositiveInfinity, _pitch;
        private int _recentCount;

        /// <inheritdoc/>
        public override string Id => SourceIds.Hum;

        /// <inheritdoc/>
        public override string Name => "Hum";

        /// <inheritdoc/>
        public override string Description => "Hum at the microphone: higher is more, and any hum is a press. The menus scan: hum to choose, a long hum to go back.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Level | Channels.Press;

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
        public override AttuneTuning.Smoothing? Smoothing => AttuneTuning.Current.HumSmoothing;

        /// <inheritdoc/>
        public override AxisCalibration DefaultCalibration(string axis)
        {
            AttuneTuning tuning = AttuneTuning.Current;
            if (axis == QuietAxis)
            {
                return AxisCalibration.Make(axis, LoudnessDetector.Silence, LoudnessDetector.Silence, LoudnessDetector.Silence);
            }

            float low = PitchDetector.Semitones(tuning.HumLowHz);
            return AxisCalibration.Make(axis, low, low, PitchDetector.Semitones(tuning.HumHighHz));
        }

        /// <inheritdoc/>
        public override void Begin(Profile profile)
        {
            _profile = profile;
            _sinceNote = float.PositiveInfinity;
            _recentCount = 0;
        }

        /// <inheritdoc/>
        public override void End() => MicrophoneInput.Release();

        /// <summary>How loud a sound must be, in dBFS, to count as a hum: louder than the room by the tuning's margin.</summary>
        public float Gate()
        {
            AttuneTuning tuning = AttuneTuning.Current;
            AxisCalibration quiet = _profile?.Calibration(SourceIds.Hum, QuietAxis);
            float room = quiet != null ? quiet.Rest + tuning.HumAboveRoomDb : float.NegativeInfinity;
            return Mathf.Max(tuning.HumQuietestDb, room);
        }

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            AttuneTuning tuning = AttuneTuning.Current;
            MicrophoneInput.Pump(dt);
            float decibels = LoudnessDetector.Silence;
            float hz = 0f;
            if (MicrophoneInput.IsOn)
            {
                int rate = MicrophoneInput.SampleRate;
                if (_detector == null || _detector.SampleRate != rate || !Mathf.Approximately(_detector.MinHz, tuning.HumMinHz) ||
                    !Mathf.Approximately(_detector.MaxHz, tuning.HumMaxHz))
                {
                    _detector = new PitchDetector(rate, tuning.HumMinHz, tuning.HumMaxHz);
                }

                _detector.Threshold = tuning.HumThreshold;
                int needed = Math.Min(_detector.SamplesNeeded, _window.Length);
                int count = MicrophoneInput.Latest(_window, needed);
                decibels = LoudnessDetector.Decibels(_window, 0, count);
                if (count >= _detector.SamplesNeeded && decibels >= Gate())
                {
                    hz = _detector.Detect(_window, 0, count, out _);
                }
            }

            if (hz > 0f)
            {
                _sinceNote = 0f;
                _pitch = hz;
                _semitones = Median(PitchDetector.Semitones(hz));
            }
            else
            {
                _sinceNote += dt;
                if (_sinceNote > tuning.HumGapSeconds)
                {
                    _recentCount = 0;
                }
            }

            bool humming = _sinceNote <= tuning.HumGapSeconds;
            reading.Level = _semitones;
            reading.LevelSilent = !humming;
            reading.PressDown = humming;
            reading.Loudness = decibels;
            reading.Pitch = humming ? _pitch : 0f;
        }

        // The median of this pitch and the two before it in the same hum.
        private float Median(float semitones)
        {
            if (_recentCount < _recent.Length)
            {
                _recent[_recentCount++] = semitones;
            }
            else
            {
                _recent[0] = _recent[1];
                _recent[1] = _recent[2];
                _recent[2] = semitones;
            }

            if (_recentCount < 3)
            {
                return semitones;
            }

            float a = _recent[0], b = _recent[1], c = _recent[2];
            return Mathf.Max(Mathf.Min(a, b), Mathf.Min(Mathf.Max(a, b), c));
        }
    }
}
