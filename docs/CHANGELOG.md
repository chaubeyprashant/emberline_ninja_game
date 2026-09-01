# Changelog

All notable changes to Emberline 3D. Newest first.

The project is fully code-generated: scenes, prefabs and materials come from
`Assets/Editor/EmberlineBootstrap.cs`, so "changed" always means changed in
code, never hand-edited in the editor.

---

## 1.2.0 (version code 8) — unreleased

**Not yet playtested on hardware.**

### Release pass — bugs fixed

- **Staged missions never loaded.** `MissionPlan` lived in `MissionStage.cs`, and
  Unity only creates a MonoScript when the class name matches the file name — so
  all twelve authored mission assets were written with `m_Script: {fileID: 0}`
  and `Resources.LoadAll<MissionPlan>` returned nothing. Every one of the ten
  staged story levels silently fell back. The type now has its own file and the
  assets were regenerated with valid script links.
- **Enemies attacked through cover.** Perception and projectiles respected the
  arena's obstacles; melee and area attacks did not, so a pike guard's 3.6m
  thrust reached straight through a chimney. `DamagePlayer` — the single
  chokepoint every melee and AOE hit passes through — now runs a line-of-fire
  test that ignores obstacles containing either endpoint, so standing against
  cover cannot make either side unhittable.
- **Settings did not pause.** The overlay left the game running underneath, so
  opening it mid-fight meant taking hits blind. It now stops time, coordinated
  with the hit-stop dip through a single owner flag so neither can strip the
  other's freeze.
- **Attacks were not buffered.** Jump has had coyote time and buffering since
  Phase 2; attacks dropped any press made during a committed state. Strike and
  cleave now buffer for 150ms and retry, matching the jump window.
- **Three enemies had no EnemyDef.** Shade, Bomber and Jin were left on the
  legacy hardcoded path when Phase 3 authored the roster, so posture, guard
  break and weakness rules did not apply to them. Defs added, mirroring their
  existing stats so balance is unchanged.
- **`EnemyBomb`'s pool was never reset** on scene load — added with the Bomber
  in W3 and missed by the Phase 7 sweep.
- **Target SDK was Automatic**, which resolves to the highest installed platform
  — a preview (android-37.0) on this machine. Pinned to 36.
- **Android settings only applied during a build**, so the project on disk sat
  in a different configuration from what shipped, and the APK and AAB paths had
  drifted to different company and product names. Hoisted into one method called
  by scene setup and both build paths.
- **vSync was on in four quality levels**, which makes `targetFrameRate`
  inoperative until `GameManager.Awake` clears it. Cleared in the asset too.

### Verified

Data and settings audit clean across both scenes (13 enemy kinds present and
correctly indexed, all with defs; 6 weapons; 12 mission plans; every story level
playable; lighting rig, grade and atmosphere present). Line-of-fire fix checked
against seven cases including both exploit directions. APK builds: 27.7 MB,
`com.ergebins.emberline3d`, versionCode 8, versionName 1.2.0, targetSdk 36,
minSdk 26, arm64-v8a, IL2CPP.

### Endless Mode 2.0

The Road North was a distance-scaled pack loop: the same two enemy types with a
rising health multiplier and a band modifier every fifth pack. It is now a run of
discrete, differently-shaped encounters through changing country. The old loop
(`MarchUpdate` / `SpawnPack` / `ApplyPackModifier`, ~160 lines) is deleted rather
than left behind a flag — two live-looking paths for one mode is worse than none.

- **Seven regions** — village, forest, temple, castle, mountain, battlefield,
  graveyard. The road changes country every three encounters, re-lighting and
  re-fogging from the Phase 7 theme table at runtime. Regions are drawn from a
  shuffled bag, so a run visits varied country instead of rolling the same place
  three times by chance.
- **Ten encounter kinds** — ambush, arrow rain, assassins, elite squad, mini
  boss, boss, rescue, defense, duel, escape. Weighted by depth, never repeating
  back to back, with straight fights losing ground to set-pieces as the run goes
  on.
