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
        };

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
                    if (letter < text.Length && inLetter < 5 && Glyphs.TryGetValue(text[letter], out var glyph) && glyph[row][inLetter] == '#')
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
