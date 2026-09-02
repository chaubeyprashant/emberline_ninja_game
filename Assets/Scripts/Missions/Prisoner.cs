using System.Collections.Generic;
using Emberline.Core;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// Someone the raiders were keeping. Walk to one to cut it loose; it then
    /// runs for the edge of the arena on its own. Prisoners are never a fail
    /// state on their own — leaving them costs the optional objective, not the
    /// mission, so the choice to press on stays a real one.
    /// </summary>
    public class Prisoner : MonoBehaviour
    {
        public static readonly List<Prisoner> Active = new();
        public static int Total { get; private set; }
        public static int Freed { get; private set; }

        public static void ResetCount() { Total = 0; Freed = 0; Active.Clear(); }

        private const float FreeRange = 2.0f;

        private NinjaRig _rig;
        private bool _free;
        private Vector3 _runTo;

        public static Prisoner Spawn(Vector3 at)
        {
            var go = new GameObject("Prisoner");
            go.transform.position = at;

            var rig = go.AddComponent<NinjaRig>();
            rig.bodyColor = new Color(0.42f, 0.38f, 0.34f);
            rig.accentColor = new Color(0.75f, 0.68f, 0.5f);
            rig.hasSword = false;
            rig.hasScarf = false;
            rig.maskStripe = false;
            rig.rigScale = 0.9f;

            var p = go.AddComponent<Prisoner>();
            p._rig = rig;
            p._runTo = at.normalized * 18f;
            Active.Add(p);
            Total++;
            return p;
        }

        private void OnDestroy() => Active.Remove(this);

        private void Update()
        {
            if (GameManager.CinematicActive) return;

            if (!_free)
            {
                var motor = SceneRefs.Motor;
                if (motor == null) return;
                var d = motor.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > FreeRange * FreeRange) return;
                _free = true;
                Freed++;
                Sfx3D.Ui();
                FloatingText.Spawn(transform.position + Vector3.up * 2.3f,
                    $"FREED — {Freed}/{Total}", new Color(0.6f, 0.9f, 0.7f), 1.2f);
                return;
            }

            var to = _runTo - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.5f) { Destroy(gameObject); return; }
            transform.position += to.normalized * (3.8f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(to), 8f * Time.deltaTime);
            if (_rig != null) _rig.move01 = 1f;
        }
    }
}
