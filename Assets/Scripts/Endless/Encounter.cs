using System.Collections.Generic;
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.Endless
{
    /// <summary>What a stretch of road throws at the marcher.</summary>
    public enum EncounterKind
    {
        Ambush, ArcherVolley, Assassins, EliteSquad, MiniBoss, Boss,
        Rescue, Defense, Duel, Escape,
    }

    /// <summary>
    /// One encounter as data: when it can appear, how often, and what it asks of
    /// the player. Composition lives in <see cref="Encounters.Compose"/> so a new
    /// encounter is a row plus a case, not a new subsystem.
    /// </summary>
    public readonly struct EncounterDef
    {
        public readonly EncounterKind Kind;
        public readonly string Banner, Objective;
        public readonly int MinDepth;      // encounters cleared before this can roll
        public readonly float Weight;      // relative pick chance once eligible
        public readonly float TimeLimit;   // 0 = untimed
        public readonly int ScoreValue;    // paid on clear, before multipliers

        public EncounterDef(EncounterKind kind, string banner, string objective,
            int minDepth, float weight, float timeLimit, int score)
        {
            Kind = kind; Banner = banner; Objective = objective;
            MinDepth = minDepth; Weight = weight; TimeLimit = timeLimit; ScoreValue = score;
        }
    }

    public static class Encounters
    {
        public static readonly EncounterDef[] All =
        {
            // Min-depths are staggered so that from depth 1 onward at least two
            // kinds are always eligible. With only one eligible kind the
            // no-repeat rule has nothing to pick and the road stutters.
            new(EncounterKind.Ambush, "AMBUSH", "SURVIVE THE AMBUSH", 0, 1.00f, 0f, 100),
            new(EncounterKind.ArcherVolley, "ARROW RAIN", "SILENCE THE ARCHERS", 1, 0.70f, 0f, 140),
            new(EncounterKind.Assassins, "THEY CAME QUIET", "KILL THE ASSASSINS", 2, 0.65f, 0f, 160),
            new(EncounterKind.EliteSquad, "A VETERAN SQUAD", "BREAK THE SQUAD", 5, 0.60f, 0f, 220),
            new(EncounterKind.MiniBoss, "SOMETHING WAITS", "KILL THE CHAMPION", 6, 0.40f, 0f, 320),
            new(EncounterKind.Boss, "SOMETHING BARS THE ROAD", "KILL IT", 9, 0.30f, 0f, 500),
            new(EncounterKind.Rescue, "SOMEONE IS STILL ALIVE", "REACH THE PRISONER", 3, 0.45f, 45f, 200),
            new(EncounterKind.Defense, "THEY WANT THE LANTERN", "HOLD THE GROUND", 2, 0.45f, 40f, 210),
            new(EncounterKind.Duel, "ONE STEPS FORWARD", "WIN THE DUEL", 7, 0.35f, 0f, 300),
            new(EncounterKind.Escape, "RUN", "OUTRUN THEM", 4, 0.40f, 30f, 190),
        };

        public static EncounterDef Get(EncounterKind k)
        {
            foreach (var d in All) if (d.Kind == k) return d;
            return All[0];
        }

        /// <summary>
        /// Weighted pick from what is eligible at this depth. The previous kind is
        /// excluded so the road never repeats itself back to back — with ten
        /// encounters and a weighted roll, repeats otherwise read as a bug.
        /// Boss Rush overrides the table entirely.
        /// </summary>
        public static EncounterKind Pick(int depth, EncounterKind? last)
        {
            if (RunModifiers.On(RunMod.BossRush))
                return depth % 3 == 2 ? EncounterKind.Boss : EncounterKind.MiniBoss;

            var total = 0f;
            foreach (var d in All)
            {
                if (depth < d.MinDepth) continue;
                if (last.HasValue && d.Kind == last.Value) continue;
                total += Weight(d, depth);
            }
            // Nothing else eligible: take the first thing that is not a repeat
            // rather than falling through to Ambush, which would repeat it.
            if (total <= 0f)
            {
                foreach (var d in All)
                    if (depth >= d.MinDepth && (!last.HasValue || d.Kind != last.Value))
                        return d.Kind;
                return EncounterKind.Ambush;
            }

            var roll = Random.value * total;
            foreach (var d in All)
            {
                if (depth < d.MinDepth) continue;
                if (last.HasValue && d.Kind == last.Value) continue;
                roll -= Weight(d, depth);
                if (roll <= 0f) return d.Kind;
            }
            return EncounterKind.Ambush;
        }

        /// <summary>
        /// Straight fights lose ground to set-pieces as the run goes on. Without
        /// this, ambushes stay the most likely roll forever and depth 30 looks
        /// like depth 3 with bigger numbers.
        /// </summary>
        private static float Weight(EncounterDef d, int depth)
        {
            var late = Mathf.Clamp01((depth - 6) / 18f);
            return d.Kind switch
            {
                EncounterKind.Ambush => d.Weight * Mathf.Lerp(1f, 0.35f, late),
                EncounterKind.Boss => d.Weight * Mathf.Lerp(0.6f, 1.5f, late),
                EncounterKind.MiniBoss => d.Weight * Mathf.Lerp(0.8f, 1.3f, late),
                EncounterKind.EliteSquad => d.Weight * Mathf.Lerp(0.7f, 1.4f, late),
                _ => d.Weight,
            };
        }

        /// <summary>
        /// The roster for an encounter at a given depth. This is where difficulty
        /// actually lives: the same encounter fields different *combinations* as
        /// the run deepens, rather than the same two enemies with more health.
        /// </summary>
        public static void Compose(EncounterKind kind, int depth, List<EnemyKind> into)
        {
            into.Clear();
            // Tier gates which enemy types exist at all, so new kinds enter the
            // pool over the run instead of everything showing up at depth 1.
            var tier = Mathf.Clamp(depth / 3, 0, 6);

            switch (kind)
            {
                case EncounterKind.Ambush:
                    Add(into, EnemyKind.Bandit, 3 + Mathf.Min(3, tier));
                    if (tier >= 1) Add(into, EnemyKind.RaiderAxe, 1 + tier / 2);
                    if (tier >= 3) Add(into, EnemyKind.PikeGuard, 1 + tier / 3);
                    if (tier >= 5) Add(into, EnemyKind.Bomber, 1);
                    break;

                case EncounterKind.ArcherVolley:
                    // Archers behind a screen of bodies: the fight is about
                    // reaching them, not about out-trading them.
                    Add(into, EnemyKind.Ranged, 2 + Mathf.Min(3, tier));
                    Add(into, EnemyKind.PikeGuard, 1 + tier / 2);
                    if (tier >= 4) Add(into, EnemyKind.Samurai, 1);
                    break;

                case EncounterKind.Assassins:
                    Add(into, EnemyKind.Assassin, 2 + Mathf.Min(3, tier));
                    if (tier >= 2) Add(into, EnemyKind.RogueNinja, 1 + tier / 3);
                    if (tier >= 4) Add(into, EnemyKind.Shade, 2);
                    break;

                case EncounterKind.EliteSquad:
                    Add(into, EnemyKind.EliteWarrior, 1 + tier / 2);
                    Add(into, EnemyKind.Samurai, 1 + tier / 3);
                    Add(into, EnemyKind.PikeGuard, 2);
                    if (tier >= 4) Add(into, EnemyKind.Ranged, 2);
                    break;

                case EncounterKind.MiniBoss:
                    into.Add(EnemyKind.Chief);
                    Add(into, EnemyKind.Bandit, 2 + tier / 2);
                    if (tier >= 3) Add(into, EnemyKind.RaiderAxe, 2);
                    break;

                case EncounterKind.Boss:
                    into.Add(((depth / 3) % 3) switch
                    {
                        0 => EnemyKind.Jin,
                        1 => EnemyKind.Kagachi,
                        _ => EnemyKind.Chief,
                    });
                    Add(into, EnemyKind.Bandit, Mathf.Min(4, 1 + tier));
                    if (tier >= 4) Add(into, EnemyKind.Ranged, 2);
                    break;

                case EncounterKind.Rescue:
                    // Light on bodies, heavy on pressure: the clock is the enemy.
                    Add(into, EnemyKind.Bandit, 2 + tier / 2);
                    Add(into, EnemyKind.Ranged, 1 + tier / 3);
                    if (tier >= 3) Add(into, EnemyKind.Assassin, 2);
                    break;

                case EncounterKind.Defense:
                    Add(into, EnemyKind.Bandit, 3 + tier);
                    if (tier >= 2) Add(into, EnemyKind.RaiderAxe, 2);
                    if (tier >= 4) Add(into, EnemyKind.Bomber, 1 + tier / 4);
                    break;

                case EncounterKind.Duel:
                    // Exactly one opponent. A duel with adds is not a duel.
                    into.Add(tier >= 4 ? EnemyKind.EliteWarrior : EnemyKind.Samurai);
                    break;

                case EncounterKind.Escape:
                    Add(into, EnemyKind.Bandit, 2 + tier / 2);
                    Add(into, EnemyKind.Assassin, 1 + tier / 3);
                    if (tier >= 3) Add(into, EnemyKind.Ranged, 2);
                    break;
            }

            // Elite Road promotes the rank and file. Applied after composition so
            // it upgrades whatever the encounter chose rather than needing a
            // second roster per encounter.
            if (RunModifiers.On(RunMod.EliteEnemies))
                for (var i = 0; i < into.Count; i++)
                    into[i] = into[i] switch
                    {
                        EnemyKind.Bandit => EnemyKind.RaiderAxe,
                        EnemyKind.RaiderAxe => EnemyKind.Samurai,
                        EnemyKind.Ranged => EnemyKind.Assassin,
                        EnemyKind.PikeGuard => EnemyKind.EliteWarrior,
                        EnemyKind.Shade => EnemyKind.RogueNinja,
                        _ => into[i],
                    };
        }

        private static void Add(List<EnemyKind> list, EnemyKind kind, int count)
        {
            for (var i = 0; i < count; i++) list.Add(kind);
        }
    }
}
