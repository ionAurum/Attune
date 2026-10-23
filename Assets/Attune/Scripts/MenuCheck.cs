using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Attune
{
    /// <summary>
    /// Static: the check (design, Checks), on the framework's self-test harness (SelfTest). A built
    /// player started with -attuneCheck plays itself: it walks the hub by keyboard, making and
    /// picking a profile; opens Settings and Timing; feeds the Scripted source and changes source
    /// mid-demo with the presenter's F2 (no reload); answers the feedback question and checks its
    /// row in -attuneFeedbackFolder; opens every demo; plays each built demo to its goal with the
    /// Scripted source (design, Demos: each demo's Check); feeds a test sound in place of the
    /// microphone through the Breath and Hum sources, calibrating a hum, checking the microphone is
    /// on while either is the source, with its indicator, and that a hum scans the menus; switches profiles and checks each brings
    /// back its source and calibration; then scans the hub to a demo with one key, goes back with a
    /// Long hold, plays the Keyboard Garden by scanning the gestures and the Dwell Builder by
    /// scanning its rows and cells. On the way it checks what the
    /// menu shell promises, that the mixer follows a volume, and that no text is under 24 points.
    /// It quits with exit code 1 if anything didn't hold or any error was logged.
    /// -attuneScreenshots folder saves a screenshot of each screen. It saves no setting.
    /// Tools/attunecheck.sh builds the player and runs it. Without the flag none of this runs.
    /// </summary>
    public static class MenuCheck
    {
        private const float WaitSeconds = 60f;
        private const Key SwitchKey = Key.K;

        private static bool _arrived;
        private static int _scenesLoaded;

        private static MenuStack Menus => SessionMenu.Menus;

        private static InputRouter Router => InputRouter.Instance;

        // Before the first scene loads, so the run answers for everything from the start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            if (!CommandLine.Has("-attuneCheck"))
            {
                return;
            }

            SelfTest.Begin();
            SelfTest.ScreenshotFolder = CommandLine.Value("-attuneScreenshots");
            QueuedInput.IgnoreFocus();
            _scenesLoaded = 0;
            SceneManager.sceneLoaded += (_, _) => _scenesLoaded++;
            SelfTest.Finishing += () => Debug.Log($"Check: {_scenesLoaded} scene loads; feedback written to {FeedbackLog.LastFile ?? "nothing"}");
            SelfTest.Run(Steps());
        }

        //----------

        private static IEnumerator Steps()
        {
            yield return HubByKeyboard();
            if (!_arrived)
            {
                yield break;
            }

            yield return SettingsAndTiming();
            yield return DemosByKeyboard();
            if (!_arrived)
            {
                yield break;
            }

            yield return ScriptedAndLiveSwap();
            yield return FeedbackAndSessionMenu();
            if (!_arrived)
            {
                yield break;
            }

            yield return EveryDemo();
            if (!_arrived)
            {
                yield break;
            }

            yield return CompassSweepEveryFlag();
            yield return KeyboardGardenPlantsAndGrows();
            yield return DwellBuilderByDwell();
            yield return KiteBreathEveryRibbon();
            yield return HumFlyerEveryRing();
            if (!_arrived)
            {
                yield break;
            }

            int back = _scenesLoaded;
            DemoSession.QuitToHub();
            yield return WaitFor(() => _scenesLoaded > back && MainMenu.IsShowing, "the hub after the demos");
            if (!_arrived)
            {
                yield break;
            }

            yield return MicrophoneSources();
            if (!_arrived)
            {
                yield break;
            }

            yield return ProfilesRestore();
            yield return ScanningWithOneKey();
            SelfTest.Finish();
        }

        // The hub, with Who's playing over it; a profile made and calibrated by keyboard.
        private static IEnumerator HubByKeyboard()
        {
            yield return WaitFor(() => Menus != null && MainMenu.IsShowing && Router != null && Menus.Count == 1, "the hub with Who's playing over it");
            if (!_arrived)
            {
                yield break;
            }

            // Whatever this machine's last profile was, the check starts on Keyboard zones with a profile of its own.
            Router.Use(new Profile { Name = "Check" });
            yield return null;
            Debug.Log($"Check: theme {UiKit.Theme.name}; scene {SceneManager.GetActiveScene().name}; Who's playing: {UiProbe.Texts(Menus.Top)}");
            SelfTest.Check(Menus.Top.name == "Profiles", "Who's playing opens over the hub as the game starts: " + Menus.Top.name);
            SelfTest.Check(MenuFocus.IsAnyOpen, "the hub holds the controls");
            CheckNavigation("Who's playing", Menus.Top);
            CheckTextSizes("Who's playing");
            yield return SelfTest.Capture("01-WhosPlaying");

            int before = Router.Store.Ids.Count;
            MenuStack.SelectMainAction(Menus.Top);
            yield return Choose("Button: New profile");
            SelfTest.Check(Menus.Top != null && Menus.Top.name == "Sources", "New profile leads to choosing a source");
            SelfTest.Check(Selected() == "Button: Switch", "for a new profile the Switch is the main action: " + Selected());
            CheckTextSizes("the source page");
            yield return SelfTest.Capture("02-Sources");
            yield return Choose("Button: Keyboard zones");
            SelfTest.Check(Menus.Top != null && Menus.Top.name == "Calibration", "choosing a source leads to calibrating it");
            Debug.Log("Check: calibration: " + UiProbe.Texts(Menus.Top));
            CheckTextSizes("calibration");
            yield return SelfTest.Capture("03-Calibration-Keyboard");
            yield return Choose("Button: Done");
            SelfTest.Check(Menus.Count == 0 && MainMenu.IsShowing, "Done goes back to the hub");
            SelfTest.Check(Router.Store.Ids.Count == before + 1 && Router.Profile.Id > 0, "a profile was made and saved: " + Router.Profile.Name);
            SelfTest.Check(Router.Source.Id == SourceIds.Keyboard && Router.Store.Load(Router.Profile.Id)?.Source == SourceIds.Keyboard,
                "the new profile plays with Keyboard zones, and keeps it");
            SelfTest.Check(Selected() == "Button: Demos", "Demos has the selection on the hub: " + Selected());
            CheckNavigation("the hub", MainMenu.Instance.Screen);
            CheckTextSizes("the hub");
            yield return SelfTest.Capture("04-Hub");

            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && MainMenu.IsShowing, "Escape on the hub opens no session menu");
        }

        // Settings over the hub, with the profile's rows; Timing over that; the mixer follows a volume.
        private static IEnumerator SettingsAndTiming()
        {
            yield return Choose("Button: Settings");
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Settings", "Settings opens over the hub");
            if (Menus.Count != 1)
            {
                yield break;
            }

            Debug.Log("Check: Settings: " + UiProbe.Texts(Menus.Top));
            SelfTest.Check(!UiProbe.HasInteractable(MainMenu.Instance.Screen), "nothing under Settings can be used");
            CheckNavigation("Settings", Menus.Top);
            SelfTest.Check(UiProbe.Find<Toggle>(Menus.Top, "Toggle: Fullscreen") != null, "Settings has the Fullscreen toggle");
            SelfTest.Check(Settings.Volumes.Count == 3 && Menus.Top.GetComponentsInChildren<Slider>().Length == 3,
                "Settings has a slider for each of the three volumes");
            SelfTest.Check(UiProbe.Find<Button>(Menus.Top, "Button: Source: Keyboard zones") != null &&
                           UiProbe.Find<Button>(Menus.Top, "Button: Recalibrate") != null &&
                           UiProbe.Find<Button>(Menus.Top, "Button: Timing") != null &&
                           Menus.Top.GetComponentInChildren<TMP_InputField>() != null,
                "Settings has the profile's rows: name, source, recalibrate, timing");
            CheckTextSizes("Settings");
            yield return SelfTest.Capture("05-Settings");

            CheckMixer("at the start");
            if (Settings.Volumes.Count > 0)
            {
                VolumeSetting first = Settings.Volumes[0];
                float volume = first.Value;
                first.Value = 0.5f;
                CheckMixer("with " + first.Name + " at 50%");
                first.Value = volume;
                CheckMixer("with " + first.Name + " back");
            }

            Press(Menus.Top, "Button: Timing");
            yield return null;
            SelfTest.Check(Menus.Count == 2 && Menus.Top.name == "Timing", "Timing opens over Settings");
            SelfTest.Check(Menus.Top.GetComponentsInChildren<Slider>().Length == 8 && Menus.Top.GetComponentInChildren<Toggle>() != null,
                "Timing has a slider for each timing value and the scan-from-the-top toggle");
            SelfTest.Check(UiProbe.Find<Toggle>(Menus.Top, "Toggle: Noisy room") != null, "Timing has the Noisy room toggle (ruling A35)");
            CheckNavigation("Timing", Menus.Top);
            CheckTextSizes("Timing");
            UiProbe.CheckWheel(Menus.Top, "Timing");
            yield return SelfTest.Capture("06-Timing");

            yield return QueuedInput.Press(GamepadButton.East);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Settings", "the gamepad's East closes Timing only");
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && MainMenu.IsShowing, "Escape closes Settings, back to the hub");
            SelfTest.Check(Selected() == "Button: Demos", "Demos has the selection again: " + Selected());

            Press(MainMenu.Instance.Screen, "Button: Feedback");
            yield return null;
            SelfTest.Check(Menus.Count == 1 && UiProbe.Texts(Menus.Top).Contains(Path.GetFileName(FeedbackLog.Folder ?? "(none")),
                "the Feedback page says where the file is: " + FeedbackLog.Folder);
            CheckTextSizes("the Feedback page");
            yield return SelfTest.Capture("07-FeedbackPage");
            yield return QueuedInput.Press(Key.Escape);
        }

        // Demos, by keyboard: every tile, one that can't be played saying why, and into the first demo's stub.
        private static IEnumerator DemosByKeyboard()
        {
            MenuStack.SelectMainAction(MainMenu.Instance.Screen);
            yield return Choose("Button: Demos");
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Demos", "Demos opens over the hub");
            if (Menus.Count != 1)
            {
                _arrived = false;
                SelfTest.Finish();
                yield break;
            }

            RectTransform page = Menus.Top;
            int tiles = DemoCatalogue.All.Count(demo => UiProbe.Find<Button>(page, "Button: " + demo.Name) != null);
            SelfTest.Check(tiles == 10, $"Demos has a tile for each of the ten demos: {tiles}");
            string lighthouse = UiProbe.Texts(UiProbe.Find<Button>(page, "Button: Lighthouse Keeper").transform);
            SelfTest.Check(lighthouse.Contains("can't play this") && lighthouse.Contains("Point"), "a tile Keyboard zones can't play says why: " + lighthouse);
            string dwell = UiProbe.Texts(UiProbe.Find<Button>(page, "Button: Dwell Builder").transform);
            SelfTest.Check(!dwell.Contains("can't play this") && dwell.Contains("Press"), "Keyboard zones plays the Dwell Builder by pressing (ruling A29): " + dwell);
            SelfTest.Check(Selected() == "Button: Compass Sweep", "the first tile has the selection: " + Selected());
            CheckNavigation("Demos", page);
            CheckTextSizes("Demos");
            yield return SelfTest.Capture("08-Demos");

            int loadsBefore = _scenesLoaded;
            yield return Choose("Button: Compass Sweep");
            yield return WaitFor(() => _scenesLoaded > loadsBefore && DemoScreen.Current is CompassSweep, "the Compass Sweep");
            if (!_arrived)
            {
                yield break;
            }

            yield return null;
            SelfTest.Check(!MainMenu.IsShowing && !MenuFocus.IsAnyOpen && DemoSession.Current?.Id == "CompassSweep",
                "the demo plays, with the hub hidden and the controls given back");
            CheckTextSizes("the Compass Sweep");
            yield return SelfTest.Capture("09-CompassSweep");
        }

        // The Scripted source feeds every channel; then F2 changes the source mid-demo, with no reload.
        private static IEnumerator ScriptedAndLiveSwap()
        {
            Router.SetSource(SourceIds.Scripted);
            var scripted = Router.Find<ScriptedSource>();
            scripted.Play(new[]
            {
                new ScriptedSource.Key(0.05f, Channels.Level, 0.8f),
                new ScriptedSource.Key(0.05f, Channels.Steer, -0.5f),
                new ScriptedSource.Key(0.05f, Channels.Point, 0.2f, 0.7f),
                new ScriptedSource.Key(0.1f, Channels.Press, 1f),
                new ScriptedSource.Key(0.25f, Channels.Press, 0f),
                new ScriptedSource.Key(0.3f, Channels.Gesture, 0f, name: KeyboardZones.TopRight),
            });
            int selects = Router.SelectCount;
            yield return new WaitForSecondsRealtime(0.8f);
            SelfTest.Check(Mathf.Abs(Router.Level - 0.8f) < 0.01f && Mathf.Abs(Router.Steer + 0.5f) < 0.01f,
                $"the Scripted source's Level and Steer come through: {Router.Level:0.00}, {Router.Steer:0.00}");
            SelfTest.Check(Vector2.Distance(Router.Point, new Vector2(0.2f, 0.7f)) < 0.01f, "its Point comes through: " + Router.Point);
            SelfTest.Check(Router.LastPress == PressKind.Tap && Router.SelectCount == selects + 1 && DemoSession.Scene.Presses >= 1,
                "its press is a Tap, a Select, and the demo sees it");
            SelfTest.Check(Router.LastGesture == KeyboardZones.TopRight && DemoSession.Scene.Gestures.Contains(KeyboardZones.TopRight),
                "its gesture comes through");
            yield return SelfTest.Capture("10-Scripted");

            IDemoScene scene = DemoSession.Scene;
            int loads = _scenesLoaded;
            string from = Router.Source.Id;
            yield return QueuedInput.Press(Key.F2);
            yield return null;
            SelfTest.Check(Router.Source.Id != from, $"F2 changes the source mid-demo: {from} to {Router.Source.Id}");
            SelfTest.Check(_scenesLoaded == loads && ReferenceEquals(DemoSession.Scene, scene), "with no reload");
            SelfTest.Check(SourceBanner.LastText != null && SourceBanner.LastText.Contains(Router.Source.Name), "and says so: " + SourceBanner.LastText);
            yield return SelfTest.Capture("11-LiveSwap");
            Router.SetSource(SourceIds.Keyboard);
        }

        // The first pause asks how it felt; the answer is a row in the check's folder; the next pause doesn't ask.
        private static IEnumerator FeedbackAndSessionMenu()
        {
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Feedback question", "the first pause in a visit asks how it felt");
            if (Menus.Count != 1)
            {
                _arrived = false;
                SelfTest.Finish();
                yield break;
            }

            SelfTest.Check(FeedbackLog.Answers.All(answer => UiProbe.Find<Button>(Menus.Top, "Button: " + answer) != null) &&
                           UiProbe.Find<Button>(Menus.Top, "Button: Skip") != null, "it offers Easy, Tiring, Frustrating, Fun and Skip");
            CheckTextSizes("the feedback question");
            yield return SelfTest.Capture("12-Feedback");
            Press(Menus.Top, "Button: Fun");
            yield return null;
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "SessionMenu", "after the answer, the session menu");
            CheckFeedbackRow("Fun");

            RectTransform session = Menus.Top;
            Debug.Log("Check: session menu: " + UiProbe.Texts(session));
            SelfTest.Check(Selected() == "Button: Resume", "Resume has the selection: " + Selected());
            CheckNavigation("the session menu", session);
            CheckTextSizes("the session menu");
            yield return SelfTest.Capture("13-SessionMenu");

            Press(session, "Button: What it hears: off");
            yield return null;
            SelfTest.Check(ChannelOverlay.Instance.IsShowing && UiProbe.Find<Button>(session, "Button: What it hears: on") != null,
                "What it hears shows the overlay, and says so");
            string before = Router.Source.Id;
            Press(session, "Button: Next source");
            yield return null;
            SelfTest.Check(Router.Source.Id != before && UiProbe.Texts(session).Contains(Router.Source.Name),
                "Next source changes the source, and the menu says so: " + Router.Source.Name);
            yield return QueuedInput.Press(Key.Escape);
            SelfTest.Check(Menus.Count == 0 && !MenuFocus.IsAnyOpen, "Escape closes the session menu");
            yield return new WaitForSecondsRealtime(0.3f);
            CheckTextSizes("the overlay");
            yield return SelfTest.Capture("14-Overlay");

            yield return QueuedInput.Press(GamepadButton.Start);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "SessionMenu", "the next pause goes straight to the session menu (ruling A21)");
            Press(Menus.Top, "Button: What it hears: on");
            yield return null;
            SelfTest.Check(!ChannelOverlay.Instance.IsShowing, "What it hears hides the overlay again");

            int loads = _scenesLoaded;
            Press(Menus.Top, "Button: Quit to Hub");
            yield return WaitFor(() => _scenesLoaded > loads && MainMenu.IsShowing, "the hub after quitting to it");
            if (!_arrived)
            {
                yield break;
            }

            yield return null;
            SelfTest.Check(Menus.Count == 0 && MenuFocus.IsAnyOpen && DemoSession.Current == null, "back on the hub, which holds the controls");
            SelfTest.Check(EventSystem.current != null, "the EventSystem outlived the scene");
            Router.SetSource(SourceIds.Keyboard);
        }

        // Every demo opens, in its own scene or the stub, played by the Scripted source; each built one's text is 24 points or more.
        private static IEnumerator EveryDemo()
        {
            Router.SetSource(SourceIds.Scripted);
            foreach (DemoInfo demo in DemoCatalogue.All)
            {
                yield return PlayDemo(demo.Id);
                if (!_arrived)
                {
                    yield break;
                }

                SelfTest.Check(demo.IsStub ? DemoStub.Instance != null : DemoScreen.Current != null && SceneManager.GetActiveScene().name == demo.Scene,
                    $"{demo.Name} plays in {(demo.IsStub ? "the stub" : "its own scene")}");
                if (!demo.IsStub)
                {
                    CheckTextSizes(demo.Name);
                }
            }
        }

        // Compass Sweep (design, Demos, 1): scripted presses at the right moments reach every flag.
        private static IEnumerator CompassSweepEveryFlag()
        {
            yield return PlayDemo("CompassSweep");
            if (!_arrived || DemoScreen.Current is not CompassSweep sweep)
            {
                yield break;
            }

            TimingValues timing = Router.Timing;
            float step = timing.ScanStepSeconds;
            timing.ScanStepSeconds = 0.25f;
            ScriptedSource scripted = Router.Find<ScriptedSource>();
            CompassMap map = sweep.Map;
            SelfTest.Check(sweep.Needle.Open.Count == 3 && sweep.Needle.Heading == 0, "the needle sweeps the three headings open from the start, from north");
            yield return SelfTest.Capture("D1-CompassSweep");

            int steps = 0, missed = 0;
            for (int flag = 0; flag < map.Flags.Count && missed == 0; flag++)
            {
                if (map.IsReached(flag))
                {
                    continue;
                }

                List<int> path = map.PathTo(map.Flags[flag].X, map.Flags[flag].Y);
                foreach (int heading in path ?? new List<int>())
                {
                    float start = Time.realtimeSinceStartup;
                    while ((sweep.Needle.Heading != heading || sweep.Needle.IsWaiting) && Time.realtimeSinceStartup - start < 20f)
                    {
                        if (sweep.Needle.IsWaiting)
                        {
                            yield return ScriptedTap(scripted); // wakes it
                        }
                        else
                        {
                            yield return null;
                        }
                    }

                    var at = map.Boat;
                    yield return ScriptedTap(scripted);
                    steps++;
                    if (map.Boat == at)
                    {
                        missed++;
                        break;
                    }
                }

                if (flag == 0)
                {
                    SelfTest.Check(map.IsReached(0), $"the boat reaches flag 1 in {steps} steps");
                    yield return SelfTest.Capture("D2-CompassSweep-Flag1");
                }
            }

            SelfTest.Check(missed == 0 && map.AllReached && sweep.Rounds == 1,
                $"scripted presses at the right moments reach every flag: {map.FlagsReached} of {map.Flags.Count} in {steps} steps");
            yield return SelfTest.Capture("D3-CompassSweep-EveryFlag");
            timing.ScanStepSeconds = step;
        }

        // Keyboard Garden (design, Demos, 2): each scripted gesture plants in its own bed; a hold grows the plant.
        private static IEnumerator KeyboardGardenPlantsAndGrows()
        {
            yield return PlayDemo("KeyboardGarden");
            if (!_arrived || DemoScreen.Current is not KeyboardGarden garden)
            {
                yield break;
            }

            ScriptedSource scripted = Router.Find<ScriptedSource>();
            GardenBeds beds = garden.Garden;
            SelfTest.Check(Router.GestureScan == null, "the Scripted source makes gestures itself, so nothing is scanned");
            bool own = true;
            for (int bed = 0; bed < KeyboardZones.Quadrants.Length; bed++)
            {
                scripted.Gesture(KeyboardZones.Quadrants[bed]);
                yield return null;
                yield return null;
                own &= beds.PlantsIn(bed) == 1 && beds.CurrentBed == bed;
            }

            SelfTest.Check(own && beds.Plants == 4, $"each scripted gesture plants in its own bed: {beds.Plants} plants");

            float before = beds.CurrentHeight;
            float half = Router.Timing.LongHoldSeconds / 2f;
            scripted.Press(true);
            yield return new WaitForSecondsRealtime(half);
            scripted.Press(false);
            yield return null;
            yield return null;
            float expected = before + (1f - GardenBeds.Seedling) / 2f;
            SelfTest.Check(Mathf.Abs(beds.CurrentHeight - expected) < 0.12f,
                $"a hold of half the Long hold grows the newest plant halfway (ruling A30): {before:0.00} to {beds.CurrentHeight:0.00}");

            // A zone held: it plants, and the hold grows that plant to full by the Long hold.
            scripted.Gesture(KeyboardZones.TopLeft);
            scripted.Press(true);
            yield return new WaitForSecondsRealtime(Router.Timing.LongHoldSeconds + 0.2f);
            yield return SelfTest.Capture("D4-KeyboardGarden");
            scripted.Press(false);
            yield return null;
            yield return null;
            SelfTest.Check(beds.CurrentBed == 0 && beds.PlantsIn(0) == 2 && beds.CurrentHeight >= 1f && beds.FullGrown == 1,
                $"a gesture held plants and grows its plant to full: {beds.PlantsIn(0)} in the top left, {beds.FullGrown} full grown");
        }

        // Dwell Builder (design, Demos, 3): a scripted path places the planned blocks; resting in the rest zone places nothing.
        private static IEnumerator DwellBuilderByDwell()
        {
            yield return PlayDemo("DwellBuilder");
            if (!_arrived || DemoScreen.Current is not DwellBuilder builder)
            {
                yield break;
            }

            ScriptedSource scripted = Router.Find<ScriptedSource>();
            TimingValues timing = Router.Timing;
            float dwellSeconds = timing.DwellSeconds;
            timing.DwellSeconds = 0.6f;
            const float rest = 1.2f;
            BuildPlate plate = builder.Plate;
            SelfTest.Check(builder.IsDwelling, "the Scripted source points, so the Dwell Builder dwells");

            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.PaletteTarget(2)), rest);
            SelfTest.Check(plate.Picked == 2, "resting on Water picks it: " + BuildPlate.BlockNames[plate.Picked]);
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(2, 3)), rest);
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.PaletteTarget(3)), rest);
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(5, 6)), rest);
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(6, 6)), 0.2f);
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(6, 6)) + new Vector2(0.002f, 0f), rest); // a tremor within the cell
            SelfTest.Check(plate.At(2, 3) == 2 && plate.At(5, 6) == 3 && plate.At(6, 6) == 3 && plate.Placed == 3,
                $"a scripted path places the planned blocks: {plate.Placed} placed");
            yield return SelfTest.Capture("D5-DwellBuilder");

            // Moving away cancels: a short rest on a cell, then off it, places nothing.
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(0, 0)), 0.3f);
            yield return RestOn(scripted, new Vector2(0.5f, 0.01f), 0.1f);
            SelfTest.Check(plate.At(0, 0) == BuildPlate.Empty, "moving away cancels the dwell");

            // The rest zone: the border never picks, however long the point rests there.
            int placed = plate.Placed;
            yield return RestOn(scripted, new Vector2(0.01f, 0.5f), rest * 2f);
            yield return SelfTest.Capture("D6-DwellBuilder-Rest");
            yield return RestOn(scripted, new Vector2(0.5f, 0.99f), rest);
            SelfTest.Check(plate.Placed == placed && plate.Picked == 3 && Router.Dwell != null && Router.Dwell.Progress == 0f,
                "resting in the rest zone places and picks nothing");

            // Resting again on a cell holding the picked block takes it off.
            yield return RestOn(scripted, builder.CentreOf(DwellBuilder.CellTarget(6, 6)), rest);
            SelfTest.Check(plate.At(6, 6) == BuildPlate.Empty, "resting on a cell holding the picked block takes it off");
            timing.DwellSeconds = dwellSeconds;
        }

        // Kite Breath (design, Demos, 4): the kite falls gently; a scripted level curve touches every ribbon.
        private static IEnumerator KiteBreathEveryRibbon()
        {
            yield return PlayDemo("KiteBreath");
            if (!_arrived || DemoScreen.Current is not KiteBreath kite)
            {
                yield break;
            }

            ScriptedSource scripted = Router.Find<ScriptedSource>();
            var middle = new Vector2(0.5f, 0.5f);
            SelfTest.Check(!MicrophoneInput.IsOn && !MicIndicator.Instance.IsShowing, "the Scripted source flies the kite with the microphone off");
            yield return SelfTest.Capture("D8-KiteBreath");

            scripted.Set(1f, 0f, middle);
            yield return Until(() => kite.Flight.Height >= 0.99f, "a full Level lifts the kite to the top");
            scripted.Set(0f, 0f, middle);
            yield return new WaitForSecondsRealtime(0.5f);
            float height = kite.Flight.Height;
            SelfTest.Check(height > 0.7f && height < 0.95f, $"let go, it falls gently: at {height:0.00} half a second later");

            yield return FlyEveryMark(kite, scripted, "a scripted level curve touches every ribbon");
            yield return SelfTest.Capture("D9-KiteBreath-EveryRibbon");
        }

        // Hum Flyer (design, Demos, 5): a scripted level path passes every ring.
        private static IEnumerator HumFlyerEveryRing()
        {
            yield return PlayDemo("HumFlyer");
            if (!_arrived || DemoScreen.Current is not HumFlyer flyer)
            {
                yield break;
            }

            ScriptedSource scripted = Router.Find<ScriptedSource>();
            yield return SelfTest.Capture("D10-HumFlyer");
            yield return FlyEveryMark(flyer, scripted, "a scripted level path passes every ring");
            yield return SelfTest.Capture("D11-HumFlyer-EveryRing");
        }

        // The Scripted source aims the flyer at each mark in turn, with the marks drifting three times as fast.
        private static IEnumerator FlyEveryMark(DriftDemo demo, ScriptedSource scripted, string what)
        {
            var middle = new Vector2(0.5f, 0.5f);
            demo.SpeedScale = 3f;
            float start = Time.realtimeSinceStartup;
            while (!demo.Course.AllReached && Time.realtimeSinceStartup - start < 40f)
            {
                int next = demo.Course.Next;
                scripted.Set(next >= 0 ? demo.Course.Height(next) : 0f, 0f, middle);
                yield return null;
            }

            SelfTest.Check(demo.Course.AllReached && demo.Rounds == 1,
                $"{what}: {demo.Course.Reached} of {demo.Course.Count} in {Time.realtimeSinceStartup - start:0} s");
            SelfTest.Progress();
            demo.SpeedScale = 1f;
            scripted.Set(0f, 0f, middle);
        }

        // Breath and Hum through their real pipeline, on a test sound in place of the microphone (the
        // check needs none): the microphone on while either is the source, with the indicator (rulings
        // A7, A36); a hum calibrated from the lowest note to the highest, the scan held while it
        // measures; a hum is a press, and silence glides the Level down (A34); a hum scans the menus,
        // and opens the menu in the Hum Flyer only when it lasts longer (A37); a breath raises the
        // Level and a puff is a press.
        private static IEnumerator MicrophoneSources()
        {
            var sound = new TestToneFeed();
            MicrophoneInput.Use(sound);
            sound.Noise(0.0005f);
            Router.SetSource(SourceIds.Hum, save: false);
            yield return null;
            yield return null;
            SelfTest.Check(MicrophoneInput.IsOn && MicIndicator.Instance.IsShowing && ScanDriver.Instance.IsScanning,
                "on the hub, the Hum source turns the microphone on, with the indicator, and scans the menus (ruling A36)");
            yield return SelfTest.Capture("22-Hub-Hum");

            CalibrationPage.Open();
            yield return null;
            yield return null;
            SelfTest.Check(MicrophoneInput.IsOn && MicIndicator.Instance.IsShowing && MicIndicator.Instance.Text == "Test sound playing",
                "calibrating Hum turns the microphone on, and the indicator says so: " + MicIndicator.Instance.Text);
            CheckTextSizes("calibrating Hum");
            yield return SelfTest.Capture("20-Calibration-Hum");
            CalibrationRun run = Menus.Top != null ? Menus.Top.GetComponentInChildren<CalibrationRun>() : null;
            if (run == null)
            {
                SelfTest.Check(false, "the calibration dialog has its run");
                yield break;
            }

            Press(Menus.Top, "Button: Start");
            yield return Until(() => run.StepName == "ReachLow", "the Rest step hears the room");
            sound.Tone(110f, 0.3f, 0.0005f);
            yield return Until(() => run.StepName == "ReachHigh", "the lowest note is heard");
            sound.Tone(220f, 0.3f, 0.0005f);
            yield return Until(() => run.StepName == "Done", "the highest note is heard");
            sound.Noise(0.0005f);
            yield return new WaitForSecondsRealtime(0.4f);
            SelfTest.Check(Menus.Top != null && Menus.Top.name == "Calibration" && run.StepName == "Done" && !ScanDriver.Suspended,
                "the long hum measured neither went back nor picked: the scan waited while the steps measured (ruling A36)");
            AxisCalibration level = Router.CalibrationOf("Level");
            AxisCalibration quiet = Router.Profile.Calibration(SourceIds.Hum, HumSource.QuietAxis);
            SelfTest.Check(level.IsSet && Mathf.Abs(level.Rest - 12f) < 0.3f && Mathf.Abs(level.High - 24f) < 0.3f,
                "Hum calibrates from the lowest note (110 Hz, 12 semitones) to the highest (220 Hz, 24): " + level.Summary());
            SelfTest.Check(quiet != null && quiet.IsSet && quiet.Rest < -60f && quiet.Rest > -80f,
                "and keeps the room's loudness: " + (quiet != null ? quiet.Summary() : "none"));
            yield return SelfTest.Capture("21-Calibration-Hum-Done");
            Press(Menus.Top, "Button: Done");
            yield return null;
            yield return null;
            SelfTest.Check(Menus.Count == 0 && MicrophoneInput.IsOn && MicIndicator.Instance.IsShowing, "Done closes it, and the microphone stays on (ruling A36)");

            yield return PlayDemo("HumFlyer");
            if (!_arrived || DemoScreen.Current is not HumFlyer flyer)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.4f);
            SelfTest.Check(MicrophoneInput.IsOn && MicIndicator.Instance.IsShowing, "in a demo, the microphone is on with the Hum source");

            // The Level may still be gliding down from calibration's highest note (ruling A34).
            yield return new WaitForSecondsRealtime(Router.Timing.FallSeconds);
            SelfTest.Check(Router.Level < 0.05f && !Router.IsPressDown, $"the room alone is no hum: Level {Router.Level:0.00}");
            sound.Tone(110f, 0.3f, 0.0005f);
            yield return new WaitForSecondsRealtime(0.8f);
            SelfTest.Check(Router.Level < 0.15f && Router.IsPressDown && Mathf.Abs(Router.Raw.Pitch - 110f) < 3f,
                $"the lowest note is Level 0, and a hum is a press: Level {Router.Level:0.00} at {Router.Raw.Pitch:0} Hz");
            sound.Tone(220f, 0.3f, 0.0005f);
            yield return new WaitForSecondsRealtime(0.8f);
            float high = Router.Level;
            SelfTest.Check(high > 0.85f, $"the highest note is full: Level {high:0.00} at {Router.Raw.Pitch:0} Hz");
            yield return SelfTest.Capture("D12-HumFlyer-Hum");
            sound.Noise(0.0005f);
            yield return new WaitForSecondsRealtime(0.4f);
            float gliding = Router.Level;
            SelfTest.Check(!Router.IsPressDown && gliding > 0.4f && gliding < high,
                $"silence lets go of the press and glides the Level down, not at once (ruling A34): {high:0.00} to {gliding:0.00}");
            yield return new WaitForSecondsRealtime(Router.Timing.FallSeconds + 0.3f);
            SelfTest.Check(Router.Level < 0.05f, $"and down to rest: {Router.Level:0.00}");
            yield return HumMenus(sound);

            // A live swap to Breath, calibrated from this room to a breath.
            Router.Profile.SetCalibration(SourceIds.Breath, AxisCalibration.Make("Level", -71f, -71f, -31f, 1f, true));
            Router.SetSource(SourceIds.Breath, save: false);
            int presses = Router.PressCount;
            yield return new WaitForSecondsRealtime(0.4f);
            SelfTest.Check(MicrophoneInput.IsOn && Router.Level < 0.05f, $"Breath hears the room as rest: Level {Router.Level:0.00}");
            sound.Noise(0.05f);
            yield return new WaitForSecondsRealtime(0.6f);
            SelfTest.Check(Router.Level > 0.8f && Router.IsPressDown, $"a breath raises the Level over the threshold: Level {Router.Level:0.00}, {Router.Raw.Loudness:0} dB");
            yield return SelfTest.Capture("D13-HumFlyer-Breath");
            sound.Noise(0.0005f);
            yield return new WaitForSecondsRealtime(0.6f);
            SelfTest.Check(Router.Level < 0.3f && !Router.IsPressDown && Router.PressCount == presses + 1,
                $"stopping lets it fall, and the puff was one press: Level {Router.Level:0.00}, {Router.PressCount - presses} press");

            Router.Profile.NoisyRoom = true;
            sound.Noise(0.0012f); // 2.4 times the room's loudness: under three times
            yield return new WaitForSecondsRealtime(0.5f);
            SelfTest.Check(Router.Level < 0.05f, $"with Noisy room on, 2.4 times the room's loudness is ignored (ruling A35): Level {Router.Level:0.00}");
            Router.Profile.NoisyRoom = false;
            yield return new WaitForSecondsRealtime(0.5f);
            SelfTest.Check(Router.Level > 0.05f, $"and with it off, it isn't: Level {Router.Level:0.00}");

            int loads = _scenesLoaded;
            DemoSession.QuitToHub();
            yield return WaitFor(() => _scenesLoaded > loads && MainMenu.IsShowing, "the hub after the microphone");
            yield return null;
            yield return null;
            SelfTest.Check(MicrophoneInput.IsOn && MicIndicator.Instance.IsShowing, "back on the hub, the microphone stays on with Breath still the source (ruling A36)");
            Router.SetSource(SourceIds.Keyboard, save: false);
            yield return null;
            SelfTest.Check(!MicrophoneInput.IsOn && !MicIndicator.Instance.IsShowing, "and goes off as Keyboard zones takes over");
            MicrophoneInput.Use(null);
        }

        // In the Hum Flyer, with the Hum source: a hum past the Long hold is still play, one past the
        // drift demos' menu hold opens the menu (ruling A37); in the menus a long hum goes back and a
        // short one picks the ringed control (ruling A36).
        private static IEnumerator HumMenus(TestToneFeed sound)
        {
            TimingValues timing = Router.Timing;
            float menuHold = DemoScreen.MenuHoldSeconds(Router);
            SelfTest.Check(menuHold > timing.LongHoldSeconds && DemoScreen.MenuHint(Router) == $"Long hum ({menuHold:0} s): menu",
                $"in the Hum Flyer a hum opens the menu only after {menuHold:0.0} s, longer than the Long hold: {DemoScreen.MenuHint(Router)}");

            yield return Hum(sound, (timing.LongHoldSeconds + menuHold) / 2f);
            SelfTest.Check(Router.LastPress == PressKind.LongHold && !MenuFocus.IsAnyOpen, "a hum past the Long hold, but short of that, is play: no menu");

            yield return Hum(sound, menuHold + 0.4f);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Feedback question", "a hum past it opens the menu: the feedback question first (ruling A21)");
            CheckTextSizes("the feedback question, scanned by hum");
            yield return Hum(sound, timing.LongHoldSeconds + 0.4f);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "SessionMenu", "a long hum goes back: the session menu follows");
            yield return ScanTo("Button: Resume");
            yield return Hum(sound, 0.4f);
            SelfTest.Check(Menus.Count == 0 && !MenuFocus.IsAnyOpen, "a short hum picks the ringed control: Resume");
        }

        // Hums the lowest note for seconds, then waits for the press to end and the Level to fall.
        private static IEnumerator Hum(TestToneFeed sound, float seconds)
        {
            sound.Tone(110f, 0.3f, 0.0005f);
            yield return new WaitForSecondsRealtime(seconds);
            sound.Noise(0.0005f);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        // Switching profile brings back its source and its calibration.
        private static IEnumerator ProfilesRestore()
        {
            Profile first = Router.Profile;
            Router.SetSource(SourceIds.Pointer);
            Router.Calibrated(AxisCalibration.Make("Steer", 0.3f, 0.1f, 0.6f, 0.01f, true));

            // A second profile, by the pointer: the Switch, chosen by pressing it.
            Press(MainMenu.Instance.Screen, "Button: Who's playing");
            yield return null;
            Press(Menus.Top, "Button: New profile");
            yield return null;
            Press(Menus.Top, "Button: Switch");
            yield return null;
            SelfTest.Check(Menus.Top != null && Menus.Top.name == "Calibration" && ScanDriver.Suspended,
                "choosing the Switch asks for the switch, with scanning held off");
            CheckTextSizes("choosing the switch");
            yield return SelfTest.Capture("15-ChooseSwitch");
            yield return QueuedInput.Press(SwitchKey);
            yield return null;
            Profile second = Router.Profile;
            SelfTest.Check(second.Id != first.Id && second.SwitchControl == "<Keyboard>/k" && !ScanDriver.Suspended,
                "pressing a key makes it the switch: " + SwitchSource.Describe(second.SwitchControl));
            Press(Menus.Top, "Button: Done");
            yield return null;
            SelfTest.Check(Menus.Count == 0 && Router.Source.Id == SourceIds.Switch, "the second profile plays with the Switch");

            Press(MainMenu.Instance.Screen, "Button: Who's playing");
            yield return null;
            Press(Menus.Top, "Button: " + ProfilesPage.Label(first));
            yield return null;
            AxisCalibration steer = Router.CalibrationOf("Steer");
            SelfTest.Check(Router.Profile.Id == first.Id && Router.Source.Id == SourceIds.Pointer && steer.IsSet && Mathf.Approximately(steer.Rest, 0.3f),
                $"picking the first profile brings back its Pointer and its calibration: {steer.Summary()}");

            Press(MainMenu.Instance.Screen, "Button: Who's playing");
            yield return null;
            Press(Menus.Top, "Button: " + ProfilesPage.Label(second));
            yield return null;
            SelfTest.Check(Router.Profile.Id == second.Id && Router.Source.Id == SourceIds.Switch && Router.Profile.SwitchControl == "<Keyboard>/k",
                "picking the second brings back its Switch on K");
        }

        // The hub scanned to a demo with one key; a Long hold back; gestures scanned in a Gesture demo.
        private static IEnumerator ScanningWithOneKey()
        {
            TimingValues timing = Router.Timing;
            timing.ScanStepSeconds = 0.5f;
            timing.ScanRounds = 0;
            timing.HoldSeconds = 0.5f;
            timing.LongHoldSeconds = 1f;
            yield return null;
            SelfTest.Check(ScanDriver.Instance.IsScanning, "the hub is scanned while the Switch source plays");
            yield return ScanTo("Button: Who's playing");
            CheckTextSizes("the hub, scanned");
            yield return SelfTest.Capture("16-Scanning-Hub");

            // Held past a scan step: the ring stands still and the Hold picks (ruling A22).
            yield return ScanTo("Button: Demos");
            yield return HoldKey(SwitchKey, 0.7f);
            SelfTest.Check(Router.LastPress == PressKind.Hold && Menus.Count == 1 && Menus.Top.name == "Demos", "a Hold on the ringed Demos opens it, the ring held still (ruling A22)");
            if (Menus.Count != 1 || Menus.Top.name != "Demos")
            {
                yield break;
            }

            string lighthouse = UiProbe.Texts(UiProbe.Find<Button>(Menus.Top, "Button: Lighthouse Keeper").transform);
            SelfTest.Check(lighthouse.Contains("Switch can't play this"), "the Lighthouse Keeper's tile says a switch can't play it (ruling A18): " + lighthouse);
            string dwell = UiProbe.Texts(UiProbe.Find<Button>(Menus.Top, "Button: Dwell Builder").transform);
            SelfTest.Check(!dwell.Contains("can't play this"), "a switch can play the Dwell Builder (ruling A29): " + dwell);
            string gardenTile = UiProbe.Texts(UiProbe.Find<Button>(Menus.Top, "Button: Keyboard Garden").transform);
            SelfTest.Check(gardenTile.Contains("scan the gestures"), "the Keyboard Garden's says how a switch plays it: " + gardenTile);
            yield return ScanTo("Button: Keyboard Garden");
            yield return SelfTest.Capture("17-Scanning-Demos");

            int loads = _scenesLoaded;
            yield return TapOn("Button: Compass Sweep");
            yield return WaitFor(() => _scenesLoaded > loads && DemoScreen.Current is CompassSweep, "the Compass Sweep, reached with one key");
            if (!_arrived)
            {
                yield break;
            }

            yield return null;
            int presses = DemoSession.Scene.Presses;
            var boat = ((CompassSweep)DemoScreen.Current).Map.Boat;
            yield return QueuedInput.Press(SwitchKey);
            yield return null;
            SelfTest.Check(DemoSession.Scene.Presses == presses + 1 && !MenuFocus.IsAnyOpen, "in play, a tap is the demo's");
            SelfTest.Check(((CompassSweep)DemoScreen.Current).Map.Boat != boat, "and sails the boat");

            yield return HoldKey(SwitchKey, 1.3f);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Feedback question", "in play, a Long hold pauses: the feedback question first (rulings A20, A21)");
            yield return TapOn("Button: Skip");
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "SessionMenu", "Skip, picked by scanning, goes on to the session menu");
            yield return ScanTo("Button: Next source");
            CheckTextSizes("the session menu, scanned");
            yield return SelfTest.Capture("18-Scanning-SessionMenu");
            yield return HoldKey(SwitchKey, 1.3f);
            SelfTest.Check(Menus.Count == 0 && !MenuFocus.IsAnyOpen, "a Long hold goes back: it closes the session menu");

            yield return PlayDemo("KeyboardGarden");
            if (!_arrived || DemoScreen.Current is not KeyboardGarden garden)
            {
                yield break;
            }

            SelfTest.Check(Router.GestureScan != null, "a Gesture demo scans the gestures for a switch (ruling A18)");
            float start = Time.realtimeSinceStartup;
            while (Router.GestureScan != null && Router.GestureScan.Highlighted != KeyboardZones.BottomLeft && Time.realtimeSinceStartup - start < 10f)
            {
                yield return null;
            }

            yield return QueuedInput.Press(SwitchKey);
            yield return null;
            yield return SelfTest.Capture("19-GestureScan");
            SelfTest.Check(Router.LastGesture == KeyboardZones.BottomLeft && DemoSession.Scene.Gestures.Contains(KeyboardZones.BottomLeft) &&
                           garden.Garden.PlantsIn(2) == 1, "a tap sends the highlighted gesture, which plants in its bed: " + Router.LastGesture);

            // A Hold grows the plant and plants nothing; Resume picked by a tap plants nothing either.
            float height = garden.Garden.CurrentHeight;
            yield return HoldKey(SwitchKey, 0.7f);
            SelfTest.Check(Router.LastPress == PressKind.Hold && garden.Garden.Plants == 1 && garden.Garden.CurrentHeight > height + 0.2f,
                $"a Hold grows the plant and plants nothing: {height:0.00} to {garden.Garden.CurrentHeight:0.00}");
            yield return HoldKey(SwitchKey, 1.3f);
            SelfTest.Check(Menus.Count == 1 && Menus.Top.name == "Feedback question", "a Long hold pauses the Garden");
            yield return TapOn("Button: Skip");
            yield return TapOn("Button: Resume");
            SelfTest.Check(Menus.Count == 0 && garden.Garden.Plants == 1,
                $"the tap that picks Resume plants nothing (it began in the menu): {garden.Garden.Plants} plants");

            yield return DwellBuilderByScanning();

            loads = _scenesLoaded;
            DemoSession.QuitToHub();
            yield return WaitFor(() => _scenesLoaded > loads && MainMenu.IsShowing, "the hub at the end");
            SelfTest.Check(Router.GestureScan == null, "the gesture scan ends with the demo");
        }

        // Dwell Builder with one key (ruling A29): the palette and the plate's rows are scanned; a tap picks the row, then the block or cell.
        private static IEnumerator DwellBuilderByScanning()
        {
            yield return PlayDemo("DwellBuilder");
            if (!_arrived || DemoScreen.Current is not DwellBuilder builder)
            {
                yield break;
            }

            RowColumnScan scan = builder.Scan;
            SelfTest.Check(!builder.IsDwelling && scan != null, "a switch scans the Dwell Builder's rows and cells (ruling A29)");
            if (scan == null)
            {
                yield break;
            }

            yield return Until(() => !scan.InRow && scan.Row == 0, "the scan lights the palette");
            yield return QueuedInput.Press(SwitchKey);
            yield return Until(() => scan.InRow && scan.Item == 1, "then Grass in it");
            yield return QueuedInput.Press(SwitchKey);
            yield return null;
            SelfTest.Check(builder.Plate.Picked == 1, "a tap on the lit block picks it: " + BuildPlate.BlockNames[builder.Plate.Picked]);
            yield return Until(() => !scan.InRow && scan.Row == 3, "the scan lights the plate's third row");
            yield return QueuedInput.Press(SwitchKey);
            yield return Until(() => scan.InRow && scan.Item == 4, "then its fifth cell");
            yield return SelfTest.Capture("D7-DwellBuilder-Scanning");
            yield return QueuedInput.Press(SwitchKey);
            yield return null;
            SelfTest.Check(builder.Plate.At(4, 2) == 1 && builder.Plate.Placed == 1, "a tap on the lit cell places the block");
        }

        //----------

        // Loads a demo and waits until it has started.
        private static IEnumerator PlayDemo(string id)
        {
            DemoInfo demo = DemoCatalogue.Find(id);
            int loads = _scenesLoaded;
            DemoSession.Play(demo);
            yield return WaitFor(() => _scenesLoaded > loads && DemoSession.Scene != null && DemoSession.Current == demo, demo.Name);
            if (_arrived)
            {
                yield return null; // its Start
                yield return null; // its layout
            }
        }

        // Waits, up to 20 seconds, for something that takes no scene load.
        private static IEnumerator Until(Func<bool> done, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < 20f)
            {
                yield return null;
            }

            SelfTest.Check(done(), what);
            SelfTest.Progress();
        }

        // A tap from the Scripted source: down for two frames, then up.
        private static IEnumerator ScriptedTap(ScriptedSource scripted)
        {
            scripted.Press(true);
            yield return null;
            yield return null;
            scripted.Press(false);
            yield return null;
            yield return null;
        }

        // The Scripted source's Point rests at point for seconds.
        private static IEnumerator RestOn(ScriptedSource scripted, Vector2 point, float seconds)
        {
            scripted.Set(0f, 0f, point);
            yield return new WaitForSecondsRealtime(seconds);
        }

        //----------

        // Waits for something that takes a scene load. If it never comes the run is over: _arrived
        // is false, and the steps stop.
        private static IEnumerator WaitFor(Func<bool> arrived, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!arrived() && Time.realtimeSinceStartup - start < WaitSeconds)
            {
                yield return null;
            }

            _arrived = SelfTest.Check(arrived(), "waited for " + what);
            if (_arrived)
            {
                SelfTest.Progress();
            }
            else
            {
                SelfTest.Finish();
            }
        }

        // By keyboard, as a player would: arrows along the UI's own navigation to the control, then Enter.
        // A control the arrows can't reach fails the check, and nothing is pressed.
        private static IEnumerator Choose(string name)
        {
            GameObject from = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            List<Key> path = PathTo(from != null ? from.GetComponent<Selectable>() : null, name);
            if (path != null)
            {
                foreach (Key key in path)
                {
                    yield return QueuedInput.Press(key);
                }
            }

            if (SelfTest.Check(Selected() == name, $"the keyboard reaches {name}: {Selected()}"))
            {
                yield return QueuedInput.Press(Key.Enter);
                yield return null;
            }

            SelfTest.Progress();
        }

        // The shortest run of arrow keys from start to the control named name, by the UI's navigation; null if none.
        private static List<Key> PathTo(Selectable start, string name)
        {
            if (start == null)
            {
                return null;
            }

            var paths = new Dictionary<Selectable, List<Key>> { [start] = new List<Key>() };
            var queue = new Queue<Selectable>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Selectable at = queue.Dequeue();
                if (at.name == name)
                {
                    return paths[at];
                }

                foreach ((Selectable next, Key key) in new[]
                         {
                             (at.FindSelectableOnDown(), Key.DownArrow), (at.FindSelectableOnUp(), Key.UpArrow),
                             (at.FindSelectableOnRight(), Key.RightArrow), (at.FindSelectableOnLeft(), Key.LeftArrow),
                         })
                {
                    if (next != null && !paths.ContainsKey(next))
                    {
                        paths[next] = new List<Key>(paths[at]) { key };
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
        }

        // Waits for the scan's ring to reach a control.
        private static IEnumerator ScanTo(string name)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 20f && (ScanDriver.Instance.Highlighted == null || ScanDriver.Instance.Highlighted.name != name))
            {
                yield return null;
            }

            Selectable ringed = ScanDriver.Instance.Highlighted;
            SelfTest.Check(ringed != null && ringed.name == name && Selected() == name, $"the scan reaches {name}");
            SelfTest.Progress();
        }

        // Waits for the ring to reach a control and taps the switch straight away, before the scan moves on.
        private static IEnumerator TapOn(string name)
        {
            yield return ScanTo(name);
            yield return QueuedInput.Press(SwitchKey);
            yield return null;
        }

        // Holds a key down for seconds, as a switch held long.
        private static IEnumerator HoldKey(Key key, float seconds)
        {
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private static void Press(RectTransform menu, string buttonName) =>
            UiProbe.Press(menu != null ? UiProbe.Find<Button>(menu, buttonName) : null, buttonName);

        private static void CheckFeedbackRow(string answer)
        {
            string file = FeedbackLog.LastFile;
            string folder = CommandLine.Value(FeedbackLog.FolderFlag);
            SelfTest.Check(file != null && folder != null && file.StartsWith(folder) && File.Exists(file),
                $"the answer is saved in the check's own folder: {file}");
            if (file == null || !File.Exists(file))
            {
                return;
            }

            string[] lines = File.ReadAllLines(file);
            SelfTest.Check(lines.Length >= 2 && lines[0] == FeedbackLog.Row(FeedbackLog.Columns), "the file starts with its header: " + lines[0]);
            List<string> row = FeedbackLog.Split(lines[lines.Length - 1]);
            SelfTest.Check(row.Count == FeedbackLog.Columns.Length && row[1] == Router.Profile.Name && row[2] == "Compass Sweep" &&
                           row[6] == answer && row[8] == BuildInfo.Version,
                "the row holds the profile, the demo, the answer and the version: " + lines[lines.Length - 1]);
        }

        // What the stack promises of the menu on top, and of the hub with none over it:
        // keyboard and gamepad navigation, from its selected control, stays in it.
        private static void CheckNavigation(string what, RectTransform menu)
        {
            List<Selectable> reached = UiProbe.Reachable();
            bool inside = reached.Count > 0;
            var names = new List<string>();
            foreach (var control in reached)
            {
                inside &= menu != null && control.transform.IsChildOf(menu);
                names.Add(control.name);
            }

            SelfTest.Check(inside, $"navigation in {what} stays in it: {string.Join(" | ", names)}");
        }

        // Ruling A10: no text on the screen under 24 points.
        private static void CheckTextSizes(string where)
        {
            var small = new List<string>();
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (text.isActiveAndEnabled && !string.IsNullOrWhiteSpace(text.text) && text.fontSize < TextFloor.Minimum - 0.01f)
                {
                    small.Add($"{text.name} \"{text.text}\" at {text.fontSize:0}");
                }
            }

            SelfTest.Check(small.Count == 0, $"no text under 24 points on {where}" + (small.Count > 0 ? ": " + string.Join(" | ", small) : ""));
        }

        // Each volume's mixer parameter exists and is at the volume's level.
        private static void CheckMixer(string when)
        {
            foreach (VolumeSetting volume in Settings.Volumes)
            {
                float? mixer = SettingsMenu.Instance != null ? SettingsMenu.Instance.MixerDecibels(volume) : null;
                string heard = mixer.HasValue ? $"{mixer.Value:0.0} dB" : "no parameter";
                SelfTest.Check(mixer.HasValue && Mathf.Abs(mixer.Value - volume.Decibels) < 0.01f,
                    $"the mixer plays {volume.Name} at its setting {when}: {volume.Decibels:0.0} dB, mixer {heard}");
            }
        }

        private static string Selected()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null ? selected.name : "nothing";
        }
    }
}
