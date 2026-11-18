using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberline.Core
{
    /// <summary>
    /// Folds the scene's FillLight and RimLight into the Surface shader's base
    /// pass, and sets anti-aliasing from the graphics tier.
    ///
    /// <para>
    /// The bootstrap's three-point rig used three real directional lights. In the
    /// Built-in forward path every directional light past the first is an
    /// additive pass over every object it lights — the whole valley drawn three
    /// times for a cool fill and a soft back light. Here they become two shader
    /// globals (colour × intensity, direction) and their Light components are
    /// switched off, which also frees the per-pixel budget for lanterns
    /// (<see cref="LanternLights"/>). The lights stay in the scene as the
    /// authored values, so TimeOfDay can keep modulating their intensity.
    /// </para>
    /// </summary>
    public class LightRig : MonoBehaviour
    {
        private static readonly int FillColor = Shader.PropertyToID("_EmberFillColor");
        private static readonly int FillDir = Shader.PropertyToID("_EmberFillDir");
        private static readonly int BackColor = Shader.PropertyToID("_EmberBackColor");
        private static readonly int BackDir = Shader.PropertyToID("_EmberBackDir");

        private Light _fill, _back;
        private int _tier = -1;
        private float _tierPoll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("LightRig");
            DontDestroyOnLoad(go);
            go.AddComponent<LightRig>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnScene;
            Bind();
        }

        private void OnDisable() => SceneManager.sceneLoaded -= OnScene;

        private void OnScene(Scene s, LoadSceneMode m) => Bind();

        private void Bind()
        {
            _tier = -1;   // a new scene has a new camera: re-apply the tier to it
            _fill = Find("FillLight");
            _back = Find("RimLight");
            foreach (var l in new[] { _fill, _back })
                if (l != null) l.enabled = false;   // the shader carries them now
            Push();
        }

        private static Light Find(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Light>() : null;
        }

        private void LateUpdate()
        {
            Push();
            if ((_tierPoll -= Time.unscaledDeltaTime) > 0f && _tier >= 0) return;
            _tierPoll = 1f;
            var tier = PlayerPrefs.GetInt("gfx_tier", 1);
            if (tier == _tier) return;
            _tier = tier;
            // Mali and Adreno resolve MSAA on-tile, so 2x is close to free on
            // the A33; 4x on the high tier only.
            QualitySettings.antiAliasing = tier == 0 ? 0 : tier == 1 ? 2 : 4;
            UI.CinematicGrade.Bloom = tier > 0;
            var cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            RenderLayers.Apply(cam, tier);
        }

        private void Push()
        {
            Set(_fill, FillColor, FillDir);
            Set(_back, BackColor, BackDir);
        }

        private static void Set(Light l, int color, int dir)
        {
            if (l == null) { Shader.SetGlobalVector(color, Vector4.zero); return; }
            var c = l.color * l.intensity;
            Shader.SetGlobalVector(color, new Vector4(c.r, c.g, c.b, 0f));
            var d = -l.transform.forward;   // toward the light, as _WorldSpaceLightPos0
            Shader.SetGlobalVector(dir, new Vector4(d.x, d.y, d.z, 0f));
        }
    }
}
