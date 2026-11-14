using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Tren elevado decorativo: mueve este objeto en línea recta de <see cref="start"/> a <see cref="end"/>,
    /// parando en la estación (ver <see cref="TrainMotion"/>). No tiene colisiones ni se puede usar.
    /// Los extremos del recorrido deben quedar fuera de la vista (más allá de la niebla).
    /// </summary>
    public class ElevatedTrain : MonoBehaviour
    {
        [SerializeField] Vector3 start;
        [SerializeField] Vector3 end;
        [Tooltip("Distancia desde el inicio a la que el centro del tren se para en la estación.")]
        [SerializeField] float stationDistance = 100f;
        [SerializeField] float maxSpeed = 16f;
        [SerializeField] float acceleration = 1.1f;
        [SerializeField] float dwellSeconds = 10f;
        [SerializeField] float layoverSeconds = 20f;
        [Tooltip("Distancia inicial, para que los dos trenes no vayan sincronizados.")]
        [SerializeField] float startDistance;

        TrainState state;

        public TrainState State => state;

        TrainRoute Route => new()
        {
            Length = Vector3.Distance(start, end),
            StationDistance = stationDistance,
            MaxSpeed = maxSpeed,
            Acceleration = acceleration,
            DwellSeconds = dwellSeconds,
            LayoverSeconds = layoverSeconds,
        };

        /// <summary>Lo usa el generador de la escena para configurar el recorrido.</summary>
        public void Configure(Vector3 from, Vector3 to, float station, float initialDistance)
        {
            start = from;
            end = to;
            stationDistance = station;
            startDistance = initialDistance;
            Place(initialDistance);
        }

        void OnEnable()
        {
            state = TrainMotion.Start(startDistance, Route);
            Place(state.Distance);
        }

        void Update()
        {
            state = TrainMotion.Step(state, Time.deltaTime, Route);
            Place(state.Distance);
        }

        void Place(float distance)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.SetPositionAndRotation(start + direction.normalized * distance, Quaternion.LookRotation(direction));
        }
    }
}
