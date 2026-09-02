# Phase 3 — Difficult and intelligent enemies

What changed, why, and how it was verified. The rule for this phase was that
difficulty comes from behaviour and composition, never from bigger numbers.

## The constraint that shaped everything

**No enemy stat was raised.** Health, damage, armour, poise, attack ranges,
windup times and cooldowns are byte-for-byte what they were. Medium difficulty
is still exactly 1.0 on every axis. The whole phase is behaviour plus a set of
per-enemy personality flags that were previously absent.

## New per-enemy knobs (`EnemyDef`)

All default to values that reproduce the old behaviour, so an enemy that is not
explicitly given a personality fights exactly as it did before.

| Field | Meaning |
|---|---|
| `dodgeChance` | chance to sidestep a read heavy |
| `counterChance` | chance to riposte out of a block |
| `punishesExposure` | attacks into the player's committed / whiffed / landing frames |
| `readsHeavies` | may answer a telegraphed heavy with a guard |
| `guardsWhenPostureLow` | raises guard and gives ground when its own posture is failing |
| `protectsRanged` | can be assigned as a bodyguard for an archer |
| `retreatBelowHp` | HP fraction under which it will disengage to recover |
| `panicRange` | distance at which a ranged enemy breaks and runs |
| `staggerDecay` | per-stagger shortening factor inside a 4-second window |

## Personalities

| Enemy | Fights like |
|---|---|
| Raider | simple and aggressive; no reads, no dodges. The tutorial enemy. |
| Assassin | fast, punishes hesitation, dodges often, disengages under 30% HP |
| Pike guard | controls distance, punishes a careless approach, guards the archer |
| Archer | repositions, panics at 3.2m, has a weak emergency jab when cornered |
| Axe raider | slow and huge; reads heavies, guard goes up when its posture drops |
| Samurai | defensive, blocks, ripostes at 0.6, punishes repetition |
| Rogue ninja | dodges at 0.45, punishes openings, withdraws when hurt |
| Elite | every mechanic at once: reads, blocks, dodges, ripostes, guards, protects |
| Bosses | never stun-locked; the slowest stagger decay in the roster |

## Behaviour added to `EnemyBrain`

- **Reading the player.** A telegraphed heavy within 3.4m that is aimed at this
  enemy is answered with a sidestep or a raised guard, once per heavy.
- **Punishing exposure.** Committed, whiffed and just-landed-from-a-dodge frames
  are openings. Enemies built to punish take them out of turn, still through the
  token pool, with the windup floored at 0.3s so it stays readable.
- **Ripostes.** A blocked swing may be answered. Two blocks in a row make it
  certain, so mashing into a guard is punished rather than merely refused.
- **Diminishing stagger.** Repeated flinches inside a 4-second window shorten
  geometrically toward a 0.12s floor, so an enemy cannot be juggled to death.
- **Retreat.** Hurt and out of posture, an enemy backs off, recovers and returns.
- **Guarding on low posture.** Rather than trading until its guard breaks.
- **Cover.** Waiting enemies hold position behind nearby cover, not in the open.
- **Bodyguards.** A new `Protect` squad role puts a protector between the player
  and a threatened archer, claimed *before* attack slots are handed out.

## Group behaviour (`SquadCoordinator`)

The ring now rotates through four jobs (guard close, circle, work round the back,
wait) so a pack reads as a unit rather than a queue, and the rotation advances so
nobody is stuck in one job. Ranged enemies without a firing lane reposition
instead of standing still.

**The attack-token pool is unchanged and still authoritative.** Every attack
path, including the new out-of-turn punish and the riposte, goes through it.

## Bugs found and fixed during verification

1. **The heavy attack was invisible to the AI.** `Cleave()` opens the deflect
   window on the next line, which overwrites the combat state with `Guard`. Any
   check of the state alone therefore missed the single most punishable action in
   the game. The exposure signal now keys off the pending cleave.
2. **Punishing starved ordinary attacks.** The punish branch took an attack token
   *before* choosing a move. When no move fit the range it kept the token's grant
   window without swinging, blocking the enemy's normal attacks every 0.3s. In one
   run an assassin failed to attack once across seven spawns. Token acquisition
   now happens last, after a move is chosen. The riposte path had the same defect.
3. **A failed read locked out the next one.** The read cooldown was set even when
   neither a dodge nor a block occurred. It is now set only on an actual reaction.
4. **Telemetry counted frames, not decisions.** A two-second bodyguard move
   registered as 227 "protect" events. Both latched counters now count once.
5. **A menu fade could throw.** `UiKit`'s enter animation kept a `CanvasGroup`
   reference across a screen rebuild and threw `MissingComponentException`,
   killing the coroutine and stranding the screen mid-fade. Now guarded.

## Verification

`Emberline/Check AI Model` (`EmberAiCheck`) — 25 static invariants: Medium is
exactly 1.0 on every axis, all 13 defs load, every damaging attack telegraphs at
least 0.3s, every stagger decay lands above the 0.12s floor, each personality is
present on the regenerated asset, and Medium caps simultaneous attackers at 3.

`Emberline/Run AI Encounters` (`EmberAiEncounters`) — a play-mode harness that
drives a scripted, slightly greedy player through ten compositions: 1 enemy
(raider / samurai / assassin), 2, 3, 5+, mixed, elite + support, archer nest, and
boss + adds. It verifies each opening it tries to create rather than assuming the
input landed, re-sends a wiped pack so every composition gets a comparable
sample, and fails a scenario if simultaneous attackers ever exceed the cap.

Latest full run: **10 / 10 passed**, no scenario exceeded its attacker cap.
