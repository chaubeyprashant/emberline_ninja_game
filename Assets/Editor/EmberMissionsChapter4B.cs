using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 4 — GORO'S TERRITORY, missions 36-40, built by hand. The smith who
    /// marked every blade, the one village that has to still be there tomorrow
    /// (at night, burning), Goro hunting through snow and fog against orders,
    /// his last wall, and his end on his own gate: lanterns out, then rain.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter4B()
        {
            // ---------------------------------------------------------------
            // 36 — THE BLACKSMITH. Daylight, for once. The man who made the
            // enemy's steel was never on their side. He swings a hammer, and he
            // is not fast; the riders reach the road before he does.
            var m36 = P_("S36_Blacksmith");
            m36.id = 36; m36.missionName = "THE BLACKSMITH"; m36.missionType = "ESCORT";
            m36.baseShards = 4; m36.applyTheme = true; m36.theme = Core.EnvThemeId.Village;
            m36.briefing = "An army needs a smith. Goro's is kept in his own forge above the village of Kiba, chained at night and worked by day.";
            m36.debrief = "Toku is safe in Kiba, and he gave Renzo the mark: every blade he forged under duress can be told from an honest one. Goro will answer the loss by burning the village that hid him.";
            m36.dressing = new[] { DressingKind.HidingVillagers, DressingKind.EmptyHome,
                DressingKind.AbandonedWeapons, DressingKind.KagehiraBanners };
            m36.challenge = MissionChallenge.NoAlarm; m36.challengeShards = 3;
            m36.stages = new[]
            {
                Scene("smith_open"),

                St(StageGoal.Reach, "FIND THE FORGE", "SMOKE ON THE HILL",
                    point: new Vector3(-10f, 0f, 12f), checkpoint: true),

                Look("THE FORGE", "MARKED STEEL", true,
                    Prop("anvil", "THE SMITH'S MARK", new Vector3(-12f, 0f, 15f), StoryPropShape.Supply,
                        "A notch under the tang. Every blade on the rack has it. He's been signing them."),
                    Prop("chain", "A CHAINED DOOR", new Vector3(-7f, 0f, 16f), StoryPropShape.Homestead,
                        "They keep him in his own forge. Chained at night, worked by day.")),

                St(StageGoal.Stealth, "THE SMITH'S KEEPERS", "THEY DON'T WANT HIM DEAD",
                    spawn: new[] { B, R }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.FreePrisoners, "UNCHAIN TOKU", "HE HAS THE HAMMER ALREADY",
                    count: 1, checkpoint: true),

                Scene("smith_toku"),

                St(StageGoal.Escort, "WALK HIM DOWN TO KIBA", "HE IS NOT FAST",
                    spawn: new[] { H, A }, onComplete: StageEvent.Ambush),

                Scene("smith_riders"),

                St(StageGoal.Wave, "GORO'S RIDERS", "THEY REACHED THE ROAD FIRST",
                    spawn: new[] { H, B }, checkpoint: true),

                St(StageGoal.Reach, "INTO KIBA", "THE VILLAGE THAT HID HIM",
                    point: new Vector3(0f, 0f, -15f)),

                Scene("smith_end"),
            };
            EditorUtility.SetDirty(m36);

            // ---------------------------------------------------------------
            // 37 — THE SIEGE. Night, and the village burning. The one time the
            // player defends a place that matters. Three gates; each wave picks
            // a different one, and the east wall comes down.
            var m37 = P_("S37_Siege");
            m37.id = 37; m37.missionName = "THE SIEGE"; m37.missionType = "DEFENSE";
            m37.baseShards = 5; m37.applyTheme = true; m37.theme = Core.EnvThemeId.BurningVillage;
            m37.briefing = "Goro's orders are to burn every village between here and the marsh. Kiba is first, and Kiba is where the freed went.";
            m37.debrief = "Kiba stands. Goro's banner in the field beyond did not advance. It waited, and then it was gone: he has stopped sending men.";
            m37.dressing = new[] { DressingKind.HidingVillagers, DressingKind.BurnedHome,
                DressingKind.KagehiraBanners, DressingKind.DestroyedCart };
            m37.challenge = MissionChallenge.NoCivilianDeaths; m37.challengeShards = 3;
            m37.stages = new[]
            {
                Scene("siege_open"),

                Look("WALK THE WALLS", "THREE GATES", true,
                    Prop("north", "THE NORTH GATE", new Vector3(0f, 0f, 14f), StoryPropShape.StoneMarker,
                        "Toku's barricade. It'll hold one wave. Maybe two."),
                    Prop("east", "THE EAST GATE", new Vector3(12f, 0f, 0f), StoryPropShape.Passage,
                        "A cart across it and nothing else. If they find this one, it goes fast."),
                    Prop("west", "THE WEST GATE", new Vector3(-12f, 0f, 0f), StoryPropShape.Passage,
                        "Daigo says he'll take this one. He means it.")),

                St(StageGoal.Defend, "HOLD THE NORTH GATE", "THE FIRST WAVE",
                    duration: 35f, point: new Vector3(0f, 0f, 14f), spawn: new[] { B, B }, checkpoint: true),

                St(StageGoal.Reach, "THE EAST GATE IS BURNING", "THEY CHANGED GATES",
                    point: new Vector3(12f, 0f, 0f), onComplete: StageEvent.Collapse),

                St(StageGoal.Defend, "HOLD THE EAST GATE", "THE WALL FAILS",
                    duration: 35f, point: new Vector3(12f, 0f, 0f), spawn: new[] { P, R, O }, checkpoint: true),

                St(StageGoal.Cinematic, "", "", beatId: "siege_mid", onComplete: StageEvent.Ambush),

                St(StageGoal.Wave, "THE WEST GATE, WITH DAIGO", "THE LAST GATE",
                    spawn: new[] { H, A, N }, checkpoint: true),

                Look("THE FIELD BEYOND", "GORO'S BANNER", false,
                    Prop("banner", "A BANNER IN THE FIELD", new Vector3(0f, 0f, 17f), StoryPropShape.Lookout,
                        "Goro's banner. It isn't advancing. It's waiting.")),

                St(StageGoal.Wave, "THE LAST WAVE", "IT WAITS",
                    spawn: new[] { E, B }, checkpoint: true),

                St(StageGoal.Reach, "THE VILLAGE STANDS", "STILL HERE",
                    point: new Vector3(0f, 0f, -6f)),

                Scene("siege_end"),
            };
            EditorUtility.SetDirty(m37);

            // ---------------------------------------------------------------
            // 38 — THE HUNTER RETURNS. Snow on the mountain, and the fog comes
            // in with him. Goro, unleashed, against Kagehira's orders. He cannot
            // be beaten here, only outlasted and outrun.
            var m38 = P_("S38_HunterReturns");
            m38.id = 38; m38.missionName = "THE HUNTER RETURNS"; m38.missionType = "ENDURE";
            m38.baseShards = 5; m38.applyTheme = true; m38.theme = Core.EnvThemeId.Mountain;
            m38.briefing = "Goro has stopped sending men. He is coming himself, up the mountain road in the snow, and Kagehira did not tell him to.";
            m38.debrief = "Goro let him run. He wants the fight on his own ground, and his ground is the mountain gate.";
            m38.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.KagehiraBanners, DressingKind.MissingNotice };
            m38.challenge = MissionChallenge.UnderTime; m38.challengeShards = 3;
            m38.stages = new[]
            {
                Scene("hunt_open"),

                St(StageGoal.Reach, "TAKE THE MOUNTAIN ROAD", "HE IS COMING HIMSELF",
                    point: new Vector3(-10f, 0f, 10f), checkpoint: true),

                Look("HIS TRAIL", "HE WANTS THIS PERSONALLY", true,
                    Prop("drag", "A GREATAXE, DRAGGED", new Vector3(-8f, 0f, 14f), StoryPropShape.Tracks,
                        "One man. He's dragging the axe through the snow. He wants me to hear it coming."),
                    Prop("messenger", "KAGEHIRA'S MESSENGER", new Vector3(-4f, 0f, 12f), StoryPropShape.Body,
                        "The serpent's seal, torn in half. Goro is disobeying orders for this.")),

                St(StageGoal.Stealth, "HIS OUTRIDERS", "HE SENT THEM AHEAD",
                    spawn: new[] { H, P }, onComplete: StageEvent.FogRolls, checkpoint: true),

                Scene("hunt_goro"),

                // Endure, not a boss fight: the clock ends it, and he lets Renzo
                // run because he wants the fight on his own ground.
                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE HIM",
                    banner = "GORO, UNLEASHED",
                    duration = 40f,
                    foeDef = "goro",
                    spawn = new[] { C },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                St(StageGoal.Escape, "RUN FOR THE RIDGE", "THE DROP BEHIND YOU",
                    duration: 60f, point: new Vector3(10f, 0f, -12f), spawn: new[] { R, P }),

                Scene("hunt_ridge"),

                St(StageGoal.Reach, "THE MOUNTAIN ROAD", "HE LET YOU RUN",
                    point: new Vector3(14f, 0f, -16f), checkpoint: true),

                Scene("hunt_end"),
            };
            EditorUtility.SetDirty(m38);

            // ---------------------------------------------------------------
            // 39 — THE MOUNTAIN GATE. Smoke and fog. Goro's last wall: burn the
            // gate open while the wall shoots down and drops powder. Inside, her
            // cell, empty, with her thread.
            var m39 = P_("S39_MountainGate");
            m39.id = 39; m39.missionName = "THE MOUNTAIN GATE"; m39.missionType = "COMBAT";
            m39.baseShards = 5; m39.applyTheme = true; m39.theme = Core.EnvThemeId.Fortress;
            m39.briefing = "Goro's ground is the mountain gate. The gate has to be burned open while the wall shoots down at you.";
            m39.debrief = "The gate is down. Aiko's cell in the gatehouse is empty, her thread on the bars, and Goro is waiting in the yard, alone, sword drawn.";
            m39.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice, DressingKind.BloodTrail };
            m39.challenge = MissionChallenge.SilentKill; m39.challengeShards = 3;
            m39.stages = new[]
            {
                Scene("gate_open"),

                Split("REACH THE WALL", "THE CLIFF PATH, OR THE ROAD",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { R }, new[] { P }),

                St(StageGoal.Wave, "UNDER THE ARCHERS", "THE WALL SHOOTS DOWN",
                    spawn: new[] { R, R, O }, onComplete: StageEvent.AlarmTriggered, checkpoint: true),

                Look("WHAT BURNS A GATE", "PITCH AND ROPE", true,
                    Prop("pitch", "BARRELS OF PITCH", new Vector3(5f, 0f, 15f), StoryPropShape.Supply,
                        "Toku's mark on the barrels. He made these to burn, and he told me where they'd be."),
                    Prop("rope", "THE GATE'S ROPES", new Vector3(-5f, 0f, 15f), StoryPropShape.Cache,
                        "Tarred rope on the hinges. Once the pitch catches, the whole gate goes.")),

                St(StageGoal.Defend, "SET THE GATE ALIGHT", "HOLD WHILE IT CATCHES",
                    duration: 35f, point: new Vector3(0f, 0f, 15f), spawn: new[] { P, H },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Scene("gate_falls"),

                St(StageGoal.Wave, "THE GATEHOUSE GARRISON", "THROUGH THE SMOKE",
                    spawn: new[] { E, P }, checkpoint: true),

                Look("THE GATEHOUSE", "HER CELL", true,
                    Prop("thread", "A RED THREAD ON THE BARS", new Vector3(0f, 0f, 17f), StoryPropShape.Keepsake,
                        "Her thread. Tied to the bars, where I'd look. She knew I'd come this far."),
                    Prop("cell", "AN EMPTY CELL", new Vector3(3f, 0f, 17f), StoryPropShape.Homestead,
                        "Blankets still folded. Days, not months. She was here.")),

                St(StageGoal.Reach, "INTO THE YARD", "HE IS WAITING",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("gate_end"),
            };
            EditorUtility.SetDirty(m39);

            // ---------------------------------------------------------------
            // 40 — GORO'S END. Night. A boss the player already beat once,
            // fighting like a man who learned from it: the lanterns go out when
            // his guard steps in, the rain comes when his pride goes, and he
            // dies on his own gate.
            var m40 = P_("S40_GorosEnd");
            m40.id = 40; m40.missionName = "GORO'S END"; m40.missionType = "BOSS";
            m40.baseShards = 5; m40.nightOverride = true;
            m40.applyTheme = true; m40.theme = Core.EnvThemeId.Fortress;
            m40.briefing = "There is nothing between Renzo and Goro now. He is in the yard, alone, and he has had a long time to think about the toll road.";
            m40.debrief = "Goro died on his own gate. 'The marsh,' he said. 'She's under it.' Kagehira had him take Aiko to a temple under the water.";
            m40.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons, DressingKind.PrisonerCamp };
            m40.challenge = MissionChallenge.UnderTime; m40.challengeShards = 3;
            m40.stages = new[]
            {
                Scene("goro_open"),

                St(StageGoal.Reach, "WALK INTO THE YARD", "ALONE, SWORD DRAWN",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("HIS GROUND", "HE CHOSE THIS", true,
                    Prop("banner", "THE TOLL-CAPTAIN'S OWN BANNER", new Vector3(-6f, 0f, 10f), StoryPropShape.StoneMarker,
                        "His own mark, not the serpent's. He's done taking orders."),
                    Prop("axe", "A SECOND AXE", new Vector3(6f, 0f, 10f), StoryPropShape.Supply,
                        "A spare, planted in the dirt where he can reach it. He learned.")),

                Scene("goro_twice"),

                // PHASE 1 — the man who learned. He has his guard and his pride,
                // and when the gate is reached the lanterns go out.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "GORO",
                    banner = "NOBODY GETS ME TWICE",
                    foeDef = "goro",
                    spawn = new[] { C },
                    bossHealthGate = 0.6f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "HIS GUARD STEPS IN", "IN THE DARK",
                    spawn: new[] { P, H }),

                Scene("goro_pride"),

                // PHASE 2 — no foeDef and no spawn: the same man, without his
                // pride, and the rain starts when it breaks.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "WITHOUT HIS PRIDE",
                    banner = "HE FIGHTS DIRTY NOW",
                    bossHealthGate = 0.25f,
                    onComplete = StageEvent.RainStarts,
                },

                St(StageGoal.Wave, "THE LAST OF HIS MEN", "ARCHERS ON THE WALL",
                    spawn: new[] { R, R }),

                St(StageGoal.BossFight, "FINISH IT", "ON HIS OWN GATE", checkpoint: true),

                Scene("goro_death"),
            };
            EditorUtility.SetDirty(m40);
        }
    }
}
