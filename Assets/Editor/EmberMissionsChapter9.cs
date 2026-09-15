using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 9 — THE BLACK SEAL, missions 81-85, built by hand. Two words and
    /// ten years across a lit hall; the night Aiko tells it while the doors are
    /// held; getting her out of the fortress; the army turning on itself at the
    /// sight of her; and the door the first Kurogawa built.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter9()
        {
            // ---------------------------------------------------------------
            // 81 — YOU CAME. No enemies. The whole mission is the walk across the
            // hall, and what she kept, and two people in a room.
            var m81 = P_("S81_YouCame");
            m81.id = 81; m81.missionName = "YOU CAME"; m81.missionType = "CONVERSATION";
            m81.baseShards = 5; m81.applyTheme = true; m81.theme = Core.EnvThemeId.Castle;
            m81.briefing = "She is standing behind the throne. Older. Alive. The hall is quiet, and the only thing between them is the length of it.";
            m81.debrief = "Aiko has been Kagehira's prisoner for ten years and his prize for the last one. She knows what the Seal is. Her story will take the night, and the fortress is not safe to tell it in.";
            m81.dressing = new[] { DressingKind.EmptyHome, DressingKind.KagehiraBanners,
                DressingKind.MissingNotice };
            m81.challenge = MissionChallenge.None; m81.challengeShards = 0;
            m81.stages = new[]
            {
                Scene("reunion_open"),

                St(StageGoal.Reach, "ACROSS THE HALL", "TWO WORDS, AND TEN YEARS",
                    point: new Vector3(0f, 0f, 12f), onComplete: StageEvent.LightsOut, checkpoint: true),

                Scene("you_came"),

                Look("WHAT SHE KEPT", "TEN YEARS OF IT", true,
                    Prop("thread", "THE RED THREAD, ON HER WRIST", new Vector3(-4f, 0f, 13f), StoryPropShape.Keepsake,
                        "Wound round and round her wrist. The same thread. She never let them take it."),
                    Prop("corner", "HER CORNER OF THE HALL", new Vector3(5f, 0f, 14f), StoryPropShape.Camp,
                        "A mat, a bowl, a lamp. He kept her in the hall like a trophy he could look at."),
                    Prop("marks", "MARKS IN FATHER'S CIPHER", new Vector3(0f, 0f, 16f), StoryPropShape.StoneMarker,
                        "The same cipher as the cell. \"Ren will come.\" Over and over, for a year.")),

                Scene("reunion_talk"),

                St(StageGoal.Reach, "SOMEWHERE SAFER", "BEFORE THEY COME BACK",
                    point: new Vector3(0f, 0f, -12f)),

                Scene("reunion_end"),
            };
            EditorUtility.SetDirty(m81);

            // ---------------------------------------------------------------
            // 82 — THE LONG NIGHT. Night. Daigo bars the doors and Aiko talks;
            // every lull between waves is another piece of her story, and the
            // doors give at the end of it.
            var m82 = P_("S82_LongNight");
            m82.id = 82; m82.missionName = "THE LONG NIGHT"; m82.missionType = "DEFENSE";
            m82.baseShards = 5; m82.applyTheme = true; m82.theme = Core.EnvThemeId.Castle;
            m82.briefing = "The fortress knows. Daigo has barred the hall doors, and they will not hold till morning. Aiko talks while Renzo holds them, and he listens between the waves.";
            m82.debrief = "Aiko hid the Seal the night Yorune burned and never told Kagehira where, in ten years of asking. 'It is under the shrine floor. He never thought to look at home.' The doors gave at dawn, and held long enough.";
            m82.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.EmptyHome };
            m82.challenge = MissionChallenge.UnderTime; m82.challengeShards = 3;
            m82.stages = new[]
            {
                Scene("longnight_open"),

                St(StageGoal.Defend, "HOLD THE HALL", "THE FIRST OF THEM",
                    duration: 35f, point: new Vector3(0f, 0f, 8f), spawn: new[] { P, P },
                    onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("longnight_story"),

                St(StageGoal.Defend, "THE SECOND WAVE", "AT THE DOORS",
                    duration: 35f, point: new Vector3(0f, 0f, 8f), spawn: new[] { R, A }, checkpoint: true),

                Scene("long_night"),

                St(StageGoal.Wave, "THE DOORS GIVE", "THE LAST WAVE",
                    spawn: new[] { E, P, A }, onComplete: StageEvent.Collapse, checkpoint: true),

                Look("DAWN THROUGH THE DOORS", "IT HELD LONG ENOUGH", false,
                    Prop("doors", "THE DOORS, OFF THEIR HINGES", new Vector3(0f, 0f, 12f), StoryPropShape.Passage,
                        "Off their hinges, at dawn. They held exactly as long as she needed to talk."),
                    Prop("thread", "HER THREAD, ON MY WRIST NOW", new Vector3(-4f, 0f, 10f), StoryPropShape.Keepsake,
                        "She tied it on me while I was fighting. I didn't feel it. I feel it now.")),

                Scene("longnight_end"),
            };
            EditorUtility.SetDirty(m82);

            // ---------------------------------------------------------------
            // 83 — THE PRISONER. Fog of smoke. Aiko can run and hide; she cannot
            // fight and will not wait. Some soldiers hesitate at her. The last
            // courtyard has the ones who did not, and then the fortress turns.
            var m83 = P_("S83_AikoOut");
            m83.id = 83; m83.missionName = "THE PRISONER"; m83.missionType = "ESCORT";
            m83.baseShards = 5; m83.applyTheme = true; m83.theme = Core.EnvThemeId.Fortress;
            m83.briefing = "Out of the fortress, with Aiko. She can run, and she can hide, and she will not wait for anyone. Daigo takes the front and Tsuru the walls.";
            m83.debrief = "Kagehira's soldiers hesitated at Aiko; some of them were at Yorune. The last courtyard had the ones who did not. Outside the walls, behind them, the fortress erupted in fighting of its own.";
            m83.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.HidingVillagers, DressingKind.BloodTrail };
            m83.challenge = MissionChallenge.UnderTime; m83.challengeShards = 3;
            m83.stages = new[]
            {
                Scene("escape_open"),

                St(StageGoal.Escort, "OUT THROUGH THE KEEP", "SHE WILL NOT WAIT",
                    spawn: new[] { P, B }, onComplete: StageEvent.Ambush, checkpoint: true),

                Look("THE ONES WHO HESITATE", "THEY KNOW HER FACE", true,
                    Prop("spear", "A SPEAR, LOWERED", new Vector3(-5f, 0f, 12f), StoryPropShape.StoneMarker,
                        "He saw her and lowered it. He didn't run, and he didn't fight. He just looked."),
                    Prop("steel", "YORUNE STEEL, DROPPED", new Vector3(5f, 0f, 12f), StoryPropShape.Supply,
                        "Dropped at her feet. Somebody here was at Yorune, and remembers the girl with the thread.")),

                Scene("escape_hesitate"),

                St(StageGoal.Stealth, "THE BARRACKS YARD", "SHE KNOWS HOW TO HIDE",
                    spawn: new[] { A, R }, checkpoint: true),

                St(StageGoal.Escort, "THE LAST COURTYARD", "THE ONES WHO DID NOT HESITATE",
                    spawn: new[] { H, E }, onComplete: StageEvent.Mutiny, checkpoint: true),

                St(StageGoal.Reach, "OUTSIDE THE WALLS", "BEHIND YOU, THE FORTRESS TURNS",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("escape_end"),
            };
            EditorUtility.SetDirty(m83);

            // ---------------------------------------------------------------
            // 84 — THE BETRAYAL. Night. The army turns: half of it followed
            // Kagehira for land, not a seal. The loyalists make their last stand;
            // the mutineers stand down at the sight of Aiko.
            var m84 = P_("S84_Betrayal");
            m84.id = 84; m84.missionName = "THE BETRAYAL"; m84.missionType = "COMBAT";
            m84.baseShards = 5; m84.applyTheme = true; m84.theme = Core.EnvThemeId.Fortress;
            m84.briefing = "The fortress is fighting itself. Daigo wants to keep walking. Aiko wants to go back in and see who is winning, because whoever it is will want her.";
            m84.debrief = "Half the army turned: they followed Kagehira for conquest, not for a seal. The loyalists died on the stair, and the mutineers stood down at the sight of Aiko, and opened the way to the chamber under the fortress.";
            m84.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons, DressingKind.DestroyedCart };
            m84.challenge = MissionChallenge.UnderTime; m84.challengeShards = 3;
            m84.stages = new[]
            {
                Scene("mutiny_open"),

                St(StageGoal.Reach, "BACK INTO THE FORTRESS", "EVERYONE IS FIGHTING EVERYONE",
                    point: new Vector3(-8f, 0f, 10f), checkpoint: true),

                St(StageGoal.Wave, "THE LOYALISTS", "THEY STILL WEAR THE SERPENT",
                    spawn: new[] { P, H }, onComplete: StageEvent.Mutiny, checkpoint: true),

                Look("WHO TURNED", "HALF HIS ARMY", true,
                    Prop("banner", "A SERPENT BANNER, TORN DOWN", new Vector3(-5f, 0f, 12f), StoryPropShape.CommandPost,
                        "Torn down by his own men, and walked on."),
                    Prop("officer", "A LOYALIST OFFICER", new Vector3(5f, 0f, 12f), StoryPropShape.Body,
                        "Killed by men in the same armour. This wasn't us.")),

                Scene("mutiny_oba"),

                St(StageGoal.Survive, "THE LOYALISTS' LAST STAND", "ON THE KEEP STAIR",
                    duration: 35f, spawn: new[] { E, R, A }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                St(StageGoal.Wave, "HIS LAST OFFICERS", "THEY WILL NOT STAND DOWN",
                    spawn: new[] { E, P }),

                Scene("mutiny_aiko"),

                St(StageGoal.Reach, "THE WAY TO THE CHAMBER", "THEY OPEN IT FOR HER",
                    point: new Vector3(10f, 0f, -12f)),

                Scene("mutiny_end"),
            };
            EditorUtility.SetDirty(m84);

            // ---------------------------------------------------------------
            // 85 — THE SEAL'S DOOR. Night. Under the fortress, a chamber the first
            // Kurogawa built; Aiko reads it as they walk, and its guardians wake
            // for the first time in a century.
            var m85 = P_("S85_SealsDoor");
            m85.id = 85; m85.missionName = "THE SEAL'S DOOR"; m85.missionType = "EXPLORATION";
            m85.baseShards = 5; m85.applyTheme = true; m85.theme = Core.EnvThemeId.Temple;
            m85.briefing = "Under the fortress is the chamber where Kagehira kept the keys he could not use. Aiko knows the door. Tsuru carries the lamp and says nothing about the carvings watching him.";
            m85.debrief = "The chamber was built by the first Kurogawa; it was never Kagehira's to open. Its guardians woke for the first time in a century. On the door: their father's mark, and a message beneath it.";
            m85.dressing = new[] { DressingKind.EmptyHome, DressingKind.MissingNotice,
                DressingKind.AbandonedWeapons };
            m85.challenge = MissionChallenge.UnderTime; m85.challengeShards = 3;
            m85.stages = new[]
            {
                Scene("sealdoor_open"),

                St(StageGoal.Reach, "UNDER THE FORTRESS", "SHE KNOWS THE WAY",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                St(StageGoal.Investigate, "AIKO READS THE WALLS", "THE CLUES ARE HERS",
                    count: 2),

                Look("THE FIRST KUROGAWA", "NEVER HIS TO OPEN", true,
                    Prop("carving", "THE FIRST KUROGAWA, CARVED", new Vector3(-5f, 0f, 12f), StoryPropShape.Shrine,
                        "Our crest, a hundred years old. Kagehira kept his keys in our family's house."),
                    Prop("lock", "A LOCK FOR THREE KEYS", new Vector3(5f, 0f, 12f), StoryPropShape.KeyPiece,
                        "Three sockets. Two keys in my coat. He has the third, Aiko says.")),

                St(StageGoal.Listen, "THE GUARDIANS WAKE", "A CENTURY ASLEEP",
                    spawn: new[] { S, S }, onComplete: StageEvent.FogRolls, checkpoint: true),

                Scene("sealdoor_guardians"),

                St(StageGoal.Wave, "WHAT THE FIRST KUROGAWA LEFT", "THEY DO NOT KNOW YOUR NAME",
                    spawn: new[] { E, S }, checkpoint: true),

                Look("THE DOOR", "HIS MARK, AND WORDS", false,
                    Prop("door", "FATHER'S MARK, AND WORDS BENEATH IT", new Vector3(0f, 0f, -12f), StoryPropShape.Passage,
                        "His mark. And under it, words. \"For both of you.\"")),

                Scene("sealdoor_end"),
            };
            EditorUtility.SetDirty(m85);
        }
    }
}
