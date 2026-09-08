# Combat 3.0 — physical weapons

Audit of the combat system as it stands after Combat 2.0, and the plan to move it from
*animation + timer + instantaneous cone* to *animation + phase timing + swept weapon
tracing + spatial resolution*.

Read `COMBAT_2_AUDIT.md` first. That audit's Phases 2–12 are **done**: attacks are data,
selection is scored, enemies have profiles and memory, the player has contextual attacks.
This document is only about the layer Combat 2.0 never reached — the physical one.

---

## 1. Existing architecture

```
INPUT            EmberInput.ConsumeStrike/Cleave/Flicker  (buffered 0.15 s)
  │
  ▼
STATE            CombatController.State : CombatState      Free/Light/Heavy/Guard/Parry/
  │              CombatRules.CanEnter(from,to)             Dodge/Recover/Staggered/Execute
  ▼
SELECTION        PlayerContextResolver → AttackContext     17 contexts, priority-ordered
  │              PlayerMoveset.For(ctx) → PlayerAttackDefinition
  ▼
EXECUTION        CombatController.Perform(def, ...)
  │                light  → StrikeArc() SYNCHRONOUSLY, same frame as the button
  │                heavy  → _pendingCleave = windup; Update() → ResolveHeavy() → StrikeArc()
  ▼
HIT              StrikeArc(range, arcDeg, dmg): CollectWithin() = distance + angle test
  │              against EnemyBrain.Active, one instant, one frame
  ▼
DAMAGE           EnemyBrain.TakeHit(amount, from, crush, postureMul)
  │                defence layer → guard/blockChance ROLL → posture → HP
  ▼
REACTION         HitReaction enum chosen ad hoc; FxPools, Sfx3D, CameraRig.Shake, hit-stop
```

Enemy side:

```
DECISION   EnemyAttackSelector.Choose(def, profile, history, memory) → AttackDefinition
  ▼        (scored: distance, position, player state, tactics, personality, repetition)
GATE       AttackTokenPool.TryTake()  — max 2 (+1 at 4 alive, ± difficulty) attackers
  ▼
WINDUP     State.Windup, _t = windup; ring telegraph; tracking; feint at 50 %
  ▼
RESOLVE    ResolveAttack → ResolvePattern(kind): ONE instant distance+angle test
  ▼        DamagePlayer(dmg)
DEFENCE    DamagePlayer: LOS check → player.Invulnerable? → player.Deflecting? → damage
  ▼
RECOVERY   State.Recover, _t = def.RecoveryFor(kind); ×1.6 if a heavy missed
```

### What is already good — preserve untouched

| System | Why it stays |
|---|---|
| `CombatState` + `CombatRules` transition table | One authoritative answer to "can I do X now". Correct design. |
| `PlayerContextResolver` / `AttackContext` (17) | Genuinely rich contextual attack selection; better than most action games ship. |
| `EnemyAttackSelector` scoring + `EnemyAttackHistory` repetition penalty | Decision-based already. Not a timer. |
| `AttackTokenPool` | The engagement manager the brief asks for **already exists** and works (verified by the encounter harness). |
| `SquadCoordinator` roles, 4 Hz tick | Positional coordination, no crowding. |
| `EnemyCombatProfile` / `EnemyCombatMemory` | Per-archetype personality and player-behaviour adaptation. |
| Posture, guard break, `CanExecute`, diminishing stagger | Fundamentally sound. |
| Time-dip hit-stop, `CameraRig.Shake/ImpactZoom`, `FxPools`, `Sfx3D.ImpactKind` | Restrained and already event-driven. |
| Perception, noise, body-watch, unaware kills | Untouched by this work. |
| `AiTelemetry`, `CombatLog`, `CombatScenarioDriver` (12 scenarios), `AiEncounterDriver` | The verification loop. Extended, not replaced. |

### What is fragile, unrealistic or gamey

