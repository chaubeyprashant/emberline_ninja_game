using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>Scenes for Chapter 3, missions 21-25: the forest that hunts back.</summary>
    public static partial class EmberStory
    {
        private static void BuildChapter3Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 21 THE HUNTER
            Make("hunter_open", "SOLDIERS TALK",
                S("SUZU", ShotCamera.Handheld, 3f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Something's following us."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Soldiers?"),
                S("SUZU", ShotCamera.PushIn, 2.6f, "SUZU", "Soldiers talk."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.4f));

            // It is not a person, and it does not pretend to be one.
            Make("hunter_shade", "OLDER THAN YOUR SERPENT",
                S("PALE SHADE", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.MusicDark),
                S("PALE SHADE", ShotCamera.Hold, 3.2f, "PALE SHADE", "…warm… this one is warm…"),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "What are you?"),
                S("PALE SHADE", ShotCamera.PushIn, 3.4f, "PALE SHADE", "…older than your serpent… and hungrier…"),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            Make("hunter_end", "IT WAS MEASURING ME",
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "It let us go.", audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "It was measuring me."),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Then next time it'll know your size."),
                S("RENZO", ShotCamera.PushIn, 3.2f, "RENZO", "Then we leave no trail for it to follow."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 22 NO FOOTPRINTS
            Make("nofoot_open", "THEY COUNT THEIR OWN",
                S("", ShotCamera.Wide, 3.4f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3.4f, "SUZU", "Twenty-eight in that camp. Kill one and they count to twenty-seven."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then nobody dies tonight."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("nofoot_end", "ONE PAGE",
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "Did you take anything?", audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "One page."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Aiko's name. Three weeks old. North, through a clearing."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Three weeks. You were never closer."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 23 BLOOD ON SNOW
            Make("snow_open", "FIRST SNOW",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Snow),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "First snow. Every step stays."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "So does everything else."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.4f));

            Make("snow_return", "LEAVE NOTHING",
                S("SOLDIER", ShotCamera.Handheld, 2.8f, "SOLDIER", "Burn the body. Leave nothing.", audio: ShotAudio.MusicDark),
                S("SUZU", ShotCamera.Hold, 2.2f, "SUZU", "They came back."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "For him. Not for me."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            // The mission's weight is one refusal, and Renzo hears his father in it.
            Make("snow_end", "HE REFUSED",
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "He wouldn't take a child north. So they killed him.", audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Three of them did it. Assassins."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "My father refused something too."),
                S("SUZU", ShotCamera.PushIn, 2.6f, "SUZU", "And they're on the road north."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 24 THE THREE BLADES
            Make("blades_open", "WE WERE TOLD YOUR NAME",
                S("BLADE", ShotCamera.SlowDolly, 3.4f, audio: ShotAudio.MusicDark),
                S("BLADE", ShotCamera.Hold, 2.2f, "BLADE", "Kurogawa."),
                S("BLADE", ShotCamera.Hold, 2.8f, "BLADE", "We were told your name. And your price."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Told by who?"),
                S("BLADE", ShotCamera.PushIn, 2.6f, "BLADE", "By the one who pays."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            Make("blades_last", "NOW I STOP BEING THEM",
                S("BLADE", ShotCamera.Handheld, 2.8f, audio: ShotAudio.Silence),
                S("BLADE", ShotCamera.Hold, 2.6f, "BLADE", "They were my sisters."),
                S("RENZO", ShotCamera.Hold, 2.2f, "RENZO", "Then stop."),
                S("BLADE", ShotCamera.PushIn, 3f, "BLADE", "We fought as three. Now I don't have to.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("blades_end", "SOMEONE ABOVE GORO",
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "They were sent for you. By name.", audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Someone above Goro is paying attention."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Then I'll go to whoever holds the cord."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 25 THE SILENT CAMP
            Make("camp_open", "NOT ONE BELL",
                S("", ShotCamera.Wide, 3.6f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3.4f, "SUZU", "Three fires. The stores, the archers' scaffold, and his tent."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "And not one bell."),
                S("", ShotCamera.Hold, 0.7f, fadeAfter: true, blackAfter: 0.3f));

            Make("camp_end", "IN THE WAY",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Fire),
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "Not one bell."),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "He was never hunting me. He was hunting the Seal."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "So you're just in the way."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then I'll be in the way properly."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
