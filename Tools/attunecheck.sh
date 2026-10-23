#!/bin/bash
# Builds the Windows player from this project and runs its self-test, the check (Assets/Attune/Scripts/MenuCheck.cs).
# Unity runs headless on the project itself, so the build refuses while an Editor has it open.
# The screenshots and the player's log land in $TEMP/<game>-runs/<name>; the player stays in $TEMP/<game>-runs/build.
# Usage: attunecheck.sh <name>
#   NOBUILD=1 reuses the last build (an Editor may be open then); BUILDONLY=1 stops after building;
#   SIZE=<width>x<height> sizes the window (1920x1080 unless given; 1280x800 is the small size, ruling A2).
# Exits with the player's exit code: 0 when every check held and no error was logged.
set -u
# The game's own: its company and product names (Player Settings), its namespace, and the prefix of its flags.
company="Jon Goldman"
product="Attune"
namespace="Attune"
game="attune"

name=${1:?usage: attunecheck.sh <name>}
size="${SIZE:-1920x1080}"
width="${size%x*}"; height="${size#*x}"
case "$width$height" in ''|*[!0-9]*) echo "SIZE is <width>x<height>, as 1280x800."; exit 2;; esac
src="$(cd "$(dirname "$0")/.." && pwd)"
runs="$TEMP/$game-runs"
unity="/c/Program Files/Unity/Hub/Editor/6000.0.78f1/Editor/Unity.exe"
w() { cygpath -w "$1"; }
mkdir -p "$runs"
if [ -z "${NOBUILD:-}" ]; then
  if [ -e "$src/Temp/UnityLockfile" ]; then
    echo "An Editor has the project open. Close it, or reuse the last build with NOBUILD=1."; exit 2
  fi
  # The build must leave the working tree as it found it, but for the .meta files of new scripts.
  before="$(git -C "$src" status --porcelain; git -C "$src" diff | sha1sum)"
  rm -rf "$runs/build"
  "$unity" -batchmode -quit -projectPath "$(w "$src")" \
    -executeMethod "$namespace.PlayerBuild.BuildWindows" "-${game}BuildPath" "$(w "$runs/build")" \
    -logFile "$(w "$runs/build.log")"
  code=$?
  after="$(git -C "$src" status --porcelain; git -C "$src" diff | sha1sum)"
  if [ "$before" != "$after" ]; then
    echo "The build changed the working tree:"; diff <(echo "$before") <(echo "$after")
  fi
  if [ $code -ne 0 ] || [ ! -f "$runs/build/$product.exe" ]; then
    echo "BUILD FAILED ($code)"; grep -E "error CS|Error" "$runs/build.log" | head -20; exit 1
  fi
  echo "built"
fi
[ -n "${BUILDONLY:-}" ] && exit 0
[ -f "$runs/build/$product.exe" ] || { echo "No $product.exe in $runs/build."; exit 1; }
out="$runs/$name"
rm -rf "$out"; mkdir -p "$out/shots"
# The player shares its registry key with the game as it is played: PlayerPrefs, and the window's mode and size,
# which Unity keeps there too and the flags below change. It is put back as it was.
key="HKCU\\Software\\$company\\$product"
saved=
reg export "$key" "$(w "$out/prefs.reg")" //y > /dev/null 2>&1 && saved=1
"$runs/build/$product.exe" -screen-fullscreen 0 -screen-width "$width" -screen-height "$height" "-${game}Check" \
  "-${game}Screenshots" "$(w "$out/shots")" "-${game}FeedbackFolder" "$(w "$out/feedback")" -logFile "$(w "$out/player.log")"
code=$?
reg delete "$key" //f > /dev/null 2>&1
[ -n "$saved" ] && reg import "$(w "$out/prefs.reg")" > /dev/null 2>&1
echo "exit $code at $size, $(ls "$out/shots" | wc -l) shots, $(cat "$out"/feedback/*.csv 2>/dev/null | tail -n +2 | wc -l) feedback rows"
grep -E "Self-test (failed|stalled|finished)|Exception" "$out/player.log" | head -20
exit $code
