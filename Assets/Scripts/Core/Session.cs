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

        /// <summary>Kept for the endless and duel catalogues that still read it.</summary>
        public static readonly LevelDef[] LegacyStory =
        {
            new() { id = 1, name = "FIRST BLOOD", marsh = false,
                story = "Raiders hit the Yorune terraces at dusk. Renzo is the only blade on the roof.",
                dialogue = new[]
                {
                    "YOTSU|Renzo! Raiders on the east terraces — they're climbing the lantern lines!",
                    "RENZO|They picked the one road in Yorune that's mine to keep.",
                    "YOTSU|Keep it, then. But come back whole, boy. The night is long.",
                },
                debrief = "The raiders carried nothing away. Whatever they came for… they did not find it. Yet.",
                waves = new[] { W(B, B), W(B, B, B) },
                planAsset = "S01_FirstBlood" },

            new() { id = 2, name = "THE LANTERN ROAD", marsh = false,
                story = "Old Yotsu carries the flame to the temple. The road is yours to keep open.",
                dialogue = new[]
                {
                    "YOTSU|They're cutting the posts. Without light, the road belongs to them.",
                    "RENZO|Then walk. I'll keep the dark off your shoulders.",
                    "YOTSU|Slow old legs, boy. Don't let them reach me — this flame doesn't relight.",
                },
                debrief = "Yotsu reached the temple with the flame still lit. Among the ashes behind them, a raider's note: 'The old flame hangs at the guard's belt.'",
                objective = MissionObjective.Escort,
                escortSeconds = 62f,
                waves = new[] { W(B, B), W(B, P), W(B, R, P) },
                planAsset = "S02_LanternRoad" },

            new() { id = 3, name = "EYES IN THE DARK", marsh = false,
                story = "Something moves between the chimneys, and it has not seen you yet. Keep it that way.",
                dialogue = new[]
                {
                    "RENZO|Something's moving between the chimneys. Faster than any bandit.",
                    "YOTSU|Old stories say the marsh sends its drowned to fetch what it wants. Don't let them see the flame, boy.",
                    "RENZO|Then I'll put them out before they turn around.",
                },
                debrief = "The shades dissolved without a sound — reaching, until the very end, for the lantern.",
                objective = MissionObjective.Stealth,
                waves = new[] { W(S, S), W(B, B, S, R) },
                planAsset = "S03_EyesInTheDark" },

            new() { id = 4, name = "GORO'S TOLL", marsh = false,
                story = "The raiders have a captain. Tonight Goro collects from you.",
                dialogue = new[]
                {
                    "GORO|Every roof pays, little lantern. Tonight I collect.",
                    "RENZO|The flame was my father's. Come take his sword-arm too.",
                    "GORO|I take what the Serpent asks. Nothing personal.",
                },
                debrief = "Beaten, Goro laughed through broken teeth: 'It was never me who wanted it. Ashfen calls.'",
                waves = new[] { W(B, A, R), W(C, A, B) },
                planAsset = "S04_GorosToll" },

            new() { id = 5, name = "THE SERPENT'S TRAIL", marsh = true,
                story = "No merchant returns from Ashfen since the drownings. The trail leads in anyway.",
                dialogue = new[]
                {
                    "YOTSU|Ashfen marsh. No one returns from that road since the drownings.",
                    "RENZO|Goro said a serpent calls. A serpent can be cut.",
                    "YOTSU|Carry the lantern low. In the marsh, light draws more than moths.",
                },
                debrief = "Glowing footprints wind through the mud — lantern-bearers, marching somewhere unseen.",
                waves = new[] { W(B, P, S, R), W(S, O, B, B, R) },
                planAsset = "S05_SerpentsTrail" },

            new() { id = 6, name = "INTO THE REEDS", marsh = true,
                story = "The marsh swallows sound. The reeds are full of shades that were people once.",
                dialogue = new[]
                {
                    "WHISPER|…warm… so warm… give it to the water…",
                    "RENZO|These were people. Merchants. Someone drowned them all.",
                    "WHISPER|…the Serpent gathers the lights… come… be gathered…",
                },
                debrief = "Each shade fell reaching for the flame — not with hunger. With longing.",
                waves = new[] { W(S, S, S), W(S, S, O) },
                planAsset = "S06_IntoTheReeds" },

            new() { id = 7, name = "THE DROWNED ROAD", marsh = true,
                story = "Merchants' carts sit sunk to the axle. Every lantern is gone — nothing else was touched.",
                dialogue = new[]
                {
                    "RENZO|Carts sunk to the axle. Cargo untouched — except the lanterns. All gone.",
                    "WHISPER|…a hundred lights below the water… the gate must burn…",
                    "RENZO|Then I'm one light short of understanding. Show me.",
                },
                debrief = "Beneath the black water, a hundred stolen lanterns glow — arranged in a spiral.",
                waves = new[] { W(B, P, S, S), W(A, S, R, O) },
                planAsset = "S07_DrownedRoad" },

            new() { id = 8, name = "TWIN LANTERNS", marsh = true,
                story = "Two toll-captains guard the crossing. Their lanterns burn a color fire should not be.",
                dialogue = new[]
                {
                    "RENZO|Two captains. Their lanterns burn green — that's not oil-fire.",
                    "KAGACHI|Closer, little bearer. My lieutenants will weigh your flame.",
                    "RENZO|It's not for sale, and it's not for the water.",
                },
                debrief = "The twin flames guttered out — and somewhere deep in the marsh, something vast exhaled.",
                waves = new[] { W(S, P, O), W(C, C) },
                planAsset = "S08_TwinLanterns" },

            new() { id = 9, name = "THE SERPENT'S GUARD", marsh = true,
                story = "The serpent's chosen bar the last bridge. Behind them, the water is perfectly still.",
                dialogue = new[]
                {
                    "KAGACHI|Your family kept the oldest light, and never asked what it was for.",
                    "RENZO|It guided people home.",
                    "KAGACHI|It guided something else. Bring it. The door is nearly open.",
                },
                debrief = "Past the last bridge, the water lies perfectly still — like a held breath.",
                waves = new[] { W(S, P, S, R, O), W(C, A, S, P, B) },
                planAsset = "S09_SerpentsGuard" },

            new() { id = 10, name = "KAGACHI", marsh = true,
                story = "The Marsh Serpent rises. Three lives, they say — the duel, the mirrors, the desperation.",
                dialogue = new[]
                {
                    "KAGACHI|Three lives, ninja. The duel. The mirrors. The desperation.",
                    "RENZO|One lantern. And it goes home with me.",
                    "KAGACHI|Then feed it to the gate yourself, lantern-bearer.",
                },
                debrief = "The gate closed. The marsh began, at last, to drain. In Yorune, every lantern burns a little brighter.",
                waves = new[] { W(K) },
                planAsset = "S10_Kagachi" },
        };

        // ------------------------------------------------------- duel catalog

        public static readonly DuelDef[] Duels =
        {
            new() { id = 1, name = "GORO", title = "THE TOLL-CAPTAIN", kind = EnemyKind.Chief, marsh = false,
                taunt = "“Every roof pays. Even yours, little lantern.”",
                philosophy = "POWER · PRESSURE · COMMITMENT",
                hp = 360f, posture = 120f, postureRegen = 10f, dmgResist = 0.32f,
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
                hp = 280f, posture = 140f, postureRegen = 12f, dmgResist = 0.30f,
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
                hp = 340f, posture = 160f, postureRegen = 11f, dmgResist = 0.28f,
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
                hp = 480f, posture = 190f, postureRegen = 10f, dmgResist = 0.26f,
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
                philosophy = "DISCIPLINE · FORMATION · ATTRITION" },
            new() { id = 6, name = "THE THREE BLADES", title = "SISTERS OF THE SILENT FOREST", kind = EnemyKind.Assassin,
                defId = "threeblades", marsh = false, taunt = "“One for the throat. One for the heart. One to watch.”",
                philosophy = "AMBUSH · ROTATION · PATIENCE" },
            new() { id = 7, name = "THE DROWNED GUARDIAN", title = "WARDEN OF THE SECOND KEY", kind = EnemyKind.EliteWarrior,
                defId = "drownedguardian", marsh = true, taunt = "“Your father set me here. He did not say you would come.”",
                philosophy = "ENDURANCE · REACH · REFUSAL" },
            new() { id = 8, name = "THE IRON GUARD", title = "KAGEHIRA'S SHIELD", kind = EnemyKind.EliteWarrior,
                defId = "ironguard", marsh = false, taunt = "“The warlord does not see you. I make sure of it.”",
                philosophy = "GUARD · PUNISHMENT · NO GROUND GIVEN" },
            new() { id = 9, name = "COMMANDER HOSHU", title = "THE INNER GATE", kind = EnemyKind.Samurai,
                defId = "finalcommander", marsh = false, taunt = "“He said you would reach this door. He did not say you would open it.”",
                philosophy = "COMMAND · TIMING · THE LAST DOOR" },
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

        public static int DuelsUnlocked
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("duels_unlocked", 1), 1, Duels.Length);
            set { PlayerPrefs.SetInt("duels_unlocked", Mathf.Max(DuelsUnlocked, value)); PlayerPrefs.Save(); }
        }

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
