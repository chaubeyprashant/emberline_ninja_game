using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 3 — THE SILENT FOREST, missions 21-25, built by hand. The chapter
    /// where Renzo is prey: a thing in the trees that cannot be beaten yet, a camp
    /// that must never know he was there, a killing in the first snow, the
    /// assassins sent for him by name, and the camp that sent them burning in
    /// silence.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter3()
        {
            // ---------------------------------------------------------------
            // 21 — THE HUNTER. A foe that does not die when cut. The player
            // listens for it, learns it is older than Kagehira, and survives it
            // until it decides to leave.
            var m21 = P_("S21_Hunter");
            m21.id = 21; m21.missionName = "THE HUNTER"; m21.missionType = "ENDURE";
            m21.baseShards = 4; m21.nightOverride = true; m21.fog = true;
            m21.applyTheme = true; m21.theme = Core.EnvThemeId.Forest;
            m21.briefing = "Since the tower burned, something has been following Renzo through the forest. It doesn't move like a soldier. It doesn't sound like one either.";
            m21.debrief = "It was not Kagehira's. It was in these trees before him. It let Renzo live, and that was worse: it was measuring him.";
            m21.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m21.challenge = MissionChallenge.UnderTime; m21.challengeShards = 3;
            m21.stages = new[]
            {
                Scene("hunter_open"),

                St(StageGoal.Reach, "INTO THE TREES", "THE FOG COMES IN",
                    point: new Vector3(-8f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Listen, "HOLD STILL AND LISTEN", "SOMETHING IS CIRCLING",
                    spawn: new[] { S }, checkpoint: true),

                Look("READ WHAT IT LEFT", "NOT A SOLDIER", true,
                    Prop("claws", "CUTS IN THE BARK", new Vector3(-4f, 0f, 14f), StoryPropShape.Tracks,
                        "Cut deep, and higher than a man can reach."),
                    Prop("soldier", "ONE OF KAGEHIRA'S MEN", new Vector3(4f, 0f, 16f), StoryPropShape.Body,
                        "His own side's colours. It didn't care whose man he was.")),

                Scene("hunter_shade"),

                // Endure, not a kill: the clock ends it, and it walks away.
                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE IT",
                    banner = "IT DOES NOT DIE WHEN CUT",
                    duration = 30f,
                    foeDef = "paleshade",
                    spawn = new[] { S },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                St(StageGoal.Escape, "GET OUT OF ITS GROUND", "BEFORE IT CHANGES ITS MIND",
                    duration: 60f, point: new Vector3(15f, 0f, -12f), spawn: new[] { A, N }),

                Scene("hunter_end"),
            };
            EditorUtility.SetDirty(m21);

            // ---------------------------------------------------------------
            // 22 — NO FOOTPRINTS. Pure stealth: every kill is optional and every
            // one of them is loud. The alarm ends the mission's point, not its run.
            var m22 = P_("S22_NoFootprints");
            m22.id = 22; m22.missionName = "NO FOOTPRINTS"; m22.missionType = "STEALTH";
            m22.baseShards = 5; m22.applyTheme = true; m22.theme = Core.EnvThemeId.Bamboo;
            m22.briefing = "The forest camp counts its own. Kill one and they count to twenty-seven. Get in, find who is paying them, and leave it exactly as you found it.";
            m22.debrief = "Transfer orders with Aiko's name on them, three weeks old. Renzo left the camp exactly as he found it, minus one document.";
            m22.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.EmptyHome, DressingKind.AbandonedWeapons };
            m22.challenge = MissionChallenge.NoAlarm; m22.challengeShards = 4;
            m22.stages = new[]
            {
                Scene("nofoot_open"),

                Split("INTO THE CAMP", "THE SCAFFOLD, OR THE STREAM",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { R }, new[] { A }),

                St(StageGoal.Stealth, "PAST THE BAMBOO SCAFFOLD", "ARCHERS ABOVE",
                    spawn: new[] { R, R }, checkpoint: true),

                Look("SEARCH THE COMMAND TENT", "WHO IS PAYING THEM", true,
                    Prop("transfer", "TRANSFER ORDERS", new Vector3(2f, 0f, 16f), StoryPropShape.CommandPost,
                        "\"The Kurogawa girl — north, through the clearing.\" Three weeks old."),
                    Prop("cot", "A COT, STILL WARM", new Vector3(-3f, 0f, 18f), StoryPropShape.Camp,
                        "Slept in last night. Whoever reads these orders isn't far.")),

                St(StageGoal.Stealth, "LEAVE THE WAY YOU CAME", "THE OTHER PATROL IS WAKING",
                    spawn: new[] { A, N }, onComplete: StageEvent.RouteWakes, checkpoint: true),

                St(StageGoal.Escape, "THROUGH THE ONLY EXIT", "THE LAST PATROL",
                    duration: 45f, point: new Vector3(0f, 0f, -16f), spawn: new[] { N }),

                Scene("nofoot_end"),
            };
            EditorUtility.SetDirty(m22);

            // ---------------------------------------------------------------
            // 23 — BLOOD ON SNOW. First snow: tracks stay, and so does blood.
            // Used once, and it is the mission.
            var m23 = P_("S23_BloodOnSnow");
            m23.id = 23; m23.missionName = "BLOOD ON SNOW"; m23.missionType = "INVESTIGATION";
            m23.baseShards = 4; m23.snow = true; m23.applyTheme = true; m23.theme = Core.EnvThemeId.Mountain;
            m23.briefing = "The orders pointed north, to a clearing where someone died last night. The first snow fell on it. Nothing that happened there has been covered yet.";
            m23.debrief = "One of Kagehira's own officers, killed by his own side. His last letter says why: he refused to take a child north.";
            m23.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons, DressingKind.KagehiraBanners };
            m23.challenge = MissionChallenge.UnderTime; m23.challengeShards = 3;
            m23.stages = new[]
            {
                Scene("snow_open"),

                Look("READ THE CLEARING", "THE SNOW KEEPS EVERYTHING", true,
                    Prop("officer", "A DEAD OFFICER", new Vector3(-5f, 0f, 10f), StoryPropShape.Body,
                        "One of theirs. An officer. Killed from behind."),
                    Prop("prints", "THREE SETS OF PRINTS", new Vector3(3f, 0f, 13f), StoryPropShape.Tracks,
                        "Three sets, light, in step. They came together and left together."),
                    Prop("drag", "DRAG MARKS", new Vector3(8f, 0f, 9f), StoryPropShape.Tracks,
                        "They dragged something out of here. Papers, by the width.")),

                St(StageGoal.Reach, "FOLLOW THE TRACKS", "WHERE THEY WENT",
                    point: new Vector3(12f, 0f, 14f), checkpoint: true),

                Look("SEARCH HIS CAMP", "WHAT HE REFUSED", true,
                    Prop("letter", "HIS LAST LETTER", new Vector3(14f, 0f, 17f), StoryPropShape.Keepsake,
                        "\"I will not take a child north.\" He refused, and they killed him for it."),
                    Prop("orders", "HIS ORDERS", new Vector3(10f, 0f, 19f), StoryPropShape.CommandPost,
                        "Orders to move a child north, and his name crossed out.")),

                Scene("snow_return"),

                St(StageGoal.Wave, "THE CLEANERS", "THEY CAME BACK FOR THE BODY",
                    spawn: new[] { H, P }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Wave, "FROM THE TREELINE", "MORE OF THEM",
                    spawn: new[] { A, A }),

                St(StageGoal.Reach, "BACK TO THE ROAD", "THE SNOW WILL COVER IT",
                    point: new Vector3(0f, 0f, -15f)),

                Scene("snow_end"),
            };
            EditorUtility.SetDirty(m23);

            // ---------------------------------------------------------------
            // 24 — THE THREE BLADES. Three who fight as one; the last changes
            // her tactics when she is alone, and is faster than the other two.
            var m24 = P_("S24_ThreeBlades");
            m24.id = 24; m24.missionName = "THE THREE BLADES"; m24.missionType = "BOSS";
            m24.baseShards = 5; m24.applyTheme = true; m24.theme = Core.EnvThemeId.Forest;
            m24.briefing = "Three sets of prints in the snow. Three assassins on the only road north, and they already know Renzo's name.";
            m24.debrief = "They were sent for Renzo specifically. The last Blade wore a silk cord: the mark of the forest camp's master, who is paying attention now.";
            m24.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.KagehiraBanners, DressingKind.EmptyHome };
            m24.challenge = MissionChallenge.UnderTime; m24.challengeShards = 3;
            m24.stages = new[]
            {
                Scene("blades_open"),

                St(StageGoal.Reach, "THE PATH THEY USE", "NO ONE ELSE WALKS HERE",
                    point: new Vector3(0f, 0f, 12f), checkpoint: true),

                St(StageGoal.Wave, "TWO OF THE THREE", "THEY MOVE AS ONE",
                    spawn: new[] { A, A }, onComplete: StageEvent.BossArrives, checkpoint: true),

                Scene("blades_last"),

                St(StageGoal.Duel, "THE LAST BLADE", "FASTER ALONE",
                    foeDef: "threeblades", spawn: new[] { A }, checkpoint: true),

                Look("SEARCH HER", "WHO SENT THEM", false,
                    Prop("cord", "A SILK CORD", Vector3.zero, StoryPropShape.Keepsake,
                        "A silk cord at her wrist. The forest camp's master marks his own with these."),
                    Prop("contract", "A CONTRACT", new Vector3(2f, 0f, 2f), StoryPropShape.CommandPost,
                        "My name, and a price. Someone above Goro wants me dead by name.")),

                Scene("blades_end"),
            };
            EditorUtility.SetDirty(m24);

            // ---------------------------------------------------------------
            // 25 — THE SILENT CAMP. Sabotage under silence: three fires while the
            // camp sleeps, the last in the commander's own tent.
            var m25 = P_("S25_SilentCamp");
            m25.id = 25; m25.missionName = "THE SILENT CAMP"; m25.missionType = "SABOTAGE";
            m25.baseShards = 5; m25.nightOverride = true; m25.applyTheme = true; m25.theme = Core.EnvThemeId.Bamboo;
            m25.briefing = "The silk cord leads to the Silent Camp. Three fires, while it sleeps: the stores, the archers' scaffold, and the commander's own tent. Not one bell.";
            m25.debrief = "The camp burned without a bell rung. Its commander was hunting the Black Seal, not Renzo. Renzo was only in the way.";
            m25.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart, DressingKind.AbandonedWeapons };
            m25.challenge = MissionChallenge.NoAlarm; m25.challengeShards = 4;
            m25.stages = new[]
            {
                Scene("camp_open"),

                Split("INTO THE SILENT CAMP", "THE CULVERT, OR THE WALL",
                    new Vector3(11f, 0f, 10f), new Vector3(-11f, 0f, 10f),
                    new[] { B }, new[] { R }),

                St(StageGoal.Stealth, "TO THE STORES", "THE CAMP IS ASLEEP",
                    spawn: new[] { B, B }, checkpoint: true),

                Look("FIRST FIRE: THE STORES", "ONE", true,
                    Prop("stores", "OIL AND RICE", new Vector3(12f, 0f, 14f), StoryPropShape.Supply,
                        "Oil jars stacked against the rice. This goes up fast.")),

                St(StageGoal.Stealth, "TO THE ARCHERS' SCAFFOLD", "THE LAMPS BURN DOWN",
                    spawn: new[] { R, N }, onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("SECOND FIRE: THE SCAFFOLD", "TWO", true,
                    Prop("signals", "SIGNAL FIRES, LAID AND UNLIT", new Vector3(-10f, 0f, 16f), StoryPropShape.Supply,
                        "Three signal fires, laid and never lit. They're lit now.")),

                Look("THIRD FIRE: HIS TENT", "THREE", true,
                    Prop("maps", "THE COMMANDER'S MAPS", new Vector3(0f, 0f, 20f), StoryPropShape.CommandPost,
                        "Every old road, every shrine marked. He was hunting the Seal, not me."),
                    Prop("cords", "A BOX OF SILK CORDS", new Vector3(3f, 0f, 21f), StoryPropShape.Keepsake,
                        "He marked every killer he sent. There are a lot of cords.")),

                St(StageGoal.Escape, "INTO THE TREES BEFORE IT CATCHES", "IT'S CATCHING",
                    duration: 45f, point: new Vector3(0f, 0f, -16f), spawn: new[] { A }),

                Scene("camp_end"),
            };
            EditorUtility.SetDirty(m25);
        }
    }
}
