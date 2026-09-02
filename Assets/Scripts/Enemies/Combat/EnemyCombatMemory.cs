using Emberline.Player;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// What an adaptive enemy has noticed about the player — read from the
    /// player's own behaviour memory, scaled by how adaptive this enemy is,
    /// and applied as probability shifts with cooldowns. Never input, never
    /// certainty: a bias the player can still play around.
    /// </summary>
    public class EnemyCombatMemory
    {
        private float _adaptCd;

        /// <summary>Extra score for a category, given what the player keeps doing.</summary>
        public float Bias(AttackCategory cat, PlayerCombatMemory player, float adaptation)
        {
            if (player == null || adaptation <= 0f) return 0f;
            var b = 0f;
            switch (cat)
            {
                case AttackCategory.GuardBreak: b = player.BlockTendency * 1.2f; break;
                case AttackCategory.Delayed: b = player.DodgeTendency * 1.1f; break;
                case AttackCategory.Counter: b = player.AggressionTendency * 1.0f + player.SpamTendency * 0.6f; break;
                case AttackCategory.Thrust:
                case AttackCategory.GapCloser: b = player.RetreatTendency * 1.1f; break;
                case AttackCategory.Feint: b = player.ParryTendency * 0.9f + player.DodgeTendency * 0.4f; break;
                case AttackCategory.Sweep: b = 0f; break;
            }
            return b * adaptation;
        }

        /// <summary>Adaptation is rate-limited so a single read cannot flip a fight.</summary>
        public bool MayAdapt(float dt)
        {
            _adaptCd = Mathf.Max(0f, _adaptCd - dt);
            if (_adaptCd > 0f) return false;
            _adaptCd = 2.5f;
            return true;
        }
    }
}
