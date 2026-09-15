using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 9, missions 86-90: the message, the meaning, the warlord,
    /// the siege and the door. Kagehira gets the orbit once, at 88, and never
    /// raises his voice. The chapter ends on the stair and a card.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter9BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 86 FATHER'S FINAL MESSAGE
            Make("final_open", "BOTH HANDS",
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.MusicOff),
                S("AIKO", ShotCamera.PushIn, 3.4f, "AIKO", "Put your hand on it with mine. He wrote 'both'."),
                S("RENZO", ShotCamera.OverShoulder, 3f),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f));

            Make("final_hand", "HE LETS HER",
                S("AIKO", ShotCamera.Hold, 3.6f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "Be less. He knew you'd need that one."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "He knew you wouldn't."),
                S("AIKO", ShotCamera.PushIn, 3.4f),
                S("RENZO", ShotCamera.Hold, 3f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("final_end", "WHAT IT IS NOT",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Wind),
                S("AIKO", ShotCamera.Hold, 3.2f, "AIKO", "He told us what it isn't. Not a weapon. Not ours to open angry."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "And what it is?"),
                S("AIKO", ShotCamera.PushIn, 3f, "AIKO", "That's carved on the chamber walls. Fumi can help me read them."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 87 THE MEANING OF THE SEAL
            Make("meaning_open", "THE CHAMBER IS THE BOOK",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "These columns. They're the same as the fortress rolls. Village, household, measure."),
                S("AIKO", ShotCamera.Hold, 2.8f, "AIKO", "Measure of what?"),
                S("FUMI", ShotCamera.PushIn, 2.6f, "FUMI", "Water."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The consequence from the design layer: Fumi will not burn the records.
            Make("meaning_fumi", "I WON'T BURN THEM",
                S("FUMI", ShotCamera.Orbit, 3.6f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "His vanguard is on the stair. Burn the records so he can't use them."),
                S("FUMI", ShotCamera.Hold, 3.6f, "FUMI", "No. I counted the rice that burned your village. I'm not burning the list of who drinks."),
                S("FUMI", ShotCamera.PushIn, 3f, "FUMI", "Hold the stair. I'll carry them. That's what they cost."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicImpact));

            Make("meaning_end", "A KEY TO THE WATER",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3.4f, "AIKO", "Not a weapon. A key. To the mountain's water, and every village under it."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "And he has the third key."),
                S("AIKO", ShotCamera.PushIn, 3.2f, "AIKO", "He's had it a year. He needs the door. The door needs one of us."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f, audio: ShotAudio.MusicDark));

            // ------------------------------------------------ 88 KAGEHIRA'S TRUTH
            Make("serpent88_open", "HE CAME HIMSELF",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "He walked through the mutineers' lines. Didn't draw. They knelt."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Stay with Aiko."),
                S("DAIGO", ShotCamera.PushIn, 2.8f, "DAIGO", "Renzo. Whatever he says out there. It's a sword."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("serpent88_arrives", "THE SERPENT",
                S("KAGACHI", ShotCamera.Orbit, 4.4f, audio: ShotAudio.Silence),
                S("KAGACHI", ShotCamera.Hold, 3.6f, "KAGACHI", "The Kurogawa boy. You have your father's eyes. I closed his."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "You took everything."),
                S("KAGACHI", ShotCamera.Hold, 3.4f, "KAGACHI", "Everything? You're standing in my yard with my prisoner and my keys. Fight my guard. Then we talk."),
                S("", ShotCamera.Hold, 0.4f, audio: ShotAudio.MusicImpact, fadeAfter: true, blackAfter: 0.2f));

            Make("serpent88_withdraws", "ONE NIGHT",
                S("KAGACHI", ShotCamera.Hold, 3.4f, audio: ShotAudio.Wind),
                S("KAGACHI", ShotCamera.Hold, 3.6f, "KAGACHI", "That is what I am without the Seal, boy. Imagine me with it."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "I don't have to."),
                S("KAGACHI", ShotCamera.Hold, 3.8f, "KAGACHI", "One night. My army comes up this mountain at dusk tomorrow. Open the door before, and they go home."),
                S("KAGACHI", ShotCamera.PullOut, 3f),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("serpent88_end", "PREPARE",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.MusicDark),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "You're bleeding in six places."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "He wasn't trying. Get Oba. Get everyone on the walls."),
                S("AIKO", ShotCamera.PushIn, 3f, "AIKO", "He's not coming with the army, Ren. He never goes where you expect."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 89 THE FINAL MARCH
            Make("march_open", "SHE CAME BACK",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Rain),
                S("SUZU", ShotCamera.SlowDolly, 3.2f),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Kanta's safe at the reed village. I came back up. Don't make a thing of it."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "I won't."),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "Torches all the way down the mountain. The whole valley's army."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicImpact));

            // The consequence from the design layer: Daigo holds the gate.
            Make("march_daigo", "THE GATE IS MINE",
                S("DAIGO", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Rain),
                S("DAIGO", ShotCamera.Hold, 3.4f, "DAIGO", "I told you at the pens I owed you. Here's the gate. I'll hold it."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Walk away from it when it's done."),
                S("DAIGO", ShotCamera.PushIn, 2.8f, "DAIGO", "If it's done. Go. Ladders on the east wall."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicDark));

            Make("march_inside", "HE IS NOT WITH IT",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("OBA", ShotCamera.Hold, 3f, "OBA", "Kurogawa! The chamber stair — the guards there are dead! Someone came in underneath!"),
                S("AIKO", ShotCamera.Hold, 2.2f, "AIKO", "Him."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "He went around his own army."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("march_end", "THE WALLS HELD",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Rain),
                S("TSURU", ShotCamera.Hold, 2.6f, "TSURU", "The walls held."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Where's Aiko? She was right behind Renzo—"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "The chamber. Move!"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicImpact));

            // ------------------------------------------------ 90 THE DOOR OPENS
            Make("opens_open", "THE RITUAL",
                S("", ShotCamera.Handheld, 3.2f, audio: ShotAudio.MusicDark),
                S("WHISPER", ShotCamera.Hold, 2.8f, "WHISPER", "…blood of the door… one of them is here… one of them is angry…"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Aiko!", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("opens_aiko", "HE ONLY NEEDED A LITTLE",
                S("AIKO", ShotCamera.Handheld, 3.2f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "I'm fine. My hand. He only needed a little blood. He didn't need me willing."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Where is he?"),
                S("AIKO", ShotCamera.PushIn, 2.8f, "AIKO", "At the door. Ren — the door is already turning."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicImpact));

            Make("opens_door", "IT OPENS",
                S("", ShotCamera.Wide, 3.2f, audio: ShotAudio.Sting),
                S("KAGACHI", ShotCamera.Hold, 3.4f, "KAGACHI", "Too late by a breath, Kurogawa. Your father was always too late by a breath."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Kagehira!"),
                S("KAGACHI", ShotCamera.PullOut, 3.4f, "KAGACHI", "The summit. Come and watch me turn it."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f, audio: ShotAudio.MusicDark));

            Make("opens_end", "THE SUMMIT ROAD",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.Wind),
                S("AIKO", ShotCamera.Hold, 3f, "AIKO", "All three keys. And the lock is at the summit."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Then that's the last road."),
                S("AIKO", ShotCamera.PushIn, 3.2f, "AIKO", "Be less, Ren. Whatever you find up there."),
                S("", ShotCamera.Hold, 2.4f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 9 — THE BLACK SEAL", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
