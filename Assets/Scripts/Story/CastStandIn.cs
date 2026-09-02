using Emberline.Core;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// A cast member for a scene that has no model for them. Adult Aiko and Jin
    /// exist in the story but not yet as gameplay characters; a beat that frames
    /// them mid-mission needs *something* to frame. This is a marked stand-in on
    /// the primitive rig, never a pretence that the asset exists — the object is
    /// named PLACEHOLDER so nobody mistakes it for finished work, and the model
    /// specification lives in docs/ASSET_SPECIFICATIONS.md.
    /// </summary>
    public class CastStandIn : MonoBehaviour
    {
        /// <summary>Make sure a named cast member exists near the player.</summary>
        public static Transform Ensure(string castName)
        {
            var found = Cast.Find(castName);
            if (found != null) return found;

            var motor = SceneRefs.Motor;
            var upper = castName.ToUpperInvariant();
            if (upper is "RENZO" or "REN" or "PLAYER")
            {
                if (motor == null) return null;
                var pc = motor.gameObject.GetComponent<CastMember>() ?? motor.gameObject.AddComponent<CastMember>();
                pc.castName = "RENZO";
                Cast.Register(pc);
                return motor.transform;
            }
            var at = motor != null ? motor.transform.position + motor.Facing * 2.4f : Vector3.zero;
            var go = new GameObject($"PLACEHOLDER_{castName}_StandIn");
            go.transform.position = at;
            if (motor != null) go.transform.rotation = Quaternion.LookRotation(-motor.Facing);

            var rig = go.AddComponent<NinjaRig>();
            var (body, accent) = castName.ToUpperInvariant() switch
            {
                "AIKO" => (new Color(0.52f, 0.20f, 0.18f), new Color(0.85f, 0.2f, 0.2f)),   // red thread
                "JIN" => (new Color(0.12f, 0.12f, 0.15f), new Color(0.55f, 0.6f, 0.7f)),
                "FATHER" => (new Color(0.30f, 0.26f, 0.22f), new Color(0.7f, 0.6f, 0.4f)),
                "KAGACHI" => (new Color(0.10f, 0.14f, 0.12f), new Color(0.3f, 0.75f, 0.5f)),
                _ => (new Color(0.35f, 0.32f, 0.28f), new Color(0.6f, 0.55f, 0.45f)),
            };
            rig.bodyColor = body;
            rig.accentColor = accent;
            rig.hasSword = castName.ToUpperInvariant() is "JIN" or "FATHER" or "KAGACHI";
            rig.hasScarf = castName.ToUpperInvariant() == "AIKO";
            rig.maskStripe = false;
            rig.rigScale = castName.ToUpperInvariant() == "AIKO" ? 0.93f : 1f;

            var cm = go.AddComponent<CastMember>();
            cm.castName = castName;
            Cast.Register(cm);
            go.AddComponent<CastStandIn>();
            return go.transform;
        }

        /// <summary>Every subject a beat frames that the scene lacks.</summary>
        public static void EnsureFor(StoryBeat beat)
        {
            if (beat == null) return;
            foreach (var shot in beat.shots)
            {
                if (string.IsNullOrEmpty(shot.subject)) continue;
                var name = shot.subject.ToUpperInvariant();
                if (name is "CAMERA" or "") continue;
                Ensure(shot.subject);
            }
        }
    }
}
