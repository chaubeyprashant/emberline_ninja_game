using Emberline.Enemies;
using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// Turns "the attack button was pressed" into "which attack". Reads only
    /// what is visible on the field: the target's state and facing, the
    /// player's own state and motion, and the counter windows the combat
    /// controller already tracks. No new buttons; the situation is the input.
    /// </summary>
    public static class PlayerContextResolver
    {
        public struct Situation
        {
            public EnemyBrain target;
            public float distance;
            public bool behindTarget;
            public bool targetUnaware;
            public bool targetStaggered;
            public bool targetGuarding;
            public bool targetRetreating;
            public bool parryCounter;
            public bool dodgeCounter;
            public bool airborne;
            public bool wallRunning;
            public bool sprinting;
            public int chainStage;      // stage the *previous* light reached (0 = none)
            public bool afterHeavy;     // a heavy resolved within the chain window
            public bool heavyPressed;   // the heavy button, not the light one
            public float reach;         // the weapon's strike range
        }

        /// <summary>Highest-priority context the situation supports.</summary>
        public static AttackContext Resolve(in Situation s)
        {
            if (s.heavyPressed)
            {
                if (s.target != null && s.targetGuarding) return AttackContext.GuardBreakPunish;
                if (s.chainStage >= 2) return AttackContext.HeavyFinisher;
                if (s.chainStage == 1) return AttackContext.HeavyThrust;
                return AttackContext.Heavy;
            }

            if (s.target != null)
            {
                if (s.targetUnaware && s.behindTarget) return AttackContext.Assassination;
                if (s.targetStaggered) return AttackContext.StaggerPunish;
            }
            if (s.parryCounter) return AttackContext.ParryCounter;
            if (s.dodgeCounter) return AttackContext.DodgeCounter;
            if (s.target != null)
            {
                if (s.behindTarget) return AttackContext.BackAttack;
                if (s.targetGuarding) return AttackContext.GuardBreakPunish;
                if (s.targetRetreating && s.distance > s.reach * 0.9f && s.distance < s.reach * 2.2f)
                    return AttackContext.GapCloser;
            }
            if (s.wallRunning) return AttackContext.WallRun;
            if (s.airborne) return AttackContext.Air;
            if (s.afterHeavy) return AttackContext.HeavyFollow;
            if (s.sprinting && s.chainStage == 0) return AttackContext.Running;
            return s.chainStage switch
            {
                >= 2 => AttackContext.Chain3,
                1 => AttackContext.Chain2,
                _ => AttackContext.Chain1,
            };
        }

        /// <summary>
        /// Contexts fall back down this ladder when a weapon has no entry for
        /// the resolved one, so a moveset can author eight attacks and still
        /// answer every situation.
        /// </summary>
        public static AttackContext Fallback(AttackContext ctx) => ctx switch
        {
            AttackContext.Assassination => AttackContext.BackAttack,
            AttackContext.StaggerPunish => AttackContext.Chain3,
            AttackContext.ParryCounter => AttackContext.Chain2,
            AttackContext.DodgeCounter => AttackContext.Chain2,
            AttackContext.BackAttack => AttackContext.Chain2,
            AttackContext.GuardBreakPunish => AttackContext.Heavy,
            AttackContext.GapCloser => AttackContext.Running,
            AttackContext.Air => AttackContext.Chain1,
            AttackContext.WallRun => AttackContext.Air,
            AttackContext.Running => AttackContext.Chain1,
            AttackContext.HeavyFinisher => AttackContext.Heavy,
            AttackContext.HeavyThrust => AttackContext.Heavy,
            AttackContext.HeavyFollow => AttackContext.Chain1,
            AttackContext.Chain3 => AttackContext.Chain1,
            AttackContext.Chain2 => AttackContext.Chain1,
            _ => AttackContext.Chain1,
        };
    }
}
