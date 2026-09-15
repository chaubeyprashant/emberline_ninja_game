using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 10 — THE SERPENT'S END, missions 91-95, built by hand. The companions
    /// are taken away one mission at a time, on purpose: the fortress burns behind
    /// them, the last army breaks, the summit road stops everyone but Renzo, Toku
    /// hands over his last blade at the last guard post, and Kagehira takes Aiko
    /// out of the fog.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter10()
        {
            // ---------------------------------------------------------------
            // 91 — THE BURNING FORTRESS. Night. Kagehira burned the bridge behind
            // him; the exits close in the order the fire chooses.
            var m91 = P_("S91_BurningFortress");
            m91.id = 91; m91.missionName = "THE BURNING FORTRESS"; m91.missionType = "CHASE";
            m91.baseShards = 6; m91.applyTheme = true; m91.theme = Core.EnvThemeId.BurningVillage;
            m91.briefing = "Kagehira went up the summit stair and set the fortress burning behind him. Aiko has a cut hand and no patience. Daigo and Tsuru have the way out, if the fire leaves one.";
            m91.debrief = "Kagehira set the fire himself, burning the bridge behind him. The last gate fell a breath after they were through it. The summit road is ahead, and his rear guard is on it.";
            m91.dressing = new[] { DressingKind.BurnedHome, DressingKind.KagehiraBanners,
                DressingKind.BloodTrail, DressingKind.AbandonedWeapons };
            m91.challenge = MissionChallenge.UnderTime; m91.challengeShards = 4;
            m91.stages = new[]
            {
                Scene("burnfort_open"),

                St(StageGoal.Escape, "OUT OF THE CHAMBER WING", "THE FIRE CHOOSES THE EXITS",
                    duration: 40f, point: new Vector3(-10f, 0f, 10f), spawn: new[] { S },
                    onComplete: StageEvent.Collapse, checkpoint: true),

                Look("HE SET IT HIMSELF", "THE BRIDGE BEHIND HIM", true,
                    Prop("oil", "OIL JARS, EMPTIED ON THE STAIR", new Vector3(-6f, 0f, 12f), StoryPropShape.Supply,
                        "Poured on his own stair. He isn't coming back down, and nobody is following."),
                    Prop("order", "HIS ORDER: \"BURN IT BEHIND ME\"", new Vector3(5f, 0f, 12f), StoryPropShape.CommandPost,
                        "His hand again. His own fortress, his own men inside it.")),

                St(StageGoal.Chase, "THE FIRE-SETTERS", "THEY ARE LIGHTING THE NEXT WING",
                    duration: 35f, spawn: new[] { A, R }, checkpoint: true),

                Scene("burnfort_daigo"),

                St(StageGoal.Survive, "THE COURTYARD BURNS", "HOLD ON UNTIL THE ROOF FALLS",
                    duration: 30f, spawn: new[] { E, S }, onComplete: StageEvent.Collapse, checkpoint: true),

                St(StageGoal.Escape, "THE LAST GATE", "THE FIRE IS FASTER",
                    duration: 35f, point: new Vector3(10f, 0f, -12f), spawn: new[] { A }),

                St(StageGoal.Reach, "THE SUMMIT ROAD", "OUT",
                    point: new Vector3(12f, 0f, -15f)),

                Scene("burnfort_end"),
            };
            EditorUtility.SetDirty(m91);

            // ---------------------------------------------------------------
            // 92 — THE LAST ARMY. Snow. The largest sustained fight in the game.
            // Tsuru puts names on the shafts; the army fights for pay, and Oba's
            // mutineers break the line from behind.
            var m92 = P_("S92_LastArmy");
            m92.id = 92; m92.missionName = "THE LAST ARMY"; m92.missionType = "COMBAT";
            m92.baseShards = 6; m92.applyTheme = true; m92.theme = Core.EnvThemeId.Mountain;
            m92.briefing = "The last of Kagehira's army holds the summit road. Suzu takes the flank, Daigo the line, and Tsuru is writing names on his arrows. Oba's mutineers are somewhere behind the enemy.";
            m92.debrief = "The army fought for pay and the mutineers fought for Aiko, and it showed. The last officer fell with Oba's men breaking the line behind him. The road is open; the summit is a day's climb.";
            m92.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.DestroyedCart,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m92.challenge = MissionChallenge.UnderTime; m92.challengeShards = 4;
            m92.stages = new[]
            {
                Scene("lastarmy_open"),

                Split("ONTO THE ROAD", "SUZU'S FLANK, OR DAIGO'S LINE",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { A }, new[] { P }),

                St(StageGoal.Wave, "THE ARMY FIGHTS FOR PAY", "THE WHOLE ROAD",
                    spawn: new[] { P, P, R }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Look("WHAT THEY FIGHT FOR", "PAY, AND NAMES", true,
                    Prop("chests", "PAY CHESTS ON A SLED", new Vector3(-6f, 0f, 12f), StoryPropShape.Supply,
                        "Silver, counted out per head. He's paying them to die on his road."),
                    Prop("arrows", "TSURU'S ARROWS, NAMED", new Vector3(5f, 0f, 12f), StoryPropShape.Keepsake,
                        "A name on every shaft. Everyone on the missing lists. He's sending them home one at a time.")),

                Scene("lastarmy_tsuru"),

                St(StageGoal.Defend, "HOLD THE BEND", "THEY COME UP THE WHOLE ROAD",
                    duration: 40f, point: new Vector3(0f, 0f, 8f), spawn: new[] { H, H, R, A },
                    checkpoint: true),

                St(StageGoal.Wave, "THE LAST OFFICER", "AND BEHIND HIM, THE MUTINEERS",
                    spawn: new[] { E, P }, onComplete: StageEvent.Mutiny, checkpoint: true),

                Scene("lastarmy_oba"),

                St(StageGoal.Reach, "THE ROAD IS OPEN", "A DAY'S CLIMB",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("lastarmy_end"),
            };
            EditorUtility.SetDirty(m92);

            // ---------------------------------------------------------------
            // 93 — THE SUMMIT ROAD. Snow and fog. Thin air and a blizzard. The
            // companions stop here, each for their own reason and each said out
            // loud; Toku goes one post further.
            var m93 = P_("S93_SummitRoad");
            m93.id = 93; m93.missionName = "THE SUMMIT ROAD"; m93.missionType = "EXPLORATION";
            m93.baseShards = 6; m93.applyTheme = true; m93.theme = Core.EnvThemeId.Mountain;
            m93.briefing = "The summit road is thin air and a blizzard. Tsuru has the last watch of the climb. After this, everyone has a reason to stop, and Renzo has already guessed what each one is.";
            m93.debrief = "Kagehira's guard died on this road; he climbed on alone. The companions stopped, each for a reason said out loud. At the summit gate stand Kagehira's strongest, the last wall.";
            m93.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail,
                DressingKind.MissingNotice };
            m93.challenge = MissionChallenge.UnderTime; m93.challengeShards = 3;
            m93.stages = new[]
            {
                Scene("summit_open"),

                St(StageGoal.Reach, "THE THIN AIR", "THE BLIZZARD CLOSES",
                    point: new Vector3(-8f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                Look("HIS GUARD DIED HERE", "HE CLIMBED ON ALONE", true,
                    Prop("sitting", "A GUARDSMAN, SITTING IN THE SNOW", new Vector3(-5f, 0f, 12f), StoryPropShape.Body,
                        "Sat down to rest and didn't get up. His master didn't stop."),
                    Prop("prints", "ONE SET OF PRINTS, GOING ON", new Vector3(4f, 0f, 14f), StoryPropShape.Tracks,
                        "One man, still walking. Dragging someone? No. Just walking. He left them all.")),

                St(StageGoal.Investigate, "THE ONES WHO COULD NOT KEEP UP", "FOLLOW THE ROAD",
                    count: 2),

                St(StageGoal.Listen, "SOMETHING IN THE BLIZZARD", "IT KEPT UP",
                    spawn: new[] { S, S }, checkpoint: true),

                Scene("summit_toku"),

                St(StageGoal.Wave, "THE LAST OF THE GUARD", "TURNING BACK TO HOLD IT",
                    spawn: new[] { A, R }),

                St(StageGoal.Reach, "THE SUMMIT GATE", "THE LAST WALL",
                    point: new Vector3(10f, 0f, -12f), checkpoint: true),

                Scene("summit_end"),
            };
            EditorUtility.SetDirty(m93);

            // ---------------------------------------------------------------
            // 94 — THE FINAL GUARD. Snow. Toku's last honest blade, Yorune steel
            // taken back, handed over at the last guard post. Every named foe's
            // tactics in one unit, and the last of the nine Iron Guard.
            var m94 = P_("S94_FinalGuard");
            m94.id = 94; m94.missionName = "THE FINAL GUARD"; m94.missionType = "BOSS";
            m94.baseShards = 6; m94.applyTheme = true; m94.theme = Core.EnvThemeId.Fortress;
            m94.briefing = "The summit gate and Kagehira's strongest in front of it, told to make sure Renzo does not arrive whole. Toku walked one post further than everyone else, carrying something wrapped in cloth.";
            m94.debrief = "The final guard read, punished, protected and retreated, and the last of the nine Iron Guard fell back to back with his last man. Beyond the open gate is the summit, and nothing on it yet.";
            m94.dressing = new[] { DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons,
                DressingKind.BloodTrail };
            m94.challenge = MissionChallenge.UnderTime; m94.challengeShards = 4;
            m94.stages = new[]
            {
                Scene("finalguard_open"),

                St(StageGoal.Reach, "THE LAST GUARD POST", "WHERE TOKU TURNS AROUND",
                    point: new Vector3(0f, 0f, 8f), checkpoint: true),

                Look("TOKU'S LAST BLADE", "NOT THEIRS", true,
                    Prop("blade", "YORUNE STEEL, TAKEN BACK", new Vector3(-5f, 0f, 11f), StoryPropShape.KeyPiece,
                        "Forged from the armour we took off the Iron Guard. The first honest thing that steel has been."),
                    Prop("post", "WHERE HE TURNED AROUND", new Vector3(5f, 0f, 11f), StoryPropShape.StoneMarker,
                        "His footprints stop here, and go back down. He said a smith's work ends at the handle.")),

                St(StageGoal.Wave, "EVERY TACTIC YOU HAVE MET", "THEY READ, THEY PUNISH",
                    spawn: new[] { N, M, E }, onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("finalguard_captain"),

                new MissionStage
                {
                    goal = StageGoal.BossPhase,
                    objective = "THE LAST OF THE NINE",
                    banner = "KAGEHIRA'S SHIELD",
                    foeDef = "ironguard",
                    spawn = new[] { E },
                    bossHealthGate = 0.5f,
                    onComplete = StageEvent.LightsOut,
                    checkpoint = true,
                },

                St(StageGoal.Wave, "THEY PROTECT HIM", "THEY RETREAT BEHIND HIM",
                    spawn: new[] { M, N }),

                St(StageGoal.BossFight, "BREAK THE SHIELD", "THE LAST OF NINE", checkpoint: true),

                St(StageGoal.Wave, "THE LAST TWO", "BACK TO BACK, IN THE SNOW",
                    spawn: new[] { E, M }),

                Look("THE GATE, OPEN", "THE SUMMIT", false,
                    Prop("summit", "THE SUMMIT, AND NOTHING ON IT", new Vector3(0f, 0f, -12f), StoryPropShape.Lookout,
                        "Fog to the top. Nothing moving. He's up there, and he's been watching me climb.")),

                Scene("finalguard_end"),
            };
            EditorUtility.SetDirty(m94);

            // ---------------------------------------------------------------
            // 95 — THE SERPENT'S SHADOW. Fog, night. Kagehira as an ambusher: out
            // of the fog and gone again. He does not want Renzo dead yet. He
            // wants him alone, and takes Aiko to make it so.
            var m95 = P_("S95_SerpentsShadow");
            m95.id = 95; m95.missionName = "THE SERPENT'S SHADOW"; m95.missionType = "ENDURE";
            m95.baseShards = 6; m95.applyTheme = true; m95.theme = Core.EnvThemeId.Mountain;
            m95.briefing = "Aiko came through the gate after him, because she will not wait. The summit is fog, and something in it is faster than the fog.";
            m95.debrief = "Kagehira came out of the fog, put his blade to Renzo's back, and did not cut. 'Alone,' he said. 'Come alone.' When the fog cleared, Aiko was gone.";
            m95.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.KagehiraBanners };
            m95.challenge = MissionChallenge.UnderTime; m95.challengeShards = 4;
            m95.stages = new[]
            {
                Scene("shadow_open"),

                St(StageGoal.Reach, "INTO THE FOG", "THE SUMMIT IS HIS",
                    point: new Vector3(0f, 0f, 10f), onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Listen, "SOMETHING CIRCLES", "FASTER THAN THE FOG",
                    spawn: new[] { S, S }, checkpoint: true),

                Scene("shadow_strike"),

                new MissionStage
                {
                    goal = StageGoal.Endure,
                    objective = "SURVIVE THE SERPENT",
                    banner = "OUT OF THE FOG",
                    duration = 45f,
                    foeDef = "kagachi",
                    spawn = new[] { K },
                    onComplete = StageEvent.FoeWithdraws,
                    checkpoint = true,
                },

                Look("WHERE SHE STOOD", "GONE", false,
                    Prop("thread", "HER THREAD, CUT", new Vector3(-4f, 0f, 12f), StoryPropShape.Keepsake,
                        "Cut clean. Not broken. He wanted me to find it."),
                    Prop("marks", "DRAG MARKS, UPWARD", new Vector3(4f, 0f, 13f), StoryPropShape.Tracks,
                        "She fought him every step. Up. He took her up.")),

                Scene("shadow_alone"),

                St(StageGoal.Wave, "WHAT HE LEFT IN THE FOG", "TO MAKE SURE YOU COME ALONE",
                    spawn: new[] { A, S }),

                St(StageGoal.Reach, "ALONE", "AS HE SAID",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("shadow_end"),
            };
            EditorUtility.SetDirty(m95);
        }
    }
}
