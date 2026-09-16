# Emberline — open-world upgrade audit (Phase 1)

Date: 2026-09-15. Branch `campaign/living-world` (67 commits ahead of `main`).
Read-only audit of the whole project before any open-world work begins. Nothing
in the project was changed to produce this document; the only side effect was
re-rendering `Logs/zone_*.png` through `Emberline/Snapshot Zone`.

Companion document: `docs/OPEN_WORLD_ROADMAP.md` (the phased plan that follows
from this audit).

Method: every runtime script was read in full (28k lines across 111 files), the
editor pipeline was skimmed (21.5k lines), the four scenes and the data assets
were inventoried, the existing design documents were read, and the zone, the cast
and the home screen were rendered and inspected. Prior in-repo audits
(`CAMPAIGN_ARCHITECTURE.md`, `COMBAT_3_AUDIT.md`, `ENVIRONMENT_AUDIT.md`,
`CONTINUATION_PROMPT.md`) were checked against the code rather than trusted.

---

## 0. The project in one paragraph

Emberline is a Unity 6000.5.9f1, **Built-in Render Pipeline**, IL2CPP/ARM64
Android game with a 100-mission campaign, nine duels, a procedural endless mode,
a skill tree, a forge, cosmetics and Firebase auth/cloud save. It is **entirely
code-generated**: scenes, prefabs, materials, movesets, enemy defs, mission plans,
cutscenes and UI are produced by `Assets/Editor/*` and must never be hand-edited.
Since 2026-09-07 all gameplay happens on one generated 200 m valley
(`EmberTerrain`) with a village, an enemy camp, a road, a river and two forests.
The canon (Renzo Kurogawa, Yorune, the Black Seal, Aiko, Kagehira and the nine
other villains, the six companions, nine camps and five villages) lives in
`CampaignTable.cs`, `CampaignDesignTable.cs`, `docs/CAMPAIGN.md` and
`docs/VILLAINS.md`, and is the source of truth for everything below.

---

## 1. What is already good (keep, build on)

**Combat core.** `CombatState` + `CombatRules` is a clean transition table
(Free/Light/Heavy/Guard/Parry/Dodge/Recover/Staggered/Execute) with one
authoritative answer per transition (`Player/CombatState.cs:12-79`). Attacks are
data (`Resources/Attacks/*` `PlayerMoveset` with 17 contexts, 6 movesets) chosen
by `PlayerContextResolver` from field-visible facts. Timing is a phase model
(34/30/16 % of the clip, `Combat/AttackPhase.cs`) that cannot drift from the
animation because clips are time-scaled to it. Hit detection is a swept blade
segment against an upright capsule, allocation-free and sub-stepped
(`Combat/WeaponTrace.cs`), with `TraceTelemetry` so silent fallback is visible.
Hit-stop stacks by "deepest wins, longest extends" on unscaled time
(`CombatController.cs:1424-1466`). Perfect parry, guard break, execution, silent
assassination on unaware targets, lock-on, kunai warp, smoke bomb and Surge all
exist and are wired to VFX at the contact point, SFX, haptics and camera impact.

**Enemy data model.** `EnemyDef` + `AttackDefinition` + `EnemyCombatProfile`
(36 profiles) make a new fighter an asset, not code. `EnemyAttackSelector` is a
scored, allocation-free, legible decision. `AttackTokenPool` + `SquadCoordinator`
is a real two-layer crowd control (2-3 simultaneous swings, roles refreshed at
4 Hz). Perception is time-sliced at 5 Hz with a vision cone, analytic 2D
line-of-sight, a visibility model (crouch, smoke, lanterns, ambient), a 16-event
noise ring and corpse awareness. `EnemyPool`, `Projectile`, `EnemyBomb`,
`FloatingText` and `FxPools` are pooled. `AiTelemetry` plus the editor harnesses
(`EmberAiCheck`, `AiEncounterDriver`) are unusual and valuable.

