using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Cómo se ve el mundo en un momento: luz principal, ambiente, niebla, cielo y farolas.</summary>
    public struct WorldLook
    {
        /// <summary>0 de noche, 1 de día (con el amanecer y el atardecer en medio).</summary>
        public float Daylight;
        public float SunIntensity;
        public Color SunColor;
        /// <summary>Giro de la luz direccional: el sol de día y la luna de noche.</summary>
        public Quaternion SunRotation;
        public Color Ambient, Fog;
        public float FogStart, FogEnd;
        public Color SkyTop, SkyHorizon, SkyBottom;
        /// <summary>Cuánto están encendidas las farolas (<see cref="NightLight"/>): 0 de día, 1 de noche.</summary>
        public float LampFactor;
        /// <summary>Brillo de las gotas y copos (no les da la luz): 1 de día, bajo de noche.</summary>
        public float ParticleTint;
    }

    /// <summary>
    /// Ciclo de día y noche y efecto del tiempo (lógica pura, testeable en EditMode). Un día de juego dura
    /// <see cref="CycleSeconds"/> segundos reales. A mediodía y sin lluvia ni nieve, <see cref="Look"/> da exactamente la
    /// luz con la que se diseñó el mundo (cielo cubierto de invierno).
    /// </summary>
    public static class DayCycleMath
    {
        /// <summary>20 minutos reales por día de juego (50 s por hora).</summary>
        public const float CycleSeconds = 1200f;

        // Día (la luz de siempre).
        public static readonly Color DaySun = new(0.8f, 0.84f, 0.92f);
        public const float DaySunIntensity = 0.7f;
        public static readonly Color DayAmbient = new(0.45f, 0.47f, 0.52f);
        public static readonly Color DayFog = new(0.56f, 0.58f, 0.62f);
        public const float DayFogStart = 25f, DayFogEnd = 260f;
        public static readonly Color DaySkyTop = new(0.44f, 0.47f, 0.52f);
        public const float DaySunPitch = 40f, DaySunYaw = -35f;

        // Noche: cielo cubierto que devuelve el naranja de las farolas de sodio de la ciudad.
        public static readonly Color NightAmbient = new(0.17f, 0.18f, 0.24f);
        public static readonly Color NightFog = new(0.16f, 0.135f, 0.12f);
        public static readonly Color NightSkyTop = new(0.035f, 0.04f, 0.055f);
        public const float NightFogStart = 18f, NightFogEnd = 190f;
        public static readonly Color MoonColor = new(0.55f, 0.62f, 0.8f);
        public const float MoonIntensity = 0.12f;
        static readonly Quaternion MoonRotation = Quaternion.Euler(55f, 160f, 0f);

        // Amanecer y atardecer: la niebla y el sol tiran a cálido.
        static readonly Color TwilightFogTint = new(0.07f, 0.02f, -0.03f);
        static readonly Color TwilightSun = new(0.98f, 0.72f, 0.52f);

        // Tiempo.
        static readonly Color SnowDayFog = new(0.72f, 0.74f, 0.77f);
        static readonly Color SnowNightFog = new(0.2f, 0.19f, 0.19f);

        /// <summary>Hora (0–24) tras <paramref name="seconds"/> segundos reales desde <paramref name="startHour"/>.</summary>
        public static float HourAt(float seconds, float startHour, float cycleSeconds = CycleSeconds)
            => Mathf.Repeat(startHour + seconds / cycleSeconds * 24f, 24f);

        /// <summary>Claridad del día: amanece de 5:30 a 7:30 y anochece de 18:30 a 20:30.</summary>
        public static float Daylight(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 7.5f, hour))
                 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18.5f, 20.5f, hour)));
        }

        /// <summary>Altura del sol (grados): 40° a mediodía, baja de lado a lado del día.</summary>
        public static float SunPitch(float hour)
            => Mathf.Max(4f, DaySunPitch * Mathf.Cos((Mathf.Repeat(hour, 24f) - 12f) / 7.5f * 0.5f * Mathf.PI));

        /// <summary>
        /// Aspecto del mundo a la hora <paramref name="hour"/> con lluvia <paramref name="rain"/> y nieve
        /// <paramref name="snow"/> (0–1). La luz direccional es el sol con más de media claridad y la luna con menos; su
        /// intensidad pasa por 0 justo en el cambio, así que no se ve el salto de sombras.
        /// </summary>
        public static WorldLook Look(float hour, float rain, float snow)
        {
            rain = Mathf.Clamp01(rain);
            snow = Mathf.Clamp01(snow);
            float d = Daylight(hour);
            float twilight = 4f * d * (1f - d);

            var look = new WorldLook
            {
                Daylight = d,
                Ambient = Color.Lerp(NightAmbient, DayAmbient, d),
                Fog = Color.Lerp(NightFog, DayFog, d) + TwilightFogTint * twilight,
                FogStart = Mathf.Lerp(NightFogStart, DayFogStart, d),
                FogEnd = Mathf.Lerp(NightFogEnd, DayFogEnd, d),
                SkyTop = Color.Lerp(NightSkyTop, DaySkyTop, d),
                // Se encienden antes de que sea de noche del todo (fotocélula) y se apagan ya de día.
                LampFactor = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.9f, d)),
                ParticleTint = Mathf.Lerp(0.3f, 1f, d),
            };

            if (d >= 0.5f)
            {
                look.SunIntensity = DaySunIntensity * (d - 0.5f) * 2f;
                look.SunColor = Color.Lerp(DaySun, TwilightSun, twilight);
                look.SunRotation = Quaternion.Euler(SunPitch(hour), DaySunYaw + (Mathf.Repeat(hour, 24f) - 12f) * 15f, 0f);
            }
            else
            {
                look.SunIntensity = MoonIntensity * (1f - d * 2f);
                look.SunColor = MoonColor;
                look.SunRotation = MoonRotation;
            }

            // Lluvia: más oscuro y gris, niebla más cerrada.
            look.Fog = Color.Lerp(look.Fog, look.Fog * 0.78f, rain);
            look.FogStart *= Mathf.Lerp(1f, 0.6f, rain);
            look.FogEnd *= Mathf.Lerp(1f, 0.62f, rain);
            look.SunIntensity *= 1f - 0.5f * rain;
            look.Ambient *= 1f - 0.18f * rain;
            look.SkyTop = Color.Lerp(look.SkyTop, look.SkyTop * 0.72f, rain);

            // Nieve: niebla blanca y cerrada (de noche, gris con el naranja de las farolas), cielo del color de la niebla.
            var snowFog = Color.Lerp(SnowNightFog, SnowDayFog, d);
            look.Fog = Color.Lerp(look.Fog, snowFog, snow);
            look.FogStart *= Mathf.Lerp(1f, 0.4f, snow);
            look.FogEnd *= Mathf.Lerp(1f, 0.5f, snow);
            look.SunIntensity *= 1f - 0.35f * snow;
            look.Ambient *= 1f + 0.05f * snow;
            look.SkyTop = Color.Lerp(look.SkyTop, look.Fog * 0.9f, snow * 0.7f);

            look.Fog.a = look.Ambient.a = look.SkyTop.a = 1f;
            look.SkyHorizon = look.Fog; // el horizonte del cielo es la niebla: lo lejano se funde con él
            look.SkyBottom = look.Fog * 0.85f;
            look.SkyBottom.a = 1f;
            return look;
        }

        /// <summary>"HH:MM" de la hora de juego.</summary>
        public static string Clock(float hour)
        {
            int minutes = Mathf.FloorToInt(Mathf.Repeat(hour, 24f) * 60f);
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }
    }
}
