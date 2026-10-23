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
    /// Runs the steps of a calibration on the open Calibration dialog (CalibrationPage): Rest, then
    /// Reach each way, for each axis of the source, timing each step and keeping its raw samples; or,
    /// for the Switch source, waits for the player to press their switch. A range axis (the Hum
    /// source) hears the room at Rest, then its lowest and highest comfortable notes. While a step
    /// measures, the menus aren't scanned, so a breath or a hum measured isn't a press that picks or
    /// goes back (ruling A36), and a microphone problem shows in place of the step's words. Two buttons do everything: the main one (Start, Again, Done) and the other
    /// (Skip, Choose again).
    /// </summary>
    public class CalibrationRun : MonoBehaviour
    {
        private enum Step
        {
            Ready,
            Rest,
            ReachLow,
            ReachHigh,
            Capture,
            Done,
        }

        private const float CaptureSeconds = 10f;
        private const int MinRangeSamples = 15; // frames with a note in each of a range's Reach steps, about a quarter of a second

        private static CalibrationRun _running;

        private readonly List<float> _samples = new();
        private readonly List<float> _lowSamples = new(); // a range axis's low step, kept for its high step

        private InputRouter _router;
        private TextMeshProUGUI _instruction, _detail, _goLabel, _otherLabel;
        private RectTransform _time, _meter;
        private Button _go, _other;
        private Step _step;
        private float _elapsed;
        private int _axisIndex;
        private AxisCalibration _working;
        private string _result, _stepDetail;

        /// <summary>True while a calibration dialog is open.</summary>
        public static bool IsRunning => _running != null;

        /// <summary>The step under way, for the self-test.</summary>
        public string StepName => _step.ToString();

        private InputSource Source => _router.Source;

        private CalibrationAxis Axis => _axisIndex < Source.Axes.Count ? Source.Axes[_axisIndex] : null;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _running = null;

        /// <summary>Hands the run the dialog's parts, and shows the first step.</summary>
        public void Build(InputRouter router, TextMeshProUGUI instruction, TextMeshProUGUI detail, RectTransform time, RectTransform meter,
            Button go, Button other)
        {
            _running = this;
            _router = router;
            _instruction = instruction;
            _detail = detail;
            _time = time;
            _meter = meter;
            _go = go;
            _other = other;
            _goLabel = go.GetComponentInChildren<TextMeshProUGUI>();
            _otherLabel = other.GetComponentInChildren<TextMeshProUGUI>();
            _axisIndex = 0;
            if (Source is SwitchSource)
            {
                StartCapture();
            }
            else if (Source.Axes.Count == 0)
            {
                Finish("Nothing to calibrate: every key is a press, the left and right halves steer, and the four corners are gestures.");
            }
            else
            {
                Show(Step.Ready);
            }
        }

        /// <summary>The main button: Start, Again, or Done.</summary>
        public void Go()
        {
            switch (_step)
            {
                case Step.Ready:
                    Begin(Step.Rest);
                    break;
                case Step.Done:
                    SessionMenu.Menus.Close();
                    break;
            }
        }

        /// <summary>The other button: Skip, or Choose again / Again.</summary>
        public void Other()
        {
            switch (_step)
            {
                case Step.Ready:
                case Step.Rest:
                case Step.ReachLow:
                case Step.ReachHigh:
                    SessionMenu.Menus.Close();
                    break;
                case Step.Done:
                    if (Source is SwitchSource)
                    {
                        StartCapture();
                    }
                    else if (Source.Axes.Count > 0)
                    {
                        _axisIndex = 0;
                        Show(Step.Ready);
                    }

                    break;
            }
        }

        /// <summary>Stops any capture as the dialog closes.</summary>
        public void Stop()
        {
            if (_running == this)
            {
                _running = null;
            }

            if (Source is SwitchSource switchSource)
            {
                switchSource.StopCapture();
            }

            if (ScanDriver.Suspended)
            {
                ScanDriver.HoldOff(); // a breath or a hum under way was the step's, not a press
            }

            ScanDriver.Suspended = false;
        }

        private void OnDestroy()
        {
            if (_running == this)
            {
                _running = null;
            }
        }

        private void Update()
        {
            if (_router == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            SetFill(_meter, Mathf.InverseLerp(-1f, 1f, ChannelRoutes.Has(Source.Supplies, Channels.Steer) ? _router.Steer : _router.Level * 2f - 1f));
            if (Source.UsesMicrophone && _step <= Step.ReachHigh)
            {
                // A microphone that isn't there or is blocked says so plainly, not as a dead meter (design, Risks).
                string text = MicrophoneInput.Problem ?? _stepDetail;
                if (_detail.text != text)
                {
                    _detail.text = text;
                }
            }

            switch (_step)
            {
                case Step.Rest:
                case Step.ReachLow:
                case Step.ReachHigh:
                    if (TrySample(out float sample))
                    {
                        _samples.Add(sample);
                    }

                    _elapsed += dt;
                    float length = _step == Step.Rest ? AttuneTuning.Current.RestSeconds : AttuneTuning.Current.ReachSeconds;
                    SetFill(_time, _elapsed / length);
                    if (_elapsed >= length)
                    {
                        EndStep();
                    }

                    break;
                case Step.Capture:
                    _elapsed += dt;
                    SetFill(_time, 1f - _elapsed / CaptureSeconds);
                    _detail.text = $"Your switch now: {SwitchSource.Describe(_router.Profile.SwitchControl)}. " +
                                   $"Nothing pressed in {Mathf.CeilToInt(CaptureSeconds - _elapsed)} s keeps it.";
                    if (_elapsed >= CaptureSeconds)
                    {
                        Captured(null);
                    }

                    break;
                case Step.Done:
                    if (Source is SwitchSource)
                    {
                        _detail.text = _result + "\n" + (_router.IsPressDown ? $"Held {_router.PressHeldSeconds:0.0} s: {_router.PressKindIfReleased}" : "Try it.");
                    }

                    break;
            }
        }

        // This frame's raw value for the step under way. A range axis's Rest step hears the room's
        // loudness, and its Reach steps take only frames with something to measure (a note).
        private bool TrySample(out float sample)
        {
            SourceReading raw = _router.Raw;
            sample = 0f;
            if (Axis == null)
            {
                return false;
            }

            if (Axis.IsRange)
            {
                if (_step == Step.Rest)
                {
                    sample = raw.Loudness;
                    return true;
                }

                sample = raw.Level;
                return !raw.LevelSilent;
            }

            sample = Axis.Axis == "Level" ? raw.Level : raw.Steer;
            return true;
        }

        private void Begin(Step step)
        {
            _samples.Clear();
            _elapsed = 0f;
            if (step == Step.Rest)
            {
                _working = _router.CalibrationOf(Axis.Axis).Copy();
                _result = null;
            }

            Show(step);
        }

        private void EndStep()
        {
            float minReach = AttuneTuning.Current.MinReach;
            if (Axis.IsRange)
            {
                EndRangeStep();
                return;
            }

            switch (_step)
            {
                case Step.Rest:
                    if (Calibrate.Rest(_samples, out float rest, out float noise))
                    {
                        // The ends move with rest, so a pointer resting somewhere new keeps its reach until measured.
                        _working.Low += rest - _working.Rest;
                        _working.High += rest - _working.Rest;
                        _working.Rest = rest;
                        _working.Noise = noise;
                    }

                    Begin(Axis.IsTwoSided ? Step.ReachLow : Step.ReachHigh);
                    break;
                case Step.ReachLow:
                    _result = Calibrate.Reach(_samples, _working.Rest, _working.Noise, -1, minReach, out float low)
                        ? null
                        : Axis.Missed ?? $"I didn't see you move {Axis.LowWord}, so that side is as it was. ";
                    if (_result == null)
                    {
                        _working.Low = low;
                    }

                    Begin(Step.ReachHigh);
                    break;
                case Step.ReachHigh:
                    string note = _result ?? "";
                    if (Calibrate.Reach(_samples, _working.Rest, _working.Noise, 1, minReach, out float high))
                    {
                        _working.High = high;
                    }
                    else
                    {
                        note += Axis.Missed ?? $"I didn't see you move {Axis.HighWord}, so that side is as it was. ";
                    }

                    KeepAxis(note);
                    break;
            }
        }

        // A range axis (the Hum source): Rest hears the room, kept as the Quiet axis; the two Reach
        // steps hear the lowest and highest comfortable notes, which become Level's 0 and 1.
        private void EndRangeStep()
        {
            switch (_step)
            {
                case Step.Rest:
                    if (Calibrate.Rest(_samples, out float room, out float noise))
                    {
                        _router.Calibrated(AxisCalibration.Make(CalibrationAxis.Quiet, room, room, room, noise, isSet: true));
                    }

                    Begin(Step.ReachLow);
                    break;
                case Step.ReachLow:
                    _lowSamples.Clear();
                    _lowSamples.AddRange(_samples);
                    Begin(Step.ReachHigh);
                    break;
                case Step.ReachHigh:
                    string note = "";
                    if (Calibrate.Range(_lowSamples, _samples, MinRangeSamples, AttuneTuning.Current.HumMinSpan, out float low, out float high))
                    {
                        _working.Rest = _working.Low = low;
                        _working.High = high;
                        _working.Noise = 0f;
                    }
                    else
                    {
                        note = "I didn't hear two different notes, so your range is as it was. ";
                    }

                    KeepAxis(note);
                    break;
            }
        }

        // Keeps the axis just measured, then goes on to the next, or to Done.
        private void KeepAxis(string note)
        {
            _working.Axis = Axis.Axis;
            _working.IsSet = true;
            _router.Calibrated(_working);
            _axisIndex++;
            if (Axis != null)
            {
                Show(Step.Ready);
            }
            else
            {
                Finish(note + "Done. Calibrated to your own range.");
            }
        }

        private void StartCapture()
        {
            ScanDriver.Suspended = true;
            _elapsed = 0f;
            Show(Step.Capture);
            ((SwitchSource)Source).Capture(Captured);
        }

        // path is the new switch, or null to keep the old one.
        private void Captured(string path)
        {
            if (Source is SwitchSource switchSource)
            {
                switchSource.StopCapture();
                if (path != null)
                {
                    _router.Profile.SwitchControl = path;
                    _router.SaveProfile();
                }
            }

            ScanDriver.HoldOff();
            ScanDriver.Suspended = false;
            Finish($"Your switch is {SwitchSource.Describe(_router.Profile.SwitchControl)}. Tap it to choose; hold it long to go back.");
        }

        private void Finish(string result)
        {
            _result = result;
            Show(Step.Done);
        }

        private void Show(Step step)
        {
            _step = step;
            HoldScanWhileMeasuring(step);
            SetFill(_time, 0f);
            string axisWord = Axis != null ? Axis.Axis : "";
            switch (step)
            {
                case Step.Ready:
                    float restSeconds = AttuneTuning.Current.RestSeconds;
                    _instruction.text = Axis?.Ready != null ? "Ready when you are" : "Calibrate " + axisWord;
                    _detail.text = Axis?.Ready != null ? string.Format(CultureInfo.InvariantCulture, Axis.Ready, restSeconds)
                        : Axis != null && Axis.IsTwoSided
                            ? $"First keep still for {restSeconds:0} seconds. Then move {Axis.LowWord}, then {Axis.HighWord}, " +
                              "as far as is comfortable: not as far as you can."
                            : $"First keep still for {restSeconds:0} seconds. Then go as far as is comfortable.";
                    Buttons("Start", "Skip");
                    break;
                case Step.Rest:
                    _instruction.text = Axis.Still ?? "Keep still";
                    _detail.text = Axis.StillDetail ?? "Measuring where you rest.";
                    Buttons(null, "Skip");
                    break;
                case Step.ReachLow:
                    _instruction.text = Axis.LowLine ?? $"Move {Axis.LowWord}, as far as is comfortable";
                    _detail.text = Axis.ReachDetail ?? "Hold it there.";
                    Buttons(null, "Skip");
                    break;
                case Step.ReachHigh:
                    _instruction.text = Axis.HighLine ?? (Axis.IsTwoSided ? $"Now {Axis.HighWord}, as far as is comfortable" : "As far as is comfortable");
                    _detail.text = Axis.ReachDetail ?? "Hold it there.";
                    Buttons(null, "Skip");
                    break;
                case Step.Capture:
                    _instruction.text = "Press your switch";
                    _detail.text = "";
                    Buttons(null, null);
                    break;
                case Step.Done:
                    _instruction.text = "Done";
                    _detail.text = _result;
                    Buttons("Done", Source is SwitchSource ? "Choose again" : Source.Axes.Count > 0 ? "Again" : null);
                    break;
            }

            _stepDetail = _detail.text;
        }

        // A breath or a hum measured is also a press: the menus aren't scanned while a step measures,
        // and the press under way as it ends is ignored, so it neither picks nor goes back (ruling A36).
        private static void HoldScanWhileMeasuring(Step step)
        {
            bool measuring = step is Step.Rest or Step.ReachLow or Step.ReachHigh;
            if (measuring)
            {
                ScanDriver.Suspended = true;
            }
            else if (step != Step.Capture && ScanDriver.Suspended)
            {
                ScanDriver.HoldOff();
                ScanDriver.Suspended = false;
            }
        }

        // Shows the buttons with these labels; null hides one. The main one keeps the selection.
        private void Buttons(string go, string other)
        {
            _go.gameObject.SetActive(go != null);
            _other.gameObject.SetActive(other != null);
            if (go != null)
            {
                _goLabel.text = go;
                _go.name = "Button: " + go;
            }

            if (other != null)
            {
                _otherLabel.text = other;
                _other.name = "Button: " + other;
            }

            MenuStack.SelectMainAction((RectTransform)transform);
        }

        private static void SetFill(RectTransform fill, float share)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(share), 1f);
            fill.offsetMax = Vector2.zero;
        }
    }
}
