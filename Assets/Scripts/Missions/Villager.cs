using System.Collections.Generic;
using Emberline.Core;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// A person who lives here. Villagers do not fight, cannot help, and are not
    /// objectives: they are what the mission is about. They hide where they were
    /// left, break for the nearest edge when a fight starts near them, and can be
    /// killed by anyone's blade including yours.
    ///
    /// Their whole job is to make the world read as inhabited, and to give the
    /// "no civilian deaths" challenge something real to protect.
    /// </summary>
    public class Villager : MonoBehaviour
    {
        public static readonly List<Villager> Active = new();

        /// <summary>Villagers killed this mission. Reset by the director.</summary>
        public static int Died { get; private set; }

        public static void ResetCount() { Died = 0; Active.Clear(); }

        private const float PanicRange = 7f;
        private const float FleeSpeed = 3.4f;

        private NinjaRig _rig;
        private Health _health;
        private Vector3 _home;
        private Vector3 _fleeTo;
        private bool _fleeing;

        public static Villager Spawn(Vector3 at, Color cloth)
        {
            var go = new GameObject("Villager");
            go.transform.position = at;

            var rig = go.AddComponent<NinjaRig>();
            rig.bodyColor = cloth;
            rig.accentColor = new Color(0.62f, 0.55f, 0.44f);
            rig.hasSword = false;
            rig.hasScarf = false;
            rig.maskStripe = false;
            rig.rigScale = 0.92f;

            var v = go.AddComponent<Villager>();
            v._rig = rig;
            v._home = at;
            v._health = go.AddComponent<Health>();
            v._health.SetMax(30f);
            v._health.OnDeath += v.OnDeath;
            Active.Add(v);
            return v;
        }

        private void OnDestroy() => Active.Remove(this);

        /// <summary>
        /// A swing that catches a villager kills them. Without this the "no
        /// civilian deaths" objective would be unbreakable and therefore
        /// meaningless: villagers flee an approaching fight on their own, so
        /// hitting one takes an actual careless swing at someone running away.
        /// </summary>
        public static void SweepDamage(Vector3 from, Vector3 facing, float range,
            float arcDeg, float damage)
        {
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var v = Active[i];
                if (v == null || v._health == null || v._health.Dead) continue;
                var to = v.transform.position - from;
                to.y = 0f;
                var d = to.magnitude;
                if (d > range || d > 1.7f && Vector3.Angle(facing, to) > arcDeg * 0.5f) continue;
                v._health.Damage(damage, from);
                v._rig?.Flash();
            }
        }

        private void OnDeath()
        {
            Died++;
            FloatingText.Spawn(transform.position + Vector3.up * 2.2f, "A VILLAGER DIES",
                new Color(0.95f, 0.5f, 0.45f), 1.3f);
            Destroy(gameObject, 0.1f);
        }

        private void Update()
        {
            if (GameManager.CinematicActive || _health == null || _health.Dead) return;

            if (!_fleeing && ThreatNear())
            {
                _fleeing = true;
                // Away from the trouble, toward the nearest arena edge.
                var away = (transform.position - NearestThreat()).normalized;
                _fleeTo = transform.position + away * 14f;
            }

            if (_fleeing)
            {
                var to = _fleeTo - transform.position;
                to.y = 0f;
                if (to.magnitude > 0.4f)
                {
                    transform.position += to.normalized * (FleeSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(to), 8f * Time.deltaTime);
                    if (_rig != null) _rig.move01 = 1f;
                    return;
                }
                if (_rig != null) _rig.move01 = 0f;
                return;
            }

            // Hiding: still, low, facing away from the road.
            if (_rig != null) _rig.move01 = 0f;
        }

        private bool ThreatNear() => Vector3.Distance(transform.position, NearestThreat()) < PanicRange;

        private Vector3 NearestThreat()
        {
            var best = _home + Vector3.forward * 999f;
            var bestD = float.MaxValue;
            for (var i = 0; i < Enemies.EnemyBrain.Active.Count; i++)
            {
                var e = Enemies.EnemyBrain.Active[i];
                if (e == null || e.Dead || e.Unaware) continue;
                var d = Vector3.SqrMagnitude(e.transform.position - transform.position);
                if (d < bestD) { bestD = d; best = e.transform.position; }
            }
            return best;
        }
    }
}
