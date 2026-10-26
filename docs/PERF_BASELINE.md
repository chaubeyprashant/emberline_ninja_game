# Emberline 3D — Performance Baseline

Phase 0 groundwork: a repeatable way to measure the game *before* optimising
anything. Nothing here changes gameplay; the overlay only observes.

Record a baseline with the current build, then re-run the same scenarios after
each optimisation and compare against the numbers you captured — not against
the targets in this document, which are goals rather than measurements.

---

## 1. Device under test

| Field | Value |
| --- | --- |
| Device | Samsung Galaxy A33 5G (`SM-A336E`, `RZCT929739K`) |
| Build | IL2CPP, ARM64, min SDK 26, landscape-left |
| Package | `com.ergebins.emberline3d` |
| Frame cap | `Application.targetFrameRate = 60`, vSync off (`GameManager.Awake`) |
| Graphics tier | Default tier 1 (`gfx_tier`, shadow distance 28m, hard shadows) |

The A33 is the reference device: a mid-range phone. If a scenario holds 60fps
here, it holds on most target hardware. Always re-test on the same physical
device — thermal state and vendor frame pacing differ enough between phones
that cross-device comparisons are meaningless.

---

## 2. The overlay

`Assets/Scripts/UI/PerfOverlay.cs` is added to both generated scenes by the
bootstrap. It is hidden by default and builds no UI until first shown, so a
hidden overlay costs one boolean check per frame.

**Toggle**

- Device: four-finger tap anywhere.
- Editor / desktop: `F3`.
- Preset before launch (survives restarts — it is a PlayerPrefs flag):
  tap it on once and it stays on until toggled off.

**Readout**

```
FPS 58.5   avg 17.1ms
p50 17.0  p95 17.9  p99 17.9 ms
worst 38.4ms   hitches 1 in 4s
mem 142MB   mono 11.8MB   gc/frame 2.4KB
draw 118  setpass 41  tris 96k
enemies 7/9   road segs 6   north 214m
```

| Line | Meaning |
| --- | --- |
| `FPS` / `avg` | Mean over a rolling ~4s window (240 frames), unscaled time |
| `p50/p95/p99` | Frame-time percentiles — **p95/p99 matter more than average**; they are what players feel |
| `worst` | Slowest single frame since the overlay was shown |
| `hitches` | Frames over 33.3ms (a visible stutter at 60fps) and the window length |
| `mem` / `mono` | Total allocated and managed heap — watch the *trend*, not the value |
| `gc/frame` | Bytes allocated per frame; sustained non-zero means per-frame garbage |
| `draw/setpass/tris` | Render counters — **development builds only**, omitted in release |
| `enemies` | Alive / total registered `EnemyBrain` (dying bodies still count) |
| `road segs` | Live `RoadNorth` corridor segments — the streamer's working set |

Frame times use unscaled time deliberately: hit-stop and the perfect-dodge
slow-mo change `Time.timeScale`, and scaled time would report those as
performance problems.

**Render counters.** `draw/setpass/tris` come from `ProfilerRecorder`, which the
engine only populates in development players. To capture them, build once with
`BuildOptions.Development` in `EmberlineBootstrap.BuildAndroid`. Frame timing,
memory and the gameplay counters work in release builds as-is.

---

## 2a. The automated tour (start here)

The open region replaced the arenas these scenarios were written for, and a
hand-driven run is never the same run twice. The game now measures itself:

```bash
./perf_run.sh --build
```

builds `Builds/emberline-dev.apk` (development, frame timing stats on for that
build only), installs it, launches it with `--es perfrun 1`, and waits. The
game (`Assets/Scripts/Debug/PerfRun.cs`, compiled into development builds and
the editor only) then holds the camera still for 6 s to settle and 20 s to
sample at each of these, with no input:

| Id | What | Why |
| --- | --- | --- |
| `menu` | Main menu idle | The floor; anything above it is gameplay |
| one per landmark (13) | The player stood just outside the landmark, facing it, camera behind | Draw cost of each place in the 800 m region, streamed in |
| `fight8` | Eight mixed fighters on the player at Yorune (player HP 100,000) | The audit's worst ordinary case |

Each scenario logs one `[PERFRUN]` JSON line: FPS, p50/p95/p99 frame time,
hitches over 33 ms, CPU main-thread and GPU p50/p95 (`FrameTimingManager`),
average draw calls, SetPass calls, batches and triangles, GC bytes per frame,
memory, enemies alive, streamed chunks, and `arrive_worst_ms` (the worst frame
while the streamer loaded the place, i.e. the teleport hitch). The script
writes them to `Logs/perf_<date>.jsonl` and prints a table.

- **The cap.** By default the run keeps the `PerfGovernor` in charge, so FPS is
  what a player gets (30 on tier 1). CPU and GPU time show the headroom under
  that cap. `--fps 60` lifts the cap for the run.
