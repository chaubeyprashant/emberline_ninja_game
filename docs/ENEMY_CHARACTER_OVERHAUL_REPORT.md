# Enemy character overhaul — report

Closing report for the enemy visual overhaul. Audit:
`docs/ENEMY_CHARACTER_ASSET_AUDIT.md`. Selection:
`docs/ENEMY_CHARACTER_SELECTION.md`. Pipeline mechanics:
`docs/MIXAMO_CAST_HANDOFF.md`. Work is **uncommitted** on
`campaign/verification`; nothing is pushed.

## What changed, in one sentence

Every enemy kind is now built from one of thirteen free Mixamo bodies on the same
skeleton, shader and pipeline as the Mixamo player, through the existing
generator — no combat, AI, difficulty, duel, story, camera or UI code changed.
No two enemies that can share a wave share a body.

## Per-character result

Status is what a runtime build actually does, not what the spec says.
"Device" means seen on the Galaxy A33 in the build from this session.

| Character | Body | Tint | Height | Prefab | Cost | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Renzo (player) | Ninja | none | 1.80 | built into each scene | 24,780 tris, 1 rend | Device |
| Raider (Bandit) | Akai | warm leather | 1.72 | `Bandit.prefab` | 10,386, 1 | Sheet |
| Weaver / Archer | **Erika Archer** | none | 1.68 | `RangedWeaver.prefab` | 20,526, 4 | Sheet |
| Assassin | **Arissa** — hooded cutthroat, long coat | none | 1.76 | `Assassin.prefab` | 8,970, 4 | Sheet |
| Rogue Ninja | Ninja, pale cold | moonlit steel | 1.78 | `RogueNinja.prefab` | as Ninja | Sheet |
| Powder Carrier | **Pirate** | night slate | 1.70 | `Bomber.prefab` | 13,115, 1 | Sheet |
| Shade / Pale Shade | Akai, ghosted | pale blue, 0.55α | 1.62 | `Shade.prefab` | as Akai | Device |
| Pike Guard | Kachujin, cold | cold steel | 1.95 | `PikeGuard.prefab` | 12,610, 1 | Sheet |
| Samurai | Kachujin | none | 1.88 | `Samurai.prefab` | as Kachujin | Device (Convoy Captain) |
| Elite Warrior | **Uriel A Plotexia** | none | 2.05 | `EliteWarrior.prefab` | 11,026 but **22 rend** | Device (Iron Guard) |
| Axe Raider | Brute, cold | soot | 1.98 | `RaiderAxe.prefab` | ~22.8k, 5 | Sheet |
| Goro (Chief) | Brute, warm | firelit | 2.25 | `BanditChief.prefab` | as Brute | Device |
| Jin Kurogane | Nightshade | none | 1.85 | `Jin.prefab` | 12,999, 1 | Device |
| Kagehira / Kagachi | Ganfaul | none | 2.10 | `Kagachi.prefab` | 13,801, 1 | Sheet |
| The Iron Guard | Uriel, cold iron | iron | 2.10 | `Named_ironguard.prefab` | as Uriel | Device |
| The Drowned Guardian | **Maw** — antlered marsh brute | none | 2.30 | `Named_drownedguardian.prefab` | 13,910, 1 | Device |
| Commander Hoshu | **Paladin** — dark plate, great helm | none | 1.98 | `Named_finalcommander.prefab` | 14,660, 2 | Device |
| The Three Blades | Vampire | none | 1.74 | `Named_threeblades.prefab` | 15,022, 1 | Sheet |
| The Scavenger King | Pirate, rust | rust | 1.95 | `Named_raiderleader.prefab` | as Pirate | Sheet |
| Convoy Captain, Three Blades, Pale Shade | base kind's body | — | — | none of their own | — | Inherit (no collision) |

### Successfully replaced

All thirteen enemy kinds, across ten bodies. Every
`Assets/Prefabs/<Name>.prefab` nests its intended body plus its prop (verified
by GUID), `SetupScenes` regenerates them, the three gates pass, and
`EmberCastCheck` reports every body slot resolving to a texture.

### The second pass, and what it fixed

The first pass put thirteen enemies on five bodies and separated them with
0.5–0.8 grey tints. That failed: multiplying an already-dark albedo by a grey
only darkens it, so four enemies on the Akai body and three on Kachujin read
as the same character. Four more bodies were added (Erika, Vampire, Uriel,
Pirate) and the tints rebuilt around values that actually shift hue and can
exceed 1.0 to brighten.

Three real defects surfaced while verifying it, all found by looking rather
than by reasoning:

1. **`SlotMaterial` collapsed distinct slots onto one asset.** It stripped
   non-alphanumerics for the filename, so Kachujin's `kachujin_MAT` and
   `kachujin_MAT_` both became `Mat_X_kachujinMAT.mat` and the second
   overwrote the first. All three Kachujin characters silently lost their
   armour texture. Fixed with a deterministic FNV-1a tag — not
   `string.GetHashCode`, which is randomised per process and would churn asset
   names on every regeneration.
