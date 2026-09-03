# Emberline — continuation prompt (whole game)

Copy everything below the line into a new session.

---

You are continuing work on **Emberline 3D**, a finished-enough Unity Android
ninja action game in this repo. Read this whole brief before changing
anything. It describes the game as designed, the game as built, and the
difference between the two.

# PART 1 — THE GAME

## Premise

A stealth-action ninja game set over one night and its aftermath in and around
the village of **Yorune**. You play **Renzo Kurogawa**, a ninja whose village
was burned by the warlord **Kagehira** while he was away escorting a
messenger. His father, the village swordmaster, was killed refusing to give up
the location of the **Black Seal** — a document that would expose Kagehira.
His mother died trying to save wounded villagers. His sister **Aiko** was
taken.

Aiko's line — *"When you're near, nothing bad can happen"* — is the emotional
spine of the game. When Renzo finally reaches her years later, she says
*"You came too late."* She dies in the snow, giving him their father's sword
and telling him *"Don't become like them."*

He carries her red thread bracelet. He is not a revenge machine: he is quiet,
controlled and damaged, dismantling Kagehira's empire piece by piece so that
nobody else loses what he lost. **The theme is that revenge destroys the person
seeking it**, and the player should gradually notice Renzo becoming like the
people he hates. Do not write him as a straightforward hero or villain.

Tone: serious, restrained, cinematic. Emotionally brutal rather than gory. No
comedy, no anime exaggeration, no arcade celebration.

## Modes

**Story** — 10 levels across three acts: *The Lantern Falls*, *Into the
Marsh*, *The Serpent's Coil*. Levels are named: First Blood, The Lantern Road,
Eyes in the Dark, Goro's Toll, The Serpent's Trail, Into the Reeds, The
Drowned Road, Twin Lanterns, The Serpent's Guard, Kagachi. Three stars per
level, 30 total. Clearing level 10 unlocks New Game+.

**Duels** — four 1v1 showdowns (Goro, The Pale Shade, Kagachi, Jin Kurogane)
with optional self-imposed handicap modifiers that pay more on a win.

**The Road North (Endless)** — a procedural survival run. See below; this is
the deepest system in the game.

## Core combat

A commitment-based melee system built on a state machine
(`Free / Light / Heavy / Guard / Parry / Dodge / Recover / Staggered /
Execute`) where states cannot conflict.

- **Light attack** chains (3 hits by default, weapon-dependent); the last hit
  crushes and launches.
- **Heavy attack (cleave)** is a real commitment with a wind-up.
- **Dodge (Flicker Step)** has i-frames; a perfect dodge slows time and opens
  a counter window.
- **Parry/deflect** lives in the tight window at the start of a guard; a
  perfect parry breaks guard and cannot be regenerated through.
- **Posture/guard-break** — enemies do not simply lose HP and keep fighting;
  they flinch, get knocked back, launched, guard-broken or staggered.
- **Execution** on a staggered, nearly-dead enemy. A kill on an unaware enemy
  is a silent assassination instead — no nova, no shake, no noise.
- **Hit-stop** scales with hit strength; camera shake and FOV punch stack with
  a "stronger wins, weaker only extends" rule.
- Attacks are input-buffered 150ms, matching the jump buffer.

**Sen and Gates** is the resource system: Sen is energy flowing through
Gates. Spending a **Surge** cracks a Gate, permanently lowering maximum Sen
until the player rests. Attrition, not a regenerating bar.

**Traversal**: jump with coyote time and input buffering, vault over cover,
MVP wall-run, air flicker, and kunai warp (blink to a thrown kunai).

**Weapons** (6, data-driven `WeaponDef` assets): Ember Katana (balanced),
Storm Tanto (parries on perfect dodge), Marsh Hook (pulls on hit 3, poison
cleave), Twin Daggers, Smoke Bomb (throws a blinding cloud; shades take
double), Hand Crossbow (fires on the strike button, fan-shot cleave). Each has
an archetype, chain length, cleave style and thrown ammunition.

