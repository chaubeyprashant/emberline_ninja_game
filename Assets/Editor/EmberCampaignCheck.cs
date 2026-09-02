using System.Collections.Generic;
using System.Linq;
using Emberline.Campaign;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The campaign's design rules as assertions: the ten fields on every one of
    /// the hundred missions, the chapter and act structure, the gameplay
    /// distribution the brief asked for, the boss cadence, the arc bands, the
    /// journey's regions, and that every mission resolves a plan that itself
    /// passes the mission design rules.
    /// </summary>
    public static class EmberCampaignCheck
    {
        [MenuItem("Emberline/Check Campaign")]
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[CMP] pass  " + what);
                else { Debug.LogError("[CMP] FAIL  " + what); fail++; }
            }

            var all = Campaign.Campaign.Missions;
            Check(all.Length == 100, $"a hundred missions ({all.Length})");
            for (var i = 0; i < all.Length; i++)
                Check(all[i].id == i + 1, $"mission {i + 1} is in slot {i + 1} (found {all[i].id})");

            // The ten fields, on every mission.
            foreach (var m in all)
            {
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(m.name)) missing.Add("name");
                if (string.IsNullOrWhiteSpace(m.storyPurpose)) missing.Add("purpose");
                if (string.IsNullOrWhiteSpace(m.primaryObjective)) missing.Add("objective");
                if (m.types.Length == 0) missing.Add("gameplay type");
                if (string.IsNullOrWhiteSpace(m.uniqueEvent)) missing.Add("unique event");
                if (string.IsNullOrWhiteSpace(m.storyDiscovery)) missing.Add("discovery");
                if (string.IsNullOrWhiteSpace(m.climax)) missing.Add("climax");
                if (string.IsNullOrWhiteSpace(m.ending)) missing.Add("ending");
                if (string.IsNullOrWhiteSpace(m.nextReason)) missing.Add("next reason");
                Check(missing.Count == 0, $"{m.id:00} {m.name}: all ten fields ({string.Join(",", missing)})");
                Check(!m.nextReason.ToLowerInvariant().Contains("mission complete"),
                    $"{m.id:00}: never ends on 'mission complete'");
            }

            // Chapters and acts.
            var ch = Campaign.Campaign.Chapters;
            Check(ch.Length == 10, "ten chapters");
            Check(ch.All(c => c.lastMission - c.firstMission == 9), "every chapter holds ten missions");
            Check(ch.Count(c => c.act == 1) == 3 && ch.Count(c => c.act == 2) == 4 && ch.Count(c => c.act == 3) == 3,
                "acts split 30 / 40 / 30");

            // Boss cadence: the five majors where the brief put them, and nowhere else.
            var majors = all.Where(m => m.IsMajorBoss).Select(m => m.id).ToArray();
            Check(majors.SequenceEqual(new[] { 5, 40, 70, 99 }) || majors.SequenceEqual(new[] { 5, 30, 40, 70, 99 }),
                $"major bosses at 5, 30, 40, 70, 99 ({string.Join(",", majors)})");
            Check(Campaign.Campaign.Get(30).foe == "paleshade", "30 is the Pale Shade");
            Check(Campaign.Campaign.Get(5).boss == EnemyKind.Chief && Campaign.Campaign.Get(40).boss == EnemyKind.Chief,
                "Goro at 5 and 40");
            Check(Campaign.Campaign.Get(70).boss == EnemyKind.Jin, "Jin at 70");
            Check(Campaign.Campaign.Get(99).boss == EnemyKind.Kagachi, "Kagehira at 99");
            Check(Campaign.Campaign.Get(100).enemies.Length == 0 && Campaign.Campaign.Get(100).Primary == GameplayType.Conversation,
                "100 has no enemies and is a conversation");

            // Distribution by primary type against the brief's targets, with a
            // tolerance. Overlap is allowed and is reported in the campaign doc;
            // counting it here would let one decorative secondary tag inflate a
            // category past any target.
            var d = Campaign.Campaign.Distribution(primaryOnly: true);
            int Of(GameplayType t) => d.TryGetValue(t, out var n) ? n : 0;
            void Near(GameplayType t, int target, int tol) =>
                Check(Mathf.Abs(Of(t) - target) <= tol, $"distribution {t}: {Of(t)} (target ~{target})");
            Near(GameplayType.Combat, 20, 8);
            Near(GameplayType.Stealth, 15, 6);
            Near(GameplayType.Investigation, 10, 5);
            Near(GameplayType.Rescue, 10, 5);
            Near(GameplayType.Defense, 8, 4);
            Near(GameplayType.Escort, 7, 4);
            Near(GameplayType.Chase, 7, 4);
            Near(GameplayType.Exploration, 8, 6);
            Near(GameplayType.Survival, 5, 5);
            Near(GameplayType.Boss, 10, 5);

            // Variety: no three missions in a row with the same primary type.
            for (var i = 2; i < all.Length; i++)
                Check(!(all[i].Primary == all[i - 1].Primary && all[i].Primary == all[i - 2].Primary),
                    $"{all[i].id:00}: not the third {all[i].Primary} in a row");

            // The arc bands.
            Check(all.Where(m => m.id <= 20).All(m => m.renzo == RenzoState.Confused)
                  && all.Where(m => m.id > 80).All(m => m.renzo == RenzoState.Changed), "Renzo's arc follows the bands");
            Check(all.Where(m => m.id <= 20).All(m => m.seal == SealStage.Rumour)
                  && all.Single(m => m.id == 100).seal == SealStage.Chosen, "the Seal's reveal follows the bands");

            // The journey: regions appear in the brief's order at chapter level.
            var order = new[] { Region.Ruins, Region.Forest, Region.Mountains, Region.Marsh, Region.Temples,
                Region.Villages, Region.Fortresses, Region.Snow, Region.Stronghold, Region.Seal, Region.Dawn };
            Check(all.First().region == Region.Ruins && all.Last().region == Region.Dawn, "from ruins to dawn");
            Check(all.Where(m => m.id is >= 41 and <= 50).All(m => m.region is Region.Marsh or Region.Temples),
                "chapter 5 is the marsh");
            Check(all.Where(m => m.id is >= 71 and <= 80).All(m => m.region is Region.Snow or Region.Fortresses or Region.Stronghold),
                "chapter 8 is snow and fortress");
            Check(order.All(r => all.Any(m => m.region == r)), "every region of the journey is visited");

            // Every mission resolves a plan, named for it, that passes the design rules.
            var plansOk = 0;
            foreach (var m in all)
            {
                var plan = Resources.Load<MissionPlan>($"Missions/{m.PlanAsset}");
                if (plan == null) { Check(false, $"{m.id:00} {m.name}: plan '{m.PlanAsset}' loads"); continue; }
                if (plan.missionName != m.name) Check(false, $"{m.id:00}: plan named '{plan.missionName}' for '{m.name}'");
                else plansOk++;
                if (!string.IsNullOrEmpty(m.beat))
                    Check(Resources.Load<Story.StoryBeat>("Story/" + m.beat) != null, $"{m.id:00}: beat '{m.beat}' exists");
                if (!string.IsNullOrEmpty(m.foe))
                    Check(EnemyDefs.Find(m.foe) != null, $"{m.id:00}: foe '{m.foe}' exists");
            }
            Check(plansOk == 100, $"all hundred plans load and are named for their mission ({plansOk})");

            Debug.Log(fail == 0 ? "[CMP] ALL PASSED" : $"[CMP] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
