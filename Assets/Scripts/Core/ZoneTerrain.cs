using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// The region's height field, at runtime.
    ///
    /// <para>
    /// This is the same function the editor uses to generate the baked valley
    /// mesh and the streamer uses to build the outer chunks — every terrain
    /// triangle and every enemy footstep read the same surface, with no raycast
    /// and no collider lookup. It stays a pure function of x and z on purpose:
    /// a physics query per enemy per frame is affordable but pointless when the
    /// ground is closed-form.
    /// </para>
    ///
    /// <para>
    /// Layout (2026-09-16). The 200 m valley the campaign is fought in sits at
    /// the centre, unchanged inside its mountain ring. The ring now has three
    /// passes (north, east, south-east) where the roads leave, and two gorges
    /// where the river enters and leaves; beyond it the region rolls out to
    /// 800 m: a temple hill to the east, Kagehira's stronghold plateau to the
    /// north-east, a cave under the north-east ridge, the marsh to the south, a
    /// hamlet and its farms to the south-west, a hidden clearing for the shinobi
    /// hideout in the north-west forest, and a waterfall where the river drops
    /// into the valley. Mountains at the region's edge close it.
    /// </para>
    /// </summary>
    public static class ZoneTerrain
    {
        // ------------------------------------------------------------- shape

        /// <summary>Half-width of the baked valley.</summary>
        public const float Half = 100f;

        /// <summary>Half-width of the whole streamed region.</summary>
        public const float RegionHalf = 400f;

        private const float RingInner = 78f, RingOuter = 100f, RingHeight = 30f, RingFall = 175f;
        private const float MeadowBase = 3.4f;
        private const float EdgeStart = 330f, EdgeHeight = 60f;

        public static readonly Vector3 VillageCentre = new(0f, 0f, 0f);
        /// <summary>
        /// The flattened bowl the village stands in. Wide enough to take the ring
        /// of houses around the green, so no building sits on a slope.
        /// </summary>
        public const float VillageRadius = 34f;

        public static readonly Vector3 CampCentre = new(46f, 0f, -54f);
        public const float CampRadius = 20f;
        private const float CampLift = 9.5f;

        private const float RiverX = -52f, RiverWidth = 9f, RiverDepth = 3.2f;
        public const float RiverCentreX = RiverX;

        /// <summary>Water surface height for the river inside the valley.</summary>
        public const float WaterLevel = -1.5f;

        /// <summary>The river north of the waterfall runs this much higher.</summary>
        public const float UpperRiverLift = 8f;
        public const float WaterfallZ = 118f;

        // Outer landmarks. Every other system (streamer, dressing, discovery,
        // HUD) reads these rather than repeating the numbers.
        public static readonly Vector3 TempleHill = new(250f, 0f, 60f);
        public const float TempleRadius = 26f, TempleLift = 22f;
        public static readonly Vector3 Stronghold = new(240f, 0f, 250f);
        public const float StrongholdRadius = 45f, StrongholdLift = 18f;
        public static readonly Vector3 CaveMouth = new(160f, 0f, 200f);
        public const float CaveRadius = 12f, CaveDepth = 5f;
        public static readonly Vector3 MarshCentre = new(40f, 0f, -290f);
        public const float MarshRadius = 120f;
        public static readonly Vector3 Hamlet = new(-250f, 0f, -150f);
        public const float HamletRadius = 40f;
        public static readonly Vector3 Hideout = new(-290f, 0f, 210f);
        public const float HideoutRadius = 18f;
        public static readonly Vector3 Waterfall = new(RiverX, 0f, WaterfallZ);
        public static readonly Vector3 PassNorth = new(4f, 0f, 92f);
        public static readonly Vector3 PassEast = new(96f, 0f, 58f);
        public static readonly Vector3 PassSouth = new(70f, 0f, -92f);

        /// <summary>The valley road, unchanged, then the roads that leave it.</summary>
        private static readonly Vector2[][] Roads =
        {
            new Vector2[]
            {
                new(4f, 92f), new(2f, 70f), new(-6f, 52f), new(-4f, 30f),
                new(0f, 8f),
                new(6f, -14f), new(20f, -30f), new(36f, -44f),
                new(46f, -54f),
            },
            // North: through the pass, up the switchbacks to the region's edge.
            new Vector2[] { new(4f, 92f), new(2f, 130f), new(-14f, 180f), new(6f, 240f), new(-4f, 290f), new(-8f, 318f) },
            // East: from the north road out through the eastern pass to the temple.
            new Vector2[] { new(2f, 70f), new(40f, 72f), new(80f, 62f), new(120f, 55f), new(170f, 55f), new(224f, 60f) },
            // South-east: past the camp, down through the pass to the marsh edge.
            new Vector2[] { new(46f, -54f), new(62f, -78f), new(82f, -112f), new(64f, -160f), new(44f, -220f) },
            // West: over the bridge to the hamlet and its farms.
            new Vector2[] { new(-52f, 6f), new(-92f, -8f), new(-142f, -48f), new(-200f, -108f), new(-246f, -146f) },
            // Temple to stronghold: the old pilgrim road Kagehira's men now hold.
            new Vector2[] { new(250f, 86f), new(262f, 140f), new(250f, 200f), new(240f, 220f) },
        };

        // ------------------------------------------------------------ height

        /// <summary>Ground height at a world XZ.</summary>
        public static float HeightAt(float x, float z) => Compose(x, z, false);

        /// <summary>
        /// The height function proper. `low` drops the high-frequency noise and
        /// the road term: that variant is what a road is flattened toward, so a
        /// road follows the hills and the pass saddles instead of cutting a trench.
        /// </summary>
        private static float Compose(float x, float z, bool low)
        {
            var r = Mathf.Sqrt(x * x + z * z);
            var inner = r < RingInner + 40f;
            // Needed by the ring (passes follow the roads) and by the road term.
            var roadDist = r > RingInner - 8f ? RoadDistance(x, z) : float.MaxValue;

            float h;
            if (inner)
            {
                h = MeadowBase
                    + Noise(x * 0.011f, z * 0.011f) * 3.0f
                    + Noise(x * 0.028f, z * 0.028f) * 1.2f
                    + (low ? 0f : Noise(x * 0.070f, z * 0.070f) * 0.4f);
            }
            else
            {
                h = low ? OuterLow(x, z) : OuterBase(x, z);
            }

            // The mountain ring: rises from the valley floor to 30 m and, beyond
            // the old world's edge, falls away again to the outer lands. Passes
            // and the river's gorges cut it.
            if (r > RingInner)
            {
                var rise = Smooth01(0f, 1f, Mathf.InverseLerp(RingInner, RingOuter, r));
                var fall = 1f - Smooth01(RingOuter + 20f, RingFall, r);
                var ridge = low ? 1f : 1f + Noise(x * 0.05f, z * 0.05f) * 0.45f;
                var open = RingOpening(x, z, roadDist);
                var ringH = rise * fall * RingHeight * ridge * (1f - open * 0.78f);
                h += ringH;
                // Where the ring falls away the inner meadow noise must hand over
                // to the outer rolling ground without a seam.
                if (inner && r > RingOuter)
                {
                    var t = Smooth01(RingOuter, RingInner + 40f, r);
                    h = Mathf.Lerp(h, (low ? OuterLow(x, z) : OuterBase(x, z)) + ringH, t);
                }
            }

            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius + 10f)
            {
                var t = 1f - Smooth01(CampRadius - 4f, CampRadius + 10f, camp);
                h = Mathf.Lerp(h, CampLift + Noise(x * 0.05f, z * 0.05f) * 0.6f, t);
            }

            var vil = Mathf.Sqrt(Sq(x - VillageCentre.x) + Sq(z - VillageCentre.z));
            if (vil < VillageRadius + 12f)
            {
                var t = 1f - Smooth01(VillageRadius - 2f, VillageRadius + 12f, vil);
                h = Mathf.Lerp(h, MeadowBase - 1.0f, t * 0.92f);
            }

            var marsh = inner && r < RingInner ? 0f : MarshMask(x, z);
            if (!inner || r > RingInner) h = Landmarks(x, z, h, marsh);

            if (!low)
            {
                var road = roadDist < float.MaxValue ? roadDist : RoadDistance(x, z);
                if (road < 7f && marsh < 0.5f)
                {
                    var t = 1f - Smooth01(3.2f, 7f, road);
                    h = Mathf.Lerp(h, RoadHeight(x, z, r), t * 0.85f);
                }
            }

            var rv = RiverDistance(x, z);
            var lift = z > WaterfallZ ? UpperRiverLift * Smooth01(WaterfallZ - 4f, WaterfallZ + 4f, z) : 0f;
            // Dry land never sinks below the water line; the marsh is allowed to.
            // The upper river's lift only raises the floor near the river itself.
            var near = 1f - Smooth01(RiverWidth + 6f, RiverWidth + 18f, rv);
            if (marsh < 0.5f) h = Mathf.Max(h, WaterLevel + lift * near + 1.1f);
            if (rv < RiverWidth + 6f && marsh < 0.5f)
            {
                var t = 1f - Smooth01(RiverWidth * 0.5f, RiverWidth + 6f, rv);
                h = Mathf.Lerp(h, -RiverDepth + lift, t);
            }

            // The world's edge: mountains no pass crosses.
            var edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge > EdgeStart)
                h += Smooth01(EdgeStart, RegionHalf, edge) * EdgeHeight * (1f + Noise(x * 0.03f, z * 0.03f) * 0.3f);

            return h;
        }

        /// <summary>Rolling ground beyond the ring.</summary>
        private static float OuterBase(float x, float z)
        {
            return MeadowBase
                   + Noise(x * 0.005f + 3f, z * 0.005f - 5f) * 8f
                   + Noise(x * 0.016f, z * 0.016f) * 3f
                   + Noise(x * 0.05f, z * 0.05f) * 1.0f
                   + Noise(x * 0.12f, z * 0.12f) * 0.35f;
        }

        /// <summary>Low-frequency ground only: what a road is flattened toward.</summary>
        private static float OuterLow(float x, float z)
        {
            return MeadowBase
                   + Noise(x * 0.005f + 3f, z * 0.005f - 5f) * 8f
                   + Noise(x * 0.016f, z * 0.016f) * 3f;
        }

        /// <summary>
        /// 0 solid ring, 1 fully open. A pass is wherever a road crosses the
        /// ring — the saddle follows the road the whole way through instead of a
        /// circle it would climb out of — and the river cuts its two gorges.
        /// </summary>
        private static float RingOpening(float x, float z, float roadDist)
        {
            var open = 1f - Smooth01(10f, 26f, roadDist);
            var rv = RiverDistance(x, z);
            open = Mathf.Max(open, 1f - Smooth01(12f, 26f, rv));
            return open;
        }

        private static float Landmarks(float x, float z, float h, float m)
        {
            // Temple hill: a broad rise with a flat crown.
            // A long skirt, so the pilgrim road climbs it at a walk rather than a scramble.
            var d = Mathf.Sqrt(Sq(x - TempleHill.x) + Sq(z - TempleHill.z));
            if (d < TempleRadius + 70f)
            {
                var t = 1f - Smooth01(TempleRadius, TempleRadius + 70f, d);
                h = Mathf.Lerp(h, OuterLow(TempleHill.x, TempleHill.z) + TempleLift + Noise(x * 0.06f, z * 0.06f) * 0.25f, t);
            }
            // Stronghold: a plateau big enough for walls, towers and a yard.
            d = Mathf.Sqrt(Sq(x - Stronghold.x) + Sq(z - Stronghold.z));
            if (d < StrongholdRadius + 40f)
            {
                var t = 1f - Smooth01(StrongholdRadius, StrongholdRadius + 40f, d);
                h = Mathf.Lerp(h, OuterLow(Stronghold.x, Stronghold.z) + StrongholdLift + Noise(x * 0.06f, z * 0.06f) * 0.3f, t);
            }
            // Cave mouth: a sunken bowl under the ridge.
            d = Mathf.Sqrt(Sq(x - CaveMouth.x) + Sq(z - CaveMouth.z));
            if (d < CaveRadius + 12f)
            {
                var t = 1f - Smooth01(CaveRadius, CaveRadius + 12f, d);
                h = Mathf.Lerp(h, h - CaveDepth, t);
            }
            // Marsh: low, flat, wet, with pools where the noise dips.
            if (m > 0f)
            {
                var pool = Noise(x * 0.03f + 9f, z * 0.03f + 2f);
                var wet = WaterLevel + 0.55f + Noise(x * 0.09f, z * 0.09f) * 0.35f
                          + (pool < -0.25f ? Mathf.Lerp(0f, -1.3f, Mathf.InverseLerp(-0.25f, -0.7f, pool)) : 0f);
                h = Mathf.Lerp(h, wet, m);
            }
            // Hamlet and hideout: flat ground for buildings.
            d = Mathf.Sqrt(Sq(x - Hamlet.x) + Sq(z - Hamlet.z));
            if (d < HamletRadius + 24f)
            {
                var t = 1f - Smooth01(HamletRadius, HamletRadius + 24f, d);
                h = Mathf.Lerp(h, OuterLow(Hamlet.x, Hamlet.z) + Noise(x * 0.05f, z * 0.05f) * 0.3f, t);
            }
            d = Mathf.Sqrt(Sq(x - Hideout.x) + Sq(z - Hideout.z));
            if (d < HideoutRadius + 14f)
            {
                var t = 1f - Smooth01(HideoutRadius, HideoutRadius + 14f, d);
                h = Mathf.Lerp(h, OuterLow(Hideout.x, Hideout.z) + 0.3f, t);
            }
            return h;
        }

        /// <summary>0 outside the marsh, 1 in its heart.</summary>
        public static float MarshMask(float x, float z)
        {
            var d = Mathf.Sqrt(Sq(x - MarshCentre.x) + Sq(z - MarshCentre.z));
            var edge = MarshRadius + Noise(x * 0.02f, z * 0.02f) * 22f;
            return 1f - Smooth01(edge - 30f, edge + 20f, d);
        }

        private static float RoadHeight(float x, float z, float r)
        {
            // Inside the valley the road is the flat packed track it always was.
            // From the ring outward it follows the low-frequency ground — the
            // same function without its fine noise or its own road term — so it
            // climbs the pass saddles and the hills. A landmark's flattened floor
            // takes no dip at all, or the road would trench the hamlet's yard.
            var flat = MeadowBase - 0.9f;
            var outside = flat;
            if (r >= RingInner - 4f)
            {
                var dip = 0.4f * (1f - FlatFloor(x, z));
                var target = Compose(x, z, true) - dip;
                outside = Mathf.Lerp(flat, target, Smooth01(RingInner - 4f, RingOuter - 4f, r));
            }
            // The camp plateau: the road climbs onto it, and on the far side hands
            // over to whatever the ground is there — the ring's saddle, not the
            // meadow, which is where the old constant left a seven-metre step.
            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius + 10f)
            {
                var tc = 1f - Smooth01(CampRadius - 4f, CampRadius + 10f, camp);
                return Mathf.Lerp(outside, CampLift, tc);
            }
            return outside;
        }

        /// <summary>1 on a landmark's flattened floor, 0 elsewhere.</summary>
        private static float FlatFloor(float x, float z)
        {
            var f = 0f;
            f = Mathf.Max(f, 1f - Smooth01(HamletRadius, HamletRadius + 24f, Mathf.Sqrt(Sq(x - Hamlet.x) + Sq(z - Hamlet.z))));
            f = Mathf.Max(f, 1f - Smooth01(TempleRadius, TempleRadius + 70f, Mathf.Sqrt(Sq(x - TempleHill.x) + Sq(z - TempleHill.z))));
            f = Mathf.Max(f, 1f - Smooth01(StrongholdRadius, StrongholdRadius + 40f, Mathf.Sqrt(Sq(x - Stronghold.x) + Sq(z - Stronghold.z))));
            f = Mathf.Max(f, 1f - Smooth01(HideoutRadius, HideoutRadius + 14f, Mathf.Sqrt(Sq(x - Hideout.x) + Sq(z - Hideout.z))));
            return f;
        }

        public static float RoadDistance(float x, float z)
        {
            var d = float.MaxValue;
            foreach (var road in Roads)
                for (var i = 0; i < road.Length - 1; i++)
                    d = Mathf.Min(d, SegDist(x, z, road[i], road[i + 1]));
            return d;
        }

        /// <summary>The road polylines, for tooling that draws or walks them.</summary>
        public static Vector2[][] RoadLines => Roads;

        public static float RiverDistance(float x, float z)
        {
            var cx = RiverX + Mathf.Sin(z * 0.032f) * 9f + Mathf.Sin(z * 0.011f) * 5f;
            return Mathf.Abs(x - cx);
        }

        /// <summary>Water surface height here: the river, the upper river, or the marsh.</summary>
        public static float WaterLevelAt(float x, float z)
        {
            if (z > WaterfallZ && MarshMask(x, z) < 0.5f)
            {
                // The upper river's lift belongs to the river: away from its banks
                // the water line is the valley's, or the cave under the ridge and
                // every low field north of the falls would flood.
                var near = 1f - Smooth01(RiverWidth + 6f, RiverWidth + 18f, RiverDistance(x, z));
                return WaterLevel + UpperRiverLift * Smooth01(WaterfallZ - 4f, WaterfallZ + 4f, z) * near;
            }
            return WaterLevel;
        }

        /// <summary>Steepness 0 flat to 1 vertical.</summary>
        public static float SlopeAt(float x, float z)
        {
            const float d = 1.5f;
            var hx = HeightAt(x + d, z) - HeightAt(x - d, z);
            var hz = HeightAt(x, z + d) - HeightAt(x, z - d);
            return Mathf.Clamp01(Mathf.Sqrt(hx * hx + hz * hz) / (2f * d));
        }

        // ------------------------------------------------------------ maths

        private static float Sq(float v) => v * v;

        /// <summary>
        /// GLSL-style smoothstep. NOT <c>Mathf.SmoothStep</c>, which is a smoothed
        /// Lerp between two values and returns the edge itself when fed a distance.
        /// </summary>
        public static float Smooth01(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1)) return x < edge0 ? 0f : 1f;
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Deterministic value noise in [-1,1]; identical on every machine.</summary>
        public static float Noise(float x, float y)
        {
            var xi = Mathf.Floor(x);
            var yi = Mathf.Floor(y);
            var xf = x - xi;
            var yf = y - yi;
            var u = xf * xf * (3f - 2f * xf);
            var v = yf * yf * (3f - 2f * yf);

            var a = Hash(xi, yi);
            var b = Hash(xi + 1f, yi);
            var c = Hash(xi, yi + 1f);
            var d = Hash(xi + 1f, yi + 1f);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Hash(float x, float y)
        {
            var h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return (h - Mathf.Floor(h)) * 2f - 1f;
        }

        private static float SegDist(float px, float pz, Vector2 a, Vector2 b)
        {
            var abx = b.x - a.x; var abz = b.y - a.y;
            var apx = px - a.x; var apz = pz - a.y;
            var len = abx * abx + abz * abz;
            var t = len < 0.0001f ? 0f : Mathf.Clamp01((apx * abx + apz * abz) / len);
            var dx = apx - abx * t; var dz = apz - abz * t;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>
    /// The ground everything stands on, whichever world is loaded.
    ///
    /// <para>
    /// The arenas were flat at y = 0 and every mover hard-coded that. This is the
    /// one place that answers "how high is the floor here", so a scene without the
    /// valley — the endless Road North corridor, the opening — keeps the old
    /// behaviour by returning 0, and a scene with it gets the real surface.
    /// </para>
    /// </summary>
    public static class Ground
    {
        /// <summary>Set by <see cref="ZoneWorld"/> when a valley scene loads.</summary>
        public static bool ZoneActive;

        public static float HeightAt(float x, float z)
            => ZoneActive ? ZoneTerrain.HeightAt(x, z) : 0f;

        public static float HeightAt(Vector3 p) => HeightAt(p.x, p.z);

        /// <summary>Same position, sitting on the floor.</summary>
        public static Vector3 Snap(Vector3 p)
        {
            p.y = HeightAt(p.x, p.z);
            return p;
        }
    }
}
