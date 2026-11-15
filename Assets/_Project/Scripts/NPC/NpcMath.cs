using UnityEngine;

namespace GTAChiraq
{
    public enum NpcRole { Civilian, Gang }

    /// <summary>Qué se hace en un sitio (<see cref="NpcSpot"/>).</summary>
    public enum NpcActivity
    {
        /// <summary>De pie en grupo, hablando (esquinas, portales, la cancha).</summary>
        Hangout,
        /// <summary>Sentado (bancos, sillas).</summary>
        Sit,
        /// <summary>Vendiendo en la esquina: de pie, vigilando la calle.</summary>
        Sell,
        /// <summary>Recado: entrar y salir de una tienda abierta.</summary>
        Errand,
        /// <summary>Esperando (parada de bus).</summary>
        Wait,
        /// <summary>Recordando en el memorial.</summary>
        Mourn,
        /// <summary>Calentándose alrededor del bidón con fuego.</summary>
        Warm,
    }

    /// <summary>
    /// Números de la vida del barrio (lógica pura, testeable en EditMode): cuánta gente hay según la hora y el tiempo,
    /// cuándo hay incursiones, qué tiendas abren y qué sitio elige cada uno. Las reglas salen de NPCS.md: de día hay
    /// de todo y pocas incursiones; de noche casi no hay civiles, solo abre la tienda de licores y los grupos están
    /// más activos; con lluvia o nieve la gente se refugia (los civiles mucho más que los que venden).
    /// </summary>
    public static class NpcMath
    {
        /// <summary>Civiles a la vez alrededor del jugador: ~22 de día, ~2 de madrugada; la lluvia y la nieve los vacían.</summary>
        public static int Civilians(float daylight, float rain, float snow)
            => Mathf.RoundToInt(Mathf.Lerp(2f, 22f, Mathf.Clamp01(daylight)) * (1f - 0.7f * Mathf.Clamp01(rain)) * (1f - 0.6f * Mathf.Clamp01(snow)));

        /// <summary>Pandilleros a la vez alrededor del jugador: ~8 de día y ~15 de noche; con mal tiempo, algo menos.</summary>
        public static int GangMembers(float daylight, float rain, float snow)
            => Mathf.RoundToInt(Mathf.Lerp(15f, 8f, Mathf.Clamp01(daylight)) * (1f - 0.3f * Mathf.Clamp01(rain)) * (1f - 0.25f * Mathf.Clamp01(snow)));

        /// <summary>Probabilidad de que un pandillero lleve pistola: la mitad de día, casi todos de noche.</summary>
        public static float ArmedChance(float daylight) => Mathf.Lerp(0.85f, 0.45f, Mathf.Clamp01(daylight));

        /// <summary>
        /// Probabilidad de incursión rival en cada tirada (una por minuto): ~4 % de día (una cada ~25 min) y ~22 % de
        /// noche (una cada ~4–5 min); con lluvia o nieve, algo menos.
        /// </summary>
        public static float RaidChance(float daylight, float rain, float snow)
            => Mathf.Lerp(0.22f, 0.04f, Mathf.Clamp01(daylight)) * (1f - 0.25f * Mathf.Clamp01(Mathf.Max(rain, snow)));

        /// <summary>Segundos mínimos entre una incursión y la siguiente.</summary>
        public const float RaidCooldown = 150f;

        /// <summary>Tiendas: de 8:00 a 21:00; la de licores, siempre.</summary>
        public static bool ShopOpen(float hour, bool liquor)
        {
            if (liquor) return true;
            hour = Mathf.Repeat(hour, 24f);
            return hour >= 8f && hour < 21f;
        }

        /// <summary>
        /// Puntuación de un sitio para un NPC (más es mejor; 0 o menos, descartado). Cuenta si el sitio es de su
        /// grupo (los habituales pesan mucho), si está a cubierto cuando llueve o nieva, la distancia, lo lleno que
        /// está y si el sitio sirve a esa hora. <paramref name="noise"/> (0.75–1.25) da algo de azar.
        /// </summary>
        public static float SpotScore(NpcRole role, Faction npcFaction, Faction spotFaction, bool habitual, bool covered, bool open,
            float precipitation, float distance, int occupants, int capacity, float noise = 1f)
        {
            if (!open || occupants >= capacity) return 0f;
            float s = 1f;
            if (role == NpcRole.Gang)
            {
                if (spotFaction == npcFaction) s *= habitual ? 6f : 3f;
                else if (FactionCatalog.Hostile(npcFaction, spotFaction)) return 0f; // no se mete en sitios del enemigo
                else if (spotFaction != Faction.None) s *= 0.6f;                      // de un aliado: poco
            }
            precipitation = Mathf.Clamp01(precipitation);
            if (covered) s *= 1f + 4f * precipitation;
            else s *= 1f - 0.6f * precipitation;
            s *= 1f / (1f + distance / 40f);
            s *= 1f - 0.5f * occupants / Mathf.Max(1f, capacity);
            return s * noise;
        }

        /// <summary>
        /// Sospecha de un pandillero enemigo que ve al jugador en su territorio: cuánto sube por segundo según la
        /// distancia (más cerca, antes) y la hora (de noche, más). A 1 se queda mirando; a <see cref="ConfrontAt"/> se
        /// le acerca y le encara; a <see cref="AttackAt"/> saca el arma.
        /// </summary>
        public static float SuspicionRate(float distance, float daylight)
            => Mathf.Clamp01(1f - distance / 32f) * Mathf.Lerp(1.3f, 0.7f, Mathf.Clamp01(daylight)) + 0.12f;

        public const float StareAt = 1f, ConfrontAt = 2.5f, AttackAt = 4f;

        /// <summary>Distancia a la que un pandillero reconoce al jugador: 22 m de día, 30 m de noche (con farolas).</summary>
        public static float SightRange(float daylight) => Mathf.Lerp(30f, 22f, Mathf.Clamp01(daylight));

        /// <summary>Dispersión de los disparos de un NPC (grados): peor de lejos, moviéndose y nervioso.</summary>
        public static float ShotSpread(float distance, bool moving, float skill)
            => (3.5f + distance * 0.12f + (moving ? 3f : 0f)) * Mathf.Lerp(1.4f, 0.7f, Mathf.Clamp01(skill));
    }
}
