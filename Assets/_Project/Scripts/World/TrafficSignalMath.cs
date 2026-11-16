using UnityEngine;

namespace GTAChiraq
{
    public enum SignalColor { Red, Amber, Green }

    /// <summary>Semáforo de peatones: muñeco blanco (cruzar), mano naranja parpadeando (acabar de cruzar) o fija (esperar).</summary>
    public enum WalkSignal { DontWalk, Flashing, Walk }

    /// <summary>Tiempos del ciclo de un cruce, en segundos.</summary>
    [System.Serializable]
    public struct SignalTiming
    {
        public float Green;     // verde de cada dirección
        public float Amber;     // ámbar después del verde
        public float AllRed;    // todo en rojo antes de dar paso a la otra dirección
        public float Clearance; // últimos segundos del verde con la mano naranja parpadeando

        public static SignalTiming Default => new() { Green = 18f, Amber = 3f, AllRed = 2f, Clearance = 6f };
    }

    /// <summary>
    /// Ciclo de un cruce con dos direcciones (los coches que van a lo largo de X y los que van a lo largo de Z).
    /// Primero tiene verde X (verde, ámbar, todo rojo) y luego Z, con la misma duración. Los peatones que cruzan en
    /// paralelo a una dirección pueden pasar mientras esa dirección tiene verde, menos los últimos segundos.
    /// Es lógica pura: la usa <see cref="TrafficLight"/> y se testea en EditMode.
    /// </summary>
    public static class TrafficSignalMath
    {
        public static float Cycle(SignalTiming t) => 2f * (t.Green + t.Amber + t.AllRed);

        /// <summary>Segundos desde que empezó el verde de esa dirección, en [0, ciclo).</summary>
        static float Phase(float time, bool alongX, SignalTiming t)
        {
            float cycle = Cycle(t);
            float p = Mathf.Repeat(time, cycle);
            return alongX ? p : Mathf.Repeat(p - cycle / 2f, cycle);
        }

        public static SignalColor Vehicle(float time, bool alongX, SignalTiming t)
        {
            float p = Phase(time, alongX, t);
            if (p < t.Green) return SignalColor.Green;
            if (p < t.Green + t.Amber) return SignalColor.Amber;
            return SignalColor.Red;
        }

        /// <summary>Semáforo de los peatones que cruzan en paralelo a la dirección <paramref name="alongX"/>.</summary>
        public static WalkSignal Pedestrian(float time, bool alongX, SignalTiming t)
        {
            float p = Phase(time, alongX, t);
            if (p < t.Green - t.Clearance) return WalkSignal.Walk;
            if (p < t.Green) return WalkSignal.Flashing;
            return WalkSignal.DontWalk;
        }

        /// <summary>Parpadeo de la mano naranja: encendida medio segundo de cada segundo.</summary>
        public static bool FlashOn(float time) => Mathf.Repeat(time, 1f) < 0.5f;
    }
}
