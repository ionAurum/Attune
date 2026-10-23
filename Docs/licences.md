# Licences

Every file in the repository that others made, under what licence, and what that licence asks.
Attune itself is MIT (`LICENSE`, ruling 7). A third-party file is listed here before it is
committed, and `.gitignore` lets nothing into `Assets/` but the folders named in it. Checked
2026-10-23 (M1).

## In the repository

| What | From | Licence | It asks | Met by |
|---|---|---|---|---|
| Liberation Sans (`Assets/TextMesh Pro/Fonts/`) | TextMesh Pro's Essentials | SIL OFL 1.1 | The copyright notice and the licence with every copy | `LiberationSans - OFL.txt` sits beside it; the testers' zip carries it in `Licences.txt` (`Tools/testerbuild.sh`, M4) |
| TextMesh Pro's other essentials (shaders, settings, the font asset) | Unity | Unity Companion License | Use with Unity | Nothing more |
| `com.aurumworks.core` | github.com/ionAurum/aurumworks-core, by git URL and tag (A41) | Jon's own, MIT | The copyright notice and the licence with every copy | Not in the repository; its `LICENSE.md` comes with it |
| Unity's packages (URP, Input System, uGUI) | Unity | Unity Companion License | Use with Unity | Not in the repository (`Packages/manifest.json` names them) |

## Left out

- **TextMesh Pro's EmojiOne sprite sheet** (`Assets/TextMesh Pro/Sprites/`,
  `Assets/TextMesh Pro/Resources/Sprite Assets/`). Its attribution names EmojiOne and points to
  their terms without giving a version; EmojiOne 2.x was CC BY 4.0, later versions were not
  free. Attune shows no emoji. Never committed, so not in the history that goes public at M4,
  and `.gitignore` keeps both folders out. As in Cairn (Jon, 2026-10-23): TMP Settings'
  *Default Sprite Asset* is None and the sprite asset is deleted, so nothing ships it; the
  picture may still sit in `Sprites/` on a machine that had it. The M9 release build should
  refuse a TextMesh Pro sprite asset in a `Resources` folder, as Cairn's does
  (`PlayerBuild.NoSpriteSheets`).

## Not done

- No lawyer has read this.
- The framework's licence check before M4 (A16).
