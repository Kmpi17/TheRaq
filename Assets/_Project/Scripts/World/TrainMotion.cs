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
        public bool StoppedAtStation;
    }

    /// <summary>Parámetros del recorrido de un tren (metros, m/s, m/s², segundos).</summary>
    public struct TrainRoute
    {
        public float Length;
        public float StationDistance;
        public float MaxSpeed;
        public float Acceleration;
        public float DwellSeconds;
        public float LayoverSeconds;
    }

    /// <summary>
    /// Movimiento del tren elevado, sin dependencias de escena para poder testearlo en EditMode.
    /// El tren arranca en la distancia 0 ya a velocidad de crucero (el principio queda escondido en la niebla),
    /// frena hasta pararse en la estación, espera, acelera hasta el final, espera fuera de la vista y vuelve a empezar.
    /// </summary>
    public static class TrainMotion
    {
        const float MinBrakingSpeed = 0.6f;

        public static TrainState Start(float distance, TrainRoute route) => new()
        {
            Distance = distance,
            Speed = route.MaxSpeed,
            Phase = TrainPhase.Running,
            StoppedAtStation = distance > route.StationDistance,
        };

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

            bool stopAhead = !s.StoppedAtStation && s.Distance < route.StationDistance;
            float target = route.MaxSpeed;
            if (stopAhead)
            {
                float remaining = route.StationDistance - s.Distance;
                target = Mathf.Min(target, Mathf.Max(MinBrakingSpeed, Mathf.Sqrt(2f * route.Acceleration * remaining)));
            }
            s.Speed = s.Speed < target ? Mathf.Min(target, s.Speed + route.Acceleration * dt) : target;
            float step = s.Speed * dt;

            if (stopAhead && s.Distance + step >= route.StationDistance)
            {
                s.Distance = route.StationDistance;
                s.Speed = 0f;
                s.Phase = TrainPhase.Dwelling;
                s.Timer = route.DwellSeconds;
                s.StoppedAtStation = true;
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
