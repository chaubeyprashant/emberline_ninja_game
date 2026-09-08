using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ZT = Emberline.Core.ZoneTerrain;

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
        //
        // The height field itself lives in Emberline.Core.ZoneTerrain, in runtime
        // code, because enemies and villagers have to ask for the ground every
        // frame and the mesh they walk on has to agree with that answer exactly.
        // Everything here forwards to it so there is one definition, not two that
        // drift.

        public const float Half = ZT.Half;
        public const float WaterLevel = ZT.WaterLevel;

        public const float VillageRadius = ZT.VillageRadius;
        public const float CampRadius = ZT.CampRadius;
        public const float RiverCentreX = ZT.RiverCentreX;

        public static Vector3 VillageCentre => ZT.VillageCentre;
        public static Vector3 CampCentre => ZT.CampCentre;

        /// <summary>Edge length of one terrain quad.</summary>
        private const float Quad = 2.5f;

        /// <summary>Quads per chunk edge. 80 quads / 10 = an 8x8 chunk grid.</summary>
        private const int ChunkQuads = 10;

        /// <summary>Palette cells, as (column,row) in the 4x4 atlas.</summary>
        private enum Ground { Grass = 0, Grass2 = 1, Grass3 = 2, Dirt = 3, Road = 4, Rock = 5, Scree = 6, Sand = 7, Riverbed = 8 }

        public static float HeightAt(float x, float z) => ZT.HeightAt(x, z);
        public static float SlopeAt(float x, float z) => ZT.SlopeAt(x, z);
        public static float RoadDistance(float x, float z) => ZT.RoadDistance(x, z);
        public static float RiverDistance(float x, float z) => ZT.RiverDistance(x, z);
        public static float Smooth01(float e0, float e1, float x) => ZT.Smooth01(e0, e1, x);
        public static float Noise(float x, float y) => ZT.Noise(x, y);

        private const float RiverWidth = 9f;


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
    }
}
