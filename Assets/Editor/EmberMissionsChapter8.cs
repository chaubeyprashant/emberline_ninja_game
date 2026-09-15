using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 8 — THE IRON FORTRESS, missions 71-75, built by hand. The climb
    /// that is killing Kagehira's own men, the guns taken in the cold, the
    /// mountain coming down, the wall and the first of the nine Iron Guard, and
    /// the drain that is the silent way in.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter8()
        {
            // ---------------------------------------------------------------
            // 71 — THE MOUNTAIN ROAD. Snow. Nire will not climb. The road is
            // littered with the enemy's dead, and the blizzard closes on the
            // first garrison of the ascent.
            var m71 = P_("S71_MountainRoad");
            m71.id = 71; m71.missionName = "THE MOUNTAIN ROAD"; m71.missionType = "EXPLORATION";
            m71.baseShards = 5; m71.applyTheme = true; m71.theme = Core.EnvThemeId.Mountain;
            m71.briefing = "Jin said the mountain fortress. The road up it is the only road, and it is cold enough to kill. Nire walked Renzo to the snow line and no further.";
            m71.debrief = "The road is littered with Kagehira's own dead. The mountain is killing his men faster than Renzo is. Above, the glow of a camp with artillery in it, covering the road.";
            m71.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.DestroyedCart, DressingKind.KagehiraBanners };
            m71.challenge = MissionChallenge.UnderTime; m71.challengeShards = 3;
            m71.stages = new[]
            {
                Scene("ascent_open"),

                St(StageGoal.Reach, "THE FIRST SWITCHBACK", "THE ROAD IS THE ENEMY",
                    point: new Vector3(-10f, 0f, 8f), checkpoint: true),

                Look("THEIR OWN DEAD", "THE MOUNTAIN DID THIS", true,
                    Prop("pikeman", "A PIKEMAN, FROZEN AT HIS POST", new Vector3(-6f, 0f, 12f), StoryPropShape.Body,
                        "Still standing his watch. Nobody came to relieve him. Nobody came back for him."),
                    Prop("rations", "RATIONS, UNOPENED", new Vector3(4f, 0f, 13f), StoryPropShape.Supply,
                        "Not hunger. The cold. They were ordered up faster than the mountain allows.")),

                St(StageGoal.Investigate, "WHAT IS KILLING THEM", "FOLLOW THE DEAD UP",
                    count: 2),

                Scene("ascent_frozen"),

                St(StageGoal.Wave, "THE FIRST GARRISON", "IN THE BLIZZARD",
                    spawn: new[] { P, H }, onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Survive, "THE WHITE CLOSES IN", "HOLD ON TO THE ROAD",
                    duration: 30f, spawn: new[] { R, A }),

                St(StageGoal.Reach, "TOWARD THE GLOW", "SOMETHING WARM UP THERE",
                    point: new Vector3(10f, 0f, -12f), checkpoint: true),

                Look("THE CAMP ABOVE", "GUNS", false,
                    Prop("guns", "ARTILLERY GLOW ON THE RIDGE", new Vector3(12f, 0f, -15f), StoryPropShape.Lookout,
                        "Guns, laid on the road. Nobody walks up this mountain while they stand.")),

                Scene("ascent_end"),
            };
            EditorUtility.SetDirty(m71);

            // ---------------------------------------------------------------
            // 72 — THE FROZEN CAMP. Night, snow. Suzu's ridge or Tsuru's ravine.
            // Fires set in the cold burn slow, the powder carriers know it, and
            // the magazine goes up with the slope above it.
            var m72 = P_("S72_FrozenCamp");
            m72.id = 72; m72.missionName = "THE FROZEN CAMP"; m72.missionType = "SABOTAGE";
            m72.baseShards = 5; m72.applyTheme = true; m72.theme = Core.EnvThemeId.Mountain;
            m72.briefing = "The artillery camp covers the road. Suzu has a ridge above it and Tsuru has a ravine below. The fires will burn slow in this cold, and the camp will have time to notice.";
            m72.debrief = "The camp's stores were meant for a siege: Kagehira expects an army, not one man. The magazine went up, the mountain shivered, and the slope above the road is loose.";
            m72.dressing = new[] { DressingKind.DestroyedCart, DressingKind.KagehiraBanners,
                DressingKind.AbandonedWeapons, DressingKind.PrisonerCamp };
            m72.challenge = MissionChallenge.UnderTime; m72.challengeShards = 3;
            m72.stages = new[]
            {
                Scene("guns_open"),

                Split("INTO THE CAMP", "SUZU'S RIDGE, OR TSURU'S RAVINE",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { R }, new[] { P }),

                St(StageGoal.Stealth, "THE GUN CREWS", "THEY ARE CARRYING POWDER",
                    spawn: new[] { O, P }, checkpoint: true),

                Look("A SIEGE CAMP", "HE EXPECTS AN ARMY", true,
                    Prop("stores", "SIEGE STORES FOR A MONTH", new Vector3(-6f, 0f, 12f), StoryPropShape.Supply,
                        "A month of rice and powder. For one road. He's afraid of something coming up it."),
                    Prop("orders", "ORDERS: \"HOLD AGAINST THE ARMY\"", new Vector3(6f, 0f, 12f), StoryPropShape.CommandPost,
                        "An army. He thinks I'm bringing an army. I'm bringing Suzu and a man with a bow.")),

                St(StageGoal.Defend, "SET THE FIRST FIRE", "IT BURNS SLOW IN THE COLD",
                    duration: 30f, point: new Vector3(0f, 0f, 10f), spawn: new[] { O, O },
                    onComplete: StageEvent.AlarmTriggered, checkpoint: true),

                Scene("guns_alarm"),

                St(StageGoal.Wave, "THE POWDER CARRIERS", "KILL THEM BEFORE THEY REACH YOU",
                    spawn: new[] { O, H }),

                St(StageGoal.Escape, "BEFORE THE MAGAZINE GOES", "THE FIRE HAS REACHED IT",
                    duration: 40f, point: new Vector3(-10f, 0f, -12f), spawn: new[] { R },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Scene("guns_end"),
            };
            EditorUtility.SetDirty(m72);

            // ---------------------------------------------------------------
            // 73 — THE AVALANCHE. Snow and fog. One direction, and the clock is
            // the mountain. Kagehira's men are caught in it too, and the ones
            // who live stop fighting.
            var m73 = P_("S73_Avalanche");
            m73.id = 73; m73.missionName = "THE AVALANCHE"; m73.missionType = "SURVIVAL";
            m73.baseShards = 5; m73.applyTheme = true; m73.theme = Core.EnvThemeId.Mountain;
            m73.briefing = "The slope above the road was loosened by the magazine. Daigo heard it crack before dawn. There are minutes, and one direction: up.";
            m73.debrief = "The mountain came down on everyone on the road. Kagehira's survivors stopped fighting and started digging. Out of the white stood the outer wall. There is no more road.";
            m73.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.DestroyedCart,
                DressingKind.BloodTrail };
            m73.challenge = MissionChallenge.UnderTime; m73.challengeShards = 3;
            m73.stages = new[]
            {
                Scene("slide_open"),

                St(StageGoal.Escape, "RUN", "THE MOUNTAIN IS COMING DOWN",
                    duration: 40f, point: new Vector3(0f, 0f, 12f), spawn: new[] { A },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Look("CAUGHT TOGETHER", "NOBODY'S ENEMY NOW", true,
                    Prop("digging", "SOLDIERS DIGGING OUT SOLDIERS", new Vector3(-6f, 0f, 12f), StoryPropShape.Body,
                        "They're not looking at me. They're digging with their hands. The mountain doesn't take sides."),
                    Prop("way", "ONE WAY LEFT", new Vector3(5f, 0f, 14f), StoryPropShape.Tracks,
                        "Down is gone. The only road left runs up to the wall.")),

                St(StageGoal.Chase, "THE SCOUT WHO SAW YOU", "HE WILL WARN THE WALL",
                    duration: 35f, spawn: new[] { A }, checkpoint: true),

                Scene("slide_truce"),

                St(StageGoal.Survive, "THE SECOND SLIDE", "WHAT THE SNOW KEPT",
                    duration: 30f, spawn: new[] { S, R }, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Escape, "OUT OF THE WHITE", "THE LAST STRETCH",
                    duration: 35f, point: new Vector3(0f, 0f, -14f)),

                St(StageGoal.Reach, "THE WALL", "THERE IS NO MORE ROAD",
                    point: new Vector3(0f, 0f, -16f)),

                Scene("slide_end"),
            };
            EditorUtility.SetDirty(m73);

            // ---------------------------------------------------------------
            // 74 — THE OUTER WALL. Assault, defense and a boss in one: take the
            // gate, hold the breach, then the first of the nine Iron Guard comes
            // to retake it, in Yorune steel. Tsuru takes the wall he deserted.
            var m74 = P_("S74_OuterWall");
            m74.id = 74; m74.missionName = "THE OUTER WALL"; m74.missionType = "BOSS";
            m74.baseShards = 6; m74.applyTheme = true; m74.theme = Core.EnvThemeId.Fortress;
            m74.briefing = "The outer wall of the fortress. Daigo takes the gate, Toku the forge-yard, and Tsuru the wall he once deserted from. Take it, hold it, and hold it again when they come back for it.";
            m74.debrief = "The wall is Renzo's. Its commander, the first of Kagehira's nine Iron Guard, wore Yorune steel with the smith's mark on it. The wall's plans mark one silent way into the inner fortress.";
            m74.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail, DressingKind.DestroyedCart };
            m74.challenge = MissionChallenge.UnderTime; m74.challengeShards = 4;
            m74.stages = new[]
            {
                Scene("wall_open"),

                St(StageGoal.Reach, "UNDER THE WALL", "OUT OF THE WHITE",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                St(StageGoal.Wave, "TAKE THE GATE", "PIKES ON THE STAIR",
                    spawn: new[] { P, P, R }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("wall_tsuru"),

                St(StageGoal.Defend, "HOLD THE BREACH", "THEY COME BACK FOR IT",
                    duration: 40f, point: new Vector3(0f, 0f, 8f), spawn: new[] { H, E, R },
                    checkpoint: true),

                Look("YORUNE STEEL", "THE SMITH'S MARK", true,
                    Prop("shield", "A SHIELD WITH TOKU'S MARK", new Vector3(-5f, 0f, 11f), StoryPropShape.KeyPiece,
                        "Toku's mark, on a Kagehira shield. Yorune steel, reforged for the men who burned it."),
                    Prop("banner", "NINE BANNERS OVER THE GATE", new Vector3(5f, 0f, 12f), StoryPropShape.StoneMarker,
                        "Nine banners. One for each of the warlord's Iron Guard.")),

                Scene("wall_ironguard"),

                // PHASE 1 — the first of nine, behind his shield. The wall torches
                // go out when it cracks.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE IRON GUARD",
                    banner = "KAGEHIRA'S SHIELD",
                    foeDef = "ironguard",
                    spawn = new[] { E },
                    bossHealthGate = 0.6f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "HIS SHIELD LINE", "THEY CLOSE AROUND HIM",
                    spawn: new[] { P, E }),

                // PHASE 2 — no ground given; the breach itself starts to go.
                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "NO GROUND GIVEN",
                    banner = "THE BREACH IS FALLING",
                    bossHealthGate = 0.25f,
                    onComplete = StageEvent.Collapse,
                },

                St(StageGoal.BossFight, "THE FIRST OF NINE", "IN THE BREACH", checkpoint: true),

                Look("THE WALL'S PLANS", "A SILENT WAY IN", false,
                    Prop("plans", "THE COMMANDER'S PLANS", new Vector3(0f, 0f, -12f), StoryPropShape.CommandPost,
                        "The inner fortress, drawn in full. One drain marked in red: \"silent\".")),

                Scene("wall_end"),
            };
            EditorUtility.SetDirty(m74);

            // ---------------------------------------------------------------
            // 75 — THE SILENT GATE. Night, snow. The meltwater drain under the
            // inner wall, an armoury full of Yorune steel, and the inner gate's
            // two elites and their archers, passed unseen.
            var m75 = P_("S75_SilentGate");
            m75.id = 75; m75.missionName = "THE SILENT GATE"; m75.missionType = "STEALTH";
            m75.baseShards = 5; m75.applyTheme = true; m75.theme = Core.EnvThemeId.Fortress;
            m75.briefing = "The drain marked 'silent' runs under the inner wall with the meltwater. Suzu goes first. The inner fortress must not know they are in it until they are at the tower.";
            m75.debrief = "The fortress armoury is full of Yorune steel, and every blade belonged to someone on the missing list. Past the inner gate, the prison tower is lit.";
            m75.dressing = new[] { DressingKind.MissingNotice, DressingKind.KagehiraBanners,
                DressingKind.AbandonedWeapons, DressingKind.PrisonerCamp };
            m75.challenge = MissionChallenge.NoAlarm; m75.challengeShards = 3;
            m75.stages = new[]
            {
                Scene("drain_open"),

                St(StageGoal.Reach, "INTO THE DRAIN", "WITH THE MELTWATER",
                    point: new Vector3(-10f, 0f, 8f), onComplete: StageEvent.WaterRises, checkpoint: true),

                St(StageGoal.Stealth, "THE DRAIN WATCH", "NOT A SOUND",
                    spawn: new[] { P, R }, checkpoint: true),

                Look("THE ARMOURY", "EVERY BLADE HAD AN OWNER", true,
                    Prop("racks", "RACKS OF YORUNE STEEL", new Vector3(-5f, 0f, 12f), StoryPropShape.Supply,
                        "Hundreds. Each one taken off somebody who didn't come home."),
                    Prop("list", "THE MISSING LIST, MATCHED", new Vector3(5f, 0f, 12f), StoryPropShape.CommandPost,
                        "A tag on every hilt, and every name is on the notices from the valley.")),

                Scene("drain_armoury"),

                St(StageGoal.Stealth, "THE INNER GATE", "TWO ELITES AND THEIR ARCHERS",
                    spawn: new[] { E, E, R }, onComplete: StageEvent.LightsOut, checkpoint: true),

                St(StageGoal.Reach, "ACROSS THE INNER YARD", "IN THE DARK",
                    point: new Vector3(10f, 0f, -12f)),

                Look("THE PRISON TOWER", "IT IS LIT", false,
                    Prop("tower", "A LIT WINDOW, AT THE TOP", new Vector3(12f, 0f, -15f), StoryPropShape.Lookout,
                        "One window lit, at the very top. Somebody's kept awake up there.")),

                Scene("drain_end"),
            };
            EditorUtility.SetDirty(m75);
        }
    }
}
