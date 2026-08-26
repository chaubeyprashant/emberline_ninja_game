using UnityEngine;
using Emberline.Enemies;

namespace Emberline.Core
{
    public enum LaunchMode { None, Story, Duel, Endless }

    /// <summary>One story level: a named, authored encounter with narrative.</summary>
    public class LevelDef
    {
        public int id;
        public string name;
        public string story;      // one-line narrative shown on the briefing
        public bool marsh;        // which arena scene
        public EnemyKind[][] waves;
    }

    /// <summary>One duel opponent: 1v1, full HP, distinct kit.</summary>
    public class DuelDef
    {
        public int id;
        public string name;
        public string title;
        public string taunt;
        public bool marsh;
        public EnemyKind kind;
    }

    /// <summary>
    /// Cross-scene launch state + story/duel catalogs + saved progression.
    /// The scenes are theme shells; this decides what actually spawns in them.
    /// </summary>
    public static class Session
    {
        public static LaunchMode Mode = LaunchMode.None; // None → main menu
        public static int LevelIndex;
        public static int DuelIndex;

        // ------------------------------------------------------ story catalog

        private static EnemyKind[] W(params EnemyKind[] k) => k;
        private const EnemyKind B = EnemyKind.Bandit;
        private const EnemyKind R = EnemyKind.Ranged;
        private const EnemyKind S = EnemyKind.Shade;
        private const EnemyKind C = EnemyKind.Chief;
        private const EnemyKind K = EnemyKind.Kagachi;

        public static readonly LevelDef[] Story =
        {
            new() { id = 1, name = "FIRST BLOOD", marsh = false,
                story = "Raiders hit the Yorune terraces at dusk. Renzo is the only blade on the roof.",
                waves = new[] { W(B, B), W(B, B, B) } },
            new() { id = 2, name = "THE LANTERN ROAD", marsh = false,
                story = "They came for the lantern oil. Hold the road until the bells ring.",
                waves = new[] { W(B, B, B), W(B, B, R) } },
            new() { id = 3, name = "EYES IN THE DARK", marsh = false,
                story = "Something faster than a bandit moves between the chimneys. It does not blink.",
                waves = new[] { W(S, S), W(B, B, S, R) } },
            new() { id = 4, name = "GORO'S TOLL", marsh = false,
                story = "The raiders have a captain. Goro collects a toll from every rooftop — tonight he collects from you.",
                waves = new[] { W(B, B, R), W(C, B, B) } },
            new() { id = 5, name = "THE SERPENT'S TRAIL", marsh = false,
                story = "A survivor whispers one word before he faints: 'Ashfen.' The trail leads into the marsh.",
                waves = new[] { W(B, B, S, R), W(S, S, B, B, R, R) } },
            new() { id = 6, name = "INTO THE REEDS", marsh = true,
                story = "The marsh swallows sound. The reeds are full of shades that were people once.",
                waves = new[] { W(S, S, S), W(S, S, R) } },
            new() { id = 7, name = "THE DROWNED ROAD", marsh = true,
                story = "Merchants' carts sit sunk to the axle. Whatever stopped them is still hungry.",
                waves = new[] { W(B, B, S, S), W(B, S, R, R) } },
            new() { id = 8, name = "TWIN LANTERNS", marsh = true,
                story = "Two toll-captains guard the crossing. Their lanterns burn a color fire should not be.",
                waves = new[] { W(S, S, R), W(C, C) } },
            new() { id = 9, name = "THE SERPENT'S GUARD", marsh = true,
                story = "The serpent's chosen bar the last bridge. Behind them, the water is perfectly still.",
                waves = new[] { W(S, S, S, R, R), W(C, S, S, B, B) } },
            new() { id = 10, name = "KAGACHI", marsh = true,
                story = "The Marsh Serpent rises. Three lives, they say — the duel, the mirrors, the desperation.",
                waves = new[] { W(K) } },
        };

        // ------------------------------------------------------- duel catalog

        public static readonly DuelDef[] Duels =
        {
            new() { id = 1, name = "GORO", title = "THE TOLL-CAPTAIN", kind = EnemyKind.Chief, marsh = false,
                taunt = "“Every roof pays. Even yours, little lantern.”" },
            new() { id = 2, name = "THE PALE SHADE", title = "WHAT THE MARSH KEPT", kind = EnemyKind.Shade, marsh = true,
                taunt = "“…come closer…”" },
            new() { id = 3, name = "KAGACHI", title = "THE MARSH SERPENT", kind = EnemyKind.Kagachi, marsh = true,
                taunt = "“Three lives, ninja. How many do you have?”" },
            new() { id = 4, name = "JIN KUROGANE", title = "THE STORM BLADE", kind = EnemyKind.Jin, marsh = false,
                taunt = "“Attachments slow the sword. I cut mine away. Show me why you keep yours.”" },
        };

        // -------------------------------------------------------- progression

        public static int StoryUnlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("story_unlocked", 1), 1, Story.Length);
            set { PlayerPrefs.SetInt("story_unlocked", Mathf.Max(StoryUnlocked, value)); PlayerPrefs.Save(); }
        }

        public static int Stars(int levelId) => PlayerPrefs.GetInt($"story_stars_{levelId}", 0);

        public static void SaveStars(int levelId, int stars)
        {
            if (stars > Stars(levelId)) PlayerPrefs.SetInt($"story_stars_{levelId}", stars);
            PlayerPrefs.Save();
        }

        public static int TotalStars
        {
            get { var t = 0; foreach (var l in Story) t += Stars(l.id); return t; }
        }

        public static int DuelsUnlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("duels_unlocked", 1), 1, Duels.Length);
            set { PlayerPrefs.SetInt("duels_unlocked", Mathf.Max(DuelsUnlocked, value)); PlayerPrefs.Save(); }
        }

        public static bool DuelWon(int duelId) => PlayerPrefs.GetInt($"duel_won_{duelId}", 0) == 1;

        public static void SaveDuelWin(int duelId)
        {
            PlayerPrefs.SetInt($"duel_won_{duelId}", 1);
            PlayerPrefs.Save();
        }
    }
}
