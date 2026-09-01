using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// String indirection for localisation. Authored text is written in English
    /// directly in the story assets; passing it through here means a translated
    /// table can be dropped in later without touching a single asset or script.
    ///
    /// A key that has no entry returns itself, so English is always the fallback
    /// and a missing translation degrades to readable text rather than to a key.
    /// No third-party dependency: the table is a plain text resource.
    /// </summary>
    public static class Loc
    {
        private static Dictionary<string, string> _table;
        private static string _lang = "";

        /// <summary>Two-letter code, or "" for the authored English.</summary>
        public static string Language
        {
            get => PlayerPrefs.GetString("loc_lang", "");
            set { PlayerPrefs.SetString("loc_lang", value ?? ""); _table = null; }
        }

        public static string T(string keyOrText)
        {
            if (string.IsNullOrEmpty(keyOrText)) return "";
            Load();
            return _table != null && _table.TryGetValue(keyOrText, out var v) ? v : keyOrText;
        }

        private static void Load()
        {
            var want = Language;
            if (_table != null && _lang == want) return;
            _lang = want;
            _table = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(want)) return;

            // "key=value" per line, # for comments. Resources/Loc/<lang>.txt
            var asset = Resources.Load<TextAsset>("Loc/" + want);
            if (asset == null) return;
            foreach (var raw in asset.text.Split('\n'))
            {
                var l = raw.Trim();
                if (l.Length == 0 || l[0] == '#') continue;
                var i = l.IndexOf('=');
                if (i <= 0) continue;
                _table[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim().Replace("\\n", "\n");
            }
        }
    }
}
