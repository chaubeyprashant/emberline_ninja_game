using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Makes TextMeshPro usable in batch. Unity 6 bundles TMP inside com.unity.ugui
    /// but ships the essential resources (settings, shaders, default font) as a
    /// .unitypackage that is normally imported through an interactive dialog.
    /// Step 1 imports it; step 2 (a separate run, after the import has settled)
    /// generates the project's font assets from the fonts already in Resources.
    /// </summary>
    public static class EmberTmpSetup
    {
        private const string FontDir = "Assets/Resources/Art/Fonts";
        private const string OutDir = "Assets/Resources/Art/Fonts/TMP";

        [MenuItem("Emberline/TMP/1. Import Essentials")]
        public static void ImportEssentials()
        {
            var pkg = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage",
                SearchOption.AllDirectories).FirstOrDefault();
            if (pkg == null)
            {
                Debug.LogError("[TMP] Essentials package not found under Library/PackageCache");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[TMP] importing " + pkg);
            AssetDatabase.importPackageCompleted += _ =>
            {
                Debug.Log("[TMP] essentials import completed");
                AssetDatabase.SaveAssets();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (_, msg) =>
            {
                Debug.LogError("[TMP] import failed: " + msg);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(pkg, false);
        }

        [MenuItem("Emberline/TMP/2. Build Font Assets")]
        public static void BuildFontAssets()
        {
            Directory.CreateDirectory(OutDir);
            var ok = true;
            // Display face for titles, bold for headings/HUD, medium for body.
            ok &= Make("Shojumaru-Regular", "Emberline-Display", 72, 6, 1024);
            ok &= Make("Rajdhani-Bold", "Emberline-Heading", 64, 6, 1024);
            ok &= Make("Rajdhani-Medium", "Emberline-Body", 56, 5, 1024);

            var settings = TMP_Settings.instance;
            Debug.Log($"[TMP] settings present: {settings != null}");
            Debug.Log($"[TMP] shader 'TextMeshPro/Mobile/Distance Field': " +
                      $"{Shader.Find("TextMeshPro/Mobile/Distance Field") != null}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(ok ? "[TMP] font assets ready" : "[TMP] font asset generation had failures");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Make(string ttf, string assetName, int sampleSize, int padding, int atlas)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{ttf}.ttf");
            if (font == null) { Debug.LogError($"[TMP] missing font {ttf}"); return false; }

            var path = $"{OutDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) { Debug.Log($"[TMP] {assetName} exists"); return true; }

            // Created Dynamic so the glyphs can be added, then locked to Static:
            // a Static asset refuses TryAddCharacters, which is how the first run
            // produced three fonts with zero glyphs. Static at the end is what a
            // mobile build wants — the SDF is baked once, no runtime atlas growth.
            var asset = TMP_FontAsset.CreateFontAsset(font, sampleSize, padding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, atlas, atlas,
                AtlasPopulationMode.Dynamic, true);
            if (asset == null) { Debug.LogError($"[TMP] CreateFontAsset failed for {ttf}"); return false; }

            // Bake the Latin range plus the glyphs the UI actually uses.
            const string extras = "★☆◆◇¤×→←·—–…‘’“”•";
            var chars = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i)) + extras;
            asset.TryAddCharacters(chars, out var missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[TMP] {assetName} lacks glyphs: {missing}");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            if (asset.material != null)
            {
                asset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
                foreach (var t in asset.atlasTextures)
                    if (t != null) { t.name = assetName + " Atlas"; AssetDatabase.AddObjectToAsset(t, asset); }
            Debug.Log($"[TMP] built {assetName} ({asset.characterTable.Count} glyphs)");
            return true;
        }
    }
}
