using Emberline.Core;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// The mountain village at three points in one night. Peace, attack and ruin
    /// share one set: the same houses either stand or are replaced by their
    /// rubble, torches become fires, and the theme carries the rest. One set means
    /// no load between scenes 1 and 3 and roughly half the geometry on a phone.
    /// </summary>
    public class VillageSet : MonoBehaviour
    {
        public static VillageSet Active { get; private set; }

        [Tooltip("Intact houses and warm props. Hidden once the village burns.")]
        public GameObject peaceGroup;

        [Tooltip("Rubble, tipped carts, scorch. Revealed for Attack and Ruin.")]
        public GameObject ruinGroup;

        [Tooltip("Fire emitters, lit during Attack and Ruin.")]
        public GameObject fireGroup;

        public SetState State { get; private set; } = SetState.Peace;

        private void OnEnable() => Active = this;
        private void OnDisable() { if (Active == this) Active = null; }

        public void Apply(SetState state)
        {
            if (state == SetState.Unchanged) return;
            State = state;

            var peaceful = state == SetState.Peace;
            if (peaceGroup != null) peaceGroup.SetActive(peaceful);
            if (ruinGroup != null) ruinGroup.SetActive(!peaceful);
            if (fireGroup != null) fireGroup.SetActive(!peaceful);

            // The theme carries light, fog and falling ash; the set only carries
            // geometry. Keeps the look in one table rather than split across both.
            var theme = EnvThemes.Get(peaceful ? EnvThemeId.VillageDawn : EnvThemeId.BurningVillage);
            if (state == SetState.Attack)
            {
                // Mid-attack: the fires have caught but the air has not filled yet.
                theme.fogDensity *= 0.6f;
                theme.keyIntensity *= 1.1f;
            }
            Atmosphere.Apply(theme, SceneRefs.Cam != null ? SceneRefs.Cam.transform : transform);
            ApplyLighting(theme);
        }

        /// <summary>Runtime twin of the bootstrap's BuildLighting, by light name.</summary>
        private static void ApplyLighting(EnvTheme theme)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.ambientSky;
            RenderSettings.ambientEquatorColor = theme.ambientEquator;
            RenderSettings.ambientGroundColor = theme.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;

            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                switch (l.name)
                {
                    case "KeyLight": l.color = theme.keyLight; l.intensity = theme.keyIntensity; break;
                    case "FillLight": l.color = theme.fillLight; break;
                    case "RimLight": l.color = theme.rimLight; break;
                }
            }
        }
    }
}
