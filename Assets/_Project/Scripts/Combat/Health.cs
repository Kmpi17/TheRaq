using System;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Vida de cualquier cosa que se pueda herir (jugador, dianas y, más adelante, personajes). Los golpes y disparos
    /// buscan este componente con GetComponentInParent en lo que tocan.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float startArmor;

        HealthPool pool;
        public HealthPool Pool => pool ??= new HealthPool(maxHealth, startArmor);
        public bool IsDead => Pool.IsDead;
        public float LastDamageTime { get; private set; } = -100f;

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        public float TakeDamage(DamageInfo info)
        {
            if (Pool.IsDead) return 0f;
            float dealt = Pool.Damage(info.Amount, info.Kind != DamageKind.Fall);
            LastDamageTime = Time.time;
            Damaged?.Invoke(info);
            if (Pool.IsDead) Died?.Invoke();
            return dealt;
        }

        public float Heal(float amount) => Pool.Heal(amount);
        public float AddArmor(float amount) => Pool.AddArmor(amount);

        /// <summary>Vuelve con la vida al máximo (sin chaleco).</summary>
        public void Revive() => pool = new HealthPool(maxHealth, 0f);
    }
}
