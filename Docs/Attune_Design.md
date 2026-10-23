# Attune: design

**Attune** (ruling 6) is a set of small tech demos, one Unity scene each, of game mechanics
designed from the start for players with physical disabilities. It is not a game. Its tagline:
*any action, tuned to you.* It has two audiences:

- **Assistive technologists** Jon knows. They play it and tell us what works. Each demo has a
  feedback prompt that saves to a file they can send back.
- **Donors and investors.** It is an early artifact for an elevator pitch: three minutes, four
  kinds of input, and one moment where the input changes mid-demo without a restart.

The idea that ties it together: **every demo reads one input layer, and never reads a device.** A
switch, a breath, a head tilt, a hum and a keyboard zone all arrive as the same few channels, each
calibrated to the player's own range. Any source that supplies what a demo needs can play it.
That is the platform story, and it is what testers will push on hardest.

Read this file by section; the hand-off names the sections a task needs. Paths are relative to the
project folder.

## Decided by Jon

From the brainstorm with Claude (2026-10-23):

1. **Physical access only** for now: motor, reach, strength, tremor, fatigue, and sight and
   hearing where a demo calls for them. Cognitive and emotional access come later.
2. **One demo per scene,** plus a hub scene that opens the app.
3. **Every demo reads the input layer** (*The input layer*). No demo calls the Input System, the
   microphone or the camera directly.
4. **Built the Aurumworks way:** a copy of `Framework/Template`, `com.aurumworks.core` for the
   menus, settings, sound and self-test, with a hand-off doc and this file.
5. **The audiences above:** feedback from assistive technologists first, then the pitch.
6. **The name is Attune,** for the repository, the name players see, the namespace (`Attune`,
   since M1: see *Not an Aurumworks product*), the flags (`-attune…`) and the tuning
   (`AttuneTuning`). It fits where the project is headed (*Later*): a system that learns what a
   player can do and tunes itself to them.
7. **MIT licence,** for Attune and for every Aurumworks project Jon open sources. Everything in
   the repository must be redistributable under MIT or its own licence:
   - no Synty or other store assets (their licences forbid redistribution);
   - sound files are CC0 or CC BY, and credited;
   - a camera model's licence allows redistribution (MediaPipe's models are Apache 2.0).

   `Docs/licences.md` lists every third-party file.

From M0 (2026-10-23), each as the design recommended. The A-numbers are kept so references
elsewhere still hold.

- **A2. Platform:** Windows only, Mono, tested at 1920×1080 and 1280×800. No WebGL build.
- **A3. Signal maths in an engine-free assembly:** `Attune.Signals`, `noEngineReferences: true`,
  holding calibration, filters, press timing, dwell, scanning, the pitch and loudness detectors,
  and the adapters, all tested in EditMode. The Unity side (`Assembly-CSharp`) only reads devices
  and draws.
- **A4. Demos:** all ten in *Demos*, in the milestone order. If something slips, the Expression
  Spells and the Lighthouse Keeper are cut last.
- **A6. Eye-tracking hardware:** not in this build. The Lighthouse Keeper uses head pose; an eye
  tracker source comes later.
- **A7. Camera and microphone privacy:** frames and audio are analysed as they arrive and are never
  written to disk or sent anywhere, even in a self-test. The camera and microphone are on only in
  a demo that needs them, and the screen shows an indicator while either is on. (A36 changes
  this for the microphone: it is on while Breath or Hum is the source.)
- **A8. Feedback:** on pause, "How did that feel?" with Easy, Tiring, Frustrating, Fun and Skip,
  answerable by any input. One CSV row per answer (*Feedback*). Testers send the file themselves;
  nothing is uploaded.
- **A9. Profiles:** several named profiles on one machine, each holding a source, its calibration
  and the timing values, stored through the framework's `Settings` (*Profiles*).
- **A10. Look:** a high-contrast `UiTheme`, no text under 24 points, text contrast of at least 7:1,
  large targets, and no meaning carried by colour alone. Shapes are drawn in code or are Unity
  primitives; no third-party art unless Jon names a pack whose licence allows redistribution.
- **A11. Time:** no demo fails a player for being slow. A demo waits for the player, or its time
  pressure is optional and off by default. Every timing value can be changed in the profile.
