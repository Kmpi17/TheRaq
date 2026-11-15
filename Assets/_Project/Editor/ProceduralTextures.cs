using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Genera en código las texturas del barrio de viviendas (estilo PS2: ladrillo rojo con ventanas,
    /// asfalto gastado, aceras de losas, césped seco). Todas son tileables y deterministas (semillas fijas).
    /// Se guardan como PNG en Assets/_Project/Textures/ y se reimportan en cada build de la escena.
    /// </summary>
    public static class ProceduralTextures
    {
        public const string TexturesDir = "Assets/_Project/Textures";

        // Tamaño real (metros) que cubre una repetición de cada textura. El generador de la escena
        // usa estos valores como tamaño de tile al poner UVs en metros.
        public const float FacadeBay = 3f;      // ancho de una ventana + muro
        public const float FacadeFloor = 3f;    // altura de una planta
        public const float PlainBrickHeight = 0.75f; // alto que cubre una tesela de PlainBrick
        public const float SidewalkTile = 3f;   // 2x2 losas de 1.5 m
        public const float GrassTile = 4f;
        public const float RoofTile = 4f;

        // ---------- Fachada de ladrillo con ventana ----------

        /// <summary>Una planta y un vano: ladrillo a soga, ventana de guillotina con marco blanco, alféizar y chorretones.</summary>
        public static Texture2D BrickFacade(string name, Color brick, bool airConditioner, int seed)
        {
            const int S = 256;
            var px = new Color[S * S];
            var mortar = new Color(0.6f, 0.56f, 0.5f);
            var frame = new Color(0.86f, 0.85f, 0.81f);
            var sill = new Color(0.66f, 0.64f, 0.6f);

            const int winX0 = 78, winX1 = 178, winY0 = 72, winY1 = 200;
            const int sillX0 = 70, sillX1 = 186, sillY0 = 64;
            const int lintelY1 = 210;

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                Color c;

                bool inLintel = y >= winY1 && y < lintelY1 && x >= winX0 - 4 && x < winX1 + 4;
                bool inSill = y >= sillY0 && y < winY0 && x >= sillX0 && x < sillX1;
                bool inWindow = x >= winX0 && x < winX1 && y >= winY0 && y < winY1;

                if (inWindow)
                {
                    c = WindowPixel(x - winX0, y - winY0, winX1 - winX0, winY1 - winY0, frame, airConditioner, seed);
                }
                else if (inSill)
                {
                    c = sill * (y == sillY0 ? 0.7f : 1f - 0.02f * (winY0 - y));
                }
                else if (inLintel)
                {
                    // Hilada a sardinel: ladrillos de pie, algo más oscuros.
                    int col = (x - winX0 + 4) / 8;
                    bool joint = (x - winX0 + 4) % 8 == 0 || y == winY1;
                    c = joint ? mortar * 0.9f : BrickTone(brick * 0.85f, col, 999, seed);
                }
                else
                {
                    c = BrickWallPixel(x, y, S, brick, mortar, seed);
                    // Chorretones de suciedad bajo el alféizar.
                    if (x >= sillX0 + 4 && x < sillX1 - 4 && y < sillY0 && y > 10)
                    {
                        float streak = Fbm(u * 1f, v * 0.08f, 32, 2, seed + 5);
                        float fade = (float)(y - 10) / (sillY0 - 10);
                        c *= 1f - 0.22f * fade * Mathf.SmoothStep(0.35f, 0.65f, streak);
                    }
                }

                // Variación general muy suave.
                c *= 0.95f + 0.1f * Fbm(u, v, 4, 3, seed + 11);
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>
        /// Ladrillo liso sin ventanas, con las mismas hiladas y el mismo ancho de vano que <see cref="BrickFacade"/>
        /// (256 px = 3 m de ancho; 64 px = <see cref="PlainBrickHeight"/> de alto). Para antepechos y hastiales, donde
        /// la textura con ventanas saldría cortada.
        /// </summary>
        public static Texture2D PlainBrick(string name, Color brick, int seed)
        {
            const int W = 256, H = 64;
            var px = new Color[W * H];
            var mortar = new Color(0.6f, 0.56f, 0.5f);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var c = BrickWallPixel(x, y, W, brick, mortar, seed);
                c *= 0.95f + 0.1f * Fbm((float)x / W, (float)y / W, 4, 3, seed + 11);
                c.a = 1f;
                px[y * W + x] = c;
            }
            return Save(name, W, H, px);
        }

        static Color BrickWallPixel(int x, int y, int size, Color brick, Color mortar, int seed)
        {
            const int course = 8;           // 32 hiladas por planta (~9 cm)
            const float brickLen = 25.6f;   // 10 ladrillos por vano, tilea exacto
            int row = y / course;
            float offset = (row % 2) * brickLen * 0.5f;
            float bx = (x + offset) / brickLen;
            int col = Mathf.FloorToInt(bx) % 10;
            float inBrickX = (bx - Mathf.Floor(bx)) * brickLen;

            if (y % course < 1.5f || inBrickX < 1.5f)
                return mortar * (0.92f + 0.12f * Hash01(x, y, seed + 3));

            var c = BrickTone(brick, col, row, seed);
            c *= 0.94f + 0.12f * Hash01(x, y, seed + 4);           // grano
            if (y % course == course - 1) c *= 0.88f;                // sombra bajo la junta superior
            return c;
        }

        static Color BrickTone(Color brick, int col, int row, int seed)
        {
            float h = Hash01(col, row, seed);
            float tone = 0.82f + 0.32f * h;
            var c = brick * tone;
            if (Hash01(col, row, seed + 1) > 0.93f) c *= 0.7f;       // ladrillo requemado
            c.r *= 0.97f + 0.08f * Hash01(col, row, seed + 2);
            return c;
        }

        static Color WindowPixel(int x, int y, int w, int h, Color frame, bool airConditioner, int seed)
        {
            const int border = 5;
            int midY = h / 2;
            bool isFrame = x < border || x >= w - border || y < border || y >= h - border || Mathf.Abs(y - midY) < 3;
            if (isFrame)
            {
                float shade = (y < border) ? 0.85f : 1f;
                return frame * shade;
            }

            // Cristal: reflejo de cielo arriba, oscuro abajo.
            float t = (float)(y % midY) / midY;
            var glass = Color.Lerp(new Color(0.1f, 0.11f, 0.13f), new Color(0.42f, 0.47f, 0.53f), t * t);
            // Diagonal de reflejo.
            if (Mathf.Abs((x - y * 0.6f) % 60) < 7) glass *= 1.25f;

            bool upper = y > midY;
            // Persiana a medio bajar en la hoja de arriba.
            if (upper && y > h - border - 26)
            {
                var blind = new Color(0.68f, 0.64f, 0.55f);
                glass = (y % 4 == 0) ? blind * 0.8f : blind;
            }

            // Sombra interior junto al marco.
            if (x < border + 3 || y > h - border - 3 || (upper && y < midY + 6)) glass *= 0.7f;

            // Aparato de aire acondicionado en la hoja de abajo.
            if (airConditioner && !upper)
            {
                int ax0 = 26, ax1 = 74, ay0 = 6, ay1 = 44;
                if (x >= ax0 && x < ax1 && y >= ay0 && y < ay1)
                {
                    var ac = new Color(0.74f, 0.73f, 0.68f);
                    if (y < ay0 + 3) return ac * 0.6f;
                    if (x >= ax0 + 6 && x < ax1 - 6 && y >= ay0 + 8 && y < ay1 - 8 && y % 3 == 0) return ac * 0.55f;
                    return ac * (0.9f + 0.1f * Hash01(x, y, seed + 9));
                }
            }
            return glass;
        }

        // ---------- Calles ----------

        /// <summary>
        /// Calzada de barrio: u cruza la calle (una repetición = ancho completo), v va a lo largo.
        /// Lleva bordes blancos, doble línea amarilla central y suciedad junto al bordillo.
        /// </summary>
        public static Texture2D Road(string name, float roadWidth, int seed)
        {
            const int S = 512;
            var px = new Color[S * S];
            var white = new Color(0.82f, 0.82f, 0.78f);
            var yellow = new Color(0.82f, 0.64f, 0.16f);
            float edge = 0.6f / roadWidth, lineW = 0.12f / roadWidth, gap = 0.1f / roadWidth;

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = AsphaltPixel(u, v, x, y, 2, seed);

                // Más sucio junto al bordillo y rodadas más lisas.
                float fromCurb = Mathf.Min(u, 1f - u) * roadWidth;
                if (fromCurb < 0.5f) c *= Mathf.Lerp(0.7f, 1f, fromCurb / 0.5f);
                float lane = Mathf.Abs(Mathf.Abs(u - 0.5f) - 0.25f) * roadWidth;
                if (lane < 0.5f) c *= 0.95f;

                // Pintura gastada: se pierde donde el ruido es bajo.
                float wear = Fbm(u, v, 16, 3, seed + 20);
                float paint = Mathf.SmoothStep(0.3f, 0.5f, wear);

                if (Mathf.Abs(u - edge) < lineW / 2f || Mathf.Abs(u - (1f - edge)) < lineW / 2f)
                    c = Color.Lerp(c, white, 0.85f * paint);
                float d = Mathf.Abs(u - 0.5f);
                if (Mathf.Abs(d - (gap / 2f + lineW / 2f)) < lineW / 2f)
                    c = Color.Lerp(c, yellow, 0.9f * paint);

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>Qué pintura lleva la calzada de las calles grandes.</summary>
        public enum MainRoadPaint
        {
            Lanes,    // tramo normal: bordes, doble amarilla y separadores de carril discontinuos
            Approach, // llegada a un cruce (v = 1 hacia el cruce): carriles continuos, línea de stop y paso de cebra
            None,     // el cruce: solo asfalto
        }

        /// <summary>
        /// Calzada de las calles grandes (z = ±80, x = ±110): 4 carriles, un cuadrado de ancho × ancho (512 px).
        /// u cruza la calle y v va a lo largo; con circulación por la derecha, quien va hacia v+ lleva su carril en u > 0.5.
        /// Asfalto más gastado que el de barrio: rodadas pulidas, sal de invierno, grietas selladas con alquitrán,
        /// parches rectangulares de obras (sin pintura encima), algún bache y la pintura borrada donde pisan las ruedas.
        /// </summary>
        public static Texture2D MainRoad(string name, float roadWidth, int seed, MainRoadPaint paint)
        {
            const int S = 512;
            var px = new Color[S * S];
            float W = roadWidth;
            var white = new Color(0.8f, 0.8f, 0.76f);
            var yellow = new Color(0.8f, 0.62f, 0.15f);
            var tar = new Color(0.12f, 0.12f, 0.13f);
            var salt = new Color(0.6f, 0.6f, 0.58f);
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Parches de obras: rectángulos (metros) que no tocan el borde de la tesela a lo largo, para que case al repetir.
            // Pocos y suaves: la tesela se repite cada 14 m (el generador alterna varias semillas del tramo normal).
            var patches = new List<Rect>();
            {
                float w = R(1.2f, 2.8f), h = R(1.5f, 3.5f);
                patches.Add(new Rect(R(0.4f, W - w - 0.4f), R(0.4f, W - h - 0.4f), w, h));
            }
            // A veces, una zanja larga y estrecha (de una acometida) cruzando media calzada.
            bool trench = paint == MainRoadPaint.Lanes && rng.NextDouble() < 0.5;
            float trenchB = R(2f, W - 3f), trenchW = R(0.6f, 0.9f);
            float trenchA0 = rng.NextDouble() < 0.5 ? 0.2f : W / 2f, trenchA1 = trenchA0 + W / 2f - 0.2f;
            // Baches.
            var potholes = new List<Vector3>();
            for (int i = 0; i < 2; i++) potholes.Add(new Vector3(R(0.8f, W - 0.8f), R(0.8f, W - 0.8f), R(0.15f, 0.3f)));
            // Una grieta transversal sellada (a lo largo, en metros), a trozos y torcida.
            float crackB0 = R(2f, W - 2f);

            // Medidas de la pintura (metros).
            const float edge = 0.6f, lineW = 0.12f, gap = 0.1f;
            float laneA = W / 4f;                                    // separadores de carril en W/4 y 3W/4
            const float dash = 3f, dashPeriod = 7f;                  // dos trazos por tesela de 14 m
            float crosswalk0 = W - 3.2f, crosswalk1 = W - 0.4f;      // paso de cebra al final del tramo
            float stop0 = crosswalk0 - 0.9f, stop1 = stop0 + 0.4f;   // línea de stop justo antes
            float solidFrom = W * 0.3f;                              // los separadores son continuos al acercarse
            // Rodadas: dos por carril, a ±0.85 m del centro de cada uno.
            var wheels = new List<float>();
            foreach (float laneCenter in new[] { (edge + laneA) / 2f, (laneA + W / 2f) / 2f, W - (laneA + W / 2f) / 2f, W - (edge + laneA) / 2f })
            {
                wheels.Add(laneCenter - 0.85f);
                wheels.Add(laneCenter + 0.85f);
            }

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float a = u * W, b = v * W;
                var c = AsphaltPixel(u, v, x, y, 3, seed);

                // Rodadas: dos por carril, más oscuras y lisas, que se pierden a trozos.
                float track = 0f;
                if (paint != MainRoadPaint.None)
                    foreach (float wheel in wheels)
                        track = Mathf.Max(track, 1f - Mathf.Clamp01(Mathf.Abs(a - wheel) / 0.4f));
                else
                    track = 0.4f * Fbm(u, v, 3, 2, seed + 30);
                track *= Mathf.Lerp(0.5f, 1f, Fbm(u, v, 6, 2, seed + 31));
                c *= 1f - 0.12f * track;

                // Cuneta: suciedad junto al bordillo.
                float fromCurb = Mathf.Min(a, W - a);
                if (paint != MainRoadPaint.None && fromCurb < 0.6f) c *= Mathf.Lerp(0.66f, 1f, fromCurb / 0.6f);

                // Sal de invierno: velo blanquecino fuera de las rodadas.
                float saltNoise = Fbm(u, v, 5, 3, seed + 40);
                float saltMask = Mathf.SmoothStep(0.5f, 0.72f, saltNoise) * (1f - track);
                if (paint != MainRoadPaint.None && fromCurb < 1.6f) saltMask = Mathf.Max(saltMask, 0.5f * (1f - fromCurb / 1.6f) * saltNoise);
                c = Color.Lerp(c, salt, 0.28f * saltMask);

                // Grietas selladas con alquitrán: longitudinales (junta de carril y centro) y transversales.
                float wobbleA = 0.25f * (Fbm(0.37f, v, 4, 2, seed + 50) - 0.5f);
                float wobbleB = 1.2f * (Fbm(u, 0.61f, 6, 3, seed + 51) - 0.5f);
                bool tarCrack = Mathf.Abs(a - (laneA + 0.5f) - wobbleA) < 0.05f && Fbm(u, v, 2, 2, seed + 52) > 0.45f;
                tarCrack |= Mathf.Abs(a - (W - laneA - 0.5f) + wobbleA) < 0.05f && Fbm(u, v, 2, 2, seed + 53) > 0.5f;
                tarCrack |= Mathf.Abs(b - crackB0 - wobbleB) < 0.05f && Fbm(u, 0.2f, 3, 2, seed + 54) > 0.5f;
                if (tarCrack) c = Color.Lerp(c, tar, 0.75f);

                // Parches y zanja: asfalto más nuevo y oscuro, con el borde sellado. Tapan la pintura.
                bool patched = false;
                foreach (var p in patches)
                {
                    if (!p.Contains(new Vector2(a, b))) continue;
                    patched = true;
                    float border = Mathf.Min(Mathf.Min(a - p.xMin, p.xMax - a), Mathf.Min(b - p.yMin, p.yMax - b));
                    float g = 0.31f + 0.04f * (Hash01(x, y, seed + 60) - 0.5f) + 0.04f * (Fbm(u, v, 12, 2, seed + 61) - 0.5f);
                    c = border < 0.05f ? Color.Lerp(c, tar, 0.7f) : Color.Lerp(c, new Color(g, g, g * 1.02f), 0.8f);
                }
                if (trench && b > trenchB && b < trenchB + trenchW && a > trenchA0 && a < trenchA1)
                {
                    patched = true;
                    float border = Mathf.Min(Mathf.Min(b - trenchB, trenchB + trenchW - b), Mathf.Min(a - trenchA0, trenchA1 - a));
                    float g = 0.33f + 0.04f * (Hash01(x, y, seed + 62) - 0.5f);
                    c = border < 0.05f ? Color.Lerp(c, tar, 0.7f) : Color.Lerp(c, new Color(g, g * 0.99f, g * 0.97f), 0.8f);
                }

                // Baches: hueco oscuro con el borde roto y gravilla.
                foreach (var h in potholes)
                {
                    float r = h.z * (0.8f + 0.4f * Fbm(u, v, 24, 2, seed + 70));
                    float d = new Vector2(a - h.x, b - h.y).magnitude;
                    if (d < r * 0.75f) { c *= 0.45f + 0.15f * Hash01(x, y, seed + 71); patched = true; }
                    else if (d < r) { c *= 0.7f; patched = true; }
                }

                if (!patched && paint != MainRoadPaint.None)
                {
                    // Pintura gastada: se pierde con el ruido y, sobre todo, donde pisan las ruedas.
                    float wear = Fbm(u, v, 16, 3, seed + 20);
                    float amount = Mathf.SmoothStep(0.28f, 0.5f, wear) * Mathf.Lerp(1f, 0.45f, track);
                    bool approach = paint == MainRoadPaint.Approach;
                    float lineEnd = approach ? stop0 : W + 1f;     // la doble amarilla para en la línea de stop
                    float edgeEnd = approach ? crosswalk0 : W + 1f;

                    Color? mark = null;
                    if (b < edgeEnd && (Mathf.Abs(a - edge) < lineW / 2f || Mathf.Abs(a - (W - edge)) < lineW / 2f)) mark = white;
                    float dc = Mathf.Abs(a - W / 2f);
                    if (b < lineEnd && Mathf.Abs(dc - (gap / 2f + lineW / 2f)) < lineW / 2f) mark = yellow;
                    bool solid = approach && b > solidFrom;
                    bool dashOn = solid || (b % dashPeriod) < dash;
                    if (b < lineEnd && dashOn && (Mathf.Abs(a - laneA) < lineW / 2f || Mathf.Abs(a - (W - laneA)) < lineW / 2f)) mark = white;
                    if (approach)
                    {
                        // Línea de stop en los carriles que llegan al cruce (los de la derecha, u > 0.5).
                        if (b > stop0 && b < stop1 && a > W / 2f + gap && a < W - edge + lineW / 2f) mark = white;
                        // Paso de cebra de barras anchas a lo largo de la calle (barras de 0.5 m cada metro).
                        if (b > crosswalk0 && b < crosswalk1 && a > 0.3f && a < W - 0.3f && ((a - 0.3f) % 1f) < 0.5f) mark = white;
                    }
                    if (mark.HasValue) c = Color.Lerp(c, mark.Value, 0.88f * amount);
                }

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        static Color AsphaltPixel(float u, float v, int x, int y, int scale, int seed)
        {
            float low = Fbm(u, v, 3 * scale, 3, seed);
            float g = 0.38f + 0.06f * (low - 0.5f) * 2f;
            g += 0.05f * (Hash01(x, y, seed + 1) - 0.5f);                    // árido
            if (Hash01(x, y, seed + 2) > 0.985f) g += 0.1f;                   // piedrecitas claras
            var c = new Color(g, g, g * 1.02f);

            // Parches de reparación más oscuros.
            float patch = Fbm(u, v, 2 * scale, 2, seed + 7);
            if (patch > 0.66f) c *= 0.78f;

            // Grietas: ruido "ridged" (cerca de 0.5 = grieta).
            float crackNoise = Fbm(u, v, 5 * scale, 4, seed + 13);
            float crack = Mathf.Abs(crackNoise - 0.5f);
            float crackMask = Fbm(u, v, 2 * scale, 2, seed + 17);              // solo en algunas zonas
            if (crack < 0.012f && crackMask > 0.48f) c *= 0.55f;
            else if (crack < 0.02f && crackMask > 0.48f) c *= 0.8f;
            c.a = 1f;
            return c;
        }

        /// <summary>Acera de hormigón: 2x2 losas con juntas, tonos distintos, manchas y alguna grieta.</summary>
        public static Texture2D Sidewalk(string name, int seed)
        {
            const int S = 256, Slab = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                int sx = x / Slab, sy = y / Slab;
                float g = 0.6f + 0.06f * (Hash01(sx, sy, seed) - 0.5f) * 2f;
                g += 0.05f * (Fbm(u, v, 8, 3, seed + 1) - 0.5f);
                g += 0.03f * (Hash01(x, y, seed + 2) - 0.5f);
                var c = new Color(g, g * 0.985f, g * 0.95f);

                float stain = Fbm(u, v, 4, 3, seed + 3);
                if (stain > 0.62f) c *= Mathf.Lerp(1f, 0.9f, Mathf.Clamp01((stain - 0.62f) / 0.15f));

                float crack = Mathf.Abs(Fbm(u, v, 4, 3, seed + 4) - 0.5f);
                if (crack < 0.01f && Hash01(sx, sy, seed + 5) > 0.5f) c *= 0.6f;

                int jx = x % Slab, jy = y % Slab;
                if (jx < 2 || jy < 2) c *= 0.55f;
                else if (jx < 3 || jy < 3) c *= 1.08f;                       // canto iluminado

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>Césped de invierno: verde apagado, paja y calvas de tierra.</summary>
        public static Texture2D DeadGrass(string name, int seed)
        {
            const int S = 256;
            var px = new Color[S * S];
            var olive = new Color(0.36f, 0.4f, 0.24f);
            var straw = new Color(0.52f, 0.48f, 0.32f);
            var dirt = new Color(0.36f, 0.3f, 0.23f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = Color.Lerp(olive, straw, Fbm(u, v, 6, 4, seed));
                float bare = Fbm(u, v, 3, 3, seed + 1);
                if (bare > 0.6f) c = Color.Lerp(c, dirt, Mathf.Clamp01((bare - 0.6f) / 0.1f));
                c *= 0.8f + 0.4f * Hash01(x, y, seed + 2);                    // briznas
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>Tela asfáltica de tejado plano con grava.</summary>
        public static Texture2D Roof(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float g = 0.3f + 0.08f * (Fbm(u, v, 4, 3, seed) - 0.5f) * 2f + 0.08f * (Hash01(x, y, seed + 1) - 0.5f);
                px[y * S + x] = new Color(g, g, g * 1.03f, 1f);
            }
            return Save(name, S, px);
        }

        // ---------- Detalle callejero ----------

        public const float IronFenceWidth = 2f, IronFenceHeight = 1.5f;
        public const float ChainLinkTile = 1f;

        /// <summary>Valla de barrotes de hierro con lanzas. 128 px = 2 m × 1.5 m. Alfa = recorte.</summary>
        public static Texture2D IronFence(string name)
        {
            const int S = 128;
            var px = new Color[S * S];
            var iron = new Color(0.07f, 0.07f, 0.08f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int bx = x % 16; // un barrote cada 25 cm
                bool rail = (y >= 12 && y < 16) || (y >= 104 && y < 108);
                bool bar = bx >= 7 && bx < 10 && y < 116;
                bool tip = y >= 116 && y < 126 && Mathf.Abs(bx - 8f) < (126 - y) * 0.25f + 0.5f;
                var c = iron * (0.85f + 0.3f * Hash01(x, y, 77));
                c.a = rail || bar || tip ? 1f : 0f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px, alpha: true);
        }

        /// <summary>
        /// Barandilla de porche de madera (apartaments.jfif): pasamanos arriba, listón abajo y balaustres cuadrados cada
        /// 12.5 cm, con alfa entre ellos. 128 px = 1 m de ancho × 1 m de alto.
        /// </summary>
        public static Texture2D PorchRail(string name, Color wood, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool top = y >= 116;
                bool bottom = y >= 6 && y < 14;
                bool spindle = x % 16 >= 6 && x % 16 < 11 && y >= 6 && y < 116;
                float grain = 0.85f + 0.2f * Hash01(x / 2, y / 6, seed) + 0.1f * Fbm((float)x / S, (float)y / S, 4, 2, seed + 1);
                var c = wood * grain;
                if (top && y < 119) c *= 0.75f; // sombra bajo el pasamanos
                c.a = top || bottom || spindle ? 1f : 0f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px, alpha: true);
        }

        /// <summary>Malla metálica de rombos (chain-link), galvanizada con algo de óxido. 128 px = 1 m.</summary>
        public static Texture2D ChainLink(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                bool wire = (x + y) % 16 < 2 || (x - y + 1024) % 16 < 2;
                var c = new Color(0.56f, 0.58f, 0.6f) * (0.8f + 0.35f * Hash01(x, y, seed));
                if (Fbm(u, v, 4, 3, seed + 1) > 0.62f) c = Color.Lerp(c, new Color(0.42f, 0.26f, 0.15f), 0.6f);
                c.a = wire ? 1f : 0f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px, alpha: true);
        }

        /// <summary>
        /// Pista de baloncesto de asfalto con líneas blancas gastadas y la zona pintada.
        /// La textura cubre la pista (width × length m) más un margen alrededor, una sola vez.
        /// </summary>
        public static Texture2D Court(string name, float width, float length, float margin, int seed)
        {
            const int S = 512;
            var px = new Color[S * S];
            var white = new Color(0.85f, 0.85f, 0.82f);
            var keyPaint = new Color(0.42f, 0.17f, 0.14f);
            const float w = 0.06f, keyHalf = 1.8f, keyLen = 5.8f, hoopFromBase = 1.6f, threeR = 5.2f, circleR = 1.8f;
            float cx = width / 2f;

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float mx = u * (width + 2f * margin) - margin, mz = v * (length + 2f * margin) - margin;
                var c = AsphaltPixel(u, v, x, y, 2, seed) * 0.92f;
                bool withinX = mx > -w && mx < width + w, withinZ = mz > -w && mz < length + w;
                bool line = false, key = false;

                if (withinX && withinZ)
                {
                    // Contorno, línea central y círculo central.
                    line |= Mathf.Abs(mx) < w || Mathf.Abs(mx - width) < w;
                    line |= Mathf.Abs(mz) < w || Mathf.Abs(mz - length) < w;
                    line |= Mathf.Abs(mz - length / 2f) < w;
                    line |= Mathf.Abs(Vector2.Distance(new Vector2(mx, mz), new Vector2(cx, length / 2f)) - circleR) < w;

                    foreach (float end in new[] { 0f, length })
                    {
                        float s = end == 0f ? 1f : -1f;           // hacia el centro
                        float along = (mz - end) * s;               // distancia desde la línea de fondo
                        float hoopZ = end + s * hoopFromBase, ftZ = end + s * keyLen;
                        bool inKeyX = Mathf.Abs(mx - cx) < keyHalf;
                        if (along >= 0f && along <= keyLen)
                        {
                            if (inKeyX) key = true;
                            line |= Mathf.Abs(Mathf.Abs(mx - cx) - keyHalf) < w;
                        }
                        line |= inKeyX && Mathf.Abs(mz - ftZ) < w;
                        if ((mz - ftZ) * s > 0f) line |= Mathf.Abs(Vector2.Distance(new Vector2(mx, mz), new Vector2(cx, ftZ)) - circleR) < w;
                        if ((mz - hoopZ) * s > 0f) line |= Mathf.Abs(Vector2.Distance(new Vector2(mx, mz), new Vector2(cx, hoopZ)) - threeR) < w;
                        else if (along >= 0f) line |= Mathf.Abs(Mathf.Abs(mx - cx) - threeR) < w;
                    }
                }

                float wear = Fbm(u, v, 16, 3, seed + 20);
                if (key) c = Color.Lerp(c, keyPaint, 0.55f * Mathf.SmoothStep(0.25f, 0.45f, wear));
                if (line) c = Color.Lerp(c, white, 0.85f * Mathf.SmoothStep(0.3f, 0.5f, wear));
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Hojas de arbusto de invierno: verde oscuro con ramas secas.</summary>
        public static Texture2D Bush(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            var leaf = new Color(0.17f, 0.23f, 0.11f);
            var dry = new Color(0.33f, 0.27f, 0.16f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = Color.Lerp(leaf, dry, Mathf.SmoothStep(0.45f, 0.7f, Fbm(u, v, 4, 3, seed)));
                c *= 0.55f + 0.9f * Hash01(x / 2, y / 2, seed + 1) * Fbm(u, v, 16, 2, seed + 2);
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Copa de árbol: mezcla de dos tonos de hoja con huecos oscuros entre las hojas. 128 px.</summary>
        public static Texture2D Foliage(string name, Color leaf, Color accent, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = Color.Lerp(leaf, accent, Mathf.SmoothStep(0.4f, 0.65f, Fbm(u, v, 4, 3, seed)));
                c *= 0.5f + 0.9f * Hash01(x / 2, y / 2, seed + 1) * Fbm(u, v, 16, 2, seed + 2);
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Nieve vieja: blanco azulado con zonas grises de suciedad y algún punto oscuro. 128 px.</summary>
        public static Texture2D Snow(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            var clean = new Color(0.88f, 0.9f, 0.94f);
            var dirty = new Color(0.58f, 0.57f, 0.55f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = Color.Lerp(clean, dirty, 0.6f * Mathf.SmoothStep(0.55f, 0.8f, Fbm(u, v, 4, 3, seed)));
                c *= 0.94f + 0.08f * Hash01(x, y, seed + 1);
                if (Hash01(x, y, seed + 2) > 0.994f) c = Color.Lerp(c, new Color(0.2f, 0.19f, 0.18f), 0.6f); // grava y hollín
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>
        /// Matojo de hierba alta y seca, de solar abandonado: briznas que salen del borde de abajo, se curvan y
        /// se afinan, con espigas opcionales. 128 px = un matojo. Alfa = recorte.
        /// </summary>
        public static Texture2D TallGrass(string name, Color dry, Color green, bool seedHeads, int seed)
        {
            const int S = 128;
            var px = new Color[S * S]; // transparente
            var rng = new System.Random(seed);
            var head = new Color(0.24f, 0.18f, 0.12f);
            for (int b = 0; b < 46; b++)
            {
                float x0 = 10f + (float)rng.NextDouble() * (S - 20f);
                float h = S * (0.45f + 0.52f * (float)rng.NextDouble());
                float lean = ((float)rng.NextDouble() - 0.5f) * 0.8f;
                float w0 = 1.2f + 1.8f * (float)rng.NextDouble();
                var col = Color.Lerp(dry, green, 0.6f * (float)rng.NextDouble()) * (0.75f + 0.4f * (float)rng.NextDouble());
                float tipX = x0;
                for (int y = 0; y < h && y < S; y++)
                {
                    float t = y / h;
                    float cx = x0 + lean * h * t * t; // se dobla hacia la punta
                    float half = Mathf.Lerp(w0, 0.4f, t);
                    float shade = Mathf.Lerp(0.65f, 1.1f, t);
                    for (int x = Mathf.FloorToInt(cx - half); x <= Mathf.CeilToInt(cx + half); x++)
                        if (x >= 0 && x < S) px[y * S + x] = new Color(col.r * shade, col.g * shade, col.b * shade, 1f);
                    tipX = cx;
                }
                if (!seedHeads || rng.NextDouble() > 0.35) continue;
                // Espiga: un óvalo oscuro en la punta.
                int ty = Mathf.Min(S - 4, (int)h);
                for (int dy = -5; dy <= 3; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = Mathf.RoundToInt(tipX) + dx, y = ty + dy;
                    if (x < 0 || x >= S || y < 0 || y >= S || dx * dx / 4f + dy * dy / 16f > 1f) continue;
                    var c = head * (0.8f + 0.4f * Hash01(x, y, seed));
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            return Save(name, S, S, px, alpha: true, clamp: true);
        }

        // ---------- Barrio de casas ----------

        public const float WoodFenceWidth = 2f, WoodFenceHeight = 1.8f;
        public const float AlleyTile = 5f, ShingleTile = 2f;

        /// <summary>Valla de tablones de madera vieja, gris parda. 128 px = 2 m × 1.8 m.</summary>
        public static Texture2D WoodFence(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            var wood = new Color(0.45f, 0.38f, 0.3f);
            var grey = new Color(0.5f, 0.49f, 0.46f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                int plank = x / 16, px16 = x % 16;
                float tone = 0.8f + 0.35f * Hash01(plank, 0, seed);                // cada tablón de un tono
                var c = Color.Lerp(wood, grey, Fbm(u, v, 4, 3, seed + 1)) * tone;
                c *= 0.9f + 0.15f * Fbm(u * 0.2f, v * 4f, 8, 2, seed + plank);       // vetas verticales
                if (px16 < 1) c *= 0.35f;                                            // junta entre tablones
                if ((y >= 18 && y < 24) || (y >= 104 && y < 110)) c *= 0.82f;       // travesaños detrás
                if (y < 10) c = Color.Lerp(c, new Color(0.22f, 0.18f, 0.13f), 0.5f * (1f - y / 10f)); // humedad abajo
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Callejón: asfalto roto y hundido, con parches de grava y tierra, y una canaleta central. 256 px = 5 m.</summary>
        public static Texture2D Alley(string name, int seed)
        {
            const int S = 256;
            var px = new Color[S * S];
            var dirt = new Color(0.33f, 0.28f, 0.22f);
            var gravel = new Color(0.45f, 0.43f, 0.4f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                var c = AsphaltPixel(u, v, x, y, 1, seed) * 0.9f;
                float broken = Fbm(u, v, 3, 3, seed + 3);
                if (broken > 0.58f) c = Color.Lerp(c, gravel * (0.8f + 0.4f * Hash01(x, y, seed + 4)), Mathf.SmoothStep(0.58f, 0.64f, broken));
                float mud = Fbm(u, v, 2, 3, seed + 5);
                if (mud > 0.62f) c = Color.Lerp(c, dirt, 0.7f * Mathf.SmoothStep(0.62f, 0.7f, mud));
                float edge = Mathf.Min(u, 1f - u);
                if (edge < 0.08f) c = Color.Lerp(c, dirt, 0.5f * (1f - edge / 0.08f));     // tierra en los bordes
                if (Mathf.Abs(u - 0.5f) < 0.03f) c *= 0.8f;                                   // canaleta
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>Tejas asfálticas en hileras, gris oscuro desigual. 128 px = 2 m.</summary>
        public static Texture2D Shingles(string name, Color tint, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                int row = y / 8, shift = row % 2 == 0 ? 0 : 8;
                int tab = (x + shift) / 16;
                var c = tint * (0.75f + 0.4f * Hash01(tab, row, seed)) * (0.9f + 0.2f * Fbm(u, v, 4, 3, seed + 1));
                if (y % 8 < 1 || (x + shift) % 16 < 1) c *= 0.55f;                         // sombra de la hilera
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        // ---------- Tren elevado ----------

        public const float TiesTile = 2.4f;

        /// <summary>
        /// Traviesas de madera vista desde arriba: 4 traviesas cada 2.4 m (filas de la textura) con huecos
        /// transparentes, para ver a través del tablero como en trainavenue.avif. 128 px = 2.4 m. Alfa = recorte.
        /// </summary>
        public static Texture2D Ties(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            var wood = new Color(0.27f, 0.22f, 0.18f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                int tie = y / 32, inTie = y % 32;
                bool solid = inTie < 14;
                var c = wood * (0.75f + 0.4f * Hash01(tie, 0, seed)) * (0.85f + 0.25f * Fbm(u * 4f, v, 8, 2, seed + 1));
                if (inTie == 0 || inTie == 13) c *= 0.6f;
                c.a = solid ? 1f : 0f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px, alpha: true);
        }

        /// <summary>Mancha de suciedad (blanco con alfa): el color lo pone el material. Bordes irregulares, sin tilear.</summary>
        public static Texture2D Grime(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                float n = Fbm(u, v, 4, 4, seed);
                float a = Mathf.Clamp01((1f - r) * 2f) * Mathf.SmoothStep(0.38f, 0.62f, n + 0.25f * (1f - r));
                float tone = 0.8f + 0.2f * Fbm(u, v, 16, 2, seed + 1);
                px[y * S + x] = new Color(tone, tone, tone, a);
            }
            return Save(name, S, S, px, alpha: true, clamp: true);
        }

        /// <summary>
        /// Grafiti abstracto (garabatos tipo "tag" con contorno y chorretones): no son letras ni nombres reales.
        /// 256×128 px con alfa, para pegar en una pared.
        /// </summary>
        public static Texture2D Graffiti(string name, int seed)
        {
            const int W = 256, H = 128;
            var rng = new System.Random(seed);
            var palette = new[]
            {
                new Color(0.9f, 0.2f, 0.55f), new Color(0.2f, 0.6f, 0.95f), new Color(0.95f, 0.75f, 0.1f),
                new Color(0.3f, 0.85f, 0.35f), new Color(0.92f, 0.92f, 0.9f), new Color(0.95f, 0.4f, 0.1f),
            };
            Color fillA = palette[rng.Next(palette.Length)], fillB = palette[rng.Next(palette.Length)];
            var outlineColor = rng.NextDouble() < 0.7 ? new Color(0.05f, 0.05f, 0.06f) : new Color(0.95f, 0.95f, 0.95f);

            var fill = new bool[W * H];
            var outline = new bool[W * H];
            float radius = 4.5f + (float)rng.NextDouble() * 3f;
            int letters = 4 + rng.Next(3);
            float step = (W - 40f) / letters;
            for (int i = 0; i < letters; i++)
            {
                float x0 = 20f + i * step;
                var prev = new Vector2(x0 + (float)rng.NextDouble() * step * 0.5f, 20f + (float)rng.NextDouble() * 20f);
                int points = 3 + rng.Next(3);
                for (int k = 0; k < points; k++)
                {
                    var next = new Vector2(x0 + (float)rng.NextDouble() * step, 18f + (float)rng.NextDouble() * (H - 40f));
                    StampSegment(outline, W, H, prev, next, radius + 3f);
                    StampSegment(fill, W, H, prev, next, radius);
                    prev = next;
                }
            }
            // Chorretones de pintura hacia abajo, desde el punto más bajo del trazo en esa columna.
            for (int d = 0; d < 6; d++)
            {
                int dx = 20 + rng.Next(W - 40);
                int bottom = -1;
                for (int y = H - 1; y >= 0; y--) if (fill[y * W + dx]) bottom = y;
                if (bottom < 0) continue;
                int len = 8 + rng.Next(22);
                StampSegment(fill, W, H, new Vector2(dx, bottom), new Vector2(dx, Mathf.Max(0, bottom - len)), 1.2f);
            }

            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                Color c;
                if (fill[i]) c = Color.Lerp(fillB, fillA, (float)y / H) * (0.85f + 0.2f * Hash01(x, y, seed));
                else if (outline[i]) c = outlineColor;
                else { px[i] = new Color(0f, 0f, 0f, 0f); continue; }
                c.a = 1f;
                px[i] = c;
            }
            return Save(name, W, H, px, alpha: true, clamp: true);
        }

        static void StampSegment(bool[] mask, int w, int h, Vector2 a, Vector2 b, float radius)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            int r = Mathf.CeilToInt(radius);
            for (int s = 0; s <= steps; s++)
            {
                var p = Vector2.Lerp(a, b, (float)s / steps);
                for (int y = (int)p.y - r; y <= (int)p.y + r; y++)
                for (int x = (int)p.x - r; x <= (int)p.x + r; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    if ((new Vector2(x, y) - p).sqrMagnitude <= radius * radius) mask[y * w + x] = true;
                }
            }
        }

        // ---------- Autopista hundida ----------

        public const float HighwayTile = 12f;                              // metros a lo largo por repetición (un trazo de 3 m cada 12 m)
        public const float HighwayShoulderLeft = 0.8f, HighwayLane = 3.1f; // arcén del lado de la mediana y carriles
        public const float WallTileWidth = 8f, WallTileHeight = 7f;        // muro de contención: dos paneles de 4 m × alto de la trinchera

        /// <summary>
        /// Una calzada de la autopista (un sentido), de <paramref name="width"/> m de ancho y <see cref="HighwayTile"/> m a lo
        /// largo (512 px). u cruza la calzada (u = 0 junto a la mediana) y v va a lo largo. Arcén izquierdo con línea amarilla
        /// continua, tres carriles con separadores discontinuos (3 m de trazo cada 12 m) y arcén derecho con línea blanca
        /// y bandas sonoras. Asfalto gastado de invierno: rodadas, sal, grietas selladas, suciedad en los arcenes.
        /// </summary>
        public static Texture2D Highway(string name, float width, int seed)
        {
            const int S = 512;
            var px = new Color[S * S];
            var white = new Color(0.8f, 0.8f, 0.76f);
            var yellow = new Color(0.8f, 0.62f, 0.15f);
            var tar = new Color(0.12f, 0.12f, 0.13f);
            var salt = new Color(0.6f, 0.6f, 0.58f);
            float W = width, L = HighwayTile;
            float leftLine = HighwayShoulderLeft, rightLine = HighwayShoulderLeft + 3f * HighwayLane;
            const float lineW = 0.15f, dash = 3f;
            var wheels = new List<float>();
            for (int k = 0; k < 3; k++)
            {
                float c = leftLine + (k + 0.5f) * HighwayLane;
                wheels.Add(c - 0.85f);
                wheels.Add(c + 0.85f);
            }

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float a = u * W, b = v * L;
                var c = AsphaltPixel(u, v, x, y, 2, seed) * 0.96f;

                float track = 0f;
                foreach (float wheel in wheels) track = Mathf.Max(track, 1f - Mathf.Clamp01(Mathf.Abs(a - wheel) / 0.45f));
                track *= Mathf.Lerp(0.6f, 1f, Fbm(u, v, 4, 2, seed + 31));
                c *= 1f - 0.14f * track;

                // Arcenes: más sucios, con gravilla y bandas sonoras fresadas junto a la línea.
                bool shoulder = a < leftLine || a > rightLine;
                if (shoulder)
                {
                    c *= 0.9f + 0.08f * (Hash01(x, y, seed + 3) - 0.5f);
                    float fromLine = a < leftLine ? leftLine - a : a - rightLine;
                    if (fromLine > 0.15f && fromLine < 0.55f && (b % 0.4f) < 0.16f) c *= 0.72f;
                    if (Fbm(u, v, 8, 3, seed + 4) > 0.6f) c = Color.Lerp(c, new Color(0.3f, 0.27f, 0.22f), 0.35f); // barro y hojas
                }

                // Sal de invierno.
                float saltNoise = Fbm(u, v, 5, 3, seed + 40);
                float saltMask = Mathf.SmoothStep(0.5f, 0.72f, saltNoise) * (1f - track);
                if (shoulder) saltMask = Mathf.Max(saltMask, 0.6f * saltNoise);
                c = Color.Lerp(c, salt, 0.3f * saltMask);

                // Grietas longitudinales selladas con alquitrán en las juntas de carril y alguna transversal.
                float wobble = 0.2f * (Fbm(0.37f, v, 4, 2, seed + 50) - 0.5f);
                bool tarCrack = false;
                for (int k = 1; k <= 2; k++)
                    tarCrack |= Mathf.Abs(a - (leftLine + k * HighwayLane + 0.35f) - wobble) < 0.05f && Fbm(u, v, 2, 2, seed + 52 + k) > 0.45f;
                tarCrack |= Mathf.Abs(b - 5.5f - 1.2f * (Fbm(u, 0.61f, 6, 3, seed + 51) - 0.5f)) < 0.05f && Fbm(u, 0.2f, 3, 2, seed + 54) > 0.55f;
                if (tarCrack) c = Color.Lerp(c, tar, 0.75f);

                // Pintura gastada.
                float wear = Fbm(u, v, 16, 3, seed + 20);
                float amount = Mathf.SmoothStep(0.25f, 0.48f, wear) * Mathf.Lerp(1f, 0.5f, track);
                Color? mark = null;
                if (Mathf.Abs(a - leftLine) < lineW / 2f) mark = yellow;
                if (Mathf.Abs(a - rightLine) < lineW / 2f) mark = white;
                if (b < dash)
                    for (int k = 1; k <= 2; k++)
                        if (Mathf.Abs(a - (leftLine + k * HighwayLane)) < lineW / 2f) mark = white;
                if (mark.HasValue) c = Color.Lerp(c, mark.Value, 0.88f * amount);

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        /// <summary>
        /// Muro de contención de hormigón de la trinchera: <see cref="WallTileWidth"/> × <see cref="WallTileHeight"/> m
        /// (512 px; v = 0 abajo). Juntas de dilatación cada 4 m, marcas del encofrado, mechinales de drenaje con su mancha,
        /// chorretones de agua y óxido desde arriba, eflorescencias blancas, y la parte de abajo ennegrecida y con sal por
        /// el agua que levantan los coches.
        /// </summary>
        public static Texture2D RetainingWall(string name, int seed)
        {
            const int S = 512;
            var px = new Color[S * S];
            var rust = new Color(0.42f, 0.28f, 0.18f);
            var salt = new Color(0.72f, 0.72f, 0.7f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float a = u * WallTileWidth, h = v * WallTileHeight;
                float g = 0.55f + 0.07f * (Fbm(u, v, 4, 4, seed) - 0.5f) * 2f + 0.04f * (Hash01(x, y, seed + 1) - 0.5f);
                var c = new Color(g, g * 0.99f, g * 0.95f);

                // Marcas horizontales del encofrado y coronación algo más clara.
                if (h % 0.6f < 0.025f) c *= 0.93f;
                if (h > WallTileHeight - 0.18f) c *= 1.07f;

                // Juntas de dilatación cada 4 m y eflorescencias blancas junto a ellas.
                float joint = Mathf.Min(a % 4f, 4f - a % 4f);
                if (joint < 0.03f) c *= 0.42f;
                else if (joint < 0.06f) c *= 0.8f;
                else if (joint < 0.5f && Fbm(u, v, 6, 3, seed + 9) > 0.62f) c = Color.Lerp(c, salt, 0.35f);

                // Chorretones desde la coronación: por columnas, de largo y fuerza distintos.
                int col = x / 5;
                float strength = Hash01(col, 0, seed + 5), length = 0.15f + 0.6f * Hash01(col, 1, seed + 6);
                float t = (v - (1f - length)) / length;
                if (strength > 0.55f && t > 0f)
                {
                    float s = t * (strength - 0.55f) / 0.45f * (0.6f + 0.4f * Fbm(u, v, 10, 2, seed + 7));
                    c = Hash01(col, 2, seed + 8) > 0.75f ? Color.Lerp(c, rust, 0.6f * s) : c * (1f - 0.3f * s);
                }

                // Mechinales de drenaje a 0.6 m, cada 2 m, con su mancha debajo.
                float hole = Mathf.Abs((a + 1f) % 2f - 1f);
                if (hole < 0.06f && h > 0.55f && h < 0.67f) c *= 0.25f;
                else if (hole < 0.12f && h < 0.6f && h > 0.2f) c *= 0.8f;

                // Abajo, suciedad y sal de lo que salpican los coches.
                float spray = Mathf.Clamp01(1f - h / 1.8f) * (0.7f + 0.3f * Fbm(u, v, 8, 3, seed + 11));
                c *= 1f - 0.45f * spray;
                if (h < 0.45f) c = Color.Lerp(c, salt, 0.25f * Fbm(u, v, 12, 2, seed + 12));

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, px);
        }

        // ---------- Instituto: campo de fútbol americano con pista ----------

        public const float FieldYard = 0.76f;                    // yarda del campo, algo menor que la real (0.914 m) para que quepa con la pista
        public const float FieldHalfLength = 60f * FieldYard;    // 45.6 m: 100 yardas + dos zonas de anotación de 10
        public const float FieldHalfWidth = 26.67f * FieldYard;  // 20.27 m
        public const float FieldGoalLine = 50f * FieldYard;      // 38 m desde el centro
        public const float TrackHalfStraight = 34f, TrackInnerRadius = 24.5f, TrackLaneWidth = 1.1f;
        public const int TrackLanes = 6;
        public const float FieldMargin = 1.4f;
        public static float TrackOuterRadius => TrackInnerRadius + TrackLanes * TrackLaneWidth;
        /// <summary>Metros que cubre la textura del campo (X a lo largo del campo, Z a lo ancho): la pista y un margen.</summary>
        public static Vector2 FieldTextureSize => new(2f * (TrackHalfStraight + TrackOuterRadius + FieldMargin), 2f * (TrackOuterRadius + FieldMargin));

        /// <summary>
        /// Campo de fútbol americano con la pista de atletismo alrededor, en una sola textura de 2048 × 1024 que cubre
        /// <see cref="FieldTextureSize"/> (u a lo largo del campo, v a lo ancho; el centro del campo es el de la textura).
        /// Hierba de invierno amarillenta con calvas de barro entre los hashes y en las porterías, zonas de anotación
        /// granates con franjas, líneas cada 5 yardas, hashes, números (se leen desde cada banda), pista de caucho rojo
        /// con 6 calles y bordillo, línea de meta y placas de nieve. La pintura está gastada.
        /// </summary>
        public static Texture2D FootballField(string name, int seed)
        {
            const int TW = 2048, TH = 1024;
            var px = new Color[TW * TH];
            var size = FieldTextureSize;
            var paint = new Color(0.88f, 0.88f, 0.84f);
            var endZone = new Color(0.42f, 0.13f, 0.12f);
            var mud = new Color(0.33f, 0.27f, 0.2f);
            var snow = new Color(0.85f, 0.87f, 0.9f);
            var rubber = new Color(0.5f, 0.23f, 0.17f);
            var curb = new Color(0.62f, 0.61f, 0.58f);
            const float cell = 0.21f, gap = 0.3f;     // números: punto de 21 cm (1.05 × 1.47 m), a 30 cm de la línea
            float numberZ = FieldHalfWidth - 8.5f;

            bool Number(float mx, float mz)
            {
                if (Mathf.Abs(Mathf.Abs(mz) - numberZ) > 0.8f) return false;
                for (int n = 1; n <= 9; n++)
                {
                    float line = -FieldGoalLine + n * 10f * FieldYard;
                    if (Mathf.Abs(mx - line) > gap + 5f * cell + 0.1f) continue;
                    int value = (n <= 5 ? n : 10 - n) * 10;
                    char tens = (char)('0' + value / 10), ones = '0';
                    bool south = mz < 0f;
                    // Desde la banda sur se lee con la parte de arriba hacia el centro (+Z); desde la norte, girado 180°.
                    int row = south ? Mathf.FloorToInt((-numberZ + 0.735f - mz) / cell) : Mathf.FloorToInt((mz - (numberZ - 0.735f)) / cell);
                    float leftX0 = line - gap - 5f * cell, rightX0 = line + gap;
                    if (mx >= leftX0 && mx < line - gap)
                    {
                        int col = south ? Mathf.FloorToInt((mx - leftX0) / cell) : Mathf.FloorToInt((line - gap - mx) / cell);
                        return GlyphDot(south ? tens : ones, col, row);
                    }
                    if (mx >= rightX0 && mx < rightX0 + 5f * cell)
                    {
                        int col = south ? Mathf.FloorToInt((mx - rightX0) / cell) : Mathf.FloorToInt((rightX0 + 5f * cell - mx) / cell);
                        return GlyphDot(south ? ones : tens, col, row);
                    }
                }
                return false;
            }

            for (int y = 0; y < TH; y++)
            for (int x = 0; x < TW; x++)
            {
                float u = (x + 0.5f) / TW, v = (y + 0.5f) / TH;
                float mx = (u - 0.5f) * size.x, mz = (v - 0.5f) * size.y;
                float nv = v * size.y / size.x; // ruido con la misma escala en los dos ejes
                float dx = Mathf.Max(Mathf.Abs(mx) - TrackHalfStraight, 0f);
                float dist = Mathf.Sqrt(dx * dx + mz * mz);
                float wear = Fbm(u, nv, 40, 3, seed + 20);
                float amount = 0.9f * Mathf.SmoothStep(0.26f, 0.46f, wear);
                Color c;

                if (dist >= TrackInnerRadius && dist <= TrackOuterRadius)
                {
                    // Pista de caucho: más gastada (clara y con calvas negras) en las calles de dentro.
                    float lane = (dist - TrackInnerRadius) / TrackLaneWidth;
                    c = rubber * (0.88f + 0.2f * Fbm(u, nv, 30, 3, seed + 1) + 0.06f * (Hash01(x, y, seed + 2) - 0.5f));
                    if (lane < 2f && Fbm(u, nv, 20, 2, seed + 3) > 0.62f) c *= 0.6f;
                    float f = Mathf.Abs(lane - Mathf.Round(lane)) * TrackLaneWidth;
                    bool line = f < 0.035f && lane > 0.5f;
                    line |= mz < 0f && Mathf.Abs(mx - TrackHalfStraight) < 0.05f;                 // meta, al final de la recta sur
                    if (line) c = Color.Lerp(c, paint, amount);
                }
                else if (dist < TrackInnerRadius && dist > TrackInnerRadius - 0.18f)
                    c = curb * (0.9f + 0.15f * Hash01(x, y, seed + 4));
                else
                {
                    // Hierba de invierno: amarillenta, con rodales más verdes.
                    float g = Fbm(u, nv, 16, 4, seed + 5);
                    c = Color.Lerp(new Color(0.46f, 0.45f, 0.29f), new Color(0.34f, 0.41f, 0.24f), Mathf.SmoothStep(0.4f, 0.65f, g));
                    c *= 0.9f + 0.18f * (Hash01(x, y, seed + 6) - 0.5f);

                    bool inField = Mathf.Abs(mx) <= FieldHalfLength + 0.06f && Mathf.Abs(mz) <= FieldHalfWidth + 0.06f;
                    if (inField)
                    {
                        // Calvas de barro: entre los hashes, en el centro y delante de cada portería.
                        float trample = Mathf.Clamp01(1f - Mathf.Abs(mz) / 7f) * (0.55f + 0.45f * Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(mx) - 30f) / 14f));
                        trample = Mathf.Max(trample, Mathf.Clamp01(1f - new Vector2(Mathf.Abs(mx) - FieldGoalLine + 4f, mz).magnitude / 6f));
                        float mudNoise = Fbm(u, nv, 24, 3, seed + 7);
                        c = Color.Lerp(c, mud * (0.85f + 0.3f * mudNoise), Mathf.SmoothStep(0.35f, 0.75f, trample * (0.4f + mudNoise)));

                        // Zonas de anotación granates con franjas diagonales.
                        if (Mathf.Abs(mx) > FieldGoalLine && Mathf.Abs(mx) < FieldHalfLength)
                        {
                            float stripe = ((mx + mz) % 3f + 3f) % 3f < 1.5f ? 1f : 0.82f;
                            c = Color.Lerp(c, endZone * stripe, 0.6f * amount);
                        }

                        bool mark = Mathf.Abs(Mathf.Abs(mx) - FieldHalfLength) < 0.07f || Mathf.Abs(Mathf.Abs(mz) - FieldHalfWidth) < 0.07f;
                        float yards = mx / FieldYard;
                        int k = Mathf.RoundToInt(yards);
                        float off = Mathf.Abs(mx - k * FieldYard);
                        if (Mathf.Abs(k) <= 50)
                        {
                            if (k % 5 == 0) mark |= off < (Mathf.Abs(k) == 50 ? 0.09f : 0.055f);
                            else
                            {
                                float am = Mathf.Abs(mz);
                                bool hashRow = (am > 2.1f && am < 2.55f) || (am > FieldHalfWidth - 0.75f && am < FieldHalfWidth - 0.3f);
                                mark |= hashRow && off < 0.04f;
                            }
                        }
                        mark |= Number(mx, mz);
                        if (mark) c = Color.Lerp(c, paint, amount);
                    }
                }

                // Placas de nieve: más fuera de la pista y en los bordes; casi nada en el centro pisoteado.
                float snowNoise = Fbm(u, nv, 10, 4, seed + 30);
                float threshold = dist > TrackOuterRadius ? 0.56f : dist > TrackInnerRadius ? 0.64f : 0.66f;
                c = Color.Lerp(c, snow * (0.92f + 0.08f * Hash01(x, y, seed + 31)), Mathf.SmoothStep(threshold, threshold + 0.05f, snowNoise));

                c.a = 1f;
                px[y * TW + x] = c;
            }
            return Save(name, TW, TH, px, clamp: true);
        }

        // ---------- Ruido tileable ----------

        static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Ruido de valor tileable: <paramref name="period"/> celdas por repetición.</summary>
        static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int Wrap(int a) => ((a % period) + period) % period;
            int xa = Wrap(x0), xb = Wrap(x0 + 1), ya = Wrap(y0), yb = Wrap(y0 + 1);
            float a = Mathf.Lerp(Hash01(xa, ya, seed), Hash01(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash01(xa, yb, seed), Hash01(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>fBm tileable en u,v ∈ [0,1). Devuelve aprox. [0,1].</summary>
        static float Fbm(float u, float v, int baseFreq, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int freq = baseFreq;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * ValueNoise(u * freq, v * freq, freq, seed + i * 31);
                norm += amp;
                amp *= 0.5f;
                freq *= 2;
            }
            return sum / norm;
        }

        // ---------- Letreros de negocios ----------

        // Fuente de píxeles 5×7 (filas de arriba abajo). Solo mayúsculas, '&' y espacio: los letreros llevan palabras
        // genéricas (BARBER SHOP, FOOD & LIQUOR...), nunca marcas ni nombres reales.
        static readonly System.Collections.Generic.Dictionary<char, string[]> Glyphs = new()
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['&'] = new[] { ".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
        };

        /// <summary>¿Está encendido el punto (col, fila desde arriba) de la letra <paramref name="c"/> de la fuente 5×7?</summary>
        static bool GlyphDot(char c, int col, int row) =>
            col >= 0 && col < 5 && row >= 0 && row < 7 && Glyphs.TryGetValue(c, out var glyph) && glyph[row][col] == '#';

        /// <summary>
        /// Letrero de caja de luz: fondo de color con borde, texto en la fuente 5×7 (6 px por punto) y algo de suciedad.
        /// El ancho depende del texto; la proporción es (letras·6 + 3) : 11.
        /// </summary>
        public static Texture2D Sign(string name, string text, Color background, Color ink, int seed)
        {
            const int K = 6, Pad = 2;
            text = text.ToUpperInvariant();
            int cellsW = text.Length * 6 - 1 + Pad * 2, cellsH = 7 + Pad * 2;
            int W = cellsW * K, H = cellsH * K;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / W, v = (float)y / H;
                int cx = x / K, cy = y / K;
                bool border = cx == 0 || cy == 0 || cx == cellsW - 1 || cy == cellsH - 1;
                var c = border ? background * 0.55f : background;
                int row = cellsH - 1 - Pad - cy; // fila de la letra contando desde arriba
                int col = cx - Pad;
                if (!border && row >= 0 && row < 7 && col >= 0)
                {
                    int letter = col / 6, inLetter = col % 6;
                    if (letter < text.Length && GlyphDot(text[letter], inLetter, row))
                        c = ink;
                }
                float grime = Fbm(u, v, 6, 3, seed);
                c *= 0.82f + 0.25f * grime;
                c *= 1f - 0.18f * Mathf.Clamp01(1f - v * 3f); // chorretón de suciedad abajo
                c.a = 1f;
                px[y * W + x] = c;
            }
            return Save(name, W, H, px, clamp: true);
        }

        /// <summary>Persiana metálica enrollable: lamas horizontales grises con óxido y suciedad (1 m de alto por repetición).</summary>
        public static Texture2D Shutter(string name, int seed)
        {
            const int S = 64;
            var px = new Color[S * S];
            var steel = new Color(0.55f, 0.56f, 0.57f);
            var rust = new Color(0.42f, 0.26f, 0.15f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                int rib = y % 8;
                float shade = rib == 0 ? 0.55f : rib == 1 ? 0.8f : rib == 7 ? 1.12f : 1f;
                var c = steel * shade * (0.9f + 0.15f * Hash01(x, y, seed));
                float r = Fbm(u, v, 4, 3, seed + 1);
                if (r > 0.6f) c = Color.Lerp(c, rust, Mathf.SmoothStep(0.6f, 0.7f, r) * 0.7f);
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Poste de barbería: franjas en espiral roja, blanca, azul y blanca (U alrededor, V a lo alto).</summary>
        public static Texture2D BarberPole(string name)
        {
            const int S = 64;
            var px = new Color[S * S];
            var colors = new[] { new Color(0.75f, 0.08f, 0.08f), Color.white * 0.92f, new Color(0.1f, 0.2f, 0.6f), Color.white * 0.92f };
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var c = colors[((x + y) / 8) % 4];
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        // ---------- Cocheras del tren ----------

        public const float BallastTile = 2f;                       // metros por repetición de la grava
        public const float CorrugatedTileU = 1.2f, CorrugatedTileV = 2.6f; // 8 ondas por 1.2 m; alto de un contenedor

        /// <summary>
        /// Balasto de vía: grava gris parda de piedras sueltas, con manchas de aceite y óxido. 128 px = <see cref="BallastTile"/> m.
        /// </summary>
        public static Texture2D Ballast(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            var stone = new Color(0.43f, 0.41f, 0.38f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                // Piedras: celdas de 4 px con tono propio y junta oscura.
                int cx = x / 4, cy = (y + (cx % 2) * 2) / 4;
                float rock = Hash01(cx, cy, seed);
                bool joint = x % 4 == 0 || (y + (cx % 2) * 2) % 4 == 0;
                var c = stone * (0.72f + 0.5f * rock) * (joint ? 0.7f : 1f);
                c = Color.Lerp(c, new Color(0.38f, 0.27f, 0.2f), Mathf.SmoothStep(0.6f, 0.8f, Fbm(u, v, 3, 3, seed + 2)) * 0.5f); // óxido
                c *= 1f - Mathf.SmoothStep(0.68f, 0.85f, Fbm(u, v, 2, 3, seed + 5)) * 0.45f; // aceite
                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>
        /// Chapa ondulada (contenedores y nave): ondas verticales con brillo y sombra, casi blanca para teñirla con el
        /// material, con chorretones de óxido y suciedad abajo. 64×128 px = <see cref="CorrugatedTileU"/> × <see cref="CorrugatedTileV"/> m.
        /// </summary>
        public static Texture2D Corrugated(string name, int seed)
        {
            const int W = 64, H = 128;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / W, v = (float)y / H;
                float wave = 0.78f + 0.22f * Mathf.Sin(u * 8f * 2f * Mathf.PI);
                float c = wave * (0.95f + 0.06f * Hash01(x, y, seed));
                // Chorretones de óxido que bajan desde arriba en algunas ondas.
                float streak = Fbm(u, 0.5f, 6, 2, seed + 3) > 0.62f ? Mathf.SmoothStep(0.3f, 1f, v) : 0f;
                float grime = (1f - v) * 0.25f * Fbm(u, v, 4, 3, seed + 7);
                var col = new Color(c, c, c) * (1f - grime);
                col = Color.Lerp(col, new Color(0.45f, 0.27f, 0.16f), streak * 0.45f);
                col.a = 1f;
                px[y * W + x] = col;
            }
            return Save(name, W, H, px);
        }

        // ---------- Ropa y piel del personaje ----------

        /// <summary>Tejido de una prenda. Las texturas son casi blancas: el color lo pone la prenda (PlayerOutfit lo tiñe).</summary>
        public enum ClothPattern { Knit, Denim, Puffer, Cotton, Twill, Suede, Sneaker, Satin, Stripes }

        /// <summary>
        /// Tejido de ropa, tileable, de 128 px (U alrededor de la pieza, V a lo largo, de abajo arriba), pintado al estilo
        /// de PS2 como en img/gang.png: poca trama y mucha sombra horneada (pliegues con cresta clara y surco oscuro,
        /// costuras con pespunte, desgaste y bordes). Valores cerca del blanco para que el tinte de la prenda mande:
        /// algodón con caídas en diagonal (camiseta XL), vaquero con hebra vertical, desgaste claro y el bajo amontonado
        /// en arrugas abajo (V≈0 es el tobillo en la espinilla y la rodilla en el muslo), felpa con puño de canalé abajo
        /// (sudadera, gorro, chándal), plumífero acolchado, sarga de gorra y caqui con costuras, nobuk de bota de trabajo
        /// con rozaduras y cuello acolchado arriba, zapatilla con pespuntes, raso con brillo y polo de rayas anchas de
        /// colores (la única con color propio; la prenda la tiñe casi de blanco).
        /// </summary>
        public static Texture2D Cloth(string name, ClothPattern pattern, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float grain = Hash01(x, y, seed) - 0.5f;
                float broad = Fbm(u, v, 2, 3, seed + 7) - 0.5f; // luz y sombra pintadas, a manchas grandes
                float c;
                switch (pattern)
                {
                    case ClothPattern.Cotton:
                    {
                        // Camiseta: caídas en diagonal cruzadas y, en la mitad de abajo, pliegues verticales del faldón.
                        float f = Fold(u, v, 2, 1, 0.5f, seed + 1) * Mask(u, v, seed + 2)
                                + Fold(u, v, -1, 2, 0.4f, seed + 3) * Mask(u, v, seed + 4) * 0.8f
                                + Fold(u, v, 5, 0, 0.35f, seed + 5) * Mathf.SmoothStep(0.55f, 0f, v) * 0.9f;
                        c = 0.97f + 0.3f * f + 0.07f * broad + 0.015f * grain;
                        if (y < 4) c *= 0.86f; // dobladillo
                        break;
                    }
                    case ClothPattern.Denim:
                    {
                        // Hebra vertical (a lo largo de la pierna), desgaste claro a manchas, costuras laterales con
                        // pespunte claro y arrugas apiladas abajo (bajo amontonado) y algunas arriba.
                        float warp = (Hash01(x, 7, seed) - 0.5f) * 0.1f + (Hash01(x, y / 6, seed + 9) - 0.5f) * 0.05f;
                        float fade = Mathf.SmoothStep(0.4f, 0.75f, Fbm(u, v, 2, 3, seed + 3)) * 0.24f;
                        float stack = Fold(u, v, 1, 9, 0.6f, seed + 11) * Mathf.SmoothStep(0.4f, 0.05f, v)
                                    + Fold(u, v, -1, 5, 0.6f, seed + 12) * Mathf.SmoothStep(0.75f, 1f, v) * 0.7f
                                    + Fold(u, v, 1, 2, 0.5f, seed + 13) * Mask(u, v, seed + 14) * 0.5f;
                        c = 0.74f + warp + fade + 0.32f * stack + 0.06f * broad + 0.03f * grain;
                        int sx = x % (S / 2);
                        if (sx < 2) c *= 0.72f;                        // costura
                        else if (sx == 3 && y % 4 < 2) c = 0.98f;      // pespunte
                        break;
                    }
                    case ClothPattern.Knit:
                    {
                        // Felpa: pliegues blandos y anchos, apenas trama, y el puño de canalé abajo.
                        float f = Fold(u, v, 1, 2, 0.6f, seed + 1) * Mask(u, v, seed + 2)
                                + Fold(u, v, 2, -1, 0.5f, seed + 3) * Mask(u, v, seed + 4) * 0.7f
                                + Fold(u, v, 0, 6, 0.5f, seed + 5) * Mathf.SmoothStep(0.35f, 0.12f, v) * 0.8f;
                        c = 0.92f + 0.26f * f + 0.08f * broad + 0.02f * grain;
                        if (y < 12) c = (x % 4 < 2 ? 0.8f : 0.92f) + 0.05f * broad;
                        else if (y == 12) c *= 0.75f;
                        break;
                    }
                    case ClothPattern.Puffer:
                    {
                        // Franjas acolchadas abombadas, con la costura hundida y arrugas cortas junto a ella.
                        const int band = 32;
                        float t = (y % band) / (float)(band - 1);
                        float puff = Mathf.Sin(t * Mathf.PI);
                        float pinch = Fold(u, v, 12, 0, 0.3f, seed + 1) * (1f - puff) * 0.5f;
                        c = 0.58f + 0.38f * Mathf.Pow(puff, 0.7f) + 0.12f * pinch + 0.06f * broad + 0.015f * grain;
                        if (y % band < 2) c = 0.38f;
                        break;
                    }
                    case ClothPattern.Twill:
                    {
                        // Gorra y caqui: sarga casi lisa, pliegues finos y costuras de panel con pespunte.
                        float f = Fold(u, v, 1, 3, 0.5f, seed + 1) * Mask(u, v, seed + 2)
                                + Fold(u, v, 0, 7, 0.4f, seed + 3) * Mathf.SmoothStep(0.35f, 0.05f, v) * 0.7f;
                        c = 0.92f + 0.24f * f + 0.07f * broad + 0.02f * grain;
                        int sx = x % (S / 4);
                        if (sx < 2) c *= 0.7f;
                        else if (sx == 3 && y % 4 < 2) c *= 0.85f;
                        break;
                    }
                    case ClothPattern.Satin:
                    {
                        // Raso: liso, con vetas de brillo largas a lo largo de la tela y arrugas tirantes.
                        float sheen = Mathf.Pow(Mathf.Abs(Mathf.Sin((u * 3f + Fbm(u, v, 2, 2, seed + 9) * 0.8f) * Mathf.PI)), 6f);
                        c = 0.8f + 0.2f * sheen + 0.06f * Fold(u, v, 2, 1, 0.5f, seed + 1) + 0.01f * grain;
                        break;
                    }
                    case ClothPattern.Stripes:
                    {
                        // Polo: rayas anchas de colores separadas por filetes crema, con los pliegues de la camiseta.
                        Color[] bands =
                        {
                            new(0.12f, 0.14f, 0.3f), new(0.86f, 0.82f, 0.7f), new(0.62f, 0.12f, 0.1f),
                            new(0.86f, 0.82f, 0.7f), new(0.82f, 0.62f, 0.16f), new(0.16f, 0.32f, 0.2f),
                            new(0.86f, 0.82f, 0.7f), new(0.12f, 0.14f, 0.3f),
                        };
                        const int bandH = 16;
                        float f = Fold(u, v, 2, 1, 0.5f, seed + 1) * Mask(u, v, seed + 2)
                                + Fold(u, v, -1, 2, 0.4f, seed + 3) * Mask(u, v, seed + 4) * 0.8f;
                        var col = bands[(y / bandH) % bands.Length] * (1f + 0.26f * f + 0.06f * broad + 0.015f * grain);
                        if (y % bandH == 0) col *= 0.85f;
                        px[y * S + x] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), 1f);
                        continue;
                    }
                    case ClothPattern.Suede:
                    {
                        // Bota de trabajo: nobuk a manchas, rozaduras oscuras, arrugas en el empeine, doble pespunte y
                        // el cuello acolchado (más oscuro) arriba.
                        float mottle = Fbm(u, v, 6, 3, seed + 5) - 0.5f;
                        float scuff = Mathf.SmoothStep(0.62f, 0.78f, Fbm(u, v, 4, 2, seed + 6)) * 0.22f;
                        float crease = Fold(u, v, 0, 5, 0.5f, seed + 1) * Mask(u, v, seed + 2) * 0.07f;
                        c = 0.86f + 0.16f * mottle - scuff + crease + 0.04f * broad + 0.03f * grain;
                        int sy = y % 64;
                        if ((sy == 10 || sy == 14 || sy == 52) && x % 5 < 3) c *= 0.62f;
                        if (y >= S - 14) c = 0.55f + 0.08f * Fbm(u, v, 8, 2, seed + 8);
                        break;
                    }
                    default: // zapatilla: piel lisa con sombra de arrugas, pespuntes y agujeros de ventilación
                    {
                        float crease = Fold(u, v, 1, 4, 0.5f, seed + 1) * Mask(u, v, seed + 2) * 0.08f;
                        c = 0.94f + crease + 0.05f * broad + 0.01f * grain;
                        int sy = y % 64;
                        if ((sy == 12 || sy == 52) && x % 6 < 3) c *= 0.7f;
                        if (sy > 22 && sy < 42 && x % 10 == 0 && y % 6 == 0) c *= 0.55f;
                        break;
                    }
                }
                c = Mathf.Clamp01(c);
                px[y * S + x] = new Color(c, c, c, 1f);
            }
            return Save(name, S, S, px);
        }

        /// <summary>
        /// Pliegue pintado, tileable (a y b enteros: vueltas en U y en V): cresta clara a un lado y surco oscuro y estrecho
        /// al otro, ondulado con ruido. Devuelve aprox. [-1, 0.35].
        /// </summary>
        static float Fold(float u, float v, int a, int b, float wobble, int seed)
        {
            float w = (Fbm(u, v, 2, 2, seed) - 0.5f) * wobble * 2f;
            float s = Mathf.Sin(2f * Mathf.PI * (a * u + b * v + w));
            float groove = Mathf.Pow((1f - s) * 0.5f, 5f);
            float ridge = Mathf.Pow((1f + s) * 0.5f, 2f);
            return 0.35f * ridge - groove;
        }

        /// <summary>Máscara suave para que los pliegues salgan a zonas y no por toda la tela.</summary>
        static float Mask(float u, float v, int seed) => Mathf.SmoothStep(0.35f, 0.65f, Fbm(u, v, 2, 2, seed));

        /// <summary>Piel con algo de grano y tono irregular, para que no quede plana. Blanca: el color lo pone el material.</summary>
        public static Texture2D Skin(string name, int seed)
        {
            const int S = 64;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                float c = (0.9f + 0.1f * Fbm(u, v, 4, 3, seed)) * (0.97f + 0.04f * Hash01(x, y, seed + 1));
                px[y * S + x] = new Color(c, c * 0.98f, c * 0.97f, 1f);
            }
            return Save(name, S, S, px);
        }

        /// <summary>Pelo rapado: puntitos oscuros muy juntos sobre un fondo algo más claro.</summary>
        public static Texture2D BuzzCut(string name, int seed)
        {
            const int S = 64;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float h = Hash01(x, y, seed);
                float c = h > 0.45f ? 0.25f : 0.55f + 0.2f * Hash01(x, y, seed + 2);
                px[y * S + x] = new Color(c, c, c, 1f);
            }
            return Save(name, S, S, px);
        }

        /// <summary>
        /// Cara pintada a todo color para la cabeza por anillos del personaje (PrototypeSceneBuilder.Body): U da la vuelta
        /// a la cabeza (0.5 es el frente) y V va de la barbilla (0) a la coronilla (1). Como las caras de PS2 de
        /// img/gang.png: sombra horneada (cuencas, laterales, bajo la mandíbula), ojos almendrados con párpado, cejas,
        /// orificios de la nariz, labios, bigote y perilla cortos, y pelo rapado con la línea de la frente marcada.
        /// No imita a nadie: es una cara genérica.
        /// </summary>
        public static Texture2D Face(string name, Color skin, Color hair, int seed)
        {
            const int S = 256;
            var px = new Color[S * S];
            var eyeWhite = new Color(0.8f, 0.76f, 0.7f);
            var iris = new Color(0.12f, 0.07f, 0.05f);
            var lid = new Color(0.07f, 0.045f, 0.035f);
            var lipTop = new Color(0.24f, 0.13f, 0.11f);
            var lipBottom = new Color(0.31f, 0.18f, 0.15f);

            // Distancia elíptica normalizada (1 en el borde).
            float E(float du, float dv, float cu, float cv, float ru, float rv)
            {
                float a = (du - cu) / ru, b = (dv - cv) / rv;
                return a * a + b * b;
            }

            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float du = u - 0.5f, adu = Mathf.Abs(du);
                float d = adu * 2f; // 0 delante, 1 detrás
                float grain = Hash01(x, y, seed) - 0.5f;

                // Piel con luz pintada: frente y caballete de la nariz más claros, pómulos, laterales y bajo la
                // mandíbula más oscuros, y las cuencas de los ojos hundidas.
                float light = 1f
                    + 0.08f * Mathf.Exp(-(du * du) / 0.004f) * Mathf.SmoothStep(0.3f, 0.6f, v)
                    + 0.06f * Mathf.Exp(-E(adu, v, 0.1f, 0.42f, 0.05f, 0.06f))
                    - 0.2f * Mathf.SmoothStep(0.12f, 0.3f, adu)
                    - 0.22f * Mathf.SmoothStep(0.2f, 0f, v) * Mathf.SmoothStep(0.03f, 0.12f, adu)
                    - 0.16f * Mathf.Exp(-E(adu, v, 0.068f, 0.53f, 0.055f, 0.045f))
                    - 0.12f * Mathf.Exp(-E(adu, v, 0.03f, 0.36f, 0.015f, 0.08f)) // sombra a los lados de la nariz
                    + 0.04f * (Fbm(u, v, 6, 3, seed + 3) - 0.5f) + 0.025f * grain;
                var c = skin * light;

                // Ojos: blanco almendrado, iris oscuro, párpado de arriba marcado y pliegue de abajo suave.
                float eye = E(adu, v, 0.068f, 0.535f, 0.03f, 0.015f);
                if (eye < 1f)
                {
                    c = eyeWhite * (0.85f + 0.15f * (1f - eye));
                    if (E(adu, v, 0.064f, 0.533f, 0.011f, 0.014f) < 1f) c = iris;
                }
                if (adu > 0.036f && adu < 0.1f && v > 0.546f && v < 0.556f && E(adu, v, 0.068f, 0.535f, 0.034f, 0.024f) < 1f) c = lid;
                if (E(adu, v, 0.07f, 0.515f, 0.03f, 0.006f) < 1f) c *= 0.8f;

                // Cejas, algo inclinadas hacia fuera.
                float browV = 0.625f - (adu - 0.07f) * 0.15f;
                if (E(adu, v, 0.07f, browV, 0.038f, 0.011f) < 1f && Hash01(x, y, seed + 5) > 0.15f) c = Color.Lerp(c, hair, 0.9f);

                // Orificios de la nariz (la nariz es de malla).
                if (E(adu, v, 0.021f, 0.31f, 0.01f, 0.008f) < 1f) c = skin * 0.35f;

                // Boca: labio de arriba más oscuro, de abajo más lleno, y la comisura.
                if (E(du, v, 0f, 0.228f, 0.046f, 0.012f) < 1f) c = lipTop * (0.95f + 0.05f * grain);
                if (E(du, v, 0f, 0.202f, 0.04f, 0.015f) < 1f) c = lipBottom * (1f + 0.08f * (1f - E(du, v, 0f, 0.205f, 0.04f, 0.015f)));
                if (adu < 0.046f && Mathf.Abs(v - 0.216f) < 0.003f) c = lid;

                // Barba corta: bigote, perilla y una sombra de barba por la mandíbula.
                float stubble = Hash01(x, y, seed + 7);
                if (E(du, v, 0f, 0.262f, 0.052f, 0.013f) < 1f && stubble > 0.2f) c = Color.Lerp(c, hair, 0.85f);
                if (E(du, v, 0f, 0.13f, 0.032f, 0.055f) < 1f && stubble > 0.2f) c = Color.Lerp(c, hair, 0.85f);
                float jaw = Mathf.SmoothStep(0.26f, 0.12f, v) * Mathf.SmoothStep(0.2f, 0.08f, adu);
                if (stubble > 1f - 0.5f * jaw) c = Color.Lerp(c, hair, 0.3f);

                // Pelo rapado: la línea de la frente está alta y recta delante, baja por las sienes y por detrás llega
                // a la nuca. Puntitos de pelo con la piel asomando.
                float hairline = Mathf.Lerp(0.79f, 0.27f, Mathf.SmoothStep(0.14f, 0.85f, d));
                float edge = v - hairline;
                if (edge > 0f)
                {
                    float density = Mathf.SmoothStep(0f, 0.012f, edge);
                    var scalp = Color.Lerp(hair, skin * 0.8f, 0.45f);
                    var h = Hash01(x, y, seed + 9) > 0.4f ? hair : scalp;
                    c = Color.Lerp(c, h * (0.95f + 0.1f * Fbm(u, v, 8, 2, seed + 11)), density);
                }

                c.a = 1f;
                px[y * S + x] = c;
            }
            return Save(name, S, S, px);
        }

        /// <summary>Copo de nieve: punto blanco de borde suave sobre transparente (partículas de nieve).</summary>
        public static Texture2D Snowflake()
        {
            const int S = 32;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S - 0.5f, dy = (y + 0.5f) / S - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.SmoothStep(0.35f, 1f, r)));
                }
            return Save("Copo", S, S, px, alpha: true, clamp: true);
        }

        // ---------- Guardado ----------

        static Texture2D Save(string name, int size, Color[] pixels) => Save(name, size, size, pixels);

        static Texture2D Save(string name, int width, int height, Color[] pixels, bool alpha = false, bool clamp = false)
        {
            if (!AssetDatabase.IsValidFolder(TexturesDir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(TexturesDir).Replace('\\', '/'), Path.GetFileName(TexturesDir));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            string assetPath = $"{TexturesDir}/{name}.png";
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.alphaIsTransparency = alpha;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            // Con recorte por alfa, conserva la cobertura en los mipmaps para que las vallas no desaparezcan de lejos.
            importer.mipMapsPreserveCoverage = alpha;
            importer.alphaTestReferenceValue = 0.5f;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
    }
}
