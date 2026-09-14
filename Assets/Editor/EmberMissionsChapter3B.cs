using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 3 — THE SILENT FOREST, missions 26-30, built by hand. The fog path
    /// and the archer who joins on it, the one mission with nothing to fight, the
    /// trap baited with Aiko's name, the clearing that closes, and the thing that
    /// has been hunting Renzo since mission 21.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter3B()
        {
            // ---------------------------------------------------------------
            // 26 — THE BLIND PATH. Fog and listening, deeper: enemies are silent
            // until they are close, and an archer in the trees decides whose side
            // he is on.
            var m26 = P_("S26_BlindPath");
            m26.id = 26; m26.missionName = "THE BLIND PATH"; m26.missionType = "SURVIVAL";
            m26.baseShards = 4; m26.fog = true; m26.applyTheme = true; m26.theme = Core.EnvThemeId.Forest;
            m26.briefing = "The commander's route runs into a fog-bound path before dawn. Nothing on it makes a sound until it's close.";
            m26.debrief = "The path was marked with red thread, tied to branches at a child's height. It ran out at a clearing, tied there on purpose. And an archer named Tsuru decided to come and see who tied it.";
            m26.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m26.challenge = MissionChallenge.UnderTime; m26.challengeShards = 3;
            m26.stages = new[]
            {
                Scene("blind_open"),

                St(StageGoal.Reach, "INTO THE FOG", "TEN PACES",
                    point: new Vector3(-8f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Listen, "LISTEN FOR WHAT'S CLOSE", "SILENT UNTIL CLOSE",
                    spawn: new[] { S }, checkpoint: true),

                Look("READ THE BRANCHES", "RED THREAD", true,
                    Prop("thread", "RED THREAD ON A BRANCH", new Vector3(-4f, 0f, 14f), StoryPropShape.Keepsake,
                        "Red thread, tied off. At a child's height."),
                    Prop("knot", "ANOTHER KNOT", new Vector3(4f, 0f, 17f), StoryPropShape.Tracks,
                        "Another, twenty paces on. Someone was marking a way.")),

                Scene("blind_archer"),

                St(StageGoal.Survive, "THEY ARRIVE AT ONCE", "EVERYTHING THAT FOLLOWED",
                    duration: 40f, spawn: new[] { S, A, N }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Reach, "FOLLOW THE THREAD", "WHERE IT LEADS",
                    point: new Vector3(0f, 0f, 18f)),

                Scene("blind_end"),
            };
            EditorUtility.SetDirty(m26);

            // ---------------------------------------------------------------
            // 27 — THE RED THREAD. No enemy, and the game never says so. The
            // tension is entirely the player's.
            var m27 = P_("S27_RedThread");
            m27.id = 27; m27.missionName = "THE RED THREAD"; m27.missionType = "EXPLORATION";
            m27.baseShards = 3; m27.applyTheme = true; m27.theme = Core.EnvThemeId.Bamboo;
            m27.briefing = "The thread goes on past the clearing, into the bamboo. Follow it to its end.";
            m27.debrief = "A bead from Aiko's bracelet, left where a nine-year-old could reach. Renzo ties it into his own wrist. Suzu watches, and says nothing.";
            m27.dressing = new[] { DressingKind.EmptyHome, DressingKind.MissingNotice, DressingKind.AbandonedWeapons };
            m27.challenge = MissionChallenge.None; m27.challengeShards = 0;
            m27.stages = new[]
            {
                Scene("redthread_open"),

                St(StageGoal.Reach, "FOLLOW THE THREAD", "INTO THE BAMBOO",
                    point: new Vector3(-10f, 0f, 8f), onComplete: StageEvent.RainStarts, checkpoint: true),

                Look("WHERE SHE STOPPED", "SOMEONE SMALL", true,
                    Prop("mark", "A MARK CUT IN THE BARK", new Vector3(-12f, 0f, 12f), StoryPropShape.StoneMarker,
                        "A child's mark. A little flame. The Kurogawa flame."),
                    Prop("hollow", "A HOLLOW UNDER THE ROOTS", new Vector3(-7f, 0f, 14f), StoryPropShape.Camp,
                        "Somebody small slept here, out of the rain.")),

                St(StageGoal.Reach, "DEEPER IN", "THE THREAD GOES ON",
                    point: new Vector3(10f, 0f, 16f), checkpoint: true),

                Look("THE END OF THE THREAD", "A BEAD", true,
                    Prop("bead", "A BEAD", new Vector3(12f, 0f, 19f), StoryPropShape.Keepsake,
                        "A bead from her bracelet. She left it here on purpose.")),

                Scene("redthread_bead"),

                St(StageGoal.Reach, "OUT OF THE WOOD", "HE CARRIES IT NOW",
                    point: new Vector3(0f, 0f, -14f)),
            };
            EditorUtility.SetDirty(m27);

            // ---------------------------------------------------------------
            // 28 — THE DECOY. The rescue is the trap: the girl is not Aiko and
            // the pen's walls are the ambush. Renzo frees her anyway.
            var m28 = P_("S28_Decoy");
            m28.id = 28; m28.missionName = "THE DECOY"; m28.missionType = "RESCUE";
            m28.baseShards = 4; m28.applyTheme = true; m28.theme = Core.EnvThemeId.Forest;
            m28.briefing = "A girl in a pen at the edge of the forest, reported this morning. It could be her.";
            m28.debrief = "The girl had never heard of Aiko. The enemy is using Aiko's name to catch Renzo now. He freed the girl anyway, and she is walking to Ashfall.";
            m28.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.KagehiraBanners, DressingKind.BloodTrail };
            m28.challenge = MissionChallenge.SaveAllPrisoners; m28.challengeShards = 3;
            m28.stages = new[]
            {
                Scene("decoy_open"),

                Split("REACH THE PEN", "THE RIDGE, OR THE GULLY",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { R }, new[] { A }),

                St(StageGoal.Stealth, "PAST THE WATCH", "TOO FEW GUARDS",
                    spawn: new[] { A, R }, checkpoint: true),

                St(StageGoal.FreePrisoners, "CUT HER LOOSE", "IT COULD BE HER",
                    count: 1, onComplete: StageEvent.Ambush, checkpoint: true),

                Scene("decoy_trap"),

                St(StageGoal.Wave, "THE WALLS WERE THE AMBUSH", "A TRAP",
                    spawn: new[] { N, P, A }, checkpoint: true),

                St(StageGoal.Escort, "WALK HER OUT", "SHE COMES WITH YOU",
                    spawn: new[] { R }),

                Scene("decoy_end"),
            };
            EditorUtility.SetDirty(m28);

            // ---------------------------------------------------------------
            // 29 — THE HUNTER'S TRAP. The clearing closes: each wave comes from a
            // new side and an exit shuts with it, until the last one holds the
            // thing Renzo cannot beat yet.
            var m29 = P_("S29_HuntersTrap");
            m29.id = 29; m29.missionName = "THE HUNTER'S TRAP"; m29.missionType = "SURVIVAL";
            m29.baseShards = 4; m29.applyTheme = true; m29.theme = Core.EnvThemeId.Forest;
            m29.briefing = "Whoever laid the decoy laid a second trap behind it. The paths out of the clearing are closing.";
            m29.debrief = "The assassins answer to something they call the Pale Shade. It stood in the last exit, and let Renzo through. The forest goes quiet around him: it has arrived.";
            m29.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail, DressingKind.KagehiraBanners };
            m29.challenge = MissionChallenge.UnderTime; m29.challengeShards = 3;
            m29.stages = new[]
            {
                Scene("trap_open"),

                St(StageGoal.Reach, "TO THE CLEARING'S HEART", "HOLD THE MIDDLE",
                    point: new Vector3(0f, 0f, 4f), checkpoint: true),

                St(StageGoal.Survive, "THE NORTH PATH CLOSES", "FROM THE NORTH",
                    duration: 35f, spawn: new[] { A, N }, onComplete: StageEvent.Reinforcements),

                Look("A DEAD ASSASSIN'S ORDERS", "WHO THEY ANSWER TO", true,
                    Prop("order", "FOLDED ORDERS", new Vector3(3f, 0f, 8f), StoryPropShape.CommandPost,
                        "\"By the word of the Pale Shade.\" They answer to it.")),

                St(StageGoal.Survive, "THE EAST PATH CLOSES", "FROM THE EAST",
                    duration: 35f, spawn: new[] { O, P }, checkpoint: true),

                Scene("trap_last"),

                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "THE THING IN THE LAST EXIT",
                    banner = "IT OWNS THIS FOREST",
                    duration = 25f,
                    foeDef = "paleshade",
                    spawn = new[] { S },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                St(StageGoal.Escape, "THROUGH THE LAST EXIT", "IT LETS YOU PASS",
                    duration: 50f, point: new Vector3(0f, 0f, -16f), spawn: new[] { S }),

                Scene("trap_end"),
            };
            EditorUtility.SetDirty(m29);

            // ---------------------------------------------------------------
            // 30 — PALE SHADE. The chapter boss: find where it forms, break its
            // first shape, survive the dead it calls, then its full form in the dark.
            var m30 = P_("S30_PaleShade");
            m30.id = 30; m30.missionName = "PALE SHADE"; m30.missionType = "BOSS";
            m30.baseShards = 5; m30.nightOverride = true; m30.fog = true;
            m30.applyTheme = true; m30.theme = Core.EnvThemeId.Graveyard;
            m30.briefing = "There is no leaving the forest without going through what owns it. It is waiting in the old graveyard, and it knows where Aiko went.";
            m30.debrief = "Dying, the Pale Shade said it: she was moved, to the toll-captain's country. Renzo goes to war with Goro.";
            m30.dressing = new[] { DressingKind.BloodTrail, DressingKind.BurnedHome,
                DressingKind.MissingNotice, DressingKind.AbandonedWeapons };
            m30.challenge = MissionChallenge.UnderTime; m30.challengeShards = 3;
            m30.stages = new[]
            {
                Scene("shade_open"),

                St(StageGoal.Reach, "INTO THE GRAVES", "IT IS HERE",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Listen, "FIND WHERE IT FORMS", "LISTEN",
                    spawn: new[] { S, S }, checkpoint: true),

                Scene("shade_confront"),

                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE PALE SHADE",
                    banner = "NOT WHOLLY THERE",
                    foeDef = "paleshade",
                    spawn = new[] { S },
                    bossHealthGate = 0.55f,
                    onComplete = StageEvent.BossArrives,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "IT CALLS THE DEAD", "THE GRAVES OPEN",
                    spawn: new[] { S, A }),

                Scene("shade_full"),

                St(StageGoal.BossFight, "ITS FULL FORM, IN THE DARK", "FULL DARK", checkpoint: true),

                Scene("shade_death"),
            };
            EditorUtility.SetDirty(m30);
        }
    }
}
