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
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            _tm.color = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(_life / 0.35f));
        }
    }

    /// <summary>
    /// Pooled particle effects — two reusable ParticleSystems configured in code
    /// (Emberline/Glow material, radial soft sprites) fired via Emit().
    /// </summary>
    public static class FxPools
    {
        private static ParticleSystem _sparks, _embers;
        private static Transform _novaQuad;
        private static Material _novaMat;
        private static float _novaT;
        private static NovaTicker _ticker;

        private static void Ensure()
        {
            if (_sparks != null) return;
            var root = new GameObject("FxPools");
            Object.DontDestroyOnLoad(root);
            _sparks = MakeSystem(root.transform, "Sparks", 0.28f, 0.14f, 7f, 1.2f);
            _embers = MakeSystem(root.transform, "Embers", 0.55f, 0.2f, 4.5f, 0.7f);

            // Surge nova: one reusable expanding glow disc.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Nova";
            quad.transform.SetParent(root.transform, false);
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            _novaMat = new Material(Shader.Find("Emberline/Glow"));
            _novaMat.color = new Color(1f, 0.45f, 0.28f, 0.9f);
            quad.GetComponent<Renderer>().material = _novaMat;
            _novaQuad = quad.transform;
            quad.SetActive(false);
            _ticker = root.AddComponent<NovaTicker>();
        }

        private static ParticleSystem MakeSystem(Transform parent, string name,
            float life, float size, float speed, float gravity)
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
            main.maxParticles = 256;
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
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Emberline/Glow"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        public static void Sparks(Vector3 pos, Color color, int count = 8)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams { startColor = color };
            _sparks.transform.position = pos;
            _sparks.Emit(ep, count);
        }

        public static void Embers(Vector3 pos, int count = 14)
        {
            Ensure();
            var ep = new ParticleSystem.EmitParams { startColor = new Color(1f, 0.5f, 0.28f) };
            _embers.transform.position = pos;
            _embers.Emit(ep, count);
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

        private class NovaTicker : MonoBehaviour
        {
            private void Update()
            {
                if (_novaT <= 0f) return;
                _novaT -= Time.deltaTime;
                var t = 1f - _novaT / 0.4f;
                _novaQuad.localScale = Vector3.one * Mathf.Lerp(1.5f, 12f, t);
                _novaMat.color = new Color(1f, 0.45f, 0.28f, 0.9f * (1f - t));
                if (_novaT <= 0f) _novaQuad.gameObject.SetActive(false);
            }
        }
    }
}
