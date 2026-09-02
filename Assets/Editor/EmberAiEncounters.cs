using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the play-mode encounter harness from the command line:
    /// -executeMethod Emberline.EditorTools.EmberAiEncounters.Run (no -quit; the
    /// harness exits the editor itself with 0 = all scenarios passed).
    /// Entering play mode reloads the domain, so the "harness armed" flag lives
    /// in SessionState and the driver is attached after the reload.
    /// </summary>
    public static class EmberAiEncounters
    {
        private const string Armed = "emberline.encounters.armed";
        private const string Started = "emberline.encounters.started";
        private const string FilterKey = "emberline.encounters.filter";

        [MenuItem("Emberline/Run AI Encounters")]
        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-encFilter") AiEncounterDriver.Filter = args[i + 1];
            SessionState.SetString(FilterKey, AiEncounterDriver.Filter ?? "");
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Core.Session.Mode = Core.LaunchMode.None;
            AudioListener.volume = 0f; // no speaker noise from a headless run
            SessionState.SetBool(Armed, true);
            SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void AfterReload()
        {
            if (!SessionState.GetBool(Armed, false)) return;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.update += Watchdog;
            if (EditorApplication.isPlaying) Attach();
        }

        private static void OnState(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) Attach();
        }

        private static bool _attached;

        private static void Attach()
        {
            if (_attached) return;
            _attached = true;
            var f = SessionState.GetString(FilterKey, "");
            AiEncounterDriver.Filter = string.IsNullOrEmpty(f) ? null : f;
            var go = new GameObject("AiEncounterDriver");
            var d = go.AddComponent<AiEncounterDriver>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[ENC] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }

        private static void Watchdog()
        {
            var started = SessionState.GetFloat(Started, 0f);
            if (EditorApplication.timeSinceStartup - started > 900)
            {
                Debug.LogError("[ENC] watchdog: harness did not finish in 15 minutes");
                SessionState.EraseBool(Armed);
                if (Application.isBatchMode) EditorApplication.Exit(3);
            }
        }
    }
}
