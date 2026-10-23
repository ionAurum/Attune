using System;
using Aurumworks;
using TMPro;

namespace Attune
{
    /// <summary>
    /// Static: "How did that feel?" (ruling A8), asked on the first pause of each visit to a demo
    /// (ruling A21), answerable by any input. Easy, Tiring, Frustrating or Fun writes one row to the
    /// feedback file; Skip, or closing it, writes nothing. Either way the session menu follows.
    /// </summary>
    public static class FeedbackPrompt
    {
        /// <summary>Asks; then is called once the question has closed, however it closed.</summary>
        public static void Open(Action then)
        {
            MenuStack menus = SessionMenu.Menus;
            InputRouter router = InputRouter.Instance;
            DemoInfo demo = DemoSession.Current;
            if (menus == null || router == null || demo == null)
            {
                then?.Invoke();
                return;
            }

            menus.OpenDialog("Feedback question", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "How did that feel?", 56f, theme.Accent, TextAlignmentOptions.Center);
                UiKit.Text(column, $"{demo.Name}, with {router.Source.Name}", 26f, theme.Muted, align: TextAlignmentOptions.Center);
                foreach (string answer in FeedbackLog.Answers)
                {
                    UiKit.Button(column, answer, () =>
                    {
                        Answer(answer);
                        menus.Close();
                    }, 64f);
                }

                UiKit.Button(column, "Skip", menus.Close, 60f);
            }, () =>
            {
                DemoSession.AskedThisVisit = true;
                then?.Invoke();
            });
        }

        private static void Answer(string answer)
        {
            InputRouter router = InputRouter.Instance;
            DemoInfo demo = DemoSession.Current;
            FeedbackLog.Write(router.Profile.Name, demo.Name, router.Source.Name, DemoSession.SecondsInDemo, router.PressCount, answer,
                router.Profile.Summary(router.Source.Id));
        }
    }
}
