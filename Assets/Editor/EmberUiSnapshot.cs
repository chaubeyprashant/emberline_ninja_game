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

            // Screens that read mission state get it through reflection: the
            // harness has no live game, only a bare GameManager.
            var gmT = typeof(GameManager);
            void Set(string prop, object value) =>
                gmT.GetProperty(prop, F | BindingFlags.Public)?.SetValue(gm, value);
            Core.Session.Mode = Core.LaunchMode.Story;
            Core.Session.LevelIndex = 0;
            Set("CurrentLevel", Core.Session.Story[0]);
            Set("CurrentPlan", Resources.Load<Missions.MissionPlan>("Missions/" + Core.Session.Story[0].planAsset));

            var toggleSettings = t.GetMethod("ToggleSettings", F);

            foreach (var (name, file, state, pause) in new[]
            {
                ("MenuRoot", "Logs/ui_menu.png", GameManager.Phase.Menu, false),
                ("Story", "Logs/ui_story.png", GameManager.Phase.Menu, false),
                ("Fight", "Logs/ui_duels.png", GameManager.Phase.Menu, false),
                ("Briefing", "Logs/ui_briefing.png", GameManager.Phase.Intro, false),
                ("Hud", "Logs/ui_hud.png", GameManager.Phase.Playing, false),
                ("Hud", "Logs/ui_pause.png", GameManager.Phase.Playing, true),
                ("Skills", "Logs/ui_skills.png", GameManager.Phase.Menu, false),
                ("Forge", "Logs/ui_forge.png", GameManager.Phase.Menu, false),
                ("Weapons", "Logs/ui_armoury.png", GameManager.Phase.Menu, false),
                ("Result", "Logs/ui_complete.png", GameManager.Phase.Won, false),
                ("Result", "Logs/ui_gameover.png", GameManager.Phase.Lost, false),
                ("MenuRoot", "Logs/ui_settings.png", GameManager.Phase.Menu, true),
                ("March", "Logs/ui_march.png", GameManager.Phase.Menu, false),
                ("Arms", "Logs/ui_arms.png", GameManager.Phase.Menu, false),
            })
            {
                Set("State", state);
                setScreen.Invoke(hud, new[] { System.Enum.Parse(screenEnum, name) });
                if (pause) toggleSettings.Invoke(hud, null);
                Canvas.ForceUpdateCanvases();
                Shoot(cam, file, 1600, 900);
                if (pause)
                {
                    // CloseSettings destroys its root with a deferred Destroy, which
                    // never runs in edit mode — the overlay bled into every later
                    // capture. Tear it down immediately here.
                    toggleSettings.Invoke(hud, null);
                    var settingsRoot = t.GetField("_settingsRoot", F)?.GetValue(hud) as RectTransform;
                    if (settingsRoot != null) Object.DestroyImmediate(settingsRoot.gameObject);
                }
            }

            RenderWeaponGlyphs(cam);
            RenderPerfOverlay(cam);

            Debug.Log("[Emberline] UI snapshots written to Logs/ui_*.png");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Dev perf overlay: edit mode never runs Start, so the panel is built
        /// and fed a synthetic frame window through reflection, the same way the
        /// menu screens above are driven.
        /// </summary>
        private static void RenderPerfOverlay(Camera cam)
        {
            var go = new GameObject("PerfOverlay");
            var overlay = go.AddComponent<UI.PerfOverlay>();
            var t = typeof(UI.PerfOverlay);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetMethod("Build", F).Invoke(overlay, null);

            // A believable 60fps window with one hitch, so every line has data.
            var frames = (System.Collections.Generic.Queue<float>)
                t.GetField("_frames", F).GetValue(overlay);
            for (var i = 0; i < 200; i++) frames.Enqueue(16.1f + (i % 7) * 0.3f);
            frames.Enqueue(38.4f);
            t.GetField("_worstMs", F).SetValue(overlay, 38.4f);
            t.GetField("_hitches", F).SetValue(overlay, 1);
            t.GetField("_sinceReset", F).SetValue(overlay, 4f);
            t.GetMethod("Refresh", F).Invoke(overlay, null);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            Shoot(cam, "Logs/ui_perf_overlay.png", 1600, 900);
        }

        /// <summary>Contact sheet of the generated weapon glyphs (boss-card icons).</summary>
        private static void RenderWeaponGlyphs(Camera cam)
        {
            var go = new GameObject("GlyphSheet");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            string[] names = { "sword", "axe", "spear", "bow", "claws", "bomb", "kunai" };
            for (var i = 0; i < names.Length; i++)
            {
                var rt = UI.UiKit.Rect(go.transform, names[i], new Vector2(0.5f, 0.5f),
                    new Vector2((i - (names.Length - 1) * 0.5f) * 150f, 20f),
                    new Vector2(110f, 110f));
                UI.UiKit.Img(rt, UI.UiKit.Icon(names[i]), UI.UiKit.Pale);
                UI.UiKit.Label(go.transform, names[i].ToUpperInvariant(), 16, UI.UiKit.Dim,
                    new Vector2(0.5f, 0.5f),
                    new Vector2((i - (names.Length - 1) * 0.5f) * 150f, -60f),
                    new Vector2(140f, 24f));
            }
            Canvas.ForceUpdateCanvases();
            Shoot(cam, "Logs/ui_weapon_glyphs.png", 1300, 420);
            Object.DestroyImmediate(go);
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
