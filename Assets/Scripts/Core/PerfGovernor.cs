using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Decides what frame rate the game is allowed to run at, from context and
    /// from what the device says about its own temperature.
    ///
    /// The game used to pin targetFrameRate to 60 for its entire lifetime, so a
    /// full 3D arena — shadows, fog, embers and the grade pass — was redrawn sixty
    /// times a second behind every menu, briefing, pause overlay and cinematic.
    /// Nothing on those screens moves fast enough to need it, and a phone has
    /// nowhere to put that heat.
    ///
    /// Two rules:
    ///   * Context. Only live gameplay gets the full rate; everything else is 30.
    ///   * Thermals. Android reports its own thermal pressure from API 29. When
    ///     the device says it is struggling we cap to 30 and, if it keeps
    ///     climbing, drop the graphics tier — before the OS throttles the CPU and
    ///     the game stutters instead of merely looking softer.
    /// </summary>
    public class PerfGovernor : MonoBehaviour
    {
        /// <summary>Frame cap for menus, pause, results and cinematics.</summary>
        private const int IdleFps = 30;

        /// <summary>How often to ask the OS about heat. Cheap, but not free.</summary>
        private const float ThermalPollSeconds = 5f;

        public static PerfGovernor Instance { get; private set; }

        /// <summary>Player's preferred rate during gameplay. 30 or 60.</summary>
        public static int GameplayFps
        {
            get
            {
                var stored = PlayerPrefs.GetInt("fps_cap", 0);
                if (stored == 30 || stored == 60) return stored;
                // Unset: only the high tier opts into 60 by default. Measured on a
                // Galaxy A33 (Exynos 1280, the mid-range this game is aimed at),
                // 60fps costs 86-102% CPU against 45-52% at 30 — roughly double,
                // sustained, with nowhere for the heat to go. A mid-range phone
                // should not run hot out of the box to buy frames its owner never
                // asked for; anyone who wants them can say so in Settings.
                return PlayerPrefs.GetInt("gfx_tier", 1) >= 2 ? 60 : 30;
            }
            set
            {
                PlayerPrefs.SetInt("fps_cap", value == 30 ? 30 : 60);
                PlayerPrefs.Save();
                Instance?.Apply(true);
            }
        }

        /// <summary>0 none, 1 capped to 30, 2 also forced to the low tier.</summary>
        public static int ThermalStep { get; private set; }

        /// <summary>Set when the governor has stepped down, for a one-off HUD note.</summary>
        public static bool ThermalNoticePending { get; set; }

        private float _poll;
        private int _applied = -1;
        private GameManager _gm;

        public static void Ensure(GameObject host)
        {
            if (Instance != null) return;
            Instance = host.GetComponent<PerfGovernor>() ?? host.AddComponent<PerfGovernor>();
        }

        private void OnEnable()
        {
            Instance = this;
            // Lives on the GameManager's object; State is an instance property.
            _gm = GetComponent<GameManager>();
        }

        private void Update()
        {
            if ((_poll -= Time.unscaledDeltaTime) <= 0f)
            {
                _poll = ThermalPollSeconds;
                PollThermal();
            }
            Apply(false);
        }

        /// <summary>Choose and set the cap. Cheap enough to call every frame.</summary>
        private void Apply(bool force)
        {
            var busy = _gm != null
                       && _gm.State == GameManager.Phase.Playing
                       && !GameManager.CinematicActive
                       && !Player.CombatController.TimeFrozen;

            var want = busy ? GameplayFps : IdleFps;
            if (ThermalStep > 0) want = Mathf.Min(want, IdleFps);

            if (!force && want == _applied) return;
            _applied = want;
            Application.targetFrameRate = want;
        }

        /// <summary>
        /// Ask Android how hot it is. Returns silently on anything below API 29 or
        /// on any device that does not implement it — a missing thermal service
        /// must never be worse than the old always-60 behaviour.
        /// </summary>
        private void PollThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var power = activity.Call<AndroidJavaObject>("getSystemService", "power");
                if (power == null) return;
                // PowerManager.THERMAL_STATUS_*: 0 NONE, 1 LIGHT, 2 MODERATE,
                // 3 SEVERE, 4 CRITICAL, 5 EMERGENCY, 6 SHUTDOWN.
                var status = power.Call<int>("getCurrentThermalStatus");
                var step = status >= 3 ? 2 : status >= 2 ? 1 : 0;
                if (step == ThermalStep) return;

                // Only ever announce getting worse; recovering should be quiet.
                if (step > ThermalStep) ThermalNoticePending = true;
                ThermalStep = step;

                if (step >= 2 && UI.EmberHud.GraphicsTier > 0) UI.EmberHud.GraphicsTier = 0;
                Apply(true);
            }
            catch
            {
                // getCurrentThermalStatus is API 29+; min SDK is 26. A device that
                // cannot answer simply never steps down.
            }
#endif
        }
    }
}
