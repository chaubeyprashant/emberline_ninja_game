using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Low-poly terrain for the open mission zone: a flat-shaded height mesh
    /// carved into chunks, coloured from a tiny palette atlas.
    ///
    /// <para>
    /// Why a generated mesh and not Unity Terrain: Unity's Terrain component
    /// wants splatmaps, its own shader family, and a heightmap resolution that
    /// costs real memory on an A33. It also renders smooth — and the whole cast
    /// is faceted KayKit low-poly, so a smooth ground plane under chunky
    /// characters is the exact mismatch the art direction cannot afford.
    /// </para>
    ///
    /// <para>
    /// Flat shading is achieved by never sharing a vertex between triangles, so
    /// every triangle gets its own normal and reads as a facet. That triples the
    /// vertex count, which is why the mesh is coarse (2.5 m quads) and chunked:
    /// each chunk is one draw call and culls on its own.
    /// </para>
    ///
    /// <para>
    /// Colour comes from a 4x4 palette texture rather than per-material tints, so
    /// grass, dirt road, rock, and riverbed all live in one material and one draw
    /// call. Every vertex of a triangle samples the same palette cell centre,
    /// which keeps the colour flat per facet and the transitions crisp — again,
    /// matching the characters rather than fighting them.
    /// </para>
    /// </summary>
    public static class EmberTerrain
    {
        // ------------------------------------------------------------ shape

        /// <summary>Half-width of the whole generated zone, in metres.</summary>
        public const float Half = 100f;

        /// <summary>Edge length of one terrain quad.</summary>
        private const float Quad = 2.5f;

        /// <summary>Quads per chunk edge. 80 quads / 10 = an 8x8 chunk grid.</summary>
        private const int ChunkQuads = 10;

        /// <summary>Where the mountain ring starts and where it tops out.</summary>
        private const float RingInner = 78f, RingOuter = 100f, RingHeight = 30f;

        /// <summary>
        /// The meadow floor sits well above the water line. Without this the base
        /// noise swings either side of zero and every dip below the river surface
        /// becomes an accidental pond — which is exactly what the first build did:
        /// it flooded the whole valley.
        /// </summary>
        private const float MeadowBase = 3.4f;

        // Landmarks. These are the contract between the terrain and everything
        // placed on it, so the zone builder reads them from here rather than
        // duplicating magic numbers.
        public static readonly Vector3 VillageCentre = new(0f, 0f, 0f);
        public const float VillageRadius = 26f;

        public static readonly Vector3 CampCentre = new(46f, 0f, -54f);
        public const float CampRadius = 20f;
        private const float CampLift = 9.5f;

        /// <summary>The river runs north-south down the west side.</summary>
        private const float RiverX = -52f, RiverWidth = 9f, RiverDepth = 3.2f;

        /// <summary>Mean X of the meander, so the water quad can be sized to it.</summary>
        public const float RiverCentreX = RiverX;

        /// <summary>Palette cells, as (column,row) in the 4x4 atlas.</summary>
        private enum Ground { Grass = 0, Grass2 = 1, Grass3 = 2, Dirt = 3, Road = 4, Rock = 5, Scree = 6, Sand = 7, Riverbed = 8 }

        // ------------------------------------------------------------ height

        /// <summary>
        /// Height of the ground at a world XZ. Public because enemies, props and
        /// the zone builder all need to sit things exactly on the surface.
        /// </summary>
        public static float HeightAt(float x, float z)
        {
            // Rolling base. Three octaves of cheap value noise; deterministic so
            // a rebuild puts every tree back where it was.
            var h = MeadowBase
                  + Noise(x * 0.011f, z * 0.011f) * 3.0f
                  + Noise(x * 0.028f, z * 0.028f) * 1.2f
                  + Noise(x * 0.070f, z * 0.070f) * 0.4f;

            // Mountain ring: the natural boundary. Rises from RingInner outward,
            // so the playable bowl is ringed by climbable-looking rock the player
            // reads as "the edge of the valley" rather than an invisible wall.
            var r = Mathf.Sqrt(x * x + z * z);
            if (r > RingInner)
            {
                var t = Mathf.InverseLerp(RingInner, RingOuter, r);
                // Smoothstep in, then a ridge wobble so the skyline is not a bowl rim.
                var ridge = 1f + Noise(x * 0.05f, z * 0.05f) * 0.45f;
                h += Smooth01(0f, 1f, t) * RingHeight * ridge;
            }

            // Camp plateau: high ground the player has to climb or flank.
            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius + 10f)
            {
                var t = 1f - Smooth01(CampRadius - 4f, CampRadius + 10f, camp);
                h = Mathf.Lerp(h, CampLift + Noise(x * 0.05f, z * 0.05f) * 0.6f, t);
            }

            // Village bowl: flattened so buildings sit level and fights are readable.
            var vil = Mathf.Sqrt(Sq(x - VillageCentre.x) + Sq(z - VillageCentre.z));
            if (vil < VillageRadius + 12f)
            {
                var t = 1f - Smooth01(VillageRadius - 2f, VillageRadius + 12f, vil);
                h = Mathf.Lerp(h, MeadowBase - 1.0f, t * 0.92f);
            }

            // The road is graded: it cuts gently through whatever it crosses.
            var road = RoadDistance(x, z);
            if (road < 7f)
            {
                var t = 1f - Smooth01(3.2f, 7f, road);
                h = Mathf.Lerp(h, RoadHeight(x, z), t * 0.85f);
            }

            // Nothing but the river may sit below the water line. Clamping here,
            // before the channel is cut, is what keeps the valley dry.
            h = Mathf.Max(h, WaterLevel + 1.1f);

            // River channel, carved last so it wins over everything but the ring.
            var rv = RiverDistance(x, z);
            if (rv < RiverWidth + 6f)
            {
                var t = 1f - Smooth01(RiverWidth * 0.5f, RiverWidth + 6f, rv);
                h = Mathf.Lerp(h, -RiverDepth, t);
            }

            return h;
        }

        /// <summary>
        /// Approximate ground steepness at a point, 0 flat to 1 vertical. Sampled
        /// from the height field rather than the mesh, so scatter code can ask
        /// before a prop exists. Flat-bottomed props sink into a slope on one side
        /// and hang in the air on the other, so anything placed by hand needs this.
        /// </summary>
        public static float SlopeAt(float x, float z)
        {
            const float d = 1.5f;
            var hx = HeightAt(x + d, z) - HeightAt(x - d, z);
            var hz = HeightAt(x, z + d) - HeightAt(x, z - d);
            var grad = Mathf.Sqrt(hx * hx + hz * hz) / (2f * d);
            return Mathf.Clamp01(grad);
        }

        /// <summary>Water surface height for the river.</summary>
        public const float WaterLevel = -1.5f;

        /// <summary>
        /// The road: north gate → village → camp approach. Returned as distance
        /// so both the mesh and the prop scatter can ask "am I on the road".
        /// </summary>
        public static float RoadDistance(float x, float z)
        {
            var d = float.MaxValue;
            for (var i = 0; i < RoadPts.Length - 1; i++)
                d = Mathf.Min(d, SegDist(x, z, RoadPts[i], RoadPts[i + 1]));
            return d;
        }

        private static float RoadHeight(float x, float z)
        {
            // Follow the terrain's broad shape but ignore its noise, so the road
            // grades smoothly instead of rippling.
            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius + 10f)
            {
                var t = 1f - Smooth01(CampRadius - 4f, CampRadius + 10f, camp);
                return Mathf.Lerp(MeadowBase - 0.8f, CampLift, t);
            }
            return MeadowBase - 0.9f;
        }

        private static readonly Vector2[] RoadPts =
        {
            new(4f, 92f), new(2f, 70f), new(-6f, 52f), new(-4f, 30f),
            new(0f, 8f),                                   // through the village
            new(6f, -14f), new(20f, -30f), new(36f, -44f),
            new(CampCentre.x, CampCentre.z),               // up to the camp gate
        };

        public static float RiverDistance(float x, float z)
        {
            // A lazy meander rather than a straight ditch.
            var cx = RiverX + Mathf.Sin(z * 0.032f) * 9f + Mathf.Sin(z * 0.011f) * 5f;
            return Mathf.Abs(x - cx);
        }

        // ------------------------------------------------------------ colour

        private static Ground GroundAt(float x, float z, float h, Vector3 normal)
        {
            if (RiverDistance(x, z) < RiverWidth * 0.55f) return Ground.Riverbed;
            if (RiverDistance(x, z) < RiverWidth + 2.5f) return Ground.Sand;

            if (RoadDistance(x, z) < 3.4f) return Ground.Road;
            if (RoadDistance(x, z) < 5.0f) return Ground.Dirt;

            // Steep faces and high ground turn to rock: the mountain ring reads as
            // stone without needing a second material.
            var slope = 1f - normal.y;
            if (slope > 0.42f) return Ground.Rock;
            if (h > 16f) return Ground.Scree;
            if (slope > 0.26f) return Ground.Scree;

            // Trodden ground around the two settlements.
            var vil = Mathf.Sqrt(Sq(x - VillageCentre.x) + Sq(z - VillageCentre.z));
            if (vil < VillageRadius * 0.72f) return Ground.Dirt;
            var camp = Mathf.Sqrt(Sq(x - CampCentre.x) + Sq(z - CampCentre.z));
            if (camp < CampRadius * 0.8f) return Ground.Dirt;

            // Three grass shades, picked by noise, so the meadow is not one flat
            // colour across 200 metres.
            var g = Noise(x * 0.04f + 11f, z * 0.04f - 7f);
            return g > 0.55f ? Ground.Grass2 : g < -0.35f ? Ground.Grass3 : Ground.Grass;
        }

        /// <summary>Palette colours, indexed by <see cref="Ground"/>.</summary>
        private static readonly Color[] Palette =
        {
            new(0.170f, 0.235f, 0.180f),  // Grass   — cool night green
            new(0.145f, 0.205f, 0.165f),  // Grass2  — darker patch
            new(0.200f, 0.255f, 0.185f),  // Grass3  — lighter patch
            new(0.245f, 0.215f, 0.170f),  // Dirt    — trodden earth
            new(0.290f, 0.250f, 0.195f),  // Road    — pale packed track
            new(0.215f, 0.225f, 0.245f),  // Rock    — blue-grey stone
            new(0.255f, 0.260f, 0.270f),  // Scree   — lighter broken stone
            new(0.300f, 0.285f, 0.235f),  // Sand    — river shingle
            new(0.130f, 0.165f, 0.180f),  // Riverbed— wet dark
        };

        private const string PalettePath = "Assets/Art/Environments/Zone/terrain_palette.png";
        private const string MatPath = "Assets/Prefabs/Mat_ZoneTerrain.mat";

        /// <summary>Writes the 4x4 palette PNG and the shared terrain material.</summary>
        public static Material EnsureMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PalettePath));

            if (!File.Exists(PalettePath))
            {
                // 4x4 cells, 16 px each, point-sampled. Padding is unnecessary
                // because every vertex samples a cell centre exactly.
                const int cell = 16, dim = 4, size = cell * dim;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var idx = (y / cell) * dim + (x / cell);
                    tex.SetPixel(x, y, idx < Palette.Length ? Palette[idx] : Color.magenta);
                }
                tex.Apply();
                File.WriteAllBytes(PalettePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(PalettePath);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(PalettePath);
            if (imp != null)
            {
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Surface"));
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            mat.shader = Shader.Find("Emberline/Surface");
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", 0.06f);   // ground is not shiny
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_WearStrength", 0.30f); // breaks up the flat palette
            mat.SetFloat("_WearScale", 0.9f);
            mat.SetFloat("_RimStrength", 0.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------ build

        /// <summary>
        /// Builds the chunked terrain under <paramref name="parent"/> and saves
        /// the meshes as assets so the scene does not carry them inline.
        /// </summary>
        public static void Build(Transform parent, string meshDir)
        {
            Directory.CreateDirectory(meshDir);
            var mat = EnsureMaterial();

            var quads = Mathf.RoundToInt(Half * 2f / Quad);      // 80
            var chunks = quads / ChunkQuads;                      // 8

            for (var cz = 0; cz < chunks; cz++)
            for (var cx = 0; cx < chunks; cx++)
            {
                var mesh = BuildChunk(cx, cz);
                var path = $"{meshDir}/terrain_{cx}_{cz}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);

                var go = new GameObject($"Terrain_{cx}_{cz}");
                go.transform.SetParent(parent, false);
                go.isStatic = true;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                // Terrain never needs to cast onto itself from the low back-light;
                // receiving is what matters and casting doubles the shadow cost.
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        private static Mesh BuildChunk(int cx, int cz)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            var x0 = -Half + cx * ChunkQuads * Quad;
            var z0 = -Half + cz * ChunkQuads * Quad;

            for (var qz = 0; qz < ChunkQuads; qz++)
            for (var qx = 0; qx < ChunkQuads; qx++)
            {
                var ax = x0 + qx * Quad;
                var az = z0 + qz * Quad;
                var bx = ax + Quad;
                var bz = az + Quad;

                var p00 = new Vector3(ax, HeightAt(ax, az), az);
                var p10 = new Vector3(bx, HeightAt(bx, az), az);
                var p01 = new Vector3(ax, HeightAt(ax, bz), bz);
                var p11 = new Vector3(bx, HeightAt(bx, bz), bz);

                // Split the quad along the shorter diagonal so ridges stay sharp
                // instead of being averaged into a saddle.
                if (Mathf.Abs(p00.y - p11.y) <= Mathf.Abs(p10.y - p01.y))
                {
                    AddTri(verts, norms, uvs, tris, p00, p01, p11);
                    AddTri(verts, norms, uvs, tris, p00, p11, p10);
                }
                else
                {
                    AddTri(verts, norms, uvs, tris, p00, p01, p10);
                    AddTri(verts, norms, uvs, tris, p01, p11, p10);
                }
            }

            var mesh = new Mesh { name = $"terrain_{cx}_{cz}" };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Appends one flat-shaded triangle: its own three vertices, one shared
        /// face normal, and all three UVs on the same palette cell centre.
        /// </summary>
        private static void AddTri(List<Vector3> verts, List<Vector3> norms,
                                   List<Vector2> uvs, List<int> tris,
                                   Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            if (n.y < 0f) n = -n;

            var mid = (a + b + c) / 3f;
            var uv = CellUv(GroundAt(mid.x, mid.z, mid.y, n));

            var i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        private static Vector2 CellUv(Ground g)
        {
            const int dim = 4;
            var idx = (int)g;
            var col = idx % dim;
            var row = idx / dim;
            // Centre of the cell, so point filtering can never bleed a neighbour.
            return new Vector2((col + 0.5f) / dim, (row + 0.5f) / dim);
        }

        // ------------------------------------------------------------ helpers

        private static float Sq(float v) => v * v;

        /// <summary>
        /// GLSL-style smoothstep: 0 below <paramref name="edge0"/>, 1 above
        /// <paramref name="edge1"/>, smoothly interpolated between.
        ///
        /// <para>
        /// This exists because <c>Mathf.SmoothStep(a, b, t)</c> is NOT this
        /// function. Unity's version is a smoothed <c>Lerp</c>: it clamps t to
        /// 0..1 and returns a value between a and b. Feeding it a world distance
        /// as t therefore returns the edge value itself — <c>SmoothStep(66, 84, 50)</c>
        /// is 84, not 0 — and the first version of this terrain used it that way
        /// throughout. Every carve silently became <c>Lerp(h, target, negative)</c>,
        /// which Mathf.Lerp clamps to zero, so the village never flattened, the
        /// road never graded and the river never cut. It looked plausible only
        /// because the colouring is computed separately from the height.
        /// </para>
        /// </summary>
        public static float Smooth01(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1)) return x < edge0 ? 0f : 1f;
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Deterministic value noise in [-1,1]. Sine hashing rather than Perlin
        /// so the terrain is identical on every machine and every rebuild without
        /// shipping a permutation table.
        /// </summary>
        public static float Noise(float x, float y)
        {
            var xi = Mathf.Floor(x);
            var yi = Mathf.Floor(y);
            var xf = x - xi;
            var yf = y - yi;
            // Smoothstep the cell interpolation, or the terrain shows a grid.
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
}
