using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 7 — KUROGANE, missions 66-70, built by hand. The duel with Jin's
    /// champion on agreed terms, the house Jin kept and Kagehira tried to burn,
    /// the confession in the rain, the lesson with a sword in it, and the duel
    /// neither of them walks away from.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter7B()
        {
            // ---------------------------------------------------------------
            // 66 — THE DUELIST. A ring in the dust, no allies, no interference.
            // Jin's champion is Commander Hoshu, keeper of the inner gate, who
            // trained beside him. He yields; he does not die here.
            var m66 = P_("S66_Duelist");
            m66.id = 66; m66.missionName = "THE DUELIST"; m66.missionType = "BOSS";
            m66.baseShards = 6; m66.applyTheme = true; m66.theme = Core.EnvThemeId.Village;
            m66.briefing = "Renzo sent his terms: a duel, no guards. Jin answered with a champion. Toku finished the blade last night, and says it will not break before Renzo does.";
            m66.debrief = "Commander Hoshu, keeper of Kagehira's inner gate, yielded in the ring. Jin was watching from the roofline. He sent a message after: a place, a time, and no guards.";
            m66.dressing = new[] { DressingKind.HidingVillagers, DressingKind.KagehiraBanners,
                DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m66.challenge = MissionChallenge.UnderTime; m66.challengeShards = 4;
            m66.stages = new[]
            {
                Scene("duelist_open"),

                St(StageGoal.Reach, "THE RING IN THE DUST", "AGREED TERMS",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("THE TERMS", "NO ALLIES, NO INTERFERENCE", false,
                    Prop("ring", "THE RING, DRAWN BY BOTH SIDES", new Vector3(-5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "Half of it drawn by his men, half by mine. Nobody steps over it."),
                    Prop("blade", "TOKU'S NEW STEEL", new Vector3(5f, 0f, 11f), StoryPropShape.KeyPiece,
                        "Toku's blade. Father's mark on the tang. It's heavier than it looks, like it should be.")),

                Scene("duelist_hoshu"),

                // PHASE 1 — the gatekeeper, measured, reading everything.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "COMMANDER HOSHU",
                    banner = "THE INNER GATE",
                    foeDef = "finalcommander",
                    spawn = new[] { M },
                    bossHealthGate = 0.55f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                Scene("duelist_respect"),

                // PHASE 2 — pressed, he stops holding the door and comes through it.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "MAKE HIM YIELD",
                    banner = "HE STOPS HOLDING THE DOOR",
                    bossHealthGate = 0.2f,
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Look("THE ROOFLINE", "HE WAS WATCHING", false,
                    Prop("roof", "A FIGURE ON THE ROOFLINE", new Vector3(0f, 0f, -12f), StoryPropShape.Lookout,
                        "Jin. He saw all of it. He's gone before I can lift my sword.")),

                Scene("duelist_end"),
            };
            EditorUtility.SetDirty(m66);

            // ---------------------------------------------------------------
            // 67 — THE BROKEN MASK. Night. Jin's old house, every room a clue,
            // and Kagehira's men arriving to burn it before it is read.
            var m67 = P_("S67_BrokenMask");
            m67.id = 67; m67.missionName = "THE BROKEN MASK"; m67.missionType = "EXPLORATION";
            m67.baseShards = 5; m67.applyTheme = true; m67.theme = Core.EnvThemeId.Village;
            m67.briefing = "Before the meeting, Fumi found the house Jin grew up in, at the edge of the garrison. Nobody has lived there for ten years. Somebody has kept it clean.";
            m67.debrief = "Jin's family was from Yorune; he left the year before it burned to serve Kagehira. In the ashes of the last room lay a mask broken in half. The other half is on Jin's face.";
            m67.dressing = new[] { DressingKind.EmptyHome, DressingKind.BurnedHome,
                DressingKind.MissingNotice, DressingKind.BloodTrail };
            m67.challenge = MissionChallenge.UnderTime; m67.challengeShards = 3;
            m67.stages = new[]
            {
                Scene("mask_open"),

                St(StageGoal.Reach, "JIN'S HOUSE", "SOMEBODY KEEPS IT CLEAN",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                St(StageGoal.Investigate, "EVERY ROOM", "EVERY ROOM IS A CLUE",
                    count: 3),

                Look("HIS FAMILY'S SHRINE", "YORUNE", true,
                    Prop("shrine", "A YORUNE FAMILY SHRINE", new Vector3(-6f, 0f, 12f), StoryPropShape.Shrine,
                        "Yorune river stones on the shrine. His family was ours. He was from Yorune."),
                    Prop("letter", "A LETTER FROM FATHER", new Vector3(6f, 0f, 12f), StoryPropShape.CommandPost,
                        "\"Come home, Jin. Whatever he promised you, come home.\" Dated the year before the fire.")),

                Scene("mask_fire"),

                St(StageGoal.Defend, "THEY CAME TO BURN IT", "BEFORE YOU CAN READ IT",
                    duration: 35f, point: new Vector3(0f, 0f, 10f), spawn: new[] { A, N },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Wave, "ARCHERS WITH FIRE ARROWS", "THE ROOF IS CATCHING",
                    spawn: new[] { R, N }),

                Look("THE LAST ROOM", "IN THE ASHES", false,
                    Prop("mask", "HALF A MASK", new Vector3(0f, 0f, -12f), StoryPropShape.Keepsake,
                        "Broken in half. I've seen the other half. He wears it.")),

                Scene("mask_end"),
            };
            EditorUtility.SetDirty(m67);

            // ---------------------------------------------------------------
            // 68 — THE CONFESSION. Rain, night. The companions refuse to attack
            // tonight; Renzo goes alone. The conversation is the mission, and
            // Kagehira's assassins cut it short.
            var m68 = P_("S68_Confession");
            m68.id = 68; m68.missionName = "THE CONFESSION"; m68.missionType = "CONVERSATION";
            m68.baseShards = 5; m68.applyTheme = true; m68.theme = Core.EnvThemeId.RainyBattlefield;
            m68.briefing = "A place, a time, and no guards. The others will not go tonight; they say it is a trap and that Renzo wants it to be. He goes alone.";
            m68.debrief = "Jin gave Kagehira the map; he did not give him the village. Kagehira took that himself. Kagehira's assassins came for both of them, and Jin fought beside Renzo until they were dead.";
            m68.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.EmptyHome, DressingKind.DestroyedCart };
            m68.challenge = MissionChallenge.UnderTime; m68.challengeShards = 3;
            m68.stages = new[]
            {
                Scene("confess_refused"),

                St(StageGoal.Reach, "ALONE, TO THE MEETING", "NO GUARDS",
                    point: new Vector3(-8f, 0f, 10f), checkpoint: true),

                Look("HIS FIRE", "HE CAME ALONE TOO", true,
                    Prop("fire", "A SMALL FIRE UNDER THE BRIDGE", new Vector3(-5f, 0f, 13f), StoryPropShape.Camp,
                        "Two cups. He poured mine before I got here."),
                    Prop("sword", "HIS SWORD, OUT OF REACH", new Vector3(4f, 0f, 13f), StoryPropShape.Keepsake,
                        "Leaning on the far pillar. He put it where he couldn't reach it first.")),

                Scene("jin_confession"),

                St(StageGoal.Survive, "KAGEHIRA'S ASSASSINS", "THEY CAME FOR BOTH OF YOU",
                    duration: 35f, spawn: new[] { A, A, R }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Wave, "BACK TO BACK", "JIN FIGHTS BESIDE YOU",
                    spawn: new[] { A }),

                Scene("confess_after"),

                St(StageGoal.Reach, "BACK TO CAMP", "TOMORROW, WITH A SWORD",
                    point: new Vector3(10f, 0f, -12f)),

                Scene("confess_end"),
            };
            EditorUtility.SetDirty(m68);

            // ---------------------------------------------------------------
            // 69 — LAST WARNING. Night, the castle steps. Nire says it first.
            // Jin fights to show, not to win: his samurai, then Renzo's own
            // rage used against him, then 'Tomorrow, then. Properly.'
            var m69 = P_("S69_LastWarning");
            m69.id = 69; m69.missionName = "LAST WARNING"; m69.missionType = "ENDURE";
            m69.baseShards = 5; m69.applyTheme = true; m69.theme = Core.EnvThemeId.Castle;
            m69.briefing = "Jin asked for one more meeting before the duel, on the castle steps. Nire came down from the marsh to walk Renzo there, and to say something on the way.";
            m69.debrief = "'If you reach Kagehira, you may become him,' Jin said. He had watched it happen before. He sheathed his sword. 'Tomorrow, then. Properly.'";
            m69.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice };
            m69.challenge = MissionChallenge.UnderTime; m69.challengeShards = 3;
            m69.stages = new[]
            {
                Scene("warning_nire"),

                St(StageGoal.Reach, "THE CASTLE STEPS", "HE IS ALREADY THERE",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                Look("WHAT HE BROUGHT", "A LESSON", false,
                    Prop("samurai", "TWO SAMURAI, KNEELING", new Vector3(-5f, 0f, 12f), StoryPropShape.Body,
                        "Kagehira's. Alive, and furious about it. He brought them to fight me."),
                    Prop("steps", "THE STEPS UP TO THE KEEP", new Vector3(5f, 0f, 13f), StoryPropShape.Passage,
                        "The keep, and past it the mountain. Where Aiko is, if anyone will say.")),

                St(StageGoal.Wave, "WATCH HOW THEY BREAK", "HE LETS THEM LOOSE",
                    spawn: new[] { M, M }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("warning_rage"),

                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "YOUR OWN RAGE, USED AGAINST YOU",
                    banner = "EVERY EXCHANGE IS A LESSON",
                    duration = 40f,
                    foeDef = "jin",
                    spawn = new[] { JinKind },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Scene("jin_warning"),

                St(StageGoal.Reach, "DOWN THE STEPS", "TOMORROW",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("warning_end"),
            };
            EditorUtility.SetDirty(m69);

            // ---------------------------------------------------------------
            // 70 — KUROGANE. Rain, night, no adds and no tricks. Three phases:
            // the technique, the storm, and a last phase that is only the two
            // of them breathing. Jin dies; Aiko is in the mountain fortress.
            var m70 = P_("S70_Kurogane");
            m70.id = 70; m70.missionName = "KUROGANE"; m70.missionType = "BOSS";
            m70.baseShards = 6; m70.nightOverride = true;
            m70.applyTheme = true; m70.theme = Core.EnvThemeId.RainyBattlefield;
            m70.briefing = "The field below the garrison, at first light that never comes through the storm. No guards. No allies. Jin is already there, and he has taken off the half mask.";
            m70.debrief = "Jin died with the half mask in his hand. 'Do not become him.' He told Renzo, dying, that Aiko is alive, held inside Kagehira's mountain fortress. Renzo begins the climb.";
            m70.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.KagehiraBanners };
            m70.challenge = MissionChallenge.UnderTime; m70.challengeShards = 4;
            m70.stages = new[]
            {
                Scene("kurogane_open"),

                St(StageGoal.Reach, "THE FIELD BELOW THE GARRISON", "NO GUARDS",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("HIS TERMS", "THE TWO OF YOU", true,
                    Prop("mask", "THE HALF MASK, SET DOWN", new Vector3(-5f, 0f, 11f), StoryPropShape.Keepsake,
                        "He took it off. He wants to be seen for this."),
                    Prop("stone", "A STONE FOR THE LOSER'S SWORD", new Vector3(5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "One stone. Only one of us will need it.")),

                Scene("kurogane_draw"),

                // PHASE 1 — technique. He reads everything and the lanterns go out.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "JIN KUROGANE",
                    banner = "THE STORM BLADE",
                    foeDef = "jin",
                    spawn = new[] { JinKind },
                    bossHealthGate = 0.65f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                Scene("kurogane_storm"),

                // PHASE 2 — the storm.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE STORM",
                    banner = "HE STOPS HOLDING BACK",
                    bossHealthGate = 0.3f,
                    onComplete = StageEvent.RainStarts,
                    checkpoint = true,
                },

                Scene("kurogane_breath"),

                // PHASE 3 — only the two of them breathing.
                St(StageGoal.BossFight, "THE LAST EXCHANGE", "ONLY THE TWO OF YOU", checkpoint: true),

                Scene("kurogane_death"),
            };
            EditorUtility.SetDirty(m70);
        }
    }
}
