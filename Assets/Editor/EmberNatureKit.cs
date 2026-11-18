using System.Collections.Generic;
using System.IO;
using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Japanese trees from Quaternius' Stylized Nature MegaKit (CC0, standard
    /// tier, <c>Assets/Art/Environments/QuaterniusNature</c>): straight pines,
    /// twisted pines that stand in for kuromatsu by shrines and roads, and the
    /// broadleaf recoloured into momiji (maple); the twisted tree is the sakura. Prefabs land as
    /// <c>Resources/Props/Zone/qn_*</c>.
    ///
    /// <para>
    /// Each prefab is normalised to 1.2 units tall, the height of Kenney's
    /// <c>tree_pineTall</c>, so the existing scatter scale ranges (4.5–7.5) give
    /// the same 5–9 m trees and the new set drops into the old tables.
    /// Leaves use the Surface shader's opt-in alpha test and are two-sided;
    /// every texture is graded to the night palette (highlights under 0.62).
    /// </para>
    /// </summary>
    public static class EmberNatureKit
    {
        private const string Src = "Assets/Art/Environments/QuaterniusNature";
        private const string PrefabDir = "Assets/Resources/Props/Zone";
        private const string MatDir = "Assets/Prefabs/EnvMaterials";
        private const float TargetHeight = 1.2f;

        [MenuItem("Emberline/Build Nature Kit")]
        public static void Build()
        {
            // The import rule keeps these models' material slots; a model imported
            // before that rule existed has none, so reimport them all first.
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { $"{Src}/Models" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            var tex = $"{Src}/Textures";
            var barkNormal = Mat("qn_bark", Graded($"{tex}/Bark_NormalTree.png", 0f, 1f, 1f), leaf: false);
            var barkTwisted = Mat("qn_bark_twisted", Graded($"{tex}/Bark_TwistedTree.png", 0f, 1f, 1f), leaf: false);
            var pine = Mat("qn_leaf_pine", Graded($"{tex}/Leaf_Pine_C.png", 0f, 0.9f, 0.9f), leaf: true);
            // The twisted tree ships as a cherry: kept pink it is the sakura; turned
            // a deep needle green, the same gnarled shape is the kuromatsu.
            var sakura = Mat("qn_leaf_sakura", Graded($"{tex}/Leaves_TwistedTree_C.png", 0.95f, 0.75f, 1.15f, "Sakura"), leaf: true);
            var matsu = Mat("qn_leaf_matsu", Graded($"{tex}/Leaves_TwistedTree_C.png", 0.30f, 0.8f, 0.6f, "Matsu"), leaf: true);
            var broad = Mat("qn_leaf_broad", Graded($"{tex}/Leaves_NormalTree_C.png", 0f, 1f, 1f), leaf: true);
            // Momiji: the broadleaf's green turned to a deep red.
            var maple = Mat("qn_leaf_maple", Graded($"{tex}/Leaves_NormalTree_C.png", 0.02f, 1.25f, 1.0f, "Maple"), leaf: true);

            var n = 0;
            for (var i = 1; i <= 5; i++)
            {
                n += Make($"qn_pine_{i}", $"Pine_{i}", barkNormal, pine);
                n += Make($"qn_matsu_{i}", $"TwistedTree_{i}", barkTwisted, matsu);
                n += Make($"qn_broad_{i}", $"CommonTree_{i}", barkNormal, broad);
                n += Make($"qn_maple_{i}", $"CommonTree_{i}", barkNormal, maple);
                n += Make($"qn_sakura_{i}", $"TwistedTree_{i}", barkTwisted, sakura);
            }
            n += Make("qn_bush", "Bush_Common", barkNormal, matsu, height: 0.35f);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NatureKit] {n} prefabs written to {PrefabDir}");
            if (Application.isBatchMode && !SessionState.GetBool("emberline.setup", false)) EditorApplication.Exit(0);
        }

        private static int Make(string name, string model, Material bark, Material leaf, float height = TargetHeight)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Src}/Models/{model}.fbx");
            if (src == null) { Debug.LogWarning($"[NatureKit] missing {model}"); return 0; }
            var go = (GameObject)Object.Instantiate(src);
            go.name = name;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            var bounds = new Bounds();
            var first = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var slots = r.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var key = slots[i] != null ? slots[i].name.ToLowerInvariant() : "";
                    slots[i] = key.Contains("leaf") || key.Contains("leaves") ? leaf : bark;
                }
                r.sharedMaterials = slots;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }

            var tris = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            Debug.Log($"[NatureKit] {name}: {tris} tris");

            // Normalise: base at the origin, the tree's height to the Kenney convention.
            var holder = new GameObject(name);
            go.transform.SetParent(holder.transform, true);
            var k = height / Mathf.Max(0.01f, bounds.size.y);
            go.transform.localScale *= k;
            go.transform.localPosition = new Vector3(-bounds.center.x * k, -bounds.min.y * k, -bounds.center.z * k);
            holder.isStatic = true;
            foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            PrefabUtility.SaveAsPrefabAsset(holder, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(holder);
            return 1;
        }

        private static Material Mat(string name, Texture2D albedo, bool leaf)
        {
            var path = $"{MatDir}/Env_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(SurfaceKit.SurfaceShader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = SurfaceKit.SurfaceShader;
            SurfaceKit.Apply(mat, leaf ? Surface.Foliage : Surface.Wood, Color.white);
            mat.mainTexture = albedo;
            // No instancing: an instanced draw renders every tree at one Mesh LOD
            // level, and per-tree LOD is the reason these trees can be afforded.
            mat.enableInstancing = false;
            if (leaf)
            {
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_AlphaTest", 1f);
                mat.SetFloat("_Cutoff", 0.45f);
                mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
            else
            {
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_AlphaTest", 0f);
                mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// A night-palette copy of a texture, optionally re-hued: <paramref name="hue"/>
        /// replaces the hue when non-zero, saturation and value are scaled, alpha kept.
        /// </summary>
        private static Texture2D Graded(string srcPath, float hue, float sat, float val, string suffix = "Ember")
        {
            if (AssetImporter.GetAtPath(srcPath) is TextureImporter si && !si.isReadable)
            {
                si.isReadable = true;
                si.textureCompression = TextureImporterCompression.Uncompressed;
                si.SaveAndReimport();
            }
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (src == null) { Debug.LogWarning($"[NatureKit] missing {srcPath}"); return null; }

            var px = src.GetPixels();
            for (var i = 0; i < px.Length; i++)
            {
                var c = px[i];
                Color.RGBToHSV(c, out var h, out var s, out var v);
                if (hue > 0f) h = hue;
                s = Mathf.Clamp01(s * sat * 0.85f);   // the shared night desaturation
                v = Mathf.Clamp01(v * val);
                var o = Color.HSVToRGB(h, s, v);
                var top = Mathf.Max(o.r, Mathf.Max(o.g, o.b));
                if (top > 0.62f) o *= Mathf.Lerp(1f, 0.62f / top, 0.85f);
                o.a = c.a;
                px[i] = o;
            }
            var outTex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true);
            outTex.SetPixels(px);
            outTex.Apply();
            var outPath = srcPath.Replace(".png", $"_{suffix}.png");
            File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(outPath);
            if (AssetImporter.GetAtPath(outPath) is TextureImporter oi)
            {
                oi.alphaIsTransparency = true;
                oi.mipmapEnabled = true;
                oi.maxTextureSize = 1024;
                oi.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }
    }
}
