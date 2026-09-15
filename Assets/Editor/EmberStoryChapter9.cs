using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 9, missions 81-85: the reunion, the long night, the way
    /// out, the mutiny and the door. Aiko is framed still and close; the fortress
    /// moves around her.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter9Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 81 YOU CAME
            Make("reunion_open", "THE LENGTH OF THE HALL",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.MusicOff),
                S("RENZO", ShotCamera.SlowDolly, 4f),
                S("AIKO", ShotCamera.Hold, 3.6f),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Don't move. Stay right there. I'm coming to you."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.4f));

            // Two people in a room. The only scene in the game built for nothing else.
            Make("reunion_talk", "TWO PEOPLE IN A ROOM",
                S("AIKO", ShotCamera.Hold, 3.6f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "You got tall. You look like him."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Everyone keeps telling me that. It hasn't been good news once."),
                S("AIKO", ShotCamera.PushIn, 3.4f, "AIKO", "It is now."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "I should have followed you that night. Out of the house."),
                S("AIKO", ShotCamera.Hold, 3.6f, "AIKO", "You'd have been caught too. Then nobody would have come."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f, audio: ShotAudio.MusicSoft));

            Make("reunion_end", "NOT HERE",
                S("AIKO", ShotCamera.Hold, 3.2f, audio: ShotAudio.Wind),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "I have ten years to tell you, and none of it can be told here."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then somewhere with a door we can bar."),
                S("AIKO", ShotCamera.PushIn, 3f, "AIKO", "They'll come for me by dark. They always come by dark."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 82 THE LONG NIGHT
            Make("longnight_open", "BAR THE DOORS",
                S("DAIGO", ShotCamera.Hold, 3f, audio: ShotAudio.MusicDark),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "Doors are barred. They won't hold till morning. They'll hold a while."),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "Then I'll talk fast. Renzo, hold the doors. Listen when you can."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "I'm listening.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("longnight_story", "THE FIRST YEAR",
                S("AIKO", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3.8f, "AIKO", "The first year they asked kindly. Rice, a blanket, a window. Where is it, little one."),
                S("AIKO", ShotCamera.Hold, 3.4f, "AIKO", "The second year the window went away. By the fifth I'd stopped counting windows."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "And you never told them."),
                S("AIKO", ShotCamera.Hold, 2.8f, "AIKO", "Ren. I was six. Nobody asks a six-year-old the right questions."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicDark));

            Make("longnight_end", "DAWN",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "Dawn. The doors are done. So are they."),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "The Seal is in Yorune, Ren. Under the shrine. It always was."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Then first, I get you out of this building."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 83 THE PRISONER
            Make("escape_open", "SHE WILL NOT WAIT",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Walls are mine. Stay under me and nothing gets a clean shot."),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "I can't fight. I can run, and I can hide, and I'm not waiting for either of you."),
                S("DAIGO", ShotCamera.PushIn, 2.6f, "DAIGO", "I like her."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicImpact));

            Make("escape_hesitate", "THEY KNOW HER",
                S("SOLDIER", ShotCamera.Handheld, 3.2f, audio: ShotAudio.Silence),
                S("SOLDIER", ShotCamera.Hold, 3f, "SOLDIER", "…the girl with the thread. I was at Yorune. I carried the torch."),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "Then put down the spear, and don't carry anything for him again."),
                S("SOLDIER", ShotCamera.PushIn, 2.6f),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "He's letting us through."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("escape_end", "THE FORTRESS TURNS",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Bells),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Listen to that. That's not a chase. That's fighting."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "The fortress is fighting itself."),
                S("AIKO", ShotCamera.PushIn, 3f, "AIKO", "Whoever wins will want me. I'd rather know who."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 84 THE BETRAYAL
            Make("mutiny_open", "BACK IN",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Fire),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "We got out. Every sensible bone says keep walking."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Half his army's in there. I want to know which half."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicImpact));

            Make("mutiny_oba", "FOR LAND, NOT A SEAL",
                S("OBA", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("OBA", ShotCamera.Hold, 3.4f, "OBA", "Captain Oba. We followed him for land and rice. Not to die on a mountain for a lock."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "And the ones still wearing the serpent?"),
                S("OBA", ShotCamera.PushIn, 3f, "OBA", "On the keep stair. Help us, Kurogawa, or get out of the way.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The consequence from the design layer: half the army stands down at her.
            Make("mutiny_aiko", "THE GIRL WITH THE THREAD",
                S("AIKO", ShotCamera.SlowDolly, 3.8f, audio: ShotAudio.Silence),
                S("OBA", ShotCamera.Hold, 3f, "OBA", "That's her. The one he kept in the hall. Ten years."),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "The chamber under the fortress. Open it for us."),
                S("OBA", ShotCamera.PushIn, 3f, "OBA", "Stand down! All of you — stand down, and open the way!"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicSoft));

            Make("mutiny_end", "UNDER THE FORTRESS",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Wind),
                S("OBA", ShotCamera.Hold, 3f, "OBA", "We'll hold the walls. Whatever's under there, it's yours, not his."),
                S("AIKO", ShotCamera.Hold, 2.8f, "AIKO", "He kept the keys down there. He could never use them."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Show me the door."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 85 THE SEAL'S DOOR
            Make("sealdoor_open", "OUR FAMILY'S HOUSE",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "I don't like the carvings. They watch the lamp."),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "They're us. The first Kurogawa built this. He kept his keys in our house."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Read it. I'll watch the dark."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("sealdoor_guardians", "A CENTURY ASLEEP",
                S("WHISPER", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("WHISPER", ShotCamera.Hold, 3f, "WHISPER", "…blood of the door… which of you… which of you is angry…"),
                S("AIKO", ShotCamera.Hold, 2.4f, "AIKO", "They're asking us."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Then don't answer.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("sealdoor_end", "FOR BOTH OF YOU",
                S("", ShotCamera.PushIn, 4f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "His mark. And words under it."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "\"For both of you.\""),
                S("AIKO", ShotCamera.PushIn, 2.8f, "AIKO", "He knew we'd both stand here."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.6f, audio: ShotAudio.MusicSoft));
        }
    }
}
