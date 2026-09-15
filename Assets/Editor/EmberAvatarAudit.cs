#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Every Mixamo body's avatar. A body imported as Humanoid whose avatar is not
    /// human cannot play the shared humanoid clips and stands in its bind T-pose
    /// forever — that is how Renzo's father ended up "standing like a cross".
    /// <see cref="Repair"/> reimports those bodies so EmberArtImport can fix their
    /// bone map, then reports every body again.
    /// </summary>
    public static class EmberAvatarAudit
    {
        private static string[] Bodies() =>
            Directory.GetFiles("Assets/Art/Characters/Mixamo", "*.fbx").OrderBy(p => p).ToArray();

        private static Avatar AvatarOf(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

        [MenuItem("Emberline/Audit Mixamo Avatars")]
        public static void Run()
        {
            Report();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Emberline/Repair Mixamo Avatars")]
        public static void Repair()
        {
            foreach (var path in Bodies())
            {
                var avatar = AvatarOf(path);
                if (avatar != null && avatar.isHuman) continue;
                Debug.Log($"[AVATAR] reimporting {Path.GetFileNameWithoutExtension(path)} (not human)");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.SaveAssets();
            var bad = Report();
            Debug.Log(bad == 0 ? "[AVATAR] ALL HUMAN" : $"[AVATAR] {bad} STILL NOT HUMAN");
            if (Application.isBatchMode) EditorApplication.Exit(bad == 0 ? 0 : 1);
        }

        private static int Report()
        {
            var bad = 0;
            foreach (var path in Bodies())
            {
                var avatar = AvatarOf(path);
                var human = avatar != null && avatar.isHuman;
                if (!human) bad++;
                Debug.Log($"[AVATAR] {Path.GetFileNameWithoutExtension(path)}: valid={(avatar && avatar.isValid)} human={human}");
            }
            return bad;
        }
    }
}
#endif
