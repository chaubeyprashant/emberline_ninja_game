# Emberline — handoff: Mixamo cast pipeline + duel grid

Continuation prompt for the work sitting uncommitted on `campaign/verification`.
Read `docs/CONTINUATION_PROMPT.md` first for the game as a whole; this covers
only what changed in this session and what is still open.

Copy everything below the line into a new session.

---

You are continuing work on **Emberline 3D** (Unity 6000.5.9f1, Built-in RP,
Android/IL2CPP/ARM64, reference device Samsung Galaxy A33 5G, package
`com.ergebins.emberline3d`). The project is 100% code-generated: scenes,
prefabs, materials and characters are produced by
`Assets/Editor/EmberlineBootstrap.SetupScenes`. Never hand-edit a scene.

Two independent change sets are **uncommitted** on `campaign/verification`.
Both are built, gated and verified on the A33. Nothing is pushed.

# PART 1 — WHAT CHANGED

## 1. Duel select is now a card grid

`BuildFightSelect` in `Assets/Scripts/UI/EmberHud.cs` was a vertical list of
nine rows. It is now a 3×3 grid of cards using the same visual language as the
home screen: panel surface, top hairline with an ember tick, faint index
numeral, name over an accent line, and a footer reading `WON` / `FIGHT` /
`LOCKED`. `DuelCard(int i, Vector2 pos, Vector2 size)` builds one card.

Locked cards still show the opponent's name, greyed, with a concrete unlock
line, so the roster reads as a ladder rather than a wall of `LOCKED`.

Two fixes came out of this:

- **`UiKit.Arrow`** (`Assets/Scripts/UI/UiKit.cs`) draws an arrow from a shaft
  and two rotated bars. The display font has no `→` glyph, and `UiKit.Clean`
  rewrites the character to a hyphen, so the home grid's `OPEN` affordance was
  rendering as a dash; it now uses the drawn arrow, and the new duel cards use
  it too.
- **`EmberUiSnapshot`** shot menus at 1600×900 (16:9) while the canvas
  reference is 1600×720 (20:9). At `matchWidthOrHeight = 0.5` that scaled the
  canvas by 1.118, leaving 1431×805 reference units, so anything past x≈1431
  was cropped. The two screens with content out there lost their third
  column's right edge and the right margin — 85 of the 284-unit third home
  mode card, 65 of the 448-unit third duel card; the other captures had
  nothing past the crop. It now shoots 1600×720, scale factor 1.

The five campaign foes in `Session.Duels` gained a `philosophy` line so all
nine cards have the same two-line body.

## 2. The character pipeline accepts Mixamo humanoid models

`docs/ART_DIRECTION.md` §4.1 specifies the replacement cast: 7–7.5 heads, adult
athletic, **Humanoid rig**, and — critically — that all characters "share one
skeleton so the existing animation mapping keeps working". Every Mixamo
character shares one skeleton, so one animation set can drive the whole cast.
That is why this route was taken.

Seven additive changes. The KayKit path is untouched and all existing characters
regenerate with no warnings.

| Change | Where |
| --- | --- |
| Mixamo character branch: Humanoid, avatar created from this model, authored materials kept in the prefab | `EmberArtImport.OnPreprocessModel` |
| Mixamo animation branch (matched first, `.../Mixamo/Anims`): Humanoid, avatar copied from `MixamoNinja.fbx`, no materials | `EmberArtImport.OnPreprocessModel` |
| Take rename + loop flags (`defaultClipAnimations` is only populated here) | `EmberArtImport.OnPreprocessAnimation` |
| Clip library unions several FBXs (`Spec.clipSources`) | `EmberCharacterFactory.Clips(Spec)` / `Harvest` |
| Humanoid avatar assigned to the built prefab | `EmberCharacterFactory.Build` |
| Weapon grip frame for rigs with no `handslot` empty | `EmberCharacterFactory.GripAnchor` |
| Embedded textures extracted from the FBX | `Assets/Editor/EmberMixamoExtract.cs` |

Details that will bite if you forget them:

- **Mixamo names every exported take `mixamo.com`.** `Harvest` guards the
  naming half of that: a clip still carrying the placeholder name is keyed by
  its filename instead, so the 21 one-take files still land as 21 distinct
  entries and the pose map resolves either way. The rename is not redundant,
  though — `OnPreprocessAnimation` is also where `loopTime`/`loopPose` get set
  for Idle, Run, Block, SideStep and Backstep, and nothing downstream restores
  those. It must happen in `OnPreprocessAnimation`, not `OnPreprocessModel` —
  `defaultClipAnimations` is empty during model preprocessing.
- **A Mixamo character ships zero animations.** Motion comes from separate
  downloads that retarget through the humanoid avatar. `Spec.clipSources`
  exists for exactly this.
