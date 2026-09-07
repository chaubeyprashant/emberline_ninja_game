using System.Collections.Generic;
using System.Linq;
using Emberline.Campaign;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Holds the campaign redesign to its own rules. The story validator checks
    /// that the hundred missions are a story; this one checks that they are a
    /// journey — that the player is offered a choice, given people, given a
    /// reason to prepare, and shown a world that changed.
    /// </summary>
    public static class EmberDesignCheck
    {
        public static int Failures;

        [MenuItem("Emberline/Check Campaign Design")]
        public static void Run()
        {
            var fail = new List<string>();
            var warn = new List<string>();
            var d = CampaignDesign.Designs;

            void Fail(string s) => fail.Add(s);
            void Warn(string s) => warn.Add(s);

            // ---- the table itself
            if (d.Length != 100) Fail($"design table has {d.Length} entries, not 100");
            for (var i = 0; i < d.Length; i++)
                if (d[i].id != i + 1) Fail($"design {i} carries id {d[i].id}");

            // ---- every mission ends on a want, and no two in a row on the same one
            for (var i = 0; i < d.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(d[i].want)) Fail($"mission {d[i].id} ends on nothing the player wants");
                if (i > 0 && d[i].want == d[i - 1].want) Fail($"missions {d[i - 1].id} and {d[i].id} end on the same want");
            }

            // ---- the flag graph is causal: nothing is read before it can be set
            var setAt = new Dictionary<string, int>();
            foreach (var m in d)
                foreach (var f in m.sets)
                    if (!setAt.ContainsKey(f)) setAt[f] = m.id;
            foreach (var m in d)
            {
                foreach (var f in m.reads)
                {
                    if (!setAt.TryGetValue(f, out var at)) { Fail($"mission {m.id} reads `{f}`, which nothing sets"); continue; }
                    if (at >= m.id) Fail($"mission {m.id} reads `{f}`, first set at {at}");
                }
                if (m.approachFlag != "")
                {
                    if (!setAt.TryGetValue(m.approachFlag, out var at2)) Fail($"mission {m.id} unlocks approaches with `{m.approachFlag}`, which nothing sets");
                    else if (at2 >= m.id) Fail($"mission {m.id} unlocks approaches with `{m.approachFlag}`, first set at {at2}");
                }
            }

            // ---- an assault the player cannot shape is not an assault
            foreach (var m in d)
            {
                if (m.role == MissionRole.Assault && m.approaches.Length < 2)
                    Fail($"mission {m.id} is an Assault with no choice at the door");
                if (m.approaches.Length > 0 && m.approaches[0] != Approach.Assault
                    && m.approaches[0] != Approach.Stealth && m.approaches[0] != Approach.Ambush
                    && m.approaches[0] != Approach.Sabotage && m.approaches[0] != Approach.Allied)
                    Fail($"mission {m.id} has an unreadable default approach");
                if (m.approaches.Length != m.approaches.Distinct().Count())
                    Fail($"mission {m.id} lists the same approach twice");
            }

            // ---- companions cannot appear before they exist, and a companion
            // who cannot be quiet cannot be on a stealth approach
            foreach (var c in CampaignDesign.Companions)
            {
                var uses = d.Where(m => m.Has(c.id)).ToArray();
                if (uses.Length == 0) { Fail($"{c.name} is never brought on a mission"); continue; }
                var first = uses.Min(m => m.id);
                if (first < c.joinMission) Fail($"{c.name} appears at {first} but joins at {c.joinMission}");
                if (uses.Length < 6) Warn($"{c.name} appears on only {uses.Length} missions");
            }
            foreach (var m in d)
                if (m.Has(Companion.Daigo) && m.Has(Approach.Stealth))
                    Fail($"mission {m.id} offers a stealth approach with Daigo on it");

            // ---- the chapter rhythm the brief asks for
            foreach (var ch in Campaign.Campaign.Chapters)
            {
                var band = d.Where(m => m.id >= ch.firstMission && m.id <= ch.lastMission).ToArray();
                var label = $"chapter {ch.number} ({ch.firstMission}-{ch.lastMission})";
                if (!band.Any(m => m.role is MissionRole.Recon or MissionRole.Discovery or MissionRole.Memory))
                    Fail($"{label} never lets the player look before acting");
                if (!band.Any(m => m.consequence != ""))
                    Fail($"{label} never changes the world");
                if (ch.number >= 2 && ch.number <= 9 && band.Count(m => m.HasChoice) < 2)
                    Fail($"{label} offers a real choice on fewer than two missions");
                if (ch.number is >= 2 and <= 9 && !band.Any(m => m.companions.Length > 0))
                    Fail($"{label} has nobody in it but Renzo");
            }

            // ---- every companion gets their own missions
            foreach (var c in CampaignDesign.Companions)
            {
                var personal = d.Count(m => m.role == MissionRole.Personal && m.Has(c.id));
                if (personal < 2) Fail($"{c.name} has {personal} personal mission(s); the rule is two");
            }

            // ---- silence between the action
            var downtime = d.Where(m => m.role == MissionRole.Downtime).ToArray();
            if (downtime.Length < 4) Fail($"only {downtime.Length} missions have no combat in them");
            foreach (var act in new[] { 1, 2, 3 })
                if (!downtime.Any(m => Campaign.Campaign.ChapterOf(m.id).act == act))
                    Warn($"act {act} never lets the player breathe");

            // ---- no long stretch of missions that are neither a choice nor a person
            var run = 0;
            foreach (var m in d)
            {
                var interesting = m.HasChoice || m.companions.Length > 0
                    || m.role is MissionRole.Recon or MissionRole.Prepare or MissionRole.Downtime
                    or MissionRole.Memory or MissionRole.Personal or MissionRole.Consequence
                    or MissionRole.Discovery;
                run = interesting ? 0 : run + 1;
                if (run > 4) Fail($"missions {m.id - run + 1}-{m.id} are five in a row with no choice, no companion and no turn");
            }

            // ---- camps and villages are places, not labels
            foreach (var c in CampaignDesign.Camps)
            {
                var listed = c.missions.OrderBy(x => x).ToArray();
                var actual = d.Where(m => m.camp == c.id).Select(m => m.id).OrderBy(x => x).ToArray();
                if (!listed.SequenceEqual(actual))
                    Fail($"{c.name} lists missions [{string.Join(",", listed)}] but is assigned to [{string.Join(",", actual)}]");
                if (c.marks.Length < 4) Fail($"{c.name} has {c.marks.Length} things to find; a camp needs at least four");
                if (c.garrison < 24) Warn($"{c.name} garrison is {c.garrison}; the brief asks for 25-40");
            }
            foreach (var v in CampaignDesign.Villages)
            {
                var at = d.Where(m => m.village == v.id).Select(m => m.id).ToArray();
                if (at.Length < 2) Fail($"{v.name} appears in {at.Length} mission(s); a village the player never returns to is a backdrop");
                if (at.Length > 0 && at.Min() != v.firstMission)
                    Fail($"{v.name} says it starts at {v.firstMission} but first appears at {at.Min()}");
            }

            // ---- the ten villains change the world when they fall
            var villainMissions = new[] { 2, 13, 24, 30, 40, 59, 70, 78, 79, 99 };
            foreach (var id in villainMissions)
                if (CampaignDesign.For(id).consequence == "")
                    Fail($"mission {id} kills a named villain and nothing changes");

            foreach (var w in warn) Debug.LogWarning($"[Design] {w}");
            if (fail.Count == 0)
            {
                Debug.Log($"[Emberline] Campaign design OK — 100 missions, " +
                          $"{d.Count(m => m.HasChoice)} with a choice at the door, " +
                          $"{d.Count(m => m.companions.Length > 0)} with somebody along, " +
                          $"{d.Count(m => m.role == MissionRole.Recon)} recon, " +
                          $"{d.Count(m => m.role == MissionRole.Prepare)} preparation, " +
                          $"{downtime.Length} with no combat, " +
                          $"{setAt.Count} facts the world remembers, " +
                          $"{d.Count(m => m.consequence != "")} visible consequences ({warn.Count} warnings)");
                Failures = 0;
                return;
            }
            foreach (var f in fail) Debug.LogError($"[Design] {f}");
            Failures = fail.Count;
            Debug.LogError($"[Emberline] Campaign design FAILED — {fail.Count} problem(s)");
        }
    }
}
