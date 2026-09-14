using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>Scenes for Chapter 3, missions 26-30: the archer, the bead, the trap, the Shade.</summary>
    public static partial class EmberStory
    {
        private static void BuildChapter3BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 26 THE BLIND PATH
            Make("blind_open", "TEN PACES",
                S("", ShotCamera.Wide, 3.4f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "I can't see ten paces."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Then we listen."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            // Tsuru joins with an arrow in the tree beside Renzo's head.
            Make("blind_archer", "THE NEXT ONE ISN'T A WARNING",
                S("TSURU", ShotCamera.SlowDolly, 3.2f, audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "Stop there. The next one isn't a warning."),
                S("RENZO", ShotCamera.Hold, 2f, "RENZO", "You missed."),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "I didn't. There are things following you. A lot of them."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "That's garrison armour. He's a deserter."),
                S("TSURU", ShotCamera.Hold, 3.6f, "TSURU", "I shot at people like you for two years. I wasn't good at it."),
                S("TSURU", ShotCamera.PushIn, 2.6f, "TSURU", "Here they come.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.2f));

            Make("blind_end", "TIED ON PURPOSE",
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "The thread stops here.", audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Someone tied it here on purpose."),
                S("TSURU", ShotCamera.PushIn, 3f, "TSURU", "Then I'm coming. I want to see who."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 27 THE RED THREAD
            // Nobody explains the silence. Nothing happens, and that is the scene.
            Make("redthread_open", "THE THREAD GOES ON",
                S("RENZO", ShotCamera.OverShoulder, 4f, audio: ShotAudio.Birds),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "It keeps going."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            Make("redthread_bead", "SHE WAS NINE",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "She was nine."),
                S("SUZU", ShotCamera.Hold, 2.2f, "SUZU", "And clever."),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "She was counting on somebody looking up."),
                S("RENZO", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3.2f),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));

            // ------------------------------------------------ 28 THE DECOY
            Make("decoy_open", "IT COULD BE HER",
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "A girl in a pen. Reported this morning.", audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 2.4f, "TSURU", "Reported to who?"),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "It doesn't matter. It could be her."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("decoy_trap", "THAT'S NOT HER",
                S("SUZU", ShotCamera.Handheld, 2.6f, "SUZU", "Renzo. That's not her.", audio: ShotAudio.Sting),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "The walls are moving. It's a trap!"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("decoy_end", "THEY'LL TRY AGAIN",
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "She's never heard of Aiko.", audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "They're using her name to catch you now."),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Send the girl to Ashfall. She'll be safe there."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "And whoever laid this will lay another."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 29 THE HUNTER'S TRAP
            Make("trap_open", "THE PATHS ARE CLOSING",
                S("TSURU", ShotCamera.Handheld, 2.6f, "TSURU", "They've closed the north path.", audio: ShotAudio.MusicDark),
                S("SUZU", ShotCamera.Hold, 2.2f, "SUZU", "And the east."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we hold until one opens."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("trap_last", "SOMETHING IN THE LAST EXIT",
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "One way out left.", audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "Something's standing in it."),
                S("PALE SHADE", ShotCamera.PushIn, 3.2f, "PALE SHADE", "…the thread-carrier… come closer…"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("trap_end", "IT HAS ARRIVED",
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "It let us through. Again.", audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "No. It wants me to come to it."),
                S("TSURU", ShotCamera.PushIn, 2.8f, "TSURU", "Then that's where we're going."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 30 PALE SHADE
            Make("shade_open", "OF COURSE IT'S A GRAVEYARD",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "A graveyard. Of course it's a graveyard."),
                S("SUZU", ShotCamera.Hold, 2f, "SUZU", "It's here."),
                S("RENZO", ShotCamera.PushIn, 2f, "RENZO", "Good."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("shade_confront", "SHE CARRIED YOURS",
                S("PALE SHADE", ShotCamera.SlowDolly, 3.4f, audio: ShotAudio.MusicDark),
                S("PALE SHADE", ShotCamera.Hold, 3.4f, "PALE SHADE", "…you carry her thread… she carried yours…"),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Where is she."),
                S("PALE SHADE", ShotCamera.PushIn, 3.4f, "PALE SHADE", "…the marsh sent me to ask… not to answer…"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("shade_full", "FULL DARK",
                S("PALE SHADE", ShotCamera.PullOut, 3f, "PALE SHADE", "…full dark now…", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("shade_death", "THE TOLL-CAPTAIN'S COUNTRY",
                S("PALE SHADE", ShotCamera.Hold, 3.2f, audio: ShotAudio.Silence),
                S("PALE SHADE", ShotCamera.PushIn, 3.6f, "PALE SHADE", "…she was moved… to the toll-captain's country…"),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Goro."),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "Then we're going to war."),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 3 — THE SILENT FOREST", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
