using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 10, missions 96-100: alone on the stair, the offer, the
    /// Seal, the serpent's end and the dawn. Kagehira never shouts. The last scene
    /// is the original ending, untouched, after the people who came.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter10BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 96 NO WAY BACK
            Make("noway_open", "ALONE",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.SlowDolly, 3.8f),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "No Suzu. No Daigo. No arrows over my head."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Like the first night. Except this time I know the way."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("noway_shades", "THEY WERE HIS",
                S("WHISPER", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("WHISPER", ShotCamera.Hold, 3.2f, "WHISPER", "…he bound us… he kept us… Kurogawa… set us down…"),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "The missing. All this time, they were the shades."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "I'll set you down. All of you.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("noway_voice", "A VOICE YOU KNOW",
                S("", ShotCamera.PushIn, 3.4f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "Ren. Don't come in angry. He wants you angry."),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "Let him come as he likes. I've waited ten years for the rest of this conversation."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicDark));

            Make("noway_end", "THE DOOR",
                S("RENZO", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "She's alive. She's talking. That's enough."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Be less. Then open the door."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 97 FATHER AND SON
            Make("fatherson_open", "HE WANTS TO TALK",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.SlowDolly, 3.8f),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "Sit, if you like. The keys are turned. There is no hurry left in the world."),
                S("AIKO", ShotCamera.Hold, 2.6f, "AIKO", "Don't sit."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "I wasn't going to."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("fatherson_offer", "HIS PLACE",
                S("KAGACHI", ShotCamera.Orbit, 4f, audio: ShotAudio.MusicDark),
                S("KAGACHI", ShotCamera.Hold, 3.6f, "KAGACHI", "Stand beside me, Kurogawa, where he should have stood. The water, fairly given. By us."),
                S("RENZO", ShotCamera.OverShoulder, 2.8f, "RENZO", "By you."),
                S("KAGACHI", ShotCamera.Hold, 3f, "KAGACHI", "Someone must hold the gate. Why not a man who knows its weight?"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "No."),
                S("KAGACHI", ShotCamera.Hold, 2.6f, "KAGACHI", "Then let my guard ask you again.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("fatherson_end", "NOTHING LEFT TO SAY",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "Your father said no the same way. One word, and he didn't sit either."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Then you know how this ends."),
                S("KAGACHI", ShotCamera.PushIn, 3.2f, "KAGACHI", "I know how it ended for him. Come to the Seal."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 98 THE BLACK SEAL
            Make("seal98_open", "IT IS REAL",
                S("", ShotCamera.Wide, 4.6f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 3.8f),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "The open hand from the carvings. Real, and the water pouring through it."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "It was never meant to be opened. Only guarded."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The question the chapter asks: why she has not run.
            Make("seal98_aiko", "WHY SHE HAS NOT RUN",
                S("AIKO", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You're unbound. Run, Aiko!"),
                S("AIKO", ShotCamera.Hold, 3.8f, "AIKO", "If I take my hand off the Seal, the water takes every village below in a night. I'm holding it back."),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "She understands it better than either of us. She always did."),
                S("AIKO", ShotCamera.PushIn, 2.8f, "AIKO", "End it, Ren. Just end it properly."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicDark));

            Make("seal98_end", "THE END OF IT",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.MusicDark),
                S("KAGACHI", ShotCamera.Hold, 3f, "KAGACHI", "Here, then. At the Seal. Where your father should have died."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Where you will."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 99 KAGACHI
            Make("kagachi_open", "THREE KEYS, ONE DOOR",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.Hold, 4f, "KAGACHI", "Three keys. One door. And a Kurogawa to open it. Your father would have been proud of the symmetry."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "My father would have cut you down before the first key."),
                S("KAGACHI", ShotCamera.PushIn, 2.6f, "KAGACHI", "He tried."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("kagachi_draw", "THE SERPENT",
                S("KAGACHI", ShotCamera.Orbit, 4.6f, audio: ShotAudio.Sting),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "Everything you learned on the road, boy. Show me all of it."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "All of it.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("kagachi_warlord", "WITH IT, I AM ORDER",
                S("KAGACHI", ShotCamera.Handheld, 3.2f, audio: ShotAudio.MusicDark),
                S("KAGACHI", ShotCamera.Hold, 3.6f, "KAGACHI", "With the Seal I am order. Without it I am a man with an army. And armies end."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "So do you.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // After lower_the_sword: the last attack from his knees, and Renzo's answer.
            Make("kagachi_death", "THE SERPENT'S END",
                S("KAGACHI", ShotCamera.PushIn, 4f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.Hold, 3.6f, "KAGACHI", "You could have… taken my head… and my gate…"),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "I turned your blade. You did the rest."),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "The water's going down. Ren — the Seal's closing itself."),
                S("KAGACHI", ShotCamera.Hold, 3.4f, "KAGACHI", "…nobody's gate… then… the villages'…"),
                S("RENZO", ShotCamera.PullOut, 3.4f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 10 — THE SERPENT'S END", fadeAfter: true, blackAfter: 1f));

            // ------------------------------------------------ 100 EMBERLINE
            Make("dawn_open", "IT IS OVER",
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.Wind, blackAfter: 0.6f, theme: EnvThemeId.VillageDawn),
                S("", ShotCamera.Wide, 4.6f, audio: ShotAudio.Birds),
                S("AIKO", ShotCamera.SlowDolly, 3.8f, "AIKO", "The first morning in ten years I've watched the sun come up outside."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "It's a long way down. Take your time."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            Make("dawn_road", "THE THREAD",
                S("AIKO", ShotCamera.PushIn, 3.6f, audio: ShotAudio.MusicSoft),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "You kept the thread on."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You told me not to take it off."),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "I was six when I said that."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "I was nine. I listened."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // The consequence from the design layer: who is standing at Yorune at dawn.
            Make("dawn_yorune", "WHO CAME",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Birds),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "You came back down. The medicine's free, boy. Just this once."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "Held the gate. Walked away from it, like you said."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Kanta's cutting beams. He says the house goes here."),
                S("TOKU", ShotCamera.Hold, 3f, "TOKU", "First honest thing I'll make after the blade: hinges. For a door."),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "I brought the records. Every family on them can come home now."),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "Last watch. Nothing on the road but us."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f, audio: ShotAudio.MusicSoft));
        }
    }
}
