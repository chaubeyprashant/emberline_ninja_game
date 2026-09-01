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