- **Without `animator.avatar` every retargeted clip binds to nothing** and the
  character stands in bind pose forever, with no error.
- **A Mixamo hand bone is the wrist joint, not a grip.** KayKit's `handslot`
  empties carry the grip pose; Mixamo has nothing equivalent, so
  `Spec.propOffsetPos` / `propOffsetRot` declare the correction and
  `GripAnchor` builds a child transform from it. The current values were
  chosen from a rotation sweep, not derived. Deriving a frame from the finger
  bones was tried and abandoned: `Quaternion.LookRotation` degenerated to
  identity, which is why negating the axis changed nothing on screen.
- **Render through `Emberline/Surface`, not Unity's Standard shader.** On
  Standard the character took lighting differently from everything else and
  read washed out against the night arenas. `MixamoRenzo` therefore sets
  `texture` to the extracted albedo and uses `MaterialMode.PaletteOverride`,
  which despite the name just means "one material on the game's shader". This
  only works because the model has a single material slot.

## 3. The player is currently the Mixamo ninja

`EmberCharacterFactory.PlayerSpec()` is the single switch. It returns
`MixamoRenzo()`. **Return `Renzo()` to go back to the KayKit model.** Nothing
else needs touching: `EmberlineBootstrap` hoists `playerSpec` once and forwards
it to both pre-attach loops so every catalogue weapon gets the same grip
correction and scale.

This was wired in so the change could be seen on device, and the enemy cast
followed on 2026-09-04 (see the overhaul report), so the player and every
enemy now share one look. Only the five story-cast characters remain chibi.

# PART 2 — MEASUREMENTS

Against the `docs/ART_DIRECTION.md` §4.1 hero budget:

| | Mixamo Ninja | Budget | KayKit RogueHooded |
| --- | --- | --- | --- |
| Triangles | 24,780 | 12,000–18,000 | 3,915 |
| Bones | 52 | ≤ 48 | 41 |
| Material slots | 1 | — | 6 |
| Animation clips in FBX | 2 | — | 152 |
| Normalised world height | 1.800 | — | 1.800 |

**It is over budget on triangles and bones.** That is unresolved. With up to
eight characters on screen it is 6.3× the geometry currently shipped, on a
mid-range Android target.

Both rigs normalise to exactly the same height, so nothing about scale,
collision or camera framing needed changing. If a build looks wrong-sized,
measure before adjusting — an early capture looked oversized and was not.

Cost:

| | Value |
| --- | --- |
| APK | 29 MB → 30 MB |
| Repo assets added | 63 MB (51 MB model, 8.9 MB of 21 animations, 3 MB textures) |

Source textures were 4096² and were downscaled to the 1024² the spec asks for,
which cut the folder from 109 MB. The repo has **no Git LFS** and its history
is 65 MB, so committing raw character art is a lasting cost worth deciding on
deliberately.

Licensing: Adobe's Mixamo FAQ grants royalty-free use for personal, commercial
and non-profit projects and names video games explicitly.

# PART 3 — WHAT IS OPEN

## Known defects found by adversarial verification

Three predate this work and affect the shipped KayKit rig equally:

1. **The pose-coverage check is vacuous.** `EmberCombatCheck` loads
   `Characters/Renzo` and `Assets/Prefabs/Characters/RenzoModel.prefab`;
   neither has ever existed — `Assets/Prefabs/Characters/` holds no prefabs at
   all, and there is no `Resources/Characters/` folder — because the *player*
   is built straight into each scene (`EmberlineBootstrap.BuildPlayer`) and
   never saved as a prefab. `rig` is always null and the `rig == null ||`
   short-circuit makes the assertion always pass. A pose with no clip ships
   silently. The enemies, by contrast, are saved: `BuildEnemyPrefabs()` writes
   thirteen prefabs to `Assets/Prefabs/`, each carrying a `SkeletalRig` whose
   `poseStates` table is complete at 23/23 against `RigPose`. So the cheapest
   fix is to point the check at one of those (`Assets/Prefabs/Samurai.prefab`),
   which passes today; asking the *spec* which poses resolve is the more
   thorough fix, since it would also cover the player.
2. **Only the first weapon trail is driven.** `SkeletalRig` caches one
   `TrailRenderer` via `GetComponentInChildren` and never reassigns it. Five
   right-hand props are pre-attached, each with its own trail, so every weapon
   except the katana swings bare.
