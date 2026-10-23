using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Demo 2, the Keyboard Garden (design, Demos, 2; Gesture and Press): a garden split into four
    /// beds, one per gesture (each keyboard quadrant is one bed). A gesture plants in its bed, and
    /// holding grows the newest plant taller: from the press's start, full grown at the Long hold
    /// threshold, for every source (ruling A30). A source without Gesture scans the beds (ruling
    /// A18): a Tap plants in the lit bed, and gives back any growth its moment down gave the plant
    /// before. Each bed's flowers have their own shape as well as colour (ruling A10).
    /// </summary>
    public class KeyboardGarden : DemoScreen
    {
        private const float Soil = 0.16f; // the soil's top, as a share of a bed's height
        private const float Reach = 0.5f; // how far up the bed a full-grown stem reaches
        private const float HeadSize = 96f; // a full-grown flower's size, in reference pixels

        private static readonly Color _bed = new(0.2f, 0.15f, 0.11f);
        private static readonly Color _soil = new(0.33f, 0.22f, 0.13f);
        private static readonly Color _stem = new(0.36f, 0.72f, 0.32f);
        private static readonly Color _lit = new(1f, 0.84f, 0.2f);
        private static readonly Color[] _flowers =
        {
            new(0.97f, 0.5f, 0.62f), new(0.98f, 0.82f, 0.25f), new(0.68f, 0.58f, 1f), new(0.99f, 0.58f, 0.22f),
        };

        private static readonly string[] _keys = { "keys 1 to 6, Q to T", "keys 7 to =, Y to ]", "keys A to G, Z to B, space", "keys H to Enter, N to /" };

        private readonly List<RectTransform> _beds = new();
        private readonly List<GameObject> _frames = new();
        private readonly List<TextMeshProUGUI> _keyLines = new();
        private (RectTransform Stem, RectTransform Head, PolyShape Shape, RectTransform Eye)[,] _plants;
        private RectTransform _marker;
        private (int Bed, int Place, float Height) _before = (-1, -1, 0f); // the growing plant as the press began

        /// <summary>The beds and their plants.</summary>
        public GardenBeds Garden { get; private set; }

        /// <inheritdoc/>
        protected override string DemoId => "KeyboardGarden";

        /// <inheritdoc/>
        protected override string Status => Garden == null ? "" : $"Plants {Garden.Plants}  ·  Full grown {Garden.FullGrown}";

        /// <inheritdoc/>
        protected override string ActionHint => Router.GestureScan != null
            ? "Tap: plant in the lit bed  ·  Hold: grow the newest"
            : "A zone plants in its bed  ·  Hold: grow";

        /// <inheritdoc/>
        protected override float StageBottom => 0.21f; // above the gesture scan's bar, which reaches 0.19 of the screen

        //----------

        /// <inheritdoc/>
        protected override void BuildStage()
        {
            Garden = new GardenBeds(KeyboardZones.Quadrants, AttuneTuning.Current.PlantsPerBed);
            _plants = new (RectTransform, RectTransform, PolyShape, RectTransform)[Garden.Beds.Count, Garden.PlantsPerBed];
            UiTheme theme = UiKit.Theme;
            Vector2[][] shapes = { PolyShape.Circle(), PolyShape.Diamond(), PolyShape.Triangle(), PolyShape.Square() };
            for (int bed = 0; bed < Garden.Beds.Count; bed++)
            {
                float x = bed % 2 == 0 ? 0f : 0.51f;
                float y = bed < 2 ? 0.51f : 0f;
                RectTransform box = UiKit.Anchor(UiKit.Rect("Bed: " + Garden.Beds[bed], Stage), x, y, x + 0.49f, y + 0.49f);
                UiKit.Shape(box, _bed, theme.PanelCorner);
                UiKit.Shape(UiKit.Anchor(UiKit.Rect("Soil", box), 0f, 0f, 1f, Soil), _soil, theme.PanelCorner);
                GameObject frame = UiKit.Shape(UiKit.Anchor(UiKit.Rect("Lit", box), 0f, 0f, 1f, 1f), _lit, theme.PanelCorner, 7f).gameObject;
                frame.SetActive(false);
                _frames.Add(frame);

                TextMeshProUGUI name = UiKit.Text(box, Garden.Beds[bed], 32f, theme.Text, FontStyles.Bold);
                UiKit.Anchor(name.rectTransform, 0.03f, 0.8f, 0.6f, 0.97f);
                TextMeshProUGUI keys = UiKit.Text(box, _keys[bed], 26f, theme.Text, align: TextAlignmentOptions.TopRight);
                UiKit.Anchor(keys.rectTransform, 0.4f, 0.8f, 0.97f, 0.97f);
                _keyLines.Add(keys);
                _beds.Add(box);

                for (int place = 0; place < Garden.PlantsPerBed; place++)
                {
                    float across = (place + 0.5f) / Garden.PlantsPerBed;
                    RectTransform stem = UiKit.Rect("Stem", box);
                    UiKit.Fill(stem, _stem);
                    RectTransform head = UiKit.Rect("Flower", box);
                    PolyShape shape = PolyShape.On(head, "Shape", shapes[bed], _flowers[bed]);
                    RectTransform eye = UiKit.Anchor(UiKit.Rect("Eye", head), 0.36f, 0.36f, 0.64f, 0.64f);
                    PolyShape.On(eye, "Shape", PolyShape.Circle(), _bed);
                    stem.anchorMin = new Vector2(across, Soil);
                    stem.anchorMax = new Vector2(across, Soil);
                    _plants[bed, place] = (stem, head, shape, eye);
                }
            }

            _marker = UiKit.Rect("Growing", Stage);
            PolyShape.On(_marker, "Shape", new[] { new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) }, theme.Text);
            _marker.sizeDelta = new Vector2(34f, 26f);
            ShowPlants();
        }

        /// <inheritdoc/>
        protected override void Begin() => Router.WantGestures(KeyboardZones.Quadrants);

        /// <inheritdoc/>
        protected override void End() => Router.WantGestures(null);

        //----------

        /// <inheritdoc/>
        protected override void Play(float dt)
        {
            // Ruling A30: full grown from a seedling at the Long hold threshold.
            if (Router.IsPressDown && !Router.PressBeganInMenu)
            {
                Garden.Grow(dt * (1f - GardenBeds.Seedling) / Mathf.Max(0.1f, Router.Timing.LongHoldSeconds));
            }

            ShowPlants();
        }

        /// <inheritdoc/>
        protected override void PressBegan() =>
            _before = Garden.CurrentBed >= 0 ? (Garden.CurrentBed, Garden.CurrentPlace, Garden.CurrentHeight) : (-1, -1, 0f);

        /// <inheritdoc/>
        protected override void Gesture(string gesture)
        {
            // A scanned gesture comes as its Tap ends: the moment it was down wasn't growing.
            if (Router.GestureScan != null && !Router.IsPressDown && _before.Bed >= 0)
            {
                Garden.SetHeight(_before.Bed, _before.Place, _before.Height);
            }

            _before = (-1, -1, 0f);
            Garden.Plant(gesture);
            ShowPlants();
        }

        private void ShowPlants()
        {
            GestureScan scan = Router.GestureScan;
            string lit = scan != null && !scan.Scanner.IsPaused ? scan.Highlighted : null;
            bool keyboard = Router.Source.Id == SourceIds.Keyboard;
            for (int bed = 0; bed < Garden.Beds.Count; bed++)
            {
                bool on = Garden.Beds[bed] == lit;
                if (_frames[bed].activeSelf != on)
                {
                    _frames[bed].SetActive(on);
                }

                if (_keyLines[bed].gameObject.activeSelf != keyboard)
                {
                    _keyLines[bed].gameObject.SetActive(keyboard);
                }

                for (int place = 0; place < Garden.PlantsPerBed; place++)
                {
                    float height = Garden.Height(bed, place);
                    var plant = _plants[bed, place];
                    bool grown = height > 0f;
                    plant.Stem.gameObject.SetActive(grown);
                    plant.Head.gameObject.SetActive(grown);
                    if (!grown)
                    {
                        continue;
                    }

                    float across = plant.Stem.anchorMin.x;
                    float top = Soil + Reach * height;
                    plant.Stem.anchorMax = new Vector2(across, top);
                    plant.Stem.sizeDelta = new Vector2(10f, 0f);
                    plant.Head.anchorMin = plant.Head.anchorMax = new Vector2(across, top);
                    plant.Head.sizeDelta = Vector2.one * Mathf.Lerp(28f, HeadSize, height);
                    plant.Eye.gameObject.SetActive(height >= 1f); // a full-grown flower opens
                }
            }

            bool growing = Garden.CurrentBed >= 0;
            _marker.gameObject.SetActive(growing);
            if (growing)
            {
                RectTransform bedRect = _beds[Garden.CurrentBed];
                float across = (Garden.CurrentPlace + 0.5f) / Garden.PlantsPerBed;
                Vector2 min = bedRect.anchorMin, max = bedRect.anchorMax;
                _marker.anchorMin = _marker.anchorMax = new Vector2(Mathf.Lerp(min.x, max.x, across), Mathf.Lerp(min.y, max.y, Soil * 0.45f));
            }
        }
    }
}
