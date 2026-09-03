#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emberline.Core;

namespace Emberline.EditorTools
{
    /// <summary>Measures the built player: world height, scale, and material state.</summary>
    public static class EmberPlayerScale
    {
        public static void Run()
        {
            foreach (var (label, spec) in new (string, EmberCharacterFactory.Spec)[]
                     {
                         ("KayKit", EmberCharacterFactory.Renzo()),
                         ("Mixamo", EmberCharacterFactory.MixamoRenzo()),
                     })
            {
                var root = new GameObject(label);
                if (!EmberCharacterFactory.Build(root, spec)) { Debug.Log($"[PS] {label} BUILD FAILED"); continue; }
                var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var b = new Bounds(root.transform.position, Vector3.zero);
                foreach (var s in smrs) b.Encapsulate(s.bounds);
                var visual = root.GetComponentInChildren<VisualRoot>();
                Debug.Log($"[PS] {label} worldHeight={b.size.y:F3} worldWidth={b.size.x:F3} " +
                          $"centerY={b.center.y:F3} minY={b.min.y:F3} " +
                          $"visualScale={(visual != null ? visual.transform.localScale.y.ToString("F3") : "n/a")} " +
                          $"specHeight={spec.height}");
                foreach (var s in smrs.Take(2))
                {
                    var m = s.sharedMaterial;
                    Debug.Log($"[PS] {label} rend={s.name} shader={(m != null ? m.shader.name : "NULL")} " +
                              $"color={(m != null && m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "n/a")} " +
                              $"tex={(m != null && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null ? m.GetTexture("_MainTex").name : "NONE")}");
                }
                var rig = root.GetComponent<SkeletalRig>();
                Debug.Log($"[PS] {label} rigTint={(rig != null ? rig.tint.ToString() : "no rig")}");
                Object.DestroyImmediate(root);
            }
            Debug.Log("[PS] DONE");
            EditorApplication.Exit(0);
        }
    }
}
#endif
