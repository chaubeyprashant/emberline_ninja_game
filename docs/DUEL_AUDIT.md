# Duel Mode audit

Part 1 of the Duel overhaul. Read-only findings, checked against source on
2026-09-03, then the plan. The one-shot investigation from the Combat 2.0 work
(`docs/COMBAT_CAMERA_AUDIT.md`) already ruled out duplicate hits; this focuses
on why a *duel* specifically ends in seconds.

## Why a duel ends in seconds — answer: H (multiple), dominated by execution

**The decisive cause is execution on guard break, against a tiny posture pool.**

- `EnemyBrain.CanExecute` returns true for a **guard-broken enemy of any rank
  below Boss**: `GuardBroken && Rank < EnemyRank.Boss`. The duel roster is Goro
  (MiniBoss), the Pale Shade (MiniBoss), the named foes (MiniBoss/Elite) — all
  below Boss. So the instant their guard breaks they become executable, and
  `StrikeArc` auto-executes any `CanExecute` target: the next light kills them
  outright regardless of HP.
- Boss posture is the **default `maxPosture = 40`** (Goro never overrides it;
  the named-foe copies get `×1.5 = 60`). A light hit chips posture by its damage
  (`Posture -= amount`, 10–18 for the katana). **Three light hits break the
  guard.** Break → executable → dead. Elapsed: ~2–3 seconds.
- Kagachi and Jin are Boss rank, so they are *not* guard-break-executable — but
  they still fall fast: 40 posture means they are staggered or knocked back on
  almost every hit (a mook's poise), which interrupts their offense and lets the
  player free-attack their 420/340 HP down with an uninterrupted combo.

Secondary contributors:

- **HP attrition, not posture, is the axis.** The player's combo removes real HP
  (katana chain 10/12/18 + heavy 26). A duel is meant to be won by *control*
  (posture) with HP as the survival meter (Part 5); today HP just drains.
- **Boss HP floor is 190** and nothing scales it per opponent, so a "final duel"
  and a "first duel" have the same order of HP.
- **No per-duel identity in data.** `DuelDef` carries name/title/taunt/kind/marsh
  only. Every duel is the same arena, the same posture, the same execution rule
  — "another enemy with different HP", exactly as the brief says.

Ruled out:

- **A. Player damage too high** — contributes, but is not the primary cause; even
  halved, execution ends the fight in ~5 hits.
- **B. HP initialised wrong** — no: `SetDef → ApplyDef (Hp = maxHp)` then the duel
  floor, modifier, NG+, difficulty, `SyncHpToMax`. HP ends correct.
- **C. Multiple damage registration** — no: damage is single-call, no trigger
  colliders, no animation events, dash guarded by `_dashHit`, flurry explicit.
- **D. Combo unintended damage** — no.
- **E. Difficulty multiplier wrong** — no: Medium is 1.0; the duel-integrity
  check reports the worst single hit at 13–36% of HP on Medium.
- **F. No meaningful defense** — partly true and addressed below: the bosses
  *have* Combat 2.0 defense (block, read-heavies, guard-when-low, dodge) but the
  posture pool is so small the guard breaks before the defense matters.
- **G. Posture/execution too early** — **yes, this is the core.**

## What already exists and should be reused

The Combat 2.0 rebuild gives every boss what the brief asks for; the duel just
does not lean on it:

- `EnemyCombatProfile` per boss and **per phase**: Goro (`goro` → `goro_enraged`),
  Kagachi (`kagachi` → warlord → marsh → exhausted), Jin (`jin` → `jin_storm`).
  Phases change the profile — attack pool, weighting, defense, aggression,
  spacing — not just HP (`ActiveProfile`, HP-gated at 0.65/0.4/0.15).
- `EnemyAttackSelector` with distance/position/state/tactical/personality/
  adaptation/repetition scoring; `EnemyAttackHistory` (anti-spam); feints,
  delayed attacks, missed-heavy recovery ×1.6, perfect-parry recoil, morale.
- The camera is already centralized and bounded (`RequestCameraImpact`, FOV
  clamped `[44,52]`, additive-from-base, strongest-wins, timer decay) with
  midpoint 1-v-1 framing via `LockFocus`.
- The story beat framework (`StoryBeat`, `CinematicDirector`, `DialogueBox`) for
  intros and endings; `BossIntroDirector` already shows a name/taunt card.

So the overhaul is **tuning and wiring, not a rebuild**: fix execution, make
posture the earned axis, give each `DuelDef` an identity (HP, posture, phases,
arena, intro/defeat lines), and lean on the phase profiles that already exist.

## Plan

1. **Execution (Part 5, 7, 19).** A boss-ranked or duel opponent is never
   executable from an ordinary guard break — only as the *earned final blow* at
   low HP. Guard break becomes a **punish window** (boss staggered, takes ×1.5
   HP damage for its duration), not an instant kill. Mooks and mission elites
   keep guard-break execution.
2. **Posture is the meter (Part 5).** Per-duel `posture` and `postureRegen`,
   raised so a break is earned over the fight and lost if pressure lapses. The
   fight loop becomes pressure → break → punish → the boss recovers into its
   next phase.
3. **Per-duel identity in data (Part 6, 12, 13, 15).** `DuelDef` gains `hp`,
   `posture`, `postureRegen`, `philosophy`, arena `theme`/weather, and short
   `intro`/`defeat` dialogue. Applied at duel spawn; arena theme applied on
   launch; dialogue shown through the existing story dialogue box.
4. **Phases for all four (Part 6, 14).** Goro/Kagachi/Jin already phase; add a
   Pale Shade duel phase profile so it, too, changes behaviour. Verify the phase
   gates fit the longer HP bars.
5. **Length (Part 3) emerges** from 1–4: no execution shortcut, posture-paced,
   per-boss HP set for 2–3 / 3–4 / 4–5 / 5–7 minutes on Medium.
6. **Verify (Part 17, 18).** Extend the duel-integrity check into a headless
   duel simulation that estimates fight duration and confirms no early-execution
   kill; then the A33.

## Files

- Created: this document.
- To modify: `Assets/Scripts/Enemies/EnemyBrain.cs` (execution rule, posture
  overrides, guard-break punish), `Assets/Scripts/Core/Session.cs` (`DuelDef`
  fields + the four duels' identities), `Assets/Scripts/GameManager.cs` (apply
  duel tuning, arena, intro/defeat), `Assets/Editor/EmberCombatKits.cs` (Pale
  Shade duel phases), `Assets/Editor/EmberDuelIntegrity.cs` (duration sim).
