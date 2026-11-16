using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Cocheras del tren elevado al sur de la calle z = −80, delante del inicio del jugador. Como en las cocheras
    /// reales del elevado de Chicago (vías de estacionamiento rectas y paralelas, escalera de agujas en ángulo a la
    /// entrada y una vía en bucle para dar la vuelta a los trenes), la playa va girada 15° y entera a lo largo de un eje:
    /// la rampa baja recta desde la vía oeste del elevado, da la vuelta en un bucle al fondo y entra por el sur en la
    /// escalera de agujas, que se abre a los dos lados en desvíos de 22° hacia 12 vías de estacionamiento y las 3 de la
    /// nave de mantenimiento. Al oeste, el patio de contenedores. Valla de malla con lona de ocultación y concertina
    /// (con agujeros), entrada con barrera y caseta de vigilante, oficina prefabricada, torres de focos, señales,
    /// traviesas y ruedas apiladas, maleza y nieve. Todo ficticio, sin marcas.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Recinto (valla) y suelo de grava. Al oeste se ensancha hasta la acera de la calle x = −110 pasados los
        // patios de los two-flats de la esquina (z −112).
        const float YardMinX = -74f, YardWestX = -99f, YardWestStepZ = -112f, YardMaxX = 100f, YardNorthZ = -91.5f, YardSouthZ = -296f;
        const float YardGravelTop = 0.04f, YardRailTop = 0.36f;
        // Entrada (hueco de la valla norte), frente al inicio del jugador.
        const float YardGateX0 = -1f, YardGateX1 = 7f;

        // Eje de la playa: girado YardAngle grados (de norte hacia el este). Las vías van a lo largo de u (hacia el
        // NNE) y se separan en v (hacia el ESE). El origen (u, v) = (0, 0) es el final del bucle, donde empieza la
        // escalera de agujas; lo fija la rampa (YardOrigin).
        const float YardAngle = 15f;
        // Rampa: sale de la vía oeste del elevado (TrainTracksX[0], z −92) con una curva de RampTopRadius hacia el
        // eje y baja en recta; mide RampLength en total (pendiente de ~4.5 %). Al pie, el bucle de LoopRadius.
        const float RampStartZ = -92f, RampTopRadius = 60f, RampLength = 185f, LoopRadius = 25f;
        // Escalera de agujas: la vía del bucle se abre en LadderSplitU en dos escaleras a ±LadderAngle; cada vía sale
        // de la escalera con una curva de TurnoutRadius que la deja paralela al eje.
        const float LadderSplitU = 4f, LadderAngle = 22f, TurnoutRadius = 40f;
        // Vías de estacionamiento (v, entre ejes 4.5 m) con topes cerca de la valla norte (z YardBumperZ).
        static readonly float[] StorageTracksV = { -13.5f, -9f, -4.5f, 0f, 4.5f, 9f, 13.5f, 18f, 22.5f, 27f, 31.5f, 36f };
        const float YardBumperZ = -97f;
        // Nave de mantenimiento (en coordenadas de la playa) y sus tres vías.
        const float ShedU0 = 100f, ShedU1 = 148f, ShedV0 = -38f, ShedV1 = -17f, ShedWallH = 8f, ShedRidge = 3f;
        static readonly float[] ShedTracksV = { -21f, -27.5f, -34f };
        // Camino de servicio: de la entrada baja a lo largo de v = YardRoadV hasta u = YardRoadEndU.
        const float YardRoadV = -46f, YardRoadEndU = 6f, YardRoadWidth = 7f;
        // Patio de contenedores (al oeste del camino).
        static readonly float[] ContainerColumnsX = { -95.6f, -93.1f, -90.6f, -83.1f, -80.6f, -78.1f, -70.6f, -68.1f };
        const float ContainerFirstZ = -120f, ContainerRowStep = 15.2f;
        const int ContainerRows = 11;
        const float ContainerLong = 12.2f, ContainerShort = 6.1f, ContainerW = 2.44f, ContainerH = 2.59f;

        static Vector2 YardDir => new(Mathf.Sin(YardAngle * Mathf.Deg2Rad), Mathf.Cos(YardAngle * Mathf.Deg2Rad));
        static Vector2 YardSide => new(Mathf.Cos(YardAngle * Mathf.Deg2Rad), -Mathf.Sin(YardAngle * Mathf.Deg2Rad));
        static Vector2 RampStart => new(TrainTracksX[0], RampStartZ);
        /// <summary>Fin de la curva de arriba de la rampa (desde ahí baja en recta a lo largo de −YardDir).</summary>
        static Vector2 RampCurveEnd => new Vector2(RampStart.x - RampTopRadius, RampStart.y) + RampTopRadius * YardSide;
        /// <summary>Pie de la rampa: empieza el bucle.</summary>
        static Vector2 RampFoot => RampCurveEnd - (RampLength - RampTopRadius * YardAngle * Mathf.Deg2Rad) * YardDir;
        static Vector2 YardOrigin => RampFoot - 2f * LoopRadius * YardSide;
        /// <summary>Punto del plano (x, z) en coordenadas de la playa.</summary>
        static Vector2 Yard(float u, float v) => YardOrigin + u * YardDir + v * YardSide;
        static Vector3 Yard3(float u, float v, float y = YardGravelTop) { var p = Yard(u, v); return new Vector3(p.x, y, p.y); }
        /// <summary>u del tope de la vía v (a la altura z = YardBumperZ).</summary>
        static float StorageEndU(float v) =>
            (YardBumperZ - YardOrigin.y + v * Mathf.Sin(YardAngle * Mathf.Deg2Rad)) / Mathf.Cos(YardAngle * Mathf.Deg2Rad);

        /// <summary>Desvío de la vía v en la escalera: punto de la escalera donde sale (T) y donde queda paralela (fin de la curva).</summary>
        static (Vector2 t, Vector2 end) Turnout(float v)
        {
            float a = LadderAngle * Mathf.Deg2Rad, s = Mathf.Sign(v);
            float vt = Mathf.Abs(v) - TurnoutRadius * (1f - Mathf.Cos(a));
            var t = new Vector2(LadderSplitU + vt / Mathf.Tan(a), s * vt);
            return (t, new Vector2(t.x + TurnoutRadius * Mathf.Sin(a), v));
        }

        /// <summary>Curva del desvío de la vía v, en coordenadas (u, v), de la escalera a la paralela.</summary>
        static List<Vector2> TurnoutCurve(float v, int steps = 5)
        {
            float a = LadderAngle * Mathf.Deg2Rad, s = Mathf.Sign(v);
            var t = Turnout(v).t;
            var pts = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float phi = Mathf.Lerp(a, 0f, (float)i / steps);
                pts.Add(new Vector2(t.x + TurnoutRadius * (Mathf.Sin(a) - Mathf.Sin(phi)), t.y + s * TurnoutRadius * (Mathf.Cos(phi) - Mathf.Cos(a))));
            }
            return pts;
        }

        /// <summary>Bucle del fondo, del pie de la rampa al origen de la playa (giro de 180° a la derecha), en planta.</summary>
        static List<Vector2> LoopCurve(int steps = 24)
        {
            var c = RampFoot - LoopRadius * YardSide;
            var pts = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float phi = Mathf.PI * i / steps;
                pts.Add(c + LoopRadius * (Mathf.Cos(phi) * YardSide - Mathf.Sin(phi) * YardDir));
            }
            return pts;
        }

        /// <summary>Recorrido de la rampa en planta: curva de arriba (cada ~1.5°) y recta hasta el pie.</summary>
        static Vector2[] RampPath()
        {
            var pts = new List<Vector2>();
            var center = new Vector2(RampStart.x - RampTopRadius, RampStart.y);
            for (int i = 0; i <= 10; i++)
            {
                float phi = -YardAngle * Mathf.Deg2Rad * i / 10f;
                pts.Add(center + RampTopRadius * new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)));
            }
            pts.Add(RampFoot);
            return pts.ToArray();
        }

        static Material yardTies, yardRail, yardThirdRail, yardCover, yardBallast, yardConcrete, yardSteel;

        static void BuildRailYard(Transform world)
        {
            var group = new GameObject("Cocheras").transform;
            group.SetParent(world, false);
            var rng = new System.Random(1902);

            yardBallast = TexturedMat("Balasto", ProceduralTextures.Ballast("Balasto", 91));
            yardTies = LoadMat("Traviesas") ?? CutoutMat("Traviesas", ProceduralTextures.Ties("Traviesas", 31));
            yardRail = LoadMat("Rail") ?? Mat("Rail", new Color(0.38f, 0.36f, 0.34f));
            yardThirdRail = Mat("TercerCarril", new Color(0.3f, 0.29f, 0.28f));
            yardCover = Mat("CubiertaTercerCarril", new Color(0.62f, 0.52f, 0.25f));
            yardConcrete = concreteMat ? concreteMat : Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            yardSteel = LoadMat("AceroEstructura") ?? Mat("AceroEstructura", new Color(0.36f, 0.4f, 0.37f));

            // Suelo de grava de todo el recinto (con el ensanche oeste), aceras a los lados (las calles x = ±110 siguen
            // al sur) y camino de servicio.
            var tile = Vector2.one * ProceduralTextures.BallastTile;
            TexturedBox("GravaCocheras", group, new Vector3((YardMinX + YardMaxX) / 2f, YardGravelTop / 2f, (YardNorthZ + YardSouthZ) / 2f),
                new Vector3(YardMaxX - YardMinX, YardGravelTop, YardNorthZ - YardSouthZ), yardBallast, tile);
            TexturedBox("GravaCocheras", group, new Vector3((YardWestX + YardMinX) / 2f, YardGravelTop / 2f, (YardWestStepZ + YardSouthZ) / 2f),
                new Vector3(YardMinX - YardWestX, YardGravelTop, YardWestStepZ - YardSouthZ), yardBallast, tile);
            // La del oeste empieza pasados los two-flats de la esquina.
            Box("AceraCocheras", group, new Vector3(101.5f, 0.075f, -195f), new Vector3(SidewalkWidth, 0.15f, 210f), yardConcrete);
            Box("AceraCocheras", group, new Vector3(-101.5f, 0.075f, -205f), new Vector3(SidewalkWidth, 0.15f, 190f), yardConcrete);
            BuildYardRoad(group);

            BuildYardTracks(group);
            BuildYardLead(group);
            BuildYardShed(group, rng);
            BuildYardTrains(group, rng);
            BuildYardContainers(group, rng);
            BuildYardFence(group, rng);
            BuildYardEntrance(group);
            BuildYardProps(group, rng);
        }

        /// <summary>Camino de servicio de asfalto: de la entrada al eje del camino y por él hacia el fondo, con un ensanche al final.</summary>
        static void BuildYardRoad(Transform parent)
        {
            var asphalt = LoadMat("Asfalto") ?? Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            float topU = StorageEndU(YardRoadV) - 6f;
            var bend = Yard(topU, YardRoadV);
            var gate = new Vector2((YardGateX0 + YardGateX1) / 2f, YardNorthZ);
            void Piece(Vector2 a, Vector2 b, float width, float extra)
            {
                var d = b - a;
                Box("CaminoServicio", parent, new Vector3((a.x + b.x) / 2f, 0.03f, (a.y + b.y) / 2f), new Vector3(width, 0.06f, d.magnitude + extra), asphalt)
                    .transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0f);
            }
            Piece(gate + Vector2.up * 0.5f, bend, YardRoadWidth + 1f, YardRoadWidth / 2f);
            Piece(bend, Yard(YardRoadEndU, YardRoadV), YardRoadWidth, 0f);
            // Explanada al final, para dar la vuelta.
            Piece(Yard(YardRoadEndU - 6f, YardRoadV + 2f), Yard(YardRoadEndU + 10f, YardRoadV + 2f), 12f, 0f);
        }

        // ---------- Vías ----------

        static void BuildYardTracks(Transform parent)
        {
            var group = new GameObject("Vias").transform;
            group.SetParent(parent, false);
            var bumper = Mat("TopeVia", new Color(0.62f, 0.12f, 0.1f));
            var lever = Mat("PalancaAguja", new Color(0.75f, 0.62f, 0.12f));
            float yaw = YardAngle;

            void Polyline(IList<Vector2> uv, bool third)
            {
                for (int i = 1; i < uv.Count; i++)
                    YardTrack(group, Yard(uv[i - 1].x, uv[i - 1].y), Yard(uv[i].x, uv[i].y), third, overlap: i < uv.Count - 1 ? 0.25f : 0f);
            }
            void Lever(Vector2 at, float side)
            {
                var p = Yard3(at.x, at.y + side * 2.4f);
                Box("CajaAguja", group, p + Vector3.up * 0.2f, new Vector3(0.5f, 0.4f, 0.5f), yardSteel).transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Strut("Palanca", group, p + Vector3.up * 0.4f, p + Vector3.up * 1.1f + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 0.25f, 0.05f, lever, cylinder: true);
            }

            // Bucle del fondo (sin tercer carril: los trenes van despacio y con la pértiga de taller).
            var loop = LoopCurve();
            for (int i = 1; i < loop.Count; i++) YardTrack(group, loop[i - 1], loop[i], false, overlap: 0.3f);

            // Vía central: sigue recta desde el bucle hasta el tope.
            YardTrack(group, Yard(0f, 0f), Yard(StorageEndU(0f), 0f), thirdRail: true);
            Bumper(group, StorageEndU(0f), 0f, bumper);
            Lever(new Vector2(LadderSplitU, 0f), -1f);

            // Escaleras a los dos lados: de la bifurcación al último desvío de cada una.
            float lastRight = 0f, lastLeft = 0f;
            foreach (float v in StorageTracksV) { lastRight = Mathf.Max(lastRight, v); lastLeft = Mathf.Min(lastLeft, v); }
            foreach (float v in ShedTracksV) lastLeft = Mathf.Min(lastLeft, v);
            foreach (float last in new[] { lastRight, lastLeft })
                YardTrack(group, Yard(LadderSplitU, 0f), Yard(Turnout(last).t.x, Turnout(last).t.y), thirdRail: false, overlap: 0.3f);

            // Cada vía: curva del desvío, recta hasta el tope y palanca de cambio junto a la aguja.
            foreach (float v in StorageTracksV)
            {
                if (v == 0f) continue;
                var (t, end) = Turnout(v);
                Polyline(TurnoutCurve(v), false);
                YardTrack(group, Yard(end.x, end.y), Yard(StorageEndU(v), v), thirdRail: true);
                Bumper(group, StorageEndU(v), v, bumper);
                if (v != lastRight) Lever(t, Mathf.Sign(v)); // por fuera de la escalera
            }
            foreach (float v in ShedTracksV)
            {
                var (t, end) = Turnout(v);
                Polyline(TurnoutCurve(v), false);
                YardTrack(group, Yard(end.x, end.y), Yard(ShedU1 - 1.5f, v), thirdRail: false); // en la nave, sin tercer carril
                Bumper(group, ShedU1 - 1.1f, v, bumper);
                if (v != lastLeft) Lever(t, Mathf.Sign(v));
            }
        }

        /// <summary>Vía recta a ras de suelo de a a b (x, z): lecho de balasto, traviesas, raíles y, si toca, tercer carril con cubierta.</summary>
        static void YardTrack(Transform parent, Vector2 a, Vector2 b, bool thirdRail, float overlap = 0f)
        {
            var d = b - a;
            float len = d.magnitude + overlap;
            var root = new GameObject("Via").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3((a.x + b.x) / 2f, 0f, (a.y + b.y) / 2f);
            root.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0f);
            TexturedBox("Lecho", root, new Vector3(0f, 0.07f, 0f), new Vector3(3.4f, 0.06f, len), yardBallast, Vector2.one * ProceduralTextures.BallastTile, collider: false);
            var ties = TexturedBox("Traviesas", root, new Vector3(0f, 0.12f, 0f), new Vector3(2.6f, 0.16f, len), yardTies,
                new Vector2(2.6f, ProceduralTextures.TiesTile), collider: false);
            ties.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            foreach (float dx in new[] { -0.72f, 0.72f })
                Box("Rail", root, new Vector3(dx, YardRailTop - 0.08f, 0f), new Vector3(0.08f, 0.16f, len), yardRail, collider: false);
            if (!thirdRail) return;
            Box("TercerCarril", root, new Vector3(1.4f, 0.25f, 0f), new Vector3(0.08f, 0.1f, len), yardThirdRail, collider: false);
            Box("Cubierta", root, new Vector3(1.4f, 0.34f, 0f), new Vector3(0.32f, 0.03f, len), yardCover, collider: false);
        }

        /// <summary>Tope al final de la vía v (en u), de cara a los trenes que llegan desde el sur.</summary>
        static void Bumper(Transform parent, float u, float v, Material paint)
        {
            var root = new GameObject("Tope").transform;
            root.SetParent(parent, false);
            root.localPosition = Yard3(u, v, 0f);
            root.localRotation = Quaternion.Euler(0f, YardAngle, 0f);
            Box("TopeVia", root, new Vector3(0f, 0.55f, 0.3f), new Vector3(2.4f, 1.1f, 0.6f), yardConcrete);
            Box("TopeViga", root, new Vector3(0f, 1.0f, -0.05f), new Vector3(2.2f, 0.3f, 0.12f), paint, collider: false);
        }

        // ---------- Rampa desde el elevado ----------

        /// <summary>
        /// Vía de acceso: sale de la vía oeste del elevado a 8.66 m y baja por tramos de 3 m con pendiente constante
        /// (curva de arriba y recta a lo largo del eje de la playa) hasta el bucle del fondo. Arriba va en viaducto
        /// (vigas y pórticos de acero cada 4 tramos; sobre la acera de la avenida no lleva pilares, se apoya en los
        /// pórticos del elevado); por debajo de 2.6 m, sobre un terraplén con muros de contención (se puede subir).
        /// </summary>
        static void BuildYardLead(Transform parent)
        {
            var group = new GameObject("RampaAcceso").transform;
            group.SetParent(parent, false);
            var wall = LoadMat("MuroContencion") ?? yardConcrete;
            var wallTile = new Vector2(ProceduralTextures.WallTileWidth, ProceduralTextures.WallTileHeight);
            var path = RampPath();

            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
            const float piece = 3f;
            int chunks = Mathf.CeilToInt(total / piece);
            float HeightAt(float s) => Mathf.Lerp(TrainRailTop, YardRailTop, s / total);
            Vector2 PointAt(float s, out Vector2 dir)
            {
                for (int i = 1; i < path.Length; i++)
                {
                    float seg = Vector2.Distance(path[i - 1], path[i]);
                    dir = (path[i] - path[i - 1]).normalized;
                    if (s <= seg || i == path.Length - 1) return path[i - 1] + dir * Mathf.Min(s, seg);
                    s -= seg;
                }
                dir = Vector2.down;
                return path[0];
            }

            float pitch = Mathf.Atan2(TrainRailTop - YardRailTop, total) * Mathf.Rad2Deg;
            for (int c = 0; c < chunks; c++)
            {
                float s = (c + 0.5f) * piece;
                var p = PointAt(s, out var dir);
                float h = HeightAt(s);
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                var root = new GameObject("Tramo").transform;
                root.SetParent(group, false);
                root.localPosition = new Vector3(p.x, h, p.y);
                root.localRotation = Quaternion.Euler(pitch, yaw, 0f);
                TexturedBox("Traviesas", root, new Vector3(0f, -0.24f, 0f), new Vector3(2.6f, 0.16f, piece + 0.1f), yardTies,
                    new Vector2(2.6f, ProceduralTextures.TiesTile), collider: false);
                foreach (float dx in new[] { -0.72f, 0.72f })
                    Box("Rail", root, new Vector3(dx, -0.08f, 0f), new Vector3(0.08f, 0.16f, piece + 0.1f), yardRail, collider: false);

                var flat = Quaternion.Euler(0f, yaw, 0f);
                var right = flat * Vector3.right;
                if (h > 2.6f)
                {
                    foreach (float dx in new[] { -1f, 1f })
                        Box("VigaRampa", root, new Vector3(dx, -0.85f, 0f), new Vector3(0.35f, 1f, piece + 0.1f), yardSteel, collider: false);
                    if (c % 4 == 0 && p.x + 2f < YardMaxX) // sobre la acera de la avenida, sin pilares
                    {
                        float top = h - 1.35f;
                        foreach (float side in new[] { -1.7f, 1.7f })
                        {
                            var g = new Vector3(p.x, 0f, p.y) + right * side;
                            Box("ZapataRampa", group, g + Vector3.up * 0.2f, new Vector3(0.9f, 0.4f, 0.9f), yardConcrete);
                            Box("PilarRampa", group, g + Vector3.up * (top / 2f), new Vector3(0.5f, top, 0.5f), yardSteel)
                                .transform.localRotation = flat;
                        }
                        Box("TravesanoRampa", group, new Vector3(p.x, top + 0.2f, p.y), new Vector3(4.2f, 0.4f, 0.6f), yardSteel, collider: false)
                            .transform.localRotation = flat;
                    }
                }
                else if (h - 0.3f > 0.1f)
                {
                    float fill = h - 0.3f;
                    TexturedBox("Terraplen", group, new Vector3(p.x, fill / 2f, p.y), new Vector3(4.4f, fill, piece + 0.05f), wall, wallTile)
                        .transform.localRotation = flat;
                }
            }
        }

        // ---------- Nave de mantenimiento ----------

        /// <summary>
        /// Nave a lo largo del eje de la playa (se construye en local: x = v, z = u, centrada). Ladrillo hasta 3 m y chapa
        /// encima, tejado a dos aguas, portones al sur por donde entran las tres vías, puerta de personal al oeste (hacia
        /// el camino) y otra cerrada al norte.
        /// </summary>
        static void BuildYardShed(Transform parent, System.Random rng)
        {
            var root = new GameObject("NaveMantenimiento").transform;
            root.SetParent(parent, false);
            float midU = (ShedU0 + ShedU1) / 2f, midV = (ShedV0 + ShedV1) / 2f;
            root.localPosition = Yard3(midU, midV, 0f);
            root.localRotation = Quaternion.Euler(0f, YardAngle, 0f);
            var brick = TexturedMat("LadrilloNave", ProceduralTextures.PlainBrick("LadrilloNave", new Color(0.5f, 0.27f, 0.2f), 92));
            var brickTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight);
            var corrugated = ProceduralTextures.Corrugated("ChapaOndulada", 93);
            var sheet = TexturedMat("ChapaNave", corrugated, new Color(0.52f, 0.56f, 0.52f));
            var roof = TexturedMat("TejadoNave", corrugated, new Color(0.3f, 0.31f, 0.32f));
            var sheetTile = new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            const float t = 0.4f, brickH = 3f;
            float width = ShedV1 - ShedV0, len = ShedU1 - ShedU0;
            float x0 = -width / 2f, x1 = width / 2f, zS = -len / 2f, zN = len / 2f;

            // Tramo de pared: ladrillo abajo y chapa arriba. a y b en planta local (x, z); de y0 a y1.
            void Wall(Vector2 a, Vector2 b, float y0, float y1)
            {
                var d = b - a;
                float l = d.magnitude;
                var rot = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
                var c = (a + b) / 2f;
                if (y0 < brickH)
                {
                    float top = Mathf.Min(brickH, y1);
                    TexturedBox("MuroLadrillo", root, new Vector3(c.x, (y0 + top) / 2f, c.y), new Vector3(l, top - y0, t), brick, brickTile, sidesOnly: false)
                        .transform.localRotation = rot;
                }
                if (y1 > brickH)
                {
                    float bottom = Mathf.Max(brickH, y0);
                    TexturedBox("MuroChapa", root, new Vector3(c.x, (bottom + y1) / 2f, c.y), new Vector3(l, y1 - bottom, t), sheet, sheetTile)
                        .transform.localRotation = rot;
                }
            }

            // Este, entera. Oeste, con una puerta de personal hacia el camino de servicio.
            float ex = x1 - t / 2f, wx = x0 + t / 2f, door = zN - 12f;
            Wall(new Vector2(ex, zS), new Vector2(ex, zN), 0f, ShedWallH);
            Wall(new Vector2(wx, zS), new Vector2(wx, door - 0.7f), 0f, ShedWallH);
            Wall(new Vector2(wx, door + 0.7f), new Vector2(wx, zN), 0f, ShedWallH);
            Wall(new Vector2(wx, door - 0.7f), new Vector2(wx, door + 0.7f), 2.3f, ShedWallH);
            // Norte, entera (con una puerta de chapa cerrada, de adorno).
            Wall(new Vector2(x0, zN - t / 2f), new Vector2(x1, zN - t / 2f), 0f, ShedWallH);
            var doorMetal = LoadMat("PuertaMetal") ?? Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f));
            Box("PuertaNorte", root, new Vector3(0f, 1.1f, zN + 0.03f), new Vector3(1.2f, 2.2f, 0.08f), doorMetal, collider: false);
            // Sur: tres portones de 5 × 6.5 m por donde entran las vías; uno con la persiana a medio bajar.
            float sz = zS + t / 2f;
            var tracks = new List<float>();
            foreach (float v in ShedTracksV) tracks.Add(v - midV);
            tracks.Sort();
            float prev = x0;
            foreach (float x in tracks)
            {
                Wall(new Vector2(prev, sz), new Vector2(x - 2.5f, sz), 0f, ShedWallH);
                Wall(new Vector2(x - 2.5f, sz), new Vector2(x + 2.5f, sz), 6.5f, ShedWallH);
                prev = x + 2.5f;
            }
            Wall(new Vector2(prev, sz), new Vector2(x1, sz), 0f, ShedWallH);
            var shutter = Mat("PersianaNave", new Color(0.55f, 0.55f, 0.53f));
            Box("Persiana", root, new Vector3(tracks[0], 5f, sz), new Vector3(5f, 3f, 0.12f), shutter, collider: false);

            // Tejado a dos aguas (cumbrera a lo largo del eje) y hastiales.
            float half = width / 2f, slope = Mathf.Atan2(ShedRidge, half) * Mathf.Rad2Deg;
            float panel = Mathf.Sqrt(half * half + ShedRidge * ShedRidge) + 0.6f;
            foreach (float side in new[] { -1f, 1f })
                TexturedBox("Tejado", root, new Vector3(side * half / 2f, ShedWallH + ShedRidge / 2f + 0.1f, 0f), new Vector3(panel, 0.2f, len + 0.8f), roof, sheetTile)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -side * slope);
            foreach (float z in new[] { zN - t / 2f, zS + t / 2f })
            {
                var gable = new GameObject("Hastial");
                gable.transform.SetParent(root, false);
                gable.transform.localPosition = new Vector3(0f, ShedWallH, z);
                gable.AddComponent<MeshFilter>().sharedMesh = GableMesh(width, ShedRidge, t - 0.02f);
                gable.AddComponent<MeshRenderer>().sharedMaterial = sheet;
            }
            // Ventanas altas corridas en los laterales.
            for (float z = zS + 4f; z < zN - 3f; z += 6f)
                foreach (float x in new[] { wx, ex })
                    Box("VentanaAlta", root, new Vector3(x, 5.8f, z), new Vector3(t + 0.06f, 1.4f, 3.2f), glass, collider: false);

            // Dentro: solera de hormigón, pasarela de trabajo entre dos vías, banco, bidones, ruedas sueltas y luces.
            Box("Solera", root, new Vector3(0f, 0.045f, 0f), new Vector3(width - 2f * t, 0.03f, len - 2f * t), yardConcrete);
            var yellow = Mat("PinturaAmarilla", new Color(0.8f, 0.64f, 0.12f));
            float walk = (tracks[1] + tracks[2]) / 2f;
            Box("PasarelaTrabajo", root, new Vector3(walk, 0.55f, 2f), new Vector3(1.6f, 1.1f, 30f), yardSteel);
            Box("BordePasarela", root, new Vector3(walk - 0.78f, 1.12f, 2f), new Vector3(0.06f, 0.04f, 30f), yellow, collider: false);
            Box("BancoTrabajo", root, new Vector3(x0 + 1f, 0.45f, zN - 6f), new Vector3(1f, 0.9f, 5f), Mat("MaderaBanco", new Color(0.4f, 0.3f, 0.2f)));
            for (int i = 0; i < 5; i++)
                Primitive(PrimitiveType.Cylinder, "Bidon", root, new Vector3(x0 + 1f + Range(rng, 0f, 0.6f), 0.45f, -2f - i * 0.75f),
                    new Vector3(0.6f, 0.45f, 0.6f), Mat("BidonAzul", new Color(0.12f, 0.24f, 0.45f)), keepCollider: true);
            float gap = (tracks[0] + tracks[1]) / 2f;
            Wheelset(root, new Vector3(gap, 0f, zS + 8f), 90f);
            Wheelset(root, new Vector3(gap, 0f, zS + 10f), 90f);
            var lamp = EmissiveMat("LuzNave", new Color(1f, 0.95f, 0.85f), 2f);
            foreach (float z in new[] { zN - 12f, zS + 14f })
            {
                Box("Lampara", root, new Vector3(0f, ShedWallH - 0.3f, z), new Vector3(1.6f, 0.15f, 0.4f), lamp, collider: false);
                var light = new GameObject("LuzNave").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.localPosition = new Vector3(0f, ShedWallH - 0.8f, z);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.93f, 0.8f);
                light.range = 22f;
                light.intensity = 2f;
                light.shadows = LightShadows.None;
            }

            // Grafitis en la fachada oeste (la del camino) y en la norte.
            if (graffitiMats != null)
            {
                for (int i = 0; i < 3; i++)
                    if (graffitiMats[i % graffitiMats.Length])
                        WallGraffiti(root, new Vector3(x0, Range(rng, 1.3f, 2f), zS + 6f + i * 12f), Vector3.left, Range(rng, 3f, 4.5f), graffitiMats[i % graffitiMats.Length]);
                if (graffitiMats[3]) WallGraffiti(root, new Vector3(-5f, 1.6f, zN), Vector3.forward, 3.5f, graffitiMats[3]);
            }
        }

        /// <summary>Prisma triangular (hastial): base de <paramref name="width"/> en X centrada, alto <paramref name="height"/>, grosor en Z.</summary>
        static Mesh GableMesh(float width, float height, float thickness)
        {
            float hw = width / 2f, hz = thickness / 2f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            void Face(Vector3[] corners, Vector3 normal)
            {
                int s = v.Count;
                foreach (var c in corners)
                {
                    v.Add(c);
                    n.Add(normal);
                    uv.Add(new Vector2((c.x + hw) / ProceduralTextures.CorrugatedTileU, c.y / ProceduralTextures.CorrugatedTileV));
                }
                for (int i = 1; i < corners.Length - 1; i++) tri.AddRange(new[] { s, s + i, s + i + 1 });
            }
            // Caras delantera y trasera (sentido horario visto desde fuera).
            Face(new[] { new Vector3(-hw, 0f, -hz), new Vector3(0f, height, -hz), new Vector3(hw, 0f, -hz) }, Vector3.back);
            Face(new[] { new Vector3(hw, 0f, hz), new Vector3(0f, height, hz), new Vector3(-hw, 0f, hz) }, Vector3.forward);
            var mesh = new Mesh { name = $"Hastial_{width:0.#}x{height:0.#}" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Wheelset(Transform parent, Vector3 ground, float yaw)
        {
            var root = new GameObject("Eje").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var dark = LoadMat("BajosTren") ?? Mat("BajosTren", new Color(0.08f, 0.08f, 0.09f));
            foreach (float x in new[] { -0.72f, 0.72f })
                Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(x, 0.45f, 0f), new Vector3(0.9f, 0.06f, 0.9f), dark, keepCollider: true)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Primitive(PrimitiveType.Cylinder, "Eje", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.8f, 0.14f), dark, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        // ---------- Trenes aparcados ----------

        static void BuildYardTrains(Transform parent, System.Random rng)
        {
            var group = new GameObject("TrenesAparcados").transform;
            group.SetParent(parent, false);
            var body = LoadMat("AceroTren");
            var dark = LoadMat("BajosTren");
            var doors = LoadMat("PuertasTren");
            var stripe = LoadMat("FranjaTren");
            // Aparcados con las luces apagadas: ventanas y faros oscuros.
            var windows = Mat("VentanasTrenApagado", new Color(0.12f, 0.14f, 0.15f));
            var headlight = Mat("FaroTrenApagado", new Color(0.5f, 0.48f, 0.42f));
            if (!body || !dark || !doors || !stripe) return; // BuildTrains crea esos materiales antes

            const float carStep = 15.2f; // vagón de 14.6 m y fuelle de 0.6 m
            // (vía v, vagones, hueco hasta el tope en m). Unos van contra el tope y otros más atrás, y quedan vías
            // vacías y huecos entre trenes para cruzar la playa a pie.
            var parked = new List<(float v, int cars, float gap, bool shed)>
            {
                (StorageTracksV[0], 6, 1.5f, false), (StorageTracksV[2], 4, 1.5f, false), (StorageTracksV[3], 8, 1.5f, false),
                (StorageTracksV[5], 4, 22f, false), (StorageTracksV[6], 6, 1.5f, false), (StorageTracksV[8], 2, 1.5f, false),
                (StorageTracksV[9], 4, 1.5f, false), (StorageTracksV[10], 4, 30f, false), (StorageTracksV[11], 2, 1.5f, false),
                (ShedTracksV[1], 2, 12f, true),
            };
            foreach (var (v, cars, gap, inShed) in parked)
            {
                float end = inShed ? ShedU1 - 1.4f : StorageEndU(v);
                float u = end - gap - cars * carStep / 2f;
                var train = Train("TrenAparcado", body, dark, windows, doors, stripe, headlight, cars);
                train.transform.SetParent(group, false);
                train.transform.localPosition = Yard3(u, v, YardRailTop);
                train.transform.localRotation = Quaternion.Euler(0f, YardAngle, 0f);
                train.AddComponent<SolidVisual>(); // los colliders de los vagones cuentan como visibles (escalar, cubrirse)
                foreach (Transform car in train.transform)
                {
                    var col = car.gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 2.15f, 0f);
                    col.size = new Vector3(2.9f, 3.7f, 14.6f);
                    // Grafitis en los costados de los de fuera de la nave.
                    if (inShed || graffitiMats == null) continue;
                    foreach (float side in new[] { -1f, 1f })
                        if (rng.NextDouble() < 0.45)
                        {
                            var mat = graffitiMats[rng.Next(graffitiMats.Length)];
                            if (mat) WallGraffiti(car, new Vector3(side * 1.46f, Range(rng, 1.7f, 2.4f), Range(rng, -3.5f, 3.5f)), side > 0f ? Vector3.right : Vector3.left,
                                Range(rng, 4f, 7f), mat);
                        }
                }
            }
        }

        // ---------- Contenedores ----------

        static void BuildYardContainers(Transform parent, System.Random rng)
        {
            var group = new GameObject("Contenedores").transform;
            group.SetParent(parent, false);
            var corrugated = ProceduralTextures.Corrugated("ChapaOndulada", 93);
            var colors = new[]
            {
                new Color(0.55f, 0.16f, 0.12f), new Color(0.14f, 0.28f, 0.5f), new Color(0.2f, 0.36f, 0.25f),
                new Color(0.5f, 0.5f, 0.48f), new Color(0.75f, 0.73f, 0.68f), new Color(0.72f, 0.38f, 0.12f),
            };
            var shells = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++) shells[i] = TexturedMat($"Contenedor_{i}", corrugated, colors[i]);
            var rods = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            var pallet = Mat("MaderaPale", new Color(0.55f, 0.44f, 0.3f));

            for (int row = 0; row < ContainerRows; row++)
            {
                float z = ContainerFirstZ - row * ContainerRowStep;
                foreach (float x in ContainerColumnsX)
                {
                    double r = rng.NextDouble();
                    int levels = r < 0.18 ? 0 : r < 0.5 ? 1 : r < 0.82 ? 2 : 3;
                    bool hideout = row == 2 && x == ContainerColumnsX[5];
                    if (hideout) levels = Mathf.Max(levels, 1);
                    for (int level = 0; level < levels; level++)
                    {
                        bool shortBox = level > 0 && rng.NextDouble() < 0.3;
                        float length = shortBox ? ContainerShort : ContainerLong;
                        float dz = shortBox ? Range(rng, -3f, 3f) : Range(rng, -0.15f, 0.15f);
                        float yaw = Range(rng, -1.2f, 1.2f) + (rng.NextDouble() < 0.5 ? 180f : 0f);
                        Container(group, new Vector3(x + Range(rng, -0.08f, 0.08f), YardGravelTop + level * ContainerH, z + dz), yaw, length,
                            shells[rng.Next(shells.Length)], rods, open: hideout && level == 0, rng);
                    }
                    // Palés apilados al pie de algunas pilas: escalón para subir encima de los contenedores.
                    if (levels > 0 && rng.NextDouble() < 0.3)
                    {
                        float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                        float pz = z + Range(rng, -4f, 4f);
                        int stack = 4 + rng.Next(3);
                        for (int k = 0; k < stack; k++)
                            Box("Pale", group, new Vector3(x + side * 1.9f, YardGravelTop + 0.075f + k * 0.16f, pz + (k % 2) * 0.03f),
                                new Vector3(1.2f, 0.15f, 1.2f), pallet);
                    }
                }
            }

            // Carretilla elevadora parada en el pasillo central.
            var forklift = new GameObject("Carretilla").transform;
            forklift.SetParent(group, false);
            forklift.localPosition = new Vector3(-86.85f, YardGravelTop, -165f);
            forklift.localRotation = Quaternion.Euler(0f, 8f, 0f);
            var yellow = Mat("PinturaAmarilla", new Color(0.8f, 0.64f, 0.12f));
            var black = LoadMat("BajosTren") ?? rods;
            Box("Cuerpo", forklift, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 1f, 2.2f), yellow);
            Box("Contrapeso", forklift, new Vector3(0f, 0.9f, -1.2f), new Vector3(1.15f, 0.9f, 0.4f), black, collider: false);
            Box("Mastil", forklift, new Vector3(0f, 1.6f, 1.2f), new Vector3(0.9f, 3f, 0.15f), black, collider: false);
            foreach (float x in new[] { -0.3f, 0.3f })
                Box("Horquilla", forklift, new Vector3(x, 0.12f, 1.85f), new Vector3(0.12f, 0.05f, 1.1f), black, collider: false);
            Box("Techo", forklift, new Vector3(0f, 2.2f, -0.1f), new Vector3(1.2f, 0.06f, 1.4f), black, collider: false);
            foreach (float x in new[] { -0.55f, 0.55f })
            foreach (float z in new[] { -0.7f, 0.7f })
                Primitive(PrimitiveType.Cylinder, "Rueda", forklift, new Vector3(x, 0.3f, z), new Vector3(0.6f, 0.12f, 0.6f), black, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        /// <summary>
        /// Contenedor de <paramref name="length"/> m (a lo largo de Z) apoyado en <paramref name="ground"/>, con las puertas
        /// en +Z y sus barras de cierre. Si va abierto (escondite), es hueco: se entra por las puertas, abiertas de par en par.
        /// </summary>
        static void Container(Transform parent, Vector3 ground, float yaw, float length, Material shell, Material rods, bool open, System.Random rng)
        {
            var root = new GameObject(open ? "ContenedorAbierto" : "Contenedor").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tile = new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);
            float hw = ContainerW / 2f, hl = length / 2f;
            if (!open)
                TexturedBox("Caja", root, new Vector3(0f, ContainerH / 2f, 0f), new Vector3(ContainerW, ContainerH, length), shell, tile);
            else
            {
                const float t = 0.06f;
                TexturedBox("Suelo", root, new Vector3(0f, t / 2f, 0f), new Vector3(ContainerW, t, length), shell, tile);
                TexturedBox("Techo", root, new Vector3(0f, ContainerH - t / 2f, 0f), new Vector3(ContainerW, t, length), shell, tile);
                foreach (float side in new[] { -1f, 1f })
                    TexturedBox("Lateral", root, new Vector3(side * (hw - t / 2f), ContainerH / 2f, 0f), new Vector3(t, ContainerH, length), shell, tile);
                TexturedBox("Fondo", root, new Vector3(0f, ContainerH / 2f, -hl + t / 2f), new Vector3(ContainerW, ContainerH, t), shell, tile);
                // Puertas abiertas contra los costados y, dentro, un colchón y una caja.
                foreach (float side in new[] { -1f, 1f })
                    TexturedBox("Puerta", root, new Vector3(side * (hw + 0.05f), ContainerH / 2f, hl + hw / 2f + 0.05f), new Vector3(0.05f, ContainerH - 0.1f, hw), shell, tile);
                Box("Colchon", root, new Vector3(0f, 0.18f, -hl + 1.4f), new Vector3(1.4f, 0.22f, 2f), Mat("Colchon", new Color(0.62f, 0.58f, 0.5f)));
                Box("Caja", root, new Vector3(0.7f, 0.3f, -hl + 3.2f), new Vector3(0.5f, 0.5f, 0.5f), Mat("CajaCarton", new Color(0.5f, 0.38f, 0.24f)));
                return;
            }
            // Barras de cierre en las puertas y esquineros.
            for (int i = 0; i < 4; i++)
                Box("BarraCierre", root, new Vector3(-0.9f + i * 0.6f, ContainerH / 2f, hl + 0.03f), new Vector3(0.04f, ContainerH - 0.2f, 0.04f), rods, collider: false);
            Box("JuntaPuertas", root, new Vector3(0f, ContainerH / 2f, hl + 0.015f), new Vector3(0.03f, ContainerH - 0.1f, 0.02f), rods, collider: false);
            // Algún grafiti en el costado.
            if (graffitiMats != null && rng.NextDouble() < 0.18 && ground.y < 0.5f)
            {
                var mat = graffitiMats[rng.Next(graffitiMats.Length)];
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                if (mat) WallGraffiti(root, new Vector3(side * (hw + 0.005f), 1.4f, Range(rng, -hl + 2.5f, hl - 2.5f)), side > 0f ? Vector3.right : Vector3.left, Mathf.Min(length - 2f, 4.5f), mat);
            }
        }

        // ---------- Valla ----------

        /// <summary>
        /// Malla de 2.2 m con concertina encima (se puede trepar, como todas las vallas) y, por dentro, una lona de
        /// ocultación verde oscura en paños de unos 15 m (rollos de 50 pies) que no deja ver la playa desde la calle;
        /// la lona tapa, así que sirve de cobertura. Algún paño está rasgado con un jirón colgando o falta, y hay dos
        /// agujeros en la malla.
        /// </summary>
        static void BuildYardFence(Transform parent, System.Random rng)
        {
            var group = new GameObject("VallaCocheras").transform;
            group.SetParent(parent, false);
            var mesh = chainLink ? chainLink : LoadMat("MallaMetalica");
            var post = ironMat ? ironMat : Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            if (!mesh) return;
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            var screen = TexturedMat("LonaOcultacion", ProceduralTextures.WindScreen("LonaOcultacion", 96));
            var screenTile = new Vector2(ProceduralTextures.WindScreenTileU, ScreenH);
            const float h = 2.2f;
            float g = YardGravelTop;

            // inward: hacia dónde queda la playa (la lona va por dentro).
            void Run(Vector3 a, Vector3 b, Vector3 inward)
            {
                FenceSegment(group, a, b, h, mesh, tile, post, 3f, 0.06f, topRail: true);
                Strut("Concertina", group, a + Vector3.up * (h + 0.25f), b + Vector3.up * (h + 0.25f), 0.45f, mesh, cylinder: true);
                ScreenRun(group, a + inward * 0.07f, b + inward * 0.07f, screen, screenTile, rng);
            }
            // Norte: abierta en la entrada.
            Run(new Vector3(YardMinX, g, YardNorthZ), new Vector3(YardGateX0, g, YardNorthZ), Vector3.back);
            Run(new Vector3(YardGateX1, g, YardNorthZ), new Vector3(YardMaxX, g, YardNorthZ), Vector3.back);
            // Oeste: detrás de los patios de los two-flats de la esquina y, pasados estos, junto a la acera de la calle
            // x = −110. El agujero da a los patios (en el quiebro).
            Run(new Vector3(YardMinX, g, YardNorthZ), new Vector3(YardMinX, g, YardWestStepZ), Vector3.right);
            Run(new Vector3(YardMinX, g, YardWestStepZ), new Vector3(-84f, g, YardWestStepZ), Vector3.back);
            Run(new Vector3(-86f, g, YardWestStepZ), new Vector3(YardWestX, g, YardWestStepZ), Vector3.back);
            Run(new Vector3(YardWestX, g, YardWestStepZ), new Vector3(YardWestX, g, YardSouthZ), Vector3.right);
            // Sur, con otro agujero, y este (la rampa pasa por encima).
            Run(new Vector3(YardWestX, g, YardSouthZ), new Vector3(-20f, g, YardSouthZ), Vector3.forward);
            Run(new Vector3(-17.8f, g, YardSouthZ), new Vector3(YardMaxX, g, YardSouthZ), Vector3.forward);
            Run(new Vector3(YardMaxX, g, YardSouthZ), new Vector3(YardMaxX, g, YardNorthZ), Vector3.left);

            // Malla arrancada colgando en los agujeros.
            var torn = TexturedBox("MallaRota", group, new Vector3(-84.5f, 0.9f, YardWestStepZ - 0.5f), new Vector3(1.4f, 1.6f, 0.03f), mesh, tile, collider: false, sidesOnly: true);
            torn.transform.localRotation = Quaternion.Euler(8f, -20f, 12f);
            var torn2 = TexturedBox("MallaRota", group, new Vector3(-18.4f, 0.8f, YardSouthZ + 0.6f), new Vector3(1.6f, 1.4f, 0.03f), mesh, tile, collider: false, sidesOnly: true);
            torn2.transform.localRotation = Quaternion.Euler(-10f, 25f, -8f);

            // Maleza al pie de la valla, por dentro y por fuera.
            if (tallGrass && weeds)
            {
                for (float x = YardWestX + 1f; x < YardMaxX - 1f; x += Range(rng, 1.4f, 2.6f))
                {
                    if (x > YardMinX && (x < YardGateX0 - 1f || x > YardGateX1 + 1f) && rng.NextDouble() < 0.5)
                        Clump(group, rng, new Vector3(x, g, YardNorthZ - Range(rng, 0.4f, 1.2f)), 0.4f, 1.1f);
                    if (rng.NextDouble() < 0.7) Clump(group, rng, new Vector3(x, g, YardSouthZ + Range(rng, 0.4f, 2.5f)), 0.5f, 1.4f);
                }
                for (float z = YardSouthZ + 1f; z < YardNorthZ - 1f; z += Range(rng, 1.4f, 2.6f))
                {
                    float west = z > YardWestStepZ ? YardMinX : YardWestX;
                    if (rng.NextDouble() < 0.55) Clump(group, rng, new Vector3(west + Range(rng, 0.4f, 1.5f), g, z), 0.4f, 1.2f);
                    if (rng.NextDouble() < 0.55) Clump(group, rng, new Vector3(YardMaxX - Range(rng, 0.4f, 1.5f), g, z), 0.4f, 1.2f);
                }
            }
        }

        const float ScreenBottom = 0.12f, ScreenH = 1.9f, ScreenPanel = 15.2f;

        /// <summary>
        /// Lona de ocultación a lo largo de a–b, en paños de ~15 m atados a la malla: la mayoría enteros, alguno rasgado
        /// por abajo con el jirón colgando y alguno que falta (por ahí se ve la playa).
        /// </summary>
        static void ScreenRun(Transform parent, Vector3 a, Vector3 b, Material screen, Vector2 tile, System.Random rng)
        {
            var d = b - a;
            float length = d.magnitude;
            if (length < 0.5f) return;
            var dir = d / length;
            var rot = Quaternion.Euler(0f, -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg, 0f);
            int panels = Mathf.Max(1, Mathf.RoundToInt(length / ScreenPanel));
            float step = length / panels;
            for (int i = 0; i < panels; i++)
            {
                float s0 = i * step, s1 = s0 + step - 0.04f;
                double r = rng.NextDouble();
                if (r < 0.07) continue; // paño que falta
                if (r < 0.17)
                {
                    // Rasgado: falta un trozo de abajo y el jirón cuelga doblado.
                    float tear0 = s0 + Range(rng, 2f, step - 6f), tear1 = tear0 + Range(rng, 2f, 3.5f);
                    float tearTop = Range(rng, 0.9f, 1.4f);
                    ScreenPiece(parent, a, dir, rot, s0, tear0, ScreenBottom, ScreenH, screen, tile);
                    ScreenPiece(parent, a, dir, rot, tear1, s1, ScreenBottom, ScreenH, screen, tile);
                    ScreenPiece(parent, a, dir, rot, tear0, tear1, ScreenBottom + tearTop, ScreenH - tearTop, screen, tile);
                    var flap = ScreenPiece(parent, a, dir, rot, tear0, tear0 + 1.2f, ScreenBottom + tearTop - 0.9f, 0.9f, screen, tile);
                    flap.transform.localRotation = rot * Quaternion.Euler(Range(rng, -25f, -12f), 0f, Range(rng, -18f, 18f));
                    continue;
                }
                ScreenPiece(parent, a, dir, rot, s0, s1, ScreenBottom, ScreenH, screen, tile);
            }
        }

        static GameObject ScreenPiece(Transform parent, Vector3 a, Vector3 dir, Quaternion rot, float s0, float s1, float y0, float height, Material screen, Vector2 tile)
        {
            var center = a + dir * ((s0 + s1) / 2f) + Vector3.up * (y0 + height / 2f);
            var piece = TexturedBox("Lona", parent, center, new Vector3(s1 - s0, height, 0.02f), screen, tile, collider: true, sidesOnly: true);
            piece.transform.localRotation = rot;
            return piece;
        }

        // ---------- Entrada ----------

        static void BuildYardEntrance(Transform parent)
        {
            var root = new GameObject("EntradaCocheras").transform;
            root.SetParent(parent, false);
            var post = ironMat ? ironMat : Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            float g = YardGravelTop;

            // Pórtico con el letrero, puerta corredera abierta y barrera medio levantada.
            foreach (float x in new[] { YardGateX0, YardGateX1 })
                Box("PostePortico", root, new Vector3(x, 2.5f, YardNorthZ), new Vector3(0.3f, 5f, 0.3f), post);
            Box("Dintel", root, new Vector3((YardGateX0 + YardGateX1) / 2f, 5f, YardNorthZ), new Vector3(YardGateX1 - YardGateX0 + 0.3f, 0.3f, 0.3f), post, collider: false);
            var sign = SignMat("Letrero_Cocheras", "RAIL YARD", new Color(0.1f, 0.2f, 0.4f), new Color(0.92f, 0.92f, 0.88f), 94, 0.3f);
            SignQuad(root, new Vector3((YardGateX0 + YardGateX1) / 2f, 4.4f, YardNorthZ + 0.18f), Vector3.forward, 4f, 4f * 11f / (9 * 6 + 3), sign);
            var warning = SignMat("Letrero_Prohibido", "NO TRESPASSING", new Color(0.85f, 0.85f, 0.82f), new Color(0.7f, 0.1f, 0.08f), 95, 0f);
            foreach (float x in new[] { -12f, 22f, 60f, -50f }) // por fuera, sobre la malla
                SignQuad(root, new Vector3(x, 1.5f, YardNorthZ + 0.04f), Vector3.forward, 2.6f, 2.6f * 11f / (14 * 6 + 3), warning);
            if (chainLink)
                FenceSegment(root, new Vector3(YardGateX1, g, YardNorthZ - 0.5f), new Vector3(YardGateX1 + 6f, g, YardNorthZ - 0.5f), 2.2f, chainLink,
                    Vector2.one * ProceduralTextures.ChainLinkTile, post, 3f, 0.06f, topRail: true);
            var red = Mat("BarreraRoja", new Color(0.7f, 0.1f, 0.08f));
            var white = LoadMat("PinturaBlanca") ?? Mat("PinturaBlanca", new Color(0.75f, 0.75f, 0.73f));
            var pivot = new Vector3(YardGateX1 - 0.6f, 1f, YardNorthZ - 2f);
            Box("PosteBarrera", root, pivot + Vector3.down * 0.5f, new Vector3(0.35f, 1f, 0.35f), white);
            var tip = pivot + new Vector3(-6.5f * Mathf.Cos(55f * Mathf.Deg2Rad), 6.5f * Mathf.Sin(55f * Mathf.Deg2Rad), 0f);
            Strut("Barrera", root, pivot, tip, 0.1f, red);
            Strut("BarreraFranja", root, Vector3.Lerp(pivot, tip, 0.33f), Vector3.Lerp(pivot, tip, 0.5f), 0.105f, white);

            // Caseta del vigilante, junto a la entrada, con luz.
            var booth = new GameObject("CasetaVigilante").transform;
            booth.SetParent(root, false);
            booth.localPosition = new Vector3(YardGateX1 + 2.8f, g, YardNorthZ - 4f);
            var wall = Mat("CasetaBlanca", new Color(0.7f, 0.69f, 0.64f));
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            Box("Muros", booth, new Vector3(0f, 1.25f, 0f), new Vector3(2.2f, 2.5f, 2.2f), wall);
            Box("Ventanas", booth, new Vector3(0f, 1.6f, 0f), new Vector3(2.24f, 0.8f, 2.24f), glass, collider: false);
            Box("Puerta", booth, new Vector3(1.12f, 1f, 0.3f), new Vector3(0.05f, 2f, 0.85f), LoadMat("PuertaMetal") ?? wall, collider: false);
            Box("Tejado", booth, new Vector3(0f, 2.6f, 0f), new Vector3(2.7f, 0.15f, 2.7f), post, collider: false);
            var lamp = EmissiveMat("LuzCaseta", new Color(1f, 0.8f, 0.55f), 2.5f);
            Box("Foco", booth, new Vector3(-0.8f, 2.4f, 1.15f), new Vector3(0.3f, 0.15f, 0.2f), lamp, collider: false);
            var light = new GameObject("LuzCaseta").AddComponent<Light>();
            light.transform.SetParent(booth, false);
            light.transform.localPosition = new Vector3(-0.8f, 2.2f, 1.6f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.45f);
            light.range = 9f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
        }
        // ---------- Resto ----------

        static void BuildYardProps(Transform parent, System.Random rng)
        {
            var group = new GameObject("Detalles").transform;
            group.SetParent(parent, false);
            float g = YardGravelTop;

            // Oficina prefabricada junto a la entrada, sobre bloques, con escalones, aparato de aire y un coche al lado.
            var office = new GameObject("Oficina").transform;
            office.SetParent(group, false);
            office.localPosition = new Vector3(-20f, g, -97f);
            var white = TexturedMat("ChapaOficina", ProceduralTextures.Corrugated("ChapaOndulada", 93), new Color(0.78f, 0.77f, 0.72f));
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            foreach (float x in new[] { -4.2f, 0f, 4.2f })
                Box("Bloque", office, new Vector3(x, 0.25f, 0f), new Vector3(0.6f, 0.5f, 2.6f), yardConcrete);
            TexturedBox("Modulo", office, new Vector3(0f, 1.8f, 0f), new Vector3(10f, 2.6f, 3f), white,
                new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV));
            for (int i = 0; i < 3; i++)
                Box("Ventana", office, new Vector3(-3.5f + i * 2.4f, 2.1f, -1.52f), new Vector3(1.2f, 0.8f, 0.04f), glass, collider: false);
            Box("Puerta", office, new Vector3(3.6f, 1.55f, -1.52f), new Vector3(0.9f, 2.0f, 0.04f), LoadMat("PuertaMetal") ?? glass, collider: false);
            for (int k = 0; k < 3; k++)
                Box("Escalon", office, new Vector3(3.6f, 0.17f + k * 0.17f, -2.4f + k * 0.3f), new Vector3(1.2f, 0.17f, 0.3f), yardSteel);
            Box("AparatoAire", office, new Vector3(-4.6f, 1.7f, -1.7f), new Vector3(0.7f, 0.5f, 0.45f), LoadMat("AparatoAire") ?? white, collider: false);
            ParkedCar(group, new Vector3(-31f, 0f, -98f), 90f, rng);
            Dumpster(group, new Vector3(-9.5f, g, -98.5f), 0f, rng.NextDouble() < 0.5);

            // Caseta de los equipos de vía junto a la escalera de agujas, con su puerta hacia el camino.
            var hut = new GameObject("CasetaVia").transform;
            hut.SetParent(group, false);
            hut.localPosition = Yard3(30f, YardRoadV + 6f);
            hut.localRotation = Quaternion.Euler(0f, YardAngle, 0f);
            Box("Muros", hut, new Vector3(0f, 1.4f, 0f), new Vector3(3f, 2.8f, 5f), LoadMat("LadrilloNave") ?? yardConcrete);
            Box("Tejado", hut, new Vector3(0f, 2.9f, 0f), new Vector3(3.4f, 0.2f, 5.4f), yardSteel, collider: false);
            Box("Puerta", hut, new Vector3(-1.52f, 1.05f, 0.8f), new Vector3(0.05f, 2.1f, 0.9f), LoadMat("PuertaMetal") ?? yardSteel, collider: false);

            // Torres de focos: en el patio de contenedores, junto al camino, en el bucle y entre las vías y la rampa.
            var pole = LoadMat("Poste") ?? Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lampMat = EmissiveMat("FocoCocheras", new Color(1f, 0.92f, 0.75f), 3f);
            foreach (var (pos, target) in new[]
                     {
                         (new Vector3(-74.5f, g, -150f), new Vector3(-86f, 0f, -200f)), (new Vector3(-74.5f, g, -250f), new Vector3(-86f, 0f, -200f)),
                         (Yard3(92f, YardRoadV + 5f), Yard3(130f, 0f, 0f)), (Yard3(40f, YardRoadV + 5f), Yard3(40f, -5f, 0f)),
                         (Yard3(-12f, LoopRadius), Yard3(20f, 15f, 0f)), (Yard3(70f, 42.5f), Yard3(90f, 15f, 0f)), (Yard3(150f, 42.5f), Yard3(140f, 15f, 0f)),
                     })
                FloodTower(group, pos, target, pole, lampMat);

            // Señales: una alta a la salida del bucle (antes de la bifurcación) y enanas al principio de cada vía.
            var redLens = EmissiveMat("SenalRoja", new Color(1f, 0.12f, 0.08f), 3f);
            var greenLens = EmissiveMat("SenalVerde", new Color(0.2f, 1f, 0.4f), 2f);
            var black = LoadMat("BajosTren") ?? pole;
            var yardRot = Quaternion.Euler(0f, YardAngle, 0f);
            var mast = Yard3(-3f, 2.6f);
            Primitive(PrimitiveType.Cylinder, "MastilSenal", group, mast + Vector3.up * 2.5f, new Vector3(0.18f, 2.5f, 0.18f), pole, keepCollider: true);
            Box("CabezaSenal", group, mast + new Vector3(0f, 5.4f, 0f), new Vector3(0.5f, 1.2f, 0.35f), black, collider: false).transform.localRotation = yardRot;
            Box("LuzRoja", group, mast + Vector3.up * 5.75f + yardRot * new Vector3(0f, 0f, -0.18f), new Vector3(0.22f, 0.22f, 0.04f), redLens, collider: false)
                .transform.localRotation = yardRot;
            Box("LuzVerde", group, mast + Vector3.up * 5.1f + yardRot * new Vector3(0f, 0f, -0.18f), new Vector3(0.22f, 0.22f, 0.04f), black, collider: false)
                .transform.localRotation = yardRot;
            var starts = new List<float>(StorageTracksV);
            starts.AddRange(ShedTracksV);
            for (int i = 0; i < starts.Count; i++)
            {
                float v = starts[i];
                float u = v == 0f ? 30f : Turnout(v).end.x + 3f;
                var p = Yard3(u, v - (v == 0f ? -2.1f : Mathf.Sign(v) * 2.1f)); // del lado de la vía de dentro, ya paralela
                Box("SenalEnana", group, p + Vector3.up * 0.45f, new Vector3(0.35f, 0.9f, 0.3f), black).transform.localRotation = yardRot;
                Box("Luz", group, p + Vector3.up * 0.7f + yardRot * new Vector3(0f, 0f, -0.16f), new Vector3(0.15f, 0.15f, 0.03f), i % 3 == 0 ? greenLens : redLens, collider: false)
                    .transform.localRotation = yardRot;
            }

            // Traviesas y carriles viejos apilados, ruedas sueltas y montones de balasto, entre las vías y la rampa y
            // en la cuña libre entre la rampa y la avenida.
            var oldWood = Mat("TraviesaVieja", new Color(0.24f, 0.19f, 0.15f));
            for (int pile = 0; pile < 4; pile++)
            {
                // Una pila por objeto, con un solo collider que la envuelve (SolidVisual: cuenta como visible).
                var c = new GameObject("PilaTraviesas").transform;
                c.SetParent(group, false);
                c.localPosition = Yard3(85f + pile * 16f, 42.5f);
                c.localRotation = yardRot;
                c.gameObject.AddComponent<SolidVisual>();
                int layers = 3 + rng.Next(4);
                for (int k = 0; k < layers; k++)
                    for (int j = 0; j < 5; j++)
                        Box("TraviesaApilada", c, new Vector3(j * 0.3f - 0.6f + (k % 2) * 0.1f, 0.1f + k * 0.2f, 0f), new Vector3(0.25f, 0.18f, 2.6f), oldWood, collider: false);
                ColliderBox(c, "LimitePila", new Vector3(0f, layers * 0.1f, 0f), new Vector3(1.6f, layers * 0.2f, 2.6f));
            }
            for (int k = 0; k < 6; k++)
                Box("CarrilViejo", group, new Vector3(84f, g + 0.08f + (k / 3) * 0.16f, -228f + (k % 3) * 0.35f), new Vector3(12f, 0.15f, 0.12f), yardRail);
            for (int k = 0; k < 4; k++)
                Wheelset(group, new Vector3(80f + k * 1.5f, g, -246f), 90f);
            foreach (var p in new[] { new Vector3(86f, g, -268f), new Vector3(-60f, g, -288f), new Vector3(-40f, g, -112f) })
            {
                var mound = Primitive(PrimitiveType.Sphere, "MontonBalasto", group, p, new Vector3(6f, 2.4f, 4.5f), yardBallast, keepCollider: true);
                mound.transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f);
            }

            // Nieve apartada a los lados del camino de servicio y en las esquinas, y bidones oxidados junto a la nave.
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                SnowMound(group, Yard3(Range(rng, 14f, 140f), YardRoadV + side * (YardRoadWidth / 2f + 1.2f)),
                    new Vector3(Range(rng, 1.6f, 3f), Range(rng, 0.4f, 0.8f), Range(rng, 1.2f, 2.2f)), Range(rng, 0f, 90f));
            }
            foreach (var p in new[] { new Vector3(YardWestX + 3f, g, YardSouthZ + 3f), new Vector3(YardMaxX - 3f, g, YardSouthZ + 3f), new Vector3(YardMaxX - 4f, g, YardNorthZ - 4f) })
                SnowMound(group, p, new Vector3(Range(rng, 2.5f, 4f), Range(rng, 0.6f, 1f), Range(rng, 2f, 3f)), Range(rng, 0f, 90f));
            var drum = Mat("BidonOxido", new Color(0.38f, 0.22f, 0.14f));
            for (int i = 0; i < 6; i++)
            {
                var p = Yard3(Range(rng, ShedU0 + 2f, ShedU1 - 16f), ShedV0 - Range(rng, 0.8f, 2.2f));
                bool tipped = rng.NextDouble() < 0.4;
                var d = Primitive(PrimitiveType.Cylinder, "Bidon", group, p + Vector3.up * (tipped ? 0.3f : 0.45f), new Vector3(0.6f, 0.45f, 0.6f), drum, keepCollider: true);
                if (tipped) d.transform.localRotation = Quaternion.Euler(90f, Range(rng, 0f, 180f), 0f);
            }
            // Hierbajos entre las vías (solo dentro del recinto).
            if (tallGrass && weeds)
                for (int i = 0; i < 140; i++)
                {
                    var p = Yard3(Range(rng, 6f, 176f), Range(rng, -15f, 38f));
                    if (p.z > YardBumperZ || p.x > YardMaxX - 1f) continue;
                    Clump(group, rng, p, 0.25f, 0.7f);
                }
        }

        /// <summary>Torre de focos de 16 m con cuatro proyectores y un foco real (sin sombras) apuntando a la playa.</summary>
        static void FloodTower(Transform parent, Vector3 ground, Vector3 target, Material pole, Material lampMat)
        {
            const float height = 16f;
            Primitive(PrimitiveType.Cylinder, "TorreFocos", parent, ground + Vector3.up * (height / 2f), new Vector3(0.4f, height / 2f, 0.4f), pole, keepCollider: true);
            var head = new GameObject("Proyectores").transform;
            head.SetParent(parent, false);
            head.localPosition = ground + Vector3.up * height;
            var flat = target - ground;
            flat.y = 0f;
            head.localRotation = Quaternion.LookRotation(flat.sqrMagnitude > 0.01f ? flat : Vector3.forward);
            Box("Bastidor", head, Vector3.zero, new Vector3(2.6f, 0.15f, 0.2f), pole, collider: false);
            for (int i = 0; i < 4; i++)
                Box("Proyector", head, new Vector3(-0.95f + i * 0.63f, 0.35f, 0.15f), new Vector3(0.5f, 0.4f, 0.12f), lampMat, collider: false)
                    .transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            var light = new GameObject("LuzFocos").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = ground + Vector3.up * (height - 0.5f);
            light.transform.localRotation = Quaternion.LookRotation(target - light.transform.localPosition);
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.92f, 0.78f);
            light.range = 60f;
            light.spotAngle = 95f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            light.gameObject.AddComponent<NightLight>(); // apagada de día, encendida al anochecer
        }
    }
}
