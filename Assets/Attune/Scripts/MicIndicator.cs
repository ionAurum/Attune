using Aurumworks;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// The indicator ruling A7 asks for: while the microphone listens, a badge at the top of the
    /// screen says so, in words and with a microphone drawn beside them, over everything, menus
    /// included. A microphone problem (none found, or blocked by Windows) shows under it in words a
    /// player can act on. Made with the menus.
    /// </summary>
    public class MicIndicator : MonoBehaviour
    {
        private RectTransform _badge, _problemBox;
        private TextMeshProUGUI _label, _problem;

        /// <summary>The session's indicator, or null before the game has started.</summary>
        public static MicIndicator Instance { get; private set; }

        /// <summary>True while the badge shows.</summary>
        public bool IsShowing => _badge != null && _badge.gameObject.activeSelf;

        /// <summary>What the badge says now, for the self-test.</summary>
        public string Text => _label != null ? _label.text : null;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake() => Instance = this;

        private void Start() => Build();

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_badge == null)
            {
                return;
            }

            bool on = MicrophoneInput.IsOn;
            if (_badge.gameObject.activeSelf != on)
            {
                _badge.gameObject.SetActive(on);
            }

            if (on && _label.text != MicrophoneInput.Label)
            {
                _label.text = MicrophoneInput.Label;
            }

            string problem = ProblemNow();
            bool showProblem = problem != null;
            if (_problemBox.gameObject.activeSelf != showProblem)
            {
                _problemBox.gameObject.SetActive(showProblem);
            }

            if (showProblem && _problem.text != problem)
            {
                _problem.text = problem;
            }
        }

        // A problem matters whenever a microphone source is active: its presses play the demos and scan the menus (ruling A36).
        private static string ProblemNow()
        {
            InputRouter router = InputRouter.Instance;
            return router != null && router.Source != null && router.Source.UsesMicrophone ? MicrophoneInput.Problem : null;
        }

        private void Build()
        {
            UiTheme theme = UiKit.Theme;
            Canvas canvas = UiKit.CreateCanvas();
            canvas.sortingOrder = 100; // over the menus
            canvas.transform.SetParent(transform, false);
            var screen = (RectTransform)canvas.transform;
            Destroy(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()); // it shows; it never takes a click

            _badge = UiKit.Pin(UiKit.Rect("Microphone indicator", screen), new Vector2(0.5f, 1f), new Vector2(380f, 60f), new Vector2(0f, -112f)); // under the source banner's place
            _badge.pivot = new Vector2(0.5f, 1f);
            UiKit.Shape(_badge, theme.Panel, 30f);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", _badge), 0f, 0f, 1f, 1f), theme.Accent, 30f, 4f);

            // A microphone: a capsule on a stand.
            RectTransform icon = UiKit.Anchor(UiKit.Rect("Icon", _badge), 0.04f, 0.12f, 0.14f, 0.88f);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Capsule", icon), 0.3f, 0.38f, 0.7f, 1f), theme.Accent, 10f);
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Stem", icon), 0.46f, 0.12f, 0.54f, 0.38f), theme.Accent);
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Base", icon), 0.25f, 0.04f, 0.75f, 0.14f), theme.Accent);

            _label = UiKit.Text(_badge, "Microphone on", 28f, theme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiKit.Anchor(_label.rectTransform, 0.17f, 0f, 0.98f, 1f);
            _badge.gameObject.SetActive(false);

            _problemBox = UiKit.Pin(UiKit.Rect("Microphone problem", screen), new Vector2(0.5f, 1f), new Vector2(900f, 120f), new Vector2(0f, -184f));
            _problemBox.pivot = new Vector2(0.5f, 1f);
            UiKit.Shape(_problemBox, theme.Panel, theme.PanelCorner);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", _problemBox), 0f, 0f, 1f, 1f), theme.Accent, theme.PanelCorner, 4f);
            _problem = UiKit.Text(_problemBox, "", 26f, theme.Text, align: TextAlignmentOptions.Center);
            UiKit.Anchor(_problem.rectTransform, 0.03f, 0.05f, 0.97f, 0.95f);
            _problemBox.gameObject.SetActive(false);
        }
    }
}
