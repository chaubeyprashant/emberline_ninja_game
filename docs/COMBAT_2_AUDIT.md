# Combat 2.0 — audit of the existing combat

Phase 1 of the Combat 2.0 brief. Read-only: nothing in this document changed code.
Every claim below was checked against the source on 2026-09-02; file references are
to the files as they stand.

Files inspected: `Player/CombatController.cs` (1060 lines), `Player/CombatState.cs`,
`Player/PlayerLocomotion.cs`, `Player/Kunai.cs`, `Player/SmokeBomb.cs`,
`Enemies/EnemyBrain.cs` (1822), `Enemies/EnemyDef.cs`, `Enemies/AttackTokenPool.cs`,
`Enemies/SquadCoordinator.cs`, `Enemies/Perception.cs`, `Enemies/Projectile.cs`,
`Enemies/EnemyBomb.cs`, `Enemies/AiTelemetry.cs`, `Core/WeaponDef.cs`,
`Core/Difficulty.cs`, `Core/SkeletalRig.cs`, `Core/CharacterRig.cs`, `Core/SenGates.cs`,
`Core/Sfx3D.cs`, `CameraRig.cs`, `UI/Fx3D.cs`, the weapon and enemy definitions
authored in `Editor/EmberlineBootstrap.cs`, and the pose→clip tables in
`Editor/EmberCharacterFactory.cs`.

---

## 1. Current player attacks

Renzo has **four offensive inputs** and everything else is a modifier on them.

| Input | What happens | Where |
|---|---|---|
| **Strike** (light) | Enters `CombatState.Light` for `strikeAnimTime` (0.17–0.36 s by weapon). Advances a chain stage if pressed inside `chainWindow` (0.62–0.95 s). Stage damage from `WeaponDef.strikeDamage[]`; the **last** stage is a crush + launch. Soft-locks facing to the best enemy in the aim cone, lunges forward, resolves an arc (`strikeRange`, `strikeArcDeg`) on the same frame. Crossbow: fires a bolt instead. | `CombatController.Strike`, `StrikeArc` |
| **Cleave** (heavy) | Enters `Heavy` for `cleaveWindup + 0.35`. The windup **is also the guard**: pressing opens a `deflectWindow` (0.4 s), holding extends it to `deflectMaxHold` (1.1 s). On expiry resolves per `CleaveStyle`: Slash arc / Spin 360° / Ground 360° + nova / FanShot (three bolts). | `Cleave`, `ResolveCleave`, `UpdateGuard` |
| **Kunai / thrown** | A flat projectile (`kunaiDamage` 9); Smoke Bomb weapon throws a bomb instead. | `ThrowKunai`, `ThrowSpecial` |
| **Surge** | Spends 30 Sen: crush AoE in `surgeRadius`. | `Surge` |

Defence and movement:

| Mechanic | Current behaviour |
|---|---|
| **Flicker** (dodge) | Locomotion-level: `flickerDuration` 0.28 s of i-frames, cooldown. A kunai in flight turns it into **Kunai Warp** (teleport to the blade). Not a `CombatState`. |
| **Perfect dodge** | Set from the enemy side (`DamagePlayer` sees `Invulnerable`): `_counterT = 0.5` → the next hit does ×2, plus 0.28 s slow-mo. Storm Tanto adds a posture hit to every winding-up enemy within 3.5 m. |
| **Guard / block** | Only exists inside the heavy windup (see Cleave). |
| **Perfect parry** | First `perfectParryWindow` (0.16 s) of a guard: 14 posture (crush) to the attacker, 0.75 s counter window, slow-mo, FOV punch. A late block: 4 posture, 0.4 s counter. Enters `Parry` for 0.2 s. |
| **Execute** | `StrikeArc` finishes any target whose `CanExecute` is true: unaware, guard-broken (non-boss), or staggered ≤ 20 % HP (mook). Silent kill when the target is unaware. |
| **Back attack** | Not a player move: a damage multiplier on the enemy (`backstabMultiplier`, archers ×2). |
| **Counter** | A number: `CounterActive` doubles the next Strike or Kunai. Nothing visible happens. |

