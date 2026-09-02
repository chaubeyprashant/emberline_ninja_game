using System.Collections.Generic;
using System.Linq;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The mission design rules, as assertions. Content rots quietly: a plan can
    /// drift into "walk, three fights, boss" one edit at a time and nothing will
    /// complain. This complains.
    /// </summary>
    public static class EmberMissionCheck
    {
        [MenuItem("Emberline/Check Mission Design")]
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[MSN] pass  " + what);
                else { Debug.LogError("[MSN] FAIL  " + what); fail++; }
            }

            var levels = Session.Story;
            var plans = new List<MissionPlan>();

            // 1. Every story level resolves a plan, and the plan is *that* mission.
            foreach (var lv in levels)
            {
                var plan = Resources.Load<MissionPlan>($"Missions/{lv.planAsset}");
                Check(plan != null, $"level {lv.id} '{lv.name}' resolves plan '{lv.planAsset}'");
                if (plan == null) continue;
                plans.Add(plan);
                Check(plan.missionName == lv.name,
                    $"level {lv.id}: plan is named '{plan.missionName}' for level '{lv.name}'");
            }
            Check(plans.Count == levels.Length, $"every story level has a plan ({plans.Count}/{levels.Length})");
            if (plans.Count == 0) { Done(fail); return; }

            bool Fight(MissionStage s) => s.goal is StageGoal.Wave or StageGoal.BossFight
                or StageGoal.Duel or StageGoal.Eliminate or StageGoal.Assassinate
                or StageGoal.BossPhase or StageGoal.Stealth or StageGoal.Listen
                or StageGoal.Survive or StageGoal.Defend or StageGoal.Chase;

            foreach (var p in plans)
            {
                var st = p.stages;
                var tag = $"{p.id} {p.missionName}";
                Check(st.Length >= 5, $"{tag}: has a shape, not a stub ({st.Length} stages)");

                // 2. The forbidden default: walk in, three fights, boss, done.
                //    A boss finale is exempt, and deliberately so: alternating
                //    phases and their adds is one encounter with a shape, which is
                //    the opposite of the problem this rule exists to catch. It is
                //    exempt only where a climax belongs, in the last third.
                var worstRun = 0;
                var worstAt = 0;
                var run = 0;
                var runHasBoss = false;
                for (var i = 0; i < st.Length; i++)
                {
                    if (!Fight(st[i])) { run = 0; runHasBoss = false; continue; }
                    run++;
                    runHasBoss |= st[i].goal is StageGoal.BossPhase or StageGoal.BossFight;
                    var allowed = runHasBoss && i >= st.Length * 2 / 3 ? 5 : 2;
                    if (run > allowed && run > worstRun) { worstRun = run; worstAt = i; }
                }
                Check(worstRun == 0,
                    $"{tag}: no unbroken slog of fights (worst run {worstRun} ending at stage {worstAt})");

                // 3. Pacing: a quiet opening, a breather past the midpoint, and an
                //    ending that is not another fight.
                Check(st[0].spawn.Length == 0 && !Fight(st[0]),
                    $"{tag}: opens quiet ({st[0].goal})");
                var lateCalm = false;
                for (var i = st.Length / 2; i < st.Length; i++) if (!Fight(st[i])) lateCalm = true;
                Check(lateCalm, $"{tag}: has a beat after the midpoint that is not a fight");
                Check(!Fight(st[^1]), $"{tag}: resolves rather than ending on a fight ({st[^1].goal})");

                // 4. A meaningful optional objective, and 5. a scripted turn.
                // A mission with nobody in it has nothing to be optional about:
                // the dawn walk at the end is exempt by design, not by oversight.
                var anyEnemy = st.Any(s => s.spawn.Length > 0 || s.spawnB.Length > 0 || !string.IsNullOrEmpty(s.foeDef));
                if (anyEnemy)
                {
                    Check(p.challenge != MissionChallenge.None, $"{tag}: has an optional objective");
                    Check(p.challengeShards > 0 && p.challengeShards <= p.baseShards,
                        $"{tag}: optional pays {p.challengeShards} against {p.baseShards} for the mission");
                }
                Check(st.Any(s => s.onComplete != StageEvent.None), $"{tag}: has a special event");

                // 6. Environmental storytelling.
                Check(p.dressing.Length >= 3, $"{tag}: dresses the world ({p.dressing.Length} kinds)");

                // Checkpoints, so a long mission is not a single punishment.
                Check(st.Any(s => s.checkpoint), $"{tag}: has a checkpoint");
            }

            // 7. Consecutive missions must not be the same mission twice.
            for (var i = 1; i < plans.Count; i++)
            {
                string Sig(MissionPlan p) => string.Join(">", p.stages.Select(s => s.goal.ToString()));
                Check(Sig(plans[i]) != Sig(plans[i - 1]),
                    $"{plans[i].id}: differs in shape from the mission before it");

                string Comp(MissionPlan p) => string.Join(",",
                    p.stages.SelectMany(s => s.spawn).Select(k => k.ToString()).OrderBy(x => x).Distinct());
                Check(Comp(plans[i]) != Comp(plans[i - 1]),
                    $"{plans[i].id}: fields a different roster from the mission before it");
            }

            // 9. Every dressing kind the brief listed is used somewhere.
            var used = plans.SelectMany(p => p.dressing).Distinct().ToArray();
            foreach (DressingKind k in System.Enum.GetValues(typeof(DressingKind)))
            {
                if (k == DressingKind.None) continue;
                Check(used.Contains(k), $"environmental storytelling uses {k}");
            }

            Done(fail);
        }

        private static void Done(int fail)
        {
            Debug.Log(fail == 0 ? "[MSN] ALL PASSED" : $"[MSN] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
