using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.UI;

namespace Emberline.Missions
{
    /// <summary>
    /// The lantern-bearer of the escort missions: walks a fixed line across the
    /// arena while the player keeps the road clear. Built at runtime from
    /// NinjaRig's primitives, so escort levels need no imported character.
    ///
    /// Enemies never path to the bearer — they still hunt the player — but any
    /// enemy that ends up beside them chips their health, so letting the fight
    /// drift onto the road is what actually loses the mission.
    /// </summary>
    public class EscortNpc : MonoBehaviour
    {
        private const float Threat = 2.4f;      // enemies this close are hurting them
        private const float DamagePerSecond = 7f;
        private const float StopDistance = 3.2f; // won't walk into a live fight

        public static EscortNpc Active { get; private set; }

        public Health Health { get; private set; }

        /// <summary>0..1 along the road. 1 = arrived.</summary>
        public float Progress01 { get; private set; }

        /// <summary>True while an enemy is close enough to be hurting them.</summary>
        public bool UnderThreat { get; private set; }

        private Vector3 _start, _goal;
        private float _speed;
        private CharacterRig _rig;

        public static EscortNpc Spawn(Vector3 start, Vector3 goal, float seconds, float maxHp)
        {
            var go = new GameObject("LanternBearer");
            go.transform.position = start;

            // Primitive rig: an unarmed civilian silhouette with a lantern scarf.
            var rig = go.AddComponent<NinjaRig>();
            rig.bodyColor = new Color(0.30f, 0.26f, 0.22f);
            rig.accentColor = new Color(1f, 0.62f, 0.35f);
            rig.hasSword = false;
            rig.hasScarf = true;
            rig.maskStripe = false;
            rig.rigScale = 0.95f;

            var npc = go.AddComponent<EscortNpc>();
            npc._start = start;
            npc._goal = goal;
            npc._speed = Vector3.Distance(start, goal) / Mathf.Max(1f, seconds);
            npc._rig = rig;
            npc.Health = go.AddComponent<Health>();
            npc.Health.SetMax(maxHp);
            Active = npc;
            return npc;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            if (GameManager.CinematicActive || Health == null || Health.Dead)
            {
                if (_rig != null) _rig.move01 = 0f;
                return;
            }

            var nearest = NearestEnemyDistance();
            UnderThreat = nearest <= Threat;
            if (UnderThreat)
            {
                Health.Damage(DamagePerSecond * Time.deltaTime, transform.position);
                _rig?.Flash();
            }

            // Hold position while a fight is on top of them — the bearer is not
            // suicidal, and it gives the player time to clear the road.
            var advancing = nearest > StopDistance && Progress01 < 1f;
            if (advancing)
            {
                var step = _speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, _goal, step);
                var total = Vector3.Distance(_start, _goal);
                Progress01 = total < 0.01f ? 1f
                    : Mathf.Clamp01(Vector3.Distance(_start, transform.position) / total);
                var dir = _goal - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, Quaternion.LookRotation(dir.normalized),
                        360f * Time.deltaTime);
            }
            if (_rig != null) _rig.move01 = advancing ? 0.7f : 0f;
        }

        private float NearestEnemyDistance()
        {
            var best = float.MaxValue;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                var d = Vector3.Distance(e.transform.position, transform.position);
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>Death flourish so the failure reads clearly.</summary>
        public void Extinguish()
        {
            FxPools.DeathBurst(transform.position, false);
            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "THE FLAME GOES OUT",
                new Color(1f, 0.42f, 0.29f), 1.2f);
        }
    }
}
