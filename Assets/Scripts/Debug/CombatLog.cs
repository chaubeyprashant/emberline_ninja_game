using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Development-only damage instrumentation (Combat 2.0 brief, Phase 2.1).
    /// Every damage event carries its attack, raw and final amount, HP before
    /// and after, and posture — and flags any single ordinary hit that removes
    /// more than a set fraction of the target's max HP, which is exactly the
    /// signature of a one-shot bug. Compiled out of release builds; verbose
    /// logging is opt-in so ordinary play is silent.
    /// </summary>
    public static class CombatLog
    {
        /// <summary>Turn on the per-hit log line. The one-shot flag fires regardless.</summary>
        public static bool Verbose;

        /// <summary>Largest single fraction of max HP any player-inflicted hit removed.</summary>
        public static float MaxPlayerHitFraction { get; private set; }
        public static float MaxEnemyHitFraction { get; private set; }
        public static int OneShotFlags { get; private set; }

        public static void Reset() { MaxPlayerHitFraction = 0f; MaxEnemyHitFraction = 0f; OneShotFlags = 0; }

        public static void PlayerHit(EnemyDef def, string id, float amount, float before, float after,
            float posture, bool crush, float maxHp)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (maxHp <= 0f) return;
            var frac = amount / maxHp;
            if (frac > MaxPlayerHitFraction) MaxPlayerHitFraction = frac;
            if (Verbose)
                Debug.Log($"[DMG] player→{id} raw={amount:0.0} hp {before:0}→{after:0}/{maxHp:0} " +
                          $"posture={posture:0} crush={crush} frac={frac:P0}");
            // A single ordinary hit taking more than 70% of a full-HP target is
            // the one-shot signature — never expected outside an execution.
            if (before >= maxHp * 0.98f && frac > 0.7f)
            {
                OneShotFlags++;
                Debug.LogWarning($"[DMG] ONE-SHOT? player→{id} took {frac:P0} of full HP in one hit " +
                                 $"(raw={amount:0.0} of {maxHp:0}) crush={crush}");
            }
#endif
        }

        public static void EnemyHitPlayer(EnemyDef def, string id, float amount, float before, float after,
            bool heavy, float maxHp)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (maxHp <= 0f) return;
            var frac = amount / maxHp;
            if (frac > MaxEnemyHitFraction) MaxEnemyHitFraction = frac;
            if (Verbose)
                Debug.Log($"[DMG] {id}→player raw={amount:0.0} hp {before:0}→{after:0}/{maxHp:0} heavy={heavy} frac={frac:P0}");
            if (before >= maxHp * 0.98f && frac > 0.7f)
            {
                OneShotFlags++;
                Debug.LogWarning($"[DMG] ONE-SHOT? {id}→player took {frac:P0} of full HP in one hit " +
                                 $"(raw={amount:0.0} of {maxHp:0})");
            }
#endif
        }
    }
}
