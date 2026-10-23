using System;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Static: the hub's Demos (design, The hub): one tile per demo with its name, a line about it,
    /// and the channels it needs with how the current source gives them. A tile the source can't
    /// play says why in words, and choosing it offers the sources that can.
    /// </summary>
    public static class DemosPage
    {
        private const float TileHeight = 150f;

        /// <summary>What a tile says about the current source and this demo.</summary>
        public static string NeedsLine(DemoInfo demo, InputSource source)
        {
            Channels missing = demo.Needs.Missing(source.Supplies);
            if (missing != Channels.None)
            {
                return $"{source.Name} can't play this: it needs {ChannelRoutes.Names(missing).Replace(", ", " or ")}.";
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (Channels need in demo.Needs.Needs)
            {
                foreach (Channels channel in ChannelRoutes.All)
                {
                    if (ChannelRoutes.Has(need, channel) && ChannelRoutes.How(channel, source.Supplies) != null)
                    {
                        parts.Add($"{channel}: {ChannelRoutes.How(channel, source.Supplies)}");
                        break;
                    }
                }
            }

            return "Needs " + string.Join(" · ", parts);
        }

        /// <summary>Opens the page over the hub.</summary>
        public static void Open()
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            if (router == null || menus == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            RectTransform page = menus.Open("Demos");
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Backdrop", page), 0f, 0f, 1f, 1f), theme.Background);
            TextMeshProUGUI title = UiKit.Title(page, "Demos", 72f, theme.Accent);
            UiKit.Anchor(title.rectTransform, 0.04f, 0.88f, 0.6f, 0.98f);
            TextMeshProUGUI playing = UiKit.Text(page, $"Playing with {router.Source.Name}", 28f, theme.Muted, align: TextAlignmentOptions.BottomRight);
            UiKit.Anchor(playing.rectTransform, 0.5f, 0.89f, 0.96f, 0.95f);

            RectTransform grid = UiKit.Anchor(UiKit.Rect("Grid", page), 0.04f, 0.12f, 0.96f, 0.87f);
            var column = grid.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 14f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            RectTransform row = null;
            for (int i = 0; i < DemoCatalogue.All.Count; i++)
            {
                if (i % 2 == 0)
                {
                    row = UiKit.Row(grid, TileHeight, 18f);
                }

                Tile(row, DemoCatalogue.All[i], router.Source);
            }

            RectTransform foot = UiKit.Anchor(UiKit.Rect("Foot", page), 0.04f, 0.025f, 0.3f, 0.095f);
            var footRow = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footRow.childControlWidth = footRow.childControlHeight = true;
            footRow.childForceExpandWidth = footRow.childForceExpandHeight = true;
            UiKit.Button(foot, "Back", menus.Close, 60f);

            TextFloor.Raise(page);
            MenuStack.SelectMainAction(page);
        }

        /// <summary>
        /// Says the current source can't play demo, and offers each source that can; choosing one
        /// changes to it and calls play.
        /// </summary>
        public static void OfferOtherSources(DemoInfo demo, Action play)
        {
            InputRouter router = InputRouter.Instance;
            MenuStack menus = SessionMenu.Menus;
            menus.OpenDialog("Other sources", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, demo.Name, 56f, theme.Accent, TextAlignmentOptions.Center);
                UiKit.Text(column, NeedsLine(demo, router.Source), 26f, align: TextAlignmentOptions.Center);
                bool any = false;
                foreach (InputSource source in router.Sources)
                {
                    if (!InputRouter.IsOffered(source) || source == router.Source || !demo.Needs.CanBeMetBy(source.Supplies))
                    {
                        continue;
                    }

                    if (!any)
                    {
                        UiKit.Text(column, "Play it with:", 26f, theme.Muted, align: TextAlignmentOptions.Center);
                        any = true;
                    }

                    UiKit.Button(column, source.IsAvailable ? source.Name : source.Name + " (none found)", () =>
                    {
                        router.SetSource(source.Id);
                        menus.CloseAll();
                        play();
                    }, 64f);
                }

                UiKit.Button(column, "Back", menus.Close, 60f, theme.Accent);
            });
        }

        private static void Tile(RectTransform row, DemoInfo demo, InputSource source)
        {
            UiTheme theme = UiKit.Theme;
            bool playable = demo.Needs.CanBeMetBy(source.Supplies);
            Button tile = UiKit.Button(row, demo.Name, () =>
            {
                if (demo.Needs.CanBeMetBy(InputRouter.Instance.Supplied))
                {
                    DemoSession.Play(demo);
                }
                else
                {
                    OfferOtherSources(demo, () => DemoSession.Play(demo));
                }
            }, TileHeight);

            // The kit centres the name over the whole button; here it heads the tile, with two lines under it.
            TextMeshProUGUI name = tile.GetComponentInChildren<TextMeshProUGUI>();
            name.alignment = TextAlignmentOptions.TopLeft;
            name.fontSize = 32f;
            name.margin = new Vector4(22f, 14f, 22f, 0f);
            TextMeshProUGUI line = UiKit.Text(tile.transform, demo.Line + (demo.IsStub ? $" (Built in {demo.Milestone}.)" : ""), 24f, theme.Text);
            UiKit.Anchor(line.rectTransform, 0f, 0f, 1f, 1f);
            line.margin = new Vector4(22f, 58f, 22f, 0f);
            TextMeshProUGUI needs = UiKit.Text(tile.transform, NeedsLine(demo, source), 24f, playable ? theme.Muted : theme.Text,
                playable ? FontStyles.Normal : FontStyles.Italic);
            UiKit.Anchor(needs.rectTransform, 0f, 0f, 1f, 1f);
            needs.alignment = TextAlignmentOptions.BottomLeft;
            needs.margin = new Vector4(22f, 0f, 22f, 12f);
        }
    }
}
