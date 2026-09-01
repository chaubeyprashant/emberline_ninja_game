using UnityEngine;

namespace Emberline.UI
{
    /// <summary>
    /// Camera post-process for the cinematic look: filmic tonemap, saturation
    /// pull, lift/gain and vignette in one full-screen pass.
    ///
    /// Built-in pipeline, so this is an OnRenderImage image effect rather than a
    /// URP volume. It is a real fill-rate cost on mobile — one extra full-screen
    /// read/write — so the low graphics tier turns it off entirely and the game
    /// falls back to the raw, still-graded-by-lighting image.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class CinematicGrade : MonoBehaviour
    {
        [SerializeField] private float saturation = 0.82f;
        [SerializeField] private float contrast = 1.08f;
        [SerializeField] private float vignette = 0.34f;
        [SerializeField] private float exposure = 1.05f;
        [SerializeField] private Color lift = new(0.03f, 0.045f, 0.065f, 0f);
        [SerializeField] private Color gain = new(1.03f, 1.0f, 0.95f, 0f);

        private Material _mat;

        /// <summary>Set by the graphics tier; false skips the pass completely.</summary>
        public static bool Enabled { get; set; } = true;

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!Enabled)
            {
                Graphics.Blit(src, dst);
                return;
            }
            if (_mat == null)
            {
                var shader = Shader.Find("Emberline/Grade");
                if (shader == null) { Graphics.Blit(src, dst); return; }
                _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            _mat.SetFloat("_Saturation", saturation);
            _mat.SetFloat("_Contrast", contrast);
            _mat.SetFloat("_Vignette", vignette);
            _mat.SetFloat("_Exposure", exposure);
            _mat.SetColor("_Lift", lift);
            _mat.SetColor("_Gain", gain);
            Graphics.Blit(src, dst, _mat);
        }

        private void OnDisable()
        {
            if (_mat != null) DestroyImmediate(_mat);
            _mat = null;
        }
    }
}
