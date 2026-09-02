using System.IO;
using UnityEditor;
using UnityEngine;
using Emberline.Enemies;
using Emberline.Missions;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Authors the ten story missions as ScriptableObjects under
    /// Resources/Missions. Every mission is a sequence of generic stage goals, so
    /// none of this is mission-specific code — an eleventh is another block here
    /// and nothing else.
    ///
    /// These replace the twelve mission-*type* templates that used to live here.
    /// Those were named for their mechanic rather than for the mission that
    /// loaded them, and eight of the ten story levels ended up attached to a plan
    /// whose name, briefing and debrief described a different mission: GORO'S
    /// TOLL contained no Goro, THE DROWNED ROAD was a rooftop rescue, and TWIN
    /// LANTERNS was a single duel with one objective.
    ///
    /// Each mission is paced quiet → tension → discovery → combat → quiet →
    /// escalation → climax → resolution: it opens on a stage that spawns nothing,
    /// keeps a no-combat beat past its midpoint, and does not end on a fight.
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
            bool optional = false, bool checkpoint = false, int bonus = 1,
            EnemyKind[] spawnB = null) => new()
        {
            goal = goal, objective = objective, banner = banner, count = count,
            duration = duration, point = point,
            spawn = spawn ?? System.Array.Empty<EnemyKind>(),
            spawnB = spawnB ?? System.Array.Empty<EnemyKind>(),
            onComplete = onComplete, optional = optional, checkpoint = checkpoint,
            bonusShards = bonus,
        };

        /// <summary>A split-route stage: two ways in, each with its own guard.</summary>
        private static MissionStage Split(string objective, string banner,
            Vector3 a, Vector3 b, EnemyKind[] guardA, EnemyKind[] guardB) => new()
        {
            goal = StageGoal.ReachAny, objective = objective, banner = banner,
            point = a, pointB = b, spawn = guardA, spawnB = guardB, checkpoint = true,
        };

        /// <summary>A boss beat that ends on a health threshold, not on a corpse.</summary>
        private static MissionStage Phase(string objective, string banner, float gate,
            EnemyKind[] spawn = null, StageEvent onComplete = StageEvent.None) => new()
        {
            goal = StageGoal.BossPhase, objective = objective, banner = banner,
            bossHealthGate = gate, spawn = spawn ?? System.Array.Empty<EnemyKind>(),
            onComplete = onComplete,
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
            var northEast = new Vector3(9f, 0f, 5.5f);
            var northWest = new Vector3(-9f, 0f, 5.5f);

            // ---------------------------------------------------------------
            // 1 — FIRST BLOOD. The lesson: one enemy who has not seen you, and
            // the game says so out loud. Then it takes the option away.
            var m1 = P_("S01_FirstBlood");
            m1.id = 1; m1.missionName = "FIRST BLOOD"; m1.missionType = "ASSASSINATION";
            m1.marsh = false; m1.baseShards = 3;
            m1.briefing = "Raiders on the east terraces. One of them is watching the lantern line and has not looked behind him once.";
            m1.debrief = "They carried nothing away. Whatever they came for, they did not find it. The notice on the post is three weeks old.";
            m1.dressing = new[] { DressingKind.BurnedHome, DressingKind.MissingNotice,
                DressingKind.HidingVillagers };
            m1.challenge = MissionChallenge.SilentKill; m1.challengeShards = 2;
            m1.stages = new[]
            {
                St(StageGoal.Reach, "GET ABOVE HIS POST", "THE EAST TERRACE", point: east, checkpoint: true),
                St(StageGoal.Stealth, "TAKE HIM UNSEEN", "HE HAS NOT SEEN YOU", spawn: new[] { B },
                    onComplete: StageEvent.AlarmTriggered),
                St(StageGoal.Investigate, "WHAT WERE THEY SEARCHING FOR?", "THE HOUSE IS EMPTY", count: 2),
                St(StageGoal.Wave, "CUT YOUR WAY OUT", "THEY KNOW", spawn: new[] { B, B, R }),
                St(StageGoal.Reach, "OFF THE ROOF", "GO HOME", point: south, checkpoint: true),
            };
            EditorUtility.SetDirty(m1);

            // ---------------------------------------------------------------
            // 2 — THE LANTERN ROAD. You are not the objective. An old man is,
            // and he keeps walking whether or not you are ready.
            var m2 = P_("S02_LanternRoad");
            m2.id = 2; m2.missionName = "THE LANTERN ROAD"; m2.missionType = "ESCORT";
            m2.marsh = false; m2.nightOverride = true; m2.baseShards = 3;
            m2.briefing = "Old Yotsu carries the flame to the temple tonight. They are cutting the posts behind him. Keep the road open.";
            m2.debrief = "The flame reached the temple. Two posts still stand out of nine. A raider's note in the ashes: 'the old flame hangs at the guard's belt'.";
            m2.dressing = new[] { DressingKind.DestroyedCart, DressingKind.HidingVillagers,
                DressingKind.EmptyHome, DressingKind.MissingNotice };
            m2.challenge = MissionChallenge.NoCivilianDeaths; m2.challengeShards = 2;
            m2.stages = new[]
            {
                St(StageGoal.Reach, "WALK THE ROAD", "NINE POSTS TO THE TEMPLE", point: west, checkpoint: true),
                St(StageGoal.Investigate, "WHY THE POSTS?", "CUT, NOT STOLEN", count: 2),
                St(StageGoal.Wave, "THE PATROL", "PATROL ON THE ROAD", spawn: new[] { B, P },
                    onComplete: StageEvent.Ambush),
                St(StageGoal.Wave, "BEHIND YOU", "AMBUSH", spawn: new[] { A, B }),
                St(StageGoal.Reach, "FIND THE BEARER", "HE IS STILL WALKING", point: south, checkpoint: true),
                St(StageGoal.Escort, "WALK THE FLAME HOME", "MOVE WITH HIM", spawn: new[] { B, R },
                    onComplete: StageEvent.RainStarts),
                St(StageGoal.Reach, "SEE HIM THROUGH THE GATE", "THE TEMPLE", point: north),
            };
            EditorUtility.SetDirty(m2);

            // ---------------------------------------------------------------
            // 3 — EYES IN THE DARK. The mission you lose by being seen. Short
            // sight, loud rain, and everything that can hurt you is at range.
            var m3 = P_("S03_EyesInTheDark");
            m3.id = 3; m3.missionName = "EYES IN THE DARK"; m3.missionType = "STEALTH";
            m3.marsh = false; m3.nightOverride = true; m3.rain = true; m3.baseShards = 4;
            m3.briefing = "Something is watching the terraces from the chimneys, and it has not seen you yet. Keep it that way.";
            m3.debrief = "The banner on the last roof is a serpent eating a lantern. Nobody in Yorune has seen that mark before.";
            m3.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.EmptyHome,
                DressingKind.BloodTrail };
            m3.challenge = MissionChallenge.NoAlarm; m3.challengeShards = 3;
            m3.stages = new[]
            {
                St(StageGoal.Reach, "GET UP TO THE ROOFLINE", "STAY LOW", point: northWest, checkpoint: true),
                St(StageGoal.Stealth, "THE WATCHERS, UNSEEN", "TWO ON THE CHIMNEYS", spawn: new[] { R, R },
                    onComplete: StageEvent.LightsOut),
                St(StageGoal.Investigate, "WHAT ARE THEY WATCHING?", "THE LANTERNS ARE OUT", count: 3),
                St(StageGoal.Stealth, "AND THE REST OF THEM", "MORE ON THE NORTH ROOF", spawn: new[] { R, P }),
                St(StageGoal.Assassinate, "THE ONE GIVING ORDERS", "THE SPOTTER", spawn: new[] { N }),
                St(StageGoal.Reach, "DOWN AND OUT", "NOBODY SAW YOU", point: south, checkpoint: true),
            };
            EditorUtility.SetDirty(m3);

            // ---------------------------------------------------------------
            // 4 — GORO'S TOLL. The first wall: a checkpoint that takes payment
            // in people, and the first enemy with a name.
            var m4 = P_("S04_GorosToll");
            m4.id = 4; m4.missionName = "GORO'S TOLL"; m4.missionType = "ASSAULT";
            m4.marsh = false; m4.baseShards = 4;
            m4.briefing = "They have put a toll on the north road. Those who cannot pay are kept. The toll-captain is called Goro.";
            m4.debrief = "The ledger at the post records eleven names and no coin. Goro was not collecting money.";
            m4.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.DestroyedCart,
                DressingKind.KagehiraBanners, DressingKind.BloodTrail };
            m4.challenge = MissionChallenge.SaveAllPrisoners; m4.challengeShards = 3;
            m4.stages = new[]
            {
                St(StageGoal.Reach, "WALK UP TO THE POST", "THE TOLL GATE", point: north, checkpoint: true),
                St(StageGoal.Investigate, "READ THE LEDGER", "ELEVEN NAMES", count: 2),
                St(StageGoal.Wave, "BREAK THE TOLL", "THEY WANT PAYING", spawn: new[] { P, H, R },
                    onComplete: StageEvent.BossArrives),
                St(StageGoal.BossFight, "GORO", "THE TOLL-CAPTAIN", spawn: new[] { C, B, B },
                    checkpoint: true),
                St(StageGoal.Reach, "OPEN THE ROAD", "THE GATE IS YOURS", point: south),
            };
            EditorUtility.SetDirty(m4);

            // ---------------------------------------------------------------
            // 5 — THE SERPENT'S TRAIL. A minute of no enemies at all. You are
            // reading the ground, and the ground is telling you about a war.
            var m5 = P_("S05_SerpentsTrail");
            m5.id = 5; m5.missionName = "THE SERPENT'S TRAIL"; m5.missionType = "INVESTIGATION";
            m5.marsh = true; m5.baseShards = 3;
            m5.briefing = "The raiders went into the marsh with eleven people and came out without them. Follow what they left.";
            m5.debrief = "The orders in his coat are signed with a serpent. The name under it is Kagehira. This is not banditry.";
            m5.dressing = new[] { DressingKind.BloodTrail, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.MissingNotice };
            m5.challenge = MissionChallenge.NoAlarm; m5.challengeShards = 2;
            m5.stages = new[]
            {
                St(StageGoal.Investigate, "FOLLOW WHAT THEY LEFT", "DRAG MARKS, GOING EAST", count: 3,
                    checkpoint: true),
                St(StageGoal.Chase, "THE SCOUT", "HE HAS SEEN YOU", duration: 40f, spawn: new[] { N },
                    onComplete: StageEvent.TargetFlees),
                St(StageGoal.Wave, "HE WAS RUNNING TO THEM", "AMBUSH", spawn: new[] { A, A, R }),
                St(StageGoal.Investigate, "SEARCH HIS COAT", "HE WAS CARRYING ORDERS", count: 2,
                    checkpoint: true),
                St(StageGoal.Reach, "TAKE IT BACK", "SOMEONE NEEDS TO SEE THIS", point: west),
            };
            EditorUtility.SetDirty(m5);

            // ---------------------------------------------------------------
            // 6 — INTO THE REEDS. You cannot see. Standing still and listening
            // is the mechanic, not the mood.
            var m6 = P_("S06_IntoTheReeds");
            m6.id = 6; m6.missionName = "INTO THE REEDS"; m6.missionType = "SURVIVAL";
            m6.marsh = true; m6.baseShards = 4;
            m6.briefing = "The reeds are full of something that was not born there. Go in. Do not run — you will not hear them coming.";
            m6.debrief = "They came apart like wet paper and left no bodies. Whatever they were, they were made, and made here.";
            m6.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.DestroyedCart };
            m6.challenge = MissionChallenge.UnderTime; m6.challengeSeconds = 210f; m6.challengeShards = 3;
            m6.stages = new[]
            {
                St(StageGoal.Reach, "INTO THE REEDS", "THE WATER IS WARM HERE", point: north,
                    checkpoint: true, onComplete: StageEvent.FogRolls),
                St(StageGoal.Listen, "STAND STILL AND LISTEN", "YOU CANNOT SEE THEM", spawn: new[] { S, S }),
                St(StageGoal.Investigate, "WHAT IS IN THE WATER?", "SOMETHING UNDER THE SURFACE", count: 2),
                St(StageGoal.Listen, "AGAIN, AND MORE OF THEM", "THEY KNOW YOU ARE HERE",
                    spawn: new[] { S, S, O }),
                St(StageGoal.Survive, "HOLD UNTIL THE FOG LIFTS", "ALL OF THEM AT ONCE", duration: 35f,
                    spawn: new[] { S, S }, checkpoint: true),
                St(StageGoal.Reach, "OUT OF THE REEDS", "THE BANK", point: south),
            };
            EditorUtility.SetDirty(m6);

            // ---------------------------------------------------------------
            // 7 — THE DROWNED ROAD. The ground is the enemy. The arena changes
            // twice while you are standing in it.
            var m7 = P_("S07_DrownedRoad");
            m7.id = 7; m7.missionName = "THE DROWNED ROAD"; m7.missionType = "CROSSING";
            m7.marsh = true; m7.baseShards = 4;
            m7.briefing = "The causeway floods twice a night. Between the tides it is the only road east. Cross it.";
            m7.debrief = "Every cart on the road is untouched except the lanterns. A hundred of them, gone, and nothing else taken.";
            m7.dressing = new[] { DressingKind.DestroyedCart, DressingKind.EmptyHome,
                DressingKind.AbandonedWeapons };
            m7.challenge = MissionChallenge.UnderTime; m7.challengeSeconds = 240f; m7.challengeShards = 3;
            m7.stages = new[]
            {
                St(StageGoal.Reach, "GET ON THE CAUSEWAY", "THE TIDE IS OUT", point: east,
                    checkpoint: true, onComplete: StageEvent.WaterRises),
                St(StageGoal.Investigate, "SEARCH THE CARTS", "NOTHING TAKEN BUT LIGHT", count: 2),
                St(StageGoal.Wave, "THE ROAD IS HELD", "PIKES ON THE CROSSING", spawn: new[] { P, P, R },
                    onComplete: StageEvent.Ambush),
                St(StageGoal.Wave, "OUT OF THE WATER", "THEY WERE UNDER IT", spawn: new[] { A, B }),
                St(StageGoal.Escape, "BEFORE THE SECOND TIDE", "IT IS COMING BACK", duration: 45f,
                    point: west, spawn: new[] { S }, onComplete: StageEvent.WaterRises, checkpoint: true),
                St(StageGoal.Reach, "THE FAR BANK", "ACROSS", point: south),
            };
            EditorUtility.SetDirty(m7);

            // ---------------------------------------------------------------
            // 8 — TWIN LANTERNS. Two objectives and a real order of operations:
            // whichever you light first, the other one is ready for you.
            var m8 = P_("S08_TwinLanterns");
            m8.id = 8; m8.missionName = "TWIN LANTERNS"; m8.missionType = "TWIN OBJECTIVE";
            m8.marsh = true; m8.baseShards = 4;
            m8.briefing = "Two lanterns hold the bridge, east and west. Both must burn before it opens. Pick your side.";
            m8.debrief = "The bridge opened. Both lanterns are a signal, and you lit them. Somebody now knows you are coming.";
            m8.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.BloodTrail };
            m8.challenge = MissionChallenge.NoAlarm; m8.challengeShards = 3;
            m8.stages = new[]
            {
                St(StageGoal.Reach, "THE BRIDGE HEAD", "TWO TOWERS, ONE BRIDGE", point: south,
                    checkpoint: true),
                Split("CHOOSE A LANTERN", "EAST OR WEST", northEast, northWest,
                    new[] { R, R, P }, new[] { A, N }),
                St(StageGoal.Wave, "TAKE THE TOWER", "ITS GUARD", spawn: new[] { R, R, P },
                    onComplete: StageEvent.RouteWakes),
                St(StageGoal.Investigate, "LIGHT IT", "ONE BURNING", count: 1, checkpoint: true),
                St(StageGoal.Wave, "THE OTHER TOWER", "THEY ARE READY FOR YOU", spawn: new[] { A, N }),
                St(StageGoal.Investigate, "LIGHT THE SECOND", "BOTH BURNING", count: 1),
                St(StageGoal.Duel, "THE BRIDGE IS NOT FREE", "HE WAS WAITING", spawn: new[] { M }),
                St(StageGoal.Reach, "CROSS", "THE BRIDGE IS OPEN", point: north),
            };
            EditorUtility.SetDirty(m8);

            // ---------------------------------------------------------------
            // 9 — THE SERPENT'S GUARD. It asks how you want to play it, once,
            // at the gate, and then holds you to the answer.
            var m9 = P_("S09_SerpentsGuard");
            m9.id = 9; m9.missionName = "THE SERPENT'S GUARD"; m9.missionType = "INFILTRATION";
            m9.marsh = true; m9.nightOverride = true; m9.baseShards = 5;
            m9.briefing = "The fortress on the drowned road holds what they took. There is a drain on the west wall and a gate on the east. One is quiet.";
            m9.debrief = "The armoury is full of Yorune steel. Every blade in it belonged to somebody on the missing list.";
            m9.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.PrisonerCamp,
                DressingKind.KagehiraBanners, DressingKind.EmptyHome };
            m9.challenge = MissionChallenge.NoAlarm; m9.challengeShards = 3;
            m9.stages = new[]
            {
                St(StageGoal.Reach, "GET TO THE WALL", "THE SERPENT'S GUARD", point: south,
                    checkpoint: true),
                Split("THE DRAIN, OR THE GATE", "CHOOSE YOUR WAY IN", west, east,
                    new[] { E }, new[] { P, P, R }),
                St(StageGoal.Wave, "THE WAY YOU CHOSE", "INSIDE", spawn: new[] { E },
                    spawnB: new[] { P, P, R }, onComplete: StageEvent.Reinforcements),
                St(StageGoal.Investigate, "FIND THE ARMOURY", "THEY KEPT THE BLADES", count: 3,
                    checkpoint: true),
                St(StageGoal.Wave, "THE INNER GATE", "ELITES", spawn: new[] { E, E, R }),
                St(StageGoal.Escape, "OUT THROUGH THE DRAIN", "TAKE WHAT YOU CAME FOR", duration: 45f,
                    point: north, checkpoint: true),
            };
            EditorUtility.SetDirty(m9);

            // ---------------------------------------------------------------
            // 10 — KAGACHI. A boss with mission-level phases: the arena changes
            // twice while he is alive, and he does not have to die for it.
            var m10 = P_("S10_Kagachi");
            m10.id = 10; m10.missionName = "KAGACHI"; m10.missionType = "BOSS";
            m10.marsh = true; m10.baseShards = 6;
            m10.briefing = "Follow the drowned lanterns down. All of them are here, arranged, and something is sitting in the middle of them.";
            m10.debrief = "The spiral went out one lantern at a time as he died. The marsh has begun to drain. Kagehira will hear about this by morning.";
            m10.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.DestroyedCart };
            m10.challenge = MissionChallenge.UnderTime; m10.challengeSeconds = 300f; m10.challengeShards = 3;
            m10.stages = new[]
            {
                St(StageGoal.Reach, "DOWN TO THE COIL", "A HUNDRED LANTERNS", point: north,
                    checkpoint: true),
                St(StageGoal.Investigate, "THE SPIRAL", "THEY ARE ARRANGED", count: 3),
                St(StageGoal.Wave, "HIS CHOSEN", "THEY KNEEL TO IT", spawn: new[] { S, S, P }),
                // The held breath before he rises. A boss that arrives on the heel
                // of the last mook is an interruption, not an entrance.
                St(StageGoal.Reach, "INTO THE SPIRAL", "THE WATER GOES STILL", point: default),
                Phase("KAGACHI", "THE SERPENT RISES", 0.75f, new[] { K }, StageEvent.WaterRises),
                St(StageGoal.Wave, "HE CALLS THEM UP", "OUT OF THE WATER", spawn: new[] { S, S }),
                Phase("HE IS HURT", "THE WATER IS RISING", 0.40f, null, StageEvent.LightsOut),
                St(StageGoal.BossFight, "FINISH IT", "IN THE DARK", checkpoint: true),
                St(StageGoal.Reach, "OUT OF THE COIL", "IT IS DRAINING", point: south),
            };
            EditorUtility.SetDirty(m10);

            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Missions authored: 10 story plans under Resources/Missions");
        }
    }
}
