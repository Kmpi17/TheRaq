using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Luz de calle (farolas, focos) que se apaga de día y se enciende al anochecer. <see cref="WeatherSystem"/> fija
    /// con <see cref="SetLevel"/> cuánto es de noche (0–1); la intensidad va de <see cref="dayLevel"/> a
    /// <see cref="nightLevel"/> veces la que tiene en la escena. Apagada (0) se desactiva, y no cuesta nada.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class NightLight : MonoBehaviour
    {
        static readonly List<NightLight> active = new();
        public static float Level { get; private set; }

        [SerializeField] float dayLevel;
        // URP atenúa con la distancia al cuadrado: con la intensidad de la escena (4 a 6 m del suelo) apenas se veía
        // el charco de luz bajo la farola.
        [SerializeField] float nightLevel = 7f;

        Light lamp;
        float baseIntensity;

        void Init()
        {
            if (lamp) return;
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
        }

        void OnEnable()
        {
            Init();
            active.Add(this);
            Apply(Level);
        }

        void OnDisable() => active.Remove(this);

        /// <summary>Pone esta luz al nivel de noche dado (también en el editor, para las capturas).</summary>
        public void Apply(float night)
        {
            Init();
            float k = Mathf.Lerp(dayLevel, nightLevel, Mathf.Clamp01(night));
            lamp.intensity = baseIntensity * k;
            lamp.enabled = k > 0.01f;
        }

        public static void SetLevel(float level)
        {
            if (level == Level || (Mathf.Abs(level - Level) < 0.002f && level > 0f && level < 1f)) return; // de día y de noche no cambia nada
            Level = level;
            foreach (var l in active) l.Apply(level);
        }
    }
}
