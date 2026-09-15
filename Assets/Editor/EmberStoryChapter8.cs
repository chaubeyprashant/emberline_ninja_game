using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 8, missions 71-75: the climb, the guns, the slide, the
    /// wall, and the drain. The mountain gets the wide shots; the first Iron
    /// Guard gets the orbit.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter8Beats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 71 THE MOUNTAIN ROAD
            // The consequence from the design layer: Nire will not climb.
            Make("ascent_open", "THE SNOW LINE",
                S("", ShotCamera.Wide, 4.4f, audio: ShotAudio.Snow),
                S("NIRE", ShotCamera.Hold, 3.2f, "NIRE", "This is the snow line. I don't go past it. Not for your father, not for you."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Then wait for us at the reed village."),
                S("NIRE", ShotCamera.PushIn, 3.2f, "NIRE", "I'll wait. The medicine won't be free when you come back down. Come back down."),
                S("RENZO", ShotCamera.SlowDolly, 3f),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("ascent_frozen", "THE MOUNTAIN IS WINNING",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.Snow),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Frozen at their posts. He sent them up faster than the mountain allows."),
                S("SOLDIER", ShotCamera.Handheld, 2.8f, "SOLDIER", "Someone on the road! Up, all of you — up, before the snow buries us!"),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "The snow first. Then me.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("ascent_end", "GUNS ON THE ROAD",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Artillery, laid on the road. Nobody gets up this mountain while they stand."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then they don't stand."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 72 THE FROZEN CAMP
            Make("guns_open", "RIDGE OR RAVINE",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Snow),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "I have the ridge. Anything I can see, I can drop."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "I have the ravine. Nobody watches the ravine. I used to be the one not watching it."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Fires in the magazine. Slow ones. Then we run."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("guns_alarm", "THEY SMELL THE SMOKE",
                S("", ShotCamera.Handheld, 3f, audio: ShotAudio.Bells),
                S("OFFICER", ShotCamera.Hold, 2.8f, "OFFICER", "Fire in the stores! Powder carriers — get it away from the magazine!"),
                S("SUZU", ShotCamera.Hold, 2.4f, "SUZU", "They're carrying it straight at you!"),
                S("RENZO", ShotCamera.PushIn, 2.2f, "RENZO", "Then they don't reach me.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("guns_end", "THE MOUNTAIN SHIVERS",
                S("", ShotCamera.PullOut, 4f, audio: ShotAudio.Fire),
                S("TSURU", ShotCamera.Hold, 2.8f, "TSURU", "That was the magazine. And that sound… that was the slope."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "The whole face above the road is loose."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then we have until it isn't."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 73 THE AVALANCHE
            Make("slide_open", "IT CRACKED",
                S("DAIGO", ShotCamera.Hold, 3f, audio: ShotAudio.Silence),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "Hear that? I heard it crack an hour ago. It's coming."),
                S("", ShotCamera.Wide, 2.6f, audio: ShotAudio.MusicImpact),
                S("RENZO", ShotCamera.Handheld, 2.2f, "RENZO", "Up! Run!"),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("slide_truce", "NOBODY'S ENEMY",
                S("SOLDIER", ShotCamera.Handheld, 3.2f, audio: ShotAudio.Snow),
                S("SOLDIER", ShotCamera.Hold, 3f, "SOLDIER", "Leave it! Leave him! Help me dig — my brother's under here!"),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "They've stopped fighting."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "The mountain doesn't take sides. Keep moving. It's not finished."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("slide_end", "OUT OF THE WHITE",
                S("", ShotCamera.PullOut, 4.4f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "There. Out of the white."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "The wall. There's no more road. Only that."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f, audio: ShotAudio.MusicDark));

            // ------------------------------------------------ 74 THE OUTER WALL
            Make("wall_open", "TAKE IT, HOLD IT",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Wind),
                S("DAIGO", ShotCamera.Hold, 2.8f, "DAIGO", "I take the gate. I've wanted a gate for a long time."),
                S("TOKU", ShotCamera.Hold, 2.8f, "TOKU", "Forge-yard. I want to see whose steel they're wearing."),
                S("TSURU", ShotCamera.Hold, 3f, "TSURU", "The wall is mine. I know it. I ran off it."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Then take it back.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // The consequence from the design layer: Tsuru on the wall he deserted.
            // He does not enjoy it, and the scene does not let anyone enjoy it.
            Make("wall_tsuru", "THE WALL HE RAN FROM",
                S("TSURU", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "I stood on this stair eight winters. That's my old post. He's wearing my old coat."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Tsuru."),
                S("TSURU", ShotCamera.PushIn, 2.8f, "TSURU", "Don't. Just hold the breach. They're coming back for it.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("wall_ironguard", "THE FIRST OF NINE",
                S("IRON GUARD", ShotCamera.Orbit, 4.2f, audio: ShotAudio.Sting),
                S("IRON GUARD", ShotCamera.Hold, 3.4f, "IRON GUARD", "Nine gates. Nine men like me. You have found the first."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "Then eight more will hear how this went."),
                S("IRON GUARD", ShotCamera.PushIn, 3f, "IRON GUARD", "Nothing behind this shield has ever heard anything.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("wall_end", "THE WALL IS OURS",
                S("TOKU", ShotCamera.PushIn, 3.6f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3.4f, "TOKU", "My mark. On his shield. Steel I made for Yorune's gate."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "The wall's plans. One drain marked 'silent'."),
                S("TOKU", ShotCamera.PushIn, 3f, "TOKU", "Go quiet, then. I'll be here, looking at my own work."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 75 THE SILENT GATE
            Make("drain_open", "WITH THE MELTWATER",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Silence),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "I go first. Step where I step. Breathe when the water's loud."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "And if they hear us?"),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "They won't. Nobody's ever heard me."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("drain_armoury", "EVERY BLADE HAD AN OWNER",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3.2f, "SUZU", "Renzo. That tag. That's my brother's name. On a blade in the rack."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "A blade in the rack isn't a body."),
                S("SUZU", ShotCamera.PushIn, 2.8f, "SUZU", "Then the tower. Quietly. Please.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("drain_end", "IT IS LIT",
                S("", ShotCamera.PushIn, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "One window lit, at the top of the tower."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Somebody's being kept awake up there."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Aiko."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));
        }
    }
}
