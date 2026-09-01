using System.Collections.Generic;
using Emberline.Core;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Endless
{
    /// <summary>
    /// Environmental hazards that belong to the ground rather than to an enemy.
    /// They exist to make position matter: a hazard the player can see and walk
    /// around is difficulty; one that ticks damage from off-screen is noise, so
    /// everything here is either static and visible or telegraphed before it hurts.
    /// Built from runtime quads on the existing glow shader — no new art.
    /// </summary>
    public class RunHazard : MonoBehaviour
    {
        public enum Kind { Fire, Spikes, Rockfall, Bog }

        private const float TickDamage = 6f;

        private static readonly List<RunHazard> Live = new();
        private static Shader _glow;

        private Kind _kind;
        private float _radius, _phase, _tick;
        private Material _mat;
        private bool _armed;

        /// <summary>
        /// Populate a stretch of road for a theme. Count rises with depth but is
        /// capped: past a point more hazards stop adding difficulty and start
        /// removing the floor the player fights on.
        /// </summary>
        public static void Populate(EnvThemeId theme, float zFrom, float zTo,
            float halfWidth, int depth)
        {
            var count = Mathf.Clamp(1 + depth / 4, 0, 5);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var x = Random.Range(-halfWidth + 1.5f, halfWidth - 1.5f);
                var z = Mathf.Lerp(zFrom, zTo, (i + 0.5f) / count) + Random.Range(-2f, 2f);
                Spawn(KindFor(theme), new Vector3(x, 0f, z));
            }
        }

        /// <summary>
        /// Hazards read as part of the place they are in. A brazier in a burning
        /// village and a bog in a graveyard cost the same to implement and do
        /// completely different things to how the space plays.
        /// </summary>
        private static Kind KindFor(EnvThemeId theme) => theme switch
        {
            EnvThemeId.BurningVillage => Kind.Fire,
            EnvThemeId.Fortress => Kind.Fire,
            EnvThemeId.Castle => Random.value < 0.5f ? Kind.Spikes : Kind.Fire,
            EnvThemeId.Temple => Kind.Spikes,
            EnvThemeId.Mountain => Kind.Rockfall,
            EnvThemeId.Graveyard => Kind.Bog,
            EnvThemeId.RainyBattlefield => Random.value < 0.5f ? Kind.Bog : Kind.Spikes,
            EnvThemeId.Forest => Kind.Bog,
            _ => Random.value < 0.5f ? Kind.Fire : Kind.Spikes,
        };

        public static void Spawn(Kind kind, Vector3 pos)
        {
            var radius = kind switch
            {
                Kind.Fire => 1.9f,
                Kind.Spikes => 1.7f,
                Kind.Rockfall => 2.2f,
                _ => 2.6f,
            };

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Hazard_" + kind;
            quad.transform.position = new Vector3(pos.x, 0.06f, pos.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (radius * 2f);

            var h = quad.AddComponent<RunHazard>();
            h._kind = kind;
            h._radius = radius;
            h._phase = Random.value * 3f;
            _glow = _glow != null ? _glow : Shader.Find("Emberline/Glow");
            h._mat = new Material(_glow) { color = h.BaseColor() };
            var r = quad.GetComponent<Renderer>();
            r.material = h._mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // A bog is a movement problem, not a damage one, so it delegates to
            // the existing slow-zone system the player's motor already queries.
            if (kind == Kind.Bog) Enemies.SlowZone.Spawn(pos, radius, 9999f);
        }

        /// <summary>Drop every hazard. Called when an encounter ends or a run does.</summary>
        public static void ClearAll()
        {
            for (var i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
            for (var i = Enemies.SlowZone.Active.Count - 1; i >= 0; i--)
                if (Enemies.SlowZone.Active[i] != null)
                    Destroy(Enemies.SlowZone.Active[i].gameObject);
        }

        private Color BaseColor() => _kind switch
        {
            Kind.Fire => new Color(1f, 0.42f, 0.16f, 0.45f),
            Kind.Spikes => new Color(0.75f, 0.78f, 0.85f, 0.25f),
            Kind.Rockfall => new Color(0.6f, 0.55f, 0.5f, 0.20f),
            _ => new Color(0.32f, 0.30f, 0.22f, 0.42f),
        };

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void Update()
        {
            _phase += Time.deltaTime;

            switch (_kind)
            {
                case Kind.Fire:
                    // Always on, always visible: the flicker is decoration, the
                    // damage is constant so the rule is never ambiguous.
                    _mat.color = new Color(1f, 0.42f, 0.16f,
                        0.42f + 0.1f * Mathf.Sin(_phase * 7f));
                    _armed = true;
                    break;

                case Kind.Spikes:
                case Kind.Rockfall:
                {
                    // Two-second cycle: 1.4s of visible tell, 0.6s armed. The
                    // player is meant to cross these, not to be denied them.
                    var t = _phase % 2f;
                    _armed = t > 1.4f;
                    var warn = Mathf.Clamp01((t - 0.6f) / 0.8f);
                    var c = BaseColor();
                    _mat.color = _armed
                        ? new Color(1f, 0.35f, 0.28f, 0.55f)
                        : new Color(Mathf.Lerp(c.r, 1f, warn * 0.6f), c.g, c.b,
                            c.a + warn * 0.25f);
                    break;
                }

                default: // Bog: the SlowZone does the work; this is only the look.
                    _armed = false;
                    break;
            }

            if (!_armed) return;
            if ((_tick -= Time.deltaTime) > 0f) return;
            _tick = _kind == Kind.Fire ? 0.5f : 0.6f;

            var motor = SceneRefs.Motor;
            if (motor == null || motor.Invulnerable) return;
            var d = motor.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > _radius * _radius) return;
            motor.GetComponent<Health>()?.Damage(TickDamage, transform.position);
            FxPools.Embers(motor.transform.position + Vector3.up * 0.5f, 4);
        }
    }
}
