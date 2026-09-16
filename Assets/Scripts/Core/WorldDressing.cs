using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// The region's landmarks, assembled at runtime from the same kits the baked
    /// valley uses. Each composition is a handful of props on a root, colliders
    /// authored per piece, an <see cref="Interactable"/> where the place has a
    /// use. Built when the landmark's chunk streams in, destroyed with it.
    ///
    /// <para>
    /// These are first passes: enough silhouette to make each place read from
    /// the road and enough floor to fight on. Phase 8 replaces the timber with a
    /// Japanese kit on the same palette; Phase 6 puts people in them.
    /// </para>
    /// </summary>
    public static class WorldDressing
    {
        private const float Cell = 3.0f;

        public static void Build(Transform parent, Landmark l)
        {
            var root = new GameObject("Landmark_" + l.id).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(l.Ground, Quaternion.Euler(0f, l.yaw, 0f));
            switch (l.kind)
            {
                case LandmarkKind.Temple: Temple(root); break;
                case LandmarkKind.Stronghold: Stronghold(root); break;
                case LandmarkKind.Cave: Cave(root); break;
                case LandmarkKind.Marsh: DrownedShrine(root); break;
                case LandmarkKind.Hamlet: Hamlet(root); break;
                case LandmarkKind.Hideout: Hideout(root); break;
                case LandmarkKind.Waterfall: Waterfall(root); break;
                case LandmarkKind.Pass: Pass(root); break;
            }
        }

        // ------------------------------------------------------------ pieces

        private static GameObject Piece(Transform root, string name, float lx, float ly, float lz, float yaw,
            float scale = Cell, bool collide = true)
        {
            var prefab = WorldScatter.Prefab(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, root);
            go.transform.localPosition = new Vector3(lx, ly, lz);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            if (collide)
            {
                var box = go.AddComponent<BoxCollider>();
                if (WorldScatter.LocalBounds(go, out var b)) { box.center = b.center; box.size = b.size; }
            }
            return go;
        }

        /// <summary>A prop dropped onto the terrain at a local offset from the root.</summary>
        private static GameObject OnGround(Transform root, string name, float lx, float lz, float yaw,
            float scale = Cell, bool collide = true, float sink = 0.1f)
        {
            var world = root.TransformPoint(new Vector3(lx, 0f, lz));
            var go = Piece(root, name, lx, 0f, lz, yaw, scale, collide);
            if (go == null) return null;
            go.transform.position = new Vector3(world.x, ZoneTerrain.HeightAt(world.x, world.z) - sink, world.z);
            return go;
        }

        private static float PrefabHeight(string name)
        {
            var prefab = WorldScatter.Prefab(name);
            if (prefab == null) return 1f;
            var any = false;
            var b = new Bounds();
            foreach (var r in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b.size.y : 1f;
        }

        private static void Tower(Transform parent, float lx, float lz, float yaw)
        {
            var root = new GameObject("Watchtower").transform;
            root.SetParent(parent, false);
            var world = parent.TransformPoint(new Vector3(lx, 0f, lz));
            root.position = new Vector3(world.x, ZoneTerrain.HeightAt(world.x, world.z) - 0.2f, world.z);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var y = 0f;
            foreach (var name in new[]
                     {
                         "castle_tower-square-base", "castle_tower-square-mid",
                         "castle_tower-square-mid-windows", "castle_tower-square-top",
                         "castle_tower-square-top-roof",
                     })
            {
                Piece(root, name, 0f, y, 0f, 0f, Cell, collide: false);
                y += PrefabHeight(name) * Cell;
            }
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, y * 0.5f, 0f);
            box.size = new Vector3(Cell, y, Cell);
        }

        private static void Torii(Transform parent, float lx, float lz, float yaw, float scale)
        {
            var root = new GameObject("Torii").transform;
            root.SetParent(parent, false);
            var world = parent.TransformPoint(new Vector3(lx, 0f, lz));
            root.position = new Vector3(world.x, ZoneTerrain.HeightAt(world.x, world.z) - 0.1f, world.z);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var side in new[] { -1f, 1f })
            {
                var p = Piece(root, "town_pillar-wood", side * scale * 0.55f, 0f, 0f, 0f, Cell, collide: false);
                if (p != null) p.transform.localScale = new Vector3(Cell * 0.45f, scale * 1.15f, Cell * 0.45f);
            }
            var lintel = Piece(root, "town_poles-horizontal", 0f, scale * 1.1f, 0f, 0f, Cell, collide: false);
            if (lintel != null) lintel.transform.localScale = new Vector3(scale * 0.75f, Cell * 0.6f, Cell * 0.6f);
            var beam = Piece(root, "town_poles-horizontal", 0f, scale * 0.92f, 0f, 0f, Cell, collide: false);
            if (beam != null) beam.transform.localScale = new Vector3(scale * 0.6f, Cell * 0.4f, Cell * 0.4f);
            foreach (var side in new[] { -1f, 1f })
            {
                var col = root.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(side * scale * 0.55f, scale * 0.6f, 0f);
                col.size = new Vector3(0.7f, scale * 1.2f, 0.7f);
            }
        }

        private static Transform ShrineHouse(Transform parent, float lx, float lz, float yaw, float scale = 1f, float sink = 0.15f)
        {
            var root = new GameObject("Shrine").transform;
            root.SetParent(parent, false);
            var world = parent.TransformPoint(new Vector3(lx, 0f, lz));
            root.position = new Vector3(world.x, ZoneTerrain.HeightAt(world.x, world.z) - sink, world.z);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.localScale = Vector3.one * scale;
            Piece(root, "town_planks", 0f, 0f, 0f, 0f, Cell, collide: false);
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var p = Piece(root, "town_pillar-wood", dx * Cell * 0.5f, 0.4f, dz * Cell * 0.5f, 0f, Cell, collide: false);
                if (p != null) p.transform.localScale = new Vector3(Cell * 0.4f, Cell * 0.9f, Cell * 0.4f);
            }
            Piece(root, "town_roof-high", 0f, Cell * 0.95f, -Cell * 0.5f, 0f, Cell, collide: false);
            Piece(root, "town_roof-high", 0f, Cell * 0.95f, Cell * 0.5f, 180f, Cell, collide: false);
            foreach (var side in new[] { -1f, 1f })
                Piece(root, "town_lantern", side * Cell * 0.9f, 0f, -Cell * 1.2f, 0f, 2.2f, collide: false);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Cell * 0.6f, 0f);
            box.size = new Vector3(Cell * 1.6f, Cell * 1.2f, Cell * 1.6f);
            return root;
        }

        private static void House(Transform parent, float lx, float lz, float yaw, bool wood)
        {
            var root = new GameObject("House").transform;
            root.SetParent(parent, false);
            var world = parent.TransformPoint(new Vector3(lx, 0f, lz));
            root.position = new Vector3(world.x, ZoneTerrain.HeightAt(world.x, world.z) - 0.1f, world.z);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wall = wood ? "town_wall-wood" : "town_wall";
            var win = wood ? "town_wall-wood-window-shutters" : "town_wall-window-shutters";
            var door = wood ? "town_wall-wood-door" : "town_wall-door";
            // A two-by-two footprint: door on the front, windows on the sides.
            Piece(root, door, -Cell * 0.5f, 0f, -Cell, 0f, Cell, collide: false);
            Piece(root, win, Cell * 0.5f, 0f, -Cell, 0f, Cell, collide: false);
            Piece(root, wall, -Cell * 0.5f, 0f, Cell, 180f, Cell, collide: false);
            Piece(root, win, Cell * 0.5f, 0f, Cell, 180f, Cell, collide: false);
            Piece(root, win, -Cell, 0f, -Cell * 0.5f, 270f, Cell, collide: false);
            Piece(root, wall, -Cell, 0f, Cell * 0.5f, 270f, Cell, collide: false);
            Piece(root, wall, Cell, 0f, -Cell * 0.5f, 90f, Cell, collide: false);
            Piece(root, win, Cell, 0f, Cell * 0.5f, 90f, Cell, collide: false);
            Piece(root, "town_roof-gable", -Cell * 0.5f, Cell, 0f, 90f, Cell, collide: false);
            Piece(root, "town_roof-gable", Cell * 0.5f, Cell, 0f, 90f, Cell, collide: false);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Cell * 0.8f, 0f);
            box.size = new Vector3(Cell * 2f, Cell * 1.6f, Cell * 2f);
        }

        private static void Fence(Transform parent, Vector2 a, Vector2 b, string prefab = "town_fence")
        {
            var d = b - a;
            var len = d.magnitude;
            if (len < 0.5f) return;
            var yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + 90f;
            var n = Mathf.Max(1, Mathf.RoundToInt(len / Cell));
            for (var i = 0; i < n; i++)
            {
                var t = (i + 0.5f) / n;
                var p = a + d * t;
                OnGround(parent, prefab, p.x, p.y, yaw, Cell, collide: true, sink: 0.15f);
            }
        }

        // ------------------------------------------------------ compositions

        /// <summary>The hill temple: an approach of lanterns and gates to a great roof.</summary>
        private static void Temple(Transform root)
        {
            // Approach from the road, up the last of the hill.
            for (var i = 0; i < 5; i++)
            {
                var z = -24f + i * 5f;
                OnGround(root, "town_lantern", -3.2f, z, 0f, 2.2f, collide: false);
                OnGround(root, "town_lantern", 3.2f, z, 0f, 2.2f, collide: false);
            }
            Torii(root, 0f, -26f, 0f, 7.5f);
            Torii(root, 0f, -10f, 0f, 6f);

            // The hall: a wide platform, a colonnade, two stacked roofs.
            var hall = new GameObject("Hall").transform;
            hall.SetParent(root, false);
            hall.localPosition = new Vector3(0f, -0.1f, 4f);
            for (var x = -1; x <= 1; x++)
            for (var z = -1; z <= 1; z++)
                Piece(hall, "town_planks", x * Cell, 0f, z * Cell, 0f, Cell, collide: false);
            foreach (var (dx, dz) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (-1.4f, 1.4f), (1.4f, 1.4f), (0f, -1.4f), (0f, 1.4f) })
            {
                var p = Piece(hall, "town_pillar-wood", dx * Cell, 0.3f, dz * Cell, 0f, Cell, collide: false);
                if (p != null) p.transform.localScale = new Vector3(Cell * 0.5f, Cell * 1.6f, Cell * 0.5f);
            }
            for (var x = -1; x <= 1; x++)
            {
                var r1 = Piece(hall, "town_roof-high", x * Cell, Cell * 1.7f, -Cell, 0f, Cell, collide: false);
                var r2 = Piece(hall, "town_roof-high", x * Cell, Cell * 1.7f, Cell, 180f, Cell, collide: false);
                if (r1 != null) r1.transform.localScale = new Vector3(Cell, Cell * 1.3f, Cell * 1.4f);
                if (r2 != null) r2.transform.localScale = new Vector3(Cell, Cell * 1.3f, Cell * 1.4f);
            }
            var box = hall.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Cell * 0.8f, 0f);
            box.size = new Vector3(Cell * 3.2f, Cell * 1.6f, Cell * 3.2f);
            hall.gameObject.AddComponent<Shrine>().shrineId = "temple";

            // Stone markers and a bell-less yard.
            foreach (var (x, z, y) in new[] { (-10f, 2f, 20f), (10f, 3f, -25f), (-8f, 12f, 60f), (9f, 13f, 110f) })
                OnGround(root, "town_pillar-stone", x, z, y, Cell * 0.8f);
            OnGround(root, "castle_flag", -6f, -4f, 0f, Cell * 0.8f, collide: false);
            OnGround(root, "castle_flag", 6f, -4f, 0f, Cell * 0.8f, collide: false);
        }

        /// <summary>Kagehira's stronghold: a walled yard on the plateau, towers on the corners.</summary>
        private static void Stronghold(Transform root)
        {
            const float r = 30f;
            // A ring of wall with a gate toward the road (local -Z).
            const int segs = 28;
            for (var i = 0; i < segs; i++)
            {
                var a = (i + 0.5f) / segs * 360f;
                if (a > 170f && a < 190f) continue; // the gate
                var rad = a * Mathf.Deg2Rad;
                var x = Mathf.Sin(rad) * r;
                var z = Mathf.Cos(rad) * r;
                OnGround(root, "castle_wall-narrow-wood", x, z, a + 90f, Cell, collide: true, sink: 0.4f);
            }
            OnGround(root, "castle_wall-narrow-gate", 0f, -r, 90f, Cell, collide: false, sink: 0.4f);
            foreach (var (x, z) in new[] { (-r * 0.72f, -r * 0.72f), (r * 0.72f, -r * 0.72f), (-r * 0.72f, r * 0.72f), (r * 0.72f, r * 0.72f) })
                Tower(root, x, z, 0f);

            // The yard: tents, a forge, racks, a fire, the loot Kagehira's men took.
            OnGround(root, "survival_tent", -10f, 8f, 30f, 4f);
            OnGround(root, "survival_tent", -4f, 14f, 340f, 4f);
            OnGround(root, "survival_tent-canvas", 8f, 12f, 200f, 4f);
            OnGround(root, "survival_workbench-anvil", 12f, -4f, 90f, Cell);
            OnGround(root, "weaponrack", 14f, 0f, 270f, Cell);
            OnGround(root, "weaponrack", 14f, 4f, 270f, Cell);
            OnGround(root, "survival_campfire-pit", 0f, 2f, 0f, Cell, collide: false);
            OnGround(root, "jp_ricebale_stack", -12f, -6f, 20f, Cell);
            OnGround(root, "jp_barrel_a", -14f, -2f, 0f, Cell);
            OnGround(root, "jp_barrel_b", -13f, 0f, 40f, Cell);
            OnGround(root, "town_cart", 6f, -12f, 120f, Cell);
            OnGround(root, "castle_flag", 0f, -r + 4f, 0f, Cell * 0.9f, collide: false);
            OnGround(root, "target", 16f, 10f, 180f, Cell);
            OnGround(root, "target", 18f, 6f, 180f, Cell);
        }

        /// <summary>The hollow under the ridge: rock arches over a sunken camp.</summary>
        private static void Cave(Transform root)
        {
            const int n = 9;
            for (var i = 0; i < n; i++)
            {
                var a = i / (float)n * 360f;
                var rad = a * Mathf.Deg2Rad;
                var d = ZoneTerrain.CaveRadius + 2f;
                var name = i % 3 == 0 ? "rock_largeB" : i % 3 == 1 ? "rock_tallE" : "rock_largeF";
                OnGround(root, name, Mathf.Sin(rad) * d, Mathf.Cos(rad) * d, a, 5f, collide: true, sink: 0.6f);
            }
            // Two leaning slabs over the back: the mouth.
            var arch1 = OnGround(root, "rock_tallJ", -3f, 9f, 20f, 7f, collide: true, sink: 0.8f);
            if (arch1 != null) arch1.transform.rotation *= Quaternion.Euler(0f, 0f, 28f);
            var arch2 = OnGround(root, "rock_tallG", 4f, 9.5f, 160f, 7f, collide: true, sink: 0.8f);
            if (arch2 != null) arch2.transform.rotation *= Quaternion.Euler(0f, 0f, -28f);
            var fire = OnGround(root, "survival_campfire-pit", 0f, 0f, 0f, Cell, collide: false);
            if (fire != null) fire.AddComponent<Campfire>();
            OnGround(root, "survival_tent-canvas-half", 4f, -3f, 220f, 4f);
            OnGround(root, "jp_basket", -3f, -2f, 0f, Cell);
            OnGround(root, "mushroom_redGroup", 6f, 4f, 0f, 3.5f, collide: false);
            OnGround(root, "mushroom_tanGroup", -6f, 3f, 0f, 3.5f, collide: false);
        }

        /// <summary>The drowned shrine: a wayshrine sunk to its platform in the marsh.</summary>
        private static void DrownedShrine(Transform root)
        {
            var s = ShrineHouse(root, 0f, 0f, 200f, 1.2f, sink: 0.9f);
            s.gameObject.AddComponent<Shrine>().shrineId = "drowned";
            Torii(root, 0f, -14f, 200f, 6f);
            foreach (var (x, z, y) in new[] { (-8f, -6f, 30f), (7f, -9f, 300f), (9f, 5f, 80f), (-9f, 6f, 200f) })
                OnGround(root, "town_pillar-stone", x, z, y, Cell * 0.8f, collide: true, sink: 0.8f);
            OnGround(root, "log_large", 12f, -2f, 60f, 4f, collide: true, sink: 0.3f);
            OnGround(root, "stump_old", -12f, 3f, 0f, 4f, collide: true, sink: 0.3f);
        }

        /// <summary>Kawai hamlet: three farms, a well, a mill, and fields.</summary>
        private static void Hamlet(Transform root)
        {
            House(root, -12f, 6f, 20f, wood: true);
            House(root, 10f, 8f, 340f, wood: false);
            House(root, 2f, -14f, 180f, wood: true);
            OnGround(root, "building_well", 0f, -2f, 0f, Cell);
            OnGround(root, "town_windmill", 20f, -12f, 60f, Cell);
            OnGround(root, "town_cart", -6f, -8f, 110f, Cell);
            OnGround(root, "jp_ricebale", 12f, 14f, 0f, Cell);
            OnGround(root, "jp_ricebale_stack", 14f, 16f, 30f, Cell);
            OnGround(root, "jp_bucket", -14f, 2f, 0f, Cell, collide: false);
            OnGround(root, "survival_signpost-single", 6f, -20f, 150f, Cell);
            var fire = OnGround(root, "survival_campfire-pit", -4f, 2f, 0f, Cell, collide: false);
            if (fire != null) fire.AddComponent<Campfire>();

            // Fields: rows of wheat and bamboo behind the houses.
            for (var r = 0; r < 5; r++)
            for (var c = 0; c < 6; c++)
            {
                var x = -24f + c * 3f;
                var z = 16f + r * 3f;
                OnGround(root, r % 2 == 0 ? "crops_wheatStageB" : "crops_wheatStageA", x, z, 0f, Cell, collide: false);
            }
            for (var r = 0; r < 3; r++)
            for (var c = 0; c < 5; c++)
                OnGround(root, "crops_bambooStageA", 18f + c * 3f, 2f + r * 3f, 0f, Cell * 0.9f, collide: false);
            Fence(root, new Vector2(-26f, 14f), new Vector2(-6f, 14f));
            Fence(root, new Vector2(-26f, 14f), new Vector2(-26f, 32f));
            Fence(root, new Vector2(16f, 0f), new Vector2(34f, 0f));
        }

        /// <summary>The shinobi hollow: tents, a fire, targets, no road in.</summary>
        private static void Hideout(Transform root)
        {
            OnGround(root, "survival_tent", -6f, 4f, 40f, 4f);
            OnGround(root, "survival_tent", 6f, 5f, 320f, 4f);
            OnGround(root, "survival_tent-canvas-half", 0f, 9f, 180f, 4f);
            var fire = OnGround(root, "survival_campfire-pit", 0f, 0f, 0f, Cell, collide: false);
            if (fire != null) fire.AddComponent<Campfire>();
            OnGround(root, "target", -9f, -6f, 30f, Cell);
            OnGround(root, "target", -6f, -9f, 10f, Cell);
            OnGround(root, "weaponrack", 8f, -4f, 250f, Cell);
            OnGround(root, "survival_workbench", 9f, -8f, 200f, Cell);
            OnGround(root, "survival_resource-planks", -8f, 8f, 70f, Cell);
            OnGround(root, "log", 3f, -3f, 100f, 4f);
            OnGround(root, "jp_lantern_stand", 2f, 3f, 0f, Cell, collide: false);
        }

        /// <summary>The falls: stacked stone at the drop, a white face, mist.</summary>
        private static void Waterfall(Transform root)
        {
            // Rock either side of the gorge at the lip.
            foreach (var side in new[] { -1f, 1f })
            {
                OnGround(root, "rock_tallC", side * 9f, 1f, side * 40f, 6f, collide: true, sink: 0.8f);
                OnGround(root, "rock_largeE", side * 12f, -4f, side * 120f, 5f, collide: true, sink: 0.6f);
                OnGround(root, "rock_tallA", side * 8f, 6f, side * 70f, 5f, collide: true, sink: 0.7f);
            }
            // The white face: an unlit quad across the drop.
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(face.GetComponent<Collider>());
            face.name = "FallFace";
            face.transform.SetParent(root, false);
            var top = ZoneTerrain.WaterLevel + ZoneTerrain.UpperRiverLift;
            face.transform.position = new Vector3(root.position.x, (top + ZoneTerrain.WaterLevel) * 0.5f + 0.4f, ZoneTerrain.WaterfallZ - 0.5f);
            face.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            face.transform.localScale = new Vector3(9f, ZoneTerrain.UpperRiverLift + 1.5f, 1f);
            var mr = face.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Emberline/Glow"));
            mat.color = new Color(0.72f, 0.8f, 0.88f, 0.55f);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Mist at the foot.
            var mist = new GameObject("Mist");
            mist.transform.SetParent(root, false);
            mist.transform.position = new Vector3(root.position.x, ZoneTerrain.WaterLevel + 0.5f, ZoneTerrain.WaterfallZ - 3f);
            var ps = mist.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startColor = new Color(0.7f, 0.78f, 0.85f, 0.12f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 8f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 0.5f, 3f);
            var pr = mist.GetComponent<ParticleSystemRenderer>();
            pr.material = UI.FxPools.WeatherMaterial(Weather.Mist);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A pass: a watchtower over the road and a signpost at the saddle.</summary>
        private static void Pass(Transform root)
        {
            Tower(root, 9f, 4f, 0f);
            OnGround(root, "survival_signpost", -5f, 0f, 20f, Cell);
            OnGround(root, "survival_fence-fortified", 12f, -2f, 90f, Cell);
            OnGround(root, "survival_fence-fortified", 12f, 2f, 90f, Cell);
            OnGround(root, "castle_flag", 9f, 8f, 0f, Cell * 0.8f, collide: false);
        }
    }
}
