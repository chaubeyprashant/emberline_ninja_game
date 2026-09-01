using UnityEngine;

namespace Emberline.Endless
{
    /// <summary>
    /// Live counters for the march in progress plus the persisted records. Score
    /// is the headline number: it folds distance, encounters, kills and combo
    /// into one figure and then applies the modifier wager, so a short brutal run
    /// can beat a long safe one.
    /// </summary>
    public static class RunStats
    {
        // ---- live run ------------------------------------------------------

        public static int Score { get; private set; }
        public static int Depth { get; private set; }        // encounters cleared
        public static int Kills { get; private set; }
        public static int BestCombo { get; private set; }
        public static int CurrentCombo { get; private set; }
        public static int CombosOver10 { get; private set; }
        public static float Time { get; private set; }
        public static int Distance { get; private set; }
        public static int RyoEarned { get; private set; }
        public static int ShardsEarned { get; private set; }

        /// <summary>True when the run just beat a record — drives the results card.</summary>
        public static bool NewScoreRecord { get; private set; }
        public static bool NewDepthRecord { get; private set; }
        public static bool NewTimeRecord { get; private set; }

        public static void Begin()
        {
            Score = Depth = Kills = BestCombo = CurrentCombo = CombosOver10 = 0;
            Distance = RyoEarned = ShardsEarned = 0;
            Time = 0f;
            NewScoreRecord = NewDepthRecord = NewTimeRecord = false;
        }

        public static void Tick(float dt, int distance)
        {
            Time += dt;
            Distance = distance;
        }

        public static void OnKill()
        {
            Kills++;
            CurrentCombo++;
            if (CurrentCombo > BestCombo) BestCombo = CurrentCombo;
            if (CurrentCombo == 10) CombosOver10++;
        }

        /// <summary>A dropped combo is the price of getting hit, not of a pause.</summary>
        public static void BreakCombo() => CurrentCombo = 0;

        public static void AddScore(int points)
        {
            if (points > 0) Score += points;
        }

        /// <summary>
        /// Encounter clear payout: the encounter's base value, scaled by depth and
        /// by how clean the fight was, then by the modifier wager.
        /// </summary>
        public static void OnEncounterCleared(EncounterDef def)
        {
            Depth++;
            var depthScale = 1f + Depth * 0.08f;
            var comboBonus = 1f + Mathf.Min(0.5f, BestCombo * 0.02f);
            // Difficulty scores like a modifier does: a Lethal depth-10 run must
            // not read the same on the board as an Easy one.
            AddScore(Mathf.RoundToInt(def.ScoreValue * depthScale * comboBonus
                                      * RunModifiers.ActiveScoreMultiplier
                                      * Core.Difficulty.Now.Score));
        }

        public static void EarnRyo(int amount)
        {
            if (amount <= 0) return;
            RyoEarned += amount;
            Core.Wallet.Earn(amount);
        }

        public static void EarnShard(int amount = 1)
        {
            if (amount <= 0) return;
            ShardsEarned += amount;
            Core.SkillTree.Shards += amount;
        }

        // ---- records -------------------------------------------------------

        public static int BestScore => PlayerPrefs.GetInt("run_best_score", 0);
        public static int BestDepth => PlayerPrefs.GetInt("run_best_depth", 0);
        public static int BestTime => PlayerPrefs.GetInt("run_best_time", 0);
        public static int BestKills => PlayerPrefs.GetInt("run_best_kills", 0);
        public static int BestComboEver => PlayerPrefs.GetInt("run_best_combo", 0);
        public static int TotalRuns => PlayerPrefs.GetInt("run_count", 0);
        public static int TotalKills => PlayerPrefs.GetInt("run_total_kills", 0);

        /// <summary>Commit the run to the record book. Safe to call once per run.</summary>
        public static void Commit()
        {
            PlayerPrefs.SetInt("run_count", TotalRuns + 1);
            PlayerPrefs.SetInt("run_total_kills", TotalKills + Kills);

            if (Score > BestScore) { PlayerPrefs.SetInt("run_best_score", Score); NewScoreRecord = true; }
            if (Depth > BestDepth) { PlayerPrefs.SetInt("run_best_depth", Depth); NewDepthRecord = true; }
            var secs = Mathf.RoundToInt(Time);
            if (secs > BestTime) { PlayerPrefs.SetInt("run_best_time", secs); NewTimeRecord = true; }
            if (Kills > BestKills) PlayerPrefs.SetInt("run_best_kills", Kills);
            if (BestCombo > BestComboEver) PlayerPrefs.SetInt("run_best_combo", BestCombo);
            PlayerPrefs.Save();
        }

        public static string TimeText(int seconds) => $"{seconds / 60}:{seconds % 60:00}";
    }
}
