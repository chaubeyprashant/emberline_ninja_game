using Emberline.Core;
using Emberline.Story;
using UnityEditor;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Scenes for Chapter 4, missions 36-40: the smith, Kiba, Goro in the snow,
    /// her cell, and his end. Reveals get an orbit; the fight beats get a sting.
    /// </summary>
    public static partial class EmberStory
    {
        private static void BuildChapter4BBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            // ------------------------------------------------ 36 THE BLACKSMITH
            Make("smith_open", "AN ARMY NEEDS A SMITH",
                S("", ShotCamera.Wide, 3.8f, audio: ShotAudio.Birds),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "Smoke on the hill. That's a forge, not a fire."),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "Toku. He made the collar I wore. He didn't want to."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then we take him away from them."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The smith is not fast, and he does not pretend to be.
            Make("smith_toku", "I MARKED EVERY ONE",
                S("TOKU", ShotCamera.Orbit, 3.6f, audio: ShotAudio.Silence),
                S("TOKU", ShotCamera.Hold, 3.4f, "TOKU", "Every blade I made for them has a notch under the tang."),
                S("RENZO", ShotCamera.OverShoulder, 2.2f, "RENZO", "Why?"),
                S("TOKU", ShotCamera.PushIn, 3.6f, "TOKU", "So someone could tell my steel from an honest man's. Someday."),
                S("TOKU", ShotCamera.Hold, 2.8f, "TOKU", "I'm slow on the road. Don't wait for me. I'll keep up."),
                S("", ShotCamera.Hold, 0.5f, fadeAfter: true, blackAfter: 0.3f));

            Make("smith_riders", "RIDERS",
                S("", ShotCamera.Wide, 2.4f, audio: ShotAudio.MusicDark),
                S("SOLDIER", ShotCamera.Handheld, 2.6f, "SOLDIER", "The smith! Take him back, kill the rest!"),
                S("DAIGO", ShotCamera.PushIn, 2f, "DAIGO", "Toku. Behind me.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("smith_end", "A FORGE THAT IS MINE",
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Village),
                S("TOKU", ShotCamera.Hold, 3.2f, "TOKU", "Kiba has an anvil. It's mine now, if they'll have me.", audio: ShotAudio.MusicSoft),
                S("SUZU", ShotCamera.Hold, 3f, "SUZU", "They hid you for a month. They'll have you."),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "Goro will burn this place for it. You know that."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then we're still here when he tries."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 37 THE SIEGE
            Make("siege_open", "THREE GATES",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Bells),
                S("RENZO", ShotCamera.SlowDolly, 3.4f, audio: ShotAudio.Fire),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "Three gates, one wall, and it isn't much of a wall."),
                S("TOKU", ShotCamera.Hold, 3f, "TOKU", "The north gate will hold. I built that barricade myself."),
                S("DAIGO", ShotCamera.Hold, 2.2f, "DAIGO", "The west gate is mine."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "Then they'll come through the east. Nobody leaves the walls.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("siege_mid", "THE WEST GATE",
                S("", ShotCamera.Wide, 2.2f, audio: ShotAudio.Fire),
                S("DAIGO", ShotCamera.Handheld, 2.8f, "DAIGO", "Renzo! West gate! They're all at the west gate!"),
                S("RENZO", ShotCamera.PushIn, 2f, "RENZO", "Hold it. I'm coming.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("siege_end", "IT WAITS",
                S("", ShotCamera.PullOut, 4.2f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "His banner's still in the field. It hasn't moved all night."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "He's stopped sending men."),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "That's not a retreat. That's a man deciding to come himself."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Good. I'm done fighting his men."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 38 THE HUNTER RETURNS
            Make("hunt_open", "AGAINST ORDERS",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Snow),
                S("RENZO", ShotCamera.SlowDolly, 3.2f),
                S("DAIGO", ShotCamera.Hold, 3.2f, "DAIGO", "Kagehira wants you alive. Goro's coming anyway."),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Then he's coming for himself."),
                S("DAIGO", ShotCamera.PushIn, 3f, "DAIGO", "A man like that can't be beaten on the road. Only outrun."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The toll road, remembered by the other man. He comes out of the fog.
            Make("hunt_goro", "I REMEMBER THE ROAD",
                S("GORO", ShotCamera.Orbit, 4.2f, audio: ShotAudio.Silence),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "You left me on one knee, boy. On my own road."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Where is she?"),
                S("GORO", ShotCamera.PushIn, 3.4f, "GORO", "Run, and you'll find out. Stand, and you won't.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("hunt_ridge", "HE LET YOU RUN",
                S("GORO", ShotCamera.Wide, 3.6f, audio: ShotAudio.Wind),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "Not here. My gate, Kurogawa. Come to my gate."),
                S("DAIGO", ShotCamera.Hold, 2.6f, "DAIGO", "He's letting us go."),
                S("RENZO", ShotCamera.PushIn, 2.8f, "RENZO", "He wants it on his own ground. He'll have it."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("hunt_end", "THE MOUNTAIN GATE",
                S("", ShotCamera.PullOut, 3.6f, audio: ShotAudio.MusicSoft),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "His ground is the mountain gate. Aiko will be behind it."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then we burn it open."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 39 THE MOUNTAIN GATE
            Make("gate_open", "HIS LAST WALL",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Wind),
                S("TSURU", ShotCamera.Hold, 3.2f, "TSURU", "Archers on the wall. They'll shoot down at anything on the road."),
                S("SUZU", ShotCamera.Hold, 2.8f, "SUZU", "Toku's pitch is stacked inside the gate. He told me where."),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "Then the gate burns while they shoot.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            Make("gate_falls", "THE GATE FALLS",
                S("", ShotCamera.Handheld, 3.2f, audio: ShotAudio.Fire),
                S("OFFICER", ShotCamera.Hold, 2.4f, "OFFICER", "The gate! Hold the gatehouse!"),
                S("DAIGO", ShotCamera.PushIn, 2.2f, "DAIGO", "Through the smoke. Now.", audio: ShotAudio.MusicImpact),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // Her cell. For one shot she is standing in it, the way he remembers
            // her; then the cell is empty again. Proof she was here, not her.
            Make("gate_end", "HER THREAD",
                S("", ShotCamera.SlowDolly, 3.6f, audio: ShotAudio.Silence),
                S("AIKO", ShotCamera.PushIn, 2.6f, audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Her thread. She left it where I'd look."),
                S("SUZU", ShotCamera.Hold, 2.6f, "SUZU", "Days, Renzo. She was here days ago."),
                S("DAIGO", ShotCamera.Hold, 3f, "DAIGO", "He's in the yard. Alone. He's waiting for you."),
                S("RENZO", ShotCamera.PushIn, 2.4f, "RENZO", "Nobody follows me in."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.5f));

            // ------------------------------------------------ 40 GORO'S END
            Make("goro_open", "ALONE",
                S("", ShotCamera.Wide, 4.2f, audio: ShotAudio.Silence),
                S("GORO", ShotCamera.SlowDolly, 3.8f),
                S("GORO", ShotCamera.Hold, 3.4f, "GORO", "No guard. No wall. Just the two of us and the gate."),
                S("RENZO", ShotCamera.OverShoulder, 2.6f, "RENZO", "You took her deeper. Where?"),
                S("GORO", ShotCamera.PushIn, 3f, "GORO", "Beat me and I'll tell you. I mean it this time."),
                S("", ShotCamera.Hold, 0.6f, fadeAfter: true, blackAfter: 0.3f));

            // The catalogue's own two lines for this mission.
            Make("goro_twice", "NOBODY GETS ME TWICE",
                S("GORO", ShotCamera.Orbit, 3.8f, audio: ShotAudio.MusicDark),
                S("GORO", ShotCamera.PushIn, 3.4f, "GORO", "Twice, Kurogawa. Nobody gets me twice.", audio: ShotAudio.MusicImpact),
                S("RENZO", ShotCamera.Hold, 3.4f, "RENZO", "Then tell me where she is, and it will only be once."),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            Make("goro_pride", "WITHOUT HIS PRIDE",
                S("GORO", ShotCamera.Handheld, 3f, audio: ShotAudio.MusicDark),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "Enough. Archers! Put him down!"),
                S("RENZO", ShotCamera.PushIn, 2.6f, "RENZO", "You said it was just the two of us."),
                S("GORO", ShotCamera.Hold, 2.4f, "GORO", "I learned, boy.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.4f, fadeAfter: true, blackAfter: 0.2f));

            // He dies on his own gate, and he keeps his word.
            Make("goro_death", "UNDER THE MARSH",
                S("GORO", ShotCamera.Hold, 3.6f, audio: ShotAudio.Rain),
                S("GORO", ShotCamera.PushIn, 3.4f, "GORO", "The marsh. She's under it. A temple, under the water."),
                S("RENZO", ShotCamera.OverShoulder, 2.4f, "RENZO", "Whose orders?"),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "Whose do you think. He wanted her where you'd drown looking."),
                S("RENZO", ShotCamera.PushIn, 3.4f, "RENZO", "You have his eyes, you said. Look at them now."),
                S("", ShotCamera.PullOut, 3.4f, audio: ShotAudio.MusicSoft),
                S("", ShotCamera.Hold, 1f, card: "CHAPTER 4 — GORO'S TERRITORY", fadeAfter: true, blackAfter: 0.8f));
        }
    }
}
