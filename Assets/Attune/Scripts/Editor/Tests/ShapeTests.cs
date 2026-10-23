using NUnit.Framework;
using UnityEngine;

namespace Attune.Tests
{
    /// <summary>
    /// The shapes drawn in code come with their CanvasRenderer. uGUI's Graphic no longer requires
    /// one, and in the Editor its lazy add can keep a missing one, which threw under the drift
    /// demos' clipped sky.
    /// </summary>
    public class ShapeTests
    {
        [Test]
        public void EachShapeComesWithItsCanvasRenderer()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            try
            {
                PolyShape poly = PolyShape.On(root.transform, "Poly", PolyShape.Diamond(), Color.white);
                ArcShape arc = ArcShape.On(root.transform, "Arc", Color.white, 4f);
                Assert.That(poly.GetComponent<CanvasRenderer>() != null, Is.True, "PolyShape");
                Assert.That(arc.GetComponent<CanvasRenderer>() != null, Is.True, "ArcShape");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
