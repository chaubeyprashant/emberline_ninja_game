using UnityEngine;
using Emberline.Core;

namespace Emberline.UI
{
    /// <summary>
    /// Scene-side hook that applies an environment theme at runtime. The bootstrap
    /// bakes the *choice* into the scene; the atmosphere itself (particles, wind,
    /// ambience) is built at play time so it obeys the current graphics tier.
    /// </summary>
    public class AtmosphereSpawner : MonoBehaviour
    {
        public EnvThemeId themeId = EnvThemeId.Village;

        private void Start()
        {
            // An endless run picks its own region and re-themes as the road moves,
            // so the scene's baked theme must stand aside. Start order between the
            // two is undefined, so this is a check rather than a race to apply.
            if (Session.Mode == LaunchMode.Endless) return;
            var cam = SceneRefs.Cam;
            Atmosphere.Apply(EnvThemes.Get(themeId), cam != null ? cam.transform : transform);
        }
    }
}
