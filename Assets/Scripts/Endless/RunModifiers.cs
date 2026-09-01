using System;
using UnityEngine;

namespace Emberline.Endless
{
    /// <summary>Optional run rules, chosen before a march. Stored as a bitmask.</summary>
    [Flags]
    public enum RunMod
    {
        None = 0,
        NoHealing = 1 << 0,
        OneLife = 1 << 1,
        DoubleDamage = 1 << 2,
        FasterEnemies = 1 << 3,
        Fog = 1 << 4,
        HeavyRain = 1 << 5,
        BossRush = 1 << 6,
        EliteEnemies = 1 << 7,
    }

    /// <summary>
    /// The modifier table. Each one pays a score bonus proportional to how much
    /// it actually costs the player, so stacking is a real wager rather than a
    /// checklist. Bonuses multiply rather than add, because the difficulties
    /// compound too — no healing and one life together are far worse than either
    /// alone. A full eight-modifier stack pays roughly 7.3×.
    /// </summary>
    public static class RunModifiers
    {
        public readonly struct Def
        {
            public readonly RunMod Mod;
            public readonly string Name, Blurb;
            public readonly float ScoreBonus; // added to a 1.0 base, then multiplied

            public Def(RunMod mod, string name, string blurb, float bonus)
            {
                Mod = mod; Name = name; Blurb = blurb; ScoreBonus = bonus;
            }
        }

        public static readonly Def[] All =
        {
            new(RunMod.NoHealing, "NO HEALING",
                "Clearing an encounter no longer mends the lantern.", 0.35f),
            new(RunMod.OneLife, "ONE LIFE",
                "No revive. The march ends the first time you fall.", 0.30f),
            new(RunMod.DoubleDamage, "DOUBLE EDGE",
                "Everything hits twice as hard — including you.", 0.25f),
            new(RunMod.FasterEnemies, "THE MIST HUNGERS",
                "Everything on the road moves 25% faster.", 0.20f),
            new(RunMod.Fog, "BLIND ROAD",
                "The fog closes to arm's length. Markers thin out.", 0.15f),
            new(RunMod.HeavyRain, "DOWNPOUR",
                "Heavy rain. Harder to see, harder to hear them coming.", 0.15f),
            new(RunMod.BossRush, "BOSS RUSH",
                "No soldiers. Only the things that bar the road.", 0.50f),
            new(RunMod.EliteEnemies, "ELITE ROAD",
                "Every soldier is replaced by its veteran.", 0.40f),
        };

        public static RunMod Selected
        {
            get => (RunMod)PlayerPrefs.GetInt("run_mods", 0);
            set { PlayerPrefs.SetInt("run_mods", (int)value); PlayerPrefs.Save(); }
        }

        /// <summary>Rules for the run in progress. Frozen at launch so toggling
        /// the menu mid-march cannot change the run that is being scored.</summary>
        public static RunMod Active { get; private set; }

        public static void BeginRun() => Active = Selected;

        public static bool On(RunMod m) => (Active & m) != 0;

        public static void Toggle(RunMod m) => Selected ^= m;

        public static bool IsSelected(RunMod m) => (Selected & m) != 0;

        /// <summary>Score multiplier for a modifier set. 1.0 with none active.</summary>
        public static float ScoreMultiplier(RunMod mods)
        {
            var mul = 1f;
            foreach (var d in All)
                if ((mods & d.Mod) != 0) mul *= 1f + d.ScoreBonus;
            return mul;
        }

        public static float ActiveScoreMultiplier => ScoreMultiplier(Active);

        /// <summary>Short "NO HEALING · ONE LIFE" line for the HUD and results.</summary>
        public static string Describe(RunMod mods)
        {
            if (mods == RunMod.None) return "NO MODIFIERS";
            var s = "";
            foreach (var d in All)
                if ((mods & d.Mod) != 0) s += (s.Length > 0 ? " · " : "") + d.Name;
            return s;
        }
    }
}
