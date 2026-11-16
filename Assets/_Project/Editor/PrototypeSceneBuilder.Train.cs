using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Tren elevado sobre la avenida principal (x = 110), de punta a punta del mapa: pórticos de acero apoyados en
    /// las aceras, vía doble con traviesas abiertas, pasarelas con barandilla, una estación con andenes, marquesinas
    /// y escaleras, y dos trenes de 4 vagones que circulan en sentidos contrarios (<see cref="ElevatedTrain"/>).
    /// Referencia: img/estructures/trainavenue.avif. Decorativo: los trenes no se pueden usar.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float TrainAxisX = 110f;                         // eje de la avenida
        const float TrainColumnWestX = 101.8f, TrainColumnEastX = 118.2f; // pilares sobre las aceras
        const float TrainColumnHeight = 6.6f;                  // gálibo libre bajo la viga
        const float TrainRailTop = 8.66f;                      // altura de la cara superior del raíl
        const float TrainBentSpacing = 15f;
        const float TrainTrackStartZ = -340f, TrainTrackEndZ = 540f; // más allá de la niebla (fin a 260 m); al norte, por encima del terraplén
        static readonly float[] TrainTracksX = { 106.5f, 113.5f };   // vía sur (oeste) y vía norte (este)

        // Estación: andenes laterales de z StationZ0 a StationZ1.
        const float StationZ0 = 4f, StationZ1 = 68f, PlatformTop = 9.6f;
        static float StationCenterZ => (StationZ0 + StationZ1) / 2f;

        // Calles que pasan por debajo (centro z, media anchura): ahí no se ponen pórticos.
        static readonly (float z, float half)[] TrainCrossings = { (80f, 7f), (-80f, 7f), (0f, 6f), (122f, 5f), (188f, 5f), (128f, 5f), (194f, 5f), (253f, 5f), (293f, 5f), (318f, 8f), (395f, 11.5f), (439.5f, 4.5f), (491f, 5f) }; // 128 y 194: calles C1 y A de la copia oeste; 253 y 293: F/Fe y H de la ampliación norte; 318, 395 y 491: diagonal, bulevar y calle del borde del barrio norte; 439.5: entrada del centro comercial

        static void BuildElevatedTrain(Transform world)
        {
            var group = new GameObject("TrenElevado_Estructura").transform;
            group.SetParent(world, false);

            var steel = Mat("AceroEstructura", new Color(0.36f, 0.4f, 0.37f));
            var rust = Mat("AceroOxidado", new Color(0.33f, 0.24f, 0.18f));
            var railMat = Mat("Rail", new Color(0.38f, 0.36f, 0.34f));
            railMat.SetFloat("_Metallic", 0.8f);
            railMat.SetFloat("_Smoothness", 0.55f);
            var ties = CutoutMat("Traviesas", ProceduralTextures.Ties("Traviesas", 31));
            var grating = Mat("Rejilla", new Color(0.2f, 0.21f, 0.21f));
            var fence = LoadMat("VallaHierro") ?? Mat("VallaHierro", new Color(0.07f, 0.07f, 0.08f));
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));

            // Pórticos.
            int bent = 0;
            for (float z = -292.5f; z <= 492.5f; z += TrainBentSpacing)
            {
                if (UnderCrossing(z)) continue;
                var root = new GameObject("Portico").transform;
                root.SetParent(group, false);
                root.localPosition = new Vector3(0f, 0f, z);
                foreach (float x in new[] { TrainColumnWestX, TrainColumnEastX })
                {
                    Box("Zapata", root, new Vector3(x, 0.2f, 0f), new Vector3(1f, 0.4f, 1f), concrete);
                    Box("Pilar", root, new Vector3(x, TrainColumnHeight / 2f, 0f), new Vector3(0.55f, TrainColumnHeight, 0.55f), bent % 3 == 1 ? rust : steel);
                    float inward = x < TrainAxisX ? 1f : -1f;
                    Strut("Cartela", root, new Vector3(x, TrainColumnHeight - 1.3f, 0f), new Vector3(x + inward * 1.6f, TrainColumnHeight, 0f), 0.18f, steel);
                }
                Box("Viga", root, new Vector3(TrainAxisX, TrainColumnHeight + 0.35f, 0f), new Vector3(TrainColumnEastX - TrainColumnWestX + 0.8f, 0.7f, 0.8f), steel, collider: false);
                bent++;
            }

            // Tablero continuo: vigas longitudinales, traviesas, raíles, pasarelas y barandillas.
            float length = TrainTrackEndZ - TrainTrackStartZ, midZ = (TrainTrackStartZ + TrainTrackEndZ) / 2f;
            float girderY = TrainColumnHeight + 0.7f + 0.5f;
            foreach (float tx in TrainTracksX)
            {
                foreach (float dx in new[] { -1f, 1f })
                    Box("VigaLongitudinal", group, new Vector3(tx + dx, girderY, midZ), new Vector3(0.35f, 1f, length), steel, collider: false);
                var tieBed = TexturedBox("Traviesas", group, new Vector3(tx, girderY + 0.6f, midZ), new Vector3(2.6f, 0.2f, length), ties,
                    new Vector2(2.6f, ProceduralTextures.TiesTile), collider: false);
                tieBed.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                foreach (float dx in new[] { -0.72f, 0.72f })
                    Box("Rail", group, new Vector3(tx + dx, TrainRailTop - 0.08f, midZ), new Vector3(0.08f, 0.16f, length), railMat, collider: false);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float fasciaX = TrainAxisX + side * 7.8f;
                Box("VigaBorde", group, new Vector3(fasciaX, girderY, midZ), new Vector3(0.3f, 1f, length), steel, collider: false);
                Box("Pasarela", group, new Vector3(TrainAxisX + side * 6.7f, girderY + 0.53f, midZ), new Vector3(2.2f, 0.06f, length), grating, collider: false);
                TexturedBox("Barandilla", group, new Vector3(TrainAxisX + side * 7.85f, girderY + 0.56f + 0.55f, midZ), new Vector3(0.03f, 1.1f, length),
                    fence, new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight), collider: false, sidesOnly: true);
            }

            BuildTrainStation(group, steel, concrete, fence);
            BuildTrains();
        }

        static bool UnderCrossing(float z)
        {
            foreach (var (cz, half) in TrainCrossings)
                if (Mathf.Abs(z - cz) < half + 1.5f) return true;
            return false;
        }

        // ---------- Estación ----------

        static void BuildTrainStation(Transform parent, Material steel, Material concrete, Material fence)
        {
            var root = new GameObject("Estacion").transform;
            root.SetParent(parent, false);
            var platformMat = Mat("AndenHormigon", new Color(0.55f, 0.54f, 0.51f));
            var edge = Mat("BordeAnden", new Color(0.85f, 0.7f, 0.15f));
            var roof = Mat("MarquesinaEstacion", new Color(0.3f, 0.32f, 0.31f));
            var glow = EmissiveMat("LuzAnden", new Color(1f, 0.92f, 0.75f), 1.5f);
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var sign = Mat("Letrero", new Color(0.1f, 0.25f, 0.6f));
            float len = StationZ1 - StationZ0, cz = StationCenterZ;

            foreach (float side in new[] { -1f, 1f })
            {
                // Andén lateral: el borde queda a 1.5 m del eje de la vía (los vagones miden 2.9 m de ancho).
                float trackX = side < 0f ? TrainTracksX[0] : TrainTracksX[1];
                float inner = trackX + side * 1.55f, outer = trackX + side * 5.4f;
                float px = (inner + outer) / 2f, pw = Mathf.Abs(outer - inner);
                Box("Anden", root, new Vector3(px, PlatformTop - 0.2f, cz), new Vector3(pw, 0.4f, len), platformMat);
                Box("BordeAmarillo", root, new Vector3(inner + side * 0.3f, PlatformTop + 0.005f, cz), new Vector3(0.6f, 0.01f, len), edge, collider: false);
                // Barandilla exterior, abierta junto al rellano de la escalera (z StationZ0 + 3.9 … + 6.1).
                foreach (var (z0, z1) in new[] { (StationZ0, StationZ0 + 3.9f), (StationZ0 + 6.1f, StationZ1) })
                    TexturedBox("BarandillaAnden", root, new Vector3(outer - side * 0.05f, PlatformTop + 0.55f, (z0 + z1) / 2f), new Vector3(0.03f, 1.1f, z1 - z0),
                        fence, new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight), sidesOnly: true);

                // Pilares propios del andén y marquesina.
                for (float z = StationZ0 + 2f; z <= StationZ1 - 2f; z += 8f)
                {
                    Box("PilarAnden", root, new Vector3(outer - side * 0.2f, (PlatformTop - 0.4f) / 2f, z), new Vector3(0.35f, PlatformTop - 0.4f, 0.35f), steel);
                    Box("PosteMarquesina", root, new Vector3(outer - side * 0.6f, PlatformTop + 1.5f, z), new Vector3(0.15f, 3f, 0.15f), steel, collider: false);
                }
                Box("Marquesina", root, new Vector3(px, PlatformTop + 3.05f, cz), new Vector3(pw + 0.4f, 0.15f, len * 0.7f), roof, collider: false);
                for (float z = cz - len * 0.3f; z <= cz + len * 0.3f; z += 9f)
                    Box("Fluorescente", root, new Vector3(px, PlatformTop + 2.9f, z), new Vector3(0.15f, 0.08f, 1.4f), glow, collider: false);

                for (int i = -1; i <= 1; i++)
                    Bench(root, new Vector3(outer - side * 0.8f, PlatformTop, cz + i * 12f), side < 0f ? 90f : -90f, wood, concrete);
                Box("Cartel", root, new Vector3(px, PlatformTop + 2.3f, cz + 6f), new Vector3(0.08f, 0.7f, 2.2f), sign, collider: false);
                TrashCan(root, new Vector3(outer - side * 0.6f, PlatformTop, cz + 18f), 0f, false);

                // Pared invisible en el borde de las vías: desde el andén no se puede bajar al tablero (no tiene colisión).
                ColliderBox(root, "LimiteVias", new Vector3(inner - side * 0.05f, PlatformTop + 1f, cz), new Vector3(0.1f, 2f, len));

                StationStairs(root, outer, side, steel, platformMat, fence);
            }
        }

        // Escalera: 1.4 m de ancho, pendiente de 32° (1.6 m de huella por metro de subida, por debajo del slopeLimit de
        // 45° del CharacterController). Bajo los peldaños hay una rampa invisible con colisión, que es lo que pisa el
        // jugador; los laterales tienen barandilla con colisión.
        const float StairWidth = 1.4f, StairRunPerRise = 1.6f;

        /// <summary>
        /// Escalera de acero hasta la acera, por fuera del andén y bajando hacia el norte, con rellano arriba (se entra
        /// por el hueco de la barandilla del andén, z StationZ0 + 3.9 … + 6.1). Se puede subir a pie.
        /// </summary>
        static void StationStairs(Transform root, float outer, float side, Material steel, Material platformMat, Material fence)
        {
            float stairX = outer + side * (StairWidth / 2f + 0.05f);
            float rise = PlatformTop - SidewalkTop;
            var top = new Vector3(stairX, PlatformTop, StationZ0 + 6f);
            var bottom = new Vector3(stairX, SidewalkTop, top.z + rise * StairRunPerRise);
            var along = (bottom - top).normalized;
            var normal = Vector3.Cross(along, Vector3.Cross(Vector3.up, along)).normalized; // perpendicular a la rampa, hacia arriba
            if (normal.y < 0f) normal = -normal;
            var tilt = Quaternion.LookRotation(along, normal);
            float length = Vector3.Distance(top, bottom);

            // Rellano con barandilla por fuera y por el sur.
            float edgeX = stairX + side * StairWidth / 2f;
            float landX0 = Mathf.Min(outer, edgeX), landX1 = Mathf.Max(outer, edgeX);
            var landing = new Vector3((landX0 + landX1) / 2f, PlatformTop - 0.2f, top.z - 1.1f);
            Box("Rellano", root, landing, new Vector3(landX1 - landX0, 0.4f, 2.2f), platformMat);
            float farX = stairX + side * (StairWidth / 2f - 0.02f);
            TexturedBox("BarandillaRellano", root, new Vector3(farX, PlatformTop + 0.55f, top.z - 1.1f), new Vector3(0.03f, 1.1f, 2.2f),
                fence, new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight), sidesOnly: true);
            TexturedBox("BarandillaRellano", root, new Vector3(landing.x, PlatformTop + 0.55f, top.z - 2.18f), new Vector3(landX1 - landX0, 1.1f, 0.03f),
                fence, new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight), sidesOnly: true);

            // Zanca de acero bajo los peldaños y rampa invisible que se pisa (su cara de arriba va por la mitad de los peldaños).
            Box("Zanca", root, (top + bottom) / 2f - normal * 0.25f, new Vector3(StairWidth, 0.3f, length), steel, collider: false).transform.localRotation = tilt;
            ColliderBox(root, "RampaEscalera", (top + bottom) / 2f - normal * 0.15f - Vector3.up * 0.1f, new Vector3(StairWidth, 0.3f, length + 0.1f)).transform.localRotation = tilt;

            // Peldaños: huella de 0.2 m de alto cada una, con la arista en la línea de la rampa.
            int steps = Mathf.RoundToInt(rise / 0.2f);
            float stepRise = rise / steps, stepRun = stepRise * StairRunPerRise;
            for (int k = 0; k < steps; k++)
            {
                float y = PlatformTop - (k + 1) * stepRise;
                float z = top.z + k * stepRun + stepRun / 2f;
                Box("Peldano", root, new Vector3(stairX, y + stepRise / 2f - 0.02f, z), new Vector3(StairWidth, stepRise - 0.04f, stepRun), platformMat, collider: false);
            }

            // Barandillas laterales (con colisión, para no caerse) y pasamanos.
            foreach (float dx in new[] { -1f, 1f })
            {
                var offset = new Vector3(dx * (StairWidth / 2f + 0.02f), 0f, 0f);
                var rail = TexturedBox("BarandillaEscalera", root, (top + bottom) / 2f + offset + Vector3.up * 0.55f, new Vector3(0.03f, 1.1f, length),
                    fence, new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight), collider: false, sidesOnly: true);
                rail.transform.localRotation = Quaternion.LookRotation(along, Vector3.up);
                StairSideWall(root, top + offset, bottom + offset, 1.4f);
                Strut("Pasamanos", root, top + offset + Vector3.up * 1.05f, bottom + offset + Vector3.up * 1.05f, 0.05f, steel, cylinder: true);
            }

            // Pies de la escalera a los lados (no en el paso).
            foreach (float dx in new[] { -1f, 1f })
            {
                var foot = Vector3.Lerp(top, bottom, 0.55f);
                float h = foot.y - SidewalkTop;
                Box("PieEscalera", root, new Vector3(stairX + dx * (StairWidth / 2f - 0.15f), SidewalkTop + h / 2f - 0.3f, foot.z), new Vector3(0.2f, h - 0.6f, 0.2f), steel);
            }
        }

        /// <summary>
        /// Pared invisible a un lado de un tramo de escalera recto (a lo largo de X o de Z), hecha de cajas verticales
        /// escalonadas de 0.3 m que siguen la pendiente y acaban justo en los extremos del tramo. Una caja inclinada no
        /// sirve: sus esquinas se meten en los rellanos y el jugador se queda enganchado al girar.
        /// </summary>
        static void StairSideWall(Transform root, Vector3 from, Vector3 to, float height)
        {
            var flat = to - from;
            flat.y = 0f;
            float run = flat.magnitude;
            var dir = flat / run;
            var across = Vector3.Cross(Vector3.up, dir);
            int n = Mathf.Max(1, Mathf.CeilToInt(run / 0.3f));
            for (int i = 0; i < n; i++)
            {
                var p0 = Vector3.Lerp(from, to, (float)i / n);
                var p1 = Vector3.Lerp(from, to, (float)(i + 1) / n);
                float yLow = Mathf.Min(p0.y, p1.y), yHigh = Mathf.Max(p0.y, p1.y) + height;
                var center = (p0 + p1) / 2f;
                center.y = (yLow + yHigh) / 2f;
                ColliderBox(root, "LimiteTramo", center, Abs(dir) * (run / n) + Abs(across) * 0.08f + Vector3.up * (yHigh - yLow));
            }
        }

        /// <summary>Caja solo de colisión (sin malla), para límites y rampas que no se ven.</summary>
        static GameObject ColliderBox(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        // ---------- Trenes ----------

        /// <summary>
        /// Dos trenes de 4 vagones (fuera de "Mundo", porque se mueven y no pueden ser estáticos).
        /// El del este va hacia el norte y el del oeste hacia el sur.
        /// </summary>
        static void BuildTrains()
        {
            var body = Mat("AceroTren", new Color(0.68f, 0.69f, 0.7f));
            body.SetFloat("_Metallic", 0.75f);
            body.SetFloat("_Smoothness", 0.55f);
            var dark = Mat("BajosTren", new Color(0.08f, 0.08f, 0.09f));
            var windows = EmissiveMat("VentanasTren", new Color(0.55f, 0.6f, 0.58f), 0.6f);
            var doors = Mat("PuertasTren", new Color(0.55f, 0.56f, 0.57f));
            var stripe = Mat("FranjaTren", new Color(0.45f, 0.08f, 0.1f));
            var headlight = EmissiveMat("FaroTren", new Color(1f, 0.95f, 0.8f), 3f);

            float northTrack = TrainTracksX[1], southTrack = TrainTracksX[0];
            var northStart = new Vector3(northTrack, TrainRailTop, TrainTrackStartZ);
            var northEnd = new Vector3(northTrack, TrainRailTop, TrainTrackEndZ);
            var southStart = new Vector3(southTrack, TrainRailTop, TrainTrackEndZ);
            var southEnd = new Vector3(southTrack, TrainRailTop, TrainTrackStartZ);

            var north = Train("TrenElevado_Norte", body, dark, windows, doors, stripe, headlight);
            north.AddComponent<ElevatedTrain>().Configure(northStart, northEnd, StationCenterZ - TrainTrackStartZ, 120f);
            var south = Train("TrenElevado_Sur", body, dark, windows, doors, stripe, headlight);
            south.AddComponent<ElevatedTrain>().Configure(southStart, southEnd, TrainTrackEndZ - StationCenterZ, 0f);
        }

        static GameObject Train(string name, Material body, Material dark, Material windows, Material doors, Material stripe, Material headlight, int cars = 4)
        {
            const float carLength = 14.6f, gap = 0.6f, w = 2.9f;
            var root = new GameObject(name);
            for (int i = 0; i < cars; i++)
            {
                float cz = (i - (cars - 1) / 2f) * (carLength + gap);
                var car = new GameObject($"Vagon_{i}").transform;
                car.SetParent(root.transform, false);
                car.localPosition = new Vector3(0f, 0f, cz);

                Box("Caja", car, new Vector3(0f, 2.4f, 0f), new Vector3(w, 2.9f, carLength), body, collider: false);
                Box("Techo", car, new Vector3(0f, 3.95f, 0f), new Vector3(w - 0.3f, 0.22f, carLength - 0.3f), body, collider: false);
                Box("Ventanas", car, new Vector3(0f, 2.75f, 0f), new Vector3(w + 0.02f, 0.85f, carLength - 1.4f), windows, collider: false);
                Box("Franja", car, new Vector3(0f, 1.6f, 0f), new Vector3(w + 0.02f, 0.12f, carLength - 0.2f), stripe, collider: false);
                foreach (float side in new[] { -1f, 1f })
                foreach (float dz in new[] { -3.6f, 3.6f })
                    Box("Puerta", car, new Vector3(side * (w / 2f + 0.015f), 2.05f, dz), new Vector3(0.03f, 2f, 1.3f), doors, collider: false);
                Box("Bajos", car, new Vector3(0f, 0.75f, 0f), new Vector3(w - 0.5f, 0.45f, carLength - 4f), dark, collider: false);
                foreach (float dz in new[] { -5f, 5f })
                    Box("Bogie", car, new Vector3(0f, 0.35f, dz), new Vector3(2.2f, 0.55f, 2.6f), dark, collider: false);

                // Testeros: cabina con luna y faros en los extremos del tren; fuelle entre vagones.
                foreach (float end in new[] { -1f, 1f })
                {
                    bool isHead = (i == 0 && end < 0f) || (i == cars - 1 && end > 0f);
                    float ez = end * (carLength / 2f + 0.01f);
                    if (isHead)
                    {
                        Box("Luna", car, new Vector3(0f, 2.75f, ez), new Vector3(w - 0.6f, 0.9f, 0.02f), windows, collider: false);
                        foreach (float dx in new[] { -0.9f, 0.9f })
                            Box("Faro", car, new Vector3(dx, 1.4f, ez), new Vector3(0.25f, 0.18f, 0.03f), headlight, collider: false);
                    }
                    else Box("Fuelle", car, new Vector3(0f, 2.3f, end * (carLength / 2f + gap / 2f)), new Vector3(2f, 2.4f, gap), dark, collider: false);
                }
            }
            return root;
        }
    }
}
