using System;
using Emberline.Core;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>How the camera behaves for one shot.</summary>
    public enum ShotCamera
    {
        Hold,          // locked off — stillness is a choice, not an absence
        SlowDolly,     // lateral drift across the subject
        PushIn,        // creep toward the subject
        PullOut,       // reveal
        Orbit,         // slow arc, for reveals of a place rather than a person
        OverShoulder,  // behind and beside the subject
        Wide,          // high and back, for destruction
        Handheld,      // Hold plus a small unsettled drift
    }

    /// <summary>
    /// Dressing state of the village set. Scenes 1-3 are the same place at three
    /// points in one night, so they are one set with three states rather than
    /// three sets — half the geometry and no load between them.
    /// </summary>
    public enum SetState { Unchanged, Peace, Attack, Ruin }

    /// <summary>Audio bed a shot asks for. Resolved by the director to Sfx3D.</summary>
    public enum ShotAudio
    {
        Unchanged, Silence, Wind, Birds, Village, Bells, Fire, Rain, Snow,
        MusicSoft, MusicDark, MusicImpact, MusicOff, Sting,
    }

    /// <summary>
    /// One shot: a camera, a subject, a line, an audio bed, and how long to hold.
    /// Everything a scene needs is data here — no scene wiring and no dialogue in
    /// gameplay scripts, so a new chapter is an asset rather than a code change.
    /// </summary>
    [Serializable]
    public class StoryShot
    {
        [Header("Framing")]
        [Tooltip("Name of the cast object to frame. Empty keeps the previous subject.")]
        public string subject = "";
        public ShotCamera camera = ShotCamera.Hold;
        [Tooltip("Seconds. Emotional beats want longer than feels comfortable.")]
        public float duration = 3f;
        [Range(0f, 1f)] public float letterbox = 1f;

        [Header("Line")]
        [Tooltip("Speaker key. Empty means no subtitle for this shot.")]
        public string speaker = "";
        [Tooltip("Localisation key; falls back to the literal string when unknown.")]
        [TextArea] public string line = "";
        [Tooltip("Voice-over. None ships today — subtitles carry the scene.")]
        public AudioClip voice;

        [Header("World")]
        public bool applyTheme;
        public EnvThemeId theme = EnvThemeId.Village;
        public ShotAudio audio = ShotAudio.Unchanged;

        [Header("Set")]
        [Tooltip("Village dressing for this shot. Unchanged keeps the current state.")]
        public SetState setState = SetState.Unchanged;

        [Header("Transitions")]
        [Tooltip("Full-screen card, e.g. THREE YEARS LATER. Empty for none.")]
        public string card = "";
        public bool fadeOutAfter;
        [Tooltip("Seconds of black after this shot. Silence is part of the edit.")]
        public float blackAfter;
    }
}
