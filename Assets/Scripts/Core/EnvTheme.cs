using UnityEngine;

namespace Emberline.Core
{
    /// <summary>The ten places the game is set.</summary>
    public enum EnvThemeId
    {
        Village, Forest, Bamboo, Mountain, Temple,
        Castle, Fortress, Graveyard, BurningVillage, RainyBattlefield,
        // Appended, never reordered: EnvThemeId is serialised by value.
        VillageDawn,
    }

    /// <summary>Ambient life and weather a place carries.</summary>
    public enum Weather { Clear, Rain, Snow, Ash, Mist }
    public enum AmbientLife { None, Fireflies, Leaves, Petals, Embers, Dust, Crows }

    /// <summary>
    /// One environment theme as data: light, fog, palette, weather and ambience.
    /// The arena builder composes from this rather than branching on a scene name,
    /// so a new place is a row in the table, not another if-else in BuildArena.
    /// </summary>
    public struct EnvTheme
    {
        public string displayName;
        public Color keyLight, fillLight, rimLight;
        public float keyIntensity;

        /// <summary>Procedural-skybox exposure. 0 = use the night default (0.45).</summary>
        public float skyExposure;
        public Color ambientSky, ambientEquator, ambientGround;
        public Color fogColor;
        public float fogDensity;
        public Color ground, structure, accent;
        public Weather weather;
        public AmbientLife life;
        public float windStrength;
        public string ambienceClip;   // Resources/Art/Audio/...
        public bool warmPractical;    // lantern colour: warm fire vs cold spirit

        /// <summary>Surface the ground reads as — drives the material profile.</summary>
        public Surface groundSurface;
    }

