using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Horario de un local (<see cref="NpcMath.ShopOpen"/>): de 8:00 a 21:00, y la tienda de licores siempre. Cerrado,
    /// apaga el cartel de OPEN y baja la persiana entera. Lee la hora de <see cref="WeatherSystem"/>; sin él, abierto.
    /// Lo pone el generador en cada local.
    /// </summary>
    public sealed class ShopHours : MonoBehaviour
    {
        [SerializeField] bool liquor;
        [SerializeField] GameObject openSign;
        [SerializeField] GameObject closedShutter;

        float nextCheck;
        bool? shown;

        public bool Liquor => liquor;
        public bool IsOpen => !WeatherSystem.Instance || NpcMath.ShopOpen(WeatherSystem.Instance.Hour, liquor);

        /// <summary>Lo usa el generador.</summary>
        public void Configure(bool liquor, GameObject openSign, GameObject closedShutter)
        {
            this.liquor = liquor;
            this.openSign = openSign;
            this.closedShutter = closedShutter;
        }

        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 2f;
            Refresh();
        }

        /// <summary>Pone el local como toca a la hora actual (también desde el editor, para las capturas).</summary>
        public void Refresh() => Show(IsOpen);

        public void Show(bool open)
        {
            if (shown == open) return;
            shown = open;
            if (openSign) openSign.SetActive(open);
            if (closedShutter) closedShutter.SetActive(!open);
        }
    }
}
