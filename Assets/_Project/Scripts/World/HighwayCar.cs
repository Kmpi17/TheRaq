using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Coche decorativo de la autopista hundida: recorre en línea recta su carril de <see cref="start"/> a
    /// <see cref="end"/> a velocidad constante y, al llegar al final, vuelve a empezar (ver <see cref="HighwayLoop"/>).
    /// Los extremos tienen que quedar fuera de la vista: el sur en la niebla y el norte dentro del túnel.
    /// No tiene colisiones.
    /// </summary>
    public class HighwayCar : MonoBehaviour
    {
        [SerializeField] Vector3 start;
        [SerializeField] Vector3 end;
        [SerializeField] float speed = 22f;
        [Tooltip("Distancia inicial, para que los coches no vayan todos juntos.")]
        [SerializeField] float startDistance;

        float distance;

        public float Distance => distance;

        /// <summary>Lo usa el generador de la escena para configurar el recorrido.</summary>
        public void Configure(Vector3 from, Vector3 to, float metersPerSecond, float initialDistance)
        {
            start = from;
            end = to;
            speed = metersPerSecond;
            startDistance = initialDistance;
            distance = HighwayLoop.Advance(initialDistance, 0f, Vector3.Distance(from, to));
            Place();
        }

        void OnEnable()
        {
            distance = HighwayLoop.Advance(startDistance, 0f, Vector3.Distance(start, end));
            Place();
        }

        void Update()
        {
            distance = HighwayLoop.Advance(distance, speed * Time.deltaTime, Vector3.Distance(start, end));
            Place();
        }

        void Place()
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.SetPositionAndRotation(start + direction.normalized * distance, Quaternion.LookRotation(direction));
        }
    }

    /// <summary>Recorrido en bucle de los coches de la autopista, sin dependencias de escena para testearlo en EditMode.</summary>
    public static class HighwayLoop
    {
        /// <summary>
        /// Avanza <paramref name="delta"/> metros desde <paramref name="distance"/> en un recorrido de
        /// <paramref name="length"/> metros y vuelve al principio al pasarse del final. Siempre devuelve [0, length).
        /// </summary>
        public static float Advance(float distance, float delta, float length)
        {
            if (length <= 0f) return 0f;
            float d = Mathf.Repeat(distance + delta, length);
            return d >= length ? 0f : d;
        }
    }
}
