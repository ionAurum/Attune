using System.Collections.Generic;
using Aurumworks;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// A convex polygon drawn in code (ruling A10: shapes drawn in code), filling its rect: points
    /// are 0 to 1 across and up the rect. The demos draw their boats, flags, needles, flowers and
    /// block marks with it. Static helpers give the common shapes.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))] // uGUI no longer requires it; the Editor's lazy add can fail
    public class PolyShape : MaskableGraphic
    {
        private readonly List<Vector2> _points = new();
        private readonly List<Vector2> _scaled = new();

        /// <summary>A shape on a new child of parent, filling it.</summary>
        public static PolyShape On(Transform parent, string name, IEnumerable<Vector2> points, Color color)
        {
            RectTransform rect = UiKit.Anchor(UiKit.Rect(name, parent), 0f, 0f, 1f, 1f);
            var shape = rect.gameObject.AddComponent<PolyShape>();
            shape.raycastTarget = false;
            shape.color = color;
            shape.SetPoints(points);
            return shape;
        }

        /// <summary>Sets the polygon: points 0 to 1 across and up the rect, in order round a convex shape.</summary>
        public void SetPoints(IEnumerable<Vector2> points)
        {
            _points.Clear();
            _points.AddRange(points);
            SetVerticesDirty();
        }

        /// <summary>A regular polygon with sides sides, its first corner at angle degrees (0 up, clockwise).</summary>
        public static Vector2[] Regular(int sides, float angle = 0f)
        {
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (angle + 360f * i / sides) * Mathf.Deg2Rad;
                points[i] = new Vector2(0.5f + 0.5f * Mathf.Sin(a), 0.5f + 0.5f * Mathf.Cos(a));
            }

            return points;
        }

        /// <summary>A circle, drawn with 32 sides.</summary>
        public static Vector2[] Circle() => Regular(32);

        /// <summary>A diamond: a square on its corner.</summary>
        public static Vector2[] Diamond() => Regular(4);

        /// <summary>A triangle pointing up.</summary>
        public static Vector2[] Triangle() => Regular(3);

        /// <summary>A square.</summary>
        public static Vector2[] Square() => new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };

        /// <summary>A boat seen from above, its bow up.</summary>
        public static Vector2[] Boat() => new[]
        {
            new Vector2(0.5f, 1f), new Vector2(0.78f, 0.6f), new Vector2(0.74f, 0f), new Vector2(0.26f, 0f), new Vector2(0.22f, 0.6f),
        };

        /// <summary>An arrowhead pointing up.</summary>
        public static Vector2[] Arrow() => new[] { new Vector2(0.5f, 1f), new Vector2(1f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, 0.25f) };

        /// <inheritdoc/>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 3)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            _scaled.Clear();
            foreach (Vector2 point in _points)
            {
                _scaled.Add(new Vector2(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height));
            }

            UiShape.Fan(vh, _scaled, color);
        }
    }
}
