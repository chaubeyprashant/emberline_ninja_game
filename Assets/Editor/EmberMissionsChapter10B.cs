using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 10 — THE SERPENT'S END, missions 96-100, built by hand. Alone up the
    /// stair through everything the marsh made; the truth about two keepers; the
    /// chamber opened and the water rising; Kagehira in four phases and a sword
    /// lowered; and the walk down to Yorune at dawn.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter10B()
        {
            // ---------------------------------------------------------------
            // 96 — NO WAY BACK. Fog, night. No allies, no mutineers, no Aiko. The
            // shades were his all along, and he has let every one of them loose.
            var m96 = P_("S96_NoWayBack");
            m96.id = 96; m96.missionName = "NO WAY BACK"; m96.missionType = "COMBAT";
            m96.baseShards = 6; m96.applyTheme = true; m96.theme = Core.EnvThemeId.Mountain;
            m96.briefing = "Alone, as he was told. The summit stair climbs to a chamber door, and between here and there Kagehira has let loose everything he has left.";
            m96.debrief = "The shades were Kagehira's all along: the marsh made them, and he kept them. The last of them and the last of his men held the chamber stair together. Behind the door, a voice Renzo knows.";
            m96.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice };
            m96.challenge = MissionChallenge.UnderTime; m96.challengeShards = 4;
            m96.stages = new[]
            {
                Scene("noway_open"),

                St(StageGoal.Reach, "ALONE", "UP THE STAIR",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                St(StageGoal.Wave, "THE SHADES WERE HIS", "EVERY ONE THE MARSH MADE",
                    spawn: new[] { S, S, S }, onComplete: StageEvent.FogRolls, checkpoint: true),

                Look("WHAT THE MARSH MADE", "HE KEPT THEM", true,
                    Prop("stone", "A SERPENT BINDING-STONE", new Vector3(-5f, 0f, 12f), StoryPropShape.Shrine,
                        "The serpent, cut in the stone that holds them. They were never the marsh's. They were his."),
                    Prop("mask", "A SHADE'S MASK, WITH A NAME INSIDE", new Vector3(5f, 0f, 12f), StoryPropShape.Body,
                        "A name scratched inside the mask. One from the missing notices.")),

                Scene("noway_shades"),

                St(StageGoal.Survive, "EVERYTHING HE HAS LEFT", "MEN AND SHADES TOGETHER",
                    duration: 40f, spawn: new[] { S, A, E }, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Wave, "THE CHAMBER STAIR", "THE LAST OF THEM",
                    spawn: new[] { E, S }),

                Scene("noway_voice"),

                St(StageGoal.Reach, "THE CHAMBER DOOR", "A VOICE YOU KNOW",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("noway_end"),
            };
            EditorUtility.SetDirty(m96);

            // ---------------------------------------------------------------
            // 97 — FATHER AND SON. Night. The final truth: two keepers, one Seal.
            // Kagehira offers, Renzo refuses, and the guard comes for the refusal.
            var m97 = P_("S97_FatherAndSon");
            m97.id = 97; m97.missionName = "FATHER AND SON"; m97.missionType = "CONVERSATION";
            m97.baseShards = 6; m97.applyTheme = true; m97.theme = Core.EnvThemeId.Temple;
            m97.briefing = "The chamber at the summit. Kagehira is waiting inside it, unhurried, with Aiko beside him and the three keys in the lock. He wants to talk before the end.";
            m97.debrief = "Renzo's father and Kagehira were brothers-in-arms, and the Seal was entrusted to both. One kept faith with the villages. Kagehira offered Renzo his father's place beside him, and his guard came for the answer.";
            m97.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.EmptyHome };
            m97.challenge = MissionChallenge.UnderTime; m97.challengeShards = 4;
            m97.stages = new[]
            {
                Scene("fatherson_open"),

                St(StageGoal.Reach, "INTO THE CHAMBER", "HE WANTS TO TALK",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("THE TWO KEEPERS", "BROTHERS-IN-ARMS", true,
                    Prop("names", "TWO NAMES, CARVED AS KEEPERS", new Vector3(-5f, 0f, 11f), StoryPropShape.Shrine,
                        "Kurogawa Daisuke. Kagehira. Side by side, as keepers. Father never said his name once."),
                    Prop("keys", "THREE KEYS, IN THE LOCK", new Vector3(5f, 0f, 11f), StoryPropShape.KeyPiece,
                        "All three, turned. It's open already. He's been waiting for me anyway.")),

                Scene("father_and_son"),

                Scene("fatherson_offer"),

                St(StageGoal.Defend, "HIS GUARD, FOR YOUR ANSWER", "HE LETS THEM TRY",
                    duration: 40f, point: new Vector3(0f, 0f, 8f), spawn: new[] { E, E, M },
                    onComplete: StageEvent.LightsOut, checkpoint: true),

                St(StageGoal.Wave, "THE LAST OF HIS GUARD", "IN THE DARK",
                    spawn: new[] { M, E }),

                Scene("fatherson_end"),
            };
            EditorUtility.SetDirty(m97);

            // ---------------------------------------------------------------
            // 98 — THE BLACK SEAL. Night. The chamber is real now, and the water is
            // rising in it. Its guardians wake on both sides. Aiko is beside
            // Kagehira, unbound, and she has not run.
            var m98 = P_("S98_BlackSeal");
            m98.id = 98; m98.missionName = "THE BLACK SEAL"; m98.missionType = "COMBAT";
            m98.baseShards = 6; m98.applyTheme = true; m98.theme = Core.EnvThemeId.Temple;
            m98.briefing = "Past the guard, the Seal itself: the place carved on every wall below, real, and the water already rising through it. Kagehira has opened what was only ever meant to be guarded.";
            m98.debrief = "The Seal was never meant to be opened, only guarded, and Kagehira opened it. Its guardians woke on both sides. At the Seal stands Kagehira, waiting, and Aiko beside him, unbound. She has not run.";
            m98.dressing = new[] { DressingKind.EmptyHome, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons };
            m98.challenge = MissionChallenge.UnderTime; m98.challengeShards = 4;
            m98.stages = new[]
            {
                Scene("seal98_open"),

                St(StageGoal.Reach, "THE OPENED CHAMBER", "THE WATER IS RISING",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.WaterRises, checkpoint: true),

                Look("IT IS REAL", "NOT CARVED ANY MORE", true,
                    Prop("seal", "THE BLACK SEAL, OPEN", new Vector3(-5f, 0f, 12f), StoryPropShape.Shrine,
                        "The open hand from the carving. Real stone, and water running out between the fingers."),
                    Prop("water", "WATER WHERE THERE WAS NONE", new Vector3(5f, 0f, 12f), StoryPropShape.Supply,
                        "Every river on the mountain starts here, and he's holding the gate open.")),

                St(StageGoal.Investigate, "WHAT HE HAS DONE", "READ IT BEFORE IT FLOODS",
                    count: 2),

                St(StageGoal.Listen, "THE GUARDIANS WAKE", "ON BOTH SIDES OF THE WATER",
                    spawn: new[] { S, S }, onComplete: StageEvent.WaterRises, checkpoint: true),

                St(StageGoal.Wave, "WHAT GUARDS THE SEAL", "IT DOES NOT CARE WHO OPENED IT",
                    spawn: new[] { E, E, S }, checkpoint: true),

                Scene("seal98_aiko"),

                St(StageGoal.Reach, "TO THE SEAL", "HE IS WAITING",
                    point: new Vector3(0f, 0f, -12f)),

                Scene("seal98_end"),
            };
            EditorUtility.SetDirty(m98);

            // ---------------------------------------------------------------
            // 99 — KAGACHI. Night, the drowned chamber. The final boss in four
            // phases: the swordsman, the warlord, the collapsing chamber, and the
            // exhausted duel. The execution is offered, and refused.
            var m99 = P_("S99_Kagachi");
            m99.id = 99; m99.missionName = "KAGACHI"; m99.missionType = "BOSS";
            m99.baseShards = 8; m99.nightOverride = true;
            m99.applyTheme = true; m99.theme = Core.EnvThemeId.Temple;
            m99.briefing = "Kagehira at the Black Seal, with the water rising around it and Aiko beside him. Three keys, one door, and a Kurogawa to open it. There is nothing left to say.";
            m99.debrief = "Kagehira fell to his knees and Renzo raised the sword. 'Don't become like them.' He lowered it. Kagehira's last attack came from his knees, and Renzo's answer ended it. He did not become him.";
            m99.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons, DressingKind.EmptyHome };
            m99.challenge = MissionChallenge.UnderTime; m99.challengeShards = 5;
            m99.stages = new[]
            {
                Scene("kagachi_open"),

                St(StageGoal.Reach, "TO THE SEAL", "THE WATER IS AT YOUR KNEES",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("AT THE SEAL", "SHE HAS NOT RUN", true,
                    Prop("aiko", "AIKO'S HAND ON THE SEAL", new Vector3(-4f, 0f, 11f), StoryPropShape.Keepsake,
                        "Her hand flat on the open stone. She's not holding it open. She's holding it back."),
                    Prop("keys", "THREE KEYS, TURNED", new Vector3(4f, 0f, 11f), StoryPropShape.KeyPiece,
                        "All three turned. Father's keys, and he turned them.")),

                Scene("kagachi_draw"),

                // PHASE 1 — the swordsman. The Seal answers the first blood with water.
                Phase("THE SWORDSMAN", "KAGEHIRA, THE SERPENT", 0.75f, new[] { K }, StageEvent.WaterRises),

                St(StageGoal.Wave, "THE WARLORD CALLS", "EVERYTHING HE COMMANDS",
                    spawn: new[] { S, S }),

                Scene("kagachi_warlord"),

                // PHASE 2 — the warlord. The chamber starts to come apart.
                Phase("THE WARLORD", "WITH IT, I AM ORDER", 0.5f, null, StageEvent.Collapse),

                St(StageGoal.Survive, "THE CHAMBER COMES DOWN", "THE MARSH HE MADE",
                    duration: 25f, spawn: new[] { S, S }),

                // PHASE 3 — the collapsing arena, in the dark.
                Phase("THE COLLAPSING CHAMBER", "HE WILL NOT FALL ALONE", 0.2f, null, StageEvent.LightsOut),

                Scene("lower_the_sword"),

                // PHASE 4 — the exhausted duel, from his knees.
                St(StageGoal.BossFight, "HIS LAST ATTACK", "FROM HIS KNEES", checkpoint: true),

                Scene("kagachi_death"),
            };
            m99.stages[4].foeDef = "kagachi";
            m99.stages[4].checkpoint = true;
            m99.stages[7].checkpoint = true;
            EditorUtility.SetDirty(m99);

            // ---------------------------------------------------------------
            // 100 — EMBERLINE. No combat. The walk down, the sunrise, and who is
            // standing at Yorune at dawn.
            var m100 = P_("S100_Emberline");
            m100.id = 100; m100.missionName = "EMBERLINE"; m100.missionType = "CONVERSATION";
            m100.baseShards = 8; m100.applyTheme = true; m100.theme = Core.EnvThemeId.VillageDawn;
            m100.briefing = "It is over. The water is nobody's, and the mountain belongs to the villages that drink from it. The only thing left is to leave.";
            m100.debrief = "Renzo chose not to become Kagehira. At Yorune, at dawn, the people who came stood in the ash where the village was. 'Where will you go?' 'Home.' 'There is no home.' 'Then we'll build one.'";
            m100.dressing = new[] { DressingKind.BurnedHome, DressingKind.HidingVillagers,
                DressingKind.EmptyHome };
            m100.challenge = MissionChallenge.None; m100.challengeShards = 0;
            m100.stages = new[]
            {
                Scene("dawn_open"),

                St(StageGoal.Reach, "LEAVE THE FORTRESS", "THE MIST OVER THE VALLEY",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                Look("THE WATER IS NOBODY'S", "IT RUNS", true,
                    Prop("river", "THE RIVER, RUNNING DOWN", new Vector3(-5f, 0f, 12f), StoryPropShape.Shrine,
                        "Every river on the mountain, running down to the villages. Nobody's hand on the gate."),
                    Prop("thread", "THE RED THREAD ON HIS WRIST", new Vector3(4f, 0f, 12f), StoryPropShape.Keepsake,
                        "She tied it on me in the hall. I never took it off.")),

                Scene("dawn_road"),

                St(StageGoal.Reach, "THE ROAD DOWN TO YORUNE", "SUNRISE",
                    point: new Vector3(0f, 0f, -12f)),

                Look("WHO CAME", "AT YORUNE, AT DAWN", false,
                    Prop("fire", "A COOK FIRE IN THE ASH", new Vector3(0f, 0f, -15f), StoryPropShape.Camp,
                        "Someone lit a cook fire where our house stood. Somebody is waiting for us.")),

                Scene("dawn_yorune"),

                Scene("emberline_dawn"),
            };
            EditorUtility.SetDirty(m100);
        }
    }
}
