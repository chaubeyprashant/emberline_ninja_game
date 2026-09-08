using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Imports the KayKit Medieval Hexagon Pack into runtime-loadable prefabs,
    /// the same way <see cref="EmberDressing"/> does for the dungeon props.
    ///
    /// <para>
    /// The whole pack shares one 1024 gradient atlas, so every prefab gets the
    /// same material and the entire environment costs one material regardless of
    /// how many props are standing in it. That is the property that makes a
    /// forest affordable on an A33.
    /// </para>
    ///
    /// <para>
    /// The pack is authored for hex tiles, so several buildings ship sitting on a
    /// hexagonal base slab. <see cref="Report"/> measures every model so those can
    /// be identified and either trimmed or sunk, rather than discovering a floating
    /// hexagon under a farmhouse at play time.
    /// </para>
    /// </summary>
    public static class EmberHexAssets
    {
        private const string SrcDir = "Assets/Art/Environments/Hexagon";
        private const string OutDir = "Assets/Resources/Props/Zone";
        private const string MatPath = "Assets/Prefabs/Mat_HexAtlas.mat";
        private const string TexPath = SrcDir + "/hexagons_medieval.png";

        /// <summary>Creates (or refreshes) the one material the whole pack shares.</summary>
        public static Material EnsureMaterial()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Surface"));
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            mat.shader = Shader.Find("Emberline/Surface");
            mat.mainTexture = tex;
            // The hexagon atlas is a bright gradient ramp authored for a daylit
            // strategy map. Left at white it makes a well roof the brightest thing
            // in the valley, so it is tinted to sit with the repainted Kenney kits.
            mat.SetColor("_Color", new Color(0.44f, 0.48f, 0.54f));
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0f);
            // The atlas is a flat gradient ramp with no detail of its own, so the
            // procedural wear is doing all the surface breakup here.
            mat.SetFloat("_WearStrength", 0.28f);
            mat.SetFloat("_WearScale", 2.2f);
            mat.SetFloat("_RimStrength", 0.18f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        [MenuItem("Emberline/Import Zone Assets")]
        public static void Import()
        {
            // The atlas is a colour ramp: filtering it smears neighbouring ramp
            // entries across a face, so it is point-sampled like the terrain palette.
            var imp = AssetImporter.GetAtPath(TexPath) as TextureImporter;
            if (imp != null)
            {
                imp.filterMode = FilterMode.Point;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;      // props are seen at range; mips stop shimmer
                imp.maxTextureSize = 512;      // 1024 is more ramp than a phone needs
                imp.SaveAndReimport();
            }

            var mat = EnsureMaterial();
            Directory.CreateDirectory(OutDir);

            var made = 0;
            foreach (var path in Directory.GetFiles(SrcDir, "*.fbx").OrderBy(p => p))
            {
                var name = Path.GetFileNameWithoutExtension(path);

                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi != null)
                {
                    mi.globalScale = 1f;
                    mi.importAnimation = false;
                    mi.importCameras = false;
                    mi.importLights = false;
                    mi.materialImportMode = ModelImporterMaterialImportMode.None;
                    // Props are static scenery; a rig on each one costs import time
                    // and gains nothing.
                    mi.animationType = ModelImporterAnimationType.None;
                    mi.meshCompression = ModelImporterMeshCompression.Medium;
                    mi.isReadable = false;
                    mi.SaveAndReimport();
                }

                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                go.name = name;

                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                    // Every prop is scenery that never moves: batching and
                    // instancing both want this flag.
                    r.gameObject.isStatic = true;
                }

                PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{name}.prefab");
                Object.DestroyImmediate(go);
                made++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Zone] imported {made} prefabs into {OutDir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Measures every imported prefab: size, triangles, and how far its
        /// footprint spreads relative to its height. A wide, flat footprint with a
        /// bottom near y=0 is the signature of an attached hex base.
        /// </summary>
        [MenuItem("Emberline/Report Zone Assets")]
        public static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"prefab",-30} {"w",6} {"h",6} {"d",6} {"minY",7} {"tris",7}  hexbase?");

            foreach (var path in Directory.GetFiles(OutDir, "*.prefab").OrderBy(p => p))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
                var any = false;
                var b = new Bounds();
                var tris = 0;
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

                if (any)
                {
                    // A hex base is wide, flat, and bottoms out right at the origin.
                    var flatWide = b.size.x > 1.6f && b.size.z > 1.6f;
                    var sitsOnZero = Mathf.Abs(b.min.y) < 0.15f;
                    var squat = b.size.y < Mathf.Max(b.size.x, b.size.z) * 1.3f;
                    var hex = flatWide && sitsOnZero && squat ? "LIKELY" : "";
                    sb.AppendLine($"{Path.GetFileNameWithoutExtension(path),-30} " +
                                  $"{b.size.x,6:F2} {b.size.y,6:F2} {b.size.z,6:F2} " +
                                  $"{b.min.y,7:F2} {tris,7}  {hex}");
                }
                Object.DestroyImmediate(inst);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/zone_assets.txt", sb.ToString());
            Debug.Log("[Zone] asset report written to Logs/zone_assets.txt\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
