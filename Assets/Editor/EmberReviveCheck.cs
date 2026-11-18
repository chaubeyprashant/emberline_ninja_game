using System.Collections;
using System.Reflection;
using Emberline.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Regression check for the Second Wind revive: start the first duel, kill
    /// Renzo, revive him as the rewarded ad does, and require that the rig left
    /// its death pose (the latch cleared, the animator out of the Dead state),
    /// he has health, and the game is playing again. Exits 0 on pass in batch mode.
    /// </summary>
    public static class EmberReviveCheck
    {
        private const string Armed = "emberline.revivecheck.armed";

        [MenuItem("Emberline/Check Revive")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.Duel;
            Session.DuelIndex = 0;
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
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Session.Mode = LaunchMode.Duel;
            Session.DuelIndex = 0;
            new GameObject("ReviveCheck").AddComponent<Driver>();
        }

        private static void Finish(int code, string why)
        {
            SessionState.EraseBool(Armed);
            Debug.Log($"[REVIVE] {(code == 0 ? "PASS" : "FAIL")} — {why}");
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else EditorApplication.ExitPlaymode();
        }

        private class Driver : MonoBehaviour
        {
            private IEnumerator Start()
            {
                DontDestroyOnLoad(gameObject);
                GameManager gm = null;
                var launched = false;
                var wait = Time.realtimeSinceStartup + 40f;
                while (Time.realtimeSinceStartup < wait)
                {
                    gm = SceneRefs.Game;
                    // The domain reload resets Session, so the scene boots to the menu:
                    // launch the duel from there, then tap through its briefing card.
                    if (gm != null && gm.State == GameManager.Phase.Menu && !launched) { launched = true; gm.LaunchDuel(0); }
                    if (gm != null && gm.State == GameManager.Phase.Intro) gm.BeginMission();
                    if (gm != null && gm.State == GameManager.Phase.Playing && gm.PlayerHealth != null) break;
                    yield return null;
                }
                if (gm == null || gm.State != GameManager.Phase.Playing) { Finish(2, $"duel never started (state {(gm != null ? gm.State.ToString() : "no GameManager")})"); yield break; }

                // Die the way a player does: damage through Health, which fires OnDeath.
                var hp = gm.PlayerHealth;
                hp.InvulnerableTimer = 0f;
                hp.Damage(hp.MaxHp * 10f, gm.PlayerT.position + Vector3.forward);
                yield return new WaitForSecondsRealtime(1.2f);
                if (gm.State != GameManager.Phase.Lost) { Finish(3, $"death did not end the fight (state {gm.State})"); yield break; }
                var rig = gm.PlayerT.GetComponentInChildren<SkeletalRig>();
                if (rig == null) { Finish(4, "no SkeletalRig on the player"); yield break; }
                var latch = typeof(SkeletalRig).GetField("_deadLatched", BindingFlags.NonPublic | BindingFlags.Instance);
                if (latch == null || !(bool)latch.GetValue(rig)) { Finish(5, "death did not latch the rig — the check no longer reproduces the bug"); yield break; }

                // The rewarded ad's success path.
                gm.RevivePlayer(0.5f);
                yield return new WaitForSecondsRealtime(1.5f);

                var anim = rig.GetComponentInChildren<Animator>();
                var inDead = anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("Dead");
                var stillLatched = (bool)latch.GetValue(rig);
                var ok = !stillLatched && !inDead && !hp.Dead && gm.State == GameManager.Phase.Playing;
                Finish(ok ? 0 : 1,
                    $"latched={stillLatched} animatorInDead={inDead} hp={hp.Hp:F0}/{hp.MaxHp:F0} state={gm.State} timeScale={Time.timeScale}");
            }
        }
    }
}
