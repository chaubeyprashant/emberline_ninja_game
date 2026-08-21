using UnityEngine;
using Emberline.Enemies;

namespace Emberline.Missions
{
    /// <summary>
    /// A mission authored as an asset: theme, named waves, wave composition.
    /// Same shape as the prototype's MissionDef so content ports 1:1.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Mission", fileName = "NewMission")]
    public class MissionDef : ScriptableObject
    {
        [System.Serializable]
        public class Wave
        {
            public string title;
            public EnemyKind[] enemies;
        }

        public int id;
        public string missionName;
        [TextArea] public string subtitle;
        public string arenaSceneName;
        public Wave[] waves;
    }
}
