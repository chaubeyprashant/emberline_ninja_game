using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Emberline.Enemies;

namespace Emberline.UI
{
    /// <summary>
    /// Developer performance readout: frame pacing, memory, render counters and
    /// the Emberline-specific load drivers (enemies alive, road segments live).
    /// Purely observational — it never touches gameplay state, reads unscaled
    /// time so hit-stop can't skew the numbers, and builds nothing until it is
    /// first shown, so a hidden overlay costs one bool check per frame.
    ///
    /// Toggle: F3 (editor/desktop) or a four-finger tap (device). The choice is
    /// remembered in PlayerPrefs so a run can be started with it already on:
    ///   adb shell am start ... after setting perf_overlay=1, or just tap.
    /// </summary>
    public class PerfOverlay : MonoBehaviour
    {
        /// <summary>Frames sampled for the rolling window (~4s at 60fps).</summary>
        private const int Window = 240;

        /// <summary>Readout refresh period. Text rebuilds are the only real cost.</summary>
        private const float RefreshInterval = 0.25f;

        /// <summary>Anything slower than this is a visible hitch at 60fps.</summary>
        private const float HitchMs = 33.3f;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("perf_overlay", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("perf_overlay", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private readonly Queue<float> _frames = new(Window);
        private readonly List<float> _sorted = new(Window);
        private float _refreshT, _worstMs, _sinceReset;
        private int _hitches;
        private bool _built, _visible;
        private TMP_Text _text;
        private Canvas _canvas;
        private GameManager _gm;

        private ProfilerRecorder _drawCalls, _setPass, _tris, _gcAlloc;

        private void Start()
        {
            // Render counters only exist in development players / the editor;
            // Valid is checked at read time so release builds simply omit them.
            _drawCalls = Recorder(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = Recorder(ProfilerCategory.Render, "SetPass Calls Count");
            _tris = Recorder(ProfilerCategory.Render, "Triangles Count");
            _gcAlloc = Recorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            _gm = FindAnyObjectByType<GameManager>();
            SetVisible(Enabled);
        }

        private static ProfilerRecorder Recorder(ProfilerCategory category, string stat)
        {
            try { return ProfilerRecorder.StartNew(category, stat); }
            catch { return default; } // stat unavailable on this platform — skip it
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _setPass.Dispose();
            _tris.Dispose();
            _gcAlloc.Dispose();
        }

        private void Update()
        {
            if (ToggleRequested()) SetVisible(!_visible);
            if (!_visible) return;

            var ms = Time.unscaledDeltaTime * 1000f;
            _frames.Enqueue(ms);
            while (_frames.Count > Window) _frames.Dequeue();
            _sinceReset += Time.unscaledDeltaTime;
            if (ms > _worstMs) _worstMs = ms;
            if (ms > HitchMs) _hitches++;

            if ((_refreshT -= Time.unscaledDeltaTime) > 0f) return;
            _refreshT = RefreshInterval;
            Refresh();
        }

        /// <summary>F3, or a four-finger tap well clear of the normal verbs.</summary>
        private static bool ToggleRequested()
        {
            if (Input.GetKeyDown(KeyCode.F3)) return true;
            if (Input.touchCount != 4) return false;
            foreach (var t in Input.touches)
                if (t.phase == TouchPhase.Began) return true;
            return false;
        }

        private void SetVisible(bool on)
        {
            _visible = on;
            Enabled = on;
            if (on && !_built) Build();
            if (_canvas != null) _canvas.enabled = on;
            if (on) ResetWindow();
        }

        /// <summary>Clears the rolling stats — call when starting a fresh measurement.</summary>
        private void ResetWindow()
        {
            _frames.Clear();
            _worstMs = 0f;
            _hitches = 0;
            _sinceReset = 0f;
            _refreshT = 0f;
        }

        private void Build()
        {
            _built = true;
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200; // above EmberHud (100)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            // Sits below the life/Sen/gate cluster so it never hides combat
            // state. Every element is raycastTarget=false via UiKit, so the
            // panel cannot swallow a stick drag or a button press.
            var panel = UiKit.Rect(transform, "PerfPanel", new Vector2(0, 1),
                new Vector2(212, -232), new Vector2(404, 148));
            UiKit.Img(panel, null, new Color(0, 0, 0, 0.55f));
            _text = UiKit.Label(panel, "", 15, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(6, 0), new Vector2(388, 136), align: TextAnchor.UpperLeft);
        }

        private void Refresh()
        {
            if (_text == null) return;

            // Percentiles over the rolling window. Sorting 240 floats four times
            // a second is far cheaper than the text rebuild it feeds.
            _sorted.Clear();
            _sorted.AddRange(_frames);
            _sorted.Sort();
            var n = _sorted.Count;
            if (n == 0) return;
            var avg = 0f;
            foreach (var f in _sorted) avg += f;
            avg /= n;
            var p50 = _sorted[n / 2];
            var p95 = _sorted[Mathf.Min(n - 1, Mathf.RoundToInt(n * 0.95f))];
            var p99 = _sorted[Mathf.Min(n - 1, Mathf.RoundToInt(n * 0.99f))];

            var sb = new System.Text.StringBuilder(320);
            sb.Append($"FPS {1000f / Mathf.Max(0.001f, avg):F1}   avg {avg:F1}ms\n");
            sb.Append($"p50 {p50:F1}  p95 {p95:F1}  p99 {p99:F1} ms\n");
            sb.Append($"worst {_worstMs:F1}ms   hitches {_hitches} in {_sinceReset:F0}s\n");

            var mem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            var mono = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
            sb.Append($"mem {mem:F0}MB   mono {mono:F1}MB");
            if (_gcAlloc.Valid) sb.Append($"   gc/frame {_gcAlloc.LastValue / 1024f:F1}KB");
            sb.Append('\n');

            if (_drawCalls.Valid || _setPass.Valid || _tris.Valid)
            {
                sb.Append("draw ").Append(_drawCalls.Valid ? _drawCalls.LastValue.ToString() : "-");
                sb.Append("  setpass ").Append(_setPass.Valid ? _setPass.LastValue.ToString() : "-");
                sb.Append("  tris ").Append(_tris.Valid ? (_tris.LastValue / 1000) + "k" : "-");
                sb.Append('\n');
            }

            sb.Append(SceneLoad());
            _text.text = sb.ToString();
        }

        /// <summary>The Emberline-specific load drivers worth watching per scene.</summary>
        private string SceneLoad()
        {
            var alive = 0;
            foreach (var e in EnemyBrain.Active)
                if (e != null && !e.Dead) alive++;

            var line = $"enemies {alive}/{EnemyBrain.Active.Count}";
            var road = RoadNorth.Instance;
            if (road == null) return line;
            line += $"   road segs {road.SegmentCount}";
            if (_gm != null) line += $"   north {_gm.DistanceNorth}m";
            return line;
        }
    }
}