**State machine.** `CombatState` = Free / Light / Heavy / Guard / Parry / Dodge / Recover /
Staggered / Execute with a transition table in `CombatRules`. Checked by grep: the
controller never enters **Dodge, Recover, Staggered or Execute** — those four states are
defined and unreachable. Dodge lives in locomotion, Execute is a method that stays in
`Light`, and the player has **no hit reaction at all**: taking damage resets `Combo` and
nothing else (`OnPlayerHit`).

**Chain.** Stage index cycles `1 → … → ChainLength → 1`. There is one branch in the entire
system: the final stage crushes. `Light → Heavy` is allowed by the rules but does nothing
different from a cold heavy. Three strike poses exist (`Strike1/2/3`) and longer chains
(daggers, 5) cycle them.

**Targeting.** `SoftLockFacing` → locked target if within `softLockRange × 1.4`, else
`BestInCone` scored by angle to the aim direction. Sensible; keep.

**Feel.** Hit-stop via a single time-dip system (`RequestDip`: deepest scale wins, longest
duration extends), camera `Shake` + `ImpactZoom`, `FxPools` sparks/puff/slash/nova/embers,
`FloatingText` for BLOCK / PERFECT PARRY / EXECUTE / damage. Restrained already; the dip
system is good and should stay.

## 2. Current enemy attacks

The vocabulary is `AttackKind` (11 entries) and a kit is `EnemyDef.attacks[]` of
`AttackPattern { kind, minRange, maxRange, weight, damageMultiplier, windupOverride,
cooldown, redTelegraph, telegraphScale }`.

| Enemy | Kit today | Notes |
|---|---|---|
| Raider (bandit) | **Flurry** only | one attack, forever |
| Assassin | Flurry, DashStrike | |
| Pike Guard | **Thrust** only | one attack |
| Archer / Weaver | ChargedShot, QuickShot, Slash (panic jab) | |
| Axe Raider | Slash, HeavySlam | |
| Samurai | Parry (stance), Slash, DashStrike | |
| Rogue Ninja | DashStrike, Slash, ThrowBomb | |
| Elite Warrior | Slash, SpinCleave, HeavySlam, DashStrike | |
| Goro (Chief) | HeavySlam, SpinCleave, DashStrike | plus a hardcoded enraged spin and slam |
| Kagachi | Slash, PoisonSpit, DashStrike | plus hardcoded spit, clones, water |
| Jin | **no def-driven kit** — legacy weapon path (generic Slash) | plus a hardcoded counter-dash |
| Shade | **Flurry** only | |
| Powder Carrier | **ThrowBomb** only | |

How an attack resolves (`ResolvePattern` / the per-kind resolvers):

- Every melee kind is **windup → single instant check → recover**. `Slash`: `if (dist <
  maxRange + 0.6) DamagePlayer`. `Flurry`: one hit now, two more delivered from `Recover`
  at 0.17 s spacing. `Thrust`: a 22° cone along facing and a 1.1 m step. `HeavySlam`:
  radius 3.4 + slow zone. `SpinCleave`: radius 5. `DashStrike`: 0.45 s dash at 13–15 m/s
  with contact damage.
- There are **no active frames, no tracking, no hitbox shape** beyond those radii and the
  one cone. Nothing moves the attacker except Thrust's step and the dash.
- **Recovery is one number**: `_t = 0.75` for almost everything (0.8 flurry, 0.9 thrust,
  0.85 shot, 1.0 spin/bomb). A missed heavy slam recovers as fast as a jab.
- The **telegraph is one pose** (`RigPose.Windup` = `Spellcast_Raise`, or `1H_Ranged_Aiming`
  for archers) plus the ring. Every category looks the same until it lands.

## 3. Current AI decisions

`EnemyBrain.Update` → `State.Chase` runs this order every frame (`EnemyBrain.cs`
≈ 470–690):

1. `ReadPlayer` — if the player's heavy is winding up within 3.4 m and aimed here: sidestep
   (`dodgeChance`) or raise a reactive block (`readsHeavies && blockChance`). One reaction
   per 1.1 s.
