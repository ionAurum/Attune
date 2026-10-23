using System;
using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Attune
{
    /// <summary>
    /// The input layer's one object (design, Sources): holds the profile and the active source, and
    /// each frame reads the source, calibrates and smooths what it read, runs the adapters for the
    /// channels it lacks, and publishes every channel. Demos read channels from here and never a
    /// device. It is the only object that knows which source is active. Made with the menus, so it
    /// lasts the session. The presenter's Next source (F2, the gamepad's Select) changes the source
    /// mid-demo without a reload (design, Live swap). It publishes whether or not a menu is open;
    /// what reads it checks MenuFocus.IsAnyOpen.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class InputRouter : MonoBehaviour
    {
        private readonly List<InputSource> _sources = new();
        private readonly SourceReading _reading = new();
        private readonly PressTracker _press = new();
        private readonly LevelToPress _levelToPress = new();
        private readonly PressToLevel _pressToLevel = new();
        private readonly LevelGlide _glide = new();
        private readonly OneEuroFilter _levelFilter = new(), _steerFilter = new(), _pointXFilter = new(), _pointYFilter = new();

        private InputActionMap _presenter;
        private IReadOnlyList<string> _wantedGestures; // a demo's gesture set, while it wants Gesture
        private double _pressStartedAt;
        private bool _pressBeganInMenu;

        /// <summary>The session's router, or null before the game has started.</summary>
        public static InputRouter Instance { get; private set; }

        /// <summary>Every source, in the order the source page and Next source go through them.</summary>
        public IReadOnlyList<InputSource> Sources => _sources;

        /// <summary>The active source.</summary>
        public InputSource Source { get; private set; }

        /// <summary>The profile playing. Unsaved (Id 0) until the player makes one.</summary>
        public Profile Profile { get; private set; }

        /// <summary>Where profiles are kept.</summary>
        public ProfileStore Store { get; private set; }

        /// <summary>The profile's timing values.</summary>
        public TimingValues Timing => Profile.Timing;

        /// <summary>The channels the active source supplies itself.</summary>
        public Channels Supplied => Source != null ? Source.Supplies : Channels.None;

        /// <summary>Level, 0 to 1, calibrated and smoothed, or from an adapter.</summary>
        public float Level { get; private set; }

        /// <summary>Steer, −1 to 1, calibrated and smoothed, or from an adapter.</summary>
        public float Steer { get; private set; }

        /// <summary>Point, 0 to 1 on each axis, y up; the screen's middle for a source without it.</summary>
        public Vector2 Point { get; private set; } = new(0.5f, 0.5f);

        /// <summary>True while the press is held.</summary>
        public bool IsPressDown => _press.IsDown;

        /// <summary>How long the press has been held; 0 when it isn't.</summary>
        public float PressHeldSeconds => _press.HeldSeconds(Now);

        /// <summary>What letting go now would make of the press, or null when it isn't held.</summary>
        public PressKind? PressKindIfReleased => _press.KindIfReleased(Now, Timing);

        /// <summary>When the press last started, on the unscaled clock.</summary>
        public double PressStartedAt => _pressStartedAt;

        /// <summary>
        /// True if the press held now, or the one that ended last, began while a menu was open: the
        /// menu's, even when it closed the menu before it ended. A demo leaves it alone.
        /// </summary>
        public bool PressBeganInMenu => _pressBeganInMenu;

        /// <summary>Presses since the count was last reset (a demo's start).</summary>
        public int PressCount => _press.Count;

        /// <summary>The last press's kind, or null before the first.</summary>
        public PressKind? LastPress { get; private set; }

        /// <summary>How long the last press that ended was held, in seconds.</summary>
        public float LastPressSeconds { get; private set; }

        /// <summary>The last gesture, or null.</summary>
        public string LastGesture { get; private set; }

        /// <summary>Selects since the count was last reset.</summary>
        public int SelectCount { get; private set; }

        /// <summary>The raw values the source read this frame, before calibration (for the overlay and calibration).</summary>
        public SourceReading Raw => _reading;

        /// <summary>Point → Select by dwell, while a demo dwells (the Dwell Builder), for the overlay to show; else null.</summary>
        public DwellTimer Dwell { get; set; }

        /// <summary>Press → Gesture by scanning, while a demo wants Gesture from a source without it (ruling A18); else null.</summary>
        public GestureScan GestureScan { get; private set; }

        /// <summary>A press ended, as Tap, Hold or Long hold.</summary>
        public event Action<PressKind> Pressed;

        /// <summary>A press started.</summary>
        public event Action PressStarted;

        /// <summary>A gesture was made: by the source, or picked by scanning.</summary>
        public event Action<string> Gestured;

        /// <summary>A Select: a Tap, when no gesture scan takes it.</summary>
        public event Action Selected;

        /// <summary>The active source changed.</summary>
        public event Action SourceChanged;

        /// <summary>A different profile is playing.</summary>
        public event Action ProfileChanged;

        private static double Now => Time.unscaledTimeAsDouble;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake()
        {
            Instance = this;
            _sources.Add(new SwitchSource());
            _sources.Add(new KeyboardZonesSource());
            _sources.Add(new PointerSource());
            _sources.Add(new GamepadSource());
            _sources.Add(new BreathSource());
            _sources.Add(new HumSource());
            _sources.Add(new ScriptedSource());

            Store = ProfileStore.OnSettings();
            Profile = Store.Load(Store.CurrentId);
            if (Profile == null)
            {
                List<Profile> all = Store.All();
                Profile = all.Count > 0 ? all[0] : new Profile { Name = "Guest" };
            }

            SetSource(Profile.Source, save: false);

            _presenter = new InputActionMap("Presenter");
            InputAction next = _presenter.AddAction("Next source", InputActionType.Button);
            next.AddBinding("<Keyboard>/f2");
            next.AddBinding("<Gamepad>/select");
            next.performed += _ => NextSource();
        }

        private void OnEnable() => _presenter?.Enable();

        private void OnDisable() => _presenter?.Disable();

        private void OnDestroy()
        {
            Source?.End();
            _presenter?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        //----------

        /// <summary>A source by Id, or null.</summary>
        public InputSource Find(string id) => _sources.Find(source => source.Id == id);

        /// <summary>A source by type.</summary>
        public T Find<T>() where T : InputSource => _sources.Find(source => source is T) as T;

        /// <summary>True if the source page and Next source offer this source: Scripted only in a self-test.</summary>
        public static bool IsOffered(InputSource source) => source.Id != SourceIds.Scripted || SelfTest.IsRunning;

        /// <summary>Plays as profile: its source and calibration take over at once (design, Profiles).</summary>
        public void Use(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            Profile = profile;
            if (profile.Id > 0)
            {
                Store.CurrentId = profile.Id;
            }

            SetSource(profile.Source, save: false);
            ProfileChanged?.Invoke();
        }

        /// <summary>Saves the profile, if it is one the player made.</summary>
        public void SaveProfile()
        {
            Profile.Timing.Clamp();
            if (Profile.Id > 0)
            {
                Store.Save(Profile);
            }
        }

        /// <summary>
        /// Makes source id the active one, with this profile's calibration of it, without a reload; the
        /// profile keeps it. An unknown Id falls back to Keyboard zones.
        /// </summary>
        public void SetSource(string id, bool save = true)
        {
            InputSource source = Find(id) ?? Find(SourceIds.Keyboard);
            Source?.End();
            _press.Cancel();
            _levelToPress.Reset();
            _pressToLevel.Reset();
            _glide.Reset();
            ResetFilters();
            Source = source;
            Source.Begin(Profile);
            Profile.Source = source.Id;
            UpdateGestureScan();
            if (save)
            {
                SaveProfile();
            }

            SourceChanged?.Invoke();
        }

        /// <summary>
        /// The presenter's Next source (design, Live swap): the next source after this one whose device
        /// is there and which can play the demo under way, if any.
        /// </summary>
        public void NextSource()
        {
            InputSource next = NextSourceFor(DemoSession.Current?.Needs);
            if (next != null && next != Source)
            {
                SetSource(next.Id);
            }
        }

        /// <summary>The source Next source would change to, or null if no other can.</summary>
        public InputSource NextSourceFor(DemoNeeds needs)
        {
            int start = Math.Max(0, _sources.IndexOf(Source));
            for (int step = 1; step < _sources.Count; step++)
            {
                InputSource candidate = _sources[(start + step) % _sources.Count];
                if (IsOffered(candidate) && candidate.IsAvailable && (needs == null || needs.CanBeMetBy(candidate.Supplies)))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>The active source's calibration of an axis: the player's, or the source's default.</summary>
        public AxisCalibration CalibrationOf(string axis) => Profile.Calibration(Source.Id, axis) ?? Source.DefaultCalibration(axis);

        /// <summary>Keeps a new calibration of an axis of the active source, and saves the profile.</summary>
        public void Calibrated(AxisCalibration axis)
        {
            Profile.SetCalibration(Source.Id, axis);
            ResetFilters();
            SaveProfile();
        }

        /// <summary>A demo made a Select of its own: a finished dwell, or a pick by scanning in the world.</summary>
        public void CountSelect() => SelectCount++;

        /// <summary>Starts counting presses and selects again, as a demo starts.</summary>
        public void ResetCounts()
        {
            _press.ResetCount();
            SelectCount = 0;
            LastPress = null;
            LastGesture = null;
        }

        /// <summary>
        /// A demo wants Gesture, from this set: a source without Gesture gets Press → Gesture by
        /// scanning (ruling A18). Null when it no longer wants it.
        /// </summary>
        public void WantGestures(IReadOnlyList<string> gestures)
        {
            _wantedGestures = gestures;
            UpdateGestureScan();
        }

        //----------

        private void Update()
        {
            if (Source == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            TimingValues timing = Timing;
            Channels supplied = Source.Supplies;
            _reading.BeginFrame();
            Source.Read(_reading, dt);
            ApplySmoothing();

            // The axes the source supplies, calibrated, then smoothed; a source in channel units as they are.
            float level = 0f;
            float steer = 0f;
            bool raw = Source.PassesThrough;
            if (ChannelRoutes.Has(supplied, Channels.Level))
            {
                if (raw)
                {
                    level = Mathf.Clamp01(_reading.Level);
                }
                else if (_reading.LevelSilent)
                {
                    // Nothing to measure (the Hum source between notes): the level glides down (ruling A34).
                    level = _glide.Update(true, 0f, dt, timing.FallSeconds);
                }
                else
                {
                    if (_glide.IsGliding)
                    {
                        _levelFilter.Reset(); // a new note starts the smoothing afresh
                    }

                    float normal = Source.InPlay(CalibrationOf("Level"), Profile).NormaliseLevel(_reading.Level, timing.DeadZone);
                    level = _glide.Update(false, Smooth(_levelFilter, normal, dt), dt, timing.FallSeconds);
                }
            }

            if (ChannelRoutes.Has(supplied, Channels.Steer))
            {
                steer = raw ? Mathf.Clamp(_reading.Steer, -1f, 1f)
                    : Smooth(_steerFilter, CalibrationOf("Steer").NormaliseSteer(_reading.Steer, timing.DeadZone), dt);
            }

            if (ChannelRoutes.Has(supplied, Channels.Point))
            {
                Point = new Vector2(Smooth(_pointXFilter, _reading.Point.x, dt), Smooth(_pointYFilter, _reading.Point.y, dt));
            }
            else
            {
                Point = new Vector2(0.5f, 0.5f);
            }

            // Press and Level make each other: whichever the source supplies drives the other.
            bool down;
            if (ChannelRoutes.Has(supplied, Channels.Press))
            {
                down = _reading.PressDown;
                if (!ChannelRoutes.Has(supplied, Channels.Level))
                {
                    _pressToLevel.RiseSeconds = timing.RiseSeconds;
                    _pressToLevel.FallSeconds = timing.FallSeconds;
                    level = _pressToLevel.Update(down, dt);
                }
            }
            else
            {
                if (!ChannelRoutes.Has(supplied, Channels.Level) && ChannelRoutes.Has(supplied, Channels.Steer))
                {
                    level = SteerLevel.ToLevel(steer);
                }

                _levelToPress.OnLevel = timing.PressOnLevel;
                _levelToPress.OffLevel = timing.PressOffLevel;
                _levelToPress.Update(level);
                down = _levelToPress.IsDown;
            }

            if (!ChannelRoutes.Has(supplied, Channels.Steer))
            {
                steer = SteerLevel.ToSteer(level);
            }

            Level = level;
            Steer = steer;

            if (down && _press.Down(Now))
            {
                _pressStartedAt = Now;
                _pressBeganInMenu = MenuFocus.IsAnyOpen;
                PressStarted?.Invoke();
            }
            else if (!down && _press.IsDown)
            {
                LastPressSeconds = (float)(Now - _pressStartedAt);
                PressKind kind = _press.Up(Now, timing) ?? PressKind.Tap;
                LastPress = kind;
                Pressed?.Invoke(kind);
                if (kind == PressKind.Tap)
                {
                    OnTap();
                }
            }

            foreach (string gesture in _reading.Gestures)
            {
                MakeGesture(gesture);
            }

            if (GestureScan != null && !MenuFocus.IsAnyOpen)
            {
                GestureScan.Scanner.StepSeconds = timing.ScanStepSeconds;
                GestureScan.Scanner.Rounds = timing.ScanRounds;
                GestureScan.Scanner.FromTop = timing.ScanFromTop;
                GestureScan.Advance(dt);
            }
        }

        private void OnTap()
        {
            // While gestures are scanned, a Tap in play is the gesture; in a menu it is the menu's,
            // even when it closed the menu (Resume) before it got here.
            if (GestureScan != null && !MenuFocus.IsAnyOpen)
            {
                if (_pressBeganInMenu)
                {
                    return;
                }

                string gesture = GestureScan.Tap();
                if (gesture != null)
                {
                    MakeGesture(gesture);
                }

                return;
            }

            SelectCount++;
            Selected?.Invoke();
        }

        private void MakeGesture(string gesture)
        {
            LastGesture = gesture;
            Gestured?.Invoke(gesture);
        }

        private void UpdateGestureScan()
        {
            bool scan = _wantedGestures != null && _wantedGestures.Count > 0 && Source != null &&
                        !ChannelRoutes.Has(Source.Supplies, Channels.Gesture);
            GestureScan = scan ? new GestureScan(_wantedGestures, new Scanner(Timing.ScanStepSeconds, Timing.ScanRounds, Timing.ScanFromTop)) : null;
        }

        private void ApplySmoothing()
        {
            AttuneTuning.Smoothing? smoothing = Source.Smoothing;
            if (!smoothing.HasValue)
            {
                return;
            }

            Tune(_levelFilter, smoothing.Value);
            Tune(_steerFilter, smoothing.Value);
            Tune(_pointXFilter, smoothing.Value);
            Tune(_pointYFilter, smoothing.Value);
        }

        private static void Tune(OneEuroFilter filter, AttuneTuning.Smoothing smoothing)
        {
            filter.MinCutoff = smoothing.MinCutoff;
            filter.Beta = smoothing.Beta;
        }

        private float Smooth(OneEuroFilter filter, float value, float dt) => Source.Smoothing.HasValue ? filter.Filter(value, dt) : value;

        private void ResetFilters()
        {
            _levelFilter.Reset();
            _steerFilter.Reset();
            _pointXFilter.Reset();
            _pointYFilter.Reset();
        }
    }
}
