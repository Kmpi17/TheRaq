using UnityEngine;

namespace GTAChiraq
{
    public enum DamageKind { Melee, Bullet, Fall }

    /// <summary>Un golpe, un disparo o una caída: cuánto daño, de qué tipo, dónde y desde dónde.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageKind Kind;
        public Vector3 Point;
        public Vector3 Direction;
        public GameObject Source;

        public DamageInfo(float amount, DamageKind kind, Vector3 point, Vector3 direction, GameObject source)
        {
            Amount = amount;
            Kind = kind;
            Point = point;
            Direction = direction;
            Source = source;
        }
    }

    /// <summary>
    /// Vida y chaleco (lógica pura). El chaleco absorbe el 70 % de los golpes y disparos hasta gastarse;
    /// las caídas no las para el chaleco.
    /// </summary>
    public sealed class HealthPool
    {
        public const float ArmorAbsorb = 0.7f;

        public float Max { get; }
        public float MaxArmor { get; }
        public float Current { get; private set; }
        public float Armor { get; private set; }
        public bool IsDead => Current <= 0f;

        public HealthPool(float max, float armor = 0f, float maxArmor = 100f)
        {
            Max = Mathf.Max(1f, max);
            MaxArmor = Mathf.Max(0f, maxArmor);
            Current = Max;
            Armor = Mathf.Clamp(armor, 0f, MaxArmor);
        }

        /// <summary>Aplica el daño y devuelve cuánta vida se ha perdido.</summary>
        public float Damage(float amount, bool armorProtects = true)
        {
            if (IsDead || amount <= 0f) return 0f;
            float toHealth = amount;
            if (armorProtects && Armor > 0f)
            {
                float absorbed = Mathf.Min(Armor, amount * ArmorAbsorb);
                Armor -= absorbed;
                toHealth -= absorbed;
            }
            float before = Current;
            Current = Mathf.Max(0f, Current - toHealth);
            return before - Current;
        }

        /// <summary>Cura (sin pasar del máximo) y devuelve lo curado. Muerto no se cura.</summary>
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;
            float before = Current;
            Current = Mathf.Min(Max, Current + amount);
            return Current - before;
        }

        public float AddArmor(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = Armor;
            Armor = Mathf.Min(MaxArmor, Armor + amount);
            return Armor - before;
        }
    }

    /// <summary>
    /// Energía (lógica pura): la gastan esprintar, escalar y pegar. Se recupera sola tras un momento sin gastarla.
    /// Si llega a 0, el personaje queda agotado (no esprinta) hasta recuperar el 30 %.
    /// </summary>
    public sealed class StaminaPool
    {
        public float Max { get; }
        public float RegenRate = 22f;
        public float RegenDelay = 0.9f;
        public float RecoverFraction = 0.3f;
        public float BoostMultiplier = 2.5f;

        public float Current { get; private set; }
        public bool Exhausted { get; private set; }
        public float BoostLeft { get; private set; }
        float sinceUse;

        public StaminaPool(float max = 100f)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            sinceUse = RegenDelay;
        }

        public bool CanSprint => !Exhausted && Current > 0f;

        /// <summary>Gasta <paramref name="amount"/> si no está agotado (aunque no llegue, se queda a 0). Devuelve si pudo.</summary>
        public bool TrySpend(float amount)
        {
            if (Exhausted) return false;
            Drain(amount);
            return true;
        }

        public void Drain(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            sinceUse = 0f;
            if (Current <= 0f) Exhausted = true;
        }

        public void Restore(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
            if (Current >= Max * RecoverFraction) Exhausted = false;
        }

        /// <summary>Recuperación más rápida durante <paramref name="seconds"/> (bebida energética).</summary>
        public void Boost(float seconds) => BoostLeft = Mathf.Max(BoostLeft, seconds);

        public void Tick(float dt)
        {
            sinceUse += dt;
            float multiplier = BoostLeft > 0f ? BoostMultiplier : 1f;
            BoostLeft = Mathf.Max(0f, BoostLeft - dt);
            if (sinceUse >= RegenDelay) Restore(RegenRate * multiplier * dt);
        }
    }

    /// <summary>Cálculos de combate (puros).</summary>
    public static class CombatMath
    {
        /// <summary>
        /// Dispersión de un disparo en grados: la base del arma más el retroceso acumulado (bloom) y lo que se mueve
        /// el tirador. Sin apuntar (desde la cadera) es 2.5 veces mayor.
        /// </summary>
        public static float Spread(float baseSpread, bool aiming, float speed, float bloom)
            => (baseSpread + bloom + speed * 0.35f) * (aiming ? 1f : 2.5f);

        /// <summary>Desvía <paramref name="direction"/> dentro de un cono de <paramref name="angle"/> grados. <paramref name="disc"/> es un punto del círculo unidad.</summary>
        public static Vector3 Scatter(Vector3 direction, float angle, Vector2 disc)
        {
            if (direction.sqrMagnitude < 1e-8f) return direction;
            float t = Mathf.Tan(Mathf.Clamp(angle, 0f, 89f) * Mathf.Deg2Rad);
            disc = Vector2.ClampMagnitude(disc, 1f);
            return (Quaternion.LookRotation(direction) * new Vector3(disc.x * t, disc.y * t, 1f)).normalized;
        }
    }
}
