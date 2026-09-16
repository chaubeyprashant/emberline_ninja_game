using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// A fire to rest by. Resting heals, mends the Gates, and turns the clock to
    /// the next dawn or dusk — the way to choose night for a stealth approach or
    /// day for the road. Saves, like a shrine, because a rest is a place the
    /// world should remember you at.
    /// </summary>
    public class Campfire : MonoBehaviour
    {
        private void Awake()
        {
            var it = gameObject.GetComponent<Interactable>() ?? gameObject.AddComponent<Interactable>();
            it.verb = "REST";
            it.radius = 3.0f;
            it.OnUse = _ => Rest();
        }

        private void Rest()
        {
            var motor = SceneRefs.Motor;
            if (motor == null) return;
            motor.GetComponent<Health>()?.ResetFull();
            var gates = motor.GetComponent<SenGates>();
            if (gates != null) for (var i = 0; i < SenGates.TotalGates; i++) gates.MendGate();
            var clock = TimeOfDay.Instance;
            var label = "REST";
            if (clock != null)
            {
                // Before dusk, sleep to dusk; after it, sleep to dawn.
                var toDusk = clock.Hour >= 6f && clock.Hour < 18f;
                clock.SetHour(toDusk ? 18.5f : 6f);
                label = toDusk ? "RESTED UNTIL DUSK" : "RESTED UNTIL DAWN";
            }
            Sfx3D.Ui();
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2.2f, label,
                new Color(1f, 0.7f, 0.4f), 1.3f);
            WorldState.Save(motor.transform.position);
            SceneRefs.Game?.Announce(label);
        }
    }
}
