using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Achievements ("feats"), persisted in PlayerPrefs. Evaluated by
    /// GameManager at mission end; each unlock pays one Ember Shard and is
    /// shown in the Codex. Checks receive a mission summary snapshot.
    /// </summary>
    public static class Feats
    {
        public class MissionSummary
        {
            public LaunchMode mode;
            public bool won;
            public int levelId;       // 0 outside story
            public float damageTaken;
            public int maxCombo;
            public int waveReached;
            public int postsIntact;   // unbroken lantern posts at mission end
            public float timeSeconds;
            public bool alarmRaised;  // stealth: were you spotted
            public float escortHealth01 = 1f; // escort: bearer's remaining health
            public int deflects;
            public int wallRuns;
        }

        public class Feat
        {
            public string id, title, desc;
            public System.Func<MissionSummary, bool> check;
        }

        public static readonly List<Feat> All = new()
        {
            new Feat { id = "first_blood", title = "FIRST BLOOD",
                desc = "Clear Level 1.",
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 1 },
            new Feat { id = "road_keeper", title = "ROAD KEEPER",
                desc = "Win 'The Lantern Road' with 3+ lanterns still burning.",
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 2 && s.postsIntact >= 3 },
            new Feat { id = "perfect_duelist", title = "PERFECT DUELIST",
                desc = "Win a duel untouched.",
                check = s => s.mode == LaunchMode.Duel && s.won && s.damageTaken <= 0f },
            new Feat { id = "combo_master", title = "COMBO MASTER",
                desc = "Weave a 50-hit thread.",
                check = s => s.maxCombo >= 50 },
            new Feat { id = "serpent_slayer", title = "SERPENT SLAYER",
                desc = "Close the gate. Beat Level 10.",
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 10 },
            new Feat { id = "mist_walker", title = "MIST WALKER",
                desc = "Cut through 15 packs on the Road North.",
                check = s => s.mode == LaunchMode.Endless && s.waveReached >= 15 },
            new Feat { id = "ghost_walk", title = "GHOST WALK",
                desc = "Clear 'Eyes in the Dark' without ever being seen.",
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 3 && !s.alarmRaised },
            new Feat { id = "lantern_shepherd", title = "LANTERN SHEPHERD",
                desc = "Walk Yotsu home with the flame above 80%.",
                check = s => s.mode == LaunchMode.Story && s.won && s.levelId == 2
                             && s.escortHealth01 >= 0.8f },
            new Feat { id = "iron_answer", title = "IRON ANSWER",
                desc = "Deflect 5 attacks in a single mission.",
                check = s => s.deflects >= 5 },
            new Feat { id = "roofrunner", title = "ROOFRUNNER",
                desc = "Run three walls in a single mission.",
                check = s => s.wallRuns >= 3 },
        };

        public static bool Has(string id) => PlayerPrefs.GetInt("feat_" + id, 0) == 1;

        public static int Count
        {
            get { var n = 0; foreach (var f in All) if (Has(f.id)) n++; return n; }
        }

        /// <summary>Returns newly earned feats (already persisted + shard paid).</summary>
        public static List<Feat> Evaluate(MissionSummary s)
        {
            var earned = new List<Feat>();
            foreach (var f in All)
            {
                if (Has(f.id) || !f.check(s)) continue;
                PlayerPrefs.SetInt("feat_" + f.id, 1);
                SkillTree.Shards += 1;
                earned.Add(f);
            }
            if (earned.Count > 0) PlayerPrefs.Save();
            return earned;
        }
    }

    /// <summary>
    /// Cosmetic blade finishes unlocked by total stars: recolors the weapon prop
    /// and slash trail. No stats — pure flair.
    /// </summary>
    public static class BladeFinish
    {
        public class Finish
        {
            public string name;
            public int starsRequired;
            public Color blade, trail;
        }

        public static readonly Finish[] All =
        {
            new() { name = "STEEL", starsRequired = 0,
                blade = new Color(0.55f, 0.58f, 0.64f), trail = new Color(0.85f, 0.93f, 1f) },
            new() { name = "EMBER", starsRequired = 9,
                blade = new Color(0.85f, 0.42f, 0.25f), trail = new Color(1f, 0.55f, 0.3f) },
            new() { name = "MOONLIT", starsRequired = 18,
                blade = new Color(0.62f, 0.72f, 0.95f), trail = new Color(0.7f, 0.85f, 1f) },
            new() { name = "SERPENT", starsRequired = 27,
                blade = new Color(0.30f, 0.75f, 0.55f), trail = new Color(0.45f, 0.95f, 0.6f) },
            new() { name = "DROWNED GOLD", starsRequired = 36,
                blade = new Color(0.82f, 0.68f, 0.32f), trail = new Color(1f, 0.86f, 0.45f) },
        };

        public static bool IsUnlocked(Finish f) => Session.TotalStars >= f.starsRequired;

        public static Finish Current
        {
            get
            {
                var i = PlayerPrefs.GetInt("blade_finish", 0);
                i = Mathf.Clamp(i, 0, All.Length - 1);
                return IsUnlocked(All[i]) ? All[i] : All[0];
            }
        }

        public static void CycleNext()
        {
            var i = PlayerPrefs.GetInt("blade_finish", 0);
            for (var step = 1; step <= All.Length; step++)
            {
                var next = (i + step) % All.Length;
                if (IsUnlocked(All[next])) { PlayerPrefs.SetInt("blade_finish", next); break; }
            }
            PlayerPrefs.Save();
        }
    }
}
