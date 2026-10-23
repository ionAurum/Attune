using Aurumworks;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Demo 4, the Kite Breath (design, Demos, 4; Level): breathe out to lift the kite, and it falls
    /// gently when you stop. Ribbons hang from the sky at different heights and drift past (ruling
    /// A33); the kite touches one by being at its height as it passes. A held switch plays it too
    /// (Press → Level), and so does any source that gives a Level.
    /// </summary>
    public class KiteBreath : DriftDemo
    {
        private static readonly float[] _heights = { 0.35f, 0.6f, 0.45f, 0.8f, 0.55f, 0.25f };
        private static readonly Color _string = new(0.92f, 0.92f, 0.92f);
        private static readonly Color _ribbon = new(0.55f, 0.6f, 0.75f);

        private RectTransform _kiteString;

        /// <inheritdoc/>
        protected override string DemoId => "KiteBreath";

        /// <inheritdoc/>
        protected override string ActionHint => LevelHint("Breathe out: the kite rises", "Hum higher: the kite rises", ": the kite rises",
            "Raise the level: the kite rises");

        /// <inheritdoc/>
        protected override float[] Heights => _heights;

        /// <inheritdoc/>
        protected override float DriftSpeed => AttuneTuning.Current.KiteDriftSpeed;

        /// <inheritdoc/>
        protected override float Reach => AttuneTuning.Current.RibbonReach;

        /// <inheritdoc/>
        protected override string MarksName => "Ribbons";

        /// <inheritdoc/>
        protected override string Cheer => "Every ribbon touched!";

        /// <inheritdoc/>
        protected override Vector2 FlyerSize => new(110f, 140f);

        /// <inheritdoc/>
        protected override FlightHeight MakeFlight()
        {
            AttuneTuning tuning = AttuneTuning.Current;
            return new FlightHeight(tuning.KiteRiseSeconds, tuning.KiteFallSeconds);
        }

        /// <inheritdoc/>
        protected override void DrawFlyer(RectTransform flyer)
        {
            // The string first, so the kite is drawn over it.
            _kiteString = UiKit.Rect("String", Sky);
            UiKit.Fill(_kiteString, _string);
            _kiteString.SetAsFirstSibling();

            RectTransform tail = UiKit.Anchor(UiKit.Rect("Tail", flyer), 0.46f, -0.5f, 0.54f, 0.1f);
            UiKit.Fill(tail, _string);
            for (int i = 0; i < 3; i++)
            {
                float y = -0.15f - 0.15f * i;
                PolyShape.On(UiKit.Anchor(UiKit.Rect("Bow " + (i + 1), flyer), 0.38f, y - 0.05f, 0.62f, y + 0.05f), "Shape",
                    PolyShape.Diamond(), Bright);
            }

            PolyShape.On(flyer, "Kite", PolyShape.Diamond(), Bright);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Spar", flyer), 0.48f, 0.04f, 0.52f, 0.96f), "Shape", PolyShape.Square(), _string);
        }

        /// <inheritdoc/>
        protected override void DrawMark(RectTransform column, int mark)
        {
            float height = Heights[mark];
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Thread", column), 0.48f, SkyY(height + 0.05f), 0.52f, 1f), _string);
            RectTransform bow = Between(column, "Bow", height - 0.05f, height + 0.05f);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Left", bow), 0.1f, 0.15f, 0.5f, 0.85f), "Shape", PolyShape.Diamond(), _ribbon);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Right", bow), 0.5f, 0.15f, 0.9f, 0.85f), "Shape", PolyShape.Diamond(), _ribbon);
            RectTransform tails = Between(column, "Tails", height - 0.14f, height - 0.03f);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Tail L", tails), 0.3f, 0f, 0.48f, 1f), "Shape",
                new[] { new Vector2(0.4f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) }, _ribbon);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Tail R", tails), 0.52f, 0f, 0.7f, 1f), "Shape",
                new[] { new Vector2(0.6f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) }, _ribbon);
            RectTransform halo = Between(column, "Halo", height - 0.11f, height + 0.11f);
            ArcShape.On(halo, "Ring", Bright, 6f);
        }

        /// <inheritdoc/>
        protected override void ShowMark(RectTransform column, int mark, bool reached)
        {
            foreach (PolyShape shape in column.GetComponentsInChildren<PolyShape>(true))
            {
                shape.color = reached ? Bright : _ribbon;
            }

            // Not by colour alone (ruling A10): a touched ribbon wears a ring.
            column.Find("Halo").gameObject.SetActive(reached);
        }

        /// <inheritdoc/>
        protected override void Placed(float height, float climb, float dt)
        {
            if (_kiteString == null)
            {
                return;
            }

            Rect sky = Sky.rect;
            var kite = new Vector2(ProbeX * sky.width, SkyY(height) * sky.height - FlyerSize.y * 0.1f);
            var hand = new Vector2((ProbeX - 0.12f) * sky.width, 0f);
            Line(_kiteString, hand, kite, 3f);
        }
    }
}