## Enemies

13 types, all data-driven via `EnemyDef` ScriptableObjects — silhouette,
weapon, speed, reach, moveset, defence and weakness all live in the asset, not
in code:

Raider (baseline), Assassin (fastest, folds if caught), Pike Guard (owns your
spacing), Weaver/Archer (harmless in melee, punishing at range, 2× backstab),
Axe Raider (armoured), Samurai (blocks then punishes greed), Rogue Ninja (your
own kit used against you), Elite Warrior (full moveset, no single answer),
Shade (marsh-born, fast, fragile, 2× damage from smoke), Powder Carrier
(bomber), Goro (mini-boss, telegraphs everything and lands anyway), Jin
(duelist boss, no armour, answered by reading him), Kagachi/Kagehira (boss,
three phases).

**AI**: time-sliced perception, vision cones, hearing and noise, darkness and
visibility, back attacks, body awareness, alarm states, and a squad
coordinator with an attack-token pool so a crowd cannot all swing at once.
Enemies cannot attack through cover.

## Missions

**The story catalogue is a 100-mission campaign** (10 chapters, 3 acts), authored
as one table in `Assets/Scripts/Campaign/CampaignTable.cs`. Every mission carries
the ten design-rule fields (purpose, objective, gameplay types, unique event,
discovery, climax, ending, next-mission reason) plus staging (arena, theme,
weather, roster, boss/named foe, beat, bespoke plan). `Campaign.Levels` feeds
`Session.Story`; the chapter select, briefing, results and unlocks read it.
Ten plans are hand-built (`Resources/Missions/S01_…S10_`), ninety are generated
(`C001…C100`) by `EmberMissions.Generate` from a template per primary gameplay
type. Regenerate with `Emberline/Build Missions`; verify with `Check Campaign`
(457 assertions) and `Check Mission Design` (1398, all hundred plans); the
bible `docs/CAMPAIGN.md` is generated by `Write Campaign Doc` — never hand-edit.
New stage goals: `Endure`, `Cinematic`, `FreePrisoners`, `ReachAny`, `BossPhase`,
`Listen`; events `Collapse`, `Mutiny`, `FoeWithdraws`, `FogRolls`, `Ambush`,
`RouteWakes`. Named foes are `EnemyDef` ids on common bodies via `EnemyDefs.Find`.


12 mission types authored as `MissionPlan` assets with staged objectives,
optional objectives, events and checkpoints: Assassination, Rescue,
Infiltration, Escort, Chase, Survival, Defense, Stealth, Duel, Boss Hunt,
Escape, Investigation.

## The Road North (Endless 2.0)

Deliberately *not* "more enemies with more health".

- **7 regions** drawn from a shuffled bag — village, forest, temple, castle,
  mountain, battlefield, graveyard — changing every three encounters, with
  full re-lighting and re-fogging.
- **10 encounter kinds** weighted by depth and never repeating back to back:
  ambush, arrow rain, assassins, elite squad, mini boss, boss, rescue,
  defense, duel, escape.
- **Difficulty comes from composition**: which enemy types have entered the
  pool, how they combine, where they spawn from (assassins open on the
  flanks; escape spawns them behind you), hazards underfoot (fire, spikes,
  rockfall, bog), and a boss modifier rolled per fight (Armoured, Swift,
  Cruel, Escorted). Stat scaling exists but is capped.
- **Healing is a resource**: the clear reward starts at 34 HP and shrinks by
  1.5 per depth to a floor of 8. A long run gets more dangerous, not safer.
- **8 optional modifiers** wagered before the run — No Healing, One Life,
  Double Edge, Faster Enemies, Fog, Heavy Rain, Boss Rush, Elite Road.
  Bonuses multiply because the difficulties compound; a full stack pays 7.31×.

