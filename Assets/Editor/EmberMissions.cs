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
            var obj = Short(m.primaryObjective, 44).ToUpperInvariant();
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

            ApplyDesign(st, m, Pack, distinct.Count);

            plan.stages = st.ToArray();
            plan.challenge = ChallengeFor(m);
            plan.challengeSeconds = m.Has(GameplayType.Chase) || m.Has(GameplayType.Survival) ? 240f : 300f;
            plan.challengeShards = m.IsMajorBoss ? 3 : 2;
            plan.baseShards = m.IsMajorBoss ? 5 : m.id > 60 ? 4 : 3;
            plan.dressing = DressingFor(m);
        }

        /// <summary>
        /// The design layer, applied to a generated plan: the approach choice at
        /// the door, the things in a camp that the player may choose to do, and
        /// the missions that are deliberately not fights.
        ///
        /// The engine already carried all of this and the campaign never used
        /// it. A ReachAny stage holds two entrances; the director remembers
        /// which one was taken and every later stage in the plan then fields its
        /// B roster instead of its A roster. Two missions in a hundred used that.
        /// Every mission whose design offers a real choice uses it now.
        /// </summary>
        private static void ApplyDesign(List<MissionStage> st, CampaignMission m,
            System.Func<int, int, EnemyKind[]> pack, int kinds)
        {
            var d = CampaignDesign.For(m.id);
            if (d == null || st.Count == 0) return;

            // --- Downtime: a mission with no fight in it at all. The brief asks
            // for silence between the action and the game never had any.
            if (d.role == MissionRole.Downtime)
            {
                st.Clear();
                st.Add(St(StageGoal.Reach, Short(m.primaryObjective).ToUpperInvariant(), m.name,
                    point: North, checkpoint: true));
                if (!string.IsNullOrEmpty(m.beat))
                    st.Add(St(StageGoal.Cinematic, "LISTEN", "", beatId: m.beat, onComplete: StageEvent.RainStarts));
                else
                    st.Add(St(StageGoal.Investigate, "SIT WITH IT", "", count: 1, onComplete: StageEvent.RainStarts));
                st.Add(St(StageGoal.Investigate, Short(m.storyDiscovery, 40).ToUpperInvariant(),
                    "", count: m.id % 2 == 0 ? 2 : 3, checkpoint: true));
                st.Add(St(StageGoal.Reach, "WALK WITH THEM", "", point: m.id % 2 == 0 ? East : West));
                st.Add(St(StageGoal.Investigate, Short(m.climax, 34).ToUpperInvariant(), "", count: 1));
                st.Add(St(StageGoal.Reach, "MORNING", "", point: South));
                return;
            }

            // --- The approach: two entrances, and the one you leave is still
            // there. The split carries no guard of its own (the director does
            // not spawn on a ReachAny stage); the difference is that every
            // fight after it fields a different roster on the far route.
            if (d.HasChoice && !st.Exists(x => x.goal == StageGoal.ReachAny))
            {
                var a = d.approaches[0];
                var b = d.approaches[1];
                var split = Split($"{Label(a)} OR {Label(b)}", CampaignDesign.Def(d.camp)?.name ?? m.name,
                    a == Approach.Stealth ? West : North,
                    b == Approach.Ambush || b == Approach.Sabotage ? South : East,
                    System.Array.Empty<EnemyKind>(), System.Array.Empty<EnemyKind>());
                st.Insert(0, split);

                // The far route's rosters. Rotated, so route B is not route A
                // with a different marker on it.
                if (kinds > 0)
                    for (var i = 1; i < st.Count; i++)
                    {
                        var s = st[i];
                        if (s.spawn.Length == 0 || s.goal == StageGoal.ReachAny) continue;
                        // The quiet way in is genuinely thinner and the loud way
                        // in is genuinely thicker. A branch that fields the same
                        // fight twice is a marker, not a decision.
                        var n = b == Approach.Stealth ? Mathf.Max(1, s.spawn.Length - 1)
                            : b is Approach.Allied or Approach.Assault ? s.spawn.Length + 1
                            : s.spawn.Length;
                        s.spawnB = pack(i + 2, n);
                    }

                // The opening stage the template wrote is now the second beat,
                // and the mission must still not open on a fight.
                if (Fights(st[1]) && st[1].spawn.Length > 0)
                    st.Insert(1, St(StageGoal.Investigate, "COMMIT TO IT", "", count: 1));
            }

            // --- A camp is a place with things in it. They are optional, they
            // are not fights, and each one is a reason to be somewhere the
            // objective did not send you.
            if (d.camp != CampId.None && d.role is MissionRole.Assault or MissionRole.Recon)
            {
                var camp = CampaignDesign.Def(d.camp);
                var at = Mathf.Max(1, st.Count - 2);
                if (camp != null && camp.marks.Length > 0)
                {
                    // The mark reads in full on the banner and as a noun phrase
                    // on the live objective line, which is a HUD row, not prose.
                    var mark = camp.marks[m.id % camp.marks.Length];
                    st.Insert(at, St(StageGoal.Investigate, Noun(mark).ToUpperInvariant(),
                        mark.ToUpperInvariant(), count: 1, optional: true, bonus: 1));
                }
                if (d.role == MissionRole.Assault && !st.Exists(x => x.goal == StageGoal.FreePrisoners))
                    st.Insert(Mathf.Max(1, st.Count - 2),
                        St(StageGoal.FreePrisoners, "THE PEN, IF YOU WANT IT", "", count: 2,
                            optional: true, bonus: 2));
            }

            // --- The same six sentences, seventy times. The templates write
            // boilerplate objective lines; a mission has four paragraphs of its
            // own prose and should be using them.
            Vary(st, m);

            // --- Recon: being seen is the fail state. No boss, no last stand.
            if (d.role == MissionRole.Recon)
                for (var i = st.Count - 1; i >= 0; i--)
                    if (st[i].goal is StageGoal.BossFight or StageGoal.Duel or StageGoal.BossPhase)
                        st[i] = St(StageGoal.Investigate, "WATCH HIM LEAVE", "", count: 1, checkpoint: true);
        }

        private static readonly string[] Boilerplate =
        {
            "READ THE GROUND", "READ THE FIELD", "WHAT IT MEANS", "A BREATH",
            "PUT IT TOGETHER", "PICK UP THE TRAIL", "SIT WITH IT", "WALK IT AGAIN",
        };

        /// <summary>The wave lines the templates reuse. The connective beats —
        /// PRESS ON, CLEAR, GONE — are left alone on purpose: they are the
        /// game's voice between beats, not a description of a fight.</summary>
        private static readonly string[] WaveBoilerplate =
        {
            "THE FIRST OF THEM", "THE REST OF THEM", "THE GUARD", "ADDS",
        };

        /// <summary>
        /// Replace a template's stock search line with something this mission
        /// actually says. Each mission carries a story purpose, a unique event,
        /// a discovery and a climax; a plan with three searches in it should be
        /// asking for three different things.
        /// </summary>
        private static void Vary(List<MissionStage> st, CampaignMission m)
        {
            var used = new List<string>();
            foreach (var s in st) if (!string.IsNullOrEmpty(s.objective)) used.Add(s.objective);

            Rewrite(st, used, StageGoal.Investigate, Boilerplate,
                new[] { m.uniqueEvent, m.storyDiscovery, m.storyPurpose, m.climax });
            Rewrite(st, used, StageGoal.Wave, WaveBoilerplate,
                new[] { m.climax, m.storyPurpose, m.uniqueEvent, m.primaryObjective });
        }

        private static void Rewrite(List<MissionStage> st, List<string> used, StageGoal goal,
            string[] stock, string[] source)
        {
            var next = 0;
            foreach (var s in st)
            {
                if (s.goal != goal || System.Array.IndexOf(stock, s.objective) < 0) continue;
                for (var tries = 0; tries < source.Length; tries++)
                {
                    var src = source[(next + tries) % source.Length];
                    var candidate = Short(src, 38).ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(candidate) || used.Contains(candidate)) continue;
                    // Only take a phrase that ends where its clause ends. A line
                    // cut mid-clause ("SEARCH THE DESTROYED") is worse than the
                    // stock line it would replace.
                    if (candidate != Short(src, 200).ToUpperInvariant()) continue;
                    used.Add(candidate);
                    s.objective = candidate;
                    next += tries + 1;
                    break;
                }
            }
        }

        /// <summary>
        /// The thing a phrase is about, as a HUD row. "the patrol that walks the
        /// ridge and not the road" is a fine line on a banner and a terrible one
        /// on an objective row; this returns "THE PATROL" and lets the banner
        /// carry the rest.
        /// </summary>
        private static string Noun(string phrase)
        {
            var stop = new HashSet<string> { "that", "which", "who", "with", "and", "but", "if",
                "where", "when", "from", "on", "in", "at", "above", "below", "is", "are", "was",
                "a", "an", "the", "of", "for", "to", "nobody", "only", "still", "already" };
            var comma = phrase.IndexOfAny(new[] { ',', ';', '.' });
            var t = comma > 4 ? phrase.Substring(0, comma) : phrase;
            var words = t.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var kept = new List<string>();
            foreach (var w in words)
            {
                if (kept.Count >= 2 && stop.Contains(w.ToLowerInvariant().Trim('\'', '"'))) break;
                kept.Add(w);
                if (kept.Count >= 5) break;
            }
            return string.Join(" ", kept);
        }

        private static string Label(Approach a) => a switch
        {
            Approach.Stealth => "QUIET",
            Approach.Ambush => "AMBUSH",
            Approach.Sabotage => "BURN IT",
            Approach.Allied => "TOGETHER",
            _ => "THE FRONT",
        };

        /// <summary>The validator's own definition of a fight, kept in step.</summary>
        private static bool Fights(MissionStage s) => s.goal is StageGoal.Wave or StageGoal.BossFight
            or StageGoal.Duel or StageGoal.Eliminate or StageGoal.Assassinate or StageGoal.BossPhase
            or StageGoal.Stealth or StageGoal.Listen or StageGoal.Survive or StageGoal.Defend
            or StageGoal.Chase;

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
            var truncated = false;
            if (t.Length > max)
            {
                truncated = true;
                var sp = t.LastIndexOf(' ', max);
                t = sp > 8 ? t.Substring(0, sp) : t.Substring(0, max);
            }

            // Never end on a word that needs the next one.
            var dangling = new HashSet<string> { "a", "an", "the", "and", "or", "of", "to", "for", "in", "on", "at",
                "with", "by", "from", "into", "that", "who", "as", "but", "his", "her", "its", "their", "is", "are", "was",
                // Words that always need the clause that follows them.
                "what", "how", "why", "whose", "whom", "whether", "than", "while", "until", "unless",
                "because", "since", "though", "although" };
            var words = new List<string>(t.Split(' ', System.StringSplitOptions.RemoveEmptyEntries));
            while (words.Count > 2 && dangling.Contains(words[^1].ToLowerInvariant().Trim('\'', '\"')))
                words.RemoveAt(words.Count - 1);
            // A cut that stopped just after a conjunction lost the other half of
            // the pair: "break through the gate" became "and break". Drop both.
            if (truncated && words.Count > 3)
            {
                var join = words[^2].ToLowerInvariant();
                if (join is "and" or "or" or "then" or "but") words.RemoveRange(words.Count - 2, 2);
            }
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
            // ---------------------------------------------------------------
            // 1 — ASHES. Renzo comes home. The mission asks one question and
            // answers none of it: WHO IS STILL HERE. No stealth verb is taught
            // here — mission 2 owns stealth, and teaching a verb this mission
            // does not use was the old opening's mistake.
            var m1 = P_("S01_Ashes");
            m1.id = 1; m1.missionName = "ASHES"; m1.missionType = "RETURN";
            m1.marsh = false; m1.baseShards = 3;
            m1.briefing = "Yorune burned ten years ago. Nobody has lived in it since.";
            m1.debrief = "The map in the assassin's coat was drawn this season. Whoever came back to Yorune is still using the road north.";
            m1.dressing = new[] { DressingKind.BurnedHome, DressingKind.AbandonedWeapons,
                DressingKind.DestroyedCart, DressingKind.MissingNotice };
            m1.ruinedVillage = true;   // Yorune is ash, not a village with a bad night
            m1.challenge = MissionChallenge.None;
            m1.stages = new[]
            {
                // The opening plays in place. The mission holds for it.
                St(StageGoal.Cinematic, "", "", beatId: "ashes_return"),

                St(StageGoal.Reach, "WALK INTO YORUNE", "TEN YEARS LATER",
                    point: north, checkpoint: true),

                // The emotional centre, authored rather than scattered: a shrine
                // the fire missed, his father's post, and the one red thing left.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH WHAT IS LEFT OF YOUR HOUSE",
                    banner = "THE KUROGAWA HOUSE",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "shrine", label = "THE FAMILY SHRINE",
                            point = new Vector3(-7.5f, 0f, 7f),
                            shape = StoryPropShape.Shrine,
                            speaker = "RENZO",
                            line = "The shrine is still standing. Of course it is.",
                        },
                        new StoryPropSpec
                        {
                            id = "post", label = "HIS FATHER'S TRAINING POST",
                            point = new Vector3(-3f, 0f, 10.5f),
                            shape = StoryPropShape.TrainingPost,
                            speaker = "RENZO",
                            line = "He'd have had me on this at dawn. Every dawn.",
                        },
                        new StoryPropSpec
                        {
                            id = "bracelet", label = "RED THREAD",
                            point = new Vector3(2.5f, 0f, 12f),
                            shape = StoryPropShape.Keepsake,
                            beatId = "ashes_bracelet",
                        },
                    },
                },

                // The turn: from loss to something being wrong.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FOLLOW THE TRACKS",
                    banner = "THESE ARE FRESH",
                    checkpoint = true,
                    // No Ambush event: it adds an assassin and a bandit behind the
                    // player, and the first fight of the game is one enemy. The
                    // surprise is carried by the Wave's banner instead.
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "tracks", label = "DISTURBED ASH",
                            point = new Vector3(9f, 0f, 8f),
                            shape = StoryPropShape.Tracks,
                            speaker = "RENZO",
                            line = "Days old. Not years.",
                            radius = 2.6f,
                        },
                    },
                },

                // One enemy. It is the combat tutorial, and it is enough.
                St(StageGoal.Wave, "SURVIVE", "HE WAS WAITING", spawn: new[] { A }),

                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH THE BODY",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "map", label = "A MAP, RECENTLY DRAWN",
                            point = Vector3.zero,   // resolved to the body at runtime
                            shape = StoryPropShape.Body,
                            speaker = "RENZO",
                            line = "One road, marked in red. Drawn this season.",
                            beatId = "ashes_map",
                        },
                    },
                },
            };
            EditorUtility.SetDirty(m1);

            // ---------------------------------------------------------------
            // 2 — RED THREAD. Mission 1 asked who is still here; this answers it
            // and asks who sent them. Tracking and stealth, not a rescue: the
            // player learns the enemy is organised by walking through what they
            // left behind, not by being told.
            //
            // Everything is inside the ~34 m play area. The valley's own enemy
            // camp is 71 m out at (46,-54) and unreachable without changing the
            // arena for every other mission, so this camp is mission dressing.
            var m2r = P_("S02_RedThread");
            m2r.id = 2; m2r.missionName = "RED THREAD"; m2r.missionType = "TRACKING";
            m2r.marsh = false; m2r.baseShards = 3;
            m2r.briefing = "The map from the assassin's coat marks one road in red. It leads out of Yorune, north-east, into the trees.";
            m2r.debrief = "Patrol routes, watch posts, supply drops, and a signal line that reaches further than the valley. Somebody is running this.";
            m2r.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.DestroyedCart,
                DressingKind.KagehiraBanners, DressingKind.EmptyHome };
            m2r.ruinedVillage = true;      // still Yorune; it is still ash
            m2r.challenge = MissionChallenge.NoAlarm; m2r.challengeShards = 2;
            m2r.stages = new[]
            {
                // 1 — the map. Short: mission 1 already did the long opening.
                St(StageGoal.Cinematic, "", "", beatId: "thread_open"),

                // 2 — the trail out of the village. Environmental, not a marker
                // trail: disturbed ash, a dropped strap, wheel ruts.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FOLLOW THE RED MARK",
                    banner = "OUT OF YORUNE",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "ash", label = "DISTURBED ASH",
                            point = new Vector3(6f, 0f, 9f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "Boots. Going out, not in.",
                        },
                        new StoryPropSpec
                        {
                            id = "ruts", label = "WHEEL RUTS",
                            point = new Vector3(11f, 0f, 4f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "Loaded carts. They have been supplying something.",
                        },
                    },
                },

                // 3 — the camp. Slept in, cooked in, worked in. The read is
                // "recently", then "for a while".
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FIND THEIR CAMP",
                    banner = "SOMEBODY HAS BEEN LIVING HERE",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "fire", label = "A FIRE, STILL WARM",
                            point = new Vector3(13f, 0f, -3f), shape = StoryPropShape.Camp,
                            speaker = "RENZO", line = "Warm. They will be back for it.",
                        },
                        new StoryPropSpec
                        {
                            id = "supply", label = "SUPPLY CRATES",
                            point = new Vector3(9f, 0f, -8f), shape = StoryPropShape.Supply,
                            speaker = "RENZO", line = "Every crate marked the same. This is not scavenging.",
                        },
                    },
                },

                // 4 — the patrol. Three, spawned unaware: sneak past, take one
                // quietly, or fight. All three work; the challenge rewards the
                // quiet answer rather than forcing it.
                St(StageGoal.Stealth, "GET PAST THE PATROL", "THREE OF THEM",
                    spawn: new[] { B, B, R }, checkpoint: true),

                // 5 — the lookout, and the signal line lighting across the valley.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "REACH THE LOOKOUT",
                    banner = "HIGH GROUND",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "lookout", label = "THE LOOKOUT",
                            point = new Vector3(-6f, 0f, -12f), shape = StoryPropShape.Lookout,
                            beatId = "thread_lanterns", radius = 2.8f,
                            // Answering each other, further out each time.
                            lanternLine = new[]
                            {
                                new Vector3(4f, 0f, -16f),
                                new Vector3(17f, 0f, -13f),
                                new Vector3(26f, 0f, -6f),
                            },
                        },
                    },
                },

                // 6 — the conversation. Examine, not Listen: Listen completes only
                // when every enemy is dead, which is the opposite of eavesdropping.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "GET CLOSE ENOUGH TO HEAR THEM",
                    banner = "TWO OF THEM, TALKING",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "earshot", label = "WITHIN EARSHOT",
                            point = new Vector3(-13f, 0f, -4f), shape = StoryPropShape.Marker,
                            beatId = "thread_kurogawa", radius = 3f,
                        },
                    },
                },

                // 7 — the regional map: the operation is bigger than the patrol.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "TAKE THEIR MAP",
                    banner = "THE WATCH POST",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "regional", label = "A REGIONAL MAP",
                            point = new Vector3(-11f, 0f, 8f), shape = StoryPropShape.Supply,
                            beatId = "thread_map",
                        },
                    },
                },
            };
            EditorUtility.SetDirty(m2r);

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
            // ---------------------------------------------------------------
            // 3 — THE LANTERNS. Mission 2 found the signal line; this follows it
            // up the chain of command. The question it asks — who is leading them
            // — is deliberately not answered: the officer is masked, unnamed, and
            // the mark on his orders is one nobody in Yorune has seen. What the
            // player leaves with is that these men were told to expect him.
            // ---------------------------------------------------------------
            // 4 — THE SILENT FOREST. The investigation acquires consequences.
            // Renzo has been watching them for three missions; here they start
            // looking for him, and the chain of command gets a face. Goro is
            // fought but not beaten: Endure ends on its clock, not on a corpse.
            // ---------------------------------------------------------------
            // 5 — THE TOLL-CAPTAIN. Goro's fight and Goro's death. Three phases
            // through BossPhase health gates with a beat between each, so the
            // mid-fight dialogue happens with him still standing in the middle
            // of the road rather than in a cutscene somewhere else.
            //
            // He gives up exactly two things: that the orders were not his, and
            // a direction. He dies without finishing the sentence about the
            // father, which is mission 6's problem.
            // ---------------------------------------------------------------
            // 6 — THE HOUSE OF KAWAI. The mission after the boss fight, and
            // deliberately slow: no boss, one avoidable search party, and five
            // things to find. Renzo has spent five missions treating his father
            // as the man who failed to save Yorune. He leaves this one knowing
            // his father spent the last year of his life trying to empty it.
            var m6k = P_("S06_HouseOfKawai");
            m6k.id = 6; m6k.missionName = "THE HOUSE OF KAWAI"; m6k.missionType = "INVESTIGATION";
            m6k.marsh = false; m6k.baseShards = 3;
            m6k.applyTheme = true; m6k.theme = Core.EnvThemeId.VillageDawn;
            m6k.briefing = "The old road out of the valley passes a house the Kurogawa name still means something in. Somebody has already been through it.";
            m6k.debrief = "His father kept a list of every family he moved south, a store of medicine and children's clothes for people who were not his, and a letter that stops mid-sentence.";
            m6k.dressing = new[] { DressingKind.BurnedHome, DressingKind.EmptyHome,
                DressingKind.DestroyedCart, DressingKind.MissingNotice };
            m6k.challenge = MissionChallenge.NoAlarm; m6k.challengeShards = 2;
            m6k.stages = new[]
            {
                St(StageGoal.Cinematic, "", "", beatId: "kawai_open"),

                // The house. Three things, and none of them are about the enemy.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH THE HOUSE OF KAWAI",
                    banner = "NOBODY HAS LIVED HERE IN YEARS",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "ledger6", label = "AN OLD LEDGER",
                            point = new Vector3(-8f, 0f, 11f), shape = StoryPropShape.Homestead,
                            speaker = "RENZO",
                            line = "Names. Forty of them, and where each family was sent. He was keeping track of everyone.",
                        },
                        new StoryPropSpec
                        {
                            id = "storebox", label = "A STORAGE BOX",
                            point = new Vector3(-2f, 0f, 15f), shape = StoryPropShape.Supply,
                            speaker = "RENZO",
                            line = "Medicine. Bandages. Children's clothes, sized for people who aren't his. He was preparing for something.",
                        },
                        new StoryPropSpec
                        {
                            id = "wallmark", label = "A MARK ON THE POST",
                            point = new Vector3(5f, 0f, 13f), shape = StoryPropShape.Shrine,
                            speaker = "RENZO",
                            line = "Our crest. And cut beside it, one I don't know — but I know what it means. Shelter.",
                        },
                    },
                },

                // The letter he hid rather than sent, and the memory under it.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FIND WHERE HE HID THINGS",
                    banner = "HE HID THINGS WELL",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "hidden", label = "A FOLDED LETTER",
                            point = new Vector3(9f, 0f, 9f), shape = StoryPropShape.CommandPost,
                            beatId = "kawai_father", radius = 2.6f,
                            speaker = "RENZO",
                            line = "\"If they come, take the families south. Do not let them reach the mountain.\"",
                        },
                    },
                },

                // Somebody has been through here, and not ten years ago.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SOMEBODY SEARCHED THIS PLACE",
                    banner = "FRESH BOOTPRINTS",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "searched", label = "A BROKEN LOCK",
                            point = new Vector3(13f, 0f, 4f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "Lock's been forced. This week. They were here.",
                        },
                    },
                },

                // What they did not find.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FIND WHAT THEY WERE LOOKING FOR",
                    banner = "THEY MISSED SOMETHING",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "cache", label = "UNDER THE STONE",
                            point = new Vector3(6f, 0f, -6f), shape = StoryPropShape.Cache,
                            beatId = "kawai_thread", radius = 2.6f,
                        },
                    },
                },

                St(StageGoal.Cinematic, "", "", beatId: "kawai_letter"),

                // One search party, avoidable. They are looking for records, not
                // for him — which is its own piece of information.
                St(StageGoal.Stealth, "THE SEARCH PARTY", "THEY CAME BACK",
                    spawn: new[] { N, N, A, B }, checkpoint: true),

                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "READ HIS MAP",
                    banner = "THE CACHE, AGAIN",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "oldmap", label = "AN OLD MAP",
                            point = new Vector3(6f, 0f, -6f), shape = StoryPropShape.Marker,
                            speaker = "RENZO",
                            line = "Yorune, the mountain, and one place marked that he never told anyone about. I've seen this. It's the old family road.",
                        },
                    },
                },

                St(StageGoal.Cinematic, "", "", beatId: "kawai_end"),
            };
            EditorUtility.SetDirty(m6k);

            var m5t = P_("S05_TollCaptain");
            m5t.id = 5; m5t.missionName = "THE TOLL-CAPTAIN"; m5t.missionType = "BOSS";
            m5t.marsh = false; m5t.nightOverride = true; m5t.baseShards = 5;
            m5t.applyTheme = true; m5t.theme = Core.EnvThemeId.Forest;
            m5t.briefing = "The token off the runner carries a toll mark. Goro keeps a road, and a road can be walked to.";
            m5t.debrief = "Goro is dead and he was not the one giving orders — the sealed message in his coat reports to somebody it does not name. The burned page says only that a Kurogawa refused.";
            m5t.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m5t.challenge = MissionChallenge.NoAlarm; m5t.challengeShards = 3;
            m5t.stages = new[]
            {
                St(StageGoal.Cinematic, "", "", beatId: "toll_open"),

                // The road he keeps. Tracking, then the post itself.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FOLLOW GORO'S ROUTE",
                    banner = "THE TOLL ROAD",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "tollmark", label = "A TOLL MARKER",
                            point = new Vector3(-6f, 0f, 14f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "Same mark as the token. This road is his.",
                        },
                        new StoryPropSpec
                        {
                            id = "barricade", label = "A BARRICADE",
                            point = new Vector3(6f, 0f, 17f), shape = StoryPropShape.Supply,
                            speaker = "RENZO", line = "Nothing moves along here without him knowing.",
                        },
                    },
                },

                // Stealth or fight — either reaches the post.
                St(StageGoal.Stealth, "GET INTO THE CHECKPOINT", "THEY HOLD THE ROAD",
                    spawn: new[] { P, R, B }, checkpoint: true),

                // The ledger, and under it a page that survived a fire badly.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH THE POST",
                    banner = "HIS PAPERWORK",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "ledger", label = "THE ROUTE LEDGER",
                            point = new Vector3(12f, 0f, 9f), shape = StoryPropShape.CommandPost,
                            speaker = "RENZO",
                            line = "\"Yorune, search complete. Kurogawa, unresolved.\" And at the bottom: if the boy returns, inform the Toll-Captain. They expected me.",
                        },
                        new StoryPropSpec
                        {
                            id = "burned", label = "A BURNED PAGE",
                            point = new Vector3(15f, 0f, 3f), shape = StoryPropShape.Supply,
                            speaker = "RENZO",
                            line = "Older. Half of it is ash. \"Kurogawa… refused… the village… the mountain…\" Father.",
                        },
                    },
                },

                St(StageGoal.Cinematic, "", "", beatId: "toll_confront"),

                // PHASE 1 — control. He spawns here and survives the gate.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "GORO",
                    banner = "THE TOLL-CAPTAIN",
                    foeDef = "goro",
                    spawn = new[] { EnemyKind.Chief },
                    bossHealthGate = 0.62f,
                    checkpoint = true,
                },

                St(StageGoal.Cinematic, "", "", beatId: "toll_mid"),

                // PHASE 2 — pressure. No foeDef and no spawn: the same man.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "HE IS NOT TIRING",
                    banner = "PRESSURE",
                    bossHealthGate = 0.28f,
                },

                St(StageGoal.Cinematic, "", "", beatId: "toll_last"),

                // PHASE 3 — he dies here, and only here.
                St(StageGoal.BossFight, "FINISH IT", "ON ONE KNEE", checkpoint: true),

                St(StageGoal.Cinematic, "", "", beatId: "toll_death"),

                // What he was carrying: a report to someone the report does not name.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH HIM",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "sealed", label = "A SEALED MESSAGE",
                            point = Vector3.zero, shape = StoryPropShape.Body,
                            speaker = "RENZO",
                            line = "\"The Kurogawa has returned. If he survives, continue the search.\" No name under it. He was reporting to somebody.",
                        },
                    },
                },

                St(StageGoal.Cinematic, "", "", beatId: "toll_end"),
            };
            EditorUtility.SetDirty(m5t);

            var m4f = P_("S04_SilentForest");
            m4f.id = 4; m4f.missionName = "THE SILENT FOREST"; m4f.missionType = "HUNT";
            m4f.marsh = false; m4f.nightOverride = true; m4f.baseShards = 4;
            m4f.applyTheme = true; m4f.theme = Core.EnvThemeId.Forest;
            m4f.briefing = "The lantern line went dark behind you on the way out. Someone counted the lights and found one missing.";
            m4f.debrief = "A stamped token off a dead runner: the toll-captain's mark. Goro is the one they report to — and he knew the Kurogawa name before Renzo said a word.";
            m4f.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.EmptyHome, DressingKind.KagehiraBanners };
            m4f.challenge = MissionChallenge.UnderTime; m4f.challengeShards = 3;
            m4f.stages = new[]
            {
                St(StageGoal.Cinematic, "", "", beatId: "forest_open"),

                // Tracking, the same verb mission 2 taught, now in the trees.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FOLLOW THE TRAIL",
                    banner = "INTO THE TREES",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "prints", label = "FOOTPRINTS, LEAVING",
                            point = new Vector3(-7f, 0f, 12f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "Going out. In a hurry.",
                        },
                        new StoryPropSpec
                        {
                            id = "blood", label = "BLOOD ON THE LEAVES",
                            point = new Vector3(2f, 0f, 16f), shape = StoryPropShape.Tracks,
                            speaker = "RENZO", line = "One of them is hurt.",
                        },
                    },
                },

                // The staging point: left in a hurry, and the map on the crate
                // says they are moving people along one route in particular.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH THE STAGING POINT",
                    banner = "RECENTLY ABANDONED",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "fire2", label = "A DEAD FIRE",
                            point = new Vector3(12f, 0f, 14f), shape = StoryPropShape.Camp,
                            speaker = "RENZO", line = "Doused, not burned out. They left fast.",
                        },
                        new StoryPropSpec
                        {
                            id = "patrolmap", label = "A PATROL MAP",
                            point = new Vector3(16f, 0f, 8f), shape = StoryPropShape.Supply,
                            speaker = "RENZO", line = "Routes all round Yorune — and one of them marked differently. They're moving people through here.",
                        },
                    },
                },

                // The transition out of investigation. Four, and they found him.
                St(StageGoal.Wave, "THEY FOUND YOU", "BRANCHES, BEHIND YOU",
                    spawn: new[] { N, N, A, B }),

                // One breaks. Catching him is the mission's only lead.
                St(StageGoal.Chase, "STOP THE RUNNER", "ONE OF THEM RAN",
                    duration: 45f, spawn: new[] { B }),

                St(StageGoal.Cinematic, "", "", beatId: "forest_runner"),

                // The token: the first physical link between scattered patrols
                // and one man.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "SEARCH THE RUNNER",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "token", label = "A STAMPED TOKEN",
                            point = Vector3.zero, shape = StoryPropShape.Body,
                            speaker = "RENZO", line = "Toll-captain's mark. So you're the one they're reporting to.",
                        },
                    },
                },

                // The crossing: they hold this route. Stealth, or fight it.
                St(StageGoal.Stealth, "CROSS THE GUARDED FORD", "THEY HOLD THE CROSSING",
                    spawn: new[] { R, P, B }, checkpoint: true),

                St(StageGoal.Cinematic, "", "", beatId: "forest_goro"),

                // Goro. Endure, not a boss fight: the clock ends it, and he walks
                // away on his own terms with his men covering him.
                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE HIM",
                    banner = "GORO, THE TOLL-CAPTAIN",
                    duration = 26f,
                    foeDef = "goro",
                    spawn = new[] { EnemyKind.Chief },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                // Discovered, and hunted out of the forest.
                St(StageGoal.Escape, "ESCAPE THE FOREST", "THEY ARE COMING",
                    duration: 70f, point: new Vector3(-15f, 0f, -10f), spawn: new[] { N, A, P }),

                St(StageGoal.Cinematic, "", "", beatId: "forest_end"),
            };
            EditorUtility.SetDirty(m4f);

            var m3 = P_("S03_Lanterns");
            m3.id = 3; m3.missionName = "THE LANTERNS"; m3.missionType = "INFILTRATION";
            m3.marsh = false; m3.nightOverride = true; m3.rain = true; m3.baseShards = 4;
            m3.briefing = "The lights answer each other along the ridge. Follow them back to whoever is lighting the first one.";
            m3.debrief = "The orders are signed with a serpent eating a lantern. Nobody in Yorune has ever seen that mark — and they are written to men who were told to expect a Kurogawa.";
            m3.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.EmptyHome,
                DressingKind.AbandonedWeapons };
            m3.challenge = MissionChallenge.NoAlarm; m3.challengeShards = 3;
            m3.stages = new[]
            {
                St(StageGoal.Cinematic, "", "", beatId: "lanterns_open"),

                // A — the first light, and the patrol below turning when it lights.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "FOLLOW THE LANTERNS",
                    banner = "THE FIRST LIGHT",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "lanternA", label = "THE FIRST LANTERN",
                            point = new Vector3(-9f, 0f, 13f), shape = StoryPropShape.Lookout,
                            beatId = "lanterns_signal", radius = 2.8f,
                            lanternLine = new[]
                            {
                                new Vector3(2f, 0f, 17f),
                                new Vector3(14f, 0f, 12f),
                                new Vector3(19f, 0f, 2f),
                            },
                        },
                    },
                },

                // B — two between here and the next light. Avoidable.
                St(StageGoal.Stealth, "REACH THE SECOND LANTERN", "TWO ON THE PATH",
                    spawn: new[] { R, B }, checkpoint: true),

                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "GET CLOSE ENOUGH TO HEAR THEM",
                    banner = "TWO OF THEM, TALKING",
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "lanternB", label = "WITHIN EARSHOT",
                            point = new Vector3(13f, 0f, 11f), shape = StoryPropShape.Marker,
                            beatId = "lanterns_overheard", radius = 3f,
                        },
                    },
                },

                // C — the officer, watched from cover. No fight here on purpose.
                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "WATCH THE COMMAND POST",
                    banner = "SOMEONE IS GIVING ORDERS",
                    checkpoint = true,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "watch", label = "COVER, ABOVE THE POST",
                            point = new Vector3(17f, 0f, -3f), shape = StoryPropShape.Lookout,
                            beatId = "lanterns_officer", radius = 3f,
                        },
                    },
                },

                // The largest stealth section so far — three, and they are awake
                // to noise. Still avoidable; the challenge pays for going unseen.
                St(StageGoal.Stealth, "GET INSIDE THE COMMAND POST", "THREE ON THE POST",
                    spawn: new[] { R, B, P }, checkpoint: true),

                new MissionStage
                {
                    goal = StageGoal.Examine,
                    objective = "TAKE THEIR ORDERS",
                    banner = "THE TABLE",
                    onComplete = StageEvent.AlarmTriggered,
                    props = new[]
                    {
                        new StoryPropSpec
                        {
                            id = "orders", label = "WRITTEN ORDERS",
                            point = new Vector3(9f, 0f, -12f), shape = StoryPropShape.CommandPost,
                            speaker = "RENZO",
                            line = "\"Priority: Kurogawa. Report immediately if he appears.\" Signed with a mark I don't know.",
                        },
                    },
                },

                // Out, with the valley awake. Short and timed rather than a fight.
                St(StageGoal.Escape, "GET OUT", "THEY KNOW YOU ARE HERE",
                    duration: 55f, point: new Vector3(-14f, 0f, -8f)),

                St(StageGoal.Cinematic, "", "", beatId: "lanterns_document"),
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
