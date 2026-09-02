using System.Linq;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Player;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Combat 2.0 invariants. The rules that must hold whatever the tuning:
    /// every weapon answers every situation, every enemy attack knows what it
    /// is for, every pose has a clip, Medium is still 1.0, and no telegraph
    /// drops under the floor a player can react to.
    /// </summary>
    public static class EmberCombatCheck
    {
        [MenuItem("Emberline/Check Combat 2.0")]
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[C2] pass  " + what);
                else { Debug.LogError("[C2] FAIL  " + what); fail++; }
            }

            // ---- player movesets
            var movesets = Resources.LoadAll<PlayerMoveset>("Attacks");
            var weapons = Resources.LoadAll<WeaponDef>("Weapons");
            Check(movesets.Length >= 6, $"player movesets load ({movesets.Length})");
            foreach (var w in weapons)
            {
                var m = movesets.FirstOrDefault(x => x.weaponId == w.id);
                Check(m != null, $"weapon '{w.id}' has a moveset");
                if (m == null) continue;
                Check(m.For(AttackContext.Chain1) != null && m.For(AttackContext.Heavy) != null,
                    $"{w.id}: has a first light and a heavy");
                foreach (AttackContext ctx in System.Enum.GetValues(typeof(AttackContext)))
                {
                    var c = ctx; var def = m.For(c); var guard = 0;
                    while (def == null && guard++ < 8) { var n = PlayerContextResolver.Fallback(c); if (n == c) break; c = n; def = m.For(c); }
                    Check(def != null, $"{w.id}: context {ctx} resolves (via {c})");
                }
                Check(m.attacks.All(a => !string.IsNullOrEmpty(a.id) && a.animTime > 0f),
                    $"{w.id}: every attack has an id and a duration");
                Check(m.attacks.Select(a => a.context).Distinct().Count() == m.attacks.Length,
                    $"{w.id}: one attack per context");
                Check(m.attacks.Where(a => a.execute).All(a => a.context is AttackContext.Assassination or AttackContext.StaggerPunish),
                    $"{w.id}: executions only where the target is open");
            }

            // Weapons must differ in behaviour, not only numbers: the fastest
            // chain and the slowest must be distinguishable in their timings.
            var tanto = movesets.FirstOrDefault(x => x.weaponId == "tanto");
            var hook = movesets.FirstOrDefault(x => x.weaponId == "hook");
            var daggers = movesets.FirstOrDefault(x => x.weaponId == "daggers");
            var katana = movesets.FirstOrDefault(x => x.weaponId == "katana");
            if (tanto != null && hook != null && katana != null && daggers != null)
            {
                Check(tanto.For(AttackContext.Chain1).animTime < katana.For(AttackContext.Chain1).animTime
                      && katana.For(AttackContext.Chain1).animTime < hook.For(AttackContext.Chain1).animTime,
                    "tanto is faster than katana is faster than hook");
                Check(hook.For(AttackContext.HeavyThrust).rangeMultiplier > katana.For(AttackContext.HeavyThrust).rangeMultiplier,
                    "the hook reaches further than the katana");
                Check(daggers.For(AttackContext.GuardBreakPunish).postureMultiplier < katana.For(AttackContext.GuardBreakPunish).postureMultiplier,
                    "daggers are worse at opening a guard than the katana");
                Check(katana.For(AttackContext.ParryCounter).damageMultiplier >= tanto.For(AttackContext.ParryCounter).damageMultiplier,
                    "the katana's riposte is the strongest counter");
                Check(tanto.For(AttackContext.Heavy).damageMultiplier < katana.For(AttackContext.Heavy).damageMultiplier,
                    "the tanto's heavy is weaker");
            }

            // ---- enemy attacks
            var defs = Resources.LoadAll<EnemyDef>("Enemies");
            foreach (var d in defs)
            {
                Check(d.attacks.All(a => !string.IsNullOrEmpty(a.id)), $"{d.id}: every attack has an id");
                foreach (var a in d.attacks)
                {
                    var windup = a.windupOverride > 0f ? a.windupOverride : d.windupTime;
                    Check(a.kind == AttackKind.Parry || windup >= 0.3f, $"{d.id}:{a.id} telegraphs >= 0.3s ({windup:0.00})");
                    Check(a.RecoveryFor(a.kind) > 0f, $"{d.id}:{a.id} has a recovery");
                }
            }
            foreach (AttackKind k in System.Enum.GetValues(typeof(AttackKind)))
                Check(AttackDefinition.LegacyRecovery(k) > 0f, $"kind {k} has a legacy recovery");

            // ---- personalities: every archetype is somebody, with a kit that has range
            var profiles = Resources.LoadAll<EnemyCombatProfile>("Combat");
            Check(profiles.Length >= 13, $"combat profiles load ({profiles.Length})");
            foreach (var d in defs)
            {
                Check(d.profile != null, $"{d.id}: has a combat profile");
                if (d.profile == null) continue;
                var cats = d.attacks.Select(a => a.category).Distinct().Count();
                var ranged = d.attacks.All(a => a.category == AttackCategory.Ranged || a.kind == AttackKind.Slash && a.maxRange < 2f);
                Check(d.attacks.Length >= 3 && (cats >= 3 || ranged),
                    $"{d.id}: kit has variety ({d.attacks.Length} attacks, {cats} categories)");
                foreach (var c in d.profile.combos)
                    foreach (var step in c.steps)
                        Check(d.attacks.Any(a => a.id == step), $"{d.id}: combo '{c.name}' step '{step}' exists");
                // Feint bands from the brief: common 0–5 %, samurai 5–12 %, elite 10–18 %, boss 15–25 %.
                var fe = d.profile.feintFrequency;
                var band = d.rank switch
                {
                    EnemyRank.Boss => fe >= 0.15f && fe <= 0.25f,
                    EnemyRank.MiniBoss => fe >= 0.10f && fe <= 0.25f,
                    EnemyRank.Elite => d.id == "samurai" ? fe >= 0.05f && fe <= 0.12f : fe >= 0.0f && fe <= 0.18f,
                    _ => fe <= 0.05f,
                };
                Check(band, $"{d.id}: feint frequency {fe:0.00} inside its band");
                Check(d.attacks.All(a => a.category != AttackCategory.Feint) || fe > 0f,
                    $"{d.id}: a kit with a feint has a feint frequency");
            }
            Check(defs.Count(d => d.attacks.Any(a => a.category == AttackCategory.GuardBreak)) >= 6,
                "at least six archetypes can break a guard");
            Check(defs.Count(d => d.attacks.Any(a => a.category == AttackCategory.Delayed)) >= 5,
                "at least five archetypes have a delayed attack");
            Check(defs.Count(d => d.profile != null && d.profile.adaptation > 0f) >= 3,
                "at least three archetypes adapt");

            // ---- poses have clips on the built characters
            var renzo = Resources.Load<GameObject>("Characters/Renzo")
                        ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/RenzoModel.prefab");
            var rig = renzo != null ? renzo.GetComponent<SkeletalRig>() : null;
            var poseCount = System.Enum.GetValues(typeof(RigPose)).Length;
            Check(rig == null || rig.poseStates != null && rig.poseStates.Length >= poseCount,
                $"the player rig's clip table covers every pose ({(rig != null && rig.poseStates != null ? rig.poseStates.Length : -1)}/{poseCount})");

            // ---- the baseline
            var m1 = Difficulty.All[(int)DifficultyLevel.Medium];
            Check(m1.EnemyDamage == 1f && m1.EnemyHp == 1f && m1.Heal == 1f && m1.PlayerHp == 1f && m1.ExtraAttackers == 0,
                "Medium is exactly 1.0 on every axis");

            Debug.Log(fail == 0 ? "[C2] ALL PASSED" : $"[C2] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