| # | Problem | Location | Severity |
|---|---|---|---|
| P1 | **A light attack deals damage on the frame the button is pressed.** `Perform()` calls `StrikeArc()` synchronously. The sword has not moved. There is no startup, no active window, no way to be interrupted mid-swing. | `CombatController.Perform` 771–800 | Critical |
| P2 | **Hit detection is an instantaneous cone**: `distance ≤ range && angle ≤ arc/2`, sampled once. A blade that visually passes through a body between frames never touches it; a body behind the player's shoulder inside the arc is hit by a sword pointing elsewhere. | `StrikeArc` / `CollectWithin` 918–1057 | Critical |
| P3 | **`PlayerAttackDefinition.startup` is authored but never read** (grep: zero call sites). Same for enemy `AttackDefinition.active`. The phase data exists; nothing consumes it. | both defs | Critical |
| P4 | **Enemy block is a coin flip at the moment of impact**: `Random.value < def.blockChance`. The brief names this explicitly. | `EnemyBrain.TakeHit` 1598 | High |
| P5 | **Player defence is a boolean sampled at resolve time**: `if (_playerCombat.Deflecting)`. Direction, the attacker's position, and where the guard is pointing are all irrelevant. Blocking a man behind you works perfectly. | `DamagePlayer` 1408 | High |
| P6 | **Enemy attacks resolve as one instant test too** — `if (dist < p.maxRange) DamagePlayer()`. The blade never occupies space, so it cannot be dodged spatially, only by the i-frame boolean. | `ResolvePattern` 1190–1292 | High |
| P7 | **No weapon clash.** Two blades meeting is not a representable event. | — | Medium |
| P8 | **No attack direction.** Every attack is a forward cone; `HitReaction` cannot vary by side because side is never computed. | — | Medium |
| P9 | Attack movement is uniformly `Facing × lunge` as a single impulse. Daggers, spear and axe differ only in the scalar. | `Perform` 764 | Medium |
| P10 | Whiff detection is "did the cone contain anyone", so a whiff is known instantly and `_whiffT` is set before the swing visually happens. | `StrikeArc` 973 | Low |

### Hard constraints found (these shape the design)

1. **There are no animation events, anywhere.** Clips are harvested from Mixamo/KayKit FBX
   by `EmberCharacterFactory`; the generated controller has one layer, `Move` and `AtkSpeed`
   parameters, and no events. Adding events means re-importing 76 clips per rig through
   `ModelImporter.clipAnimations` — expensive, fragile, and it breaks the moment a clip is
   time-scaled.
2. **`animator.applyRootMotion = false`.** Movement is entirely code-driven. Root motion is
   not available without re-authoring every character.
3. **`SkeletalRig.PlayOneShot(pose, duration)` time-scales the clip to a duration the code
   chooses** (`AtkSpeed = clipLength / duration`). This is the key enabler: **because the
   code already dictates how long the animation takes, code-side phase fractions are
   frame-accurate against the animation by construction.** Animation events would be
   strictly worse here — they would fight the time-scaling.
4. **Weapon wrapper prefabs already carry the trace skeleton.** Every sourced weapon has
   `PrimaryGrip`, `BladeRoot`, `TrailOrigin`, `HitPoint` children, parented under
   `GripAnchor_{side}` on the animated hand bone. The blade genuinely moves with the
   animation. Legacy KayKit props (`axe_2handed`, `sword_1handed`, `dagger`, …) have no
   markers and need bounds-derived fallback points.
5. **`AnimatorCullingMode.CullUpdateTransforms`** — an off-screen enemy does not update its
   bone transforms, so its blade is frozen. Tracing must detect a stale blade and fall back.
6. **Enemies are transform-driven with no colliders against the ground or each other.**
   There is no physics body to trace *against* on a character — targeting is by root
   transform. Traces must therefore be resolved against character capsules computed from
   the root transform and height, not against scene colliders.

