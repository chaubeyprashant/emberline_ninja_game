using UnityEngine;

namespace Emberline.Core
{
    public enum DifficultyLevel { Easy, Medium, Hard, Lethal }

    /// <summary>
    /// Global difficulty. Every value is a multiplier over the shipped tuning, and
    /// Medium is exactly 1.0 on purpose: the game's balance was tuned at Medium, so
    /// difficulty layers over that rather than replacing it. That also means a bug
    /// in this table can only ever shift the curve, never redefine it.
    ///
    /// Difficulty changes pressure, not just numbers. The damage and health
    /// multipliers are the obvious half; the interesting half is how many enemies
    /// may attack at once (<see cref="ExtraAttackers"/>) and how much the game
    /// gives back when you clear a fight (<see cref="Heal"/>) — a Lethal run is
    /// harder mostly because it stops refilling you.
    /// </summary>
    public static class Difficulty
    {
        public readonly struct Def
        {
            public readonly DifficultyLevel Level;
            public readonly string Name, Blurb;
            public readonly float EnemyDamage, EnemyHp, Heal, PlayerHp, Score;
            public readonly int ExtraAttackers;

            // Combat 2.0: how enemies *decide*, not how hard they hit. Medium is
            // 1.0 on all of them; Easy thinks slower and never feints, Lethal
            // thinks faster, adapts harder and coordinates better.
            public float FeintScale => Level switch { DifficultyLevel.Easy => 0f, DifficultyLevel.Hard => 1.2f, DifficultyLevel.Lethal => 1.5f, _ => 1f };
            public float AdvancedScale => Level switch { DifficultyLevel.Easy => 0.5f, DifficultyLevel.Hard => 1.2f, DifficultyLevel.Lethal => 1.5f, _ => 1f };
            public float TeamworkScale => Level switch { DifficultyLevel.Easy => 0.6f, DifficultyLevel.Hard => 1.3f, DifficultyLevel.Lethal => 1.6f, _ => 1f };
            public float AdaptationScale => Level switch { DifficultyLevel.Easy => 0f, DifficultyLevel.Hard => 1.2f, DifficultyLevel.Lethal => 1.6f, _ => 1f };
            public float DecisionScale => Level switch { DifficultyLevel.Easy => 1.6f, DifficultyLevel.Hard => 0.9f, DifficultyLevel.Lethal => 0.7f, _ => 1f };

            public Def(DifficultyLevel level, string name, string blurb, float enemyDamage,
                float enemyHp, float heal, float playerHp, int extraAttackers, float score)
            {
                Level = level; Name = name; Blurb = blurb;
                EnemyDamage = enemyDamage; EnemyHp = enemyHp; Heal = heal;
                PlayerHp = playerHp; ExtraAttackers = extraAttackers; Score = score;
            }
        }

        public static readonly Def[] All =
        {
            new(DifficultyLevel.Easy, "EASY",
                "They hit softer and come at you one at a time.",
                enemyDamage: 0.60f, enemyHp: 0.80f, heal: 1.40f, playerHp: 1.25f,
                extraAttackers: -1, score: 0.60f),

            // The baseline the whole game was tuned against. Do not move these.
            new(DifficultyLevel.Medium, "MEDIUM",
                "The road as it was meant to be walked.",
                enemyDamage: 1f, enemyHp: 1f, heal: 1f, playerHp: 1f,
                extraAttackers: 0, score: 1f),

            new(DifficultyLevel.Hard, "HARD",
                "More of them swing at once, and the lantern mends slower.",
                enemyDamage: 1.40f, enemyHp: 1.25f, heal: 0.70f, playerHp: 1f,
                extraAttackers: 1, score: 1.35f),

            new(DifficultyLevel.Lethal, "LETHAL",
                "Everything reaches you. Almost nothing gives back.",
                enemyDamage: 2.00f, enemyHp: 1.45f, heal: 0.45f, playerHp: 0.85f,
                extraAttackers: 2, score: 1.90f),
        };

        public static DifficultyLevel Current
        {
            get => (DifficultyLevel)Mathf.Clamp(
                PlayerPrefs.GetInt("difficulty", (int)DifficultyLevel.Medium),
                0, All.Length - 1);
            set
            {
                PlayerPrefs.SetInt("difficulty",
                    (int)(DifficultyLevel)Mathf.Clamp((int)value, 0, All.Length - 1));
                PlayerPrefs.Save();
            }
        }

        /// <summary>The active row. Everything else reads through this.</summary>
        public static Def Now => All[(int)Current];

        public static string Name => Now.Name;

        public static void Step(int delta) =>
            Current = (DifficultyLevel)Mathf.Clamp((int)Current + delta, 0, All.Length - 1);

        /// <summary>
        /// Scale a freshly spawned enemy. Called from every spawn path immediately
        /// before SyncHpToMax, on top of whatever mode-specific scaling already
        /// applied — difficulty is the outermost multiplier, never the only one.
        /// </summary>
        public static void ApplyTo(Enemies.EnemyBrain brain)
        {
            if (brain == null) return;
            var d = Now;
            brain.maxHp *= d.EnemyHp;
            brain.damage *= d.EnemyDamage;
        }

        /// <summary>Scale a heal the game hands the player. Never below 1 HP.</summary>
        public static float ScaleHeal(float amount) => Mathf.Max(1f, amount * Now.Heal);
    }
}
