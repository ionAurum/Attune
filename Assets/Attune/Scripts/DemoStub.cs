using System.Collections.Generic;
using System.Globalization;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// The scene every demo plays in until it is built: its name, what it needs and how the current
    /// source gives it, and the channels live (Level, Steer, Point, Press, Gesture, Select), read
    /// from InputRouter like any demo. It proves the hub reaches a demo, that a source changes
    /// mid-scene with no reload, and that the feedback question follows a pause. Made by
    /// SceneBootstrap as the stub scene loads; draws on its own canvas, under the menus.
    /// </summary>
    public class DemoStub : MonoBehaviour, IDemoScene
    {
        private InputRouter _router;
        private DemoInfo _demo;
        private TextMeshProUGUI _needs, _level, _steer, _press, _gesture, _select, _hint;
        private RectTransform _levelFill, _steerFill, _dot, _lamp;
        private UiShape _lampShape;
        private float _gestureFlash;

        /// <summary>The stub in the scene, or null.</summary>
        public static DemoStub Instance { get; private set; }

        /// <summary>Presses this visit has seen while no menu was open.</summary>
        public int Presses { get; private set; }

        /// <summary>Gestures this visit has seen while no menu was open.</summary>
        public List<string> Gestures { get; } = new();

        IReadOnlyList<string> IDemoScene.Gestures => Gestures;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            Instance = this;
            DemoSession.Scene = this;
        }

        private void Start()
        {
            _router = InputRouter.Instance;
            _demo = DemoSession.Current ?? DemoCatalogue.All[0];
            if (_router == null)
            {
                return;
            }

            Build();
            _router.Pressed += OnPressed;
            _router.Gestured += OnGestured;
            if (ChannelRoutes.Has(_demo.Needs.Named, Channels.Gesture))
            {
                _router.WantGestures(KeyboardZones.Quadrants);
            }
        }

        private void OnDestroy()
        {
            if (_router != null)
            {
                _router.Pressed -= OnPressed;
                _router.Gestured -= OnGestured;
                _router.WantGestures(null);
            }

            if (Instance == this)
            {
                Instance = null;
            }

            if (ReferenceEquals(DemoSession.Scene, this))
            {
                DemoSession.Scene = null;
            }
        }

        private void OnPressed(PressKind kind)
        {
            if (!MenuFocus.IsAnyOpen)
            {
                Presses++;
            }
        }

        private void OnGestured(string gesture)
        {
            if (!MenuFocus.IsAnyOpen)
            {
                Gestures.Add(gesture);
                _gestureFlash = 1f;
            }
        }

        //----------

        private void Update()
        {
            if (_router == null || _needs == null)
            {
                return;
            }

            // What it says about the source stays current; the readouts give way while a menu is open.
            _needs.text = NeedsText();
            _hint.text = (_router.Source.ScansMenus ? DemoScreen.MenuHint(_router) : "Escape: menu") + "  ·  F2: next source";
            if (MenuFocus.IsAnyOpen)
            {
                return;
            }

            CultureInfo c = CultureInfo.InvariantCulture;
            UiTheme theme = UiKit.Theme;
            _level.text = string.Format(c, "Level  {0:0.00}", _router.Level);
            SetFill(_levelFill, 0f, _router.Level);
            _steer.text = string.Format(c, "Steer  {0:+0.00;-0.00;0.00}", _router.Steer);
            float steer = Mathf.Clamp(_router.Steer, -1f, 1f);
            SetFill(_steerFill, Mathf.Min(0.5f, 0.5f + steer / 2f), Mathf.Max(0.5f, 0.5f + steer / 2f));
            _dot.anchorMin = _dot.anchorMax = _router.Point;
            _press.text = _router.IsPressDown
                ? string.Format(c, "Press  held {0:0.0} s", _router.PressHeldSeconds)
                : $"Press  up · last {(_router.LastPress.HasValue ? _router.LastPress.Value.ToString() : "none")} · {Presses} here";
            _lampShape.color = _router.IsPressDown ? theme.Accent : theme.Background;
            _gestureFlash = Mathf.Max(0f, _gestureFlash - Time.unscaledDeltaTime);
            _gesture.text = "Gesture  " + (_router.LastGesture ?? "none");
            _gesture.color = _gestureFlash > 0f ? theme.Accent : theme.Text;
            _select.text = $"Select  {_router.SelectCount}";
        }

        private string NeedsText()
        {
            InputSource source = _router.Source;
            var lines = new List<string> { $"Playing with {source.Name}" };
            foreach (Channels need in _demo.Needs.Needs)
            {
                foreach (Channels channel in ChannelRoutes.All)
                {
                    if (!ChannelRoutes.Has(need, channel))
                    {
                        continue;
                    }

                    string how = ChannelRoutes.How(channel, source.Supplies);
                    lines.Add($"{channel}: {how ?? "this source can't give it"}");
                    if (how != null)
                    {
                        break;
                    }
                }
            }

            return string.Join("\n", lines);
        }

        //----------

        private void Build()
        {
            UiTheme theme = UiKit.Theme;
            Canvas canvas = UiKit.CreateCanvas();
            canvas.sortingOrder = -10; // under the menus
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Backdrop", root), 0f, 0f, 1f, 1f), theme.Background);

            RectTransform left = Column(root, 0.04f, 0.5f);
            UiKit.Title(left, _demo.Name, 80f, theme.Accent);
            UiKit.Text(left, _demo.Line, 30f);
            UiKit.Text(left, _demo.IsStub
                ? $"This demo is built in {_demo.Milestone}. Until then, this page shows what Attune reads from you."
                : "", 26f, theme.Muted);
            UiKit.Spacer(left, 12f);
            _needs = UiKit.Text(left, "", 28f);
            UiKit.Spacer(left, 12f);
            _hint = UiKit.Text(left, "", 26f, theme.Muted);

            RectTransform right = Column(root, 0.54f, 0.96f);
            _level = UiKit.Text(right, "", 30f);
            _levelFill = Bar(right, theme);
            _steer = UiKit.Text(right, "", 30f);
            _steerFill = Bar(right, theme);
            RectTransform pointBox = UiKit.Rect("Point", right);
            pointBox.gameObject.AddComponent<LayoutElement>().preferredHeight = 240f;
            UiKit.Shape(pointBox, theme.Panel, theme.SmallCorner);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", pointBox), 0f, 0f, 1f, 1f), theme.Line, theme.SmallCorner, 3f);
            _dot = UiKit.Pin(UiKit.Rect("Dot", pointBox), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));
            UiKit.Shape(_dot, theme.Accent, 14f);
            RectTransform pressRow = UiKit.Row(right, 56f, 16f);
            pressRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            _lamp = UiKit.Rect("Lamp", pressRow);
            var lampSize = _lamp.gameObject.AddComponent<LayoutElement>();
            lampSize.preferredWidth = lampSize.minWidth = 56f;
            lampSize.flexibleWidth = 0f;
            _lampShape = UiKit.Shape(_lamp, theme.Background, 28f);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", _lamp), 0f, 0f, 1f, 1f), theme.Text, 28f, 4f);
            _press = UiKit.Text(pressRow, "", 30f);
            _press.alignment = TextAlignmentOptions.MidlineLeft;
            _press.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _gesture = UiKit.Text(right, "", 30f);
            _select = UiKit.Text(right, "", 30f);
        }

        private static RectTransform Column(RectTransform root, float from, float to)
        {
            RectTransform column = UiKit.Anchor(UiKit.Rect("Column", root), from, 0.08f, to, 0.92f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        private static RectTransform Bar(RectTransform column, UiTheme theme)
        {
            RectTransform bar = UiKit.Rect("Bar", column);
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            UiKit.Shape(bar, theme.Panel, theme.SmallCorner);
            RectTransform fill = UiKit.Anchor(UiKit.Rect("Fill", bar), 0f, 0f, 0f, 1f);
            UiKit.Shape(fill, theme.Accent, theme.SmallCorner);
            return fill;
        }

        private static void SetFill(RectTransform fill, float from, float to)
        {
            fill.anchorMin = new Vector2(Mathf.Clamp01(from), 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(to), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }
    }
}