- **Difficulty through composition, not multipliers.** Stat scaling exists but is
  capped (+160% HP, +90% damage at the ceiling); the real curve is which enemy
  types are in the pool, how they are combined, where they spawn from (assassins
  open on the flanks, escape spawns behind you), the hazards underfoot, and the
  boss modifier rolled per fight — ARMOURED, SWIFT, CRUEL or ESCORTED.
- **Healing is a resource.** The clear reward starts at 34 HP and shrinks by 1.5
  per depth to a floor of 8, so a long run gets more dangerous rather than safer.
- **Environmental hazards** — fire, spike traps, rockfall and bog, chosen by
  region. Everything either sits still and visible or telegraphs for 1.4s before
  it can hurt you.
- **Eight optional modifiers** — no healing, one life, double edge, faster
  enemies, fog, heavy rain, boss rush, elite road. Bonuses multiply because the
  difficulties compound; a full eight-modifier stack pays ×7.31.
- **Rewards** — Ryo (new spending currency), weapon upgrade tracks (edge / reach
  / tempo, five points each, applied as multipliers over the WeaponDef), six
  cloth dyes, and Ember Shards on a slower drip than the old per-100m payout.
- **Records** — score, depth, time, kills, best thread, lifetime runs and kills,
  shown on the march briefing and the run report.
- **Weekly challenges** alongside the existing dailies, keyed to ISO week and
  scored against the run's own statistics.

### Known gap — no backend

Daily and weekly challenges are **local**: seeded from the device clock and
stored in PlayerPrefs. There is no server, so there are no shared leaderboards,
no cross-device progression, and nothing stops a player changing the device date
to reroll a challenge. Everything under "high score" is a personal best on that
device.

### Cosmetics are dyes, not outfits

The characters carry one material and one albedo atlas with no garment mask, so
per-garment recolouring is not possible without new art (see
[ART_DIRECTION.md](ART_DIRECTION.md) §4). Cosmetics are multiplicative tints over
the whole character, which is why every dye shifts hue or darkens — a multiply
cannot brighten what it is given.

---

## 1.1.0 (version code 7) — unreleased

Seven phases of work on top of 1.0.1. **Not yet playtested on hardware** — see
[QA_CHECKLIST.md](QA_CHECKLIST.md).

### Added — world, camera and audio

- **Environment themes as data.** `Core/EnvTheme.cs` holds all ten places
  (village, forest, bamboo, mountain, temple, castle, fortress, graveyard,
  burning village, rainy battlefield) as a table of light, fog, palette,
  weather, ambient life, wind and ambience bed. `BuildLighting` and the
  atmosphere both read it, so a new place is a row rather than a branch.
  **Only two are reachable today** — there are two arena scenes, mapped to
  Village (Rooftop) and Graveyard (Marsh). The other eight are built and
  smoke-tested but nothing loads them yet.
- **Atmosphere layer** (`UI/Atmosphere.cs`): rain, snow, ash and mist, plus
  fireflies, leaves, petals, embers, dust and crows. Emitters follow the camera
  so a small volume covers the view, and every budget scales through
  `FxPools.Density`, so Low tier thins them rather than dropping them.
- **Camera.** Dynamic combat distance (eases back with the size of the fight),
  target-lock framing on the player→enemy axis, wider and lower boss framing, a
  cinematic execution swing, and an FOV impact punch that decays on unscaled
  time so hit-stop cannot freeze it. Lock framing yields the instant the player
  drags.
- **Audio.** A 12-source positional pool for anything happening in the world,
  no-repeat bank selection (the same variant never fires twice running),
  distance-based footsteps split across grass and wood banks, cloth on dodges,
  landings and heavy wind-ups, blade whooshes, layered impacts by material
  (flesh / blade / guard / heavy), low-health breathing, and positional enemy
  voices on detection, damage and death.
