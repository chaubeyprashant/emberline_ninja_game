using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.UI;

namespace Emberline
{
    /// <summary>
    /// "The Road North": endless-march corridor for the Endless launch mode.
    /// Opens the arena's north parapet into a road mouth, then streams rooftop
    /// causeway segments ahead of the player and reclaims them behind. Also owns
    /// the mist barrier that seals the road while a soldier pack is alive.
    /// Everything is built from runtime primitives so the batch bootstrap and
    /// the two authored theme scenes stay untouched.
    /// </summary>
    public class RoadNorth : MonoBehaviour
    {
        public static RoadNorth Instance { get; private set; }

        public const float HalfWidth = 7f;
        private const float MouthZ = 8.6f;    // arena north edge = first road plank
        private const float SegLen = 12f;
        private const float BuildAhead = 48f;
        private const float KeepBehind = 26f;

        public float StartZ { get; private set; }

        /// <summary>Live corridor segments — the streamer's load, for the perf overlay.</summary>
        public int SegmentCount => _segments.Count;

        private Transform _player;
        private float _builtToZ = MouthZ;
        private int _segIndex;
        private readonly List<Segment> _segments = new();
        private GameObject _barrier;
        private Material _barrierMat;
        private Material _deckMat, _trimMat, _ridgeMat, _chimneyMat, _skylineMat, _flameMat;
        private readonly System.Random _rng = new(1234);

        private struct Segment
        {
            public GameObject root;
            public float z;
            public Vector4 obstacle; // w == 0 → none registered
        }

        public static RoadNorth Begin(Transform player)
        {
            var road = new GameObject("RoadNorth").AddComponent<RoadNorth>();
            road._player = player;
            road.StartZ = player.position.z;
            Instance = road;
            road.BuildMaterials();
            road.OpenArenaMouth();
            road.StreamTo(player.position.z + BuildAhead);
            return road;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
            if (_player == null) return;
            StreamTo(_player.position.z + BuildAhead);

            // Reclaim segments the march has left behind (and their AI markers).
            for (var i = _segments.Count - 1; i >= 0; i--)
            {
                var seg = _segments[i];
                if (seg.z + SegLen > _player.position.z - KeepBehind) continue;
                if (seg.obstacle.w > 0f && ArenaMarkers.Instance != null)
                    ArenaMarkers.Instance.obstacles.Remove(seg.obstacle);
                Kill(seg.root);
                _segments.RemoveAt(i);
            }

            if (_barrier != null && _barrierMat != null)
                _barrierMat.color = new Color(0.6f, 0.78f, 0.95f,
                    0.3f + 0.09f * Mathf.Sin(Time.time * 3.2f));
        }

        /// <summary>
        /// Corridor clamp for the transform-driven enemies: replaces the arena
        /// box. No north limit; the road never runs south of the staging arena.
        /// </summary>
        public static Vector3 Clamp(Vector3 p, Vector2 arenaHalf)
        {
            p.x = Mathf.Clamp(p.x, -XLimitAt(p.z, arenaHalf.x), XLimitAt(p.z, arenaHalf.x));
            p.z = Mathf.Max(p.z, -arenaHalf.y);
            p.y = 0;
            return p;
        }

        /// <summary>
        /// Corridor half-width at a given z. Split out so the player can be kept
        /// in bounds without having its height flattened — jumping needs its y.
        /// </summary>
        public static float XLimitAt(float z, float arenaHalfX) =>
            z > MouthZ - 0.4f ? HalfWidth - 0.4f : arenaHalfX;

        // ------------------------------------------------------------ barrier

        /// <summary>Seal the road at `z` until the pack blocking it falls.</summary>
        public void RaiseBarrier(float z)
        {
            ClearBarrier(silent: true);
            _barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _barrier.name = "MistBarrier";
            // Tall enough that a jump — or a wall-run into a wall-jump — can't
            // clear it; the barrier has to actually seal the road.
            _barrier.transform.position = new Vector3(0, 3f, z);
            _barrier.transform.localScale = new Vector3(HalfWidth * 2f + 1.4f, 6f, 0.35f);
            _barrierMat = new Material(Shader.Find("Emberline/Ghost"));
            _barrierMat.color = new Color(0.6f, 0.78f, 0.95f, 0.32f);
            var r = _barrier.GetComponent<Renderer>();
            r.material = _barrierMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void ClearBarrier(bool silent = false)
        {
            if (_barrier == null) return;
            if (!silent)
            {
                FxPools.Embers(_barrier.transform.position, 22);
                Sfx3D.Surge();
            }
            Kill(_barrier);
            _barrier = null;
        }

        // ------------------------------------------------------- construction

        private void BuildMaterials()
        {
            var toon = Shader.Find("Emberline/Toon") ?? Shader.Find("Standard");
            Material M(Color c)
            {
                var m = new Material(toon) { color = c };
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.02f);
                return m;
            }
            _deckMat = M(new Color(0.15f, 0.18f, 0.23f));
            _trimMat = M(new Color(0.2f, 0.24f, 0.3f));
            _ridgeMat = M(new Color(0.12f, 0.15f, 0.2f));
            _chimneyMat = M(new Color(0.21f, 0.17f, 0.19f));
            _skylineMat = M(new Color(0.08f, 0.10f, 0.15f));
            _flameMat = new Material(Shader.Find("Emberline/Glow") ?? toon)
                { color = new Color(1f, 0.62f, 0.35f) };
        }

