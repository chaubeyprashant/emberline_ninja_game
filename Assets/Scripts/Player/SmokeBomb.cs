using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.Player
{
    /// <summary>
    /// Renzo's thrown smoke bomb. Flies a short arc, then bursts into a lingering
    /// cloud that blinds whatever stands in it. Pooled like the kunai — the throw
    /// slot is spammable and must never allocate.
    /// </summary>
    public class SmokeBomb : MonoBehaviour
    {
        private const float Speed = 12f;
        private const float Fuse = 0.75f;     // ~9m of reach before it bursts
        private const float Gravity = -9f;

        private static readonly Queue<SmokeBomb> Pool = new();
        private static Shader _glow;

        private static Shader Glow => _glow != null ? _glow : _glow = Shader.Find("Emberline/Glow");

        private Vector3 _dir;
        private float _fuse, _vertVel;

        public static void Spawn(Vector3 at, Vector3 dir)
        {
            SmokeBomb b = null;
            while (Pool.Count > 0 && b == null) b = Pool.Dequeue(); // skip destroyed
            if (b == null)
            {
                var prefab = Resources.Load<GameObject>("Props/Bomb");
                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab);
                    go.name = "SmokeBomb";
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(go.GetComponent<Collider>());
                    go.name = "SmokeBomb";
                    go.transform.localScale = Vector3.one * 0.3f;
                    go.GetComponent<Renderer>().material =
                        new Material(Glow) { color = new Color(0.42f, 0.44f, 0.48f) };
                }
                b = go.AddComponent<SmokeBomb>();
            }
            b.gameObject.SetActive(true);
            b.transform.position = at;
            b._dir = dir.normalized;
            b._fuse = Fuse;
            b._vertVel = 1.5f; // lobbed, not fired
        }

        /// <summary>Drop the pool across a scene load; the instances are gone with it.</summary>
        public static void ResetPool() => Pool.Clear();

        private void Despawn()
        {
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _vertVel += Gravity * dt;
            var step = _dir * (Speed * dt) + Vector3.up * (_vertVel * dt);
            var next = transform.position + step;
            transform.Rotate(180f * dt, 240f * dt, 0f, Space.Self); // tumbles in flight

            // Burst on the fuse, on the deck, or on the first body it touches.
            var hitBody = false;
            for (var i = 0; i < EnemyBrain.Active.Count && !hitBody; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                if ((e.transform.position + Vector3.up - next).sqrMagnitude < 0.9f * 0.9f)
                    hitBody = true;
            }

            transform.position = next;
            if ((_fuse -= dt) > 0f && next.y > 0.15f && !hitBody) return;
            Burst();
        }

        private void Burst()
        {
            var at = transform.position;
            at.y = 0f;
            SmokeCloud.Spawn(at);
            UI.FxPools.Puff(at + Vector3.up * 0.6f, new Color(0.45f, 0.55f, 0.44f), 26);
            Sfx3D.Surge();
            Despawn();
        }
    }

    /// <summary>
    /// The cloud left by a smoke bomb. Anything standing in it fights half-blind;
    /// shades, which are made of the same stuff the marsh breathes out, come apart
    /// twice as fast inside one.
    /// </summary>
    public class SmokeCloud : MonoBehaviour
    {
        public const float Radius = 3.2f;
        private const float Life = 3f;

        public static readonly List<SmokeCloud> Active = new();

        private static Shader _glow;

        private float _life;
        private Material _mat;
        private Transform _quad;

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        /// <summary>True if a world point sits inside any live cloud.</summary>
        public static bool Inside(Vector3 p)
        {
            for (var i = 0; i < Active.Count; i++)
            {
                var c = Active[i];
                if (c == null) continue;
                var dx = p.x - c.transform.position.x;
                var dz = p.z - c.transform.position.z;
                if (dx * dx + dz * dz <= Radius * Radius) return true;
            }
            return false;
        }

        public static void Spawn(Vector3 pos)
        {
            var go = new GameObject("SmokeCloud");
            go.transform.position = pos;
            var cloud = go.AddComponent<SmokeCloud>();
            cloud._life = Life;

            // Flat disc on the ground marks the footprint; the particle puff sells
            // the volume. A quad is enough and costs one draw call.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Kill(quad.GetComponent<Collider>());
            quad.name = "CloudDisc";
            quad.transform.SetParent(go.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (Radius * 2f);
            _glow = _glow != null ? _glow : Shader.Find("Emberline/Glow");
            cloud._mat = new Material(_glow) { color = new Color(0.62f, 0.72f, 0.60f, 0.75f) };
            quad.GetComponent<Renderer>().material = cloud._mat;
            quad.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            cloud._quad = quad.transform;
        }

        /// <summary>Destroy that also works in edit mode (snapshot verification).</summary>
        private static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            // Keep breathing out smoke so the volume reads from any camera angle.
            if (Random.value < 0.5f)
                UI.FxPools.Puff(transform.position + new Vector3(
                        Random.Range(-Radius, Radius) * 0.6f, 0.5f,
                        Random.Range(-Radius, Radius) * 0.6f),
                    new Color(0.45f, 0.55f, 0.44f), 2);

            var fade = Mathf.Clamp01(_life / 0.8f);
            _mat.color = new Color(0.62f, 0.72f, 0.60f, 0.75f * fade);
            if (_quad != null)
            {
                var s = Radius * 2f * (1f + 0.04f * Mathf.Sin(Time.time * 2.2f));
                _quad.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
