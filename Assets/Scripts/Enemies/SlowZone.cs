using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Cracked, dust-choked ground left by Goro's slam: it drags at whoever stands
    /// in it, so the slam threatens the space afterwards instead of only the
    /// instant it lands. Built from a runtime quad — no new art.
    /// </summary>
    public class SlowZone : MonoBehaviour
    {
        private const float SpeedMultiplier = 0.55f;

        public static readonly List<SlowZone> Active = new();

        private static Shader _glow;

        private float _life, _maxLife, _radius;
        private Material _mat;

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        public static void Spawn(Vector3 pos, float radius, float life)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "SlowZone";
            quad.transform.position = new Vector3(pos.x, 0.05f, pos.z);
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = Vector3.one * (radius * 2f);

            var zone = quad.AddComponent<SlowZone>();
            zone._radius = radius;
            zone._life = zone._maxLife = life;
            zone._mat = new Material(_glow != null ? _glow : _glow = Shader.Find("Emberline/Glow"))
            {
                color = new Color(0.55f, 0.35f, 0.22f, 0.45f),
            };
            quad.GetComponent<Renderer>().material = zone._mat;
        }

        /// <summary>Movement multiplier at a world point; 1 when clear.</summary>
        public static float SpeedMulAt(Vector3 p)
        {
            for (var i = 0; i < Active.Count; i++)
            {
                var z = Active[i];
                if (z == null) continue;
                var dx = p.x - z.transform.position.x;
                var dz = p.z - z.transform.position.z;
                if (dx * dx + dz * dz <= z._radius * z._radius) return SpeedMultiplier;
            }
            return 1f;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            var fade = Mathf.Clamp01(_life / _maxLife);
            _mat.color = new Color(0.55f, 0.35f, 0.22f,
                0.45f * fade + 0.05f * Mathf.Sin(Time.time * 5f));
        }
    }
}
