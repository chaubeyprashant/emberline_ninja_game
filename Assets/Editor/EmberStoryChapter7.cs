using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 7, missions 61-65: Jin in the rain, the rooftops, the
    /// yard where Renzo loses, the garrison that loved him, and the men he trained.
    /// Jin is always framed still; everyone else moves around him.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter7Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 61 THE BLACK BLADE
            Make("blade_open", "THE MAN WHO DREW THE MAP",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Rain),
                S("RENZO", ShotCamera.SlowDolly, 3.4f),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "The crest from the fog, on every third door. They're not hiding him."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "They're proud of him."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("blade_jin", "THE STORM BLADE",
                S("JIN", ShotCamera.Orbit, 4.2f, audio: ShotAudio.Silence),
                S("JIN", ShotCamera.Hold, 3.4f, "JIN", "Kurogawa. You have your father's stance."),
                S("RENZO", ShotCamera.OverShoulder, 3.2f, "RENZO", "You are the second man to say that. The first is dead."),
                S("JIN", ShotCamera.Hold, 2.8f, "JIN", "The first was not me."),
                S("JIN", ShotCamera.PushIn, 3.2f, "JIN", "Renzo. Aiko. Daisuke. Come, then. Show me what he left.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("blade_end", "NOT YET",
                S("JIN", ShotCamera.Hold, 3.4f, audio: ShotAudio.Rain),
                S("JIN", ShotCamera.Hold, 3f, "JIN", "You cut me. Good. Not yet, though."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Stand still and say that."),
                S("", ShotCamera.PullOut, 3f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "The roofs. He's not running. He's walking."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.4f));

            // ------------------------------------------------ 62 THE PURSUIT
            Make("pursuit_open", "OVER THE ROOFS",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Rain),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "I'm on the roofline. He's two streets ahead, and he keeps looking back."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "To see if I'm still coming."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "To see if you're keeping up.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("pursuit_suzu", "HE'S CHOOSING THE WAY",
                S("SUZU", ShotCamera.Handheld, 3.2f, audio: ShotAudio.Rain),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Renzo, stop. Every turn he takes, he takes toward the castle road."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then he wants me on the castle road."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "And you're going to give him what he wants."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "This once."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("pursuit_end", "THE FAR SIDE",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Silence),
                S("JIN", ShotCamera.Hold, 3.2f, "JIN", "You made the jump. Most of my men would not have."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Draw."),
                S("JIN", ShotCamera.PushIn, 3.4f, "JIN", "Tomorrow night. The castle yard. I will draw a ring for you."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 63 NO HONOR
            Make("nohonor_open", "THE RING",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("JIN", ShotCamera.SlowDolly, 3.4f),
                S("JIN", ShotCamera.Hold, 3.2f, "JIN", "Step in. When you are good enough to make me draw, I will."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "And if I'm not?"),
                S("JIN", ShotCamera.PushIn, 2.8f, "JIN", "Then you will learn that too.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("nohonor_draw", "HE DREW",
                S("JIN", ShotCamera.Handheld, 3f, audio: ShotAudio.Sting),
                S("JIN", ShotCamera.Hold, 3f, "JIN", "There. I drew. Now you get to see why nobody wants me to."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Come on, then.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("nohonor_end", "GO HOME",
                S("RENZO", ShotCamera.Hold, 3.6f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "He could have. Twice."),
                S("RENZO", ShotCamera.PushIn, 3.4f, "RENZO", "He wanted me to know he could. Why?"),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.8f, audio: ShotAudio.MusicSoft));

            // ------------------------------------------------ 64 THE FALLEN SOLDIER
            Make("fallen_open", "A HERO HERE",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Village),
                S("FUMI", ShotCamera.Hold, 3.2f, "FUMI", "Ask anyone here about Jin Kurogane and they stand up straighter."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then I'll ask them sitting down."),
                S("FUMI", ShotCamera.PushIn, 3f, "FUMI", "A garrison keeps ledgers. Ledgers keep everything. Let me find the room."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The consequence from the design layer: Fumi's own hand in the ledger.
            Make("fallen_ledger", "HER HAND",
                S("FUMI", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("FUMI", ShotCamera.Hold, 3.4f, "FUMI", "Renzo. This entry. The supply count for Yorune's road, that autumn. That's my hand."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You counted the carts that burned my village."),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "I counted rice. I didn't know what it was for."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Nobody ever does.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("fallen_end", "BEHIND MY FATHER",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "In the portrait he's standing behind my father. Like a student."),
                S("FUMI", ShotCamera.Hold, 3f, "FUMI", "His old unit will know how. They're quartered in the castle."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then I'll ask them, too."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 65 KUROGANE'S MEN
            Make("men_open", "THE UNIT HE TRAINED",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "I take the gate. Loud. You go where they aren't looking."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "I take the wall. Anything on a ladder is mine."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "They were all at Yorune. Leave the captain for me."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f, audio: ShotAudio.MusicImpact));

            Make("men_ryo", "JIN'S SECOND",
                S("RYO", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("RYO", ShotCamera.Hold, 3.2f, "RYO", "Kurogawa. You fight like the man in the portrait. We all heard about you."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "You were at Yorune."),
                S("RYO", ShotCamera.PushIn, 3f, "RYO", "Every one of us. Hold the yard, if you want to ask me about it.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("men_end", "HE TRIED TO STOP IT",
                S("RYO", ShotCamera.Hold, 3.6f, audio: ShotAudio.Silence),
                S("RYO", ShotCamera.Hold, 3f, "RYO", "Kill me if you want. I won't say anything else."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "Say one thing."),
                S("RYO", ShotCamera.PushIn, 3.6f, "RYO", "He tried to stop it. He was too late, and he's never forgiven anyone. Least of all himself."),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "A man who fights honest can be asked honest."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then I'll ask him. With terms."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
