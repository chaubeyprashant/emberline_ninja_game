using System;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// One rotating challenge per real-world day, checked against mission
    /// results. Completion pays Ember Shards. All state in PlayerPrefs.
    /// </summary>
    public static class DailyChallenge
    {
        public class Def
        {
            public string desc;
            public int reward;
            // (mode, won, damageTaken, timeSeconds, maxCombo, waveReached) → success
            public Func<LaunchMode, bool, float, float, int, int, bool> check;
        }

        private static readonly Def[] Pool =
        {
            new() { desc = "Clear any story level without taking damage", reward = 3,
                check = (m, won, dmg, t, combo, wave) => m == LaunchMode.Story && won && dmg <= 0f },
            new() { desc = "Win any duel in under 60 seconds", reward = 2,
                check = (m, won, dmg, t, combo, wave) => m == LaunchMode.Duel && won && t < 60f },
            new() { desc = "Cut through 8 packs on the Road North", reward = 2,
                check = (m, won, dmg, t, combo, wave) => m == LaunchMode.Endless && wave >= 8 },
            new() { desc = "Land a 15-hit thread in any mission", reward = 2,
                check = (m, won, dmg, t, combo, wave) => combo >= 15 },
            new() { desc = "Clear any story level in under 90 seconds", reward = 2,
                check = (m, won, dmg, t, combo, wave) => m == LaunchMode.Story && won && t < 90f },
        };

        private static string TodayKey => DateTime.Now.ToString("yyyyMMdd");

        public static Def Today => Pool[Math.Abs(TodayKey.GetHashCode()) % Pool.Length];

        public static bool DoneToday => PlayerPrefs.GetString("daily_done", "") == TodayKey;

        /// <summary>Returns shards awarded (0 if not completed or already done).</summary>
        public static int Evaluate(LaunchMode mode, bool won, float damageTaken,
            float time, int maxCombo, int waveReached)
        {
            if (DoneToday) return 0;
            var d = Today;
            if (!d.check(mode, won, damageTaken, time, maxCombo, waveReached)) return 0;
            PlayerPrefs.SetString("daily_done", TodayKey);
            SkillTree.Shards += d.reward;
            return d.reward;
        }
    }
}
