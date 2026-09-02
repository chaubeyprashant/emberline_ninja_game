using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// Lightweight combat analytics: what Renzo has been doing lately, as
    /// decaying counters. Adaptive enemies read this to shift probabilities —
    /// a player who blocks a lot sees more guard-breaks, one who dodges early
    /// sees more delayed attacks. It is a description of behaviour that already
    /// happened, never a look at input; that is what keeps it honest.
    /// </summary>
    public class PlayerCombatMemory
    {
        public float LightAttacks { get; private set; }
        public float HeavyAttacks { get; private set; }
        public float Blocks { get; private set; }
        public float Dodges { get; private set; }
        public float PerfectParries { get; private set; }
        public float PerfectDodges { get; private set; }
        public float Retreats { get; private set; }
        public float Whiffs { get; private set; }

        /// <summary>Seconds for a counter to fall to half. Short: adaptation is about *now*.</summary>
        public float HalfLife = 8f;

        private float _lastStrikeTime = -10f;
        private int _sameChainRepeats;
        private int _lastChainLength;

        public void Tick(float dt)
        {
            var k = Mathf.Exp(-0.6931f * dt / HalfLife);
            LightAttacks *= k; HeavyAttacks *= k; Blocks *= k; Dodges *= k;
            PerfectParries *= k; PerfectDodges *= k; Retreats *= k; Whiffs *= k;
        }

        public void OnLight() { LightAttacks += 1f; _lastStrikeTime = Time.time; }
        public void OnHeavy() => HeavyAttacks += 1f;
        public void OnBlock() => Blocks += 1f;
        public void OnDodge() => Dodges += 1f;
        public void OnPerfectParry() => PerfectParries += 1f;
        public void OnPerfectDodge() => PerfectDodges += 1f;
        public void OnRetreat(float dt) => Retreats += dt;
        public void OnWhiff() => Whiffs += 1f;

        /// <summary>A chain ended; remember whether it was the same length as the last one.</summary>
        public void OnChainEnded(int length)
        {
            _sameChainRepeats = length == _lastChainLength ? _sameChainRepeats + 1 : 0;
            _lastChainLength = length;
        }

        /// <summary>How predictable the chain habit is: 0 varied, 1 the same thing four times.</summary>
        public float ChainRepetition => Mathf.Clamp01(_sameChainRepeats / 4f);

        /// <summary>Normalised tendencies for the selector: 0 rare, 1 very frequent.</summary>
        public float BlockTendency => Mathf.Clamp01(Blocks / 4f);
        public float DodgeTendency => Mathf.Clamp01(Dodges / 4f);
        public float AggressionTendency => Mathf.Clamp01((LightAttacks + HeavyAttacks * 1.5f) / 10f);
        public float RetreatTendency => Mathf.Clamp01(Retreats / 4f);
        public float ParryTendency => Mathf.Clamp01(PerfectParries / 2f);
        public float SpamTendency => Mathf.Clamp01(Mathf.Max(ChainRepetition, LightAttacks / 12f));
    }
}
