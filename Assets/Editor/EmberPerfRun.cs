using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the perf tour (<see cref="PerfRun"/>) in the editor with short samples.
    /// The numbers mean nothing on a Mac; this proves the tour reaches every
    /// scenario and emits a line for each before a phone is plugged in. Writes
    /// <c>Logs/perf_editor.jsonl</c> and exits with the run's code in batch mode.
    /// </summary>
    public static class EmberPerfRun
    {
        private const string Armed = "emberline.perfrun.armed";

        [MenuItem("Emberline/Run Perf Tour (Editor)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.None;
            AudioListener.volume = 0f;
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void AfterReload()
        {
            if (!SessionState.GetBool(Armed, false)) return;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode) Attach();
            };
            if (EditorApplication.isPlaying) Attach();
        }

        private static bool _attached;

        private static void Attach()
        {
            if (_attached) return;
            _attached = true;
            var run = PerfRun.Begin("quick");
            run.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"{PerfRun.Tag} editor exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
