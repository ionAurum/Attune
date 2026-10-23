using System;
using Aurumworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Static: calibrating the active source (design, Calibration). Short, and skippable: Rest (keep
    /// still for 2 seconds), then Reach (as far as is comfortable for 3 seconds) each way that
    /// applies. The Switch source asks for its switch instead; Keyboard zones has nothing to
    /// calibrate. CalibrationRun runs the steps on the open dialog. It can be redone at any time,
    /// from the session menu and Settings.
    /// </summary>
    public static class CalibrationPage
    {
        /// <summary>Opens the page for the active source; done is called as it closes.</summary>
        public static void Open(Action done = null)
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            if (router == null || menus == null)
            {
                done?.Invoke();
                return;
            }

            CalibrationRun run = null;
            RectTransform panel = menus.OpenDialog("Calibration", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Calibrate: " + router.Source.Name, 56f, theme.Accent, TextAlignmentOptions.Center);
                TextMeshProUGUI instruction = UiKit.Text(column, "", 36f, theme.Text, align: TextAlignmentOptions.Center);
                TextMeshProUGUI detail = UiKit.Text(column, "", 26f, theme.Muted, align: TextAlignmentOptions.Center);
                RectTransform time = Bar(column, "Time", 14f, theme.Accent);
                RectTransform meter = Bar(column, "Meter", 34f, theme.AccentDim);
                RectTransform row = UiKit.Row(column, 64f, 16f);
                Button go = UiKit.Button(row, "Start", () => run.Go(), 64f, theme.Accent);
                Button other = UiKit.Button(row, "Skip", () => run.Other(), 64f);
                run = column.gameObject.AddComponent<CalibrationRun>();
                run.Build(router, instruction, detail, time, meter, go, other);
            }, () =>
            {
                if (run != null)
                {
                    run.Stop();
                }

                done?.Invoke();
            });
            TextFloor.Raise(panel);
        }

        // A bar of height; returns its fill, whose right anchor CalibrationRun moves.
        private static RectTransform Bar(RectTransform column, string name, float height, Color color)
        {
            RectTransform back = UiKit.Rect(name, column);
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            UiKit.Shape(back, UiKit.Theme.Background, UiKit.Theme.SmallCorner);
            RectTransform fill = UiKit.Anchor(UiKit.Rect("Fill", back), 0f, 0f, 0f, 1f);
            UiKit.Shape(fill, color, UiKit.Theme.SmallCorner);
            return fill;
        }
    }
}
