using System;
using System.Globalization;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: the profile's timing values (ruling A11: every one can be changed), as sliders. They
    /// apply as they move and are saved as the page closes. With the Switch source a tap on a slider
    /// steps it a tenth along, wrapping round (ScanDriver).
    /// </summary>
    public static class TimingPage
    {
        /// <summary>Opens the page over whatever is showing.</summary>
        public static void Open()
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            if (router == null || menus == null)
            {
                return;
            }

            TimingValues timing = router.Timing;
            menus.OpenDialog("Timing", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Timing", 60f, theme.Accent, TextAlignmentOptions.Center);
                Seconds(column, "Hold", 0.3f, 3f, timing.HoldSeconds, value => timing.HoldSeconds = value);
                Seconds(column, "Long hold", 1f, 5f, timing.LongHoldSeconds, value => timing.LongHoldSeconds = value);
                Seconds(column, "Dwell", 0.5f, 5f, timing.DwellSeconds, value => timing.DwellSeconds = value);
                Seconds(column, "Scan step", 0.4f, 5f, timing.ScanStepSeconds, value => timing.ScanStepSeconds = value);
                Range(column, "Scan rounds", 0f, 10f, timing.ScanRounds, value => timing.ScanRounds = Mathf.RoundToInt(value),
                    value => Mathf.RoundToInt(value) == 0 ? "never stop" : Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture));
                UiKit.Toggle(column, "Scan from the top again", timing.ScanFromTop, value => timing.ScanFromTop = value);
                Seconds(column, "Hold to raise", 0.3f, 5f, timing.RiseSeconds, value => timing.RiseSeconds = value);
                Seconds(column, "Fall", 0.3f, 8f, timing.FallSeconds, value => timing.FallSeconds = value);
                Range(column, "Dead zone", 0f, 0.5f, timing.DeadZone, value => timing.DeadZone = value,
                    value => Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture) + "%");
                UiKit.Toggle(column, "Noisy room", router.Profile.NoisyRoom, value => router.Profile.NoisyRoom = value);
                UiKit.Text(column, string.Format(CultureInfo.InvariantCulture, "Breath ignores anything under {0:0.#} times the room's noise.",
                    AttuneTuning.Current.NoisyRoomTimes), 24f, theme.Muted);
                UiKit.Button(column, "Back", menus.Close, 60f, theme.Accent);
            }, router.SaveProfile);
        }

        private static void Seconds(RectTransform column, string label, float min, float max, float value, Action<float> set) =>
            Range(column, label, min, max, value, set, seconds => seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");

        // A slider from min to max; the kit's slider runs 0 to 1, so the values are mapped both ways.
        private static void Range(RectTransform column, string label, float min, float max, float value, Action<float> set,
            Func<float, string> show)
        {
            UiKit.Slider(column, label, Mathf.InverseLerp(min, max, value), t => set(Mathf.Lerp(min, max, t)),
                t => show(Mathf.Lerp(min, max, t)), 150f);
        }
    }
}
