using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 7, missions 66-70: the champion, the mask, the
    /// confession, the warning, and Jin's death. The chapter ends on his last
    /// line and a card.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter7BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 66 THE DUELIST
            Make("duelist_open", "TOKU'S STEEL",
                S("TOKU", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.Village),
                S("TOKU", ShotCamera.Hold, 3.4f, "TOKU", "Finished it last night. Your father's mark on the tang, where only the blade can see it."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Will it hold?"),
                S("TOKU", ShotCamera.PushIn, 3f, "TOKU", "It'll hold longer than you. That's the most any smith can promise."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("duelist_hoshu", "THE INNER GATE",
                S("COMMANDER HOSHU", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.6f, "COMMANDER HOSHU", "Jin and I learned the sword in the same yard. He asked me to see if you are worth his time."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "And you came."),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.2f, "COMMANDER HOSHU", "I keep Kagehira's inner gate. I wanted to see the Kurogawa who might reach it."),
                S("COMMANDER HOSHU", ShotCamera.PushIn, 2.4f, "COMMANDER HOSHU", "Draw.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("duelist_respect", "PROPERLY",
                S("COMMANDER HOSHU", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3f, "COMMANDER HOSHU", "Good. You don't fight angry. Not yet."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Stop holding the door, then.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("duelist_end", "HE WAS WATCHING",
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.4f, audio: ShotAudio.Silence),
                S("COMMANDER HOSHU", ShotCamera.Hold, 3.2f, "COMMANDER HOSHU", "I yield. Tell Jin he was right about you."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Right about what?"),
                S("COMMANDER HOSHU", ShotCamera.PushIn, 3.4f, "COMMANDER HOSHU", "Ask him. And when you reach my gate, Kurogawa, I will not yield twice."),
                S("", ShotCamera.PullOut, 3f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "A message. A place, a time. No guards."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 67 THE BROKEN MASK
            Make("mask_open", "SOMEBODY KEEPS IT CLEAN",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "Ten years empty, and no dust. Somebody comes here."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "He does."),
                S("FUMI", ShotCamera.PushIn, 2.8f, "FUMI", "Then read it before someone decides you shouldn't."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("mask_fire", "BEFORE YOU CAN READ IT",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Fire),
                S("OFFICER", ShotCamera.Hold, 2.8f, "OFFICER", "Burn it! The warlord wants nothing of Kurogane's left standing!"),
                S("FUMI", ShotCamera.Hold, 2.4f, "FUMI", "They're not after us. They're after the house."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Then they go through us.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("mask_end", "THE OTHER HALF",
                S("RENZO", ShotCamera.PushIn, 3.8f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "He was from Yorune. My father wrote to him. 'Come home.'"),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "He didn't."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "He's been wearing the other half of this for ten years."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f, audio: ShotAudio.MusicSoft));

            // ------------------------------------------------ 68 THE CONFESSION
            // The consequence from the design layer: they refuse to attack tonight.
            Make("confess_refused", "NOT TONIGHT",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Rain),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "It's a trap, and you want it to be one. We're not going tonight."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "Morning. With all of us. That's the offer."),
                S("NIRE", ShotCamera.Hold, 3f, "NIRE", "Go alone, boy, and you'll come back with his answers and none of your own."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "He said no guards. I'm going alone."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("confess_after", "BACK TO BACK",
                S("JIN", ShotCamera.Hold, 3.2f, audio: ShotAudio.Rain),
                S("JIN", ShotCamera.Hold, 3f, "JIN", "Kagehira's knives. For both of us. He knows I talked."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "You fought beside me."),
                S("JIN", ShotCamera.PushIn, 3.4f, "JIN", "Tonight. I have one more thing to say, and I only say it with a sword in my hand."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("confess_end", "THE MAP",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Rain),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "He drew the map. He didn't light the fires. He's hated himself for the difference."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "And you? What do you hate?"),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "I haven't decided."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 69 LAST WARNING
            // The consequence from the design layer: Nire says it before Jin does.
            Make("warning_nire", "SHE SAYS IT FIRST",
                S("NIRE", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.Wind),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "I knew your father, and I knew the boy who drew that map. Both of them."),
                S("NIRE", ShotCamera.Hold, 3.4f, "NIRE", "You walk like the one who wanted to win more than he wanted to be right."),
                S("RENZO", ShotCamera.Hold, 2.4f),
                S("NIRE", ShotCamera.PushIn, 2.8f, "NIRE", "Don't answer me. Answer him."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("warning_rage", "YOUR OWN RAGE",
                S("JIN", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("JIN", ShotCamera.Hold, 3.2f, "JIN", "You broke them because you were angry. Now watch what I do with your anger."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Stop teaching me."),
                S("JIN", ShotCamera.Hold, 2.6f, "JIN", "Stop needing it.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("warning_end", "TOMORROW",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("NIRE", ShotCamera.Hold, 2.8f, "NIRE", "Well?"),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "He says I'll become Kagehira. You said it first."),
                S("NIRE", ShotCamera.PushIn, 3f, "NIRE", "Then prove us both wrong tomorrow, and come back down the mountain."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 70 KUROGANE
            Make("kurogane_open", "NO GUARDS",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Rain),
                S("JIN", ShotCamera.SlowDolly, 3.6f),
                S("JIN", ShotCamera.Hold, 3.2f, "JIN", "You came. Good. I did not want to die in bed."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "You could walk away."),
                S("JIN", ShotCamera.PushIn, 3f, "JIN", "I did that once. It burned a village."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("kurogane_draw", "PROPERLY",
                S("JIN", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("JIN", ShotCamera.Hold, 3f, "JIN", "No lessons today, Kurogawa. Today I try to kill you."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Finally."),
                S("", ShotCamera.Hold, 0.5f, audio: ShotAudio.MusicImpact, fadeAfter: true, blackAfter: 0.2f));

            Make("kurogane_storm", "THE STORM",
                S("JIN", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("JIN", ShotCamera.Hold, 2.8f, "JIN", "You changed. He never did. Now see the storm."),
                S("", ShotCamera.Hold, 0.4f, audio: ShotAudio.MusicDark, fadeAfter: true, blackAfter: 0.2f));

            Make("kurogane_breath", "ONLY THE TWO OF US",
                S("JIN", ShotCamera.Hold, 3.4f, audio: ShotAudio.Silence),
                S("JIN", ShotCamera.Hold, 3f, "JIN", "…breathe, Kurogawa. One more. We both have one more."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "One.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("kurogane_death", "DO NOT BECOME HIM",
                S("JIN", ShotCamera.PushIn, 4f, audio: ShotAudio.Rain),
                S("JIN", ShotCamera.Hold, 3.6f, "JIN", "Aiko is alive. Kagehira keeps her inside the mountain fortress."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Why tell me now?"),
                S("JIN", ShotCamera.Hold, 3.8f, "JIN", "Because you won honestly. And because I never went home."),
                S("JIN", ShotCamera.PushIn, 3.8f, "JIN", "Do not become him.", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "The mountain, then."),
                S("", ShotCamera.Hold, 2.6f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 7 — KUROGANE", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
