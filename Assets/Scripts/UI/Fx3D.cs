using System.Collections.Generic;
using UnityEngine;

namespace Emberline.UI
{
    /// <summary>Pooled world-space damage/heal numbers. Zero assets, zero churn.</summary>
    public class FloatingText : MonoBehaviour
    {
        private static readonly Queue<FloatingText> Pool = new();

        private float _life;
        private TextMesh _tm;
        private Color _color;

        public static void Spawn(Vector3 pos, string text, Color color, float size = 1f)
        {
            FloatingText ft = null;
            while (Pool.Count > 0 && ft == null) ft = Pool.Dequeue(); // skip destroyed
            if (ft == null)
            {
                var go = new GameObject("FloatingText");
                ft = go.AddComponent<FloatingText>();
                ft._tm = go.AddComponent<TextMesh>();
                ft._tm.fontSize = 54;
                ft._tm.anchor = TextAnchor.MiddleCenter;
                ft._tm.fontStyle = FontStyle.Bold;
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    ft._tm.font = font;
                    go.GetComponent<MeshRenderer>().material = font.material;
                }
            }
            ft.gameObject.SetActive(true);
            ft.transform.position = pos;
            ft._tm.text = text;
            ft._tm.characterSize = 0.045f * size;
            ft._color = color;
            ft._life = 0.7f;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                gameObject.SetActive(false);
                Pool.Enqueue(this);
                return;
            }
            transform.position += Vector3.up * (1.6f * Time.deltaTime);
            var cam = Core.SceneRefs.Cam; // Camera.main is a tagged search, per text, per frame
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            _tm.color = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(_life / 0.35f));
        }
    }

    /// <summary>
    /// Pooled particle effects — reusable ParticleSystems configured in code,
    /// now textured with the Kenney particle pack (Resources/Art/VFX) through
    /// Emberline/GlowTex, with the procedural Glow shader as fallback.
    /// Also: pooled slash-arc quads and enemy death bursts. Zero runtime churn.
    /// </summary>
    public static class FxPools
    {
        private static ParticleSystem _sparks, _embers, _smoke;
        private static Transform _novaQuad;
        private static Material _novaMat;
        private static float _novaT;
        private static NovaTicker _ticker;

        /// <summary>
        /// Particle budget multiplier driven by the graphics tier
        /// (EmberHud.ApplyGraphicsTier). Low-end phones spend their frame on the
        /// fight, not on embers; the high tier can afford a denser look.
        /// </summary>
        public static float Density { get; set; } = 1f;

        private static int Scaled(int count) => Mathf.Max(1, Mathf.RoundToInt(count * Density));

        /// <summary>
        /// Per-system particle ceiling for the current tier. Applied live so the
        /// options screen takes effect without a scene reload.
        /// </summary>
        public static int MaxParticles
        {
            get => _maxParticles;
            set
            {
                _maxParticles = value;
                Apply(_sparks);
                Apply(_embers);
                Apply(_smoke);

                static void Apply(ParticleSystem ps)
                {
                    if (ps == null) return;
                    var main = ps.main;
                    main.maxParticles = _maxParticles;
                }
            }
        }

        private static int _maxParticles = 256;

        // Pooled slash arcs.
        private const int SlashCount = 4;
        private static readonly Transform[] SlashQuads = new Transform[SlashCount];
        private static readonly Material[] SlashMats = new Material[SlashCount];
        private static readonly float[] SlashT = new float[SlashCount];
        private static int _slashNext;

        private static Material GlowMat(string texName, Color color)
        {
            var tex = texName != null ? Resources.Load<Texture2D>("Art/VFX/" + texName) : null;
            var mat = new Material(Shader.Find(tex != null ? "Emberline/GlowTex" : "Emberline/Glow"));
            if (tex != null) mat.mainTexture = tex;
            mat.color = color;
            return mat;
        }

        private static void Ensure()
        {
            if (_sparks != null) return;
            var root = new GameObject("FxPools");
            Object.DontDestroyOnLoad(root);
            _sparks = MakeSystem(root.transform, "Sparks", 0.28f, 0.14f, 7f, 1.2f, "spark_04");
            _embers = MakeSystem(root.transform, "Embers", 0.55f, 0.2f, 4.5f, 0.7f, "circle_05");
            _smoke = MakeSystem(root.transform, "Smoke", 0.8f, 0.7f, 1.6f, -0.25f, "smoke_05");

            // Surge nova: one reusable expanding glow disc.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Nova";
            quad.transform.SetParent(root.transform, false);
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            _novaMat = GlowMat("twirl_01", new Color(1f, 0.45f, 0.28f, 0.9f));
            quad.GetComponent<Renderer>().material = _novaMat;
            _novaQuad = quad.transform;
            quad.SetActive(false);

            // Slash arc quads.
            for (var i = 0; i < SlashCount; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(s.GetComponent<Collider>());
                s.name = "Slash" + i;
                s.transform.SetParent(root.transform, false);
                SlashMats[i] = GlowMat("slash_02", Color.white);
                s.GetComponent<Renderer>().material = SlashMats[i];
                SlashQuads[i] = s.transform;
                s.SetActive(false);
            }

            _ticker = root.AddComponent<NovaTicker>();
        }

        private static ParticleSystem MakeSystem(Transform parent, string name,
            float life, float size, float speed, float gravity, string texName = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _maxParticles;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            colorOverLife.color = grad;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GlowMat(texName, Color.white);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static Material _weatherMat, _lifeGlowMat, _lifeSoftMat;

        /// <summary>Shared material for a weather layer — one per game, not per system.</summary>
        public static Material WeatherMaterial(Core.Weather weather)
        {
            if (_weatherMat != null) return _weatherMat;
            // Rain and snow both read as a soft bright mote; the particle's own
            // colour and stretch do the rest, so one material serves every kind.
            _weatherMat = GlowMat("circle_05", Color.white);
            return _weatherMat;
        }

        /// <summary>Ambient-life material: glowing (fireflies, embers) or soft (leaves).</summary>
        public static Material LifeMaterial(bool glowing)
        {
            if (glowing) return _lifeGlowMat ??= GlowMat("circle_05", Color.white);
            return _lifeSoftMat ??= GlowMat("smoke_05", Color.white);
        }

        /// <summary>Build all pooled systems while a load/menu hides the cost.</summary>
        public static void Prewarm() => Ensure();

        public static void Sparks(Vector3 pos, Color color, int count = 8)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams { startColor = color };
            _sparks.transform.position = pos;
            _sparks.Emit(ep, Scaled(count));
        }

        public static void Embers(Vector3 pos, int count = 14)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams { startColor = new Color(1f, 0.5f, 0.28f) };
            _embers.transform.position = pos;
            _embers.Emit(ep, Scaled(count));
        }

        public static void Nova(Vector3 pos)
        {
            Ensure();
            _novaQuad.position = pos + Vector3.up * 0.15f;
            _novaQuad.localScale = Vector3.one * 1.5f;
            _novaQuad.gameObject.SetActive(true);
            _novaT = 0.4f;
            Embers(pos + Vector3.up * 0.6f, 30);
        }

        /// <summary>Camera-facing slash arc at the strike point, rolled along `dir`.</summary>
        public static void Slash(Vector3 pos, Vector3 dir, bool crush)
        {
            Ensure();
            var i = _slashNext;
            _slashNext = (_slashNext + 1) % SlashCount;
            var quad = SlashQuads[i];
            quad.position = pos;
            var cam = Core.SceneRefs.Cam;
            if (cam != null)
            {
                quad.rotation = Quaternion.LookRotation(quad.position - cam.transform.position);
                var roll = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg + Random.Range(-25f, 25f);
                quad.Rotate(0, 0, roll, Space.Self);
            }
            SlashMats[i].color = crush
                ? new Color(1f, 0.55f, 0.3f, 0.95f)
                : new Color(0.9f, 0.95f, 1f, 0.85f);
            quad.localScale = Vector3.one * (crush ? 2.2f : 1.6f);
            quad.gameObject.SetActive(true);
            SlashT[i] = 0.16f;
        }

        /// <summary>
        /// Grey-green smoke puff — the smoke bomb's cloud, and any other soft
        /// dissipating burst. Reuses the death-smoke system with a tinted colour.
        /// </summary>
        public static void Puff(Vector3 pos, Color color, int count = 14)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams { startColor = color };
            _smoke.transform.position = pos;
            _smoke.Emit(ep, Scaled(count));
        }

        /// <summary>
        /// Twin-dagger signature: two quick, thin slash lines instead of one heavy
        /// arc, so the fast chain reads as fast rather than just frequent.
        /// </summary>
        public static void QuickSlash(Vector3 pos, Vector3 dir)
        {
            Slash(pos, dir, false);
            Slash(pos + Vector3.up * 0.25f - dir * 0.2f,
                Quaternion.Euler(0f, 35f, 0f) * dir, false);
        }

        /// <summary>Crossbow bolt: a tight spark line along the shot, no arc.</summary>
        public static void BoltTrail(Vector3 pos, Vector3 dir)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams
            {
                startColor = new Color(0.95f, 0.85f, 0.55f, 0.9f),
            };
            for (var i = 0; i < 3; i++)
            {
                _sparks.transform.position = pos + dir * (i * 0.35f);
                _sparks.Emit(ep, Scaled(3));
            }
        }

        /// <summary>
        /// Enemy death: smoke puff + rising embers + a spark spray (bigger for
        /// bosses). Deaths used to under-sell themselves next to an ordinary hit;
        /// killing something should be the loudest thing on screen.
        /// </summary>
        public static void DeathBurst(Vector3 pos, bool boss)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams
            {
                startColor = new Color(0.16f, 0.14f, 0.15f, 0.75f),
            };
            _smoke.transform.position = pos + Vector3.up * 0.9f;
            _smoke.Emit(ep, Scaled(boss ? 30 : 16));
            Embers(pos + Vector3.up, boss ? 64 : 34);
            Sparks(pos + Vector3.up * 1.1f, new Color(1f, 0.55f, 0.3f), boss ? 30 : 18);
            if (boss) Nova(pos);
        }

        private class NovaTicker : MonoBehaviour
        {
            private void Update()
            {
                var dt = Time.deltaTime;
                if (_novaT > 0f)
                {
                    _novaT -= dt;
                    var t = 1f - _novaT / 0.4f;
                    _novaQuad.localScale = Vector3.one * Mathf.Lerp(1.5f, 12f, t);
                    _novaMat.color = new Color(1f, 0.45f, 0.28f, 0.9f * (1f - t));
                    if (_novaT <= 0f) _novaQuad.gameObject.SetActive(false);
                }
                for (var i = 0; i < SlashCount; i++)
                {
                    if (SlashT[i] <= 0f) continue;
                    SlashT[i] -= dt;
                    var k = 1f - SlashT[i] / 0.16f;
                    SlashQuads[i].localScale = Vector3.one * Mathf.Lerp(
                        SlashQuads[i].localScale.x, SlashQuads[i].localScale.x + 1.2f * dt * 8f, 0.5f);
                    var c = SlashMats[i].color;
                    SlashMats[i].color = new Color(c.r, c.g, c.b, Mathf.Max(0f, 0.9f * (1f - k)));
                    if (SlashT[i] <= 0f) SlashQuads[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
