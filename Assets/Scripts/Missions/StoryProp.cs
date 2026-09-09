using Emberline.Core;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>What a story prop is built from. Assembled out of the dressing library.</summary>
    public enum StoryPropShape
    {
        Marker,        // a lit point — the fallback
        Shrine,        // standing stone and a doused torch: the thing the fire missed
        TrainingPost,  // a father's post, weathered rather than burned
        Keepsake,      // small, low, and red: the one coloured thing in a grey mission
        Body,          // a corpse and what it was carrying
        Tracks,        // disturbed ash going somewhere
        Camp,          // a fire ring, bedding and the things people leave out
        Supply,        // stacked crates, kegs, and a lantern to work by
        Lookout,       // the high post: a ladder, a rail, a signal lantern
        CommandPost,   // a table someone works at: orders, a banner, lamplight
        Homestead,     // what is left of a house: beams, a doorway, household things
        Cache,         // a stone nobody would move, and what is under it
        KeyPiece,      // small, worked metal: three grooves and a missing section
        StoneMarker,   // a weathered post with a partial mark cut into it
        Passage,       // a stone panel that is not quite part of the wall
    }

    /// <summary>
    /// One authored discovery: a named object at a known place, with its own line
    /// and optionally its own cinematic.
    ///
    /// <para>
    /// The existing <c>Clue</c> is a glowing cube dropped at a procedural ring
    /// position. That is right for "search the camp for orders" and useless for
    /// "the bracelet in the ashes of your sister's room" — a scene needs the
    /// object to be a specific thing in a specific place. This is that.
    /// </para>
    /// </summary>
    [System.Serializable]
    public class StoryPropSpec
    {
        [Tooltip("Stable id. Diagnostics only.")]
        public string id = "";

        [Tooltip("Shown when the player is close enough to read it.")]
        public string label = "";

        [Tooltip("Arena-space position.")]
        public Vector3 point;

        public StoryPropShape shape = StoryPropShape.Marker;

        [Tooltip("Spoken on discovery. Empty for silence — which is sometimes the point.")]
        public string speaker = "";
        [TextArea] public string line = "";

        [Tooltip("Story beat played on discovery. Empty for none.")]
        public string beatId = "";

        [Tooltip("How close the player must be.")]
        public float radius = 2.2f;

        [Tooltip("Light the enemy signal line when this is found — three lanterns " +
                 "answering each other across the valley. Points are world-space.")]
        public Vector3[] lanternLine = System.Array.Empty<Vector3>();
    }

    /// <summary>
    /// The live prop. Walking into it is the interaction — this game has no
    /// interact button and adding one for a single mission would be a worse
    /// answer than walking somewhere, which the player already knows how to do.
    /// </summary>
    public class StoryProp : MonoBehaviour
    {
        public StoryPropSpec spec;

        /// <summary>Already discovered — the marker moves past it.</summary>
        public bool Taken => _taken;
        private bool _taken;
        private Transform _label;

        public static StoryProp Build(StoryPropSpec spec, Transform parent)
        {
            var go = new GameObject($"StoryProp_{spec.id}");
            go.transform.SetParent(parent, false);
            go.transform.position = spec.point;
            var prop = go.AddComponent<StoryProp>();
            prop.spec = spec;
            prop.Assemble();
            return prop;
        }

        private void Assemble()
        {
            switch (spec.shape)
            {
                case StoryPropShape.Shrine:
                    Dress("column", Vector3.zero, 0f, 0.9f);
                    Dress("torch_lit", new Vector3(0.9f, 0f, 0.2f), 20f, 1f, doused: true);
                    Glow(new Vector3(0f, 1.5f, 0f), new Color(0.82f, 0.86f, 1f), 0.22f);
                    break;

                case StoryPropShape.TrainingPost:
                    // A post, not rubble: the weather took it, not the fire.
                    Bar(new Vector3(0f, 0.85f, 0f), new Vector3(0.16f, 1.7f, 0.16f),
                        new Color(0.24f, 0.20f, 0.16f));
                    Dress("box_small", new Vector3(-0.9f, 0f, 0.5f), 35f);
                    Glow(new Vector3(0f, 1.75f, 0f), new Color(0.9f, 0.84f, 0.7f), 0.18f);
                    break;

                case StoryPropShape.Keepsake:
                    // Small, low, and the only red in the mission.
                    Bar(new Vector3(0f, 0.10f, 0f), new Vector3(0.26f, 0.05f, 0.26f),
                        new Color(0.62f, 0.11f, 0.12f));
                    Glow(new Vector3(0f, 0.30f, 0f), new Color(0.95f, 0.35f, 0.32f), 0.30f);
                    break;

                case StoryPropShape.Body:
                    Bar(new Vector3(0f, 0.16f, 0f), new Vector3(0.55f, 0.28f, 1.5f),
                        new Color(0.12f, 0.12f, 0.15f));
                    Dress("box_small", new Vector3(1.0f, 0f, -0.4f), 60f, 0.8f);
                    Glow(new Vector3(0f, 0.6f, 0f), new Color(0.85f, 0.9f, 1f), 0.22f);
                    break;

                case StoryPropShape.Camp:
                    // Slept in, cooked in, worked in — and only just left.
                    Dress("keg", new Vector3(-1.3f, 0f, 0.7f), 15f);
                    Dress("box_small", new Vector3(1.2f, 0f, 0.9f), 70f);
                    Dress("torch_lit", new Vector3(0f, 0f, -1.4f), 0f);
                    Patch(new Vector3(0f, 0.03f, 0f), 1.5f, new Color(0.09f, 0.08f, 0.075f));
                    Bar(new Vector3(0.9f, 0.09f, -0.5f), new Vector3(1.5f, 0.16f, 0.6f),
                        new Color(0.30f, 0.27f, 0.21f));   // bedroll
                    Glow(new Vector3(0f, 0.55f, 0f), new Color(1f, 0.6f, 0.3f), 0.24f);
                    break;

                case StoryPropShape.Supply:
                    // Identical crates, stacked by someone who expects to come back.
                    Dress("crates_stacked", Vector3.zero, 20f, 1.05f);
                    Dress("box_large", new Vector3(1.9f, 0f, 0.4f), -25f);
                    Dress("barrel_large", new Vector3(-1.7f, 0f, 0.8f), 40f);
                    Dress("torch_lit", new Vector3(0.4f, 0f, -1.8f), 0f);
                    Glow(new Vector3(0f, 1.4f, 0f), new Color(0.85f, 0.9f, 1f), 0.22f);
                    break;

                case StoryPropShape.Lookout:
                    // A post to see the valley from, and a lantern to answer with.
                    Dress("crates_stacked", new Vector3(0.9f, 0f, 0.6f), 0f, 1.1f);
                    Bar(new Vector3(0f, 1.3f, 0f), new Vector3(0.18f, 2.6f, 0.18f),
                        new Color(0.22f, 0.19f, 0.16f));
                    Bar(new Vector3(0f, 2.5f, 0f), new Vector3(1.6f, 0.12f, 0.12f),
                        new Color(0.22f, 0.19f, 0.16f));
                    Glow(new Vector3(0f, 2.75f, 0f), new Color(1f, 0.62f, 0.3f), 0.3f);
                    break;

                case StoryPropShape.CommandPost:
                    // Not a camp. Somebody works here: a table, light to read by,
                    // and a banner hung where the men can see whose orders these are.
                    Dress("table_small", Vector3.zero, 12f, 1.15f);
                    Dress("torch_lit", new Vector3(-1.5f, 0f, 0.6f), 0f);
                    Dress("torch_lit", new Vector3(1.6f, 0f, 0.5f), 0f);
                    Dress("banner_red", new Vector3(0f, 1.55f, -2.2f), 180f, 1.2f);
                    Dress("chest", new Vector3(2.2f, 0f, -1.2f), -40f);
                    Dress("crates_stacked", new Vector3(-2.6f, 0f, -1.4f), 25f, 0.95f);
                    Glow(new Vector3(0f, 1.05f, 0f), new Color(0.95f, 0.88f, 0.6f), 0.24f);
                    break;

                case StoryPropShape.Homestead:
                    // A home, not a ruin in general: the things people owned are
                    // still lying where the roof came down on them.
                    Dress("rubble_large", new Vector3(-1.6f, 0f, 0.9f), 25f, 1.1f);
                    Dress("column", new Vector3(1.8f, 0f, 1.2f), 0f, 0.8f);
                    Dress("table_small", new Vector3(0.2f, 0f, -0.9f), 40f);
                    Dress("chest", new Vector3(-2.2f, 0f, -1.3f), -20f);
                    Dress("barrel_small", new Vector3(2.4f, 0f, -1.6f), 60f);
                    Patch(new Vector3(0f, 0.03f, 0f), 2.4f, new Color(0.10f, 0.09f, 0.08f));
                    Glow(new Vector3(0f, 1f, 0f), new Color(0.9f, 0.86f, 0.72f), 0.22f);
                    break;

                case StoryPropShape.Cache:
                    // A stone nobody would think to move, and a box under it.
                    Bar(new Vector3(0f, 0.14f, 0f), new Vector3(1.5f, 0.28f, 1.1f),
                        new Color(0.29f, 0.28f, 0.26f));
                    Dress("box_small", new Vector3(0.7f, 0f, -0.9f), 30f, 0.9f);
                    Glow(new Vector3(0f, 0.5f, 0f), new Color(0.95f, 0.45f, 0.38f), 0.26f);
                    break;

                case StoryPropShape.KeyPiece:
                    // Small and deliberate. It reads as made, not as debris.
                    Bar(new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.06f, 0.5f),
                        new Color(0.44f, 0.42f, 0.36f));
                    Bar(new Vector3(0f, 0.24f, 0.18f), new Vector3(0.1f, 0.05f, 0.1f),
                        new Color(0.5f, 0.47f, 0.4f));
                    Glow(new Vector3(0f, 0.45f, 0f), new Color(0.92f, 0.86f, 0.62f), 0.24f);
                    break;

                case StoryPropShape.StoneMarker:
                    // Old, deliberate, and half swallowed by the hill.
                    Dress("column", Vector3.zero, 0f, 0.62f);
                    Bar(new Vector3(0f, 0.9f, 0.22f), new Vector3(0.34f, 0.34f, 0.06f),
                        new Color(0.38f, 0.36f, 0.33f));
                    Glow(new Vector3(0f, 1.15f, 0f), new Color(0.86f, 0.9f, 1f), 0.2f);
                    break;

                case StoryPropShape.Passage:
                    // A slab that does not match the courses around it.
                    Bar(new Vector3(0f, 1.05f, 0f), new Vector3(1.7f, 2.1f, 0.35f),
                        new Color(0.26f, 0.25f, 0.24f));
                    Dress("rubble_half", new Vector3(-1.5f, 0f, 0.4f), 30f, 0.9f);
                    Dress("torch_lit", new Vector3(1.4f, 0f, 0.3f), 0f, 1f, doused: true);
                    Glow(new Vector3(0f, 1.3f, 0.3f), new Color(0.9f, 0.84f, 0.66f), 0.26f);
                    break;

                case StoryPropShape.Tracks:
                    for (var i = 0; i < 5; i++)
                        Patch(new Vector3((i % 2 == 0 ? 0.22f : -0.22f), 0.03f, i * 0.75f),
                            0.42f, new Color(0.10f, 0.09f, 0.08f));
                    Glow(new Vector3(0f, 0.5f, 1.5f), new Color(0.7f, 0.9f, 1f), 0.2f);
                    break;

                default:
                    Glow(new Vector3(0f, 0.8f, 0f), new Color(0.7f, 0.9f, 1f), 0.28f);
                    break;
            }
        }

        // ---- assembly helpers ------------------------------------------------

        private void Dress(string propName, Vector3 offset, float yaw, float scale = 1f,
            bool doused = false)
        {
            var prefab = Resources.Load<GameObject>("Props/Dressing/" + propName);
            if (prefab == null) return;
            var go = Instantiate(prefab, transform.position + offset,
                Quaternion.Euler(0f, yaw, 0f), transform);
            go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (doused)
                foreach (var l in go.GetComponentsInChildren<Light>(true)) l.enabled = false;
        }

        private void Bar(Vector3 offset, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            go.transform.localScale = size;
            Paint(go, c, glow: false);
        }

        private void Patch(Vector3 offset, float size, Color c)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            q.transform.localPosition = offset;
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(size, size * 1.5f, 1f);
            Paint(q, c, glow: false);
        }

        /// <summary>The read: a small pulse so the eye finds it without a waypoint.</summary>
        private void Glow(Vector3 offset, Color c, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Read";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            go.transform.localScale = Vector3.one * size;
            Paint(go, c, glow: true);
            _label = go.transform;
        }

        private static void Paint(GameObject go, Color c, bool glow)
        {
            var shader = Shader.Find(glow ? "Emberline/Glow" : "Emberline/Toon");
            var r = go.GetComponent<Renderer>();
            if (shader != null) r.material = new Material(shader) { color = c };
            else r.material.color = c;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- the interaction -------------------------------------------------

        private void Update()
        {
            if (_label != null)
            {
                var s = 1f + 0.12f * Mathf.Sin(Time.time * 2.4f);
                _label.localScale = Vector3.one * (_label.localScale.x > 0f ? s * 0.24f : 0.24f);
            }
            if (_taken) return;

            var motor = SceneRefs.Motor;
            if (motor == null) return;
            var d = motor.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > spec.radius * spec.radius) return;

            _taken = true;
            FxPools.Sparks(transform.position + Vector3.up * 0.6f,
                new Color(0.82f, 0.9f, 1f), 6);
            MissionDirector.Active?.OnStoryPropFound(spec);
            if (_label != null) Destroy(_label.gameObject);
        }
    }
}
