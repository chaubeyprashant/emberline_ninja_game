using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// A whole mission plan: a named sequence of stages with its own environment,
    /// enemy mix and rewards. Adding a mission type is authoring an asset — the
    /// director needs no new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Mission Plan")]
    public class MissionPlan : ScriptableObject
    {
        [Header("Identity")]
        public int id = 1;
        public string missionName = "MISSION";
        public string missionType = "ASSAULT";
        [TextArea] public string briefing = "";
        [TextArea] public string debrief = "";
        public bool marsh;

        [Header("Environment")]
        [Tooltip("Starts the mission dark — vision ranges drop for everyone.")]
        public bool nightOverride;
        public bool rain;

        [Header("Flow")]
        public MissionStage[] stages = System.Array.Empty<MissionStage>();

        [Header("Reward")]
        public int baseShards = 2;

        /// <summary>Total non-optional beats — used for objective progress text.</summary>
        public int RequiredStages
        {
            get
            {
                var n = 0;
                foreach (var s in stages) if (!s.optional) n++;
                return n;
            }
        }
    }
}
