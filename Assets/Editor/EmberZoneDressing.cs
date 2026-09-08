using System.Collections.Generic;
using UnityEngine;
using static Emberline.EditorTools.EmberScatter;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Populates the zone: forest, village, enemy camp, river bank and the
    /// resource sites between them.
    ///
    /// <para>
    /// <b>Scale.</b> The Kenney kits are modular on a 1-unit grid — a wall panel
    /// is 1.00 tall, a roof piece 1.07 wide. At a human scale of roughly 3 m per
    /// cell that gives 3 m walls, which is right for a character 1.8 m tall. The
    /// nature models are scaled separately and randomly, because a forest of
    /// identically sized pines reads as wallpaper.
    /// </para>
    ///
    /// <para>
    /// <b>Buildings are assembled, not dropped.</b> Fantasy Town ships wall,
    /// door, window and roof pieces rather than finished houses, so
    /// <see cref="House"/> lays them out around a footprint. That is what makes a
    /// village of eight houses look like eight different houses.
    /// </para>
    ///
    /// <para>
    /// <b>Placement answers to the mission design.</b> The camp has a gated
    /// palisade on the road side and a deliberate unwalled stretch at the rear, so
    /// flanking is a real route; the towers stand where they can see the road; the
    /// prisoner pen is at the back, so a rescue and an assault are not the same
    /// approach.
    /// </para>
    /// </summary>
    public static class EmberZoneDressing
    {
        private const string Dir = "Props/Zone/";

        /// <summary>Metres per modular grid cell.</summary>
        private const float Cell = 3.0f;

        /// <summary>
        /// Radius of open ground at the origin — the village green.
        ///
        /// <para>
        /// Every mission centres on the origin: the player spawns there, and
        /// objectives, clues and markers appear around it. So it has to be a
        /// fighting floor, not furniture. Buildings ring it from 22 m out, which
        /// gives cover at the edges of a fight without putting a cart in the
        /// middle of one. Earlier passes at 11 m were not enough: the bot spent
        /// whole stages wedged between the tavern and a hand cart.
        /// </para>
        /// </summary>
        public const float PlazaRadius = 20f;

        private static GameObject L(string n)
        {
            var go = Resources.Load<GameObject>(Dir + n);
            if (go == null) Debug.LogWarning($"[Zone] missing prop {n}");
            return go;
        }

        public static void BuildAll(Transform root)
        {
            var forest = New(root, "Forest");
            var wilds = New(root, "Wilds");
            var village = New(root, "Village");
            var camp = New(root, "EnemyCamp");
            var shrines = New(root, "Shrines");

            BuildForest(forest);
            BuildGroundCover(forest);
            BuildRocks(wilds);
            BuildRiverBank(wilds);
            BuildVillage(village);
            BuildCamp(camp);
            BuildShrines(shrines);
            BuildWayside(wilds);
        }

        private static Transform New(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // ------------------------------------------------------------ forest

        private static void BuildForest(Transform parent)
        {
            // Pines carry the valley: they suit a cold mountain bowl and they give
            // a vertical silhouette that reads at distance against the fog.
            var pines = new[]
            {
                "tree_pineTallA", "tree_pineTallB", "tree_pineTallC", "tree_pineTallD",
                "tree_pineTallA_detailed", "tree_pineTallC_detailed",
                "tree_pineRoundA", "tree_pineRoundC", "tree_pineRoundE",
            };
            var small = new[] { "tree_pineSmallA", "tree_pineSmallC",
                                "tree_pineDefaultA", "tree_pineDefaultB" };
            var broad = new[] { "tree_default", "tree_default_dark", "tree_tall",
                                "tree_tall_dark", "tree_thin", "tree_thin_dark" };

            var set = new List<Spec>();
            foreach (var n in pines) set.Add(P(L(n), 3f, 4.5f, 7.5f, Body.Trunk, 0.5f, 0.3f));
            foreach (var n in small) set.Add(P(L(n), 1.4f, 3.5f, 5.5f, Body.Trunk, 0.4f, 0.3f));
            // A minority of broadleaf keeps the wood from looking like a plantation.
            foreach (var n in broad) set.Add(P(L(n), 0.7f, 4.0f, 6.5f, Body.Trunk, 0.5f, 0.3f));

            var n1 = Scatter(parent, set, 14000, 20260907,
                (x, z, h) => EmberTerrain.SlopeAt(x, z) > 0.6f ? 0f : ForestMask(x, z, h),
                minSpacing: 3.4f);

            // Stumps and fallen logs where the forest meets the road: someone has
            // been cutting here, which is a story the woodcutters' camp finishes.
            var deadwood = new List<Spec>
            {
                P(L("stump_round"), 1f, 3f, 4.5f, Body.Solid, sink: 0.15f),
                P(L("stump_old"), 1f, 3f, 4.5f, Body.Solid, sink: 0.15f),
                P(L("stump_squareDetailed"), 1f, 3f, 4.5f, Body.Solid, sink: 0.15f),
                P(L("log"), 1.2f, 3f, 4.5f, Body.Solid, sink: 0.2f),
                P(L("log_large"), 0.8f, 3f, 4.5f, Body.Solid, sink: 0.2f),
            };
            Scatter(parent, deadwood, 260, 5150,
                (x, z, h) => ForestMask(x, z, h) > 0f ? 0.5f : 0f, minSpacing: 6f);

            Debug.Log($"[Zone] forest: {n1} trees");
        }

        /// <summary>
        /// Grass tufts, bushes and mushrooms. No colliders and no shadows: this is
        /// the layer that makes ground look inhabited, and it is also the layer
        /// that would wreck the frame budget if it were treated as geometry that
        /// matters.
        /// </summary>
        private static void BuildGroundCover(Transform parent)
        {
            var cover = new List<Spec>
            {
                P(L("grass"), 4f, 3.0f, 5.0f, Body.None, sink: 0.1f),
                P(L("grass_large"), 3f, 3.0f, 5.0f, Body.None, sink: 0.1f),
                P(L("grass_leafs"), 2f, 3.0f, 4.5f, Body.None, sink: 0.1f),
                P(L("plant_bush"), 2f, 3.0f, 5.0f, Body.None, sink: 0.15f),
                P(L("plant_bushSmall"), 2f, 3.0f, 5.0f, Body.None, sink: 0.15f),
                P(L("plant_bushLarge"), 1.5f, 3.5f, 5.5f, Body.None, sink: 0.15f),
                P(L("plant_bushDetailed"), 1.5f, 3.0f, 5.0f, Body.None, sink: 0.15f),
                P(L("mushroom_tanGroup"), 0.4f, 3f, 4f, Body.None, sink: 0.05f),
                P(L("mushroom_redGroup"), 0.3f, 3f, 4f, Body.None, sink: 0.05f),
                P(L("flower_purpleA"), 0.3f, 3f, 4f, Body.None, sink: 0.05f),
                P(L("flower_yellowA"), 0.3f, 3f, 4f, Body.None, sink: 0.05f),
            };

            var n = Scatter(parent, cover, 5200, 8821,
                (x, z, h) =>
                {
                    if (Clearances(x, z) <= 0f) return 0f;
                    if (h > 17f) return 0f;                 // bare rock above the treeline
                    // Thickest at the forest edge, thinning across the open meadow.
                    return ForestMask(x, z, h) > 0f ? 0.85f : 0.45f;
                }, minSpacing: 1.5f);

            foreach (var r in parent.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("grass") || r.name.StartsWith("flower")
                    || r.name.StartsWith("mushroom"))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Debug.Log($"[Zone] ground cover: {n}");
        }

        private static void BuildRocks(Transform parent)
        {
            var rocks = new List<Spec>();
            foreach (var n in new[] { "rock_largeA", "rock_largeB", "rock_largeC",
                                      "rock_largeD", "rock_largeE", "rock_largeF" })
                rocks.Add(P(L(n), 1.4f, 2.5f, 5.0f, Body.Solid, sink: 0.4f));
            foreach (var n in new[] { "rock_tallA", "rock_tallC", "rock_tallE",
                                      "rock_tallG", "rock_tallJ" })
                rocks.Add(P(L(n), 1.2f, 2.5f, 5.5f, Body.Solid, sink: 0.4f));
            foreach (var n in new[] { "rock_smallA", "rock_smallC", "rock_smallE",
                                      "rock_smallG", "rock_smallFlatA", "rock_smallFlatB",
                                      "stone_smallB", "stone_smallD" })
                rocks.Add(P(L(n), 2f, 2.5f, 4.5f, Body.Solid, sink: 0.3f));
            foreach (var n in new[] { "stone_largeA", "stone_largeC", "stone_tallB", "stone_tallE" })
                rocks.Add(P(L(n), 1f, 3.0f, 6.0f, Body.Solid, sink: 0.4f));

            var n2 = Scatter(parent, rocks, 1500, 771,
                (x, z, h) =>
                {
                    if (Clearances(x, z) <= 0f) return 0f;
                    if (ArenaClear(x, z) <= 0f) return 0f;   // keep the arena walkable
                    // Not on the steep faces. A flat-bottomed rock on a 45-degree
                    // slope buries one edge and leaves the other hanging, which is
                    // what made the first pass look like rubble floating on the
                    // mountainside.
                    if (EmberTerrain.SlopeAt(x, z) > 0.55f) return 0f;
                    // Sparse in the meadow, heavy where the mountains shed scree.
                    var r = Mathf.Sqrt(x * x + z * z);
                    return Mathf.Lerp(0.12f, 0.95f, EmberTerrain.Smooth01(38f, 80f, r));
                }, minSpacing: 3.2f);
            Debug.Log($"[Zone] rocks: {n2}");
        }

        private static void BuildRiverBank(Transform parent)
        {
            // Bamboo on the near bank: the clearest East-Asian read available in a
            // CC0 kit, and it belongs by water.
            var reeds = new List<Spec>
            {
                // Bamboo is a clump 0.34 wide before scaling; at 7x that is a
                // 2.4 m thick pillar, which is what the first pass planted along
                // the whole bank. Kept to a stand about 3 m tall instead.
                P(L("crops_bambooStageB"), 3f, 2.6f, 4.0f, Body.None, sink: 0.2f),
                P(L("crops_bambooStageA"), 2f, 2.6f, 3.6f, Body.None, sink: 0.2f),
                P(L("grass_leafsLarge"), 2f, 3.5f, 5.5f, Body.None, sink: 0.15f),
                P(L("plant_bushSmall"), 1f, 3f, 5f, Body.None, sink: 0.15f),
            };
            Scatter(parent, reeds, 900, 991,
                (x, z, h) =>
                {
                    var d = EmberTerrain.RiverDistance(x, z);
                    return d > 4.5f && d < 15f ? 0.8f : 0f;
                }, minSpacing: 1.8f);
        }

        // ---------------------------------------------------------- buildings

        /// <summary>
        /// Assembles one modular building: wall panels around a footprint, a door
        /// on the chosen face, windows on the rest, and a gabled roof on top.
        /// </summary>
        /// <param name="cellsX">Footprint width in grid cells.</param>
        /// <param name="cellsZ">Footprint depth in grid cells.</param>
        /// <param name="wood">Timber walls rather than plastered stone.</param>
        private static void House(Transform parent, float cx, float cz, float yaw,
                                  int cellsX, int cellsZ, int storeys, bool wood)
        {
            var pre = wood ? "town_wall-wood" : "town_wall";
            var wall = L(pre);
            var door = L(pre + "-door");
            var win = L(pre + "-window-shutters");
            var winS = L(pre + "-window-small");
            if (wall == null) return;

            var root = new GameObject($"House_{cellsX}x{cellsZ}").transform;
            root.SetParent(parent, false);
            var ground = EmberTerrain.HeightAt(cx, cz);
            root.SetPositionAndRotation(new Vector3(cx, ground - 0.15f, cz),
                                        Quaternion.Euler(0f, yaw, 0f));

            var halfX = (cellsX - 1) * 0.5f;
            var halfZ = (cellsZ - 1) * 0.5f;
            var rng = new System.Random(Mathf.RoundToInt(cx * 31 + cz * 17));

            // The door goes on one long face, at the middle cell.
            var doorCell = cellsX / 2;

            for (var s = 0; s < storeys; s++)
            for (var ix = 0; ix < cellsX; ix++)
            for (var iz = 0; iz < cellsZ; iz++)
            {
                var edgeN = iz == cellsZ - 1;
                var edgeS = iz == 0;
                var edgeE = ix == cellsX - 1;
                var edgeW = ix == 0;
                if (!edgeN && !edgeS && !edgeE && !edgeW) continue;   // interior cell

                var lx = (ix - halfX) * Cell;
                var lz = (iz - halfZ) * Cell;
                var ly = s * Cell;

                // Each wall panel sits on the outward edge of its cell, facing out.
                if (edgeS) Panel(root, lx, ly, lz - Cell * 0.5f, 0f, Pick(s, ix, iz, doorCell, 0));
                if (edgeN) Panel(root, lx, ly, lz + Cell * 0.5f, 180f, Pick(s, ix, iz, doorCell, 1));
                if (edgeW) Panel(root, lx - Cell * 0.5f, ly, lz, 90f, Pick(s, ix, iz, doorCell, 2));
                if (edgeE) Panel(root, lx + Cell * 0.5f, ly, lz, 270f, Pick(s, ix, iz, doorCell, 3));
            }

            // Gabled roof, ridge running along X.
            var roof = L(storeys > 1 ? "town_roof-high" : "town_roof");
            var gable = L(storeys > 1 ? "town_roof-high-gable" : "town_roof-gable");
            var roofY = storeys * Cell;
            for (var ix = 0; ix < cellsX; ix++)
            {
                var lx = (ix - halfX) * Cell;
                // Two slopes meeting over the middle of the footprint.
                Piece(root, roof, lx, roofY, (0 - halfZ) * Cell, 0f);
                Piece(root, roof, lx, roofY, (cellsZ - 1 - halfZ) * Cell, 180f);
            }
            if (gable != null && cellsZ > 1)
            {
                Piece(root, gable, (0 - halfX) * Cell - Cell * 0.5f, roofY, 0f, 90f);
                Piece(root, gable, (cellsX - 1 - halfX) * Cell + Cell * 0.5f, roofY, 0f, 270f);
            }

            // A chimney on about half of them.
            if (rng.Next(2) == 0)
                Piece(root, L("town_chimney"), (halfX - 0.5f) * Cell, roofY + Cell * 0.4f, 0f, 0f);

            // One box collider for the whole building beats one per panel. Built
            // from the footprint rather than from world-space renderer bounds,
            // which for a rotated house is a much larger skewed box.
            var box = root.gameObject.AddComponent<BoxCollider>();
            var w = cellsX * Cell;
            var d = cellsZ * Cell;
            var hgt = storeys * Cell + Cell * 0.7f;   // walls plus the roof
            box.center = new Vector3(0f, hgt * 0.5f, 0f);
            box.size = new Vector3(w, hgt, d);

            GameObject Pick(int storey, int ix, int iz, int dc, int face)
            {
                // Door only on the ground floor, front face, middle cell.
                if (storey == 0 && face == 0 && ix == dc && door != null) return door;
                var roll = rng.Next(100);
                if (roll < 34 && win != null) return win;
                if (roll < 50 && winS != null) return winS;
                return wall;
            }

            void Panel(Transform p, float lx, float ly, float lz, float ry, GameObject prefab)
                => Piece(p, prefab, lx, ly, lz, ry);
        }

        private static void Piece(Transform parent, GameObject prefab,
                                  float lx, float ly, float lz, float ry)
        {
            if (prefab == null) return;
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = new Vector3(lx, ly, lz);
            go.transform.localRotation = Quaternion.Euler(0f, ry, 0f);
            go.transform.localScale = Vector3.one * Cell;
            go.isStatic = true;
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
        }

        // ----------------------------------------------------------- village

        private static void BuildVillage(Transform parent)
        {
            // Homes around the square, each a different size and material so the
            // village reads as having been built over time.
            // A ring of homes facing the green, all beyond PlazaRadius.
            House(parent, -20f, 16f, 128f, 2, 2, 1, wood: true);
            House(parent, -8f, 26f, 168f, 3, 2, 2, wood: false);
            House(parent, 14f, 22f, 205f, 2, 2, 1, wood: true);
            House(parent, 26f, 7f, 254f, 3, 2, 1, wood: false);
            House(parent, 22f, -17f, 300f, 2, 2, 2, wood: true);
            House(parent, -4f, -26f, 355f, 3, 2, 1, wood: true);
            House(parent, -24f, -12f, 60f, 2, 2, 1, wood: false);
            // The blacksmith: bigger, stone, and the reason to come back here.
            House(parent, -29f, 3f, 78f, 4, 2, 1, wood: false);

            // Nothing solid stands within PlazaRadius of the origin. Missions are
            // centred there — the player spawns on it and objectives and clues
            // appear around it — so it has to be open ground. The first pass put
            // the well two metres off origin and the bot spent a whole stage
            // pressed against it, unable to reach a clue three metres away.
            EmberScatter.PlaceOnGround(parent, L("building_well"), -15f, 15f, 20f, 7f);

            // Market stalls off the square, on the road side, where rumours are heard.
            foreach (var (x, z, yaw, p) in new (float, float, float, string)[]
                     { (17f, -13f, 200f, "town_stall"), (21f, -15f, 200f, "town_stall-green"),
                       (13f, -18f, 190f, "town_stall-red") })
                EmberScatter.PlaceOnGround(parent, L(p), x, z, yaw, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_stall-bench"), 16f, -19f, 20f, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_cart"), -16f, -19f, 40f, Cell);

            // The blacksmith's tools, outside the forge.
            EmberScatter.PlaceOnGround(parent, L("survival_workbench-anvil"), -26f, 5f, 100f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_workbench"), -26f, 2f, 100f, Cell);

            // Mills: one on the river, one on the rise.
            EmberScatter.PlaceOnGround(parent, L("town_watermill"), -38f, 13f, 96f, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_windmill"), 26f, 21f, 150f, Cell);

            // Lantern posts: the warm accent the whole palette is built around, and
            // the only saturated colour in the valley.
            // Lanterns ring the plaza rather than standing in it.
            foreach (var (x, z) in new[] { (21f, 8f), (-21f, 6f), (9f, -22f), (-11f, -22f),
                                           (22f, 19f), (-23f, -9f) })
                EmberScatter.PlaceOnGround(parent, L("town_lantern"), x, z, 0f, 2.2f);

            // Farm plots south of the square.
            Rows(parent, -22f, -22f, 14, "crops_wheatStageB");
            Rows(parent, 4f, -24f, 12, "crops_wheatStageA");

            FenceLine(parent, new Vector2(-26f, -19f), new Vector2(-5f, -25f), "town_fence");
            FenceLine(parent, new Vector2(-5f, -25f), new Vector2(16f, -21f), "town_fence");
            FenceLine(parent, new Vector2(20f, 13f), new Vector2(24f, -4f), "town_fence");

            var clutter = new List<Spec>
            {
                P(L("survival_barrel"), 2f, Cell, Cell),
                P(L("survival_box"), 1.6f, Cell, Cell),
                P(L("survival_box-large"), 1f, Cell, Cell),
                P(L("survival_resource-planks"), 1f, Cell, Cell),
                P(L("survival_resource-wood"), 1f, Cell, Cell),
                P(L("survival_bucket"), 1f, Cell, Cell),
                P(L("town_wheel"), 0.5f, Cell, Cell),
            };
            Scatter(parent, clutter, 240, 313,
                (x, z, h) =>
                {
                    var d = Vector2.Distance(new Vector2(x, z), Vector2.zero);
                    if (d > EmberTerrain.VillageRadius - 3f) return 0f;
                    if (d < PlazaRadius) return 0f;              // the fighting floor
                    if (EmberTerrain.RoadDistance(x, z) < 3.6f) return 0f;
                    return 0.6f;
                }, minSpacing: 2.4f);
        }

        private static void Rows(Transform parent, float x0, float z0, int n, string crop)
        {
            var prefab = L(crop);
            var soil = L("crops_dirtRow");
            for (var i = 0; i < n; i++)
            {
                var x = x0 + (i % 4) * 2.6f;
                var z = z0 + (i / 4) * 2.6f;
                EmberScatter.PlaceOnGround(parent, soil, x, z, 0f, 2.4f, Body.None, 0.05f);
                EmberScatter.PlaceOnGround(parent, prefab, x, z, 0f, 2.4f, Body.None, 0.05f);
            }
        }

        private static void FenceLine(Transform parent, Vector2 a, Vector2 b, string prefabName)
        {
            var prefab = L(prefabName);
            if (prefab == null) return;
            var seg = Cell;                      // the fence piece spans one cell
            var dir = b - a;
            var steps = Mathf.Max(1, Mathf.RoundToInt(dir.magnitude / seg));
            var yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            for (var i = 0; i < steps; i++)
            {
                var p = Vector2.Lerp(a, b, (i + 0.5f) / steps);
                EmberScatter.PlaceOnGround(parent, prefab, p.x, p.y, yaw, Cell, Body.Solid, 0.15f);
            }
        }

        // -------------------------------------------------------- enemy camp

        private static void BuildCamp(Transform parent)
        {
            var c = EmberTerrain.CampCentre;
            const float ringR = 17f;

            // Palisade arc facing the road, open at the south-east. The gap is the
            // flank route and it is deliberate.
            var wall = L("castle_wall-narrow-wood");
            var post = L("castle_wall-pillar");
            var segs = Mathf.RoundToInt(Mathf.PI * 2f * ringR / Cell);
            for (var i = 0; i < segs; i++)
            {
                var ang = i / (float)segs * 360f;
                if (ang > 95f && ang < 200f) continue;             // the open flank
                if (ang > 300f && ang < 322f) continue;            // the gateway itself

                var rad = ang * Mathf.Deg2Rad;
                var x = c.x + Mathf.Sin(rad) * ringR;
                var z = c.z + Mathf.Cos(rad) * ringR;
                EmberScatter.PlaceOnGround(parent, wall, x, z, ang, Cell, Body.Solid, 0.25f);
                if (i % 4 == 0)
                    EmberScatter.PlaceOnGround(parent, post, x, z, ang, Cell, Body.None, 0.25f);
            }

            // The gate, where the road actually arrives.
            var gateAng = 311f * Mathf.Deg2Rad;
            EmberScatter.PlaceOnGround(parent, L("castle_wall-narrow-gate"),
                c.x + Mathf.Sin(gateAng) * ringR, c.z + Mathf.Cos(gateAng) * ringR,
                311f, Cell, Body.Solid, 0.25f);

            // Two towers covering the gate and the road below.
            Tower(parent, c.x - 12f, c.z + 11f, 200f);
            Tower(parent, c.x + 13f, c.z + 8f, 250f);

            // The commander's hall at the centre.
            House(parent, c.x + 1f, c.z - 1f, 145f, 4, 2, 1, wood: true);

            // Garrison tents.
            foreach (var (dx, dz, yaw) in new[]
                     { (-8f, 4f, 30f), (-11f, -2f, 65f), (8f, 4f, 300f), (11f, -1f, 280f),
                       (4f, 9f, 190f), (-4f, 10f, 160f), (-9f, -7f, 100f) })
                EmberScatter.PlaceOnGround(parent, L("survival_tent"), c.x + dx, c.z + dz, yaw, 4.5f);
            foreach (var (dx, dz, yaw) in new[] { (-6f, 6f, 20f), (7f, 6f, 240f) })
                EmberScatter.PlaceOnGround(parent, L("survival_tent-canvas"), c.x + dx, c.z + dz, yaw, 4.5f);

            // Fires: the light source that makes the camp visible from the ridge,
            // and the thing a scout counts to guess the garrison.
            foreach (var (dx, dz) in new[] { (-2f, 5f), (6f, 0f), (-7f, 0f) })
                EmberScatter.PlaceOnGround(parent, L("survival_campfire-pit"), c.x + dx, c.z + dz, 0f, Cell);

            // Training ground on the open side: targets, racks, and the archery butts.
            foreach (var (dx, dz, yaw) in new[] { (-4f, -12f, 20f), (-1f, -13f, 15f), (2f, -12.5f, 10f) })
                EmberScatter.PlaceOnGround(parent, L("target"), c.x + dx, c.z + dz, yaw, 8f);
            foreach (var (dx, dz, yaw) in new[] { (6f, -7f, 80f), (7.5f, -5f, 80f) })
                EmberScatter.PlaceOnGround(parent, L("weaponrack"), c.x + dx, c.z + dz, yaw, 9f);
            EmberScatter.PlaceOnGround(parent, L("bucket_arrows"), c.x + 5f, c.z - 9f, 0f, 9f);

            // Supply dump at the back: what sabotage burns.
            var supply = new List<Spec>
            {
                P(L("survival_box-large"), 2f, Cell, Cell),
                P(L("survival_box"), 2f, Cell, Cell),
                P(L("survival_barrel"), 2f, Cell, Cell),
                P(L("survival_barrel-open"), 1f, Cell, Cell),
                P(L("survival_chest"), 0.8f, Cell, Cell),
                P(L("survival_resource-planks"), 1.2f, Cell, Cell),
                P(L("survival_resource-stone"), 1f, Cell, Cell),
                P(L("survival_resource-wood"), 1.2f, Cell, Cell),
            };
            Scatter(parent, supply, 200, 4242,
                (x, z, h) =>
                {
                    var d = Vector2.Distance(new Vector2(x, z), new Vector2(c.x, c.z));
                    if (d > 14f) return 0f;
                    return z < c.z + 1f ? 0.85f : 0.2f;   // piled away from the gate
                }, minSpacing: 2.2f);

            // Prisoner pen at the rear, so rescue and assault are different routes.
            var pen = new Vector2(c.x - 3f, c.z - 14f);
            FenceLine(parent, pen + new Vector2(-4.5f, -4.5f), pen + new Vector2(4.5f, -4.5f), "survival_fence-fortified");
            FenceLine(parent, pen + new Vector2(4.5f, -4.5f), pen + new Vector2(4.5f, 4.5f), "survival_fence-fortified");
            FenceLine(parent, pen + new Vector2(4.5f, 4.5f), pen + new Vector2(-4.5f, 4.5f), "survival_fence-doorway");
            FenceLine(parent, pen + new Vector2(-4.5f, 4.5f), pen + new Vector2(-4.5f, -4.5f), "survival_fence-fortified");

            // Banners: whose camp this is, readable from the road.
            foreach (var (dx, dz) in new[] { (-10f, 9f), (10f, 8f), (0f, 12f) })
                EmberScatter.PlaceOnGround(parent, L("castle_flag"), c.x + dx, c.z + dz, 0f, Cell);
        }

        /// <summary>A stacked modular watchtower: base, midsections, top, roof.</summary>
        private static void Tower(Transform parent, float x, float z, float yaw)
        {
            var root = new GameObject("Watchtower").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(new Vector3(x, EmberTerrain.HeightAt(x, z) - 0.2f, z),
                                        Quaternion.Euler(0f, yaw, 0f));

            // Advance by each piece's own height, never by a fixed step. The kit
            // is modular but not uniform — `tower-square-top` is 0.30 units tall
            // where the midsections are 1.01 — so stacking on a constant Cell left
            // a two-metre gap and hung the roof in the sky above the tower.
            var y = 0f;
            foreach (var name in new[]
                     {
                         "castle_tower-square-base",
                         "castle_tower-square-mid",
                         "castle_tower-square-mid-windows",
                         "castle_tower-square-top",
                         "castle_tower-square-top-roof",
                     })
            {
                var prefab = L(name);
                if (prefab == null) continue;
                Piece(root, prefab, 0f, y, 0f, 0f);
                y += PrefabHeight(prefab) * Cell;
            }

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, y * 0.5f, 0f);
            box.size = new Vector3(Cell, y, Cell);
        }

        /// <summary>Height of a prefab's renderers in model units.</summary>
        private static float PrefabHeight(GameObject prefab)
        {
            var any = false;
            var b = new Bounds();
            foreach (var r in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                var rb = r.bounds;
                if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
            }
            return any ? b.size.y : 1f;
        }

        // ----------------------------------------------------------- shrines

        /// <summary>
        /// Emberline's own East-Asian landmarks, kitbashed from the modular kits.
        ///
        /// <para>
        /// No CC0 pack ships a torii or a pagoda in this style — that gap was
        /// searched for and confirmed. Rather than import one model from a foreign
        /// palette and break the single-atlas coherence that makes the rest of the
        /// valley read as one world, the gates are built here from the timber
        /// pieces the kits already provide: two uprights, a lintel and a ridge
        /// beam. It costs four prefabs and keeps the whole zone on one material.
        /// </para>
        /// </summary>
        private static void BuildShrines(Transform parent)
        {
            // The road gate at the north entrance: the first thing the player walks
            // through on the way in.
            Torii(parent, 3f, 74f, 6f, 3.6f);
            // A second, smaller gate on the path to the shrine above the village.
            Torii(parent, -30f, 34f, 55f, 2.6f);

            // The shrine itself: a raised platform under a high roof, lanterns
            // either side. Small, quiet, and a landmark you can navigate by.
            Shrine(parent, -33f, 40f, 55f);
        }

        private static void Torii(Transform parent, float x, float z, float yaw, float scale)
        {
            var post = L("town_pillar-wood");
            var beam = L("town_poles-horizontal");
            var plank = L("town_planks");
            if (post == null || beam == null) return;

            var root = new GameObject("Torii").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(new Vector3(x, EmberTerrain.HeightAt(x, z) - 0.1f, z),
                                        Quaternion.Euler(0f, yaw, 0f));

            var s = scale;
            var halfSpan = 1.15f * s;

            // Two uprights.
            foreach (var side in new[] { -1f, 1f })
            {
                var p = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(post, root);
                p.transform.localPosition = new Vector3(side * halfSpan, 0f, 0f);
                p.transform.localScale = new Vector3(s * 0.55f, s * 2.2f, s * 0.55f);
                p.isStatic = true;
                foreach (var c in p.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var col = p.AddComponent<CapsuleCollider>();
                col.height = 2.2f; col.radius = 0.35f; col.center = new Vector3(0f, 1.1f, 0f);
            }

            // The lintel, and the ridge beam above it that gives a torii its shape.
            var lintel = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(beam, root);
            lintel.transform.localPosition = new Vector3(0f, s * 1.75f, 0f);
            lintel.transform.localScale = new Vector3(s * 2.9f, s * 0.5f, s * 0.5f);
            lintel.isStatic = true;
            foreach (var c in lintel.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            if (plank != null)
            {
                var ridge = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(plank, root);
                ridge.transform.localPosition = new Vector3(0f, s * 2.15f, 0f);
                ridge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ridge.transform.localScale = new Vector3(s * 0.45f, s * 3.4f, s * 0.5f);
                ridge.isStatic = true;
                foreach (var c in ridge.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }
        }

        private static void Shrine(Transform parent, float x, float z, float yaw)
        {
            var root = new GameObject("Shrine").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(new Vector3(x, EmberTerrain.HeightAt(x, z) - 0.15f, z),
                                        Quaternion.Euler(0f, yaw, 0f));

            // A raised timber platform.
            Piece(root, L("town_planks"), 0f, 0f, 0f, 0f);
            // Four corner posts under a high roof.
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var p = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(L("town_pillar-wood"), root);
                p.transform.localPosition = new Vector3(dx * Cell * 0.5f, 0.4f, dz * Cell * 0.5f);
                p.transform.localScale = new Vector3(Cell * 0.4f, Cell * 0.9f, Cell * 0.4f);
                p.isStatic = true;
                foreach (var c in p.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }
            Piece(root, L("town_roof-high"), 0f, Cell * 0.95f, -Cell * 0.5f, 0f);
            Piece(root, L("town_roof-high"), 0f, Cell * 0.95f, Cell * 0.5f, 180f);

            // Lanterns flanking the steps: the shrine is a light in the trees.
            foreach (var side in new[] { -1f, 1f })
            {
                var lp = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(L("town_lantern"), root);
                lp.transform.localPosition = new Vector3(side * Cell * 0.9f, 0f, -Cell * 1.2f);
                lp.transform.localScale = Vector3.one * 2.2f;
                lp.isStatic = true;
            }

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Cell * 0.6f, 0f);
            box.size = new Vector3(Cell * 1.6f, Cell * 1.2f, Cell * 1.6f);
        }

        // ---------------------------------------------------------- wayside

        private static void BuildWayside(Transform parent)
        {
            // The bridge over the river, on the west trail. Placed on the water
            // line, because the ground under it is the channel floor.
            var deck = L("bridge_wood");
            if (deck != null)
                for (var i = -2; i <= 2; i++)
                {
                    var bz = 6f + i * 3.9f;
                    var bx = EmberTerrain.RiverCentreX + Mathf.Sin(bz * 0.032f) * 9f
                             + Mathf.Sin(bz * 0.011f) * 5f;
                    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(deck, parent);
                    go.transform.SetPositionAndRotation(
                        new Vector3(bx, EmberTerrain.WaterLevel + 0.35f, bz),
                        Quaternion.Euler(0f, 90f, 0f));
                    go.transform.localScale = Vector3.one * 4f;
                    go.isStatic = true;
                    foreach (var c in go.GetComponentsInChildren<Collider>(true))
                        Object.DestroyImmediate(c);
                    var box = go.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.15f, 0f);
                    box.size = new Vector3(1.1f, 0.3f, 1.1f);
                }

            // A ruin off the north road: somewhere to find, and cover if a patrol
            // catches you in it.
            EmberScatter.PlaceOnGround(parent, L("town_wall-broken"), -26f, 48f, 40f, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_wall-wood-broken"), -24f, 46f, 130f, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_pillar-stone"), -28f, 45f, 0f, Cell);
            EmberScatter.PlaceOnGround(parent, L("rock_largeC"), -25f, 43f, 30f, 4f);

            // Woodcutters' camp on the south road: a resource site with a story.
            EmberScatter.PlaceOnGround(parent, L("survival_resource-wood"), 24f, -34f, 25f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_resource-planks"), 26f, -36f, 70f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_workbench"), 22f, -37f, 200f, Cell);
            EmberScatter.PlaceOnGround(parent, L("stump_squareDetailed"), 27f, -32f, 0f, 4f);
            EmberScatter.PlaceOnGround(parent, L("survival_campfire-pit"), 25f, -35f, 0f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_tent-canvas-half"), 21f, -34f, 40f, 4f);

            // A quarry face under the eastern ridge.
            EmberScatter.PlaceOnGround(parent, L("survival_resource-stone-large"), 62f, -8f, 100f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_resource-stone"), 64f, -12f, 140f, Cell);

            // Signposts where the road forks: navigation without an objective marker.
            EmberScatter.PlaceOnGround(parent, L("survival_signpost"), 1f, 30f, 190f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_signpost-single"), 12f, -22f, 150f, Cell);
        }
    }
}
