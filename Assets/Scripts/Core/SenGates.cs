using System;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Renzo's resource system: Sen (energy) flowing through cracked Gates.
    /// Surging cracks a Gate, lowering max Sen until the player rests.
    /// Values match the tuned Flutter prototype.
    /// </summary>
    public class SenGates : MonoBehaviour
    {
        public const int TotalGates = 5;
        public const float SurgeCost = 30f;

        [SerializeField] private float senRegenPerSecond = 3.5f;
        [SerializeField] private float senPerHitLanded = 6f;
        [SerializeField] private float senPerPerfectDodge = 15f;
        [SerializeField] private float maxSenFloor = 40f;
        [SerializeField] private float senLostPerCrack = 12f;

        public float Sen { get; private set; } = 60f;
        public float MaxSen { get; private set; } = 100f;
        public int CrackedGates { get; private set; }

        public event Action<int> OnGateCracked;
        public event Action<int> OnGateMended;

        private void Update()
        {
            Sen = Mathf.Min(MaxSen, Sen + senRegenPerSecond * Time.deltaTime);
        }

        public bool CanSurge => Sen >= SurgeCost;

        /// <summary>Spend Sen and crack a Gate. Returns false if unaffordable.</summary>
        public bool Surge()
        {
            if (!CanSurge) return false;
            Sen -= SurgeCost;
            if (CrackedGates < TotalGates)
            {
                CrackedGates++;
                MaxSen = Mathf.Max(maxSenFloor, 100f - CrackedGates * senLostPerCrack);
                Sen = Mathf.Min(Sen, MaxSen);
                OnGateCracked?.Invoke(CrackedGates);
            }
            return true;
        }

        /// <summary>Resting between waves mends one Gate.</summary>
        public void MendGate()
        {
            if (CrackedGates == 0) return;
            CrackedGates--;
            MaxSen = Mathf.Max(maxSenFloor, 100f - CrackedGates * senLostPerCrack);
            OnGateMended?.Invoke(CrackedGates);
        }

        public void OnHitLanded() => Sen = Mathf.Min(MaxSen, Sen + senPerHitLanded);
        public void OnPerfectDodge() => Sen = Mathf.Min(MaxSen, Sen + senPerPerfectDodge);

        public void ResetAll()
        {
            Sen = 60f;
            MaxSen = 100f;
            CrackedGates = 0;
        }
    }
}
