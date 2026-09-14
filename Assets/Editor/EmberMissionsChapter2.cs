using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 2 — THE LANTERN NETWORK, missions 11-15, built by hand.
    ///
    /// <para>
    /// These five used to come out of the per-type template, which gave them the
    /// right goals in the wrong words: "FIND WHAT BURNS", "READ THE GROUND", no
    /// scenes, nothing to pick up, and the two companions the chapter introduces
    /// never on screen. Each is now staged around its own set piece — the fire
    /// that takes time, the guard standing on the crate, the survivor who will
    /// not leave her ledgers — with its discoveries carried by things Renzo finds
    /// and its turns carried by scenes.
    /// </para>
    /// </summary>
    public static partial class EmberMissions
    {
        private static StoryPropSpec Prop(string id, string label, Vector3 at, StoryPropShape shape,
            string line, string speaker = "RENZO") => new()
        {
            id = id, label = label, point = at, shape = shape, speaker = speaker, line = line,
        };

        private static MissionStage Look(string objective, string banner, bool checkpoint,
            params StoryPropSpec[] props) => new()
        {
            goal = StageGoal.Examine, objective = objective, banner = banner,
            checkpoint = checkpoint, props = props,
        };

        private static MissionStage Scene(string beatId) => St(StageGoal.Cinematic, "", "", beatId: beatId);

        private static void BuildChapter2()
        {
            // ---------------------------------------------------------------
            // 11 — THE SUPPLY ROUTE. Kagehira is building an army, and the
            // player learns it from the size of the wagons before anyone says so.
            // The set piece is the fire: it takes time, and the yard comes back.
            var m11 = P_("S11_SupplyRoute");
            m11.id = 11; m11.missionName = "THE SUPPLY ROUTE"; m11.missionType = "SABOTAGE";
            m11.baseShards = 4; m11.applyTheme = true; m11.theme = Core.EnvThemeId.Village;
            m11.briefing = "Wagon ruts cut deep into the old village road, more of them than a raiding party needs. Follow them to the depot and burn what they carry.";
            m11.debrief = "The supply wagon burned with its escort watching. Three soldiers ran, and Renzo let them: running men go home.";
            m11.dressing = new[] { DressingKind.DestroyedCart, DressingKind.KagehiraBanners, DressingKind.AbandonedWeapons };
            m11.challenge = MissionChallenge.NoAlarm; m11.challengeShards = 3;
            m11.stages = new[]
            {
                Scene("supply_open"),

                Look("READ THE ROAD", "WAGON RUTS", true,
                    Prop("ruts", "DEEP WAGON RUTS", new Vector3(-6f, 0f, 12f), StoryPropShape.Tracks,
                        "Axle-deep. That's iron in those wagons, not grain."),
                    Prop("tally", "A QUARTERMASTER'S TALLY", new Vector3(4f, 0f, 15f), StoryPropShape.Supply,
                        "Rice for three hundred men. This isn't a raiding party.")),

                // The design offers two doors: the oil store to the east, the
                // culvert to the west.
                Split("REACH THE DEPOT", "BURN IT, OR GO IN QUIET",
                    new Vector3(12f, 0f, 10f), new Vector3(-12f, 0f, 10f),
                    new[] { B, B }, new[] { R }),

                St(StageGoal.Stealth, "CROSS THE WAGON YARD UNSEEN", "THE YARD IS AWAKE",
                    spawn: new[] { P, B, R }, checkpoint: true),

                Look("FIND WHAT BURNS", "THE DEPOT", true,
                    Prop("oil", "JARS OF LAMP OIL", new Vector3(13f, 0f, 6f), StoryPropShape.Supply,
                        "Lamp oil, enough to light a fortress. Or burn one."),
                    Prop("manifest", "A LOADING MANIFEST", new Vector3(9f, 0f, 1f), StoryPropShape.CommandPost,
                        "Spears, rope, rice. Destination: the Broken Banner. He's arming a camp.")),

                // The fire takes time, and the escort comes back while it catches.
                St(StageGoal.Defend, "SET THE FIRE", "THE FIRE TAKES TIME",
                    duration: 30f, point: new Vector3(11f, 0f, 4f), spawn: new[] { B, B, P },
                    onComplete: StageEvent.Reinforcements, checkpoint: true),

                Scene("supply_burn"),

                St(StageGoal.Reach, "FOLLOW THE RUNNERS", "RUNNING MEN GO HOME",
                    point: new Vector3(0f, 0f, -14f)),
            };
            EditorUtility.SetDirty(m11);

            // ---------------------------------------------------------------
            // 12 — SILENT CARGO. A night shipment, every guard takeable unseen,
            // and a thief already on the wagon: Suzu, stealing from the same
            // convoy, badly. The climax is the last guard awake on the crate.
            var m12 = P_("S12_SilentCargo");
            m12.id = 12; m12.missionName = "SILENT CARGO"; m12.missionType = "STEALTH";
            m12.baseShards = 4; m12.nightOverride = true; m12.applyTheme = true; m12.theme = Core.EnvThemeId.Village;
            m12.briefing = "A shipment moves tonight with no lanterns and a full escort. Whatever it is, they don't want it seen. Neither should you be.";
            m12.debrief = "Weapons from four provinces, and a bill of lading naming a supplier in Kiba. Kagehira is buying from everyone, and now a scout named Suzu knows it too.";
            m12.dressing = new[] { DressingKind.DestroyedCart, DressingKind.KagehiraBanners, DressingKind.MissingNotice };
            m12.challenge = MissionChallenge.SilentKill; m12.challengeShards = 3;
            m12.stages = new[]
            {
                Scene("cargo_open"),

                St(StageGoal.Reach, "SHADOW THE CONVOY", "NO LANTERNS",
                    point: new Vector3(-10f, 0f, 12f), checkpoint: true),

                St(StageGoal.Stealth, "THIN THE OUTER ESCORT UNSEEN", "EVERY ALARM COSTS YOU",
                    spawn: new[] { A, B }, checkpoint: true),

                Scene("cargo_suzu"),

                Split("REACH THE CARGO WAGONS", "HER WAY, OR THROUGH THE ESCORT",
                    new Vector3(9f, 0f, 14f), new Vector3(-9f, 0f, 14f),
                    new[] { A }, new[] { R, B }),

                Look("OPEN THE CARGO", "WHAT THEY'RE HIDING", true,
                    Prop("stamps", "FOUR PROVINCE STAMPS", new Vector3(3f, 0f, 16f), StoryPropShape.Supply,
                        "Four crates, four provinces. He's buying from everyone."),
                    Prop("lading", "A BILL OF LADING", new Vector3(-3f, 0f, 17f), StoryPropShape.CommandPost,
                        "Supplier: Kiba. A village I've never heard of, selling him steel by the wagon.")),

                // The last guard is awake, armed, and standing on the crate.
                St(StageGoal.Assassinate, "THE GUARD ON THE CRATE", "HE IS AWAKE",
                    spawn: new[] { A }, onComplete: StageEvent.AlarmTriggered),

                St(StageGoal.Escape, "GONE BEFORE THE ESCORT WAKES", "THE ROAD BEHIND YOU",
                    duration: 55f, point: new Vector3(0f, 0f, -15f), spawn: new[] { R, A }, checkpoint: true),

                Scene("cargo_end"),
            };
            EditorUtility.SetDirty(m12);

            // ---------------------------------------------------------------
            // 13 — THE BROKEN VILLAGE. Ashfall burned for the same reason as
            // Yorune, and the ruin says so before anybody does. The Scavenger
            // King keeps the ruin now, paid to keep people out of it.
            var m13 = P_("S13_BrokenVillage");
            m13.id = 13; m13.missionName = "THE BROKEN VILLAGE"; m13.missionType = "INVESTIGATION";
            m13.baseShards = 5; m13.applyTheme = true; m13.theme = Core.EnvThemeId.BurningVillage;
            m13.briefing = "The bill of lading led past a village that isn't on the map any more. Ashfall burned. Find out why.";
            m13.debrief = "Ashfall was searching for the Seal too, and burned for it. Under the elder's floor: a map older than Renzo's, with the marsh temple marked. And in the cellar, someone breathing.";
            m13.dressing = new[] { DressingKind.BurnedHome, DressingKind.EmptyHome, DressingKind.MissingNotice, DressingKind.BloodTrail };
            m13.challenge = MissionChallenge.UnderTime; m13.challengeShards = 3;
            m13.stages = new[]
            {
                Scene("ashfall_open"),

                // The village tells it without a line of dialogue.
                Look("READ WHAT HAPPENED", "ASHFALL", true,
                    Prop("notice", "A NOTICE ON THE GATE", new Vector3(-8f, 0f, 10f), StoryPropShape.StoneMarker,
                        "\"Surrender the elder's records.\" Nobody did."),
                    Prop("cart", "A SEARCHED CART", new Vector3(2f, 0f, 14f), StoryPropShape.Supply,
                        "Every sack cut open. They weren't raided. They were searched."),
                    Prop("shrine", "THE SHRINE, DUG UP", new Vector3(10f, 0f, 9f), StoryPropShape.Shrine,
                        "They dug under the shrine. Same as they would have at home.")),

                St(StageGoal.Wave, "DRIVE OUT THE SCAVENGERS", "THIS RUIN HAS OWNERS",
                    spawn: new[] { B, B, H }, checkpoint: true),

                Look("SEARCH THE ELDER'S HOUSE", "THE FLOOR PULLED UP", true,
                    Prop("floor", "PULLED FLOORBOARDS", new Vector3(-12f, 0f, 16f), StoryPropShape.Homestead,
                        "Whoever pulled these up missed the cellar door."),
                    Prop("oldmap", "AN OLDER MAP", new Vector3(-9f, 0f, 19f), StoryPropShape.Cache,
                        "Older than mine. The marsh temple, marked in the same hand as Father's.")),

                Scene("ashfall_king"),

                // He was paid to keep people out; when he falls, his men stop being paid.
                St(StageGoal.Duel, "THE SCAVENGER KING", "WHAT THE VILLAGE LEFT",
                    foeDef: "raiderleader", spawn: new[] { H }, onComplete: StageEvent.Mutiny, checkpoint: true),

                Look("SEARCH HIM", "SERPENT COIN", false,
                    Prop("purse", "A HEAVY PURSE", Vector3.zero, StoryPropShape.Body,
                        "Serpent-stamped coin. Kagehira paid a scavenger to keep people out of a ruin.")),

                Scene("ashfall_end"),
            };
            EditorUtility.SetDirty(m13);

            // ---------------------------------------------------------------
            // 14 — THE SURVIVOR. Fumi was in the cellar copying the ledger when
            // the village burned, and she will not leave without it. The fight
            // moves with her; the elite comes to finish the village.
            var m14 = P_("S14_Survivor");
            m14.id = 14; m14.missionName = "THE SURVIVOR"; m14.missionType = "ESCORT";
            m14.baseShards = 4; m14.applyTheme = true; m14.theme = Core.EnvThemeId.Village;
            m14.briefing = "Someone has been living under Ashfall's elder's house since the fire. They remember a girl with a red thread on her wrist. Get them out alive.";
            m14.debrief = "Fumi remembers Aiko: alive after the fire, and not alone. She knows a road that doesn't appear on any map, and she has decided Renzo is worth reading.";
            m14.dressing = new[] { DressingKind.HidingVillagers, DressingKind.BurnedHome, DressingKind.KagehiraBanners };
            m14.challenge = MissionChallenge.NoCivilianDeaths; m14.challengeShards = 3;
            m14.stages = new[]
            {
                Scene("survivor_cellar"),

                Look("GATHER WHAT SHE WON'T LEAVE", "THE CELLAR", true,
                    Prop("copies", "COPIED LEDGERS", new Vector3(-4f, 0f, 8f), StoryPropShape.CommandPost,
                        "Every page, copied by hand. Names, dates, where each family was sent.", "FUMI"),
                    Prop("bracelet", "A RED THREAD, PINNED TO A PAGE", new Vector3(3f, 0f, 9f), StoryPropShape.Keepsake,
                        "She pinned it next to Aiko's name. Six years ago.")),

                St(StageGoal.Reach, "GET HER TO THE ROAD", "UP AND OUT",
                    point: new Vector3(0f, 0f, -10f), checkpoint: true),

                // Waves from every side while she walks; the fight moves with her.
                St(StageGoal.Escort, "WALK FUMI OUT", "THEY'RE COMING FROM EVERY SIDE",
                    spawn: new[] { B, P }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Defend, "HOLD THE BRIDGE WHILE SHE CROSSES", "THE BRIDGE",
                    duration: 35f, point: new Vector3(8f, 0f, -16f), spawn: new[] { R, B }),

                Scene("survivor_elite"),

                St(StageGoal.Wave, "STAND BETWEEN THEM AND HER", "SENT TO FINISH THE VILLAGE",
                    spawn: new[] { E, P }, checkpoint: true),

                Scene("survivor_end"),
            };
            EditorUtility.SetDirty(m14);

            // ---------------------------------------------------------------
            // 15 — HIDDEN ROAD. Traversal under watch: the archers are on the
            // ridge, not the road, so the treeline is the level. Rain is cover.
            var m15 = P_("S15_HiddenRoad");
            m15.id = 15; m15.missionName = "HIDDEN ROAD"; m15.missionType = "STEALTH";
            m15.baseShards = 4; m15.applyTheme = true; m15.theme = Core.EnvThemeId.Forest; m15.rain = true;
            m15.briefing = "Fumi's road runs through the forest under a ridge full of archers. It's watched, which is how you know it matters.";
            m15.debrief = "The hidden road ends at a watchtower that sees the whole valley. The tower is lit, and someone is standing on top of it.";
            m15.dressing = new[] { DressingKind.AbandonedWeapons, DressingKind.BloodTrail, DressingKind.KagehiraBanners };
            m15.challenge = MissionChallenge.NoAlarm; m15.challengeShards = 3;
            m15.stages = new[]
            {
                Scene("road_open"),

                St(StageGoal.Reach, "LEAVE THE ROAD FOR THE TREELINE", "THE RAIN COVERS YOU",
                    point: new Vector3(-12f, 0f, 8f), onComplete: StageEvent.RainStarts, checkpoint: true),

                St(StageGoal.Stealth, "PASS UNDER THE ARCHERS", "THEY WATCH THE ROAD",
                    spawn: new[] { R, R }, checkpoint: true),

                Look("READ THE HIDDEN ROAD", "NOT ON ANY MAP", true,
                    Prop("wheels", "NARROW WHEEL TRACKS", new Vector3(-6f, 0f, 16f), StoryPropShape.Tracks,
                        "Narrow wheels, no lantern soot. They move at night."),
                    Prop("waystone", "A WAYSTONE", new Vector3(4f, 0f, 18f), StoryPropShape.StoneMarker,
                        "Goro's toll mark, scratched out. Someone new owns this road.")),

                St(StageGoal.Stealth, "SLIP THE CHECKPOINT", "ONE LAMP, TWO GUARDS",
                    spawn: new[] { A, R }, checkpoint: true),

                St(StageGoal.Reach, "INTO THE TOWER'S SHADOW", "WITHOUT A SHOT FIRED",
                    point: new Vector3(0f, 0f, 20f)),

                Scene("road_tower"),
            };
            EditorUtility.SetDirty(m15);
        }
    }
}
