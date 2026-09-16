using Emberline.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The traversal course, in edit mode. Builds a scene of boxes at the heights
    /// the motor has to classify — a step, a thin wall, a deep block, a chest-high
    /// ledge, a roof edge, a wall too tall to climb, a blocked overhang and a ramp —
    /// and asserts what <see cref="TraversalProbe.Scan"/> says about each. Physics
    /// queries work without play mode, so this runs in the same batch as the other
    /// checks and exits non-zero on any failure.
    ///
    ///   Unity -batchmode -nographics -executeMethod Emberline.EditorTools.EmberTraversalCheck.Run
    /// </summary>
    public static class EmberTraversalCheck
    {
        private const float Radius = 0.35f, Height = 1.8f, Reach = 1.1f;
        private static int _fails;

        [MenuItem("Emberline/Check Traversal")]
        public static void Run()
        {
            _fails = 0;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(200f, 1f, 200f));

            // Each case stands the body 0.6 m in front of the obstacle's face,
            // looking +Z, on ground at y = 0. Obstacles are centred on x = i * 10.
            Case("step 0.3 m is left to the controller", 0, 0.3f, 2f, TraversalKind.None);
            Case("thin wall 1.0 m is vaulted", 1, 1.0f, 0.3f, TraversalKind.Vault);
            Case("deep block 1.0 m is mantled", 2, 1.0f, 4f, TraversalKind.Mantle);
            Case("ledge 2.0 m is mantled", 3, 2.0f, 4f, TraversalKind.Mantle);
            Case("roof edge 3.0 m is a grab", 4, 3.0f, 4f, TraversalKind.Grab);
            Case("wall 4.5 m is nothing", 5, 4.5f, 4f, TraversalKind.None);
            Case("thin wall 1.2 m with a 0.3 m drop beyond is mantled, not vaulted", 6, 1.2f, 0.3f,
                TraversalKind.Mantle, farFloor: 0.9f);

            // Airborne: feet 1.6 m up in front of the 3 m block — hands reach.
            {
                var hit = TraversalProbe.Scan(new Vector3(40f, 1.6f, -0.6f - Radius), Vector3.forward,
                    Radius, Height, 0.9f, airborne: true);
                Report("airborne past a 3.0 m edge catches it", hit.kind == TraversalKind.Grab, hit);
            }
            // Airborne, ledge below the waist: just land on it.
            {
                var hit = TraversalProbe.Scan(new Vector3(30f, 1.8f, -0.6f - Radius), Vector3.forward,
                    Radius, Height, 0.9f, airborne: true);
                Report("airborne above a 2.0 m ledge is a landing, not a grab", hit.kind == TraversalKind.None, hit);
            }

            // A 2 m ledge under a balcony with no headroom: the balcony's own lip
            // is what the hands go for.
            Box("Balcony", new Vector3(70f, 2f + 0.9f + 0.15f, 2f), new Vector3(4f, 0.3f, 4f));
            Case("ledge under a low balcony: the balcony lip is the grab", 7, 2.0f, 4f, TraversalKind.Grab,
                wantHeight: 3.2f);
            // The same ledge under a solid wall that goes on up: nothing to do.
            Box("SolidAbove", new Vector3(80f, 2f + 0.9f + 1.5f, 2f), new Vector3(4f, 3f, 4f));
            Case("ledge under a solid overhang is refused", 8, 2.0f, 4f, TraversalKind.None);

            // A 30° ramp: the sweep sees a face whose normal points well upward.
            {
                var ramp = Box("Ramp", new Vector3(90f, 0f, 2f), new Vector3(4f, 0.4f, 6f));
                ramp.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);
                Physics.SyncTransforms();
                var hit = TraversalProbe.Scan(new Vector3(90f, 0f, -1.5f), Vector3.forward, Radius, Height, Reach, false);
                Report("ramp is walked, not climbed", hit.kind == TraversalKind.None, hit);
            }

            // Glancing approach: a wall hit at 70° off is not "ahead".
            {
                var dir = Quaternion.Euler(0f, 70f, 0f) * Vector3.forward;
                var hit = TraversalProbe.Scan(new Vector3(30f, 0f, -0.6f - Radius), dir, Radius, Height, Reach, false);
                Report("glancing angle is ignored", hit.kind == TraversalKind.None, hit);
            }

            Debug.Log(_fails == 0 ? "[TRV] PASS — every course case classified as authored"
                : $"[TRV] FAIL — {_fails} case(s) misclassified");
            if (Application.isBatchMode && !EmberCampaignBatch.Chained) EditorApplication.Exit(_fails == 0 ? 0 : 1);
        }

        private static void Case(string label, int slot, float height, float depth, TraversalKind want,
            float farFloor = 0f, float wantHeight = -1f)
        {
            if (wantHeight < 0f) wantHeight = height;
            var x = slot * 10f;
            // Face at z = 0; the block extends +Z by `depth`.
            Box($"Obstacle{slot}", new Vector3(x, height * 0.5f, depth * 0.5f), new Vector3(4f, height, depth));
            if (farFloor > 0f)
                Box($"Far{slot}", new Vector3(x, farFloor * 0.5f, depth + 2f), new Vector3(4f, farFloor, 4f));
            Physics.SyncTransforms();
            var feet = new Vector3(x, 0f, -0.6f - Radius);
            var hit = TraversalProbe.Scan(feet, Vector3.forward, Radius, Height, Reach, false);
            var ok = hit.kind == want;
            if (ok && want != TraversalKind.None)
            {
                // The landing must be on top (or beyond, for a vault) and standable.
                ok &= Mathf.Abs(hit.height - wantHeight) < 0.08f || want == TraversalKind.Vault;
                if (want == TraversalKind.Vault) ok &= hit.landing.z > depth && hit.landing.y < height - 0.4f;
                else ok &= Mathf.Abs(hit.landing.y - wantHeight) < 0.08f && hit.landing.z > 0f;
            }
            Report(label, ok, hit);
        }

        private static void Report(string label, bool ok, TraversalHit hit)
        {
            if (!ok) _fails++;
            Debug.Log($"[TRV] {(ok ? "pass" : "FAIL")}  {label}  → {hit.kind} h={hit.height:0.00} " +
                      $"landing=({hit.landing.x:0.0},{hit.landing.y:0.00},{hit.landing.z:0.0})");
        }

        private static GameObject Box(string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = centre;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            return go;
        }
    }
}