## Progression and economy

- **Stars** (30) from story levels; **Ember Shards** buy the skill tree.
- **Skill tree**: 12 nodes in four branches — Combat (Heavy Ember, Long
  Thread, Thread Burst), Defense (Second Step, Ember Salve, Steady Gates),
  Ember (Wide Nova, Sen Flow, Lantern's Wrath), Traversal (Sky Step, Blade
  Tether, Roofrunner).
- **Ryo** is a separate spending currency earned on the Road North, spent in
  **The Forge** on weapon upgrade tracks (Edge / Reach / Tempo, five points
  each) and cloth dyes. Kept separate from Shards so a cosmetic never competes
  with a combat unlock.
- **Feats** (achievements), **blade finishes** (cosmetic), and **daily and
  weekly challenges**.
- **Difficulty**: Easy / Medium / Hard / Lethal, scaling enemy damage and
  health, healing, player HP, how many enemies may attack at once, and Endless
  score. **Medium is exactly 1.0 on every axis — the game is balanced there.
  Do not move those numbers.**

## Presentation

Environment themes drive light, fog, palette, weather (rain/snow/ash/mist),
ambient life (fireflies, leaves, petals, embers, dust, crows), wind and
ambience bed from one data table — 10 themes plus one daylight theme used by a
single story scene.

Camera: third-person with dynamic combat distance, target-lock framing, wider
boss framing, a cinematic execution swing, controlled shake and FOV impact
punch. Audio: pooled positional sources, no-repeat variant banks, surface-aware
footsteps, cloth, whooshes, layered impacts, breathing at low health, enemy
voices, and an exploration/combat/boss music state machine.

# PART 2 — THE BUILD

## Technical shape

Unity **6000.5.9f1**, **Built-in Render Pipeline (NOT URP)**, IL2CPP, ARM64,
min SDK 26 / target SDK 36, landscape-left, `com.ergebins.emberline3d`.
Reference device: Samsung Galaxy A33 5G (`SM-A336E`).

The project is **fully code-generated**. Scenes, prefabs, materials and
ScriptableObjects are produced by editor scripts:

- `Assets/Editor/EmberlineBootstrap.cs` — `SetupScenes` regenerates all three
  scenes (Opening, Rooftop, Marsh) plus prefabs, materials, enemy defs and
  Android player settings. `BuildAndroid` builds the APK.
- `Assets/Editor/EmberMissions.cs` — the 12 mission plans.
- `Assets/Editor/EmberStory.cs` — story beats. **All dialogue lives here**,
  never in gameplay scripts.

**Never hand-edit a scene or generated prefab.** Change the generator and
re-run it, or your change is destroyed on the next run.

Code lives in `Assets/Scripts/`: `Core`, `Player`, `Enemies`, `Missions`,
`Endless`, `Story`, `UI`, plus `GameManager.cs`, `CameraRig.cs`,
`RoadNorth.cs`, `ArenaMarkers.cs`.

## How to verify anything — do this every time

There is no test framework. The loop that works:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity

$UNITY -batchmode -projectPath . \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.SetupScenes \
  -quit -logFile Logs/setup.log
grep -c "error CS" Logs/setup.log        # must be 0

# then LOOK at it — never assume a visual works
$UNITY -batchmode -projectPath . -executeMethod Emberline.EditorTools.EmberSnapshot.RenderArenas -quit -logFile Logs/s.log
$UNITY -batchmode -projectPath . -executeMethod Emberline.EditorTools.EmberUiSnapshot.Render -quit -logFile Logs/u.log
$UNITY -batchmode -projectPath . -executeMethod Emberline.EditorTools.EmberStorySnapshot.Run -quit -logFile Logs/t.log
# PNGs land in Logs/ — open and inspect them
```

Do **not** pass `-nographics` to snapshot methods; the software path segfaults
in shadow-map rendering.

For logic, write a throwaway `Assets/Editor/_Check.cs` with a static `Run()`
that asserts and `Debug.Log`s, run it via `-executeMethod`, then delete it.
Several real bugs were found only this way.

### On device

**Use the SDK adb, not Homebrew's** — a stale Homebrew adb server makes the
device look disconnected when it is fine.

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB install -r Builds/emberline3d.apk
$ADB shell am force-stop com.ergebins.emberline3d
$ADB shell monkey -p com.ergebins.emberline3d -c android.intent.category.LAUNCHER 1
$ADB exec-out screencap -p > /tmp/shot.png     # then look at it
$ADB logcat -d -s Unity:V | grep -iE "exception|corrupted"
```

