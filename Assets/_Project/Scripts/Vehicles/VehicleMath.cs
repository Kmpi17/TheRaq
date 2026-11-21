using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Números de la conducción del coche del jugador (lógica pura, se testea en EditMode): cuánto giran las ruedas según
    /// la velocidad, cómo se mueve el volante con el teclado, cuánto debería girar el coche (modelo de bicicleta) y el
    /// régimen del motor para el sonido.
    /// </summary>
    public static class VehicleMath
    {
        /// <summary>Ángulo máximo de las ruedas: <paramref name="maxAngle"/> parado, bajando hasta <paramref name="minAngle"/> a <paramref name="atSpeed"/> m/s.</summary>
        public static float SteerLimit(float speed, float maxAngle, float minAngle, float atSpeed)
            => Mathf.Lerp(maxAngle, minAngle, Mathf.Abs(speed) / Mathf.Max(atSpeed, 0.01f));

        /// <summary>
        /// Volante con teclado: va hacia <paramref name="input"/> a <paramref name="rate"/> por segundo y vuelve al centro
        /// (o cambia de lado) más deprisa, a <paramref name="returnRate"/>.
        /// </summary>
        public static float SmoothSteer(float current, float input, float dt, float rate, float returnRate)
        {
            bool back = Mathf.Abs(input) < Mathf.Abs(current) || current * input < 0f;
            return Mathf.MoveTowards(current, input, (back ? returnRate : rate) * dt);
        }

        /// <summary>
        /// Giro que debería llevar el coche (rad/s, positivo a la derecha) con las ruedas a <paramref name="steerDeg"/>:
        /// modelo de bicicleta, v·tan(δ)/batalla, limitado a <paramref name="maxRate"/> y a lo que da de sí el agarre:
        /// la aceleración lateral v·ω no pasa de <paramref name="maxLateral"/> m/s² (14 ≈ 1.4 g, de arcade). Marcha atrás
        /// sale con el signo cambiado.
        /// </summary>
        public static float TargetYawRate(float speed, float steerDeg, float wheelbase, float maxRate, float maxLateral = float.MaxValue)
        {
            float rate = speed * Mathf.Tan(steerDeg * Mathf.Deg2Rad) / Mathf.Max(wheelbase, 0.5f);
            float limit = Mathf.Min(maxRate, maxLateral / Mathf.Max(Mathf.Abs(speed), 0.1f));
            return Mathf.Clamp(rate, -limit, limit);
        }

        /// <summary>
        /// Régimen del motor entre 0 (ralentí) y 1 (corte) para una caja automática de <paramref name="gears"/> marchas
        /// que cambia cada <paramref name="gearSpan"/> m/s: sube en cada marcha y baja al cambiar. Devuelve la marcha (1…).
        /// </summary>
        public static float EngineRpm(float speed, float throttle, int gears, float gearSpan, out int gear)
        {
            float v = Mathf.Abs(speed);
            gear = Mathf.Clamp(Mathf.FloorToInt(v / gearSpan) + 1, 1, gears);
            float inGear = gear == gears ? Mathf.Clamp01((v - gearSpan * (gears - 1)) / (gearSpan * 1.6f)) : Mathf.Repeat(v, gearSpan) / gearSpan;
            float rpm = Mathf.Lerp(0.18f, 0.95f, inGear);
            // Con el acelerador pisado y casi parado, el motor sube un poco (embrague).
            return Mathf.Max(rpm, Mathf.Abs(throttle) * 0.35f * Mathf.Clamp01(1f - v / 4f));
        }
    }
}
