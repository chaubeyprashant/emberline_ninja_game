using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 7 — KUROGANE, missions 61-65, built by hand. The man who drew the
    /// map, met in the rain and not beaten; the rooftops he chooses to be chased
    /// across; the castle yard where Renzo loses; the garrison town that still
    /// calls Jin a hero; and the unit he trained, who were all at Yorune.
    /// </summary>
    public static partial class EmberMissions
    {
        private const EnemyKind JinKind = EnemyKind.Jin;

        private static void BuildChapter7()
        {
            // ---------------------------------------------------------------
            // 61 — THE BLACK BLADE. Rain, night, the garrison town's lower
            // streets. Jin cannot be beaten here. He can be hurt, and he notices.
            var m61 = P_("S61_BlackBlade");
            m61.id = 61; m61.missionName = "THE BLACK BLADE"; m61.missionType = "ENDURE";
            m61.baseShards = 5; m61.applyTheme = true; m61.theme = Core.EnvThemeId.RainyBattlefield;
            m61.briefing = "The Kurogane crest leads to a garrison town under Kagehira's banner. Renzo came to find the man who drew the map. The man is already looking for him.";
            m61.debrief = "Jin knows Renzo's name, his father's, and his sister's, and says them like a man reading a list. He stepped back into the rain. 'Not yet,' he said, and went over the rooftops.";
            m61.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.MissingNotice,
                DressingKind.EmptyHome, DressingKind.AbandonedWeapons };
            m61.challenge = MissionChallenge.UnderTime; m61.challengeShards = 3;
            m61.stages = new[]
            {
                Scene("blade_open"),

                St(StageGoal.Reach, "THE LOWER STREETS", "UNDER THE SERPENT'S BANNER",
                    point: new Vector3(-8f, 0f, 10f), checkpoint: true),

                Look("HE KNOWS YOU ARE COMING", "A LIST OF NAMES", true,
                    Prop("notice", "A NOTICE WITH THREE NAMES", new Vector3(-5f, 0f, 13f), StoryPropShape.CommandPost,
                        "Kurogawa Renzo. Kurogawa Aiko. Kurogawa Daisuke. Father's name, in fresh ink."),
                    Prop("crest", "THE KUROGANE CREST ON A DOOR", new Vector3(5f, 0f, 14f), StoryPropShape.StoneMarker,
                        "The crest from the hunters in the fog. Painted on the door, not hidden.")),

                St(StageGoal.Wave, "HIS WATCHERS", "THEY WERE TOLD TO SLOW YOU",
                    spawn: new[] { A, R }, onComplete: StageEvent.Ambush, checkpoint: true),

                Scene("blade_jin"),

                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE JIN KUROGANE",
                    banner = "THE STORM BLADE",
                    duration = 40f,
                    foeDef = "jin",
                    spawn = new[] { JinKind },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                St(StageGoal.Reach, "AFTER HIM", "HE WENT OVER THE ROOFS",
                    point: new Vector3(10f, 0f, -12f)),

                Scene("blade_end"),
            };
            EditorUtility.SetDirty(m61);

            // ---------------------------------------------------------------
            // 62 — THE PURSUIT. Rooftops in the rain. Jin leads; his men hold
            // every ledge; the roof gives under Renzo on the last gap.
            var m62 = P_("S62_Pursuit");
            m62.id = 62; m62.missionName = "THE PURSUIT"; m62.missionType = "CHASE";
            m62.baseShards = 5; m62.applyTheme = true; m62.theme = Core.EnvThemeId.RainyBattlefield;
            m62.briefing = "Jin went over the rooftops and did not hurry. Suzu is on the roofline ahead. Every ledge between them has one of his men on it.";
            m62.debrief = "Jin was leading Renzo all along: to the edge of the garrison, where the last roof drops to the castle road. He waited on the far side of the gap, sheathing his sword.";
            m62.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.DestroyedCart,
                DressingKind.KagehiraBanners, DressingKind.BloodTrail };
            m62.challenge = MissionChallenge.UnderTime; m62.challengeShards = 3;
            m62.stages = new[]
            {
                Scene("pursuit_open"),

                St(StageGoal.Chase, "THE FIRST ROOF", "HE IS NOT RUNNING. HE IS LEADING",
                    duration: 40f, spawn: new[] { N }, checkpoint: true),

                Look("THE TRAIL HE LEFT", "ON PURPOSE", true,
                    Prop("cut", "A CUT ROPE, CLEAN", new Vector3(-6f, 0f, 12f), StoryPropShape.Tracks,
                        "He cut the washing lines as he went. Not to stop me. To show me the way."),
                    Prop("tile", "A TILE SET STRAIGHT", new Vector3(4f, 0f, 13f), StoryPropShape.StoneMarker,
                        "A loose tile, put back. He had time to put it back.")),

                St(StageGoal.Chase, "HIS MEN AT THE LEDGE", "THEY FIGHT, THEN THEY RUN",
                    duration: 40f, spawn: new[] { N, A }),

                Scene("pursuit_suzu"),

                St(StageGoal.Escape, "THE GAP", "THE ROOF IS GOING",
                    duration: 35f, point: new Vector3(10f, 0f, -10f), spawn: new[] { R },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Reach, "THE LAST ROOF", "HE IS WAITING",
                    point: new Vector3(12f, 0f, -14f)),

                Scene("pursuit_end"),
            };
            EditorUtility.SetDirty(m62);

            // ---------------------------------------------------------------
            // 63 — NO HONOR. The castle yard at night. Renzo hurts Jin once;
            // Jin steps back and lets his men try; then he takes the fight apart
            // and does not kill him. The player is meant to lose.
            var m63 = P_("S63_NoHonor");
            m63.id = 63; m63.missionName = "NO HONOR"; m63.missionType = "ENDURE";
            m63.baseShards = 5; m63.applyTheme = true; m63.theme = Core.EnvThemeId.Castle;
            m63.briefing = "Jin chose the castle yard. He has drawn a ring in the gravel and left his sword in its sheath. Renzo is good enough, now, to make him draw it.";
            m63.debrief = "Jin could have killed him twice, and wanted Renzo to know it. He withdrew his blade from Renzo's throat. 'Go home, Kurogawa. There is nothing up this mountain but me.'";
            m63.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.EmptyHome };
            m63.challenge = MissionChallenge.UnderTime; m63.challengeShards = 3;
            m63.stages = new[]
            {
                Scene("nohonor_open"),

                St(StageGoal.Reach, "INTO THE RING", "HE DREW IT FOR YOU",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("THE CASTLE YARD", "HIS GROUND", false,
                    Prop("ring", "A RING DRAWN IN GRAVEL", new Vector3(-5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "A perfect circle. He's done this before. Many times."),
                    Prop("sheath", "A SWORD STILL SHEATHED", new Vector3(5f, 0f, 11f), StoryPropShape.Keepsake,
                        "He hasn't drawn. He's waiting to see if I'm worth it.")),

                // He can be hurt. When he is, the lanterns go and he stops playing.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "MAKE HIM DRAW",
                    banner = "JIN KUROGANE",
                    foeDef = "jin",
                    spawn = new[] { JinKind },
                    bossHealthGate = 0.8f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "HE STEPS BACK AND LETS THEM TRY", "HE IS WATCHING YOUR FEET",
                    spawn: new[] { M, A }),

                Scene("nohonor_draw"),

                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "LAST AS LONG AS YOU CAN",
                    banner = "HE TAKES IT APART",
                    duration = 35f,
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Scene("jin_mercy"),

                Scene("nohonor_end"),
            };
            EditorUtility.SetDirty(m63);

            // ---------------------------------------------------------------
            // 64 — THE FALLEN SOLDIER. The garrison town by day, where Jin is a
            // hero. Fumi finds her own hand in the ledger; the portrait in the
            // hall puts Jin behind Renzo's father.
            var m64 = P_("S64_FallenSoldier");
            m64.id = 64; m64.missionName = "THE FALLEN SOLDIER"; m64.missionType = "INVESTIGATION";
            m64.baseShards = 5; m64.applyTheme = true; m64.theme = Core.EnvThemeId.Village;
            m64.briefing = "Renzo is alive because Jin chose it. The garrison town remembers Jin as its finest officer. Fumi says a garrison keeps ledgers, and ledgers keep everything.";
            m64.debrief = "Jin was Kagehira's finest officer and left the army the year Yorune burned. In the garrison hall hangs a portrait: Jin, ten years younger, standing behind Renzo's father.";
            m64.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.MissingNotice,
                DressingKind.HidingVillagers, DressingKind.EmptyHome };
            m64.challenge = MissionChallenge.NoAlarm; m64.challengeShards = 3;
            m64.stages = new[]
            {
                Scene("fallen_open"),

                St(StageGoal.Reach, "THE GARRISON TOWN", "HE WAS A HERO HERE",
                    point: new Vector3(-10f, 0f, 8f), checkpoint: true),

                St(StageGoal.Investigate, "ASK ABOUT JIN", "THEY ALL KNEW HIM",
                    count: 2),

                Scene("fallen_ledger"),

                St(StageGoal.Stealth, "INTO THE GARRISON HALL", "THE LEDGER ROOM IS GUARDED",
                    spawn: new[] { P, R }, onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("THE GARRISON HALL", "WHAT THEY HUNG ON THE WALL", true,
                    Prop("portrait", "A PORTRAIT: TWO SWORDSMEN", new Vector3(0f, 0f, 14f), StoryPropShape.Shrine,
                        "Jin. Ten years younger. And the man in front of him is my father."),
                    Prop("resign", "A RESIGNATION, UNSIGNED BY THE WARLORD", new Vector3(-6f, 0f, 12f), StoryPropShape.CommandPost,
                        "He quit the army the month Yorune burned. Kagehira never accepted it.")),

                St(StageGoal.Wave, "JIN'S OLD UNIT", "STILL LOYAL",
                    spawn: new[] { A, A, P }, checkpoint: true),

                St(StageGoal.Reach, "OUT OF THE HALL", "THEY WILL KNOW HOW",
                    point: new Vector3(10f, 0f, -12f)),

                Scene("fallen_end"),
            };
            EditorUtility.SetDirty(m64);

            // ---------------------------------------------------------------
            // 65 — KUROGANE'S MEN. The castle barracks at night: the unit Jin
            // trained, who read heavies and never block twice the same way.
            // Daigo takes the front; Tsuru takes the wall.
            var m65 = P_("S65_KuroganesMen");
            m65.id = 65; m65.missionName = "KUROGANE'S MEN"; m65.missionType = "COMBAT";
            m65.baseShards = 5; m65.applyTheme = true; m65.theme = Core.EnvThemeId.Castle;
            m65.briefing = "Jin's personal unit is quartered in the castle barracks. The ledger says every one of them was at Yorune. Daigo takes the gate. Tsuru takes the wall.";
            m65.debrief = "Jin's men were at Yorune, every one. Their captain, Ryo, was Jin's second that night. On his knees he said only: 'He tried to stop it.'";
            m65.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.DestroyedCart };
            m65.challenge = MissionChallenge.UnderTime; m65.challengeShards = 3;
            m65.stages = new[]
            {
                Scene("men_open"),

                Split("INTO THE BARRACKS", "DAIGO'S GATE, OR TSURU'S WALL",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { A }, new[] { R }),

                St(StageGoal.Wave, "THEY READ YOUR HEAVIES", "HE TAUGHT THEM",
                    spawn: new[] { M, E }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Look("THE UNIT'S ROLL", "EVERY NAME", true,
                    Prop("roll", "THE UNIT ROLL, YORUNE'S NIGHT", new Vector3(-6f, 0f, 12f), StoryPropShape.CommandPost,
                        "Every name on this roll was at Yorune. Every one, marked present."),
                    Prop("drill", "A PRACTICE YARD, WORN SMOOTH", new Vector3(6f, 0f, 12f), StoryPropShape.TrainingPost,
                        "Worn like Father's post. The same drills. He taught them what Father taught him.")),

                Scene("men_ryo"),

                St(StageGoal.Survive, "THEY NEVER BLOCK TWICE THE SAME WAY", "HOLD THE YARD",
                    duration: 35f, spawn: new[] { M, A, R }, checkpoint: true),

                St(StageGoal.Reach, "THE CAPTAIN'S QUARTERS", "JIN'S SECOND",
                    point: new Vector3(0f, 0f, -12f)),

                St(StageGoal.Wave, "CAPTAIN RYO", "HE WAS THERE THAT NIGHT",
                    spawn: new[] { E, M }, checkpoint: true),

                Scene("men_end"),
            };
            EditorUtility.SetDirty(m65);
        }
    }
}
