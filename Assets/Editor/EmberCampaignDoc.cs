using System.IO;
using System.Linq;
using System.Text;
using Emberline.Campaign;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Writes docs/CAMPAIGN.md from the campaign table, so the bible and the
    /// game can never disagree: the document is the data, rendered.
    /// </summary>
    public static class EmberCampaignDoc
    {
        [MenuItem("Emberline/Write Campaign Doc")]
        public static void Write()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Emberline — the hundred-mission campaign");
            sb.AppendLine();
            sb.AppendLine("Generated from `Assets/Scripts/Campaign/CampaignTable.cs` by `Emberline/Write Campaign Doc`. Edit the table, not this file.");
            sb.AppendLine();
            sb.AppendLine("## Structure");
            sb.AppendLine();
            sb.AppendLine("100 missions · 10 chapters · 3 acts. Every mission carries the ten fields the design rule requires; the last of them, the **next-mission reason**, is shown on the results screen and names the mission it leads to.");
            sb.AppendLine();
            foreach (var act in new[] { 1, 2, 3 })
            {
                sb.AppendLine($"### {Campaign.Campaign.ActNames[act - 1]}");
                foreach (var c in Campaign.Campaign.Chapters.Where(c => c.act == act))
                    sb.AppendLine($"- **Chapter {c.number} — {c.name}** ({c.firstMission}–{c.lastMission}) · {c.theme} · *{c.region}*");
                sb.AppendLine();
            }

            sb.AppendLine("## Gameplay distribution");
            sb.AppendLine();
            sb.AppendLine("The first type on a mission is its primary and is what the campaign validator holds against the brief's targets; the overlap column counts every mission that carries the type at all, since the brief allows categories to overlap.");
            sb.AppendLine();
            sb.AppendLine("| Type | Brief target | Missions carrying it | As primary |");
            sb.AppendLine("|---|---|---|---|");
            var targets = new (GameplayType t, string target)[]
            {
                (GameplayType.Combat, "~20"), (GameplayType.Stealth, "~15"), (GameplayType.Investigation, "~10"),
                (GameplayType.Rescue, "~10"), (GameplayType.Defense, "~8"), (GameplayType.Escort, "~7"),
                (GameplayType.Chase, "~7"), (GameplayType.Exploration, "~8"), (GameplayType.Survival, "~5"),
                (GameplayType.Boss, "~10"), (GameplayType.Sabotage, "—"), (GameplayType.Memory, "—"),
                (GameplayType.Conversation, "—"), (GameplayType.Endure, "—"),
            };
            var any = Campaign.Campaign.Distribution(false);
            var prim = Campaign.Campaign.Distribution(true);
            foreach (var (t, target) in targets)
                sb.AppendLine($"| {t} | {target} | {(any.TryGetValue(t, out var a) ? a : 0)} | {(prim.TryGetValue(t, out var p) ? p : 0)} |");
            sb.AppendLine();

            sb.AppendLine("## Boss cadence");
            sb.AppendLine();
            foreach (var m in Campaign.Campaign.Missions.Where(m => m.IsMajorBoss || m.foe == "paleshade" && m.Primary == GameplayType.Boss))
                sb.AppendLine($"- **{m.id} — {m.name}**: {(m.boss.HasValue ? m.boss.Value.ToString() : m.foe)}");
            sb.AppendLine();
            sb.AppendLine("Named foes on common bodies (no new model): Convoy Captain (2), Scavenger King (13), the Three Blades (24), Pale Shade (21, 30), Drowned Guardian (59), Commander Hoshu (66, 79), Iron Guard (74, 78, 94). Jin and Kagehira appear as unbeatable foes (Endure) before their boss missions.");
            sb.AppendLine();

            sb.AppendLine("## The journey");
            sb.AppendLine();
            sb.AppendLine("Two arena geometries exist (rooftop deck, marsh). Every region is carried on them by lighting theme, weather, visibility and set dressing; the real geometry for forest, mountain, snow, temple and fortress is specified in `docs/ASSET_SPECIFICATIONS.md` and is **not** present. Mission by mission:");
            sb.AppendLine();
            sb.AppendLine("| Missions | Region | Arena | Theme | Weather |");
            sb.AppendLine("|---|---|---|---|---|");
            // Contiguous runs in mission order, so the table reads as the road
            // does: one row per stretch, a new row wherever the place changes.
            var all = Campaign.Campaign.Missions;
            var start = 0;
            for (var i = 1; i <= all.Length; i++)
            {
                var ends = i == all.Length || all[i].region != all[start].region || all[i].marsh != all[start].marsh
                           || all[i].theme != all[start].theme;
                if (!ends) continue;
                var run = all.Skip(start).Take(i - start).ToArray();
                var weather = string.Join("/", run.SelectMany(x => new[]
                        { x.night ? "night" : "", x.rain ? "rain" : "", x.snow ? "snow" : "", x.fog ? "fog" : "" })
                    .Where(w => w != "").Distinct());
                var first = run.First();
                var label = run.Length == 1 ? $"{first.id}" : $"{first.id}–{run.Last().id}";
                sb.AppendLine($"| {label} | {first.region} | {(first.marsh ? "Marsh" : "Rooftop")} | {first.theme} | {(weather == "" ? "clear" : weather)} |");
                start = i;
            }
            sb.AppendLine();

            sb.AppendLine("## Renzo, and the Seal");
            sb.AppendLine();
            sb.AppendLine("| Missions | Renzo | The Black Seal |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in Campaign.Campaign.Missions.GroupBy(m => (m.renzo, m.seal)).OrderBy(g => g.Min(x => x.id)))
                sb.AppendLine($"| {g.Min(x => x.id)}–{g.Max(x => x.id)} | {g.Key.renzo} | {g.Key.seal} |");
            sb.AppendLine();

            sb.AppendLine("## The missions");
            sb.AppendLine();
            foreach (var c in Campaign.Campaign.Chapters)
            {
                sb.AppendLine($"### Chapter {c.number} — {c.name}");
                sb.AppendLine();
                sb.AppendLine($"*{c.theme}.*");
                sb.AppendLine();
                foreach (var m in Campaign.Campaign.Missions.Where(m => m.chapter == c.number))
                {
                    sb.AppendLine($"#### {m.id:00} — {m.name}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Story purpose:** {m.storyPurpose}");
                    sb.AppendLine($"- **Primary objective:** {m.primaryObjective}");
                    sb.AppendLine($"- **Gameplay type:** {string.Join(" + ", m.types)}");
                    sb.AppendLine($"- **Unique event:** {m.uniqueEvent}");
                    sb.AppendLine($"- **Story discovery:** {m.storyDiscovery}");
                    sb.AppendLine($"- **Climax:** {m.climax}");
                    sb.AppendLine($"- **Ending:** {m.ending}");
                    sb.AppendLine($"- **Next mission reason:** {m.nextReason}");
                    var roster = m.enemies.Length == 0 ? "none" : string.Join(", ", m.enemies);
                    var extras = (m.boss.HasValue ? $" · boss {m.boss}" : "") + (m.foe != "" ? $" · named foe `{m.foe}`" : "") + (m.beat != "" ? $" · beat `{m.beat}`" : "") + (m.plan != "" ? $" · bespoke plan `{m.plan}`" : "");
                    sb.AppendLine($"- *Staging:* {m.region}, {(m.marsh ? "marsh" : "rooftop")} arena, {m.theme}{(m.night ? ", night" : "")}{(m.rain ? ", rain" : "")}{(m.snow ? ", snow" : "")}{(m.fog ? ", fog" : "")} · enemies: {roster}{extras}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("## Post-campaign");
            sb.AppendLine();
            sb.AppendLine("Finishing mission 100 unlocks New Game+ (the existing NG+ scaling: harder enemies, altered compositions, nightmare duels), the full nine-opponent duel roster (campaign bosses and named elites on the bodies they used), and the Infinite March as the endless mode over the campaign's environments and factions.");
            sb.AppendLine();
            sb.AppendLine("## What this campaign does not yet have");
            sb.AppendLine();
            sb.AppendLine("- Environment geometry for forest, mountain, snow, temple, fortress and the stronghold. Carried by theme, weather and dressing on the two arenas; specified, not built.");
            sb.AppendLine("- Gameplay models for adult Aiko and Jin. Cinematic beats that frame them use a clearly named `PLACEHOLDER_*_StandIn` on the primitive rig.");
            sb.AppendLine("- A fourth Kagehira phase with a physically collapsing arena. The final fight has three mission-level phases with arena changes (water, dark) plus the refusal beat; a collapsing floor is a geometry feature.");
            sb.AppendLine("- Voice acting. Every line is subtitled through the existing story framework's VO hook.");

            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/CAMPAIGN.md", sb.ToString());
            Debug.Log("[Emberline] docs/CAMPAIGN.md written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
