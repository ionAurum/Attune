using System;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Attune's tuned values (design, Building in code): one block per source, calibration, the
    /// overlay, then one block per demo. Jon tunes them in the Inspector, on Resources/AttuneTuning.asset, which
    /// SceneMaker makes if there is none and never overwrites. Without the asset the code's defaults
    /// apply.
    /// </summary>
    [CreateAssetMenu(menuName = "Attune/Tuning", fileName = "AttuneTuning")]
    public class AttuneTuning : ScriptableObject
    {
        /// <summary>A One Euro filter's settings (design, Calibration, Smooth).</summary>
        [Serializable]
        public struct Smoothing
        {
            [Tooltip("The smoothing at rest, in hertz: lower is smoother, and slower to follow.")]
            [SerializeField] private float _minCutoff;
            [Tooltip("How fast the smoothing lets go as the input speeds up: higher is less lag.")]
            [SerializeField] private float _beta;

            /// <summary>Settings with these values.</summary>
            public Smoothing(float minCutoff, float beta)
            {
                _minCutoff = minCutoff;
                _beta = beta;
            }

            /// <summary>The cutoff at rest, in hertz.</summary>
            public float MinCutoff => _minCutoff;

            /// <summary>How much the cutoff rises with speed.</summary>
            public float Beta => _beta;
        }

        private const string ResourceName = "AttuneTuning";

        private static AttuneTuning _current;

        [Header("Pointer")]
        [Tooltip("Before calibration: how far the pointer moves from where it rests for full Steer, as a share of the screen's width.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _pointerReach = 0.2f;
        [SerializeField] private Smoothing _pointerSmoothing = new(1.5f, 2f);

        [Header("Gamepad")]
        [Tooltip("How fast the stick moves Point at full tilt, in screens per second.")]
        [SerializeField, Range(0.1f, 3f)] private float _gamepadPointSpeed = 0.7f;
        [SerializeField] private Smoothing _gamepadSmoothing = new(2f, 0.5f);

        [Header("Keyboard zones")]
        [Tooltip("How long a keyboard half takes to swing Steer from centre to full, in seconds; 0 jumps straight there.")]
        [SerializeField, Range(0f, 1f)] private float _keyboardSteerSeconds = 0.15f;

        [Header("Microphone (Breath and Hum)")]
        [Tooltip("The samples a second asked of the microphone; it may give another rate.")]
        [SerializeField, Range(16000, 48000)] private int _microphoneRate = 48000;

        [Header("Breath")]
        [Tooltip("Before calibration: the room's loudness, in dBFS (0 is the loudest a microphone gives).")]
        [SerializeField, Range(-90f, -20f)] private float _breathQuietDb = -60f;
        [Tooltip("Before calibration: a comfortable breath at the microphone, in dBFS: full Level.")]
        [SerializeField, Range(-60f, 0f)] private float _breathFullDb = -25f;
        [Tooltip("How long a stretch of sound each loudness is measured over, in seconds.")]
        [SerializeField, Range(0.01f, 0.2f)] private float _breathWindowSeconds = 0.05f;
        [Tooltip("Noisy room (ruling A35): Breath ignores anything under this many times the room's loudness.")]
        [SerializeField, Range(1.5f, 6f)] private float _noisyRoomTimes = 3f;
        [SerializeField] private Smoothing _breathSmoothing = new(3f, 1f);

        [Header("Hum")]
        [Tooltip("Before calibration: the lowest comfortable note, in hertz: Level 0.")]
        [SerializeField, Range(60f, 400f)] private float _humLowHz = 110f;
        [Tooltip("Before calibration: the highest comfortable note, in hertz: Level 1.")]
        [SerializeField, Range(100f, 1000f)] private float _humHighHz = 330f;
        [Tooltip("The lowest pitch heard at all, in hertz.")]
        [SerializeField, Range(50f, 120f)] private float _humMinHz = 70f;
        [Tooltip("The highest pitch heard at all, in hertz.")]
        [SerializeField, Range(400f, 1500f)] private float _humMaxHz = 1000f;
        [Tooltip("How unlike itself a sound may be at its period and still be a note, 0 to 1: lower is stricter (YIN's threshold).")]
        [SerializeField, Range(0.05f, 0.4f)] private float _humThreshold = 0.15f;
        [Tooltip("A hum must be this many dB louder than the room (measured at calibration) to count.")]
        [SerializeField, Range(0f, 30f)] private float _humAboveRoomDb = 8f;
        [Tooltip("And never quieter than this, in dBFS, calibrated or not.")]
        [SerializeField, Range(-90f, -20f)] private float _humQuietestDb = -55f;
        [Tooltip("A gap in a hum this short, in seconds, doesn't count as stopping.")]
        [SerializeField, Range(0f, 0.5f)] private float _humGapSeconds = 0.15f;
        [Tooltip("Calibration: the lowest and highest notes must be at least this many semitones apart.")]
        [SerializeField, Range(1f, 12f)] private float _humMinSpan = 2f;
        [SerializeField] private Smoothing _humSmoothing = new(1.5f, 0.3f);

        [Header("Calibration")]
        [Tooltip("How long the Rest step lasts, in seconds (design: 2).")]
        [SerializeField, Range(1f, 5f)] private float _restSeconds = 2f;
        [Tooltip("How long each Reach step lasts, in seconds (design: 3).")]
        [SerializeField, Range(1f, 6f)] private float _reachSeconds = 3f;
        [Tooltip("A Reach step that moves less than this, in the source's units, saw nothing and keeps the axis as it was.")]
        [SerializeField, Range(0f, 0.2f)] private float _minReach = 0.01f;

        [Header("Overlay")]
        [Tooltip("How wide the What it hears overlay is, in reference pixels.")]
        [SerializeField, Range(360f, 800f)] private float _overlayWidth = 520f;

        [Header("Compass Sweep")]
        [Tooltip("How long the boat takes to sail one cell, in seconds; a press while it sails sends it on at once.")]
        [SerializeField, Range(0f, 1f)] private float _boatSailSeconds = 0.3f;
        [Tooltip("How long 'Every flag reached' shows before the flags come back, in seconds.")]
        [SerializeField, Range(1f, 10f)] private float _flagsBackSeconds = 4f;

        [Header("Keyboard Garden")]
        [Tooltip("How many plants each bed holds; planting in a full bed replaces its oldest.")]
        [SerializeField, Range(1, 8)] private int _plantsPerBed = 5;

        [Header("Dwell Builder")]
        [Tooltip("How wide the rest zone along every edge of the screen is, as a share of the screen; nothing there is ever picked.")]
        [SerializeField, Range(0.02f, 0.15f)] private float _restBorder = 0.07f;
        [Tooltip("How big the pointer's dwell ring is, in reference pixels.")]
        [SerializeField, Range(40f, 160f)] private float _dwellRingSize = 84f;

        [Header("Kite Breath and Hum Flyer")]
        [Tooltip("How long a breath or a hum must last to open the menu in these two demos, in seconds; never less than the profile's Long hold (ruling A37).")]
        [SerializeField, Range(2f, 15f)] private float _driftMenuHoldSeconds = 5f;

        [Header("Kite Breath")]
        [Tooltip("How long the kite takes to rise from the ground to the top at full Level, in seconds.")]
        [SerializeField, Range(0.1f, 3f)] private float _kiteRiseSeconds = 0.6f;
        [Tooltip("How long the kite takes to fall from the top to the ground once the Level drops, in seconds: it falls gently.")]
        [SerializeField, Range(0.5f, 10f)] private float _kiteFallSeconds = 3f;
        [Tooltip("How fast the ribbons drift past, in screen widths a second.")]
        [SerializeField, Range(0.02f, 0.3f)] private float _kiteDriftSpeed = 0.07f;
        [Tooltip("How near the kite's height must be to a ribbon's as it passes to touch it, as a share of the sky's height.")]
        [SerializeField, Range(0.03f, 0.2f)] private float _ribbonReach = 0.09f;

        [Header("Hum Flyer")]
        [Tooltip("How long the flyer takes to climb or dive the whole sky, in seconds.")]
        [SerializeField, Range(0.1f, 3f)] private float _flyerFollowSeconds = 0.5f;
        [Tooltip("How fast the rings drift past, in screen widths a second.")]
        [SerializeField, Range(0.02f, 0.3f)] private float _flyerDriftSpeed = 0.07f;
        [Tooltip("How near the flyer's height must be to a ring's middle as it passes to fly through, as a share of the sky's height.")]
        [SerializeField, Range(0.03f, 0.2f)] private float _ringReach = 0.08f;

        /// <summary>The tuning in Resources, or the code's defaults if there is none.</summary>
        public static AttuneTuning Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<AttuneTuning>(ResourceName);
                    if (_current == null)
                    {
                        Debug.LogWarning($"No {ResourceName} asset in a Resources folder: the code's defaults apply (Attune > Make Scenes makes it).");
                        _current = CreateInstance<AttuneTuning>();
                    }
                }

                return _current;
            }
        }

        /// <summary>The pointer's reach for full Steer before calibration, as a share of the screen's width.</summary>
        public float PointerReach => _pointerReach;

        /// <summary>The pointer's smoothing.</summary>
        public Smoothing PointerSmoothing => _pointerSmoothing;

        /// <summary>How fast the stick moves Point at full tilt, in screens per second.</summary>
        public float GamepadPointSpeed => _gamepadPointSpeed;

        /// <summary>The gamepad stick's smoothing.</summary>
        public Smoothing GamepadSmoothing => _gamepadSmoothing;

        /// <summary>How long a keyboard half takes to swing Steer to full.</summary>
        public float KeyboardSteerSeconds => _keyboardSteerSeconds;

        /// <summary>The samples a second asked of the microphone.</summary>
        public int MicrophoneRate => _microphoneRate;

        /// <summary>The room's loudness before calibration, in dBFS.</summary>
        public float BreathQuietDb => _breathQuietDb;

        /// <summary>A comfortable breath before calibration, in dBFS.</summary>
        public float BreathFullDb => _breathFullDb;

        /// <summary>How long a stretch each loudness is measured over.</summary>
        public float BreathWindowSeconds => _breathWindowSeconds;

        /// <summary>Noisy room: how many times the room's loudness Breath ignores (ruling A35).</summary>
        public float NoisyRoomTimes => _noisyRoomTimes;

        /// <summary>The Breath source's smoothing.</summary>
        public Smoothing BreathSmoothing => _breathSmoothing;

        /// <summary>The lowest comfortable note before calibration, in hertz.</summary>
        public float HumLowHz => _humLowHz;

        /// <summary>The highest comfortable note before calibration, in hertz.</summary>
        public float HumHighHz => _humHighHz;

        /// <summary>The lowest pitch heard at all.</summary>
        public float HumMinHz => _humMinHz;

        /// <summary>The highest pitch heard at all.</summary>
        public float HumMaxHz => _humMaxHz;

        /// <summary>The pitch detector's threshold.</summary>
        public float HumThreshold => _humThreshold;

        /// <summary>How much louder than the room a hum must be, in dB.</summary>
        public float HumAboveRoomDb => _humAboveRoomDb;

        /// <summary>The quietest a hum may be, in dBFS.</summary>
        public float HumQuietestDb => _humQuietestDb;

        /// <summary>A gap in a hum this short doesn't count as stopping.</summary>
        public float HumGapSeconds => _humGapSeconds;

        /// <summary>How far apart, in semitones, calibration's lowest and highest notes must be.</summary>
        public float HumMinSpan => _humMinSpan;

        /// <summary>The Hum source's smoothing.</summary>
        public Smoothing HumSmoothing => _humSmoothing;

        /// <summary>How long the Rest step lasts.</summary>
        public float RestSeconds => _restSeconds;

        /// <summary>How long each Reach step lasts.</summary>
        public float ReachSeconds => _reachSeconds;

        /// <summary>The least a Reach step must move to count.</summary>
        public float MinReach => _minReach;

        /// <summary>How wide the overlay is.</summary>
        public float OverlayWidth => _overlayWidth;

        /// <summary>How long the Compass Sweep's boat takes to sail one cell.</summary>
        public float BoatSailSeconds => _boatSailSeconds;

        /// <summary>How long the Compass Sweep says every flag was reached before they come back.</summary>
        public float FlagsBackSeconds => _flagsBackSeconds;

        /// <summary>How many plants each of the Keyboard Garden's beds holds.</summary>
        public int PlantsPerBed => _plantsPerBed;

        /// <summary>The Dwell Builder's rest zone along every edge, as a share of the screen.</summary>
        public float RestBorder => _restBorder;

        /// <summary>The Dwell Builder's dwell ring's size.</summary>
        public float DwellRingSize => _dwellRingSize;

        /// <summary>How long a breath or a hum must last to open the menu in the Kite Breath and the Hum Flyer (ruling A37).</summary>
        public float DriftMenuHoldSeconds => _driftMenuHoldSeconds;

        /// <summary>How long the kite takes to rise the whole sky.</summary>
        public float KiteRiseSeconds => _kiteRiseSeconds;

        /// <summary>How long the kite takes to fall the whole sky.</summary>
        public float KiteFallSeconds => _kiteFallSeconds;

        /// <summary>How fast the Kite Breath's ribbons drift past, in screen widths a second.</summary>
        public float KiteDriftSpeed => _kiteDriftSpeed;

        /// <summary>How near the kite must pass a ribbon to touch it.</summary>
        public float RibbonReach => _ribbonReach;

        /// <summary>How long the Hum Flyer's flyer takes to cross the whole sky.</summary>
        public float FlyerFollowSeconds => _flyerFollowSeconds;

        /// <summary>How fast the Hum Flyer's rings drift past, in screen widths a second.</summary>
        public float FlyerDriftSpeed => _flyerDriftSpeed;

        /// <summary>How near the flyer must pass a ring's middle to fly through it.</summary>
        public float RingReach => _ringReach;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _current = null;
    }
}
