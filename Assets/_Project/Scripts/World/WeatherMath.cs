using UnityEngine;

namespace GTAChiraq
{
    public enum Weather { Nublado, Lluvia, Nieve }

    /// <summary>
    /// Tiempo aleatorio (lógica pura, testeable en EditMode): una cadena de Markov de episodios. Tras cada episodio se
    /// elige el siguiente con <see cref="Next"/> y cuánto dura con <see cref="Duration"/>. Con estas cifras, a la larga
    /// ~70 % del tiempo está nublado y seco, ~20 % nieva y ~10 % llueve (un invierno normal de Chicago), y nunca
    /// llueve o nieva dos episodios seguidos.
    /// </summary>
    public static class WeatherMath
    {
        /// <summary>Segundos que tarda la lluvia o la nieve en llegar a su fuerza o en irse.</summary>
        public const float FadeSeconds = 40f;

        /// <summary>Episodio siguiente; <paramref name="roll"/> es un número aleatorio en [0, 1).</summary>
        public static Weather Next(Weather current, float roll) => current switch
        {
            Weather.Nublado => roll < 0.3f ? Weather.Lluvia : roll < 0.7f ? Weather.Nieve : Weather.Nublado,
            Weather.Lluvia => roll < 0.2f ? Weather.Nieve : Weather.Nublado, // a veces la lluvia pasa a nieve
            _ => roll < 0.1f ? Weather.Lluvia : Weather.Nublado,
        };

        /// <summary>Duración del episodio (segundos reales): nublado 5–12 min, lluvia 2–6 min, nieve 3–7 min.</summary>
        public static float Duration(Weather weather, float roll) => weather switch
        {
            Weather.Lluvia => Mathf.Lerp(120f, 360f, roll),
            Weather.Nieve => Mathf.Lerp(180f, 420f, roll),
            _ => Mathf.Lerp(300f, 720f, roll),
        };

        /// <summary>Fuerza del episodio (0 si está seco): desde chispear (0.45) hasta temporal (1).</summary>
        public static float Strength(Weather weather, float roll)
            => weather == Weather.Nublado ? 0f : Mathf.Lerp(0.45f, 1f, roll);

        /// <summary>Acerca la lluvia o nieve actual a su objetivo, de 0 a 1 en <see cref="FadeSeconds"/>.</summary>
        public static float Approach(float current, float target, float dt)
            => Mathf.MoveTowards(current, target, dt / FadeSeconds);

        public static string Name(Weather weather) => weather switch
        {
            Weather.Lluvia => "Lluvia",
            Weather.Nieve => "Nieve",
            _ => "Nublado",
        };
    }
}