2. **The Archer rendered nude.** Erika's mesh and material names are shuffled
   against each other, and the two slots were mapped the wrong way round, so
   her armour drew with the skin atlas. `Body_MAT` is the skin.
3. **The Bomber was white.** The Pirate atlas is mostly pale grey, and the
   first tint brightened it further — the opposite of what a night saboteur
   needs.

**A fourth defect only showed up in play: the named foes were clones.** The
seven named campaign foes are `CopySerialized` copies of a base kind's def, so
they inherited its body. `EnemyDef.modelSpec` existed for this and nothing read
it, which left two pairs of *duel opponents* in identical skins two rungs
apart — the Drowned Guardian and the Iron Guard, the Convoy Captain and
Commander Hoshu. `GameManager.PrefabFor(kind, defId)` is now the one place that
picks a body, and the bootstrap emits `Named_<id>.prefab` for any foe that
declares its own; the rest still fall back to their kind.

**A fifth, on the same theme: a recolour is not a unique character.** The first
pass at the named foes gave the Drowned Guardian Goro's body in green and
Commander Hoshu Jin's armour in bronze — the tint defect one level up. Three
more bodies (Maw, Paladin, Arissa) were added so that **all nine duel
opponents are different models**, not different colours. Morak was downloaded
for this and rejected at 47,768 triangles from a 114 MB source.

**The contact sheet was itself misleading and had to be fixed first.**
`SkeletalRig` pushes `spec.tint` into a `MaterialPropertyBlock` in `Awake`,
which never runs in edit mode, so every variant of a shared body rendered
identically in the preview — precisely the thing the preview exists to catch.
`EmberCastSheet` now applies the tint itself.

### Needs manual review

- **Uriel costs 22 draw calls.** Its triangle count is modest (11k) but it
  arrives as 22 separate skinned meshes. It went to the Elite Warrior — tough
  and never numerous — for that reason, and the Iron Guard duel held 59.8 fps
  at 247 setpass calls. If Elites ever spawn in threes, revisit this first.
- **Per-weapon grip.** One grip correction serves every weapon on every body.
  The greataxe and the spear shaft are the ones to eyeball in play.
- **Goro and the Axe Raider have no slash trail.** Their specs set no
  `trail`; only Jin, Kagachi, Samurai and the Elite Warrior carry one.
  Setting `trail = true` on the two Brute specs is a one-line change if the
  greataxe should streak. Note the separate pre-existing defect this meets:
  `SkeletalRig` caches only the first `TrailRenderer` it finds, so even a
  spec that asks for a trail only gets one on the weapon that happens to be
  first. Fixing the spec flag without fixing that cache will not show a trail.
- **Goro at 2.25 m.** Reduced from the chibi's 2.45 because a realistic body
  at 2.45 read as a giant; the brief allows larger bosses, so this is a taste
  call.
- **Female body on Samurai, Pike Guard and Elite Warrior.** Kachujin is the
  only authentically Japanese figure in the library. Fine for mooks; flagged.
- **Ranged animation.** The shared set has no bow-draw. The archer aims with
  the power-up clip and shoots with the one-hand cast. Acceptable at distance;
  a "Lite Longbow Pack" download (7 clips, small) would improve it.

### Not replaced, and why

- **Story cast** (Father, Mother, Aiko as a child, the village child, young
  Ren): out of the brief's scope ("do not change the story"). They remain
  KayKit chibis and appear in the opening cinematic beside the realistic
  Renzo. This is the most visible remaining mismatch and is a story-art task.

## Performance

Measured on the A33 with the game's own `PerfOverlay`. It had no in-game
toggle (it reads a PlayerPref), so it now also honours a flag file that adb
can create on a release build:

```
adb shell touch /sdcard/Android/data/com.ergebins.emberline3d/files/perf_overlay
```

The rolling window is ~4 s; `hitches` counts frames over 33.3 ms, so on the
30 fps menu cap every frame registers as one and the count is meaningless
there. The gameplay cap on this save was 60.

| Scene | Bodies on screen | fps | p50 / p99 frame | worst | tris | setpass |
| --- | --- | --- | --- | --- | --- | --- |
| Jin duel (Rooftop, rain) | Renzo + Jin | 59.8 | 16.7 / 16.8 ms | 67–100 ms (boss intro) | 228k | 181–183 |
| Iron Guard duel (Uriel, 22 renderers) | Renzo + Elite | 59.8 | 16.7 / 16.8 ms | 284 ms (intro) | 219k | 247 |
| Convoy Captain duel (Kachujin) | Renzo + Samurai | 59.5 | 16.7 / 16.7 ms | 67 ms | 226k | 184 |
| Drowned Guardian duel (Maw) | Renzo + Guardian | 59.8 | 16.7 / 16.8 ms | 251 ms (intro) | 218k | 158 |
| Iron Guard duel (named prefab, Uriel) | Renzo + Iron Guard | 57.4 | 16.7 / 50.2 ms | 50 ms | 220k | 258 |
| Jin duel, three fresh launches | same | 59.8 each | 16.7 / 16.8 ms | 67 ms | 228k | 181 |
| Main menu | — | 29.9 (30 cap) | 33.5 ms | 184 ms (load) | 1k | 14 |

