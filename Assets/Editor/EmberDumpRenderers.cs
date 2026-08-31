using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    public static class EmberDumpRenderers
    {
        [MenuItem("Emberline/Dump Renderers")]
        public static void Dump()
        {
            string[] fbx =
            {
                "Assets/Art/Characters/Adventurers/RogueHooded.fbx",
                "Assets/Art/Characters/Adventurers/Rogue.fbx",
                "Assets/Art/Characters/Adventurers/Barbarian.fbx",
                "Assets/Art/Characters/Skeletons/Skeleton_Minion.fbx",
            };
            var sb = new System.Text.StringBuilder("[Renderers]\n");
            foreach (var path in fbx)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                sb.AppendLine("== " + path);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    sb.AppendLine($"  {(r is SkinnedMeshRenderer ? "SKIN" : "MESH")} {Path(r.transform)}");
            }
            System.IO.File.WriteAllText("Logs/renderers.txt", sb.ToString());
            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static string Path(Transform t) =>
            t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
