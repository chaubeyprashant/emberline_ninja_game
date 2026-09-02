using System.Collections.Generic;
using System.IO;
using Emberline.Core;
using Emberline.Story;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Authors the story beats as assets under Resources/Story. Text lives here
    /// rather than in gameplay code, and re-running regenerates every beat, so a
    /// rewrite is an edit to this file plus one batch run.
    /// </summary>
    public static class EmberStory
    {
        [MenuItem("Emberline/Build Story")]
        public static void BuildStory()
        {
            Directory.CreateDirectory("Assets/Resources/Story");
            BuildOpening();
            BuildCampaignBeats();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Emberline] Story beats written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static StoryBeat Beat(string file)
        {
            var path = $"Assets/Resources/Story/{file}.asset";
            var b = AssetDatabase.LoadAssetAtPath<StoryBeat>(path);
            if (b == null)
            {
                b = ScriptableObject.CreateInstance<StoryBeat>();
                AssetDatabase.CreateAsset(b, path);
            }
            return b;
        }

        /// <summary>Shorthand so the scene list below reads like a shot list.</summary>
        private static StoryShot S(string subject, ShotCamera cam, float dur,
            string speaker = "", string line = "", ShotAudio audio = ShotAudio.Unchanged,
            float letterbox = 1f, string card = "", float blackAfter = 0f,
            EnvThemeId? theme = null, bool fadeAfter = false)
        {
            var s = new StoryShot
            {
                subject = subject, camera = cam, duration = dur,
                speaker = speaker, line = line, audio = audio,
                letterbox = letterbox, card = card, blackAfter = blackAfter,
                fadeOutAfter = fadeAfter,
            };
            if (theme.HasValue) { s.applyTheme = true; s.theme = theme.Value; }
            return s;
        }

        /// <summary>A shot that also re-dresses the village.</summary>
        private static StoryShot Set(SetState state, ShotCamera cam, float dur,
            string subject = "", string speaker = "", string line = "",
            ShotAudio audio = ShotAudio.Unchanged, float blackAfter = 0f)
        {
            var s = S(subject, cam, dur, speaker, line, audio, blackAfter: blackAfter);
            s.setState = state;
            return s;
        }

        /// <summary>
        /// The campaign's in-mission beats: the memories under the temple, Jin's
        /// mercy and warning, the reunion, the refusal, the dawn. Each is short —
        /// a beat inside a mission is a held breath, not a second opening.
        /// Subjects are cast names; a scene without the model gets a marked
        /// stand-in (see CastStandIn).
        /// </summary>
        private static void BuildCampaignBeats()
        {
            void Make(string id, string title, params StoryShot[] shots)
            {
                var b = Beat(id);
                b.id = id;
                b.title = title;
                b.shots = shots;
                EditorUtility.SetDirty(b);
            }

            Make("memory_lastnight", "THE LAST NIGHT",
                S("", ShotCamera.Hold, 2.5f, audio: ShotAudio.Wind, blackAfter: 1f, theme: EnvThemeId.VillageDawn),
                S("FATHER", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.Village),
                S("FATHER", ShotCamera.PushIn, 4f, "FATHER", "Not tonight, Ren. Tonight I need you to watch your sister."),
                S("AIKO", ShotCamera.Hold, 4f, "AIKO", "He is packing something. He never packs."),
                S("RENZO", ShotCamera.OverShoulder, 3.5f, "RENZO", "…I remember this. I remember all of it."),
                S("", ShotCamera.Wide, 3f, fadeAfter: true, blackAfter: 1.5f));

            Make("memory_burning", "THE BURNING VILLAGE",
                S("", ShotCamera.Wide, 3.5f, audio: ShotAudio.Bells, theme: EnvThemeId.BurningVillage),
                S("RENZO", ShotCamera.Handheld, 3.5f, "RENZO", "They were not taking anything. They were looking."),
                S("FATHER", ShotCamera.PushIn, 4f, "FATHER", "Behind me. Both of you. Do not open this door for anyone."),
                S("", ShotCamera.Hold, 2f, fadeAfter: true, blackAfter: 1f));

            Make("memory_aiko", "AIKO",
                S("AIKO", ShotCamera.Handheld, 4f, audio: ShotAudio.Fire, theme: EnvThemeId.BurningVillage),
                S("AIKO", ShotCamera.PushIn, 4.5f, "AIKO", "Under the shrine floor. He never thinks to look at home."),
                S("AIKO", ShotCamera.Hold, 3.5f, "AIKO", "When you're near, nothing bad can happen. …Ren, be near."),
                S("", ShotCamera.Hold, 2f, fadeAfter: true, blackAfter: 2f));

            Make("father_message", "THE TRUTH BENEATH YORUNE",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.MusicOff, theme: EnvThemeId.Temple),
                S("FATHER", ShotCamera.Hold, 5f, "FATHER", "He came for the Seal, not for us. I told him no. The village paid for my no."),
                S("FATHER", ShotCamera.Hold, 4.5f, "FATHER", "The man who drew him the map is called Kurogane. He was my friend."),
                S("RENZO", ShotCamera.OverShoulder, 3.5f, "RENZO", "Kurogane."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 1f));

            Make("jin_mercy", "NO HONOR",
                S("JIN", ShotCamera.OverShoulder, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.5f, "JIN", "Twice, Kurogawa. Twice I could have."),
                S("JIN", ShotCamera.PushIn, 4.5f, "JIN", "Go home. There is nothing up this mountain but me."),
                S("JIN", ShotCamera.PullOut, 3.5f, fadeAfter: true, blackAfter: 1f));

            Make("jin_confession", "THE CONFESSION",
                S("JIN", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Wind, theme: EnvThemeId.RainyBattlefield),
                S("JIN", ShotCamera.Hold, 5f, "JIN", "I gave him the map. I did not give him the village. He took that himself."),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "Then you watched."),
                S("JIN", ShotCamera.Hold, 4f, "JIN", "I watched. Then I stopped serving him. It was not enough. It will never be enough."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true));

            Make("jin_warning", "LAST WARNING",
                S("JIN", ShotCamera.PushIn, 4.5f, "JIN", "If you reach Kagehira, you may become him."),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "I am not him."),
                S("JIN", ShotCamera.Hold, 4.5f, "JIN", "Neither was he. Tomorrow, then. Properly."),
                S("", ShotCamera.PullOut, 2.5f, fadeAfter: true, blackAfter: 1f));

            Make("you_came", "YOU CAME",
                S("AIKO", ShotCamera.PullOut, 5f, audio: ShotAudio.MusicOff),
                S("AIKO", ShotCamera.PushIn, 5f, "AIKO", "You came."),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "I said I would."),
                S("AIKO", ShotCamera.Hold, 4f, "AIKO", "You were nine."),
                S("RENZO", ShotCamera.OverShoulder, 4.5f, fadeAfter: true, blackAfter: 1.5f));

            Make("long_night", "THE LONG NIGHT",
                S("AIKO", ShotCamera.SlowDolly, 4.5f, "AIKO", "He asked every day for ten years. Where is it. Where did you put it."),
                S("AIKO", ShotCamera.Hold, 4.5f, "AIKO", "It is under the shrine floor. He never thought to look at home."),
                S("RENZO", ShotCamera.PushIn, 3.5f, "RENZO", "Then we go home."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true));

            Make("father_final", "FATHER'S FINAL MESSAGE",
                S("", ShotCamera.Hold, 2f, audio: ShotAudio.MusicOff, blackAfter: 1f),
                S("FATHER", ShotCamera.PushIn, 5f, "FATHER", "If you are hearing this, I was wrong about how much time we had."),
                S("FATHER", ShotCamera.Hold, 5f, "FATHER", "The Seal is not a weapon. Do not let anyone make it one. Do not make it one yourselves."),
                S("FATHER", ShotCamera.Hold, 5f, "FATHER", "Whatever it is you are angry about when you hear this — be less."),
                S("AIKO", ShotCamera.OverShoulder, 4f, fadeAfter: true, blackAfter: 1.5f));

            Make("kagehira_truth", "KAGEHIRA'S TRUTH",
                S("KAGACHI", ShotCamera.PushIn, 5f, "KAGACHI", "Every village on this mountain drinks from one river. The Seal is the river."),
                S("KAGACHI", ShotCamera.Hold, 4.5f, "KAGACHI", "With it, I am order. Without it, I am a man with an army, and armies end."),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "You burned Yorune for a well."),
                S("KAGACHI", ShotCamera.PullOut, 4f, "KAGACHI", "I burned Yorune for every well. Open it, or I open it with your sister's hands."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true));

            Make("father_and_son", "FATHER AND SON",
                S("KAGACHI", ShotCamera.SlowDolly, 4.5f, "KAGACHI", "Your father and I carried the Seal together. Two keepers. One kept faith with the villages."),
                S("KAGACHI", ShotCamera.Hold, 4.5f, "KAGACHI", "The other kept faith with the future. One of us was right, boy."),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "He chose people."),
                S("KAGACHI", ShotCamera.Hold, 3.5f, "KAGACHI", "Let us find out which."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true));

            Make("lower_the_sword", "DON'T BECOME LIKE THEM",
                S("KAGACHI", ShotCamera.PushIn, 4f, audio: ShotAudio.MusicOff),
                S("RENZO", ShotCamera.OverShoulder, 4f),
                S("AIKO", ShotCamera.Hold, 4.5f, "AIKO", "Don't become like them."),
                S("RENZO", ShotCamera.Hold, 4.5f),
                S("KAGACHI", ShotCamera.Handheld, 3f, "KAGACHI", "…Weak."),
                S("", ShotCamera.Hold, 1f));

            Make("emberline_dawn", "EMBERLINE",
                S("", ShotCamera.Wide, 5f, audio: ShotAudio.Wind, theme: EnvThemeId.VillageDawn),
                S("AIKO", ShotCamera.SlowDolly, 5f, audio: ShotAudio.Birds),
                S("AIKO", ShotCamera.Hold, 4f, "AIKO", "Where will you go?"),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "Home."),
                S("AIKO", ShotCamera.Hold, 4f, "AIKO", "There is no home."),
                S("RENZO", ShotCamera.PushIn, 5f, "RENZO", "Then we'll build one."),
                S("", ShotCamera.Wide, 6f, fadeAfter: true, blackAfter: 4f, card: "EMBERLINE"));
        }

        private static void BuildOpening()
        {
            var b = Beat("opening");
            b.id = "opening";
            b.title = "THE LAST EMBER";

            var shots = new List<StoryShot>();

            // ---- SCENE 1 — PEACE ------------------------------------------
            // Black and wind first. Nothing is shown until the ear has settled,
            // so the cut to a warm morning has something to land against.
            shots.Add(Set(SetState.Peace, ShotCamera.Hold, 3.5f,
                audio: ShotAudio.Wind, blackAfter: 1.5f));
            shots.Add(S("FATHER", ShotCamera.SlowDolly, 5f, audio: ShotAudio.Birds));
            shots.Add(S("REN", ShotCamera.OverShoulder, 4.5f, "FATHER",
                "Again. Slower. The blade is not in a hurry."));
            shots.Add(S("AIKO", ShotCamera.PushIn, 4f, audio: ShotAudio.Village));
            // The line the whole game is built on. Held long, and alone.
            shots.Add(S("AIKO", ShotCamera.Hold, 5.5f, "AIKO",
                "When you're near, nothing bad can happen."));
            shots.Add(S("REN", ShotCamera.Hold, 3f, fadeAfter: true, blackAfter: 2f));

            // ---- SCENE 2 — THE ATTACK --------------------------------------
            // No violence on camera. Torches, bells, and the village answering.
            shots.Add(Set(SetState.Attack, ShotCamera.Wide, 4.5f,
                audio: ShotAudio.Bells));
            shots.Add(S("MOTHER", ShotCamera.Handheld, 3.5f, audio: ShotAudio.Fire));
            shots.Add(S("FATHER", ShotCamera.Hold, 4f, "KAGEHIRA",
                "The Black Seal. Say where it is, and this stops."));
            shots.Add(S("FATHER", ShotCamera.PushIn, 4.5f, "FATHER",
                "You will burn it all either way."));
            shots.Add(S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2.5f));

            // ---- SCENE 3 — REN RETURNS -------------------------------------
            shots.Add(Set(SetState.Ruin, ShotCamera.Wide, 6f,
                audio: ShotAudio.MusicOff));
            shots.Add(S("REN", ShotCamera.OverShoulder, 5f, audio: ShotAudio.Fire));
            // The sword in the dirt. Framed low and held — no line, no music.
            shots.Add(S("FATHER", ShotCamera.PushIn, 5.5f));
            shots.Add(S("REN", ShotCamera.Hold, 4.5f, "REN", "…Father."));
            shots.Add(S("REN", ShotCamera.PullOut, 5f, fadeAfter: true, blackAfter: 1.5f));

            b.shots = shots.ToArray();
            EditorUtility.SetDirty(b);
        }
    }
}
