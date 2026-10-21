#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberline.DebugTools
{
    /// <summary>
    /// The perf baseline, taken by the game itself so it is the same run every
    /// time. Holds still on the main menu, at every landmark of the region and in
    /// an eight-enemy fight, and logs one <c>[PERFRUN]</c> JSON line per scenario:
    /// frame-time percentiles, CPU and GPU time, draw calls, triangles, garbage
    /// and memory. <c>perf_run.sh</c> collects the lines from logcat.
    ///
    /// Development builds and the editor only. Started by an intent extra
    /// (<c>am start … --es perfrun 1</c>), a <c>perfrun.txt</c> file in the
    /// persistent data folder, <c>-perfrun</c> on a desktop command line, or the
    /// <c>Emberline/Run Perf Tour</c> editor entry. Options, comma separated:
    /// <c>quick</c> (short samples) and <c>fps=60</c> (lift the governor's cap;
    /// without it the run measures what players get).
    /// </summary>
    public class PerfRun : MonoBehaviour
    {
        public const string Tag = "[PERFRUN]";
        /// <summary>A visible stutter at 60 fps; at a 30 fps cap every frame is 33 ms, so the bar rises with the cap.</summary>
        private static float HitchMs => Mathf.Max(33.3f,
            1500f / (Application.targetFrameRate > 0 ? Application.targetFrameRate : 60));
        private const string WorldKey = "world_state";
        private const string WorldBackupKey = "world_state_perfrun_backup";

        public float settle = 6f, sample = 20f, fightSample = 30f;
        public int fps;
        public System.Action<int> onFinished;

        private static bool _running;
        public static bool Running => _running;
        private readonly List<float> _ms = new(4096), _cpu = new(4096), _gpu = new(4096);
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private ProfilerRecorder _draw, _setPass, _batches, _tris, _gc, _sysMem;
        private double _drawSum, _setPassSum, _batchSum, _trisSum, _gcSum;
        private int _counterFrames;
        private float _arriveWorst;
        private string _resultsPath;

        // ------------------------------------------------------------ start

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            RestoreWorld(); // an earlier run that was killed leaves its backup behind
            if (_running) return;
            var opts = AndroidExtra() ?? FileTrigger() ?? CommandLine();
            if (opts != null) Begin(opts);
        }

        public static PerfRun Begin(string opts)
        {
            var go = new GameObject("PerfRun");
            DontDestroyOnLoad(go);
            var run = go.AddComponent<PerfRun>();
            foreach (var raw in (opts ?? "").Split(','))
            {
                var o = raw.Trim().ToLowerInvariant();
                if (o == "quick") { run.settle = 3f; run.sample = 5f; run.fightSample = 8f; }
                else if (o.StartsWith("fps=") && int.TryParse(o.Substring(4), out var f)) run.fps = f;
            }
            return run;
        }

        private static string AndroidExtra()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                return intent.Call<string>("getStringExtra", "perfrun");
            }
            catch (System.Exception e) { Debug.LogWarning($"{Tag} intent unreadable: {e.Message}"); }
