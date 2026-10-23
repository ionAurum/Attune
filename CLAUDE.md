# Attune

Tech demos of game mechanics designed from the start for players with physical disabilities:
one Unity scene per demo, plus a hub. Every demo reads one input layer (switch, keyboard zones,
pointer, breath, hum, head pose, face), calibrated to the player's own range. For feedback from
assistive technologists, and as an early pitch. Unity 6000.0.78f1 (URP 17.0.4), Input System
1.19.0, uGUI with TextMeshPro. Windows.

**Read `Docs/Attune_Handoff.md` first.** It has the state, what comes next and what to read for
it. The design and Jon's rulings are in `Docs/Attune_Design.md`; read the sections the hand-off
names, not all of it.

## Layout
- `Assets/Attune/Signals/`: `Attune.Signals`, plain C# with `noEngineReferences: true`
  (ruling A3): calibration, filters, press timing, dwell, scanning, detectors, adapters.
- `Assets/Attune/Scripts/`: the Unity side, in Assembly-CSharp, namespace
  `Attune`: sources, `InputRouter`, `ScanDriver`, the hub, the demos, feedback.
- `Assets/Attune/Scenes/`: `Hub.unity` and one scene per demo (`DemoStub.unity` until a demo has
  its own). `SceneMaker` (*Attune > Make Scenes*, or headless with
  `-executeMethod Attune.SceneMaker.MakeAndExit`) makes whichever scene or `AttuneTuning.asset`
  is missing, never overwrites one, and sets Build Settings (ruling A19).
- Attune is not an Aurumworks product (design, *Not an Aurumworks product*), but it uses the
  Aurumworks framework. Code shared with the Aurumworks games is in `com.aurumworks.core` (namespace
  `Aurumworks`): `Settings` (every PlayerPrefs value goes through it),
  `SoundPlayer` and its cues, the UI kit and `UiTheme`, the menu shell, the self-test harness.
  Attune takes it by git URL and tag from the public `ionAurum/aurumworks-core` (A16, A41), not
  from `../Framework/`. A change to it is made in `../Framework/` (follow its `README.md`, commit
  there first), published with `../Framework/Tools/publish-core.sh` under a new version, and then
  Attune's tag in `Packages/manifest.json` moves (Jon's approval, as for any manifest change).
  Nothing goes into the framework until two games use it.
- The menus are built in code with the UI kit, on `Assets/Attune/Resources/Menus.prefab`.

## Never edit
- `.meta` files. Unity owns them.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `.vs/`, the generated `*.csproj` / `*.slnx`.
- Without explicit approval: `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings/`,
  and serialized assets (`.unity`, `.prefab`, `.asset`, `.mat`, `.mixer`, `.inputactions`).
  Describe the Inspector change for Jon to make, or ask to make it.
- No package upgrades unless asked. A new package or plugin (camera tracking) needs Jon's approval
  and an entry in `Docs/licences.md`.

## Privacy (rulings A7 and A27)
- Jon's own details stay out of files, commits and reports (A27): no hardware or device names
  from his machine, no measurements of his face, voice or movement, no account details beyond
  what the repository needs. Report what was found ("a fast laptop", "every expression but cheek
  puff registered"), not his numbers.
- Camera frames and microphone audio are analysed as they arrive and never written to disk or
  sent anywhere, in play or in a self-test. Screenshots never include the camera image.
- The camera is on only in a demo that needs it; the microphone is on while Breath or Hum is the
  source, since they scan the menus too (A36). The screen shows an indicator while either is on.
- Testers' feedback files are read, never changed or deleted.

## Open source (ruling 7; A16 and A17)
- Attune is MIT licensed and will be public on GitHub. Nothing goes into the repository that its
  licence doesn't allow to be redistributed: no Synty or other store assets, and every
  third-party file listed in `Docs/licences.md`.
- Never write a password, token or key into a file, a commit or a chat. Pushing uses Jon's own
  `gh auth login`.

## Unity rules
- One MonoBehaviour or ScriptableObject per file, and the file name must match the class name.
- Never rename or move assets from the file system; ask Jon to do it in the Editor.
- New files: create only the file; Unity writes its `.meta` on import (the check's build does).
- Inspector values override code defaults. Tuned values live in `AttuneTuning.asset`; never
  overwrite Jon's tuning.
- Statics and static events are reset in a
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method.
- Whatever reads input checks `MenuFocus.IsAnyOpen` and does nothing while a menu is open.
- **No demo reads a device.** Demos read channels from `InputRouter`; only sources touch the Input
  System, `Microphone` or `WebCamTexture`.
- **No chords.** Nothing needs two inputs held at once.
- **No timing failure** unless the player turned it on (ruling A11).

## Code style
`../Framework/README.md` (*Code style*): braces on their own lines, `_camelCase` private fields,
`[SerializeField] private` Inspector fields, `<summary>` on public members.

## The check
`bash Tools/attunecheck.sh <name>` builds the Windows player and runs it with `-attuneCheck`
(`Docs/Attune_Design.md`, *Checks*). It needs a window, refuses while an Editor has the project
open (`Temp/UnityLockfile`), and puts the game's registry key back afterwards. Screenshots, the log
and feedback rows land in `%TEMP%\attune-runs\<name>`. `NOBUILD=1` reuses the last build;
`SIZE=1280x800` runs the small size. With the Editor open, run it on a scratch copy as Cairn does
(`../Cairn/CLAUDE.md`, *The bake, the looks and the tests*).

EditMode tests: `"$U" -batchmode -nographics --burst-disable-compilation -projectPath <project>
-runTests -testPlatform EditMode -testResults <xml> -logFile <log>`. `Assert.That` model only.

## After every change: editor checklist
Claude can't run the Unity Editor. End every change with what Jon should verify:
1. It compiles (Console free of errors).
2. Inspector fields to assign or re-check, and assets or `.meta` files Unity will create.
3. What to test in play, and with which source.
State plainly what couldn't be verified (a real camera or microphone, for a start).

## Workflow
- **One milestone at a time.** At the end of each, stop and report what was built, which tests
  prove it, deviations from the design, and open questions. Wait for Jon's go-ahead.
- **M0 and M3 are hard stops.** M0 asks the rulings; M3's spike is reported before any camera demo.
- **Don't invent design.** If the design is silent or ambiguous, ask. A ruling of Jon's goes into
  `Docs/Attune_Design.md` (*Decided by Jon*) with its date.
- Git with LFS, branch per milestone (`m2-input`), merged to `main` once Jon has played it.
- `Docs/Attune_Handoff.md` is the hand-off doc. Update it at the end of every turn that changes
  state, so a fresh chat can start from it at any time.
