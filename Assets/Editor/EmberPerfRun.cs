using Emberline.Core;
using Emberline.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Runs the perf tour (<see cref="PerfRun"/>) in the editor with short samples.
    /// The numbers mean nothing on a Mac; this proves the tour reaches every
    /// scenario and emits a line for each before a phone is plugged in. Writes
    /// <c>Logs/perf_editor.jsonl</c> and exits with the run's code in batch mode.
    /// </summary>
    public static class EmberPerfRun
    {
        private const string Armed = "emberline.perfrun.armed";
        private const string GateArmed = "emberline.perfgate.armed";

        // Regression ceilings for the editor's render stats (Mac, 1600×720, tier 1,
        // night with lanterns lit), set ~15 % above the step-6 result so a change
        // that undoes it fails loudly. Device targets are tighter: PERF_BASELINE.md.
        public const int MaxDraws = 780, MaxSetPass = 230, MaxTris = 400_000, MaxTotalDraws = 4_300;

        /// <summary>The tour with render stats, failing (exit 1) on any ceiling breached.</summary>
        [MenuItem("Emberline/Perf Gate (Editor)")]
        public static void RunGate()
        {
            SessionState.SetBool(GateArmed, true);
            Run();
        }

        [MenuItem("Emberline/Run Perf Tour (Editor)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            Session.Mode = LaunchMode.None;
            AudioListener.volume = 0f;
            SessionState.SetBool(Armed, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void AfterReload()
        {
            if (!SessionState.GetBool(Armed, false)) return;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode) Attach();
            };
            if (EditorApplication.isPlaying) Attach();
        }

        private static bool _attached;

        private static int Gate()
        {
            var fails = 0; var total = 0; var stops = 0;
            foreach (var line in System.IO.File.ReadAllLines("Logs/perf_editor.jsonl"))
            {
                if (!line.Contains("\"kind\":\"render\"") || !line.Contains("\"rig\":\"folded\"")) continue;
                var id = Field(line, "id"); var d = (int)Num(line, "draws");
                var sp = (int)Num(line, "setpass"); var t = (int)Num(line, "tris");
                total += d; stops++;
                var bad = d > MaxDraws || sp > MaxSetPass || t > MaxTris;
                if (bad) fails++;
                Debug.Log($"[GATE] {(bad ? "FAIL" : "ok  ")} {id,-11} draws {d,5}/{MaxDraws} setpass {sp,4}/{MaxSetPass} tris {t,7}/{MaxTris}");
            }
            if (total > MaxTotalDraws) { fails++; Debug.Log($"[GATE] FAIL total draws {total}/{MaxTotalDraws}"); }
            if (stops == 0) { Debug.Log("[GATE] FAIL no render stats were recorded"); return 1; }
            Debug.Log(fails == 0 ? $"[GATE] PASS — {stops} stops, {total} draws" : $"[GATE] FAIL — {fails} breach(es)");
            return fails == 0 ? 0 : 1;
        }

        private static string Field(string json, string key)
        {
            var i = json.IndexOf($"\"{key}\":\"", System.StringComparison.Ordinal);
            if (i < 0) return "?";
            i += key.Length + 4;
            return json.Substring(i, json.IndexOf('"', i) - i);
        }

        private static double Num(string json, string key)
        {
            var i = json.IndexOf($"\"{key}\":", System.StringComparison.Ordinal);
            if (i < 0) return 0;
            i += key.Length + 3;
            var j = i;
            while (j < json.Length && (char.IsDigit(json[j]) || json[j] == '.' || json[j] == '-')) j++;
            return double.Parse(json.Substring(i, j - i), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void Attach()
        {
            if (_attached) return;
            _attached = true;
            // Silent: the domain reload on entering play mode resets the volume set
            // in Run, and the tour otherwise plays the score through the speakers.
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            // -perfopts on the command line adds options, e.g. "quick,shots,night".
            var extra = "";
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-perfopts") extra = "," + args[i + 1];
            var gate = SessionState.GetBool(GateArmed, false);
            if (gate) extra += ",render,night";
            var run = PerfRun.Begin("quick" + extra);
            run.onFinished = code =>
            {
                SessionState.EraseBool(Armed);
                SessionState.EraseBool(GateArmed);
                if (gate && code == 0) code = Gate();
                Debug.Log($"{PerfRun.Tag} editor exit {code}");
                if (Application.isBatchMode) EditorApplication.Exit(code);
                else EditorApplication.ExitPlaymode();
            };
        }
    }
}
