using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// The clock. Twenty-four hours in a configurable number of real minutes,
    /// driving the key light's angle, colour and strength, the ambient trilight,
    /// the fog and sky, and how visible Renzo is to a guard.
    ///
    /// <para>
    /// The scenes are authored and validated at their theme's own hour (the
    /// village theme is night), so the clock only <em>modulates</em> what the
    /// theme baked, and only while <see cref="Running"/> — the explore mode.
    /// A mission fixes the hour from its own night flag and the clock stands
    /// still, which keeps a hundred validated missions looking exactly as they
    /// did. Night never drops below 55 % of the theme's key light: a phone
    /// screen in daylight still has to show the fight.
    /// </para>
    /// </summary>
    public class TimeOfDay : MonoBehaviour
    {
        public static TimeOfDay Instance { get; private set; }

        [Tooltip("Real minutes for one full day.")]
        public float dayMinutes = 24f;
        [Range(0f, 24f)] public float startHour = 7f;

        /// <summary>Advances only in explore; a mission holds it.</summary>
        public bool Running { get; set; }

        public float Hour { get; private set; }

        /// <summary>0 at midnight, 1 at noon.</summary>
        public float Daylight { get; private set; }

        public bool IsNight => Hour < 5.5f || Hour > 19.5f;

        private Light _key;
        private Quaternion _keyRest;
        private Color _keyColor, _skyAmb, _eqAmb, _groundAmb, _fogColor;
        private float _keyIntensity, _fogDensity;
        private Material _sky;
        private float _skyExposure;
        private Color _skyTint;
        private bool _captured;

        private void Awake()
        {
            Instance = this;
            Hour = startHour;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Remember the theme's authored look so the clock has something to modulate.</summary>
        private void Capture()
        {
            if (_captured) return;
            var go = GameObject.Find("KeyLight");
            _key = go != null ? go.GetComponent<Light>() : null;
            if (_key == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { _key = l; break; }
            }
            if (_key != null)
            {
                _keyRest = _key.transform.rotation;
                _keyColor = _key.color;
                _keyIntensity = _key.intensity;
            }
            _skyAmb = RenderSettings.ambientSkyColor;
            _eqAmb = RenderSettings.ambientEquatorColor;
            _groundAmb = RenderSettings.ambientGroundColor;
            _fogColor = RenderSettings.fogColor;
            _fogDensity = RenderSettings.fogDensity;
            _sky = RenderSettings.skybox;
            if (_sky != null)
            {
                // Instance the sky so a modulated exposure never writes back to the asset.
                _sky = new Material(_sky);
                RenderSettings.skybox = _sky;
                _skyExposure = _sky.HasProperty("_Exposure") ? _sky.GetFloat("_Exposure") : 0.45f;
                _skyTint = _sky.HasProperty("_SkyTint") ? _sky.GetColor("_SkyTint") : Color.grey;
            }
            _captured = true;
        }

        public void SetHour(float hour)
        {
            Hour = Mathf.Repeat(hour, 24f);
            Apply();
        }

        private void Update()
        {
            if (!Running) return;
            Hour = Mathf.Repeat(Hour + Time.deltaTime * 24f / (Mathf.Max(1f, dayMinutes) * 60f), 24f);
            Apply();
        }

        /// <summary>Weather multiplies the light and fog on top of the hour.</summary>
        public static float WeatherLight { get; set; } = 1f;
        public static float WeatherFog { get; set; } = 1f;

        public void Apply()
        {
            Capture();
            // Daylight follows a sine from 6 to 18; dawn and dusk are the shoulders.
            Daylight = Mathf.Clamp01(Mathf.Sin((Hour - 6f) / 12f * Mathf.PI));
            var dawnDusk = Mathf.Clamp01(1f - Mathf.Abs(Daylight - 0.22f) / 0.22f) * (Daylight > 0.02f ? 1f : 0f);

            // The theme baked a night. Day is the same rig turned up and warmed:
            // never darker than the theme, up to 2.3x brighter at noon.
            var lightMul = Mathf.Lerp(1f, 2.3f, Daylight) * WeatherLight;
            var warm = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.38f), dawnDusk * 0.7f);
            var dayTint = Color.Lerp(Color.white, new Color(1f, 0.96f, 0.88f), Daylight);

            if (_key != null)
            {
                _key.intensity = _keyIntensity * lightMul;
                _key.color = _keyColor * dayTint * warm;
                // Swing the sun: low at dawn and dusk, high at noon, back to the
                // theme's authored night angle after dark.
                var elev = Mathf.Lerp(12f, 62f, Daylight);
                var yaw = _keyRest.eulerAngles.y + Mathf.Lerp(-40f, 40f, (Hour - 6f) / 12f) * (Daylight > 0.02f ? 1f : 0f);
                _key.transform.rotation = Daylight > 0.02f
                    ? Quaternion.Slerp(_keyRest, Quaternion.Euler(elev, yaw, 0f), Mathf.Clamp01(Daylight * 4f))
                    : _keyRest;
            }

            var ambMul = Mathf.Lerp(1f, 2.0f, Daylight) * Mathf.Lerp(1f, 0.85f, 1f - WeatherLight);
            RenderSettings.ambientSkyColor = _skyAmb * ambMul * dayTint;
            RenderSettings.ambientEquatorColor = _eqAmb * ambMul * warm;
            RenderSettings.ambientGroundColor = _groundAmb * ambMul;

            var fogDay = Color.Lerp(_fogColor, new Color(0.62f, 0.68f, 0.72f), Daylight * 0.85f);
            RenderSettings.fogColor = Color.Lerp(fogDay, fogDay * warm, dawnDusk);
            RenderSettings.fogDensity = _fogDensity * WeatherFog * Mathf.Lerp(1f, 0.7f, Daylight);

            if (_sky != null)
            {
                if (_sky.HasProperty("_Exposure"))
                    _sky.SetFloat("_Exposure", _skyExposure * Mathf.Lerp(1f, 2.4f, Daylight) * Mathf.Lerp(1f, 0.7f, 1f - WeatherLight));
                if (_sky.HasProperty("_SkyTint"))
                    _sky.SetColor("_SkyTint", Color.Lerp(_skyTint, new Color(0.45f, 0.6f, 0.8f), Daylight * 0.8f) * warm);
            }

            // Guards see less at night; the scale sits beside the mission's own
            // fog and night modifiers instead of compounding with them.
            Enemies.Visibility.DaylightScale = Mathf.Lerp(0.55f, 1f, Daylight);
        }
    }
}
