using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 5, missions 41-45: back into the fog. The marsh gets the
    /// wide shots; the things in it get the orbit, and the voices get silence.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter5Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 41 THE DROWNED ROAD
            Make("road41_open", "UNDER THE MARSH",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Rain),
                S("RENZO", ShotCamera.SlowDolly, 3.6f),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "Under the marsh, he said. He didn't say the marsh would be on top of us."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "The causeway floods twice a night. We cross between."),
                S("DAIGO", ShotCamera.PushIn, 2.4f, "DAIGO", "And if we're slow?"),
                S("RENZO", ShotCamera.Hold, 2f, "RENZO", "We aren't."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("road41_voices", "SOMEBODY IS CALLING",
                S("", ShotCamera.PullOut, 3.8f, audio: ShotAudio.Silence),
                S("WHISPER", ShotCamera.Hold, 3f, "WHISPER", "…here… we're here… bring the light…"),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "That isn't a soldier."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "No. The tide is. Move.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("road41_end", "NOTHING TAKEN BUT LIGHT",
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "A hundred carts. A hundred lanterns gone, and nothing else touched."),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "Somebody's collecting light. Out there, where the voices are."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then that's where we go."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 42 VOICES IN THE FOG
            Make("fog_open", "THEY MOVE",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("WHISPER", ShotCamera.Hold, 2.8f, "WHISPER", "…this way… no… this way…"),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "They move. Every time I mark one, it's somewhere else."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we mark where they were, and walk the line between."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("fog_camp", "A CAMP THE MARSH TOOK",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.MusicDark),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Prisoners. Chained to posts the water's already over."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "They were calling for light."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Something answered them. It's answering us now.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("fog_end", "UNDER THE LINE",
                S("", ShotCamera.SlowDolly, 3.8f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "The records are under the water line. The whole camp is."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then the answers are under it too."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Renzo. Something's been keeping the lanterns lit down there."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 43 THE SUNKEN CAMP
            Make("sunken_open", "STAY OUT OF THE DEEP",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "Search the shallows. Stay out of the deep."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Why?"),
                S("DAIGO", ShotCamera.PushIn, 2.6f, "DAIGO", "Because I'm asking."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The debt, said once. He never says it again, including at the gate.
            Make("sunken_daigo", "WHAT I OWE",
                S("DAIGO", ShotCamera.Orbit, 4f, audio: ShotAudio.Silence),
                S("DAIGO", ShotCamera.Hold, 3.6f, "DAIGO", "They put me in the pens because I wouldn't drown a girl in a place like this."),
                S("DAIGO", ShotCamera.Hold, 3.4f, "DAIGO", "I held the door. Somebody else did it. I still hear the water."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Daigo."),
                S("DAIGO", ShotCamera.PushIn, 3f, "DAIGO", "That's what I owe. I'm saying it once. Search the shallows."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The thing in the deep water, seen once and not fought. Not yet.
            Make("sunken_deep", "IT WAS WATCHING",
                S("", ShotCamera.Wide, 3.2f, audio: ShotAudio.Silence),
                S("DROWNED GUARDIAN", ShotCamera.Orbit, 4.2f, audio: ShotAudio.Sting),
                S("DAIGO", ShotCamera.Handheld, 2.4f, "DAIGO", "Out of the water. NOW."),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Go!", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("sunken_end", "A LOCK",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "\"The daughter, to the temple. For the key.\" She isn't a prisoner."),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "Then what is she?"),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "A lock. And somebody wants what she opens."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 44 MARSH HUNTERS
            Make("hunters_open", "SIX SETS OF PRINTS",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "Six sets of prints behind us. Then I stopped counting."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Goro's dead."),
                S("TSURU", ShotCamera.PushIn, 3f, "TSURU", "These aren't Goro's. They walk like they're paid by the head.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("hunters_tsuru", "TURN IT",
                S("TSURU", ShotCamera.Handheld, 2.8f, "TSURU", "They can't see either. The fog's ours as much as theirs.", audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then we stop being hunted.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("hunters_end", "KUROGANE",
                S("RENZO", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Kurogane. Father said that name once, at the door."),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "You know it?"),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "No. But it stings, and I don't know why."),
                S("TSURU", ShotCamera.PushIn, 3f, "TSURU", "A patrol went into the fog ahead of us. It never came out."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 45 THE MISSING PATROL
            Make("patrol_open", "EVEN THEY ARE AFRAID",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Twelve men. Full kit. They went in here."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Even the enemy's afraid of this place."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Then we find out what they're afraid of before it finds us."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("patrol_last", "THE MARSH'S OWN",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Silence),
                S("WHISPER", ShotCamera.Hold, 2.6f, "WHISPER", "…no warlord… no serpent… ours…"),
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "They don't answer to anyone."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Then neither do we.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // Suzu takes the long way round and finds the village first. She is proud of it.
            Make("patrol_end", "REED SMOKE",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Wind),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Smoke. Reed smoke, over there. I went round while you were being dramatic."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Somebody lives out here."),
                S("SUZU", ShotCamera.PushIn, 3f, "SUZU", "A whole village. And they'll know the way to the temple.", audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
