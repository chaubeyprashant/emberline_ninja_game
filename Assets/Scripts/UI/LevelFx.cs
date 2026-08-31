using UnityEngine;

namespace Emberline.UI
{
    /// <summary>
    /// Per-level atmosphere built at runtime: rain streaks that follow the
    /// camera (Level 3) and the glowing footprint trail through the marsh
    /// (Level 5). Zero assets, mobile-cheap particle counts.
    /// </summary>
    public static class LevelFx
    {
        /// <summary>Stretched-billboard rain that follows the main camera.</summary>
        public static void EnableRain()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var go = new GameObject("RainFx");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0, 7f, 6f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.75f;
            main.startSpeed = 0f;
            main.gravityModifier = 5.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.06f);
            main.startColor = new Color(0.65f, 0.72f, 0.85f, 0.35f);
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 190f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 0.5f, 20f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Emberline/Glow"))
            {
                color = new Color(0.65f, 0.72f, 0.85f, 0.4f),
            };
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.055f;
            renderer.lengthScale = 0f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Rain closes the sky in.
            RenderSettings.fogEndDistance = Mathf.Min(RenderSettings.fogEndDistance, 32f);
        }

        /// <summary>Glowing footprints winding through the marsh (Act II opener).</summary>
        public static void SpawnFootprints()
        {
            var mat = new Material(Shader.Find("Emberline/Glow"))
            {
                color = new Color(0.55f, 0.95f, 0.7f, 0.6f),
            };
            var root = new GameObject("FootprintTrail");
            for (var i = 0; i < 14; i++)
            {
                var t = i / 13f;
                // S-curve from the player's spawn toward the far reeds.
                var x = Mathf.Lerp(-1f, 10.5f, t) + Mathf.Sin(t * 5.2f) * 2.4f;
                var z = Mathf.Lerp(-5.5f, 7f, t);
                var side = i % 2 == 0 ? 0.22f : -0.22f;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(quad.GetComponent<Collider>());
                quad.name = "Footprint";
                quad.transform.SetParent(root.transform, false);
                quad.transform.position = new Vector3(x + side, 0.03f, z);
                quad.transform.rotation = Quaternion.Euler(90f, Mathf.Atan2(10.5f, 12.5f) * Mathf.Rad2Deg, 0);
                quad.transform.localScale = new Vector3(0.22f, 0.34f, 1f);
                var r = quad.GetComponent<Renderer>();
                r.material = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
