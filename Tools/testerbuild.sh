#!/bin/bash
# Builds the testers' zip (design, Milestones, M4): the Windows player, How To Run.txt and Licences.txt, zipped as
# Builds/Attune-v<version>-windows.zip. The version is Project Settings > Player > Version; bump it before sharing.
# Unity runs headless on the project itself, so this refuses while an Editor has it open.
# Usage: testerbuild.sh   (NOBUILD=1 zips the last build in Builds/Tester instead of building again)
set -u
product="Attune"
src="$(cd "$(dirname "$0")/.." && pwd)"
unity="/c/Program Files/Unity/Hub/Editor/6000.0.78f1/Editor/Unity.exe"
w() { cygpath -w "$1"; }
version="$(grep -m1 'bundleVersion:' "$src/ProjectSettings/ProjectSettings.asset" | sed 's/.*bundleVersion: *//; s/[[:space:]]*$//')"
[ -n "$version" ] || { echo "No version in ProjectSettings."; exit 1; }
out="$src/Builds"
player="$out/Tester/$product"
zip="$out/$product-v$version-windows.zip"
mkdir -p "$out"

if [ -z "${NOBUILD:-}" ]; then
  if [ -e "$src/Temp/UnityLockfile" ]; then
    echo "An Editor has the project open. Close it first."; exit 2
  fi
  rm -rf "$out/Tester"
  "$unity" -batchmode -quit -projectPath "$(w "$src")" \
    -executeMethod Attune.PlayerBuild.BuildWindows -attuneBuildPath "$(w "$player")" \
    -logFile "$(w "$out/tester-build.log")"
  code=$?
  if [ $code -ne 0 ] || [ ! -f "$player/$product.exe" ]; then
    echo "BUILD FAILED ($code)"; grep -E "error CS|Error" "$out/tester-build.log" | head -20; exit 1
  fi
fi
[ -f "$player/$product.exe" ] || { echo "No $product.exe in $player."; exit 1; }

# Nothing a player doesn't need: Burst's debug information stays out.
rm -rf "$player"/*_BurstDebugInformation_DoNotShip

sed "s/{VERSION}/$version/" "$src/Tools/Tester/How To Run.txt" > "$player/How To Run.txt"
{
  echo "LICENCES"
  echo
  echo "Attune, under the MIT licence:"
  echo
  cat "$src/LICENSE"
  echo
  echo "------------------------------------------------------------------------"
  echo
  echo "Liberation Sans, the font, under the SIL Open Font License 1.1:"
  echo
  cat "$src/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt"
  echo
  echo "------------------------------------------------------------------------"
  echo
  echo "The Aurumworks framework (com.aurumworks.core), by Jon Goldman, under the MIT licence above."
  echo
  echo "The Unity engine and its packages are used under Unity's own terms (unity.com/legal)."
} > "$player/Licences.txt"

rm -f "$zip"
powershell -NoProfile -Command "Compress-Archive -Path '$(w "$player")' -DestinationPath '$(w "$zip")'" || { echo "ZIP FAILED"; exit 1; }
echo "$(w "$zip"): $(du -h "$zip" | cut -f1)"