- **Music state machine** with crossfades: exploration → combat → boss, chosen
  from what is actually awake and near rather than from the level.

### Known gap — music and ambience assets

The music and ambience *system* ships; most of the *tracks* do not exist. The
project contains exactly one music-length clip, `marsh_ambience.ogg`. Themes
name the bed they want (`village_ambience`, `rain_ambience`, `fire_ambience`,
…) and `SetMusicState` looks for `explore_theme`, `combat_theme` and
`boss_theme`; each falls back cleanly when its file is missing, so nothing
breaks and the wiring is already correct when the audio lands. What is needed:

| File (`Resources/Art/Audio/Music/`) | Length | Notes |
| --- | --- | --- |
| `explore_theme.ogg` | 60–90s seamless loop | sparse, low intensity |
| `combat_theme.ogg` | 60–90s seamless loop | percussive, matches exploration key |
| `boss_theme.ogg` | 60–90s seamless loop | fuller arrangement |
| 9 × `*_ambience.ogg` | 30–60s seamless loop | one per theme, named as in `EnvTheme.cs` |

Mono, 44.1 kHz, Vorbis, target ≤1.5 MB each; all three music tracks should
share a key so the crossfades do not clash.

### Added — traversal

- Jump, with coyote time (0.12s) and input buffering (0.15s).
- Vault: jumping into cover clears it, using the arena's existing obstacle
  markers so it agrees with what the AI steers around.
- Air flicker — one mid-air dash per airborne stretch.
- Wall running (MVP) on rooftop parapets and road walls, with a wall jump.
- JUMP button on the HUD. **Space is now Jump; Flicker moved to Left Shift**
  in the editor.

### Added — combat depth

- Deflect stance on the cleave: pressing opens a 0.4s parry window that holding
  extends. A deflected blow costs no health, staggers the attacker, pays Sen and
  opens the counter window.
- Launcher on the third strike of the chain — mooks pop airborne for a juggle.
  Bosses are exempt.
- Kunai warp: flicker while a kunai is in flight to blink to the blade.
- Execution: a staggered mook at ≤20% health is finished outright.
- Assassination: striking an enemy that hasn't seen you kills instantly, with
  deliberately quiet feedback.
- Soft-lock now scores targets inside a 130° cone around your aim instead of
  taking the nearest in any direction, plus a TARGET button to cycle a hard lock.

### Added — mission types

- `MissionObjective` on `LevelDef`: Clear, Hold, Stealth, Escort, Chase.
  **Chase is scaffolding only** — no level uses it; it falls back to clear-all.
- **Level 2 is now an escort.** Yotsu carries the flame across the arena while
  waves cycle; he halts near enemies and resumes when clear. Flame out = failure.
- **Level 3 is now stealth.** Enemies start unaware with vision cones; cover and
  distance buy time. An alarm wakes everyone and costs rank rather than ending
  the run.
- Road North pack modifiers every 5 packs: THE MIST THICKENS, ONLY THE DROWNED,
  ARROW RAIN, THE MIST HUNGERS.
- Level 2 and 3 dialogue, story and debrief text rewritten to match.

### Added — meta progression

- TRAVERSAL skill branch: SKY STEP (second air flicker), BLADE TETHER (−45%
  warp cooldown), ROOFRUNNER (+70% wall-run duration).
- Four feats: GHOST WALK, LANTERN SHEPHERD, IRON ANSWER, ROOFRUNNER.
- Two daily challenge types: 4 deflects in a mission; clear Level 3 unseen.
- Duel modifiers chosen on the briefing — EVEN TERMS, IRON WILL, STORM PACE,
  ONE BREATH — paying up to +2 bonus shards. Persisted between sessions.
- DROWNED GOLD blade finish at 36 stars.
  ⚠️ **Currently unobtainable**: the story caps at 30 stars. Needs a decision
  before release.

### Added — tooling

- Dev performance overlay (`PerfOverlay`): frame percentiles, hitch count,
  memory, GC per frame, render counters, plus live enemy and road-segment
  counts. Four-finger tap or F3; hidden by default and free when hidden.
