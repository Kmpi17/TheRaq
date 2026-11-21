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

        /// <summary>Claxon de coche: dos tonos desafinados (como los de verdad), con ataque y caída cortos. Medio segundo.</summary>
        public static AudioClip Horn() => Make("Claxon", 0.5f, 5,
            (t, n) => (Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 415f)) + Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 495f))) * 0.12f
                      * Mathf.Clamp01(t * 60f) * Mathf.Clamp01((0.5f - t) * 25f));

        /// <summary>
        /// Bocina del tren de mercancías al acercarse a un paso a nivel: largo, largo, corto, largo, con un acorde grave
        /// de tres notas. Unos 5 s.
        /// </summary>
        public static AudioClip TrainHorn() => Make("BocinaTren", 5.2f, 6, (t, n) =>
        {
            float[] starts = { 0f, 1.5f, 3f, 3.6f }, lengths = { 1.2f, 1.2f, 0.4f, 1.5f };
            float env = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                float u = t - starts[i];
                if (u >= 0f && u < lengths[i]) env = Mathf.Clamp01(u * 20f) * Mathf.Clamp01((lengths[i] - u) * 12f);
            }
            float chord = 0f;
            foreach (float f in new[] { 233f, 277f, 349f }) chord += Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * f)) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * f);
            return chord * env * 0.09f;
        });

        // ---------- Coches ----------
        // Los bucles duran 1 s y todas sus frecuencias son números enteros de Hz, para que empalmen sin chasquido.

        /// <summary>
        /// Motor de gasolina en bucle: una base grave de 40 Hz con sus armónicos y pulsos de explosión. El coche sube el tono
        /// con <c>AudioSource.pitch</c> según el régimen.
        /// </summary>
        public static AudioClip EngineLoop() => Make("Motor", 1f, 8, (t, n) =>
        {
            float w = 2f * Mathf.PI * t;
            float firing = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(w * 80f), 6f);      // pulsos (V8 a ralentí)
            float body = Mathf.Sin(w * 40f) * 0.6f + Mathf.Sin(w * 80f) * 0.35f + Mathf.Sin(w * 120f) * 0.2f
                         + Mathf.Sign(Mathf.Sin(w * 160f)) * 0.06f;
            float wobble = 1f + 0.15f * Mathf.Sin(w * 7f);
            return (body * wobble + firing * 0.5f) * 0.32f;
        });

        /// <summary>Chirrido de ruedas en bucle: dos tonos agudos que tiemblan, con algo de ruido.</summary>
        public static AudioClip SkidLoop() => Make("Derrape", 1f, 9, (t, n) =>
        {
            float w = 2f * Mathf.PI * t;
            float wob = Mathf.Sin(w * 6f) * 0.004f;
            return (Mathf.Sin(w * 1180f + Mathf.Sin(w * 13f) * 3f) * 0.5f + Mathf.Sin(w * 1630f + wob * 400f) * 0.3f + n * 0.25f) * 0.35f;
        });

        /// <summary>Choque de coche: golpe sordo, chapa que tiembla y cristales.</summary>
        public static AudioClip Crash() => Make("Choque", 0.9f, 10, (t, n) =>
        {
            float w = 2f * Mathf.PI * t;
            float thump = Mathf.Sin(w * 55f) * Mathf.Exp(-t * 14f);
            float crunch = n * Mathf.Exp(-t * 9f) * 0.8f;
            float metal = (Mathf.Sin(w * 430f) + Mathf.Sin(w * 1170f) * 0.6f + Mathf.Sin(w * 2210f) * 0.4f) * Mathf.Exp(-t * 6f) * 0.3f;
            float glass = t > 0.05f && n > 0.93f ? n * Mathf.Exp(-(t - 0.05f) * 5f) * 0.6f : 0f;
            return (thump + crunch + metal + glass) * 0.7f;
        });

        /// <summary>Claxon sostenido en bucle (mientras se mantiene H).</summary>
        public static AudioClip HornLoop() => Make("ClaxonBucle", 1f, 11,
            (t, n) => (Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 415f)) + Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 495f))) * 0.12f);

        /// <summary>Puerta de coche: un golpe seco y grave.</summary>
        public static AudioClip CarDoor() => Make("Puerta", 0.3f, 12,
            (t, n) => (Mathf.Sin(t * 2f * Mathf.PI * 95f) * 0.8f + n * 0.4f) * Mathf.Exp(-t * 22f) * Mathf.Clamp01(t * 400f));

        // ---------- Cosas que se rompen ----------

        /// <summary>Poste o señal de metal que se dobla y cae: golpe metálico con su vibración.</summary>
        public static AudioClip MetalClang() => Make("GolpeMetal", 0.9f, 13, (t, n) =>
        {
            float w = 2f * Mathf.PI * t;
            return ((Mathf.Sin(w * 520f) + Mathf.Sin(w * 1390f) * 0.7f + Mathf.Sin(w * 2550f) * 0.4f) * Mathf.Exp(-t * 5f) * 0.35f
                    + n * Mathf.Exp(-t * 30f) * 0.6f) * 0.7f;
        });

        /// <summary>Tablones que se parten: crujidos sueltos y un golpe grave.</summary>
        public static AudioClip WoodBreak() => Make("Madera", 0.7f, 14, (t, n) =>
        {
            float crack = Mathf.Abs(n) > 0.8f ? n * Mathf.Exp(-t * 6f) : n * 0.15f * Mathf.Exp(-t * 10f);
            return (crack + Mathf.Sin(t * 2f * Mathf.PI * 140f) * Mathf.Exp(-t * 18f) * 0.7f) * 0.75f;
        });

        /// <summary>Malla metálica arrancada: tintineo de alambre y chapa.</summary>
        public static AudioClip FenceRattle() => Make("Malla", 0.8f, 15, (t, n) =>
        {
            float w = 2f * Mathf.PI * t;
            float rattle = n * (0.5f + 0.5f * Mathf.Sin(w * 31f)) * Mathf.Exp(-t * 5f);
            return (rattle * 0.6f + (Mathf.Sin(w * 3100f) + Mathf.Sin(w * 4370f)) * Mathf.Exp(-t * 8f) * 0.15f
                    + Mathf.Sin(w * 70f) * Mathf.Exp(-t * 20f) * 0.6f) * 0.7f;
        });

        /// <summary>Chorro de agua de una boca de incendio rota, en bucle (siseo).</summary>
        public static AudioClip WaterSpray() => Make("Chorro", 1f, 16,
            (t, n) => n * (0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 9f)) * 0.3f);

        /// <summary>Registrar un cuerpo: roce de ropa.</summary>
        public static AudioClip Rummage() => Make("Registrar", 0.45f, 17,
            (t, n) => n * Mathf.Abs(Mathf.Sin(t * 2f * Mathf.PI * 9f)) * Mathf.Clamp01((0.45f - t) * 8f) * 0.35f);

        /// <summary>Campana del paso a nivel: un golpe metálico agudo con su caída. Medio segundo, para poner en bucle.</summary>
        public static AudioClip CrossingBell() => Make("CampanaPasoNivel", 0.5f, 7,
            (t, n) => (Mathf.Sin(t * 2f * Mathf.PI * 1250f) + 0.6f * Mathf.Sin(t * 2f * Mathf.PI * 2710f) + n * 0.05f) * Mathf.Exp(-t * 9f) * 0.25f);
    }
}
