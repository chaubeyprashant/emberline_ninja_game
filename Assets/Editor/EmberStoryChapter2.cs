using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 2, missions 11-15. Short and in character: Renzo says
    /// less than he thinks, Suzu says more than she should, Fumi corrects both.
    /// Each scene names who is in it, so CastStandIn can stand them in front of
    /// Renzo and the camera can find whoever is speaking.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter2Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 11 THE SUPPLY ROUTE
            Make("supply_open", "WAGON RUTS",
                S("RENZO", ShotCamera.OverShoulder, 3.6f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Wagon ruts. Heavy ones."),
                S("RENZO", ShotCamera.PushIn, 3.4f, "RENZO", "Too many for a raiding party."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // He lets them go on purpose. The line is the mission's hinge.
            Make("supply_burn", "LET THEM RUN",
                S("", ShotCamera.Wide, 3.5f, audio: ShotAudio.Fire),
                S("SOLDIER", ShotCamera.Handheld, 2.4f, "SOLDIER", "The wagon's gone! Fall back!"),
                S("RENZO", ShotCamera.Hold, 2.8f),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Let them run."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Running men go home."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 12 SILENT CARGO
            Make("cargo_open", "NO LANTERNS",
                S("RENZO", ShotCamera.SlowDolly, 3.8f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "A night shipment, and not one lantern."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Whatever that is, they don't want it seen."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // Suzu, stealing from the same wagon, badly. The join is an argument.
            Make("cargo_suzu", "GET YOUR OWN WAGON",
                S("SUZU", ShotCamera.Handheld, 2.8f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "Get your own wagon."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You'll wake the whole escort."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Then be quieter than me."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "That won't be hard."),
                S("SUZU", ShotCamera.Hold, 3.4f, "SUZU", "Two guards on the far side, one asleep. Left wheel squeaks."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Suzu. Don't make it weird."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            // The captain, counting. The one man on the road who was awake.
            Make("cargo_captain", "COUNTED",
                S("CONVOY CAPTAIN", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("CONVOY CAPTAIN", ShotCamera.Hold, 3.4f, "CONVOY CAPTAIN", "Everything on this road is counted."),
                S("CONVOY CAPTAIN", ShotCamera.PushIn, 2.8f, "CONVOY CAPTAIN", "You were not.", audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Then count faster."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("cargo_end", "KIBA",
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "You're going after the supplier.", audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You're not coming."),
                S("SUZU", ShotCamera.Hold, 3.4f, "SUZU", "Somebody has to know the patrol routes."),
                S("SUZU", ShotCamera.PushIn, 3f, "SUZU", "And it isn't going to be you."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 13 THE BROKEN VILLAGE
            Make("ashfall_open", "ASHFALL",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "This is Ashfall. Was."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Same as Yorune."),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Not the same. Look how they burned it."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // He was paid not to know. That is the payload.
            Make("ashfall_king", "WHAT THE FIRE LEFT",
                S("SCAVENGER KING", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.MusicDark),
                S("SCAVENGER KING", ShotCamera.Hold, 3.2f, "SCAVENGER KING", "Everything the fire left is mine."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Then you know what they were looking for."),
                S("SCAVENGER KING", ShotCamera.Hold, 3.4f, "SCAVENGER KING", "I know what they paid me not to look for."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Paid by who?"),
                S("SCAVENGER KING", ShotCamera.Hold, 2.6f, "SCAVENGER KING", "Come and ask."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("ashfall_end", "SOMEONE BREATHING",
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "There's a cellar under the elder's house.", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "And someone breathing in it."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 14 THE SURVIVOR
            // The first person alive who remembers Aiko, and she is annoyed.
            Make("survivor_cellar", "CLOSE THE DOOR",
                S("FUMI", ShotCamera.PushIn, 3.4f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "Close the door. You're letting the light in."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "You've been down here since the fire?"),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "Since before it. Somebody had to copy the ledger."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "A girl. Red thread on her wrist. Aiko Kurogawa."),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "Alive after the fire.", audio: ShotAudio.Sting),
                S("FUMI", ShotCamera.PushIn, 2.8f, "FUMI", "And not alone."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.4f));

            Make("survivor_elite", "BURN THE REST",
                S("OFFICER", ShotCamera.SlowDolly, 3.2f, audio: ShotAudio.MusicDark),
                S("OFFICER", ShotCamera.Hold, 2.8f, "OFFICER", "Burn the rest of it. And her."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "She walks."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("survivor_end", "THE SIDE OF KNOWING",
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "There's a road that isn't on any map.", audio: ShotAudio.MusicSoft),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "Their wagons use it. Nobody else does."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Show me."),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "I'm not on your side, Kurogawa."),
                S("FUMI", ShotCamera.PushIn, 3f, "FUMI", "I'm on the side of knowing."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 15 HIDDEN ROAD
            Make("road_open", "THEY WATCH THE ROAD",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Rain),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Archers on the ridge. They watch the road, not the trees."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then I'll use the trees."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            Make("road_tower", "SOMEONE ON TOP",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "It sees every road in the valley."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "And someone's standing on top of it."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then it comes down."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
