using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Physical surface types. A material is described by what it *is* — leather,
    /// steel, wet stone — not by hand-tuned sliders per object.
    /// </summary>
    public enum Surface
    {
        Skin, Cloth, Leather, Steel, DarkMetal, Wood, Stone, WetStone,
        Water, Foliage, Rope, Emissive, Ghost,
    }

    /// <summary>
    /// The material system for the realistic pass. Everything in the game asks
    /// SurfaceKit for a material by surface type and colour, so the look is
    /// defined in one table instead of scattered across every builder.
    ///
    /// Values are plausible-PBR rather than measured: mobile forward rendering
    /// with no reflection probes cannot pay for true metals, so metallic is used
    /// sparingly and smoothness carries most of the read.
    /// </summary>
    public static class SurfaceKit
    {
        public struct Profile
        {
            public float smoothness;
            public float metallic;
            public float wear;        // procedural grime strength
            public float wearScale;
            public float rim;
            public Color specTint;
        }

        /// <summary>The look-up table that defines the game's material language.</summary>
        public static readonly Dictionary<Surface, Profile> Profiles = new()
        {
            // Skin is soft and slightly oily — low smoothness, no metal, faint rim
            // so faces read against dark backgrounds without glowing.
            [Surface.Skin] = new Profile { smoothness = 0.22f, metallic = 0f, wear = 0.06f,
                wearScale = 6f, rim = 0.22f, specTint = new Color(1f, 0.93f, 0.88f) },
            // Woven cloth: broad, dull highlight and visible dirt.
            [Surface.Cloth] = new Profile { smoothness = 0.13f, metallic = 0f, wear = 0.30f,
                wearScale = 4f, rim = 0.16f, specTint = new Color(0.9f, 0.9f, 0.92f) },
            [Surface.Leather] = new Profile { smoothness = 0.30f, metallic = 0f, wear = 0.38f,
                wearScale = 3.2f, rim = 0.18f, specTint = new Color(0.95f, 0.9f, 0.84f) },
            // Worn steel: bright but scratched, so smoothness stays off the ceiling.
            [Surface.Steel] = new Profile { smoothness = 0.62f, metallic = 0.85f, wear = 0.30f,
                wearScale = 5f, rim = 0.30f, specTint = Color.white },
            [Surface.DarkMetal] = new Profile { smoothness = 0.42f, metallic = 0.75f, wear = 0.42f,
                wearScale = 4.5f, rim = 0.22f, specTint = new Color(0.82f, 0.84f, 0.9f) },
            [Surface.Wood] = new Profile { smoothness = 0.18f, metallic = 0f, wear = 0.40f,
                wearScale = 2.6f, rim = 0.12f, specTint = new Color(0.92f, 0.86f, 0.75f) },
            [Surface.Stone] = new Profile { smoothness = 0.11f, metallic = 0f, wear = 0.45f,
                wearScale = 2.2f, rim = 0.10f, specTint = new Color(0.88f, 0.88f, 0.9f) },
            // Rain-slick rooftops: the sheen is what sells a night exterior.
            // Kept deliberately below a mirror finish: on a large flat deck a high
            // smoothness collapses into one blown highlight rather than a sheen.
            [Surface.WetStone] = new Profile { smoothness = 0.34f, metallic = 0.02f, wear = 0.34f,
                wearScale = 2.6f, rim = 0.20f, specTint = new Color(0.86f, 0.9f, 1f) },
            [Surface.Water] = new Profile { smoothness = 0.85f, metallic = 0.02f, wear = 0f,
                wearScale = 1f, rim = 0.45f, specTint = new Color(0.8f, 0.9f, 1f) },
            [Surface.Foliage] = new Profile { smoothness = 0.16f, metallic = 0f, wear = 0.22f,
                wearScale = 5f, rim = 0.30f, specTint = new Color(0.85f, 0.95f, 0.8f) },
            [Surface.Rope] = new Profile { smoothness = 0.10f, metallic = 0f, wear = 0.45f,
                wearScale = 7f, rim = 0.12f, specTint = Color.white },
            [Surface.Emissive] = new Profile { smoothness = 0.4f, metallic = 0f, wear = 0f,
                wearScale = 1f, rim = 0f, specTint = Color.white },
            [Surface.Ghost] = new Profile { smoothness = 0.5f, metallic = 0f, wear = 0f,
                wearScale = 1f, rim = 0.6f, specTint = Color.white },
        };

        private static Shader _surface;

        public static Shader SurfaceShader =>
            _surface != null ? _surface
                : _surface = Shader.Find("Emberline/Surface") ?? Shader.Find("Standard");

        /// <summary>Apply a surface profile to an existing material.</summary>
        public static void Apply(Material mat, Surface surface, Color albedo)
        {
            if (mat == null) return;
            var p = Profiles[surface];
            mat.color = albedo;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", p.smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", p.metallic);
            if (mat.HasProperty("_WearStrength")) mat.SetFloat("_WearStrength", p.wear);
            if (mat.HasProperty("_WearScale")) mat.SetFloat("_WearScale", p.wearScale);
            if (mat.HasProperty("_RimStrength")) mat.SetFloat("_RimStrength", p.rim);
            if (mat.HasProperty("_SpecTint")) mat.SetColor("_SpecTint", p.specTint);
        }

        /// <summary>Runtime material for a surface. Callers should cache the result.</summary>
        public static Material Make(Surface surface, Color albedo)
        {
            var mat = new Material(SurfaceShader);
            Apply(mat, surface, albedo);
            return mat;
        }

        /// <summary>
        /// Desaturate and darken an authored colour toward the cinematic palette.
        /// The old look leaned on saturated ink-blue and ember-orange; realistic
        /// night exteriors sit far lower in both saturation and value.
        /// </summary>
        public static Color Grade(Color c, float desaturate = 0.35f, float value = 0.9f)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            return Color.HSVToRGB(h, s * (1f - desaturate), v * value);
        }
    }
}
