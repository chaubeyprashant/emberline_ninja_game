using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Plays story level 1 headlessly with the scripted player, to prove the new
    /// AI behaves through the real mission and wave path and not merely in the
    /// encounter harness. Exits 0 when the fight ran clean.
    /// </summary>
    public static class EmberMissionSmoke
    {
        private const string Armed = "emberline.mission.armed";

        [MenuItem("Emberline/Run Mission Smoke Test")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.Duel;
            Session.DuelIndex = 0;
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
            // Session is reset by the domain reload, so restate the launch here.
            Session.Mode = LaunchMode.Duel;
            Session.DuelIndex = 0;
            var d = new GameObject("MissionSmokeDriver").AddComponent<MissionSmokeDriver>();
            d.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                Debug.Log($"[MIS] exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
