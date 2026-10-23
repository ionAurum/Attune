using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// What the Kite Breath and the Hum Flyer share (ruling A33, a slow loop): a sky over a strip of
    /// ground, a flyer a quarter of the way across whose height follows the Level (FlightHeight),
    /// and marks at different heights drifting past it (DriftCourse). One missed comes round again;
    /// every one reached, a banner shows for a moment and a new round begins, as in the Compass
    /// Sweep. Nothing drifts while a menu is open. Heights 0 to 1 are drawn from just above the
    /// ground to just under the top of the sky (SkyY).
    /// </summary>
    public abstract class DriftDemo : DemoScreen
    {
        /// <summary>Where the flyer is, across the sky.</summary>
        protected const float ProbeX = 0.25f;

        /// <summary>How far apart the marks are, in screen widths.</summary>
        protected const float Spacing = 0.45f;

        /// <summary>A reached mark's colour, and the kite's.</summary>
        protected static readonly Color Bright = new(1f, 0.84f, 0.2f);

        private static readonly Color _skyTop = new(0.04f, 0.09f, 0.22f);
        private static readonly Color _skyBottom = new(0.12f, 0.25f, 0.42f);
        private static readonly Color _ground = new(0.2f, 0.34f, 0.18f);

        private readonly List<RectTransform> _marks = new();
        private RectTransform _flyer, _banner;
        private float _bannerLeft;

        /// <summary>The marks and how far they have drifted.</summary>
        public DriftCourse Course { get; private set; }

        /// <summary>The flyer's height.</summary>
        public FlightHeight Flight { get; private set; }

        /// <summary>Rounds finished: every mark reached.</summary>
        public int Rounds { get; private set; }

        /// <summary>How much faster than the tuning the marks drift: the check speeds them up; never saved.</summary>
        public float SpeedScale { get; set; } = 1f;

        /// <summary>The sky, which the marks and the flyer are drawn on.</summary>
        protected RectTransform Sky { get; private set; }

        /// <summary>The flyer's rect, centred on it.</summary>
        protected RectTransform Flyer => _flyer;

        /// <summary>The marks' heights, in the order they come.</summary>
        protected abstract float[] Heights { get; }

        /// <summary>How fast the marks drift, in screen widths a second, from the tuning.</summary>
        protected abstract float DriftSpeed { get; }

        /// <summary>
        /// A long breath or hum is how these demos are played, so with the Breath or Hum source the
        /// menu takes a longer one: the tuning's, never less than the profile's Long hold (ruling A37).
        /// </summary>
        protected override float MenuHold(InputRouter router) =>
            router.Source.UsesMicrophone ? Mathf.Max(AttuneTuning.Current.DriftMenuHoldSeconds, router.Timing.LongHoldSeconds) : base.MenuHold(router);

        /// <summary>How near the flyer must pass a mark, from the tuning.</summary>
        protected abstract float Reach { get; }

        /// <summary>The marks' name in the header: "Ribbons".</summary>
        protected abstract string MarksName { get; }

        /// <summary>What the banner says once every mark is reached: "Every ribbon touched!".</summary>
        protected abstract string Cheer { get; }

        /// <summary>The flyer's size, in reference pixels.</summary>
        protected abstract Vector2 FlyerSize { get; }

        /// <summary>The flyer's height, following the Level at the demo's rates.</summary>
        protected abstract FlightHeight MakeFlight();

        /// <summary>Draws the flyer on its rect.</summary>
        protected abstract void DrawFlyer(RectTransform flyer);

        /// <summary>Draws a mark on its column, a strip of the sky's full height that drifts across it.</summary>
        protected abstract void DrawMark(RectTransform column, int mark);

        /// <summary>Shows a mark as reached or not, by its shape as well as its colour (ruling A10).</summary>
        protected abstract void ShowMark(RectTransform column, int mark, bool reached);

        /// <summary>The flyer was placed this frame, at height, having moved by climb (for a tilt or a kite string).</summary>
        protected virtual void Placed(float height, float climb, float dt)
        {
        }

        /// <inheritdoc/>
        protected override string Status => Course == null ? "" :
            $"{MarksName} {Course.Reached} of {Course.Count}" + (Rounds > 0 ? $"  ·  Rounds {Rounds}" : "");

        /// <summary>A height, 0 to 1, as a share of the sky's height: the kite or flyer clear of the ground at 0, under the top at 1.</summary>
        protected static float SkyY(float height) => 0.1f + 0.8f * Mathf.Clamp01(height);

        /// <summary>The hint for how this source raises the Level: breath, hum, or held.</summary>
        protected string LevelHint(string breath, string hum, string held, string other)
        {
            InputSource source = Router.Source;
            if (source.Id == SourceIds.Breath)
            {
                return breath;
            }

            if (source.Id == SourceIds.Hum)
            {
                return hum;
            }

            bool press = ChannelRoutes.Has(source.Supplies, Channels.Press) && !ChannelRoutes.Has(source.Supplies, Channels.Level);
            return press ? (source.ScansMenus ? "Hold your switch" : "Hold a press") + held : other;
        }

        //----------

        /// <inheritdoc/>
        protected override void BuildStage()
        {
            Course = new DriftCourse(Heights, Spacing, ProbeX) { Speed = DriftSpeed, Reach = Reach };
            Flight = MakeFlight();
            UiTheme theme = UiKit.Theme;

            Sky = UiKit.Anchor(UiKit.Rect("Sky", Stage), 0f, 0.08f, 1f, 1f);
            UiKit.GradientFill(Sky, _skyTop, _skyBottom);
            Sky.gameObject.AddComponent<RectMask2D>(); // marks drifting in and out are cut at its edges
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Ground", Stage), 0f, 0f, 1f, 0.08f), _ground);

            for (int i = 0; i < Course.Count; i++)
            {
                RectTransform column = UiKit.Rect($"{MarksName} {i + 1}", Sky);
                column.anchorMin = new Vector2(0f, 0f);
                column.anchorMax = new Vector2(0f, 1f);
                column.pivot = new Vector2(0.5f, 0.5f);
                column.sizeDelta = new Vector2(130f, 0f);
                DrawMark(column, i);
                ShowMark(column, i, false);
                _marks.Add(column);
            }

            _flyer = UiKit.Rect("Flyer", Sky);
            _flyer.sizeDelta = FlyerSize;
            DrawFlyer(_flyer);

            _banner = UiKit.Anchor(UiKit.Rect("Banner", Stage), 0.3f, 0.36f, 0.7f, 0.64f);
            UiKit.Shape(_banner, theme.Background, theme.PanelCorner);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", _banner), 0f, 0f, 1f, 1f), Bright, theme.PanelCorner, 5f);
            TextMeshProUGUI cheer = UiKit.Title(_banner, Cheer, 60f, theme.Text, TextAlignmentOptions.Center);
            UiKit.Anchor(cheer.rectTransform, 0.04f, 0.4f, 0.96f, 0.95f);
            TextMeshProUGUI after = UiKit.Text(_banner, "A new round in a moment.", 28f, theme.Text, align: TextAlignmentOptions.Center);
            UiKit.Anchor(after.rectTransform, 0.04f, 0.08f, 0.96f, 0.4f);
            _banner.gameObject.SetActive(false);

            Lay(0f, 0f);
        }

        /// <inheritdoc/>
        protected override void Play(float dt)
        {
            if (_bannerLeft > 0f)
            {
                _bannerLeft -= dt;
                if (_bannerLeft <= 0f)
                {
                    Course.Reset();
                    _banner.gameObject.SetActive(false);
                    for (int i = 0; i < _marks.Count; i++)
                    {
                        ShowMark(_marks[i], i, false);
                    }
                }
            }

            float before = Flight.Height;
            float height = Flight.Update(Router.Level, dt);
            Course.Speed = DriftSpeed * SpeedScale;
            Course.Reach = Reach;
            int reached = Course.Advance(dt, height);
            if (reached >= 0)
            {
                ShowMark(_marks[reached], reached, true);
                if (Course.AllReached && _bannerLeft <= 0f)
                {
                    Rounds++;
                    _bannerLeft = AttuneTuning.Current.FlagsBackSeconds;
                    _banner.gameObject.SetActive(true);
                }
            }

            Lay(height - before, dt);
        }

        // Puts the marks where they have drifted to, and the flyer at its height.
        private void Lay(float climb, float dt)
        {
            for (int i = 0; i < _marks.Count; i++)
            {
                float x = Course.ScreenX(i);
                RectTransform column = _marks[i];
                bool shown = x > -0.1f && x < 1.1f;
                if (column.gameObject.activeSelf != shown)
                {
                    column.gameObject.SetActive(shown);
                }

                column.anchorMin = new Vector2(x, 0f);
                column.anchorMax = new Vector2(x, 1f);
                column.anchoredPosition = Vector2.zero;
            }

            float y = SkyY(Flight.Height);
            _flyer.anchorMin = _flyer.anchorMax = new Vector2(ProbeX, y);
            _flyer.anchoredPosition = Vector2.zero;
            Placed(Flight.Height, climb, dt);
        }

        /// <summary>A child of column spanning heights low to high of the sky, and the column's width.</summary>
        protected static RectTransform Between(RectTransform column, string name, float low, float high) =>
            UiKit.Anchor(UiKit.Rect(name, column), 0f, SkyY(low), 1f, SkyY(high));

        /// <summary>Lays line, a thin rect on the sky, from a to b, in the sky's own pixels from its bottom left.</summary>
        protected static void Line(RectTransform line, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 along = b - a;
            line.anchorMin = line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = a;
            line.sizeDelta = new Vector2(along.magnitude, thickness);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
        }
    }
}
