using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Imports the Kenney environment kits and, crucially, <b>repaints them</b>.
    ///
    /// <para>
    /// The kits arrive in Kenney's daylight palette: 0.17,0.85,0.72 grass and
    /// 0.89,0.51,0.34 dirt — cheerful, saturated, and completely wrong next to a
    /// cast lit by one cold key light at night. Dropping them in unmodified is
    /// exactly the "high-quality character plus low-quality cartoon trees" failure
    /// the art direction cannot afford, so every source colour is mapped to an
    /// Emberline one on the way in.
    /// </para>
    ///
    /// <para>
    /// The kits come in two flavours and need different handling:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Nature Kit</b> ships no texture at all — its models carry named
    ///   flat-colour materials (<c>grass</c>, <c>woodBarkDark</c>, <c>stone</c>).
    ///   Those names are the mapping key, so the whole forest resolves to about a
    ///   dozen shared materials in the game's own palette.</item>
    ///   <item><b>Survival / Town / Castle</b> each ship one <c>colormap.png</c>
    ///   atlas, so each becomes a single shared material, tinted and desaturated
    ///   toward night.</item>
    /// </list>
    /// </summary>
    public static class EmberKenneyAssets
    {
        private const string SrcRoot = "Assets/Art/Environments/Kenney";
        private const string OutDir = "Assets/Resources/Props/Zone";
        private const string MatDir = "Assets/Prefabs/EnvMaterials";

        private static readonly string[] AtlasKits = { "survival", "town", "castle" };

        /// <summary>
        /// Kenney material name to Emberline colour. Keyed by the names found in
        /// the Nature Kit's .mtl files, which the FBX materials inherit.
        /// </summary>
        private static readonly Dictionary<string, Color> Repaint = new()
        {
            // Foliage: cool, dark, desaturated. Matches the terrain grass palette
            // in EmberTerrain so trees do not float on a differently-coloured field.
            ["grass"]        = new Color(0.185f, 0.270f, 0.205f),
            ["leafsGreen"]   = new Color(0.165f, 0.245f, 0.190f),
            ["leafsDark"]    = new Color(0.120f, 0.185f, 0.160f),

            // Bark and timber: brown pulled toward grey so it sits in the night air.
            ["woodBark"]     = new Color(0.215f, 0.170f, 0.140f),
            ["woodBarkDark"] = new Color(0.160f, 0.125f, 0.105f),
            ["woodDark"]     = new Color(0.150f, 0.120f, 0.100f),
            ["wood"]         = new Color(0.275f, 0.205f, 0.150f),
            ["woodInner"]    = new Color(0.350f, 0.280f, 0.205f),

            // Ground, matched to the terrain palette's Dirt and Rock cells.
            ["dirt"]         = new Color(0.245f, 0.215f, 0.170f),
            ["dirtDark"]     = new Color(0.185f, 0.160f, 0.130f),
            ["stone"]        = new Color(0.215f, 0.225f, 0.245f),
            ["stoneDark"]    = new Color(0.170f, 0.180f, 0.200f),

            // Accents. Warm colours are the only saturated thing in Emberline and
            // they belong to lantern light, so these stay muted and rare.
            ["colorRed"]     = new Color(0.400f, 0.155f, 0.140f),
            ["colorYellow"]  = new Color(0.600f, 0.410f, 0.160f),
            ["colorPurple"]  = new Color(0.255f, 0.215f, 0.330f),
            ["colorTan"]     = new Color(0.390f, 0.320f, 0.235f),
            ["_defaultMat"]  = new Color(0.245f, 0.245f, 0.255f),
        };

        /// <summary>
        /// How far the atlas kits are pulled toward their own luminance. Kenney's
        /// colormaps carry postbox-red roofs and mint-green awnings; a plain tint
        /// multiply darkens those but leaves them just as saturated, so the atlas
        /// is genuinely repainted on import instead.
        /// </summary>
        private const float Desaturate = 0.70f;

        /// <summary>Cool multiplier applied after desaturation.</summary>
        private static readonly Color AtlasTint = new(0.46f, 0.50f, 0.57f);

        /// <summary>
        /// Warm accents survive the wash. Emberline's palette allows exactly one
        /// saturated colour — lantern orange — so hues near it keep more of their
        /// chroma and everything else goes to grey.
        /// </summary>
        private static Color Repaint32(Color c)
        {
            Color.RGBToHSV(c, out var hue, out var sat, out var val);
            // Hue 0.03-0.13 is the amber/orange band.
            var warm = hue > 0.02f && hue < 0.14f ? 1f : 0f;
            var keep = Mathf.Lerp(1f - Desaturate, 1f - Desaturate * 0.45f, warm);

            var lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            var outc = new Color(
                Mathf.Lerp(lum, c.r, keep),
                Mathf.Lerp(lum, c.g, keep),
                Mathf.Lerp(lum, c.b, keep), c.a);

            // Compress the highlights. Desaturating alone keeps a bright roof
            // bright, and the first pass turned Kenney's red tiles into what read
            // as snow — the lightest thing in a night valley, which is exactly
            // where the eye should not be going.
            var ceiling = 0.62f;
            var v = Mathf.Max(outc.r, Mathf.Max(outc.g, outc.b));
            if (v > ceiling)
            {
                var k = Mathf.Lerp(1f, ceiling / Mathf.Max(v, 0.001f), 0.85f);
                outc = new Color(outc.r * k, outc.g * k, outc.b * k, c.a);
            }

            return new Color(outc.r * AtlasTint.r, outc.g * AtlasTint.g,
                             outc.b * AtlasTint.b, c.a);
        }

        /// <summary>
        /// Writes an Emberline-palette copy of a kit's colormap and returns it.
        /// The original is left untouched so the repaint can be re-run.
        /// </summary>
        private static Texture2D RepaintAtlas(string kit, string srcPath)
        {
            var outPath = $"{SrcRoot}/{kit}/colormap_{kit}_ember.png";

            var srcImp = AssetImporter.GetAtPath(srcPath) as TextureImporter;
            if (srcImp != null && (!srcImp.isReadable || srcImp.textureCompression != TextureImporterCompression.Uncompressed))
            {
                srcImp.isReadable = true;
                srcImp.textureCompression = TextureImporterCompression.Uncompressed;
                srcImp.maxTextureSize = 512;
                srcImp.SaveAndReimport();
            }

            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (src == null) return null;

            var px = src.GetPixels();
            for (var i = 0; i < px.Length; i++) px[i] = Repaint32(px[i]);

            var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            dst.SetPixels(px);
            dst.Apply();
            File.WriteAllBytes(outPath, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(outPath);

            if (AssetImporter.GetAtPath(outPath) is TextureImporter oi)
            {
                oi.filterMode = FilterMode.Point;
                oi.mipmapEnabled = true;
                oi.maxTextureSize = 512;
                oi.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        [MenuItem("Emberline/Import Kenney Zone Kits")]
        public static void Import()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(MatDir);

            var made = 0;
            made += ImportNature();
            foreach (var kit in AtlasKits) made += ImportAtlasKit(kit);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Zone] Kenney import: {made} prefabs into {OutDir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------ nature

        private static int ImportNature()
        {
            var dir = $"{SrcRoot}/nature";
            if (!Directory.Exists(dir)) return 0;

            // One shared material per repaint key, created once and reused by every
            // model that references that Kenney material name.
            var shared = new Dictionary<string, Material>();
            foreach (var kv in Repaint)
                shared[kv.Key] = SolidMaterial($"Env_{kv.Key}", kv.Value);

            var made = 0;
            foreach (var path in Directory.GetFiles(dir, "*.fbx").OrderBy(p => p))
            {
                ConfigureModel(path, keepMaterialNames: true);
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                go.name = Path.GetFileNameWithoutExtension(path);

                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                    {
                        // The importer names the slot after the source material, so
                        // that name is the key even though the material itself is
                        // discarded.
                        var key = mats[i] != null ? StripInstance(mats[i].name) : "_defaultMat";
                        mats[i] = shared.TryGetValue(key, out var m) ? m : shared["_defaultMat"];
                    }
                    r.sharedMaterials = mats;
                    r.gameObject.isStatic = true;
                }

                PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{go.name}.prefab");
                Object.DestroyImmediate(go);
                made++;
            }
            return made;
        }

        /// <summary>Unity appends " (Instance)" and " 1" to imported material names.</summary>
        private static string StripInstance(string n)
        {
            var i = n.IndexOf(" (", System.StringComparison.Ordinal);
            if (i > 0) n = n[..i];
            return n.Trim();
        }

        // ------------------------------------------------------- textured kits

        private static int ImportAtlasKit(string kit)
        {
            var dir = $"{SrcRoot}/{kit}";
            if (!Directory.Exists(dir)) return 0;

            var texPath = $"{dir}/colormap_{kit}.png";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null)
            {
                // Kenney colormaps are colour-swatch grids: bilinear filtering
                // bleeds one swatch into the next along a UV seam.
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 512;
                imp.SaveAndReimport();
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Env_{kit}.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Surface"));
                AssetDatabase.CreateAsset(mat, $"{MatDir}/Env_{kit}.mat");
            }
            mat.shader = Shader.Find("Emberline/Surface");
            mat.mainTexture = RepaintAtlas(kit, texPath);
            mat.SetColor("_Color", Color.white);   // the repaint already carries the tint
            mat.SetFloat("_Smoothness", 0.10f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_WearStrength", 0.26f);
            mat.SetFloat("_WearScale", 2.0f);
            mat.SetFloat("_RimStrength", 0.16f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);

            var made = 0;
            foreach (var path in Directory.GetFiles(dir, "*.fbx").OrderBy(p => p))
            {
                ConfigureModel(path, keepMaterialNames: false);
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                // Kit-prefixed so a "wall" from the town kit and a "wall" from the
                // castle kit can both exist under one Resources folder.
                go.name = $"{kit}_{Path.GetFileNameWithoutExtension(path)}";

                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                    r.gameObject.isStatic = true;
                }

                PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{go.name}.prefab");
                Object.DestroyImmediate(go);
                made++;
            }
            return made;
        }

        private static Material SolidMaterial(string name, Color c)
        {
            var path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Surface"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Emberline/Surface");
            mat.mainTexture = null;          // flat colour: the shader falls back to white
            mat.SetColor("_Color", c);
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            // Flat colour with no texture is exactly what the procedural wear term
            // exists for, so it runs a little stronger here than on atlas kits.
            mat.SetFloat("_WearStrength", 0.34f);
            mat.SetFloat("_WearScale", 2.6f);
            mat.SetFloat("_RimStrength", 0.14f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureModel(string path, bool keepMaterialNames)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) return;
            mi.globalScale = 1f;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.isReadable = false;
            // Nature needs the slot names to survive so they can be used as repaint
            // keys; the atlas kits get one material regardless, so importing none
            // is faster and leaves no orphan material assets behind.
            mi.materialImportMode = keepMaterialNames
                ? ModelImporterMaterialImportMode.ImportStandard
                : ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
        }

        // ------------------------------------------------------------ report

        [MenuItem("Emberline/Report Kenney Zone Kits")]
        public static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"prefab",-34} {"w",6} {"h",6} {"d",6} {"minY",7} {"tris",6} {"mats",4}");

            foreach (var path in Directory.GetFiles(OutDir, "*.prefab").OrderBy(p => p))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);

                var any = false; var b = new Bounds(); var tris = 0;
                var mats = new HashSet<string>();
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                    foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m.name);
                }
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

                if (any)
                    sb.AppendLine($"{Path.GetFileNameWithoutExtension(path),-34} " +
                                  $"{b.size.x,6:F2} {b.size.y,6:F2} {b.size.z,6:F2} " +
                                  $"{b.min.y,7:F2} {tris,6} {mats.Count,4}");
                Object.DestroyImmediate(inst);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/zone_kenney.txt", sb.ToString());
            Debug.Log("[Zone] Kenney report written to Logs/zone_kenney.txt");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
