using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;

namespace Emberline.Enemies
{
    /// <summary>What an enemy shot is. Colour and on-hit rider come from this.</summary>
    public enum ProjectileKind { EmberBolt, PoisonSpit }

    /// <summary>
    /// Enemy shot — the Weaver's ember bolt and Kagachi's venom spit. Pooled:
    /// projectiles are recycled instead of Instantiate/Destroy so volleys never
    /// touch the GC.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static readonly Queue<Projectile> Pool = new();

        private Vector3 _dir;
        private float _damage;
        private float _life;
        private ProjectileKind _kind;
        private Renderer _renderer;
        private Transform _player;
        private Player.PlayerLocomotion _motor;
        private Health _health;
        private SenGates _gates;
        private bool _dodged;

        public static void Spawn(Vector3 at, Vector3 dir, float damage,
            ProjectileKind kind = ProjectileKind.EmberBolt)
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
                p._renderer = r;
                p.Bind();
            }
            if (p._renderer == null) p._renderer = p.GetComponent<Renderer>();
            p.gameObject.SetActive(true);
            p.transform.position = at;
            p._dir = dir.normalized;
            p._damage = damage;
            p._life = 3f;
            p._dodged = false;
            p._kind = kind;
            // Venom reads green and travels a touch slower than an ember bolt.
            if (p._renderer != null)
                p._renderer.material.color = kind == ProjectileKind.PoisonSpit
                    ? new Color(0.45f, 0.9f, 0.4f)
                    : new Color(1f, 0.55f, 0.35f);
            p.transform.localScale = Vector3.one * (kind == ProjectileKind.PoisonSpit ? 0.45f : 0.35f);
        }

        private void Bind()
        {
            _motor = SceneRefs.Motor;
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
            transform.position += _dir * ((_kind == ProjectileKind.PoisonSpit ? 7.5f : 9f)
                                          * Time.deltaTime);
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
                    // Venom clings: the hit is small, the slow is the real cost.
                    if (_kind == ProjectileKind.PoisonSpit)
                    {
                        _motor.ApplySlow(3f, 0.55f);
                        UI.FxPools.Puff(transform.position, new Color(0.45f, 0.85f, 0.4f), 10);
                        UI.FloatingText.Spawn(transform.position + Vector3.up * 1.6f, "VENOM",
                            new Color(0.5f, 0.95f, 0.45f), 1f);
                    }
                    Despawn();
                }
            }
        }
    }

    /// <summary>
    /// Jin's storm trail: the air he tore through stays charged for a second and
    /// bites anything standing in it. Turns his dash from a single hit into a
    /// line you have to leave, which is the whole point of fighting a duelist.
    /// </summary>
    public class StormTrail : MonoBehaviour
    {
        private const float Radius = 1.1f;
        private const float Life = 1f;

        private static Shader _glow;

        private float _life = Life, _tick;
        private float _damage;
        private Material _mat;

        public static void Spawn(Vector3 pos, float damage)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "StormTrail";
            quad.transform.position = new Vector3(pos.x, 0.07f, pos.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (Radius * 2f);
            var t = quad.AddComponent<StormTrail>();
            t._damage = damage;
            _glow = _glow != null ? _glow : Shader.Find("Emberline/Glow");
            t._mat = new Material(_glow) { color = new Color(0.55f, 0.62f, 1f, 0.55f) };
            var r = quad.GetComponent<Renderer>();
            r.material = t._mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            var fade = Mathf.Clamp01(_life / Life);
            _mat.color = new Color(0.55f, 0.62f, 1f,
                0.55f * fade + 0.08f * Mathf.Sin(Time.time * 14f));

            if ((_tick -= Time.deltaTime) > 0f) return;
            _tick = 0.35f;
            var motor = SceneRefs.Motor;
            if (motor == null || motor.Invulnerable) return;
            if (Vector3.Distance(motor.transform.position, transform.position) > Radius) return;
            motor.GetComponent<Health>()?.Damage(_damage, transform.position);
            UI.FxPools.Sparks(motor.transform.position + Vector3.up, new Color(0.7f, 0.78f, 1f), 6);
        }
    }
}
