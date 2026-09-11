using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// Three lights answering each other across the valley.
    ///
    /// <para>
    /// The enemy's signal network, shown rather than explained: one lantern
    /// lights, a second answers, a third answers further out. That is the whole
    /// component — it exists because <c>LanternPost</c> is a destructible prop
    /// that is lit or broken, with no notion of a sequence, and the reveal needs
    /// timing more than it needs a system.
    /// </para>
    ///
    /// <para>
    /// Deliberately tiny. It lights points in order on a timer and then stops.
    /// Nothing reads its state, nothing else drives it, and it is gone with the
    /// mission.
    /// </para>
    /// </summary>
    public class LanternSignal : MonoBehaviour
    {
        private Vector3[] _points;
        private float _interval = 1.15f;
        private float _t;
        private int _next;

        /// <summary>Light these in order, one every <paramref name="interval"/> seconds.</summary>
        public static LanternSignal Play(Vector3[] points, float interval = 1.15f)
        {
            var go = new GameObject("LanternSignal");
            var sig = go.AddComponent<LanternSignal>();
            sig._points = points;
            sig._interval = interval;
            sig._t = 0.4f;   // a beat of dark first, so the first light is an event
            return sig;
        }

        private void Update()
        {
            if (_points == null || _next >= _points.Length) return;
            if ((_t -= Time.deltaTime) > 0f) return;
            _t = _interval;
            Light(_points[_next++]);
        }

        private void Light(Vector3 at)
        {
            var pos = new Vector3(at.x, Core.Ground.HeightAt(at.x, at.z) + 2.6f, at.z);

            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bulb.GetComponent<Collider>());
            bulb.name = "SignalLantern";
            bulb.transform.SetParent(transform, false);
            bulb.transform.position = pos;
            bulb.transform.localScale = Vector3.one * 0.42f;
            var shader = Shader.Find("Emberline/Glow");
            var r = bulb.GetComponent<Renderer>();
            if (shader != null) r.material = new Material(shader) { color = new Color(1f, 0.62f, 0.28f) };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = bulb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.58f, 0.3f);
            light.intensity = 3.2f;
            light.range = 14f;
            // Vertex-lit for the same reason the road's lanterns are: the toon
            // shader's additive pass redraws every renderer a pixel light touches.
            light.renderMode = LightRenderMode.ForceVertex;

            UI.FxPools.Embers(pos, 8);
            Core.Sfx3D.Ui();
        }
    }
}
