using System.IO;
using Emberline.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The launch splash: Emberline's key art and title instead of the "Made with
    /// Unity" card.
    ///
    /// <para>
    /// Unity 6 lets every plan turn the Unity logo off, so the splash is just
    /// our own logo on the key art's night-navy. <see cref="Build"/> renders that
    /// logo from <c>Assets/Icon.png</c> and the game's own display font — like the
    /// rest of the project it is generated, not hand-made in an image editor — and
    /// <see cref="Apply"/> wires it into PlayerSettings. The APK build calls
    /// <see cref="Apply"/> so a build can never ship the Unity card by accident.
    /// </para>
    /// </summary>
    public static class EmberSplash
    {
        private const string IconPath = "Assets/Icon.png";
        public const string LogoPath = "Assets/Art/Splash/splash_logo.png";

        /// <summary>The deep navy of the key art's sky, so the logo sits seamlessly.</summary>
        public static readonly Color Background = new(0.027f, 0.055f, 0.13f);

        private static readonly Color Ember = new(0.96f, 0.49f, 0.22f);
        private static readonly Color Pale = new(0.72f, 0.76f, 0.84f);
        private const int W = 1200, H = 1000;

        /// <summary>Render the splash logo, then apply it. Run WITHOUT -nographics.</summary>
        [MenuItem("Emberline/Build Splash Screen")]
        public static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("SplashCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.orthographic = true;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var canvas = new GameObject("SplashCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            var root = (RectTransform)canvas.transform;

            // Key art, framed exactly as the launcher icon is.
            var art = Rect<RawImage>(root, "Art", new Vector2(0f, 150f), new Vector2(620f, 620f));
            art.texture = IconWithoutCorners();

            var title = Rect<TextMeshProUGUI>(root, "Title", new Vector2(0f, -268f), new Vector2(1150f, 190f));
            title.font = UiKit.DisplayFont;
            title.text = "EMBERLINE";
            title.fontSize = 150f;
            title.characterSpacing = 6f;
            title.color = Ember;
            title.alignment = TextAlignmentOptions.Center;

            var rule = Rect<Image>(root, "Rule", new Vector2(0f, -372f), new Vector2(240f, 4f));
            rule.color = new Color(Ember.r, Ember.g, Ember.b, 0.6f);

            var studio = Rect<TextMeshProUGUI>(root, "Studio", new Vector2(0f, -420f), new Vector2(1150f, 60f));
            studio.font = UiKit.HeadingFont;
            studio.text = "ERGEBINS TECHNOLOGIES";
            studio.fontSize = 38f;
            studio.characterSpacing = 14f;
            studio.color = Pale;
            studio.alignment = TextAlignmentOptions.Center;

            Canvas.ForceUpdateCanvases();
            title.ForceMeshUpdate();
            studio.ForceMeshUpdate();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(LogoPath));
            File.WriteAllBytes(LogoPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            AssetDatabase.ImportAsset(LogoPath);
            Debug.Log($"[Emberline] Splash logo rendered to {LogoPath}");

            Apply();
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Show only our logo on navy; the Unity logo is off.</summary>
        public static void Apply()
        {
            if (AssetImporter.GetAtPath(LogoPath) is TextureImporter ti &&
                (ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled ||
                 ti.spriteImportMode != SpriteImportMode.Single))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = false;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }

            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            if (logo == null)
            {
                Debug.LogWarning($"[Emberline] No splash logo at {LogoPath} - run Emberline/Build Splash Screen.");
                return;
            }

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.backgroundColor = Background;
            // Overlay tints the logo toward the background colour; the logo
            // already carries that colour, so leave it untouched.
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
            Debug.Log("[Emberline] Splash: Emberline logo on navy, Unity logo off");
        }

        /// <summary>
        /// The launcher icon with its white corner matte made transparent. Icon.png
        /// is a rounded card on an opaque white square, which on the navy splash
        /// read as a white box around the art. Flood-filled from the four corners
        /// so the card's own light pixels (the moon) are never touched.
        /// </summary>
        private static Texture2D IconWithoutCorners()
        {
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (src == null) return null;

            // Read through a RenderTexture so the icon's importer settings stay as they are.
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            var cleared = new bool[px.Length];
            var visited = new bool[px.Length];
            var stack = new System.Collections.Generic.Stack<int>();
            stack.Push(0); stack.Push(w - 1); stack.Push((h - 1) * w); stack.Push(h * w - 1);
            while (stack.Count > 0)
            {
                var i = stack.Pop();
                if (i < 0 || i >= px.Length || visited[i]) continue;
                visited[i] = true;
                var c = px[i];
                if (c.r < 200 || c.g < 200 || c.b < 200) continue;   // the card's edge
                cleared[i] = true;
                px[i].a = 0;
                var x = i % w;
                if (x > 0) stack.Push(i - 1);
                if (x < w - 1) stack.Push(i + 1);
                stack.Push(i - w);
                stack.Push(i + w);
            }

            // Soften the anti-aliased rim: light pixels touching the cleared matte
            // fade out in proportion to how white they are, so no pale fringe remains.
            for (var i = 0; i < px.Length; i++)
            {
                if (cleared[i]) continue;
                var x = i % w;
                var touches = (x > 0 && cleared[i - 1]) || (x < w - 1 && cleared[i + 1]) ||
                              (i >= w && cleared[i - w]) || (i + w < px.Length && cleared[i + w]);
                if (!touches) continue;
                var min = Mathf.Min(px[i].r, Mathf.Min(px[i].g, px[i].b));
                if (min > 120) px[i].a = (byte)Mathf.Clamp((255 - min) * 3, 0, 255);
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static T Rect<T>(RectTransform parent, string name, Vector2 pos, Vector2 size) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go.AddComponent<T>();
        }
    }
}