2. Retreat — `retreatBelowHp` and posture < 50 %: back off for 1.6 s.
3. Guard-when-low — `guardsWhenPostureLow` and posture < 34 %: guard up, give ground.
4. Protect — `protectsRanged` + squad role `Protect`: stand between the player and the archer.
5. **Squad role holds** — Wait (hold 4.5 m, use cover), Guard (stand close, guard up, never
   swing), Circle (strafe), Reposition (work to the player's back). These enemies do not
   attack at all while in those roles.
6. Out-of-turn **punish** — `punishesExposure` and the player is committed / whiffed / just
   dodged: `ChooseAttack(dist)` with a 0.7× windup (floor 0.3 s), through the token pool.
7. **Normal attack** — `_attackCd <= 0` → `def.ChooseAttack(dist)` → token → `Windup`.
8. Legacy fallback for enemies without a def (Jin): `inRange` per weapon, generic windup.

`EnemyDef.ChooseAttack(distance)` is a **weighted random draw among the patterns whose
range band contains the distance**. Its inputs are the distance and the weights. It does
not know the player's state, position, facing, the enemy's own health or posture, what it
did last, what the player did last, or what allies are doing.

Reactions on being hit (`TakeHit`): a guard up or a `blockChance` roll turns a non-crush
hit aside; two blocks in a row (or `counterChance`) trigger a riposte (`ChooseAttack`
again, 0.32 s windup). Then weakness multipliers, armour, posture damage (crush ×1.9),
guard break at zero posture, then poise roll → Flinch or Knockback with diminishing
stagger. Jin has a hardcoded 35 % counter-dash on any light hit.

Movement: `StyleDir` — eight `MovementStyle`s (Direct, Flank, Spacing, Kite, Ambush,
Reach, Erratic, Flee) plus a legacy per-kind `ChaseDir` for Jin.

Recover: sidestep after a dodge read; Shade and Jin back away after striking; flurry
follow-ups.

## 4. Current enemy personalities

A personality is the **nine knobs** on `EnemyDef` (added in Phase 3) plus a movement style
and a preferred range:

`dodgeChance, counterChance, punishesExposure, readsHeavies, guardsWhenPostureLow,
protectsRanged, retreatBelowHp, panicRange, staggerDecay`.

They produce genuinely different behaviour (a samurai blocks and ripostes, an assassin
punishes, a pike guard bodyguards), but they are **all defensive or positional**. Nothing
describes how an enemy *attacks*: no aggression, bravery, attack frequency, combo length,
feint or guard-break tendency, teamwork, or any reaction to low health beyond a single
retreat threshold, and nothing at all about ally deaths.

## 5. Current attack selection logic

```
every frame, in Chase, when _attackCd <= 0 and the squad role allows it:
    pattern = weighted-random among patterns with minRange <= dist <= maxRange
    if pattern != null and token available:
        windup = pattern.windupOverride or def.windupTime
        after windup: single distance/cone check → damage → Recover (0.75 s)
        _attackCd = pattern.cooldown
```

That is the whole selection model. The answer to "why did it attack?" is always "the
timer expired and you were in range". The answer to "why *this* attack?" is "the dice".
With one-pattern kits the dice have one face.

## 6. Current problems — why combat feels repetitive

Ranked by how much they contribute to the complaint.

1. **Selection has no purpose.** Range + weight random, no player-state, position, history
   or tactical input. Four of thirteen enemies have exactly one attack; the rest have two
   to four with no relationship between them.
2. **Attacks are timers, not motions.** No startup/active/recovery split, no tracking, no
   hitbox shape (radius or one cone), one recovery length for everything. A heavy that
   misses is not more punishable than a jab that misses, so there is nothing to bait.
3. **One telegraph pose for every category.** `Spellcast_Raise` + a ring. Quick, heavy,
   thrust, sweep and guard-break are indistinguishable until they resolve, which forces the
   player into one answer (dodge on ring) and makes reading impossible in principle.
4. **No repetition protection.** Nothing remembers the last attack. The riposte and punish
   paths call the same random draw.
5. **The player's chain is deterministic.** 1-2-3 with the last hit special; heavy is one
   move; contextual "counters" are invisible ×2 multipliers; no running, air, dodge,
   back-attack or guard-break-punish moves exist. Four `CombatState`s are unreachable and the
   player has no hit reaction, so being hit costs nothing but a combo counter.
6. **Weapons are mostly numbers.** Range, speed, damage, chain length and a cleave style.
   Daggers are a longer chain of the same three swings. The Hook's pull and poison and the
   Tanto's dodge-parry are the only behavioural differences.
7. **Reactions are thin.** A perfect parry costs the enemy 14 posture and a generic Flinch;
   there is no recoil, no opening state, no distinct animation. Aware enemies do not react
   to ally deaths at all (`BodyWatch` only feeds unaware detection). Near death changes only
   the retreat knob. Back hits change only damage.
8. **No adaptation.** `_blocksInRow` and `PlayerExposed` are the only reads of player
   behaviour. Nothing tracks blocking, dodging early, retreating or spamming over time.
9. **Bosses are hardcoded branches.** `Enraged`, `Phase`, Goro's spin/slam, Kagachi's spit
   and clones and water, Jin's counter-dash all live as `kind ==` checks in the brain, beside
   the data-driven path. Pale Shade is the Shade def scaled ×4.5 HP with no behaviour of its
   own. Phases change speed and HP thresholds, not decisions.
10. **Difficulty only scales HP, damage and attacker count.** Easy and Lethal make the same
    decisions at the same speed with the same kits.
11. **Squad roles are position jobs only.** They stop crowding well (token cap holds; verified
    by the encounter harness) but "Pressure", "Flanker" and "Reserve" do not exist as
    intents that feed attack choice, and a Guard-role enemy never swings even when the player
    is exposed in front of it.

Performance is not a cause. The code is already careful: a static registry instead of
`FindObjectsByType`, cached component refs, no LINQ, pooled effects, a 4 Hz squad tick.
Two small allocations were found: the squad sort lambda captures `origin` (a closure per
tick, `SquadCoordinator.cs:96`) and `FloatingText.Spawn` per block. Neither is a feel
problem; both are noted for Phase 12.

## 7. Duplicate systems

| Duplicate | Where | Resolution |
|---|---|---|
| Def-driven `ChooseAttack`/`ResolvePattern` **and** the legacy weapon/kind `ResolveAttack` fallback | `EnemyBrain.cs` 1125–1192 | Give Jin a def; delete the fallback. |
| `StyleDir` (movement styles) **and** legacy per-kind `ChaseDir` | 1617–1683 | Same: delete the per-kind switch once every enemy has a def. |
| `inRange` per weapon/kind **and** pattern range bands | Chase branch | Ranges belong to attacks only. |
| Enemy `AttackKind.Parry` (a stance *attack*) **and** `blockChance`/`readsHeavies` guard | `ResolvePattern`, `TakeHit`, `ReadPlayer` | One guard model, driven by the profile. |
| Player `CounterActive` (×2 multiplier) **and** `OnDeflect` riposte (`Parry` state) | `CombatController` | One "counter opening" that selects a distinct attack. |
| Boss behaviour in `kind ==` branches **and** the def-driven path | `CheckPhaseTransitions`, Chase, `TakeHit` | Boss profiles with phase profiles. |
| `HitReaction` enum shared, but reactions decided ad hoc in `TakeHit` | | One reaction resolver keyed by attack definition + enemy profile. |

## 8. Systems that should be preserved (as they are)

- `CombatState` + `CombatRules` — the transition table is right; it needs its four dead
  states used, not replaced.
- Attack input buffering (`AttackBuffer`) and the strike/cleave priority.
- The **attack-token pool** (base 2, +1 at four alive, + difficulty) — the brief requires it.
- `SquadCoordinator` as the place roles are assigned, its 4 Hz tick and its nearest-first
  ordering.
- Perception: `NoiseSystem`, `BodyWatch`, `Visibility`, alarm states, unaware/stealth kills.
- Posture, guard break, `CanExecute`, diminishing stagger.
- The time-dip hit-stop system, `CameraRig.Shake/ImpactZoom/PlayExecution`, `FxPools`,
  `Sfx3D.ImpactKind` banks, `FloatingText` (restrained already).
- `WeaponDef` + `Loadout` + `WeaponUpgrades` + cosmetics; `CleaveStyle` as the heavy verb.
- `Difficulty` — the table and `ApplyTo` stay; Medium remains 1.0 on every axis.
- `EnemyDef` as the single data home per enemy (Combat 2.0 adds to it; no second roster).
- `AiTelemetry`, `EmberAiCheck`, `EmberAiEncounters` / `AiEncounterDriver` — the
  verification loop Combat 2.0 will be measured with.
- The telegraph ring and the red/white distinction (red for what genuinely hurts).

## 9. Systems that should be refactored

| System | Change |
|---|---|
| `AttackPattern` | Becomes `AttackDefinition`: id, category, startup/active/recovery, damage, posture damage, range band and preferred range, tracking + strength, movement, knockback, stagger/guard-break power, parryable/dodgeable/interruptible, feint/chain rules, cooldown, target-state requirement, AI/player weights, audio cue, camera impact, hit reaction. `EnemyDef.attacks[]` keeps its slot; assets under `Resources/Attacks`. |
| `EnemyDef.ChooseAttack` | Replaced by `EnemyAttackSelector` scoring every candidate: distance, position (front/side/back), player state, tactical, personality, repetition penalty, cooldown. Returns the best with a small stochastic margin so it stays unpredictable *which*, never *why*. |
| Enemy `State` machine | `Windup` → `Startup / Active / Recovery` with the definition's timings; add `Feint` (cancel at the defined point into a follow-up) and `Delayed` (a held startup). Recovery length comes from the attack, so a missed heavy is punishable. |
| Telegraphs | One pose per category (see §10's clip table) plus the ring; audio cue per category. |
| `EnemyDef` knobs | Move into `EnemyCombatProfile` (a ScriptableObject referenced by the def) with the full personality set the brief lists; the nine existing knobs migrate, values preserved. |
| Reactions | `EnemyReactions` resolver keyed by hit reaction + profile; adds parry recoil, missed-heavy vulnerability, back-hit reaction, ally-death morale, near-death behaviour. |
| Adaptation | `EnemyCombatMemory` (rolling player-behaviour counters, decayed) read by the selector for adaptive profiles (Elite, Jin, Kagachi) as probability shifts with cooldowns — never as input reading. |
| Bosses | Profiles per boss with a profile per phase; delete the `kind ==` branches once the profiles reproduce them (Goro's spin, Kagachi's spit/clones/water become attacks and phase events). |
| Player | `PlayerAttackDefinition` set per weapon; a `ContextResolver` that maps the attack button to the contextual move (parry counter, dodge counter, guard-break punish, back attack, stagger punish, running attack, air attack, gap closer); the four dead states put to use (Recover after heavies and whiffs, Staggered on guard break, Execute for finishers, Dodge for the flicker). |
| Difficulty | Add decision-cadence, feint-rate, advanced-attack and teamwork scalars to `Difficulty.Def`, Medium = 1.0, applied in the selector and the coordinator. HP/damage axes untouched. |
| `SquadCoordinator` | Roles become Attacker / Pressure / Support / Flanker / Reserve with role-aware attack gating (Pressure may punish; Reserve may not), fed to the selector's tactical score. Token pool unchanged. |

## 10. Proposed Combat 2.0 architecture

```
                  ┌──────────────────────────────┐
                  │  AttackDefinition (SO, data) │  Resources/Attacks/*.asset
                  └──────────────┬───────────────┘
   EnemyDef ──references──▶ EnemyCombatProfile (SO)   ◀── boss phase profiles
        │                        │
        ▼                        ▼
   EnemyBrain ──asks──▶ EnemyAttackSelector ──reads──▶ EnemyCombatDecision (scores)
        │                        │                      ▲          ▲
        │                        │            EnemyAttackHistory   EnemyCombatMemory
        │                        │            (last 5, penalties)  (player behaviour)
        │                        ▼
        │              chosen AttackDefinition
        ▼
   Startup → Active → Recovery   (Feint / Delayed variants)
        │
        ▼
   CombatEvents (hit, parried, dodged, ally died, guard broke, phase changed)
        │                                 │
        ▼                                 ▼
   EnemyReactions (recoil, morale)   SquadCoordinator (roles, token pool — unchanged core)
                                          │
                                          ▼
                                     Difficulty scalars (cadence, feints, teamwork)

   Player side:  PlayerAttackDefinition (per weapon) ──▶ ContextResolver ──▶ CombatController
                 PlayerCombatMemory feeds EnemyCombatMemory (what Renzo has been doing)

   Tools:  CombatDebugOverlay (editor/dev only)   CombatTestArena (scene tool + bot)
```

Files (existing namespaces, one class per file, MonoBehaviour/ScriptableObject file names
matching class names):

```
Assets/Scripts/Enemies/Combat/AttackDefinition.cs        (SO)
Assets/Scripts/Enemies/Combat/AttackCategory.cs
Assets/Scripts/Enemies/Combat/EnemyCombatProfile.cs      (SO)
Assets/Scripts/Enemies/Combat/EnemyCombatDecision.cs
Assets/Scripts/Enemies/Combat/EnemyAttackSelector.cs
Assets/Scripts/Enemies/Combat/EnemyAttackHistory.cs
Assets/Scripts/Enemies/Combat/EnemyCombatMemory.cs
Assets/Scripts/Enemies/Combat/EnemyReactions.cs
Assets/Scripts/Enemies/Combat/CombatEvents.cs
Assets/Scripts/Player/Combat/PlayerAttackDefinition.cs   (SO)
Assets/Scripts/Player/Combat/PlayerContextResolver.cs
Assets/Scripts/Player/Combat/PlayerCombatMemory.cs
Assets/Scripts/Debug/CombatDebugOverlay.cs               (#if UNITY_EDITOR || DEVELOPMENT_BUILD)
Assets/Scripts/Debug/CombatTestArena.cs                  (editor/dev)
Assets/Editor/EmberCombatAssets.cs                       (authors the SOs; verifies they load)
Assets/Editor/EmberCombatCheck.cs                        (invariants: Medium 1.0, telegraph floors, no dead states, repetition rules)
```

**Animation inventory (no new assets required).** The KayKit skeleton FBX carries 76 clips;
the factory maps 12 `RigPose`s. Clips available and unused that give each category a
readable pose: `Block`, `Blocking`, `Block_Hit`, `Block_Attack` (guard, guard-break punish,
parry recoil), `Dodge_Left` / `Dodge_Right` / `Dodge_Backward` (sidestep, retreat attack),
`1H_/2H_/Dualwield_Melee_Attack_Stab` (thrust), `2H_Melee_Attack_Spin` vs `Spinning`
(sweep vs heavy), `Throw` (kunai, powder), `Unarmed_Melee_Attack_Kick` (guard break /
shove), `Running_Strafe_Left/Right`, `Walking_Backwards` (spacing), `Jump_*` (air attack),
`Spellcast_Long` (a held, delayed startup that reads differently from `Spellcast_Raise`).
`RigPose` grows by roughly ten entries; the factory's clip table is data.

**Decision cadence.** Selection runs on an interval per enemy (0.15–0.35 s, difficulty
scaled), not per frame, and only in `Chase`; scores are computed without allocation from
cached references and squared distances.

**Non-cheating contract.** The selector reads only: distance, relative position and
facing, the player's *visible* `CombatState` and its remaining time, whether the player is
invulnerable *right now* (already visible as a dodge), squad roles, allies alive, its own
HP/posture, its history, and the decayed behaviour memory. It never reads `EmberInput`,
never sees an attack before `CombatController` has entered its state, and every attack
keeps a telegraph ≥ 0.3 s (the existing `EmberAiCheck` floor) and the existing
line-of-fire check in `DamagePlayer`.

## Implementation order (mapped to the brief)

| Phase | Deliverable | Verification |
|---|---|---|
| 2 | `AttackDefinition`, `AttackCategory`; migrate every `AttackPattern` 1:1 (behaviour identical) | SetupScenes 0 errors; `EmberAiCheck` unchanged; encounter harness unchanged |
| 3 | Player: attack definitions per weapon, context resolver, dead states used, branching chain, hit reaction | `EmberAiCheck` + a new `EmberCombatCheck` (rules, no unreachable states) |
| 4–7 | `EnemyCombatProfile`, selector + decision scores, history/repetition, personalities for 8 archetypes + powder carrier | Test 1–6, 10, 11 in `CombatTestArena` via the bot; telemetry: distinct attacks per fight ≥ 3, max same-attack run ≤ 2 |
| 8 | Roles → Attacker/Pressure/Support/Flanker/Reserve; token pool untouched | Test 7–9; `maxSimul` ≤ cap (existing assertion) |
| 9 | `EnemyCombatMemory` + adaptive profiles (Elite, Jin, Kagachi) | Test 2–4 with a scripted player |
| 10 | Boss profiles + phase profiles; delete `kind ==` branches | Test 12; campaign bot missions 5, 30, 40, 70, 99 |
| 11 | Category poses, audio cues, camera responses | snapshot + device |
| 12 | Cadence intervals, allocation pass, profiler on the A33 | logcat, frame time |
| 13 | Full campaign bot, device session | 100/100, no errors |
