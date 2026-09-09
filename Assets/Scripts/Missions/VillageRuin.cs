using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// Burns the valley's village down for the missions set in a ruin.
    ///
    /// <para>
    /// The valley builds one village and it is a living one: roofs on, stalls up,
    /// fences straight. That is right for most of the campaign and wrong for
    /// Yorune, which has been ash for ten years — and a mission that opens on an
    /// intact market while Renzo says "this was my home" undoes its own scene.
    /// <c>SetState.Ruin</c> could not do it: that runs through
    /// <c>VillageSet</c>, which exists only in the authored opening scene, so in
    /// a generated valley it silently does nothing.
    /// </para>
    ///
    /// <para>
    /// So this takes the village apart in place: most of it goes, what is left is
    /// scorched ground and rubble where it stood, and the few buildings kept
    /// standing are the ones a fire leaves — the shells. Deterministic per
    /// mission, so a replay burns the same houses.
    /// </para>
    /// </summary>
    public static class VillageRuin
    {
        private static GameObject _rubble;

        /// <summary>Undo nothing — the scene is rebuilt per mission — but drop our own props.</summary>
        public static void Clear()
        {
            if (_rubble != null) Object.Destroy(_rubble);
            _rubble = null;
        }

        /// <summary>
        /// Ruin the village. <paramref name="keepFraction"/> of its structures
        /// survive as shells; the rest become scorch and rubble.
        /// </summary>
        public static void Apply(int seed, float keepFraction = 0.34f)
        {
            var zone = Object.FindFirstObjectByType<Core.ZoneWorld>();
            if (zone == null) return;
            var village = FindChild(zone.transform, "Village");
            if (village == null) return;

            Clear();
            _rubble = new GameObject("YoruneRuin");
            var state = Random.state;
            Random.InitState(seed * 7919 + 101);

            for (var i = village.childCount - 1; i >= 0; i--)
            {
                var child = village.GetChild(i);
                if (!child.gameObject.activeSelf) continue;

                // The well and the shrine stay: a fire takes the timber and
                // leaves the stone, and the mission needs landmarks to navigate by.
                var name = child.name.ToLowerInvariant();
                var stone = name.Contains("well") || name.Contains("shrine")
                            || name.Contains("torii") || name.Contains("stone");
                // Stone stands, but nothing here was spared the smoke — a green
                // roof on the well would be the brightest thing in a dead village.
                if (stone) { Char(child); Scorch(child.position, 3f); continue; }

                if (Random.value < keepFraction)
                {
                    // Standing, but not spared. Two thirds of the village gone
                    // still read as a village while the third left was white
                    // timber in daylight — a burned house has to look burned.
                    Char(child);
                    Scorch(child.position, Random.Range(3.4f, 5f));
                    continue;
                }

                Scorch(child.position, Random.Range(3.2f, 5.5f));
                Rubble(child.position, Random.Range(2, 4));
                child.gameObject.SetActive(false);
            }

            // Nobody lives here. Villagers wandering a dead village read as a bug.
            foreach (var v in Object.FindObjectsByType<Villager>(FindObjectsSortMode.None))
                Object.Destroy(v.gameObject);

            Random.state = state;
        }

        private static Transform FindChild(Transform root, string name)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var deeper = FindChild(c, name);
                if (deeper != null) return deeper;
            }
            return null;
        }

        /// <summary>Darken everything a surviving structure is made of.</summary>
        private static void Char(Transform structure)
        {
            foreach (var r in structure.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;   // instances: the shared asset is reused elsewhere
                for (var i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var c = mats[i].color;
                    // Toward soot, and most of the way there: a fire does not
                    // leave a tint, it leaves charcoal.
                    mats[i].color = Color.Lerp(c, new Color(0.11f, 0.10f, 0.095f), 0.74f);
                }
                r.materials = mats;
            }
        }

        private static void Scorch(Vector3 at, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "Scorch";
            q.transform.SetParent(_rubble.transform, false);
            q.transform.position = new Vector3(at.x, Core.Ground.HeightAt(at.x, at.z) + 0.04f, at.z);
            q.transform.rotation = Quaternion.Euler(90f, Random.value * 360f, 0f);
            q.transform.localScale = new Vector3(size, size * 0.85f, 1f);
            Paint(q, new Color(0.075f, 0.065f, 0.06f));
        }

        private static void Rubble(Vector3 at, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var name = Random.value < 0.5f ? "rubble_large" : "rubble_half";
                var prefab = Resources.Load<GameObject>("Props/Dressing/" + name);
                var off = new Vector3(Random.Range(-1.9f, 1.9f), 0f, Random.Range(-1.9f, 1.9f));
                var pos = at + off;
                pos.y = Core.Ground.HeightAt(pos.x, pos.z);
                if (prefab != null)
                {
                    var go = Object.Instantiate(prefab, pos,
                        Quaternion.Euler(0f, Random.value * 360f, 0f), _rubble.transform);
                    go.transform.localScale = Vector3.one * Random.Range(0.9f, 1.3f);
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    continue;
                }
                // No prop library: a charred beam still says the same thing.
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(_rubble.transform, false);
                beam.transform.position = pos + Vector3.up * 0.16f;
                beam.transform.rotation = Quaternion.Euler(Random.Range(-14f, 14f),
                    Random.value * 360f, Random.Range(-8f, 8f));
                beam.transform.localScale = new Vector3(0.28f, 0.28f, Random.Range(1.4f, 2.8f));
                Paint(beam, new Color(0.13f, 0.11f, 0.10f));
            }
        }

        private static void Paint(GameObject go, Color c)
        {
            var shader = Shader.Find("Emberline/Toon");
            var r = go.GetComponent<Renderer>();
            if (shader != null) r.material = new Material(shader) { color = c };
            else r.material.color = c;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
