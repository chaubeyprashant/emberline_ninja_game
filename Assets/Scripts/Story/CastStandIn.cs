using System.Collections.Generic;
using Emberline.Core;
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// Puts the people a cutscene talks about in front of Renzo.
    ///
    /// <para>
    /// The mission scenes contain no cast, so this used to build every Father,
    /// Aiko, Goro and Kagachi from cubes at runtime, only for shot subjects — a
    /// SOLDIER who only ever speaks got nobody at all — and never removed them,
    /// so a primitive Goro stood beside the real Goro boss for the rest of the
    /// mission. Now each role loads its real character from
    /// Resources/Prefabs/Cast (built by EmberCastPrefabs), a named foe already
    /// fighting in the scene plays their own part, speakers are covered as well
    /// as subjects, each actor gets its own spot, and <see cref="ReleaseAll"/>
    /// clears them when the beat ends. The primitive rig remains only as the
    /// fallback for a role with no prefab, and for <c>DistantFigure</c>, which is
    /// deliberately faceless.
    /// </para>
    /// </summary>
    public class CastStandIn : MonoBehaviour
    {
        private static readonly List<GameObject> Spawned = new();
        private static readonly List<CastMember> Borrowed = new();

        /// <summary>Make sure a named cast member exists near the player.</summary>
        public static Transform Ensure(string castName) => Ensure(castName, 0, primitive: false, track: true);

        /// <summary>
        /// A featureless primitive stand-in, for a figure whose identity the scene
        /// must not confirm. Not released with the beat; its owner removes it.
        /// </summary>
        public static Transform EnsurePrimitive(string castName) =>
            Ensure(castName, 0, primitive: true, track: false);

        /// <summary>Every subject and speaker a beat needs that the scene lacks.</summary>
        public static void EnsureFor(StoryBeat beat)
        {
            if (beat == null) return;
            var slot = 0;
            foreach (var shot in beat.shots)
            {
                foreach (var who in new[] { shot.subject, shot.speaker })
                {
                    if (string.IsNullOrWhiteSpace(who)) continue;
                    var name = who.Trim();
                    if (name.ToUpperInvariant() == "CAMERA") continue;
                    if (Cast.Find(name) != null) continue;
                    Ensure(name, IsPlayer(name) ? 0 : slot++, primitive: false, track: true);
                }
            }
        }

        /// <summary>Remove every actor the last beat placed, and untag borrowed foes.</summary>
        public static void ReleaseAll()
        {
            foreach (var go in Spawned)
                if (go != null) Destroy(go);
            Spawned.Clear();
            foreach (var tag in Borrowed)
                if (tag != null) Destroy(tag);
            Borrowed.Clear();
        }

        private static bool IsPlayer(string name) =>
            name.ToUpperInvariant() is "RENZO" or "REN" or "PLAYER";

        private static Transform Ensure(string castName, int slot, bool primitive, bool track)
        {
            var found = Cast.Find(castName);
            if (found != null) return found;

            var motor = SceneRefs.Motor;
            var upper = castName.ToUpperInvariant();
            if (IsPlayer(castName))
            {
                if (motor == null) return null;
                var pc = motor.gameObject.GetComponent<CastMember>() ?? motor.gameObject.AddComponent<CastMember>();
                pc.castName = "RENZO";
                Cast.Register(pc);
                return motor.transform;
            }

            // A named foe already in the fight plays their own part: the Goro
            // boss is Goro, not a second Goro standing next to him.
            if (!primitive)
            {
                var live = LiveFoe(upper);
                if (live != null)
                {
                    var tag = live.gameObject.AddComponent<CastMember>();
                    tag.castName = castName;
                    Cast.Register(tag);
                    Borrowed.Add(tag);
                    return live.transform;
                }
            }

            var at = SlotPosition(motor, slot);
            var rotation = Quaternion.identity;
            if (motor != null)
            {
                var toPlayer = motor.transform.position - at;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f) rotation = Quaternion.LookRotation(toPlayer);
            }

            var prefab = primitive ? null : Resources.Load<GameObject>("Prefabs/Cast/" + upper);
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, at, rotation);
                go.name = "Cast_" + upper;
                go.AddComponent<GroundHug>();
            }
            else
            {
                go = PrimitiveFigure(castName, at, rotation);
            }

            var cm = go.GetComponent<CastMember>() ?? go.AddComponent<CastMember>();
            cm.castName = castName;
            Cast.Register(cm);
            go.AddComponent<CastStandIn>();
            if (track) Spawned.Add(go);
            return go.transform;
        }

        /// <summary>
        /// Where the n-th actor of a beat stands: a loose arc in front of Renzo,
        /// alternating sides, so two characters are never inside each other.
        /// </summary>
        private static Vector3 SlotPosition(Player.PlayerLocomotion motor, int slot)
        {
            if (motor == null) return Vector3.zero;
            var forward = motor.Facing;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            var side = slot == 0 ? 0f : (slot % 2 == 1 ? 1f : -1f) * 1.4f * ((slot + 1) / 2);
            var depth = 2.6f + 0.5f * ((slot + 1) / 2);
            return Ground.Snap(motor.transform.position + forward * depth + right * side);
        }

        private static EnemyBrain LiveFoe(string castName)
        {
            EnemyKind? kind = castName switch
            {
                "GORO" => EnemyKind.Chief,
                "JIN" => EnemyKind.Jin,
                "KAGACHI" => EnemyKind.Kagachi,
                _ => null,
            };
            if (kind == null) return null;
            foreach (var brain in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                if (brain.kind == kind && !brain.Dead && brain.isActiveAndEnabled)
                    return brain;
            return null;
        }

        private static GameObject PrimitiveFigure(string castName, Vector3 at, Quaternion rotation)
        {
            var go = new GameObject($"PLACEHOLDER_{castName}_StandIn");
            go.transform.SetPositionAndRotation(at, rotation);

            var upper = castName.ToUpperInvariant();
            var rig = go.AddComponent<NinjaRig>();
            var (body, accent) = upper switch
            {
                "AIKO" => (new Color(0.52f, 0.20f, 0.18f), new Color(0.85f, 0.2f, 0.2f)),   // red thread
                "JIN" => (new Color(0.12f, 0.12f, 0.15f), new Color(0.55f, 0.6f, 0.7f)),
                "FATHER" => (new Color(0.30f, 0.26f, 0.22f), new Color(0.7f, 0.6f, 0.4f)),
                "KAGACHI" => (new Color(0.10f, 0.14f, 0.12f), new Color(0.3f, 0.75f, 0.5f)),
                _ => (new Color(0.35f, 0.32f, 0.28f), new Color(0.6f, 0.55f, 0.45f)),
            };
            rig.bodyColor = body;
            rig.accentColor = accent;
            rig.hasSword = upper is "JIN" or "FATHER" or "KAGACHI";
            rig.hasScarf = upper == "AIKO";
            rig.maskStripe = false;
            rig.rigScale = upper == "AIKO" ? 0.93f : 1f;
            return go;
        }
    }
}
