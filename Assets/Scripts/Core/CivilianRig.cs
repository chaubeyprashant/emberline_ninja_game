using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Builds the unarmed people of the world — villagers, prisoners, lantern
    /// bearers — as one silhouette.
    ///
    /// They used to be raw <see cref="NinjaRig"/> primitives because no imported
    /// civilian existed. One does now (Resources/Prefabs/VillagerPrefab), and a
    /// blocky stand-in reads as a bug when it stands next to the skeletal cast,
    /// so the prefab is the normal path. The primitive build stays as the
    /// fallback, which keeps the old guarantee that these missions still populate
    /// if the prefab is ever missing.
    /// </summary>
    public static class CivilianRig
    {
        /// <summary>
        /// Spawn an unarmed civilian at `at`. `body`/`accent` only apply to the
        /// primitive fallback; the imported prefab carries its own materials.
        /// </summary>
        public static CharacterRig Spawn(string name, Vector3 at, Color body, Color accent,
                                         float scale = 1f, bool scarf = false)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/VillagerPrefab");
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, at, Quaternion.identity);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.position = at;
            }

            var rig = go.GetComponent<CharacterRig>();
            if (rig == null)
            {
                var ninja = go.AddComponent<NinjaRig>();
                ninja.bodyColor = body;
                ninja.accentColor = accent;
                ninja.hasSword = false;
                ninja.hasScarf = scarf;
                ninja.maskStripe = false;
                ninja.rigScale = scale;
                rig = ninja;
            }
            else
            {
                go.transform.localScale = Vector3.one * scale;
            }

            // On terrain these movers would walk at their spawn height: they
            // drive their transform directly and have no collider against the
            // ground. One hugger keeps all three on the surface.
            go.AddComponent<GroundHug>();

            return rig;
        }
    }
}
