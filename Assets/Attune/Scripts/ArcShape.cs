using Aurumworks;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// A ring, or the part of one that Fill says, from the top clockwise: the Dwell Builder's dwell
    /// progress (design, Demos, 3: a ring shows dwell progress). It fills its rect's largest circle.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))] // uGUI no longer requires it; the Editor's lazy add can fail
    public class ArcShape : MaskableGraphic
    {
        private const int Segments = 64;

        private float _fill = 1f;
        private float _thickness = 10f;

        /// <summary>An arc on a new child of parent, filling it.</summary>
        public static ArcShape On(Transform parent, string name, Color color, float thickness)
        {
            RectTransform rect = UiKit.Anchor(UiKit.Rect(name, parent), 0f, 0f, 1f, 1f);
            var arc = rect.gameObject.AddComponent<ArcShape>();
            arc.raycastTarget = false;
            arc.color = color;
            arc.Thickness = thickness;
            return arc;
        }

        /// <summary>How much of the ring is drawn, 0 to 1, from the top clockwise.</summary>
        public float Fill
        {
            get => _fill;
            set
            {
                float fill = Mathf.Clamp01(value);
                if (!Mathf.Approximately(fill, _fill))
                {
                    _fill = fill;
                    SetVerticesDirty();
                }
            }
        }

        /// <summary>How thick the ring is, in the rect's units.</summary>
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = Mathf.Max(1f, value);
                SetVerticesDirty();
            }
        }

        /// <inheritdoc/>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int segments = Mathf.CeilToInt(Segments * _fill);
            if (segments <= 0)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            Vector2 centre = rect.center;
            float outer = Mathf.Min(rect.width, rect.height) / 2f;
            float inner = Mathf.Max(0f, outer - _thickness);
            Color32 tint = color;
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * _fill * i / segments;
                var along = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                vh.AddVert(centre + along * outer, tint, Vector2.zero);
                vh.AddVert(centre + along * inner, tint, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = 2 * i;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }
    }
}
