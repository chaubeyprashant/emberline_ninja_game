using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Reimports the models whose import rule turns on Unity 6 Mesh LOD (Mixamo
    /// bodies, Quaternius trees) so the generated levels land in the Library, and
    /// logs the triangle count of each one's full and coarsest level.
    /// </summary>
    public static class EmberLodAssets
    {
        [MenuItem("Emberline/Reimport Mesh LOD Models")]
        public static void Run()
        {
            var n = 0;
            foreach (var root in new[] { "Assets/Art/Characters/Mixamo", "Assets/Art/Environments/QuaterniusNature" })
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { root }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/Anims/")) continue;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (o is Mesh m && m.lodCount > 1)
                            Debug.Log($"[LODs] {path} :: {m.name} levels={m.lodCount} full={m.triangles.Length / 3}");
                    n++;
                }
            Debug.Log($"[LODs] reimported {n} models");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