    /// <summary>
    /// The theme table. Values are authored for a night-leaning cinematic look,
    /// consistent with the grade: low saturation, dense-ish fog, warm practicals
    /// against cool ambient.
    /// </summary>
    public static class EnvThemes
    {
        public static EnvTheme Get(EnvThemeId id) => id switch
        {
            EnvThemeId.Village => new EnvTheme
            {
                displayName = "YORUNE VILLAGE",
                keyLight = new Color(0.55f, 0.64f, 0.86f), keyIntensity = 1.05f,
                fillLight = new Color(0.20f, 0.26f, 0.40f), rimLight = new Color(0.55f, 0.48f, 0.42f),
                ambientSky = new Color(0.14f, 0.17f, 0.25f),
                ambientEquator = new Color(0.10f, 0.115f, 0.15f),
                ambientGround = new Color(0.07f, 0.055f, 0.05f),
                fogColor = new Color(0.055f, 0.065f, 0.09f), fogDensity = 0.022f,
                ground = new Color(0.15f, 0.18f, 0.23f), structure = new Color(0.20f, 0.24f, 0.30f),
                accent = new Color(0.62f, 0.22f, 0.16f),
                weather = Weather.Clear, life = AmbientLife.Fireflies, windStrength = 0.35f,
                ambienceClip = "village_ambience", warmPractical = true,
                groundSurface = Surface.WetStone,
            },
            EnvThemeId.Forest => new EnvTheme
            {
                displayName = "THE DEEP WOOD",
                keyLight = new Color(0.42f, 0.55f, 0.62f), keyIntensity = 0.75f,
                fillLight = new Color(0.16f, 0.24f, 0.22f), rimLight = new Color(0.40f, 0.48f, 0.38f),
                ambientSky = new Color(0.10f, 0.15f, 0.13f),
                ambientEquator = new Color(0.08f, 0.11f, 0.09f),
                ambientGround = new Color(0.05f, 0.06f, 0.04f),
                fogColor = new Color(0.05f, 0.08f, 0.06f), fogDensity = 0.045f,
                ground = new Color(0.13f, 0.16f, 0.11f), structure = new Color(0.18f, 0.15f, 0.11f),
                accent = new Color(0.30f, 0.42f, 0.24f),
                weather = Weather.Mist, life = AmbientLife.Leaves, windStrength = 0.6f,
                ambienceClip = "forest_ambience", warmPractical = true,
                groundSurface = Surface.Foliage,
            },
            EnvThemeId.Bamboo => new EnvTheme
            {
                displayName = "THE BAMBOO SEA",
                keyLight = new Color(0.52f, 0.66f, 0.60f), keyIntensity = 0.9f,
                fillLight = new Color(0.18f, 0.28f, 0.24f), rimLight = new Color(0.55f, 0.62f, 0.45f),
                ambientSky = new Color(0.12f, 0.18f, 0.15f),
                ambientEquator = new Color(0.09f, 0.13f, 0.11f),
                ambientGround = new Color(0.06f, 0.07f, 0.05f),
                fogColor = new Color(0.06f, 0.09f, 0.07f), fogDensity = 0.038f,
                ground = new Color(0.14f, 0.17f, 0.12f), structure = new Color(0.34f, 0.38f, 0.20f),
                accent = new Color(0.52f, 0.58f, 0.28f),
                weather = Weather.Clear, life = AmbientLife.Leaves, windStrength = 0.9f,
                ambienceClip = "bamboo_ambience", warmPractical = true,
                groundSurface = Surface.Foliage,
            },
            EnvThemeId.Mountain => new EnvTheme
            {
                displayName = "THE COLD PASS",
                keyLight = new Color(0.66f, 0.74f, 0.92f), keyIntensity = 1.2f,
                fillLight = new Color(0.24f, 0.30f, 0.42f), rimLight = new Color(0.62f, 0.68f, 0.80f),
                ambientSky = new Color(0.18f, 0.22f, 0.30f),
                ambientEquator = new Color(0.13f, 0.15f, 0.19f),
                ambientGround = new Color(0.10f, 0.11f, 0.13f),
                fogColor = new Color(0.10f, 0.12f, 0.16f), fogDensity = 0.05f,
                ground = new Color(0.22f, 0.24f, 0.28f), structure = new Color(0.26f, 0.28f, 0.32f),
                accent = new Color(0.50f, 0.56f, 0.66f),
                weather = Weather.Snow, life = AmbientLife.None, windStrength = 1.2f,
                ambienceClip = "mountain_wind", warmPractical = true,
                groundSurface = Surface.Stone,
            },
            EnvThemeId.Temple => new EnvTheme
            {
                displayName = "THE STILL TEMPLE",
                keyLight = new Color(0.60f, 0.58f, 0.66f), keyIntensity = 0.85f,
                fillLight = new Color(0.22f, 0.20f, 0.28f), rimLight = new Color(0.70f, 0.55f, 0.36f),
                ambientSky = new Color(0.13f, 0.13f, 0.17f),
                ambientEquator = new Color(0.11f, 0.10f, 0.12f),
                ambientGround = new Color(0.09f, 0.07f, 0.05f),
                fogColor = new Color(0.07f, 0.06f, 0.08f), fogDensity = 0.020f,
                ground = new Color(0.19f, 0.17f, 0.16f), structure = new Color(0.28f, 0.22f, 0.18f),
                accent = new Color(0.72f, 0.45f, 0.20f),
                weather = Weather.Clear, life = AmbientLife.Dust, windStrength = 0.15f,
                ambienceClip = "temple_ambience", warmPractical = true,
                groundSurface = Surface.Stone,
            },
            EnvThemeId.Castle => new EnvTheme
            {
                displayName = "THE HIGH KEEP",
                keyLight = new Color(0.50f, 0.58f, 0.80f), keyIntensity = 1f,
                fillLight = new Color(0.18f, 0.22f, 0.34f), rimLight = new Color(0.58f, 0.50f, 0.42f),
                ambientSky = new Color(0.12f, 0.15f, 0.22f),
                ambientEquator = new Color(0.10f, 0.11f, 0.15f),
                ambientGround = new Color(0.07f, 0.06f, 0.06f),
                fogColor = new Color(0.06f, 0.07f, 0.10f), fogDensity = 0.026f,
                ground = new Color(0.20f, 0.21f, 0.24f), structure = new Color(0.24f, 0.25f, 0.29f),
                accent = new Color(0.55f, 0.20f, 0.18f),
                weather = Weather.Clear, life = AmbientLife.Crows, windStrength = 0.7f,
                ambienceClip = "castle_ambience", warmPractical = true,
                groundSurface = Surface.Stone,
            },
            EnvThemeId.Fortress => new EnvTheme
            {
                displayName = "THE IRON FORT",
                keyLight = new Color(0.46f, 0.50f, 0.66f), keyIntensity = 0.95f,
                fillLight = new Color(0.16f, 0.18f, 0.26f), rimLight = new Color(0.66f, 0.42f, 0.28f),
                ambientSky = new Color(0.11f, 0.12f, 0.16f),
                ambientEquator = new Color(0.09f, 0.09f, 0.11f),
                ambientGround = new Color(0.08f, 0.06f, 0.04f),
                fogColor = new Color(0.05f, 0.055f, 0.07f), fogDensity = 0.030f,
                ground = new Color(0.17f, 0.17f, 0.19f), structure = new Color(0.21f, 0.20f, 0.22f),
                accent = new Color(0.68f, 0.30f, 0.14f),
                weather = Weather.Clear, life = AmbientLife.Embers, windStrength = 0.4f,
                ambienceClip = "fortress_ambience", warmPractical = true,
                groundSurface = Surface.DarkMetal,
            },
            EnvThemeId.Graveyard => new EnvTheme
            {
                displayName = "THE DROWNED FIELD",
                // Readability beats mood in a place you fight in: at key 0.7 with
                // fog 0.055 the arena floor and the player silhouette merged.
                keyLight = new Color(0.42f, 0.54f, 0.60f), keyIntensity = 0.95f,
                fillLight = new Color(0.16f, 0.25f, 0.27f), rimLight = new Color(0.36f, 0.60f, 0.52f),
                ambientSky = new Color(0.12f, 0.16f, 0.17f),
                ambientEquator = new Color(0.09f, 0.12f, 0.12f),
                ambientGround = new Color(0.06f, 0.09f, 0.08f),
                fogColor = new Color(0.05f, 0.08f, 0.08f), fogDensity = 0.038f,
                ground = new Color(0.14f, 0.17f, 0.16f), structure = new Color(0.22f, 0.24f, 0.23f),
                accent = new Color(0.35f, 0.70f, 0.58f),
                weather = Weather.Mist, life = AmbientLife.Fireflies, windStrength = 0.25f,
                ambienceClip = "marsh_ambience", warmPractical = false, // cold spirit light
                groundSurface = Surface.WetStone,
            },
            EnvThemeId.BurningVillage => new EnvTheme
            {
                displayName = "THE BURNING ROW",
                keyLight = new Color(0.86f, 0.50f, 0.28f), keyIntensity = 1.15f,
                fillLight = new Color(0.34f, 0.16f, 0.10f), rimLight = new Color(0.95f, 0.55f, 0.25f),
                ambientSky = new Color(0.26f, 0.14f, 0.08f),
                ambientEquator = new Color(0.20f, 0.11f, 0.07f),
                ambientGround = new Color(0.16f, 0.08f, 0.04f),
                fogColor = new Color(0.16f, 0.08f, 0.05f), fogDensity = 0.048f,
                ground = new Color(0.20f, 0.15f, 0.12f), structure = new Color(0.24f, 0.16f, 0.12f),
                accent = new Color(1f, 0.48f, 0.18f),
                weather = Weather.Ash, life = AmbientLife.Embers, windStrength = 0.8f,
                ambienceClip = "fire_ambience", warmPractical = true,
                groundSurface = Surface.Stone,
            },
            EnvThemeId.VillageDawn => new EnvTheme
            {
                // The only daylight in the game. It exists for one scene — the
                // morning before the village burns — and everything about it is
                // the inverse of the palette the rest of the game uses.
                displayName = "YORUNE, MORNING",
                keyLight = new Color(1f, 0.94f, 0.82f), keyIntensity = 1.45f,
                fillLight = new Color(0.46f, 0.54f, 0.64f), rimLight = new Color(0.98f, 0.86f, 0.66f),
                ambientSky = new Color(0.46f, 0.53f, 0.62f),
                ambientEquator = new Color(0.38f, 0.37f, 0.34f),
                ambientGround = new Color(0.26f, 0.22f, 0.17f),
                // Enough haze to soften the ridge line without greying the scene:
                // at 0.010 the ground plane ran to a razor-sharp horizon.
                fogColor = new Color(0.68f, 0.72f, 0.76f), fogDensity = 0.013f,
                skyExposure = 1.15f,   // the one scene with a sky worth seeing
                ground = new Color(0.42f, 0.36f, 0.28f), structure = new Color(0.52f, 0.44f, 0.34f),
                accent = new Color(0.85f, 0.55f, 0.30f),
                weather = Weather.Clear, life = AmbientLife.Petals, windStrength = 0.3f,
                ambienceClip = "village_ambience", warmPractical = true,
                groundSurface = Surface.Wood,
            },
            _ => new EnvTheme // RainyBattlefield
            {
                displayName = "THE RAIN FIELD",
                keyLight = new Color(0.44f, 0.52f, 0.68f), keyIntensity = 0.8f,
                fillLight = new Color(0.16f, 0.20f, 0.28f), rimLight = new Color(0.50f, 0.56f, 0.68f),
                ambientSky = new Color(0.11f, 0.13f, 0.17f),
                ambientEquator = new Color(0.09f, 0.10f, 0.13f),
                ambientGround = new Color(0.07f, 0.07f, 0.08f),
                fogColor = new Color(0.07f, 0.08f, 0.10f), fogDensity = 0.052f,
                ground = new Color(0.16f, 0.17f, 0.19f), structure = new Color(0.20f, 0.21f, 0.24f),
                accent = new Color(0.48f, 0.52f, 0.60f),
                weather = Weather.Rain, life = AmbientLife.None, windStrength = 1.0f,
                ambienceClip = "rain_ambience", warmPractical = true,
                groundSurface = Surface.WetStone,
            },
        };
    }
}
