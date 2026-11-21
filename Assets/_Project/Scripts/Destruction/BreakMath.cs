using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Lógica pura de lo que se rompe (se testea en EditMode).</summary>
    public static class BreakMath
    {
        /// <summary>Un tramo a lo largo de una valla, en metros desde su centro.</summary>
        public readonly struct Span
        {
            public readonly float From, To;
            public Span(float from, float to) { From = from; To = to; }
            public float Length => To - From;
            public float Mid => (From + To) / 2f;
            public bool Contains(float x, float margin = 0f) => x >= From - margin && x <= To + margin;
            public override string ToString() => $"[{From:F2}, {To:F2}]";
        }

        /// <summary>
        /// Parte una valla de <paramref name="length"/> m (de −L/2 a L/2) donde da el coche (<paramref name="hit"/>): se abre
        /// un hueco de <paramref name="gap"/> m. Lo que queda a cada lado sigue en pie (<paramref name="kept"/>) si mide al
        /// menos <paramref name="minKeep"/>; si no, sale volando con el hueco. El hueco se parte en trozos de como mucho
        /// <paramref name="maxPiece"/> m (<paramref name="debris"/>). Entre los dos cubren la valla entera, sin solaparse.
        /// </summary>
        public static void SplitFence(float length, float hit, float gap, float minKeep, float maxPiece, List<Span> kept, List<Span> debris)
        {
            kept.Clear();
            debris.Clear();
            float half = length / 2f;
            if (length <= 0f) return;
            hit = Mathf.Clamp(hit, -half, half);
            float g0 = Mathf.Max(-half, hit - gap / 2f), g1 = Mathf.Min(half, hit + gap / 2f);

            if (g0 + half >= minKeep) kept.Add(new Span(-half, g0));
            else g0 = -half;
            if (half - g1 >= minKeep) kept.Add(new Span(g1, half));
            else g1 = half;

            float span = g1 - g0;
            if (span <= 1e-4f) return;
            int pieces = Mathf.Max(1, Mathf.CeilToInt(span / Mathf.Max(maxPiece, 0.1f) - 1e-4f));
            for (int i = 0; i < pieces; i++)
                debris.Add(new Span(g0 + span * i / pieces, g0 + span * (i + 1) / pieces));
        }

        /// <summary>
        /// Velocidad con la que sale un trozo golpeado por un coche que iba a <paramref name="carVelocity"/>: algo menos
        /// que el coche, hacia arriba, y abierto hacia el lado en el que estaba (<paramref name="side"/>, de −1 a 1).
        /// </summary>
        public static Vector3 LaunchVelocity(Vector3 carVelocity, Vector3 carRight, float side, float share = 0.75f)
            => carVelocity * share + Vector3.up * (1.5f + carVelocity.magnitude * 0.08f) + carRight * (side * carVelocity.magnitude * 0.15f);
    }
}
