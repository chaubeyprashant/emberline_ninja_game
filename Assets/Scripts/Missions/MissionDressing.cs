using Emberline.Core;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>What a mission leaves lying around for the player to read.</summary>
    public enum DressingKind
    {
        None,
        BurnedHome,        // rubble and scorch where a house stood
        EmptyHome,         // a doused torch, an open chest, nobody home
        DestroyedCart,     // a tipped cart and its spilled load
        AbandonedWeapons,  // blades left in the ground where people dropped them
        BloodTrail,        // used sparingly: one trail, going somewhere
        MissingNotice,     // a paper nailed to a post
        PrisonerCamp,      // a pen, a watch-torch, and people in it
        HidingVillagers,   // people who stayed, pressed into the corners
        KagehiraBanners,   // the serpent, hung where it can be seen
    }

    /// <summary>
    /// Places a mission's environmental storytelling when it starts.
    ///
    /// The world is supposed to say what happened without dialogue, so this is
    /// deliberately physical: rubble where a home was, a cart nobody righted,
    /// weapons left where their owners dropped them. Placement is seeded by the
    /// mission id, so a mission looks the same every time you replay it — a
    /// scene that reshuffles itself on retry reads as noise, not as a place.
    ///
    /// Everything is parked around the arena edge, clear of the middle where the
    /// fighting happens, so none of it becomes an obstacle in a fight.
    /// </summary>
    public static class MissionDressing
    {
        private static GameObject _root;

        public static void Clear()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }

        public static void Build(MissionPlan plan, Vector2 halfExtents)
        {
            Clear();
            if (plan == null || plan.dressing == null || plan.dressing.Length == 0) return;

            _root = new GameObject("MissionDressing");
            var state = Random.state;
            Random.InitState(plan.id * 7919 + 13);

            var n = plan.dressing.Length;
            for (var i = 0; i < n; i++)
            {
                // Spread the clusters around the perimeter rather than clumping:
                // the player should meet the story on the way to things.
                var angle = (i + 0.5f) / n * Mathf.PI * 2f + 0.6f;
                var at = new Vector3(
                    Mathf.Cos(angle) * (halfExtents.x - 2.2f),
                    0f,
                    Mathf.Sin(angle) * (halfExtents.y - 1.8f));
                Place(plan.dressing[i], at, angle * Mathf.Rad2Deg);
            }

            Random.state = state;
        }

        private static void Place(DressingKind kind, Vector3 at, float yaw)
        {
            switch (kind)
            {
                case DressingKind.BurnedHome:
                    Prop("rubble_large", at, yaw, 1.25f);
                    Prop("rubble_half", at + Off(1.8f), yaw + 40f, 1.1f);
                    Scorch(at + Off(0.9f), 3.4f);
                    break;

                case DressingKind.EmptyHome:
                    Prop("torch_lit", at, yaw, 1.1f, doused: true);
                    Prop("chest", at + Off(1.4f), yaw - 30f);
                    Prop("box_small", at + Off(2.2f), yaw + 70f);
                    break;

                case DressingKind.DestroyedCart:
                    // Tipped onto its side: the load went with it.
                    var cart = Prop("table_small", at, yaw, 1.15f);
                    if (cart != null) cart.transform.rotation = Quaternion.Euler(0f, yaw, 74f);
                    Prop("barrel_large", at + Off(1.5f), yaw + 20f);
                    Prop("box_small", at + Off(2.3f), yaw - 55f);
                    Prop("keg", at + Off(1.1f), yaw + 120f);
                    break;

                case DressingKind.AbandonedWeapons:
                    Prop("crates_stacked", at, yaw, 0.9f);
                    for (var i = 0; i < 4; i++) DroppedBlade(at + Off(1.6f + i * 0.5f));
                    break;

                case DressingKind.BloodTrail:
                    BloodTrail(at, yaw);
                    break;

                case DressingKind.MissingNotice:
                    Notice(at, yaw);
                    break;

                case DressingKind.PrisonerCamp:
                    Pen(at, yaw);
                    break;

                case DressingKind.HidingVillagers:
                    Prop("crates_stacked", at, yaw, 1f);
                    Villager.Spawn(at + Off(1.3f), new Color(0.34f, 0.30f, 0.26f));
                    Villager.Spawn(at + Off(2.1f), new Color(0.40f, 0.33f, 0.28f));
                    break;

                case DressingKind.KagehiraBanners:
                    Prop("banner_red", at + Vector3.up * 1.55f, yaw + 180f, 1.2f);
                    Prop("banner_thin_red", at + Off(2.6f) + Vector3.up * 1.55f, yaw + 180f, 1.2f);
                    break;
            }
        }

        // ------------------------------------------------------------ pieces

        private static Vector3 Off(float r)
        {
            var a = Random.value * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r * 0.7f);
        }

        private static GameObject Prop(string name, Vector3 at, float yaw, float scale = 1f,
            bool doused = false)
        {
            var prefab = Resources.Load<GameObject>("Props/Dressing/" + name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, at, Quaternion.Euler(0f, yaw, 0f), _root.transform);
            go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (doused)
                foreach (var l in go.GetComponentsInChildren<Light>(true)) l.enabled = false;
            return go;
        }

        /// <summary>A flat dark patch. Ground scorch, or the shape of a fire.</summary>
        private static void Scorch(Vector3 at, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "Scorch";
            q.transform.SetParent(_root.transform, false);
            q.transform.position = new Vector3(at.x, 0.035f, at.z);
            q.transform.rotation = Quaternion.Euler(90f, Random.value * 360f, 0f);
            q.transform.localScale = new Vector3(size, size * 0.8f, 1f);
            Paint(q, new Color(0.07f, 0.06f, 0.055f));
        }

        /// <summary>
        /// Sparingly, and going somewhere: a trail says a person was moved, which
        /// is a different sentence from a stain, which only says a person died.
        /// </summary>
        private static void BloodTrail(Vector3 at, float yaw)
        {
            var dir = Quaternion.Euler(0f, yaw + 150f, 0f) * Vector3.forward;
            for (var i = 0; i < 6; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.name = "Blood";
                q.transform.SetParent(_root.transform, false);
                q.transform.position = at + dir * (i * 0.85f) + Vector3.up * 0.032f
                                       + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
                q.transform.rotation = Quaternion.Euler(90f, Random.value * 360f, 0f);
                var s = Mathf.Lerp(0.7f, 0.28f, i / 5f);
                q.transform.localScale = new Vector3(s, s * 0.75f, 1f);
                Paint(q, new Color(0.17f, 0.035f, 0.03f));
            }
        }

        /// <summary>A blade left point-down where somebody dropped it.</summary>
        private static void DroppedBlade(Vector3 at)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "DroppedBlade";
            go.transform.SetParent(_root.transform, false);
            go.transform.position = at + Vector3.up * 0.42f;
            go.transform.rotation = Quaternion.Euler(Random.Range(55f, 78f), Random.value * 360f, 0f);
            go.transform.localScale = new Vector3(0.07f, 0.9f, 0.02f);
            Paint(go, new Color(0.44f, 0.45f, 0.48f));
        }

        /// <summary>Paper on a post. Nobody has taken it down.</summary>
        private static void Notice(Vector3 at, float yaw)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(post.GetComponent<Collider>());
            post.name = "NoticePost";
            post.transform.SetParent(_root.transform, false);
            post.transform.position = at + Vector3.up * 0.85f;
            post.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            post.transform.localScale = new Vector3(0.1f, 1.7f, 0.1f);
            Paint(post, new Color(0.24f, 0.19f, 0.15f));

            var paper = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(paper.GetComponent<Collider>());
            paper.name = "MissingNotice";
            paper.transform.SetParent(_root.transform, false);
            paper.transform.position = at + Vector3.up * 1.28f
                                       + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.06f);
            paper.transform.rotation = Quaternion.Euler(0f, yaw + 180f, Random.Range(-6f, 6f));
            paper.transform.localScale = new Vector3(0.42f, 0.56f, 1f);
            Paint(paper, new Color(0.78f, 0.74f, 0.64f));
        }

        /// <summary>A pen of stakes with people inside it.</summary>
        private static void Pen(Vector3 at, float yaw)
        {
            for (var i = 0; i < 6; i++)
            {
                var a = i / 6f * Mathf.PI * 2f;
                Prop("column", at + new Vector3(Mathf.Cos(a) * 2.3f, 0f, Mathf.Sin(a) * 2.3f),
                    yaw, 0.55f);
            }
            Prop("torch_lit", at + Off(3.2f), yaw, 1.1f);
            Prisoner.Spawn(at + new Vector3(0.7f, 0f, 0.3f));
            Prisoner.Spawn(at + new Vector3(-0.6f, 0f, -0.5f));
            Prisoner.Spawn(at + new Vector3(0.1f, 0f, 0.9f));
        }

        private static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            r.material = SurfaceKit.Make(Surface.Stone, c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
