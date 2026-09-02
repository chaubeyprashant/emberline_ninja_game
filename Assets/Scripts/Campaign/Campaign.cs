using System.Collections.Generic;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.Campaign
{
    /// <summary>
    /// The hundred-mission campaign: ten chapters in three acts, authored as one
    /// table so the whole arc can be read top to bottom. Everything the rest of
    /// the game needs — the level catalogue, the chapter select, the "why the
    /// next mission follows" line on the results screen — is derived from here.
    /// </summary>
    public static partial class Campaign
    {
        public const int Count = 100;

        public static readonly Chapter[] Chapters =
        {
            new() { number = 1, name = "ASHES OF YORUNE", theme = "Return, mystery, first clues", firstMission = 1, lastMission = 10, act = 1, region = Region.Ruins },
            new() { number = 2, name = "THE LANTERN NETWORK", theme = "Kagehira's larger operation", firstMission = 11, lastMission = 20, act = 1, region = Region.Villages },
            new() { number = 3, name = "THE SILENT FOREST", theme = "Assassins, tracking and pressure", firstMission = 21, lastMission = 30, act = 1, region = Region.Forest },
            new() { number = 4, name = "GORO'S TERRITORY", theme = "War, prisoners and Goro", firstMission = 31, lastMission = 40, act = 2, region = Region.Mountains },
            new() { number = 5, name = "INTO THE MARSH", theme = "Isolation and the Black Seal", firstMission = 41, lastMission = 50, act = 2, region = Region.Marsh },
            new() { number = 6, name = "THE DROWNED TEMPLE", theme = "Renzo's family history", firstMission = 51, lastMission = 60, act = 2, region = Region.Temples },
            new() { number = 7, name = "KUROGANE", theme = "Jin and Renzo's rivalry", firstMission = 61, lastMission = 70, act = 2, region = Region.Villages },
            new() { number = 8, name = "THE IRON FORTRESS", theme = "The final approach", firstMission = 71, lastMission = 80, act = 3, region = Region.Snow },
            new() { number = 9, name = "THE BLACK SEAL", theme = "Truth and payoff", firstMission = 81, lastMission = 90, act = 3, region = Region.Stronghold },
            new() { number = 10, name = "THE SERPENT'S END", theme = "War, revenge and choice", firstMission = 91, lastMission = 100, act = 3, region = Region.Seal },
        };

        public static readonly string[] ActNames =
        {
            "ACT I — THE RETURN", "ACT II — THE HUNT", "ACT III — THE END",
        };

        public static CampaignMission Get(int id) =>
            id >= 1 && id <= Missions.Length ? Missions[id - 1] : null;

        public static Chapter ChapterOf(int missionId)
        {
            foreach (var c in Chapters)
                if (missionId >= c.firstMission && missionId <= c.lastMission) return c;
            return Chapters[0];
        }

        public static string ActName(int missionId) => ActNames[ChapterOf(missionId).act - 1];

        /// <summary>How many missions carry each type as their primary.</summary>
        public static Dictionary<GameplayType, int> Distribution(bool primaryOnly = true)
        {
            var d = new Dictionary<GameplayType, int>();
            foreach (var m in Missions)
            {
                if (primaryOnly) { d[m.Primary] = d.TryGetValue(m.Primary, out var n) ? n + 1 : 1; continue; }
                foreach (var t in m.types) d[t] = d.TryGetValue(t, out var k) ? k + 1 : 1;
            }
            return d;
        }

        private static LevelDef[] _levels;

        /// <summary>
        /// The campaign as the level catalogue the rest of the game already
        /// understands. Built once; the story select, unlocks, stars, briefing
        /// and results all read this.
        /// </summary>
        public static LevelDef[] Levels
        {
            get
            {
                if (_levels != null) return _levels;
                _levels = new LevelDef[Missions.Length];
                for (var i = 0; i < Missions.Length; i++)
                {
                    var m = Missions[i];
                    _levels[i] = new LevelDef
                    {
                        id = m.id,
                        name = m.name,
                        story = m.storyPurpose,
                        marsh = m.marsh,
                        // Fallback waves for a plan that fails to load: two
                        // authored packs drawn from the mission's own roster.
                        waves = FallbackWaves(m),
                        objective = MissionObjective.Clear,
                        planAsset = m.PlanAsset,
                        dialogue = m.dialogue,
                        debrief = m.ending,
                    };
                }
                return _levels;
            }
        }

        private static EnemyKind[][] FallbackWaves(CampaignMission m)
        {
            if (m.enemies.Length == 0) return new[] { new[] { EnemyKind.Bandit } };
            var a = new List<EnemyKind>();
            var b = new List<EnemyKind>();
            for (var i = 0; i < m.enemies.Length; i++) (i % 2 == 0 ? a : b).Add(m.enemies[i]);
            if (b.Count == 0) b.Add(m.enemies[0]);
            return new[] { a.ToArray(), b.ToArray() };
        }
    }
}