A startup `ClassNotFoundException` for `AssetPackManager` is normal for a plain
APK — ignore it. Screenshots are 2400x1080; multiply displayed coordinates by
1.2 for tap coordinates.

## Traps that have already cost real time

1. **A MonoBehaviour or ScriptableObject class must live in a file with the
   same name.** Unity only creates the MonoScript when they match. Break it and
   components serialise with a null `m_Script`: assets silently fail to load,
   and a scene ships that crashes the player with `level0 is corrupted`. This
   bit twice (`MissionPlan`, `CastMember`). `BuildOpeningScene` now asserts
   against it — keep that guard.
2. **C# switch expressions bind tighter than `%`.** Write `(x % 3) switch`,
   never `x % 3 switch`.
3. **Statics must be reset on scene load.** `Init` re-runs after a load because
   the old object is a destroyed reference that compares equal to null. A pool
   only ever appended to accumulates dead entries and throws — this produced a
   bug where every footstep threw out of `PlayerLocomotion.Update` and silently
   skipped jump and movement.
4. **Both build paths share one scene list** (`ShippedScenes`). They used to
   hardcode separate arrays, so a scene registered in `EditorBuildSettings`
   never reached an APK.
5. **Never gate UI screens behind an allow-list.** `UpdateScreenRouting` used
   one and silently broke every screen added after it was written — they were
   built and destroyed within a frame, which reads as a dead button.
6. **The editor lies about runtime.** Characters T-pose in edit-mode snapshots
   (animators do not run). Three of the last four real bugs were invisible in
   the editor and appeared only on device.

## Current state

Campaign (1.3.0): all 100 missions authored and playable on the two arenas;
both validators pass; the play bot is run over the campaign in four chunks of
25 (`-playFrom/-playTo`). Not built: forest/mountain/snow/temple/fortress
geometry, adult Aiko and Jin gameplay models (stand-ins are named
`PLACEHOLDER_*`), a collapsing arena for Kagehira's last phase — see
`docs/ASSET_SPECIFICATIONS.md` §10.


Branch `campaign/verification`, pushed; `main` synced to it as of 515d549.
Uncommitted on top: the duel-select card grid, the Mixamo character pipeline
and the full enemy cast conversion — see `docs/MIXAMO_CAST_HANDOFF.md` and
`docs/ENEMY_CHARACTER_OVERHAUL_REPORT.md`.

**Verified on hardware**: opening cinematic (letterbox, scripted camera,
subtitles), main menu, story select, briefing with dialogue, in-mission HUD,
pause with Leave Mission, Road North briefing and launch, The Forge, The
Armoury, difficulty selection, staged missions loading their plans, frame
capping.

**Never opened on hardware**: Loading screen (does not exist), Mission
Complete, Game Over.

**Never playtested for feel by a human at all** — no one has judged whether
the parry window, dodge timing, camera comfort or combat responsiveness are
actually good. `docs/QA_CHECKLIST.md` exists for exactly this and is unrun.

# PART 3 — WHAT TO DO NEXT

Pick one, do it properly, verify it on device, report honestly.

### UI/UX overhaul (planned, not started)

Target: premium dark cinematic — charcoal/black, ember accent, minimal, no
oversized buttons or arcade elements.

