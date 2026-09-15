using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 6 — THE DROWNED TEMPLE, missions 51-55, built by hand. His father's
    /// journal, and the night Yorune burned walked from inside the memory: the
    /// village alive, the search that was not a raid, the door his father held
    /// against a young Goro, and his mother's lantern line through the fire.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter6()
        {
            // ---------------------------------------------------------------
            // 51 — FATHER'S JOURNAL. The pages are scattered through the upper
            // halls; each one read changes what Renzo sees. The watchers wake
            // for the last page, and the fog comes up through the floor.
            var m51 = P_("S51_FathersJournal");
            m51.id = 51; m51.missionName = "FATHER'S JOURNAL"; m51.missionType = "INVESTIGATION";
            m51.baseShards = 5; m51.applyTheme = true; m51.theme = Core.EnvThemeId.Temple;
            m51.briefing = "The first key is in Renzo's coat. The journal that maps the second is in the temple's upper halls, torn apart by someone who could not read it.";
            m51.debrief = "The journal begins the year before Yorune burned. His father knew Kagehira was coming. The last page is a date, the night of the fire, and a place to stand.";
            m51.dressing = new[] { DressingKind.EmptyHome, DressingKind.MissingNotice,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m51.challenge = MissionChallenge.NoAlarm; m51.challengeShards = 3;
            m51.stages = new[]
            {
                Scene("journal_open"),

                St(StageGoal.Investigate, "THE FIRST PAGES", "HIS HAND",
                    count: 2, checkpoint: true),

                Look("THE YEAR BEFORE", "HE KNEW", true,
                    Prop("warning", "A PAGE: \"THEY ARE COMING\"", new Vector3(-6f, 0f, 12f), StoryPropShape.CommandPost,
                        "\"Kagehira has asked twice. The third time he will not ask.\" A year before the fire."),
                    Prop("keys", "THREE KEYS, DRAWN", new Vector3(5f, 0f, 13f), StoryPropShape.KeyPiece,
                        "Three keys, drawn in his hand. The first is in my coat. He drew the second in water.")),

                St(StageGoal.Listen, "SOMETHING READS OVER YOUR SHOULDER", "THE WATCHERS",
                    spawn: new[] { S, S }, onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Investigate, "THE UPPER HALL", "THE PAGES THEY TORE",
                    count: 2),

                Scene("journal_watchers"),

                St(StageGoal.Wave, "THE TEMPLE'S WATCHERS", "FOR THE LAST PAGE",
                    spawn: new[] { M, S, M }, checkpoint: true),

                Look("THE LAST PAGE", "A DATE, AND A PLACE TO STAND", false,
                    Prop("date", "THE LAST PAGE", new Vector3(0f, 0f, -12f), StoryPropShape.Keepsake,
                        "The night Yorune burned. \"Remember it, Ren. Stand where I stood, and remember.\"")),

                Scene("journal_end"),
            };
            EditorUtility.SetDirty(m51);

            // ---------------------------------------------------------------
            // 52 — THE LAST NIGHT. Dawn light that is really dusk. The village
            // whole, the people alive, and the player walking through it knowing.
            // The only enemies are the scouts nobody saw that night.
            var m52 = P_("S52_TheLastNight");
            m52.id = 52; m52.missionName = "THE LAST NIGHT"; m52.missionType = "MEMORY";
            m52.baseShards = 4; m52.applyTheme = true; m52.theme = Core.EnvThemeId.VillageDawn;
            m52.briefing = "Stand where he stood, the journal says. Renzo closes his eyes on the temple floor and opens them in Yorune, on its last evening.";
            m52.debrief = "The evening was ordinary. His father was not: he was packing something wrapped in cloth. And there were scouts on the ridge before the first fire, watching the house.";
            m52.dressing = new[] { DressingKind.HidingVillagers, DressingKind.EmptyHome,
                DressingKind.DestroyedCart };
            m52.challenge = MissionChallenge.NoAlarm; m52.challengeShards = 3;
            m52.stages = new[]
            {
                Scene("lastnight_open"),

                St(StageGoal.Reach, "WALK INTO YORUNE", "TEN YEARS AGO",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                Look("THE VILLAGE, ALIVE", "EVERYONE IS HERE", true,
                    Prop("post", "FATHER'S TRAINING POST", new Vector3(-7f, 0f, 12f), StoryPropShape.TrainingPost,
                        "New wood. I haven't worn the grooves into it yet. I will."),
                    Prop("home", "HOME, WITH THE LAMP LIT", new Vector3(6f, 0f, 13f), StoryPropShape.Homestead,
                        "The lamp's lit. Mother's inside. I could walk in. It wouldn't change anything."),
                    Prop("thread", "AIKO'S RED THREAD", new Vector3(0f, 0f, 15f), StoryPropShape.Keepsake,
                        "Tied round the post, where she tied it every night. So she'd find her way back.")),

                Scene("memory_lastnight"),

                St(StageGoal.Stealth, "THE RIDGE, UNSEEN", "SOMEBODY IS WATCHING THE HOUSE",
                    spawn: new[] { N, A }, onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("WHAT THE SCOUTS SAW", "THEY KNEW WHICH HOUSE", false,
                    Prop("chalk", "A CHALK MARK ON OUR DOOR", new Vector3(8f, 0f, -10f), StoryPropShape.StoneMarker,
                        "Marked before the fire. They didn't search Yorune. They came for one house."),
                    Prop("bundle", "THE CLOTH BUNDLE", new Vector3(3f, 0f, -12f), StoryPropShape.Cache,
                        "What he was packing. I never asked. I was nine, and it was just cloth.")),

                St(StageGoal.Reach, "THE FIRST FIRE", "ON THE RIDGE",
                    point: new Vector3(-10f, 0f, -12f)),

                Scene("lastnight_end"),
            };
            EditorUtility.SetDirty(m52);

            // ---------------------------------------------------------------
            // 53 — THE BURNING VILLAGE. What actually happened. The village burns
            // around the memory and the searchers go house to house; the roofs
            // come down on the route to his father's door.
            var m53 = P_("S53_BurningVillage");
            m53.id = 53; m53.missionName = "THE BURNING VILLAGE"; m53.missionType = "COMBAT";
            m53.baseShards = 5; m53.applyTheme = true; m53.theme = Core.EnvThemeId.BurningVillage;
            m53.briefing = "The next page is the fire. Renzo has run from this memory for ten years. This time he walks into it, sword drawn, and looks.";
            m53.debrief = "The raiders were not raiding. They opened every house, pulled out every drawer, and took nothing. The memory broke at his father's door. Renzo could not go in then.";
            m53.dressing = new[] { DressingKind.BurnedHome, DressingKind.HidingVillagers,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m53.challenge = MissionChallenge.UnderTime; m53.challengeShards = 3;
            m53.stages = new[]
            {
                Scene("burning_open"),

                St(StageGoal.Reach, "THROUGH THE SMOKE", "YORUNE BURNS",
                    point: new Vector3(-8f, 0f, 10f), onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Wave, "THE SEARCHERS", "THEY ARE NOT RAIDING",
                    spawn: new[] { B, H }, checkpoint: true),

                Look("EVERY HOUSE OPENED", "NOTHING TAKEN", true,
                    Prop("drawers", "EVERY DRAWER PULLED OUT", new Vector3(-6f, 0f, 13f), StoryPropShape.Homestead,
                        "The rice is still here. The silver. They pulled out every drawer and took nothing."),
                    Prop("neighbour", "OLD KENJI, AT HIS DOOR", new Vector3(4f, 0f, 14f), StoryPropShape.Body,
                        "He told them he didn't know. He didn't. They asked anyway.")),

                Scene("memory_burning"),

                St(StageGoal.Wave, "ARCHERS ON THE ROOFS", "THE FIRE SPREADS",
                    spawn: new[] { R, R, B }, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Escape, "TO FATHER'S DOOR", "THE ROOFS ARE COMING DOWN",
                    duration: 45f, point: new Vector3(10f, 0f, -12f), spawn: new[] { H }),

                Scene("burning_door"),
            };
            EditorUtility.SetDirty(m53);

            // ---------------------------------------------------------------
            // 54 — THE SWORDMASTER. His father's stand, as the journal tells it:
            // hold the door for the lantern line, then a young Goro. The door
            // holds; the man does not. Goro lives — he has thirty years of toll
            // road ahead of him — so the last phase is endured, not won.
            var m54 = P_("S54_Swordmaster");
            m54.id = 54; m54.missionName = "THE SWORDMASTER"; m54.missionType = "BOSS";
            m54.baseShards = 5; m54.applyTheme = true; m54.theme = Core.EnvThemeId.BurningVillage;
            m54.briefing = "The page Renzo has never been able to read: his father at the door. The journal was finished by someone else. It says he held it long enough.";
            m54.debrief = "His father held the door long enough for the Seal to be carried out. He did not hold it for himself. The raider captain at the door was Goro, young, and his father gave him the scar he wore to his death.";
            m54.dressing = new[] { DressingKind.BurnedHome, DressingKind.KagehiraBanners,
                DressingKind.BloodTrail, DressingKind.AbandonedWeapons };
            m54.challenge = MissionChallenge.UnderTime; m54.challengeShards = 3;
            m54.stages = new[]
            {
                Scene("sword_open"),

                St(StageGoal.Reach, "STAND WHERE HE STOOD", "THE DOOR",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                St(StageGoal.Defend, "HOLD THE DOOR", "AS HE HELD IT",
                    duration: 35f, point: new Vector3(0f, 0f, 8f), spawn: new[] { B, P },
                    onComplete: StageEvent.Reinforcements, checkpoint: true),

                Look("WHAT HE WAS HOLDING IT FOR", "THE LANTERN LINE", false,
                    Prop("line", "LANTERNS, GOING UP THE RIDGE", new Vector3(-7f, 0f, 12f), StoryPropShape.Tracks,
                        "Mother's line. Every lantern that gets up that ridge is a family. He's counting them."),
                    Prop("door", "THE DOOR AT HIS BACK", new Vector3(5f, 0f, 11f), StoryPropShape.Passage,
                        "Aiko and I were behind it. I heard all of this. I never saw it.")),

                Scene("sword_goro"),

                // PHASE 1 — the swordmaster against the raider captain, and the
                // roof over the door comes down between them.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE RAIDER CAPTAIN",
                    banner = "A YOUNG GORO",
                    foeDef = "goro",
                    spawn = new[] { C },
                    bossHealthGate = 0.6f,
                    onComplete = StageEvent.Collapse,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "HIS RAIDERS COME THROUGH THE FIRE", "THE DOOR, FROM BOTH SIDES",
                    spawn: new[] { A, P }),

                Scene("sword_scar"),

                // PHASE 2 — the scar. Goro breaks, and the rain comes too late.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "GIVE HIM THE SCAR",
                    banner = "THE SWORDMASTER",
                    bossHealthGate = 0.3f,
                    onComplete = StageEvent.RainStarts,
                },

                // The memory will not let him win: Goro walks out of Yorune alive.
                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "THE DOOR HOLDS",
                    banner = "THE MAN BEHIND IT DOES NOT",
                    duration = 30f,
                    spawn = new[] { A, A },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Scene("sword_end"),
            };
            EditorUtility.SetDirty(m54);

            // ---------------------------------------------------------------
            // 55 — MOTHER'S CHOICE. An escort through fire with a child at the
            // head of the line: cut the penned families loose, get them up the
            // ridge, and watch her turn back for the last child.
            var m55 = P_("S55_MothersChoice");
            m55.id = 55; m55.missionName = "MOTHER'S CHOICE"; m55.missionType = "ESCORT";
            m55.baseShards = 5; m55.applyTheme = true; m55.theme = Core.EnvThemeId.BurningVillage;
            m55.briefing = "While his father held the door, his mother led the village out by lantern. The journal's last pages are in her hand.";
            m55.debrief = "His mother carried the Seal out of Yorune and gave it to the child at the head of the line to hold. The villagers reached the ridge. She turned back for the last child.";
            m55.dressing = new[] { DressingKind.HidingVillagers, DressingKind.BurnedHome,
                DressingKind.DestroyedCart, DressingKind.PrisonerCamp };
            m55.challenge = MissionChallenge.NoCivilianDeaths; m55.challengeShards = 3;
            m55.stages = new[]
            {
                Scene("mother_open"),

                St(StageGoal.Escort, "THE LANTERN LINE", "BEHIND MOTHER",
                    spawn: new[] { B, R }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.FreePrisoners, "THE PENNED FAMILIES", "CUT THEM LOOSE",
                    count: 3, checkpoint: true),

                Scene("mother_seal"),

                St(StageGoal.Wave, "BOMBS ON THE PATH", "THEY SAW THE LANTERNS",
                    spawn: new[] { O, O, B }, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Escort, "UP THE RIDGE", "THE RAIDERS AT THE END OF IT",
                    spawn: new[] { R, O }),

                Look("THE RIDGE", "EVERY LANTERN COUNTED", false,
                    Prop("families", "THE VILLAGE, ON THE RIDGE", new Vector3(-6f, 0f, -12f), StoryPropShape.Camp,
                        "Forty lanterns up the ridge. Forty families. The fire took the village, not the people."),
                    Prop("lantern", "MOTHER'S LANTERN, SET DOWN", new Vector3(4f, 0f, -13f), StoryPropShape.Supply,
                        "She set it down here. She didn't need a light to go back.")),

                Scene("mother_end"),
            };
            EditorUtility.SetDirty(m55);
        }
    }
}
