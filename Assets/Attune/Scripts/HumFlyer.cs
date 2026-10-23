using Aurumworks;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Demo 5, the Hum Flyer (design, Demos, 5; Level): hum higher to climb and lower to dive,
    /// within the range calibrated, and fly through rings that drift past at different heights
    /// (ruling A33). When the hum stops the flyer glides down (ruling A34). Any source that gives a
    /// Level plays it: a held switch climbs, letting go dives.
    /// </summary>
    public class HumFlyer : DriftDemo
    {
        private const float MaxTilt = 25f;

        private static readonly float[] _heights = { 0.5f, 0.75f, 0.3f, 0.6f, 0.15f, 0.85f };
        private static readonly Color _ring = new(0.92f, 0.92f, 0.92f);
        private static readonly Color _body = new(0.95f, 0.45f, 0.35f);

        private float _tilt;

        /// <inheritdoc/>
        protected override string DemoId => "HumFlyer";

        /// <inheritdoc/>
        protected override string ActionHint => LevelHint("Breathe harder to climb", "Hum higher to climb, lower to dive",
            " to climb; let go to dive", "Raise the level to climb");

        /// <inheritdoc/>
        protected override float[] Heights => _heights;

        /// <inheritdoc/>
        protected override float DriftSpeed => AttuneTuning.Current.FlyerDriftSpeed;

        /// <inheritdoc/>
        protected override float Reach => AttuneTuning.Current.RingReach;

        /// <inheritdoc/>
        protected override string MarksName => "Rings";

        /// <inheritdoc/>
        protected override string Cheer => "Through every ring!";

        /// <inheritdoc/>
        protected override Vector2 FlyerSize => new(140f, 80f);

        /// <inheritdoc/>
        protected override FlightHeight MakeFlight()
        {
            float follow = AttuneTuning.Current.FlyerFollowSeconds;
            return new FlightHeight(follow, follow);
        }

        /// <inheritdoc/>
        protected override void DrawFlyer(RectTransform flyer)
        {
            PolyShape.On(flyer, "Body", new[] { new Vector2(0f, 0.3f), new Vector2(0f, 0.7f), new Vector2(1f, 0.5f) }, _body);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Wing", flyer), 0.2f, 0.5f, 0.6f, 1f), "Shape",
                new[] { new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Vector2(1f, 0f) }, Bright);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Eye", flyer), 0.66f, 0.47f, 0.74f, 0.6f), "Shape", PolyShape.Circle(), UiKit.Theme.Background);
        }

        /// <inheritdoc/>
        protected override void DrawMark(RectTransform column, int mark)
        {
            float height = Heights[mark];
            float reach = Reach;
            RectTransform ring = Between(column, "Ring", height - reach, height + reach);
            PolyShape.On(UiKit.Anchor(UiKit.Rect("Inside", ring), 0.12f, 0.12f, 0.88f, 0.88f), "Disc", PolyShape.Circle(),
                new Color(Bright.r, Bright.g, Bright.b, 0.45f));
            ArcShape.On(ring, "Rim", _ring, 9f);
        }

        /// <inheritdoc/>
        protected override void ShowMark(RectTransform column, int mark, bool reached)
        {
            Transform ring = column.Find("Ring");
            ring.Find("Rim").GetComponent<ArcShape>().color = reached ? Bright : _ring;

            // Not by colour alone (ruling A10): a ring flown through is filled.
            ring.Find("Inside").gameObject.SetActive(reached);
        }

        /// <inheritdoc/>
        protected override void Placed(float height, float climb, float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            float target = Mathf.Clamp(climb / dt * 60f, -MaxTilt, MaxTilt);
            _tilt = Mathf.Lerp(_tilt, target, Mathf.Clamp01(dt * 8f));
            Flyer.localEulerAngles = new Vector3(0f, 0f, _tilt);
        }
    }
}
