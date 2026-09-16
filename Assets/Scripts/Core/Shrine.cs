using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// A wayshrine. Praying heals, mends the Gates, and remembers where Renzo is:
    /// the explore loop's save point, and the quiet beat the campaign's shrines
    /// always promised. Added by the dressing to the shrine root at build time.
    /// </summary>
    public class Shrine : MonoBehaviour
    {
        public string shrineId = "wayshrine";

        private void Awake()
        {
            var it = gameObject.GetComponent<Interactable>() ?? gameObject.AddComponent<Interactable>();
            it.verb = "PRAY";
            it.radius = 3.2f;
            it.OnUse = _ => Pray();
        }

        private void Pray()
        {
            var motor = SceneRefs.Motor;
            if (motor == null) return;
            var health = motor.GetComponent<Health>();
            health?.ResetFull();
            var gates = motor.GetComponent<SenGates>();
            if (gates != null) for (var i = 0; i < SenGates.TotalGates; i++) gates.MendGate();
            Sfx3D.Ui();
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2.6f, "THE SHRINE REMEMBERS",
                new Color(1f, 0.78f, 0.45f), 1.3f);
            UI.FxPools.Embers(transform.position + Vector3.up * 1.2f, 14);
            WorldState.Save(motor.transform.position);
            SceneRefs.Game?.Announce("REST — THE GATES MEND");
        }
    }
}