- **The save.** Touring discovers every landmark. The run backs up the explore
  save (`world_state`) first and restores it at the end, or on the next launch
  if the run was killed.
- **Signing.** The development APK is debug-signed. A phone holding a
  store-signed build refuses it; the script stops and leaves the uninstall
  (which deletes that phone's save) to the owner.
- **Before a phone.** `Emberline/Run Perf Tour (Editor)` (`EmberPerfRun.Run`)
  runs the same tour with short samples and writes `Logs/perf_editor.jsonl`.
  Its numbers mean nothing; it proves every scenario is reached.

S2–S9 below predate the region (they use the Rooftop arena and the Road North)
and stay as manual checks for combat-heavy moments the tour does not stage.

---

## 3. Running a measurement

```bash
adb install -r Builds/emberline3d.apk
```

Then, for each scenario: reach the described state, four-finger tap to show the
overlay (this also resets the rolling window), hold the state for **at least 30
seconds** without touching the controls beyond what the scenario requires, and
capture the result:

```bash
adb exec-out screencap -p > Logs/perf_S3.png
```

Rules that keep runs comparable:

- Let the phone sit at room temperature for ~5 minutes between long runs.
  Thermal throttling on the A33 is the single largest source of run-to-run
  variance, and a hot device can lose 20% of its framerate.
- Screen brightness and battery saver fixed; battery above 30%.
- Kill background apps before a soak test.
- Record three runs per scenario and keep the **median**, not the best.

---

## 4. Scenarios

Ordered cheapest to most demanding. S1–S4 cover the authored content; S5–S8 the
Road North streamer; S9–S10 are stress and stability.

### S1 — Menu idle (floor)
**Setup:** launch, stay on the main menu.
**Why:** establishes the cheapest possible frame — UI, ember particles and the
arena behind the menu. Anything above this is gameplay cost.
**Watch:** `gc/frame` should be near zero; the animated menu embers are the only
moving thing.

### S2 — Story L1, waves 1–2 (light combat)
**Setup:** Story → Level 1 (Rooftop), play through the first two waves.
**Why:** the common case — 4–6 melee enemies, no boss, no weather.
**Watch:** `enemies` 4–7; frame time should be flat. This is the scenario that
must never regress.

### S3 — Story L1, wave 4 (boss + adds + FX)
**Setup:** reach the Bandit Chief wave; capture during his ground-slam.
**Why:** boss intro cinematic, nova FX, camera shake, floating damage text and
five enemies at once.
**Watch:** the intro card and the slam are the two hitch candidates. Note
`worst` separately for the cinematic versus the fight.

### S4 — Marsh, Kagachi phase 3 (worst authored case)
**Setup:** Story → the Serpent's Toll; fight Kagachi below 30% HP.
**Why:** the heaviest authored moment in the game — clone split spawns two extra
skeletal rigs, `ArenaMarkers.RaiseWater` rescales every pool, and the marsh
scene already carries reeds, ghost lanterns and rain.
**Watch:** a one-off hitch at the split and at "THE WATER RISES" is expected;
record its magnitude. Sustained frame time after the transition matters more.

### S5 — Road North, first 100m (streamer warm-up)
**Setup:** March → walk north from the start without stopping to fight.
**Why:** the corridor builds its first segments while the arena is still loaded.
**Watch:** `road segs` should settle around 5–7 and **stop growing**. A number
that climbs forever means reclamation is broken — that is a leak, not a slow
frame.

### S6 — Road North, ~500m (streamer steady state)
**Setup:** march to roughly 500m, clearing packs as they come.
**Why:** proves the stream-ahead / reclaim-behind cycle is genuinely bounded and
that distance-scaled enemy stats have not inflated the fight past budget.
**Watch:** `mem` must be flat versus S5 at the same enemy count. Segment
construction is the per-segment hitch candidate — check `hitches`.

### S7 — Road North boss (150m milestone)
**Setup:** reach the first road boss with its escorts; mist barrier raised.
**Why:** combines streaming, a boss intro, the barrier and 3–4 escorts.
**Watch:** boss intro hitch; `enemies` 4–5 with a boss-grade rig.

### S8 — FX storm (peak load)
**Setup:** on the Road North past ~300m, build a 10+ combo to trigger Thread
Burst, then immediately fire Surge into a full pack.
**Why:** the densest particle and floating-text moment the game can produce —
nova, embers, sparks, damage numbers and hit-stop in the same frames.
**Watch:** `worst` and `gc/frame`. This is where pooling gaps surface.

### S9 — Kunai pool churn
**Setup:** throw kunai continuously into a pack for 30s.
**Why:** validates that the projectile pool recycles instead of allocating.
**Watch:** `gc/frame` must stay flat while throwing. A sawtooth `mono` means the
pool is missing.

### S10 — Soak: 15-minute march
**Setup:** march north continuously for 15 minutes; note `mem` and `road segs`
every 5 minutes.
**Why:** the only test that catches slow leaks — the streamer, the enemy
registry and the pools all accumulate across a long run.
**Watch:** `mem` should plateau. Steady growth is a leak; investigate before
touching framerate. Also compare FPS at minute 1 versus minute 15 to separate a
genuine leak from thermal throttling.

### S11 — Scene-transition churn
**Setup:** menu → level → die/retry → menu, five times.
**Why:** static registries (`EnemyBrain.Active`, `LanternPost.Active`,
`RoadNorth.Instance`) and pooled objects survive scene loads and are a classic
source of cross-scene leaks.
**Watch:** `mem` at the menu after five cycles versus the first S1 reading; they
should match closely.

---

## 5. Targets

Goals for the A33 at graphics tier 1 — aims, not current measurements:

| Metric | Target | Hard floor |
| --- | --- | --- |
| Average FPS, combat (S2/S6) | 60 | 45 |
| p95 frame time, combat | ≤ 20ms | ≤ 28ms |
| p99 frame time, combat | ≤ 28ms | ≤ 40ms |
| Hitches (>33ms) per 30s of steady combat | 0 | ≤ 2 |
| One-off hitch, boss intro / phase change | ≤ 100ms | ≤ 250ms |
| `gc/frame` sustained | ~0 | < 4KB |
| Memory growth over the S10 soak | flat | < 30MB |
| `road segs` at any distance | ≤ 8 | ≤ 12 |

Menu and idle scenarios should sit comfortably at the cap; they are a
correctness check on the overlay itself more than a performance goal.

---

## 6. Results log

### First device baseline — 2026-10-21, automated tour

Galaxy A33 (Mali-G68, Vulkan), development build 1.2.0+steps 1-6, tier 1 but the
phone's `fps_cap` set to **60**, Mobile Ads initialised (test device), daytime
explore. Raw: `Logs/perf_20261021_2323.jsonl`.

| Stop | FPS | p95 ms | CPU p95 | GPU p95 | SetPass | Tris k | Sys MB |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Yorune | 59 | 16.8 | 16.6 | 17.4 | 88 | 168 | 712 |
| Raiders' camp | 59 | 16.7 | 13.7 | 16.2 | 129 | 170 | 713 |
| Wayshrine | 59 | 16.7 | 13.2 | 18.7 | 138 | 222 | 707 |
| North pass | 60 | 16.7 | 13.6 | 16.5 | 74 | 140 | 672 |
| Pilgrim pass | 60 | 16.7 | 14.4 | 16.8 | 129 | 188 | 700 |
| Toll gap | 60 | 16.7 | 12.1 | 16.2 | 62 | 139 | 677 |
| Falls | 60 | 16.7 | 11.8 | 16.4 | 70 | 115 | 680 |
| Hill temple | 60 | 16.7 | 11.9 | 15.0 | 69 | 121 | 680 |
| Stronghold | 60 | 16.7 | 12.4 | 16.0 | 70 | 140 | 668 |
| The Hollow | 59 | 16.8 | 11.5 | 16.0 | 94 | 151 | 652 |
| Drowned marsh | 60 | 16.8 | 14.7 | 17.0 | 89 | 165 | 668 |
| Kawai hamlet | 60 | 16.7 | 14.5 | 17.1 | 84 | 173 | 650 |
| Shinobi hollow | 59 | 16.8 | 14.5 | 15.6 | 45 | 120 | 655 |
| **8-enemy fight** | **46** | **33.5** | **24.6** | **25.5** | 195 | 352 | 635 |

Reading: every landmark holds 60 fps, but with GPU p95 at 15-19 ms there is no
headroom left at 60 — the 30 fps tier-1 default is the right one. The fight
cannot hold 60 (CPU and GPU both ~25 ms) and would hold 30. Memory sits at
650-710 MB. The `Draws` counter reads 0 on this device (not exposed on Vulkan
development players); SetPass and triangles are valid. No ad-related hitches
showed: ads loaded during the run without frame spikes.



Copy this table per build. `Build` is the version code from
`EmberlineBootstrap`; keep the git SHA so a regression can be bisected.

| Date | Build / SHA | Scenario | avg ms | p95 | p99 | worst | hitches | mem | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | S1 menu idle | | | | | | | |
| | | S2 L1 waves 1–2 | | | | | | | |
| | | S3 L1 chief | | | | | | | |
| | | S4 Kagachi P3 | | | | | | | |
| | | S5 road 0–100m | | | | | | | |
| | | S6 road ~500m | | | | | | | |
| | | S7 road boss | | | | | | | |
| | | S8 FX storm | | | | | | | |
| | | S9 kunai churn | | | | | | | |
| | | S10 soak 15min | | | | | | | |
| | | S11 transitions | | | | | | | |

**No baseline numbers are recorded yet** — this document defines the method; the
first run on the A33 fills the table.
