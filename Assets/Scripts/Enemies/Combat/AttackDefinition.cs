using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// What an attack is *for*. The resolver still runs on <see cref="AttackKind"/>
    /// (how it lands); the selector reasons in categories (why it was chosen). One
    /// kind can serve several categories — a Slash with a long startup is a
    /// Heavy, the same Slash with a cancel point is a Feint.
    /// </summary>
    public enum AttackCategory
    {
        Quick,         // interrupt, punish recovery, start a chain
        Heavy,         // damage, posture, punish passive defence
        GuardBreak,    // built to open a block
        Thrust,        // long and narrow: punish retreat, hold distance
        Sweep,         // wide: punish circling, own the space
        Delayed,       // the active frame arrives late on purpose
        Feint,         // starts, cancels into something else
        GapCloser,     // movement plus attack
        RetreatAttack, // attack while making distance
        Counter,       // after a successful block/dodge
        Ranged,        // projectile
        TeamAttack,    // needs a squad set-up
    }

    /// <summary>What the target must be doing for the attack to be considered.</summary>
    public enum TargetStateRequirement
    {
        Any,
        Attacking,
        Guarding,
        Recovering,
        Dodging,
        Staggered,
        Retreating,
        Circling,
        BackTurned,
    }

    /// <summary>
    /// One move in an enemy's kit — the data-driven attack the Combat 2.0 brief
    /// asks for. Extends the pattern that already drove every enemy: the fields
    /// the brain has always read are unchanged in name and meaning, so the
    /// thirteen authored kits migrate untouched, and the new fields default to
    /// "behave as before" (a zero recovery falls back to the kind's old value).
    ///
    /// Kept as a serializable class embedded in the EnemyDef rather than an
    /// asset per attack: forty assets that can each lose their script binding
    /// buy nothing over a list the def already owns.
    /// </summary>
    [System.Serializable]
    public class AttackDefinition
    {
        [Header("Identity")]
        public string id = "";
        public string displayName = "";

        [Tooltip("Why this attack exists. The selector reasons in categories.")]
        public AttackCategory category = AttackCategory.Quick;

        [Tooltip("How it resolves once it lands.")]
        public AttackKind kind = AttackKind.Slash;

        [Header("Timing (seconds)")]
        [Tooltip("Telegraph length. 0 uses the enemy's default windup.")]
        public float windupOverride;

        [Tooltip("Frames during which the hit can land. 0 = instant on resolve (legacy).")]
        public float active;

        [Tooltip("Committed time after the attack. 0 = the kind's legacy value.")]
        public float recovery;

        [Tooltip("Seconds before this enemy may attack again after using it.")]
        public float cooldown = 1.5f;

        [Header("Reach")]
        [Tooltip("Usable when the player is within this band.")]
        public float minRange;
        public float maxRange = 2.4f;

        [Tooltip("Distance the enemy would like to be at when it starts. 0 = maxRange × 0.7.")]
        public float preferredRange;

        [Header("Force")]
        public float damageMultiplier = 1f;

        [Tooltip("Posture damage to a guarding player, in multiples of the damage. 0 = same as damage.")]
        public float postureMultiplier;

        [Tooltip("Knockback impulse on hit.")]
        public float knockback;

        [Range(0f, 2f)] public float staggerPower = 1f;
        [Range(0f, 3f)] public float guardBreakPower;

        [Header("Motion")]
        [Tooltip("Turns toward the player during startup: 0 none, 1 full.")]
        [Range(0f, 1f)] public float tracking = 1f;

        [Tooltip("Ground covered during the attack, along facing. Negative retreats.")]
        public float movement;

        [Header("Rules")]
        public bool parryable = true;
        public bool dodgeable = true;
        public bool interruptible = true;
        public bool canFeint;
        public bool canChain;

        [Tooltip("Ids of attacks this one may follow directly. Empty = any.")]
        public string[] followsAfter = System.Array.Empty<string>();

        public TargetStateRequirement requires = TargetStateRequirement.Any;

        [Header("Selection")]
        [Tooltip("Relative pick chance among patterns currently in range.")]
        public float weight = 1f;

        [Tooltip("Weight for the player-side resolver, where an attack is shared.")]
        public float playerWeight;

        [Header("Presentation")]
        [Tooltip("Red ring — reserved for attacks that genuinely hurt.")]
        public bool redTelegraph;

        [Tooltip("Telegraph footprint multiplier; describes the threatened area.")]
        public float telegraphScale = 1f;

        [Tooltip("Sfx3D cue name at startup. Empty = the category's default.")]
        public string audioCue = "";

        [Tooltip("Camera shake amplitude on hit. 0 = the kind's default.")]
        public float cameraImpact;

        public Player.HitReaction hitReaction = Player.HitReaction.Flinch;

        /// <summary>The recovery the brain should use: authored, else the kind's legacy value.</summary>
        public float RecoveryFor(AttackKind k) => recovery > 0f ? recovery : LegacyRecovery(k);

        /// <summary>The numbers every kind recovered with before recovery was data.</summary>
        public static float LegacyRecovery(AttackKind k) => k switch
        {
            AttackKind.Flurry => 0.8f,
            AttackKind.Thrust => 0.9f,
            AttackKind.ThrowBomb => 1f,
            AttackKind.ChargedShot => 0.85f,
            AttackKind.SpinCleave => 1f,
            AttackKind.DashStrike => 0.8f,
            AttackKind.Sweep => 0.85f,
            AttackKind.GuardBreak => 0.9f,
            AttackKind.RetreatSlash => 0.5f,
            _ => 0.75f,
        };

        /// <summary>Which category a bare kind reads as, for kits authored before categories existed.</summary>
        public static AttackCategory CategoryOf(AttackKind k, float windup) => k switch
        {
            AttackKind.Flurry => AttackCategory.Quick,
            AttackKind.Thrust => AttackCategory.Thrust,
            AttackKind.HeavySlam => AttackCategory.Heavy,
            AttackKind.SpinCleave => AttackCategory.Sweep,
            AttackKind.DashStrike => AttackCategory.GapCloser,
            AttackKind.Parry => AttackCategory.Counter,
            AttackKind.Sweep => AttackCategory.Sweep,
            AttackKind.GuardBreak => AttackCategory.GuardBreak,
            AttackKind.RetreatSlash => AttackCategory.RetreatAttack,
            AttackKind.ChargedShot or AttackKind.QuickShot or AttackKind.ThrowBomb
                or AttackKind.PoisonSpit => AttackCategory.Ranged,
            _ => windup >= 0.6f ? AttackCategory.Heavy : AttackCategory.Quick,
        };
    }
}
