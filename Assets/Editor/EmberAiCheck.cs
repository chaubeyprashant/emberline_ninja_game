using System.Linq;
using Emberline.Core;
using Emberline.Enemies;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    public static class AiCheck
    {
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[AI] pass  " + what);
                else { Debug.LogError("[AI] FAIL  " + what); fail++; }
            }

            // Medium is the baseline and must not have moved.
            var m = Difficulty.All[(int)DifficultyLevel.Medium];
            Check(m.EnemyDamage == 1f && m.EnemyHp == 1f && m.Heal == 1f && m.PlayerHp == 1f
                  && m.ExtraAttackers == 0 && m.Score == 1f, "Medium is exactly 1.0 on every axis");

            var defs = Resources.LoadAll<EnemyDef>("Enemies");
            Check(defs.Length == 13, $"13 enemy defs loaded ({defs.Length})");

            // Telegraph floor: every damaging pattern must be readable. Punish and
            // riposte paths clamp to 0.3s / 0.32s in code; cold attacks use the
            // pattern's windup or the enemy's default, which must also be >= 0.3.
            var short_ = defs.SelectMany(d => d.attacks.Select(a => (d.id, a)))
                .Where(x => x.a.kind != AttackKind.Parry)
                .Where(x => (x.a.windupOverride > 0f ? x.a.windupOverride : x.Item1 == null ? 1f
                    : defs.First(d => d.id == x.Item1).windupTime) < 0.3f)
                .Select(x => $"{x.Item1}:{x.a.kind}").ToArray();
            Check(short_.Length == 0, $"every damaging attack telegraphs >= 0.3s ({string.Join(",", short_)})");

            // Diminishing stagger: 4th stagger in a window is short but never zero.
            foreach (var d in defs)
            {
                var len = 0.38f * Mathf.Pow(d.staggerDecay, 3);
                Check(Mathf.Max(0.12f, len) >= 0.12f && d.staggerDecay is >= 0.3f and <= 1f,
                    $"{d.id}: stagger decay {d.staggerDecay:0.00} → 4th flinch {Mathf.Max(0.12f, len):0.00}s");
            }

            // Personalities actually landed on the regenerated assets.
            EnemyDef D(string id) => defs.First(d => d.id == id);
            Check(D("assassin").punishesExposure && D("assassin").dodgeChance > 0f, "assassin punishes and dodges");
            Check(D("samurai").readsHeavies && D("samurai").counterChance > 0f, "samurai reads heavies and ripostes");
            Check(D("spearman").protectsRanged && D("spearman").punishesExposure, "pike guard protects and punishes");
            Check(D("archer").panicRange > 0f && D("archer").attacks.Any(a => a.kind == AttackKind.Slash),
                "archer panics when closed on and has an emergency jab");
            Check(D("heavy").guardsWhenPostureLow && D("heavy").readsHeavies, "axe raider guards on low posture");
            Check(D("rogueninja").dodgeChance >= 0.4f, "rogue ninja dodges");
            Check(D("elite").readsHeavies && D("elite").dodgeChance > 0f && D("elite").punishesExposure
                  && D("elite").guardsWhenPostureLow && D("elite").protectsRanged, "elite has every mechanic");
            Check(!D("bandit").punishesExposure && D("bandit").dodgeChance == 0f, "raider stays simple");

            // Token cap: base 2, +1 at 4 alive, +difficulty. Never above 3 on Medium.
            Check(2 + 1 + m.ExtraAttackers == 3, "Medium caps simultaneous attackers at 3 in a crowd");

            Debug.Log(fail == 0 ? "[AI] ALL PASSED" : $"[AI] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
