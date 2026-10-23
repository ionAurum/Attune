using System.IO;
using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: the hub's Feedback page (design, Feedback): what Attune keeps, that nothing is sent,
    /// where the file is, and a button that opens its folder.
    /// </summary>
    public static class FeedbackPage
    {
        /// <summary>Opens the page over the hub.</summary>
        public static void Open()
        {
            MenuStack menus = SessionMenu.Menus;
            if (menus == null)
            {
                return;
            }

            menus.OpenDialog("Feedback", column =>
            {
                UiTheme theme = UiKit.Theme;
                string folder = FeedbackLog.Folder;
                UiKit.Title(column, "Feedback", 60f, theme.Accent, TextAlignmentOptions.Center);
                UiKit.Text(column, "When you pause a demo, Attune asks how it felt. Each answer is saved on this computer, " +
                                   "and nothing is sent anywhere. To share your answers, send the files in this folder:", 26f);
                UiKit.Text(column, folder ?? "(none: a self-test keeps no feedback)", 24f, theme.Muted);
                UiKit.Button(column, "Back", menus.Close, 60f, theme.Accent);
                if (folder != null)
                {
                    UiKit.Button(column, "Open the folder", () => OpenFolder(folder), 60f);
                }
            });
        }

        private static void OpenFolder(string folder)
        {
            if (SelfTest.IsRunning)
            {
                return;
            }

            Directory.CreateDirectory(folder);
            Application.OpenURL("file:///" + folder.Replace('\\', '/'));
        }
    }
}
