# Attune: hand-off

Status (2026-10-23): **M5 is done and Attune is public** at github.com/ionAurum/Attune, MIT,
as one squashed commit (A40); the old private history is `ionAurum/Attune-history`. `core` is
public as `ionAurum/aurumworks-core` `v0.1.0`, and Attune takes it by git URL (A41). Jon played
M5 with Hum (A38) and Breath (A39): their choices stand until testers report. The version is
0.5.0. **Next: Jon sends the testers' zip, then S1.**

- 84 EditMode tests pass (`Assets/Attune/Scripts/Editor/Tests/`; `AudioTests`, `DriftTests` and `ShapeTests` are M5's).
- The check passes at 1920×1080 and 1280×800 with no errors: 35 screenshots (`D1`–`D13` are the
  demos, `20`–`21` calibrating Hum, `22` the hub scanned by Hum) and one feedback row in
  `%TEMP%\attune-runs\<name>`. It needs no microphone: `TestToneFeed` plays a test sound through
  the real Breath and Hum pipeline. `HumMenus` checks A36 and A37 in the Hum Flyer.
- `bash Tools/testerbuild.sh` makes `Builds/Attune-v<version>-windows.zip`.
  `Tools/Tester/How To Run.txt` covers Breath, Hum and the microphone.
- Commits are authored with Jon's GitHub noreply email (this repository's `git config`, A40).
  Never push the local `history/*` branches: they hold the old history with his details.

## Read, in order

1. `../CLAUDE.md`, then `CLAUDE.md`, then this file.
2. `Docs/Attune_Design.md`: the opening, *Decided by Jon*, *Still open*, *Milestones*.
3. For whatever comes next, the sections named under *Next*.

## What M5 added

