using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Runtime scatter for the streamed region: the same three rules the editor's
    /// scatter follows (nothing floats, nothing blocks a road, colliders are
    /// authored and never mesh), from a seed per chunk so the forest is the same
    /// forest every visit. Prefabs come from Resources/Props/Zone, loaded once.
    /// </summary>
    public static class WorldScatter
    {
        public enum Body { None, Trunk, Solid }

        public struct Spec
        {
            public string prefab;
            public float weight, minScale, maxScale, trunkRadius, sink;
            public Body body;
        }

        private static Spec P(string prefab, float weight, float min, float max, Body body,
            float trunkRadius = 0.5f, float sink = 0.1f) => new()
        {
            prefab = prefab, weight = weight, minScale = min, maxScale = max, body = body,
            trunkRadius = trunkRadius, sink = sink,
        };

        private static readonly Dictionary<string, GameObject> Cache = new();

        public static GameObject Prefab(string name)
        {
            if (Cache.TryGetValue(name, out var go)) return go;
            go = Resources.Load<GameObject>("Props/Zone/" + name);
            if (go == null) Debug.LogWarning($"[World] missing prop {name}");
            Cache[name] = go;
            return go;
        }

        // ------------------------------------------------------------- sets

        private static readonly Spec[] Pines =
        {
            P("tree_pineTallA", 3f, 4.5f, 7.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineTallB", 3f, 4.5f, 7.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineTallC", 3f, 4.5f, 7.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineTallD", 3f, 4.5f, 7.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineRoundA", 2f, 4.5f, 7f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineRoundC", 2f, 4.5f, 7f, Body.Trunk, 0.5f, 0.3f),
            P("tree_pineSmallA", 1.4f, 3.5f, 5.5f, Body.Trunk, 0.4f, 0.3f),
            P("tree_pineDefaultA", 1.4f, 3.5f, 5.5f, Body.Trunk, 0.4f, 0.3f),
            P("tree_default", 0.7f, 4f, 6.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_tall", 0.7f, 4f, 6.5f, Body.Trunk, 0.5f, 0.3f),
            P("tree_thin", 0.5f, 4f, 6.5f, Body.Trunk, 0.4f, 0.3f),
        };

        private static readonly Spec[] MarshTrees =
        {
            P("tree_thin_dark", 3f, 4f, 7f, Body.Trunk, 0.4f, 0.4f),
            P("tree_default_dark", 2f, 4f, 6.5f, Body.Trunk, 0.5f, 0.4f),
            P("tree_tall_dark", 2f, 4f, 7f, Body.Trunk, 0.5f, 0.4f),
            P("stump_old", 1f, 3f, 4.5f, Body.Solid, sink: 0.2f),
            P("log_large", 0.8f, 3f, 4.5f, Body.Solid, sink: 0.25f),
        };

        private static readonly Spec[] Rocks =
        {
            P("rock_largeA", 1.4f, 2.5f, 5f, Body.Solid, sink: 0.4f),
            P("rock_largeB", 1.4f, 2.5f, 5f, Body.Solid, sink: 0.4f),
            P("rock_largeD", 1.4f, 2.5f, 5f, Body.Solid, sink: 0.4f),
            P("rock_tallA", 1.2f, 2.5f, 5.5f, Body.Solid, sink: 0.4f),
            P("rock_tallC", 1.2f, 2.5f, 5.5f, Body.Solid, sink: 0.4f),
            P("rock_smallA", 2f, 2.5f, 4.5f, Body.Solid, sink: 0.3f),
            P("rock_smallE", 2f, 2.5f, 4.5f, Body.Solid, sink: 0.3f),
            P("rock_smallFlatA", 2f, 2.5f, 4.5f, Body.Solid, sink: 0.3f),
            P("stone_largeA", 1f, 3f, 6f, Body.Solid, sink: 0.4f),
            P("stone_tallB", 1f, 3f, 6f, Body.Solid, sink: 0.4f),
        };

        private static readonly Spec[] Cover =
        {
            P("grass", 4f, 3f, 5f, Body.None, sink: 0.1f),
            P("grass_large", 3f, 3f, 5f, Body.None, sink: 0.1f),
            P("grass_leafs", 2f, 3f, 4.5f, Body.None, sink: 0.1f),
            P("plant_bush", 2f, 3f, 5f, Body.None, sink: 0.15f),
            P("plant_bushSmall", 2f, 3f, 5f, Body.None, sink: 0.15f),
            P("mushroom_tanGroup", 0.4f, 3f, 4f, Body.None, sink: 0.05f),
            P("flower_purpleA", 0.3f, 3f, 4f, Body.None, sink: 0.05f),
        };

        private static readonly Spec[] Reeds =
        {
            P("crops_bambooStageB", 3f, 2.6f, 4f, Body.None, sink: 0.2f),
            P("crops_bambooStageA", 2f, 2.6f, 3.6f, Body.None, sink: 0.2f),
            P("grass_leafsLarge", 3f, 3.5f, 5.5f, Body.None, sink: 0.15f),
        };

        // ------------------------------------------------------------ masks

        /// <summary>0 where nothing may be scattered: roads, water, landmark floors.</summary>
        public static float Clearance(float x, float z, float h)
        {
            if (ZoneTerrain.RoadDistance(x, z) < 6f) return 0f;
            if (h < ZoneTerrain.WaterLevelAt(x, z) + 0.4f) return 0f;
            if (Dist(x, z, ZoneTerrain.TempleHill) < ZoneTerrain.TempleRadius + 6f) return 0f;
            if (Dist(x, z, ZoneTerrain.Stronghold) < ZoneTerrain.StrongholdRadius + 4f) return 0f;
            if (Dist(x, z, ZoneTerrain.Hamlet) < ZoneTerrain.HamletRadius) return 0f;
            if (Dist(x, z, ZoneTerrain.Hideout) < ZoneTerrain.HideoutRadius) return 0f;
            if (Dist(x, z, ZoneTerrain.CaveMouth) < ZoneTerrain.CaveRadius + 4f) return 0f;
            if (Dist(x, z, ZoneTerrain.Waterfall) < 16f) return 0f;
            return 1f;
        }

        /// <summary>Forest bands from low-frequency noise, thinning with height.</summary>
        public static float Forest(float x, float z, float h)
        {
            if (h > 40f) return 0f;
            var n = ZoneTerrain.Noise(x * 0.0045f + 21f, z * 0.0045f - 13f);
            var band = Mathf.Clamp01((n - 0.02f) / 0.35f);
            // The hideout hides in the thickest wood in the region.
            var hide = 1f - ZoneTerrain.Smooth01(ZoneTerrain.HideoutRadius, 90f, Dist(x, z, ZoneTerrain.Hideout));
            band = Mathf.Max(band, hide * 0.9f);
            return band * Mathf.Lerp(1f, 0.25f, ZoneTerrain.Smooth01(24f, 40f, h));
        }

        private static float Dist(float x, float z, Vector3 c)
        {
            var dx = x - c.x; var dz = z - c.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ---------------------------------------------------------- populate

        /// <summary>Trees, rocks and marsh growth for one chunk. Deterministic per chunk.</summary>
        public static void Populate(Transform parent, int cx, int cz, float x0, float z0)
        {
            var rng = new System.Random(unchecked(cx * 73856093 ^ cz * 19349663 ^ 20260916));
            var placed = new List<Vector2>(48);

            // Trees: about forty attempts a chunk; a dense band keeps ten or so.
            Scatter(parent, rng, Pines, 40, x0, z0, placed, 3.4f, (x, z, h) =>
            {
                if (Clearance(x, z, h) <= 0f) return 0f;
                if (ZoneTerrain.MarshMask(x, z) > 0.3f) return 0f;
                if (ZoneTerrain.SlopeAt(x, z) > 0.6f) return 0f;
                return Forest(x, z, h);
            });
            // Dead wood and reeds in the marsh.
            Scatter(parent, rng, MarshTrees, 14, x0, z0, placed, 4f, (x, z, h) =>
            {
                var m = ZoneTerrain.MarshMask(x, z);
                if (m < 0.35f || Clearance(x, z, h) <= 0f) return 0f;
                return h > ZoneTerrain.WaterLevel + 0.3f ? m * 0.6f : 0f;
            });
            // Rocks: sparse on the meadow, heavy on the slopes and scree.
            Scatter(parent, rng, Rocks, 10, x0, z0, placed, 3.2f, (x, z, h) =>
            {
                if (Clearance(x, z, h) <= 0f) return 0f;
                var s = ZoneTerrain.SlopeAt(x, z);
                if (s > 0.55f) return 0f;
                return Mathf.Lerp(0.1f, 0.9f, ZoneTerrain.Smooth01(0.12f, 0.4f, s)) * (h > 16f ? 1.2f : 0.7f);
            });
        }

        /// <summary>Grass and reeds: no colliders, no shadows, built only near the player.</summary>
        public static void PopulateCover(Transform parent, int cx, int cz, float x0, float z0)
        {
            var rng = new System.Random(unchecked(cx * 83492791 ^ cz * 2971215073u.GetHashCode() ^ 7));
            var placed = new List<Vector2>(40);
            Scatter(parent, rng, Cover, 36, x0, z0, placed, 1.6f, (x, z, h) =>
            {
                if (Clearance(x, z, h) <= 0f || h > 17f) return 0f;
                if (ZoneTerrain.MarshMask(x, z) > 0.4f) return 0f;
                return Forest(x, z, h) > 0.1f ? 0.85f : 0.4f;
            });
            Scatter(parent, rng, Reeds, 24, x0, z0, placed, 1.8f, (x, z, h) =>
            {
                var marsh = ZoneTerrain.MarshMask(x, z);
                var river = ZoneTerrain.RiverDistance(x, z);
                var bank = river > 4.5f && river < 15f;
                if (Clearance(x, z, h) <= 0f && !(marsh > 0.4f && h > ZoneTerrain.WaterLevel + 0.15f)) return 0f;
                return marsh > 0.4f ? 0.7f : bank ? 0.8f : 0f;
            });
            foreach (var r in parent.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Scatter(Transform parent, System.Random rng, Spec[] set, int attempts,
            float x0, float z0, List<Vector2> placed, float minSpacing,
            System.Func<float, float, float, float> accept)
        {
            var total = 0f;
            foreach (var s in set) total += s.weight;
            for (var i = 0; i < attempts; i++)
            {
                var x = x0 + (float)rng.NextDouble() * TerrainMesh.ChunkSize;
                var z = z0 + (float)rng.NextDouble() * TerrainMesh.ChunkSize;
                var h = ZoneTerrain.HeightAt(x, z);
                if (rng.NextDouble() > accept(x, z, h)) continue;

                var sq = minSpacing * minSpacing;
                var close = false;
                foreach (var p in placed)
                    if ((p.x - x) * (p.x - x) + (p.y - z) * (p.y - z) < sq) { close = true; break; }
                if (close) continue;

                var roll = (float)rng.NextDouble() * total;
                var spec = set[set.Length - 1];
                foreach (var s in set) { roll -= s.weight; if (roll <= 0f) { spec = s; break; } }

                var scale = Mathf.Lerp(spec.minScale, spec.maxScale, (float)rng.NextDouble());
                Place(parent, spec, new Vector3(x, h - spec.sink, z), (float)rng.NextDouble() * 360f, scale);
                placed.Add(new Vector2(x, z));
            }
        }

        /// <summary>One prop, on the ground, with its authored collider.</summary>
        public static GameObject Place(Transform parent, Spec spec, Vector3 pos, float yaw, float scale)
        {
            var prefab = Prefab(spec.prefab);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            switch (spec.body)
            {
                case Body.Trunk:
                {
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.radius = spec.trunkRadius;
                    cap.height = 6f;
                    cap.center = new Vector3(0f, 3f, 0f);
                    break;
                }
                case Body.Solid:
                {
                    var box = go.AddComponent<BoxCollider>();
                    if (LocalBounds(go, out var b)) { box.center = b.center; box.size = b.size; }
                    break;
                }
            }
            return go;
        }

        /// <summary>Place a named prefab on the ground with a box collider; for dressing.</summary>
        public static GameObject PlaceOnGround(Transform parent, string name, float x, float z, float yaw,
            float scale = 3f, Body body = Body.Solid, float sink = 0.1f)
        {
            var spec = P(name, 1f, scale, scale, body, sink: sink);
            return Place(parent, spec, new Vector3(x, ZoneTerrain.HeightAt(x, z) - sink, z), yaw, scale);
        }

        /// <summary>Bounds of a prop's meshes in the prop's own local space.</summary>
        public static bool LocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            var toRoot = root.transform.worldToLocalMatrix;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var m = toRoot * mf.transform.localToWorldMatrix;
                var b = mf.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any;
        }
    }
}