- `docs/PERF_BASELINE.md` — 11 measurement scenarios and a results table.
- `docs/QA_CHECKLIST.md`, `docs/CHANGELOG.md`.
- `EmberSnapshot.RenderRoad` and a HUD screen in the UI snapshot set, for batch
  visual verification without opening the editor.

### Changed — enemy AI

- Per-kind approach behaviour: bandits flank on their own lane, archers slide to
  clear a blocked shot, shades arc to your back, the Chief walks straight in,
  Jin keeps duelist spacing and retreats after each exchange.
- Chief's slam now leaves a slowing scar on the ground for 4.5s.
- Kagachi telegraphs the clone split — he coils, holds still and rings red for a
  beat, which is a punish window.
- `AttackTokenPool`: capacity scales with crowd size (2, or 3 past 7 enemies),
  openings are rate-limited to one per 0.3s, and a 1.2s per-enemy reuse delay
  rotates aggression.

### Changed — performance

- Enemy instances are pooled instead of Instantiate/Destroy per wave.
- Combat targeting reuses buffers; the per-frame `List` allocation and the
  `yield` iterator in the strike path are gone.
- After-images in `NinjaRig` are pooled and re-posed instead of cloned.
- Telegraph rings share two static materials instead of one instance per enemy.
- Scene singletons and `Camera.main` come from a `SceneRefs` cache — the Chief's
  slam was searching the scene on every slam, and damage numbers were calling
  `Camera.main` per text per frame.
- Graphics tiers now scale particle density, the particle ceiling and the HUD's
  off-screen marker budget, not just shadows.
- Hit-stop is timer-driven rather than coroutine-driven, removing a
  `WaitForSecondsRealtime` allocation on every hit.

### Changed — feel

- Hit-stop stacking: the deepest dip wins and the longest extends, so a crush
  landing mid-flurry still reads heavier than the light hits around it.
- Camera shake follows the same rule and now ramps down instead of cutting off.
- Camera can be orbited and tilted by dragging the right side of the screen,
  with an optional gyro mode (persisted, off by default).
- The kunai is a real KayKit dagger flying point-first with a glow trail,
  replacing the spinning cube placeholder.

### Fixed

- `Time.timeScale` could stay dipped forever if a hit-stop coroutine was
  interrupted by the player dying into a scene load. Dips are now driven from
  `Update`, restored in `OnDisable`, and reset in `GameManager.Awake`.
- Pooled enemies kept `SkeletalRig._deadLatched`, so a recycled enemy would
  spawn animation-frozen in its death pose.
- Pooled enemies compounded spawn stat multipliers across reuses; base stats are
  now captured in `Awake`, before the spawner scales them.
- Pooled enemies could spawn wounded, because health was refilled before the
  spawner applied its multipliers.
- Enemies could be shoved inside cover by crowding or knockback — steering is
  now backed by a hard positional constraint.
- Enemy and kunai pools plus the warp anchor are reset per scene, so a load no
  longer leaves queues full of destroyed references.
- Wall-run into a wall-jump could clear the Road North mist barrier and skip a
  pack; the barrier is now 6m tall.
- Jumping could carry the player over the arena parapets into the void; the play
  area is now enforced directly, without flattening height.
- HUD enemy markers are pooled by index and could keep a previous target's lock
  tint and scale.

### Build

- Version 1.0.1 → **1.1.0**, version code 6 → **7**.
- Settings verified: IL2CPP, ARM64 (APK) / ARM64+ARMv7 (AAB), min SDK 26,
  LandscapeLeft, engine code stripping on with `link.xml` preserving the physics
  module. Target SDK is still **Automatic** — worth pinning.

---

## 1.0.1 (version code 6)

Baseline at the start of this work: 10 story levels, 4 duels, an Endless Trial,
skill tree, feats, daily challenges, blade finishes, and the Rooftop + Marsh
arenas — all generated from code.
