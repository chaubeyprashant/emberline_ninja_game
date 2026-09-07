using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Captures the marketing reel's source footage from real play:
    /// -executeMethod Emberline.EditorTools.EmberReelCapture.Run (no -quit).
    ///
    /// Optional arguments: -reelShot N (one shot only), -reelSeconds S (shorter
    /// takes, for a framing check), -reelOut DIR.
    /// </summary>
    public static class EmberReelCapture
    {
        private const string Armed = "emberline.reel.armed";
        private const string ShotKey = "emberline.reel.shot";
        private const string SecondsKey = "emberline.reel.seconds";
        private const string OutKey = "emberline.reel.out";

        [MenuItem("Emberline/Capture Reel Footage")]
        public static void Run()
        {
            var shot = -1;
            var seconds = -1f;
            var outRoot = "Logs/reel";
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-reelShot") int.TryParse(args[i + 1], out shot);
                if (args[i] == "-reelSeconds") float.TryParse(args[i + 1], out seconds);
                if (args[i] == "-reelOut") outRoot = args[i + 1];
            }
            SessionState.SetInt(ShotKey, shot);
            SessionState.SetFloat(SecondsKey, seconds);
            SessionState.SetString(OutKey, outRoot);

            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.Story;
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
            ReelDirector.OnlyShot = SessionState.GetInt(ShotKey, -1);
            ReelDirector.SecondsOverride = SessionState.GetFloat(SecondsKey, -1f);
            ReelDirector.OutRoot = SessionState.GetString(OutKey, "Logs/reel");
            var d = new GameObject("ReelDirector").AddComponent<ReelDirector>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[REEL] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
