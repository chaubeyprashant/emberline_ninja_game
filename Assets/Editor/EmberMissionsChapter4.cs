using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 4 — GORO'S TERRITORY, missions 31-35, built by hand. Renzo takes the
    /// war to Goro: the fortified road in fog, wagons in the rain, the pens burning
    /// at night, the executioner at dawn, and the first real army in a storm.
    /// Weather and night come from the campaign table; the events here change the
    /// world mid-mission so no stage plays on the same stage it opened on.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter4()
        {
            // ---------------------------------------------------------------
            // 31 — THE FORTRESS ROAD. Fog on the road, a serpent every hundred
            // paces, and the fog thickens as Renzo climbs to see what it hides.
            var m31 = P_("S31_FortressRoad");
            m31.id = 31; m31.missionName = "THE FORTRESS ROAD"; m31.missionType = "EXPLORATION";
            m31.baseShards = 4; m31.applyTheme = true; m31.theme = Core.EnvThemeId.Castle;
            m31.briefing = "The Pale Shade said Aiko was moved to the toll-captain's country. The fortress road runs straight into it, and the fog is on it.";
            m31.debrief = "Goro has fortified the whole valley. From the ridge, through the fog, a line of prisoner wagons moves toward his fortress.";
            m31.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m31.challenge = MissionChallenge.NoAlarm; m31.challengeShards = 3;
            m31.stages = new[]
            {
                Scene("road31_open"),

                St(StageGoal.Reach, "WALK THE FORTRESS ROAD", "A SERPENT EVERY HUNDRED PACES",
                    point: new Vector3(-10f, 0f, 10f), checkpoint: true),

                Look("READ THE ROAD", "NOT A TOLL POST ANY MORE", true,
                    Prop("banner", "A NEW SERPENT BANNER", new Vector3(-12f, 0f, 13f), StoryPropShape.StoneMarker,
                        "New cloth. Put up this month. He wants the valley to know whose it is."),
                    Prop("post", "A TOLL POST, WALLED IN", new Vector3(-6f, 0f, 15f), StoryPropShape.Supply,
                        "The toll post's a garrison now. He isn't collecting tolls any more."),
                    Prop("ruts", "HEAVY WAGON RUTS", new Vector3(-2f, 0f, 12f), StoryPropShape.Tracks,
                        "Heavy, and a lot of them. All going toward the fortress. None coming back.")),

                St(StageGoal.Stealth, "PAST THE FIRST GARRISON", "THEY WATCH THE ROAD",
                    spawn: new[] { P, R }, checkpoint: true),

                St(StageGoal.Wave, "THE ROAD'S GARRISON", "NOBODY PASSES",
                    spawn: new[] { P, H }, onComplete: StageEvent.Reinforcements),

                St(StageGoal.Reach, "UP TO THE RIDGE", "SEE THE VALLEY",
                    point: new Vector3(8f, 0f, 18f), onComplete: StageEvent.FogRolls, checkpoint: true),

                Scene("road31_wagons"),
            };
            EditorUtility.SetDirty(m31);

            // ---------------------------------------------------------------
            // 32 — PRISONER WAGONS. Rain on the road. The wagons are moving:
            // free who you can while the escort fights and the drivers whip on.
            var m32 = P_("S32_PrisonerWagons");
            m32.id = 32; m32.missionName = "PRISONER WAGONS"; m32.missionType = "RESCUE";
            m32.baseShards = 5; m32.applyTheme = true; m32.theme = Core.EnvThemeId.Castle;
            m32.briefing = "Wagons full of people, rolling for Goro's fortress in the rain. The drivers won't stop, so Renzo stops the wagons.";
            m32.debrief = "The prisoners came from a dozen villages. Goro has been emptying the valley, and the freed talk of camps that are not prisons. Pens.";
            m32.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.DestroyedCart,
                DressingKind.KagehiraBanners, DressingKind.BloodTrail };
            m32.challenge = MissionChallenge.SaveAllPrisoners; m32.challengeShards = 3;
            m32.stages = new[]
            {
                Scene("wagons_open"),

                Split("INTERCEPT THE CONVOY", "HEAD THEM OFF, OR HIT THE TAIL",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { B }, new[] { R }),

                St(StageGoal.Chase, "CATCH THE FIRST WAGON", "THE DRIVER WHIPS ON",
                    duration: 40f, spawn: new[] { B }, onComplete: StageEvent.TargetFlees, checkpoint: true),

                St(StageGoal.FreePrisoners, "FREE THE FIRST WAGON", "CUT THEM LOOSE",
                    count: 2, checkpoint: true),

                St(StageGoal.Wave, "THE ESCORT TURNS BACK", "THEY WANT THEM BACK",
                    spawn: new[] { P, A }),

                St(StageGoal.FreePrisoners, "FREE THE SECOND WAGON", "MORE OF THEM",
                    count: 2, onComplete: StageEvent.Ambush, checkpoint: true),

                Scene("wagons_captain"),

                St(StageGoal.Wave, "THE ESCORT'S CAPTAIN", "THE ROAD IS RUNNING OUT",
                    spawn: new[] { H, B }, checkpoint: true),

                Look("WHAT THEY CARRIED", "A DOZEN VILLAGES", false,
                    Prop("manifest", "THE WAGON MANIFEST", new Vector3(4f, 0f, 6f), StoryPropShape.CommandPost,
                        "Names from a dozen villages. Ages. Trades. He's emptying the valley by the ledger.")),

                Scene("wagons_end"),
            };
            EditorUtility.SetDirty(m32);

            // ---------------------------------------------------------------
            // 33 — BROKEN CHAINS. Night. Two pens, two guard rotations: the
            // second wakes when the first goes quiet. The man in the collar is
            // Daigo, and the pens burn on their own pitch.
            var m33 = P_("S33_BrokenChains");
            m33.id = 33; m33.missionName = "BROKEN CHAINS"; m33.missionType = "SABOTAGE";
            m33.baseShards = 5; m33.applyTheme = true; m33.theme = Core.EnvThemeId.Fortress;
            m33.briefing = "The nearest pens hold forty guards and more prisoners. Free them in the dark, then burn the pens so they can't be filled again.";
            m33.debrief = "The camp is ash and its prisoners are in the hills. Its record sent Aiko to 'the execution ground' two months ago, and a giant named Daigo walked out of the collar to hold the gate.";
            m33.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.KagehiraBanners,
                DressingKind.BurnedHome, DressingKind.MissingNotice };
            m33.challenge = MissionChallenge.UnderTime; m33.challengeShards = 3;
            m33.stages = new[]
            {
                Scene("chains_open"),

                St(StageGoal.Stealth, "SILENCE THE FIRST PEN'S GUARD", "THE FIRST ROTATION",
                    spawn: new[] { P, R }, onComplete: StageEvent.RouteWakes, checkpoint: true),

                St(StageGoal.FreePrisoners, "OPEN THE FIRST PEN", "THE COLLARED ONE",
                    count: 3, checkpoint: true),

                Scene("chains_daigo"),

                St(StageGoal.Wave, "THE SECOND ROTATION WAKES", "THE OTHER PEN",
                    spawn: new[] { P, B, O }, onComplete: StageEvent.BossArrives, checkpoint: true),

                Look("THE CAMP RECORDS", "WHERE SHE WENT", true,
                    Prop("record", "THE TRANSFER RECORD", new Vector3(-6f, 0f, 14f), StoryPropShape.CommandPost,
                        "\"Aiko Kurogawa. Transferred, two months ago. To the execution ground.\""),
                    Prop("pitch", "BARRELS OF PITCH", new Vector3(5f, 0f, 16f), StoryPropShape.Supply,
                        "Pitch for the watchfires. Enough to burn every pen in the camp.")),

                Scene("chains_commander"),

                St(StageGoal.Wave, "THE COMMANDER, IN THE BURNING YARD", "THE PENS BURN",
                    spawn: new[] { H, H }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.Reach, "OUT THROUGH THE SMOKE", "ASH",
                    point: new Vector3(0f, 0f, -15f)),

                Scene("chains_end"),
            };
            EditorUtility.SetDirty(m33);

            // ---------------------------------------------------------------
            // 34 — THE EXECUTION GROUND. Night into dawn. A clock the player
            // cannot see: the executioner walks the line. Everyone on the
            // platform lives, none of them is her, and the executioner is a
            // fight of his own, not a wave.
            var m34 = P_("S34_ExecutionGround");
            m34.id = 34; m34.missionName = "THE EXECUTION GROUND"; m34.missionType = "DEFENSE";
            m34.baseShards = 5; m34.applyTheme = true; m34.theme = Core.EnvThemeId.Fortress;
            m34.briefing = "The execution ground is north of the pens, and they start at dawn. There is no tomorrow for this one.";
            m34.debrief = "Everyone on the platform lived, and the executioner did not. None of them was Aiko: the ledger says she was moved again, the night before.";
            m34.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.BloodTrail,
                DressingKind.KagehiraBanners, DressingKind.MissingNotice };
            m34.challenge = MissionChallenge.SaveAllPrisoners; m34.challengeShards = 3;
            m34.stages = new[]
            {
                Scene("exec_open"),

                St(StageGoal.Escape, "REACH THE PLATFORM", "HE WALKS THE LINE",
                    duration: 45f, point: new Vector3(0f, 0f, 14f), spawn: new[] { R },
                    onComplete: StageEvent.AlarmTriggered, checkpoint: true),

                St(StageGoal.Wave, "THE EXECUTIONER'S GUARD", "ON THE STEPS",
                    spawn: new[] { A, M }, checkpoint: true),

                St(StageGoal.FreePrisoners, "CUT THEM LOOSE", "EVERY ONE OF THEM",
                    count: 4, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("exec_hold"),

                St(StageGoal.Defend, "HOLD THE PLATFORM", "UNTIL THE LAST ARE LOOSE",
                    duration: 32f, point: new Vector3(0f, 0f, 14f), spawn: new[] { H, R }, checkpoint: true),

                Scene("exec_executioner"),

                St(StageGoal.BossFight, "THE EXECUTIONER", "HE WALKS THE LINE",
                    foeDef: "executioner", spawn: new[] { H }, checkpoint: true),

                Look("THE EXECUTION LEDGER", "NONE OF THEM IS HER", false,
                    Prop("ledger", "THE EXECUTION LEDGER", new Vector3(3f, 0f, 16f), StoryPropShape.CommandPost,
                        "Her name, crossed out. \"Moved, the night before.\" Somebody wanted her alive more than he did.")),

                Scene("exec_end"),
            };
            EditorUtility.SetDirty(m34);

            // ---------------------------------------------------------------
            // 35 — GORO'S ARMY. A battlefield, and the rain comes down when the
            // pike line breaks. Pikes in front, archers behind, powder in the
            // rear, an officer who calls the changes. Tsuru sees his old wall.
            var m35 = P_("S35_GorosArmy");
            m35.id = 35; m35.missionName = "GORO'S ARMY"; m35.missionType = "COMBAT";
            m35.baseShards = 5; m35.applyTheme = true; m35.theme = Core.EnvThemeId.RainyBattlefield;
            m35.briefing = "Goro has stopped sending patrols. He has sent a squad: pikes, bows, powder, and an officer who knows how to use all three.";
            m35.debrief = "The squad is finished. Its orders said to bring Renzo in alive, sealed with a serpent: Kagehira wants to talk.";
            m35.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.KagehiraBanners,
                DressingKind.BloodTrail, DressingKind.DestroyedCart };
            m35.challenge = MissionChallenge.UnderTime; m35.challengeShards = 3;
            m35.stages = new[]
            {
                Scene("army_open"),

                St(StageGoal.Reach, "TAKE THE HIGH GROUND", "A REAL FORMATION",
                    point: new Vector3(-10f, 0f, 10f), checkpoint: true),

                Scene("army_tsuru"),

                St(StageGoal.Wave, "BREAK THE PIKE LINE", "PIKES IN FRONT",
                    spawn: new[] { P, P }, onComplete: StageEvent.RainStarts, checkpoint: true),

                St(StageGoal.Defend, "HOLD WHILE TSURU TAKES THE ARCHERS", "BOWS BEHIND",
                    duration: 30f, point: new Vector3(-10f, 0f, 10f), spawn: new[] { R, R }),

                Look("THE SQUAD'S ORDERS", "ALIVE", true,
                    Prop("orders", "SEALED ORDERS", new Vector3(-6f, 0f, 14f), StoryPropShape.CommandPost,
                        "\"Bring the Kurogawa in alive.\" Kagehira wants to talk. Goro didn't write this."),
                    Prop("powder", "POWDER KEGS", new Vector3(-2f, 0f, 16f), StoryPropShape.Supply,
                        "Powder, in the rain. They meant to use it on the village, not on me.")),

                Scene("army_officer"),

                St(StageGoal.Wave, "THE OFFICER'S LAST STAND", "THE LINE BREAKS",
                    spawn: new[] { M, P, O }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.Reach, "WALK THE FIELD", "WHAT IS LEFT OF IT",
                    point: new Vector3(6f, 0f, -12f)),

                Scene("army_end"),
            };
            EditorUtility.SetDirty(m35);
        }
    }
}
