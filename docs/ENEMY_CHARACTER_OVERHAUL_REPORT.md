# Enemy character overhaul — report

Closing report for the enemy visual overhaul. Audit:
`docs/ENEMY_CHARACTER_ASSET_AUDIT.md`. Selection:
`docs/ENEMY_CHARACTER_SELECTION.md`. Pipeline mechanics:
`docs/MIXAMO_CAST_HANDOFF.md`. Work is **uncommitted** on
`campaign/verification`; nothing is pushed.

## What changed, in one sentence

Every enemy kind is now built from one of five free Mixamo bodies on the same
skeleton, shader and pipeline as the Mixamo player, through the existing
generator — no combat, AI, difficulty, duel, story, camera or UI code changed.

## Per-character result

Status is what a runtime build actually does, not what the spec says.
"Device" means seen on the Galaxy A33 in the build from this session.

| Character | Old asset | New asset | Source | Licence | Rig | Animation strategy | Weapon | Prefab | Performance notes | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Renzo (player) | KayKit `RogueHooded.fbx` | Mixamo **Ninja** (`MixamoNinja.fbx`) | mixamo.com | Adobe Mixamo, royalty-free incl. commercial games | Humanoid | 21 retargeted clips (`Mixamo/Anims`) | sword_1handed prop on grip anchor | built into each scene by `BuildPlayer` | 24,780 tris, 52 bones, 1 slot | Device: gameplay, combat |
| Raider (Bandit) | `Rogue.fbx` | **Akai E Espiritu**, worn-brown tint | mixamo.com | same | Humanoid | shared set | twin daggers (props; were embedded knives) | `Assets/Prefabs/Bandit.prefab` | 10,386 tris, 65 bones, 1 slot | Regenerated; lineup |
| Weaver / Archer (Ranged) | `Mage.fbx` | Akai, natural | same | same | Humanoid | shared set; aim = Windup, shoot = Throw, retreat = Backstep | hand crossbow prop | `RangedWeaver.prefab` | as Akai | Regenerated; lineup |
| Goro (Chief) | `Barbarian.fbx` | **Brute**, 2.25 m | same | same | Humanoid | shared set | greataxe prop (model's own axe hidden) | `BanditChief.prefab` | ~22.8k tris drawn of 31,301; 72 bones; 6 of 10 slots | Device: duel |
| Shade / Pale Shade | `Skeleton_Minion.fbx` | Akai under the ghost shader, pale blue | same | same | Humanoid | shared set; unarmed strikes = Kick/Stab | none | `Shade.prefab` (Pale Shade rides it by def) | as Akai; ghost material, no shadows | Device: Pale Shade duel |
| Kagehira / Kagachi | `Skeleton_Warrior.fbx` | **Ganfaul M Aure**, 2.1 m | same | same | Humanoid | shared set | sword_1handed prop, trail | `Kagachi.prefab` | 13,801 tris, 99 bones, 1 slot; clones ×2 in phase 4 | Regenerated; lineup |
| Jin Kurogane | `Knight.fbx` | **Nightshade J Friedrich**, 1.85 m | same | same | Humanoid | shared set | sword_2handed prop, trail (trail was inert before: no prop) | `Jin.prefab` | 12,999 tris, 68 bones, 1 slot | Regenerated; lineup |
| Axe Raider | `Knight.fbx` | Brute, soot tint, 1.95 m | same | same | Humanoid | shared set | greataxe prop | `RaiderAxe.prefab` | as Brute | Regenerated; lineup |
| Pike Guard | `Knight.fbx` | **Kachujin G Rosales**, steel-blue tint | same | same | Humanoid | shared set; Strike1/2 = Stab | sword_2handed scaled into a spear shaft | `PikeGuard.prefab` | 12,610 tris, 75 bones, 2 slots | Regenerated; lineup |
| Powder Carrier (Bomber) | `Mage.fbx` | Ninja, ochre tint | same | same | Humanoid | shared set; lob = Throw, wind-up = Windup | smoke bomb prop | `Bomber.prefab` | as Ninja | Regenerated; lineup |
| Assassin | `Skeleton_Rogue.fbx` | Akai, bruised-violet tint | same | same | Humanoid | shared set; dash = SideStep | twin daggers | `Assassin.prefab` | as Akai | Regenerated; lineup |
| Samurai | `Knight.fbx` | Kachujin, natural red | same | same | Humanoid | shared set | sword_2handed, trail | `Samurai.prefab` | as Kachujin | Regenerated; lineup |
| Rogue Ninja | `RogueHooded.fbx` | Ninja, charcoal tint | same | same | Humanoid | shared set; Strike2 = Stab | dagger | `RogueNinja.prefab` | as Ninja | Regenerated; lineup |
| Elite Warrior | `Skeleton_Warrior.fbx` | Kachujin, dark-bronze tint, 2.1 m | same | same | Humanoid | shared set | greataxe, trail | `EliteWarrior.prefab` | as Kachujin | Regenerated; lineup |
| Named foes (×7) | base kind's body | base kind's new body | — | — | — | — | — | none of their own | — | Inherit (verified in audit §0) |

### Successfully replaced

All thirteen enemy kinds. Every `Assets/Prefabs/<Name>.prefab` now nests a
Mixamo body plus its prop (verified by GUID), `SetupScenes` regenerates them,
the three gates pass, and the lineup poses every body through its own clip
map.

### Needs manual review

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