        /// <summary>
        /// Swap the arena's solid north parapet for two flanking stubs, leaving
        /// a HalfWidth-wide mouth the road grows out of.
        /// </summary>
        private void OpenArenaMouth()
        {
            foreach (var go in FindObjectsByType<Transform>())
            {
                if (go == null || go.name != "Parapet" || go.position.z < 8f) continue;
                Kill(go.gameObject);
            }
            foreach (var side in new[] { -1f, 1f })
            {
                var stub = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stub.name = "RoadMouth";
                stub.transform.position = new Vector3(side * (HalfWidth + 3.4f), 0.4f, MouthZ);
                stub.transform.localScale = new Vector3(6.6f, 0.8f, 0.6f);
                stub.GetComponent<Renderer>().sharedMaterial = _trimMat;
            }
        }

        private void StreamTo(float z)
        {
            while (_builtToZ < z) BuildSegment();
        }

        private void BuildSegment()
        {
            var z0 = _builtToZ;
            _builtToZ += SegLen;
            var index = _segIndex++;
            var root = new GameObject($"RoadSeg_{index}");
            root.transform.position = Vector3.zero;
            var mid = z0 + SegLen * 0.5f;

            GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat,
                bool collider = true, bool shadows = true)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!collider) Kill(c.GetComponent<Collider>());
                c.name = name;
                c.transform.SetParent(root.transform, false);
                c.transform.position = pos;
                c.transform.localScale = scale;
                var r = c.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return c;
            }

            // Causeway deck + side parapets (the walls that make it a road).
            Cube("RoadDeck", new Vector3(0, -0.25f, mid),
                new Vector3(HalfWidth * 2f + 2f, 0.5f, SegLen), _deckMat);
            foreach (var side in new[] { -1f, 1f })
                Cube("RoadParapet", new Vector3(side * (HalfWidth + 0.7f), 0.4f, mid),
                    new Vector3(0.6f, 0.8f, SegLen), _trimMat);

            // Plank seams across the road for a sense of speed.
            for (var z = z0 + 2f; z < z0 + SegLen; z += 4f)
                Cube("RoadRidge", new Vector3(0, 0.015f, z),
                    new Vector3(HalfWidth * 2f + 1.2f, 0.04f, 0.12f), _ridgeMat,
                    collider: false, shadows: false);

            // Occasional chimney: real cover — blocks bolts, steers the AI.
            var obstacle = Vector4.zero;
            if (_rng.NextDouble() < 0.4)
            {
                var cx = (float)(_rng.NextDouble() * 9 - 4.5);
                var cz = z0 + 2.5f + (float)_rng.NextDouble() * (SegLen - 5f);
                Cube("Chimney", new Vector3(cx, 0.8f, cz), new Vector3(1.5f, 1.6f, 1.5f), _chimneyMat);
                Cube("ChimneyCap", new Vector3(cx, 1.7f, cz), new Vector3(1.8f, 0.22f, 1.8f),
                    _chimneyMat, collider: false);
                obstacle = new Vector4(cx, 0, cz, 1.15f);
                if (ArenaMarkers.Instance != null) ArenaMarkers.Instance.obstacles.Add(obstacle);
            }

            // Every other segment: destructible lanterns on both parapets.
            if (index % 2 == 0)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var basePos = new Vector3(side * (HalfWidth - 0.6f), 0, mid);
                    var post = Cube("LanternPost", basePos + Vector3.up * 0.9f,
                        new Vector3(0.15f, 1.8f, 0.15f), _trimMat);
                    var bulb = Cube("Lantern", basePos + Vector3.up * 1.9f,
                        Vector3.one * 0.35f, _flameMat, collider: false);
                    var light = new GameObject("LanternLight").AddComponent<Light>();
                    light.transform.SetParent(root.transform, false);
                    light.transform.position = basePos + Vector3.up * 2.1f;
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.55f, 0.3f);
                    light.intensity = 2f;
                    light.range = 8f;
                    // Vertex-lit: the four arena lanterns own the pixel-light budget.
                    // With the toon shader's additive pass, every pixel light costs
                    // an extra draw of every renderer it touches, and the corridor
                    // would otherwise add two more of them every second segment.
                    light.renderMode = LightRenderMode.ForceVertex;
                    var postComp = post.AddComponent<LanternPost>();
                    postComp.bulb = bulb;
                    postComp.glow = light;
                }
            }

            // Skyline silhouettes drifting past beyond the parapets — depth only.
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1f : 1f;
                Cube("SkylineRoof", new Vector3(
                        side * (11f + (float)_rng.NextDouble() * 8f),
                        (float)(_rng.NextDouble() * 2.5 - 2.5),
                        z0 + (float)_rng.NextDouble() * SegLen),
                    new Vector3(3.5f + (float)_rng.NextDouble() * 5f,
                        2.5f + (float)_rng.NextDouble() * 3f,
                        3f + (float)_rng.NextDouble() * 3f),
                    _skylineMat, collider: false, shadows: false);
            }

            // One batched mesh per segment instead of ~15 loose renderers. The
            // segment never moves after this, which is exactly the case static
            // batching wants; reclaiming it later destroys the combined mesh too.
            StaticBatchingUtility.Combine(root);
            _segments.Add(new Segment { root = root, z = z0, obstacle = obstacle });
        }
    }
}
