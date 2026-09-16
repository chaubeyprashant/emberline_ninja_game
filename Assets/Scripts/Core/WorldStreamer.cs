using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Streams the region around the player in 25 m chunks: terrain built from
    /// <see cref="ZoneTerrain"/>, a water quad where the chunk dips below the
    /// water line, scatter from a seed, and a landmark's dressing when its chunk
    /// arrives. The baked valley (|x|,|z| under 100 m) is left alone — those
    /// chunks are already in the scene with the campaign fought on them.
    ///
    /// <para>
    /// Nothing is ever loaded from disk: the height is a function, the props are
    /// prefabs already in Resources, so a chunk costs one mesh build, one
    /// collider bake and a few dozen instantiates. Two chunks a frame keeps a
    /// sprint across a chunk boundary under a millisecond a frame on the A33;
    /// a chunk is queued the moment it enters the radius and unloaded a ring
    /// later, so the horizon never pops in front of the player.
    /// </para>
    /// </summary>
    public class WorldStreamer : MonoBehaviour
    {
        public static WorldStreamer Instance { get; private set; }

        [Tooltip("Assigned by the scene builder: the same palette material the baked valley uses.")]
        public Material terrainMaterial;
        public Material waterMaterial;

        [Tooltip("Chunks within this many metres of the player are loaded.")]
        public float loadRadius = 175f;
        [Tooltip("Grass and small cover only this close: it is the layer that costs draw calls.")]
        public float coverRadius = 70f;
        [Tooltip("Chunks built per frame while the queue is non-empty.")]
        public int perFrame = 2;

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly Queue<Vector2Int> _queue = new();
        private readonly HashSet<Vector2Int> _queued = new();
        private readonly List<Vector2Int> _scratch = new();
        private Transform _player;
        private float _pollT;
        private Vector2Int _last = new(int.MinValue, int.MinValue);

        private class Chunk
        {
            public GameObject root;
            public Transform cover;
            public bool coverBuilt;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetPlayer(Transform t) => _player = t;

        private void Update()
        {
            if (_player == null)
            {
                var motor = SceneRefs.Motor;
                if (motor == null) return;
                _player = motor.transform;
            }

            _pollT -= Time.deltaTime;
            if (_pollT <= 0f)
            {
                _pollT = 0.25f;
                Refresh(_player.position);
            }

            for (var i = 0; i < perFrame && _queue.Count > 0; i++)
            {
                var c = _queue.Dequeue();
                _queued.Remove(c);
                if (!_chunks.ContainsKey(c)) Build(c);
            }
        }

        private static Vector2Int ChunkOf(Vector3 p) => new(
            Mathf.FloorToInt(p.x / TerrainMesh.ChunkSize),
            Mathf.FloorToInt(p.z / TerrainMesh.ChunkSize));

        /// <summary>True for a chunk the editor already baked into the scene.</summary>
        private static bool Baked(Vector2Int c)
        {
            var x0 = c.x * TerrainMesh.ChunkSize;
            var z0 = c.y * TerrainMesh.ChunkSize;
            return x0 >= -ZoneTerrain.Half && x0 + TerrainMesh.ChunkSize <= ZoneTerrain.Half
                   && z0 >= -ZoneTerrain.Half && z0 + TerrainMesh.ChunkSize <= ZoneTerrain.Half;
        }

        private static bool InRegion(Vector2Int c)
        {
            var lim = Mathf.RoundToInt(ZoneTerrain.RegionHalf / TerrainMesh.ChunkSize);
            return c.x >= -lim && c.x < lim && c.y >= -lim && c.y < lim;
        }

        private void Refresh(Vector3 p)
        {
            var centre = ChunkOf(p);
            var reach = Mathf.CeilToInt(loadRadius / TerrainMesh.ChunkSize);
            var r2 = loadRadius * loadRadius;
            var drop2 = (loadRadius + TerrainMesh.ChunkSize * 1.5f) * (loadRadius + TerrainMesh.ChunkSize * 1.5f);

            // Unload what has fallen out of the ring.
            _scratch.Clear();
            foreach (var kv in _chunks)
                if (Dist2(kv.Key, p) > drop2) _scratch.Add(kv.Key);
            foreach (var c in _scratch) Unload(c);

            // Grass comes and goes on its own, tighter, radius.
            var c2 = coverRadius * coverRadius;
            foreach (var kv in _chunks)
            {
                var near = Dist2(kv.Key, p) < c2;
                if (near && !kv.Value.coverBuilt) BuildCover(kv.Key, kv.Value);
                else if (!near && kv.Value.coverBuilt) DropCover(kv.Value);
            }

            if (centre == _last && _queue.Count == 0) return;
            _last = centre;

            // Queue nearest first so the ground under the feet always wins.
            _scratch.Clear();
            for (var dz = -reach; dz <= reach; dz++)
            for (var dx = -reach; dx <= reach; dx++)
            {
                var c = new Vector2Int(centre.x + dx, centre.y + dz);
                if (Baked(c) || !InRegion(c) || _chunks.ContainsKey(c) || _queued.Contains(c)) continue;
                if (Dist2(c, p) > r2) continue;
                _scratch.Add(c);
            }
            _scratch.Sort((a, b) => Dist2(a, p).CompareTo(Dist2(b, p)));
            foreach (var c in _scratch) { _queue.Enqueue(c); _queued.Add(c); }
        }

        private static float Dist2(Vector2Int c, Vector3 p)
        {
            var cx = (c.x + 0.5f) * TerrainMesh.ChunkSize;
            var cz = (c.y + 0.5f) * TerrainMesh.ChunkSize;
            var dx = cx - p.x; var dz = cz - p.z;
            return dx * dx + dz * dz;
        }

        private void Build(Vector2Int c)
        {
            var x0 = c.x * TerrainMesh.ChunkSize;
            var z0 = c.y * TerrainMesh.ChunkSize;
            var root = new GameObject($"Chunk_{c.x}_{c.y}");
            root.transform.SetParent(transform, false);

            var mesh = TerrainMesh.BuildChunk(x0, z0, root.name);
            var mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = terrainMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;

            if (waterMaterial != null && TerrainMesh.ChunkHasWater(x0, z0, out var level))
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(w.GetComponent<Collider>());
                w.name = "Water";
                w.transform.SetParent(root.transform, false);
                w.transform.position = new Vector3(x0 + TerrainMesh.ChunkSize * 0.5f, level, z0 + TerrainMesh.ChunkSize * 0.5f);
                w.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                w.transform.localScale = new Vector3(TerrainMesh.ChunkSize + 0.1f, TerrainMesh.ChunkSize + 0.1f, 1f);
                var wr = w.GetComponent<MeshRenderer>();
                wr.sharedMaterial = waterMaterial;
                wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var chunk = new Chunk { root = root };
            _chunks[c] = chunk;

            WorldScatter.Populate(root.transform, c.x, c.y, x0, z0);
            foreach (var l in WorldLandmarks.All)
                if (ChunkOf(l.centre) == c) WorldDressing.Build(root.transform, l);
        }

        private void BuildCover(Vector2Int c, Chunk chunk)
        {
            chunk.coverBuilt = true;
            var cover = new GameObject("Cover").transform;
            cover.SetParent(chunk.root.transform, false);
            chunk.cover = cover;
            WorldScatter.PopulateCover(cover, c.x, c.y, c.x * TerrainMesh.ChunkSize, c.y * TerrainMesh.ChunkSize);
        }

        private void DropCover(Chunk chunk)
        {
            chunk.coverBuilt = false;
            if (chunk.cover != null) Destroy(chunk.cover.gameObject);
            chunk.cover = null;
        }

        private void Unload(Vector2Int c)
        {
            if (!_chunks.TryGetValue(c, out var chunk)) return;
            _chunks.Remove(c);
            if (chunk.root == null) return;
            var mf = chunk.root.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
            Destroy(chunk.root);
        }

        /// <summary>Loaded chunk count, for the perf overlay.</summary>
        public int Loaded => _chunks.Count;
        public int Pending => _queue.Count;
    }
}
