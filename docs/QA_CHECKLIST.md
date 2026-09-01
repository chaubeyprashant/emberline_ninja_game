# Emberline 3D — QA Checklist

Pre-ship pass for build **1.2.0 (code 8)** on the reference device
(Samsung Galaxy A33 5G, `SM-A336E`). Everything below needs a human with the
phone in hand — none of it can be verified from a desktop build.

Work top to bottom. Mark ✅ / ❌ and note the build code you tested.

> **What the release pass could and could not check.** The 1.2.0 audit ran
> statically and through editor harnesses: asset and settings integrity, the
> encounter tables, the line-of-fire fix, and a real APK build. **Nothing below
> has been played.** No device has been attached since the Phase 5 stealth check,
> so every claim about feel — responsiveness, parry and dodge windows, enemy
> reactions, boss pacing, camera comfort, frame rate, thermals and battery —
> remains unverified. Those are exactly the items this checklist exists for.

### New in 1.2.0 — verify these first

- [ ] **Pause actually pauses.** Open settings mid-fight: enemies must freeze.
      Close it: time resumes, and a hit-stop that was running does not resume
      the game early or leave it dilated.
- [ ] **Nothing hits through cover.** Stand with a chimney or crate between you
      and a pike guard (3.6m reach) — the thrust must not land. Then stand
      *against* the same cover with an enemy adjacent: you must still be
      hittable. Both directions matter; the second is the exploit.
- [ ] **Attack buffering.** Tap STRIKE just before the current swing ends — the
      next hit should come out. It must not double-fire, and a ranged weapon
      must fire exactly one bolt per press.
- [ ] **Staged missions run.** All ten story levels with a mission plan now load
      their plan for the first time (the assets were previously unreadable).
      Every one needs a full playthrough — objectives, checkpoints, debrief.
- [ ] **Shade, Bomber and Jin** now carry EnemyDefs. Check posture, guard break
      and backstab behave like the other enemies and that their damage and
      health feel unchanged.

```bash
adb install -r Builds/emberline3d.apk
```

---

## 0. Smoke

- [ ] App launches to the **main menu** — not into a mission.
      *(Known anomaly: on 2026-08-31 a launch came up already inside a Road
      North run at 0:00. Unconfirmed — the device dropped off USB before it
      could be reproduced. If it recurs, capture `adb logcat -s Unity:V` from
      launch and check whether `Session.Mode` is somehow non-`None`.)*
- [ ] No crash in the first 60 seconds: `adb logcat -s AndroidRuntime:E CRASH:E`
      stays empty. (One `AssetPackManager` ClassNotFound line at startup is
      normal for a plain APK — ignore it.)
- [ ] Menu shows STORY / FIGHT / MARCH cards with sensible counts.

## 1. Time scale — the highest-risk regression

Hit-stop writes `Time.timeScale` globally. Anything that leaves it dipped
soft-locks the game into slow motion.

- [ ] Land a normal 3-hit chain: time returns to normal immediately after.
- [ ] **Die mid-combo** (let a hit kill you during a swing). The defeat screen
      must run at full speed.
- [ ] Die during a **Surge nova**, then hit RISE AGAIN — the retry must not be
      in slow motion.
- [ ] Quit to MENU mid-fight while a crush hit lands. Menu animates normally.
- [ ] Trigger a boss intro cinematic with a hit-stop active — the intro plays at
      normal speed and gameplay resumes normally.
- [ ] Background the app mid-hit-stop (home button), reopen. Normal speed.

## 2. Pooling — leaks and recycled-state bugs

- [ ] Road North: clear 6+ packs. Every wave's enemies animate normally — none
      arrive T-posed, frozen in a death pose, or already red from a past enrage.
- [ ] Recycled enemies spawn at **full health** (watch a health marker on spawn).
- [ ] Difficulty does not creep on replay: a bandit at 100m feels the same on a
      fresh launch as after a long run (base-stat capture).
- [ ] Fight two road bosses of the same kind (≈300m apart). The second is not
      pre-enraged and its bar starts full.
- [ ] Kagachi's clones die without leaving ghost-tinted enemies in later waves.
- [ ] Menu → level → die → retry → menu, **five times**. Memory on the perf
      overlay is flat versus the first reading (see `PERF_BASELINE.md` S11).
