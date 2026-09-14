using System.Linq;
using UnityEngine;
using Emberline.Enemies;

namespace Emberline.Core
{
    public enum LaunchMode { None, Story, Duel, Endless }

    /// <summary>
    /// What a level asks of you. Clear and Hold are the original two; Stealth,
    /// Escort and Chase are the mission types added on top. GameManager owns the
    /// win/lose rule for each, so a level only has to declare its intent here.
    /// </summary>
    public enum MissionObjective { Clear, Hold, Stealth, Escort, Chase }

    /// <summary>One story level: a named, authored encounter with narrative.</summary>
    public class LevelDef
    {
        public int id;
        public string name;
        public string story;      // one-line narrative shown on the briefing
        public bool marsh;        // which arena scene
        public EnemyKind[][] waves;

        /// <summary>Mission rule. Hold is implied when holdSeconds &gt; 0.</summary>
        public MissionObjective objective = MissionObjective.Clear;

        /// <summary>Escort: seconds the bearer needs to walk the road end to end.</summary>
        public float escortSeconds = 60f;

        /// <summary>
        /// Resources/Missions asset name for this level's staged plan. When set,
        /// the MissionDirector runs the mission and the wave list is unused.
        /// </summary>
        public string planAsset = "";

        /// <summary>Briefing dialogue, "SPEAKER|line". Shown with portraits before the fight.</summary>
        public string[] dialogue = System.Array.Empty<string>();

        /// <summary>Cliffhanger shown on the victory screen.</summary>
        public string debrief = "";

        /// <summary>&gt; 0: survive this many seconds against streaming waves instead of clearing them.</summary>
        public float holdSeconds;
    }

    /// <summary>
    /// An optional handicap chosen on the duel briefing. Harder terms pay more
    /// shards, so a duel you've already won stays worth replaying.
    /// </summary>
    public class DuelModifier
    {
        public string name;
        public string desc;
        public float bossHpMul = 1f;
        public float bossSpeedMul = 1f;
        public float playerHpMul = 1f;
        public int bonusShards;
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
        /// <summary>Named foe def (Resources/Enemies) on `kind`'s body; empty for the kind itself.</summary>
        public string defId = "";
        /// <summary>
        /// The campaign mission where this villain is first met. The duel stays
        /// locked until that mission is cleared: a duel against someone the
        /// player has never seen is a menu entry, not a grudge.
        /// </summary>
        public int storyMission;

        // ---- Duel identity (Duel overhaul). 0 = fall back to the generic floor.
        [System.Serializable] public class Tuning { }
        /// <summary>The opponent's fighting philosophy, shown on the briefing.</summary>
        public string philosophy = "";
        /// <summary>HP for this duel. Tuned per opponent for the length targets.</summary>
        public float hp;
        /// <summary>Posture pool — the meter the fight is really about. Earned, not chipped in three hits.</summary>
        public float posture = 120f;
        public float postureRegen = 7f;
        /// <summary>HP damage taken from ordinary swings (outside the guard-break punish). Low = posture-paced.</summary>
        public float dmgResist = 0.28f;
        /// <summary>Arena mood: lighting theme + weather, so each duel is its own place.</summary>
        public EnvThemeId theme = EnvThemeId.Village;
        public bool night, rain, fog;
        /// <summary>Short intro/defeat lines, "SPEAKER|text", shown through the story dialogue box.</summary>
        public string[] intro = System.Array.Empty<string>();
        public string[] defeat = System.Array.Empty<string>();
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
        // Weapon-defined raiders: axe bruiser, pike guard, bomber.
        private const EnemyKind A = EnemyKind.RaiderAxe;
        private const EnemyKind P = EnemyKind.PikeGuard;
        private const EnemyKind O = EnemyKind.Bomber;

        /// <summary>Act title for a level id — shown on briefings and the level select.</summary>
        public static string ActName(int levelId) => Campaign.Campaign.ActName(levelId);

        /// <summary>
        /// The story catalogue is the hundred-mission campaign. The ten levels
        /// that used to live here were the first draft of the same story; they
        /// survive as the bespoke plans for the missions they became.
        /// </summary>
        public static LevelDef[] Story => Campaign.Campaign.Levels;

        // ------------------------------------------------------- duel catalog

