# Combat + camera audit (Combat 2.0 · camera stabilization)

Read-only audit for the three urgent problems, then the fixes made. Checked
against the source on 2026-09-03. The combat-variety problem (Problem 1) was
already addressed by the Combat 2.0 rebuild; this document covers its state
plus the two new problems — the one-shot 1-v-1 and the camera zoom.

## Problem 1 — repetitive combat: already rebuilt

The variety problem is the Combat 2.0 work now on `main` (see
`docs/COMBAT_2_AUDIT.md`). In place today, verified by `EmberCombatCheck`
(595 assertions), `AiCheck`, the encounter harness and the twelve-scenario
acceptance harness:

- `AttackDefinition` with categories (quick, heavy, guard-break, thrust,
  sweep, delayed, feint, gap-closer, retreat, counter, ranged, team) and
  startup/active/recovery/damage/posture/tracking/movement/knockback data.
- `EnemyAttackSelector` scoring distance, position, player state, tactical,
  personality, adaptation and a repetition penalty (`EnemyAttackHistory`,
  last five, near-forbids an immediate repeat). Runs on a cadence, not per
  frame, allocation-free.
- `EnemyCombatProfile` per archetype and per boss phase; combos;
  feint bands; morale (retreat / guard / berserk / call-allies / desperate);
  ally-death reactions; low-health behaviour.
- `PlayerMoveset` + `PlayerContextResolver`: the attack button resolves to
  the situation's move; the four dead combat states are used.
- Difficulty gains decision/feint/advanced/teamwork/adaptation scalars,
  Medium 1.0 on every axis.

The acceptance scenarios (Phase 32 Tests 1–12) run headless in
`EmberCombatScenarios`.

## Problem 2 — 1-v-1 ending in one hit: root cause

**Not a duplicate-hit bug.** The damage path is entirely imperative:

- No `OnTriggerEnter`/`OnCollisionEnter`/`OnTriggerStay` anywhere in the
  combat scripts (grep confirms zero). No hitbox colliders, no animation
  events, no `SendMessage`. Damage is applied by direct method calls:
  `StrikeArc` → one `TakeHit` per target per swing; the enemy → one
  `DamagePlayer` per resolved attack. The dash guards its single contact
  with `_dashHit`; the flurry delivers its follow-ups explicitly (two more,
  by design). So a single attack cannot register twice.
- `Health.Damage` clamps at zero and fires `OnDeath` once.
- HP and posture are separate scales in `TakeHit`: `Hp -= amount` and, on a
  different line, `Posture -= (crush ? amount*1.9 : amount) * postureMul`.
  A guard-break's power is a `postureMultiplier` on the player attack, never
  added to the HP number.
- Execution is double-gated: `CanExecute` (unaware, or guard-broken non-boss,
  or staggered mook ≤20% HP) **and** the contextual attack being an
  Assassination/StaggerPunish. Every duel opponent is MiniBoss rank or above,
  so none is execute-eligible from full HP; the only opening is a guard break,
  which is the intended finisher.

**The actual cause is scaling stack, not a bug in the hit.** A duel enemy is
floored to 190 HP, so the *player* never one-shots it. The player being
one-shot comes from the multiplier chain on the enemy's damage, and it only
reaches lethal in one hit under the hardest handicaps combined:

```
enemy hit = base damage
          × max(_, 12 duel floor)
          × duel modifier (ONE BREATH: none to damage, but halves *player* HP)
          × NG+ (1.35)
          × difficulty EnemyDamage (Lethal 2.0)
          + set-piece bonus (slam/spin +8)
```

