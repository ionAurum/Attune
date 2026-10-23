# Attune

*Any action, tuned to you.*

Attune is a set of small tech demos of game mechanics designed from the start for players with
physical disabilities. Each demo is one short scene. Every demo reads the same input layer, so a
switch, a keyboard zone, a pointer, a breath, a hum or a head tilt can all play it, each
calibrated to the player's own range. The demos are for play and access; they make no medical or
therapeutic claims.

It is made by Jon Goldman for feedback from assistive technologists, and as an early look at the
idea. It is not a game, and it is not finished.

## Status

An early build. The project is set up and its menus run; the input layer and the hub come next,
then the demos one or two at a time. A build for testers is planned once the first three demos
are in.

## The demos

| Demo | Played with |
|---|---|
| Compass Sweep | a single press |
| Keyboard Garden | zones of the keyboard |
| Dwell Builder | pointing and resting |
| Kite Breath | breath |
| Hum Flyer | humming |
| Head-Lean Sailing | leaning the head |
| Tremor Loom | pointing, with smoothing |
| Expression Spells | facial expressions |
| Lighthouse Keeper | head pointing and resting |
| Echo Room | sound and a single press |

Any source that supplies what a demo needs can play it: a single switch can scan to a choice, a
breath can steer. Nothing needs two inputs held at once, and no demo fails a player for being
slow unless they turn timing on.

## Privacy

The camera and microphone are on only in a demo that needs them, and the screen says so while
either is on. What they see and hear is analysed as it arrives and never saved or sent anywhere.
Feedback answers are saved to a file on your own machine, which you can choose to send back.

## Building it

Unity 6000.0.78f1 (URP) on Windows. The project takes the Aurumworks framework's package
`com.aurumworks.core` from a folder beside it (`../Framework/`), so it does not open on its own
yet; the package will be published before the repository is.

`bash Tools/attunecheck.sh <name>` builds the Windows player and runs its self-test (Git Bash,
with no Editor open on the project). Screenshots and the log land in `%TEMP%\attune-runs\<name>`.

## Licence

MIT (`LICENSE`). Third-party files and their licences are listed in `Docs/licences.md`.
