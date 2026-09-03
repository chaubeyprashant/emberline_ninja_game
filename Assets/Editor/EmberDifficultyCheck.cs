using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the Difficulty 2.0 A/B probe (the same enemy under all four levels):
    /// -executeMethod Emberline.EditorTools.EmberDifficultyCheck.Run (no -quit).
    /// </summary>
    public static class EmberDifficultyCheck
    {
        private const string Armed = "emberline.difficulty.armed";

        [MenuItem("Emberline/Check Difficulty 2.0")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.None;
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
            var d = new GameObject("DifficultyProbe").AddComponent<DifficultyProbeDriver>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[DIFF] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
