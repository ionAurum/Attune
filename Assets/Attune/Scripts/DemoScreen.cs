using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// What every built demo shares (design, Demos; ruling A28: flat 2D, drawn in code): its own
    /// canvas under the menus, a header with the demo's name and how it stands, a hint line along
    /// the bottom, and the stage between them that the demo draws on. It reads channels from
    /// InputRouter and hands the demo only what is the demo's: nothing while a menu is open, no press
    /// that began in a menu, and not the Switch source's Long hold, which opens the session menu
    /// (ruling A20). Made by SceneBootstrap as the demo's scene loads.
    /// </summary>
    public abstract class DemoScreen : MonoBehaviour, IDemoScene
    {
        private readonly List<string> _gestures = new();
        private TextMeshProUGUI _status, _hint;

        /// <summary>The demo screen in the scene, or null.</summary>
        public static DemoScreen Current { get; private set; }

        /// <inheritdoc/>
        public int Presses { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<string> Gestures => _gestures;

        /// <summary>The session's input layer.</summary>
        protected InputRouter Router { get; private set; }

        /// <summary>The demo being played.</summary>
        protected DemoInfo Demo { get; private set; }

        /// <summary>The area between the header and the hint line, for the demo to draw on.</summary>
        protected RectTransform Stage { get; private set; }

        /// <summary>The whole canvas, for what follows Point across the screen.</summary>
        protected RectTransform ScreenRect { get; private set; }

        /// <summary>The demo's Id in DemoCatalogue.</summary>
        protected abstract string DemoId { get; }

        /// <summary>How the demo stands, for the header: "Flags 2 of 5".</summary>
        protected abstract string Status { get; }

        /// <summary>What the player does, for the hint line: "Press: sail where the needle points".</summary>
        protected abstract string ActionHint { get; }

        /// <summary>How long a press must last to open the session menu in play: the profile's Long hold.</summary>
        protected virtual float MenuHold(InputRouter router) => router.Timing.LongHoldSeconds;

        /// <summary>Where the stage's bottom edge is, as a share of the screen's height.</summary>
        protected virtual float StageBottom => 0.08f;

        /// <summary>How far in from each side the stage starts, as a share of the screen's width.</summary>
        protected virtual float StageSide => 0.03f;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        /// <summary>
        /// How long a press must last to open the session menu in play, with a source that scans the
        /// menus: the profile's Long hold (ruling A20), unless the demo under way asks for longer
        /// (the drift demos, for breath and hum: ruling A37).
        /// </summary>
        public static float MenuHoldSeconds(InputRouter router) =>
            Current != null ? Current.MenuHold(router) : router.Timing.LongHoldSeconds;

        /// <summary>True if the press that just ended, as kind, opens the session menu in play (ScanDriver opens it).</summary>
        public static bool OpensMenu(InputRouter router, PressKind kind) =>
            kind == PressKind.LongHold && router.Source.ScansMenus && router.LastPressSeconds >= MenuHoldSeconds(router);

        /// <summary>The hint for opening the menu in play: "Long hold: menu", or "Long breath (5 s): menu" when it takes longer than a Long hold.</summary>
        public static string MenuHint(InputRouter router)
        {
            float seconds = MenuHoldSeconds(router);
            string longHold = router.Source.Words.LongHold;
            return seconds > router.Timing.LongHoldSeconds + 0.05f
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} ({1:0} s): menu", longHold, seconds)
                : longHold + ": menu";
        }

        private void Awake()
        {
            Current = this;
            DemoSession.Scene = this;
        }

        private void Start()
        {
            Router = InputRouter.Instance;
            Demo = DemoCatalogue.Find(DemoId);
            if (Router == null || Demo == null)
            {
                return;
            }

            Build();
            Router.PressStarted += OnPressStarted;
            Router.Pressed += OnPressed;
            Router.Gestured += OnGestured;
            Router.SourceChanged += OnSourceChanged;
            Begin();
        }

        private void OnDestroy()
        {
            if (Router != null)
            {
                Router.PressStarted -= OnPressStarted;
                Router.Pressed -= OnPressed;
                Router.Gestured -= OnGestured;
                Router.SourceChanged -= OnSourceChanged;
                End();
            }

            if (Current == this)
            {
                Current = null;
            }

            if (ReferenceEquals(DemoSession.Scene, this))
            {
                DemoSession.Scene = null;
            }
        }

        //---------- What a demo fills in

        /// <summary>Draws the demo on Stage (and ScreenRect), once, as it starts.</summary>
        protected abstract void BuildStage();

        /// <summary>The demo starts, after BuildStage.</summary>
        protected virtual void Begin()
        {
        }

        /// <summary>The demo ends: the scene is unloading.</summary>
        protected virtual void End()
        {
        }

        /// <summary>A frame of play, with no menu open; dt in unscaled seconds.</summary>
        protected abstract void Play(float dt);

        /// <summary>A frame with a menu open over the demo.</summary>
        protected virtual void Paused()
        {
        }

        /// <summary>A press began, with no menu open.</summary>
        protected virtual void PressBegan()
        {
        }

        /// <summary>A press ended, with no menu open, that is the demo's.</summary>
        protected virtual void PressEnded(PressKind kind)
        {
        }

        /// <summary>A gesture, with no menu open.</summary>
        protected virtual void Gesture(string gesture)
        {
        }

        /// <summary>The source changed mid-demo (design, Live swap).</summary>
        protected virtual void SourceChanged()
        {
        }

        //----------

        private void Update()
        {
            if (Router == null || _status == null)
            {
                return;
            }

            string menu = Router.Source.ScansMenus ? MenuHint(Router) : "Escape: menu";
            SetText(_hint, $"{ActionHint}  ·  {menu}  ·  F2: next source");
            if (MenuFocus.IsAnyOpen)
            {
                Paused();
            }
            else
            {
                Play(Time.unscaledDeltaTime);
            }

            SetText(_status, Status); // after the frame's play, so it never lags what the stage shows
        }

        private void OnPressStarted()
        {
            if (!MenuFocus.IsAnyOpen)
            {
                PressBegan();
            }
        }

        private void OnPressed(PressKind kind)
        {
            if (MenuFocus.IsAnyOpen || Router.PressBeganInMenu || OpensMenu(Router, kind))
            {
                return;
            }

            Presses++;
            PressEnded(kind);
        }

        private void OnGestured(string gesture)
        {
            if (MenuFocus.IsAnyOpen)
            {
                return;
            }

            _gestures.Add(gesture);
            Gesture(gesture);
        }

        private void OnSourceChanged() => SourceChanged();

        private void Build()
        {
            UiTheme theme = UiKit.Theme;
            Canvas canvas = UiKit.CreateCanvas();
            canvas.sortingOrder = -10; // under the menus
            canvas.transform.SetParent(transform, false);
            ScreenRect = (RectTransform)canvas.transform;
            UiKit.Fill(UiKit.Anchor(UiKit.Rect("Backdrop", ScreenRect), 0f, 0f, 1f, 1f), theme.Background);

            TextMeshProUGUI title = UiKit.Title(ScreenRect, Demo.Name, 52f, theme.Accent, TextAlignmentOptions.MidlineLeft);
            UiKit.Anchor(title.rectTransform, 0.03f, 0.9f, 0.5f, 0.99f);
            _status = UiKit.Text(ScreenRect, "", 32f, theme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Anchor(_status.rectTransform, 0.5f, 0.9f, 0.97f, 0.99f);
            _hint = UiKit.Text(ScreenRect, "", 26f, theme.Muted, align: TextAlignmentOptions.Midline);
            UiKit.Anchor(_hint.rectTransform, 0.03f, 0f, 0.97f, 0.06f);

            Stage = UiKit.Anchor(UiKit.Rect("Stage", ScreenRect), StageSide, StageBottom, 1f - StageSide, 0.89f);
            BuildStage();
            TextFloor.Raise(ScreenRect);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text.text != value)
            {
                text.text = value;
            }
        }
    }
}
