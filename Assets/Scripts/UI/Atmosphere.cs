using UnityEngine;
using Emberline.Core;

namespace Emberline.UI
{
    /// <summary>
    /// Turns an EnvTheme into living air: weather, wind and ambient life. One
    /// pooled particle system per layer rather than emitters scattered through the
    /// scene, and every count runs through the graphics tier so the low tier can
    /// keep the mood at a third of the particles.
    ///
    /// Applied once when a scene builds; the theme decides what appears.
    /// </summary>
    public class Atmosphere : MonoBehaviour
    {
        public static Atmosphere Active { get; private set; }

        /// <summary>Wind strength 0..1+. Read by cloth/foliage sway.</summary>
        public static float Wind { get; private set; }

        /// <summary>Wind direction on the ground plane.</summary>
        public static Vector3 WindDir { get; private set; } = Vector3.forward;

        /// <summary>Footstep bank selector: decking vs. earth. Set by the theme.</summary>
        public static bool GroundIsWood { get; private set; }

        private ParticleSystem _weather, _life;
        private Transform _follow;

        public static Atmosphere Apply(EnvTheme theme, Transform follow)
        {
            var go = new GameObject("Atmosphere");
            var a = go.AddComponent<Atmosphere>();
            a._follow = follow;
            Active = a;

            Wind = theme.windStrength;
            WindDir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;

            a.BuildWeather(theme.weather);
            a.BuildLife(theme.life);
            // Each theme names the bed it wants; themes whose bed is not authored
            // yet fall back to the one clip that ships, so the wiring is already
            // correct when the real audio lands.
            GroundIsWood = theme.groundSurface == Surface.Wood;
            if (!string.IsNullOrEmpty(theme.ambienceClip))
            {
                Sfx3D.PlayAmbience(theme.ambienceClip);
                Sfx3D.PlayAmbience("marsh_ambience"); // no-op once the themed bed loads
            }
            return a;
        }

        private void OnDestroy() { if (Active == this) Active = null; }

        // Weather falls in a column that follows the camera, so a small emitter
        // covers the whole visible field instead of filling the arena.
        private void BuildWeather(Weather weather)
        {
            if (weather == Weather.Clear) return;
            var go = new GameObject("Weather");
            go.transform.SetParent(transform, false);
            _weather = go.AddComponent<ParticleSystem>();
            var main = _weather.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = _weather.emission;
            var shape = _weather.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = FxPools.WeatherMaterial(weather);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            switch (weather)
            {
                case Weather.Rain:
                    main.startLifetime = 1.1f;
                    main.startSpeed = 22f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
                    main.startColor = new Color(0.68f, 0.76f, 0.9f, 0.5f);
                    main.maxParticles = Tier(320);
                    emission.rateOverTime = Tier(260);
                    shape.scale = new Vector3(34f, 0.2f, 26f);
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.12f;
                    break;

                case Weather.Snow:
                    main.startLifetime = 6f;
                    main.startSpeed = 1.6f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
                    main.startColor = new Color(0.92f, 0.95f, 1f, 0.8f);
                    main.maxParticles = Tier(220);
                    emission.rateOverTime = Tier(45);
                    shape.scale = new Vector3(34f, 0.2f, 26f);
                    break;

                case Weather.Ash:
                    main.startLifetime = 7f;
                    main.startSpeed = 1.1f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                    main.startColor = new Color(0.55f, 0.36f, 0.24f, 0.7f);
                    main.maxParticles = Tier(180);
                    emission.rateOverTime = Tier(38);
                    shape.scale = new Vector3(30f, 0.2f, 24f);
                    break;

                case Weather.Mist:
                    main.startLifetime = 9f;
                    main.startSpeed = 0.35f;
                    main.startSize = new ParticleSystem.MinMaxCurve(3.5f, 7f);
                    main.startColor = new Color(0.5f, 0.58f, 0.6f, 0.10f);
                    main.maxParticles = Tier(40);
                    emission.rateOverTime = Tier(6);
                    shape.scale = new Vector3(28f, 1f, 22f);
                    break;
            }

            // Wind pushes everything that falls.
            var vel = _weather.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(WindDir.x * Wind * 2.4f);
            vel.z = new ParticleSystem.MinMaxCurve(WindDir.z * Wind * 2.4f);
        }

        /// <summary>Fireflies, drifting leaves, embers — the signal that a place is alive.</summary>
        private void BuildLife(AmbientLife life)
        {
            if (life == AmbientLife.None) return;
            var go = new GameObject("AmbientLife");
            go.transform.SetParent(transform, false);
            _life = go.AddComponent<ParticleSystem>();
            var main = _life.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.startLifetime = 8f;
            main.startSpeed = life == AmbientLife.Leaves ? 1.4f : 0.35f;
            main.maxParticles = Tier(70);

            var emission = _life.emission;
            emission.rateOverTime = Tier(12);
            var shape = _life.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 4f, 20f);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            switch (life)
            {
                case AmbientLife.Fireflies:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
                    main.startColor = new Color(0.85f, 1f, 0.55f, 0.9f);
                    r.material = FxPools.LifeMaterial(true);
                    // Fireflies blink rather than glide.
                    var noise = _life.noise;
                    noise.enabled = true;
                    noise.strength = 0.6f;
                    noise.frequency = 0.35f;
                    break;
                case AmbientLife.Embers:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
                    main.startColor = new Color(1f, 0.55f, 0.25f, 0.85f);
                    main.gravityModifier = -0.25f;   // embers rise
                    r.material = FxPools.LifeMaterial(true);
                    break;
                case AmbientLife.Leaves:
                case AmbientLife.Petals:
                    main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
                    main.startColor = life == AmbientLife.Petals
                        ? new Color(0.95f, 0.72f, 0.78f, 0.85f)
                        : new Color(0.45f, 0.52f, 0.28f, 0.85f);
                    main.gravityModifier = 0.12f;
                    r.material = FxPools.LifeMaterial(false);
                    var rot = _life.rotationOverLifetime;
                    rot.enabled = true;
                    rot.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);
                    break;
                default: // Dust, Crows stand in as slow motes
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.startColor = new Color(0.7f, 0.68f, 0.62f, 0.35f);
                    r.material = FxPools.LifeMaterial(false);
                    break;
            }

            var vel = _life.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(WindDir.x * Wind);
            vel.z = new ParticleSystem.MinMaxCurve(WindDir.z * Wind);
        }

        /// <summary>Particle budgets follow the graphics tier like every other effect.</summary>
        private static int Tier(int full) => Mathf.Max(4, Mathf.RoundToInt(full * FxPools.Density));

        private void Update()
        {
            // Emitters ride above the camera so a small volume covers the view.
            if (_follow == null) return;
            var p = _follow.position;
            if (_weather != null)
                _weather.transform.position = new Vector3(p.x, p.y + 12f, p.z);
            if (_life != null)
                _life.transform.position = new Vector3(p.x, p.y + 1.5f, p.z);
        }
    }
}
