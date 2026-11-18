using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Explore mode's ambience: the bed follows where Renzo stands, what the sky
    /// is doing and the hour, and crossfades as he walks. Missions keep the bed
    /// their theme names (<see cref="UI.Atmosphere"/>); this only runs in the
    /// open valley.
    ///
    /// Order of precedence: rain drowns everything; then the place (marsh,
    /// falls, a village, the camp's fires, high ground); then night insects or,
    /// by day, the forest.
    /// </summary>
    public class AmbienceZones : MonoBehaviour
    {
        private const float Poll = 1f, Fade = 3f;
        private float _t;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("AmbienceZones");
            DontDestroyOnLoad(go);
            go.AddComponent<AmbienceZones>();
        }

        private void Update()
        {
            if ((_t -= Time.unscaledDeltaTime) > 0f) return;
            _t = Poll;
            if (Session.Mode != LaunchMode.Explore) return;
            var gm = SceneRefs.Game;
            var motor = SceneRefs.Motor;
            if (gm == null || motor == null || gm.State != GameManager.Phase.Playing) return;
            Sfx3D.PlayAmbience(BedAt(motor.transform.position), 0.45f, Fade);
        }

        /// <summary>The bed for a point in the region. Pure apart from the clock and weather.</summary>
        public static string BedAt(Vector3 p)
        {
            var weather = WeatherSystem.Instance != null ? WeatherSystem.Instance.State : WeatherState.Clear;
            if (weather is WeatherState.Rain or WeatherState.Storm) return "rain_ambience";

            var night = TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight;
            foreach (var l in WorldLandmarks.All)
            {
                var d = new Vector2(p.x - l.centre.x, p.z - l.centre.z).magnitude;
                switch (l.kind)
                {
                    case LandmarkKind.Marsh when d < l.radius:
                        return "marsh_ambience";
                    case LandmarkKind.Waterfall when d < l.radius * 1.6f:
                        return "river_ambience";
                    case LandmarkKind.Village or LandmarkKind.Hamlet when d < l.radius:
                        return night ? "night_ambience" : "village_ambience";
                    case LandmarkKind.Camp when d < l.radius:
                        return "fire_ambience";
                    case LandmarkKind.Pass or LandmarkKind.Stronghold or LandmarkKind.Temple when d < l.radius:
                        return "mountain_wind";
                }
            }
            return night ? "night_ambience" : "forest_ambience";
        }
    }
}
