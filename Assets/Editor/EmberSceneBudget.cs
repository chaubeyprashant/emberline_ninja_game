using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Where a generated scene's triangles and renderers go, by prop family,
    /// read in edit mode before static batching merges anything. Writes
    /// <c>Logs/scene_budget_&lt;scene&gt;.txt</c>: family, instances, triangles each,
    /// total, share, and how many of them sit further than 60 m from the
    /// origin (the fighting floor), which is what distance culling can save.
    /// </summary>
    public static class EmberSceneBudget
    {
        [MenuItem("Emberline/Report Scene Budget")]
        public static void Run()
        {
            foreach (var scene in new[] { "Assets/Scenes/Zone.unity", "Assets/Scenes/Rooftop.unity" })
                Report(scene);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Report(string path)
        {
            EditorSceneManager.OpenScene(path);
            var fam = new Dictionary<string, (int n, long tris, int far, bool shadow)>();
            long total = 0;
            var renderers = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Mesh m = r is SkinnedMeshRenderer sk ? sk.sharedMesh
                    : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                if (m == null) continue;
                long t = 0;
                for (var i = 0; i < m.subMeshCount; i++) t += (long)m.GetIndexCount(i) / 3;
                var key = Regex.Replace(m.name, @"[\d_\- ]+$", "");
                var far = new Vector2(r.bounds.center.x, r.bounds.center.z).magnitude > 60f ? 1 : 0;
                fam.TryGetValue(key, out var v);
                fam[key] = (v.n + 1, v.tris + t, v.far + far, r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off);
                total += t;
                renderers++;
            }
            Directory.CreateDirectory("Logs");
            var name = Path.GetFileNameWithoutExtension(path);
            using var w = new StreamWriter($"Logs/scene_budget_{name}.txt");
            w.WriteLine($"{name}: {renderers} renderers, {total:N0} triangles");
            w.WriteLine($"{"family",-34} {"count",6} {"tris/ea",8} {"total",10} {"share",6} {"far>60m",8} shadows");
            foreach (var kv in fam.OrderByDescending(k => k.Value.tris).Take(40))
                w.WriteLine($"{kv.Key,-34} {kv.Value.n,6} {kv.Value.tris / kv.Value.n,8} {kv.Value.tris,10:N0} {kv.Value.tris * 100.0 / total,5:F1}% {kv.Value.far,8} {(kv.Value.shadow ? "yes" : "no")}");
            Debug.Log($"[Budget] {name}: {renderers} renderers, {total:N0} tris");
        }
    }
}
