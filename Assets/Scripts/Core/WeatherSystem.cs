using UnityEngine;

namespace Emberline.Core
{
    public enum WeatherState { Clear, Cloudy, Rain, Storm, Fog }

    /// <summary>
    /// Weather that changes while you are out in it. A random walk between five
    /// states every few minutes in explore, each a set of multipliers on the
    /// light and fog the clock already modulates, rain through the atmosphere's
    /// emitter, and two gameplay knobs: guards see less in rain and fog, and
    /// rain covers footsteps. Transitions are twenty seconds so a storm arrives
    /// rather than switches on. A mission keeps its authored weather.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public WeatherState State { get; private set; } = WeatherState.Clear;
        public bool Running { get; set; }

        private WeatherState _from;
        private float _blend = 1f, _nextChange;
        private float _lightningT;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private static (float light, float fog, float rain, float wind, float sight, float hush) Profile(WeatherState s) => s switch
        {
            WeatherState.Cloudy => (0.82f, 1.35f, 0f, 1.2f, 0.95f, 1f),
            WeatherState.Rain => (0.62f, 1.8f, 0.7f, 1.6f, 0.8f, 0.7f),
            WeatherState.Storm => (0.45f, 2.3f, 1f, 2.6f, 0.65f, 0.55f),
            WeatherState.Fog => (0.7f, 3.2f, 0f, 0.4f, 0.55f, 0.9f),
            _ => (1f, 1f, 0f, 1f, 1f, 1f),
        };

        public void SetState(WeatherState s, bool instant = false)
        {
            _from = instant ? s : State;
            State = s;
            _blend = instant ? 1f : 0f;
            _nextChange = Time.time + Random.Range(180f, 360f);
            Apply();
        }

        private void Update()
        {
            if (!Running) return;
            if (_blend < 1f)
            {
                _blend = Mathf.Min(1f, _blend + Time.deltaTime / 20f);
                Apply();
            }
            else if (Time.time > _nextChange)
            {
                SetState(Next(State));
            }
            if (State == WeatherState.Storm && Time.time > _lightningT)
            {
                _lightningT = Time.time + Random.Range(6f, 18f);
                StartCoroutine(Lightning());
            }
        }

        private static WeatherState Next(WeatherState s)
        {
            // Weather drifts a step at a time: clear to cloudy to rain to storm and
            // back, with fog a branch off cloudy. Never clear straight to storm.
            var r = Random.value;
            return s switch
            {
                WeatherState.Clear => r < 0.55f ? WeatherState.Cloudy : WeatherState.Clear,
                WeatherState.Cloudy => r < 0.35f ? WeatherState.Rain : r < 0.55f ? WeatherState.Fog : WeatherState.Clear,
                WeatherState.Rain => r < 0.3f ? WeatherState.Storm : r < 0.7f ? WeatherState.Cloudy : WeatherState.Rain,
                WeatherState.Storm => r < 0.6f ? WeatherState.Rain : WeatherState.Cloudy,
                _ => r < 0.6f ? WeatherState.Cloudy : WeatherState.Clear,
            };
        }

        private void Apply()
        {
            var a = Profile(_from);
            var b = Profile(State);
            var k = Mathf.SmoothStep(0f, 1f, _blend);
            TimeOfDay.WeatherLight = Mathf.Lerp(a.light, b.light, k);
            TimeOfDay.WeatherFog = Mathf.Lerp(a.fog, b.fog, k);
            UI.Atmosphere.Active?.SetRain01(Mathf.Lerp(a.rain, b.rain, k), Mathf.Lerp(a.wind, b.wind, k));
            Enemies.Visibility.WeatherScale = Mathf.Lerp(a.sight, b.sight, k);
            Enemies.NoiseSystem.Damping = Mathf.Lerp(a.hush, b.hush, k);
            TimeOfDay.Instance?.Apply();
        }

        private System.Collections.IEnumerator Lightning()
        {
            var keep = TimeOfDay.WeatherLight;
            TimeOfDay.WeatherLight = keep * 4f;
            TimeOfDay.Instance?.Apply();
            yield return new WaitForSeconds(0.08f);
            TimeOfDay.WeatherLight = keep;
            TimeOfDay.Instance?.Apply();
            yield return new WaitForSeconds(Random.Range(0.6f, 2.2f));
            Sfx3D.Thunder();
        }
    }
}
