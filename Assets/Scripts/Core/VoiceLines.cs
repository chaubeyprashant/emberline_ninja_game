using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Finds the generated voice for a line of dialogue.
    ///
    /// <para>
    /// A voice file is named by the MD5 of the exact string the game shows —
    /// "SPEAKER|text", or the bare text when there is no speaker — and lives in
    /// Resources/Voices. The name is the whole contract: the briefing dialogue
    /// box, mid-mission story props and cinematic shots all ask here, and
    /// <c>EmberVoiceLines</c> exports every such string so
    /// <c>Tools/generate_voices.py</c> can speak them.
    /// </para>
    /// </summary>
    public static class VoiceLines
    {
        /// <summary>The string a line is keyed by.</summary>
        public static string Key(string speaker, string text) =>
            string.IsNullOrEmpty(speaker) ? text : speaker + "|" + text;

        public static string Hash(string key)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            var sb = new System.Text.StringBuilder(32);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>The voice for this line, or null when none was generated.</summary>
        public static AudioClip Clip(string key) =>
            string.IsNullOrEmpty(key) ? null : Resources.Load<AudioClip>("Voices/" + Hash(key));
    }
}
