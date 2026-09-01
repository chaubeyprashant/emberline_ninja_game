using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Enemies share attack rights so they coordinate instead of mobbing:
    /// at most `Capacity` non-boss enemies may be winding up or dashing.
    /// Scene-level singleton; assign to every EnemyBrain.
    /// </summary>
    public class AttackTokenPool : MonoBehaviour
    {
        [SerializeField] private int baseSimultaneous = 2;

        /// <summary>Minimum gap between handing out openings.</summary>
        [SerializeField] private float grantInterval = 0.3f;

        /// <summary>How long an enemy waits before it may attack again.</summary>
        [SerializeField] private float reuseDelay = 1.2f;

        private readonly List<EnemyBrain> _holders = new();
        private readonly Dictionary<EnemyBrain, float> _lastAttack = new();
        private float _nextGrant;

        /// <summary>
        /// A big crowd earns one extra attacker so packs still feel dangerous,
        /// but the ceiling stays low enough to stay readable.
        /// </summary>
        private int Capacity
        {
            get
            {
                var alive = 0;
                for (var i = 0; i < EnemyBrain.Active.Count; i++)
                {
                    var b = EnemyBrain.Active[i];
                    if (b != null && !b.Dead) alive++;
                }
                // Four is already a pack: let a third attacker in so waves apply
                // real pressure instead of politely queueing. Difficulty shifts the
                // ceiling either way — this, more than damage, is what makes Lethal
                // feel lethal and Easy feel survivable.
                var cap = alive >= 4 ? baseSimultaneous + 1 : baseSimultaneous;
                return Mathf.Max(1, cap + Core.Difficulty.Now.ExtraAttackers);
            }
        }

        public bool TryTake(EnemyBrain requester)
        {
            _holders.RemoveAll(b => b == null || b.Dead || !b.InWindupOrDash);
            if (_holders.Contains(requester)) return true;
            if (_holders.Count >= Capacity) return false;

            // Stagger openings so a pack attacks in a rhythm instead of in unison.
            if (Time.time < _nextGrant) return false;

            // Rotate aggression: whoever just swung waits its turn while someone
            // else steps up, which stops one enemy monopolising the fight.
            if (_lastAttack.TryGetValue(requester, out var last)
                && Time.time - last < reuseDelay && _holders.Count > 0) return false;

            _nextGrant = Time.time + grantInterval;
            if (_lastAttack.Count > 64) Prune(); // destroyed clones leave dead keys
            _lastAttack[requester] = Time.time;
            _holders.Add(requester);
            return true;
        }

        private void Prune()
        {
            _scratch.Clear();
            foreach (var pair in _lastAttack)
                if (pair.Key == null) _scratch.Add(pair.Key);
            foreach (var dead in _scratch) _lastAttack.Remove(dead);
        }

        private readonly List<EnemyBrain> _scratch = new();
    }
}
