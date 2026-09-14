using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 5, missions 46-50: the reed village, Nire, the ruin,
    /// the chamber, and the first key. The chapter ends on the catalogue's own
    /// two lines and a card.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter5BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 46 THE REED VILLAGE
            Make("reed_open", "NO SERPENT HERE",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Village),
                S("RENZO", ShotCamera.SlowDolly, 3.4f),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "Houses on stilts. No banner on the gate."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "The only place in the valley that never paid."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then they won't want us either."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("reed_refused", "THEY COME AT DUSK",
                S("NIRE", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "We chose the marsh over the serpent, boy. We didn't choose you."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "I'm not asking you to."),
                S("NIRE", ShotCamera.PushIn, 3.2f, "NIRE", "The shades come at dusk. Stand at the gate or don't. We'll see what you are."),
                S("", ShotCamera.Hold, 0.5f, card: "", fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicDark));

            Make("reed_guide", "SHE HAS BEEN THERE",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "The old woman. She's been to the temple and come back."),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "Nobody comes back from there."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "She did. Her house, before they change their minds."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("reed_end", "I DON'T HAVE TO LIKE IT",
                S("NIRE", ShotCamera.Hold, 3.4f, audio: ShotAudio.MusicSoft),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "You held the gate. Fine. I'll take you to the stair."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Thank you."),
                S("NIRE", ShotCamera.PushIn, 3.2f, "NIRE", "I agreed to take you. I didn't agree to like it."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 47 THE OLD GUIDE
            Make("guide_open", "SHE STOPS FOR NOTHING",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("NIRE", ShotCamera.SlowDolly, 3.4f),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "Keep up. The water doesn't wait, and neither do I."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Then don't stop."),
                S("NIRE", ShotCamera.PushIn, 2.8f, "NIRE", "I'll stop once. You'll know why when I do."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // She knew his father. This is where he stopped, ten years ago.
            Make("guide_father", "TEN YEARS AGO",
                S("NIRE", ShotCamera.Orbit, 3.8f, audio: ShotAudio.Silence),
                S("NIRE", ShotCamera.Hold, 3.6f, "NIRE", "A swordsman came this way, ten years back. Carrying something wrapped in cloth."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "My father."),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "He stopped here and cut that mark. He said his son might read it someday."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "He never said a word to me about it.", audio: ShotAudio.Sting),
                S("NIRE", ShotCamera.Hold, 2.8f, "NIRE", "No. He said it to the stone. Come on."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("guide_stair", "IT'S ALL BELOW",
                S("", ShotCamera.Wide, 3.2f, audio: ShotAudio.MusicDark),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "The stair. Everything in this marsh that doesn't want it climbed is behind us."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Then it can try.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("guide_end", "BELOW",
                S("NIRE", ShotCamera.Hold, 3.6f, audio: ShotAudio.Silence),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "This is where I sit. This is where I sat for him."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "You won't come down."),
                S("NIRE", ShotCamera.PushIn, 3.4f, "NIRE", "Below, boy. It's all below. I'll be here when you come up. If."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 48 BENEATH THE WATER
            Make("ruin_open", "UNDER THE MARSH",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 3.6f),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Stone stairs, under a marsh. Somebody built this before the water came."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "And the water's coming in behind me."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("ruin_guardian", "WHAT WAS LEFT HERE",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("SOLDIER", ShotCamera.Hold, 2.6f, "SOLDIER", "Nobody opens that door. Nobody. That's the order."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Whose order? He's been dead ten years.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("ruin_end", "FATHER'S MARK",
                S("RENZO", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "The symbol from his blade. Cut into the door."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "The fragment in my coat. It fits. He meant for me to have it."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 49 THE SEAL CHAMBER
            Make("chamber_open", "NOTHING HAS BEEN HERE",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 3.6f),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Nothing has been in here since him. Not even the water."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("chamber_dark", "IN THE DARK",
                S("", ShotCamera.Handheld, 2.8f, audio: ShotAudio.Sting),
                S("WHISPER", ShotCamera.Hold, 2.8f, "WHISPER", "…the key… put it back… put it BACK…"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "No.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("chamber_end", "ONE OF THREE",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Not a weapon. A lock with three keys, and he made the keys."),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "One of three. The journal will say where he hid the second."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 50 THE FIRST KEY
            Make("key_open", "THEY CAME FOR THE KEY",
                S("", ShotCamera.Wide, 3.4f, audio: ShotAudio.MusicDark),
                S("OFFICER", ShotCamera.Handheld, 3f, "OFFICER", "The Kurogawa has it! Take the key, leave the rest of him!"),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Come and take it.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("key_stair", "THE MARSH ABOVE",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Fire),
                S("SOLDIER", ShotCamera.Hold, 2.4f, "SOLDIER", "The stair's coming down! Hold him on it!"),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Then it comes down on you."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The catalogue's own two lines, and the chapter's last word.
            Make("key_end", "WHAT DID YOU DO",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.Rain),
                S("NIRE", ShotCamera.Hold, 2.8f, "NIRE", "You came up. He didn't tell me you would."),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "He hid it. He hid it and they burned the village for it.", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 3.4f, "RENZO", "Father. What did you do."),
                S("", ShotCamera.Hold, 2.6f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 5 — INTO THE MARSH", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
