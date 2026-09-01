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
            public Func<Feats.MissionSummary, bool> check;
        }

        // Shares Feats.MissionSummary rather than a positional signature: the two
        // systems ask the same questions of the same mission, and the summary is
        // what lets a challenge reference stealth or deflects at all.
        private static readonly Def[] Pool =
        {
            new() { desc = "Clear any story level without taking damage", reward = 3,
                check = s => s.mode == LaunchMode.Story && s.won && s.damageTaken <= 0f },
            new() { desc = "Win any duel in under 60 seconds", reward = 2,
                check = s => s.mode == LaunchMode.Duel && s.won && s.timeSeconds < 60f },
            new() { desc = "Cut through 8 packs on the Road North", reward = 2,
                check = s => s.mode == LaunchMode.Endless && s.waveReached >= 8 },
            new() { desc = "Land a 15-hit thread in any mission", reward = 2,
                check = s => s.maxCombo >= 15 },
            new() { desc = "Clear any story level in under 90 seconds", reward = 2,
                check = s => s.mode == LaunchMode.Story && s.won && s.timeSeconds < 90f },
            new() { desc = "Deflect 4 attacks in a single mission", reward = 2,
                check = s => s.deflects >= 4 },
            new() { desc = "Take the dark road unseen — clear Level 3 without an alarm", reward = 3,
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 3 && !s.alarmRaised },
        };

        /// <summary>
        /// Weekly challenges: bigger asks, bigger payouts, one per ISO week. They
        /// lean on the endless run's own statistics, which is what makes a march
        /// worth taking on a specific week rather than any week.
        /// </summary>
        private static readonly Def[] WeeklyPool =
        {
            new() { desc = "Reach depth 12 on a single march", reward = 6,
                check = s => s.mode == LaunchMode.Endless && Endless.RunStats.Depth >= 12 },
            new() { desc = "Score 6000 on a single march", reward = 6,
                check = s => s.mode == LaunchMode.Endless && Endless.RunStats.Score >= 6000 },
            new() { desc = "March with two modifiers and reach depth 8", reward = 7,
                check = s => s.mode == LaunchMode.Endless && Endless.RunStats.Depth >= 8
                             && CountBits((int)Endless.RunModifiers.Active) >= 2 },
            new() { desc = "Kill 80 on a single march", reward = 6,
                check = s => s.mode == LaunchMode.Endless && Endless.RunStats.Kills >= 80 },
            new() { desc = "Land a 25-hit thread on the Road North", reward = 5,
                check = s => s.mode == LaunchMode.Endless && Endless.RunStats.BestCombo >= 25 },
        };

        private static string TodayKey => DateTime.Now.ToString("yyyyMMdd");

        private static string WeekKey
        {
            get
            {
                var now = DateTime.Now;
                var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
                var week = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);
                return $"{now.Year}W{week:00}";
            }
        }

        public static Def ThisWeek => WeeklyPool[Math.Abs(WeekKey.GetHashCode()) % WeeklyPool.Length];

        public static bool DoneThisWeek => PlayerPrefs.GetString("weekly_done", "") == WeekKey;

        /// <summary>Returns shards awarded for the weekly (0 if not earned).</summary>
        public static int EvaluateWeekly(Feats.MissionSummary summary)
        {
            if (DoneThisWeek) return 0;
            var d = ThisWeek;
            if (!d.check(summary)) return 0;
            PlayerPrefs.SetString("weekly_done", WeekKey);
            SkillTree.Shards += d.reward;
            return d.reward;
        }

        private static int CountBits(int v)
        {
            var n = 0;
            while (v != 0) { n += v & 1; v >>= 1; }
            return n;
        }

        public static Def Today => Pool[Math.Abs(TodayKey.GetHashCode()) % Pool.Length];

        public static bool DoneToday => PlayerPrefs.GetString("daily_done", "") == TodayKey;

        /// <summary>Returns shards awarded (0 if not completed or already done).</summary>
        public static int Evaluate(Feats.MissionSummary summary)
        {
            if (DoneToday) return 0;
            var d = Today;
            if (!d.check(summary)) return 0;
            PlayerPrefs.SetString("daily_done", TodayKey);
            SkillTree.Shards += d.reward;
            return d.reward;
        }
    }
}
