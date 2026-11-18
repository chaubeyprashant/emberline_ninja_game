using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Real light from the valley's lanterns and fires, on a budget.
    ///
    /// <para>
    /// Each per-pixel point light in the Built-in path is an extra pass over
    /// every renderer inside its range, so a light per lantern would multiply
    /// the frame. Instead a small pool of point lights is lent to the
    /// <see cref="LightAnchor"/>s nearest the camera: as many as the tier's
    /// pixel-light budget allows after the key (0 / 2 / 4), short range so each
    /// pass touches only what is near it, no shadows. Lights fade between
    /// anchors so a lantern never pops on.
    /// </para>
    ///
    /// <para>
    /// By day the lanterns are out: intensity follows <see cref="TimeOfDay"/>
    /// in explore mode; mission themes are all dusk or night and keep them lit.
    /// </para>
    /// </summary>
    public class LanternLights : MonoBehaviour
    {
        private const int MaxPool = 4;
        private const float Reach = 32f, Fade = 2.5f, Poll = 0.25f;

        private readonly Light[] _pool = new Light[MaxPool];
        private readonly LightAnchor[] _target = new LightAnchor[MaxPool];
        private readonly float[] _level = new float[MaxPool];
        private readonly bool[] _wanted = new bool[MaxPool];
        private float _poll;
        private Camera _cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("LanternLights");
            DontDestroyOnLoad(go);
            go.AddComponent<LanternLights>();
        }

        private void Awake()
        {
            for (var i = 0; i < MaxPool; i++)
            {
                var l = new GameObject("LanternLight_" + i).AddComponent<Light>();
                l.transform.SetParent(transform, false);
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.ForcePixel;
                l.enabled = false;
                _pool[i] = l;
            }
        }

        private void Update()
        {
            // Camera.main can come back empty (batch play, a retagged camera);
            // fall back to any camera, found once and cached.
            if (_cam == null || !_cam.isActiveAndEnabled)
                _cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            var cam = _cam;
            if (cam == null) return;
            var budget = Mathf.Clamp(QualitySettings.pixelLightCount - 1, 0, MaxPool);

            if ((_poll -= Time.unscaledDeltaTime) <= 0f)
            {
                _poll = Poll;
                Choose(cam.transform.position, budget);
            }

            var night = TimeOfDay.Instance != null && TimeOfDay.Instance.Running
                ? 1f - TimeOfDay.Instance.Daylight : 1f;
            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < MaxPool; i++)
            {
                var a = _target[i];
                var want = a != null && _wanted[i] && i < budget ? 1f : 0f;
                _level[i] = Mathf.MoveTowards(_level[i], want, dt / Fade);
                var l = _pool[i];
                l.enabled = _level[i] > 0.01f && night > 0.05f && a != null;
                if (!l.enabled) continue;
                l.transform.position = a.Position;
                l.color = a.color;
                l.range = a.range;
                var flick = a.flicker ? 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 7f, i * 3.1f) : 1f;
                l.intensity = a.intensity * _level[i] * night * flick;
            }
        }

        /// <summary>Keep anchors already lit if still among the nearest; fill free slots with the next nearest.</summary>
        private void Choose(Vector3 from, int budget)
        {
            var best = new LightAnchor[MaxPool];
            var bestD = new float[MaxPool];
            for (var i = 0; i < MaxPool; i++) bestD[i] = Reach * Reach;
            foreach (var a in LightAnchor.Active)
            {
                var d = (a.Position - from).sqrMagnitude;
                for (var k = 0; k < budget; k++)
                {
                    if (d >= bestD[k]) continue;
                    for (var m = budget - 1; m > k; m--) { best[m] = best[m - 1]; bestD[m] = bestD[m - 1]; }
                    best[k] = a; bestD[k] = d;
                    break;
                }
            }
            // Slots whose anchor survived keep it (no swap, no flash); the rest
            // fade out where they are and are handed a newcomer once dark.
            for (var i = 0; i < MaxPool; i++)
                _wanted[i] = _target[i] != null && System.Array.IndexOf(best, _target[i]) >= 0;
            for (var i = 0; i < MaxPool; i++)
            {
                if (_wanted[i] || _level[i] > 0.01f) continue;
                _target[i] = null;
                foreach (var b in best)
                {
                    if (b == null || System.Array.IndexOf(_target, b) >= 0) continue;
                    _target[i] = b;
                    _wanted[i] = true;
                    break;
                }
            }
        }
    }
}
