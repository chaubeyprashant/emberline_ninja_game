#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>Renders the same character at several grip rotations so the right
    /// one is chosen by eye instead of by guesswork.</summary>
    public static class EmberGripSweep
    {
        private static readonly Vector3[] Candidates =
        {
            new(0, 0, 0), new(0, 0, 90), new(0, 0, -90), new(-90, 0, 90),
        };

        public static void Run()
        {
            var stage = new GameObject("Stage");
            for (var i = 0; i < Candidates.Length; i++)
            {
                var spec = EmberCharacterFactory.MixamoRenzo();
                spec.name = "Grip" + i;
                spec.lantern = false;
                spec.propOffsetRot = Candidates[i];
                spec.propOffsetPos = new Vector3(0f, 0.02f, 0f);
                var root = new GameObject("G" + i);
                root.transform.position = new Vector3(i * 1.6f - 2.4f, 0, 0);
                EmberCharacterFactory.Build(root, spec);
                var anim = root.GetComponentInChildren<Animator>();
                var idle = anim?.runtimeAnimatorController?.animationClips.FirstOrDefault(c => c.name == "Idle");
                if (idle != null) idle.SampleAnimation(anim.gameObject, idle.length * 0.35f);
                Debug.Log($"[GRIP] {i} rot={Candidates[i]}");
            }

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.1f, 5.4f);
            cam.transform.rotation = Quaternion.Euler(12f, 180f, 0f);
            cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
            var light = new GameObject("Key").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(38f, 152f, 0f);
            RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.5f);

            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes("Logs/grip_sweep.png", tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(stage);
            Debug.Log("[GRIP] wrote Logs/grip_sweep.png");
            EditorApplication.Exit(0);
        }
    }
}
#endif
