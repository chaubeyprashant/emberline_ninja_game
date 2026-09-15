using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 6, missions 51-55: the journal, and the night Yorune
    /// burned seen from inside it. The living get the slow dolly; the dead get
    /// held shots and silence; Goro, young, gets the orbit he had at 5.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter6Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 51 FATHER'S JOURNAL
            Make("journal_open", "HIS HAND",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 3.4f),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "Somebody tore it apart looking for a map. They couldn't read it."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "It isn't a map. It's him talking to me."),
                S("NIRE", ShotCamera.PushIn, 2.8f, "NIRE", "Then read quietly. Things live up here that listen."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("journal_watchers", "THEY WANT THE LAST PAGE",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("WHISPER", ShotCamera.Hold, 2.8f, "WHISPER", "…the last page… leave the last page…"),
                S("SOLDIER", ShotCamera.Hold, 2.6f, "SOLDIER", "The Kurogawa's in the upper hall! Burn the pages if you have to!"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "You've burned enough of his.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("journal_end", "STAND WHERE I STOOD",
                S("RENZO", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("FATHER", ShotCamera.Hold, 4f, "FATHER", "Remember it, Ren. Stand where I stood that night, and remember all of it."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "I've spent ten years trying not to."),
                S("NIRE", ShotCamera.Hold, 3f, "NIRE", "Then close your eyes. I'll watch the door."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 1.2f, audio: ShotAudio.MusicSoft));

            // ------------------------------------------------ 52 THE LAST NIGHT
            Make("lastnight_open", "TEN YEARS AGO",
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.Wind, blackAfter: 0.8f, theme: EnvThemeId.VillageDawn),
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Village),
                S("RENZO", ShotCamera.SlowDolly, 3.6f, "RENZO", "Yorune. Whole. The smoke's from the cook fires."),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "Ren! You're late for supper again."),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "She can't see me. She's calling the boy I was."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("lastnight_end", "THE FIRST FIRE",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "A chalk mark on our door. They knew which house before the first torch."),
                S("", ShotCamera.Wide, 2.8f, audio: ShotAudio.Bells),
                S("FATHER", ShotCamera.PushIn, 3.2f, "FATHER", "Inside. Both of you. Now.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));

            // ------------------------------------------------ 53 THE BURNING VILLAGE
            Make("burning_open", "WHAT ACTUALLY HAPPENED",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Fire),
                S("RENZO", ShotCamera.Handheld, 3.2f, "RENZO", "I ran from this for ten years. I'm walking into it now."),
                S("SEARCHER", ShotCamera.Hold, 2.8f, "SEARCHER", "Every house! Every floorboard! He said it's small, wrapped in cloth!"),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Not a raid. A search.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("burning_door", "I COULD NOT GO IN",
                S("", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Fire),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "His door. I stood right here with my hand on it."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "I couldn't open it then."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "The journal says what's on the other side. I'm going to read it.", audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.6f));

            // ------------------------------------------------ 54 THE SWORDMASTER
            Make("sword_open", "HE HELD THE DOOR",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Fire),
                S("FATHER", ShotCamera.SlowDolly, 3.6f),
                S("FATHER", ShotCamera.Hold, 3.4f, "FATHER", "Take the lanterns up the ridge. I'll give you the time."),
                S("MOTHER", ShotCamera.Hold, 3f, "MOTHER", "How much time?"),
                S("FATHER", ShotCamera.PushIn, 3f, "FATHER", "All of it."),
                S("RENZO", ShotCamera.OverShoulder, 2.8f, "RENZO", "Stand where he stood. Hold what he held.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            // Goro, young, before the toll road. The orbit he had at 5, thirty years early.
            Make("sword_goro", "THE RAIDER CAPTAIN",
                S("GORO", ShotCamera.Orbit, 3.8f, audio: ShotAudio.Silence),
                S("GORO", ShotCamera.Hold, 3.4f, "GORO", "Swordmaster. Step aside, and the rest of them live."),
                S("FATHER", ShotCamera.Hold, 3f, "FATHER", "You don't believe that. Neither do I."),
                S("GORO", ShotCamera.PushIn, 2.6f, "GORO", "Then you die first.", audio: ShotAudio.Sting),
                S("FATHER", ShotCamera.PushIn, 2.2f, "FATHER", "Somebody does.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("sword_scar", "THE SCAR",
                S("GORO", ShotCamera.Handheld, 3f, audio: ShotAudio.Fire),
                S("GORO", ShotCamera.Hold, 2.8f, "GORO", "My face — you cut my face!"),
                S("FATHER", ShotCamera.Hold, 3f, "FATHER", "Wear it. Every time you raise a toll, remember the door you didn't pass."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "The scar. Father gave him the scar.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("sword_end", "THE MAN BEHIND IT",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Rain),
                S("FATHER", ShotCamera.Hold, 3.6f, "FATHER", "The last lantern's over the ridge. Good."),
                S("GORO", ShotCamera.Hold, 2.8f, "GORO", "It's not here! Burn it all and fall back!"),
                S("FATHER", ShotCamera.PushIn, 4f, "FATHER", "Ren. Aiko. Don't open the door.", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "The door held. He didn't."),
                S("", ShotCamera.Hold, 1.2f, fadeAfter: true, blackAfter: 1f, audio: ShotAudio.MusicSoft));

            // ------------------------------------------------ 55 MOTHER'S CHOICE
            Make("mother_open", "BEHIND THE LANTERN",
                S("MOTHER", ShotCamera.SlowDolly, 3.8f, audio: ShotAudio.Fire),
                S("MOTHER", ShotCamera.Hold, 3.4f, "MOTHER", "Everyone behind my lantern. Nobody runs ahead. Nobody stops."),
                S("AIKO", ShotCamera.Hold, 2.6f, "AIKO", "Where's Ren?"),
                S("MOTHER", ShotCamera.PushIn, 3f, "MOTHER", "With your father. Hold my hand, and don't look back."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("mother_seal", "HOLD THIS",
                S("MOTHER", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("MOTHER", ShotCamera.Hold, 3.6f, "MOTHER", "Aiko. Hold this, and don't let go of it for anyone."),
                S("AIKO", ShotCamera.Hold, 2.6f, "AIKO", "It's heavy."),
                S("MOTHER", ShotCamera.Hold, 3.2f, "MOTHER", "It's supposed to be. That's how you know it's still there."),
                S("RENZO", ShotCamera.OverShoulder, 3f, "RENZO", "She gave it to Aiko. The Seal. She was six.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("mother_end", "THE LAST CHILD",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("MOTHER", ShotCamera.Hold, 3.2f, "MOTHER", "Forty lanterns. Where's the Tanaka boy?"),
                S("AIKO", ShotCamera.Hold, 2.6f, "AIKO", "Mama, don't —"),
                S("MOTHER", ShotCamera.PushIn, 3.6f, "MOTHER", "Stay on the ridge. Keep it safe. I'll be one minute."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "She wasn't. And Aiko didn't stay.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