        public static readonly DuelDef[] Duels =
        {
            new() { id = 1, name = "GORO", title = "THE TOLL-CAPTAIN", kind = EnemyKind.Chief, marsh = false,
                taunt = "“Every roof pays. Even yours, little lantern.”",
                philosophy = "POWER · PRESSURE · COMMITMENT",
                storyMission = 5, hp = 320f, posture = 110f, postureRegen = 9f, dmgResist = 0.30f,
                theme = EnvThemeId.BurningVillage, night = true,
                intro = new[] {
                    "GORO|You came up the toll road on your own feet. Brave. Stupid.",
                    "RENZO|Move, or be moved.",
                    "GORO|Boys who swing first bleed first. Come on, then." },
                defeat = new[] {
                    "GORO|…heh. You waited. Your father… never learned that.",
                    "GORO|The serpent has her. Kagehira. Go north… if you still can." } },
            new() { id = 2, name = "THE PALE SHADE", title = "WHAT THE MARSH KEPT", kind = EnemyKind.Shade, marsh = true,
                defId = "paleshade", taunt = "“…come closer…”",
                philosophy = "SPEED · DECEPTION · POSITIONING",
                storyMission = 21, hp = 350f, posture = 140f, postureRegen = 13f, dmgResist = 0.28f,
                theme = EnvThemeId.Graveyard, fog = true, night = true,
                intro = new[] {
                    "PALE SHADE|…you carry her thread… the girl who tied it still breathes…",
                    "RENZO|Where. Say it.",
                    "PALE SHADE|…catch me, and I will tell you where the marsh took her…" },
                defeat = new[] {
                    "PALE SHADE|…she lives… north, past the drowned road… find her before he does…",
                    "RENZO|Aiko." } },
            new() { id = 3, name = "JIN KUROGANE", title = "THE STORM BLADE", kind = EnemyKind.Jin, marsh = false,
                taunt = "“Attachments slow the sword. I cut mine away. Show me why you keep yours.”",
                philosophy = "TECHNIQUE · COUNTERS · ADAPTATION",
                storyMission = 61, hp = 440f, posture = 175f, postureRegen = 12f, dmgResist = 0.28f,
                theme = EnvThemeId.RainyBattlefield, rain = true,
                intro = new[] {
                    "JIN|I have watched you fight. You repeat yourself.",
                    "RENZO|And you talk.",
                    "JIN|Then teach me something new. I will teach you what you are becoming." },
                defeat = new[] {
                    "JIN|Better. You changed. He never did — and you are walking his road.",
                    "JIN|Reach Kagehira, and look at him. That is your ending, unless you choose another." } },
            new() { id = 4, name = "KAGACHI", title = "THE SERPENT, KAGEHIRA", kind = EnemyKind.Kagachi, marsh = true,
                taunt = "“Three lives, ninja. How many do you have?”",
                philosophy = "MASTERY · EVERYTHING YOU HAVE LEARNED",
                storyMission = 88, hp = 560f, posture = 210f, postureRegen = 11f, dmgResist = 0.26f,
                theme = EnvThemeId.Temple, night = true, fog = true,
                intro = new[] {
                    "KAGACHI|The Kurogawa boy. You have your father's eyes. I closed his.",
                    "RENZO|You took everything.",
                    "KAGACHI|And you have come to take it back with a sword. How like him. Show me." },
                defeat = new[] {
                    "KAGACHI|…so this is where it ends. Finish it, then. Become me.",
                    "RENZO|No. I am not you." } },
            // Campaign foes, for the roster the brief asks for: bosses and elites
            // from the hundred missions, on the bodies they used there.
            new() { id = 5, name = "THE CONVOY CAPTAIN", title = "KEEPER OF THE LANTERN ROAD", kind = EnemyKind.Samurai,
                defId = "convoycaptain", marsh = false, taunt = "“Everything on this road is counted. You were not.”",
                philosophy = "DISCIPLINE · FORMATION · ATTRITION",
                storyMission = 12, hp = 340f, posture = 125f, postureRegen = 10f, dmgResist = 0.30f,
                theme = EnvThemeId.Village, night = true,
                intro = new[] {
                    "CONVOY CAPTAIN|Four provinces of steel, and one thief on the crate.",
                    "RENZO|Not a thief. A reader. Your ledger names a village.",
                    "CONVOY CAPTAIN|Then you have read your last page." },
                defeat = new[] {
                    "CONVOY CAPTAIN|Counted… every wagon… never counted you.",
                    "RENZO|Nobody does." } },
            new() { id = 6, name = "THE THREE BLADES", title = "SISTERS OF THE SILENT FOREST", kind = EnemyKind.Assassin,
                defId = "threeblades", marsh = false, taunt = "“One for the throat. One for the heart. One to watch.”",
                philosophy = "AMBUSH · ROTATION · PATIENCE",
                storyMission = 24, hp = 370f, posture = 150f, postureRegen = 12f, dmgResist = 0.30f,
                theme = EnvThemeId.Forest, night = true, fog = true,
                intro = new[] {
                    "BLADE|Three of us walked into your forest, Kurogawa.",
                    "RENZO|One of you walks out. Choose.",
                    "BLADE|We already did. The one who watches." },
                defeat = new[] {
                    "BLADE|…the sisters… will count you… among the trees…",
                    "RENZO|Let them count." } },
            new() { id = 7, name = "THE DROWNED GUARDIAN", title = "WARDEN OF THE SECOND KEY", kind = EnemyKind.EliteWarrior,
                defId = "drownedguardian", marsh = true, taunt = "“Your father set me here. He did not say you would come.”",
                philosophy = "ENDURANCE · REACH · REFUSAL",
                storyMission = 59, hp = 420f, posture = 165f, postureRegen = 9f, dmgResist = 0.34f,
                theme = EnvThemeId.Graveyard, fog = true, rain = true,
                intro = new[] {
                    "DROWNED GUARDIAN|The water keeps what it is given. He gave it a key, and me.",
                    "RENZO|Then he meant for me to take it back.",
                    "DROWNED GUARDIAN|He meant for no one to. Come and drown." },
                defeat = new[] {
                    "DROWNED GUARDIAN|…the key is yours… so is the water…",
                    "RENZO|I've been under it before." } },
            new() { id = 8, name = "THE IRON GUARD", title = "KAGEHIRA'S SHIELD", kind = EnemyKind.EliteWarrior,
                defId = "ironguard", marsh = false, taunt = "“The warlord does not see you. I make sure of it.”",
                philosophy = "GUARD · PUNISHMENT · NO GROUND GIVEN",
                storyMission = 74, hp = 500f, posture = 200f, postureRegen = 9f, dmgResist = 0.36f,
                theme = EnvThemeId.Mountain, night = true,
                intro = new[] {
                    "IRON GUARD|Nine gates. Nine men like me. You have found the first.",
                    "RENZO|Then eight more will hear how this went.",
                    "IRON GUARD|Nothing behind this shield has ever heard anything." },
                defeat = new[] {
                    "IRON GUARD|…the shield… falls… he will not… look up…",
                    "RENZO|He will." } },
            new() { id = 9, name = "COMMANDER HOSHU", title = "THE INNER GATE", kind = EnemyKind.Samurai,
                defId = "finalcommander", marsh = false, taunt = "“He said you would reach this door. He did not say you would open it.”",
                philosophy = "COMMAND · TIMING · THE LAST DOOR",
                storyMission = 66, hp = 470f, posture = 185f, postureRegen = 11f, dmgResist = 0.30f,
                theme = EnvThemeId.Fortress, night = true,
                intro = new[] {
                    "COMMANDER HOSHU|I have held this door for eleven years. You are not the first Kurogawa to reach it.",
                    "RENZO|I'm the last.",
                    "COMMANDER HOSHU|Then let it end properly. Draw." },
                defeat = new[] {
                    "COMMANDER HOSHU|…properly… yes. Go through, then. He is waiting.",
                    "RENZO|He has been for a long time." } },
        };

