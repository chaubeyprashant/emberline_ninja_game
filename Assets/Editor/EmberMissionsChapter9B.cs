using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 9 — THE BLACK SEAL, missions 86-90, built by hand. Their father's
    /// last words to both of them; what the Seal really is; the warlord in his own
    /// words, and a fight he does not need to win; the siege; and the door opening
    /// with Aiko's blood on the key.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter9B()
        {
            // ---------------------------------------------------------------
            // 86 — FATHER'S FINAL MESSAGE. No enemies. A memory neither of them
            // has: their father, the night before, speaking to children who were
            // not there yet.
            var m86 = P_("S86_FathersFinalMessage");
            m86.id = 86; m86.missionName = "FATHER'S FINAL MESSAGE"; m86.missionType = "MEMORY";
            m86.baseShards = 5; m86.applyTheme = true; m86.theme = Core.EnvThemeId.Temple;
            m86.briefing = "'For both of you,' the door says. Aiko puts her hand on their father's mark, and Renzo puts his beside it.";
            m86.debrief = "Their father knew Kagehira would come, and hid the Seal so that no one, his children included, would open it in anger. 'Whatever it is you are angry about when you hear this — be less.' Aiko took Renzo's hand. He let her.";
            m86.dressing = new[] { DressingKind.EmptyHome, DressingKind.MissingNotice,
                DressingKind.AbandonedWeapons };
            m86.challenge = MissionChallenge.None; m86.challengeShards = 0;
            m86.stages = new[]
            {
                Scene("final_open"),

                St(StageGoal.Reach, "THE DOOR", "FOR BOTH OF YOU",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("THE WORDS UNDER THE MARK", "HIS HAND", true,
                    Prop("words", "\"FOR BOTH OF YOU\"", new Vector3(-4f, 0f, 13f), StoryPropShape.CommandPost,
                        "Both of us. He wrote it the night before. He knew he'd never say it to our faces."),
                    Prop("threads", "TWO THREADS, KNOTTED", new Vector3(4f, 0f, 13f), StoryPropShape.Keepsake,
                        "Two red threads, knotted, tucked into the mark. One for each of us.")),

                Scene("father_final"),

                Scene("final_hand"),

                Look("WHAT IT IS NOT", "NOT A WEAPON", false,
                    Prop("notweapon", "A CARVING: A HAND, OPEN", new Vector3(0f, 0f, -10f), StoryPropShape.Shrine,
                        "Not a sword. Not a fist. An open hand, holding water.")),

                St(StageGoal.Reach, "BACK TO THE OTHERS", "BE LESS",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("final_end"),
            };
            EditorUtility.SetDirty(m86);

            // ---------------------------------------------------------------
            // 87 — THE MEANING OF THE SEAL. Fog. The investigation is the chamber
            // itself; the fight is Kagehira's vanguard arriving to stop it being
            // read. Fumi will not burn the records, and it costs something.
            var m87 = P_("S87_MeaningOfTheSeal");
            m87.id = 87; m87.missionName = "THE MEANING OF THE SEAL"; m87.missionType = "INVESTIGATION";
            m87.baseShards = 5; m87.applyTheme = true; m87.theme = Core.EnvThemeId.Temple;
            m87.briefing = "The chamber's carvings are the Seal's instructions, if anyone can put them in order. Fumi says the fortress's lists match them. Kagehira's vanguard is already on the stair.";
            m87.debrief = "The Black Seal is not a weapon. It is a key to the mountain's water, and every village below it. Kagehira has had the third key for a year. He needs the door, and the door needs a Kurogawa.";
            m87.dressing = new[] { DressingKind.MissingNotice, DressingKind.KagehiraBanners,
                DressingKind.EmptyHome, DressingKind.BloodTrail };
            m87.challenge = MissionChallenge.UnderTime; m87.challengeShards = 3;
            m87.stages = new[]
            {
                Scene("meaning_open"),

                St(StageGoal.Investigate, "THE CARVINGS", "THE CHAMBER IS THE BOOK",
                    count: 3, checkpoint: true),

                Look("THE RIVER", "WHAT IT OPENS", true,
                    Prop("river", "THE MOUNTAIN'S WATER, CARVED", new Vector3(-5f, 0f, 12f), StoryPropShape.Shrine,
                        "Every river on the mountain starts behind this door. The Seal is the gate on it."),
                    Prop("villages", "EVERY VILLAGE BELOW IT", new Vector3(5f, 0f, 12f), StoryPropShape.CommandPost,
                        "Yorune. The reed village. The garrison. Every village in the valley, drinking from one lock.")),

                Scene("meaning_fumi"),

                St(StageGoal.Defend, "KEEP THEM OFF AIKO", "ORDERED TO TAKE HER ALIVE",
                    duration: 35f, point: new Vector3(0f, 0f, 8f), spawn: new[] { E, A },
                    onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.Wave, "THE VANGUARD", "HIS BEST, ON THE STAIR",
                    spawn: new[] { E, M, R }),

                Look("THE THIRD SOCKET", "HE HAS IT", false,
                    Prop("socket", "A SOCKET, SCRATCHED FRESH", new Vector3(0f, 0f, -12f), StoryPropShape.KeyPiece,
                        "Fresh scratches. He's tried the third key in it. It won't turn without us.")),

                Scene("meaning_end"),
            };
            EditorUtility.SetDirty(m87);

            // ---------------------------------------------------------------
            // 88 — KAGEHIRA'S TRUTH. Night. The warlord himself, first met in a
            // fight: his honour guard, then his reasons, then a demonstration he
            // does not need to win. The last duel villain, and the hardest.
            var m88 = P_("S88_KagehirasTruth");
            m88.id = 88; m88.missionName = "KAGEHIRA'S TRUTH"; m88.missionType = "ENDURE";
            m88.baseShards = 6; m88.applyTheme = true; m88.theme = Core.EnvThemeId.Fortress;
            m88.briefing = "Kagehira came back to his fortress alone but for his honour guard, and walked through the mutineers' lines without drawing. He is in the yard, and he wants to talk to the Kurogawa.";
            m88.debrief = "With the Seal, Kagehira controls every village's water; without it, he is a bandit with an army. He showed Renzo what he can do without it, and withdrew to raise that army. There is one night to prepare.";
            m88.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons, DressingKind.HidingVillagers };
            m88.challenge = MissionChallenge.UnderTime; m88.challengeShards = 4;
            m88.stages = new[]
            {
                Scene("serpent88_open"),

                St(StageGoal.Reach, "THE FORTRESS YARD", "HE CAME HIMSELF",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("HE WALKED THROUGH THEM", "WITHOUT DRAWING", true,
                    Prop("banner", "THE SERPENT, PLANTED IN THE YARD", new Vector3(-5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "He planted it himself, in the middle of the mutiny. Nobody touched him."),
                    Prop("mutineers", "MUTINEERS, KNEELING", new Vector3(5f, 0f, 11f), StoryPropShape.Body,
                        "Not dead. Kneeling. He walked past and they knelt. That's worse.")),

                Scene("serpent88_arrives"),

                St(StageGoal.Wave, "HIS HONOUR GUARD", "HE WATCHES YOU FIGHT",
                    spawn: new[] { E, E }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("kagehira_truth"),

                // The first fight with the warlord: every sentence is a cut, and he
                // leaves when he has made his point, not when he is beaten.
                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE HIS DEMONSTRATION",
                    banner = "KAGEHIRA, THE SERPENT",
                    duration = 45f,
                    foeDef = "kagachi",
                    spawn = new[] { K },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Scene("serpent88_withdraws"),

                St(StageGoal.Reach, "TO THE WALLS", "ONE NIGHT",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("serpent88_end"),
            };
            EditorUtility.SetDirty(m88);

            // ---------------------------------------------------------------
            // 89 — THE FINAL MARCH. Night, rain. The largest waves in the game.
            // The mutineers hold the walls; Daigo holds the gate; Suzu is back.
            // And Kagehira is not with his army.
            var m89 = P_("S89_FinalMarch");
            m89.id = 89; m89.missionName = "THE FINAL MARCH"; m89.missionType = "DEFENSE";
            m89.baseShards = 6; m89.applyTheme = true; m89.theme = Core.EnvThemeId.Fortress;
            m89.briefing = "Kagehira's army comes up the mountain in the rain, torches to the horizon. The mutineers are on the walls Fumi's records rebuilt. Daigo has the gate. And Suzu came back up the mountain.";
            m89.debrief = "The army broke on the walls and Kagehira did not care: he was not with it. He went around, to the chamber, with the third key. The walls held. The chamber did not.";
            m89.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m89.challenge = MissionChallenge.UnderTime; m89.challengeShards = 4;
            m89.stages = new[]
            {
                Scene("march_open"),

                St(StageGoal.Reach, "THE WALLS", "TORCHES TO THE HORIZON",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("WHAT THEY REBUILT", "OUT OF THE RECORDS", false,
                    Prop("walls", "WALLS SHORED WITH THE LEDGERS' TIMBER", new Vector3(-5f, 0f, 11f), StoryPropShape.Supply,
                        "Fumi kept the records and they gave us the plans. Every weak stone, shored."),
                    Prop("torches", "AN ARMY IN THE RAIN", new Vector3(5f, 0f, 12f), StoryPropShape.Lookout,
                        "Torches all the way down the mountain. The largest army in the valley.")),

                St(StageGoal.Defend, "THE FIRST WAVE", "THE WHOLE MOUNTAIN IS COMING",
                    duration: 40f, point: new Vector3(0f, 0f, 8f), spawn: new[] { B, B, P, R },
                    onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("march_daigo"),

                St(StageGoal.Defend, "THE SECOND WAVE", "LADDERS ON EVERY WALL",
                    duration: 40f, point: new Vector3(0f, 0f, 8f), spawn: new[] { P, H, A, R },
                    checkpoint: true),

                St(StageGoal.Wave, "HIS ELITE, AT THE GATE", "THE LAST WAVE",
                    spawn: new[] { E, E, H }),

                Scene("march_inside"),

                St(StageGoal.Escape, "TO THE CHAMBER", "HE WENT AROUND",
                    duration: 45f, point: new Vector3(-10f, 0f, -12f), spawn: new[] { A },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Scene("march_end"),
            };
            EditorUtility.SetDirty(m89);

            // ---------------------------------------------------------------
            // 90 — THE DOOR OPENS. Night. A chase against a door: the clock is the
            // ritual, and the enemies are the ones guarding it. Kagehira has
            // Aiko's blood on the key; the door opens as Renzo reaches it.
            var m90 = P_("S90_DoorOpens");
            m90.id = 90; m90.missionName = "THE DOOR OPENS"; m90.missionType = "CHASE";
            m90.baseShards = 6; m90.applyTheme = true; m90.theme = Core.EnvThemeId.Temple;
            m90.briefing = "Kagehira is at the chamber door with the third key, and Aiko was taken off the wall in the confusion. The ritual has started. Every guard on the stair is time.";
            m90.debrief = "The door opened as Renzo reached it, with Aiko's blood on the key. Kagehira took the final key and went up. The chamber is open, and he has all three keys and a road to the summit.";
            m90.dressing = new[] { DressingKind.BloodTrail, DressingKind.KagehiraBanners,
                DressingKind.AbandonedWeapons };
            m90.challenge = MissionChallenge.UnderTime; m90.challengeShards = 4;
            m90.stages = new[]
            {
                Scene("opens_open"),

                St(StageGoal.Reach, "DOWN TO THE CHAMBER", "THE RITUAL HAS STARTED",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.LightsOut, checkpoint: true),

                St(StageGoal.Chase, "THE RITUAL GUARD", "EVERY GUARD IS TIME",
                    duration: 40f, spawn: new[] { M, A }, checkpoint: true),

                Look("ON THE STAIR", "HER BLOOD", true,
                    Prop("blood", "BLOOD ON THE STAIR", new Vector3(-4f, 0f, 12f), StoryPropShape.Tracks,
                        "Hers. A cut hand, not a wound. He only needed a little."),
                    Prop("binding", "A CUT BINDING", new Vector3(4f, 0f, 12f), StoryPropShape.KeyPiece,
                        "She fought him. She cut herself free, and he took the blood anyway.")),

                Scene("opens_aiko"),

                St(StageGoal.Chase, "THE LAST OF HIS GUARD", "THE DOOR IS OPENING",
                    duration: 35f, spawn: new[] { E, R }, checkpoint: true),

                Scene("opens_door"),

                St(StageGoal.Escape, "AFTER HIM", "THE CHAMBER IS COMING DOWN",
                    duration: 30f, point: new Vector3(10f, 0f, -12f), spawn: new[] { E },
                    onComplete: StageEvent.Collapse),

                St(StageGoal.Reach, "THE SUMMIT STAIR", "HE IS CLIMBING",
                    point: new Vector3(12f, 0f, -15f)),

                Scene("opens_end"),
            };
            EditorUtility.SetDirty(m90);
        }
    }
}
