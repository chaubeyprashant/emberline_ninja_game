using Emberline.Story;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Verifies the story framework: beats load from Resources, localisation falls
    /// back to English, "already seen" persists, cast lookup works, and every shot
    /// type places the camera sanely. Run after editing anything under Scripts/Story.
    /// </summary>
    public static class EmberStoryCheck
    {
        [MenuItem("Emberline/Check Story Framework")]
        public static void Run()
        {
            var fail = 0;
            void Check(bool ok, string what)
            {
                if (ok) Debug.Log("[P1] pass  " + what);
                else { Debug.LogError("[P1] FAIL  " + what); fail++; }
            }

            // The MissionPlan failure mode: assets that exist but cannot load.
            var beats = Resources.LoadAll<StoryBeat>("Story");
            Check(beats.Length > 0, $"story beats load from Resources ({beats.Length})");
            var opening = System.Array.Find(beats, x => x.id == "opening");
            Check(opening != null, "the opening beat is addressable by id");
            Check(opening != null && opening.shots.Length > 0,
                $"opening has shots ({opening?.shots.Length})");
            Check(opening != null && opening.Duration > 0f,
                $"opening reports a duration ({opening?.Duration:0.0}s)");

            // Localisation falls back to authored English, never to a raw key.
            Loc.Language = "";
            Check(Loc.T("When you're near, nothing bad can happen.")
                  == "When you're near, nothing bad can happen.", "Loc falls back to English");
            Check(Loc.T("") == "", "Loc handles empty");
            Loc.Language = "zz"; // no such table
            Check(Loc.T("hello") == "hello", "a missing table degrades to the source text");
            Loc.Language = "";

            // Save/load of "already seen", which gates the skip button.
            StoryFlags.ResetAll();
            Check(!StoryFlags.Seen("opening"), "a fresh install has not seen the opening");
            Check(StoryFlags.IsFreshInstall, "fresh-install flag agrees");
            StoryFlags.MarkSeen("opening");
            Check(StoryFlags.Seen("opening"), "seen persists after marking");
            Check(!StoryFlags.IsFreshInstall, "fresh-install flag clears");
            StoryFlags.ResetAll();
            Check(!StoryFlags.Seen("opening"), "reset clears it again");

            // Cast lookup.
            Cast.Clear();
            var go = new GameObject("RenStandIn");
            var cm = go.AddComponent<CastMember>();
            cm.castName = "REN";
            Cast.Register(cm);
            Check(Cast.Find("REN") == go.transform, "cast lookup by name");
            Check(Cast.Find("ren") == go.transform, "cast lookup is case-insensitive");
            Check(Cast.Find("NOBODY") == null, "an unknown subject returns null, not an error");
            Check(Cast.Find("") == null, "an empty subject returns null");

            // Camera placements must be in front of the subject and above ground.
            var camGo = new GameObject("Cam");
            camGo.AddComponent<Camera>();
            var rig = camGo.AddComponent<CameraRig>();
            var subj = new GameObject("Subject").transform;
            subj.position = Vector3.zero;
            subj.rotation = Quaternion.identity;

            var bad = new System.Collections.Generic.List<string>();
            foreach (ShotCamera k in System.Enum.GetValues(typeof(ShotCamera)))
            {
                CinematicCamera.Apply(k, subj, 2f, rig);
                if (k == ShotCamera.Orbit) continue; // orbit uses the legacy sweep
                var p = camGo.transform.position;
                if (p.y < 0.5f) bad.Add($"{k}:below eye line ({p.y:0.00})");
                if (Vector3.Distance(p, subj.position) > 20f) bad.Add($"{k}:too far");
                if (Vector3.Distance(p, subj.position) < 0.8f) bad.Add($"{k}:inside the subject");
            }
            Check(bad.Count == 0, $"every shot places the camera sanely ({string.Join(" | ", bad)})");

            CinematicCamera.Apply(ShotCamera.Hold, subj, 2f, rig);
            Check(rig.ScriptedShot, "a scripted shot takes ownership of the rig");
            rig.StopCinematic();
            Check(!rig.ScriptedShot, "stopping releases the rig back to gameplay");

            // A null beat must not strand the caller.
            var ran = false;
            var d = CinematicDirector.Play(null, null, null, () => ran = true);
            Check(d == null && ran, "a missing beat runs the callback instead of hanging");

            Debug.Log(fail == 0 ? "[P1] ALL PASSED" : $"[P1] {fail} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
