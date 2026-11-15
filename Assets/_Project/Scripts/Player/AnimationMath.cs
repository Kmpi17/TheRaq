using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Ángulos de una pose del ciclo de paso (grados; X local de cada articulación, + hacia atrás).</summary>
    public struct GaitPose
    {
        public float HipL, HipR, KneeL, KneeR, AnkleL, AnkleR;
        public float ShoulderL, ShoulderR, ElbowL, ElbowR;
        /// <summary>Bajada de la pelvis (m, negativa), giro de la pelvis y de la columna (yaw) e inclinación hacia delante.</summary>
        public float Bob, PelvisYaw, SpineYaw, Lean;
    }

    /// <summary>
    /// Cálculos puros de la animación procedural del personaje (testeables en EditMode): el ciclo de andar y correr
    /// (<see cref="Gait"/>) y el IK de dos huesos para llevar la mano a un punto (<see cref="SolveTwoBone"/>).
    /// Convención: las piezas cuelgan hacia −Y de su articulación; girar en X positivo lleva el miembro hacia atrás (−Z).
    /// </summary>
    public static class AnimationMath
    {
        /// <summary>Metros que avanza un ciclo completo (dos pasos) andando y esprintando.</summary>
        public const float WalkStride = 1.4f, SprintStride = 2.6f;

        /// <summary>Avance de la fase (radianes) con la velocidad dada: un ciclo cada zancada.</summary>
        public static float AdvancePhase(float phase, float speed, float run, float dt)
        {
            float stride = Mathf.Lerp(WalkStride, SprintStride, run);
            phase += speed / stride * dt * 2f * Mathf.PI;
            return Mathf.Repeat(phase, 2f * Mathf.PI);
        }

        /// <summary>
        /// Pose del ciclo en la fase <paramref name="phase"/>. <paramref name="move"/> (0–1) mezcla desde quieto y
        /// <paramref name="run"/> (0–1) va de andar a esprintar: más amplitud, rodillas y codos más doblados, más
        /// inclinación y más bote. Las piernas van en oposición y cada brazo con la pierna contraria.
        /// </summary>
        public static GaitPose Gait(float phase, float move, float run)
        {
            move = Mathf.Clamp01(move);
            run = Mathf.Clamp01(run);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            // Rodillas, codos e inclinación suben deprisa al pasar de andar a trotar (raíz): con una mezcla lineal, el
            // trote quedaba como un andar rápido con las piernas casi rectas.
            float jog = Mathf.Sqrt(run);
            float hipAmp = Mathf.Lerp(26f, 48f, run) * move;
            float kneeAmp = Mathf.Lerp(35f, 105f, jog) * move;
            float armAmp = Mathf.Lerp(22f, 55f, run) * move;

            var p = new GaitPose
            {
                // Izquierda delante cuando sin > 0 (ángulo negativo = hacia delante).
                HipL = -s * hipAmp,
                HipR = s * hipAmp,
                // La rodilla se dobla al pasar la pierna hacia delante (fase de vuelo) y algo siempre al correr.
                KneeL = (4f + 14f * jog) * move + kneeAmp * Mathf.Max(0f, c),
                KneeR = (4f + 14f * jog) * move + kneeAmp * Mathf.Max(0f, -c),
                // Brazos al revés que la pierna de su lado.
                ShoulderL = s * armAmp,
                ShoulderR = -s * armAmp,
                ElbowL = -Mathf.Lerp(12f, 88f, jog) * move - Mathf.Max(0f, -s) * 15f * move,
                ElbowR = -Mathf.Lerp(12f, 88f, jog) * move - Mathf.Max(0f, s) * 15f * move,
                Bob = -Mathf.Abs(c) * Mathf.Lerp(0.025f, 0.07f, run) * move,
                PelvisYaw = s * Mathf.Lerp(5f, 10f, run) * move,
                Lean = Mathf.Lerp(3f, 14f, jog) * move,
            };
            p.SpineYaw = -p.PelvisYaw * 1.4f;
            // El pie, casi plano: compensa parte de cadera y rodilla.
            p.AnkleL = -(p.HipL + p.KneeL) * 0.6f;
            p.AnkleR = -(p.HipR + p.KneeR) * 0.6f;
            return p;
        }

        /// <summary>
        /// Eje (local del cuerpo) en el que se balancean las piernas para avanzar en la dirección <paramref name="localDir"/>
        /// (horizontal, en el espacio del cuerpo: +Z delante, +X derecha). Girar un ángulo negativo en este eje lleva la
        /// pierna hacia donde se va, así que el mismo ciclo sirve hacia delante (eje +X), hacia atrás y de lado.
        /// </summary>
        public static Vector3 SwingAxis(Vector3 localDir)
        {
            localDir.y = 0f;
            return localDir.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, localDir.normalized) : Vector3.right;
        }

        /// <summary>
        /// Ángulo de cadera para la pierna del lado <paramref name="side"/> (−1 izquierda, +1 derecha) yendo en
        /// <paramref name="localDir"/>: de lado, la pierna que cruza por delante de la otra hacia dentro da un paso corto
        /// (no se cruzan las piernas); la de fuera, el paso entero.
        /// </summary>
        public static float SideStepHip(float hipAngle, float side, Vector3 localDir)
        {
            localDir.y = 0f;
            if (localDir.sqrMagnitude < 1e-6f) return hipAngle;
            var d = localDir.normalized;
            // El pie va hacia donde se va con ángulo negativo y al revés con positivo.
            float footSide = -Mathf.Sign(hipAngle) * d.x;
            bool inward = footSide * side < 0f;
            return inward ? hipAngle * Mathf.Lerp(1f, 0.35f, Mathf.Abs(d.x)) : hipAngle;
        }

        /// <summary>
        /// IK de dos huesos (brazo o pierna) en un mismo espacio: desde <paramref name="root"/> hasta
        /// <paramref name="target"/> con huesos de <paramref name="upper"/> y <paramref name="lower"/> metros, doblando la
        /// articulación hacia <paramref name="pole"/>. Devuelve las direcciones (unitarias) del hueso de arriba y del de
        /// abajo. Si el objetivo queda lejos, estira el brazo hacia él.
        /// </summary>
        public static void SolveTwoBone(Vector3 root, Vector3 target, float upper, float lower, Vector3 pole,
            out Vector3 upperDir, out Vector3 lowerDir)
        {
            var toTarget = target - root;
            float d = Mathf.Clamp(toTarget.magnitude, 0.001f, upper + lower - 0.001f);
            var dir = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector3.down;
            float cosA = Mathf.Clamp((upper * upper + d * d - lower * lower) / (2f * upper * d), -1f, 1f);
            float sinA = Mathf.Sqrt(1f - cosA * cosA);
            var bend = pole - dir * Vector3.Dot(pole, dir);
            bend = bend.sqrMagnitude > 1e-8f ? bend.normalized : Vector3.Cross(dir, Vector3.right).normalized;
            upperDir = (dir * cosA + bend * sinA).normalized;
            var elbow = root + upperDir * upper;
            var end = root + dir * d;
            lowerDir = (end - elbow).normalized;
        }
    }
}
