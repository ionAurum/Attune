using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Scans the menus while a source that scans them is active: the Switch, Breath or Hum (design,
    /// Scanning; ruling A36). The controls of the menu on top (or of the hub, with none over it) take
    /// the EventSystem's selection one at a time, ringed so it can't be missed, and a Tap or a Hold
    /// picks the ringed one (ruling A22); a Long hold goes back: it closes the top menu, or in a demo
    /// opens the session menu (ruling A20; longer in the drift demos for breath and hum, A37). The
    /// scan stands still while the press is down, so a press picks what was ringed when it began.
    /// Breath and Hum don't scan while the microphone has a problem, so the keyboard and the pointer
    /// still work the menus. The speed, the rounds before pausing and starting from the top again
    /// are the profile's. Every menu built with the UI kit
    /// is scanned this way without changing the framework. While it scans, the UI's own keyboard
    /// navigation and Submit are off, so the switch does one thing per press. Made with the menus.
    /// </summary>
    public class ScanDriver : MonoBehaviour
    {
        private const float RingPad = 10f;
        private const float RingWidth = 6f;
        private const float RowTolerance = 12f;

        private static double _holdOffUntil;

        private readonly Scanner _scanner = new();
        private readonly List<Selectable> _items = new();
        private readonly List<InputAction> _disabled = new();

        private Transform _root;
        private RectTransform _ring, _hintBox;
        private TextMeshProUGUI _hint;
        private InputRouter _subscribed;

        /// <summary>The session's scan driver, or null before the game has started.</summary>
        public static ScanDriver Instance { get; private set; }

        /// <summary>True while scanning is held off: while the player is choosing their switch.</summary>
        public static bool Suspended { get; set; }

        /// <summary>True while the menus are being scanned.</summary>
        public bool IsScanning { get; private set; }

        /// <summary>The scanner stepping through the controls.</summary>
        public Scanner Scanner => _scanner;

        /// <summary>The control ringed now, or null.</summary>
        public Selectable Highlighted => IsScanning && _scanner.Index >= 0 && _scanner.Index < _items.Count ? _items[_scanner.Index] : null;

        /// <summary>The controls being scanned, in order.</summary>
        public IReadOnlyList<Selectable> Items => _items;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Suspended = false;
            _holdOffUntil = 0;
        }

        /// <summary>Ignores the press under way, as the one that just chose the switch.</summary>
        public static void HoldOff() => _holdOffUntil = Time.unscaledTimeAsDouble + 0.05;

        //----------

        private void Awake() => Instance = this;

        private void Start()
        {
            MenuStack menus = SessionMenu.Menus;
            if (menus == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            _ring = UiKit.Rect("Scan ring", menus.transform);
            _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f);
            UiKit.Shape(_ring, theme.Accent, theme.ButtonCorner + RingPad, RingWidth);
            _ring.gameObject.SetActive(false);

            // The bottom-right corner is free on every screen but the hub, where it covers only the version.
            _hintBox = UiKit.Pin(UiKit.Rect("Scan hint", menus.transform), new Vector2(1f, 0f), new Vector2(620f, 64f), new Vector2(-24f, 24f));
            _hintBox.pivot = new Vector2(1f, 0f);
            _hintBox.anchoredPosition = new Vector2(-24f, 24f);
            UiKit.Shape(_hintBox, theme.Panel, theme.SmallCorner);
            _hint = UiKit.Text(_hintBox, "", 28f, theme.Text, align: TextAlignmentOptions.Center);
            UiKit.Anchor(_hint.rectTransform, 0f, 0f, 1f, 1f);
            _hintBox.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            SetUiInput(true);
            if (_subscribed != null)
            {
                _subscribed.Pressed -= OnPressed;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            InputRouter router = InputRouter.Instance;
            if (router != _subscribed)
            {
                if (_subscribed != null)
                {
                    _subscribed.Pressed -= OnPressed;
                }

                _subscribed = router;
                if (router != null)
                {
                    router.Pressed += OnPressed;
                }
            }

            bool active = ShouldScan(router);
            SetUiInput(!active);
            if (!active)
            {
                if (IsScanning)
                {
                    IsScanning = false;
                    _root = null;
                    _items.Clear();
                }

                Show(false);
                return;
            }

            TimingValues timing = router.Timing;
            _scanner.StepSeconds = timing.ScanStepSeconds;
            _scanner.Rounds = timing.ScanRounds;
            _scanner.FromTop = timing.ScanFromTop;

            Transform root = ScanRoot();
            if (root != _root || !SameItems(root))
            {
                _root = root;
                Collect(root, _items);
                _scanner.Reset(_items.Count);
                IsScanning = true;
                SelectHighlighted();
            }
            else if (!router.IsPressDown && _scanner.Advance(Time.unscaledDeltaTime))
            {
                SelectHighlighted();
            }

            ShowRing();
            ShowHint(router);
        }

        //----------

        private static bool ShouldScan(InputRouter router) =>
            ScansNow(router) && MenuFocus.IsAnyOpen && !Suspended && SessionMenu.Menus != null;

        // The active source scans the menus and can press: a microphone with a problem can't, and the
        // keyboard's navigation stays on for whoever helps.
        private static bool ScansNow(InputRouter router) =>
            router != null && router.Source != null && router.Source.ScansMenus && !(router.Source.UsesMicrophone && MicrophoneInput.Problem != null);

        private void OnPressed(PressKind kind)
        {
            InputRouter router = InputRouter.Instance;
            if (!ScansNow(router) || Suspended || router.PressStartedAt <= _holdOffUntil)
            {
                return;
            }

            MenuStack menus = SessionMenu.Menus;
            if (!MenuFocus.IsAnyOpen)
            {
                // In play, a Long hold opens the session menu (ruling A20; longer in the drift demos, A37); the demo has the rest.
                if (DemoSession.Current != null && SessionMenu.Instance != null && DemoScreen.OpensMenu(router, kind))
                {
                    SessionMenu.Instance.OpenMenu();
                }

                return;
            }

            if (kind == PressKind.LongHold)
            {
                Back(menus);
                return;
            }

            if ((kind != PressKind.Tap && kind != PressKind.Hold) || !IsScanning)
            {
                return;
            }

            int picked = _scanner.Press();
            if (picked >= 0 && picked < _items.Count && _items[picked] != null)
            {
                Activate(_items[picked]);
            }

            SelectHighlighted();
        }

        private static void Back(MenuStack menus)
        {
            TMP_Dropdown open = OpenDropdown(menus);
            if (open != null)
            {
                open.Hide();
            }
            else if (menus.Count > 0)
            {
                menus.Close();
            }
        }

        private static void Activate(Selectable control)
        {
            if (control is Slider slider)
            {
                float step = slider.wholeNumbers ? Mathf.Max(1f, Mathf.Round((slider.maxValue - slider.minValue) / 10f)) : (slider.maxValue - slider.minValue) / 10f;
                float next = slider.value + step;
                slider.value = next > slider.maxValue + step * 0.01f ? slider.minValue : Mathf.Min(next, slider.maxValue);
                return;
            }

            ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        // An open dropdown's list is scanned first; then the menu on top; then the hub.
        private static Transform ScanRoot()
        {
            MenuStack menus = SessionMenu.Menus;
            if (OpenDropdown(menus) != null)
            {
                Transform list = menus.transform.Find("Dropdown List");
                if (list != null)
                {
                    return list;
                }
            }

            if (menus.Top != null)
            {
                return menus.Top;
            }

            return menus.Screen;
        }

        private static TMP_Dropdown OpenDropdown(MenuStack menus)
        {
            Transform top = menus.Top != null ? menus.Top : menus.Screen;
            if (top == null)
            {
                return null;
            }

            foreach (TMP_Dropdown dropdown in top.GetComponentsInChildren<TMP_Dropdown>())
            {
                if (dropdown.IsExpanded)
                {
                    return dropdown;
                }
            }

            return null;
        }

        // The controls a switch can use, top to bottom, then left to right. Text fields are left out: they need typing.
        private static void Collect(Transform root, List<Selectable> into)
        {
            into.Clear();
            if (root == null)
            {
                return;
            }

            foreach (Selectable control in root.GetComponentsInChildren<Selectable>())
            {
                if (control.IsInteractable() && control.isActiveAndEnabled && !(control is TMP_InputField) && !(control is Scrollbar))
                {
                    into.Add(control);
                }
            }

            into.Sort((a, b) =>
            {
                Vector3 pa = a.transform.position, pb = b.transform.position;
                if (Mathf.Abs(pa.y - pb.y) > RowTolerance)
                {
                    return pb.y.CompareTo(pa.y);
                }

                return pa.x.CompareTo(pb.x);
            });
        }

        private bool SameItems(Transform root)
        {
            var now = new List<Selectable>();
            Collect(root, now);
            if (now.Count != _items.Count)
            {
                return false;
            }

            for (int i = 0; i < now.Count; i++)
            {
                if (now[i] != _items[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectHighlighted()
        {
            Selectable control = Highlighted;
            if (EventSystem.current != null && control != null && EventSystem.current.currentSelectedGameObject != control.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(control.gameObject);
            }
        }

        private void ShowRing()
        {
            Selectable control = Highlighted;
            bool show = control != null && !_scanner.IsPaused;
            _ring.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            var target = (RectTransform)control.transform;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var parent = (RectTransform)_ring.parent;
            Vector3 min = parent.InverseTransformPoint(corners[0]);
            Vector3 max = parent.InverseTransformPoint(corners[2]);
            _ring.anchoredPosition = (min + max) / 2f;
            _ring.sizeDelta = new Vector2(max.x - min.x, max.y - min.y) + Vector2.one * (RingPad * 2f);
            _ring.SetAsLastSibling();
        }

        private void ShowHint(InputRouter router)
        {
            PressWords words = router.Source.Words;
            string text;
            if (_scanner.IsPaused)
            {
                text = $"Scanning paused. {words.Tap} to go on.";
            }
            else if (router.IsPressDown && router.PressKindIfReleased == PressKind.LongHold)
            {
                text = $"{words.LetGo} to go back.";
            }
            else if (router.IsPressDown && router.PressKindIfReleased == PressKind.Hold)
            {
                text = $"{words.LetGo} to choose. {words.Keep} to go back.";
            }
            else
            {
                text = $"{words.Tap}: choose  ·  {words.LongHold}: back";
            }

            if (_hint.text != text)
            {
                _hint.text = text;
            }

            Show(true);
            _hintBox.SetAsLastSibling();
        }

        private void Show(bool show)
        {
            if (_ring != null && !show)
            {
                _ring.gameObject.SetActive(false);
            }

            if (_hintBox != null && _hintBox.gameObject.activeSelf != show)
            {
                _hintBox.gameObject.SetActive(show);
            }
        }

        // While scanning, the UI's keyboard navigation and Submit are off, and its click too when the switch is a mouse button.
        private void SetUiInput(bool on)
        {
            if (on)
            {
                foreach (InputAction action in _disabled)
                {
                    action?.Enable();
                }

                _disabled.Clear();
                return;
            }

            if (_disabled.Count > 0 || EventSystem.current == null)
            {
                return;
            }

            if (EventSystem.current.currentInputModule is InputSystemUIInputModule module)
            {
                Disable(module.move);
                Disable(module.submit);
                InputRouter router = InputRouter.Instance;
                if (router != null && router.Profile.SwitchControl.StartsWith("<Mouse>"))
                {
                    Disable(module.leftClick);
                }
            }
        }

        private void Disable(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            if (action != null && action.enabled)
            {
                action.Disable();
                _disabled.Add(action);
            }
        }
    }
}
