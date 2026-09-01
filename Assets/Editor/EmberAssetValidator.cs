using System.Linq;
using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Checks a candidate character FBX against docs/ASSET_SPECIFICATIONS.md
    /// before anyone spends time integrating it. Reports every failure at once
    /// rather than stopping at the first, because an artist iterating on a model
    /// wants the whole list.
    ///
    /// Select an FBX in the Project window and run Emberline/Validate Character,
    /// or call ValidatePath from a batch script.
    /// </summary>
    public static class EmberAssetValidator
    {
        /// <summary>Budgets from the spec document, by character class.</summary>
        public enum Class { Player, Mook, Boss }

        private static int TriBudget(Class c) => c switch
        {
            Class.Player => 24_000,
            Class.Boss => 18_000,
            _ => 13_000,
        };

        private static int TextureBudget(Class c) => c == Class.Boss ? 2048 : 1024;

        private const int MaxBones = 52;
        private const int MaxMaterials = 2;

        [MenuItem("Emberline/Validate Character")]
        private static void ValidateSelection()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".fbx"))
            {
                Debug.LogError("[Validate] Select a .fbx in the Project window.");
                return;
            }
            ValidatePath(path, Class.Mook);
        }

        /// <summary>Returns true when the model satisfies every requirement.</summary>
        public static bool ValidatePath(string fbxPath, Class characterClass)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (go == null)
            {
                Debug.LogError($"[Validate] Cannot load {fbxPath}");
                return false;
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(fbxPath);
            var fails = 0;
            void Require(bool ok, string what)
            {
                if (ok) Debug.Log($"[Validate] pass  {name}: {what}");
                else { Debug.LogError($"[Validate] FAIL  {name}: {what}"); fails++; }
            }

            // ---- geometry ----------------------------------------------------
            var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(skinned.Length > 0, "has a skinned mesh");

            var tris = 0;
            foreach (var smr in skinned)
                if (smr.sharedMesh != null) tris += smr.sharedMesh.triangles.Length / 3;
            Require(tris > 0 && tris <= TriBudget(characterClass),
                $"triangles {tris:N0} within budget {TriBudget(characterClass):N0}");

            var bones = skinned.Length > 0 && skinned[0].bones != null ? skinned[0].bones.Length : 0;
            Require(bones > 0 && bones <= MaxBones, $"bones {bones} within {MaxBones}");

            var slots = skinned.Sum(s => s.sharedMaterials?.Length ?? 0);
            Require(slots <= MaxMaterials, $"material slots {slots} within {MaxMaterials}");

            // ---- rig ---------------------------------------------------------
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            Require(importer != null && importer.animationType == ModelImporterAnimationType.Human,
                "rig is Humanoid");

            // ---- sockets -----------------------------------------------------
            var all = go.GetComponentsInChildren<Transform>(true);
            bool HasBone(params string[] options) => options.Any(o =>
                all.Any(t => t.name.ToLowerInvariant().Contains(o)));
            Require(HasBone("hand.r", "handr", "handslot.r"), "right-hand socket present");
            Require(HasBone("hand.l", "handl", "handslot.l"), "left-hand socket present");

            // ---- orientation and pivot ---------------------------------------
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var seeded = false;
            foreach (var smr in skinned)
            {
                if (smr.sharedMesh == null) continue;
                if (!seeded) { bounds = smr.sharedMesh.bounds; seeded = true; }
                else bounds.Encapsulate(smr.sharedMesh.bounds);
            }
            Require(seeded && bounds.size.y > bounds.size.x && bounds.size.y > bounds.size.z,
                "taller than wide (Y-up)");
            Require(seeded && Mathf.Abs(bounds.min.y) < bounds.size.y * 0.15f,
                $"origin at the feet (min.y {bounds.min.y:F2} of height {bounds.size.y:F2})");

            // ---- clips -------------------------------------------------------
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"))
                .Select(c => c.name).ToArray();
            var poses = System.Enum.GetValues(typeof(RigPose)).Length;
            Require(clips.Length >= poses,
                $"carries at least {poses} clips for the pose table (found {clips.Length})");

            // ---- textures ----------------------------------------------------
            var budget = TextureBudget(characterClass);
            var bad = 0;
            foreach (var dep in AssetDatabase.GetDependencies(fbxPath, true))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dep);
                if (tex == null) continue;
                if (Mathf.Max(tex.width, tex.height) > budget) bad++;
            }
            Require(bad == 0, $"no texture exceeds {budget}px ({bad} over)");

            Debug.Log(fails == 0
                ? $"[Validate] {name}: PASSES the spec ({tris:N0} tris, {bones} bones)"
                : $"[Validate] {name}: {fails} requirement(s) not met");
            return fails == 0;
        }
    }
}
