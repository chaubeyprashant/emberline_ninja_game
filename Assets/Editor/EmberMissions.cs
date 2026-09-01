using System.IO;
using UnityEditor;
using UnityEngine;
using Emberline.Enemies;
using Emberline.Missions;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Authors the twelve mission plans as ScriptableObjects under
    /// Resources/Missions. Every mission is a sequence of generic stage goals, so
    /// none of this is mission-specific code — adding a thirteenth is another
    /// block here and nothing else.
    ///
    /// Each mission follows the required beat structure: an exploration opening,
    /// a variation beat, a combat encounter, a scripted turn, an optional
    /// objective, and a closing stage that hands off to the debrief.
    /// </summary>
    public static class EmberMissions
    {
        private const EnemyKind B = EnemyKind.Bandit;
        private const EnemyKind R = EnemyKind.Ranged;
        private const EnemyKind S = EnemyKind.Shade;
        private const EnemyKind A = EnemyKind.Assassin;
        private const EnemyKind P = EnemyKind.PikeGuard;
        private const EnemyKind H = EnemyKind.RaiderAxe;
        private const EnemyKind M = EnemyKind.Samurai;
        private const EnemyKind N = EnemyKind.RogueNinja;
        private const EnemyKind E = EnemyKind.EliteWarrior;
        private const EnemyKind C = EnemyKind.Chief;
        private const EnemyKind K = EnemyKind.Kagachi;
        private const EnemyKind O = EnemyKind.Bomber;

        private static MissionStage St(StageGoal goal, string objective, string banner = "",
            int count = 1, float duration = 0f, Vector3 point = default,
            EnemyKind[] spawn = null, StageEvent onComplete = StageEvent.None,
            bool optional = false, bool checkpoint = false, int bonus = 1) => new()
        {
            goal = goal, objective = objective, banner = banner, count = count,
            duration = duration, point = point,
            spawn = spawn ?? System.Array.Empty<EnemyKind>(),
            onComplete = onComplete, optional = optional, checkpoint = checkpoint,
            bonusShards = bonus,
        };

        [MenuItem("Emberline/Build Missions")]
        public static void BuildMissions()
        {
            Directory.CreateDirectory("Assets/Resources/Missions");

            MissionPlan P_(string file)
            {
                var path = $"Assets/Resources/Missions/{file}.asset";
                var m = AssetDatabase.LoadAssetAtPath<MissionPlan>(path);
                if (m == null)
                {
                    m = ScriptableObject.CreateInstance<MissionPlan>();
                    AssetDatabase.CreateAsset(m, path);
                }
                return m;
            }

            var north = new Vector3(0f, 0f, 6.5f);
            var south = new Vector3(0f, 0f, -6.5f);
            var east = new Vector3(10f, 0f, 0f);
            var west = new Vector3(-10f, 0f, 0f);

            // 1 — ASSASSINATION
            var m1 = P_("01_Assassination");
            m1.id = 1; m1.missionName = "A NAME ON A LIST"; m1.missionType = "ASSASSINATION";
            m1.marsh = false; m1.baseShards = 3;
            m1.briefing = "One man on the terraces knows where the lantern-oil goes. He does not need to keep knowing it.";
            m1.debrief = "The list is one name shorter. The next is written in the same hand.";
            m1.stages = new[]
            {
                St(StageGoal.Reach, "FIND HIS POST", "THE EAST TERRACE", point: east, checkpoint: true),
                St(StageGoal.Stealth, "TAKE HIM UNSEEN", "HE HAS NOT SEEN YOU", spawn: new[] { B, B, R },
                    onComplete: StageEvent.AlarmTriggered),
                St(StageGoal.Wave, "CUT YOUR WAY OUT", "THEY KNOW", spawn: new[] { B, A, R }),
                St(StageGoal.Escape, "GET OFF THE ROOF", "GO", duration: 45f, point: south, checkpoint: true),
            };
            EditorUtility.SetDirty(m1);

            // 2 — RESCUE
            var m2 = P_("02_Rescue");
            m2.id = 2; m2.missionName = "WHAT THEY TOOK"; m2.missionType = "RESCUE";
            m2.marsh = false; m2.baseShards = 3;
            m2.briefing = "They took a lantern-keeper alive. That is worse than taking the lantern.";
            m2.debrief = "She walked out on her own feet. She would not say what they asked her.";
            m2.stages = new[]
            {
                St(StageGoal.Investigate, "FIND WHERE THEY HELD HER", "SIGNS OF A STRUGGLE", count: 3, checkpoint: true),
                St(StageGoal.Wave, "BREAK THE GUARD", "GUARDS", spawn: new[] { B, P, R },
                    onComplete: StageEvent.Reinforcements),
                St(StageGoal.Defend, "KEEP THEM OFF HER", "HOLD", duration: 35f, point: north, spawn: new[] { B, A }),
                St(StageGoal.Escort, "WALK HER HOME", "MOVE", checkpoint: true),
            };
            EditorUtility.SetDirty(m2);

            // 3 — INFILTRATION (the worked example from the brief)
            var m3 = P_("03_Infiltration");
            m3.id = 3; m3.missionName = "THE QUIET ROAD"; m3.missionType = "INFILTRATION";
            m3.marsh = false; m3.nightOverride = true; m3.baseShards = 4;
            m3.briefing = "Their captain keeps a ledger. Walk in, read it, walk out. Nobody needs to wake up.";
            m3.debrief = "The ledger named a marsh, a date, and a price. All three were wrong by morning.";
            m3.stages = new[]
            {
                St(StageGoal.Reach, "GET INSIDE THE CORDON", "STAY LOW", point: west, checkpoint: true),
                St(StageGoal.Stealth, "PAST THE GUARDS, UNSEEN", "DO NOT BE SEEN", spawn: new[] { B, B, R, P }),
                St(StageGoal.Investigate, "FIND THE LEDGER", "SEARCH THE CRATES", count: 2, optional: false),
                St(StageGoal.Assassinate, "SILENCE THE CAPTAIN", "HE IS ALONE", spawn: new[] { M },
                    onComplete: StageEvent.AlarmTriggered),
                St(StageGoal.Escape, "OUT BEFORE THEY CLOSE IT", "ALARM", duration: 50f, point: east, checkpoint: true),
                St(StageGoal.BossFight, "THE GATE IS BARRED", "SOMETHING HEAVIER", spawn: new[] { E }),
                St(StageGoal.Reach, "EXTRACTION", "GO HOME", point: south),
            };
            EditorUtility.SetDirty(m3);

            // 4 — ESCORT
            var m4 = P_("04_Escort");
            m4.id = 4; m4.missionName = "THE LANTERN ROAD"; m4.missionType = "ESCORT";
            m4.marsh = false; m4.baseShards = 3;
            m4.briefing = "Old Yotsu carries the flame to the temple. The road is yours to keep open.";
            m4.debrief = "The flame reached the temple. It has not gone out in two hundred years, and not tonight.";
            m4.stages = new[]
            {
                St(StageGoal.Reach, "MEET THE BEARER", "YOTSU IS WAITING", point: west, checkpoint: true),
                St(StageGoal.Escort, "WALK THE FLAME HOME", "MOVE WITH HIM",
                    spawn: new[] { B, B, R }, onComplete: StageEvent.RainStarts),
                St(StageGoal.Eliminate, "CLEAR THE LAST POST", "ONE MORE", count: 3, spawn: new[] { P, A, R }),
                St(StageGoal.Reach, "SEE HIM THROUGH THE GATE", "ALMOST", point: north, optional: true, bonus: 2),
            };
            EditorUtility.SetDirty(m4);

            // 5 — CHASE
            var m5 = P_("05_Chase");
            m5.id = 5; m5.missionName = "RUN THEM DOWN"; m5.missionType = "CHASE";
            m5.marsh = false; m5.baseShards = 3;
            m5.briefing = "A runner left the terraces with something that was not his. He is fast. Be faster.";
            m5.debrief = "He was carrying a lantern-key. Someone is collecting them.";
            m5.stages = new[]
            {
                St(StageGoal.Investigate, "FOLLOW THE TRAIL", "FRESH TRACKS", count: 2, checkpoint: true),
                St(StageGoal.Chase, "CATCH HIM", "THERE", duration: 40f, spawn: new[] { N },
                    onComplete: StageEvent.TargetFlees),
                St(StageGoal.Wave, "HIS FRIENDS OBJECT", "AMBUSH", spawn: new[] { A, A, R }),
                St(StageGoal.Chase, "DO NOT LET HIM REACH THE EDGE", "AGAIN", duration: 30f,
                    spawn: new[] { N }, checkpoint: true),
            };
            EditorUtility.SetDirty(m5);

            // 6 — SURVIVAL
            var m6 = P_("06_Survival");
            m6.id = 6; m6.missionName = "UNTIL THE BELLS"; m6.missionType = "SURVIVAL";
            m6.marsh = true; m6.baseShards = 3;
            m6.briefing = "The marsh sends everything it has. Stay standing until the temple bells ring.";
            m6.debrief = "The bells rang. Whatever was counting on you not lasting has to think again.";
            m6.stages = new[]
            {
                St(StageGoal.Survive, "STAY ALIVE", "THEY COME", duration: 45f,
                    spawn: new[] { S, S, B }, checkpoint: true),
                St(StageGoal.Survive, "STILL STANDING", "MORE", duration: 45f,
                    spawn: new[] { S, S, R, B }, onComplete: StageEvent.WaterRises),
                St(StageGoal.Survive, "THE LAST STRETCH", "THE WATER IS RISING", duration: 40f,
                    spawn: new[] { S, O, A }),
                St(StageGoal.Eliminate, "CLEAR WHAT IS LEFT", "SILENCE", count: 2, spawn: new[] { H, S }),
            };
            EditorUtility.SetDirty(m6);

            // 7 — DEFENSE
            var m7 = P_("07_Defense");
            m7.id = 7; m7.missionName = "THE LAST POST"; m7.missionType = "DEFENSE";
            m7.marsh = false; m7.baseShards = 3;
            m7.briefing = "One lantern post still burns on the north wall. If it goes out, the road goes with it.";
            m7.debrief = "The post still burns. The wall around it does not.";
            m7.stages = new[]
            {
                St(StageGoal.Reach, "GET TO THE POST", "NORTH WALL", point: north, checkpoint: true),
                St(StageGoal.Defend, "HOLD THE POST", "THEY ARE COMING", duration: 40f,
                    point: north, spawn: new[] { B, B, R }),
                St(StageGoal.Defend, "HOLD IT LONGER", "AGAIN", duration: 40f, point: north,
                    spawn: new[] { P, H, R }, onComplete: StageEvent.LightsOut),
                St(StageGoal.Wave, "IN THE DARK", "THE LANTERNS ARE OUT", spawn: new[] { S, A, N }),
            };
            EditorUtility.SetDirty(m7);

            // 8 — STEALTH
            var m8 = P_("08_Stealth");
            m8.id = 8; m8.missionName = "EYES IN THE DARK"; m8.missionType = "STEALTH";
            m8.marsh = false; m8.nightOverride = true; m8.rain = true; m8.baseShards = 4;
            m8.briefing = "Something moves between the chimneys, and it has not seen you yet. Keep it that way.";
            m8.debrief = "They dissolved without a sound, still reaching for the lantern.";
            m8.stages = new[]
            {
                St(StageGoal.Stealth, "CUT THEM DOWN UNSEEN", "THEY HAVE NOT SEEN YOU",
                    spawn: new[] { S, S }, checkpoint: true),
                St(StageGoal.Investigate, "WHAT WERE THEY GUARDING?", "SEARCH", count: 3, optional: true, bonus: 2),
                St(StageGoal.Stealth, "AND THE REST", "MORE IN THE REEDS", spawn: new[] { S, B, R }),
                St(StageGoal.Assassinate, "THE ONE GIVING ORDERS", "THE LAST ONE", spawn: new[] { N }),
            };
            EditorUtility.SetDirty(m8);

            // 9 — DUEL
            var m9 = P_("09_Duel");
            m9.id = 9; m9.missionName = "ONE BLADE, ONE ROAD"; m9.missionType = "DUEL";
            m9.marsh = false; m9.baseShards = 4;
            m9.briefing = "He has been waiting on the bridge since dusk. He will not move until one of you cannot.";
            m9.debrief = "He bowed before he fell. Whatever he served, he served it honestly.";
            m9.stages = new[]
            {
                St(StageGoal.Reach, "WALK OUT TO MEET HIM", "HE IS WAITING", point: north, checkpoint: true),
                St(StageGoal.Duel, "NO INTERFERENCE", "BEGIN", spawn: new[] { M }),
                St(StageGoal.Wave, "HIS STUDENTS DISAGREE", "THEY DO NOT ACCEPT IT",
                    spawn: new[] { A, A }, optional: true, bonus: 2),
            };
            EditorUtility.SetDirty(m9);

            // 10 — BOSS HUNT
            var m10 = P_("10_BossHunt");
            m10.id = 10; m10.missionName = "THE SERPENT'S COIL"; m10.missionType = "BOSS HUNT";
            m10.marsh = true; m10.baseShards = 5;
            m10.briefing = "Follow the drowned lanterns down. Whatever is collecting them is at the bottom.";
            m10.debrief = "The gate closed. The marsh began, at last, to drain.";
            m10.stages = new[]
            {
                St(StageGoal.Investigate, "FOLLOW THE LANTERNS", "THEY LEAD DOWN", count: 3, checkpoint: true),
                St(StageGoal.Wave, "THE GUARD OF THE COIL", "ITS CHOSEN", spawn: new[] { S, S, P, R }),
                St(StageGoal.BossFight, "KAGACHI", "THE SERPENT RISES", spawn: new[] { K },
                    onComplete: StageEvent.WaterRises, checkpoint: true),
            };
            EditorUtility.SetDirty(m10);

            // 11 — ESCAPE
            var m11 = P_("11_Escape");
            m11.id = 11; m11.missionName = "NOTHING BUT THE WAY OUT"; m11.missionType = "ESCAPE";
            m11.marsh = true; m11.baseShards = 4;
            m11.briefing = "The crossing is behind you and it is coming apart. Do not stop to win anything.";
            m11.debrief = "You reached the bank with the lantern still lit. Nothing else came out.";
            m11.stages = new[]
            {
                St(StageGoal.Escape, "GET TO THE CAUSEWAY", "IT IS COMING APART", duration: 40f,
                    point: east, spawn: new[] { S, S }, checkpoint: true),
                St(StageGoal.Wave, "CUT THROUGH", "THEY BAR THE WAY", spawn: new[] { P, H },
                    onComplete: StageEvent.WaterRises),
                St(StageGoal.Escape, "THE LAST STRETCH", "RUN", duration: 35f, point: west,
                    spawn: new[] { S, A }, checkpoint: true),
            };
            EditorUtility.SetDirty(m11);

            // 12 — INVESTIGATION
            var m12 = P_("12_Investigation");
            m12.id = 12; m12.missionName = "WHAT THE WATER KEPT"; m12.missionType = "INVESTIGATION";
            m12.marsh = true; m12.baseShards = 3;
            m12.briefing = "Every cart on the drowned road is untouched except the lanterns. Find out who is counting.";
            m12.debrief = "A hundred lanterns, arranged in a spiral. Somebody is building something.";
            m12.stages = new[]
            {
                St(StageGoal.Investigate, "SEARCH THE CARTS", "NOTHING WAS STOLEN BUT LIGHT",
                    count: 4, checkpoint: true),
                St(StageGoal.Wave, "SOMETHING OBJECTS", "YOU ARE NOT ALONE", spawn: new[] { S, S, R },
                    onComplete: StageEvent.LightsOut),
                St(StageGoal.Investigate, "FINISH THE COUNT IN THE DARK", "THE LANTERNS ARE OUT",
                    count: 3),
                St(StageGoal.Eliminate, "WHATEVER WAS WATCHING", "IT WAS WATCHING", count: 2,
                    spawn: new[] { S, N }),
            };
            EditorUtility.SetDirty(m12);

            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Missions authored: 12 plans under Resources/Missions");
        }
    }
}
