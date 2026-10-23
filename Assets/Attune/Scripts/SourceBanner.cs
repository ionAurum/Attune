using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Says so for a few seconds at the top of the screen when the source or the profile changes:
    /// "Now playing with Pointer". The pitch's live swap (design, Live swap) needs the audience to
    /// see it. Made with the menus; the source the game starts with isn't announced.
    /// </summary>
    public class SourceBanner : MonoBehaviour
    {
        private const float ShowSeconds = 2.5f;

        private RectTransform _box;
        private TextMeshProUGUI _text;
        private InputRouter _router;
        private float _hideAt;

        /// <summary>What the banner last said, for the self-test.</summary>
        public static string LastText { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LastText = null;

        private void Start()
        {
            MenuStack menus = SessionMenu.Menus;
            _router = InputRouter.Instance;
            if (menus == null || _router == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            _box = UiKit.Pin(UiKit.Rect("Source banner", menus.transform), new Vector2(0.5f, 1f), new Vector2(900f, 80f), new Vector2(0f, -24f));
            UiKit.Shape(_box, theme.Accent, theme.PanelCorner);
            _text = UiKit.Text(_box, "", 34f, theme.OnAccent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Anchor(_text.rectTransform, 0f, 0f, 1f, 1f);
            _box.gameObject.SetActive(false);
            _router.SourceChanged += OnSourceChanged;
            _router.ProfileChanged += OnSourceChanged;
        }

        private void OnDestroy()
        {
            if (_router != null)
            {
                _router.SourceChanged -= OnSourceChanged;
                _router.ProfileChanged -= OnSourceChanged;
            }
        }

        private void Update()
        {
            if (_box != null && _box.gameObject.activeSelf)
            {
                _box.SetAsLastSibling();
                if (Time.unscaledTime >= _hideAt)
                {
                    _box.gameObject.SetActive(false);
                }
            }
        }

        private void OnSourceChanged()
        {
            LastText = $"{_router.Profile.Name}: now playing with {_router.Source.Name}";
            _text.text = LastText;
            _box.gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + ShowSeconds;
        }
    }
}
