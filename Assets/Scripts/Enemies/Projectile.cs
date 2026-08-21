using UnityEngine;
using Emberline.Core;

namespace Emberline.Enemies
{
    /// <summary>Ranged Weaver's ember bolt; built from a primitive at runtime.</summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _dir;
        private float _damage;
        private float _life = 3f;
        private Transform _player;
        private Player.PlayerLocomotion _motor;
        private Health _health;
        private SenGates _gates;
        private bool _dodged;

        public static void Spawn(Vector3 at, Vector3 dir, float damage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "EmberBolt";
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.35f;
            go.GetComponent<Renderer>().material.color = new Color(1f, 0.55f, 0.35f);
            var p = go.AddComponent<Projectile>();
            p._dir = dir.normalized;
            p._damage = damage;
        }

        private void Start()
        {
            _motor = FindFirstObjectByType<Player.PlayerLocomotion>();
            if (_motor != null)
            {
                _player = _motor.transform;
                _health = _motor.GetComponent<Health>();
                _gates = _motor.GetComponent<SenGates>();
            }
        }

        private void Update()
        {
            transform.position += _dir * (9f * Time.deltaTime);
            if ((_life -= Time.deltaTime) <= 0) { Destroy(gameObject); return; }
            if (_player == null) return;

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
                    Destroy(gameObject);
                }
            }
        }
    }
}
