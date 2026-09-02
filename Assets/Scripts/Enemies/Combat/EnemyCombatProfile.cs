using UnityEngine;

namespace Emberline.Enemies
{
    public enum LowHealthBehaviour { Retreat, Guard, Berserk, CallAllies, Desperate }
    public enum AllyDeathReaction { Ignore, Hesitate, Aggress, Isolate }

    /// <summary>A named attack sequence this enemy likes to run. Interruptible like any attack.</summary>
    [System.Serializable]
    public class ComboChain
    {
        public string name = "";
        public string[] steps = System.Array.Empty<string>();  // attack ids
    }

    /// <summary>
    /// Who an enemy is in a fight. The EnemyDef says what it can do; this says
    /// what it *wants* to do, how far it stands, how often it feints, what it
    /// does when it is losing and when its friends die. One asset per
    /// archetype (and per boss phase), referenced from the def.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Enemy Combat Profile")]
    public class EnemyCombatProfile : ScriptableObject
    {
        public string id = "raider";

        [Header("Temperament (0..1)")]
        [Range(0f, 1f)] public float aggression = 0.6f;
        [Range(0f, 1f)] public float bravery = 0.5f;
        [Range(0f, 1f)] public float attackFrequency = 0.6f;
        [Range(0f, 1f)] public float defenseFrequency = 0.2f;
        [Range(0f, 1f)] public float dodgeFrequency = 0.1f;
        [Range(0f, 1f)] public float parryAbility;
        [Range(0f, 1f)] public float retreatTendency = 0.2f;
        [Range(0f, 1f)] public float feintFrequency;
        [Range(0f, 1f)] public float counterFrequency = 0.1f;
        [Range(0f, 1f)] public float guardBreakFrequency = 0.2f;
        [Range(0f, 1f)] public float teamwork = 0.4f;

        [Header("Spacing (metres)")]
        public float preferredDistance = 2f;
        public float minDistance = 1.2f;
        public float maxDistance = 3.5f;
        public float retreatDistance = 1.4f;
        public float approachDistance = 4.5f;

        [Header("Chains")]
        [Range(1, 4)] public int comboLength = 2;
        public ComboChain[] combos = System.Array.Empty<ComboChain>();

        [Header("Adaptation")]
        [Tooltip("Reads the player's recent behaviour and shifts its choices. 0 = never.")]
        [Range(0f, 1f)] public float adaptation;
        [Range(0f, 1f)] public float reactionToPlayerAggression = 0.3f;

        [Header("Morale")]
        public LowHealthBehaviour lowHealth = LowHealthBehaviour.Retreat;
        [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
        public AllyDeathReaction allyDeath = AllyDeathReaction.Ignore;

        [Header("Cadence")]
        [Tooltip("Seconds between decisions. Difficulty scales it; the selector never runs per frame.")]
        public float decisionInterval = 0.25f;

        /// <summary>The next step after `lastId` in any combo, or null.</summary>
        public string NextComboStep(string lastId)
        {
            if (string.IsNullOrEmpty(lastId)) return null;
            foreach (var c in combos)
                for (var i = 0; i + 1 < c.steps.Length; i++)
                    if (c.steps[i] == lastId) return c.steps[i + 1];
            return null;
        }
    }
}
