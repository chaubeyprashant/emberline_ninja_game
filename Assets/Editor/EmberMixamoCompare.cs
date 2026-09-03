#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Builds the Mixamo replacement candidate next to the KayKit placeholder it
    /// would replace and renders both, so the art decision is made on a picture
    /// rather than a description. Also reports each against docs/ART_DIRECTION.md §4.1.
    /// </summary>
    public static class EmberMixamoCompare
    {
        public static void Run()
        {
            var stage = new GameObject("Stage");

            var mix = Make(EmberCharacterFactory.MixamoRenzo(), new Vector3(0.75f, 0, 0));
            var kay = Make(EmberCharacterFactory.Renzo(), new Vector3(-0.75f, 0, 0));

            Pose(mix, EmberCharacterFactory.MixamoRenzo());
            Pose(kay, EmberCharacterFactory.Renzo());
            Report("Mixamo  ", mix);
            Report("KayKit  ", kay);

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.05f, 3.4f);
            cam.transform.LookAt(new Vector3(0f, 0.95f, 0f));
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);

            var lgo = new GameObject("Key");
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(38f, 152f, 0f);
            RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.45f);

            Shoot(cam, "Logs/mixamo_compare.png", 1500, 900);
            Object.DestroyImmediate(stage);
            Debug.Log("[CMP] wrote Logs/mixamo_compare.png");
            EditorApplication.Exit(0);
        }

        /// <summary>Sample the character's Idle clip so both stand as they do in play.</summary>
        private static void Pose(GameObject go, EmberCharacterFactory.Spec spec)
        {
            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = anim != null ? anim.runtimeAnimatorController : null;
            if (ctrl == null) return;
            var idle = ctrl.animationClips.FirstOrDefault(c => c.name == "Idle")
                       ?? ctrl.animationClips.FirstOrDefault();
            if (idle == null) return;
            var target = anim.gameObject;
            idle.SampleAnimation(target, idle.length * 0.35f);
            Debug.Log($"[CMP] posed {go.name} with '{idle.name}'");
        }

        private static GameObject Make(EmberCharacterFactory.Spec spec, Vector3 pos)
        {
            var root = new GameObject(spec.name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.identity;   // +Z faces the camera
            if (!EmberCharacterFactory.Build(root, spec))
                Debug.Log($"[CMP] BUILD FAILED {spec.name}");
            return root;
        }

        /// <summary>Measure the built character against the §4.1 budget.</summary>
        private static void Report(string label, GameObject go)
        {
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var tris = smrs.Where(s => s.sharedMesh != null).Sum(s => s.sharedMesh.triangles.Length / 3);
            var bones = smrs.Length == 0 ? 0 : smrs.Max(s => s.bones?.Length ?? 0);
            var mats = smrs.Sum(s => s.sharedMaterials.Length);
            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = anim != null ? anim.runtimeAnimatorController : null;
            var states = ctrl != null ? ctrl.animationClips.Length : 0;
            var propT = go.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.StartsWith("Prop_"));
            var props = propT != null ? 1 : 0;
            if (propT != null)
                Debug.Log($"[CMP] {label} prop parent={propT.parent.name} " +
                          $"localPos={propT.localPosition} localRot={propT.localEulerAngles} " +
                          $"worldPos={propT.position}");
            var grip = go.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "GripAnchor");
            Debug.Log($"[CMP] {label} gripAnchor={(grip != null ? grip.parent.name : "NONE")}");
            Debug.Log($"[CMP] {label} tris={tris,6} bones={bones,3} mats={mats} " +
                      $"avatar={(anim != null && anim.avatar != null ? "yes" : "NO")} " +
                      $"clipsInController={states,3} props={props} " +
                      $"budget(tris<=18000 bones<=48): {(tris <= 18000 ? "PASS" : "OVER")}/{(bones <= 48 ? "PASS" : "OVER")}");
        }

        private static void Shoot(Camera cam, string file, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
        }
    }
}
#endif
