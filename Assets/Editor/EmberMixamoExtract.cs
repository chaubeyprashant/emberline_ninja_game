#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Mixamo ships its textures embedded in the FBX. Unity will not surface an
    /// embedded texture as a usable asset on its own, so the model imports with
    /// a white default material. Pull them out once, into a folder per model,
    /// cap them at the 1024² the art spec asks for, and remap the materials.
    /// Runs over every character FBX under the Mixamo folder; idempotent.
    /// </summary>
    public static class EmberMixamoExtract
    {
        private const string Dir = "Assets/Art/Characters/Mixamo";

        public static void Run()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                var model = AssetDatabase.GUIDToAssetPath(g);
                if (model.Contains("/Anims/")) continue;
                Extract(model);
            }
            Debug.Log("[MXT] DONE");
            EditorApplication.Exit(0);
        }

        private static void Extract(string model)
        {
            var stem = System.IO.Path.GetFileNameWithoutExtension(model);
            var texDir = $"{Dir}/Textures/{stem}";
            System.IO.Directory.CreateDirectory(texDir);

            var mi = (ModelImporter)AssetImporter.GetAtPath(model);
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.External;
            mi.SaveAndReimport();

            var already = AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }).Length > 0;
            if (!already)
            {
                mi.ExtractTextures(texDir);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(model, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }

            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                var changed = false;
                if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; changed = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
                var isNormal = p.ToLowerInvariant().Contains("normal");
                if (isNormal && ti.textureType != TextureImporterType.NormalMap)
                { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                if (changed) ti.SaveAndReimport();
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                Debug.Log($"[MXT] {stem} texture {System.IO.Path.GetFileName(p)} {t.width}x{t.height}");
            }

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            var inst = Object.Instantiate(go);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                foreach (var sm in r.sharedMaterials)
                    Debug.Log($"[MXT] {stem} renderer {r.name} mat={(sm != null ? sm.name : "NULL")} " +
                              $"tex={(sm != null && sm.HasProperty("_MainTex") && sm.GetTexture("_MainTex") != null ? sm.GetTexture("_MainTex").name : "NONE")}");
            Object.DestroyImmediate(inst);
        }
    }
}
#endif