- **A12. Sound:** Jon sources the files; cues are folders, as in Cairn, with test tones until the
  files arrive. The Echo Room starts with Unity's 3D panning; Steam Audio (Apache 2.0) is added for
  HRTF only if panning doesn't read, and that needs Jon's approval as a new package. Every sound
  that carries meaning has a caption or a visual cue, except in the Echo Room's screen-off mode.
- **A13. The pitch:** a *Pitch* playlist on the hub (Hub → Compass Sweep → Kite Breath →
  Head-Lean Sailing → Expression Spells), and a presenter action that changes the source mid-demo
  (*Live swap*).
- **A14. Claims:** no medical or therapeutic claims anywhere in the app or its text. The demos are
  for play and access, not rehabilitation.
- **A16. The framework on GitHub:** `Framework/` is made public under MIT before Attune's first
  public push (M4), and Attune then takes `com.aurumworks.core` by git URL with a tag. Before that,
  check that nothing in `Framework/` carries another game's private code or a licence that forbids
  sharing. Until then Attune takes `core` by path.
- **A17. When to push:** a private GitHub repository from M1, made public at M4 when the testers'
  build goes out. Keep LFS binaries small (sounds, no large art). Jon signs in with
  `gh auth login`; no password or token goes into a chat or a file.
- **Copyright** (2026-10-23): the MIT `LICENSE` reads *Copyright (c) 2026 Jon Goldman*. Jon may
  later set up a non-profit entity to hold it; until he says so, every copyright line names him.
- **Not an Aurumworks product** (2026-10-23, M1): Attune uses the Aurumworks framework but is
  not an Aurumworks product. Its files are under `Assets/Attune/`, its namespace is `Attune`
  (the signals assembly `Attune.Signals`), Player Settings' Company Name is *Jon Goldman* (so
  saves are under `LocalLow/Jon Goldman/Attune/`), its own settings keys start `Attune.`, and
  the README credits Jon. The framework keeps its names: `com.aurumworks.core`, the namespace
  `Aurumworks`, and the volume keys it makes (`Aurumworks.Volume.<name>`).

From the start of M2 (2026-10-23), each as recommended:

- **A18. What the Switch source must play.** A new adapter, **Press → Gesture by scanning**
  (*Adapters*), lets one switch play the Keyboard Garden and the Expression Spells. The Dwell
  Builder and the Lighthouse Keeper (`Point`) are exempt from the Switch test for now; a way to
  scan their cells or ships is designed when they are built (M4, M6), and their hub tiles say why
  a switch can't play them.
- **A19. Scenes and tuning made by an Editor script.** `SceneMaker` (*Attune > Make Scenes*) makes
  whichever of `Hub.unity`, the demo stub and `AttuneTuning.asset` is missing, never overwrites
  one, and sets Build Settings to start on the Hub. It runs headless while no Editor is open.
- **A20. A Long hold always goes back** with the Switch source: it closes the top menu, or opens
  the session menu in a demo. Demos use only Tap and Hold from a switch.
- **A21. Feedback asks on the first pause of each visit** to a demo. After an answer or Skip, later
  pauses in that visit go straight to the session menu; coming back to the demo asks again.

After Jon played M2 (2026-10-23), on the choices M2 made where the design was silent:

- **A22. A Hold picks in a menu,** like a Tap, when scanning with the Switch source. The scan stands
  still while the switch is down, so a press picks what was ringed when it began.
- **A23. First launch starts on Keyboard zones.** On a new profile's source page the Switch is the
  main action, so a player with one key reaches scanning by pressing it on each screen.
- **A24. Who's playing opens at every launch,** with the last profile first, so one press goes on.
  New profiles are named Player 1, 2…, and renamed in Settings. No delete yet.
- **A25. Next source** cycles through every source whose device is present and which can play the
  demo; the profile keeps the new source. The session menu's Next follows the hub's order outside
  the pitch.
- **A26. Scanning a slider:** a Tap steps it a tenth along and wraps round; text fields are
  skipped. Kept until testers' feedback says otherwise.

After the M3 spike (2026-10-23):

