using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the bot through all ten story missions:
    /// -executeMethod Emberline.EditorTools.EmberMissionPlaythrough.Run (no -quit).
    /// </summary>
    public static class EmberMissionPlaythrough
    {
        private const string Armed = "emberline.playthrough.armed";
        private const string LevelKey = "emberline.playthrough.level";

        [MenuItem("Emberline/Play All Missions")]
        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-playLevel" && int.TryParse(args[i + 1], out var lv))
                    MissionPlaythroughDriver.OnlyLevel = lv;
                if (args[i] == "-playFrom" && int.TryParse(args[i + 1], out var from))
                    MissionPlaythroughDriver.FromLevel = from;
                if (args[i] == "-playTo" && int.TryParse(args[i + 1], out var to))
                    MissionPlaythroughDriver.ToLevel = to;
            }
            SessionState.SetInt(LevelKey, MissionPlaythroughDriver.OnlyLevel);
            SessionState.SetInt(LevelKey + ".from", MissionPlaythroughDriver.FromLevel);
            SessionState.SetInt(LevelKey + ".to", MissionPlaythroughDriver.ToLevel);
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.Story;
            Session.LevelIndex = 0;
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
            Session.Mode = LaunchMode.Story;
            Session.LevelIndex = 0;
            MissionPlaythroughDriver.OnlyLevel = SessionState.GetInt(LevelKey, -1);
            MissionPlaythroughDriver.FromLevel = SessionState.GetInt(LevelKey + ".from", 0);
            MissionPlaythroughDriver.ToLevel = SessionState.GetInt(LevelKey + ".to", 9999);
            var d = new GameObject("MissionPlaythrough").AddComponent<MissionPlaythroughDriver>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[PLAY] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