**Camera.** `CameraRig` (483 lines) is the strongest single file: over-shoulder
placement, exponential follow, spherecast occlusion with asymmetric attack and
release, a single-funnel FOV impact API with hard clamps, max-wins shake, boss
orbit and execution shots with a clear priority chain, crowd pull-back.

**Mission data and campaign.** `MissionPlan`/`MissionStage`/`StageEvent` (20
goals, 13 events, optional objectives, checkpoints) and a one-switch evaluator in
`MissionDirector`. The two-route branch (`ReachAny` + `spawnB`, B-roster
propagation through the rest of the plan) works and is used by 43 plans. The
campaign table's *every-mission-ends-on-the-reason-the-next-begins* chain, the
Endure missions, the memory chapter (52-56) and the optional-objective-as-
condition design are all right and must survive untouched.

**Story stack.** `StoryBeat`/`StoryShot` (8 camera kinds, 14 audio beds),
`CinematicDirector` with gameplay suppression and graceful degradation,
`Cast.Find` with `CastStandIn` fallback, and **1,138 voiced lines** keyed by MD5
of speaker+line (`VoiceLines.cs`). 32 cast prefabs in `Resources/Prefabs/Cast`.

**UI foundation.** `UiKit` is a genuine design system (palette, three generated
TMP faces, glyph rasteriser, `FitText`). The home screen on device already reads
as the premium dark cinematic look the continuation brief asked for.

**Pipeline and verification.** 62 `Emberline/*` menu items; `SetupScenes`
regenerates the world; `AssertNoMissingScripts` guards the class-name/file-name
trap; eight content validators with hard exit codes chained by
`EmberCampaignBatch.All`; a playthrough bot with level ranges; PNG snapshot tools
for characters, arenas, UI, story and zone; `PerfOverlay` with p50/p95/p99,
GC/frame, draw/setpass/tris; `PerfGovernor` thermal stepping; `MarkArenaStatic`
and per-class asset budgets in `EmberAssetValidator`. `release.sh` signs without
echoing secrets.

**Design thinking.** `docs/CAMPAIGN_ARCHITECTURE.md` already diagnosed the
campaign honestly (59 of 100 missions were two shapes, one named ally, villagers
as cargo, no consequence) and specified the fix as an 11-item order of work, of
which items 1-4 shipped.

---

## 2. What is broken (bugs found, all verified in code)

