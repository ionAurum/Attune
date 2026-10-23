using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Shows Press → Gesture by scanning (ruling A18) while a demo wants gestures from a source
    /// without them: the gestures in a row along the bottom, the highlighted one framed, filled and
    /// marked with arrows, so it reads without colour. Hidden while a menu is open. Made with the
    /// menus.
    /// </summary>
    public class GestureScanBar : MonoBehaviour
    {
        private readonly List<(UiShape Box, UiShape Frame, TextMeshProUGUI Label, string Gesture)> _boxes = new();

        private RectTransform _bar;
        private GestureScan _shown;

        private void Update()
        {
            InputRouter router = InputRouter.Instance;
            GestureScan scan = router != null ? router.GestureScan : null;
            if (scan != _shown)
            {
                Rebuild(scan);
            }

            if (_bar == null)
            {
                return;
            }

            bool show = scan != null && !MenuFocus.IsAnyOpen;
            if (_bar.gameObject.activeSelf != show)
            {
                _bar.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            string highlighted = scan.Scanner.IsPaused ? null : scan.Highlighted;
            foreach (var box in _boxes)
            {
                bool on = box.Gesture == highlighted;
                box.Box.color = on ? theme.Accent : theme.Panel;
                box.Frame.gameObject.SetActive(on);
                box.Label.color = on ? theme.OnAccent : theme.Text;
                string text = on ? "> " + box.Gesture + " <" : box.Gesture;
                if (box.Label.text != text)
                {
                    box.Label.text = text;
                }
            }
        }

        private void Rebuild(GestureScan scan)
        {
            _shown = scan;
            if (_bar != null)
            {
                Destroy(_bar.gameObject);
                _bar = null;
            }

            _boxes.Clear();
            MenuStack menus = SessionMenu.Menus;
            if (scan == null || menus == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            _bar = UiKit.Pin(UiKit.Rect("Gesture scan", menus.transform), new Vector2(0.5f, 0f), new Vector2(1400f, 96f), new Vector2(0f, 110f));
            _bar.SetAsFirstSibling(); // under the menus
            var row = _bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            foreach (string gesture in scan.Gestures)
            {
                RectTransform cell = UiKit.Rect("Gesture: " + gesture, _bar);
                UiShape box = UiKit.Shape(cell, theme.Panel, theme.ButtonCorner);
                UiShape frame = UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", cell), 0f, 0f, 1f, 1f), theme.Text, theme.ButtonCorner, 5f);
                TextMeshProUGUI label = UiKit.Text(cell, gesture, 30f, theme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Anchor(label.rectTransform, 0f, 0f, 1f, 1f);
                label.richText = false;
                _boxes.Add((box, frame, label, gesture));
            }
        }
    }
}
