using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Recycles enemy instances instead of Instantiate/Destroy per wave. Spawning
    /// a skeletal enemy builds a full rig, an AnimatorController binding and a
    /// telegraph ring; on the Road North that happened every time a pack was
    /// triggered, which is exactly when the frame budget is tightest.
    ///
    /// Keyed by prefab, following the same "skip destroyed entries" pattern the
    /// FloatingText and Kunai pools use — a scene load destroys pooled instances
    /// and the queue drains itself on the next spawn.
    /// </summary>
    public static class EnemyPool
    {
        private static readonly Dictionary<GameObject, Queue<EnemyBrain>> Pools = new();

        /// <summary>
        /// Drop every queued instance. A scene load destroys the pooled objects
        /// but not the queues that point at them, so without this the next scene
        /// starts by dequeuing a pile of dead references before it can spawn.
        /// Called once per scene from GameManager.Awake.
        /// </summary>
        public static void Clear() => Pools.Clear();

        public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null) return null;
            if (!Pools.TryGetValue(prefab, out var queue))
                Pools[prefab] = queue = new Queue<EnemyBrain>();

            EnemyBrain brain = null;
            while (queue.Count > 0 && brain == null) brain = queue.Dequeue(); // skip destroyed

            if (brain == null)
            {
                var spawned = Object.Instantiate(prefab, pos, rot);
                brain = spawned.GetComponent<EnemyBrain>();
                if (brain == null) return spawned; // no brain: caller owns its lifetime
                brain.poolKey = prefab;
                return spawned;
            }

            var go = brain.gameObject;
            go.transform.SetPositionAndRotation(pos, rot);
            brain.ResetForSpawn();
            go.SetActive(true); // OnEnable re-registers it in EnemyBrain.Active
            return go;
        }

        /// <summary>
        /// Return a dead enemy. Instances without a pool key (Kagachi's mirror
        /// clones, which are cloned from a live object rather than a prefab) are
        /// destroyed as before.
        /// </summary>
        public static void Release(EnemyBrain brain)
        {
            if (brain == null) return;
            if (brain.poolKey == null)
            {
                Object.Destroy(brain.gameObject);
                return;
            }
            brain.gameObject.SetActive(false);
            if (!Pools.TryGetValue(brain.poolKey, out var queue))
                Pools[brain.poolKey] = queue = new Queue<EnemyBrain>();
            queue.Enqueue(brain);
        }
    }
}
