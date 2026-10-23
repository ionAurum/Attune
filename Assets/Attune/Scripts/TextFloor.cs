using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Keeps every text on the menus' canvas at 24 points or more (ruling A10), the framework's own
    /// pages included (their foot buttons and version line are smaller). It raises the texts of
    /// whatever opens, once, as it appears. Made with the menus.
    /// </summary>
    public class TextFloor : MonoBehaviour
    {
        /// <summary>The smallest text allowed, in points (ruling A10).</summary>
        public const float Minimum = 24f;

        private RectTransform _lastTop;
        private int _lastChildren = -1;

        /// <summary>Raises every text under root, active or not, to Minimum.</summary>
        public static void Raise(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.fontSize < Minimum)
                {
                    text.fontSize = Minimum;
                }

                if (text.enableAutoSizing && text.fontSizeMin < Minimum)
                {
                    text.fontSizeMin = Minimum;
                }
            }

            foreach (TMP_InputField field in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (field.pointSize < Minimum)
                {
                    field.pointSize = Minimum;
                }
            }
        }

        private void LateUpdate()
        {
            MenuStack menus = SessionMenu.Menus;
            if (menus == null)
            {
                return;
            }

            int children = menus.transform.childCount;
            if (menus.Top == _lastTop && children == _lastChildren)
            {
                return;
            }

            _lastTop = menus.Top;
            _lastChildren = children;
            Raise(menus.transform);
        }
    }
}
