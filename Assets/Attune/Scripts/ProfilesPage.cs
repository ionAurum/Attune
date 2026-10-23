using System;
using System.Collections.Generic;
using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: Who's playing (design, The hub): pick a profile, which brings back its source and
    /// calibration at once, or make one, which leads into choosing a source and calibrating it. The
    /// profile playing is first and is the main action, so one press goes on as before; with no
    /// profile yet, New profile is. New profiles are named Player 1, Player 2…, renamed in Settings.
    /// </summary>
    public static class ProfilesPage
    {
        private const int MaxShown = 6;

        /// <summary>A profile's button label: "Player 1 · Switch".</summary>
        public static string Label(Profile profile)
        {
            InputSource source = InputRouter.Instance != null ? InputRouter.Instance.Find(profile.Source) : null;
            return profile.Name + " · " + (source != null ? source.Name : profile.Source);
        }

        /// <summary>Opens the page over whatever is showing; done is called once a profile is playing.</summary>
        public static void Open(Action done = null)
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            if (router == null || menus == null)
            {
                return;
            }

            menus.OpenDialog("Profiles", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Who's playing?", 60f, theme.Accent, TextAlignmentOptions.Center);
                List<Profile> all = router.Store.All();
                all.Sort((a, b) => (b.Id == router.Profile.Id).CompareTo(a.Id == router.Profile.Id));
                if (all.Count == 0)
                {
                    UiKit.Text(column, "Make a profile. It keeps what you play with, your calibration and your timing.", 26f, theme.Muted,
                        align: TextAlignmentOptions.Center);
                }

                for (int i = 0; i < all.Count && i < MaxShown; i++)
                {
                    Profile profile = all[i];
                    bool current = profile.Id == router.Profile.Id;
                    UiKit.Button(column, Label(profile), () =>
                    {
                        router.Use(profile);
                        menus.Close();
                        done?.Invoke();
                    }, 64f, current ? theme.Accent : (Color?)null);
                }

                UiKit.Button(column, "New profile", () =>
                {
                    Profile made = router.Store.Create();
                    router.Use(made);
                    menus.Close();
                    SourcePage.Open(isNew: true, done);
                }, 64f, all.Count == 0 ? theme.Accent : (Color?)null);
                UiKit.Button(column, "Back", menus.Close, 60f);
            });
        }
    }
}
