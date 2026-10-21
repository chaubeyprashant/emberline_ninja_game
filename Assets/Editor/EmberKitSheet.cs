using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Builds the Japan and nature kits, then renders them in flat daylight as a
    /// contact sheet (<c>Logs/kit_buildings.png</c>, <c>Logs/kit_trees.png</c>) so
    /// their shapes can be judged without the valley's night grading. Run
    /// WITHOUT -nographics.
    /// </summary>
    public static class EmberKitSheet
    {
        [MenuItem("Emberline/Build + Snapshot Environment Kits")]
        public static void Run()
        {
            SessionState.SetBool("emberline.setup", true);
            try { EmberJapanKit.Build(); EmberNatureKit.Build(); }
            finally { SessionState.SetBool("emberline.setup", false); }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.2f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 20f;
            ground.GetComponent<Renderer>().sharedMaterial =
                Core.SurfaceKit.Make(Core.Surface.Stone, new Color(0.30f, 0.33f, 0.24f));

            string[] buildings = { "jpk_minka_s", "jpk_minka_s_lit", "jpk_minka_m", "jpk_minka_l", "jpk_kura",
                                   "jpk_yagura", "jpk_gate", "jpk_saku", "jpk_torii", "jpk_hokora", "jpk_toro" };
            var x = 0f;
            foreach (var n in buildings)
            {
                var p = Resources.Load<GameObject>("Props/Zone/" + n);
                if (p == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
                var w = Bounds(go).size.x;
                go.transform.position = new Vector3(x + w * 0.5f, 0f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
                x += w + 2.5f;
            }
            Shoot(new Vector3(x * 0.5f, 9f, -x * 0.55f), new Vector3(x * 0.5f, 2.5f, 0f), "Logs/kit_buildings.png");
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.gameObject != ground && r.transform.root.name.StartsWith("jpk_")) Object.DestroyImmediate(r.transform.root.gameObject);

            x = 0f;
            foreach (var kind in new[] { "pine", "matsu", "broad", "maple", "sakura" })
                for (var i = 1; i <= 3; i++)
                {
                    var p = Resources.Load<GameObject>($"Props/Zone/qn_{kind}_{i}");
                    if (p == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
                    go.transform.localScale = Vector3.one * 6f;
                    go.transform.position = new Vector3(x, 0f, 0f);
                    x += 6f;
                }
            Shoot(new Vector3(x * 0.5f, 7f, -x * 0.5f), new Vector3(x * 0.5f, 3.5f, 0f), "Logs/kit_trees.png");

            Debug.Log("[Kits] sheets written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Bounds Bounds(GameObject go)
        {
            var b = new Bounds(go.transform.position, Vector3.one);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Shoot(Vector3 pos, Vector3 look, string file)
        {
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(look);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.62f, 0.72f);
            var rt = new RenderTexture(2000, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(2000, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 2000, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject);
        }
    }
}
