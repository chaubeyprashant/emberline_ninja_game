using Emberline.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Emberline.UI
{
    /// <summary>
    /// The cinematic background behind opaque menus — and the reason the arena
    /// camera can be switched off while they are open.
    ///
    /// Menus used to sit over the live 3D scene, which meant the whole arena —
    /// shadows, fog, embers, the grade pass — was redrawn every frame behind a
    /// screen the player was reading. Measured on the Galaxy A33 that is roughly
    /// half a CPU core at 30fps for nothing. Now the scene is rendered <b>once</b>
    /// into a half-resolution texture the moment an opaque screen opens, the
    /// camera is disabled, and the texture is shown darkened and vignetted as a
    /// still. It reads as a deliberate cinematic plate rather than a paused game,
    /// and costs one blit.
    ///
    /// Transparent overlays (pause over gameplay) do not use this: the player
    /// is looking at the frozen fight, which is the point of a pause screen.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        public static MenuBackdrop Instance { get; private set; }

        private RenderTexture _plate;
        private Camera _cam;
        private bool _cameraWasEnabled = true;
        private RawImage _view;
        private Image _shade, _vignette;

        private void Awake() => Instance = this;
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Release();
        }

        /// <summary>
        /// Show a still of the scene under `screenRoot` and stop live rendering.
        /// Safe to call repeatedly; re-captures only if the plate is gone.
        /// </summary>
        public void Show(RectTransform screenRoot, float darkness = 0.72f)
        {
            _cam = _cam != null ? _cam : SceneRefs.Cam;
            if (_cam == null) return;

            if (_plate == null)
            {
                // Half resolution: this is a blurred-by-distance still behind text,
                // and the grade pass already softens it. Full res would cost four
                // times the memory for no visible gain.
                var w = Mathf.Max(320, Screen.width / 2);
                var h = Mathf.Max(180, Screen.height / 2);
                _plate = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "MenuBackdrop",
                    antiAliasing = 1,
                };
                _cameraWasEnabled = _cam.enabled;
                _cam.enabled = true; // Render() needs it on for this one call
                var prev = _cam.targetTexture;
                _cam.targetTexture = _plate;
                _cam.Render();
                _cam.targetTexture = prev;
            }

            // Freeze the arena. Nothing behind an opaque screen needs drawing.
            _cam.enabled = false;

            // Plate, then a dark shade, then the vignette — bottom to top.
            var plateRt = UiKit.Group(screenRoot, "Backdrop");
            plateRt.SetAsFirstSibling();
            _view = plateRt.gameObject.AddComponent<RawImage>();
            _view.texture = _plate;
            _view.color = new Color(0.78f, 0.80f, 0.86f, 1f); // cool it slightly
            _view.raycastTarget = false;

            var shadeRt = UiKit.Group(screenRoot, "Shade");
            shadeRt.SetSiblingIndex(1);
            _shade = UiKit.Img(shadeRt, null, new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, darkness));

            var vigRt = UiKit.Group(screenRoot, "Vignette");
            vigRt.SetSiblingIndex(2);
            _vignette = UiKit.Img(vigRt, UiKit.Vignette, new Color(0, 0, 0, 0.55f));
        }

        /// <summary>Gameplay or a transparent overlay: resume live rendering.</summary>
        public void Hide()
        {
            if (_cam != null && !_cam.enabled) _cam.enabled = true;
            Release();
        }

        private void Release()
        {
            if (_plate != null)
            {
                if (_cam != null && _cam.targetTexture == _plate) _cam.targetTexture = null;
                _plate.Release();
                Destroy(_plate);
                _plate = null;
            }
            _view = null;
            _shade = null;
            _vignette = null;
        }
    }
}