| # | Where | What | Effect |
|---|---|---|---|
| B1 | `CombatController.cs:668` via `Enter():157-164` | `Cleave()` sets `_motor.Busy = true`, then `Enter(Guard)` runs `_motor.Busy = CombatRules.Committed(Guard)` = false and overwrites `_stateT` with the deflect window | The heavy wind-up does not commit the player; `_pendingCleave` exists to hide the symptom |
| B2 | `Campaign.cs:86` | `objective = MissionObjective.Clear` for all 100 levels | Stealth/escort/chase scoring in `GameManager.MissionResult()` can never fire for a campaign mission |
| B3 | `StoryRunner.cs:37` | "We no longer play the intro cinematic" → `Advance()`; `BeginBeat()` and skip are dead | The opening beat never plays |
| B4 | `CloudSaveManager.cs:72,232` | `for i = 1..20` | Stars for missions 21-100 never sync; no schema version; `sf_*`, `ckpt_*`, difficulty unsynced; guests skipped entirely |
| B5 | `Wallet.Earn` only called from `Endless/RunStats.cs:81` | Campaign pays zero Ryo | Weapon upgrades and dyes are economically stranded for story players |
| B6 | `EnemyBrain.cs:1655` and `GameManager.cs:395` | `Unaware` is one-way; `RaiseAlarm` wakes every enemy in the level | No de-escalation, no local alert; stealth cannot recover |
| B7 | `AttackTokenPool.cs:69-75` | `Prune()` collects null keys, never destroyed entries; dictionary keyed on pooled `EnemyBrain` objects | Growth + recycled enemies inherit cooldowns |
| B8 | `PlayerLocomotion.cs:132` | `Grounded = _cc.isGrounded` read before `Move()` | One frame stale; contradicts its own comment at `:41` |
| B9 | `EmberInput` statics | Never reset on scene load; `PointerExit` releases holds (`EmberHud.cs:2075`) | Crouch/guard latch across scenes; thumb drift drops holds |
| B10 | `Player/Combat/PlayerAttackDefinition.cs:73` | `hitStop` authored in all six movesets, never read; `LastSwingDirection` written never read; `AttackDirections.WithinGuard` has zero call sites | Directional defence (COMBAT_3 stage 3/5) is half-landed dead code |
| B11 | `MissionDirector.cs:691` | `SpawnBehindPlayer` calls `SpawnOne` (arena perimeter) | "BEHIND YOU" is a lie |
| B12 | `MissionDirector.cs:742`, `SlowZone.cs:26-42`, `CombatController.cs:1470+` | `new Material` per marker / per slam; `PoisonPuddle` `CreatePrimitive` per cleave | Allocation and leaks on hot paths |
| B13 | `Prisoner.cs:36` | `_runTo = at.normalized * 18f` | Assumes an origin-centred arena |
| B14 | `Feats.cs`, `DailyChallenge.cs` | Five feats reference retired ten-level ids/names; dailies rotate on `string.GetHashCode()` | Unreachable-as-described feats; latent rotation instability |
| B15 | `Assets/Editor/EmberBuild.cs:7` vs `EmberlineBootstrap.cs:100` | Duplicate `MenuItem("Emberline/Build Android APK")` with different outputs and settings; versions hardcoded in three places; keystore path points at a non-existent sibling project | Build reproducibility |
| B16 | `AuthManager` | Instance in both arena scenes, lazy `Instance` may create a third; auto Google sign-in on cold start | Duplicate managers, surprise sign-in |
| B17 | `Assets/Resources/Loc/` | Does not exist; `Loc.T` falls through | English only; HUD never calls `Loc.T` anyway |
| B18 | `EmberHud.cs:462` | ZONE debug button and LOGOUT on the home screen | Must be gone before the next store upload |

Also broken in intent rather than code: Chase has no fleeing target, Defend has
no point to defend, `Eliminate` is never used, `Assassinate` falls through to
"kill everything", `BossArrives`/`TargetFlees` are announcements with no effect,
`MissionPlan.baseShards` and stage `hint` are never read.

---

## 3. What is missing (against the open-world brief)

**Traversal.** Present: run (one speed), jump with coyote/buffer, Flicker air
dash with i-frames, wall-run MVP (two raycasts, no layer mask), wall-jump, crouch
hold, kunai warp. **Absent:** sprint, roll as an animated move, climbing, ledge
grab/mantle, vault over real geometry (the "vault" keys off authored marker
circles), slide, swim (water is a 0.75 speed multiplier), rope/zip, cover,
stealth-walk speed, slope handling, acceleration/deceleration, root motion, IK,
foot placement. No `Rigidbody`, no `NavMesh`, no Input System anywhere.

