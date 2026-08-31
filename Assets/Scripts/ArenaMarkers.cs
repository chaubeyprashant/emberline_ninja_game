using System.Collections.Generic;
using UnityEngine;

namespace Emberline
{
    /// <summary>
    /// Gameplay annotations for the generated arenas, filled by the bootstrap:
    /// chimney/cover circles (AI steering + projectile blocking), water pools
    /// (movement slow), and reed clusters (shade spawn points). Lives on the
    /// scene's GameManager object; one per scene.
    /// </summary>
    public class ArenaMarkers : MonoBehaviour
    {
        public static ArenaMarkers Instance { get; private set; }

        [Tooltip("x,z = center, w = radius")] public List<Vector4> obstacles = new();
        [Tooltip("x,z = center, w = radius")] public List<Vector4> waters = new();
        public List<Vector3> shadeSpawns = new();

        private List<Vector4> _baseWaters;

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>
        /// Kagachi's arena transformation: every pool widens by `mul` — the slow
        /// zones grow and their visuals scale with them.
        /// </summary>
        public static void RaiseWater(float mul)
        {
            if (Instance == null) return;
            Instance._baseWaters ??= new List<Vector4>(Instance.waters);
            for (var i = 0; i < Instance.waters.Count; i++)
            {
                var w = Instance._baseWaters[i];
                Instance.waters[i] = new Vector4(w.x, w.y, w.z, w.w * mul);
            }
            // Scale every pool's visual footprint from its remembered base size.
            foreach (var pool in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (pool.name != "Pool") continue;
                var baseKey = pool.GetComponent<PoolBaseScale>();
                if (baseKey == null)
                {
                    baseKey = pool.gameObject.AddComponent<PoolBaseScale>();
                    baseKey.baseScale = pool.localScale;
                }
                pool.localScale = new Vector3(baseKey.baseScale.x * mul,
                    baseKey.baseScale.y, baseKey.baseScale.z * mul);
            }
        }

        public static bool InWater(Vector3 p)
        {
            if (Instance == null) return false;
            foreach (var w in Instance.waters)
                if (new Vector2(p.x - w.x, p.z - w.z).sqrMagnitude < w.w * w.w) return true;
            return false;
        }

        /// <summary>True if the segment a→b crosses any cover obstacle (archer LoS).</summary>
        public static bool Blocked(Vector3 a, Vector3 b)
        {
            if (Instance == null) return false;
            var a2 = new Vector2(a.x, a.z);
            var b2 = new Vector2(b.x, b.z);
            foreach (var o in Instance.obstacles)
            {
                var c = new Vector2(o.x, o.z);
                var ab = b2 - a2;
                var t = Mathf.Clamp01(Vector2.Dot(c - a2, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                if ((a2 + ab * t - c).sqrMagnitude < o.w * o.w) return true;
            }
            return false;
        }

        /// <summary>Steering: repulsion away from obstacle circles for transform-driven AI.</summary>
        public static Vector3 Avoid(Vector3 pos)
        {
            if (Instance == null) return Vector3.zero;
            var push = Vector3.zero;
            foreach (var o in Instance.obstacles)
            {
                var d = new Vector3(pos.x - o.x, 0, pos.z - o.z);
                var dist = d.magnitude;
                var margin = o.w + 0.6f;
                if (dist < margin && dist > 0.01f)
                    push += d / dist * ((margin - dist) / margin) * 2.2f;
            }
            return push;
        }

        public static Vector3 RandomShadeSpawn(Vector3 fallback)
        {
            if (Instance == null || Instance.shadeSpawns.Count == 0) return fallback;
            var p = Instance.shadeSpawns[Random.Range(0, Instance.shadeSpawns.Count)];
            return p + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
        }
    }

    /// <summary>Remembers a marsh pool's authored scale so RaiseWater is idempotent.</summary>
    public class PoolBaseScale : MonoBehaviour
    {
        public Vector3 baseScale;
    }
}
