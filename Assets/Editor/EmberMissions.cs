using System.IO;
using UnityEditor;
using UnityEngine;
using Emberline.Enemies;
using Emberline.Missions;
using Emberline.Campaign;
using Emberline.Core;
using System.Collections.Generic;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Authors the campaign's hundred mission plans under Resources/Missions.
    ///
    /// Ten are bespoke, hand-built stage by stage — the ones the design phase
    /// proved out — re-slotted to the campaign numbers they became. The other
    /// ninety are generated from a template per gameplay type, fed by the
    /// mission's own ten fields: its objective becomes the main stage's text,
    /// its climax the climax banner, its discovery the search, its unique event
    /// the scripted turn, its roster the spawns. Same framework, no two alike.
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
            EnemyKind[] spawnB = null, string foeDef = "", string beatId = "") => new()
        {
            goal = goal, objective = objective, banner = banner, count = count,
            duration = duration, point = point,
            spawn = spawn ?? System.Array.Empty<EnemyKind>(),
            spawnB = spawnB ?? System.Array.Empty<EnemyKind>(),
            onComplete = onComplete, optional = optional, checkpoint = checkpoint,
            bonusShards = bonus, foeDef = foeDef, beatId = beatId,
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
            BuildBespoke();
            var generated = 0;
            foreach (var m in Campaign.Campaign.Missions)
            {
                var plan = P_(m.PlanAsset);
                if (string.IsNullOrEmpty(m.plan)) { Generate(plan, m); generated++; }
                Reslot(plan, m);
                EditorUtility.SetDirty(plan);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Emberline] Missions authored: 100 plans ({generated} generated, {100 - generated} bespoke)");
        }

        private static MissionPlan P_(string file)
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

        /// <summary>Everything a plan carries that comes from the campaign entry.</summary>
        private static void Reslot(MissionPlan plan, CampaignMission m)
        {
            plan.id = m.id;
            plan.missionName = m.name;
            plan.missionType = m.Primary.ToString().ToUpperInvariant();
            plan.marsh = m.marsh;
            plan.nightOverride = m.night;
            plan.rain = m.rain;
            plan.snow = m.snow;
            plan.fog = m.fog;
            plan.applyTheme = true;
            plan.theme = m.theme;
            plan.region = m.region.ToString().ToUpperInvariant();
            plan.nextReason = m.nextReason;
            if (string.IsNullOrEmpty(plan.briefing) || string.IsNullOrEmpty(m.plan)) plan.briefing = m.primaryObjective;
            plan.debrief = m.ending;
            if (plan.baseShards < 3) plan.baseShards = 3;
            if (m.IsMajorBoss && plan.baseShards < 5) plan.baseShards = 5;
        }

        // ================================================================ generation

        private static readonly Vector3 North = new(0f, 0f, 6.5f), South = new(0f, 0f, -6.5f),
            East = new(10f, 0f, 0f), West = new(-10f, 0f, 0f),
            NorthEast = new(9f, 0f, 5.5f), NorthWest = new(-9f, 0f, 5.5f);

        /// <summary>
        /// Build a mission's stages from its type and its own words. The
        /// template gives the shape; the fields give it a face.
        /// </summary>
        private static void Generate(MissionPlan plan, CampaignMission m)
        {
            var st = new List<MissionStage>();
            var roster = m.enemies;
            // Distinct kinds first, so a pack of three from a roster of five
            // kinds is three *different* enemies, then repeats. Consecutive
            // missions with overlapping rosters still field different packs.
            var distinct = new List<EnemyKind>();
            foreach (var k in roster) if (!distinct.Contains(k)) distinct.Add(k);
            EnemyKind[] Pack(int from, int count)
            {
                var list = new List<EnemyKind>();
                if (distinct.Count == 0) return list.ToArray();
                for (var i = 0; i < count; i++) list.Add(distinct[(from + i) % distinct.Count]);
                return list.ToArray();
            }
            var light = Pack(0, Mathf.Min(2, roster.Length));
            var mid = Pack(1, Mathf.Min(3, roster.Length));
            var heavy = Pack(m.id % 2, Mathf.Min(5, roster.Length + 1));
            var ranged = System.Array.Exists(roster, k => k == R);
            var foe = m.foe;
            var upperName = m.name;
            var obj = Short(m.primaryObjective).ToUpperInvariant();
            var disc = Short(m.storyDiscovery, 40).ToUpperInvariant();
            var climax = Short(m.climax, 34).ToUpperInvariant();
            var unique = Short(m.uniqueEvent, 34).ToUpperInvariant();

            // Which scripted turn the unique event most resembles.
            var ev = EventFor(m);

            switch (m.Primary)
            {
                case GameplayType.Exploration:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "READ THE GROUND", unique, count: 2));
                    if (m.Has(GameplayType.Combat) || m.Has(GameplayType.Stealth))
                        st.Add(St(m.Has(GameplayType.Stealth) ? StageGoal.Stealth : StageGoal.Wave,
                            m.Has(GameplayType.Stealth) ? "UNSEEN" : "WHAT WAS WAITING", disc, spawn: light, onComplete: ev));
                    else st.Add(St(StageGoal.Reach, "FURTHER IN", disc, point: East, onComplete: ev));
                    st.Add(St(StageGoal.Investigate, "WHAT IT MEANS", "THE LAST PIECE", count: 1, checkpoint: true));
                    if (m.Has(GameplayType.Combat) || m.Has(GameplayType.Survival) || roster.Length > 2)
                        st.Add(FoeOrWave(m, "THE ANSWER COMES ARMED", climax, mid));
                    st.Add(St(StageGoal.Reach, "TAKE IT WITH YOU", "OUT", point: South));
                    break;

                case GameplayType.Combat:
                    if (roster.Length == 0) roster = new[] { B, A };
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "READ THE FIELD", unique, count: 1));
                    st.Add(St(StageGoal.Wave, "THE FIRST OF THEM", "CONTACT", spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.Reach, "PRESS ON", disc, point: East, checkpoint: true));
                    if (m.Has(GameplayType.Defense))
                        st.Add(St(StageGoal.Defend, "HOLD WHAT YOU TOOK", "HOLD", duration: 40f, point: East, spawn: mid));
                    st.Add(FoeOrWave(m, "THE REST OF THEM", climax, heavy));
                    st.Add(St(StageGoal.Reach, "CLEAR", "IT IS DONE", point: South));
                    break;

                case GameplayType.Stealth:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: West, checkpoint: true));
                    st.Add(St(StageGoal.Stealth, "THE OUTER WATCH, UNSEEN", "THEY HAVE NOT SEEN YOU", spawn: light));
                    st.Add(St(StageGoal.Investigate, "FIND WHAT YOU CAME FOR", disc, count: 2, checkpoint: true));
                    if (m.Has(GameplayType.Rescue))
                        st.Add(St(StageGoal.FreePrisoners, "CUT THEM LOOSE", unique, count: 3, onComplete: ev));
                    else if (m.Has(GameplayType.Sabotage))
                        st.Add(St(StageGoal.Defend, "SET THE FIRE", unique, duration: 25f, point: East, spawn: light, onComplete: ev));
                    else
                        st.Add(St(StageGoal.Stealth, "THE INNER WATCH", unique, spawn: mid, onComplete: ev));
                    if (!string.IsNullOrEmpty(foe))
                        st.Add(St(StageGoal.Assassinate, "THE ONE IN CHARGE", climax, foeDef: foe));
                    else if (m.Has(GameplayType.Chase))
                        st.Add(St(StageGoal.Chase, "THEY RUN", climax, duration: 40f, spawn: Pack(2, 1)));
                    else if (m.Has(GameplayType.Combat) || m.Has(GameplayType.Investigation))
                        st.Add(St(StageGoal.Escape, "OUT BEFORE THEY CLOSE IT", climax, duration: 50f, point: East,
                            spawn: ranged ? new[] { R } : light));
                    else
                        st.Add(St(StageGoal.Escape, "LEAVE NO TRACE", climax, duration: 55f, point: East));
                    st.Add(St(StageGoal.Reach, "GONE", "NOBODY KNOWS", point: South));
                    break;

                case GameplayType.Investigation:
                    st.Add(St(StageGoal.Investigate, obj, upperName, count: 3, checkpoint: true));
                    st.Add(m.Has(GameplayType.Stealth)
                        ? St(StageGoal.Stealth, "SOMEONE IS GUARDING THE REST", "QUIET", spawn: light, onComplete: ev)
                        : St(StageGoal.Wave, "SOMEONE OBJECTS", "YOU ARE NOT ALONE", spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.Investigate, "THE LAST PIECES", disc, count: 2, checkpoint: true));
                    if (!(m.Has(GameplayType.Combat) || m.Has(GameplayType.Survival) || m.Has(GameplayType.Chase)))
                        st.Add(St(StageGoal.Reach, "PUT IT TOGETHER", climax, point: East));
                    if (m.Has(GameplayType.Combat) || m.Has(GameplayType.Survival) || m.Has(GameplayType.Chase))
                        st.Add(m.Has(GameplayType.Chase)
                            ? St(StageGoal.Chase, "THEY SAW YOU READ IT", climax, duration: 40f, spawn: Pack(1, 1))
                            : FoeOrWave(m, "THEY CAME BACK FOR IT", climax, mid));
                    st.Add(St(StageGoal.Reach, "TAKE IT BACK", unique, point: West));
                    break;

                case GameplayType.Rescue:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "FIND WHERE THEY ARE KEPT", unique, count: 1));
                    st.Add(St(StageGoal.Wave, "THE GUARD", "GUARDS", spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.FreePrisoners, "CUT THEM LOOSE", disc, count: 3, checkpoint: true));
                    if (m.Has(GameplayType.Defense))
                        st.Add(St(StageGoal.Defend, "KEEP THEM OFF THE PEN", climax, duration: 40f, point: North, spawn: mid));
                    else if (m.Has(GameplayType.Chase))
                        st.Add(St(StageGoal.Chase, "THE ONE WHO RUNS", climax, duration: 40f, spawn: Pack(2, 1)));
                    else st.Add(FoeOrWave(m, "THEY WANT THEM BACK", climax, mid));
                    st.Add(St(m.Has(GameplayType.Escort) ? StageGoal.Escort : StageGoal.Reach,
                        m.Has(GameplayType.Escort) ? "WALK THEM OUT" : "SEE THEM OFF", "THE ROAD", point: South));
                    break;

                case GameplayType.Defense:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Defend, "HOLD", "THEY ARE COMING", duration: 40f, point: North, spawn: light));
                    st.Add(St(StageGoal.Investigate, "SEE TO THE WALLS", disc, count: 1, checkpoint: true));
                    st.Add(St(StageGoal.Defend, "HOLD THEM AGAIN", unique, duration: 45f, point: North, spawn: mid, onComplete: ev));
                    st.Add(FoeOrWave(m, "THE LAST OF THEM", climax, heavy));
                    st.Add(St(StageGoal.Reach, "IT HELD", "STILL STANDING", point: South));
                    break;

                case GameplayType.Escort:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: West, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "CLEAR THE ROAD AHEAD", unique, count: 1));
                    st.Add(St(StageGoal.Wave, "THE FIRST PATROL", "PATROL", spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.Reach, "GO BACK FOR THEM", disc, point: South, checkpoint: true));
                    st.Add(St(StageGoal.Escort, "WALK THEM HOME", "MOVE WITH THEM", spawn: mid));
                    st.Add(St(StageGoal.Reach, "THROUGH", climax, point: North));
                    break;

                case GameplayType.Chase:
                    st.Add(St(StageGoal.Investigate, "PICK UP THE TRAIL", upperName, count: 2, checkpoint: true));
                    st.Add(St(StageGoal.Chase, obj, "THERE", duration: 40f, spawn: Pack(roster.Length - 1, 1), onComplete: StageEvent.TargetFlees));
                    st.Add(St(StageGoal.Wave, "THEY HAD FRIENDS", unique, spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.Investigate, "WHERE DID THEY GO?", disc, count: 1, checkpoint: true));
                    if (!string.IsNullOrEmpty(foe)) st.Add(St(StageGoal.Duel, "NO MORE RUNNING", climax, foeDef: foe));
                    else if (m.Has(GameplayType.Survival))
                        st.Add(St(StageGoal.Escape, "GET OUT BEFORE IT FALLS", climax, duration: 45f, point: South, spawn: light));
                    else st.Add(St(StageGoal.Chase, "DO NOT LET THEM REACH THE EDGE", climax, duration: 30f, spawn: Pack(roster.Length - 1, 1)));
                    st.Add(St(StageGoal.Reach, "DONE", "IT IS OVER", point: South));
                    break;

                case GameplayType.Survival:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true, onComplete: m.fog ? StageEvent.FogRolls : StageEvent.None));
                    st.Add(St(StageGoal.Survive, "STAY ALIVE", "THEY COME", duration: 40f, spawn: light));
                    st.Add(St(StageGoal.Investigate, "A BREATH", disc, count: 1, checkpoint: true));
                    st.Add(St(StageGoal.Survive, "STILL STANDING", unique, duration: 45f, spawn: mid, onComplete: ev));
                    st.Add(m.Has(GameplayType.Chase)
                        ? St(StageGoal.Escape, "GET OUT", climax, duration: 45f, point: South, spawn: light)
                        : FoeOrWave(m, "WHAT IS LEFT", climax, heavy));
                    st.Add(St(StageGoal.Reach, "OUT", "THE FAR SIDE", point: South));
                    break;

                case GameplayType.Boss:
                    st.Add(St(StageGoal.Reach, "WALK OUT TO MEET THEM", upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "READ THE GROUND", unique, count: 1));
                    if (roster.Length > 0 && !(m.boss.HasValue && roster.Length == 0))
                        st.Add(St(StageGoal.Wave, "THEIR CHOSEN", "THEY DO NOT STEP ASIDE", spawn: light, onComplete: ev));
                    st.Add(St(StageGoal.Reach, "THE HELD BREATH", disc, point: default, checkpoint: true));
                    if (m.Has(GameplayType.Combat) && !m.boss.HasValue)
                        st.Add(St(StageGoal.Defend, "HOLD THE GROUND", "THEY PRESS", duration: 35f, point: default, spawn: mid));
                    if (m.boss.HasValue)
                    {
                        st.Add(Phase(obj, climax, 0.6f, new[] { m.boss.Value }, ev));
                        st.Add(St(StageGoal.Wave, "THEY CALL FOR HELP", "ADDS", spawn: light));
                        st.Add(St(StageGoal.BossFight, "FINISH IT", "THE LAST PHASE", checkpoint: true));
                    }
                    else st.Add(St(StageGoal.BossFight, obj, climax, foeDef: foe, spawn: string.IsNullOrEmpty(foe) ? light : System.Array.Empty<EnemyKind>(), checkpoint: true, onComplete: ev));
                    st.Add(St(StageGoal.Reach, "WALK AWAY", "IT IS OVER", point: South));
                    break;

                case GameplayType.Sabotage:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: West, checkpoint: true));
                    st.Add(St(StageGoal.Stealth, "THE WATCH, UNSEEN", "QUIET", spawn: light));
                    st.Add(St(StageGoal.Investigate, "FIND WHAT BURNS", disc, count: 1, checkpoint: true));
                    st.Add(St(StageGoal.Defend, "SET THE FIRE", unique, duration: 30f, point: East, spawn: mid, onComplete: ev));
                    st.Add(St(StageGoal.Escape, "BEFORE IT GOES UP", climax, duration: 45f, point: South, spawn: light));
                    st.Add(St(StageGoal.Reach, "WATCH IT BURN", "FROM THE TREES", point: South));
                    break;

                case GameplayType.Memory:
                    st.Add(St(StageGoal.Cinematic, "REMEMBER", upperName, beatId: m.beat, checkpoint: true));
                    st.Add(St(StageGoal.Reach, "WALK IT AGAIN", unique, point: North, onComplete: ev));
                    st.Add(St(StageGoal.Investigate, "WHAT WAS THERE", disc, count: 2, checkpoint: true));
                    if (roster.Length > 0)
                        st.Add(m.Has(GameplayType.Stealth)
                            ? St(StageGoal.Stealth, "AS SHE DID", climax, spawn: light)
                            : m.Has(GameplayType.Escort)
                                ? St(StageGoal.Escort, "AS SHE DID", climax, spawn: light)
                                : FoeOrWave(m, "AS IT WAS", climax, light));
                    else st.Add(St(StageGoal.Reach, "THE PLACE IT ENDED", climax, point: East));
                    st.Add(St(StageGoal.Reach, "WAKE", "THE TEMPLE FLOOR", point: South));
                    break;

                case GameplayType.Conversation:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Cinematic, "LISTEN", unique, beatId: m.beat, onComplete: ev));
                    if (roster.Length > 0)
                    {
                        st.Add(St(StageGoal.Investigate, "A BREATH", disc, count: 1, checkpoint: true));
                        st.Add(m.Has(GameplayType.Endure) && !string.IsNullOrEmpty(foe)
                            ? St(StageGoal.Endure, "LAST THE LESSON", climax, duration: 40f, foeDef: foe, spawn: light)
                            : m.Has(GameplayType.Defense)
                                ? St(StageGoal.Defend, "HOLD WHILE SHE SPEAKS", climax, duration: 45f, point: North, spawn: mid)
                                : FoeOrWave(m, "THEY INTERRUPT", climax, light));
                    }
                    else
                    {
                        st.Add(St(StageGoal.Reach, "WALK", disc, point: East));
                        st.Add(St(StageGoal.Investigate, m.id == 100 ? "THE THREAD ON HIS WRIST" : "TAKE IT IN", climax, count: 1, checkpoint: true));
                    }
                    st.Add(St(StageGoal.Reach, m.id == 100 ? "LEAVE THE FORTRESS" : "GO", m.id == 100 ? "SUNRISE" : "ON", point: South));
                    break;

                case GameplayType.Endure:
                    st.Add(St(StageGoal.Reach, obj, upperName, point: North, checkpoint: true));
                    st.Add(St(StageGoal.Investigate, "SOMETHING IS HERE", unique, count: 1));
                    st.Add(St(StageGoal.Endure, "SURVIVE", "IT IS HERE", duration: 45f, foeDef: foe, spawn: light, onComplete: StageEvent.FoeWithdraws));
                    st.Add(St(StageGoal.Reach, "GET OFF ITS GROUND", disc, point: South, checkpoint: true));
                    st.Add(m.Has(GameplayType.Chase)
                        ? St(StageGoal.Escape, "RUN", climax, duration: 45f, point: West, spawn: light)
                        : m.Has(GameplayType.Boss)
                            ? St(StageGoal.Cinematic, "ON YOUR KNEES", climax, beatId: m.beat, onComplete: ev)
                            : St(StageGoal.Stealth, "LEAVE NO TRAIL", climax, spawn: light));
                    st.Add(St(StageGoal.Reach, "GONE", "IT LET YOU GO", point: West));
                    break;
            }

            // A mid-mission beat on a non-memory mission plays before the climax.
            if (!string.IsNullOrEmpty(m.beat) && m.Primary is not (GameplayType.Memory or GameplayType.Conversation)
                && !st.Exists(x => x.goal == StageGoal.Cinematic))
                st.Insert(Mathf.Max(1, st.Count - 2), St(StageGoal.Cinematic, "LISTEN", "", beatId: m.beat));

            plan.stages = st.ToArray();
            plan.challenge = ChallengeFor(m);
            plan.challengeSeconds = m.Has(GameplayType.Chase) || m.Has(GameplayType.Survival) ? 240f : 300f;
            plan.challengeShards = m.IsMajorBoss ? 3 : 2;
            plan.baseShards = m.IsMajorBoss ? 5 : m.id > 60 ? 4 : 3;
            plan.dressing = DressingFor(m);
        }

        private static MissionStage FoeOrWave(CampaignMission m, string objective, string banner, EnemyKind[] pack) =>
            string.IsNullOrEmpty(m.foe)
                ? St(StageGoal.Wave, objective, banner, spawn: pack)
                : St(StageGoal.BossFight, objective, banner, foeDef: m.foe, spawn: pack.Length > 2 ? new[] { pack[0], pack[1] } : pack);

        private static StageEvent EventFor(CampaignMission m)
        {
            var e = m.uniqueEvent.ToLowerInvariant();
            if (e.Contains("collapse") || e.Contains("comes down") || e.Contains("avalanche") || e.Contains("falls around")) return StageEvent.Collapse;
            if (e.Contains("turn against") || e.Contains("mutin")) return StageEvent.Mutiny;
            if (e.Contains("fog")) return StageEvent.FogRolls;
            if (e.Contains("water")) return StageEvent.WaterRises;
            if (e.Contains("alarm")) return StageEvent.AlarmTriggered;
            if (e.Contains("ambush") || e.Contains("behind")) return StageEvent.Ambush;
            if (e.Contains("dark") || e.Contains("lantern")) return StageEvent.LightsOut;
            if (e.Contains("rain")) return StageEvent.RainStarts;
            if (e.Contains("reinforce") || e.Contains("come back") || e.Contains("arrive")) return StageEvent.Reinforcements;
            return StageEvent.Reinforcements;
        }

        private static MissionChallenge ChallengeFor(CampaignMission m) => m.Primary switch
        {
            GameplayType.Stealth => MissionChallenge.NoAlarm,
            GameplayType.Investigation => MissionChallenge.NoAlarm,
            GameplayType.Sabotage => MissionChallenge.NoAlarm,
            GameplayType.Rescue => MissionChallenge.SaveAllPrisoners,
            GameplayType.Escort => MissionChallenge.NoCivilianDeaths,
            GameplayType.Defense => MissionChallenge.NoCivilianDeaths,
            GameplayType.Exploration => string.IsNullOrEmpty(m.foe) ? MissionChallenge.UnderTime : MissionChallenge.SilentKill,
            GameplayType.Memory => MissionChallenge.UnderTime,
            GameplayType.Conversation => m.enemies.Length > 0 ? MissionChallenge.UnderTime : MissionChallenge.None,
            GameplayType.Endure => MissionChallenge.UnderTime,
            _ => MissionChallenge.UnderTime,
        };

        private static DressingKind[] DressingFor(CampaignMission m)
        {
            var d = new List<DressingKind>();
            switch (m.region)
            {
                case Region.Ruins: d.Add(DressingKind.BurnedHome); d.Add(DressingKind.MissingNotice); d.Add(DressingKind.HidingVillagers); break;
                case Region.Forest: d.Add(DressingKind.AbandonedWeapons); d.Add(DressingKind.DestroyedCart); d.Add(DressingKind.EmptyHome); break;
                case Region.Mountains: d.Add(DressingKind.KagehiraBanners); d.Add(DressingKind.PrisonerCamp); d.Add(DressingKind.DestroyedCart); break;
                case Region.Marsh: d.Add(DressingKind.DestroyedCart); d.Add(DressingKind.AbandonedWeapons); d.Add(DressingKind.EmptyHome); break;
                case Region.Temples: d.Add(DressingKind.KagehiraBanners); d.Add(DressingKind.EmptyHome); d.Add(DressingKind.AbandonedWeapons); break;
                case Region.Villages: d.Add(DressingKind.HidingVillagers); d.Add(DressingKind.EmptyHome); d.Add(DressingKind.DestroyedCart); break;
                case Region.Fortresses: d.Add(DressingKind.KagehiraBanners); d.Add(DressingKind.PrisonerCamp); d.Add(DressingKind.AbandonedWeapons); break;
                case Region.Snow: d.Add(DressingKind.AbandonedWeapons); d.Add(DressingKind.DestroyedCart); d.Add(DressingKind.EmptyHome); break;
                case Region.Stronghold: d.Add(DressingKind.KagehiraBanners); d.Add(DressingKind.PrisonerCamp); d.Add(DressingKind.AbandonedWeapons); break;
                case Region.Seal: d.Add(DressingKind.KagehiraBanners); d.Add(DressingKind.AbandonedWeapons); d.Add(DressingKind.EmptyHome); break;
                case Region.Dawn: d.Add(DressingKind.EmptyHome); d.Add(DressingKind.MissingNotice); d.Add(DressingKind.HidingVillagers); break;
            }
            // Blood, sparingly: only where the mission's own words put it there.
            var words = (m.uniqueEvent + " " + m.storyDiscovery + " " + m.climax).ToLowerInvariant();
            if (words.Contains("blood") || words.Contains("killed") || words.Contains("execution") || words.Contains("bodies"))
                d.Add(DressingKind.BloodTrail);
            if (m.Has(GameplayType.Rescue) && !d.Contains(DressingKind.PrisonerCamp)) d.Add(DressingKind.PrisonerCamp);
            if (m.region is Region.Ruins or Region.Villages && m.Has(GameplayType.Escort) && !d.Contains(DressingKind.HidingVillagers))
                d.Add(DressingKind.HidingVillagers);
            return d.ToArray();
        }

        /// <summary>
        /// A banner-sized phrase from a sentence of authored prose. Prefers a
        /// quoted line if there is one, otherwise the first clause; never ends
        /// on a word that needs the next one, so a cut reads as a title rather
        /// than as a sentence that stopped.
        /// </summary>
        private static string Short(string text, int max = 30)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var t = text.Trim();

            // "AIKO: 'Where will you go?' RENZO: 'Home.'" → the first spoken line.
            var q1 = t.IndexOfAny(new[] { '\'', '“', '"' });
            if (q1 >= 0)
            {
                var q2 = t.IndexOfAny(new[] { '\'', '”', '"' }, q1 + 1);
                if (q2 > q1 + 3) t = t.Substring(q1 + 1, q2 - q1 - 1);
            }
            // Drop a speaker prefix that survived without quotes.
            var colon = t.IndexOf(':');
            if (colon > 0 && colon < 12 && t.Substring(0, colon).ToUpperInvariant() == t.Substring(0, colon)) t = t.Substring(colon + 1);

            // First clause.
            var cut = t.IndexOfAny(new[] { '.', ':', ';', '—', '?', '!' });
            if (cut > 8) t = t.Substring(0, cut + (t[cut] == '?' ? 1 : 0));
            var comma = t.IndexOf(',');
            if (comma > 12 && comma <= max) t = t.Substring(0, comma);

            // Fit, on a word boundary.
            t = t.Trim().TrimEnd('.', ',', ';', ':');
            if (t.Length > max)
            {
                var sp = t.LastIndexOf(' ', max);
                t = sp > 8 ? t.Substring(0, sp) : t.Substring(0, max);
            }

            // Never end on a word that needs the next one.
            var dangling = new HashSet<string> { "a", "an", "the", "and", "or", "of", "to", "for", "in", "on", "at",
                "with", "by", "from", "into", "that", "who", "as", "but", "his", "her", "its", "their", "is", "are", "was" };
            var words = new List<string>(t.Split(' ', System.StringSplitOptions.RemoveEmptyEntries));
            while (words.Count > 2 && dangling.Contains(words[^1].ToLowerInvariant().Trim('\'', '\"')))
                words.RemoveAt(words.Count - 1);
            return string.Join(" ", words).Trim().TrimEnd(',', ';', ':', '\'', '\"');
        }

        /// <summary>The ten hand-built plans, re-slotted to the campaign numbers they became.</summary>
        private static void BuildBespoke()
        {
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

        }
    }
}
