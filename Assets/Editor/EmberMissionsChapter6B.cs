using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 6 — THE DROWNED TEMPLE, missions 56-60, built by hand. Aiko's last
    /// hour of freedom, the cell she scratched her code into, the flooded nave and
    /// the second key, the guardian his father set over the way down, and the
    /// message under it that names the man who sold Yorune.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter6B()
        {
            // ---------------------------------------------------------------
            // 56 — AIKO. Renzo follows his sister's path through the memory: the
            // small ways, the shrine, the searchers. He did not follow her then.
            var m56 = P_("S56_Aiko");
            m56.id = 56; m56.missionName = "AIKO"; m56.missionType = "STEALTH";
            m56.baseShards = 5; m56.applyTheme = true; m56.theme = Core.EnvThemeId.BurningVillage;
            m56.briefing = "The child at the head of the lantern line was Aiko, holding the Seal. The memory follows her back into the fire, and this time Renzo follows too.";
            m56.debrief = "Aiko hid the Seal under the shrine floor before they took her. She never told them where. Kagehira has spent ten years asking her.";
            m56.dressing = new[] { DressingKind.BurnedHome, DressingKind.KagehiraBanners,
                DressingKind.HidingVillagers, DressingKind.EmptyHome };
            m56.challenge = MissionChallenge.UnderTime; m56.challengeShards = 3;
            m56.stages = new[]
            {
                Scene("aiko_open"),

                St(StageGoal.Reach, "AIKO'S WAY", "THE SMALL PATHS",
                    point: new Vector3(-10f, 0f, 8f), checkpoint: true),

                St(StageGoal.Stealth, "UNSEEN, AS SHE WAS", "SHE KNOWS YORUNE BETTER THAN THEY DO",
                    spawn: new[] { A, N }, checkpoint: true),

                Look("WHERE SHE WENT", "THE SHRINE", true,
                    Prop("floor", "A LOOSE SHRINE BOARD", new Vector3(-5f, 0f, 13f), StoryPropShape.Shrine,
                        "Under the shrine floor. He never thinks to look at home. Neither did they."),
                    Prop("thread", "RED THREAD ON A NAIL", new Vector3(4f, 0f, 14f), StoryPropShape.Keepsake,
                        "Her thread, caught on the nail as she went under. She left the way back marked.")),

                Scene("memory_aiko"),

                St(StageGoal.Stealth, "THE SEARCHERS AT THE SHRINE", "THEY HEARD THE BOARD",
                    spawn: new[] { B, N, A }, onComplete: StageEvent.AlarmTriggered, checkpoint: true),

                St(StageGoal.Escape, "RUN, AS SHE RAN", "THEY HAVE SEEN HER",
                    duration: 40f, point: new Vector3(10f, 0f, -12f), spawn: new[] { N }),

                Scene("aiko_end"),
            };
            EditorUtility.SetDirty(m56);

            // ---------------------------------------------------------------
            // 57 — THE PRISONER. Back in the present, at night. The prison wing,
            // read cell by cell; hers is the one with the thread and a wall of
            // her handwriting in a code only a Kurogawa could read.
            var m57 = P_("S57_Prisoner");
            m57.id = 57; m57.missionName = "THE PRISONER"; m57.missionType = "INVESTIGATION";
            m57.baseShards = 5; m57.applyTheme = true; m57.theme = Core.EnvThemeId.Temple;
            m57.briefing = "Renzo wakes on the temple floor with Nire beside him. The prison wing is below them. Aiko was held there for years, and the wardens were told to let no one read the walls.";
            m57.debrief = "Aiko scratched the second key's place into her cell wall in their father's cipher. Renzo read his sister's handwriting for the first time in ten years. The key is under the temple's guardian.";
            m57.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.MissingNotice,
                DressingKind.EmptyHome, DressingKind.BloodTrail };
            m57.challenge = MissionChallenge.NoAlarm; m57.challengeShards = 3;
            m57.stages = new[]
            {
                Scene("prisoner_open"),

                St(StageGoal.Reach, "THE PRISON WING", "BELOW THE HALLS",
                    point: new Vector3(0f, 0f, 10f), checkpoint: true),

                St(StageGoal.Investigate, "READ THE CELLS", "ONE OF THEM IS HERS",
                    count: 3),

                St(StageGoal.Stealth, "THE WARDENS", "THEY DOUSE THE LAMPS AT THE BELL",
                    spawn: new[] { P, R }, onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("HER CELL", "THE ONE WITH THE THREAD", true,
                    Prop("tally", "TEN YEARS OF MARKS", new Vector3(-6f, 0f, 12f), StoryPropShape.StoneMarker,
                        "A mark a day. I stopped counting at three thousand. She didn't."),
                    Prop("wall", "FATHER'S CIPHER, IN HER HAND", new Vector3(0f, 0f, 14f), StoryPropShape.CommandPost,
                        "The cipher he taught us on the training post. Nobody else alive can read it."),
                    Prop("thread", "THE RED THREAD", new Vector3(6f, 0f, 12f), StoryPropShape.Keepsake,
                        "Tied round the bars. So I'd find my way to her.")),

                Scene("prisoner_wall"),

                St(StageGoal.Wave, "THE WARDENS WHO GUARD THE WALLS", "NO ONE READS THEM",
                    spawn: new[] { P, M, M }, checkpoint: true),

                St(StageGoal.Reach, "OUT OF THE WING", "UNDER THE GUARDIAN",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("prisoner_end"),
            };
            EditorUtility.SetDirty(m57);

            // ---------------------------------------------------------------
            // 58 — THE SECOND KEY. The flooded nave, chest-deep. Suzu comes back to
            // the kind of pen she could not open at 29 and opens it. The floor
            // gives when the key comes free.
            var m58 = P_("S58_SecondKey");
            m58.id = 58; m58.missionName = "THE SECOND KEY"; m58.missionType = "RESCUE";
            m58.baseShards = 5; m58.applyTheme = true; m58.theme = Core.EnvThemeId.Temple;
            m58.briefing = "The second key is in the flooded nave. Kagehira's men found the chamber years ago and could not open it, so they keep prisoners there to try the lock.";
            m58.debrief = "Two keys. The prisoners are out, with Suzu. The floor of the nave gave way as the key came free, and under it is the way down, and what his father set to guard it.";
            m58.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.KagehiraBanners,
                DressingKind.AbandonedWeapons, DressingKind.DestroyedCart };
            m58.challenge = MissionChallenge.SaveAllPrisoners; m58.challengeShards = 3;
            m58.stages = new[]
            {
                Scene("second_open"),

                Split("INTO THE NAVE", "THE FLOODED AISLE, OR THE GALLERY",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { S }, new[] { E }),

                St(StageGoal.FreePrisoners, "THE PEN BY THE LOCK", "THEY MAKE THEM TRY IT",
                    count: 3, checkpoint: true),

                Scene("second_suzu"),

                St(StageGoal.Wave, "CHEST-DEEP", "THE WATER DOES NOT CARE",
                    spawn: new[] { S, E }, onComplete: StageEvent.WaterRises, checkpoint: true),

                St(StageGoal.Wave, "THE NAVE'S LAST GUARD", "WAIST-DEEP AND WAITING",
                    spawn: new[] { E, M }),

                Look("THE SECOND LOCK", "ONLY A KUROGAWA", true,
                    Prop("key", "THE SECOND KEY", new Vector3(0f, 0f, -10f), StoryPropShape.KeyPiece,
                        "The same grooves as the first. It comes to my hand like it knows it."),
                    Prop("scratches", "YEARS OF FAILED TRIES", new Vector3(6f, 0f, -9f), StoryPropShape.Passage,
                        "Knife marks round the lock. Hundreds. It was never going to open for them.")),

                St(StageGoal.Investigate, "LIFT THE SECOND KEY", "THE FLOOR GIVES",
                    count: 1, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Escape, "OFF THE FALLING FLOOR", "THE WAY DOWN OPENS",
                    duration: 45f, point: new Vector3(-10f, 0f, -12f), spawn: new[] { S }),

                Scene("second_end"),
            };
            EditorUtility.SetDirty(m58);

            // ---------------------------------------------------------------
            // 59 — THE DROWNED GUARDIAN. Night, in the water. A warden bound to the
            // family: three phases, the lights go out, the water rises, and it
            // does not tire. The chapter's hardest fight, and a duel villain.
            var m59 = P_("S59_DrownedGuardian");
            m59.id = 59; m59.missionName = "THE DROWNED GUARDIAN"; m59.missionType = "BOSS";
            m59.baseShards = 6; m59.applyTheme = true; m59.theme = Core.EnvThemeId.Temple;
            m59.briefing = "Under the nave is the way down, and across it stands the thing his father set there ten years ago, to keep the last of the Seal from anyone. Including his own children.";
            m59.debrief = "The Drowned Guardian fell in the dark water. Under it was not a key but a message, cut in stone by his father's hand.";
            m59.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice, DressingKind.EmptyHome };
            m59.challenge = MissionChallenge.UnderTime; m59.challengeShards = 4;
            m59.stages = new[]
            {
                Scene("guardian_open"),

                St(StageGoal.Reach, "DOWN INTO THE DARK WATER", "THE WAY DOWN",
                    point: new Vector3(0f, 0f, 8f), onComplete: StageEvent.WaterRises, checkpoint: true),

                Look("FATHER'S BINDING", "HE SET IT HERE", true,
                    Prop("stones", "THE BINDING STONES", new Vector3(-6f, 0f, 11f), StoryPropShape.StoneMarker,
                        "Father's mark on every stone. He didn't find this thing. He put it here."),
                    Prop("tried", "THE ONES WHO TRIED", new Vector3(6f, 0f, 11f), StoryPropShape.Body,
                        "Kagehira's men, in serpent armour. Years of them. None of them got past.")),

                Scene("guardian_wakes"),

                // PHASE 1 — the warden, patient, in the shallows. When the gate is
                // reached the lamps drown and the fight goes dark.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE DROWNED GUARDIAN",
                    banner = "WARDEN OF THE SECOND KEY",
                    foeDef = "drownedguardian",
                    spawn = new[] { E },
                    bossHealthGate = 0.65f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "WHAT IT DRAGGED DOWN", "THEY RISE FOR IT",
                    spawn: new[] { S, S }),

                Scene("guardian_blood"),

                // PHASE 2 — it drags the fight into the water, and the water rises.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "INTO THE WATER",
                    banner = "IT DOES NOT TIRE",
                    bossHealthGate = 0.3f,
                    onComplete = StageEvent.WaterRises,
                    checkpoint = true,
                },

                St(StageGoal.Survive, "IT IS UNDER THE WATER WITH YOU", "HOLD ON",
                    duration: 25f, spawn: new[] { S }),

                St(StageGoal.BossFight, "ITS LAST PHASE", "IN THE DARK, IN THE WATER", checkpoint: true),

                Look("UNDER IT", "NOT A KEY", false,
                    Prop("message", "A MESSAGE CUT IN STONE", new Vector3(0f, 0f, -12f), StoryPropShape.CommandPost,
                        "Not the third key. Words. His hand. \"Ren. If you are reading this, it let you pass.\"")),

                Scene("guardian_end"),
            };
            EditorUtility.SetDirty(m59);

            // ---------------------------------------------------------------
            // 60 — THE TRUTH BENEATH YORUNE. No fight. The walk to the message
            // and the walk back, changed. The chapter's revelation and its card.
            var m60 = P_("S60_TruthBeneathYorune");
            m60.id = 60; m60.missionName = "THE TRUTH BENEATH YORUNE"; m60.missionType = "CONVERSATION";
            m60.baseShards = 4; m60.applyTheme = true; m60.theme = Core.EnvThemeId.Temple;
            m60.briefing = "The guardian is dead and the temple is quiet for the first time in ten years. His father's message is below. Nire, Toku and Suzu wait at the stair.";
            m60.debrief = "Kagehira burned Yorune for the Black Seal, and Renzo's father refused him. The man who told Kagehira where to find it was Jin Kurogane. Renzo has stopped looking for answers. He is looking for Kurogane.";
            m60.dressing = new[] { DressingKind.EmptyHome, DressingKind.MissingNotice,
                DressingKind.AbandonedWeapons };
            m60.challenge = MissionChallenge.None; m60.challengeShards = 0;
            m60.stages = new[]
            {
                Scene("truth_open"),

                St(StageGoal.Reach, "THE WALK TO THE MESSAGE", "BELOW YORUNE",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                Look("WHAT HE LEFT", "HIS LAST WORDS TO US", true,
                    Prop("wall", "FATHER'S MESSAGE", new Vector3(0f, 0f, 14f), StoryPropShape.CommandPost,
                        "The whole wall, in his hand. He knew he wouldn't come back to say it."),
                    Prop("drawing", "A CHILD'S DRAWING", new Vector3(-6f, 0f, 12f), StoryPropShape.Keepsake,
                        "Aiko's. Three people and a small one with a sword. He carried it down here."),
                    Prop("nokey", "AN EMPTY THIRD LOCK", new Vector3(6f, 0f, 12f), StoryPropShape.KeyPiece,
                        "The third key isn't here. He never made it. Or someone took it first.")),

                Scene("father_message"),

                Look("THE NAME", "THE MAN WHO DREW THE MAP", false,
                    Prop("name", "KUROGANE", new Vector3(0f, 0f, -8f), StoryPropShape.StoneMarker,
                        "Kurogane. The crest on the hunters in the fog. Father's friend.")),

                Scene("truth_changed"),

                St(StageGoal.Reach, "THE WALK BACK", "CHANGED",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("truth_end"),
            };
            EditorUtility.SetDirty(m60);
        }
    }
}
