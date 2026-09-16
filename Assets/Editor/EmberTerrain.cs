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

        /// <summary>Palette cells, as (column,row) in the 4x4 atlas. Runtime owns them now.</summary>
        private static Color[] Palette => Emberline.Core.TerrainMesh.Palette;

        private const float Quad = Emberline.Core.TerrainMesh.Quad;
        private const int ChunkQuads = Emberline.Core.TerrainMesh.ChunkQuads;

        public static float HeightAt(float x, float z) => ZT.HeightAt(x, z);
        public static float SlopeAt(float x, float z) => ZT.SlopeAt(x, z);
        public static float RoadDistance(float x, float z) => ZT.RoadDistance(x, z);
        public static float RiverDistance(float x, float z) => ZT.RiverDistance(x, z);
        public static float Smooth01(float e0, float e1, float x) => ZT.Smooth01(e0, e1, x);
        public static float Noise(float x, float y) => ZT.Noise(x, y);

        private const string PalettePath = "Assets/Art/Environments/Zone/terrain_palette.png";
        private const string MatPath = "Assets/Prefabs/Mat_ZoneTerrain.mat";

        /// <summary>Writes the 4x4 palette PNG and the shared terrain material.</summary>
        public static Material EnsureMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PalettePath));

            {
                // 4x4 cells, 16 px each, point-sampled. Padding is unnecessary
                // because every vertex samples a cell centre exactly. Rewritten
                // on every build so a new palette cell reaches the atlas.
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
            var x0 = -Half + cx * ChunkQuads * Quad;
            var z0 = -Half + cz * ChunkQuads * Quad;
            return Emberline.Core.TerrainMesh.BuildChunk(x0, z0, $"terrain_{cx}_{cz}");
        }

        // ------------------------------------------------------------ helpers

        private static float Sq(float v) => v * v;
    }
}
