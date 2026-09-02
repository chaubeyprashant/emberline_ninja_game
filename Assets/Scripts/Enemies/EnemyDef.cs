using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>How an enemy wants to occupy the ground between it and the player.</summary>
    public enum MovementStyle
    {
        Direct,     // walks straight in — bruisers
        Flank,      // approaches off-axis so packs surround
        Spacing,    // holds a preferred band and lunges from it — duelists
        Kite,       // retreats to keep a firing lane
        Ambush,     // circles to the player's back before closing
        Reach,      // closes to weapon reach then holds — polearms
        Erratic,    // fast, unpredictable strafing — mobile assassins
        Flee,       // runs for the far edge — the quarry in a chase
    }

    /// <summary>
    /// The shared vocabulary of attacks. Every enemy composes its moveset from
    /// these; the brain implements each one exactly once, so a new enemy is a data
    /// change rather than new code.
    /// </summary>
    public enum AttackKind
    {
        Slash,        // single committed melee swing
        Flurry,       // several rapid low-damage hits
        Thrust,       // long, narrow line attack
        HeavySlam,    // telegraphed AoE with a ground scar
        SpinCleave,   // 360° melee, no safe side
        ChargedShot,  // long windup, double-damage projectile
        QuickShot,    // fast, weaker projectile
        ThrowBomb,    // lobbed AoE that denies ground
        PoisonSpit,   // projectile that slows
        DashStrike,   // closing dash that damages on contact
        Parry,        // defensive stance that punishes a hit
    }

    /// <summary>
    /// One move in an enemy's kit. The brain chooses among these by range and
    /// weight, so attack *patterns* differ per enemy without branching per enemy.
    /// </summary>
    [System.Serializable]
    public class AttackPattern
    {
        public AttackKind kind = AttackKind.Slash;

        [Tooltip("Usable when the player is within this band.")]
        public float minRange;
        public float maxRange = 2.4f;

        [Tooltip("Relative pick chance among patterns currently in range.")]
        public float weight = 1f;

        public float damageMultiplier = 1f;

        [Tooltip("Telegraph length. 0 uses the enemy's default windup.")]
        public float windupOverride;

        [Tooltip("Seconds before this enemy may attack again after using it.")]
        public float cooldown = 1.5f;

        [Tooltip("Red ring — reserved for attacks that genuinely hurt.")]
        public bool redTelegraph;

        [Tooltip("Telegraph footprint multiplier; describes the threatened area.")]
        public float telegraphScale = 1f;
    }

    /// <summary>
    /// Everything that makes one enemy different from another: identity, model,
    /// weapon, stats, movement, moveset, defence and weaknesses.
    ///
    /// This is the expansion point. A new enemy type is a new asset plus a
    /// character Spec — no new subclass, no new branch in the brain.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Enemy")]
    public class EnemyDef : ScriptableObject
    {
        [Header("Identity")]
        public string id = "bandit";
        public string displayName = "RAIDER";
        [TextArea] public string codexLine = "";
        public EnemyKind kind = EnemyKind.Bandit;
        public EnemyWeapon weapon = EnemyWeapon.Sword;

        [Tooltip("Rank drives boss bars, intro cards and execution immunity.")]
        public EnemyRank rank = EnemyRank.Mook;

        [Header("Body")]
        [Tooltip("EmberCharacterFactory spec name, e.g. BanditModel.")]
        public string modelSpec = "BanditModel";
        public float scale = 1f;

        [Header("Stats")]
        public float maxHp = 72f;
        public float moveSpeed = 3.2f;
        public float attackRange = 1.8f;
        public float damage = 13f;
        public float windupTime = 0.5f;
        public float spawnTime = 1.1f;

        [Header("Movement")]
        public MovementStyle movement = MovementStyle.Flank;

        [Tooltip("Band the enemy tries to hold. Meaning depends on the style.")]
        public float preferredRange = 2f;

        [Header("Moveset")]
        public AttackPattern[] attacks = System.Array.Empty<AttackPattern>();

        [Header("Defence")]
        [Tooltip("Flat damage subtracted from every hit, before multipliers.")]
        public float armor;

        [Tooltip("0 staggers on any hit; 1 never staggers to a light hit.")]
        [Range(0f, 1f)] public float poise;

        [Tooltip("Chance to shrug off a non-crush hit entirely (samurai guard).")]
        [Range(0f, 1f)] public float blockChance;

        [Header("Posture")]
        [Tooltip("Guard pool. Hits chip it; at zero the guard breaks and they are "
                 + "open. This is what stops an enemy from bleeding HP while still "
                 + "fighting normally.")]
        public float maxPosture = 40f;

        [Tooltip("Posture regained per second once they stop being hit.")]
        public float postureRegen = 9f;

        [Tooltip("Seconds after a hit before posture starts coming back.")]
        public float postureRegenDelay = 1.6f;

        [Tooltip("How long the guard-break window lasts.")]
        public float guardBreakSeconds = 2.2f;

        [Header("Card")]
        [Tooltip("Boss intro title, for named foes. Empty: no intro card.")]
        public string bossTitle = "";
        [Tooltip("Boss intro taunt, for named foes.")]
        [TextArea] public string bossTaunt = "";

        [Header("Behaviour")]
        [Tooltip("Chance to sidestep when the player's heavy telegraph is up.")]
        [Range(0f, 1f)] public float dodgeChance;
        [Tooltip("Chance to riposte immediately after a successful block.")]
        [Range(0f, 1f)] public float counterChance;
        [Tooltip("May attack out of turn when the player is committed, whiffed, or "
                 + "just dodged — still needs an attack token.")]
        public bool punishesExposure;
        [Tooltip("Blocks become reactive: raised against a visible heavy wind-up "
                 + "rather than rolled at random on the hit.")]
        public bool readsHeavies;
        [Tooltip("Holds guard and backs off when posture drops below a third.")]
        public bool guardsWhenPostureLow;
        [Tooltip("Keeps itself between the player and the nearest ranged ally.")]
        public bool protectsRanged;
        [Tooltip("Backs away to recover when HP falls below this fraction. 0 never.")]
        [Range(0f, 1f)] public float retreatBelowHp;
        [Tooltip("Ranged only: distance at which it abandons the shot and runs.")]
        public float panicRange;
        [Tooltip("Each stagger inside the window shortens the next by this factor.")]
        [Range(0.3f, 1f)] public float staggerDecay = 0.65f;

        [Header("Weaknesses (damage multipliers)")]
        [Tooltip("Hit from behind — archers and unaware enemies fold to this.")]
        public float backstabMultiplier = 1f;

        [Tooltip("Heavy/crush hits — armoured foes resist, brittle ones do not.")]
        public float crushMultiplier = 1f;

        [Tooltip("Thrown weapons — kunai and bolts.")]
        public float thrownMultiplier = 1f;

        [Tooltip("Smoke and fire — shades come apart in it.")]
        public float elementalMultiplier = 1f;

        /// <summary>Picks a pattern usable at this distance, weighted. Null if none.</summary>
        public AttackPattern ChooseAttack(float distance)
        {
            if (attacks == null || attacks.Length == 0) return null;
            var total = 0f;
            for (var i = 0; i < attacks.Length; i++)
            {
                var a = attacks[i];
                if (distance >= a.minRange && distance <= a.maxRange) total += Mathf.Max(0f, a.weight);
            }
            if (total <= 0f) return null;

            var roll = Random.value * total;
            for (var i = 0; i < attacks.Length; i++)
            {
                var a = attacks[i];
                if (distance < a.minRange || distance > a.maxRange) continue;
                roll -= Mathf.Max(0f, a.weight);
                if (roll <= 0f) return a;
            }
            return null;
        }

        /// <summary>Longest reach in the kit — used for approach decisions.</summary>
        public float MaxAttackRange
        {
            get
            {
                var max = attackRange;
                if (attacks == null) return max;
                foreach (var a in attacks) if (a.maxRange > max) max = a.maxRange;
                return max;
            }
        }
    }

    /// <summary>
    /// Rank rather than a hardcoded kind list. Bosses ignore light stagger, never
    /// launch or execute, and get an intro card; elites sit in between.
    /// </summary>
    public enum EnemyRank { Mook, Elite, MiniBoss, Boss }

    /// <summary>Def lookup by id. Asset file names and ids differ (Goro.asset is
    /// "goro"), and everything that names a foe names it by id.</summary>
    public static class EnemyDefs
    {
        private static EnemyDef[] _all;

        public static EnemyDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _all ??= Resources.LoadAll<EnemyDef>("Enemies");
            foreach (var d in _all) if (d != null && d.id == id) return d;
            var byFile = Resources.Load<EnemyDef>("Enemies/" + id);
            if (byFile != null) return byFile;
            _all = Resources.LoadAll<EnemyDef>("Enemies"); // a def authored since the cache
            foreach (var d in _all) if (d != null && d.id == id) return d;
            return null;
        }
    }
}