        // ---------------------------------------------------- duel modifiers

        public static readonly DuelModifier[] DuelModifiers =
        {
            new() { name = "EVEN TERMS", desc = "No handicap. Blade against blade." },
            new() { name = "IRON WILL", desc = "They carry 45% more life.",
                bossHpMul = 1.45f, bonusShards = 1 },
            new() { name = "STORM PACE", desc = "They move a third faster.",
                bossSpeedMul = 1.33f, bonusShards = 1 },
            new() { name = "ONE BREATH", desc = "Half your life. Nothing else changes.",
                playerHpMul = 0.5f, bonusShards = 2 },
        };

        /// <summary>Chosen duel handicap, remembered between sessions.</summary>
        public static int DuelModifierIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("duel_mod", 0), 0, DuelModifiers.Length - 1);
            set
            {
                PlayerPrefs.SetInt("duel_mod", Mathf.Clamp(value, 0, DuelModifiers.Length - 1));
                PlayerPrefs.Save();
            }
        }

        public static DuelModifier CurrentDuelModifier => DuelModifiers[DuelModifierIndex];

        /// <summary>Name card + taunt for story-mode boss intros (duels carry their own).</summary>
        public static (string name, string title, string taunt) BossCard(EnemyKind kind) => kind switch
        {
            EnemyKind.Chief => ("GORO", "THE TOLL-CAPTAIN",
                "“Every roof pays. Even yours, little lantern.”"),
            EnemyKind.Kagachi => ("KAGACHI", "THE MARSH SERPENT",
                "“Three lives, ninja. How many do you have?”"),
            EnemyKind.Jin => ("JIN KUROGANE", "THE STORM BLADE",
                "“Attachments slow the sword. Show me why you keep yours.”"),
            _ => (null, null, null),
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

        /// <summary>
        /// A duel opens once its villain has been met in the campaign — the
        /// mission they first appear in is cleared — or once the story is done.
        /// The roster follows the story; it does not gate itself.
        /// </summary>
        public static bool IsDuelUnlocked(DuelDef d) =>
            d != null && (NewGamePlus || StoryUnlocked > d.storyMission);

        /// <summary>The roster in the order the story introduces them.</summary>
        public static DuelDef[] DuelsInStoryOrder =>
            Duels.OrderBy(d => d.storyMission).ThenBy(d => d.id).ToArray();

        public static int DuelsUnlocked => Duels.Count(IsDuelUnlocked);

        public static bool DuelWon(int duelId) => PlayerPrefs.GetInt($"duel_won_{duelId}", 0) == 1;

        /// <summary>Unlocked by finishing Level 10. Enemies hit harder; duels turn nightmare.</summary>
        public static bool NewGamePlus
        {
            get => PlayerPrefs.GetInt("ngplus", 0) == 1;
            set { PlayerPrefs.SetInt("ngplus", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void SaveDuelWin(int duelId)
        {
            PlayerPrefs.SetInt($"duel_won_{duelId}", 1);
            PlayerPrefs.Save();
        }
    }
}
