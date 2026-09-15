using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 10, missions 91-95: the fire, the army, the goodbyes on
    /// the summit road, Toku's blade, and the fog. Each companion gets one last
    /// held shot and a reason, said out loud.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter10Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 91 THE BURNING FORTRESS
            Make("burnfort_open", "HE BURNED THE BRIDGE",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Fire),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "The whole chamber wing's going up. He set it behind him."),
                S("AIKO", ShotCamera.Hold, 2.8f, "AIKO", "It's a cut hand, Ren. I can run. Stop looking at it."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Then run.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("burnfort_daigo", "THE ROOF FIRST",
                S("DAIGO", ShotCamera.Handheld, 3f, audio: ShotAudio.Fire),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "Courtyard's the only way through, and the roof's about to come in on it."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Then we're under it when it does."),
                S("DAIGO", ShotCamera.PushIn, 2.6f, "DAIGO", "I was hoping you'd say that. I wasn't.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("burnfort_end", "THE SUMMIT ROAD",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.Wind),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "Ten years in that building. I thought I'd feel something watching it burn."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Do you?"),
                S("AIKO", ShotCamera.PushIn, 2.8f, "AIKO", "Cold. Let's go up."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 92 THE LAST ARMY
            Make("lastarmy_open", "THE WHOLE ROAD",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Snow),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "I have the flank. Don't wait for me, I'll be there."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "I have the line. Nobody walks through it who isn't us."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Last army. Last road. Go."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicImpact));

            // The consequence from the design layer: Tsuru puts names on the shafts.
            Make("lastarmy_tsuru", "NAMES ON THE SHAFTS",
                S("TSURU", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 3.4f, "TSURU", "Every name from the notices. One on each arrow. I ran off a wall once. Not today."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "How many names?"),
                S("TSURU", ShotCamera.PushIn, 2.6f, "TSURU", "More than I have arrows.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("lastarmy_oba", "BEHIND THE LINE",
                S("OBA", ShotCamera.Handheld, 3.2f, audio: ShotAudio.MusicImpact),
                S("OBA", ShotCamera.Hold, 3f, "OBA", "Kurogawa! We came up behind them. They were fighting for silver. We weren't."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "For Aiko."),
                S("OBA", ShotCamera.PushIn, 2.8f, "OBA", "For the girl who told us to put the spears down."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("lastarmy_end", "A DAY'S CLIMB",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "The road's open."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "A day's climb to the top. Then it's just him."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then it's just him."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 93 THE SUMMIT ROAD
            // The consequence from the design layer: the companions stop here, each
            // for their own reason, said out loud.
            Make("summit_open", "EVERYONE HAS A REASON",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Snow),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Kanta's at the reed village waiting on me. I stop here. I'm not sorry."),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "The gate I held has people behind it. Somebody has to hold it again. That's me."),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "The water records. If he dies up there, somebody has to know who drinks. I'll keep them."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "I'll take the last watch of the road. After that you're past my arrows."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "All good reasons."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("summit_toku", "ONE POST FURTHER",
                S("TOKU", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.Wind),
                S("TOKU", ShotCamera.Hold, 3.2f, "TOKU", "I'm going one post further than the rest of them. Carrying something."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "What is it?"),
                S("TOKU", ShotCamera.PushIn, 2.8f, "TOKU", "You'll see at the post. Walk. The guard's turning back to hold it.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("summit_end", "THE LAST WALL",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Summit gate. His strongest are in front of it. Past here, I can't cover you."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "You covered me enough."),
                S("TSURU", ShotCamera.PushIn, 2.6f, "TSURU", "Not yet. Soon."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 94 THE FINAL GUARD
            // The consequence from the design layer: Toku's last honest blade.
            Make("finalguard_open", "A BLADE THAT IS NOT THEIRS",
                S("TOKU", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3.6f, "TOKU", "Yorune steel. Taken back off the Iron Guard and made honest. Last blade I'll make."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Toku—"),
                S("TOKU", ShotCamera.Hold, 3.4f, "TOKU", "A smith's work ends at the handle. This is the handle. I'm going back down."),
                S("TOKU", ShotCamera.PullOut, 3f),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicSoft));

            Make("finalguard_captain", "THE LAST OF NINE",
                S("IRON GUARD", ShotCamera.Orbit, 4f, audio: ShotAudio.Sting),
                S("IRON GUARD", ShotCamera.Hold, 3.4f, "IRON GUARD", "Eight shields fell before you reached me. I am the ninth, and the last."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "He's up there, watching you."),
                S("IRON GUARD", ShotCamera.PushIn, 3f, "IRON GUARD", "He told us: make sure you do not arrive whole.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("finalguard_end", "NOTHING ON IT",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Toku's steel held. The gate's open."),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "You didn't think I'd stay at the post, did you?"),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "No. Stay behind me, then. Fog up there."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 95 THE SERPENT'S SHADOW
            Make("shadow_open", "FASTER THAN THE FOG",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 2.8f, "AIKO", "He's close. I know how he breathes. Ten years of it."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Back to back."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("shadow_strike", "OUT OF THE FOG",
                S("KAGACHI", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("KAGACHI", ShotCamera.Hold, 3.2f, "KAGACHI", "Back to back. Your father stood like that. It made him easy to find."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Aiko, down!", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The consequence from the design layer: from here Renzo is alone.
            Make("shadow_alone", "ALONE",
                S("KAGACHI", ShotCamera.OverShoulder, 3.6f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.Hold, 3.4f, "KAGACHI", "I could open you here. I won't. Not yet."),
                S("KAGACHI", ShotCamera.PushIn, 3f, "KAGACHI", "Alone, Kurogawa. Come alone."),
                S("", ShotCamera.Wide, 3f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Aiko? …Aiko!"),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicDark));

            Make("shadow_end", "AS HE SAID",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Her thread. Cut clean. He wanted me to find it."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Alone, then. Like the first night."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
