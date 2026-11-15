using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Ángulos de una pose del ciclo de paso (grados; X local de cada articulación, + hacia atrás).</summary>
    public struct GaitPose
    {
        public float HipL, HipR, KneeL, KneeR, AnkleL, AnkleR;
        /// <summary>Inclinación del pie respecto al suelo (+ punta abajo): el tobillo la consigue compensando cadera y rodilla.</summary>
        public float FootL, FootR;
        public float ShoulderL, ShoulderR, ElbowL, ElbowR;
        /// <summary>Bajada de la pelvis (m, negativa), giro de la pelvis y de la columna (yaw) e inclinación hacia delante.</summary>
        public float Bob, PelvisYaw, SpineYaw, Lean;
        /// <summary>Desplazamiento lateral de la pelvis sobre la pierna de apoyo (m, + derecha) y caída de cadera (grados en Z).</summary>
        public float SwayX, PelvisRoll;
    }

    /// <summary>
    /// Cálculos puros de la animación procedural del personaje (testeables en EditMode): el ciclo de andar, trotar y
    /// esprintar (<see cref="Gait"/>) y el IK de dos huesos para llevar la mano a un punto (<see cref="SolveTwoBone"/>).
    /// Convención: las piezas cuelgan hacia −Y de su articulación; girar en X positivo lleva el miembro hacia atrás (−Z).
    /// </summary>
    public static class AnimationMath
    {
        /// <summary>
        /// Metros de un ciclo completo (dos pasos) andando, trotando y esprintando. Salen de cadencias reales: unos
        /// ~120 pasos/min al andar deprisa, ~170 al trotar (4.4 m/s) y ~195 al esprintar (7.2 m/s) (antes el trote iba a ~270 y parecía un
        /// pataleo).
        /// </summary>
        public const float WalkStride = 1.7f, JogStride = 3.1f, SprintStride = 4.4f;
        /// <summary>Metros de un ciclo andando agachado (pasos cortos).</summary>
        public const float CrouchStride = 0.8f;

        /// <summary>Avance de la fase (radianes) con la velocidad dada: un ciclo cada zancada.</summary>
        public static float AdvancePhase(float phase, float speed, float jog, float sprint, float dt)
        {
            phase += speed / Stride(jog, sprint) * dt * 2f * Mathf.PI;
            return Mathf.Repeat(phase, 2f * Mathf.PI);
        }

        public static float Stride(float jog, float sprint)
            => Mathf.Lerp(Mathf.Lerp(WalkStride, JogStride, Mathf.Clamp01(jog)), SprintStride, Mathf.Clamp01(sprint));

        // ---------- Curvas del ciclo (una pierna; u = 0 es el contacto del talón, 10 claves cada 0.1) ----------
        // Andar (datos normales de la marcha): cadera de 25° de flexión al apoyar a 10° de extensión al despegar,
        // rodilla con el amortiguado de 18° al cargar y 62° en el vuelo, pie que apoya el talón y despega de punta.
        static readonly float[] WalkHip = { 25f, 22f, 12f, 3f, -5f, -10f, -3f, 12f, 24f, 27f };
        static readonly float[] WalkKnee = { 4f, 18f, 12f, 5f, 4f, 12f, 38f, 62f, 45f, 15f };
        static readonly float[] WalkFoot = { -14f, 0f, 0f, 0f, 3f, 14f, 32f, 18f, 0f, -12f };
        // Trotar: apoyo corto (~38 %) con la rodilla doblada (40° en el apoyo medio), vuelo con la rodilla a ~100°.
        static readonly float[] JogHip = { 30f, 18f, 4f, -10f, -16f, -6f, 16f, 38f, 46f, 40f };
        static readonly float[] JogKnee = { 22f, 40f, 30f, 22f, 32f, 72f, 100f, 92f, 58f, 30f };
        static readonly float[] JogFoot = { -6f, 0f, 6f, 26f, 40f, 34f, 14f, 0f, -8f, -10f };
        // Esprintar: rodilla muy alta delante y talón casi al glúteo en el vuelo.
        static readonly float[] SprintHip = { 36f, 22f, 4f, -14f, -18f, 0f, 34f, 60f, 70f, 58f };
        static readonly float[] SprintKnee = { 26f, 42f, 32f, 28f, 55f, 110f, 128f, 110f, 70f, 36f };
        static readonly float[] SprintFoot = { 2f, 5f, 16f, 38f, 48f, 34f, 10f, -5f, -10f, -5f };

        /// <summary>Curva cíclica por claves equiespaciadas (Catmull-Rom): suave, sin parones en las claves.</summary>
        public static float Cyclic(float u, float[] keys)
        {
            int n = keys.Length;
            float x = Mathf.Repeat(u, 1f) * n;
            int i = Mathf.FloorToInt(x);
            float t = x - i;
            float p0 = keys[(i - 1 + n) % n], p1 = keys[i % n], p2 = keys[(i + 1) % n], p3 = keys[(i + 2) % n];
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }

        static float Mix(float u, float[] walk, float[] jog, float[] sprint, float j, float s)
            => Mathf.Lerp(Mathf.Lerp(Cyclic(u, walk), Cyclic(u, jog), j), Cyclic(u, sprint), s);

        /// <summary>
        /// Pose del ciclo en la fase <paramref name="phase"/> (radianes; 0 = la pierna izquierda apoya el talón).
        /// <paramref name="move"/> (0–1) mezcla desde quieto, <paramref name="jog"/> (0–1) de andar a trotar y
        /// <paramref name="sprint"/> (0–1) de trotar a esprintar. Cada pierna sigue curvas de la marcha real (la cadera va
        /// más hacia delante que hacia atrás, la rodilla amortigua al apoyar y se dobla mucho en el vuelo, el pie apoya el
        /// talón y despega de punta); la derecha, medio ciclo después. Encima: brazos contra su pierna con el codo
        /// retrasado (inercia), bote de la pelvis (abajo al apoyar andando, en el apoyo medio corriendo), la pelvis se
        /// desplaza sobre la pierna de apoyo y cae del lado de la que vuela, y hombros contra caderas.
        /// </summary>
        public static GaitPose Gait(float phase, float move, float jog, float sprint)
        {
            move = Mathf.Clamp01(move);
            jog = Mathf.Clamp01(jog);
            sprint = Mathf.Clamp01(sprint);
            float uL = Mathf.Repeat(phase / (2f * Mathf.PI), 1f), uR = Mathf.Repeat(uL + 0.5f, 1f);
            const float Tau = 2f * Mathf.PI;

            var p = new GaitPose
            {
                // Flexión hacia delante es ángulo negativo en la convención de las articulaciones.
                HipL = -Mix(uL, WalkHip, JogHip, SprintHip, jog, sprint) * move,
                HipR = -Mix(uR, WalkHip, JogHip, SprintHip, jog, sprint) * move,
                KneeL = Mix(uL, WalkKnee, JogKnee, SprintKnee, jog, sprint) * move,
                KneeR = Mix(uR, WalkKnee, JogKnee, SprintKnee, jog, sprint) * move,
                FootL = Mix(uL, WalkFoot, JogFoot, SprintFoot, jog, sprint) * move,
                FootR = Mix(uR, WalkFoot, JogFoot, SprintFoot, jog, sprint) * move,
            };

            // Brazos: cada uno atrás del todo cuando su pierna va delante del todo (u ≈ 0.88), con un poco de retraso.
            // Corriendo van algo más hacia delante que hacia atrás y con el codo a ~90°.
            float armAmp = Mathf.Lerp(Mathf.Lerp(17f, 32f, jog), 48f, sprint) * move;
            float armBias = Mathf.Lerp(Mathf.Lerp(-2f, -10f, jog), -8f, sprint) * move;
            const float ArmBack = 0.88f;
            p.ShoulderL = armBias + armAmp * Mathf.Cos(Tau * (uL - ArmBack));
            p.ShoulderR = armBias + armAmp * Mathf.Cos(Tau * (uR - ArmBack));
            // El codo se cierra al ir el brazo hacia delante y se abre al ir atrás (llega tarde: seguimiento).
            float elbowBase = Mathf.Lerp(Mathf.Lerp(14f, 82f, jog), 92f, sprint);
            float elbowSwing = Mathf.Lerp(Mathf.Lerp(12f, 20f, jog), 26f, sprint);
            p.ElbowL = -(elbowBase + elbowSwing * (0.5f + 0.5f * Mathf.Cos(Tau * (uL - ArmBack + 0.5f - 0.06f)))) * move;
            p.ElbowR = -(elbowBase + elbowSwing * (0.5f + 0.5f * Mathf.Cos(Tau * (uR - ArmBack + 0.5f - 0.06f)))) * move;

            // Pelvis: andando, abajo en los dobles apoyos (u = 0 y 0.5); corriendo, abajo en el apoyo medio y arriba en el vuelo.
            float runMix = Mathf.Max(jog, sprint);
            float walkBob = -0.028f * (0.5f + 0.5f * Mathf.Cos(2f * Tau * uL));
            float runBob = -Mathf.Lerp(0.055f, 0.07f, sprint) * (0.5f + 0.5f * Mathf.Cos(2f * Tau * (uL - 0.14f))) - 0.03f;
            p.Bob = Mathf.Lerp(walkBob, runBob, runMix) * move;
            // Sobre la pierna de apoyo (la izquierda apoya en u ≈ 0.25) y caída de la cadera del otro lado.
            float side = Mathf.Sin(Tau * uL);
            p.SwayX = -Mathf.Lerp(0.024f, 0.009f, runMix) * side * move;
            p.PelvisRoll = -Mathf.Lerp(4.5f, 3f, runMix) * side * move;
            // La pelvis gira con la pierna que avanza; los hombros, al revés.
            p.PelvisYaw = Mathf.Lerp(Mathf.Lerp(5f, 8f, jog), 11f, sprint) * Mathf.Cos(Tau * (uL - ArmBack)) * move;
            p.SpineYaw = -p.PelvisYaw * 1.5f;
            // Inclinación: más al trotar y esprintar, y algo más al empujar.
            p.Lean = (Mathf.Lerp(Mathf.Lerp(3f, 9f, jog), 15f, sprint) + 1.5f * runMix * Mathf.Cos(2f * Tau * (uL - 0.3f))) * move;

            // Tobillo para que el pie tenga la inclinación pedida respecto al suelo (yendo hacia delante).
            p.AnkleL = p.FootL - (p.HipL + p.KneeL);
            p.AnkleR = p.FootR - (p.HipR + p.KneeR);
            return p;
        }

        /// <summary>
        /// Postura de reposo con el peso cargado en una pierna (<paramref name="weight"/>: −1 izquierda, +1 derecha): la
        /// pelvis se va hacia esa pierna y sube de ese lado, la otra rodilla se suelta y los hombros se inclinan al revés
        /// (contrapposto). Devuelve el desplazamiento de la pelvis (m), su caída (grados en Z) y la rodilla suelta (grados).
        /// </summary>
        public static void WeightShift(float weight, out float swayX, out float roll, out float looseKnee)
        {
            weight = Mathf.Clamp(weight, -1f, 1f);
            swayX = 0.035f * weight;
            roll = 5f * weight;
            looseKnee = 14f * Mathf.Abs(weight);
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