---

## 2. Recommended architecture

Additive. No existing class is deleted; `StrikeArc` survives as the documented fallback.

```
        PlayerAttackDefinition / AttackDefinition          (existing, fields now READ)
                    │  startup · active · recovery
                    ▼
            ┌──────────────────┐
            │  AttackPhase     │  Startup → Active → FollowThrough → Recovery
            │  (new, shared)   │  fractions of animTime, absolute overrides
            └────────┬─────────┘
                     │ every frame while Active
                     ▼
            ┌──────────────────┐        ┌─────────────────────────┐
            │  WeaponTrace     │◀───────│  BladePoints            │
            │  (new component) │        │  BladeRoot/Mid/HitPoint │
            └────────┬─────────┘        │  markers, else bounds   │
                     │                  └─────────────────────────┘
                     │  swept capsule prev→now, per segment
                     ▼
            ┌──────────────────┐
            │  CombatHitResolver│  dedup per swing · direction · defence · clash
            └────────┬─────────┘
                     ▼
        TakeHit / DamagePlayer  (existing entry points, unchanged signatures)
```

New files (one class per file, matching the project's conventions):

```
Assets/Scripts/Combat/AttackPhase.cs        phase enum + timing struct, shared
Assets/Scripts/Combat/BladePoints.cs        resolves trace points on a weapon prop
Assets/Scripts/Combat/WeaponTrace.cs        swept capsule tracing + per-swing dedup
Assets/Scripts/Combat/CombatHitResolver.cs  direction, defence, clash decision
Assets/Scripts/Combat/AttackDirection.cs    Left/Right/High/Low/Thrust/Back
```

### Why swept capsules and not blade colliders

| Approach | Verdict |
|---|---|
| Permanent `BoxCollider` + `Rigidbody` on each blade, `OnTriggerEnter` | **Rejected.** Characters have no colliders to hit (transform-driven, `Ground` is a height function). Would need a rigidbody per weapon per character, physics layers, and it tunnels at `AtkSpeed` > 1 where a 0.28 s clip plays in 0.18 s. |
| Instantaneous cone (today) | The thing being replaced. |
| **Swept capsule between blade samples, resolved analytically against character capsules** | **Chosen.** No physics bodies, no allocation, exact against the animated blade, immune to tunnelling because it sweeps `prev → now`, and it degrades gracefully to the cone when a rig is culled. |

Cost per attacker per active frame: 2 segments × N candidate enemies, each a
segment-to-segment closest-distance test (~20 flops). With 8 enemies that is ~320 flops per
frame during an active window that lasts ~0.1 s. Negligible against the 4 Hz squad tick.

---

## 3. Migration plan

| Stage | Change | Risk | Backward compatibility |
|---|---|---|---|
| 1 | `AttackPhase` timing; player light/heavy run through phases | Medium — changes when damage lands | `phaseTiming = null` → legacy instant behaviour |
| 2 | `BladePoints` + `WeaponTrace`; player attacks trace | Medium | Trace finds nothing → `StrikeArc` fallback, identical result |
| 3 | Direction from blade motion; reactions read it | Low | Direction defaults to `Forward` |
| 4 | Enemy Startup/Active/Recovery + tracing | Medium | Per-attack opt-in via `active > 0` |
| 5 | Spatial defence: guard arc vs. attack direction; enemy block earned not rolled | High — changes difficulty | Roll retained as the *decision* to guard |
| 6 | Weapon clash | Low | New event; nothing depends on it |

Every stage compiles, runs `SetupScenes`, and runs the 12-scenario harness before the next.

## 4. Verification

`EmberCombatScenarios.Run` (12 scenarios, ~7 min) is the regression gate; `CombatLog`
reports max hit fractions and one-shot flags. `EmberAiCheck` asserts telegraph floors.
`EmberMissionPlaythrough` proves missions still complete. Device build last.