- `Signals/`: `LoudnessDetector` (RMS in dBFS, offset removed), `PitchDetector` (YIN, 70–1000 Hz,
  in semitones above 55 Hz), `LevelGlide` (A34), `WavReader` (the mic file), `Calibrate.Range`
  (a hum's lowest and highest notes, by median), `AxisCalibration.RaisedRest` (Noisy room, A35).
- The microphone: `MicrophoneInput` (static; on while Breath or Hum is the source, A36; a short ring
  buffer, nothing written), fed by `MicrophoneFeed` (the default device, by `null`, so no device
  name is ever read), `SampleFeed` (`-attuneMicFile <wav>`, and silence in any self-test) or
  `TestToneFeed` (the check). `MicIndicator` shows "Microphone on", and a problem in words (no
  microphone; nothing heard for 1.5 s, which is how Windows' privacy block shows).
- `BreathSource` (`Level` in dBFS; Level → Press makes a puff a press) and `HumSource` (`Level` in
  semitones, `Press` while humming, `LevelSilent` between notes; its calibration's Rest step keeps
  the room as the `Quiet` axis, which gates what counts as a hum). `CalibrationAxis` carries each
  source's own words; `IsRange` marks the hum's low-to-high steps. `InputRouter` glides a silent
  Level and applies `InputSource.InPlay` (Noisy room).
- `DriftDemo` (A33) with `DriftCourse` and `FlightHeight` (plain C#, tested), and the demos
  `KiteBreath` and `HumFlyer`; their scenes, made by `SceneMaker`. Tuning blocks for the
  microphone, Breath, Hum, Kite Breath and Hum Flyer.
- `Profile.NoisyRoom` and its toggle on the Timing page. The overlay shows the microphone's dB and Hz.

## What the replay changed (A36, A37)

- `BreathSource` and `HumSource` have `ScansMenus`; `InputSource.Words` (`PressWords`) names each
  source's presses in the hints. `MicrophoneInput.Allowed` is gone: the active microphone source
  keeps it on, and `End()` turns it off.
- `CalibrationRun` sets `ScanDriver.Suspended` while Rest and Reach measure, and ignores the
  press under way when they end (`HoldOff`). `ScanDriver` doesn't scan Breath or Hum while the
  microphone has a problem, so the keyboard works for a helper.
- `DemoScreen.MenuHoldSeconds` / `OpensMenu` / `MenuHint`; `DriftDemo` asks for
  `AttuneTuning.DriftMenuHoldSeconds` (5 s) with a microphone source. `InputRouter.LastPressSeconds`.
- `CLAUDE.md` (*Privacy*) and `Tools/Tester/How To Run.txt` say the new microphone rule.

## Open questions for Jon (after playing)

M5's choices are ruled (A38, A39). One remains:

6. The device check (`-attuneDevices`, design *Checks*) isn't built: it opens the camera too, so
   M6 builds it. The microphone logs only its sample rate.

## Next

1. **(M5 is done.)** For later Breath or Hum feedback from testers, read `Scripts/BreathSource.cs`
   or `HumSource.cs`, `Signals/LoudnessDetector.cs` / `PitchDetector.cs`, `Scripts/DriftDemo.cs`
   and its demo, the Microphone, Breath and Hum blocks of `AttuneTuning.cs`, and design A33–A39.
   A recording replays through the real pipeline with `Attune.exe -attuneMicFile <wav>`
   (recordings in `Tools/Recordings/` are Jon's and stay out of git).
2. **Jon sends the testers' zip:** `Builds/Attune-v0.5.0-windows.zip` (rebuild with
   `bash Tools/testerbuild.sh` after any change). The repository is public, so the zip may go out.
3. **S1–S4, steer from a switch, before M6** (design, *Still open*, *Milestones*). One candidate
   per iteration, each an adapter in `Attune.Signals` with tests; Jon plays each, then rules after
   S4. Read *Adapters*, *Scanning*, design A42, and `SteerLevel` and `PressTracker` in
   `Signals/`. To settle with Jon before building:
   - where he plays them before the sailing demo exists (a test boat in the stub?);
   - S1 is settled (A42): the turn lasts while the switch is held, and each press turns the
     other way from the last.
   - S2: which way a Tap turns and which way a Hold turns, and how long a Tap's turn lasts (a
     Tap is only known on release). A Hold held on to a Long hold opens the session menu (A20).
   - S3: the scan speed and whether it is the profile's; whether straight is scanned first.
   - S4: which way a press nudges with one switch; the step's size; whether it wraps.

## For M6 and M7: camera tracking (A5)

The spike is in `../AttuneSpike/` (outside git: its `README.md`, `spike.sh` and `Inference/`
project hold the pipeline). What M6 adds, with Jon's approval for `manifest.json`:

- `com.unity.ai.inference` 2.6.1 from Unity's registry (it brings Burst, Collections, App UI and
  Newtonsoft JSON). Its licence is Unity's Terms of Service; it isn't redistributed, only named in
  the manifest.
- MediaPipe's three `.tflite` models (6.8 MB) under `Assets/Attune/`, in LFS, with an Apache 2.0
  entry in `Docs/licences.md`.
- The spike's pipeline rewritten to Attune's rules: the maths (anchors, crop, tracking, head pose
  from the landmarks) in `Attune.Signals`, the Head and Face sources in `Scripts/`, off the main
  thread (results read a frame later, about 17 ms).

What it gives: 478 landmarks and 52 blendshapes per frame at the camera's 30 fps, in Mono and
IL2CPP, on the CPU or the GPU. Brow raise, mouth open, smile and each eye's blink registered
clearly; **cheek puff stayed at zero**, a known weakness of the model, so Expression Spells may drop
it or detect it from the landmarks (ask Jon at M7). Not tested yet: a modest laptop, poor light,
glasses, beards, a wink as distinct from a blink, and anyone but one person at one desk.

## Known gaps

- The theme is still the template's: A10's 7:1 contrast hasn't been measured or tuned (the
  muted hint text and the compass's blocked headings are the likeliest to fall short).
- No sound cues yet (A12: Jon sources the files), so the demos are silent and the check plays none.
- No real microphone has been tried: the detectors are tested on synthetic sound only. Untested:
  a real breath (which carries low rumble the detector doesn't filter), a real voice's hum,
  Windows' privacy block, a laptop's built-in microphone, and a noisy room.

## Worth knowing

- **Pushing:** `gh` is in `C:\Program Files\GitHub CLI\`, signed in to Jon's account. Git Bash's
  `PATH` doesn't have it yet; PowerShell finds it once `PATH` is reloaded from the registry.
- **`.gitignore` lets nothing into `Assets/` but** `Attune/`, `Scenes/`, `Settings/`,
  `TextMesh Pro/` and the input actions. A new top-level folder needs a `!` line there, and its
  licence in `Docs/licences.md` first. `Tools/Recordings/` and `Builds/` stay out of git.
- Jon deleted the template's `Assets/Scenes/Game.unity`. The empty `Assets/Scenes/` folder and its
  tracked `Scenes.meta` are left: Jon deletes the folder in the Editor, then the two `Scenes` lines
  in `.gitignore` can go. The check's class is still `MenuCheck`; renaming it is Editor work too.
- The check makes its own profiles in memory. A tap must follow the scan ring at once
  (`TapOn`): a screenshot between them lets the scan move on. The check speeds timing up in
  memory (scan step, dwell) and never saves it.
- Attune's settings keys start `Attune.` (`Attune.Profile.<n>`, `Attune.Profiles`,
  `Attune.CurrentProfile`, `Attune.Overlay`). The volumes stay `Aurumworks.Volume.<name>`.
- A switch is saved by its device's layout (`<Keyboard>/k`), not by the device's name.
- In a self-test `MicrophoneInput` never opens the real microphone (silence, or the check's
  `TestToneFeed` given by `MicrophoneInput.Use`). The Hum source's Level glides down after a note,
  so a check that changes sound waits out `FallSeconds` before expecting rest.
- A `Graphic` drawn in code needs `[RequireComponent(typeof(CanvasRenderer))]`: without it the
  Editor (not the player, so not the check) can throw under a `RectMask2D`.
- A built demo is a `DemoScreen` subclass, listed in `SceneBootstrap._screens`, with its scene
  named by its Id in `DemoCatalogue`; `SceneMaker` then makes the scene.
