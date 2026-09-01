# Emberline — continuation prompt

Copy everything between the lines below into a new session.

---

You are continuing work on **Emberline 3D**, an existing Unity Android ninja
action game at this repo. Read this whole brief before touching anything.

## What it is

Unity **6000.5.9f1**, **Built-in Render Pipeline (NOT URP)**, IL2CPP, ARM64,
Android min SDK 26 / target SDK 36, landscape-left. Package
`com.ergebins.emberline3d`.

The project is **fully code-generated**. Scenes, prefabs, materials and
ScriptableObjects are produced by editor scripts, not hand-authored:

- `Assets/Editor/EmberlineBootstrap.cs` — `SetupScenes` regenerates all three
  scenes (Opening, Rooftop, Marsh) plus prefabs, materials and enemy defs.
  `BuildAndroid` builds the APK.
- `Assets/Editor/EmberMissions.cs` — authors the 12 mission plans.
- `Assets/Editor/EmberStory.cs` — authors the story beats (all dialogue lives
  here, never in gameplay scripts).

**Never hand-edit a scene or a generated prefab.** Change the generator and
re-run it, or your change is destroyed on the next run.

Gameplay code is under `Assets/Scripts/`: `Core`, `Player`, `Enemies`,
`Missions`, `Endless`, `Story`, `UI`, plus `GameManager.cs`, `CameraRig.cs`,
`RoadNorth.cs`, `ArenaMarkers.cs`.

## How to verify anything (do this, every time)

There is no test framework. The loop that works:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity

# 1. compile + regenerate
$UNITY -batchmode -projectPath . \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.SetupScenes \
  -quit -logFile Logs/setup.log
grep -c "error CS" Logs/setup.log        # must be 0

# 2. look at it — do not assume it renders correctly
$UNITY -batchmode -projectPath . \
  -executeMethod Emberline.EditorTools.EmberSnapshot.RenderArenas -quit -logFile Logs/s.log
$UNITY -batchmode -projectPath . \
  -executeMethod Emberline.EditorTools.EmberUiSnapshot.Render -quit -logFile Logs/u.log
$UNITY -batchmode -projectPath . \
  -executeMethod Emberline.EditorTools.EmberStorySnapshot.Run -quit -logFile Logs/t.log
# PNGs land in Logs/ — actually open and inspect them
```

Do **not** pass `-nographics` to snapshot methods; the software path segfaults
in shadow-map rendering.

For logic, write a throwaway `Assets/Editor/_Check.cs` with an
`[InitializeOnLoad]`-free static `Run()` that asserts and `Debug.Log`s, run it
via `-executeMethod`, then delete it. Several real bugs were found this way and
would not have been found any other way.

### On device

The device is a Samsung Galaxy A33 5G (`SM-A336E`).

**Use the SDK adb, not Homebrew's** — a stale Homebrew adb server makes the
device look disconnected when it is fine:

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB devices -l
$ADB install -r Builds/emberline3d.apk
$ADB shell am force-stop com.ergebins.emberline3d
$ADB shell monkey -p com.ergebins.emberline3d -c android.intent.category.LAUNCHER 1
$ADB exec-out screencap -p > /tmp/shot.png     # then look at it
$ADB logcat -d -s Unity:V | grep -iE "exception|corrupted"
```

A `ClassNotFoundException` for `AssetPackManager` at startup is normal for a
plain APK. Ignore it.

Screenshots come back 2400x1080. Multiply displayed coordinates by 1.2 to get
tap coordinates.

## Traps that have already cost real time

1. **A MonoBehaviour or ScriptableObject class must live in a file with the
   same name.** Unity only creates the MonoScript when they match. Break this
   and components serialise with a null `m_Script`: assets silently fail to
   load, and a scene ships that crashes the player with
   `level0 is corrupted`. This bit twice (`MissionPlan` in `MissionStage.cs`,
   `CastMember` in `Cast.cs`). `BuildOpeningScene` now asserts against it —
   keep that guard.
2. **C# switch expressions bind tighter than `%`.** `x % 3 switch { ... }`
   parses as `x % (3 switch {...})`. Always write `(x % 3) switch { ... }`.
3. **Statics must be reset on scene load.** `Init` methods re-run after a load
   because the old object is a destroyed reference that compares equal to
   null. A pool or list that is only appended to will accumulate dead entries
   and throw. This produced a bug where every footstep threw out of
   `PlayerLocomotion.Update` and silently skipped jump and movement.
4. **Both build paths share one scene list** (`ShippedScenes`). They used to
   hardcode separate arrays, so a scene registered in `EditorBuildSettings`
   never reached an APK.
5. **The editor lies about runtime.** Characters T-pose in edit-mode
   snapshots (animators do not run); `Cast.Find` needs a scene-search fallback
   because `OnEnable` has not fired. Three of the last four real bugs were
   invisible in the editor and only appeared on device.

## Current state

Branch `feature/story-endless-polish`. No remote configured.

