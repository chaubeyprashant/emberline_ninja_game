using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 8, missions 76-80: the tower, the records, Toku's steel,
    /// Hoshu's death and the empty hall. The chapter ends on the one person in the
    /// hall who is not a guard, and a card; her first words belong to 81.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter8BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 76 THE PRISON TOWER
            Make("tower_open", "EVERY CELL",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Eleven floors. I counted the windows from the drain."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Open every cell on the way up."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Every cell. Even the ones that aren't her."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The consequence from the design layer: Suzu gets what Renzo came for.
            Make("tower_kanta", "NOT HER",
                S("KANTA", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("KANTA", ShotCamera.Hold, 2.8f, "KANTA", "…Suzu? Suzu, is that you?"),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Kanta. Kanta, I'm here. I'm here, I'm here."),
                S("RENZO", ShotCamera.OverShoulder, 3f, "RENZO", "Not her. But somebody's.", audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Handheld, 2.4f, "GUARD", "The bell! Somebody get to the bell!", audio: ShotAudio.Bells),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("tower_end", "STILL WARM",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Her marks. Father's cipher. The last one is yesterday."),
                S("KANTA", ShotCamera.Hold, 3.2f, "KANTA", "The girl with the red thread. They took her down last night. She was singing."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "One night. I missed her by one night."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));

            // ------------------------------------------------ 77 THE EMPTY CELL
            // The consequence from the design layer: Suzu walks her brother down.
            Make("records_open", "GO",
                S("SUZU", ShotCamera.Hold, 3.4f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "He can't walk the drain alone. I'm taking him down the mountain."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Go."),
                S("SUZU", ShotCamera.PushIn, 3f, "SUZU", "You found mine. Find yours."),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "A cell has a record. And I know how a fortress files."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("records_smoke", "THEY ARE BURNING IT",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Fire),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "Smoke. They're burning the records room. Now, of all nights."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Because of tonight.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("records_end", "TO ME",
                S("FUMI", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "His own hand. \"The daughter to the inner hall. To me.\""),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "She's with him."),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "And three empty key cases. Renzo, you're carrying two of what he wants."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f, audio: ShotAudio.MusicDark));

            // ------------------------------------------------ 78 THE IRON GUARD
            Make("iron_open", "KAGEHIRA'S SHIELD",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.MusicDark),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "They move like one man. I've fought a wall before. This is worse."),
                S("TOKU", ShotCamera.Hold, 3f, "TOKU", "It's not a wall. It's my steel. Let me stand in front of it."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The consequence from the design layer: Toku unmakes his own work.
            Make("iron_toku", "HIS OWN WORK",
                S("TOKU", ShotCamera.Orbit, 3.8f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3.6f, "TOKU", "You're wearing my work! Every plate in this hall came off my anvil, for Yorune's gate!"),
                S("TOKU", ShotCamera.Hold, 3.2f, "TOKU", "I know where it's thin. Renzo — under the left arm. I never finished the left arm."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Left arm.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("iron_captain", "THE SECOND OF NINE",
                S("IRON GUARD", ShotCamera.Orbit, 3.8f, audio: ShotAudio.Sting),
                S("IRON GUARD", ShotCamera.Hold, 3.4f, "IRON GUARD", "We were Goro's once. We carried his toll-chests. We know exactly who you are."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "Then you know how Goro ended."),
                S("IRON GUARD", ShotCamera.PushIn, 2.6f, "IRON GUARD", "Change! Close on me!", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("iron_end", "THE LINE BREAKS",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3f, "TOKU", "Look at them. Taking it off. Nobody to call the change."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "The inner gate's ahead. And the one man left who holds it."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Hoshu. He said he wouldn't yield twice."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 79 THE INNER GATE
            Make("inner_open", "HE MEANS IT",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "A man only tells you he won't yield twice if he means it."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "I know."),
                S("DAIGO", ShotCamera.PushIn, 2.8f, "DAIGO", "I'll hold his men. The ring is yours."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("inner_hoshu", "I WILL NOT YIELD TWICE",
                S("COMMANDER HOSHU", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.6f, "COMMANDER HOSHU", "I have held this door for eleven years. You are not the first Kurogawa to reach it."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "I'm the last."),
                S("COMMANDER HOSHU", ShotCamera.PushIn, 3f, "COMMANDER HOSHU", "Then let it end properly. Draw.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("inner_strength", "EVERYTHING AT ONCE",
                S("COMMANDER HOSHU", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.2f, "COMMANDER HOSHU", "The Three Blades taught me patience. Goro taught me the rest. Watch."),
                S("", ShotCamera.Hold, 0.4f, audio: ShotAudio.MusicDark, fadeAfter: true, blackAfter: 0.2f));

            Make("inner_death", "PROPERLY",
                S("COMMANDER HOSHU", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.6f, "COMMANDER HOSHU", "…properly… yes. Go through, then. He is waiting."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "He has been for a long time."),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.2f, "COMMANDER HOSHU", "Not for you, Kurogawa. For the keys you carry."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            Make("inner_end", "LIT, AND EMPTY",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "The gate's open."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "The hall's lit. And there's nobody in it."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f, audio: ShotAudio.MusicDark));

            // ------------------------------------------------ 80 THE WARLORD'S HALL
            Make("hall_open", "THE END OF THE ROAD",
                S("", ShotCamera.Wide, 4.6f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 4f),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Forty days of road. It ends in a lit room with nobody in it."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("hall_gone", "HE LEFT",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "The throne's cold. He left before we took the wall. He knew."),
                S("OFFICER", ShotCamera.Handheld, 2.8f, "OFFICER", "The warlord's orders: nobody leaves this hall. Including us."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "He left you here to die.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // She says nothing yet. Her first words are 81's.
            Make("hall_end", "STANDING. OLDER. ALIVE.",
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.MusicOff),
                S("RENZO", ShotCamera.OverShoulder, 3.6f, "RENZO", "…red thread."),
                S("AIKO", ShotCamera.PullOut, 4.6f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.PushIn, 4f),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Aiko."),
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 8 — THE IRON FORTRESS", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
