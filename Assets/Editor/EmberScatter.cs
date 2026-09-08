using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Places props onto the zone terrain: forests, rock fields, village clutter.
    ///
    /// <para>
    /// Three rules the whole zone depends on:
    /// </para>
    /// <list type="number">
    ///   <item><b>Nothing floats.</b> Every prop is dropped to
    ///   <see cref="EmberTerrain.HeightAt"/> and sunk slightly, so it meets the
    ///   ground even on a slope.</item>
    ///   <item><b>Nothing blocks the road.</b> Placement is masked away from the
    ///   road, the village, the camp interior and the river, so the routes the
    ///   player needs stay walkable.</item>
    ///   <item><b>Colliders are authored, never mesh.</b> A forest of convex mesh
    ///   colliders is the fastest way to lose the frame budget on an A33, so a
    ///   tree gets one capsule around its trunk and nothing else.</item>
    /// </list>
    ///
    /// <para>
    /// Placement is deterministic: a fixed seed per call means a rebuild puts
    /// every tree back where it was, so the zone can be regenerated without the
    /// level design shifting under the gameplay that was tuned against it.
    /// </para>
    /// </summary>
    public static class EmberScatter
    {
        /// <summary>How a prop meets the ground and what collider it earns.</summary>
        public enum Body
        {
            /// <summary>No collider. Grass, flowers, small debris.</summary>
            None,
            /// <summary>Capsule around a trunk. Trees.</summary>
            Trunk,
            /// <summary>Box roughly matching the renderer bounds. Rocks, crates, structures.</summary>
            Solid,
        }

        public struct Spec
        {
            public GameObject prefab;
            public float weight;        // relative chance among the set
            public float minScale, maxScale;
            public Body body;
            public float trunkRadius;   // Trunk only
            public float sink;          // metres pushed into the ground
        }

        public static Spec P(GameObject prefab, float weight = 1f, float min = 0.9f,
                             float max = 1.15f, Body body = Body.Solid,
                             float trunkRadius = 0.5f, float sink = 0.1f)
            => new()
            {
                prefab = prefab, weight = weight, minScale = min, maxScale = max,
                body = body, trunkRadius = trunkRadius, sink = sink,
            };

        /// <summary>
        /// Scatters <paramref name="count"/> attempts of <paramref name="set"/>
        /// inside the zone, keeping only positions the mask accepts.
        /// </summary>
        /// <param name="accept">
        /// Given world X/Z and the ground height, returns 0..1 as the chance of
        /// keeping a candidate there. This is how a forest gets a soft edge
        /// instead of a hard circle.
        /// </param>
        public static int Scatter(Transform parent, IReadOnlyList<Spec> set, int count,
                                  int seed, System.Func<float, float, float, float> accept,
                                  float minSpacing = 0f)
        {
            if (set == null || set.Count == 0) return 0;

            var rng = new System.Random(seed);
            var total = 0f;
            foreach (var s in set) total += Mathf.Max(0.0001f, s.weight);

            var placed = new List<Vector2>();
            var made = 0;

            for (var i = 0; i < count; i++)
            {
                var x = (float)(rng.NextDouble() * 2 - 1) * EmberTerrain.Half;
                var z = (float)(rng.NextDouble() * 2 - 1) * EmberTerrain.Half;
                var h = EmberTerrain.HeightAt(x, z);

                if (rng.NextDouble() > accept(x, z, h)) continue;

                if (minSpacing > 0f)
                {
                    var tooClose = false;
                    var sq = minSpacing * minSpacing;
                    foreach (var p in placed)
                        if ((p.x - x) * (p.x - x) + (p.y - z) * (p.y - z) < sq) { tooClose = true; break; }
                    if (tooClose) continue;
                }

                // Pick from the weighted set.
                var roll = (float)rng.NextDouble() * total;
                var spec = set[set.Count - 1];
                foreach (var s in set)
                {
                    roll -= Mathf.Max(0.0001f, s.weight);
                    if (roll <= 0f) { spec = s; break; }
                }
                if (spec.prefab == null) continue;

                var scale = Mathf.Lerp(spec.minScale, spec.maxScale, (float)rng.NextDouble());
                Place(parent, spec, new Vector3(x, h - spec.sink, z),
                      (float)rng.NextDouble() * 360f, scale);

                placed.Add(new Vector2(x, z));
                made++;
            }

            return made;
        }

        /// <summary>Places one prop exactly, for hand-authored landmarks.</summary>
        public static GameObject Place(Transform parent, Spec spec, Vector3 pos,
                                       float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(spec.prefab, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;

            AddCollider(go, spec, scale);
            return go;
        }

        /// <summary>Places one prop by prefab, sitting it on the ground.</summary>
        public static GameObject PlaceOnGround(Transform parent, GameObject prefab,
                                               float x, float z, float yaw,
                                               float scale = 1f, Body body = Body.Solid,
                                               float sink = 0.1f)
        {
            if (prefab == null) return null;
            var spec = P(prefab, body: body, sink: sink);
            return Place(parent, spec, new Vector3(x, EmberTerrain.HeightAt(x, z) - sink, z),
                         yaw, scale);
        }

        private static void AddCollider(GameObject go, Spec spec, float scale)
        {
            // Imported meshes arrive with no collider; whatever they do bring, drop
            // it, so nothing sneaks a mesh collider into a forest.
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);

            switch (spec.body)
            {
                case Body.None:
                    return;

                case Body.Trunk:
                {
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.radius = spec.trunkRadius;
                    cap.height = 6f;
                    cap.center = new Vector3(0f, 3f, 0f);
                    return;
                }

                case Body.Solid:
                {
                    var box = go.AddComponent<BoxCollider>();
                    if (LocalBounds(go, out var b))
                    {
                        box.center = b.center;
                        box.size = b.size;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Bounds of a prop's meshes in the prop's own local space.
        ///
        /// <para>
        /// The obvious version — encapsulate <c>Renderer.bounds</c> and divide by
        /// scale — is wrong the moment the prop is rotated, because
        /// <c>Renderer.bounds</c> is a world-space axis-aligned box. For a fence
        /// turned 40 degrees that AABB is far larger than the fence, so the
        /// collider ends up oversized and skewed. Overlapping oversized boxes are
        /// how a CharacterController gets squeezed downward and drops through the
        /// floor, which is exactly what happened in the village.
        /// </para>
        ///
        /// <para>Mesh bounds are local by definition, so they are used instead and
        /// transformed only by each child's offset from the root.</para>
        /// </summary>
        private static bool LocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            var toRoot = root.transform.worldToLocalMatrix;

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;

                var m = toRoot * mf.transform.localToWorldMatrix;
                var mb = mesh.bounds;

                // All eight corners, so rotation of a child is accounted for.
                for (var i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any;
        }

        // ------------------------------------------------------------ masks

        /// <summary>
        /// 1 where the routes are clear, 0 on the road, in the village, inside the
        /// camp, and in the river. Every scatter multiplies by this.
        /// </summary>
        public static float Clearances(float x, float z)
        {
            if (EmberTerrain.RoadDistance(x, z) < 5.5f) return 0f;

            if (EmberTerrain.RiverDistance(x, z) < 11f) return 0f;

            var vil = Vector2.Distance(new Vector2(x, z),
                new Vector2(EmberTerrain.VillageCentre.x, EmberTerrain.VillageCentre.z));
            if (vil < EmberTerrain.VillageRadius) return 0f;

            var camp = Vector2.Distance(new Vector2(x, z),
                new Vector2(EmberTerrain.CampCentre.x, EmberTerrain.CampCentre.z));
            if (camp < EmberTerrain.CampRadius) return 0f;

            return 1f;
        }

        /// <summary>
        /// 0 inside the mission arena, 1 outside. Applied to anything with a
        /// collider — trees, rocks — but deliberately NOT to grass and flowers,
        /// which cost nothing to walk through and are what stop the clearing
        /// looking like a bald patch.
        /// </summary>
        public static float ArenaClear(float x, float z)
            => new Vector2(x, z).magnitude < 36f ? 0f : 1f;

        /// <summary>
        /// Forest density: two bands, north and south, fading at their edges so the
        /// treeline is ragged rather than a drawn circle. Steep ground and the
        /// mountain tops stay bare.
        /// </summary>
        public static float ForestMask(float x, float z, float h)
        {
            var clear = Clearances(x, z);
            if (clear <= 0f) return 0f;
            if (ArenaClear(x, z) <= 0f) return 0f;
            if (h > 15f) return 0f;                       // above the treeline

            // Bands: north of +40, south of -36. They used to start at +26/-22,
            // which put trunks inside the mission play area — a fight in a
            // thicket, and a player wedged between two capsule colliders. The
            // clearing has to be genuinely clear.
            var band = z > 40f ? Mathf.InverseLerp(40f, 54f, z)
                     : z < -36f ? Mathf.InverseLerp(-36f, -50f, z)
                     : 0f;
            if (band <= 0f) return 0f;

            // Ragged edge from the same noise the terrain uses.
            var n = EmberTerrain.Noise(x * 0.045f + 31f, z * 0.045f - 19f) * 0.5f + 0.5f;
            var d = Mathf.Clamp01(band * 1.15f) * Mathf.Clamp01(0.35f + n);

            // Thin out toward the mountains so trees do not climb the rim.
            var r = Mathf.Sqrt(x * x + z * z);
            d *= 1f - EmberTerrain.Smooth01(66f, 84f, r);
            return d;
        }
    }
}
