# Difficulty 2.0 audit

Phase 1. Read-only findings, then the extension plan. Checked on 2026-09-03.

## 1. Where difficulty is stored

`Assets/Scripts/Core/Difficulty.cs` — `enum DifficultyLevel { Easy, Medium,
Hard, Lethal }`, a `readonly struct Def` per level in `Difficulty.All[]`, and
`Difficulty.Current` persisted in PlayerPrefs (`gfx`-style key `difficulty`).
`Difficulty.Now` is the active `Def`. One authority; no duplicate manager.

## 2. Where difficulty is selected

Settings, through `EmberHud` (the settings sheet) → `Difficulty.Current`. The
menu shows `Difficulty.Now.Name`/`.Blurb`. Applied at spawn and read live by the
combat systems, so a change takes effect on the next spawn/decision — no restart.

## 3. How enemy stats currently change

`Difficulty.ApplyTo(brain)` multiplies `maxHp` and `damage` at spawn.
`Def` also carries `Heal`, `PlayerHp`, `ExtraAttackers` (token-pool cap) and
`Score`. Easy 0.60/0.80 dmg/hp, Medium 1.0 everything, Hard 1.40/1.25, Lethal
2.00/1.45. **Medium is exactly 1.0 on every stat axis** — protected.

## 4. How enemy AI currently changes (already behavioural)

The Combat 2.0 rebuild added five *behaviour* scalars on `Def`, computed per
level, Medium 1.0:

| Scalar | Easy | Medium | Hard | Lethal | Wired into |
|---|---|---|---|---|---|
| `FeintScale` | 0 | 1 | 1.2 | 1.5 | selector gates feints; Easy never feints |
| `AdvancedScale` | 0.5 | 1 | 1.2 | 1.5 | gates delayed attacks; scales player-state score |
| `TeamworkScale` | 0.6 | 1 | 1.3 | 1.6 | scales the tactical score |
| `AdaptationScale` | 0 | 1 | 1.2 | 1.6 | scales how much the enemy reads the player; Easy never adapts |
| `DecisionScale` | 1.6 | 1 | 0.9 | 0.7 | `DecisionInterval` — Easy thinks slower |

So difficulty is **already** more than stats: Easy makes no feints, no delayed
attacks, no adaptation and decides slowly; Lethal feints, delays, adapts and
decides fast. This is the base to extend, not replace (`EnemyAttackSelector`
reads all five).

## 5. Which systems currently ignore difficulty

The gaps this overhaul fills:

- **Defense.** `ReadPlayer` (reactive dodge/block) and `TakeHit` (block/counter)
  use only the enemy's own `dodgeChance`/`blockChance`/`counterChance` — no
  difficulty multiplier. Easy and Lethal defend identically.
- **Recovery punishment.** `punishesExposure` (the out-of-turn whiff/dodge
  punish) fires whenever an opening exists, on every difficulty. Easy should
  miss most openings; Lethal should take them.
- **Spacing.** `StyleDir` positions the same on all four — no spacing-accuracy axis.
- **Reaction delay.** `_readCd` is a flat 1.1 s; not difficulty-scaled.
- **Mistakes.** There is no mistake axis at all. The brief's central demand
  (§21) is that harder AI is *smarter, not robotic*, and that even Lethal errs.
  Nothing today makes Easy hesitate/over-approach or makes Lethal's rare error
  believable.
- **Combo length / variety caps** are only indirectly gated (feint/delayed
  scales); no explicit per-difficulty combo cap.

## 6. Hardcoded values to move onto the profile

The reaction cooldown (`_readCd = 1.1f`), the punish-window floor, and the
implicit "always take the opening" — these become difficulty-driven.

## 7. Systems safe to extend

`Difficulty.Def` (add computed scalars, Medium neutral), `EnemyAttackSelector`
(one more scoring term + caps), `EnemyBrain.ReadPlayer`/`Pick`/`TakeHit`
(multiply by scalars, add a mistake step), `AiTelemetry` (mistake counter),
`CombatDebugOverlay` (difficulty line), `AiEncounterDriver`/a new A/B check.

## 8. Duplicate difficulty logic

None. `Difficulty` is the single source; `ApplyTo` the single stat application.

## 9. Combat 2.0 systems to reuse (not duplicate)

`EnemyCombatProfile`, `EnemyAttackSelector`, `EnemyAttackHistory`,
`EnemyCombatMemory`, `PlayerCombatMemory`, boss phase profiles, the token pool,
posture/guard-break, the bounded camera. All stay authoritative.

## Plan

Extend `Difficulty.Def` with, Medium = 1.0/neutral throughout:

- `DefenseScale` (0.5 / 1 / 1.35 / 1.7) → multiplies reactive dodge/block/counter
  chance, hard-capped ≤ 0.85 so defense is never perfect.
- `RecoveryPunishChance` (0.4 / 0.75 / 0.9 / 1.0) → probability a spotted opening
  is actually taken.
- `SpacingScale` (0.5 / 1 / 1.2 / 1.4) → how tightly the enemy holds its
  preferred band; Easy over-approaches.
- `ReactionDelay` (0.5 / 0.28 / 0.18 / 0.12 s) → the read/punish cooldown floor.
- `MistakeChance` (0.35 / 0.15 / 0.07 / 0.03) → per-decision chance to degrade the
  choice (hesitate, or take a poor-fit attack, or over-commit). **Never zero** —
  Lethal still errs.
- `MaxComboLength` (1 / 2 / 3 / 4) → combo-continuation cap.

Wire into `ReadPlayer` (chance × DefenseScale, cooldown = ReactionDelay), the
punish gate (`Random < RecoveryPunishChance`), `TakeHit` block/counter (×
DefenseScale, capped), `StyleDir` (band tolerance × SpacingScale), and a new
mistake step in `Pick`. Add mistake telemetry and a difficulty line to the
overlay. Verify with `EmberDifficultyCheck`: the same enemy under all four
levels must show monotonic differences in feints, delayed attacks, defense,
punishes, variety, decision interval and mistakes — the acceptance test (§39)
that the difference is behaviour, not damage.

## Files

- Created: this document, `Assets/Editor/EmberDifficultyCheck.cs`.
- To modify: `Core/Difficulty.cs`, `Enemies/EnemyBrain.cs`,
  `Enemies/Combat/EnemyAttackSelector.cs`, `Enemies/AiTelemetry.cs`,
  `Debug/CombatDebugOverlay.cs`.