- `UiKit.cs` is the component library: change the look there once for all 14
  screens rather than screen by screen. `EmberHud.cs` (~1900 lines) holds a
  `Screen` enum and one `Build*()` per screen.
- The arcade look is two sprites: `button_rectangle_depth_gradient` on every
  button and `panel-000` (an ornate 9-slice frame that visibly breaks at small
  sizes) on every panel.
- **TextMeshPro resolves but is unusable**: `TMP_Settings.instance` is null —
  essentials were never imported and no font assets exist. Generate both
  first. The migration is small: ~19 `Text`-typed references across 7 files,
  because everything funnels through `UiKit.Label`.
- **Responsiveness is at risk**: `matchWidthOrHeight` is 1.0 (height only)
  with 259 hardcoded pixel offsets. Test 20:9 and 4:3.
- Missing: loading screen, screen transitions, cinematic backgrounds on menu
  and briefing.

### Story scenes 4–9

Scenes 1–3 are cut and running. Remaining: the child under the cart, Aiko
through the gate, capture by Kagehira, "You came too late", the choice, the
snow, the THREE YEARS LATER time skip, title card, and the hook into the first
playable mission.

Canon already decided: protagonist stays **Renzo**; the existing boss
**Kagachi is Kagehira**, promoted to the warlord, with Goro and Jin as his
lieutenants. The dialogue reconciliation for that is not done.

**Stage emotional beats in silhouette, over-the-shoulder, and on hands and
objects.** The characters are chibi meshes with no face rig; a literal held
close-up on a face lands as funny, not devastating.

### Gaps that need assets or a decision, not code

- **Art.** Chibi KayKit models, single albedo atlases, no face rig. "Realistic
  cinematic" is unreachable by shader work. Replacement specs are in
  `docs/ART_DIRECTION.md` §4 (characters) and §5 (story cast). Story cast
  entries in `EmberCharacterFactory` are marked `PLACEHOLDER` on purpose.
  **Done for the player and all thirteen enemy kinds** (2026-09-04): the
  factory imports and animates Mixamo humanoid models, and the whole combat
  cast is built from six of them on one shared skeleton. Bones are over the
  §4.1 budget on every body; four of six bodies are inside the triangle
  budget. The five story-cast characters (Father, Mother, Aiko as a child,
  the village child, young Ren) are still chibi — a story-art task. See
  `docs/ENEMY_CHARACTER_OVERHAUL_REPORT.md`; that work is uncommitted.
- **Music.** One music-length clip exists (`marsh_ambience.ogg`). The state
  machine and 9 themed ambience beds are wired and fall back cleanly, so the
  marsh loop plays everywhere. Spec table in `docs/CHANGELOG.md` under 1.2.0.
- **No backend.** Daily/weekly challenges are device-local PlayerPrefs seeded
  from the clock. No leaderboards, no cross-device saves; changing the date
  rerolls a challenge.
- **DROWNED GOLD** blade finish needs 36 stars but the story caps at 30 — it is
  unobtainable. Lower it, add star sources, or add levels 11–12.

### Thermal follow-up

Frame capping is done (menus/pause/cinematics 30fps; gameplay 30 by default
except High tier; thermal step-down via Android's thermal status). Measured on
the A33: gameplay CPU 86–102% at 60fps vs 45–52% at 30. Remaining: a
full-screen menu still renders the live 3D arena behind it (~50% CPU at
30fps). Not drawing the scene behind opaque menus is the next real win.

## How to work on this

- Read before changing. There are twelve phases of tuning in here; prefer
  additive changes that preserve behaviour over rewrites.
- Compile after every change. Snapshot and **look** before claiming a visual
  works. Run on device before claiming anything works.
- Report honestly: what you verified, what you did not, what you could not do.
  If something cannot be delivered as asked, say so and explain why rather
  than shipping a plausible-looking substitute.

---
