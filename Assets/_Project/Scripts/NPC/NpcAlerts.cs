using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Un disparo o una agresión: dónde, quién y a quién.</summary>
    public struct NpcAlert
    {
        public Vector3 Position;
        public GameObject Source;
        public GameObject Target;
        public float Time;
        public float Radius;
    }

    /// <summary>
    /// Lo que oyen los NPC: disparos y golpes recientes. Los avisa el combate del jugador y el de los NPC; cada NPC
    /// mira los últimos para huir (civiles), defender (aliados) o responder (enemigos). Solo guarda unos segundos.
    /// </summary>
    public static class NpcAlerts
    {
        const float KeepSeconds = 3f;
        static readonly List<NpcAlert> recent = new();

        public static IReadOnlyList<NpcAlert> Recent => recent;

        /// <summary>Un disparo se oye a 45 m; un golpe, a 12 m.</summary>
        public static void Gunshot(Vector3 position, GameObject source, GameObject target) => Report(position, source, target, 45f);
        public static void Hit(Vector3 position, GameObject source, GameObject target) => Report(position, source, target, 12f);

        public static void Report(Vector3 position, GameObject source, GameObject target, float radius)
        {
            Prune();
            recent.Add(new NpcAlert { Position = position, Source = source, Target = target, Time = UnityEngine.Time.time, Radius = radius });
        }

        /// <summary>El aviso más reciente que se oye desde <paramref name="listener"/> y no tiene más de <paramref name="maxAge"/> s.</summary>
        public static bool Heard(Vector3 listener, float maxAge, GameObject ignore, out NpcAlert alert)
        {
            float now = UnityEngine.Time.time;
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                var a = recent[i];
                if (now - a.Time > maxAge) break;
                if (a.Source == ignore) continue;
                if ((a.Position - listener).sqrMagnitude <= a.Radius * a.Radius)
                {
                    alert = a;
                    return true;
                }
            }
            alert = default;
            return false;
        }

        public static void Clear() => recent.Clear();

        static void Prune()
        {
            float now = UnityEngine.Time.time;
            int drop = 0;
            while (drop < recent.Count && now - recent[drop].Time > KeepSeconds) drop++;
            if (drop > 0) recent.RemoveRange(0, drop);
        }
    }
}
