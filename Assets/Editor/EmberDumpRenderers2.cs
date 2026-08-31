using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    public static class EmberDumpRenderers2
    {
        [MenuItem("Emberline/Dump Renderers 2")]
        public static void Dump()
        {
            string[] fbx =
            {
                "Assets/Art/Characters/Skeletons/Skeleton_Warrior.fbx",
                "Assets/Art/Characters/Adventurers/Knight.fbx",
                "Assets/Art/Characters/Adventurers/Mage.fbx",
            };
            var sb = new System.Text.StringBuilder("[Renderers2]\n");
            foreach (var path in fbx)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) { sb.AppendLine("MISSING " + path); continue; }
                sb.AppendLine("== " + path);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    sb.AppendLine($"  {(r is SkinnedMeshRenderer ? "SKIN" : "MESH")} {r.gameObject.name}");
            }
            System.IO.File.WriteAllText("Logs/renderers2.txt", sb.ToString());
            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
