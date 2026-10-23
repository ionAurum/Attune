using System;
using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: choosing what the profile plays with, then calibrating it (design, The hub). For a
    /// new profile the Switch is the main action, so a player with one key reaches scanning by
    /// pressing it on every screen; otherwise the source playing is.
    /// </summary>
    public static class SourcePage
    {
        /// <summary>Opens the page; done is called once the source is chosen and calibrated or skipped.</summary>
        public static void Open(bool isNew, Action done = null)
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            if (router == null || menus == null)
            {
                return;
            }

            menus.OpenDialog("Sources", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Play with", 60f, theme.Accent, TextAlignmentOptions.Center);
                string main = isNew ? SourceIds.Switch : router.Source.Id;
                foreach (InputSource source in router.Sources)
                {
                    if (!InputRouter.IsOffered(source))
                    {
                        continue;
                    }

                    string label = source.IsAvailable ? source.Name : source.Name + " (none found)";
                    UiKit.Button(column, label, () =>
                    {
                        router.SetSource(source.Id);
                        menus.Close();
                        CalibrationPage.Open(done);
                    }, 64f, source.Id == main ? theme.Accent : (Color?)null);
                    UiKit.Text(column, source.Description, 24f, theme.Muted);
                }

                UiKit.Button(column, "Back", menus.Close, 60f);
            });
        }
    }
}