**Stealth.** Present: vision cone (8.5 m / 95°, hardcoded), detection meter,
visibility model, noise ring (only the player's feet emit), corpse awareness.
**Absent:** patrol routes (Patrol is a 28°/s spin in place), search (rotate for
3 s), hide spots, tall grass, light volumes, distractions, rear-arc takedown with
its own animation, body carry, local alert propagation, de-escalation, vision
cone shown to the player, a HUD detection indicator (the escort bar is reused).

**Enemy AI.** 21 defs = 13 distinct fighters + 8 stat clones for named foes.
Movement is `transform +=` against circle obstacles, no pathfinding, no
enemy-enemy separation, no colliders on enemies, attacks swing into walls.
Enemies exist only inside missions and spawn on a random arena perimeter; camps
are props. No line soldier, shield unit, true ninja kit, elite guard/post
archetype, captain with orders.

**World.** One 200 m valley with village, camp, road, river, two forests, ruin,
shrine, two torii. Missing from the target list: mountains you can walk on
(the ring is a wall), farms as places, samurai outposts, ninja hideout, temple,
caves, waterfall, ruins as a site, watchtowers off the camp, training ground,
market as a system, burned village as a persistent state, fortified compound,
hidden paths, mountain pass. No streaming, no LOD groups, no occlusion, no
lightmaps (world/rendering detail in §7).

**Dynamic world.** Villagers stand or flee in a straight line; no schedules,
work, talk, carry, react-to-player, report. No faction, reputation, territory,
patrol, outpost or leader concepts (grep: `Faction` 0, `Reputation` 0). No
open-world events; every encounter is a mission stage.

**Missions.** No `Objective`/`Trigger`/`FailCondition`/`Reward` types; the goal
is an enum plus a `count`/`duration` whose meaning changes per goal. Checkpoints
restore only the stage index. No side missions, contracts, quest givers,
availability conditions or zone-anchored objectives. The design table
(approaches, companions, camps, villages, 79 flags) is **not read by the
runtime** at all.

**Progression and equipment.** No XP or levels. `Loadout` is one PlayerPrefs
string; `ApplyWeapon` runs once at `Start`, so weapons cannot swap mid-mission.
No secondary weapon, armour, accessories, consumables, inventory, crafting; Toku's
steel-to-upgrade loop is unimplemented.

**Interaction.** No centralized interaction system (`Interact` appears in one
file). Prisoners free by proximity, clues by walking within 1.5 m.

**Day/night, weather.** `EnvTheme` sets light/fog/particles per mission theme;
rain, snow, ash, mist exist as theme flags. No time of day, no clock, no
transitions, no gameplay coupling beyond a global `Visibility.AmbientScale`.

**Camera.** No real pitch control, no terrain-height awareness, no auto-recenter,
no explore/combat profiles, no dead-zone, shoulder swap or velocity look-ahead;
`minDistance 2.6` clips walls instead of closing in.

**Mobile controls.** Nine fixed buttons plus a stick, none contextual, no
customization, no dead zone, no sensitivity, **no safe area** (with
`androidRenderOutsideSafeArea: 1`), five canvases with four different reference
resolutions, and a second divergent stick in `ZoneControls`.

**UI.** No minimap, compass, inventory, XP, currency HUD, detection indicator,
interaction prompt, weapon indicator, damage direction, quest log, map, mission
menu, accessibility, or localization path.

**Audio.** 55 SFX, one ambience file (`marsh_ambience.ogg`), no music tracks in
`Resources/Art/Audio/Music`, 1,138 voice AIFFs (101 MB in Resources, Vorbis
0.45 at import). No exploration/combat/boss music assets despite the state
machine in `Sfx3D`.

**Save.** PlayerPrefs only (171 references, 24 files), no JSON, no version, no
migration; player position, world state, inventory, companion state, trust,
intel, approach taken and villain consequences are not saved.

**Tests.** No Unity Test Framework, no asmdef, no unit tests, no perf gate, no
golden-image diff, no CI. The game has **never been human-playtested for feel**
(`CONTINUATION_PROMPT.md`).

---

## 4. What should be improved (not replaced)

- **CombatController** (1,543 lines): fix B1; wire `hitStop` and
  `LastSwingDirection`; promote block to its own input; replace the three special
  bools with a trait list; introduce `IDamageable`/`ITarget`; split into
  `SwingResolver`, `TargetSelector`, `PlayerGuard`, `Core/TimeDip`,
  `PlayerWeaponVisuals`; move `PoisonPuddle` to its own file.
- **EnemyBrain** (2,317 lines): make `AiState` authoritative; add alert decay
  (Alert → Search → Patrol with a 2-3 point sweep) and distance/LOS-gated
  propagation instead of the global alarm; per-def vision fields; enemy-enemy
  separation; abort attacks when blocked; split into `EnemyPerception`,
  `EnemyMotor`, `EnemyAttackResolver`, `EnemyDefence`, per-boss scripts,
  `TelegraphRing`; migrate the three attack-selection paths to the selector.
- **PlayerLocomotion**: acceleration/deceleration, layer masks, fix B8,
  extract stealth/noise/skill/audio concerns.
- **SkeletalRig**: replace `Play(state, 0, phase)` scrubs with crossfades; add a
  2D locomotion blend tree; add foot IK later.
- **CameraRig**: pitch, terrain floor, recenter, profiles, dead-zone, presets to
  a ScriptableObject.
- **MissionDirector**: read `MissionObjective` from the table (B2); read the
  design table (approaches on the briefing, `sets`/`reads` → `StoryFlags`);
  give Defend a leash and Chase a fleeing target; make `SpawnBehindPlayer` true;
  world-position checkpoints.
- **EmberHud** (2,762 lines): one `UiKit.MakeCanvas` with safe area for all
  five canvases; minimum 16 ref-px text; route strings through `Loc.T`; split
  into `ScreenRouter` + one file per screen, `CombatHud`, `TouchInputLayer`,
  `GraphicsTier`, `PauseMenu`.
- **Economy**: pay Ryo in the campaign; rewrite feats/dailies against campaign
  ids.
- **Cloud save**: loop to 100, add a version, sync flags and checkpoints, back
  up guests.
- **Build**: one build entry point, version/keystore from env, merge the two
  define scripts, fix the `.aab` filename mismatch.
- **Repo hygiene**: untrack `test.cs`, `*.log`, `screenshot*.png`,
  `auth_dump.txt`; ignore `*.blob`, `*_BurstDebugInformation_DoNotShip/`.

---

## 5. What should be completely replaced

| Current | Replace with | Why |
|---|---|---|
| `EmberInput` static class + legacy `Input.*` + editor `Scripted` override | Unity Input System `InputActionAsset` with a touch control scheme | Destructive `Consume*()` reads, execution-order races, no rebinding |
| Input half of `EmberHud` + `ZoneControls` | One `TouchControls` prefab: safe-area anchored, contextual action button, layout editor | Two divergent sticks, nine fixed buttons, no customization |
| `NinjaRig` (545-line procedural pose table) | Nothing; every cast member is skeletal now | Dead fallback |
| Vault-by-`ArenaMarkers` and two-raycast wall-run | Probe-based traversal module (capsule sweeps classifying step/vault/mantle/climb heights) | Cannot be extended to ledges or climbing |
| Search = spin in place; Patrol = spin in place | `PatrolRoute` waypoints + guard posts + last-known-position search | Prerequisite for camps and stealth |
| Global `RaiseAlarm` | Local alert propagation with range, LOS and staged awareness | Stealth cannot recover |
| Eight stat-clone named foes | Defs with at least one unique attack or profile each | A recoloured duplicate reads as a cheat |
| PlayerPrefs-as-database | One versioned `SaveData` JSON blob with migrations; PlayerPrefs only for settings | Open-world state cannot live in 171 scattered keys |
| Single-string `Loadout` | Slot-based loadout + stat aggregation, callable at runtime | Prerequisite for secondary weapons, armour, accessories |
| `CampaignDesignTable` as compiled-only documentation | Runtime-readable design data driving briefing, rosters and flags | It is the open-world design and nothing reads it |
| Kenney Fantasy Town / Castle architecture in the village and camp | Japanese modular set (own or licensed) on the same palette | See §7: the valley reads as low-poly fantasy Europe |
| Mixamo stock fantasy bodies for enemies | A coherent East-Asian cast on one silhouette language | Paladin, Vampire, Maw, antlered demon do not belong in Yorune |
| `Zone.unity` as a menu-only sandbox with no exit | The open-world hub scene that owns GameManager, HUD, missions and side content | Or cut the button |
| Hand-run `-executeMethod` gates | CI that runs `EmberCampaignBatch.All`, the bot, snapshot diffs and a perf gate on every push | No regression net |

---

## 6. Performance bottlenecks (found in code; on-device numbers in §7)

- **Rendering tier**: Built-in RP forward with two hand-written CG passes per
  object (`EmberSurface`/`EmberToon` ForwardBase + ForwardAdd); no SRP batcher,
  no lightmaps, no occlusion culling, no LOD groups, no streaming. Grass and
  scatter are individual renderers.
- **AI**: `Update()` on every enemy with no distance LOD; `ArenaMarkers`
  obstacle loops 2-3× per enemy per frame; `Visibility.Of` computed twice per
  frame per enemy.
- **Allocations on hot paths**: B12 (materials per marker/slam, primitives per
  cleave), `ApplyWeapon` hierarchy walks, civilians not pooled, `SlowZone` not
  pooled.
- **UI**: destroy-and-rebuild on every screen change; up to 20 enemy markers
  projected with two different scale conversions.
- **Audio/APK**: 101 MB of voice AIFF in `Resources` (108 MB APK); Vorbis at
  import helps the build but every clip is a Resources entry.
- **Statics leaking across scenes**: `EmberInput`, `MissionBounds`, `Visibility`
  lists, `BodyWatch`.

---

## 7. World, rendering, assets, audio and measured performance

**World.** One analytic valley: `ZoneTerrain` is a pure height function (3-octave
value noise + ring lift + camp plateau + village bowl + road flatten + river
carve, `ZoneTerrain.cs:63-108`), meshed as 64 chunks / 12.8 k tris with a
`MeshCollider` per chunk. `EmberZone.BuildWorld` → `EmberZoneDressing.BuildAll`
places 1,999 prop instances from 134 prefabs. **Rooftop.unity and Marsh.unity are
the same 1,999 instances re-lit**; Zone.unity is the third copy. There is no
streaming: the only streamer in the project is `RoadNorth` (Endless), which
deactivates the valley while it runs. Present places: village, camp, road,
river, two forests, ruin, shrine, two torii, bridge, woodcutters, watchtowers
(camp), training targets (camp), market stalls, farm rows, mountain ring (a
wall, not walkable). Missing entirely: cave, waterfall, temple, burned village
as a place, ninja hideout, samurai outpost, hidden path, mountain pass.
`EnvThemeId` lists 11 places but nine are colour/fog filters over the same
geometry.

**Rendering.** Built-in forward. Six hand-written CG shaders:
`Emberline/Surface` (151 of 172 materials; GGX + Lambert, hemispheric ambient,
rim, per-fragment grime; ForwardBase + ForwardAdd + ShadowCaster) has **no
instancing pragma**, so the 29 materials flagged for instancing get nothing.
`Emberline/Toon` is legacy with no shadow caster and six remaining call sites.
`Ghost`/`Glow`/`GlowTex` are cheap transparent passes. `Grade` is an
`OnRenderImage` full-screen blit (`CinematicGrade.cs`). Lighting is 100 %
realtime: three directional lights, **zero point lights** in the valley despite
~14 lantern props, no lightmaps, no reflection probes, **no occlusion culling
data in any scene**, AA off at every level. Fog exp², procedural skybox tinted
per theme. Graphics tiers in `EmberHud.ApplyGraphicsTier` scale shadow distance
18/30/45, particle density, marker count, pixel lights 1/3/5 (with nothing to
spend them on), cascades, resolution and skin weights.

**Assets.** 442 MB under `Assets/Art`: characters 409 MB (Mixamo 352 MB, one
FBX 51 MB), environments 24 MB, weapons 8.7 MB. Kits: Kenney Nature / Survival /
Fantasy Town / Castle (CC0, atlases repainted on import), KayKit Adventurers /
Skeletons / Hexagon / Dungeon (CC0), Mixamo bodies, seven Sketchfab weapons
**CC BY 4.0 (attribution must be surfaced in-game, currently only
`Assets/Art/CREDITS.txt`)**. Licence bookkeeping is good (16 licence files under
`Assets/Documentation/AssetLicenses`). Coherence is not: CC0 flat-shaded kits
beside photoscan-derived Mixamo bodies. **LOD groups: zero.** Instancing: inert.
Static flags: zone prefabs carry *Everything* (including ContributeGI that is
never baked). 234 textures at 2048 px against the project's own class budgets.
Scene total ≈289 k tris against a 200 k worst-case target.

**Audio.** `Sfx3D`: 12-slot 3D pool, twelve no-repeat banks, crossfading music
sources, synthesised PCM fallbacks for every bark and impact (clever, zero
asset). Clips: 55 SFX, **one** ambience file, **zero music** (`SetMusicState`
looks up `explore_theme`/`combat_theme`/`boss_theme`, none exist), 1,138 voice
AIFFs (101 MB). `Atmosphere.Apply` plays the marsh bed in every biome. Footsteps
pick wood/soft by theme, not by surface. No AudioMixer, snapshots, reverb zones
or occlusion.

**Weather and time.** `Atmosphere` = one camera-following particle column per
weather (rain/snow/ash/mist) plus one ambient-life emitter, tier-scaled. Wind is
a scalar for cloth only; no vegetation sway. `LevelFx` is a second, un-scaled
rain path whose fog tweak is a no-op under exp² fog. River is one Unity plane
with smoothness 0.85. No time of day anywhere.

**Measured performance.** `docs/PERF_BASELINE.md` is a method document whose
results table is **empty**; its sample readout (118 draws / 96 k tris) is from
the old flat deck. **The valley has never been profiled on the reference
device.** No device was attached during this audit, so no number could be
recorded. From scene composition: 1,999 static roots + 64 chunks + cast, with
static batching but no instancing, LOD, occlusion or distance culling, a
down-valley view should land in the **600-1,500 draw-call** range. `PerfGovernor`
defaults gameplay to 30 fps unless tier ≥ 2 (measured earlier: 86-102 % CPU at
60 vs 45-52 % at 30), which is the game admitting the ceiling. Additional
per-frame costs: `ZoneTerrain.HeightAt` (~30 transcendentals) per enemy per
frame and 4× in `SlopeAt`; `ArenaMarkers.RaiseWater` does
`FindObjectsByType<Transform>` over ~3,700 objects; `CinematicGrade` does a
full-screen read/write every frame at tier ≥ 1; `SurfaceKit.Make` allocates a
`Material` per call.

---

## 8. Visual-quality gaps

1. **Identity**: the village is Kenney Fantasy Town (half-timbered European
   houses) and the camp uses Castle kit square towers. Trees are conical pines.
   Torii and shrine are hand-assembled timber. The valley reads as "generic
   low-poly fantasy Europe", not Japan. This is the single largest visual gap and
   it is an asset problem, not a rendering problem.
2. **Cast coherence**: ~30 Mixamo stock bodies of mixed fidelity and genre
   (winged pauldrons, a vampire, an antlered demon, a glowing ghost) beside
   KayKit-style low-poly props.
3. **Lighting**: night themes render so dark that the zone snapshots are barely
   legible; no baked GI, no ambient occlusion, no post grade outside cinematics.
4. **Animation**: pose scrubs with speed 0, no blending on forced poses, no root
   motion, no IK, instant velocity and facing snaps.
5. **Water, weather, sky**: flat river plane, particle-flag weather, no wind on
   vegetation.
6. **UI**: strong home screen, but in-fight clutter (nine buttons, markers,
   objective, challenge, hint, banner, combo, boss bar) and 10-14 px text.

---

## 9. Gameplay-quality gaps

1. Every mission still centres on the origin; the camp, river and forest are
   walkable but nothing is staged there.
2. Stealth is a detection meter with no way to recover once seen.
3. Combat commitment is undermined by B1; block is not an input; no directional
   defence; ranged is a thrown kunai with a different name.
4. Traversal is jump, dash and a wall-run; the valley's roofs, walls and cliffs
   are scenery.
5. Companions, approaches, camps, villages, trust and consequences are authored
   in data the game never reads.
6. No reason to explore: no side content, collectibles, contracts or discoveries
   outside a mission plan.
7. No one has ever judged whether the parry window, dodge timing or camera
   comfort are good on a phone.
