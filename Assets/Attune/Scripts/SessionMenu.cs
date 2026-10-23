using System.Collections.Generic;
using Aurumworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// The menu in a demo, built in code on the framework's menu shell (SessionMenuPage): where the
    /// demo stands, then Resume, Settings, Next source (design, Live swap), Recalibrate, What it
    /// hears (the overlay), Next, and Quit to Hub. The first time it opens in a visit to a demo,
    /// "How did that feel?" comes first (ruling A21). The UI Menu action (Escape, the gamepad's
    /// Start) opens it; with the Switch source a Long hold does (ruling A20). It doesn't pause
    /// anything; what reads input checks MenuFocus.IsAnyOpen.
    ///
    /// Also owns what every menu is drawn on and opens through: the Canvas, the EventSystem and the
    /// menu stack (Menus), and makes the input layer that lasts the session with them: InputRouter,
    /// ScanDriver, the overlay and the rest. No scene holds them: the Menus prefab in Resources,
    /// which this is on with MainMenu and SettingsMenu, is made as the game starts and outlives
    /// scene loads.
    /// </summary>
    public class SessionMenu : MonoBehaviour
    {
        private const string PrefabName = "Menus"; // in a Resources folder
        private const float DialogWidth = 0.5f; // wider than the kit's, for 24-point text (ruling A10)

        [Tooltip("How everything built in code looks: colours, corners, motion and sounds (Create > Aurumworks > " +
                 "UI Theme). Empty: the kit's plain default.")]
        [SerializeField] private UiTheme _theme;

        private SessionMenuPage _page; // the open menu, or null

        /// <summary>The stack every menu opens through, or null before the game has started.</summary>
        public static MenuStack Menus { get; private set; }

        /// <summary>The session menu's owner, or null before the game has started.</summary>
        public static SessionMenu Instance { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Menus = null;
            Instance = null;
        }

        // After the first scene's Awakes, whichever scene that is, and before any Start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var prefab = Resources.Load<SessionMenu>(PrefabName);
            if (prefab == null)
            {
                Debug.LogError($"No {PrefabName} prefab with a SessionMenu in a Resources folder: the game has no menus.");
                return;
            }

            GameObject menus = Instantiate(prefab).gameObject;
            DontDestroyOnLoad(menus);

            // The input layer lasts the session with the menus (design, Building in code).
            menus.AddComponent<InputRouter>();
            menus.AddComponent<ScanDriver>();
            menus.AddComponent<TextFloor>();
            menus.AddComponent<ChannelOverlay>();
            menus.AddComponent<GestureScanBar>();
            menus.AddComponent<SourceBanner>();
            menus.AddComponent<MicIndicator>();
        }

        //----------

        // Before anything is built: MainMenu builds in its Start.
        private void Awake()
        {
            Instance = this;
            if (_theme != null)
            {
                UiKit.Theme = _theme;
            }

            Canvas canvas = UiKit.CreateCanvas();
            canvas.transform.SetParent(transform, false);

            // The kit made the EventSystem in the scene, which loading another scene would destroy.
            if (EventSystem.current != null)
            {
                EventSystem.current.transform.SetParent(transform, false);
            }

            Menus = MenuStack.On((RectTransform)canvas.transform);
            Menus.DialogWidth = DialogWidth;
            Menus.MenuPressed += OpenMenu;
        }

        private void OnDestroy()
        {
            if (Menus != null && Menus.transform.IsChildOf(transform))
            {
                Menus = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        //----------

        /// <summary>
        /// Opens the session menu in a demo, with nothing else open: after the feedback question on a
        /// visit's first pause. On the hub there is no demo to come back from.
        /// </summary>
        public void OpenMenu()
        {
            if (_page != null || MainMenu.IsShowing || DemoSession.Current == null || Menus.Count > 0)
            {
                return;
            }

            if (!DemoSession.AskedThisVisit)
            {
                FeedbackPrompt.Open(OpenPage);
            }
            else
            {
                OpenPage();
            }
        }

        private void OpenPage()
        {
            if (_page != null || DemoSession.Current == null)
            {
                return;
            }

            _page = new SessionMenuPage
            {
                Status = StatusLines(),
                Settings = OnSettings,
                LeaveLabel = "Quit to Hub",
                Leave = DemoSession.QuitToHub,
                Closed = () => _page = null,
            };
            _page.Button("Next source", OnNextSource);
            _page.Button("Recalibrate", () => CalibrationPage.Open(() => _page?.SetStatus(StatusLines())));
            _page.Button(OverlayLabel(), OnOverlay);
            DemoInfo next = DemoSession.NextDemo;
            _page.Button(next != null ? "Next: " + next.Name : "Next: back to the hub", DemoSession.Next);
            _page.Open(Menus);
        }

        private static IEnumerable<string> StatusLines()
        {
            DemoInfo demo = DemoSession.Current;
            InputRouter router = InputRouter.Instance;
            var lines = new List<string>();
            if (demo != null)
            {
                lines.Add(demo.IsStub ? $"{demo.Name}: built in {demo.Milestone}" : demo.Name);
            }

            if (router != null)
            {
                lines.Add($"{router.Profile.Name}, playing with {router.Source.Name}");
            }

            return lines;
        }

        private static string OverlayLabel() =>
            ChannelOverlay.Instance != null && ChannelOverlay.Instance.IsShowing ? "What it hears: on" : "What it hears: off";

        private void OnNextSource()
        {
            InputRouter.Instance?.NextSource();
            _page?.SetStatus(StatusLines());
        }

        private void OnOverlay()
        {
            if (ChannelOverlay.Instance == null)
            {
                return;
            }

            string before = OverlayLabel();
            ChannelOverlay.Instance.Toggle();
            Button button = Menus.Top != null ? UiProbe.Find<Button>(Menus.Top, "Button: " + before) : null;
            if (button != null)
            {
                string after = OverlayLabel();
                button.name = "Button: " + after;
                button.GetComponentInChildren<TextMeshProUGUI>().text = after;
            }
        }

        private static void OnSettings()
        {
            if (SettingsMenu.Instance != null)
            {
                SettingsMenu.Instance.Open();
            }
        }
    }
}
