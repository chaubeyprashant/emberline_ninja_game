using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Enemies share attack rights so they coordinate instead of mobbing:
    /// at most `maxSimultaneous` non-boss enemies may be winding up or dashing.
    /// Scene-level singleton; assign to every EnemyBrain.
    /// </summary>
    public class AttackTokenPool : MonoBehaviour
    {
        [SerializeField] private int maxSimultaneous = 2;

        private readonly List<EnemyBrain> _holders = new();

        public bool TryTake(EnemyBrain requester)
        {
            _holders.RemoveAll(b => b == null || b.Dead || !b.InWindupOrDash);
            if (_holders.Contains(requester)) return true;
            if (_holders.Count >= maxSimultaneous) return false;
            _holders.Add(requester);
            return true;
        }
    }
}
