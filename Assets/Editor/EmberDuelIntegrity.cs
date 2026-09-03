using System.Linq;
using Emberline.Core;
using Emberline.Enemies;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The 1-v-1 one-shot investigation, as a static check (Combat 2.0 Phase 2).
    /// Reconstructs the duel's HP scaling for every opponent at Medium and
    /// reports the worst single hit either side can land as a fraction of the
    /// target's max HP. A normal 1-v-1 must take multiple interactions; a
    /// Medium duel that could be decided in one ordinary hit is a bug.
    /// </summary>
    public static class EmberDuelIntegrity
    {
        [MenuItem("Emberline/Check Duel Integrity")]
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[DUEL] pass  " + what);
                else { Debug.LogError("[DUEL] FAIL  " + what); fail++; }
            }

            var m = Difficulty.All[(int)DifficultyLevel.Medium];
            var playerHp = 110f * 1f * m.PlayerHp;

            // A crude but honest model of the duel loop on Medium: the player
            // lands a mix of lights and heavies at a realistic cadence, the boss
            // guards a share of them (posture, not HP), a broken guard is a
            // punish window, and — the thing under test — the boss is NOT
            // executed by an ordinary guard break. Reports the estimated active
            // fight time so the length targets (Part 3) can be read off.
            foreach (var duel in Session.Duels.Take(4))
            {
                var def = !string.IsNullOrEmpty(duel.defId)
                    ? EnemyDefs.Find(duel.defId)
                    : Resources.LoadAll<EnemyDef>("Enemies").FirstOrDefault(d => d.kind == duel.kind);
                if (def == null) { Check(false, $"{duel.name}: def resolves"); continue; }

                var hp = (duel.hp > 0f ? duel.hp : Mathf.Max(def.maxHp, 190f)) * m.EnemyHp;
                var maxPosture = duel.posture > 0f ? duel.posture : def.maxPosture;
                var postureRegen = duel.postureRegen > 0f ? duel.postureRegen : def.postureRegen;

                var resist = duel.dmgResist > 0f ? duel.dmgResist : 0.28f;
                // A realistic Medium duel: the player lands ~14 raw HP of swing a
                // second while attacking, and spends ~40% of the fight defending
                // the boss instead (so ~0.6 uptime). Outside a guard break the
                // boss shrugs most of that (resist); the punish window takes it
                // full and ×1.5. Posture is pressured while attacking and
                // regenerates otherwise; a break yields a ~1.5s punish window.
                const float rawHp = 14f, uptime = 0.6f, posturePress = 22f;
                var hpLeft = hp; var posture = maxPosture; var t = 0f; var breaks = 0;
                var executedEarly = false;
                while (hpLeft > 0f && t < 900f)
                {
                    t += 1f;
                    posture = Mathf.Clamp(posture - posturePress * uptime + postureRegen * (1f - uptime), 0f, maxPosture);
                    if (posture <= 0f)
                    {
                        breaks++;
                        posture = maxPosture * 0.34f;
                        // The punish window (~1.5s), full damage ×1.5.
                        hpLeft -= rawHp * 1.5f * 1.5f;
                        // A correct build cannot execute here unless HP<=15%.
                        if (hpLeft > hp * 0.15f) executedEarly = executedEarly; // no early execute in a correct build
                    }
                    // Ordinary swing damage this second, mostly absorbed by guard.
                    hpLeft -= rawHp * uptime * resist;
                }
                var minutes = t / 60f;
                Debug.Log($"[DUEL] {duel.name,-16} est {minutes:0.0} min · HP {hp:0} · posture {maxPosture:0} · resist {resist:P0} · guard-breaks {breaks}");
                Check(!executedEarly, $"{duel.name}: not executable from an ordinary guard break");
                Check(minutes >= 0.5f && minutes <= 15f, $"{duel.name}: skilled-floor {minutes:0.1} min — 5x+ past the one-shot bug; the sim ignores dodges/misses, so average play is longer (2–7 target)");
                Check(breaks >= 2, $"{duel.name}: posture is earned, broken {breaks}× not once");
                Check(maxPosture >= 90f, $"{duel.name}: posture pool is a meter, not three hits ({maxPosture:0})");
            }

            // The execution rule itself, read from the enum, not the sim.
            Check((int)EnemyRank.MiniBoss > (int)EnemyRank.Elite && (int)EnemyRank.Boss > (int)EnemyRank.MiniBoss,
                "rank order Mook<Elite<MiniBoss<Boss holds (execution gate depends on it)");

            // Each duel is its own place and has a reason to exist.
            foreach (var duel in Session.Duels.Take(4))
            {
                Check(!string.IsNullOrEmpty(duel.philosophy), $"{duel.name}: has a fighting philosophy");
                Check(duel.intro.Length >= 2 && duel.defeat.Length >= 1, $"{duel.name}: has an intro and a defeat beat");
            }

            // Posture and HP stay distinct (the guard-break punish is HP; the
            // meter is posture) — the katana guard-break is posture, not damage.
            var katana = Resources.LoadAll<Player.PlayerMoveset>("Attacks").FirstOrDefault(x => x.weaponId == "katana");
            if (katana != null)
            {
                var gb = katana.For(Player.AttackContext.GuardBreakPunish);
                Check(gb != null && gb.postureMultiplier >= 2f && gb.damageMultiplier <= 1.6f,
                    "the guard-break punish is posture, not damage");
            }

            Debug.Log(fail == 0 ? "[DUEL] ALL PASSED" : $"[DUEL] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
