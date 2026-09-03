#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Reports what a Mixamo model actually costs and whether it satisfies the
    /// rig contract, so the decision to adopt one is made on measurements.
    /// </summary>
    public static class EmberMixamoProbe
    {
        public static void Run()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Characters/Mixamo" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.Contains("/Anims/")) Probe(path);
            }
            Probe("Assets/Art/Characters/Adventurers/RogueHooded.fbx");
            Debug.Log("[MX] DONE");
            EditorApplication.Exit(0);
        }

        private static void Probe(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { Debug.Log("[MX] MISSING " + path); return; }

            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            Debug.Log($"[MX] ==== {System.IO.Path.GetFileName(path)} animationType={mi.animationType}");

            var inst = Object.Instantiate(go);
            int tris = 0, bones = 0, mats = 0;
            var smrs = inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var s in smrs)
            {
                if (s.sharedMesh != null) tris += s.sharedMesh.triangles.Length / 3;
                if (s.bones != null) bones = Mathf.Max(bones, s.bones.Length);
                mats += s.sharedMaterials.Length;
            }
            Debug.Log($"[MX] skinned={smrs.Length} tris={tris} bones={bones} matSlots={mats}");
            foreach (var s in smrs)
                Debug.Log($"[MX]   part {s.name,-40} tris={(s.sharedMesh != null ? s.sharedMesh.triangles.Length / 3 : 0),6} " +
                          $"mats=[{string.Join(",", s.sharedMaterials.Select(m => m != null ? m.name : "null"))}]");
            foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                Debug.Log($"[MX]   static {mr.name} parent={mr.transform.parent?.name}");

            var anim = inst.GetComponentInChildren<Animator>();
            Debug.Log($"[MX] animator={(anim != null)} avatar={(anim != null && anim.avatar != null ? anim.avatar.name : "NONE")} " +
                      $"isHuman={(anim != null && anim.avatar != null && anim.avatar.isHuman)}");

            // The two lookups the pipeline actually performs.
            var rh = FindDeep(inst.transform, "righthand");
            var spine = FindDeep(inst.transform, "spine");
            Debug.Log($"[MX] rightHand={(rh != null ? rh.name : "NOT FOUND")} spine={(spine != null ? spine.name : "NOT FOUND")}");
            if (anim != null && anim.avatar != null && anim.avatar.isHuman)
            {
                var h = anim.GetBoneTransform(HumanBodyBones.RightHand);
                Debug.Log($"[MX] humanoid RightHand={(h != null ? h.name : "NONE")}");
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(path);
            int n = 0; foreach (var c in clips) if (c is AnimationClip) n++;
            Debug.Log($"[MX] clipsInFbx={n}");

            foreach (var t in AssetDatabase.LoadAllAssetsAtPath(path))
                if (t is Texture2D tex) Debug.Log($"[MX] texture {tex.name} {tex.width}x{tex.height}");

            var bounds = new Bounds(inst.transform.position, Vector3.zero);
            foreach (var s in smrs) bounds.Encapsulate(s.bounds);
            Debug.Log($"[MX] heightUnits={bounds.size.y:F3}");
            Object.DestroyImmediate(inst);
        }

        private static Transform FindDeep(Transform t, string frag)
        {
            if (t.name.ToLowerInvariant().Contains(frag)) return t;
            for (var i = 0; i < t.childCount; i++)
            {
                var f = FindDeep(t.GetChild(i), frag);
                if (f != null) return f;
            }
            return null;
        }
    }
}
#endif
