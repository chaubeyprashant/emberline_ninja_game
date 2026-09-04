#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emberline.Core;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Builds every enemy spec and reports what it actually renders: body, tint,
    /// and whether each material slot resolved to a texture. A slot with no
    /// texture is the failure that makes a character read as bare skin.
    /// </summary>
    public static class EmberCastCheck
    {
        public static void Run()
        {
            var specs = new (string, EmberCharacterFactory.Spec)[]
            {
                ("Renzo", EmberCharacterFactory.PlayerSpec()),
                ("Bandit", EmberCharacterFactory.Bandit()),
                ("Archer", EmberCharacterFactory.Archer()),
                ("Goro", EmberCharacterFactory.Goro()),
                ("Shade", EmberCharacterFactory.Shade()),
                ("Kagachi", EmberCharacterFactory.Kagachi()),
                ("Jin", EmberCharacterFactory.Jin()),
                ("RaiderAxe", EmberCharacterFactory.RaiderAxe()),
                ("PikeGuard", EmberCharacterFactory.PikeGuard()),
                ("Bomber", EmberCharacterFactory.Bomber()),
                ("Assassin", EmberCharacterFactory.Assassin()),
                ("Samurai", EmberCharacterFactory.Samurai()),
                ("RogueNinja", EmberCharacterFactory.RogueNinja()),
                ("EliteWarrior", EmberCharacterFactory.EliteWarrior()),
            };
            var bad = 0;
            foreach (var (label, spec) in specs)
            {
                var root = new GameObject(label);
                if (!EmberCharacterFactory.Build(root, spec)) { Debug.Log($"[CAST] {label} BUILD FAILED"); bad++; continue; }
                // Props, the slash trail and the lantern glow are deliberately
                // untextured (Glow/Prop materials), so they are not body slots.
                var rends = root.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.gameObject.activeInHierarchy
                                && !r.name.StartsWith("Prop_")
                                && r is not TrailRenderer
                                && !r.name.Contains("Lantern")
                                && !r.name.Contains("Trail")).ToArray();
                var missing = rends.SelectMany(r => r.sharedMaterials)
                    .Where(m => m == null || !m.HasProperty("_MainTex") || m.GetTexture("_MainTex") == null)
                    .Count();
                var tex = rends.SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                    .Select(m => m.GetTexture("_MainTex").name).Distinct().ToArray();
                var body = System.IO.Path.GetFileNameWithoutExtension(spec.fbx);
                if (missing > 0 && !spec.ghost) bad++;
                Debug.Log($"[CAST] {label,-13} body={body,-17} h={spec.height:F2} " +
                          $"tint=({spec.tint.r:F2},{spec.tint.g:F2},{spec.tint.b:F2}) " +
                          $"rend={rends.Length,2} noTex={missing} tex=[{string.Join("|", tex)}]");
                Object.DestroyImmediate(root);
            }
            Debug.Log(bad == 0 ? "[CAST] ALL TEXTURED" : $"[CAST] {bad} PROBLEM(S)");
            EditorApplication.Exit(0);
        }
    }
}
#endif
