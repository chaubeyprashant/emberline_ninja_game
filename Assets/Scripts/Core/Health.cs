using System;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>Simple health with hurt/death events. Used by the player rig.</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;

        public float Hp { get; private set; }
        public float MaxHp => maxHp;
        public bool Dead => Hp <= 0f;

        public event Action<float, Vector3> OnHurt; // (amount, from)
        public event Action OnDeath;

        private void Awake() => Hp = maxHp;

        public void Damage(float amount, Vector3 from)
        {
            if (Dead) return;
            Hp = Mathf.Max(0, Hp - amount);
            OnHurt?.Invoke(amount, from);
            if (Dead) OnDeath?.Invoke();
        }

        public void ResetFull() => Hp = maxHp;

        public void SetMax(float value, bool refill = true)
        {
            maxHp = value;
            if (refill) Hp = maxHp;
            else Hp = Mathf.Min(Hp, maxHp);
        }

        public void Heal(float amount) => Hp = Mathf.Min(maxHp, Hp + amount);
    }
}
