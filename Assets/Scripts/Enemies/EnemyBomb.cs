using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.UI;

namespace Emberline.Enemies
{
    /// <summary>
    /// The Bomber's lobbed charge. Arcs to where the player was standing, then
    /// bursts for area damage and leaves a burning patch — the point is to deny
    /// ground, so it threatens even when the throw itself is dodged.
    /// Pooled like every other projectile in the game.
    /// </summary>
    public class EnemyBomb : MonoBehaviour
    {
        private const float BlastRadius = 2.6f;
        private const float Gravity = -14f;

        private static readonly Queue<EnemyBomb> Pool = new();
        private static Shader _glow;

        private static Shader Glow => _glow != null ? _glow : _glow = Shader.Find("Emberline/Glow");

        private Vector3 _vel;
        private float _damage, _life;

        public static void ResetPool() => Pool.Clear();

        public static void Spawn(Vector3 at, Vector3 target, float damage)
        {
            EnemyBomb b = null;
            while (Pool.Count > 0 && b == null) b = Pool.Dequeue(); // skip destroyed
            if (b == null)
            {
                var prefab = Resources.Load<GameObject>("Props/Bomb");
                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.localScale = Vector3.one * 0.3f;
                    go.GetComponent<Renderer>().material =
                        new Material(Glow) { color = new Color(0.5f, 0.3f, 0.22f) };
                }
                go.name = "EnemyBomb";
                b = go.AddComponent<EnemyBomb>();
            }
            b.gameObject.SetActive(true);
            b.transform.position = at;
            b._damage = damage;
            b._life = 3f;

            // Ballistic solve for a fixed flight time — a lob the player can read
            // and walk out of, rather than a homing shot.
            const float flight = 0.9f;
            var delta = target - at;
            b._vel = new Vector3(delta.x / flight, 0f, delta.z / flight);
            b._vel.y = (delta.y - 0.5f * Gravity * flight * flight) / flight;
        }

        private void Despawn()
        {
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _vel.y += Gravity * dt;
            transform.position += _vel * dt;
            transform.Rotate(220f * dt, 160f * dt, 0f, Space.Self);
            if ((_life -= dt) > 0f && transform.position.y > 0.12f) return;
            Burst();
        }

        private void Burst()
        {
            var at = transform.position;
            at.y = 0f;
            FxPools.Nova(at);
            FxPools.Embers(at + Vector3.up * 0.4f, 22);
            Sfx3D.HitCrush();
            SceneRefs.Rig?.Shake(5f, 0.25f);

            var motor = SceneRefs.Motor;
            if (motor != null)
            {
                var d = Vector3.Distance(motor.transform.position, at);
                if (d <= BlastRadius)
                {
                    // I-frames still beat it; that's the dodge the telegraph buys.
                    if (motor.Invulnerable) motor.GetComponent<SenGates>()?.OnPerfectDodge();
                    else motor.GetComponent<Health>()?.Damage(_damage, at);
                }
            }
            FirePuddle.Spawn(at);
            Despawn();
        }
    }

    /// <summary>
    /// Burning ground left by a bomb burst: ticks damage while the player stands
    /// in it. Mirrors the player's PoisonPuddle, pointed the other way.
    /// </summary>
    public class FirePuddle : MonoBehaviour
    {
        private const float Radius = 2.2f;
        private const float Life = 4f;
        private const float TickDamage = 4f;

        private static Shader _glow;

        private float _life = Life, _tick;
        private Material _mat;

        public static void Spawn(Vector3 pos)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "FirePuddle";
            quad.transform.position = new Vector3(pos.x, 0.055f, pos.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (Radius * 2f);
            var p = quad.AddComponent<FirePuddle>();
            _glow = _glow != null ? _glow : Shader.Find("Emberline/Glow");
            p._mat = new Material(_glow) { color = new Color(1f, 0.45f, 0.20f, 0.5f) };
            var r = quad.GetComponent<Renderer>();
            r.material = p._mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            _mat.color = new Color(1f, 0.45f, 0.20f,
                0.5f * Mathf.Clamp01(_life / 1.2f) + 0.09f * Mathf.Sin(Time.time * 8f));

            if ((_tick -= Time.deltaTime) > 0f) return;
            _tick = 0.5f;
            var motor = SceneRefs.Motor;
            if (motor == null || motor.Invulnerable) return;
            var d = Vector3.Distance(motor.transform.position, transform.position);
            if (d > Radius) return;
            motor.GetComponent<Health>()?.Damage(TickDamage, transform.position);
            FxPools.Embers(motor.transform.position + Vector3.up * 0.5f, 4);
        }
    }
}
