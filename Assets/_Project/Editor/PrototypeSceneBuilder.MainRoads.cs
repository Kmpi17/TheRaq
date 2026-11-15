using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Calles grandes alrededor de la manzana central: la norte y la sur (z = ±80) y la avenida y la del oeste
    /// (x = ±110), de 14 m y de borde a borde del mundo. Encima del "Suelo" liso van tramos con la textura
    /// <see cref="ProceduralTextures.MainRoad"/>: tramos normales, una llegada de 14 m a cada lado de cada cruce
    /// (carriles continuos, línea de stop en los carriles que llegan y paso de cebra) y el cuadrado del cruce.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float MainRoadTop = 0.01f;          // por debajo de las calles de barrio (RoadTop = 0.02), que tapan la boca
        const float MainRoadHalfLength = 300f;    // de borde a borde del mundo

        static float MainRoadZ => BlockHalfZ + SidewalkWidth + RoadWidth / 2f; // 80: calles norte y sur
        static float MainRoadX => BlockHalfX + SidewalkWidth + RoadWidth / 2f; // 110: avenida y calle del oeste

        /// <summary>
        /// Las cuatro calles grandes: si van a lo largo de X, su eje (z o x), dónde las cruzan las otras y dónde acaban
        /// (empiezan todas en −300). Las calles norte y sur acaban en la avenida: al este está el barrio de viviendas
        /// (z −88…88), así que esos dos cruces son en T.
        /// </summary>
        static IEnumerable<(string name, bool alongX, float center, float[] crossings, float end)> MainRoadsList()
        {
            float[] crossX = { -MainRoadX, MainRoadX }, crossZ = { -MainRoadZ, MainRoadZ };
            float avenueEast = MainRoadX + RoadWidth / 2f;
            yield return ("Norte", true, MainRoadZ, crossX, avenueEast);
            yield return ("Sur", true, -MainRoadZ, crossX, avenueEast);
            yield return ("Avenida", false, MainRoadX, crossZ, MainRoadHalfLength);
            yield return ("Oeste", false, -MainRoadX, crossZ, MainRoadHalfLength);
        }

        /// <summary>
        /// Bocas de las calles y entradas que dan a las calles grandes, ensanchadas 1.5 m: ahí no hay bordillo, así que no
        /// van sumideros ni nieve apartada.
        /// </summary>
        static List<Rect> MainRoadMouths()
        {
            var mouths = new List<Rect>();
            void Add(Rect r) => mouths.Add(Rect.MinMaxRect(r.xMin - 1.5f, r.yMin - 1.5f, r.xMax + 1.5f, r.yMax + 1.5f));
            foreach (var r in HousesRoads) { Add(r); Add(HousesWestRect(r)); }
            foreach (var r in WestExtRoads) Add(r);
            foreach (var r in NorthEastRoads) Add(r);
            foreach (var r in NorthWestRoads) Add(r);
            foreach (var r in FarWestRoads) Add(r);
            foreach (var r in GasDriveways) Add(r);
            foreach (var r in SchoolDriveways) Add(r);
            foreach (var r in TowersDriveways) Add(r);
            Add(ShopsLotDriveway);
            Add(Rect.MinMaxRect(DistrictMinX - SidewalkWidth, -DistrictRoadWidth / 2f, DistrictMaxX, DistrictRoadWidth / 2f)); // calle del barrio de viviendas
            return mouths;
        }

        /// <summary>Punto de una calle grande: <paramref name="t"/> a lo largo y <paramref name="across"/> desde el eje.</summary>
        static Vector3 MainRoadPoint(bool alongX, float center, float t, float across, float y) =>
            alongX ? new Vector3(t, y, center + across) : new Vector3(center + across, y, t);

        static void BuildMainRoads(Transform world)
        {
            var group = new GameObject("CallesGrandes").transform;
            group.SetParent(world, false);

            // Tres variantes del tramo normal, repartidas en trozos de 42 m, para que los parches no se repitan cada 14 m.
            var lanes = new Material[3];
            int[] laneSeeds = { 21, 24, 25 };
            for (int i = 0; i < lanes.Length; i++)
                lanes[i] = TexturedMat($"CalleGrande{i + 1}", ProceduralTextures.MainRoad($"CalleGrande{i + 1}", RoadWidth, laneSeeds[i], ProceduralTextures.MainRoadPaint.Lanes));
            var rng = new System.Random(707);
            var approach = TexturedMat("CalleGrandeLlegada", ProceduralTextures.MainRoad("CalleGrandeLlegada", RoadWidth, 22, ProceduralTextures.MainRoadPaint.Approach));
            var junction = TexturedMat("CalleGrandeCruce", ProceduralTextures.MainRoad("CalleGrandeCruce", RoadWidth, 23, ProceduralTextures.MainRoadPaint.None));
            float half = RoadWidth / 2f;

            foreach (var (name, alongX, center, crossings, end) in MainRoadsList())
            {
                float t = -MainRoadHalfLength;
                foreach (float k in crossings)
                {
                    float arrive = k - half - RoadWidth;
                    MainRoadStretch(group, $"Calle_{name}", alongX, center, t, arrive, lanes, rng);
                    MainRoadPiece(group, $"Llegada_{name}", alongX, center, arrive, k - half, 1f, approach);
                    t = k + half;
                    if (t >= end - 0.01f) break; // cruce en T: la calle acaba aquí
                    MainRoadPiece(group, $"Llegada_{name}", alongX, center, t, t + RoadWidth, -1f, approach);
                    t += RoadWidth;
                }
                MainRoadStretch(group, $"Calle_{name}", alongX, center, t, end, lanes, rng);
            }

            // Cuadrado de cada cruce, girado un poco al azar para que los parches no se repitan igual.
            int index = 0;
            foreach (float x in new[] { -MainRoadX, MainRoadX })
            foreach (float z in new[] { -MainRoadZ, MainRoadZ })
            {
                var square = TexturedBox("Cruce", group, new Vector3(x, MainRoadTop / 2f, z), new Vector3(RoadWidth, MainRoadTop, RoadWidth),
                    junction, Vector2.one * RoadWidth, collider: false);
                square.transform.localRotation = Quaternion.Euler(0f, 90f * index++, 0f);
            }
        }

        /// <summary>Tramo normal de t0 a t1 partido en trozos de 3 teselas, cada uno con una variante al azar (y a veces del revés).</summary>
        static void MainRoadStretch(Transform parent, string name, bool alongX, float center, float t0, float t1, Material[] variants, System.Random rng)
        {
            float chunk = RoadWidth * 3f;
            for (float t = t0; t < t1 - 0.01f; t += chunk)
            {
                float end = t1 - t < chunk * 1.5f ? t1 : t + chunk; // el último trozo se queda con el resto
                MainRoadPiece(parent, name, alongX, center, t, end, rng.Next(2) == 0 ? 1f : -1f, variants[rng.Next(variants.Length)]);
                if (end >= t1) break;
            }
        }

        /// <summary>
        /// Tramo de calle grande de t0 a t1. La V de la textura va hacia <paramref name="direction"/> (±1 a lo largo
        /// de la calle): en las llegadas apunta al cruce, para que el paso de cebra quede pegado a él.
        /// </summary>
        static void MainRoadPiece(Transform parent, string name, bool alongX, float center, float t0, float t1, float direction, Material material)
        {
            float length = t1 - t0;
            if (length <= 0.01f) return;
            var piece = TexturedBox(name, parent, MainRoadPoint(alongX, center, (t0 + t1) / 2f, 0f, MainRoadTop / 2f),
                new Vector3(RoadWidth, MainRoadTop, length), material, Vector2.one * RoadWidth, collider: false);
            // Sin girar, V va hacia +z; girada 90°, hacia +x. U queda a la derecha de quien va hacia V.
            float yaw = alongX ? (direction > 0f ? 90f : 270f) : (direction > 0f ? 0f : 180f);
            piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Tapas de alcantarilla, sumideros junto al bordillo, nieve sucia apartada contra el bordillo, charcos,
        /// manchas de aceite y suciedad en las calles grandes. Va después de los barrios: reutiliza sus materiales.
        /// </summary>
        static void BuildMainRoadDetails(Transform world)
        {
            var group = new GameObject("CallesGrandes_Detalle").transform;
            group.SetParent(world, false);

            var iron = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            var grate = Mat("RejillaSumidero", new Color(0.05f, 0.05f, 0.055f));
            var slush = Mat("NieveSucia", new Color(0.84f, 0.86f, 0.89f));
            var puddle = LoadMat("Charco");
            var oil = LoadMat("Aceite");
            var dirt = LoadMat("Suciedad");
            var rng = new System.Random(808);
            float half = RoadWidth / 2f;
            int layer = 0;
            float DecalY() => MainRoadTop + 0.003f + (layer++ % 40) * 0.0001f;
            var mouths = MainRoadMouths();
            // Las bocas se miran en el bordillo (a 1 m dentro de la acera), no en el punto de la calzada.
            bool AtMouth(bool alongX, float center, float t, float side) =>
                InsideAny(mouths, MainRoadPoint(alongX, center, t, side * (half + 1f), 0f));

            foreach (var (name, alongX, center, crossings, end) in MainRoadsList())
            {
                bool NearCrossing(float t, float margin)
                {
                    foreach (float k in crossings)
                        if (Mathf.Abs(t - k) < half + margin) return true;
                    return false;
                }
                float yaw = alongX ? 90f : 0f;

                // Tapas de alcantarilla en el centro de un carril, cada ~50 m, fuera de los cruces y de los pasos de cebra.
                for (float t = -MainRoadHalfLength + 20f; t < end - 10f; t += Range(rng, 40f, 60f))
                {
                    if (NearCrossing(t, RoadWidth + 2f)) continue;
                    float across = (rng.Next(2) == 0 ? -1f : 1f) * (rng.Next(2) == 0 ? 1.8f : 5.1f);
                    Primitive(PrimitiveType.Cylinder, "Alcantarilla", group, MainRoadPoint(alongX, center, t, across, MainRoadTop + 0.005f),
                        new Vector3(0.75f, 0.005f, 0.75f), iron, keepCollider: false);
                    Decal(group, MainRoadPoint(alongX, center, t, across, DecalY()), Range(rng, 0f, 360f), Vector2.one * 1.6f, dirt);
                }

                // Sumideros junto a los dos bordillos, con una mancha de suciedad alrededor.
                foreach (float side in new[] { 1f, -1f })
                    for (float t = -MainRoadHalfLength + 15f + Range(rng, 0f, 20f); t < end - 5f; t += Range(rng, 35f, 50f))
                    {
                        if (NearCrossing(t, 1f) || AtMouth(alongX, center, t, side)) continue;
                        var drain = Box("Sumidero", group, MainRoadPoint(alongX, center, t, side * (half - 0.3f), MainRoadTop + 0.006f),
                            new Vector3(0.5f, 0.012f, 0.9f), grate, collider: false);
                        drain.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        Decal(group, MainRoadPoint(alongX, center, t + Range(rng, -0.5f, 0.5f), side * (half - 0.7f), DecalY()), yaw,
                            new Vector2(1.4f, 2.4f), dirt);
                    }

                // Nieve sucia apartada contra los bordillos por las quitanieves.
                foreach (float side in new[] { 1f, -1f })
                    for (int i = 0; i < 26; i++)
                    {
                        float t = Range(rng, -MainRoadHalfLength + 5f, end - 5f);
                        if (NearCrossing(t, 3f) || AtMouth(alongX, center, t, side)) continue;
                        float width = Range(rng, 0.35f, 0.8f), height = Range(rng, 0.04f, 0.12f);
                        var bank = Box("NieveBordillo", group, MainRoadPoint(alongX, center, t, side * (half - width / 2f - 0.05f), MainRoadTop + height / 2f),
                            new Vector3(width, height, Range(rng, 1.5f, 6f)), slush, collider: false);
                        bank.transform.localRotation = Quaternion.Euler(0f, yaw + Range(rng, -2f, 2f), 0f);
                    }

                // Charcos junto al bordillo y manchas de aceite en los carriles (más en las llegadas a los cruces).
                for (int i = 0; i < 14; i++)
                {
                    float side = rng.Next(2) == 0 ? 1f : -1f;
                    float t = Range(rng, -MainRoadHalfLength + 5f, end - 5f);
                    Decal(group, MainRoadPoint(alongX, center, t, side * Range(rng, half - 1.6f, half - 0.6f), DecalY()), yaw + Range(rng, -20f, 20f),
                        new Vector2(Range(rng, 1f, 2f), Range(rng, 1.8f, 4f)), puddle);
                }
                for (int i = 0; i < 26; i++)
                {
                    float t;
                    if (i < 10)
                    {
                        float k = crossings[rng.Next(crossings.Length)];
                        float sign = k + half >= end - 0.01f || rng.Next(2) == 0 ? -1f : 1f;
                        t = k + sign * (half + RoadWidth - Range(rng, 3.5f, 9f)); // donde paran los coches
                    }
                    else t = Range(rng, -MainRoadHalfLength + 5f, end - 5f);
                    float across = (rng.Next(2) == 0 ? -1f : 1f) * (rng.Next(2) == 0 ? 1.8f : 5.1f) + Range(rng, -0.3f, 0.3f);
                    Decal(group, MainRoadPoint(alongX, center, t, across, DecalY()), Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.7f, 1.6f), oil);
                }
            }
        }
    }
}