- **A5. Camera tracking: Unity's Inference Engine running Google's MediaPipe face models,** not
  the MediaPipe Unity Plugin. Both worked and gave the same answers; the Inference Engine is about
  as fast, adds less to the build, has no native library, comes from Unity's registry, and its
  models carry over to Godot and Unreal. The cost is our own detect, crop and track pipeline
  (about 300 lines). The package goes into `Packages/manifest.json` at M6 with Jon's approval, and
  the models' licence (Apache 2.0) into `Docs/licences.md`.
- **A27. Jon's own details stay out of the repository** ("be more careful with my info"): no
  hardware or device names from his machine, no measurements of his face, voice or movement, and
  no account details beyond what the repository needs. Reports say what was found, not his numbers
  (`CLAUDE.md`, *Privacy*).

At the start of M4 (2026-10-23), each as recommended:

- **A28. The demos are flat 2D, drawn in code,** top-down, on their own canvas under the menus,
  like the hub: high contrast, and the same at both sizes. No 3D primitives for now.
- **A29. The Dwell Builder scans for a switch** (row-column scanning, settling A18 for it): the
  palette and the plate's rows are scanned; a press picks the lit row, then the lit block or cell
  in it. Its needs become `Point` or `Press`: a source that points dwells, one that only presses
  scans. The Lighthouse Keeper stays exempt until M6.
- **A30. The Keyboard Garden grows while the press is held,** from the press's start, full grown at
  the Long hold threshold, for every source. Letting go keeps the height; with the Switch, holding
  past the Long hold still opens the session menu on release (A20).
- **A31. The Compass Sweep's needle stays on the heading it sailed,** so pressing again repeats the
  step at once, and it sweeps on from there.

After Jon played M4 (2026-10-23): "it works as expected", so the choices M4 made where the design
was silent stand, until testers say otherwise:

- **Compass Sweep:** the needle skips headings onto land or off the map; a Tap or a Hold sails;
  the needle waits after one sweep, whatever the profile's scan rounds, and moves at the profile's
  scan step; one island and five flags; every flag reached shows a banner for 4 s, then the same
  flags come back.
- **Keyboard Garden:** five plants a bed, a sixth replaces the oldest; a hold grows the newest
  plant; with a switch a Tap gives back the sliver its moment down grew. The beds are the
  keyboard quadrants for every source; the Face source's mapping is asked at M7.
- **Dwell Builder:** the first block starts picked; the picked block on a cell holding it takes
  it off; the rest zone is 7% of each edge plus the header's band; clicks do nothing, even with
  the Pointer. Scanning: the palette is the first row; a row's items get one round, then the rows
  come back at that row; the rows follow the profile's rounds and "from the top".

The testers' build's version (0.1.0) is still open.

At the start of M5 (2026-10-23), each as recommended:

- **A32 (replaced by A36). The microphone is on while a demo is played with the Breath or Hum
  source, and while either is calibrated,** always with the indicator, and off on the hub
  otherwise. The menus are never driven by breath or hum: keyboard, pointer or the Switch, as
  before. A sip-and-puff device that appears as keys already plays through the Switch source.
- **A33. The Kite Breath and the Hum Flyer are a slow loop:** the ribbons or rings hang at
  different heights and drift past at a slow, steady speed (in the tuning). One missed comes round
  again, so missing costs nothing (A11). Every one reached, a banner, then a new round, as in the
  Compass Sweep.
- **A34. When the hum stops, the Level glides down** at the profile's fall rate, as the kite falls.
  Rest stays 0, and any hum is a press, so the Hum source supplies `Press` as well and plays the
  press demos.
- **A35. Noisy room** is a toggle on the profile's Timing page. When it is on, the Breath source
  ignores anything under three times the room's loudness measured at calibration. It is saved
  with the profile and applies wherever Breath plays.

After Jon replayed M5 (2026-10-23): "the most recent interaction mode doesn't control the menus,
specifically the breath-only control." Each as recommended:

