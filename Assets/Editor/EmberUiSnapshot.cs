using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Renders the uGUI menu screens to PNGs in batch mode by driving EmberHud's
    /// private builders via reflection (runtime lifecycle doesn't run in edit
    /// mode). Verifies layout, fonts and Kenney sprites without a device.
    /// </summary>
    public static class EmberUiSnapshot
    {
        [MenuItem("Emberline/Snapshot UI")]
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();

            var camGo = new GameObject("SnapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);

            var hudGo = new GameObject("EmberHud");
            var hud = hudGo.AddComponent<UI.EmberHud>();
            var t = typeof(UI.EmberHud);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("_gm", F).SetValue(hud, gm);
            t.GetMethod("BuildCanvas", F).Invoke(hud, null);

            var canvas = hudGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;

            var screenEnum = t.GetNestedType("Screen", BindingFlags.NonPublic);
            var setScreen = t.GetMethod("SetScreen", F);

            foreach (var (name, file) in new[]
            {
                ("MenuRoot", "Logs/ui_menu.png"),
                ("Story", "Logs/ui_story.png"),
                ("Skills", "Logs/ui_skills.png"),
                ("Hud", "Logs/ui_hud.png"),
            })
            {
                setScreen.Invoke(hud, new[] { System.Enum.Parse(screenEnum, name) });
                Canvas.ForceUpdateCanvases();
                Shoot(cam, file, 1600, 900);
            }

            Debug.Log("[Emberline] UI snapshots written to Logs/ui_*.png");
            if (Application.isBatchMode) EditorApplication.Exit(0);
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
            RenderTexture.active = null;
            cam.targetTexture = null;
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(rt);
        }
    }
}
