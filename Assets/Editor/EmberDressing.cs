using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Turns the KayKit dungeon props into runtime-loadable prefabs under
    /// Resources/Props/Dressing, so a mission can place its own environmental
    /// storytelling when it starts rather than every arena carrying every prop.
    ///
    /// The props themselves already exist and are already used by the scene
    /// builder; this only makes them reachable from `Resources.Load` at runtime
    /// and pins the shared dungeon material so a dressed mission does not add a
    /// draw-call-per-prop material.
    /// </summary>
    public static class EmberDressing
    {
        /// <summary>Props a mission may place. Names match the source FBX files.</summary>
        public static readonly string[] Props =
        {
            "rubble_large", "rubble_half", "crates_stacked", "box_large", "box_small",
            "barrel_large", "barrel_small", "keg", "chest", "table_small",
            "banner_red", "banner_thin_red", "column", "torch_lit",
        };

        private const string OutDir = "Assets/Resources/Props/Dressing";

        [MenuItem("Emberline/Build Dressing Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mat_DungeonAtlas.mat");
            var made = 0;

            foreach (var name in Props)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Art/Environments/Dungeon/{name}.fbx");
                if (src == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                go.name = name;
                if (mat != null)
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        var mats = r.sharedMaterials;
                        for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                        r.sharedMaterials = mats;
                    }

                PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{name}.prefab");
                Object.DestroyImmediate(go);
                made++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Emberline] Dressing prefabs: {made}/{Props.Length} under Resources/Props/Dressing");
        }
    }
}
