using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Deterministic proof that the four difficulties differ by *behaviour*, not
    /// only stats: the behaviour scalars on Difficulty.Def must be monotonic
    /// across Easy → Medium → Hard → Lethal, Medium neutral, and the stat axes
    /// must stay exactly 1.0 at Medium. Combined with the code that reads them
    /// (selector, ReadPlayer, the mistake step), this is the acceptance evidence
    /// that harder means smarter, not just harder-hitting.
    /// </summary>
    public static class EmberDifficultyScalars
    {
        [MenuItem("Emberline/Check Difficulty Scalars")]
        public static void Run()
        {
            var fail = 0;
            void Chk(bool ok, string what)
            {
                if (ok) Debug.Log("[DSC] pass  " + what);
                else { Debug.LogError("[DSC] FAIL  " + what); fail++; }
            }

            var e = Difficulty.All[(int)DifficultyLevel.Easy];
            var m = Difficulty.All[(int)DifficultyLevel.Medium];
            var h = Difficulty.All[(int)DifficultyLevel.Hard];
            var l = Difficulty.All[(int)DifficultyLevel.Lethal];

            // Medium: the untouched 1.0 baseline on every axis, stat and behaviour.
            Chk(m.EnemyDamage == 1f && m.EnemyHp == 1f && m.Heal == 1f && m.PlayerHp == 1f && m.ExtraAttackers == 0,
                "Medium stats are exactly 1.0");
            Chk(m.FeintScale == 1f && m.AdvancedScale == 1f && m.TeamworkScale == 1f && m.AdaptationScale == 1f
                && m.DecisionScale == 1f && m.DefenseScale == 1f && m.SpacingScale == 1f && m.RecoveryPunishChance == 0.75f
                && m.MistakeChance == 0.15f && m.MaxComboLength == 2,
                "Medium behaviour is the neutral baseline");

            // Behaviour rises with difficulty.
            Chk(e.FeintScale == 0f && e.FeintScale < m.FeintScale && m.FeintScale < h.FeintScale && h.FeintScale < l.FeintScale,
                $"feints rise, Easy never feints (E{e.FeintScale} M{m.FeintScale} H{h.FeintScale} L{l.FeintScale})");
            Chk(e.AdaptationScale == 0f && m.AdaptationScale < h.AdaptationScale && h.AdaptationScale < l.AdaptationScale,
                $"adaptation rises, Easy never adapts (E{e.AdaptationScale} L{l.AdaptationScale})");
            Chk(e.DefenseScale < m.DefenseScale && m.DefenseScale < h.DefenseScale && h.DefenseScale < l.DefenseScale,
                $"defence rises (E{e.DefenseScale} M{m.DefenseScale} H{h.DefenseScale} L{l.DefenseScale})");
            Chk(e.RecoveryPunishChance < m.RecoveryPunishChance && m.RecoveryPunishChance < h.RecoveryPunishChance
                && h.RecoveryPunishChance <= l.RecoveryPunishChance,
                $"recovery punishment rises (E{e.RecoveryPunishChance} L{l.RecoveryPunishChance})");
            Chk(e.SpacingScale < m.SpacingScale && m.SpacingScale < h.SpacingScale && h.SpacingScale < l.SpacingScale,
                $"spacing intelligence rises (E{e.SpacingScale} L{l.SpacingScale})");
            Chk(e.AdvancedScale < m.AdvancedScale && m.AdvancedScale < h.AdvancedScale && h.AdvancedScale < l.AdvancedScale,
                $"delayed/advanced attacks rise (E{e.AdvancedScale} L{l.AdvancedScale})");
            Chk(e.MaxComboLength < m.MaxComboLength && m.MaxComboLength < h.MaxComboLength && h.MaxComboLength < l.MaxComboLength,
                $"combo length rises (E{e.MaxComboLength} M{m.MaxComboLength} H{h.MaxComboLength} L{l.MaxComboLength})");

            // Faster decisions, faster reactions with difficulty.
            Chk(e.DecisionScale > m.DecisionScale && m.DecisionScale > h.DecisionScale && h.DecisionScale > l.DecisionScale,
                $"decisions speed up (E{e.DecisionScale} L{l.DecisionScale})");
            Chk(e.ReactionDelay > m.ReactionDelay && m.ReactionDelay > h.ReactionDelay && h.ReactionDelay > l.ReactionDelay,
                $"reactions speed up (E{e.ReactionDelay}s L{l.ReactionDelay}s)");

            // Mistakes fall with difficulty — but Lethal is never zero (§21).
            Chk(e.MistakeChance > m.MistakeChance && m.MistakeChance > h.MistakeChance && h.MistakeChance > l.MistakeChance,
                $"mistakes fall (E{e.MistakeChance} M{m.MistakeChance} H{h.MistakeChance} L{l.MistakeChance})");
            Chk(l.MistakeChance > 0f, $"Lethal still makes mistakes ({l.MistakeChance:P0}) — never a perfect machine");

            // Defence is never perfect at any level: the caps live in the brain,
            // but the scalar must not be able to push a base chance to certainty.
            Chk(l.DefenseScale <= 2f, "even Lethal's defence scalar stays below runaway (capped ≤ 0.85 in the brain)");

            Debug.Log(fail == 0 ? "[DSC] ALL PASSED" : $"[DSC] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
