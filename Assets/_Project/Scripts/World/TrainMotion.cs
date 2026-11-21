using UnityEngine;

namespace GTAChiraq
{
    public enum TrainPhase { Running, Dwelling, Waiting }

    /// <summary>Estado de un tren que recorre una línea recta: distancia recorrida, velocidad y fase.</summary>
    public struct TrainState
    {
        public float Distance;
        public float Speed;
        public float Timer;
        public TrainPhase Phase;
        /// <summary>Distancia de la última estación en la que ha parado (o que ya ha dejado atrás); −1 si ninguna.</summary>
        public float LastStop;
        public bool StoppedAtStation => LastStop >= 0f;
    }

    /// <summary>Parámetros del recorrido de un tren (metros, m/s, m/s², segundos).</summary>
    public struct TrainRoute
    {
        public float Length;
        public float StationDistance;
        /// <summary>Más estaciones en el recorrido (distancias desde el inicio); puede ser null.</summary>
        public float[] ExtraStations;
        public float MaxSpeed;
        public float Acceleration;
        public float DwellSeconds;
        public float LayoverSeconds;
    }

    /// <summary>
    /// Movimiento del tren elevado, sin dependencias de escena para poder testearlo en EditMode.
    /// El tren arranca en la distancia 0 ya a velocidad de crucero (el principio queda escondido en la niebla),
    /// frena hasta pararse en cada estación, espera, acelera hasta el final, espera fuera de la vista y vuelve a empezar.
    /// </summary>
    public static class TrainMotion
    {
        const float MinBrakingSpeed = 0.6f;

        public static TrainState Start(float distance, TrainRoute route)
        {
            // Las estaciones que quedan detrás del punto de salida cuentan como ya hechas.
            float last = -1f;
            foreach (float stop in Stops(route))
                if (stop < distance) last = Mathf.Max(last, stop);
            return new TrainState { Distance = distance, Speed = route.MaxSpeed, Phase = TrainPhase.Running, LastStop = last };
        }

        static System.Collections.Generic.IEnumerable<float> Stops(TrainRoute route)
        {
            yield return route.StationDistance;
            if (route.ExtraStations == null) yield break;
            foreach (float stop in route.ExtraStations) yield return stop;
        }

        /// <summary>La siguiente estación por delante (la más cercana que no ha hecho), o −1 si no quedan.</summary>
        static float NextStop(TrainState s, TrainRoute route)
        {
            float next = -1f;
            foreach (float stop in Stops(route))
                if (stop > s.LastStop && stop >= s.Distance && (next < 0f || stop < next)) next = stop;
            return next;
        }

        public static TrainState Step(TrainState s, float dt, TrainRoute route)
        {
            switch (s.Phase)
            {
                case TrainPhase.Dwelling:
                    s.Timer -= dt;
                    if (s.Timer <= 0f) s.Phase = TrainPhase.Running;
                    return s;

                case TrainPhase.Waiting:
                    s.Timer -= dt;
                    if (s.Timer <= 0f) s = Start(0f, route);
                    return s;
            }

            float next = NextStop(s, route);
            bool stopAhead = next >= 0f;
            float target = route.MaxSpeed;
            if (stopAhead)
            {
                float remaining = next - s.Distance;
                target = Mathf.Min(target, Mathf.Max(MinBrakingSpeed, Mathf.Sqrt(2f * route.Acceleration * remaining)));
            }
            s.Speed = s.Speed < target ? Mathf.Min(target, s.Speed + route.Acceleration * dt) : target;
            float step = s.Speed * dt;

            if (stopAhead && s.Distance + step >= next)
            {
                s.Distance = next;
                s.Speed = 0f;
                s.Phase = TrainPhase.Dwelling;
                s.Timer = route.DwellSeconds;
                s.LastStop = next;
                return s;
            }

            s.Distance += step;
            if (s.Distance >= route.Length)
            {
                s.Distance = route.Length;
                s.Speed = 0f;
                s.Phase = TrainPhase.Waiting;
                s.Timer = route.LayoverSeconds;
            }
            return s;
        }
    }
}