- [ ] Throw a kunai, then quit to menu mid-flight, then start a new mission —
      FLICKER still dodges normally (stale warp anchor cleared).

## 3. Story mode

- [ ] **L1 FIRST BLOOD** — plain clear-all, 2 waves, wins on the last kill.
- [ ] **L2 THE LANTERN ROAD (escort)** — Yotsu walks the diagonal; bar reads
      "YOTSU — THE LANTERN" in ember; he halts when enemies are near and resumes
      when clear; waves keep cycling; **win fires on his arrival**, not wave count.
- [ ] L2 failure — let enemies sit on him: "THE FLAME IS OUT" → defeat screen.
- [ ] **L3 EYES IN THE DARK (stealth)** — enemies start unaware and sweep their
      gaze; striking one from behind reads "ASSASSINATE" with **no screen shake**.
- [ ] L3 detection — stand in a cone: bar fills and reads "EYES SEARCHING" →
      "THEY'RE LOOKING RIGHT AT YOU"; break line of sight behind a chimney and it
      decays.
- [ ] L3 alarm — let it fill: "ALARM — THEY'VE SEEN YOU", everyone wakes, the
      mission continues as a fight, and the end rank is visibly lower.
- [ ] **L4 GORO** — boss intro card plays; slam leaves a ground scar that
      visibly slows you; scar fades after ~4.5s.
- [ ] L5–L9 still play as plain clear-all with no objective text regressions.
- [ ] **L10 KAGACHI** — phase 2 telegraphs ("THE SERPENT COILS…", he holds still
      and rings red) *before* clones appear; phase 3 raises the water.
- [ ] Victory screen: rank, stars, shards, and any feats all render.

## 4. Duels

- [ ] Briefing shows a **TERMS** button; cycling it moves through EVEN TERMS →
      IRON WILL → STORM PACE → ONE BREATH with the shard bonus shown.
- [ ] ONE BREATH visibly halves the life bar; a win pays +2 shards.
- [ ] IRON WILL opponent is noticeably tankier; STORM PACE noticeably faster.
- [ ] The chosen terms persist after closing and reopening the app.
- [ ] Story levels are **unaffected** by the duel modifier.

## 5. Road North (March)

- [ ] Corridor streams ahead and reclaims behind; `road segs` on the perf
      overlay settles ≤8 and stops growing.
- [ ] Packs trigger by distance; the mist barrier seals the road while a pack
      lives and bursts when it dies.
- [ ] Boss bars the road at ~150m, then every ~200m, each with an intro card.
- [ ] Shard awarded every 100m.
- [ ] **Pack modifiers at packs 5/10/15/20** — each announces and visibly
      changes the fight (fog closes / shades replace bandits / extra archers /
      everything faster). Modifier shows in the top-right wave label.
- [ ] Distance record persists: die, relaunch, best distance is remembered.
- [ ] You cannot escape: try hard to jump/wall-run off the causeway sides, and
      over the mist barrier. Both must be impossible.

## 6. Traversal

- [ ] Jump works from the JUMP button and the space bar equivalent on device.
- [ ] **Coyote time** — run off a ledge and press jump ~0.1s late: still jumps.
- [ ] **Buffer** — press jump just before landing: fires on touchdown.
- [ ] **Vault** — run at a chimney holding the stick into it and press JUMP:
      clears the top with forward carry, does not stall on the side.
- [ ] **Air flicker** — one mid-air dash per jump (two with SKY STEP bought).
- [ ] **Wall run** — jump toward a parapet holding movement *along* it: ~1s run
      with after-images; JUMP kicks off. A head-on wall must not latch.
- [ ] Cannot leave the arena in either scene.

## 7. Combat depth

- [ ] **Cone soft-lock** — with enemies front and behind, pushing the stick
      toward the front one makes the swing hit *that* one.
- [ ] **TARGET** cycles an ember-tinted lock marker; swings follow it for ~4s.
- [ ] **Deflect** — press CLEAVE into an incoming swing: "DEFLECT", no damage,
      attacker staggered, Sen jumps. A late cleave still takes the hit.