The duel holds the cap with the two heaviest bodies in the game on screen
(Nightshade 13k, Ninja 24.8k, plus props and arena). A full wave scene with
six enemies was not captured on the new bodies (see the anomaly below), so
the eight-character worst case remains unmeasured on device.

### An anomaly, chased and closed

In one session several launches rendered the **old** cast: a chibi player, a
KayKit Knight as Jin, and a Jin duel missing its rain theme, as though the
scene were from an earlier build.

It is not in the build. The installed APK's combat scenes contain no KayKit
meshes at all — `level1` and `level2` hold only Mixamo bodies; KayKit meshes
survive solely in `level0`, for the Opening's story cast, which is correct.
No code path substitutes a body at runtime: `EnemyPool` instantiates
`GameManager.enemyPrefabs[(int)kind]` and nothing else, and the graphics-tier
and thermal governors change shadows, resolution and frame cap, never models.

To settle it rather than argue from the build, the game now logs the
`VisualRoot.modelId` of every body at scene start and at each enemy spawn
(`[Cast]` lines in logcat). Across a clean reinstall and four launches
covering the Jin duel three times and a Marsh wave mission, every line read
`MixamoRenzoModel` for the player and `JinModel` for Jin, and every capture
showed the new bodies — including the same Marsh mission that had previously
shown a chibi.

Conclusion: a stale install in that one session, not a defect in the cast
work. The `[Cast]` logging is left in deliberately, so a recurrence can be
diagnosed from logcat in one launch instead of an afternoon; remove the two
`Debug.Log` calls in `Assets/Scripts/GameManager.cs` and
`Assets/Scripts/Enemies/EnemyPool.cs` if the noise is unwanted.

Budget context: the `docs/ART_DIRECTION.md` §4.1 hero budget is 12–18k
triangles and ≤ 48 bones. Four of six bodies are inside the triangle budget;
the commonest mook body (Akai) is the lightest at 10.4k. Every body is over
the bone budget (52–99) — that is Mixamo's auto-rig and is not editable here.
No LOD system existed and none was added; skinned renderers keep the
project's existing shadow strategy (tiered shadow distance, one key light).

Asset weight on disk after texture downscale to 1024²:

| Item | Size |
| --- | --- |
| `Mixamo/*.fbx` (6 bodies) | 118 MB |
| `Mixamo/Anims` (21 clips) | 8.9 MB |
| `Mixamo/Textures` | ~30 MB |
| APK | 30 MB (unchanged from the player-only build) |

The repo has **no Git LFS** and its history was 65 MB. Committing ~160 MB of
character sources is a decision to make deliberately; the Brady body
(110 MB, 53k tris) was downloaded, measured and removed.

## What the generator now does differently

- `EmberCharacterFactory.Mixamo(spec, body, albedo)` fills the shared fields
  for a Mixamo body: FBX, texture, sockets, grip offsets, clip sources.
- `MaterialMode.ConvertAuthored` + `Spec.slotTextures` give a multi-part body
  one game-shader material per authored slot with that slot's own albedo
  (longest prefix match). `Spec.hideRenderers` deactivates parts the props
  replace (Brute's embedded axe) or that cost more than they show.
- `EmberCharacterFactory.ResolveClip(spec, pose)` lets tooling pose any body
  through its own map; `EmberSnapshot.RenderLineup` uses it, and
  `EmberSnapshot.RenderBosses` renders the hierarchy sheet
  (`Logs/bosses.png`).
- `EmberMixamoExtract.Run` and `EmberMixamoProbe.Run` run over every body in
  the folder.

## Verification performed

- `SetupScenes`: 0 compile errors, 0 missing-hand-slot warnings, 0 exceptions.
- Gates: `[DUEL] ALL PASSED`, `[DSC] ALL PASSED`, `[C2] ALL PASSED`.
- Prefab nesting verified by GUID for all thirteen kinds.
- `Logs/lineup.png` (all fourteen bodies posed), `Logs/bosses.png`.
- Device: Pale Shade, Goro and Jin duels entered and fought on the new
  bodies; no Unity runtime errors in logcat (the `AssetPackManager` warning
  is benign for non-Play builds).

Not done on device: every kind in every mode. The mission and endless
rosters spawn from the same thirteen prefabs the duels use, so the body
substitution is the same code path; but "every enemy, every pose, every
arena" is a human playtest and `docs/QA_CHECKLIST.md` exists for it.
