using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>Scenes for Chapter 2, missions 16-20.</summary>
    public static partial class EmberStory
    {
        private static void BuildChapter2BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 16 WATCHFIRE
            Make("watch_open", "THE CAMP'S EYES",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Thirty-one men at the Broken Banner. That tower is their eyes."),
                S("FUMI", ShotCamera.Hold, 2.8f, "FUMI", "Take the eyes, and the camp is blind."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then I climb."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.4f));

            // The reversal is a decision, and Suzu thinks it's a bad one.
            Make("watch_hold", "LET THEM COME",
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "Their relief column. It'll be here before dark.", audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Light the fire."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "That tells them exactly where we are!"),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Good. For once they come to me."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("watch_end", "A FOURTH TERRITORY",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Fire),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "Three territories on their maps. And a fourth with only a serpent."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Someone will know which one matters."),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "A messenger left at dawn. Sealed pouch."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 17 THE MESSENGER
            Make("messenger_open", "THE BORDER",
                S("SUZU", ShotCamera.OverShoulder, 3.2f, audio: ShotAudio.Birds),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "He's heading for the border. If he crosses, we lose him."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "He won't cross."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("messenger_turn", "HE STOPS RUNNING",
                S("RUNNER", ShotCamera.Handheld, 2.8f, audio: ShotAudio.MusicDark),
                S("RUNNER", ShotCamera.Hold, 2.6f, "RUNNER", "You'll never read it."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Then you won't mind giving it to me."),
                S("RUNNER", ShotCamera.PushIn, 2.2f, "RUNNER", "Come and take it."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            Make("messenger_end", "A CIPHER",
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Can you read it?", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "No."),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "But whoever he reported to can. His relay post has the key."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 18 DEAD LETTER
            Make("letter_open", "THREE PIECES",
                S("FUMI", ShotCamera.SlowDolly, 3.4f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "Every cipher they send passes through that post."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Then the key's inside."),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "In three pieces. They're not stupid. Just careful."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.4f));

            // The chapter's first gut-punch, read off a page.
            Make("letter_decode", "FIND THE DAUGHTER",
                S("FUMI", ShotCamera.PushIn, 3f, "FUMI", "Give me that. You're holding it upside down.", audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "\"Find the daughter.\""),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "\"She knows where he hid it.\"", audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "There's only one daughter he could mean."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("letter_end", "WHERE HE LOOKED",
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "He's been hunting her. For years.", audio: ShotAudio.MusicSoft),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "Then his prisoner records will say where he looked."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 19 THE DAUGHTER
            Make("daughter_open", "ONE DOOR",
                S("SUZU", ShotCamera.OverShoulder, 3f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "The records room has one door. You won't leave it quietly."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then it's quiet until it isn't."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("daughter_alarm", "THE RECORDS ROOM",
                S("SOLDIER", ShotCamera.Handheld, 2.4f, "SOLDIER", "Someone's in the records room!", audio: ShotAudio.Bells),
                S("SUZU", ShotCamera.Hold, 2.2f, "SUZU", "That's us. Run."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // Fumi's hand is in those rolls, and she says nothing — the line is
            // true and it is not the whole truth.
            Make("daughter_end", "WHERE SHE WAS HELD",
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Six years ago she was alive.", audio: ShotAudio.MusicSoft),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "It says where she was held. Not where she is."),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "Every transfer order passes through their second signal tower."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then that tower has hers."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 20 THE SECOND LANTERN
            Make("lantern2_open", "LIGHT ONE, THE OTHER WAKES",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Two towers. Light one and the other wakes up."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Then I choose which one wakes."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("lantern2_squad", "KEEP IT STANDING",
                S("OFFICER", ShotCamera.SlowDolly, 3f, audio: ShotAudio.MusicDark),
                S("OFFICER", ShotCamera.Hold, 3f, "OFFICER", "Keep that tower standing. Kill whoever lit the first."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.2f));

            // The chapter's last line belongs to the enemy.
            Make("lantern2_end", "KUROGAWA IS COMING",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Fire),
                S("RUNNER", ShotCamera.Handheld, 3f, "RUNNER", "Word for the Serpent! Kurogawa is coming!"),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "They know your name now."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "They always did. Now they know where I am."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Let him come looking.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 2 — THE LANTERN NETWORK", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
