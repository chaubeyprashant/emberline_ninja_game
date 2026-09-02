using UnityEngine;
using Emberline.Enemies;

namespace Emberline.Missions
{
    /// <summary>
    /// What one beat of a mission asks for. A mission is a *sequence* of these —
    /// that is the whole point of the overhaul. "Infiltration" is not a mission
    /// type with bespoke code; it is Reach → Stealth → Assassinate → (alarm event)
    /// → Escape → BossFight → Reach, composed from goals the director already
    /// knows how to run.
    /// </summary>
    public enum StageGoal
    {
        Reach,       // get to a marked point
        Wave,        // clear an authored wave
        Eliminate,   // kill a specific number of a kind
        Assassinate, // kill a marked target — ideally unseen
        Survive,     // stay alive for a duration
        Defend,      // keep enemies off a point for a duration
        Escort,      // walk the bearer to their goal
        Stealth,     // clear the area without raising the alarm
        Investigate, // find N clues in the environment
        Chase,       // catch a fleeing target before it escapes
        Duel,        // one named opponent
        BossFight,   // boss encounter
        Escape,      // reach the exit under time pressure
        ReachAny,    // two ways in: take either, the other is still waiting
        BossPhase,   // fight a boss down to a health threshold, not to death
        Listen,      // hold still in the fog and find what is circling you
        Endure,      // survive a foe you cannot beat yet; the clock ends it, not a corpse
        Cinematic,   // play a story beat in place; the mission waits for it
        FreePrisoners, // cut a number of prisoners loose
    }

    /// <summary>
    /// The scripted "unexpected event" a stage can fire when it completes. These
    /// are what stop a mission reading as a list of chores.
    /// </summary>
    public enum StageEvent
    {
        None,
        AlarmTriggered,   // everything wakes up
        Reinforcements,   // a fresh pack arrives
        BossArrives,      // the encounter escalates
        LightsOut,        // the arena darkens; vision ranges drop
        RainStarts,       // weather shift — noise cover
        WaterRises,       // the marsh floods; movement slows
        TargetFlees,      // the objective turns and runs
        FogRolls,         // the marsh closes in; sight collapses to a few metres
        Ambush,           // a pack arrives behind the player, already awake
        RouteWakes,       // the way you did not take is ready for you now
        Collapse,         // the ground gives: shake, dust, and a clock on the way out
        Mutiny,           // the remaining guards turn and leave; nobody is coming
        FoeWithdraws,     // the named foe breaks off and is gone
    }

    /// <summary>One beat of a mission. Authored as data, run by the director.</summary>
    [System.Serializable]
    public class MissionStage
    {
        public StageGoal goal = StageGoal.Wave;

        [Tooltip("Shown as the live objective while this stage is active.")]
        public string objective = "";

        [Tooltip("Banner shown when the stage begins. Empty for no announcement.")]
        public string banner = "";

        [Tooltip("Meaning depends on the goal: kill count, clue count, wave index.")]
        public int count = 1;

        [Tooltip("Seconds, for Survive / Defend / Escape.")]
        public float duration;

        [Tooltip("Target point for Reach / Defend / Escape, in arena space.")]
        public Vector3 point;

        [Tooltip("Enemies spawned when this stage starts.")]
        public EnemyKind[] spawn = System.Array.Empty<EnemyKind>();

        [Tooltip("ReachAny: the second way in.")]
        public Vector3 pointB;

        [Tooltip("ReachAny: what waits on the second route. The route you leave " +
                 "is remembered, so the mission can send it after you later.")]
        public EnemyKind[] spawnB = System.Array.Empty<EnemyKind>();

        [Tooltip("Named foe for this stage: an EnemyDef id under Resources/Enemies. " +
                 "Spawned on the matching kind's body with that def's stats and card.")]
        public string foeDef = "";

        [Tooltip("Cinematic: the StoryBeat id under Resources/Story to play.")]
        public string beatId = "";

        [Tooltip("BossPhase: end the stage when the boss drops below this " +
                 "fraction of its health. The boss survives; the mission moves on.")]
        [Range(0.05f, 0.95f)] public float bossHealthGate = 0.6f;

        [Tooltip("Fires when the stage completes — the mission's turn.")]
        public StageEvent onComplete = StageEvent.None;

        [Tooltip("Optional stages can be skipped; they pay a bonus if done.")]
        public bool optional;

        [Tooltip("Shards paid for an optional stage.")]
        public int bonusShards = 1;

        /// <summary>Checkpoint here, so a retry resumes from this beat.</summary>
        public bool checkpoint;
    }
}
