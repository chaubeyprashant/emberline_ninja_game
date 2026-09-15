using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 6, missions 56-60: Aiko's hour, her cell, the second
    /// key, the guardian, and the truth. The guardian gets the orbit and the
    /// sting; the message gets nothing but a held frame and the card.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter6BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 56 AIKO
            Make("aiko_open", "SHE DIDN'T STAY",
                S("AIKO", ShotCamera.Handheld, 3.6f, audio: ShotAudio.Fire),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "If they find Mama, they find this. So they won't find this."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "I was behind the door. I didn't follow her. I'm following her now."),
                S("AIKO", ShotCamera.PushIn, 2.8f, "AIKO", "Small paths. Soldiers don't know the small paths."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("aiko_end", "A THREAD SNAPPING",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("SEARCHER", ShotCamera.Hold, 2.8f, "SEARCHER", "Got her! The little one — where is it? Where did you put it?"),
                S("AIKO", ShotCamera.PushIn, 3.2f, "AIKO", "I don't know what you mean."),
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.6f, "RENZO", "She hid it. Ten years, and she's never told them where."),
                S("", ShotCamera.Hold, 1.2f, fadeAfter: true, blackAfter: 1f));

            // ------------------------------------------------ 57 THE PRISONER
            Make("prisoner_open", "YOU SAID HER NAME",
                S("NIRE", ShotCamera.Hold, 3.4f, audio: ShotAudio.Silence),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "You were gone a long time. You said her name, twice."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "She was held here. Under our feet, for years."),
                S("NIRE", ShotCamera.PushIn, 3f, "NIRE", "Then the walls will remember her. Walls always do."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("prisoner_wall", "HER HANDWRITING",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.MusicSoft),
                S("AIKO", ShotCamera.Hold, 4f, "AIKO", "Ren. If you're reading this, you remembered the cipher. The second key sleeps under the guardian's water."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Ten years in a cell, and she was still leaving me directions."),
                S("GUARD", ShotCamera.Handheld, 2.6f, "GUARD", "Someone's reading the walls! Lamps up — lamps up!", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("prisoner_end", "UNDER THE GUARDIAN",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("NIRE", ShotCamera.Hold, 3f, "NIRE", "The guardian. Your father asked me never to go near it."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Aiko's sending me to it."),
                S("NIRE", ShotCamera.PushIn, 3f, "NIRE", "Then one of them trusted you more than the other."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 58 THE SECOND KEY
            Make("second_open", "THEY MAKE THEM TRY IT",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.SlowDolly, 3.2f),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "There's a pen by the lock. They make the prisoners try it, one after another."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "It only opens for a Kurogawa."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "They don't know that. So they keep making them try."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The consequence from the design layer: the pen she could not open at 29.
            // The game does not remark on it, and neither does she, for long.
            Make("second_suzu", "THIS ONE OPENS",
                S("SUZU", ShotCamera.PushIn, 3.6f, audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Out. All of you. Up the gallery, stay low."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Suzu —"),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Don't. Get the key.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("second_end", "THE WAY DOWN",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Two keys. The floor went, and there's a stair under it."),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "That's where he set it. Whatever you hear down there, it isn't your father."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "It knows his name, though."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 59 THE DROWNED GUARDIAN
            Make("guardian_open", "WHAT HE SET HERE",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.SlowDolly, 3.4f),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "He told me what it would do. Keep the last of it from anyone. Anyone."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Including me."),
                S("NIRE", ShotCamera.PushIn, 3f, "NIRE", "Including his own children. He said that part twice."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("guardian_wakes", "WARDEN OF THE SECOND KEY",
                S("DROWNED GUARDIAN", ShotCamera.Orbit, 4f, audio: ShotAudio.Sting),
                S("DROWNED GUARDIAN", ShotCamera.Hold, 3.6f, "DROWNED GUARDIAN", "Kurogawa. Your father set me here. He did not say you would come."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "He didn't say a lot of things."),
                S("DROWNED GUARDIAN", ShotCamera.PushIn, 3f, "DROWNED GUARDIAN", "Then I will ask the water what you are.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("guardian_blood", "IT DOES NOT TIRE",
                S("DROWNED GUARDIAN", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("DROWNED GUARDIAN", ShotCamera.Hold, 3.2f, "DROWNED GUARDIAN", "You bleed like him. You do not yield like him."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "I'm not him.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("guardian_end", "NOT A KEY",
                S("DROWNED GUARDIAN", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("DROWNED GUARDIAN", ShotCamera.Hold, 3.4f, "DROWNED GUARDIAN", "Pass, then. He hoped you would."),
                S("", ShotCamera.Hold, 2f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "No key. Words. His hand, cut into the stone."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f, audio: ShotAudio.MusicSoft));

            // ------------------------------------------------ 60 THE TRUTH BENEATH YORUNE
            Make("truth_open", "THE QUIET",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3f, "TOKU", "Listen to that. Ten years this place has been screaming."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Go on, Renzo. We'll be at the stair."),
                S("RENZO", ShotCamera.SlowDolly, 3.4f),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("truth_changed", "HE STOPPED ASKING",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Kurogane. The crest on the hunters in the fog. Father's friend."),
                S("NIRE", ShotCamera.Hold, 3f, "NIRE", "You came down asking what happened."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "I know what happened."),
                S("NIRE", ShotCamera.PushIn, 3f, "NIRE", "That's what worries me.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("truth_end", "KUROGANE",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.Wind),
                S("TOKU", ShotCamera.Hold, 2.8f, "TOKU", "Well? What did he say?"),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Kagehira came for the Seal. Father said no. Jin Kurogane drew him the map."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Renzo. Where are you going?"),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "To find the man who drew the map."),
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 6 — THE DROWNED TEMPLE", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
