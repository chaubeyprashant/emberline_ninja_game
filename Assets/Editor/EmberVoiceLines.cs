using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Emberline.Core;
using Emberline.Missions;
using Emberline.Story;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Lists every line of spoken dialogue in the game, so each can be voiced.
    ///
    /// <para>
    /// The first voice pass scraped <c>dialogue: L(...)</c> out of CampaignTable.cs
    /// with a regex, which found the briefings and nothing else. Story-prop
    /// discoveries (the lines Renzo says mid-mission), duel intros and defeats, and
    /// every cinematic shot stayed silent next to voiced briefings. This reads the
    /// loaded data instead — the same objects the game shows — so a line cannot be
    /// voiced under a slightly different string than the one it is looked up by.
    /// </para>
    ///
    /// <para>
    /// Loop: <see cref="Export"/> → <c>python3 Assets/Editor/Tools/generate_voices.py</c>
    /// → <see cref="ConfigureImports"/>.
    /// </para>
    /// </summary>
    public static class EmberVoiceLines
    {
        public const string VoiceDir = "Assets/Resources/Voices";
        private const string ManifestPath = "Logs/voice_lines.tsv";

        private struct Entry
        {
            public string speaker, text, key;
            public List<string> sources;
        }

        [MenuItem("Emberline/Voices/Export Voice Lines")]
        public static void Export()
        {
            var entries = new Dictionary<string, Entry>();

            void Add(string speaker, string text, string key, string source)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                var hash = VoiceLines.Hash(key);
                if (!entries.TryGetValue(hash, out var e))
                    e = new Entry { speaker = speaker ?? "", text = text, key = key, sources = new List<string>() };
                e.sources.Add(source);
                entries[hash] = e;
            }

            // Cinematics only. Briefings, duel lines and mid-mission story-prop lines
            // are text: voiced, they interrupted play and the text-to-speech read as
            // robotic. DialogueBox no longer plays voice, so exporting them would
            // only ship clips nothing loads.
            foreach (var beat in Resources.LoadAll<StoryBeat>("Story"))
                foreach (var s in beat.shots)
                    if (s.voice == null)
                        Add(s.speaker, s.line, VoiceLines.Key(s.speaker, s.line), $"beat {beat.id}");

            var sb = new StringBuilder();
            string Clean(string v) => v.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            var have = 0;
            foreach (var kv in entries.OrderBy(k => k.Value.sources[0]))
            {
                var e = kv.Value;
                if (File.Exists($"{VoiceDir}/{kv.Key}.aiff")) have++;
                sb.Append(kv.Key).Append('\t').Append(Clean(e.speaker.ToUpperInvariant())).Append('\t')
                  .Append(Clean(e.text)).Append('\t').Append(Clean(string.Join("; ", e.sources))).Append('\n');
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ManifestPath, sb.ToString());

            var speakers = entries.Values.GroupBy(e => e.speaker.ToUpperInvariant())
                .OrderByDescending(g => g.Count()).Select(g => $"{(g.Key == "" ? "(none)" : g.Key)} {g.Count()}");
            Debug.Log($"[Voices] {entries.Count} lines → {ManifestPath}; {have} already voiced, " +
                      $"{entries.Count - have} missing\n[Voices] speakers: {string.Join(", ", speakers)}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Speech import settings: mono Vorbis, decompressed only when its line
        /// plays. Several hundred lines at the importer's defaults would cost more
        /// APK than the rest of the audio together.
        /// </summary>
        [MenuItem("Emberline/Voices/Configure Voice Imports")]
        public static void ConfigureImports()
        {
            AssetDatabase.Refresh();
            var changed = 0;
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter ai) continue;

                var s = ai.defaultSampleSettings;
                if (ai.forceToMono && ai.loadInBackground &&
                    s.loadType == AudioClipLoadType.CompressedInMemory &&
                    s.compressionFormat == AudioCompressionFormat.Vorbis &&
                    Mathf.Approximately(s.quality, 0.45f) && !s.preloadAudioData)
                    continue;

                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.45f;
                s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                s.preloadAudioData = false;
                ai.defaultSampleSettings = s;
                ai.forceToMono = true;
                ai.loadInBackground = true;
                ai.SaveAndReimport();
                changed++;
            }
            Debug.Log($"[Voices] {guids.Length} voice clips, {changed} reconfigured");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
