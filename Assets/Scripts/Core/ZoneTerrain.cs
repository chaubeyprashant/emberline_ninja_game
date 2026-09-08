using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// The valley's height field, at runtime.
    ///
    /// <para>
    /// This is the same function the editor uses to generate the terrain mesh —
    /// <c>EmberTerrain</c> delegates to it — so a query here is exactly the
    /// surface the player is standing on, with no raycast and no collider lookup.
    /// That matters because enemies ask for it every frame: a physics raycast per
    /// enemy per frame is affordable but pointless when the ground is a pure
    /// function of x and z.
    /// </para>
    ///
    /// <para>
    /// It lives in runtime code rather than the editor assembly because the
    /// arenas used to be a flat deck at y = 0 and every mover assumed it. Enemies
    /// landed at <c>y = 0</c>, "launched" meant <c>y &gt; 0.02</c>, and villagers
    /// walked a plane. On real terrain all of that needs a ground height, and it
    /// needs one that agrees with the mesh to the millimetre.
    /// </para>
    /// </summary>
    public static class ZoneTerrain
    {
        // ------------------------------------------------------------- shape

        public const float Half = 100f;

        private const float RingInner = 78f, RingOuter = 100f, RingHeight = 30f;
        private const float MeadowBase = 3.4f;

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

        /// <summary>Water surface height for the river.</summary>
        public const float WaterLevel = -1.5f;

        private static readonly Vector2[] RoadPts =
        {
            new(4f, 92f), new(2f, 70f), new(-6f, 52f), new(-4f, 30f),
            new(0f, 8f),
            new(6f, -14f), new(20f, -30f), new(36f, -44f),
            new(46f, -54f),
        };

        // ------------------------------------------------------------ height

        /// <summary>Ground height at a world XZ.</summary>
        public static float HeightAt(float x, float z)
        {
            var h = MeadowBase
                  + Noise(x * 0.011f, z * 0.011f) * 3.0f
                  + Noise(x * 0.028f, z * 0.028f) * 1.2f
                  + Noise(x * 0.070f, z * 0.070f) * 0.4f;

            var r = Mathf.Sqrt(x * x + z * z);
            if (r > RingInner)
            {
                var t = Mathf.InverseLerp(RingInner, RingOuter, r);
                var ridge = 1f + Noise(x * 0.05f, z * 0.05f) * 0.45f;
                h += Smooth01(0f, 1f, t) * RingHeight * ridge;
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

            var road = RoadDistance(x, z);
            if (road < 7f)
            {
                var t = 1f - Smooth01(3.2f, 7f, road);
                h = Mathf.Lerp(h, RoadHeight(x, z), t * 0.85f);
            }

            h = Mathf.Max(h, WaterLevel + 1.1f);

            var rv = RiverDistance(x, z);
            if (rv < RiverWidth + 6f)
            {
                var t = 1f - Smooth01(RiverWidth * 0.5f, RiverWidth + 6f, rv);
                h = Mathf.Lerp(h, -RiverDepth, t);
            }

            return h;
        }

        private static float RoadHeight(float x, float z)
        {
            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius + 10f)
            {
                var t = 1f - Smooth01(CampRadius - 4f, CampRadius + 10f, camp);
                return Mathf.Lerp(MeadowBase - 0.8f, CampLift, t);
            }
            return MeadowBase - 0.9f;
        }

        public static float RoadDistance(float x, float z)
        {
            var d = float.MaxValue;
            for (var i = 0; i < RoadPts.Length - 1; i++)
                d = Mathf.Min(d, SegDist(x, z, RoadPts[i], RoadPts[i + 1]));
            return d;
        }

        public static float RiverDistance(float x, float z)
        {
            var cx = RiverX + Mathf.Sin(z * 0.032f) * 9f + Mathf.Sin(z * 0.011f) * 5f;
            return Mathf.Abs(x - cx);
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

    /// <summary>
    /// Keeps a transform-driven mover sitting on the ground.
    ///
    /// <para>
    /// Villagers, prisoners and lantern bearers all walk by writing XZ straight to
    /// their transform, which was correct when the floor was a plane at y = 0.
    /// One component in LateUpdate is cheaper and far less error-prone than
    /// finding every position write in three movers.
    /// </para>
    /// </summary>
    public class GroundHug : MonoBehaviour
    {
        [Tooltip("Metres above the surface to sit. Negative sinks the model in.")]
        public float offset;

        private void LateUpdate()
        {
            if (!Ground.ZoneActive) return;
            var p = transform.position;
            p.y = Ground.HeightAt(p.x, p.z) + offset;
            transform.position = p;
        }
    }

    /// <summary>
    /// Marks a scene as carrying the valley. Placed by the scene builder; its only
    /// job is to turn <see cref="Ground"/> on for the lifetime of that scene.
    /// </summary>
    public class ZoneWorld : MonoBehaviour
    {
        private void Awake() => Ground.ZoneActive = true;
        private void OnDestroy() => Ground.ZoneActive = false;
    }
}