`ONE BREATH` halves the player's HP by design ("Half your life"), and on
Lethal + New Game+ a heavy set-piece can then exceed it in one blow. On
**Medium, EVEN TERMS** — the baseline the brief protects — no single hit
comes close. Fix: this is verified, not changed. `EmberDuelIntegrity`
reconstructs the scaling for every opponent and asserts that at Medium the
biggest hit either side can land is under 60% (enemy→player) and 50%
(player→enemy) of the target's HP. The handicaps that stack past that are
opt-in difficulty, kept as authored. `CombatLog` instruments every hit
(Phase 2.1 fields) and flags any single hit removing >70% from a full-HP
target, so a real one-shot regression is caught the moment it happens.

## Problem 3 — camera zoom: root cause

`CameraRig` is the single camera controller; nothing else moves the
transform. But **FOV was written from six call sites** through `ImpactZoom`,
and the Combat 2.0 rebuild added a `RequestCameraImpact`-shaped call
(`def.cameraImpact`) on every contextual attack. With `_zoom = Max(_zoom,
strength)` and a per-frame decay, a fast, varied attack string kept `_zoom`
pinned high, so the FOV sat pulled-in during a combo and sprang back when it
ended — the "sudden zoom during normal combat." The dramatic reframes are
separate and intended: `PlayExecution` (a side-on finisher shot) and the
target-lock pitch swing.

The FOV maths were already additive (`baseFov − _zoom*4`) and strongest-wins,
so they did not accumulate — but they were unbounded below `base` only by the
×4 constant, and the many new callers made the pumping visible.

**Fixes (Phases 22–28):**

- **One API.** `RequestCameraImpact(strength, duration)` is now the only way
  to ask for a zoom; `ImpactZoom` is a thin wrapper. Attacks request; the rig
  decides.
- **No stacking.** The strongest live request wins; a weaker one may extend
  the tail slightly but never deepens it. Heavy + Light + Light reads as a
  Heavy.
- **Bounded.** `baseFov 50`, `minFov 44`, `maxFov 52`; the FOV is recomputed
  every frame as `base − impact` and hard-clamped to `[min,max]`, so it can
  never accumulate or drift.
- **Clean return.** Decay is on a duration timer, not a frame-rate-dependent
  subtraction, and zeroes cleanly when the timer ends, so the camera always
  settles back to `base`.
- **Calm normal combat.** Per-attack impacts are softened to ×0.7 of the
  attack's `cameraImpact`, so an ordinary light is a fraction of a degree —
  felt at most, never a zoom. Heavy, parry and execution keep their weight.
- **Shake** was already bounded: it ramps down over its life and zeroes
  `_shakeAmp` at the end, and only offsets the position for the frame, so it
  never permanently moves the camera. Hit-stop runs on `unscaledDeltaTime`
  for the FOV and shake, so a time dip cannot freeze or drift them.

Camera collision/clip and the midpoint 1-v-1 framing are handled by the
existing lock-focus path (swing behind the player→enemy axis, ease, yield to
drags); no second rig, no per-attack transform writes.

## Files

- Created: `Assets/Scripts/Debug/CombatLog.cs` (dev-only damage
  instrumentation), `Assets/Editor/EmberDuelIntegrity.cs` (1-v-1 scaling
  check).
- Modified: `Assets/Scripts/CameraRig.cs` (centralized, bounded impact),
  `Assets/Scripts/Player/CombatController.cs` (impacts routed and softened),
  `Assets/Scripts/Enemies/EnemyBrain.cs` (damage instrumentation hooks).
- The Combat 2.0 files (`Enemies/Combat/*`, `Player/Combat/*`,
  `Debug/CombatDebugOverlay.cs`, `Debug/CombatTestArena.cs`) are already on
  `main`.

## Verification

`EmberDuelIntegrity` (Medium 1-v-1 scaling), `EmberCombatCheck` (595
invariants incl. Medium 1.0 and posture-vs-damage separation),
`EmberCombatScenarios` (Tests 1–12), the AI check and the encounter harness.
Camera stability (Tests 15–19) is bounded by construction — FOV clamped to
`[44,52]`, additive from base, strongest-wins, timer decay — and confirmed on
device.
