using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.Campaign
{
    /// <summary>What a mission mostly asks of the player. The first entry on a
    /// mission is its primary type; the rest colour it.</summary>
    public enum GameplayType
    {
        Combat, Stealth, Investigation, Rescue, Defense, Escort, Chase,
        Exploration, Survival, Boss, Sabotage, Memory, Conversation, Endure,
    }

    /// <summary>Where Renzo is, emotionally, across the hundred missions.</summary>
    public enum RenzoState { Confused, Angry, Obsessed, Consumed, Changed }

    /// <summary>How much of the Black Seal the player is allowed to understand.</summary>
    public enum SealStage
    {
        Rumour,      // 1–20: the name is heard
        Hunted,      // 21–40: Kagehira is searching for it
        Evidence,    // 41–50: the first physical piece
        Protected,   // 51–60: father hid it
        Witness,     // 61–70: Jin knows the truth
        Assembled,   // 71–80: Kagehira holds the rest
        Understood,  // 81–90: what it actually is
        Opened,      // 91–99: Kagehira opens it
        Chosen,      // 100: why Renzo chose differently
    }

    /// <summary>The journey, in the order the player walks it.</summary>
    public enum Region
    {
        Ruins, Forest, Mountains, Marsh, Temples, Villages, Fortresses, Snow,
        Stronghold, Seal, Dawn,
    }

    /// <summary>
    /// One campaign mission: the ten fields the design rule requires, plus what
    /// the game needs to actually stage it. Plain data, authored in code like
    /// the rest of the catalogue — a ScriptableObject per mission would be a
    /// hundred assets that can silently lose their script binding.
    /// </summary>
    public class CampaignMission
    {
        // ---- the ten required fields
        public int id;
        public string name = "";
        public string storyPurpose = "";
        public string primaryObjective = "";
        public GameplayType[] types = System.Array.Empty<GameplayType>();
        public string uniqueEvent = "";
        public string storyDiscovery = "";
        public string climax = "";
        public string ending = "";
        public string nextReason = "";

        // ---- staging
        public int chapter;
        public Region region;
        public EnvThemeId theme = EnvThemeId.Village;
        public bool marsh;                 // which arena geometry
        public bool night, rain, snow, fog;
        public EnemyKind[] enemies = System.Array.Empty<EnemyKind>();
        public EnemyKind? boss;            // a major boss fight in this mission
        public string foe = "";            // named foe: an EnemyDef id (Resources/Enemies)
        public string[] dialogue = System.Array.Empty<string>();
        public string beat = "";           // mid-mission cinematic beat id, if any
        public string plan = "";           // bespoke plan asset; empty = generated
        public RenzoState renzo;
        public SealStage seal;

        public GameplayType Primary => types.Length > 0 ? types[0] : GameplayType.Combat;
        public bool Has(GameplayType t) => System.Array.IndexOf(types, t) >= 0;
        public bool IsMajorBoss => boss.HasValue;

        /// <summary>Resources/Missions asset name for this mission's plan.</summary>
        public string PlanAsset => string.IsNullOrEmpty(plan) ? $"C{id:000}" : plan;
    }

    public class Chapter
    {
        public int number;
        public string name = "";
        public string theme = "";
        public int firstMission, lastMission;
        public int act;
        public Region region;
    }
}
