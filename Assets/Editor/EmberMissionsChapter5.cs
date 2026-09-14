using Emberline.Enemies;
using Emberline.Missions;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Chapter 5 — INTO THE MARSH, missions 41-45, built by hand. Back into the fog
    /// with what Goro said: the drowned road and its stolen lanterns, voices that
    /// move, a camp under the water line and the thing that watches it, Jin's
    /// hunters, and a patrol the marsh took. Water and fog are the enemy here;
    /// the events raise one or thicken the other mid-stage.
    /// </summary>
    public static partial class EmberMissions
    {
        private static void BuildChapter5()
        {
            // ---------------------------------------------------------------
            // 41 — THE DROWNED ROAD. Rain on the causeway; the tide comes in
            // twice while you are standing on it, and every cart has lost its
            // lantern and nothing else.
            var m41 = P_("S41_DrownedRoad");
            m41.id = 41; m41.missionName = "THE DROWNED ROAD"; m41.missionType = "SURVIVAL";
            m41.baseShards = 4; m41.applyTheme = true; m41.theme = Core.EnvThemeId.Graveyard;
            m41.briefing = "Under the marsh, Goro said. The causeway floods twice a night, and between the tides it is the only road in.";
            m41.debrief = "Every cart on the road was untouched except its lantern. A hundred lanterns gone, and voices in the fog on the far bank that are not soldiers.";
            m41.dressing = new[] { DressingKind.DestroyedCart, DressingKind.EmptyHome,
                DressingKind.AbandonedWeapons, DressingKind.BloodTrail };
            m41.challenge = MissionChallenge.UnderTime; m41.challengeShards = 3;
            m41.stages = new[]
            {
                Scene("road41_open"),

                St(StageGoal.Reach, "GET ON THE CAUSEWAY", "THE TIDE IS OUT",
                    point: new Vector3(-12f, 0f, 8f), onComplete: StageEvent.WaterRises, checkpoint: true),

                Look("THE CARTS", "NOTHING TAKEN BUT LIGHT", true,
                    Prop("hooks", "EMPTY LANTERN HOOKS", new Vector3(-8f, 0f, 12f), StoryPropShape.Supply,
                        "Every lantern gone. The rice is still here. The silver is still here. Only the light."),
                    Prop("feet", "BARE FOOTPRINTS", new Vector3(-3f, 0f, 14f), StoryPropShape.Tracks,
                        "Bare feet. Dozens. All walking into the water, none walking out.")),

                St(StageGoal.Wave, "THE ROAD IS HELD", "PIKES ON THE CROSSING",
                    spawn: new[] { P, P, R }, onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Wave, "OUT OF THE WATER", "THEY WERE UNDER IT",
                    spawn: new[] { A, B }),

                Scene("road41_voices"),

                St(StageGoal.Escape, "BEFORE THE SECOND TIDE", "IT IS COMING BACK",
                    duration: 50f, point: new Vector3(10f, 0f, -12f), spawn: new[] { S },
                    onComplete: StageEvent.WaterRises, checkpoint: true),

                St(StageGoal.Reach, "THE FAR BANK", "VOICES",
                    point: new Vector3(14f, 0f, -15f)),

                Scene("road41_end"),
            };
            EditorUtility.SetDirty(m41);

            // ---------------------------------------------------------------
            // 42 — VOICES IN THE FOG. The voices move: each clue is where a
            // voice was, not where it is. What answers them is not a soldier.
            var m42 = P_("S42_VoicesInTheFog");
            m42.id = 42; m42.missionName = "VOICES IN THE FOG"; m42.missionType = "INVESTIGATION";
            m42.baseShards = 4; m42.applyTheme = true; m42.theme = Core.EnvThemeId.Graveyard;
            m42.briefing = "Voices in the fog, calling from nowhere Renzo can see. Suzu says they move. Find where they come from.";
            m42.debrief = "The voices were prisoners, calling from a camp the marsh has half swallowed. The camp is under the water line, and so are its records.";
            m42.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.MissingNotice,
                DressingKind.BloodTrail, DressingKind.EmptyHome };
            m42.challenge = MissionChallenge.NoAlarm; m42.challengeShards = 3;
            m42.stages = new[]
            {
                Scene("fog_open"),

                St(StageGoal.Investigate, "FOLLOW THE FIRST VOICE", "WHERE IT WAS",
                    count: 2, checkpoint: true),

                St(StageGoal.Listen, "SOMETHING ANSWERS", "IT IS NOT A SOLDIER",
                    spawn: new[] { S, S }, onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Investigate, "FOLLOW THE SECOND VOICE", "IT MOVED",
                    count: 2),

                Scene("fog_camp"),

                St(StageGoal.Wave, "THE SHADES THAT ANSWER", "WHAT WAS CALLING BACK",
                    spawn: new[] { S, A }, checkpoint: true),

                Look("THE WATER LINE", "HALF SWALLOWED", false,
                    Prop("chain", "A PRISONER'S CHAIN", new Vector3(4f, 0f, 14f), StoryPropShape.Cache,
                        "Chained to a post the marsh took. They were calling from here. They aren't now."),
                    Prop("records", "THE CAMP RECORDS, UNDER WATER", new Vector3(-4f, 0f, 15f), StoryPropShape.CommandPost,
                        "The records are below the line. So are the answers.")),

                St(StageGoal.Reach, "TO THE CAMP'S EDGE", "UNDER THE LINE",
                    point: new Vector3(0f, 0f, -14f)),

                Scene("fog_end"),
            };
            EditorUtility.SetDirty(m42);

            // ---------------------------------------------------------------
            // 43 — THE SUNKEN CAMP. Half the camp is under water; the search is
            // in the shallows, and the deep is not safe. Daigo says his debt.
            var m43 = P_("S43_SunkenCamp");
            m43.id = 43; m43.missionName = "THE SUNKEN CAMP"; m43.missionType = "INVESTIGATION";
            m43.baseShards = 5; m43.applyTheme = true; m43.theme = Core.EnvThemeId.Graveyard;
            m43.briefing = "The camp's records are under the water line. Search the shallows. Daigo says stay out of the deep, and he does not say why.";
            m43.debrief = "The camp's last commander wrote that the 'daughter' was moved to the temple 'for the key.' Aiko is not a prisoner. She is a lock. And something in the deep water watched the whole search.";
            m43.dressing = new[] { DressingKind.PrisonerCamp, DressingKind.DestroyedCart,
                DressingKind.KagehiraBanners, DressingKind.BloodTrail };
            m43.challenge = MissionChallenge.UnderTime; m43.challengeShards = 3;
            m43.stages = new[]
            {
                Scene("sunken_open"),

                Split("INTO THE CAMP", "THE SHALLOWS, OR THE PALISADE",
                    new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
                    new[] { S }, new[] { A }),

                St(StageGoal.Investigate, "SEARCH THE SHALLOWS", "STAY OUT OF THE DEEP",
                    count: 3, checkpoint: true),

                Scene("sunken_daigo"),

                St(StageGoal.Wave, "POWDER ON THE WATER", "THE CAMP'S LAST GUARD",
                    spawn: new[] { O, A }, onComplete: StageEvent.WaterRises, checkpoint: true),

                Look("THE COMMANDER'S LAST ENTRY", "FOR THE KEY", true,
                    Prop("journal", "THE COMMANDER'S JOURNAL", new Vector3(-6f, 0f, 14f), StoryPropShape.CommandPost,
                        "\"The daughter moved to the temple. For the key.\" Not a prisoner. A lock."),
                    Prop("lantern", "A LANTERN, LIT, UNDER WATER", new Vector3(5f, 0f, 16f), StoryPropShape.Lookout,
                        "Still burning under the water. Something down there is keeping them lit.")),

                Scene("sunken_deep"),

                St(StageGoal.Survive, "THE DEEP WAKES", "GET OUT OF THE WATER",
                    duration: 35f, spawn: new[] { S, S, S }, checkpoint: true),

                St(StageGoal.Reach, "THE HIGH GROUND", "OUT OF ITS REACH",
                    point: new Vector3(0f, 0f, -15f)),

                Scene("sunken_end"),
            };
            EditorUtility.SetDirty(m43);

            // ---------------------------------------------------------------
            // 44 — MARSH HUNTERS. Assassins in fog: the enemy is closer than
            // you can see, and so are you. They were not sent by Goro.
            var m44 = P_("S44_MarshHunters");
            m44.id = 44; m44.missionName = "MARSH HUNTERS"; m44.missionType = "SURVIVAL";
            m44.baseShards = 5; m44.applyTheme = true; m44.theme = Core.EnvThemeId.Graveyard;
            m44.briefing = "Somebody followed Renzo into the fog. Tsuru counted six sets of prints and then stopped counting.";
            m44.debrief = "The assassins were sent by Jin Kurogane, not by Goro. A Kurogane crest on the last body. Renzo has heard the name. He does not know why it stings.";
            m44.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice, DressingKind.DestroyedCart };
            m44.challenge = MissionChallenge.SilentKill; m44.challengeShards = 3;
            m44.stages = new[]
            {
                Scene("hunters_open"),

                St(StageGoal.Reach, "INTO THE FOG", "THEY ARE ALREADY HERE",
                    point: new Vector3(-10f, 0f, 10f), onComplete: StageEvent.Ambush, checkpoint: true),

                St(StageGoal.Survive, "THE FOG IS THEIRS", "CLOSER THAN YOU CAN SEE",
                    duration: 40f, spawn: new[] { A, A }, checkpoint: true),

                Look("A HUNTER'S BODY", "NOT GORO'S", true,
                    Prop("crest", "AN ASSASSIN'S CREST", new Vector3(-6f, 0f, 13f), StoryPropShape.Body,
                        "Not the serpent. A crest I don't know. It stings, and I don't know why."),
                    Prop("east", "THEY CAME FROM THE EAST", new Vector3(-2f, 0f, 15f), StoryPropShape.Tracks,
                        "From the east. Not from Goro's valley. Somebody else is paying for me.")),

                Scene("hunters_tsuru"),

                St(StageGoal.Stealth, "TURN THE HUNT", "HUNT THE HUNTERS",
                    spawn: new[] { A, N }, onComplete: StageEvent.AlarmTriggered),

                St(StageGoal.Wave, "WHEN THE FOG THINS", "BOTH SIDES CAN SEE",
                    spawn: new[] { A, R, N }, checkpoint: true),

                Look("THE LAST BODY", "KUROGANE", false,
                    Prop("kurogane", "A KUROGANE CREST", new Vector3(3f, 0f, 16f), StoryPropShape.Keepsake,
                        "Kurogane. Father said that name once, at the door, and then never again.")),

                Scene("hunters_end"),
            };
            EditorUtility.SetDirty(m44);

            // ---------------------------------------------------------------
            // 45 — THE MISSING PATROL. Even the enemy is afraid of the marsh.
            // The clues are bodies, and what killed them is still here.
            var m45 = P_("S45_MissingPatrol");
            m45.id = 45; m45.missionName = "THE MISSING PATROL"; m45.missionType = "EXPLORATION";
            m45.baseShards = 4; m45.applyTheme = true; m45.theme = Core.EnvThemeId.Graveyard;
            m45.briefing = "An enemy patrol went into the fog ahead of Renzo and did not come out. Whatever stopped them is between him and the temple.";
            m45.debrief = "The patrol was killed by the marsh's own guardians, shades that answer to no warlord. Past them, reed smoke on the wind. Somebody lives out here.";
            m45.dressing = new[] { DressingKind.BloodTrail, DressingKind.AbandonedWeapons,
                DressingKind.MissingNotice, DressingKind.EmptyHome };
            m45.challenge = MissionChallenge.UnderTime; m45.challengeShards = 3;
            m45.stages = new[]
            {
                Scene("patrol_open"),

                St(StageGoal.Investigate, "FOLLOW THE PATROL'S TRAIL", "THEY WENT IN",
                    count: 2, checkpoint: true),

                Look("THE FIRST BODIES", "WHAT KILLED THEM", true,
                    Prop("pikeman", "A PIKEMAN, DROWNED STANDING", new Vector3(-8f, 0f, 12f), StoryPropShape.Body,
                        "Drowned on his feet, on dry ground. The water didn't do that. Something that lives in it did."),
                    Prop("lantern", "A DROPPED LANTERN", new Vector3(-4f, 0f, 15f), StoryPropShape.Supply,
                        "They came for lanterns too. The marsh took theirs instead.")),

                St(StageGoal.Listen, "IT IS STILL HERE", "WHAT KILLED THEM",
                    spawn: new[] { S, S }, onComplete: StageEvent.FogRolls, checkpoint: true),

                St(StageGoal.Investigate, "THE LAST OF THEM", "DEEPER IN",
                    count: 2),

                Scene("patrol_last"),

                St(StageGoal.Wave, "THE MARSH'S OWN", "THEY ANSWER TO NO WARLORD",
                    spawn: new[] { S, S, S }, checkpoint: true),

                Look("REED SMOKE", "SOMEBODY LIVES HERE", false,
                    Prop("smoke", "SMOKE OVER THE REEDS", new Vector3(6f, 0f, 14f), StoryPropShape.Lookout,
                        "Reed smoke. Somebody lives out here, and they aren't afraid of what just killed a patrol.")),

                St(StageGoal.Reach, "TOWARD THE SMOKE", "SUZU'S WAY",
                    point: new Vector3(12f, 0f, -12f)),

                Scene("patrol_end"),
            };
            EditorUtility.SetDirty(m45);
        }
    }
}
