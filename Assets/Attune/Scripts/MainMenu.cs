using Aurumworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Attune
{
    /// <summary>
    /// The hub (design, The hub), built in code in the menu shell's frame (MainMenuFrame) and drawn
    /// over the Hub scene: Demos, Who's playing and Pitch under the title, and along the bottom
    /// Settings, Feedback and Quit with the version. When the game starts, Who's playing opens over
    /// it so a profile is picked or made first. Choosing a demo hides it and loads the demo's scene;
    /// Quit to Hub (DemoSession) brings it back. While it shows it counts as an open menu
    /// (MenuFocus). With the Switch source it is scanned (ScanDriver): the hub is the first
    /// scanning demo. On the Menus prefab, with SessionMenu.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string _title = "Attune";
        [Tooltip("How far the scene behind the menu is darkened, so the menu reads over it (0 = not at all).")]
        [SerializeField, Range(0f, 1f)] private float _dim = 0.45f;

        private RectTransform _screen;
        private MainMenuFrame _frame;
        private TextMeshProUGUI _playing;
        private bool _askedWho;

        /// <summary>The hub, or null before the game has started.</summary>
        public static MainMenu Instance { get; private set; }

        /// <summary>True while the hub is what the player sees, with or without a menu over it.</summary>
        public static bool IsShowing => Instance != null && Instance._screen != null && Instance._screen.gameObject.activeSelf;

        /// <summary>The hub's screen, or null before it is built.</summary>
        public RectTransform Screen => _screen;

        private static MenuStack Menus => SessionMenu.Menus;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake() => Instance = this;

        // SessionMenu's Awake has set the theme, made the stack and the router.
        private void Start()
        {
            if (Menus == null)
            {
                return;
            }

            Build();
            if (InputRouter.Instance != null)
            {
                InputRouter.Instance.SourceChanged += ShowPlaying;
                InputRouter.Instance.ProfileChanged += ShowPlaying;
            }

            // A demo's scene opened straight in the Editor plays that demo, with no hub over it.
            if (SceneManager.GetActiveScene().name == DemoCatalogue.StubScene)
            {
                Hide();
                return;
            }

            Show();
        }

        private void OnDestroy()
        {
            MenuFocus.SetOpen(this, false);
            if (InputRouter.Instance != null)
            {
                InputRouter.Instance.SourceChanged -= ShowPlaying;
                InputRouter.Instance.ProfileChanged -= ShowPlaying;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        //----------

        /// <summary>Shows the hub over the scene, and the first time Who's playing over it.</summary>
        public void Show()
        {
            if (_screen == null)
            {
                return;
            }

            _screen.gameObject.SetActive(true);
            MenuFocus.SetOpen(this, true);
            Menus.Screen = _screen;
            ShowPlaying();
            MenuStack.SelectMainAction(_screen);
            if (!_askedWho)
            {
                _askedWho = true;
                ProfilesPage.Open();
            }
        }

        /// <summary>Hides the hub, as a demo starts.</summary>
        public void Hide()
        {
            if (_screen == null)
            {
                return;
            }

            _screen.gameObject.SetActive(false);
            MenuFocus.SetOpen(this, false);
            if (Menus.Screen == _screen)
            {
                Menus.Screen = null;
            }
        }

        /// <summary>Leaves the demo under way for the hub (the session menu's Quit to Hub).</summary>
        public void EndGame() => DemoSession.QuitToHub();

        //----------

        private void Build()
        {
            _screen = UiKit.Anchor(UiKit.Rect("MainMenu", Menus.transform), 0f, 0f, 1f, 1f);
            Color dim = UiKit.Theme.Background;
            dim.a = _dim;
            UiKit.Fill(_screen, dim);

            UiTheme theme = UiKit.Theme;
            _frame = MainMenuFrame.On(_screen, _title);
            RectTransform choices = _frame.Choices;
            UiKit.Text(choices, "Any action, tuned to you.", 36f, theme.Muted);
            _playing = UiKit.Text(choices, "", 28f, theme.Text);
            UiKit.Spacer(choices, 8f);
            UiKit.Button(choices, "Demos", () => DemosPage.Open(), 72f, theme.Accent);
            UiKit.Button(choices, "Who's playing", () => ProfilesPage.Open(), 64f);
            UiKit.Button(choices, "Pitch", OnPitch, 64f);
            _frame.FootButton("Settings", OnSettings, 190f);
            _frame.FootButton("Feedback", () => FeedbackPage.Open(), 200f);
            _frame.FootButton("Quit", OnQuit, 150f);
            TextFloor.Raise(_screen);
        }

        private void ShowPlaying()
        {
            InputRouter router = InputRouter.Instance;
            if (_playing == null || router == null || router.Source == null)
            {
                return;
            }

            _playing.text = $"{router.Profile.Name}, playing with {router.Source.Name}";
        }

        private static void OnPitch()
        {
            DemoInfo first = DemoCatalogue.Find(DemoCatalogue.Pitch[0]);
            InputRouter router = InputRouter.Instance;
            if (router != null && first != null && !first.Needs.CanBeMetBy(router.Supplied))
            {
                DemosPage.OfferOtherSources(first, () => DemoSession.Play(first, pitch: true));
                return;
            }

            DemoSession.Play(first, pitch: true);
        }

        private static void OnSettings()
        {
            if (SettingsMenu.Instance != null)
            {
                SettingsMenu.Instance.Open();
            }
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
