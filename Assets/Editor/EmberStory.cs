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

            // ---- MISSION 6 — THE HOUSE OF KAWAI ------------------------------
            // The mission after the boss fight, and deliberately quiet. Renzo has
            // spent five missions thinking of his father as the man who failed to
            // save Yorune; he leaves this one knowing his father spent the last
            // year of his life trying to get everyone out. Nothing is explained:
            // the man he refuses is never shown, the sentence about the mountain
            // never finishes, and the red thread is a thing he lost, not a lead.

            Make("kawai_open", "FATHER CAME THIS WAY",
                S("RENZO", ShotCamera.OverShoulder, 4f, audio: ShotAudio.Wind,
                    theme: EnvThemeId.VillageDawn),
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Birds),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "Father came this way."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            // The refusal, from the outside. Whoever he is talking to is never in
            // frame and is never named — the scene exists to make the player ask.
            Make("kawai_father", "THEY ALREADY WILL",
                S("", ShotCamera.Hold, 2.5f, audio: ShotAudio.Silence, blackAfter: 0.8f),
                S("FATHER", ShotCamera.Handheld, 4f, audio: ShotAudio.Village,
                    theme: EnvThemeId.VillageDawn),
                S("", ShotCamera.Hold, 3f, "VISITOR", "You know what happens if you refuse."),
                S("FATHER", ShotCamera.Hold, 2.6f, "FATHER", "I know."),
                S("", ShotCamera.Hold, 2.6f, "VISITOR", "Then open it."),
                S("FATHER", ShotCamera.PushIn, 3f, "FATHER", "No."),
                S("", ShotCamera.Hold, 2.8f, "VISITOR", "People will die."),
                S("FATHER", ShotCamera.Hold, 3.6f, "FATHER", "They already will if I do."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 1.2f),
                S("RENZO", ShotCamera.Hold, 3.8f, "RENZO", "Who were you talking to?",
                    audio: ShotAudio.Wind));

            // The thread. A promise he did not keep, and nothing more than that —
            // no suggestion she lived, because mission 9 needs that to be new.
            Make("kawai_thread", "TWO THINGS",
                S("RENZO", ShotCamera.PushIn, 3.5f, audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 2.2f, blackAfter: 0.6f),
                S("AIKO", ShotCamera.Handheld, 3f, "AIKO", "When you come back, will you bring me something?",
                    audio: ShotAudio.Birds, theme: EnvThemeId.VillageDawn),
                S("", ShotCamera.Hold, 2.2f, "RENZO", "What?"),
                S("AIKO", ShotCamera.Hold, 2.6f, "AIKO", "Something from outside."),
                S("", ShotCamera.Hold, 2.6f, "RENZO", "I'll bring you two things."),
                S("AIKO", ShotCamera.Hold, 2.4f, "AIKO", "Promise?"),
                S("", ShotCamera.Hold, 2.6f, "RENZO", "Promise."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 1.5f),
                S("RENZO", ShotCamera.Hold, 4.5f, audio: ShotAudio.Silence));

            // His father's hand, and it stops mid-sentence.
            Make("kawai_letter", "DO NOT FOLLOW THE PATH THEY OFFER",
                S("RENZO", ShotCamera.PushIn, 3.5f, audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 3.4f, "FATHER", "Renzo. If you ever find this, then I failed to keep you away."),
                S("", ShotCamera.Hold, 3.4f, "FATHER", "There are things a son should never have to carry."),
                S("", ShotCamera.Hold, 3.2f, "FATHER", "I chose to protect this village."),
                S("", ShotCamera.Hold, 3.6f, "FATHER", "Whatever happens, do not follow the path they offer you."),
                S("", ShotCamera.Hold, 3.2f, "FATHER", "The mountain must remain—"),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "Remain what?", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 1f));

            Make("kawai_end", "WHAT DID YOU LEAVE BEHIND",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Wind,
                    theme: EnvThemeId.VillageDawn),
                S("RENZO", ShotCamera.PushIn, 3.8f, "RENZO", "You were protecting them."),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "But from what?"),
                S("", ShotCamera.Wide, 4.5f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "What did you leave behind?"),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f,
                    card: "THE HOUSE OF KAWAI"));

            // ---- MISSION 5 — THE TOLL-CAPTAIN --------------------------------
            // The first real answer, and it is a small one: Renzo's father knew
            // what was coming and refused something. Goro will not say what, and
            // he dies without saying it. What he does give away is that he was
            // never the top of anything — "not mine" is the line the mission is
            // built around.

            Make("toll_open", "A TOLL ROAD",
                S("RENZO", ShotCamera.PushIn, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "A toll road."),
                S("RENZO", ShotCamera.OverShoulder, 4f, "RENZO", "That's where he'll be."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            // He is not surprised, and he does not hurry. That is the character.
            Make("toll_confront", "YOU SHOULD HAVE STAYED AWAY",
                S("", ShotCamera.Hold, 2.5f, "SOLDIER", "Toll-Captain!", audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 2.2f, "GORO", "I know."),
                S("", ShotCamera.Hold, 2.5f, "SOLDIER", "The Kurogawa is here."),
                S("", ShotCamera.Hold, 3f, "GORO", "Then stop wasting my time."),
                S("GORO", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.MusicDark),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "You should have stayed away."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "You knew my father."),
                S("GORO", ShotCamera.Hold, 2.6f, "GORO", "I did."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "What did he do?"),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "What he believed was right."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "That's not an answer."),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "It's the only one you're getting."),
                S("RENZO", ShotCamera.PushIn, 3f, "RENZO", "Then I'll make you talk."),
                S("GORO", ShotCamera.Hold, 2.5f, "GORO", "Try."),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // The mission's real payload: he was following someone else's orders.
            Make("toll_mid", "NOT MINE",
                S("RENZO", ShotCamera.Handheld, 2.8f, "RENZO", "You were there.", audio: ShotAudio.Silence),
                S("GORO", ShotCamera.Hold, 2.2f, "GORO", "Where?"),
                S("RENZO", ShotCamera.Hold, 2.4f, "RENZO", "Yorune."),
                S("GORO", ShotCamera.Hold, 3f, "GORO", "Yes."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "You watched it burn."),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "I watched men follow orders."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Whose orders?"),
                S("GORO", ShotCamera.PushIn, 3.5f, "GORO", "Not mine.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 0.8f, fadeAfter: true, blackAfter: 0.4f));

            // On one knee, and still refusing. He answers with a direction, not a
            // fact, and then makes Renzo finish it.
            Make("toll_last", "ASK THE MOUNTAIN",
                S("GORO", ShotCamera.Hold, 3.2f, audio: ShotAudio.MusicOff),
                S("GORO", ShotCamera.Hold, 2.8f, "GORO", "Go ahead."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Tell me."),
                S("GORO", ShotCamera.Hold, 3.4f, "GORO", "Your father knew what was coming."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "What was coming?"),
                S("GORO", ShotCamera.PushIn, 3.6f, "GORO", "Ask the mountain."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.5f));

            // Four words, and no speech. He does not explain anything on the way out.
            Make("toll_death", "YOU HAVE HIS EYES",
                S("GORO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Silence),
                S("GORO", ShotCamera.Hold, 3f, "GORO", "You have his eyes."),
                S("RENZO", ShotCamera.Hold, 2.6f, "RENZO", "Who?"),
                S("GORO", ShotCamera.PushIn, 3.5f, "GORO", "Your father."),
                S("", ShotCamera.Hold, 3f, fadeAfter: true, blackAfter: 1.2f));

            Make("toll_end", "ASK THE MOUNTAIN",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "You knew him."),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "And you knew what happened."),
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.PushIn, 3.8f, "RENZO", "Then that's where I'll look."),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f, card: "THE TOLL-CAPTAIN"));

            // ---- MISSION 4 — THE SILENT FOREST ------------------------------
            // The investigation acquires consequences. Renzo stops being someone
            // watching them and becomes someone they are looking for, and the
            // chain of command finally has a face on it. Goro names Renzo's
            // family and refuses to explain it — "ask your father" is the whole
            // payload, and it is mission 5's problem.

            Make("forest_open", "THE LIGHTS GO OUT",
                S("RENZO", ShotCamera.OverShoulder, 4f, audio: ShotAudio.Wind),
                S("", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "The line is going dark behind me."),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "They know someone was here."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            // The runner. Short, ugly and not a set piece: he is a frightened man
            // who gives up a name and then makes the wrong choice.
            Make("forest_runner", "THE TOLL-CAPTAIN",
                S("RENZO", ShotCamera.Handheld, 3f, audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 3f, "RENZO", "Who gives the orders?"),
                S("", ShotCamera.Hold, 3.2f, "RUNNER", "You don't know what you're walking into."),
                S("", ShotCamera.Hold, 2.5f, "RENZO", "Then tell me."),
                S("", ShotCamera.PushIn, 3f, "RUNNER", "The Toll-Captain."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Goro.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 2.8f, "RUNNER", "You've heard of him?"),
                S("RENZO", ShotCamera.Hold, 3.2f, "RENZO", "Not enough."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 0.8f));

            // Goro, at distance and then close. He is controlled, faintly amused,
            // and he stops his own soldier from saying the name out loud.
            Make("forest_goro", "ASK YOUR FATHER",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.MusicOff),
                S("", ShotCamera.Hold, 2.8f, "GORO", "You searched the village?"),
                S("", ShotCamera.Hold, 2.4f, "SOLDIER", "Every house."),
                S("", ShotCamera.Hold, 2.2f, "GORO", "And?"),
                S("", ShotCamera.Hold, 2.2f, "SOLDIER", "Nothing."),
                S("", ShotCamera.Hold, 2.6f, "GORO", "Then search again."),
                S("", ShotCamera.Hold, 2.4f, "SOLDIER", "The Kurogawa—"),
                S("", ShotCamera.PushIn, 3.2f, "GORO", "Don't say that name here."),
                S("", ShotCamera.Hold, 3.4f, "GORO", "If the boy is here, he'll come looking."),
                // The turn: he sees him. Cut Renzo, cut Goro, hold the silence.
                S("GORO", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Silence),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "Kurogawa."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "You know me."),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "I know your family."),
                S("RENZO", ShotCamera.Hold, 3f, "RENZO", "Then tell me what happened."),
                S("GORO", ShotCamera.Hold, 3.2f, "GORO", "You should have stayed away."),
                S("RENZO", ShotCamera.Hold, 2.8f, "RENZO", "Who sent you?"),
                S("GORO", ShotCamera.PushIn, 4.5f, "GORO", "Ask your father.", audio: ShotAudio.Sting),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 0.8f));

            Make("forest_end", "FIND THE TOLL-CAPTAIN",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Wind),
                S("", ShotCamera.Wide, 4.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 3.5f, "RENZO", "Goro."),
                S("RENZO", ShotCamera.Hold, 3.8f, "RENZO", "He knows my father."),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "And he knows me."),
                S("RENZO", ShotCamera.Hold, 3.8f, "RENZO", "Then I'll find him.", audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f, card: "THE SILENT FOREST"));

            // ---- MISSION 3 — THE LANTERNS -----------------------------------
            // One question: who is leading them. It is not answered — the officer
            // is masked and unnamed, and the signature on his orders is a mark
            // nobody in Yorune has seen. What the player gets instead is worse:
            // these men were told to expect a Kurogawa.

            Make("lanterns_open", "THEY ARE WATCHING EACH OTHER",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Wind),
                S("", ShotCamera.SlowDolly, 4f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "They're not watching the village."),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "They're watching each other."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            // The network's purpose, shown: a light goes up and men below change
            // where they are walking.
            Make("lanterns_signal", "THE SIGNAL MOVES THEM",
                S("", ShotCamera.Wide, 4.5f, audio: ShotAudio.Silence),
                S("", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "One light, and they all turn."),
                S("RENZO", ShotCamera.PushIn, 3.5f, "RENZO", "It tells them where to be."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            Make("lanterns_overheard", "COMMAND POST",
                S("", ShotCamera.Hold, 2.5f, "GUARD", "Signal came from the north.", audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 2.2f, "PATROL", "Then move."),
                S("", ShotCamera.Hold, 2.2f, "GUARD", "Where?"),
                S("", ShotCamera.PushIn, 3f, "PATROL", "Command post."),
                S("RENZO", ShotCamera.Hold, 3.5f, fadeAfter: true, blackAfter: 0.8f));

            // The turn of the mission. The officer is a shape and a voice: no
            // name, no face, and nothing above him named either.
            Make("lanterns_officer", "THEY WERE WAITING",
                S("", ShotCamera.Wide, 4f, audio: ShotAudio.MusicOff),
                S("", ShotCamera.Hold, 3f, "OFFICER", "Nothing in the ruins."),
                S("", ShotCamera.Hold, 2.8f, "SOLDIER", "And the Kurogawa?"),
                S("", ShotCamera.Hold, 2.8f, "OFFICER", "Keep searching."),
                S("", ShotCamera.Hold, 2.8f, "SOLDIER", "If he returns?"),
                S("", ShotCamera.PushIn, 4f, "OFFICER", "Then we'll know.", audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.PushIn, 4.5f, "RENZO", "They were waiting for me."),
                S("RENZO", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 1f));

            Make("lanterns_document", "SO WHO ARE YOU",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "They know my name."),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "They knew I'd come back."),
                S("", ShotCamera.SlowDolly, 4.5f, audio: ShotAudio.MusicDark),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "So who are you?"),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f, card: "THE LANTERNS"));

            // ---- MISSION 2 — RED THREAD -------------------------------------
            // One question: who are they. It is answered — an organised force,
            // searching for something, that knows the Kurogawa name — and it
            // opens the next one, which is who sent them. Nothing here names what
            // they are looking for, and nothing suggests Aiko lived.

            Make("thread_open", "THE RED MARK",
                S("RENZO", ShotCamera.PushIn, 3.5f, audio: ShotAudio.Wind),
                S("RENZO", ShotCamera.Hold, 3.5f, "RENZO", "They left a trail."),
                S("RENZO", ShotCamera.OverShoulder, 3.5f, "RENZO", "I'll follow it."),
                S("", ShotCamera.Hold, 1f, fadeAfter: true, blackAfter: 0.6f));

            // The network. Three lights answering each other across a valley says
            // "organised" faster than any line of dialogue could.
            Make("thread_lanterns", "THEY ARE TALKING",
                S("", ShotCamera.Wide, 5f, audio: ShotAudio.Silence),
                S("", ShotCamera.SlowDolly, 5.5f, audio: ShotAudio.MusicSoft),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "One light. Then an answer."),
                S("RENZO", ShotCamera.PushIn, 4.5f, "RENZO", "They are talking to each other across the whole valley."),
                S("", ShotCamera.Hold, 1.5f, fadeAfter: true, blackAfter: 0.8f));

            // The turn. Restrained on purpose: they do not say what they are
            // looking for, and the only name spoken is his own.
            Make("thread_kurogawa", "KUROGAWA",
                S("", ShotCamera.Hold, 2.5f, "GUARD", "Nothing?", audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 2.2f, "PATROL", "Nothing."),
                S("", ShotCamera.Hold, 2.5f, "GUARD", "Then keep searching."),
                S("", ShotCamera.Hold, 2.5f, "PATROL", "Until when?"),
                S("", ShotCamera.Hold, 3.5f, "GUARD", "Until we find what they left behind."),
                S("", ShotCamera.PushIn, 3.5f, "PATROL", "…Kurogawa."),
                S("", ShotCamera.Hold, 2.5f, "GUARD", "Report it.", audio: ShotAudio.Sting),
                S("RENZO", ShotCamera.PushIn, 4.5f, "RENZO", "Why are they looking for my family?"),
                S("RENZO", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 1f));

            Make("thread_map", "SOMEONE SENT THEM",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "Routes. Watch posts. Supply. This is not a raiding party."),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "Someone sent them here."),
                S("RENZO", ShotCamera.Hold, 4f, "RENZO", "But who?"),
                S("", ShotCamera.PullOut, 5.5f, audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f, card: "RED THREAD"));

            // ---- MISSION 1 — ASHES ------------------------------------------
            // Renzo comes home. The mission asks one question, WHO IS STILL HERE,
            // and answers none. Aiko is a loss here, not a lead: nothing in these
            // three beats suggests she lived, because mission 9 needs that to land.

            Make("ashes_return", "ASHES",
                // Black, and wind, before anything is shown. The ear settles first.
                S("", ShotCamera.Hold, 4f, audio: ShotAudio.Wind, blackAfter: 1f),
                // The only wide shot in the mission. Destruction is established
                // once and never sold again.
                S("", ShotCamera.Wide, 5.5f, audio: ShotAudio.Wind,
                    theme: EnvThemeId.BurningVillage),
                S("RENZO", ShotCamera.OverShoulder, 4f, audio: ShotAudio.Silence),
                // Stops before it arrives: the shot wants to be closer and cannot.
                S("RENZO", ShotCamera.PushIn, 5f, "RENZO", "This was my home."),
                S("", ShotCamera.Hold, 4.5f, audio: ShotAudio.Wind, fadeAfter: true,
                    blackAfter: 1f, card: "TEN YEARS LATER"));

            // The emotional centre. Aiko is never on camera — a primitive stand-in
            // in close-up would undercut the one scene that has to work, and a
            // voice in an empty frame is the better shot anyway.
            Make("ashes_bracelet", "WHAT THE ASH KEPT",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Silence),
                S("", ShotCamera.Hold, 2f, "AIKO", "Renzo!", audio: ShotAudio.Birds),
                S("RENZO", ShotCamera.PushIn, 4.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.Hold, 5f, "RENZO", "…She never took it off."),
                S("RENZO", ShotCamera.PullOut, 4f, audio: ShotAudio.MusicSoft,
                    fadeAfter: true, blackAfter: 1.5f));

            Make("ashes_map", "SOMEBODY CAME BACK",
                S("RENZO", ShotCamera.Hold, 3.5f, audio: ShotAudio.Silence),
                S("RENZO", ShotCamera.PushIn, 4f, "RENZO", "Ten years."),
                S("RENZO", ShotCamera.Hold, 4.5f, "RENZO", "Why come back now?"),
                // Holds two beats past comfortable on the empty valley. Whoever
                // is out there is not shown; the player supplies them.
                S("", ShotCamera.PullOut, 6f, audio: ShotAudio.MusicDark),
                S("", ShotCamera.Hold, 2.5f, fadeAfter: true, blackAfter: 2f,
                    card: "ASHES"));

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
            // Named nothing. The player is a child in this memory and hears a
            // threat, not a proper noun — the name arrives in mission 8 with the
            // letter, which is where it can mean something.
            shots.Add(S("FATHER", ShotCamera.Hold, 4f, "KAGEHIRA",
                "You know what I came for. Say where it is, and this stops."));
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
