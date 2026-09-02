using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the twelve Combat 2.0 acceptance scenarios:
    /// -executeMethod Emberline.EditorTools.EmberCombatScenarios.Run (no -quit).
    /// </summary>
    public static class EmberCombatScenarios
    {
        private const string Armed = "emberline.scenarios.armed";

        [MenuItem("Emberline/Run Combat Scenarios")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Core.Session.Mode = Core.LaunchMode.None;
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void AfterReload()
        {
            if (!SessionState.GetBool(Armed, false)) return;
            EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode) Attach(); };
            if (EditorApplication.isPlaying) Attach();
        }

        private static bool _attached;

        private static void Attach()
        {
            if (_attached) return;
            _attached = true;
            var d = new GameObject("CombatScenarioDriver").AddComponent<CombatScenarioDriver>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[SCN] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
