using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.UI;

namespace Emberline
{
    /// <summary>
    /// Destructible arena lantern: breaking it drops a health pickup for Renzo
    /// and darkens its corner of the arena. Configured by the bootstrap with
    /// references to its bulb renderer and light.
    /// </summary>
    public class LanternPost : MonoBehaviour
    {
        public static readonly List<LanternPost> Active = new();

        public GameObject bulb;
        public Light glow;
        public float hp = 18f;

        public bool Broken { get; private set; }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        public void Damage(float amount, Health playerHealth)
        {
            if (Broken) return;
            hp -= amount;
            FxPools.Sparks(transform.position + Vector3.up * 1.8f, new Color(1f, 0.7f, 0.4f), 5);
            if (hp > 0f) return;
            Broken = true;
            Sfx3D.Death();
            FxPools.Embers(transform.position + Vector3.up * 1.8f, 22);
            if (bulb != null) bulb.SetActive(false);
            if (glow != null) glow.enabled = false;
            if (playerHealth != null)
            {
                playerHealth.Heal(12f);
                FloatingText.Spawn(transform.position + Vector3.up * 2.2f, "+12",
                    new Color(0.5f, 0.9f, 0.55f), 1.05f);
            }
        }
    }
}
