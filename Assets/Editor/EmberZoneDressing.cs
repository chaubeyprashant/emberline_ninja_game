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
    /// <b>Buildings come from <see cref="EmberJapanKit"/>.</b> The Fantasy Town
    /// walls and gables read as low-poly Europe; the village, camp and shrine
    /// are now minka, kura, yagura, saku and torii generated in code, placed
    /// whole by <see cref="Building"/> on the footprints the old houses held.
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

        /// <summary>
        /// Nothing solid is built inside this radius. It covers the whole play
        /// area (34 m) with a little margin, so combat never happens against a
        /// building wall.
        /// </summary>
        public const float BuildRadius = 38f;

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
            BuildHeroTrees(forest);
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
        /// A whole building from EmberJapanKit, sized in metres, standing on the
        /// terrain. Keeps the prefab's own collider (walls, or a gate's two
        /// pillars) instead of a bounds box that would swallow the eaves.
        /// </summary>
        private static GameObject Building(Transform parent, string name, float x, float z, float yaw,
                                           float scale = 1f)
        {
            var prefab = L(name);
            if (prefab == null) return null;
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(new Vector3(x, EmberTerrain.HeightAt(x, z) - 0.1f, z),
                                                Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;
            return go;
        }

        // ----------------------------------------------------------- village

        private static void BuildVillage(Transform parent)
        {
            // Homes around the square, each a different size and material so the
            // village reads as having been built over time.
            // A ring of homes facing the green, all clear of the fighting area.
            //
            // They sit beyond BuildRadius, not merely beyond PlazaRadius: the play
            // area is 34 m and its soft push-back starts at 29, so a house at 26 m
            // stands right where fights happen. The bot proved it — wedged in the
            // corner of the north house with full stick input, unable to route
            // around a wall a person would simply step past.
            // Thatched farmhouses, tiled town houses and storehouses from
            // EmberJapanKit, on the footprints the Fantasy Town houses held.
            Building(parent, "jpk_minka_s", -28f, 22f, 128f);
            Building(parent, "jpk_minka_m", -11f, 36f, 168f);
            Building(parent, "jpk_minka_s_lit", 20f, 31f, 205f);
            Building(parent, "jpk_kura", 36f, 10f, 254f);
            Building(parent, "jpk_minka_s", 31f, -24f, 300f);
            Building(parent, "jpk_minka_m", -6f, -36f, 355f);
            Building(parent, "jpk_kura", -33f, -17f, 60f);
            // The blacksmith: the biggest house, noren over the door, and the
            // reason to come back here.
            Building(parent, "jpk_minka_l", -39f, 4f, 78f);

            // Nothing solid stands within PlazaRadius of the origin. Missions are
            // centred there — the player spawns on it and objectives and clues
            // appear around it — so it has to be open ground. The first pass put
            // the well two metres off origin and the bot spent a whole stage
            // pressed against it, unable to reach a clue three metres away.
            EmberScatter.PlaceOnGround(parent, L("building_well"), -21f, 21f, 20f, 7f);

            // Market stalls off the square, on the road side, where rumours are heard.
            foreach (var (x, z, yaw, p) in new (float, float, float, string)[]
                     { (24f, -19f, 200f, "town_stall"), (28f, -21f, 200f, "town_stall-green"),
                       (19f, -25f, 190f, "town_stall-red") })
                EmberScatter.PlaceOnGround(parent, L(p), x, z, yaw, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_stall-bench"), 23f, -26f, 20f, Cell);
            EmberScatter.PlaceOnGround(parent, L("town_cart"), -23f, -26f, 40f, Cell);

            // The blacksmith's tools, outside the forge.
            EmberScatter.PlaceOnGround(parent, L("survival_workbench-anvil"), -36f, 6f, 100f, Cell);
            EmberScatter.PlaceOnGround(parent, L("survival_workbench"), -36f, 2f, 100f, Cell);

            // Mills: one on the river, one on the rise.
            EmberScatter.PlaceOnGround(parent, L("town_watermill"), -38f, 13f, 96f, Cell);
            // A windmill is Europe; the rise gets a rice store instead.
            Building(parent, "jpk_kura", 26f, 21f, 150f);

            // Lantern posts: the warm accent the whole palette is built around, and
            // the only saturated colour in the valley.
            // Lanterns ring the plaza rather than standing in it.
            foreach (var (x, z) in new[] { (30f, 11f), (-30f, 8f), (13f, -31f), (-16f, -31f),
                                           (31f, 27f), (-33f, -13f) })
                EmberScatter.PlaceOnGround(parent, L("jpk_toro"), x, z, 0f, 1.1f);

            // Farm plots south of the square.
            Rows(parent, -33f, -31f, 14, "crops_wheatStageB");
            Rows(parent, 6f, -34f, 12, "crops_wheatStageA");

            FenceLine(parent, new Vector2(-36f, -26f), new Vector2(-7f, -35f), "town_fence");
            FenceLine(parent, new Vector2(-7f, -35f), new Vector2(22f, -30f), "town_fence");
            FenceLine(parent, new Vector2(28f, 18f), new Vector2(34f, -6f), "town_fence");

            var clutter = new List<Spec>
            {
                // Village stores: sake barrels, rice and baskets (EmberJapanProps).
                // Those are sized in metres, so they take no Cell scale.
                P(L("jp_barrel_a"), 1.2f),
                P(L("jp_barrel_b"), 0.8f),
                P(L("jp_ricebag"), 1f),
                P(L("jp_basket"), 0.6f),
                P(L("jp_ricebale_stack"), 1f),
                P(L("survival_resource-planks"), 1f, Cell, Cell),
                P(L("survival_resource-wood"), 1f, Cell, Cell),
                P(L("jp_bucket"), 1f),
                P(L("town_wheel"), 0.5f, Cell, Cell),
            };
            Scatter(parent, clutter, 240, 313,
                (x, z, h) =>
                {
                    var d = Vector2.Distance(new Vector2(x, z), Vector2.zero);
                    if (d > EmberTerrain.VillageRadius - 3f) return 0f;
                    if (d < BuildRadius) return 0f;             // the fighting floor
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
            // A stake palisade (saku), one 3 m segment per cell.
            var wall = L("jpk_saku");
            var segs = Mathf.RoundToInt(Mathf.PI * 2f * ringR / Cell);
            for (var i = 0; i < segs; i++)
            {
                var ang = i / (float)segs * 360f;
                if (ang > 95f && ang < 200f) continue;             // the open flank
                if (ang > 300f && ang < 322f) continue;            // the gateway itself

                var rad = ang * Mathf.Deg2Rad;
                var x = c.x + Mathf.Sin(rad) * ringR;
                var z = c.z + Mathf.Cos(rad) * ringR;
                EmberScatter.PlaceOnGround(parent, wall, x, z, ang, 1f, Body.Solid, 0.25f);
            }

            // The gate, where the road actually arrives.
            var gateAng = 311f * Mathf.Deg2Rad;
            Building(parent, "jpk_gate",
                c.x + Mathf.Sin(gateAng) * ringR, c.z + Mathf.Cos(gateAng) * ringR, 311f);

            // Two towers covering the gate and the road below.
            Building(parent, "jpk_yagura", c.x - 12f, c.z + 11f, 200f);
            Building(parent, "jpk_yagura", c.x + 13f, c.z + 8f, 250f);

            // The commander's hall at the centre.
            Building(parent, "jpk_minka_l", c.x + 1f, c.z - 1f, 145f);

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
            {
                var fire = EmberScatter.PlaceOnGround(parent, L("survival_campfire-pit"), c.x + dx, c.z + dz, 0f, Cell);
                if (fire == null) continue;
                var a = fire.AddComponent<Core.LightAnchor>();
                a.offset = new Vector3(0f, 0.8f, 0f);
                a.color = new Color(1f, 0.5f, 0.2f);
                a.range = 9f;
                a.intensity = 2.2f;
                a.flicker = true;
            }

            // Training ground on the open side: targets, racks, and the archery butts.
            foreach (var (dx, dz, yaw) in new[] { (-4f, -12f, 20f), (-1f, -13f, 15f), (2f, -12.5f, 10f) })
                EmberScatter.PlaceOnGround(parent, L("target"), c.x + dx, c.z + dz, yaw, 8f);
            foreach (var (dx, dz, yaw) in new[] { (6f, -7f, 80f), (7.5f, -5f, 80f) })
                EmberScatter.PlaceOnGround(parent, L("weaponrack"), c.x + dx, c.z + dz, yaw, 9f);
            EmberScatter.PlaceOnGround(parent, L("bucket_arrows"), c.x + 5f, c.z - 9f, 0f, 9f);

            // Supply dump at the back: what sabotage burns.
            var supply = new List<Spec>
            {
                P(L("jp_ricebale_stack"), 2f),
                P(L("jp_ricebag"), 1.2f),
                P(L("jp_basket_tall"), 0.8f),
                P(L("jp_barrel_a"), 1f),
                P(L("jp_barrel_b"), 1f),
                P(L("jp_tub_a"), 1f),
                P(L("jp_tansu"), 0.8f),
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


        /// <summary>
        /// The few trees that say where this is: sakura by the shrine and the
        /// village, kuromatsu at the road gate, momiji lining the road north.
        ///
        /// <para>
        /// Hand-placed and few on purpose. The Quaternius trees cost 1.6–10 k
        /// triangles each against ~100 for a Kenney cone pine, so the forest mass
        /// stays Kenney until LODs exist (roadmap Phase 9); these fourteen add
        /// about 85 k, rarely more than a third of them in view.
        /// </para>
        /// </summary>
        private static void BuildHeroTrees(Transform parent)
        {
            foreach (var (n, x, z, s) in new (string, float, float, float)[]
                     {
                         ("qn_sakura_1", -38f, 37f, 4.6f), ("qn_sakura_3", -27f, 45f, 4.2f),
                         ("qn_sakura_2", 40f, -8f, 4.4f), ("qn_sakura_5", -20f, 42f, 4.0f),
                         ("qn_matsu_2", 9f, 71f, 4.8f), ("qn_matsu_4", -4f, 78f, 4.4f),
                     })
                EmberScatter.PlaceOnGround(parent, L(n), x, z, (x * 37f + z * 11f) % 360f, s, Body.Trunk, 0.3f);

            // Momiji along the road out of the village: walk the road north and
            // plant one a few metres off it every ten metres or so, alternating sides.
            string[] maples = { "qn_maple_3", "qn_maple_5" };
            var placed = 0;
            for (var z = 44f; z <= 66f && placed < 8; z += 7f)
                foreach (var side in new[] { -1f, 1f })
                {
                    for (var x = -30f; x <= 30f; x += 0.5f)
                    {
                        var d = EmberTerrain.RoadDistance(x, z);
                        if (d < 5f || d > 5.6f || Mathf.Sign(x - RoadX(z)) != side) continue;
                        EmberScatter.PlaceOnGround(parent, L(maples[placed % 2]), x, z,
                            placed * 47f, 4.6f + placed % 3 * 0.3f, Body.Trunk, 0.3f);
                        placed++;
                        break;
                    }
                }
            Debug.Log($"[Zone] hero trees: 6 + {placed} maples");

            static float RoadX(float z)
            {
                // The road's centre at this z: the x with the smallest road distance.
                var best = 0f; var bd = float.MaxValue;
                for (var x = -30f; x <= 30f; x += 0.5f)
                {
                    var d = EmberTerrain.RoadDistance(x, z);
                    if (d < bd) { bd = d; best = x; }
                }
                return best;
            }
        }

        // ----------------------------------------------------------- shrines

        /// <summary>
        /// The road torii, the shrine path's smaller torii and the hokora with its
        /// lanterns, all from <see cref="EmberJapanKit"/>. No CC0 pack ships these
        /// in the valley's style (searched again 2026-10-21), so they are generated.
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
            // The kit's torii spans 4.2 m; the old kitbash spanned 2.3 × scale.
            => Building(parent, "jpk_torii", x, z, yaw, scale / 1.8f);


        private static void Shrine(Transform parent, float x, float z, float yaw)
        {
            // A hokora on its plinth, a stone lantern either side of the approach.
            var root = Building(parent, "jpk_hokora", x, z, yaw, 1.7f);
            if (root == null) return;
            foreach (var side in new[] { -1f, 1f })
            {
                var at = root.transform.TransformPoint(new Vector3(side * 1.6f, 0f, -2.2f));
                EmberScatter.PlaceOnGround(parent, L("jpk_toro"), at.x, at.z, yaw, 1.1f);
            }
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
            var fire = EmberScatter.PlaceOnGround(parent, L("survival_campfire-pit"), 25f, -35f, 0f, Cell);
            if (fire != null) fire.AddComponent<Emberline.Core.Campfire>();
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
