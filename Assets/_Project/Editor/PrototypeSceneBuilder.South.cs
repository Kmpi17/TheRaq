using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Polígono industrial del sur (z −90…−485, x −300…300): segunda franja de la ampliación a 1 km × 1 km. Las piezas
    /// están en SouthBuildings.cs. Trazado:
    /// - S JUDSON AVE (J) baja desde la calle sur hasta el muelle.
    /// - W GANTRY ST (eje z = −319) cruza de J al borde este; W DOCK ST (eje z = −475) es la calle del muelle.
    /// - S WHARF RD es la primera calle **curva** del mapa: baja recta desde Gantry (x −45…−35), gira un cuarto de
    ///   círculo de 45 m de radio alrededor de (5, −365) y sigue recta hacia el este hasta la avenida (z −415…−405).
    /// - Vía de mercancías a nivel (lecho z −466…−454, dos vías) de lado a lado, con pasos a nivel en J y en las calles
    ///   grandes y un apartadero que entra en la fábrica abandonada.
    /// - Al sur del muelle, el canal cierra el mundo: puente levadizo levantado en S CALDER AVE, puente fijo (cortado por
    ///   obras) en S MARLOW AVE con el elevado al lado en celosía, y la otra orilla con silos, grúas y depósitos.
    /// En coordenadas del mundo.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // ---------- Trazado ----------

        const float GantryZ = -319f;                                  // W GANTRY ST: calzada z −324…−314
        const float SouthDockZ = -475f;                               // W DOCK ST: calzada z −480…−470
        const float SouthFreightZ = -460f;                            // eje del lecho de la vía de mercancías
        const float FreightBedHalf = 6f;                              // lecho de z −466 a −454
        static readonly float[] FreightTracksZ = { -457.5f, -462.5f }; // vía norte y vía sur
        const float SouthEdgeX = 300f;                                // borde este provisional: calles cortadas
        const float CanalFarZ = -510f;                                // la otra orilla del canal
        const float CanalWater = -1.6f;                               // nivel del agua
        const float SidingZ = -434.5f;                                // apartadero de la fábrica, junto al muelle de carga

        // Tramos de las calles este–oeste: de J a la calle x = −110, entre las calles grandes y de la avenida al borde.
        static readonly (float x0, float x1)[] SouthSpans = { (-252f, -117f), (-103f, 103f), (117f, SouthEdgeX) };

        static readonly Rect SsJ = Rect.MinMaxRect(-262f, -480f, -252f, -87f);                 // J baja hasta el muelle
        static readonly Vector2 WharfCenter = new(5f, -365f);                                   // centro de la curva
        const float WharfRadius = 45f;                                                          // radio del eje de la curva
        const string WharfName = "S WHARF RD";
        static readonly Rect SsWharfWest = Rect.MinMaxRect(-45f, -365f, -35f, -324f);           // tramo recto norte–sur
        static readonly Rect SsWharfEast = Rect.MinMaxRect(5f, -415f, 103f, -405f);             // tramo recto este–oeste

        // Parcelas.
        static readonly Rect SouthDepot = Rect.MinMaxRect(-247f, -300f, -170f, -118f);           // cocheras de autobuses escolares
        static readonly Rect SouthVacant = Rect.MinMaxRect(-166f, -280f, -122f, -112f);          // solar con valla publicitaria
        static readonly Rect SouthAutoBody = Rect.MinMaxRect(-152f, -306f, -124f, -288f);        // taller de chapa y neumáticos
        static readonly Rect SouthScrap = Rect.MinMaxRect(-247f, -452f, -122f, -329f);           // desguace
        static readonly Rect SouthFactoryLot = Rect.MinMaxRect(-100.5f, -454f, -47.5f, -326.5f); // fábrica abandonada
        static readonly Rect SouthFactory = Rect.MinMaxRect(-97f, -430f, -52f, -334f);
        static readonly Rect SouthWarehouse = Rect.MinMaxRect(15f, -390f, 90f, -340f);          // almacén dentro de la curva
        static readonly Rect SouthPipeYard = Rect.MinMaxRect(25f, -451f, 97f, -421f);            // tubos y perfiles de acero
        static readonly Rect SouthColdStorage = Rect.MinMaxRect(140f, -420f, 240f, -345f);       // frigorífico
        static readonly Vector2 SaltPile = new(272f, -422f);                                    // montón de sal con cinta al muelle

        static Rect GantryRect(int span) => Rect.MinMaxRect(SouthSpans[span].x0, GantryZ - 5f, SouthSpans[span].x1, GantryZ + 5f);
        static Rect DockRect(int span) => Rect.MinMaxRect(SouthSpans[span].x0, SouthDockZ - 5f, SouthSpans[span].x1, SouthDockZ + 5f);
        static Rect FreightBand => Rect.MinMaxRect(-300f, SouthFreightZ - FreightBedHalf, 300f, SouthFreightZ + FreightBedHalf);

        // Cortes de las aceras de enfrente de las calles grandes (BuildStreets): solo las calzadas; la vía de mercancías
        // cruza las aceras a nivel.
        static Rect[] SouthBandAvenueCuts => new[]
        {
            Rect.MinMaxRect(-300f, GantryZ - 5f, 300f, GantryZ + 5f),
            Rect.MinMaxRect(-300f, SouthDockZ - 5f, 300f, SouthDockZ + 5f),
        };

        /// <summary>Calles con nombre del polígono (para NamedStreets): rectángulo, nombre y ancho de sus aceras.</summary>
        static IEnumerable<(Rect rect, string name, float sidewalk)> SouthBandStreets()
        {
            yield return (SsJ, "S JUDSON AVE", HousesSidewalk);
            for (int i = 0; i < SouthSpans.Length; i++)
            {
                yield return (GantryRect(i), "W GANTRY ST", HousesSidewalk);
                yield return (DockRect(i), "W DOCK ST", HousesSidewalk);
            }
            yield return (SsWharfWest, WharfName, HousesSidewalk);
            yield return (SsWharfEast, WharfName, HousesSidewalk);
        }

        /// <summary>Bocas del polígono en las calles grandes (MainRoadMouths) y la vía, donde no van sumideros ni nieve.</summary>
        static IEnumerable<Rect> SouthBandMouths()
        {
            for (int i = 0; i < SouthSpans.Length; i++)
            {
                yield return GantryRect(i);
                yield return DockRect(i);
            }
            yield return SsWharfEast;
            yield return SsJ;
            yield return FreightBand;
        }

        // ---------- Construcción ----------

        static void BuildSouthBand(Transform world)
        {
            var group = new GameObject("Poligono_Sur").transform;
            group.SetParent(world, false);

            BuildHousesPatch(group, "Calles", 4001, SouthBandRoads(), new Rect[0], SouthBandSidewalks(), SouthBandLawns(),
                new (string, Vector3, float, float, float, RowKind)[0], SouthBandLife);

            var rng = new System.Random(4003);
            BuildWharfCurve(group);
            BuildFreightLine(group, rng);
            BuildCanal(group, rng);
            BuildSouthSkyline(world);
            ExportHousesFootprints(); // vuelve a escribir mapa/edificios_casas.json con las naves del sur
        }

        static Rect[] SouthBandRoads()
        {
            var roads = new List<Rect> { SsJ, SsWharfWest, SsWharfEast };
            for (int i = 0; i < SouthSpans.Length; i++) { roads.Add(GantryRect(i)); roads.Add(DockRect(i)); }
            return roads.ToArray();
        }

        static Rect[] SouthBandSidewalks()
        {
            const float w = HousesSidewalk;
            var list = new List<Rect>
            {
                // Aceras de dentro de las calles grandes (las de fuera las pone BuildStreets), hasta el muelle.
                Rect.MinMaxRect(-103f, SouthQuayZ, -100.5f, -300f),
                Rect.MinMaxRect(100f, SouthQuayZ, 103f, -300f),
                // J: la del oeste llega al borde del muelle; la del este, a Dock.
                Rect.MinMaxRect(-262f - w, SouthQuayZ, -262f, -90f),
                Rect.MinMaxRect(-252f, SouthDockZ + 5f, -252f + w, -90f),
                // S WHARF RD (tramos rectos; la curva va aparte, BuildWharfCurve).
                Rect.MinMaxRect(-45f - w, -365f, -45f, GantryZ - 5f - w),
                Rect.MinMaxRect(-35f, -365f, -35f + w, GantryZ - 5f - w),
                Rect.MinMaxRect(5f, -405f, 100f, -405f + w),
                Rect.MinMaxRect(5f, -415f - w, 100f, -415f),
            };
            // Gantry y Dock: dos aceras por tramo, sin pisar las de las calles grandes. Al sur de Dock, el paseo del muelle.
            var spanX = new[] { (-252f + w, -120f), (-100.5f, 100f), (120f, SouthEdgeX) };
            foreach (var (x0, x1) in spanX)
            {
                list.Add(Rect.MinMaxRect(x0, GantryZ + 5f, x1, GantryZ + 5f + w));
                list.Add(Rect.MinMaxRect(x0, GantryZ - 5f - w, x1, GantryZ - 5f));
                list.Add(Rect.MinMaxRect(x0, SouthDockZ + 5f, x1, SouthDockZ + 5f + w));
                list.Add(Rect.MinMaxRect(x0 == -252f + w ? -262f : x0, SouthQuayZ, x1, SouthDockZ - 5f));
            }
            return list.ToArray();
        }

        static Rect[] SouthBandLawns() => new[]
        {
            Rect.MinMaxRect(HighwayMaxX, -454f, -264.5f, -90f),    // al oeste de J (vallas publicitarias hacia la autopista)
            Rect.MinMaxRect(HighwayMaxX, SouthQuayZ, -264.5f, -466f),
            Rect.MinMaxRect(-249.5f, -112f, -122f, -90f),          // detrás de los two-flats de la calle sur
            SouthVacant,
            Rect.MinMaxRect(-100.5f, -311.5f, 100f, -296.5f),      // entre la valla de las cocheras y Gantry
            Rect.MinMaxRect(120f, -311.5f, SouthEdgeX, -108f),     // descampado del sureste (fase 3: el este)
            Rect.MinMaxRect(196f, -108f, SouthEdgeX, -90f),
        };

        // ---------- Vida de calle ----------

        static void SouthBandLife(Transform parent, System.Random rng)
        {
            // Los two-flats de la calle sur (BuildSurroundings) no los conoce el parche: que no les caiga nieve encima.
            housesOccupied.Add(Rect.MinMaxRect(-206f, -107f, -121f, -91f));
            housesOccupied.Add(Rect.MinMaxRect(119f, -107f, 196f, -91f));

            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            // Farolas: pocas y espaciadas (es un polígono), alternando aceras.
            float gN = GantryZ + 5f + 0.6f, gS = GantryZ - 5f - 0.6f;
            foreach (float x in new[] { -235f, -160f, -70f, 40f, 160f, 250f }) StreetLight(group, new Vector3(x, 0f, gN), Vector3.back, pole, lamp);
            foreach (float x in new[] { -200f, -135f, -10f, 75f, 205f, 285f }) StreetLight(group, new Vector3(x, 0f, gS), Vector3.forward, pole, lamp);
            float dN = SouthDockZ + 5f + 0.6f, dS = SouthDockZ - 5f - 0.6f;
            foreach (float x in new[] { -225f, -150f, -60f, 30f, 150f, 230f }) StreetLight(group, new Vector3(x, 0f, dN), Vector3.back, pole, lamp);
            foreach (float x in new[] { -190f, -20f, 70f, 190f, 270f }) StreetLight(group, new Vector3(x, 0f, dS), Vector3.forward, pole, lamp);
            foreach (float z in new[] { -130f, -200f, -270f, -360f, -420f }) StreetLight(group, new Vector3(-251.4f, 0f, z), Vector3.left, pole, lamp);
            foreach (float z in new[] { -165f, -235f, -395f }) StreetLight(group, new Vector3(-262.6f, 0f, z), Vector3.right, pole, lamp);
            StreetLight(group, new Vector3(-45.6f, 0f, -345f), Vector3.right, pole, lamp);
            StreetLight(group, new Vector3(30f, 0f, -404.4f), Vector3.back, pole, lamp);
            StreetLight(group, new Vector3(75f, 0f, -415.6f), Vector3.forward, pole, lamp);

            // Postes de madera con cables por la acera norte de Gantry y la de J.
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(-248f, SidewalkTop, GantryZ + 7.2f), new Vector3(-122f, SidewalkTop, GantryZ + 7.2f), 30f), wood, cable);
            PoleLine(group, Points(new Vector3(-98f, SidewalkTop, GantryZ + 7.2f), new Vector3(98f, SidewalkTop, GantryZ + 7.2f), 32f), wood, cable);
            PoleLine(group, Points(new Vector3(122f, SidewalkTop, GantryZ + 7.2f), new Vector3(296f, SidewalkTop, GantryZ + 7.2f), 32f), wood, cable);
            PoleLine(group, Points(new Vector3(-263.8f, SidewalkTop, -100f), new Vector3(-263.8f, SidewalkTop, -448f), 31f), wood, cable);

            // Coches aparcados (pocos) y camiones.
            // ParkedCar: yaw 0 = morro hacia +X; se aparca en el sentido de la marcha (por la derecha).
            ParkedCar(group, new Vector3(-180f, RoadTop, GantryZ + 3.8f), 180f, rng);
            ParkedCar(group, new Vector3(-140f, RoadTop, GantryZ - 3.8f), 0f, rng);
            ParkedCar(group, new Vector3(-20f, RoadTop, GantryZ + 3.8f), 180f, rng);
            ParkedCar(group, new Vector3(180f, RoadTop, GantryZ - 3.8f), 0f, rng);
            ParkedCar(group, new Vector3(-253.8f, RoadTop, -210f), -90f, rng);
            ParkedCar(group, new Vector3(60f, RoadTop, SouthDockZ - 3.8f), 0f, rng);
            ParkedCar(group, new Vector3(-43.8f, RoadTop, -350f), 90f, rng);
            Semi(group, new Vector3(220f, RoadTop, GantryZ + 3.6f), 270f, true, rng);
            Semi(group, new Vector3(-80f, RoadTop, SouthDockZ + 3.6f), 270f, false, rng);

            // Calles cortadas en el borde este.
            RoadClosed(group, SouthEdgeX - 1f, GantryZ - 5f, GantryZ + 5f);
            RoadClosed(group, SouthEdgeX - 1f, SouthDockZ - 5f, SouthDockZ + 5f);

            BusDepot(group, rng);
            AutoBodyShop(group, rng);
            SouthVacantLot(group, rng);
            Scrapyard(group, rng);
            WestOfJudson(group, rng);
            AbandonedFactory(group, rng);
            Warehouse(group, rng);
            PipeYard(group, rng);
            ColdStorage(group, rng);
            SouthEastLot(group, rng);
        }
    }
}