3. **The hit flash is a no-op on an opaque rig at the default white tint.**
   `Flash()` sets white at full alpha over a resting state that is also white
   at full alpha, so on the player and on every enemy in its authored state,
   taking a hit shows nothing. It is visible only where the resting state
   differs: on the Pale Shade (`Shade()` is the one spec with a coloured tint,
   plus a ghost alpha bump), on any enemy after `SetBaseColor` — bosses
   recolour on enrage, so every hit in phase 2 flashes — and on any ghosted
   rig even at a white tint, since `Flash()` lifts alpha by 0.3 (Kagachi's
   clones are ghosted at 0.45). A bought cosmetic dye also makes it visible on
   the player, and that path carries a second defect the flash exposes:
   `Flash()` overwrites the dye's `_Color` block, and when it expires
   `ApplyTint()` restores `tint` (white) rather than the dye, so the first hit
   strips the cosmetic until the next weapon equip re-runs `Cosmetics.ApplyTo`.

## Not done

- **The cast conversion happened after this document** (2026-09-04): all
  thirteen enemy kinds now build from Akai, Kachujin, Nightshade, Ganfaul,
  Brute and the Ninja. Read `docs/ENEMY_CHARACTER_ASSET_AUDIT.md`,
  `docs/ENEMY_CHARACTER_SELECTION.md` and
  `docs/ENEMY_CHARACTER_OVERHAUL_REPORT.md` for what was chosen and why;
  Brady (oni mask) was rejected at 53k tris. Sections 1 and 2 above remain
  the accurate description of the pipeline.
- **Polycount is not addressed.** Decimation, an LOD chain, or a revised budget
  is needed before this ships.
- **Grip offsets are per-weapon-untuned.** One rotation serves all five
  catalogue weapons. The axe and crossbow have not been looked at.
- **No commit, no push.**

# PART 4 — HOW TO VERIFY

Unity binary:
`/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity`

```
Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.SetupScenes -logFile Logs/setup.log
```

Then grep the log for `error CS` and for `no hand slot`. Both must be zero.

The three authoritative gates, all of which must print `ALL PASSED`:

```
Emberline.EditorTools.EmberDuelIntegrity.Run
Emberline.EditorTools.EmberDifficultyScalars.Run
Emberline.EditorTools.EmberCombatCheck.Run
```

**Rendering checks** — `EmberMixamoCompare.Run`, `EmberGripSweep.Run`,
`EmberSnapshot.RenderArenas`, `EmberUiSnapshot.Render` — write PNGs to
`Logs/` and must run **without** `-nographics`; the software path segfaults in
shadow rendering.

**Log-only reports** — `EmberMixamoProbe.Run`, `EmberPlayerScale.Run`,
`EmberMixamoClips.Run` — write no image. Read the `[MX]` / `[PS]` / `[MXC]`
lines in the log file. `-nographics` is fine and is how the PART 2
measurements were taken (see `Logs/mx3.log`, `Logs/ps3.log`, `Logs/mxc2.log`).
Each calls `EditorApplication.Exit(0)` itself, so `-quit` is redundant.

| Tool | Shows |
| --- | --- |
| `EmberMixamoCompare.Run` | Mixamo vs KayKit side by side, posed, with a budget report |
| `EmberMixamoProbe.Run` | tris/bones/materials/avatar/bone-name resolution |
| `EmberPlayerScale.Run` | world height, scale and material state for both specs |
| `EmberMixamoClips.Run` | force-reimports every FBX under `Assets/Art/Characters/Mixamo/Anims` (not read-only), then reports names and retargeting |
| `EmberGripSweep.Run` | the same character at four grip rotations |
| `EmberSnapshot.RenderArenas` | the player in a real scene |
| `EmberUiSnapshot.Render` | every menu screen, plus `ui_perf_overlay.png` and `ui_weapon_glyphs.png` |

Device:

```
~/Library/Android/sdk/platform-tools/adb install -r Builds/emberline3d.apk
~/Library/Android/sdk/platform-tools/adb shell monkey -p com.ergebins.emberline3d -c android.intent.category.LAUNCHER 1
```

The activity is `UnityPlayerGameActivity`, so `am start …UnityPlayerActivity`
fails silently. `screencap` is landscape 2400×1080 while the app is foreground;
a portrait black capture means it is not. Canvas is 1600×720 at 0.5 match, so a
device tap is roughly canvas × 1.5. Use `input swipe x y x y 90` rather than
`input tap` — a press-and-release registers with Unity's UI more reliably.
Ignore the `AssetPackManager` `ClassNotFoundException`; it is benign for
non-Play builds.

# PART 5 — HOW TO WORK ON THIS

- Read before changing. Prefer additive changes that preserve behaviour.
- Compile after every change. Snapshot and **look** before claiming a visual
  works. Run on device before claiming anything works.
- Measure before adjusting. Two apparent problems in this session (character
  oversized, blade axis inverted) were wrong diagnoses that measurement or
  instrumentation corrected.
- **Do not push or open a pull request** unless told to in that message.
  Report what is ready instead.
- Report honestly: what you verified, what you did not, what you could not do.