Working and verified on hardware: the opening cinematic (letterbox, scripted
camera, subtitles), main menu, story select, mission briefing with dialogue,
in-mission HUD, pause with LEAVE MISSION, Road North briefing with modifiers,
The Forge, The Armoury, Endless run start, difficulty selection, staged
missions loading their plans.

Systems present: 13 enemy types with ScriptableObject defs; 12 mission plans
with staged objectives and checkpoints; Endless 2.0 (10 encounter kinds, 7
regions, hazards, 8 modifiers, Ryo currency, weapon upgrades, cosmetic dyes,
records); a story framework (`StoryBeat`/`StoryShot` assets, cinematic
director, subtitles, localisation indirection, save flags); environment themes
driving light/fog/weather/ambience; pooled positional audio; Easy/Medium/Hard/
Lethal difficulty; context- and thermal-aware frame capping.

## What actually needs doing

Pick one, do it properly, verify it, then report honestly.

### 1. UI/UX overhaul (planned, not started)

Target: premium dark cinematic, charcoal/black, ember accent, minimal, no
oversized buttons or arcade elements.

- `Assets/Scripts/UI/UiKit.cs` is the component library and is the right place
  to change the look once for every screen. `EmberHud.cs` (~1900 lines) holds a
  `Screen` enum and one `Build*()` per screen.
- The "arcade" look is two sprites: `button_rectangle_depth_gradient` on every
  button and `panel-000` (an ornate 9-slice frame that visibly breaks at small
  sizes) on every panel. Replacing both with flat semi-transparent fills is a
  change in `UiKit`, not in 14 screens.
- **TextMeshPro is available but unusable as-is.** `TMPro.TMP_Text` resolves
  (Unity 6 bundles TMP in `com.unity.ugui`) but `TMP_Settings.instance` is
  null — TMP Essential Resources were never imported and no font assets exist.
  Both must be generated first. The migration surface is small: only ~19
  `Text`-typed references across 7 files, because everything funnels through
  `UiKit.Label`.
- **Responsiveness is genuinely at risk**: `CanvasScaler.matchWidthOrHeight`
  is 1.0 (height only) with 259 hardcoded pixel offsets. Test on a tall
  (20:9) and a 4:3 aspect.
- Missing entirely: loading screen, screen transitions, cinematic backgrounds
  on menu and briefing.
- Not yet opened on hardware at all: **Loading, Mission Complete, Game Over**.

### 2. Story P3–P5

Scenes 1–3 are cut and running. Remaining: scenes 4–9 (the child under the
cart, Aiko through the gate, capture by Kagehira, "You came too late", the
choice, the snow), the THREE YEARS LATER time skip, title card, and the hook
into the first playable mission.

Canon decisions already made: the protagonist stays **Renzo**; the existing
boss **Kagachi is Kagehira**, promoted to the warlord behind everything, with
Goro and Jin as his lieutenants. The dialogue reconciliation for that has not
been done yet.

**Stage the emotional beats in silhouette, over-the-shoulder and on hands and
objects.** The characters are chibi meshes with no face rig, so a literal
held close-up on a face will land as funny rather than devastating.

### 3. Known gaps that need assets or a decision, not code

- **Art.** Characters are chibi KayKit models with single albedo atlases and
  no face rig. "Realistic cinematic" is not reachable by shader work. Specs
  for replacements are in `docs/ART_DIRECTION.md` §4 (characters) and §5
  (story cast). Everything currently in `EmberCharacterFactory` for the story
  cast is marked `PLACEHOLDER` on purpose.
- **Music.** Exactly one music-length clip exists (`marsh_ambience.ogg`). The
  music state machine and 9 themed ambience beds are wired and fall back
  cleanly, so you hear the marsh loop everywhere. Spec table is in
  `docs/CHANGELOG.md` under 1.2.0.
- **No backend.** Daily and weekly challenges are device-local PlayerPrefs
  seeded from the clock. No leaderboards, no cross-device saves, and changing
  the date rerolls a challenge.
- **DROWNED GOLD** blade finish requires 36 stars; the story caps at 30, so it
  is unobtainable. Lower it, add star sources, or add levels 11–12.

### 4. Thermal follow-up

Frame capping is done (menus/pause/cinematics 30fps; gameplay default 30
except High tier; thermal step-down via Android's thermal status). Measured on
the A33: gameplay CPU 86–102% at 60fps vs 45–52% at 30.

Remaining: a full-screen menu still renders the live 3D arena behind it
(~50% CPU at 30fps). Not drawing the scene behind opaque menus is the next
real win and has not been attempted.

## How to work

- Read before changing. This codebase has twelve phases of tuning in it;
  prefer additive changes that preserve existing behaviour over rewrites.
- Compile after every change. Snapshot and **look** before claiming a visual
  works. Run on device before claiming anything works.
- Difficulty Medium is exactly 1.0 on every axis by design — the game is
  balanced there. Do not move those numbers.
- Report honestly. Say what you verified, what you did not, and what you could
  not do. If something cannot be delivered as asked, say so and explain why
  rather than shipping a plausible-looking substitute.

---
