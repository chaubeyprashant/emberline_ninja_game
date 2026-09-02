using Emberline.Core;
using Emberline.Player;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// "What is the best thing I can do right now?" Scores every attack in the
    /// kit against the situation and picks among the best with a little
    /// randomness, so the *reason* for an attack is always legible and the
    /// exact attack never quite certain. No allocation per call: the
    /// candidates live in a fixed buffer.
    /// </summary>
    public class EnemyAttackSelector
    {
        public struct Context
        {
            public float distance;
            public float relativeAngle;        // player's bearing from the enemy's forward, degrees
            public bool playerBackTurned;      // the player is facing away from this enemy
            public ObservedPlayerState state;
            public float stateRemaining;       // seconds of the observed commitment left
            public int alliesNear;             // aware allies within 6 m
            public int othersAttacking;        // enemies currently in a wind-up or dash
            public bool playerSurrounded;      // ≥3 enemies within 4 m of the player
            public bool hasToken;              // may this enemy attack now
            public SquadRole role;
            public float hp01;
            public float posture01;
            public PlayerCombatMemory playerMemory;
            public string lastAttackId;
        }

        private const int Max = 16;
        private readonly EnemyCombatDecision[] _buf = new EnemyCombatDecision[Max];
        private int _n;

        /// <summary>The scored candidates of the last call, for the overlay.</summary>
        public EnemyCombatDecision Last { get; private set; }
        public ObservedPlayerState LastState { get; private set; }

        public AttackDefinition Choose(EnemyDef def, EnemyCombatProfile profile, EnemyAttackHistory history,
            EnemyCombatMemory memory, in Context c, float attackCooldownLeft)
        {
            _n = 0;
            LastState = c.state;
            if (def == null || def.attacks == null) return null;
            var d = Difficulty.Now;

            for (var i = 0; i < def.attacks.Length && _n < Max; i++)
            {
                var a = def.attacks[i];
                if (a == null || a.kind == AttackKind.Parry) continue;
                if (c.distance < a.minRange || c.distance > a.maxRange) continue;
                if (!Requires(a.requires, c)) continue;
                // Advanced categories are gated by difficulty, never by HP.
                if (a.category == AttackCategory.Feint && d.FeintScale <= 0f) continue;
                if (a.category == AttackCategory.Delayed && d.AdvancedScale <= 0f) continue;

                var dec = new EnemyCombatDecision { attack = a, cooldownOk = attackCooldownLeft <= 0f ? 1f : 0f };
                if (dec.cooldownOk <= 0f && c.state is not (ObservedPlayerState.Recovering or ObservedPlayerState.Staggered))
                    continue; // only an opening justifies breaking cadence

                dec.distance = DistanceScore(a, c.distance);
                dec.position = PositionScore(a, c);
                dec.playerState = StateScore(a, c) * d.AdvancedScale;
                dec.tactical = TacticalScore(a, c, profile) * d.TeamworkScale;
                dec.personality = PersonalityScore(a, profile);
                dec.adaptation = profile != null && memory != null
                    ? memory.Bias(a.category, c.playerMemory, profile.adaptation * d.AdaptationScale) : 0f;
                dec.repetition = history != null ? history.Penalty(a) : 1f;

                // Combo continuation: the next step of a chain the enemy is on.
                if (profile != null && history != null)
                {
                    var next = profile.NextComboStep(history.LastId);
                    if (next != null && next == a.id) dec.personality += 1.4f;
                }
                _buf[_n++] = dec;
            }
            if (_n == 0) return null;

            // Pick among the top: probability ∝ score², over candidates within
            // 65 % of the best. The best usually wins; it does not always.
            var best = 0f;
            for (var i = 0; i < _n; i++) best = Mathf.Max(best, _buf[i].Total);
            if (best <= 0f) return null;
            var sum = 0f;
            for (var i = 0; i < _n; i++)
            {
                var t = _buf[i].Total;
                if (t < best * 0.65f) continue;
                sum += t * t;
            }
            var roll = Random.value * sum;
            for (var i = 0; i < _n; i++)
            {
                var t = _buf[i].Total;
                if (t < best * 0.65f) continue;
                roll -= t * t;
                if (roll <= 0f) { Last = _buf[i]; return _buf[i].attack; }
            }
            Last = _buf[0];
            return _buf[0].attack;
        }

        private static bool Requires(TargetStateRequirement r, in Context c) => r switch
        {
            TargetStateRequirement.Any => true,
            TargetStateRequirement.Attacking => c.state == ObservedPlayerState.Attacking,
            TargetStateRequirement.Guarding => c.state == ObservedPlayerState.Guarding,
            TargetStateRequirement.Recovering => c.state == ObservedPlayerState.Recovering,
            TargetStateRequirement.Dodging => c.state == ObservedPlayerState.Dodging,
            TargetStateRequirement.Staggered => c.state == ObservedPlayerState.Staggered,
            TargetStateRequirement.Retreating => c.state == ObservedPlayerState.Retreating,
            TargetStateRequirement.Circling => c.state == ObservedPlayerState.Circling,
            TargetStateRequirement.BackTurned => c.playerBackTurned,
            _ => true,
        };

        private static float DistanceScore(AttackDefinition a, float dist)
        {
            var pref = a.preferredRange > 0f ? a.preferredRange : a.maxRange * 0.7f;
            var span = Mathf.Max(0.4f, (a.maxRange - a.minRange) * 0.5f);
            var off = Mathf.Abs(dist - pref) / span;
            return Mathf.Lerp(1f, 0.3f, Mathf.Clamp01(off));
        }

        private static float PositionScore(AttackDefinition a, in Context c)
        {
            var side = c.relativeAngle > 35f && c.relativeAngle < 110f;
            var behindMe = c.relativeAngle >= 110f;
            var s = 0f;
            if (side && a.category == AttackCategory.Sweep) s += 0.6f;
            if (behindMe && a.category is AttackCategory.GapCloser or AttackCategory.RetreatAttack) s += 0.4f;
            if (c.playerBackTurned && a.category is AttackCategory.Quick or AttackCategory.GapCloser) s += 0.5f;
            if (c.playerBackTurned && a.requires == TargetStateRequirement.BackTurned) s += 0.8f;
            return s;
        }

        private static float StateScore(AttackDefinition a, in Context c)
        {
            var cat = a.category;
            switch (c.state)
            {
                case ObservedPlayerState.Attacking:
                    if (cat == AttackCategory.Counter) return 0.9f;
                    if (cat == AttackCategory.Quick && a.interruptible) return 0.5f;
                    if (cat == AttackCategory.Heavy) return -0.3f;
                    return 0f;
                case ObservedPlayerState.Guarding:
                    if (cat == AttackCategory.GuardBreak) return 1.1f;
                    if (cat == AttackCategory.Heavy) return 0.35f;
                    if (cat == AttackCategory.Feint) return 0.4f;
                    if (cat == AttackCategory.Quick) return -0.4f;
                    return 0f;
                case ObservedPlayerState.Dodging:
                    if (cat == AttackCategory.Delayed) return 0.9f;
                    if (cat == AttackCategory.Sweep) return 0.3f;
                    if (cat == AttackCategory.Quick) return 0.25f;
                    return 0f;
                case ObservedPlayerState.Recovering:
                    if (cat == AttackCategory.Quick) return 0.9f;
                    if (cat == AttackCategory.Heavy) return 0.6f;
                    if (cat == AttackCategory.GapCloser) return 0.5f;
                    return 0.1f;
                case ObservedPlayerState.Staggered:
                    if (cat == AttackCategory.Heavy) return 1.1f;
                    if (cat == AttackCategory.Quick) return 0.5f;
                    return 0.2f;
                case ObservedPlayerState.Retreating:
                    if (cat == AttackCategory.Thrust) return 0.9f;
                    if (cat == AttackCategory.GapCloser) return 0.85f;
                    if (cat == AttackCategory.Ranged) return 0.5f;
                    if (cat == AttackCategory.Heavy) return -0.4f;
                    return 0f;
                case ObservedPlayerState.Circling:
                    if (cat == AttackCategory.Sweep) return 0.9f;
                    if (cat == AttackCategory.Thrust) return -0.3f;
                    return 0.1f;
                default:
                    return cat == AttackCategory.Quick ? 0.2f : cat == AttackCategory.Feint ? 0.15f : 0f;
            }
        }

        private static float TacticalScore(AttackDefinition a, in Context c, EnemyCombatProfile p)
        {
            var s = 0f;
            var team = p != null ? p.teamwork : 0.4f;
            // Someone else is swinging: pressure roles hold back, except to interrupt.
            if (c.othersAttacking > 0 && c.role is SquadRole.Wait or SquadRole.Circle or SquadRole.Reposition)
                s -= 0.5f * (1f - team * 0.5f);
            if (a.category == AttackCategory.TeamAttack) s += c.alliesNear >= 2 ? 0.8f * team : -2f;
            if (c.playerSurrounded && a.category == AttackCategory.Quick) s += 0.2f * team;
            if (c.alliesNear >= 1 && a.category == AttackCategory.RetreatAttack) s += 0.2f * team;
            return s;
        }

        private static float PersonalityScore(AttackDefinition a, EnemyCombatProfile p)
        {
            if (p == null) return a.weight * 0.3f;
            var s = a.weight * 0.3f;
            switch (a.category)
            {
                case AttackCategory.Quick: s += p.aggression * 0.6f + p.attackFrequency * 0.3f; break;
                case AttackCategory.Heavy: s += p.aggression * 0.5f + p.bravery * 0.2f; break;
                case AttackCategory.GuardBreak: s += p.guardBreakFrequency * 0.9f; break;
                case AttackCategory.Thrust: s += 0.35f; break;
                case AttackCategory.Sweep: s += 0.3f; break;
                case AttackCategory.Delayed: s += p.feintFrequency * 0.6f + 0.15f; break;
                case AttackCategory.Feint: s += p.feintFrequency * 1.2f; break;
                case AttackCategory.GapCloser: s += p.aggression * 0.4f + 0.15f; break;
                case AttackCategory.RetreatAttack: s += p.retreatTendency * 0.9f + (1f - p.bravery) * 0.3f; break;
                case AttackCategory.Counter: s += p.counterFrequency * 0.9f; break;
                case AttackCategory.Ranged: s += 0.4f + (1f - p.bravery) * 0.3f; break;
                case AttackCategory.TeamAttack: s += p.teamwork * 0.5f; break;
            }
            return s;
        }
    }
}
