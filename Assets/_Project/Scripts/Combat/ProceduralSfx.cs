using System;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Sonidos provisionales generados en código (ruido con caída), mientras no haya audio de verdad.</summary>
    public static class ProceduralSfx
    {
        const int Rate = 22050;

        static AudioClip Make(string name, float seconds, int seed, Func<float, float, float> sample)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            var rng = new System.Random(seed);
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(sample((float)i / Rate, (float)(rng.NextDouble() * 2.0 - 1.0)), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Gunshot() => Make("Disparo", 0.4f, 1,
            (t, n) => n * Mathf.Exp(-t * 16f) * 0.9f + Mathf.Sin(t * 2f * Mathf.PI * 85f) * Mathf.Exp(-t * 22f) * 0.7f);

        public static AudioClip Punch() => Make("Golpe", 0.14f, 2,
            (t, n) => (n * 0.45f + Mathf.Sin(t * 2f * Mathf.PI * 110f)) * Mathf.Exp(-t * 35f) * 0.8f);

        public static AudioClip Click() => Make("Clic", 0.05f, 3, (t, n) => n * Mathf.Exp(-t * 140f) * 0.6f);

        /// <summary>Alarma de coche: dos tonos que se alternan cada cuarto de segundo. Un segundo, para poner en bucle.</summary>
        public static AudioClip CarAlarm() => Make("Alarma", 1f, 4,
            (t, n) => Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * (t % 0.5f < 0.25f ? 880f : 620f))) * 0.22f);
    }
}
