using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;

namespace Emberline.Enemies
{
    /// <summary>
    /// Ranged Weaver's ember bolt. Pooled: bolts are recycled instead of
    /// Instantiate/Destroy so archer volleys never touch the GC.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static readonly Queue<Projectile> Pool = new();

        private Vector3 _dir;
        private float _damage;
        private float _life;
        private Transform _player;
        private Player.PlayerLocomotion _motor;
        private Health _health;
        private SenGates _gates;
        private bool _dodged;

        public static void Spawn(Vector3 at, Vector3 dir, float damage)
        {
            Projectile p = null;
            while (Pool.Count > 0 && p == null) p = Pool.Dequeue(); // skip destroyed
            if (p == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(go.GetComponent<Collider>());
                go.name = "EmberBolt";
                go.transform.localScale = Vector3.one * 0.35f;
                var r = go.GetComponent<Renderer>();
                r.material.color = new Color(1f, 0.55f, 0.35f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p = go.AddComponent<Projectile>();
                p.Bind();
            }
            p.gameObject.SetActive(true);
            p.transform.position = at;
            p._dir = dir.normalized;
            p._damage = damage;
            p._life = 3f;
            p._dodged = false;
        }

        private void Bind()
        {
            _motor = FindFirstObjectByType<Player.PlayerLocomotion>();
            if (_motor != null)
            {
                _player = _motor.transform;
                _health = _motor.GetComponent<Health>();
                _gates = _motor.GetComponent<SenGates>();
            }
        }

        private void Despawn()
        {
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private void Update()
        {
            var prev = transform.position;
            transform.position += _dir * (9f * Time.deltaTime);
            // Chimney cover eats bolts — breaking line of sight is real.
            if (ArenaMarkers.Blocked(prev, transform.position))
            {
                UI.FxPools.Sparks(transform.position, new Color(1f, 0.7f, 0.4f), 5);
                Despawn();
                return;
            }
            if ((_life -= Time.deltaTime) <= 0) { Despawn(); return; }
            if (_player == null) { Bind(); if (_player == null) return; }

            var d = Vector3.Distance(transform.position, _player.position + Vector3.up);
            if (d < 0.8f)
            {
                if (_motor.Invulnerable)
                {
                    if (!_dodged) { _dodged = true; _gates?.OnPerfectDodge(); }
                }
                else
                {
                    _health?.Damage(_damage, transform.position - _dir * 2f);
                    Despawn();
                }
            }
        }
    }
}