- [ ] CLEAVE button lights pale while deflecting, dims while recharging.
- [ ] **Launcher** — the 3rd chain hit pops mooks airborne; bosses never launch.
- [ ] **Kunai warp** — FLICKER while a kunai is in flight teleports you to it;
      with no kunai out, FLICKER dodges normally.
- [ ] **Execution** — stagger a mook below 20% and hit it: "EXECUTE", instant
      kill. Bosses never execute.
- [ ] Shake stacking — a Chief slam's shake is not cut short by a light hit
      landing immediately after.

## 8. Enemy AI

- [ ] No enemy ever stands inside a chimney, crate stack or rubble pile — also
      after being knocked into one by a crush hit.
- [ ] A bandit pack fans out rather than queueing single-file.
- [ ] Hide behind a chimney from an archer: it repositions to regain the shot.
- [ ] A shade arcs around toward your back before closing.
- [ ] Jin refuses to be crowded and backs off after each exchange.
- [ ] In a 7+ pack, attacks come in a staggered rhythm, not in unison, and no
      single enemy monopolises the aggression.

## 9. Meta progression

- [ ] Skills screen shows **four** columns: COMBAT / DEFENSE / EMBER / TRAVERSAL,
      no overlap, all cards legible.
- [ ] Buying SKY STEP grants a second air flicker; ROOFRUNNER visibly lengthens
      wall runs; BLADE TETHER makes the flicker ring refill faster after a warp
      than after a dodge.
- [ ] Feats fire once and pay 1 shard: GHOST WALK, LANTERN SHEPHERD, IRON
      ANSWER (5 deflects), ROOFRUNNER (3 wall runs).
- [ ] Daily challenge shows in the Codex and pays out once per day; the two new
      types (4 deflects / stealth unseen) are reachable.
- [ ] Blade finishes unlock at 0/9/18/27 stars.
      ⚠️ **DROWNED GOLD is set to 36 stars and is currently unobtainable** — the
      story caps at 30 (10 levels × 3). Decide before shipping: lower to 30, add
      star sources, or add levels.

## 10. Options, HUD, input

- [ ] Graphics tiers 0/1/2 visibly change particle density and marker count
      mid-fight, with no reload needed.
- [ ] Gyro toggle appears (device has a gyroscope), persists, and tilting aims
      the camera. **Confirm both axes are the right way round** — this has never
      been verified on hardware.
- [ ] Touch camera: right-side drag orbits and tilts; left side is the stick; a
      second finger on the stick side never spins the camera.
- [ ] All seven combat buttons respond on press (not release) and none overlap.
- [ ] Perf overlay: four-finger tap toggles it; numbers update; hidden by default.
- [ ] Tutorial hints on L1 progress move → strike → jump, then stop.

## 11. Performance

Run the scenarios in [PERF_BASELINE.md](PERF_BASELINE.md) and fill its results
table. Minimum before shipping:

- [ ] S2 (light combat) and S6 (road at ~500m) hold ≥45fps with p95 ≤28ms.
- [ ] S10 soak (15 min): memory plateaus rather than climbing.
- [ ] `gc/frame` near zero during kunai spam and Thread Burst (S8/S9).

## 12. Build settings

Verified from the build script and `ProjectSettings.asset` on 2026-08-31:

| Setting | Value | Note |
| --- | --- | --- |
| Package | `com.ergebins.emberline3d` | consistent APK + AAB |
| Version | 1.1.0 (code 7) | bumped this phase |
| Scripting backend | IL2CPP | required for 64-bit |
| Architectures | ARM64 (APK), ARM64+ARMv7 (AAB) | Play 64-bit rule met |
| Min SDK | 26 | |
| Target SDK | **Automatic** | resolves to 36 on this machine |
| Orientation | LandscapeLeft | |
| Engine code stripping | On | `Assets/link.xml` preserves PhysicsModule |

- [ ] ⚠️ Consider **pinning Target SDK** instead of Automatic — builds on a
      machine with a different SDK installed will silently target something else.
- [ ] AAB path signs via `jarsigner` in `release.sh` (batch builds reject
      scripted keystore passwords). Confirm the release AAB is correctly signed
      before upload.
- [ ] Confirm code 7 exceeds the highest code already on Play.
