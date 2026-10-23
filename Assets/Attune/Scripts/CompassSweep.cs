using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Demo 1, the Compass Sweep (design, Demos, 1; Press): a small island map. A compass needle
    /// sweeps the headings the boat can sail, and a press sends the boat one cell that way; reach the
    /// flags. The needle waits after a full sweep with no press, so missing it costs nothing, and
    /// stays on the heading it sailed, so pressing again repeats the step (ruling A31). It stands
    /// still while a press is held, so a press sails the heading it began on (as ruling A22 in
    /// menus). The boat points where the next press sends it, and that cell is framed. Every flag
    /// reached, the flags come back for another round.
    /// </summary>
    public class CompassSweep : DemoScreen
    {
        private static readonly string[] _short = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        // Drawn in code (ruling A28); not the theme's, as a map needs its own colours.
        private static readonly Color _sea = new(0.07f, 0.2f, 0.33f);
        private static readonly Color _seaCell = new(0.1f, 0.27f, 0.43f);
        private static readonly Color _land = new(0.36f, 0.52f, 0.27f);
        private static readonly Color _flag = new(0.98f, 0.55f, 0.15f);
        private static readonly Color _target = new(1f, 0.84f, 0.2f);

        private readonly List<GameObject> _flagViews = new();
        private readonly TextMeshProUGUI[] _headingLabels = new TextMeshProUGUI[CompassMap.Headings];

        private RectTransform _map, _boat, _targetFrame, _needle;
        private RectTransform _banner;
        private TextMeshProUGUI _needleText;
        private Vector2 _boatAt; // where the boat is drawn, in cells: it sails towards the map's boat
        private float _bannerLeft;

        /// <summary>The map: the boat, the land and the flags.</summary>
        public CompassMap Map { get; private set; }

        /// <summary>The needle.</summary>
        public CompassNeedle Needle { get; private set; }

        /// <summary>Rounds finished: every flag reached.</summary>
        public int Rounds { get; private set; }

        /// <inheritdoc/>
        protected override string DemoId => "CompassSweep";

        /// <inheritdoc/>
        protected override string Status => Map == null ? "" :
            $"Flags {Map.FlagsReached} of {Map.Flags.Count}" + (Rounds > 0 ? $"  ·  Rounds {Rounds}" : "");

        /// <inheritdoc/>
        protected override string ActionHint => Router.Source.ScansMenus ? Router.Source.Words.Tap + ": sail where the needle points" : "Press: sail where the needle points";

        //----------

        /// <inheritdoc/>
        protected override void BuildStage()
        {
            Map = new CompassMap(CompassMap.Island);
            Needle = new CompassNeedle(new Scanner());
            Needle.Refresh(Map, 0);
            _boatAt = new Vector2(Map.Boat.X, Map.Boat.Y);

            UiTheme theme = UiKit.Theme;
            RectTransform mapArea = UiKit.Anchor(UiKit.Rect("Map area", Stage), 0f, 0f, 0.7f, 1f);
            _map = UiKit.Rect("Map", mapArea);
            Fit(_map, (float)Map.Width / Map.Height);
            UiKit.Shape(_map, _sea, theme.SmallCorner);
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    RectTransform cell = Cell($"Cell {x},{y}", x, y);
                    if (Map.IsLand(x, y))
                    {
                        UiKit.Fill(cell, _land); // square and edge to edge, so the island reads as one shape
                    }
                    else
                    {
                        UiKit.Shape(Inset(cell, 0.06f), _seaCell, 8f);
                    }
                }
            }

            for (int i = 0; i < Map.Flags.Count; i++)
            {
                _flagViews.Add(Flag(i));
            }

            _targetFrame = Cell("Target", 0, 0);
            UiKit.Shape(_targetFrame, _target, 10f, 7f);
            _boat = Cell("Boat", Map.Boat.X, Map.Boat.Y);
            PolyShape.On(Inset(_boat, 0.12f), "Hull", PolyShape.Boat(), theme.Text);

            _banner = UiKit.Anchor(UiKit.Rect("Banner", _map), 0.12f, 0.34f, 0.88f, 0.66f);
            UiKit.Shape(_banner, theme.Background, theme.PanelCorner);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", _banner), 0f, 0f, 1f, 1f), _target, theme.PanelCorner, 5f);
            TextMeshProUGUI cheer = UiKit.Title(_banner, "Every flag reached!", 60f, theme.Text, TextAlignmentOptions.Center);
            UiKit.Anchor(cheer.rectTransform, 0.04f, 0.4f, 0.96f, 0.95f);
            TextMeshProUGUI after = UiKit.Text(_banner, "The flags come back in a moment.", 28f, theme.Text, align: TextAlignmentOptions.Center);
            UiKit.Anchor(after.rectTransform, 0.04f, 0.08f, 0.96f, 0.4f);
            _banner.gameObject.SetActive(false);

            BuildDial(theme);
            ShowNeedle();
        }

        private void BuildDial(UiTheme theme)
        {
            RectTransform side = UiKit.Anchor(UiKit.Rect("Compass", Stage), 0.72f, 0f, 1f, 1f);
            RectTransform dialArea = UiKit.Anchor(UiKit.Rect("Dial area", side), 0f, 0.3f, 1f, 1f);
            RectTransform dial = UiKit.Rect("Dial", dialArea);
            Fit(dial, 1f);
            PolyShape.On(dial, "Face", PolyShape.Circle(), theme.Panel);
            ArcShape.On(dial, "Rim", theme.Line, 6f);
            for (int heading = 0; heading < CompassMap.Headings; heading++)
            {
                float angle = heading * 45f * Mathf.Deg2Rad;
                var at = new Vector2(0.5f + 0.38f * Mathf.Sin(angle), 0.5f + 0.38f * Mathf.Cos(angle));
                TextMeshProUGUI label = UiKit.Text(dial, _short[heading], 30f, theme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Anchor(label.rectTransform, at.x - 0.12f, at.y - 0.08f, at.x + 0.12f, at.y + 0.08f);
                _headingLabels[heading] = label;
            }

            _needle = UiKit.Anchor(UiKit.Rect("Needle", dial), 0.2f, 0.2f, 0.8f, 0.8f);
            PolyShape.On(_needle, "Tail", new[] { new Vector2(0.38f, 0.5f), new Vector2(0.5f, 0.56f), new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.08f) }, theme.Muted);
            PolyShape.On(_needle, "Point", new[] { new Vector2(0.5f, 1f), new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.44f), new Vector2(0.38f, 0.5f) }, _target);
            RectTransform hub = UiKit.Anchor(UiKit.Rect("Hub", dial), 0.47f, 0.47f, 0.53f, 0.53f);
            PolyShape.On(hub, "Pin", PolyShape.Circle(), theme.Text);

            _needleText = UiKit.Text(side, "", 32f, theme.Text, FontStyles.Bold, TextAlignmentOptions.Top);
            UiKit.Anchor(_needleText.rectTransform, 0f, 0f, 1f, 0.28f);
        }

        //----------

        /// <inheritdoc/>
        protected override void Play(float dt)
        {
            // The needle stands still while a press is held, so the press sails the heading it began on.
            if (!Router.IsPressDown)
            {
                Needle.Advance(dt, Router.Timing.ScanStepSeconds);
            }

            float sail = AttuneTuning.Current.BoatSailSeconds;
            var to = new Vector2(Map.Boat.X, Map.Boat.Y);
            _boatAt = sail <= 0f ? to : Vector2.MoveTowards(_boatAt, to, dt / sail);
            if (_bannerLeft > 0f)
            {
                _bannerLeft -= dt;
                if (_bannerLeft <= 0f)
                {
                    Map.ResetFlags();
                    _banner.gameObject.SetActive(false);
                }
            }

            ShowNeedle();
        }

        /// <inheritdoc/>
        protected override void PressEnded(PressKind kind)
        {
            int heading = Needle.Press();
            if (heading < 0)
            {
                ShowNeedle(); // it woke
                return;
            }

            _boatAt = new Vector2(Map.Boat.X, Map.Boat.Y); // a press while it sails sends it on at once
            if (Map.Step(heading, out int flag) && flag >= 0)
            {
                _flagViews[flag].SetActive(false);
                if (Map.AllReached)
                {
                    Rounds++;
                    _bannerLeft = AttuneTuning.Current.FlagsBackSeconds;
                    _banner.gameObject.SetActive(true);
                }
            }

            Needle.Refresh(Map, heading);
            ShowNeedle();
        }

        private void ShowNeedle()
        {
            UiTheme theme = UiKit.Theme;
            int heading = Needle.Heading;
            bool waiting = Needle.IsWaiting;
            for (int i = 0; i < CompassMap.Headings; i++)
            {
                bool open = Map.IsOpen(i);
                bool on = i == heading && !waiting;
                _headingLabels[i].color = on ? _target : open ? theme.Text : theme.Muted;
                _headingLabels[i].fontStyle = on ? FontStyles.Bold | FontStyles.Underline : FontStyles.Bold;
            }

            if (heading >= 0)
            {
                _needle.localEulerAngles = new Vector3(0f, 0f, -45f * heading);
                ((RectTransform)_boat.GetChild(0)).localEulerAngles = new Vector3(0f, 0f, -45f * heading);
                (int dx, int dy) = CompassMap.Offset(heading);
                Place(_targetFrame, Map.Boat.X + dx, Map.Boat.Y + dy);
            }

            _targetFrame.gameObject.SetActive(heading >= 0 && !waiting);
            Place(_boat, _boatAt.x, _boatAt.y);

            for (int i = 0; i < _flagViews.Count; i++)
            {
                if (_flagViews[i].activeSelf == Map.IsReached(i))
                {
                    _flagViews[i].SetActive(!Map.IsReached(i));
                }
            }

            string text = heading < 0 ? "No way out!"
                : waiting ? "The needle waits.\nPress to wake it."
                : "Points " + CompassMap.HeadingNames[heading];
            if (_needleText.text != text)
            {
                _needleText.text = text;
            }
        }

        //----------

        private GameObject Flag(int index)
        {
            (int x, int y) = Map.Flags[index];
            RectTransform cell = Cell("Flag " + (index + 1), x, y);
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Pole", cell), 0.3f, 0.12f, 0.36f, 0.9f), UiKit.Theme.Text);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Pennant", cell), 0.36f, 0.52f, 0.86f, 0.9f), "Shape",
                new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0.5f) }, _flag);
            TextMeshProUGUI number = UiKit.Text(cell, (index + 1).ToString(), 26f, UiKit.Theme.Text, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            UiKit.Anchor(number.rectTransform, 0.4f, 0.02f, 0.96f, 0.5f);
            return cell.gameObject;
        }

        private RectTransform Cell(string name, float x, float y)
        {
            RectTransform cell = UiKit.Rect(name, _map);
            Place(cell, x, y);
            return cell;
        }

        // Puts a rect over cell (x, y) of the map, row 0 at the top.
        private void Place(RectTransform rect, float x, float y)
        {
            float w = Map.Width, h = Map.Height;
            rect.anchorMin = new Vector2(x / w, 1f - (y + 1f) / h);
            rect.anchorMax = new Vector2((x + 1f) / w, 1f - y / h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Inset(RectTransform parent, float by) =>
            UiKit.Anchor(UiKit.Rect("Inset", parent), by, by, 1f - by, 1f - by);

        // Keeps rect as large as its parent allows at this width-to-height ratio, centred.
        private static void Fit(RectTransform rect, float ratio)
        {
            UiKit.Anchor(rect, 0f, 0f, 1f, 1f);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = ratio;
        }
    }
}
