#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Emberline.Core;
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// The QA arena: spawn any composition, reset in one tap, optionally fight
    /// forever. Development only. Drives the same spawn path as missions, so
    /// what is tested is what ships.
    /// </summary>
    public class CombatTestArena : MonoBehaviour
    {
        public static readonly (string name, EnemyKind[] kinds, string foe)[] Presets =
        {
            ("Raider", new[] { EnemyKind.Bandit }, ""),
            ("Assassin", new[] { EnemyKind.Assassin }, ""),
            ("Pike Guard", new[] { EnemyKind.PikeGuard }, ""),
            ("Archer", new[] { EnemyKind.Ranged }, ""),
            ("Axe Raider", new[] { EnemyKind.RaiderAxe }, ""),
            ("Samurai", new[] { EnemyKind.Samurai }, ""),
            ("Rogue Ninja", new[] { EnemyKind.RogueNinja }, ""),
            ("Elite", new[] { EnemyKind.EliteWarrior }, ""),
            ("Powder Carrier", new[] { EnemyKind.Bomber }, ""),
            ("2 enemies", new[] { EnemyKind.Bandit, EnemyKind.PikeGuard }, ""),
            ("3 enemies", new[] { EnemyKind.Bandit, EnemyKind.Assassin, EnemyKind.Ranged }, ""),
            ("4 enemies", new[] { EnemyKind.Samurai, EnemyKind.Bandit, EnemyKind.PikeGuard, EnemyKind.Ranged }, ""),
            ("Goro", new[] { EnemyKind.Chief }, ""),
            ("Pale Shade", new[] { EnemyKind.Shade }, "paleshade"),
            ("Jin", new[] { EnemyKind.Jin }, ""),
            ("Kagachi", new[] { EnemyKind.Kagachi }, ""),
        };

        public bool infinite;
        public int preset;
        private GameManager _gm;
        private float _respawnT;
        private GUIStyle _btn;

        public static CombatTestArena Ensure()
        {
            var a = FindFirstObjectByType<CombatTestArena>();
            if (a != null) return a;
            var go = new GameObject("CombatTestArena");
            DontDestroyOnLoad(go);
            return go.AddComponent<CombatTestArena>();
        }

        public void Spawn(int index)
        {
            _gm = SceneRefs.Game;
            if (_gm == null) return;
            preset = Mathf.Clamp(index, 0, Presets.Length - 1);
            Reset();
            var (_, kinds, foe) = Presets[preset];
            foreach (var k in kinds) _gm.SpawnOne(k, false);
            if (!string.IsNullOrEmpty(foe)) _gm.SpawnNamed(kinds[0], foe);
            AiTelemetry.Reset();
        }

        /// <summary>Clear the field and restore the player.</summary>
        public void Reset()
        {
            for (var i = EnemyBrain.Active.Count - 1; i >= 0; i--) EnemyPool.Release(EnemyBrain.Active[i]);
            var motor = SceneRefs.Motor;
            if (motor != null) motor.TryWarpTo(Vector3.zero);
            _gm?.PlayerHealth?.SetMax(_gm.PlayerHealth.MaxHp);
        }

        private void Update()
        {
            if (!infinite) return;
            var alive = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
                if (EnemyBrain.Active[i] != null && !EnemyBrain.Active[i].Dead) alive++;
            if (alive > 0) { _respawnT = 1.5f; return; }
            if ((_respawnT -= Time.deltaTime) <= 0f) Spawn(preset);
        }

        private void OnGUI()
        {
            _btn ??= new GUIStyle(GUI.skin.button) { fontSize = 20 };
            var x = Screen.width - 300f;
            var y = 120f;
            GUI.Label(new Rect(x, y - 30, 280, 28), "<b>COMBAT TEST ARENA</b>", new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true });
            for (var i = 0; i < Presets.Length; i++)
            {
                if (GUI.Button(new Rect(x, y, 280, 34), Presets[i].name, _btn)) Spawn(i);
                y += 36f;
            }
            if (GUI.Button(new Rect(x, y + 6, 136, 36), "RESET", _btn)) Reset();
            if (GUI.Button(new Rect(x + 144, y + 6, 136, 36), infinite ? "INFINITE: ON" : "INFINITE: OFF", _btn)) infinite = !infinite;
            if (GUI.Button(new Rect(x, y + 48, 280, 34), CombatDebugOverlay.Enabled ? "OVERLAY: ON" : "OVERLAY: OFF", _btn))
            { CombatDebugOverlay.Ensure(); CombatDebugOverlay.Enabled = !CombatDebugOverlay.Enabled; }
        }
    }
}
#endif