- **A36. Breath and Hum scan the menus, everywhere,** as the Switch does (*Scanning*): a puff or
  a hum picks the ringed control, a long one (the profile's Long hold) goes back, and in a demo
  opens the session menu. So the microphone is on whenever Breath or Hum is the profile's source,
  on the hub too, always with the indicator (this replaces A32 and changes A7 for the
  microphone); it goes off when another source takes over. While a calibration step measures,
  the menus aren't scanned, so the breath or hum it measures neither picks nor goes back. While the
  microphone has a problem, Breath and Hum don't scan, so a helper can still use the keyboard.
- **A37. In the Kite Breath and the Hum Flyer, the menu takes a longer breath or hum:** 5 s by
  default (`AttuneTuning`, never less than the profile's Long hold), because a long one is how
  those demos are played. The hint says so ("Long breath (5 s): menu"). Every other demo, and the
  Switch everywhere, keeps the Long hold (A20).

After Jon played M5 with Hum (hub, menus, calibration, Hum Flyer), 2026-10-23: "it works well
enough until we get proper player feedback."

- **A38. Hum's choices stand until testers' feedback:** the hints ("Hum: choose · Long hum:
  back"; "Stop to choose. Keep going to go back." while held), the Hum Flyer's 5 s menu hum (A37);
  six rings, the whole sky climbed or dived in 0.5 s with a tilt, a ring passed within 8% of its
  middle, drifting at 0.07 screen widths a second; a gap of 0.15 s doesn't end a hum, a hum must
  be 8 dB over the room and never under −55 dBFS, calibration needs the two notes 2 semitones
  apart, and the range is 110–330 Hz before calibration. Revisit them when testers report.

Jon played M5 with Breath, 2026-10-23, asked for no changes, and had it merged.

- **A39. Breath's choices stand until testers' feedback,** as Hum's do (A38): the hints ("Puff:
  choose · Long breath: back"; "Stop to choose. Keep going to go back." while held), the Kite
  Breath's 5 s menu breath (A37), and Breath and Hum not scanning while the microphone has a
  problem; six ribbons, the whole sky climbed in 0.6 s and fallen in 3 s, a ribbon touched within
  9% of the sky's height, drifting at 0.07 screen widths a second; loudness over the last 50 ms,
  and a room of −60 dBFS and a breath of −25 before calibration; the microphone indicator at the
  top centre. Revisit them when testers report.

Going public, 2026-10-23 (Jon, each as recommended):

- **A40. Attune goes public with a fresh history:** one squashed commit of `main` as it stood,
  authored with Jon's GitHub noreply email, in a new public `ionAurum/Attune`. The old repository
  is renamed `ionAurum/Attune-history` and stays private (a backup Jon may delete), because
  GitHub keeps commits that were force-pushed over reachable by their hash. Every later commit is
  authored with the noreply email. The Player version is 0.5.0.
- **A41. `core` is public as its own repository,** `ionAurum/aurumworks-core`, MIT, holding
  snapshots of `../Framework/com.aurumworks.core` (never Framework's history). Attune takes it by
  git URL and tag (A16). `com.aurumworks.online` and the template stay private.
- **A42. S1's turn lasts while the switch is held:** at rest, straight; a press turns while it is
  held, and the next press turns the other way.

## Still open

- **Steer from a switch.** Today it follows *Adapters* ("a `Steer` centred on half the level"), so
  a switch at rest steers full left. Jon (2026-10-23): having to keep pressing to stay centred
  isn't good, and he wants to try the alternatives before choosing. Decided before Head-Lean
  Sailing (M6). The candidates Jon named (2026-10-23), one iteration each (*Milestones*, S1–S4):
  1. **Each press turns the other way.** At rest, straight; a press turns, to the other side
     from the last press.
  2. **Tap and Hold turn opposite ways.** At rest, straight. These are press lengths, not two
     inputs at once, so this is not a chord.
  3. **Scanned direction.** Left, straight and right are scanned in turn (`Scanner`), and a pick
     holds until the next press.
  4. **Nudge.** Each press moves the heading one step, and the boat holds that heading.

  Each is a Press → Steer adapter in `Attune.Signals` with tests, and a choice Jon can pick and
  play. Jon rules after playing them. Each iteration settles its own open details with Jon before
  it is built (see the hand-off).

## The input layer

### Channels

What a demo can ask for. A source supplies some of them; adapters derive others (*Adapters*).

| Channel | Shape | Examples |
|---|---|---|
| `Level` | 0–1, rest is 0 | Breath strength, hum pitch within the player's range, how far the head leans one way |
| `Steer` | −1 to 1, rest is 0 | Head tilt, keyboard left/right half, mouse x around its start point |
| `Point` | A position on the screen, 0–1 on each axis | Mouse, trackpad, head pointer |
| `Press` | Down and up events, classified as **Tap**, **Hold** or **Long hold** by duration | A switch, any key, a breath puff, a chosen expression |
| `Gesture` | A named discrete event from the player's own set | Keyboard zones, facial expressions |
| `Select` | One "do it" event | A Tap, a finished dwell, or a scan pick |

A demo declares the channels it needs (`DemoNeeds`). The hub offers a demo only to sources that can
supply them, directly or through an adapter, and says why when it can't.

### Sources

Each source is a class that reads one device and supplies channels. `InputRouter` holds the
active source and publishes the channels; it is the only object that knows which source is active.

| Source | Device | Supplies | Milestone |
|---|---|---|---|
| Keyboard zones | Keyboard | `Press` (any key), `Steer` (left/right half), `Gesture` (four quadrants) | M2 |
| Pointer | Mouse, trackpad | `Point`, `Steer` (x from where the movement started), `Press` (any button) | M2 |
| Switch | One chosen key or mouse button (USB switch interfaces usually appear as one of these) | `Press` | M2 |
| Gamepad | Gamepad | `Steer`, `Point` (stick), `Press` | M2 (cheap through the Input System) |
| Breath | Microphone loudness | `Level` (from the room to a comfortable breath) | M5 |
| Hum | Microphone pitch | `Level` (lowest to highest comfortable note), `Press` (any hum, A34) | M5 |
| Head | Webcam head pose | `Steer` (roll), `Level` (lean), `Point` (yaw and pitch) | M6 |
| Face | Webcam face landmarks | `Gesture` (the expressions the player chose), `Press` (one chosen expression) | M7 |
| Scripted | A script of timed values | Any channel | M2 (the self-test and demo playback) |

No source ever needs two inputs held at once. Combinations are sequences, not chords.

Later sources, not in this build: non-verbal sounds as gestures ("pop", "sss"), an eye tracker, a
sip-and-puff device that appears as keys (it already works through the Switch source), and
pen pressure.

### Adapters

These let any source play any demo. They are pure functions in `Attune.Signals`.

- **Level → Press:** crossing a threshold, with hysteresis, is a press. A breath puff becomes a
  switch.
- **Press → Level:** holding ramps the level up, and releasing lets it fall, at rates from the
  profile. A switch can fly the kite.
- **Point → Select by dwell:** resting inside a target for the dwell time selects it. Moving away
  cancels. A *rest zone* never selects, so looking around is always safe.
- **Press → Select by scanning:** options highlight one at a time and a press picks the
  highlighted one (*Scanning*).
- **Steer ↔ Level:** a one-sided `Level` from a `Steer`, or a `Steer` centred on half the level.
- **Press → Gesture by scanning** (A18): the player's gestures highlight one at a time and a Tap
  sends the highlighted one.

### Calibration

Calibration is short, and it can be skipped. A player can redo it from the session menu at any
time.

- **Rest:** keep still or quiet for 2 seconds. This records the noise floor and the rest
  position.
- **Reach:** go as far as is *comfortable* for 3 seconds, each way that applies. This sets 100%.
  Steer each way, the loudest comfortable breath, the lowest and highest comfortable hum.
- **Normalise:** `(value − rest) / (reach − rest)`, clamped, with a dead zone around rest. A 5°
  tilt and a 30° tilt can both be full input.
- **Smooth:** a One Euro filter (Casiez et al., 2012) per axis, tuned per source. It smooths
  tremor at slow speeds and keeps lag low at high speeds.
- **Press timing:** the Tap, Hold and Long hold thresholds, the dwell time and the scan speed come
  from the profile, with defaults that are generous.

### Profiles

A profile is a name, a source, that source's calibration and the timing values. Profiles are
stored through `Settings.SetString` as JSON (`Attune.Profile.<n>`, plus
`Attune.Profiles` for the list). Never through `PlayerPrefs` directly. The hub's first
screen picks or makes one.

### Scanning

`Scanner` (in `Attune.Signals`) steps through options on a timer. The Unity side has `ScanDriver`,
which moves the EventSystem's selection through the framework's menus, so every menu built with
the UI kit can be scanned without changing `core`. When the Switch, Breath or Hum source is active
(A36; the hints name a breath's presses "Puff" and "Long breath", a hum's "Hum" and "Long hum"):

- a Tap or a Hold picks (Submit, A22); the scan stands still while the switch is down;
- a Long hold goes back (Cancel);
- the speed, how many rounds to scan before pausing, and whether to start from the top again
  come from the profile.

The demos use the same `Scanner` for scanning that happens in the world (the Compass Sweep's
needle).

### Live swap

A `Presenter` action map has one action, *Next source* (F2, and Select on a gamepad). It changes the
active source mid-demo without a reload, using that source's saved calibration. The session menu
has the same thing as a button. This is the pitch's key moment.

### What it hears

An overlay, toggled from the session menu, shows each channel live: raw and calibrated, the
thresholds, the dwell ring, and the current scan step. Testers will want to see what the system
reads from their input.

## The hub

The first scene. It is built with `MainMenuFrame`:

- **Who's playing:** pick a profile or make one, which leads into choosing a source and
  calibrating it.
- **Demos:** one tile per demo, with its name, a one-line description, and icons for the channels
  it needs. A tile the current source can't play says why, and offers to switch source.
- **Pitch:** plays the pitch path in order. Each demo has *Next* in its session menu.
- **Settings:** the framework's page, plus profile rows (source, recalibrate, timing values).
- The menus can be scanned whenever the Switch source is active. **The hub is the first scanning
  demo.**

## Demos

Each demo is a small scene with no fail state unless A11 says otherwise. For each one: what the
player does, the channels it needs, what it proves, and how the self-test shows it works.

### 1. Compass Sweep (`Press`)

A small island map. A compass needle sweeps around eight headings, and a press sends a little boat
one step that way. Reach the flags. The needle waits after a full sweep with no press, so missing
it costs nothing. After a step it stays on that heading (A31).
**Proves** that scanning can be part of the world rather than a menu workaround.
**Check:** scripted presses at the right moments reach every flag.

### 2. Keyboard Garden (`Gesture`, `Press`)

A garden split into four beds. A gesture plants in its bed (each keyboard quadrant is one bed),
and holding makes the plant grow taller (A30: full grown at the Long hold). Any `Gesture` source plays it, the Face source included.
**Proves** that coarse input, a fist, an elbow or a mouth stick on a big zone, can still be
expressive.
**Check:** each scripted gesture plants in its own bed; a hold grows the plant.

### 3. Dwell Builder (`Point`, `Select` by dwell)

An 8×8 build plate and a palette of four blocks. Dwell on a block to pick it and dwell on a cell
to place it. A ring shows dwell progress, moving away cancels, and the border is a rest zone. There
are no clicks anywhere. A source that only presses scans the rows, then the blocks or cells (A29).
**Proves** dwell selection, and the answer to the "Midas touch" problem.
**Check:** a scripted path places the planned blocks; resting in the rest zone places nothing;
one key places a block by scanning.

### 4. Kite Breath (`Level`)

Breathe out to lift the kite. It falls gently when you stop. Ribbons hang at different heights to
touch. A held switch plays it too (Press → Level).
**Proves** that breath gives smooth, analog control.
**Check:** a scripted level curve touches every ribbon. EditMode tests cover the loudness
detector and noise floor on synthetic signals.

### 5. Hum Flyer (`Level`)

Hum higher to climb and lower to dive, within the range you calibrated, and fly through rings.
**Proves** a hands-free analog axis.
**Check:** a scripted level path passes every ring. EditMode tests cover the pitch detector on
synthetic tones from 80 to 800 Hz, within 2%, with and without noise.

### 6. Head-Lean Sailing (`Steer`)

A calm lake and three buoys to round. Lean or tilt to steer. Wind and waves are gentle, and storms
are an option that is off by default.
**Proves** a calibrated range of motion.
**Check:** a scripted steer completes the course. Run by hand with the Head source on Jon's
camera.

### 7. Tremor Loom (`Point` or `Steer`)

Draw threads across a loom. The game reads the intended path (smoothed), and a small, bounded
part of the jitter becomes the weave's texture. A toggle shows the raw path beside the read
path.
**Proves** that precision isn't required, and turns tremor into an aesthetic choice rather than an
error.
**Check:** a scripted path with synthetic tremor gives a read path within tolerance of the
intended one, and a finished cloth.

### 8. Expression Spells (`Gesture`)

The player first shows which expressions they can make comfortably, from the ones the Face source
detects (brow raise, mouth open, smile, cheek puff, wink each side). Those become their alphabet.
A spell is two gestures in a row, cast at lanterns to light them. Any `Gesture` source plays it,
keyboard zones included.
**Proves** a personal input vocabulary chosen by the player.
**Check:** scripted gestures light every lantern.

### 9. Lighthouse Keeper (`Point`, `Select` by dwell)

Night sea. The beam follows `Point`, and ships steer toward the light. Dwelling on a ship guides it
into harbour. Looking around is always safe, and ships wait at sea rather than wreck.
**Proves** gaze-style control where looking never commits an action. Head pose now; an eye tracker
later (A6).
**Check:** a scripted point path brings every ship in.

### 10. Echo Room (`Steer`, `Press`)

Audio first. Turn with `Steer`, take a step with a Tap, and send a ping with a Hold. The ping's
echoes say where the walls are. Find the chime. A screen-off mode blacks out the picture; the
screen-on mode shows rings where sounds come from.
**Proves** spatial audio navigation for blind and low-vision players. Sighted players can play it
too.
**Check:** a scripted route reaches the chime; every ping plays its echoes (the sound report).

## Feedback

On pause, A8's question. Each answer is one CSV row in
`LocalLow/Jon Goldman/Attune/Feedback/feedback-<date>.csv`:

`time, profile, demo, source, seconds in demo, presses, answer, calibration summary, version`

The *Feedback* page on the hub says where the file is, and opens the folder. A self-test writes
to its own folder (`-attuneFeedbackFolder`) and never to the tester's. Testers' files are read,
never changed or deleted.

## Building in code

As in Cairn, each scene holds its environment and little else. The game's systems are made in code
as the scene loads, by a bootstrap. The tuning is `Assets/Attune/Resources/AttuneTuning.asset`
(`AttuneTuning`), with one block per source and per demo, which Jon tunes in the Inspector.
`InputRouter`, `ScanDriver`, the overlay and the feedback prompt are made with the menus, so they
last for the whole session.

## Checks

- **EditMode tests:**
  - in `Attune.Signals`: calibration and normalising, the dead zone, the One Euro filter, press
    classification, the dwell timer and rest zone, the scanner's timing, every adapter, the
    loudness and pitch detectors on synthetic signals;
  - on the Unity side: profiles round-trip through `Settings`, CSV rows escape commas and quotes,
    and every demo's `DemoNeeds` can be met by the Switch source through adapters, but for the
    `Point` demos (A18);
  - `Assert.That` only.
- **The check** (`bash Tools/attunecheck.sh <name>`, `-attuneCheck`, on the framework's
  self-test harness):
  - walks the hub by keyboard, then by scanning with the Switch source;
  - makes and picks a profile;
  - plays every demo to its goal with the Scripted source;
  - changes source mid-demo;
  - answers the feedback prompt and checks the row;
  - checks that no menu text is under 24 points;
  - plays every sound cue on test tones;
  - exits 0 only if every check held and nothing logged an error.
  
  It needs no camera or microphone, and takes screenshots of every demo. It grows with each
  milestone.
- **Device check** (`-attuneDevices`, by hand on Jon's machine): opens the camera and microphone,
  shows the overlay for each real source, and logs what it found (the camera's resolution and
  frame rate, the microphone's sample rate). It writes nothing of what it sees or hears.
- **Mic file** (`-attuneMicFile <wav>`): feeds a recording through the real Breath and Hum
  pipeline in place of the microphone, so a real breath or hum can be replayed in a check. The
  recordings in `Tools/Recordings/` are Jon's.

## Risks

- **Camera tracking is the biggest unknown.** Plugin size, native libraries, Mono versus IL2CPP,
  speed on a modest laptop, lighting, glasses and beards. The M3 spike chose the Inference Engine
  (A5); a modest laptop, poor light, glasses and beards are still untested (M6, M7).
- **Windows privacy settings** can block the camera or microphone for desktop apps. The device
  check and the hub must say this plainly rather than show a dead meter.
- **Rooms are loud** at a pitch. The noise floor is measured at calibration, and the Kite has a
  Noisy room option (A35) that raises the threshold.
- **Calibration fatigue.** Keep it short, make it skippable, and remember it per profile.
- **Scope.** Ten demos is a lot; A4's cut order applies.

## Later

Not in this build. This is the direction the name points to, so M2's calibration and profiles are
built to grow into it, not to be replaced.

- **An interview, not a form.** A short guided conversation that asks what a player can do
  comfortably, never what their condition is ("Can you tilt your head? Show me."). It holds
  nothing that looks like medical data.
- **Trying, not telling.** The interview asks the player to blow, hum, tilt and press, measures
  each, and suggests the source and timing that suit them. Calibration becomes part of the
  conversation.
- **Tuning as you play, with consent.** The system notices dwells that keep cancelling, presses
  that just miss, and inputs slowing over a session. It *offers* a change ("Slow the scan a
  little?") and never changes anything silently.
- **Profiles that travel.** An open profile format (MIT) that other games can read, the other
  Aurumworks games first. "Built on Attune."

## Milestones

| # | What | Checked by |
|---|---|---|
| M0 | Rulings A2–A14, A16 and A17. Nothing built. **Done 2026-10-23**; A5 decided after M3 | Jon answers |
| M1 | Project from the template, `CLAUDE.md`, this file, the hand-off, README; the root `CLAUDE.md` lists Attune | The check (the template's walk) |
| M2 | The input layer: `Attune.Signals`, `InputRouter`, Keyboard zones, Pointer, Switch, Gamepad and Scripted sources, calibration, profiles, adapters, scanning, the overlay, live swap, feedback, the hub | Tests, the check, Jon scans the hub with one key |
| M3 | **Spike: camera tracking.** Throwaway. Report and stop. **Done 2026-10-23**: A5 | Jon decides A5 |
| M4 | Compass Sweep, Keyboard Garden, Dwell Builder. **A first build for testers** (`Tools/testerbuild.sh`). Built 2026-10-23; A28–A31. Jon played it the same day | The check, Jon plays, then the testers |
| M5 | Breath and Hum sources; Kite Breath, Hum Flyer. Built 2026-10-23; A32–A39. **Done 2026-10-23**: Jon played Hum and Breath | Tests, the check, Jon by mouth |
| S1–S4 | Steer from a switch (*Still open*): one candidate per iteration, each played by Jon before the next; Jon rules after S4. Before M6 | Tests, Jon plays each with one key |
| M6 | Head source; Head-Lean Sailing, Lighthouse Keeper | The check, Jon on his camera |
| M7 | Face source; Expression Spells | The check, Jon on his camera |
| M8 | Tremor Loom, Echo Room | The check, Jon plays |
| M9 | The pitch: the Pitch playlist polished, a release build with `How To Run.txt` and `Licences.txt` | Jon gives the pitch to someone |

**Done when**, per milestone:

- **M1:** the project builds, the template's menu walk passes as `-attuneCheck`, and a fresh chat
  can start from the hand-off.
- **M2:** every adapter and detector has tests; the hub can be walked to a demo stub with one key
  by scanning; a source changes mid-scene with no reload; a feedback row lands in the check's
  folder; switching profile restores its source and calibration.
- **M3:**
  - the report covers each option tried (MediaPipe Unity Plugin, Unity's Sentis/Inference Engine
    with a face-landmark model, anything else found):
    - frame rate and latency on Jon's machine;
    - build size;
    - Mono or IL2CPP;
    - licence;
    - what head pose and which expressions it gives;
  - with a recommendation for A5. **Hard stop.**
- **M4 to M8:** each demo meets its *Check* in the check, and Jon has played it with a real
  source. M4 also ships a zip for testers.
- **M9:** the release build passes the check at both sizes, and the pitch path runs start to
  finish with a live swap in under three minutes.

**Nothing goes into the framework until two games use it,** so everything stays in Attune.
Candidates to raise in the M9 report: `ScanDriver` (scanning for every Aurumworks menu), and the
input layer itself. Accessibility across every Aurumworks game is also a pitch point.
