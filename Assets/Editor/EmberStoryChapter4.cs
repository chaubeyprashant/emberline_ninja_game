using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 4, missions 31-35: Goro's war, and the man in the collar.
    /// Every beat opens on the place before the people (a wide or a dolly), gives
    /// the reveal its own shot (an orbit, a sting), and leaves on a fade.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter4Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 31 THE FORTRESS ROAD
            Make("road31_open", "A WARNING",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.SlowDolly, 3.6f),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Serpent banners. Every hundred paces."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "It isn't a road. It's a warning."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then let's read it."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("road31_wagons", "THOSE ARE PEOPLE",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Orbit, 3.6f),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Wagons. A whole line of them, through the fog."),
                S("TSURU", ShotCamera.PushIn, 2.6f, "TSURU", "Those are people.", audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "Then we go down."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 32 PRISONER WAGONS
            Make("wagons_open", "THE DRIVERS WON'T STOP",
                S("", ShotCamera.Wide, 3.4f, audio: ShotAudio.Rain),
                S("TSURU", ShotCamera.OverShoulder, 3f),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "Four wagons, one escort. The drivers won't stop for anyone."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then I'll stop the wagons."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("wagons_captain", "LEAVE THE REST",
                S("OFFICER", ShotCamera.Handheld, 2.8f, "OFFICER", "Drive! Leave the rest of them!", audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "No."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("wagons_end", "NOT PRISONS",
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "They all keep saying the same word."),
                S("RENZO", ShotCamera.Hold, 2f, "RENZO", "Pens."),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Not prisons. Pens. Like cattle."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we open the nearest one."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 33 BROKEN CHAINS
            Make("chains_open", "VERY QUIET",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Two pens. Two guard rotations."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "The second wakes when the first goes quiet."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then the first goes very quiet."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // Daigo joins the way he means to go on: asking for the gate.
            Make("chains_daigo", "THAT IS ALL I AM FOR",
                S("DAIGO", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("DAIGO", ShotCamera.PushIn, 2.8f, "DAIGO", "Cut the collar."),
                S("RENZO", ShotCamera.OverShoulder, 2.2f, "RENZO", "Who are you?"),
                S("DAIGO", ShotCamera.Hold, 3.4f, "DAIGO", "Daigo. Give me the gate. I'll hold it.", audio: ShotAudio.Sting),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "That's all I'm for. Don't waste it."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("chains_commander", "THE COMMANDER",
                S("", ShotCamera.Wide, 2.6f, audio: ShotAudio.Fire),
                S("SOLDIER", ShotCamera.Handheld, 2.4f, "SOLDIER", "The pens are burning! Get the commander!"),
                S("DAIGO", ShotCamera.PushIn, 2.2f, "DAIGO", "Let him come.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("chains_end", "NOT TOMORROW",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Fire),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "The execution ground is north. They start at dawn."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we don't wait for tomorrow."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 34 THE EXECUTION GROUND
            Make("exec_open", "HE WALKS THE LINE",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Bells),
                S("EXECUTIONER", ShotCamera.SlowDolly, 3.8f),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "Six on the platform. He's walking the line."),
                S("DAIGO", ShotCamera.Hold, 1.8f, "DAIGO", "Then run."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "I'm already running.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("exec_hold", "I HAVE THE STEPS",
                S("DAIGO", ShotCamera.Handheld, 2.8f, "DAIGO", "I have the steps. Get the rest of them loose.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The man himself, and the count he keeps.
            Make("exec_executioner", "SEVEN, NOW",
                S("EXECUTIONER", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("EXECUTIONER", ShotCamera.PushIn, 3.2f, "EXECUTIONER", "Six this morning. Seven, now."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "None. Ever again."),
                S("EXECUTIONER", ShotCamera.Hold, 1.8f, audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The game does not soften it.
            Make("exec_end", "NONE OF THEM IS HER",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Everyone lived."),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "None of them is her."),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "Goro moved her. The night before."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "Then his army knows where."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 35 GORO'S ARMY
            Make("army_open", "A REAL ARMY",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.MusicDark),
                S("OFFICER", ShotCamera.SlowDolly, 3.6f),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "Pikes in front. Bows behind. An officer calling the changes."),
                S("DAIGO", ShotCamera.Hold, 2.2f, "DAIGO", "A real army, then."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then we break the officer's voice."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // Why he counts his arrows.
            Make("army_tsuru", "MY OLD WALL",
                S("TSURU", ShotCamera.PushIn, 3.2f, audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "That's my old wall's banner."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "You can stay back."),
                S("TSURU", ShotCamera.Hold, 3.8f, "TSURU", "No. I counted every arrow I didn't fire at men like you."),
                S("TSURU", ShotCamera.PushIn, 2.8f, "TSURU", "I'll fire these.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            Make("army_officer", "CLOSE THE LINE",
                S("OFFICER", ShotCamera.Handheld, 2.8f, "OFFICER", "Close the line! Close it!", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("army_end", "KAGEHIRA WANTS TO TALK",
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Rain),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Alive. They were told to take me alive.", audio: ShotAudio.MusicSoft),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "And the seal on it is a serpent."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "An army that size needs a smith."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we find Goro's."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
