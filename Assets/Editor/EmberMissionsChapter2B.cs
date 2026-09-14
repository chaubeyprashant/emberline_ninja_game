using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 2 — THE LANTERN NETWORK, missions 16-20, built by hand. The second
    /// half of the chapter turns the investigation into a campaign against the
    /// network itself: take its eyes, catch its words, read them, follow them to
    /// the records, and burn the tower that names Renzo to the whole valley.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter2B()
        {
            // ---------------------------------------------------------------
            // 16 — WATCHFIRE. A vertical assault, then the reversal: once the
            // tower is Renzo's, he lights its fire on purpose and holds it.
            var m16 = P_("S16_Watchfire");
            m16.id = 16; m16.missionName = "WATCHFIRE"; m16.missionType = "ASSAULT";
            m16.baseShards = 5; m16.applyTheme = true; m16.theme = Core.EnvThemeId.Castle;
            m16.briefing = "The Broken Banner's watchtower sees every road in the valley. Take it, and the camp goes blind.";
            m16.debrief = "Three territories on the tower's maps, and a fourth marked only with a serpent. The signal fire is Renzo's now, and he let it burn so they would come to him.";
            m16.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m16.challenge = MissionChallenge.UnderTime; m16.challengeShards = 3;
            m16.stages = new[]
            {
                Scene("watch_open"),

                Split("REACH THE TOWER", "THE GATE, OR THE WALL",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { M, P }, new[] { R }),

                St(StageGoal.Wave, "TAKE THE LOWER FLOOR", "THEY HOLD THE STAIR",
                    spawn: new[] { P, B }, checkpoint: true),

                St(StageGoal.Reach, "CLIMB", "HIGHER", point: new Vector3(0f, 0f, 16f)),

                St(StageGoal.Wave, "CLEAR THE ARCHERS ON TOP", "THE ROOF",
                    spawn: new[] { R, R }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Look("READ THEIR MAPS", "WHAT THE TOWER SEES", true,
                    Prop("territories", "THE VALLEY MAP", new Vector3(3f, 0f, 17f), StoryPropShape.CommandPost,
                        "Three territories. And a fourth, marked only with a serpent."),
                    Prop("signals", "THE SIGNAL LEDGER", new Vector3(-3f, 0f, 18f), StoryPropShape.Supply,
                        "Every fire this tower lit, and when. They check in at dusk.")),

                Scene("watch_hold"),

                // The reversal: now Renzo is the one being attacked.
                St(StageGoal.Defend, "HOLD THE TOWER", "THEY CAME, LIKE YOU WANTED",
                    duration: 45f, point: new Vector3(0f, 0f, 16f), spawn: new[] { M, P, B }, checkpoint: true),

                Scene("watch_end"),
            };
            EditorUtility.SetDirty(m16);

            // ---------------------------------------------------------------
            // 17 — THE MESSENGER. A moving duel: he runs, fights, runs again,
            // and on the last stretch stops running and turns.
            var m17 = P_("S17_Messenger");
            m17.id = 17; m17.missionName = "THE MESSENGER"; m17.missionType = "CHASE";
            m17.baseShards = 4; m17.applyTheme = true; m17.theme = Core.EnvThemeId.Forest;
            m17.briefing = "A runner left the Broken Banner at dawn with a sealed pouch. If he reaches the border, whatever he carries is gone.";
            m17.debrief = "Orders sealed by Kagehira himself, and written in a cipher Renzo cannot read. Yet.";
            m17.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m17.challenge = MissionChallenge.UnderTime; m17.challengeShards = 3;
            m17.stages = new[]
            {
                Scene("messenger_open"),

                Look("PICK UP HIS TRAIL", "BOOTS, NOT HOOVES", true,
                    Prop("boots", "RUNNING FOOTPRINTS", new Vector3(-5f, 0f, 11f), StoryPropShape.Tracks,
                        "Boots, not hooves. He's taking the forest paths."),
                    Prop("waterskin", "A DROPPED WATERSKIN", new Vector3(5f, 0f, 14f), StoryPropShape.Camp,
                        "He didn't stop to pick it up. Whatever he carries matters more.")),

                St(StageGoal.Chase, "CATCH THE MESSENGER", "THERE HE IS",
                    duration: 45f, spawn: new[] { N }, onComplete: StageEvent.TargetFlees, checkpoint: true),

                St(StageGoal.Wave, "HIS ESCORT DOUBLES BACK", "YOU'RE NOT THE ONLY ONE RUNNING",
                    spawn: new[] { B, B }),

                St(StageGoal.Reach, "CUT HIM OFF AT THE STREAM", "THE SHORT WAY",
                    point: new Vector3(12f, 0f, -8f), checkpoint: true),

                St(StageGoal.Chase, "DON'T LET HIM REACH THE BORDER", "THE LAST STRETCH",
                    duration: 35f, spawn: new[] { N }),

                Scene("messenger_turn"),

                St(StageGoal.Wave, "HE STOPS RUNNING", "HE TURNS",
                    spawn: new[] { N, R }, checkpoint: true),

                Look("TAKE THE POUCH", "KAGEHIRA'S SEAL", false,
                    Prop("pouch", "A SEALED POUCH", Vector3.zero, StoryPropShape.Body,
                        "The serpent seal, Kagehira's own. And every word in a cipher I can't read.")),

                Scene("messenger_end"),
            };
            EditorUtility.SetDirty(m17);

            // ---------------------------------------------------------------
            // 18 — DEAD LETTER. The investigation is the mission: three pieces
            // of the key, hidden in a relay post full of assassins.
            var m18 = P_("S18_DeadLetter");
            m18.id = 18; m18.missionName = "DEAD LETTER"; m18.missionType = "INVESTIGATION";
            m18.baseShards = 4; m18.nightOverride = true; m18.applyTheme = true; m18.theme = Core.EnvThemeId.Village;
            m18.briefing = "Every cipher the network sends passes through one relay post. The key is inside, in three pieces, guarded by the people who use it.";
            m18.debrief = "Kagehira's order, decoded: \"Find the daughter. She knows where he hid it.\" There is only one daughter this could mean.";
            m18.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.EmptyHome, DressingKind.MissingNotice };
            m18.challenge = MissionChallenge.NoAlarm; m18.challengeShards = 3;
            m18.stages = new[]
            {
                Scene("letter_open"),

                Split("GET INTO THE RELAY POST", "THE ROOF, OR THE DOOR",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { A }, new[] { A, R }),

                St(StageGoal.Stealth, "THE COURIERS' ROOM, UNSEEN", "EVERY ONE OF THEM IS A KILLER",
                    spawn: new[] { A, R }, checkpoint: true),

                Look("THE FIRST PIECE", "A CODEBOOK PAGE", true,
                    Prop("codepage", "A CODEBOOK PAGE", new Vector3(6f, 0f, 14f), StoryPropShape.CommandPost,
                        "A column of symbols, and half a wheel drawn beside it. The first piece.")),

                St(StageGoal.Stealth, "THROUGH THE ASSASSINS' QUARTERS", "THE LAMPS GO OUT",
                    spawn: new[] { A, A }, onComplete: StageEvent.LightsOut, checkpoint: true),

                Look("THE LAST TWO PIECES", "THE CIPHER WHEEL", true,
                    Prop("wheel", "A CIPHER WHEEL", new Vector3(-6f, 0f, 16f), StoryPropShape.KeyPiece,
                        "The wheel itself, missing its centre."),
                    Prop("disc", "A LAMP, TOO HEAVY", new Vector3(-2f, 0f, 19f), StoryPropShape.Cache,
                        "Something sunk in the lamp oil. The centre disc.")),

                Scene("letter_decode"),

                St(StageGoal.Escape, "OUT BEFORE THE RELIEF ARRIVES", "THEY'LL MISS THE WHEEL",
                    duration: 50f, point: new Vector3(0f, 0f, -15f), spawn: new[] { A, R }, checkpoint: true),

                Scene("letter_end"),
            };
            EditorUtility.SetDirty(m18);

            // ---------------------------------------------------------------
            // 19 — THE DAUGHTER. The records are in the one room you cannot leave
            // quietly: the alarm is inevitable, and the mission is the escape.
            var m19 = P_("S19_Daughter");
            m19.id = 19; m19.missionName = "THE DAUGHTER"; m19.missionType = "INFILTRATION";
            m19.baseShards = 5; m19.applyTheme = true; m19.theme = Core.EnvThemeId.Castle;
            m19.briefing = "If Kagehira has been hunting Aiko, his prisoner rolls will say where he looked. They're kept in a records house with one door.";
            m19.debrief = "Confirmed: Aiko Kurogawa was imprisoned, alive, six years ago. The roll says where she was held. It does not say where she is.";
            m19.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.PrisonerCamp, DressingKind.MissingNotice };
            m19.challenge = MissionChallenge.UnderTime; m19.challengeShards = 3;
            m19.stages = new[]
            {
                Scene("daughter_open"),

                St(StageGoal.Reach, "REACH THE RECORDS HOUSE", "ONE DOOR",
                    point: new Vector3(-10f, 0f, 10f), checkpoint: true),

                St(StageGoal.Stealth, "PAST THE PIKE GUARDS", "QUIET, UNTIL IT ISN'T",
                    spawn: new[] { P, P }, checkpoint: true),

                // Reading the rolls is what gives Renzo away.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FIND THE PRISONER ROLLS",
                    banner = "SIX YEARS OF NAMES",
                    checkpoint = true,
                    onComplete = StageEvent.AlarmTriggered,
                    props = new[]
                    {
                        Prop("shelves", "THE PRISONER ROLLS", new Vector3(-12f, 0f, 14f), StoryPropShape.CommandPost,
                            "Six years of rolls. Every person they took, and where."),
                        Prop("entry", "ONE ENTRY", new Vector3(-9f, 0f, 16f), StoryPropShape.Keepsake,
                            "\"Aiko Kurogawa. Alive.\" Six years ago. She was alive six years ago."),
                        Prop("copied", "A COPIED PAGE", new Vector3(-14f, 0f, 17f), StoryPropShape.Supply,
                            "Someone copied these entries out by hand. A neat hand. Not a soldier's."),
                    },
                },

                Scene("daughter_alarm"),

                St(StageGoal.Escape, "GET OUT WITH THE ROLL", "THE WHOLE HOUSE IS AWAKE",
                    duration: 55f, point: new Vector3(0f, 0f, -16f), spawn: new[] { P, R, B }),

                St(StageGoal.Wave, "THEY CUT OFF THE GATE", "THE GATE",
                    spawn: new[] { A, B }, checkpoint: true),

                St(StageGoal.Reach, "OVER THE WALL", "GONE", point: new Vector3(12f, 0f, -18f)),

                Scene("daughter_end"),
            };
            EditorUtility.SetDirty(m19);

            // ---------------------------------------------------------------
            // 20 — THE SECOND LANTERN. Two towers, two routes: whichever you
            // light first, the other is ready for you. The chapter ends on the
            // world learning Renzo's name.
            var m20 = P_("S20_SecondLantern");
            m20.id = 20; m20.missionName = "THE SECOND LANTERN"; m20.missionType = "SABOTAGE";
            m20.baseShards = 5; m20.nightOverride = true; m20.applyTheme = true; m20.theme = Core.EnvThemeId.Castle;
            m20.briefing = "Every transfer order passes through the network's second signal tower, and Aiko's will be in its log. Burn it, and read it first.";
            m20.debrief = "The log sent Aiko to the silent forest. As the tower burned, a rider carried its last message into the dark: KUROGAWA IS COMING.";
            m20.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m20.challenge = MissionChallenge.NoAlarm; m20.challengeShards = 3;
            m20.stages = new[]
            {
                Scene("lantern2_open"),

                // Light either first; the other wakes and waits.
                new MissionStage
                {
                    goal = StageGoal.ReachAny,
                    objective = "CHOOSE A TOWER",
                    banner = "EAST OR WEST",
                    point = new Vector3(11f, 0f, 9f),
                    pointB = new Vector3(-11f, 0f, 9f),
                    spawn = System.Array.Empty<EnemyKind>(),
                    spawnB = System.Array.Empty<EnemyKind>(),
                    onComplete = StageEvent.RouteWakes,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "TAKE THE FIRST TOWER", "ITS GUARD",
                    spawn: new[] { R, R, P }, checkpoint: true),

                Look("READ THE TOWER LOG", "EVERY TRANSFER ORDER", true,
                    Prop("log", "THE TOWER LOG", new Vector3(12f, 0f, 12f), StoryPropShape.CommandPost,
                        "Transfer orders. \"The Kurogawa girl — to the silent forest.\"")),

                St(StageGoal.Defend, "LIGHT IT, AND HOLD", "THE OTHER TOWER IS AWAKE",
                    duration: 30f, point: new Vector3(11f, 0f, 9f), spawn: new[] { A, N }),

                St(StageGoal.Reach, "THE SECOND TOWER", "THEY'RE READY FOR YOU",
                    point: new Vector3(-11f, 0f, 9f), checkpoint: true),

                Scene("lantern2_squad"),

                St(StageGoal.Wave, "BREAK THE ELITE SQUAD", "SENT TO KEEP IT STANDING",
                    spawn: new[] { E, N }, checkpoint: true),

                Scene("lantern2_end"),
            };
            EditorUtility.SetDirty(m20);
        }
    }
}
