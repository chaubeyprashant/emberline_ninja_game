using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.Player
{
    /// <summary>
    /// Renzo's thrown kunai: the ninja's answer at range. Pooled like the
    /// archers' ember bolts so throws never touch the GC. Flies flat and fast,
    /// spins in the air, is eaten by chimney cover, and reports its hit back to
    /// the CombatController so kunai feed the combo/Sen loop like sword hits.
    /// </summary>
    public class Kunai : MonoBehaviour
    {
        private const float Speed = 17f;
        private const float Life = 1.1f; // ~19m of reach

        // One pool per thrown model — a bolt can't be recycled as a bomb.
        private static readonly Dictionary<string, Queue<Kunai>> Pools = new();
        private static Shader _glow; // string lookup — resolve once, not per throw

        private string _poolKey = "Kunai";

        /// <summary>Most recent kunai still in flight — the warp anchor. Null when none.</summary>
        public static Kunai Latest { get; private set; }

        /// <summary>
        /// Forget the pool and the warp anchor across a scene load: both point at
        /// objects the load destroyed. Called once per scene from GameManager.Awake.
        /// </summary>
        public static void ResetPool()
        {
            Pools.Clear();
            Latest = null;
        }

        private static Shader Glow => _glow != null ? _glow : _glow = Shader.Find("Emberline/Glow");

        private Vector3 _dir;
        private float _damage;
        private float _life;
        private CombatController _owner;

        /// <summary>
        /// Throw one. `prefabName` picks the Resources/Props model — kunai, bolt or
        /// bomb — so a weapon's thrown slot looks like its own ammunition. Pooled
        /// per model, since a bolt can't be recycled as a bomb.
        /// </summary>
        public static void Spawn(Vector3 at, Vector3 dir, float damage, CombatController owner,
            string prefabName = "Kunai")
        {
            if (string.IsNullOrEmpty(prefabName)) prefabName = "Kunai";
            if (!Pools.TryGetValue(prefabName, out var pool))
                Pools[prefabName] = pool = new Queue<Kunai>();

            Kunai k = null;
            while (pool.Count > 0 && k == null) k = pool.Dequeue(); // skip destroyed
            if (k == null)
            {
                // Baked KayKit prop prefab (bootstrap); primitive fallback.
                var prefab = Resources.Load<GameObject>("Props/" + prefabName)
                             ?? Resources.Load<GameObject>("Props/Kunai");
                GameObject go;
                if (prefab != null)
                {
                    go = Object.Instantiate(prefab);
                    go.name = "Kunai";
                }
                else
                {
                    go = new GameObject("Kunai");
                    var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(blade.GetComponent<Collider>());
                    blade.name = "Blade";
                    blade.transform.SetParent(go.transform, false);
                    blade.transform.localScale = new Vector3(0.07f, 0.07f, 0.55f);
                    var br = blade.GetComponent<Renderer>();
                    br.material = new Material(Glow) { color = new Color(0.8f, 0.88f, 1f) };
                    br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                // Thin pale trail so the throw reads against the night.
                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.09f;
                trail.endWidth = 0.01f;
                trail.material = new Material(Glow);
                trail.startColor = new Color(0.8f, 0.88f, 1f, 0.7f);
                trail.endColor = new Color(0.8f, 0.88f, 1f, 0f);
                k = go.AddComponent<Kunai>();
                k._poolKey = prefabName;
            }
            k.GetComponent<TrailRenderer>()?.Clear();
            k.gameObject.SetActive(true);
            k.transform.position = at;
            k.transform.rotation = Quaternion.LookRotation(dir);
            k._dir = dir.normalized;
            k._damage = damage;
            k._life = Life;
            k._owner = owner;
            Latest = k;
        }

        private void Despawn()
        {
            if (Latest == this) Latest = null;
            gameObject.SetActive(false);
            if (!Pools.TryGetValue(_poolKey, out var pool))
                Pools[_poolKey] = pool = new Queue<Kunai>();
            pool.Enqueue(this);
        }

        /// <summary>Spent as a warp anchor: burst where it hung, then recycle.</summary>
        public void ConsumeForWarp()
        {
            UI.FxPools.Sparks(transform.position, new Color(0.85f, 0.9f, 1f), 10);
            Despawn();
        }

        private void Update()
        {
            var prev = transform.position;
            transform.position += _dir * (Speed * Time.deltaTime); // flies point-first

            // Cover works both ways — chimneys eat kunai like they eat bolts.
            if (ArenaMarkers.Blocked(prev, transform.position))
            {
                UI.FxPools.Sparks(transform.position, new Color(0.85f, 0.9f, 1f), 5);
                Despawn();
                return;
            }
            if ((_life -= Time.deltaTime) <= 0) { Despawn(); return; }

            foreach (var brain in EnemyBrain.Active)
            {
                if (brain == null || brain.Dead) continue;
                var to = brain.transform.position + Vector3.up - transform.position;
                if (to.sqrMagnitude > 0.9f * 0.9f) continue;
                var dealt = brain.TakeHit(_damage, transform.position - _dir * 2f);
                Sfx3D.Hit();
                UI.FxPools.Sparks(brain.transform.position + Vector3.up * 1.1f,
                    new Color(0.85f, 0.9f, 1f));
                _owner?.OnKunaiHit(brain, dealt);
                Despawn();
                return;
            }
        }
    }
}
