using UnityEngine;
using UnityEngine.Rendering;

namespace Emberline.Core
{
    /// <summary>
    /// Distance culling by layer, and who casts shadows.
    ///
    /// <para>
    /// The valley bake is ~2,000 renderers; about 550 of them are grass and
    /// another 500 bushes, flowers, mushrooms and pebbles. Nobody reads a tuft of
    /// grass at 80 m, but the camera drew every one inside its 600 m far plane.
    /// Scattered props are sorted onto two layers whose cull distances the
    /// camera applies per renderer (spherical, so turning the camera does not
    /// pop them): <see cref="Detail"/> for ground cover, <see cref="Props"/> for
    /// small solid clutter. Trees, buildings and terrain stay on Default and are
    /// seen to the horizon.
    /// </para>
    ///
    /// <para>
    /// Ground cover under 1.5 m also stops casting shadows: a bush's shadow is a
    /// shadow-map draw the size of a dinner plate.
    /// </para>
    /// </summary>
    public static class RenderLayers
    {
        public const int Detail = 8;
        public const int Props = 9;

        /// <summary>Sort one placed prop. Call after its collider is decided.</summary>
        public static void Classify(GameObject go, bool solid)
        {
            if (!Bounds(go, out var b)) return;
            var size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            int layer;
            if (!solid && size < 3f) layer = Detail;
            else if (size < 2.2f) layer = Props;
            else return;   // trees, rocks big enough to hide behind, buildings
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            if (b.size.y < 1.5f)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Cull distances per graphics tier, metres from the camera.</summary>
        public static void Apply(Camera cam, int tier)
        {
            if (cam == null) return;
            var d = new float[32];
            d[Detail] = tier == 0 ? 28f : tier == 1 ? 42f : 60f;
            d[Props] = tier == 0 ? 55f : tier == 1 ? 85f : 120f;
            cam.layerCullDistances = d;
            cam.layerCullSpherical = true;
        }

        private static bool Bounds(GameObject go, out Bounds b)
        {
            b = default;
            var any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}
