using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// Every exclusive thing Renzo can be doing. Before this the controller tracked
    /// combat with a handful of independent timers, which meant states could
    /// silently overlap — you could open a guard during a heavy windup, or dodge
    /// out of an execution. One enum with an explicit transition table makes those
    /// conflicts impossible to express.
    /// </summary>
    public enum CombatState
    {
        Free,        // neutral: moving, nothing committed
        Light,       // light attack, chainable
        Heavy,       // heavy attack windup + swing
        Guard,       // holding block
        Parry,       // the tight window at the start of a guard
        Dodge,       // i-frames
        Recover,     // committed recovery, cannot act
        Staggered,   // posture broken, punished
        Execute,     // finisher, fully locked
    }

    /// <summary>
    /// Transition rules for the combat state machine. Kept as data rather than
    /// scattered if-checks so "can I do X right now" has exactly one answer.
    /// </summary>
    public static class CombatRules
    {
        /// <summary>Can we leave `from` to enter `to` right now?</summary>
        public static bool CanEnter(CombatState from, CombatState to)
        {
            // Nothing interrupts these two — they own the character until done.
            if (from is CombatState.Execute or CombatState.Staggered) return false;

            switch (to)
            {
                // Dodge is the universal escape: it cancels attacks and guards,
                // which is what keeps the game responsive under pressure.
                case CombatState.Dodge:
                    return from != CombatState.Dodge;

                // Light attacks chain out of themselves and out of a guard/parry
                // (that is the riposte), but never out of a heavy commitment.
                case CombatState.Light:
                    return from is CombatState.Free or CombatState.Light
                        or CombatState.Guard or CombatState.Parry;

                // Heavy is a commitment: only from neutral or a finished light.
                case CombatState.Heavy:
                    return from is CombatState.Free or CombatState.Light;

                // Heavy is included deliberately: the heavy windup *is* the guard
                // window in this game, so a parry landing during it is the design,
                // not a conflict. Without this the deflect system never opens.
                case CombatState.Guard:
                case CombatState.Parry:
                    return from is CombatState.Free or CombatState.Guard
                        or CombatState.Parry or CombatState.Heavy;

                case CombatState.Execute:
                    return from is CombatState.Free or CombatState.Light or CombatState.Guard;

                // Recover and Staggered are imposed, never requested by input.
                default:
                    return true;
            }
        }

        /// <summary>States where movement input is ignored or heavily damped.</summary>
        public static bool Committed(CombatState s) =>
            s is CombatState.Heavy or CombatState.Recover
                or CombatState.Staggered or CombatState.Execute;

        /// <summary>States that should not accept new attack input at all.</summary>
        public static bool Locked(CombatState s) =>
            s is CombatState.Staggered or CombatState.Execute;
    }

    /// <summary>
    /// How a body reacts to being hit. Different attacks must read differently —
    /// a jab should flinch, a heavy should throw them, a broken guard should drop
    /// them — so reaction is chosen per hit rather than being one shared stagger.
    /// </summary>
    public enum HitReaction
    {
        Flinch,      // light hit: brief hitch, keeps its feet
        Knockback,   // heavy hit: shoved along the blow
        Launch,      // finisher: airborne
        GuardBreak,  // posture gone: long, punishable collapse
        Deflected,   // the target turned it aside
    }
}
