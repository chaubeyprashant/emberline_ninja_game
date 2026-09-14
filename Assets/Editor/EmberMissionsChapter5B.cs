using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 5 — INTO THE MARSH, missions 46-50, built by hand. The reed village
    /// that chose the marsh over Kagehira, the old guide who knew Renzo's father,
    /// the ruin under the water that floods behind you, the Seal chamber read
    /// before it is fought for, and the first key carried up a closing stair.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter5B()
        {
            // ---------------------------------------------------------------
            // 46 — THE REED VILLAGE. Night. People who chose the marsh over
            // Kagehira. Hostile at first; the defence is what turns them.
            var m46 = P_("S46_ReedVillage");
            m46.id = 46; m46.missionName = "THE REED VILLAGE"; m46.missionType = "DEFENSE";
            m46.baseShards = 5; m46.applyTheme = true; m46.theme = Core.EnvThemeId.Village;
            m46.briefing = "A village on stilts above the water, with no serpent on its gate. They do not want Renzo. The shades come at dusk, and they will want him then.";
            m46.debrief = "The village stands, and it opened. It hides an old guide who has been to the temple and returned. She agrees to take him. She does not agree to like it.";
            m46.dressing = new[] { DressingKind.HidingVillagers, DressingKind.EmptyHome,
                DressingKind.MissingNotice, DressingKind.AbandonedWeapons };
            m46.challenge = MissionChallenge.NoCivilianDeaths; m46.challengeShards = 3;
            m46.stages = new[]
            {
                Scene("reed_open"),

                St(StageGoal.Reach, "THE VILLAGE GATE", "THEY DON'T WANT YOU",
                    point: new Vector3(0f, 0f, 12f), checkpoint: true),

                Look("A VILLAGE THAT CHOSE THE MARSH", "NO SERPENT HERE", true,
                    Prop("stilts", "REED HOUSES ON STILTS", new Vector3(-6f, 0f, 14f), StoryPropShape.Homestead,
                        "Built above the water line. They've lived with the marsh, not against it."),
                    Prop("shrine", "A SHRINE WITHOUT A SERPENT", new Vector3(6f, 0f, 15f), StoryPropShape.Shrine,
                        "No banner. No serpent. The only place in the valley that never paid.")),

                Scene("reed_refused"),

                St(StageGoal.Defend, "HOLD THE VILLAGE", "THE SHADES COME AT DUSK",
                    duration: 40f, point: new Vector3(0f, 0f, 12f), spawn: new[] { S, S },
                    onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Wave, "THE SECOND COMING", "THEY WANT THE LANTERNS",
                    spawn: new[] { S, A }),

                Scene("reed_guide"),

                St(StageGoal.Reach, "THE GUIDE'S HOUSE", "SHE HAS BEEN TO THE TEMPLE",
                    point: new Vector3(-10f, 0f, -8f), checkpoint: true),

                St(StageGoal.Wave, "ONE MORE", "AT HER BACK DOOR",
                    spawn: new[] { R, S }),

                Scene("reed_end"),
            };
            EditorUtility.SetDirty(m46);

            // ---------------------------------------------------------------
            // 47 — THE OLD GUIDE. Nire stops for nothing, and the water rises
            // behind her. She knew his father, and where he stopped.
            var m47 = P_("S47_OldGuide");
            m47.id = 47; m47.missionName = "THE OLD GUIDE"; m47.missionType = "ESCORT";
            m47.baseShards = 5; m47.applyTheme = true; m47.theme = Core.EnvThemeId.Graveyard;
            m47.briefing = "Nire knows the way to the temple stair. The way is not safe, she is not fast, and she stops for nothing. The water rises behind her.";
            m47.debrief = "Nire sat down on the temple stair and would go no further. 'Below,' she said. 'It's all below.' His father came this way ten years ago, with something wrapped in cloth.";
            m47.dressing = new[] { DressingKind.EmptyHome, DressingKind.BloodTrail,
                DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m47.challenge = MissionChallenge.NoCivilianDeaths; m47.challengeShards = 3;
            m47.stages = new[]
            {
                Scene("guide_open"),

                St(StageGoal.Escort, "FOLLOW NIRE", "SHE STOPS FOR NOTHING",
                    spawn: new[] { S, B }, onComplete: StageEvent.WaterRises),

                Look("WHERE SHE STOPS", "TEN YEARS AGO", true,
                    Prop("cairn", "A CAIRN WITH FATHER'S MARK", new Vector3(-6f, 0f, 13f), StoryPropShape.StoneMarker,
                        "Father's mark, cut into the stone. He came this way with something wrapped in cloth."),
                    Prop("path", "A PATH ONLY SHE KNOWS", new Vector3(-2f, 0f, 15f), StoryPropShape.Tracks,
                        "No map has this. She walked it once with him, and she has never forgotten a step.")),

                Scene("guide_father"),

                St(StageGoal.Stealth, "PAST THE SUNKEN WATCH", "THEY WATCH THE STAIR",
                    spawn: new[] { R, A }, checkpoint: true),

                St(StageGoal.Escort, "TO THE TEMPLE STAIR", "THE WATER RISES BEHIND HER",
                    spawn: new[] { S, S }, onComplete: StageEvent.WaterRises, checkpoint: true),

                Scene("guide_stair"),

                St(StageGoal.Wave, "EVERYTHING THAT DOESN'T WANT IT CLIMBED", "ON THE STAIR",
                    spawn: new[] { S, A, B }, checkpoint: true),

                St(StageGoal.Reach, "THE STAIR", "BELOW",
                    point: new Vector3(0f, 0f, 16f)),

                Scene("guide_end"),
            };
            EditorUtility.SetDirty(m47);

            // ---------------------------------------------------------------
            // 48 — BENEATH THE WATER. The ruin floods behind you; the way in
            // becomes the way not-out. The carvings are Kurogawa work.
            var m48 = P_("S48_BeneathTheWater");
            m48.id = 48; m48.missionName = "BENEATH THE WATER"; m48.missionType = "SURVIVAL";
            m48.baseShards = 5; m48.applyTheme = true; m48.theme = Core.EnvThemeId.Temple;
            m48.briefing = "The ruin is under the marsh, and the marsh is coming in with him. The way in floods behind. There is only forward.";
            m48.debrief = "The ruin is Kurogawa work: the same hand as the symbol in the pines. The flood reached the chamber door as the last guardian fell. The door is sealed, and marked with the symbol from his father's blade.";
            m48.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.MissingNotice, DressingKind.EmptyHome };
            m48.challenge = MissionChallenge.UnderTime; m48.challengeShards = 3;
            m48.stages = new[]
            {
                Scene("ruin_open"),

                St(StageGoal.Reach, "DOWN THE STAIR", "UNDER THE MARSH",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.WaterRises, checkpoint: true),

                Look("KUROGAWA WORK", "THE SAME HAND", true,
                    Prop("lintel", "A CARVED LINTEL", new Vector3(-5f, 0f, 13f), StoryPropShape.Shrine,
                        "The same hand as the symbol in the pines. Kurogawa work. My family built this."),
                    Prop("line", "THE FLOOD LINE", new Vector3(5f, 0f, 13f), StoryPropShape.StoneMarker,
                        "The water's already over the stair I came down. The way in is gone.")),

                St(StageGoal.Listen, "SOMETHING IN THE HALLS", "IT WAS LEFT HERE",
                    spawn: new[] { S, S }, checkpoint: true),

                St(StageGoal.Escape, "THE FLOOD BEHIND YOU", "THE WAY IN IS GONE",
                    duration: 50f, point: new Vector3(10f, 0f, -10f), spawn: new[] { S },
                    onComplete: StageEvent.WaterRises),

                Scene("ruin_guardian"),

                St(StageGoal.Wave, "THE LAST GUARDIAN", "AT THE CHAMBER DOOR",
                    spawn: new[] { E, S }, onComplete: StageEvent.Collapse, checkpoint: true),

                Look("THE DOOR", "FATHER'S MARK", false,
                    Prop("door", "A SEALED DOOR", new Vector3(0f, 0f, -14f), StoryPropShape.KeyPiece,
                        "Sealed. Marked with the symbol from his blade. The fragment in my coat fits it.")),

                Scene("ruin_end"),
            };
            EditorUtility.SetDirty(m48);

            // ---------------------------------------------------------------
            // 49 — THE SEAL CHAMBER. Night. No enemies until the chamber is
            // read; the fight comes for what you learned, in the dark.
            var m49 = P_("S49_SealChamber");
            m49.id = 49; m49.missionName = "THE SEAL CHAMBER"; m49.missionType = "INVESTIGATION";
            m49.baseShards = 5; m49.applyTheme = true; m49.theme = Core.EnvThemeId.Temple;
            m49.briefing = "The door opened to the fragment. Beyond it is the chamber the Seal was made for. Nothing has been in here since his father.";
            m49.debrief = "The Seal is not a weapon. It is a lock with three keys, and Renzo's father made the keys. The first is in Renzo's hand, and the chamber went dark when he lifted it.";
            m49.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.MissingNotice, DressingKind.EmptyHome };
            m49.challenge = MissionChallenge.UnderTime; m49.challengeShards = 3;
            m49.stages = new[]
            {
                Scene("chamber_open"),

                St(StageGoal.Reach, "THE CHAMBER", "THREE LOCKS",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                Look("READ THE CHAMBER", "A LOCK WITH THREE KEYS", true,
                    Prop("key", "THE FIRST KEY", new Vector3(0f, 0f, 14f), StoryPropShape.KeyPiece,
                        "Not a weapon. A key. One of three, and Father made them."),
                    Prop("locks", "THE THREE LOCKS", new Vector3(-6f, 0f, 13f), StoryPropShape.Shrine,
                        "Three locks, one door. Whatever the Seal is, it isn't a piece of paper."),
                    Prop("hand", "FATHER'S HAND", new Vector3(6f, 0f, 13f), StoryPropShape.CommandPost,
                        "His handwriting, cut in stone. \"What I hid, I hid from him. Not from you.\"")),

                St(StageGoal.Investigate, "LIFT THE FIRST KEY", "IT COMES FREE",
                    count: 1, onComplete: StageEvent.LightsOut, checkpoint: true),

                Scene("chamber_dark"),

                St(StageGoal.Wave, "THE CHAMBER'S GUARDIANS WAKE", "IN THE DARK",
                    spawn: new[] { S, S, O }, checkpoint: true),

                St(StageGoal.Wave, "THE INNER GUARD", "WHAT FATHER LEFT",
                    spawn: new[] { E, M }),

                St(StageGoal.Reach, "BACK TO THE STAIR", "WITH THE KEY",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("chamber_end"),
            };
            EditorUtility.SetDirty(m49);

            // ---------------------------------------------------------------
            // 50 — THE FIRST KEY. Night. Kagehira's men arrive for what Renzo
            // already holds; the chamber's own defences turn on both sides.
            var m50 = P_("S50_FirstKey");
            m50.id = 50; m50.missionName = "THE FIRST KEY"; m50.missionType = "COMBAT";
            m50.baseShards = 5; m50.applyTheme = true; m50.theme = Core.EnvThemeId.Temple;
            m50.briefing = "Kagehira's elite are on the stair below. They came for the key. The chamber's defences do not know whose side anyone is on.";
            m50.debrief = "Renzo surfaced with the first key, and the temple closed behind him. His father hid the Seal from Kagehira on purpose, at the cost of Yorune. The journal is the map to the second key.";
            m50.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.KagehiraBanners, DressingKind.DestroyedCart };
            m50.challenge = MissionChallenge.UnderTime; m50.challengeShards = 3;
            m50.stages = new[]
            {
                Scene("key_open"),

                St(StageGoal.Wave, "KAGEHIRA'S ELITE, BELOW", "THEY CAME FOR THE KEY",
                    spawn: new[] { E, M }, checkpoint: true),

                Look("THE CHAMBER TURNS", "ON BOTH SIDES", true,
                    Prop("trap", "A TRAP STONE", new Vector3(-5f, 0f, 12f), StoryPropShape.StoneMarker,
                        "Father's defences. They don't know whose side I'm on either."),
                    Prop("crushed", "ONE OF THEIRS, CRUSHED", new Vector3(5f, 0f, 12f), StoryPropShape.Body,
                        "The chamber took him. It'll take me too if I stay.")),

                St(StageGoal.Escape, "UP THE STAIR", "THE MARSH ABOVE, THE ELITE BELOW",
                    duration: 55f, point: new Vector3(0f, 0f, -12f), spawn: new[] { A, A },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Scene("key_stair"),

                St(StageGoal.Wave, "THE LAST OF THEM", "ON THE STAIR",
                    spawn: new[] { R, E }, checkpoint: true),

                St(StageGoal.Reach, "SURFACE", "THE TEMPLE CLOSES",
                    point: new Vector3(0f, 0f, -16f), onComplete: StageEvent.WaterRises),

                Scene("key_end"),
            };
            EditorUtility.SetDirty(m50);
        }
    }
}
