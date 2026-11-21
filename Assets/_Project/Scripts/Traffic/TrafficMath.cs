using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Qué tiene que hacer un coche del tráfico al llegar al final de su tramo.</summary>
    public enum TrafficGate { Free, Signal, Stop }

    /// <summary>
    /// Lógica pura del tráfico de la ciudad (sin escena, para testearla en EditMode): velocidades de frenada, decisión
    /// ante el ámbar, curva de giro en los cruces, elección del siguiente tramo, carriles desplazados y densidad por hora.
    /// </summary>
    public static class TrafficMath
    {
        /// <summary>
        /// Velocidad máxima para pararse a <paramref name="distance"/> metros con una deceleración de
        /// <paramref name="braking"/> m/s² (v = √(2·a·d)). Por debajo de cero, 0.
        /// </summary>
        public static float StoppingSpeed(float distance, float braking) => distance <= 0f ? 0f : Mathf.Sqrt(2f * braking * distance);

        /// <summary>
        /// Velocidad con la que seguir a un obstáculo a <paramref name="gap"/> metros dejando <paramref name="minGap"/> de
        /// hueco: la que permite pararse antes de llegar a ese hueco, sin pasar de <paramref name="cruise"/>.
        /// </summary>
        public static float FollowSpeed(float gap, float minGap, float braking, float cruise) =>
            Mathf.Min(cruise, StoppingSpeed(gap - minGap, braking));

        /// <summary>
        /// ¿Se para ante un ámbar? Solo si le da tiempo a frenar con suavidad antes de la línea; si no, pasa.
        /// </summary>
        public static bool StopsForAmber(float distanceToLine, float speed, float braking) =>
            distanceToLine > speed * speed / (2f * braking) + 0.5f;

        /// <summary>Acerca <paramref name="speed"/> a <paramref name="target"/>: acelera o frena con su límite.</summary>
        public static float Approach(float speed, float target, float acceleration, float braking, float dt) =>
            target > speed ? Mathf.Min(target, speed + acceleration * dt) : Mathf.Max(target, speed - braking * dt);

        /// <summary>Punto de una curva de Bézier cuadrática.</summary>
        public static Vector3 Bezier(Vector3 p0, Vector3 control, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * control + t * t * p2;
        }

        /// <summary>
        /// Punto de control del giro de un cruce: donde se cortan la recta que llega (<paramref name="from"/> hacia
        /// <paramref name="inDir"/>) y la que sale (hacia <paramref name="to"/> por <paramref name="outDir"/>), en el plano XZ.
        /// Si van paralelas (seguir recto), el punto medio.
        /// </summary>
        public static Vector3 TurnControl(Vector3 from, Vector3 inDir, Vector3 to, Vector3 outDir)
        {
            float cross = inDir.x * outDir.z - inDir.z * outDir.x;
            if (Mathf.Abs(cross) < 0.05f) return (from + to) / 2f;
            var d = to - from;
            float t = (d.x * outDir.z - d.z * outDir.x) / cross;
            var p = from + inDir * t;
            p.y = (from.y + to.y) / 2f;
            return p;
        }

        /// <summary>
        /// Línea desplazada <paramref name="offset"/> metros a la derecha (en el sentido de la marcha, mirando desde arriba).
        /// En los vértices interiores usa la media de las normales de los dos lados, así que vale para las curvas.
        /// </summary>
        public static Vector3[] OffsetRight(IReadOnlyList<Vector3> line, float offset)
        {
            var result = new Vector3[line.Count];
            for (int i = 0; i < line.Count; i++)
            {
                var a = i > 0 ? line[i] - line[i - 1] : Vector3.zero;
                var b = i + 1 < line.Count ? line[i + 1] - line[i] : Vector3.zero;
                var dir = a.normalized + b.normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude < 1e-6f) dir = b.sqrMagnitude > 0f ? b : a;
                dir.Normalize();
                result[i] = line[i] + new Vector3(dir.z, 0f, -dir.x) * offset;
            }
            return result;
        }

        /// <summary>
        /// Elige el siguiente tramo entre <paramref name="options"/> (direcciones de salida): nunca da media vuelta
        /// (dirección opuesta a <paramref name="incoming"/>) ni entra en los que lleven a un callejón sin salida
        /// (<paramref name="usable"/> falso). Seguir recto pesa el doble que girar. Con <paramref name="roll"/> en [0, 1).
        /// Devuelve −1 si no hay ninguno.
        /// </summary>
        public static int ChooseNext(Vector3 incoming, IReadOnlyList<Vector3> options, IReadOnlyList<bool> usable, float roll)
        {
            float total = 0f;
            for (int i = 0; i < options.Count; i++) total += Weight(incoming, options[i], usable[i]);
            if (total <= 0f) return -1;
            float pick = roll * total;
            for (int i = 0; i < options.Count; i++)
            {
                float w = Weight(incoming, options[i], usable[i]);
                if (w <= 0f) continue;
                if (pick < w) return i;
                pick -= w;
            }
            for (int i = options.Count - 1; i >= 0; i--)
                if (Weight(incoming, options[i], usable[i]) > 0f) return i;
            return -1;
        }

        static float Weight(Vector3 incoming, Vector3 option, bool usable)
        {
            if (!usable) return 0f;
            float dot = Vector3.Dot(Flat(incoming), Flat(option));
            if (dot < -0.7f) return 0f;      // media vuelta
            return dot > 0.7f ? 2f : 1f;     // recto o giro
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0f ? v.normalized : v;
        }

        /// <summary>
        /// Cuántos coches circulan a esa hora, como fracción del máximo: madrugada casi vacía, horas punta (8 y 17 h)
        /// llenas y el resto del día entre medias.
        /// </summary>
        public static float Density(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            float night = Mathf.InverseLerp(5f, 7f, hour) * (1f - Mathf.InverseLerp(21f, 24f, hour));
            float baseLevel = Mathf.Lerp(0.18f, 0.7f, night);
            float rush = Mathf.Max(Bump(hour, 8f, 1.5f), Bump(hour, 17.5f, 2f));
            return Mathf.Clamp01(baseLevel + rush * 0.3f);
        }

        static float Bump(float x, float center, float halfWidth) => Mathf.Clamp01(1f - Mathf.Abs(x - center) / halfWidth);
    }
}
