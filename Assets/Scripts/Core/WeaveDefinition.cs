using UnityEngine;

namespace Emberline.Core
{
    public enum Strand { Ember, Gale, Stone, Tide, Storm, Veil, Bind, Pact, Inner }

    /// <summary>
    /// One Weave (ability) authored as an asset — the design bible's rule:
    /// every ability has a cost, a cooldown, and an explicit weakness.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Weave", fileName = "NewWeave")]
    public class WeaveDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaveName;
        public Strand strand;
        [TextArea] public string description;
        [TextArea] public string explicitWeakness; // required by design rule

        [Header("Economy")]
        public float senCost = 10f;
        public float cooldown = 4f;
        public bool surgeable = true; // can Renzo overcharge it?

        [Header("Effect")]
        public float damage = 15f;
        public float radius = 3f;
        [Range(0, 360)] public float arcDegrees = 120f;
        public float staggerPower = 1f;
        public GameObject vfxPrefab;

        [Header("Surge variant (overcharged)")]
        public float surgeDamageMultiplier = 1.8f;
        public float surgeRadiusMultiplier = 1.5f;
    }
}
