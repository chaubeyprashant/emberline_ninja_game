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