#endif
            return null;
        }

        private static string FileTrigger()
        {
            var path = Path.Combine(Application.persistentDataPath, "perfrun.txt");
            if (!File.Exists(path)) return null;
            var opts = File.ReadAllText(path).Trim();
            File.Delete(path); // one run per file, so the next normal launch is normal
            return opts.Length > 0 ? opts : "1";
        }

        private static string CommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
                if (args[i] == "-perfrun")
                    return i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : "1";
            return null;
        }

        // ------------------------------------------------------------- tour

        private IEnumerator Start()
        {
            _running = true;
            _resultsPath = Application.isEditor
                ? Path.Combine(Application.dataPath, "..", "Logs", "perf_editor.jsonl")
                : Path.Combine(Application.persistentDataPath, "perfrun.jsonl");
            File.WriteAllText(_resultsPath, "");
            StartRecorders();

            // The tour discovers every landmark and moves the player; none of
            // that may end up in the owner's save.
            if (!PlayerPrefs.HasKey(WorldBackupKey))
                PlayerPrefs.SetString(WorldBackupKey, PlayerPrefs.GetString(WorldKey, ""));
            PlayerPrefs.Save();

            Emit(Meta());

            var gm = (GameManager)null;
            var wait = Time.realtimeSinceStartup + 20f;
            while ((gm = SceneRefs.Game) == null && Time.realtimeSinceStartup < wait) yield return null;
            if (gm == null)
            {
                // The opening scene is waiting on something; skip to the menu.
                SceneManager.LoadScene("Rooftop");
                wait = Time.realtimeSinceStartup + 30f;
                while ((gm = SceneRefs.Game) == null && Time.realtimeSinceStartup < wait) yield return null;
            }
            if (gm == null) { Finish(2, "no GameManager"); yield break; }

            if (Session.Mode == LaunchMode.None)
            {
                _arriveWorst = 0f;
                yield return Measure("menu", "Main menu idle", sample);
            }

            if (Session.Mode != LaunchMode.Explore) gm.LaunchExplore();
            wait = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < wait)
            {
                gm = SceneRefs.Game;
                if (gm != null && gm.State == GameManager.Phase.Playing
                    && Session.Mode == LaunchMode.Explore && SceneRefs.Motor != null) break;
                yield return null;
            }
            if (gm == null || gm.State != GameManager.Phase.Playing) { Finish(3, "explore never started"); yield break; }

            gm.PlayerHealth?.SetMax(100000f);
            if (fps > 0)
            {
                if (PerfGovernor.Instance != null) PerfGovernor.Instance.enabled = false;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = fps;
            }

            foreach (var l in WorldLandmarks.All)
            {
                yield return Place(l);
                yield return Measure(l.id, l.name, sample);
            }

            // The audit's worst ordinary case: eight mixed fighters on the player.
            var village = WorldLandmarks.Find("yorune");
            yield return Place(village);
            var motor = SceneRefs.Motor;
            var kinds = new[] { EnemyKind.Bandit, EnemyKind.RaiderAxe, EnemyKind.PikeGuard, EnemyKind.Assassin,
                                EnemyKind.Samurai, EnemyKind.RogueNinja, EnemyKind.Ranged, EnemyKind.Bomber };
            for (var i = 0; i < kinds.Length; i++)
                gm.SpawnOne(kinds[i], false,
                    motor.transform.position + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 6f);
            yield return Measure("fight8", "Eight-enemy fight at Yorune", fightSample);

            Finish(0, null);
        }

        /// <summary>
        /// Stands the player a little outside a landmark, facing it, with the
        /// camera behind: the view a player gets walking up to it. The motor is
        /// off while the streamer loads the ground, so nobody falls through it.
        /// </summary>
        private IEnumerator Place(Landmark l)
        {
            _arriveWorst = 0f;
            var motor = SceneRefs.Motor;
            if (motor == null || l == null) yield break;
            var cc = motor.GetComponent<CharacterController>();
            var toValley = new Vector3(-l.centre.x, 0f, -l.centre.z);
            var dir = toValley.sqrMagnitude > 1f ? toValley.normalized : Quaternion.Euler(0f, l.yaw, 0f) * Vector3.back;
            var at = l.centre + dir * (l.radius * 0.5f + 12f);

            motor.enabled = false;
            if (cc != null) cc.enabled = false;
            motor.transform.position = new Vector3(at.x, ZoneTerrain.HeightAt(at.x, at.z) + 1f, at.z);

            var streamer = WorldStreamer.Instance;
            var t = 0f;
            while (t < 1f || (streamer != null && streamer.Pending > 0 && t < 10f))
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                _arriveWorst = Mathf.Max(_arriveWorst, Time.unscaledDeltaTime * 1000f);
            }

            var top = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(new Vector3(at.x, 400f, at.z), Vector3.down, 800f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(motor.transform) && hit.point.y > top) top = hit.point.y;
            if (!float.IsNegativeInfinity(top)) motor.transform.position = new Vector3(at.x, top + 0.05f, at.z);

            if (cc != null) cc.enabled = true;
            motor.enabled = true;
            var look = l.centre - motor.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) motor.SetFacing(look.normalized);
            SceneRefs.Rig?.SnapBehindTarget();
        }

        // ---------------------------------------------------------- measure

        private IEnumerator Measure(string id, string label, float seconds)
        {
            for (var t = 0f; t < settle; t += Time.unscaledDeltaTime)
            {
                yield return null;
                _arriveWorst = Mathf.Max(_arriveWorst, Time.unscaledDeltaTime * 1000f);
            }

            _ms.Clear(); _cpu.Clear(); _gpu.Clear();
            _drawSum = _setPassSum = _batchSum = _trisSum = _gcSum = 0;
            _counterFrames = 0;
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < seconds)
            {
                yield return null;
                _ms.Add(Time.unscaledDeltaTime * 1000f);
                _counterFrames++;
                _drawSum += _draw.Valid ? _draw.LastValue : 0;
                _setPassSum += _setPass.Valid ? _setPass.LastValue : 0;
                _batchSum += _batches.Valid ? _batches.LastValue : 0;
                _trisSum += _tris.Valid ? _tris.LastValue : 0;
                _gcSum += _gc.Valid ? _gc.LastValue : 0;
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
                {
                    if (_timing[0].cpuMainThreadFrameTime > 0) _cpu.Add((float)_timing[0].cpuMainThreadFrameTime);
                    if (_timing[0].gpuFrameTime > 0) _gpu.Add((float)_timing[0].gpuFrameTime);
                }
            }

            _ms.Sort(); _cpu.Sort(); _gpu.Sort();
            var avg = 0f;
            var hitches = 0;
            var hitchMs = HitchMs;
            foreach (var m in _ms) { avg += m; if (m > hitchMs) hitches++; }
            avg /= Mathf.Max(1, _ms.Count);
            var n = Mathf.Max(1, _counterFrames);
            var streamer = WorldStreamer.Instance;

            var j = new Json();
            j.S("kind", "scenario").S("id", id).S("name", label)
             .I("frames", _ms.Count).F("fps", 1000f / Mathf.Max(0.001f, avg)).F("avg_ms", avg)
             .F("p50_ms", Pct(_ms, 0.50f)).F("p95_ms", Pct(_ms, 0.95f)).F("p99_ms", Pct(_ms, 0.99f))
             .F("worst_ms", _ms.Count > 0 ? _ms[^1] : 0f).I("hitches", hitches).F("hitch_ms", hitchMs)
             .F("cpu_p50_ms", Pct(_cpu, 0.50f)).F("cpu_p95_ms", Pct(_cpu, 0.95f))
             .F("gpu_p50_ms", Pct(_gpu, 0.50f)).F("gpu_p95_ms", Pct(_gpu, 0.95f))
             .F("draw", (float)(_drawSum / n)).F("setpass", (float)(_setPassSum / n))
             .F("batches", (float)(_batchSum / n)).F("tris_k", (float)(_trisSum / n / 1000.0))
             .F("gc_kb_frame", (float)(_gcSum / n / 1024.0))
             .F("mem_mb", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f)
             .F("sys_mb", _sysMem.Valid ? _sysMem.LastValue / 1048576f : 0f)
             .F("mono_mb", UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f)
             .I("enemies", EnemyBrain.Active.Count)
             .I("chunks", streamer != null ? streamer.Loaded : 0)
             .S("bed_wanted", SceneRefs.Motor != null ? AmbienceZones.BedAt(SceneRefs.Motor.transform.position) : "")
             .S("bed_playing", Sfx3D.CurrentAmbience ?? "").S("music", Sfx3D.CurrentMusic.ToString())
             .F("arrive_worst_ms", _arriveWorst)
             .I("target_fps", Application.targetFrameRate)
             .I("thermal_step", PerfGovernor.ThermalStep);
            Emit(j);
        }

        private static float Pct(List<float> sorted, float q) =>
            sorted.Count == 0 ? 0f : sorted[Mathf.Min(sorted.Count - 1, Mathf.FloorToInt(sorted.Count * q))];

        private Json Meta() => new Json().S("kind", "meta")
            .S("version", Application.version).S("device", SystemInfo.deviceModel)
            .S("gpu", SystemInfo.graphicsDeviceName).S("api", SystemInfo.graphicsDeviceType.ToString())
            .I("gfx_tier", PlayerPrefs.GetInt("gfx_tier", 1)).I("gameplay_fps", PerfGovernor.GameplayFps)
            .I("fps_override", fps).I("screen_w", Screen.width).I("screen_h", Screen.height)
            .F("settle_s", settle).F("sample_s", sample)
            .S("frame_timing", FrameTimingManager.IsFeatureEnabled() ? "on" : "off");

        private void Finish(int code, string error)
        {
            var j = new Json().S("kind", "done").I("code", code);
            if (error != null) j.S("error", error);
            Emit(j);
            if (fps > 0 && PerfGovernor.Instance != null) PerfGovernor.Instance.enabled = true;
            RestoreWorld();
            StopRecorders();
            _running = false;
            enabled = false;
            onFinished?.Invoke(code);
        }

        private static void RestoreWorld()
        {
            if (!PlayerPrefs.HasKey(WorldBackupKey)) return;
            var saved = PlayerPrefs.GetString(WorldBackupKey, "");
            if (saved.Length > 0) PlayerPrefs.SetString(WorldKey, saved);
            else PlayerPrefs.DeleteKey(WorldKey);
            PlayerPrefs.DeleteKey(WorldBackupKey);
            PlayerPrefs.Save();
        }

        private void Emit(Json j)
        {
            var line = j.ToString();
            Debug.Log($"{Tag} {line}");
            try { File.AppendAllText(_resultsPath, line + "\n"); }
            catch (IOException e) { Debug.LogWarning($"{Tag} results file: {e.Message}"); }
        }

        // -------------------------------------------------------- recorders

        private void StartRecorders()
        {
            _draw = Recorder(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = Recorder(ProfilerCategory.Render, "SetPass Calls Count");
            _batches = Recorder(ProfilerCategory.Render, "Batches Count");
            _tris = Recorder(ProfilerCategory.Render, "Triangles Count");
            _gc = Recorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            _sysMem = Recorder(ProfilerCategory.Memory, "System Used Memory");
        }

        private void StopRecorders()
        {
            _draw.Dispose(); _setPass.Dispose(); _batches.Dispose();
            _tris.Dispose(); _gc.Dispose(); _sysMem.Dispose();
        }

        private void OnDestroy() { if (_running) StopRecorders(); }

        private static ProfilerRecorder Recorder(ProfilerCategory category, string stat)
        {
            try { return ProfilerRecorder.StartNew(category, stat); }
            catch (System.Exception) { return default; }
        }

        /// <summary>One flat JSON object, invariant culture, no allocations worth caring about.</summary>
        private class Json
        {
            private readonly StringBuilder _sb = new StringBuilder(512).Append('{');
            private bool _first = true;

            private Json Key(string k)
            {
                if (!_first) _sb.Append(',');
                _first = false;
                _sb.Append('"').Append(k).Append("\":");
                return this;
            }

            public Json S(string k, string v)
            {
                Key(k).Append('"');
                foreach (var c in v ?? "")
                    if (c == '"' || c == '\\') _sb.Append('\\').Append(c);
                    else if (c >= ' ') _sb.Append(c);
                _sb.Append('"');
                return this;
            }

            public Json I(string k, int v) { Key(k)._sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }

            public Json F(string k, float v)
            {
                Key(k)._sb.Append(float.IsFinite(v) ? v.ToString("0.##", CultureInfo.InvariantCulture) : "0");
                return this;
            }

            private Json Append(char c) { _sb.Append(c); return this; }

            public override string ToString() => _sb.ToString() + "}";
        }
    }
}
#endif
