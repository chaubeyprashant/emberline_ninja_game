using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>What a given enemy is allowed to be doing in the group right now.</summary>
    public enum SquadRole
    {
        Wait,      // hold at a readable distance and do nothing threatening
        Circle,    // reposition around the player, no attacking
        Engage,    // cleared to attack
        Support,   // ranged: hold a firing lane
    }

    /// <summary>
    /// Group-level combat direction. The AttackTokenPool already capped how many
    /// enemies could *swing* at once; this decides what the rest should be doing
    /// instead of crowding — so a pack reads as a pack rather than a queue.
    ///
    /// Roles are recomputed on a slow tick (4/second) for the whole group at once,
    /// not per enemy per frame: with a dozen enemies that is the difference
    /// between a nested distance scan every frame and one pass four times a second.
    /// </summary>
    public class SquadCoordinator : MonoBehaviour
    {
        public static SquadCoordinator Instance { get; private set; }

        [Tooltip("Melee enemies cleared to attack at once.")]
        [SerializeField] private int meleeEngaged = 2;

        [Tooltip("Extra attacker allowed once the pack is this big.")]
        [SerializeField] private int crowdBonusAt = 5;

        [Tooltip("Ranged enemies allowed to fire at once.")]
        [SerializeField] private int rangedEngaged = 2;

        [SerializeField] private float retickSeconds = 0.25f;

        private readonly Dictionary<EnemyBrain, SquadRole> _roles = new();
        private readonly List<EnemyBrain> _scratch = new();
        private float _tick;

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>Role for an enemy; Engage by default so a lone enemy just fights.</summary>
        public SquadRole RoleOf(EnemyBrain brain)
        {
            if (brain == null) return SquadRole.Engage;
            return _roles.TryGetValue(brain, out var r) ? r : SquadRole.Engage;
        }

        private void Update()
        {
            if ((_tick -= Time.deltaTime) > 0f) return;
            _tick = retickSeconds;
            Reassign();
        }

        private void Reassign()
        {
            var player = Core.SceneRefs.Motor;
            if (player == null) return;
            var origin = player.transform.position;

            _scratch.Clear();
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e != null && !e.Dead && !e.Unaware) _scratch.Add(e);
            }
            _roles.Clear();
            if (_scratch.Count == 0) return;

            // Nearest first: whoever is already in your face gets to commit.
            _scratch.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - origin)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - origin)));

            var meleeSlots = meleeEngaged + (_scratch.Count >= crowdBonusAt ? 1 : 0);
            var rangedSlots = rangedEngaged;

            for (var i = 0; i < _scratch.Count; i++)
            {
                var e = _scratch[i];
                var ranged = e.IsRanged;

                if (ranged)
                {
                    // Archers and bombers never crowd; they either have a lane or
                    // they are repositioning to get one.
                    _roles[e] = rangedSlots-- > 0 ? SquadRole.Support : SquadRole.Circle;
                    continue;
                }

                // Bosses are never told to wait — their fight is the encounter.
                if (e.IsElite) { _roles[e] = SquadRole.Engage; continue; }

                if (meleeSlots > 0)
                {
                    meleeSlots--;
                    _roles[e] = SquadRole.Engage;
                }
                else
                {
                    // Alternate the rest between circling and holding, so the ring
                    // around the player moves instead of standing still.
                    _roles[e] = (i & 1) == 0 ? SquadRole.Circle : SquadRole.Wait;
                }
            }
        }
    }
}
