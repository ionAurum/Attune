using System.Globalization;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// What it hears (design): an overlay down the right of the screen showing each channel live,
    /// raw and calibrated, with the thresholds, the press's timing, the dwell time and the current
    /// scan step, so testers can see what the system reads from their input. Toggled from the
    /// session menu; remembered on this machine. Drawn under the menus. Made with the menus.
    /// </summary>
    public class ChannelOverlay : MonoBehaviour
    {
        private const string ShowingKey = "Attune.Overlay";
        private const float RowHeight = 34f;

        private RectTransform _panel;
        private TextMeshProUGUI _source, _mic, _level, _steer, _point, _press, _gesture, _select, _scan, _dwell;
        private RectTransform _levelFill, _levelRaw, _steerFill, _pressFill, _onTick, _offTick, _holdTick, _longTick;

        /// <summary>The session's overlay, or null before the game has started.</summary>
        public static ChannelOverlay Instance { get; private set; }

        /// <summary>True while it shows.</summary>
        public bool IsShowing => _panel != null && _panel.gameObject.activeSelf;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake() => Instance = this;

        private void Start()
        {
            if (SessionMenu.Menus == null)
            {
                return;
            }

            Build();
            _panel.gameObject.SetActive(Settings.GetFloat(ShowingKey, 0f) > 0.5f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Shows it if hidden, hides it if showing.</summary>
        public void Toggle() => SetShowing(!IsShowing);

        /// <summary>Shows or hides it, and remembers which.</summary>
        public void SetShowing(bool showing)
        {
            if (_panel == null)
            {
                return;
            }

            _panel.gameObject.SetActive(showing);
            Settings.SetFloat(ShowingKey, showing ? 1f : 0f);
            Settings.Save();
        }

        //----------

        private void Update()
        {
            InputRouter router = InputRouter.Instance;
            if (!IsShowing || router == null || router.Source == null)
            {
                return;
            }

            CultureInfo c = CultureInfo.InvariantCulture;
            Channels supplied = router.Supplied;
            TimingValues timing = router.Timing;
            Set(_source, $"{router.Source.Name}: {ChannelRoutes.Names(supplied)}");
            bool mic = router.Source.UsesMicrophone;
            if (_mic.gameObject.activeSelf != mic)
            {
                _mic.gameObject.SetActive(mic);
            }

            if (mic)
            {
                SourceReading raw = router.Raw;
                Set(_mic, !MicrophoneInput.IsOn ? (MicrophoneInput.Problem ?? "Microphone: off")
                    : string.Format(c, "Microphone: {0:0} dB{1}{2}", raw.Loudness, raw.Pitch > 0f ? string.Format(c, ", {0:0} Hz", raw.Pitch) : "",
                        router.Raw.LevelSilent && router.Source.Id == SourceIds.Hum ? ", no note" : ""));
            }

            Set(_level, string.Format(c, "Level {0:0.00}  {1}", router.Level, How(Channels.Level, supplied, router.Raw.Level)));
            SetFill(_levelFill, 0f, router.Level);
            // The raw bar is the reading before smoothing and the dead zone, against the calibration.
            float rawLevel = !ChannelRoutes.Has(supplied, Channels.Level) ? router.Level
                : router.Source.PassesThrough ? Mathf.Clamp01(router.Raw.Level)
                : router.Raw.LevelSilent ? 0f
                : router.CalibrationOf("Level").NormaliseLevel(router.Raw.Level, 0f);
            SetFill(_levelRaw, 0f, rawLevel);
            bool threshold = !ChannelRoutes.Has(supplied, Channels.Press);
            _onTick.gameObject.SetActive(threshold);
            _offTick.gameObject.SetActive(threshold);
            SetTick(_onTick, timing.PressOnLevel);
            SetTick(_offTick, timing.PressOffLevel);

            Set(_steer, string.Format(c, "Steer {0:+0.00;-0.00;0.00}  {1}", router.Steer, How(Channels.Steer, supplied, router.Raw.Steer)));
            float steer = Mathf.Clamp(router.Steer, -1f, 1f);
            SetFill(_steerFill, Mathf.Min(0.5f, 0.5f + steer / 2f), Mathf.Max(0.5f, 0.5f + steer / 2f));

            Set(_point, ChannelRoutes.Has(supplied, Channels.Point)
                ? string.Format(c, "Point {0:0.00}, {1:0.00}", router.Point.x, router.Point.y)
                : "Point: none from this source");

            float span = timing.LongHoldSeconds * 1.25f;
            Set(_press, router.IsPressDown
                ? string.Format(c, "Press: held {0:0.0} s, {1}", router.PressHeldSeconds, Kind(router.PressKindIfReleased))
                : $"Press: up. Last {Kind(router.LastPress)}, {router.PressCount} so far");
            SetFill(_pressFill, 0f, router.IsPressDown ? Mathf.Clamp01(router.PressHeldSeconds / span) : 0f);
            SetTick(_holdTick, timing.HoldSeconds / span);
            SetTick(_longTick, timing.LongHoldSeconds / span);

            Set(_gesture, router.GestureScan != null
                ? $"Gesture: scanning, at {router.GestureScan.Highlighted ?? "none"}; last {router.LastGesture ?? "none"}"
                : $"Gesture: last {router.LastGesture ?? "none"}");
            Set(_select, $"Select: {router.SelectCount} ({ChannelRoutes.How(Channels.Select, supplied) ?? "none"})");

            ScanDriver scan = ScanDriver.Instance;
            Set(_scan, scan != null && scan.IsScanning
                ? scan.Scanner.IsPaused ? "Scan: paused" : $"Scan: {scan.Scanner.Index + 1} of {scan.Scanner.Count}, {timing.ScanStepSeconds:0.0} s a step"
                : "Scan: off");
            DwellTimer dwell = router.Dwell;
            Set(_dwell, dwell == null ? string.Format(c, "Dwell: {0:0.0} s; nothing to dwell on here", timing.DwellSeconds)
                : dwell.Target == DwellTimer.None ? string.Format(c, "Dwell: {0:0.0} s; resting on nothing", dwell.DwellSeconds)
                : string.Format(c, "Dwell: {0:0.0} s; {1:0%} of the way", dwell.DwellSeconds, dwell.Progress));
        }

        private static string How(Channels channel, Channels supplied, float raw)
        {
            string how = ChannelRoutes.How(channel, supplied);
            return how == "direct" ? string.Format(CultureInfo.InvariantCulture, "raw {0:0.000}", raw) : "(" + (how ?? "none") + ")";
        }

        private static string Kind(PressKind? kind) => kind switch
        {
            PressKind.Tap => "Tap",
            PressKind.Hold => "Hold",
            PressKind.LongHold => "Long hold",
            _ => "none",
        };

        private static void Set(TextMeshProUGUI text, string value)
        {
            if (text.text != value)
            {
                text.text = value;
            }
        }

        private static void SetFill(RectTransform fill, float from, float to)
        {
            fill.anchorMin = new Vector2(Mathf.Clamp01(from), 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(to), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        private static void SetTick(RectTransform tick, float at)
        {
            tick.anchorMin = new Vector2(Mathf.Clamp01(at), 0f);
            tick.anchorMax = new Vector2(Mathf.Clamp01(at), 1f);
            tick.offsetMin = new Vector2(-2f, -4f);
            tick.offsetMax = new Vector2(2f, 4f);
        }

        //----------

        private void Build()
        {
            UiTheme theme = UiKit.Theme;
            float width = AttuneTuning.Current.OverlayWidth;
            _panel = UiKit.Pin(UiKit.Rect("What it hears", SessionMenu.Menus.transform), new Vector2(1f, 1f), new Vector2(width, 0f),
                new Vector2(-16f, -16f));
            _panel.pivot = new Vector2(1f, 1f);
            UiKit.Shape(_panel, new Color(theme.Panel.r, theme.Panel.g, theme.Panel.b, 0.94f), theme.PanelCorner);
            var column = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6f;
            column.padding = new RectOffset(20, 20, 16, 18);
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            _panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiKit.Text(_panel, "What it hears", 28f, theme.Accent, FontStyles.Bold);
            _source = Line(theme.Muted);
            _mic = Line(theme.Text);
            _level = Line(theme.Text);
            RectTransform levelBar = Bar(theme);
            _levelRaw = Fill(levelBar, theme.Line);
            _levelFill = Fill(levelBar, theme.Accent);
            _onTick = Fill(levelBar, theme.Text);
            _offTick = Fill(levelBar, theme.Muted);
            _steer = Line(theme.Text);
            RectTransform steerBar = Bar(theme);
            _steerFill = Fill(steerBar, theme.Accent);
            SetTick(Fill(steerBar, theme.Muted), 0.5f);
            _point = Line(theme.Text);
            _press = Line(theme.Text);
            RectTransform pressBar = Bar(theme);
            _pressFill = Fill(pressBar, theme.Accent);
            _holdTick = Fill(pressBar, theme.Muted);
            _longTick = Fill(pressBar, theme.Text);
            _gesture = Line(theme.Text);
            _select = Line(theme.Text);
            _scan = Line(theme.Text);
            _dwell = Line(theme.Muted);
        }

        private TextMeshProUGUI Line(Color color) => UiKit.Text(_panel, "", 24f, color);

        private RectTransform Bar(UiTheme theme)
        {
            RectTransform bar = UiKit.Rect("Bar", _panel);
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight / 2f;
            UiKit.Shape(bar, theme.Background, theme.SmallCorner);
            return bar;
        }

        private static RectTransform Fill(RectTransform bar, Color color)
        {
            RectTransform fill = UiKit.Anchor(UiKit.Rect("Fill", bar), 0f, 0f, 0f, 1f);
            UiKit.Shape(fill, color, 0f);
            return fill;
        }
    }
}
