using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>What a given enemy is allowed to be doing in the group right now.</summary>
    public enum SquadRole
    {
        Wait,        // hold at a readable distance and do nothing threatening
        Circle,      // reposition around the player, no attacking
        Engage,      // cleared to attack
        Support,     // ranged: hold a firing lane
        Guard,       // stand close with the guard up — the wall the others work behind
        Reposition,  // work round to the player's back or to cover
        Protect,     // interpose between the player and a ranged ally
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

        /// <summary>Nearest living ranged ally to a brain, or null. For protectors.</summary>
        public EnemyBrain NearestRangedAlly(EnemyBrain of, float within)
        {
            EnemyBrain best = null;
            var bestD = within * within;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e == of || e.Dead || !e.IsRanged) continue;
                var d = (e.transform.position - of.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private int _rotation;
        private static Vector3 _sortOrigin;
        private static readonly System.Comparison<EnemyBrain> ByDistance = (a, b) =>
            Vector3.SqrMagnitude(a.transform.position - _sortOrigin)
                .CompareTo(Vector3.SqrMagnitude(b.transform.position - _sortOrigin));

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
            // The comparison reads a field, not a captured local, so the sort
            // allocates nothing four times a second.
            _sortOrigin = origin;
            _scratch.Sort(ByDistance);

            // Bodyguards first. A pike guard standing in the attack queue while
            // the player walks up to the archer behind it is the single most
            // obviously stupid thing a group can do, so this claim happens before
            // the attack slots are handed out — one protector per threatened ally.
            for (var i = 0; i < _scratch.Count; i++)
            {
                var ally = _scratch[i];
                if (!ally.IsRanged || ally.Dead) continue;
                if (Vector3.Distance(ally.transform.position, origin) > 6.5f) continue;
                EnemyBrain guard = null;
                var bestD = float.MaxValue;
                for (var j = 0; j < _scratch.Count; j++)
                {
                    var e = _scratch[j];
                    if (e == ally || e.IsRanged || _roles.ContainsKey(e)) continue;
                    if (e.def == null || !e.def.protectsRanged) continue;
                    var d = Vector3.SqrMagnitude(e.transform.position - ally.transform.position);
                    if (d < bestD) { bestD = d; guard = e; }
                }
                if (guard != null) _roles[guard] = SquadRole.Protect;
            }

            var meleeSlots = meleeEngaged + (_scratch.Count >= crowdBonusAt ? 1 : 0);
            var rangedSlots = rangedEngaged;
            var attacking = 0;

            // The ring rotates through four jobs so it reads as a unit working the
            // player rather than a queue: one guards close, one circles, one works
            // round the back, one waits. The rotation advances each tick so no
            // enemy is stuck in a job for the whole fight.
            _rotation++;
            var ringIndex = 0;
            for (var i = 0; i < _scratch.Count; i++)
            {
                var e = _scratch[i];
                if (e.InWindupOrDash) attacking++;
                if (_roles.ContainsKey(e)) continue; // already a bodyguard
                var ranged = e.IsRanged;

                if (ranged)
                {
                    _roles[e] = rangedSlots-- > 0 ? SquadRole.Support : SquadRole.Reposition;
                    continue;
                }

                if (e.IsElite) { _roles[e] = SquadRole.Engage; continue; }

                if (meleeSlots > 0)
                {
                    meleeSlots--;
                    _roles[e] = SquadRole.Engage;
                    continue;
                }

                var job = (ringIndex++ + _rotation / 6) % 4;
                var role = job switch
                {
                    0 => SquadRole.Guard,
                    1 => SquadRole.Circle,
                    2 => SquadRole.Reposition,
                    _ => SquadRole.Wait,
                };
                // Personality bends the job: an assassin given "guard" flanks
                // instead, a coward given "reposition" waits at distance, and a
                // brave bruiser never waits at all — he presses.
                var p = e.def != null ? e.def.profile : null;
                if (p != null)
                {
                    if (p.retreatTendency >= 0.5f && role == SquadRole.Guard) role = SquadRole.Reposition;
                    if (p.bravery <= 0.3f && role == SquadRole.Reposition) role = SquadRole.Wait;
                    if (p.bravery >= 0.8f && p.aggression >= 0.65f && role == SquadRole.Wait) role = SquadRole.Circle;
                }
                _roles[e] = role;
            }
            AiTelemetry.Sample(attacking, _scratch.Count);
        }
    }
}
