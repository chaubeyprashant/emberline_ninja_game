namespace Emberline.Campaign
{
    /// <summary>What a mission is FOR, in the seven-beat loop around a place.</summary>
    public enum MissionRole
    {
        Story,       // the spine: it happens, and it is not optional
        Discovery,   // you learn a place exists; nothing is asked of you yet
        Recon,       // go and look. Being seen is the fail state, not dying
        Prepare,     // optional work that changes a later mission
        Assault,     // the mission you chose the shape of
        Defend,      // they come to you, and what you built decides it
        Consequence, // the world after; the change is visible, not narrated
        Downtime,    // no enemies. A fire, a meal, a repair, a conversation
        Personal,    // a companion's own mission
        Memory,      // the past, played
    }

    /// <summary>How the player decided to do it. Assault is always available.</summary>
    public enum Approach
    {
        Assault,   // the front door. The most enemies and the most room
        Stealth,   // unseen. Fewest enemies, no margin
        Ambush,    // do not go in. Break the road and fight on your ground
        Sabotage,  // burn the supplies and take a shell instead of a camp
        Allied,    // companions and militia, from more than one direction
    }

    /// <summary>The six. Four fight, two do not. See docs/CAMPAIGN_ARCHITECTURE.md.</summary>
    public enum Companion { Suzu, Fumi, Tsuru, Daigo, Toku, Nire }

    /// <summary>A place with a garrison, revisited across a chapter.</summary>
    public enum CampId
    {
        None, TollPost, BrokenBanner, SilentCamp, ThePens, SunkenCamp,
        Garrison, FrozenCamp, IronFortress, SummitRoad,
    }

    /// <summary>A place with people, which remembers what you did for it.</summary>
    public enum VillageId { None, Yorune, Ashfall, Kiba, ReedVillage, GarrisonTown }

    public class CompanionDef
    {
        public Companion id;
        public string name = "", role = "", joins = "";
        public int joinMission;
        public bool fights;
        public string specialty = "", weakness = "", line = "";
    }

    public class CampDef
    {
        public CampId id;
        public string name = "";
        public int garrison;
        public int[] missions = System.Array.Empty<int>();
        public string[] marks = System.Array.Empty<string>();
    }

    public class VillageDef
    {
        public VillageId id;
        public string name = "", gives = "";
        public int firstMission;
    }

    /// <summary>
    /// The design layer over a campaign mission: what it is for, what the player
    /// may choose at the door, who can come, which place it belongs to, and what
    /// it remembers. Kept beside the story table rather than inside it, because
    /// the story spine is canon and this is the part that decides how a mission
    /// is played.
    /// </summary>
    public class MissionDesign
    {
        public int id;
        public MissionRole role;

        /// <summary>Two or more means a real choice on the briefing screen. The
        /// first entry is the default and is always available with no
        /// preparation at all — preparation is rewarding, never mandatory.</summary>
        public Approach[] approaches = System.Array.Empty<Approach>();

        /// <summary>Flag that unlocks every approach past the first. Empty means
        /// the choice is open from the start.</summary>
        public string approachFlag = "";

        /// <summary>Who may be brought. Two at most are taken into a mission.</summary>
        public Companion[] companions = System.Array.Empty<Companion>();

        public CampId camp;
        public VillageId village;

        /// <summary>Persisted facts this mission writes on success.</summary>
        public string[] sets = System.Array.Empty<string>();

        /// <summary>Persisted facts this mission reads, and is easier for.</summary>
        public string[] reads = System.Array.Empty<string>();

        /// <summary>What is different in the world afterwards. Shown, not said.</summary>
        public string consequence = "";

        /// <summary>The one thing the player wants when this mission ends.</summary>
        public string want = "";

        public bool HasChoice => approaches.Length > 1;
        public bool Has(Approach a) => System.Array.IndexOf(approaches, a) >= 0;
        public bool Has(Companion c) => System.Array.IndexOf(companions, c) >= 0;
    }
}
