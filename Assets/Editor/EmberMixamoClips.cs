#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>Reimports the Mixamo animations and reports how they landed.</summary>
    public static class EmberMixamoClips
    {
        public static void Run()
        {
            const string dir = "Assets/Art/Characters/Mixamo/Anims";
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { dir }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            var ok = 0; var bad = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview")).ToArray();
                var names = string.Join("/", clips.Select(c => c.name));
                var human = clips.Any(c => c.isHumanMotion);
                if (clips.Length == 1 && human) ok++; else bad++;
                Debug.Log($"[MXC] {System.IO.Path.GetFileNameWithoutExtension(path),-10} " +
                          $"clips=[{names}] human={human} len={(clips.Length > 0 ? clips[0].length : 0f):F2}s " +
                          $"srcAvatar={(mi.sourceAvatar != null ? mi.sourceAvatar.name : "NONE")}");
            }
            Debug.Log($"[MXC] ok={ok} bad={bad}");
            EditorApplication.Exit(0);
        }
    }
}
#endif
