#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Development-only overlay: what every enemy is thinking. Toggle with F3
    /// in the editor or three-finger tap on a development build. Never
    /// compiled into a release.
    /// </summary>
    public class CombatDebugOverlay : MonoBehaviour
    {
        public static bool Enabled;
        private static CombatDebugOverlay _instance;
        private GUIStyle _style;

        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("CombatDebugOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CombatDebugOverlay>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3)) Enabled = !Enabled;
            if (Input.touchCount == 3 && Input.GetTouch(2).phase == TouchPhase.Began) Enabled = !Enabled;
        }

        private void OnGUI()
        {
            if (!Enabled) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true };
            var cam = Camera.main;
            if (cam == null) return;
            var y = 60f;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                var d = e.LastDecision;
                var line = $"<b>{(e.def != null ? e.def.displayName : e.kind.ToString())}</b>  " +
                           $"State: {e.Ai}  Intent: {e.Intent}\n" +
                           $"  Attack: {(d.attack != null ? d.attack.id : "-")}  Decision: {d.Total:0.00}  " +
                           $"Target: {e.LastObserved}  Role: {(SquadCoordinator.Instance != null ? SquadCoordinator.Instance.RoleOf(e).ToString() : "-")}\n" +
                           $"  Distance: {(Core.SceneRefs.Motor != null ? Vector3.Distance(e.transform.position, Core.SceneRefs.Motor.transform.position) : 0f):0.0}m  " +
                           $"Preferred: {(e.ActiveProfile != null ? e.ActiveProfile.preferredDistance : 0f):0.0}m  " +
                           $"Profile: {(e.ActiveProfile != null ? e.ActiveProfile.id : "-")}  HP {e.Hp:0}/{e.maxHp:0}  Posture {e.Posture01:P0}";
                GUI.Label(new Rect(20, y, Screen.width - 40, 90), line, _style);
                y += 92f;
                if (y > Screen.height - 100) break;
            }
        }
    }
}
#endif
