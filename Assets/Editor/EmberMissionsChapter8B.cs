using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 8 — THE IRON FORTRESS, missions 76-80, built by hand. The prison
    /// tower where every cell is the wrong one, the records they burn, the Iron
    /// Guard in Toku's own steel, Hoshu at the gate he will not yield twice, and
    /// the warlord's hall, empty of the warlord.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter8B()
        {
            // ---------------------------------------------------------------
            // 76 — THE PRISON TOWER. Night. Every cell freed is a voice that might
            // be hers; none of them is. One of them is Suzu's brother.
            var m76 = P_("S76_PrisonTower");
            m76.id = 76; m76.missionName = "THE PRISON TOWER"; m76.missionType = "RESCUE";
            m76.baseShards = 5; m76.applyTheme = true; m76.theme = Core.EnvThemeId.Fortress;
            m76.briefing = "The prison tower, cell by cell to the lit window at the top. The wardens have a bell. Suzu has been counting the floors under her breath since the drain.";
            m76.debrief = "Every cell opened, and none was Aiko. One was Suzu's brother Kanta, alive. The prisoners said the girl with the thread was moved last night. Her cell at the top is empty, and still warm.";
            m76.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.MissingNotice,
                DressingKind.HidingVillagers, DressingKind.BloodTrail };
            m76.challenge = MissionChallenge.SaveAllPrisoners; m76.challengeShards = 3;
            m76.stages = new[]
            {
                Scene("tower_open"),

                St(StageGoal.Reach, "THE TOWER STAIR", "TO THE LIT WINDOW",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                St(StageGoal.FreePrisoners, "EVERY CELL", "NONE OF THEM IS HER",
                    count: 3, checkpoint: true),

                St(StageGoal.Stealth, "THE WARDENS", "THEY ARE GOING FOR THE BELL",
                    spawn: new[] { P, A }, onComplete: StageEvent.AlarmTriggered, checkpoint: true),

                Scene("tower_kanta"),

                St(StageGoal.Wave, "THE WARDENS AT THE BELL", "THE TOWER IS AWAKE",
                    spawn: new[] { P, R }, checkpoint: true),

                St(StageGoal.FreePrisoners, "THE UPPER CELLS", "SHE MIGHT BE ABOVE",
                    count: 2),

                Look("THE TOP CELL", "EMPTY. WARM.", false,
                    Prop("blanket", "A BLANKET, STILL WARM", new Vector3(-4f, 0f, -12f), StoryPropShape.Keepsake,
                        "Still warm. She was here last night. Last night."),
                    Prop("tally", "A TALLY THAT STOPS AT YESTERDAY", new Vector3(4f, 0f, -12f), StoryPropShape.StoneMarker,
                        "Her marks, in Father's cipher. The last one is yesterday.")),

                Scene("tower_end"),
            };
            EditorUtility.SetDirty(m76);

            // ---------------------------------------------------------------
            // 77 — THE EMPTY CELL. Suzu walks her brother down the mountain; Fumi
            // stays. The evidence is in rooms the guards are burning.
            var m77 = P_("S77_EmptyCell");
            m77.id = 77; m77.missionName = "THE EMPTY CELL"; m77.missionType = "INVESTIGATION";
            m77.baseShards = 5; m77.applyTheme = true; m77.theme = Core.EnvThemeId.Fortress;
            m77.briefing = "Suzu is taking Kanta down the mountain. Fumi stays. An empty cell has a transfer record, and the guards have been ordered to burn every record in the wing.";
            m77.debrief = "The transfer order is in Kagehira's own hand: Aiko moved to the inner fortress, to the warlord himself. His elite guard stands between the outer fortress and his hall.";
            m77.dressing = new[] { DressingKind.BurnedHome, DressingKind.MissingNotice,
                DressingKind.KagehiraBanners, DressingKind.EmptyHome };
            m77.challenge = MissionChallenge.UnderTime; m77.challengeShards = 3;
            m77.stages = new[]
            {
                Scene("records_open"),

                St(StageGoal.Investigate, "THE WARDEN'S LEDGERS", "FUMI KNOWS HOW THEY FILE",
                    count: 2, checkpoint: true),

                Scene("records_smoke"),

                St(StageGoal.Wave, "THE BURNERS", "THEY ARE BURNING THE ROOMS",
                    spawn: new[] { H, A }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.Defend, "THE RECORDS ROOM", "SAVE WHAT HASN'T BURNED",
                    duration: 30f, point: new Vector3(0f, 0f, 10f), spawn: new[] { P, R, A },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Look("THE TRANSFER ORDER", "IN HIS OWN HAND", true,
                    Prop("order", "AN ORDER, SEALED WITH THE SERPENT", new Vector3(-5f, 0f, 12f), StoryPropShape.CommandPost,
                        "\"The daughter to the inner hall. To me.\" His own hand. Not a clerk's."),
                    Prop("cases", "THREE EMPTY KEY CASES", new Vector3(5f, 0f, 12f), StoryPropShape.KeyPiece,
                        "Three cases, cut to fit the keys. He's been waiting for all of them.")),

                St(StageGoal.Escape, "OUT OF THE BURNING WING", "THE ROOF IS GOING",
                    duration: 35f, point: new Vector3(10f, 0f, -12f), spawn: new[] { H }),

                Scene("records_end"),
            };
            EditorUtility.SetDirty(m77);

            // ---------------------------------------------------------------
            // 78 — THE IRON GUARD. Night. Kagehira's shield in Toku's own steel,
            // fighting as a unit with a captain who calls the changes. Take the
            // captain, and the line breaks.
            var m78 = P_("S78_IronGuard");
            m78.id = 78; m78.missionName = "THE IRON GUARD"; m78.missionType = "BOSS";
            m78.baseShards = 6; m78.applyTheme = true; m78.theme = Core.EnvThemeId.Fortress;
            m78.briefing = "The iron hall. Kagehira's Iron Guard hold it as one body, and their captain calls every change. Toku came up the mountain for this. It is his steel they are wearing.";
            m78.debrief = "Toku stood in front of his own mark on their armour, and the Iron Guard broke when their captain fell. They were Goro's men once. The inner gate is ahead, and the last commander.";
            m78.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.EmptyHome };
            m78.challenge = MissionChallenge.UnderTime; m78.challengeShards = 4;
            m78.stages = new[]
            {
                Scene("iron_open"),

                St(StageGoal.Reach, "THE IRON HALL", "KAGEHIRA'S SHIELD",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("HIS OWN STEEL", "TOKU'S MARK", true,
                    Prop("plate", "TOKU'S MARK, ON EVERY PLATE", new Vector3(-5f, 0f, 11f), StoryPropShape.KeyPiece,
                        "Every breastplate. He made the steel for Yorune's gate, and they wore it here."),
                    Prop("colours", "GORO'S OLD COLOURS, UNDER THE SERPENT", new Vector3(5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "Goro's toll-road colours, painted over. These were his men.")),

                Scene("iron_toku"),

                St(StageGoal.Wave, "THE GUARD AS ONE BODY", "HE CALLS THE CHANGES",
                    spawn: new[] { E, E, R }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("iron_captain"),

                // PHASE 1 — the captain behind his line. The hall goes dark.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE CAPTAIN OF THE GUARD",
                    banner = "THE SECOND OF NINE",
                    foeDef = "ironguard",
                    spawn = new[] { E },
                    bossHealthGate = 0.5f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "THE UNIT CLOSES RANKS", "IN THE DARK",
                    spawn: new[] { M, E }),

                St(StageGoal.BossFight, "TAKE THE CAPTAIN", "AND THE LINE BREAKS", checkpoint: true),

                Look("THE LINE BREAKS", "THEY THROW IT DOWN", false,
                    Prop("thrown", "PLATE, THROWN DOWN", new Vector3(0f, 0f, -12f), StoryPropShape.Supply,
                        "They're taking it off. Toku's steel, on the floor, and nobody to call the change.")),

                Scene("iron_end"),
            };
            EditorUtility.SetDirty(m78);

            // ---------------------------------------------------------------
            // 79 — THE INNER GATE. Commander Hoshu, who yielded once, does not
            // yield twice. He fights with the Three Blades' discipline and Goro's
            // strength, and dies at his gate.
            var m79 = P_("S79_InnerGate");
            m79.id = 79; m79.missionName = "THE INNER GATE"; m79.missionType = "BOSS";
            m79.baseShards = 6; m79.applyTheme = true; m79.theme = Core.EnvThemeId.Fortress;
            m79.briefing = "Commander Hoshu holds the inner gate, as he said he would. He yielded once, in a ring in the dust. Daigo says a man only tells you he won't yield twice if he means it.";
            m79.debrief = "Hoshu died at his gate. He had been told for months that the Kurogawa would reach it. The gate opened on the throne hall beyond: lit, and empty.";
            m79.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons };
            m79.challenge = MissionChallenge.UnderTime; m79.challengeShards = 4;
            m79.stages = new[]
            {
                Scene("inner_open"),

                St(StageGoal.Reach, "THE INNER GATE", "HE SAID HE'D BE HERE",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                St(StageGoal.Wave, "THE GATE'S LAST GUARD", "HIS OWN MEN",
                    spawn: new[] { M, R }, onComplete: StageEvent.Ambush, checkpoint: true),

                Look("HE WAS EXPECTING YOU", "FOR MONTHS", true,
                    Prop("orders", "ORDERS: \"THE KUROGAWA WILL COME\"", new Vector3(-5f, 0f, 11f), StoryPropShape.CommandPost,
                        "Dated before the marsh. Kagehira knew I'd reach this gate before I did."),
                    Prop("ring", "A RING, REDRAWN IN THE SNOW", new Vector3(5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "The ring from the duel, drawn again. He's keeping his word.")),

                Scene("inner_hoshu"),

                // PHASE 1 — the discipline.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "COMMANDER HOSHU",
                    banner = "HE WILL NOT YIELD TWICE",
                    foeDef = "finalcommander",
                    spawn = new[] { M },
                    bossHealthGate = 0.6f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "THE THREE BLADES' DISCIPLINE", "HIS KNIVES COME OUT OF THE DARK",
                    spawn: new[] { A, A }),

                Scene("inner_strength"),

                // PHASE 2 — the strength.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "GORO'S STRENGTH",
                    banner = "EVERYTHING, AT ONCE",
                    bossHealthGate = 0.25f,
                    onComplete = StageEvent.Collapse,
                },

                St(StageGoal.BossFight, "HIS LAST STAND", "AT THE GATE ITSELF", checkpoint: true),

                Scene("inner_death"),

                St(StageGoal.Reach, "THROUGH THE GATE", "THE HALL IS LIT",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("inner_end"),
            };
            EditorUtility.SetDirty(m79);

            // ---------------------------------------------------------------
            // 80 — THE WARLORD'S HALL. Night. Every lantern from the drowned road,
            // an empty throne, the last defenders, and behind the throne the one
            // person who is not one of them.
            var m80 = P_("S80_WarlordsHall");
            m80.id = 80; m80.missionName = "THE WARLORD'S HALL"; m80.missionType = "EXPLORATION";
            m80.baseShards = 6; m80.applyTheme = true; m80.theme = Core.EnvThemeId.Castle;
            m80.briefing = "The throne hall of the Iron Fortress. Forty missions of road end here. It is lit, it is quiet, and nobody has come to meet him.";
            m80.debrief = "Kagehira left the fortress before Renzo took the wall. His hall is full of what he collected: every lantern from the drowned road. Behind the throne stood Aiko. Older. Alive.";
            m80.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.EmptyHome,
                DressingKind.MissingNotice };
            m80.challenge = MissionChallenge.UnderTime; m80.challengeShards = 4;
            m80.stages = new[]
            {
                Scene("hall_open"),

                St(StageGoal.Reach, "INTO THE HALL", "LIT, AND EMPTY",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("WHAT HE COLLECTED", "EVERY LANTERN", true,
                    Prop("lanterns", "A HUNDRED LANTERNS, LIT", new Vector3(-6f, 0f, 12f), StoryPropShape.Supply,
                        "The lanterns from the drowned road. Every one. He's been collecting light."),
                    Prop("throne", "AN EMPTY THRONE, COLD", new Vector3(0f, 0f, 15f), StoryPropShape.Shrine,
                        "Cold. He hasn't sat here in days. He left before we took the wall.")),

                St(StageGoal.Investigate, "WHERE IS HE", "HE LEFT SOMETHING",
                    count: 2),

                Scene("hall_gone"),

                St(StageGoal.Wave, "THE HALL'S LAST DEFENDERS", "LEFT BEHIND TO DIE",
                    spawn: new[] { E, A, R }, onComplete: StageEvent.LightsOut, checkpoint: true),

                St(StageGoal.Wave, "BETWEEN THE LANTERNS", "IN THE DARK",
                    spawn: new[] { E, A }),

                Look("BEHIND THE THRONE", "SOMEONE WHO IS NOT ONE OF THEM", false,
                    Prop("thread", "A RED THREAD ON THE THRONE STEP", new Vector3(0f, 0f, -12f), StoryPropShape.Keepsake,
                        "Red thread. Tied round the step, so someone could find their way.")),

                Scene("hall_end"),
            };
            EditorUtility.SetDirty(m80);
        }
    }
}
