using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Barrio norte (z 298…500, x −300…300): primera franja de la ampliación del mapa a 1 km × 1 km. Las calles cambian
    /// de forma respecto al resto del mapa:
    /// - Bulevar W KINGSLEY BLVD (eje z = 395): dos calzadas de un sentido de 8 m y una mediana de 6 m con árboles y
    ///   farolas dobles, de la calle J al borde este. Cruza las calles grandes con semáforos.
    /// - Diagonal S HALDANE AVE (eje x + z = 421): de la avenida al bulevar, a 45°. Deja parcelas en cuña, casas giradas y
    ///   un edificio en cuña (flatiron) en la punta que da a la avenida.
    /// - Calle sin salida S MERRIN CT con rotonda (bulb) e isleta al fondo.
    /// - J, G y la nueva S WEBLEY AVE siguen rectas hacia el norte; W PRESCOTT ST es la calle del borde.
    /// Al norte, el terraplén del tren de mercancías cierra el mundo: muro de 5.8 m con grafitis y dos pasos inferiores
    /// oscuros donde siguen las calles grandes; detrás, siluetas de ciudad que se pierden en la niebla.
    /// Hay locales sueltos, un mini centro comercial (KINGSLEY PLAZA) con aparcamiento delante, un bloque con patio, una
    /// torre suelta, una iglesia con campanario, un parque de barrio y vallas publicitarias. En coordenadas del mundo.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // ---------- Trazado ----------

        const float BoulevardZ = 395f;                                     // eje del bulevar
        const float BoulevardLane = 8f, BoulevardMedian = 6f;               // calzadas de un sentido y mediana
        const float BoulevardHalf = BoulevardLane + BoulevardMedian / 2f;  // 11: de z 384 a 406
        const string BoulevardName = "W KINGSLEY BLVD";
        const float PrescottZ0 = 486f, PrescottZ1 = 496f;                  // W PRESCOTT ST, la calle del borde
        const float NorthWallZ = 499.5f;                                   // cara del muro del terraplén (borde del mundo)
        const float EmbankmentTop = 5.8f;                                  // por debajo del gálibo del elevado (6.6)
        const float NorthEdgeX = 300f;                                     // borde este provisional: el bulevar y Prescott acaban en barreras

        // Tramos de las calles este–oeste: de J a la calle x = −110, entre las calles grandes y de la avenida al borde.
        static readonly (float x0, float x1)[] NorthSpans = { (-252f, -117f), (-103f, 103f), (117f, NorthEdgeX) };

        // Diagonal: eje x + z = DiagC. DiagLine(a) es la paralela a "a" metros hacia el NE (negativo: hacia el SO).
        const float DiagC = 421f, DiagHalf = 5f;
        const string DiagName = "S HALDANE AVE";
        static float DiagLine(float across) => DiagC + across * 1.41421356f;

        static readonly Rect NbJ = Rect.MinMaxRect(-262f, 298f, -252f, PrescottZ1);                 // J sigue hasta el borde
        static readonly Rect NbGSouth = Rect.MinMaxRect(207f, 298f, 217f, 384f);                     // G, del borde viejo al bulevar
        static readonly Rect NbGNorth = Rect.MinMaxRect(207f, 406f, 217f, PrescottZ0);               // y del bulevar a Prescott
        static readonly Rect NbWebleySouth = Rect.MinMaxRect(-45f, 298f, -35f, 384f);                // S WEBLEY AVE
        static readonly Rect NbWebleyNorth = Rect.MinMaxRect(-45f, 406f, -35f, PrescottZ0);
        static readonly Rect NbCourt = Rect.MinMaxRect(155f, 446f, 165f, PrescottZ0);                // S MERRIN CT (se mete en la rotonda)
        static readonly Vector2 NbBulb = new(160f, 440f);
        const float NbBulbRadius = 12f;
        static readonly Rect NbMallLot = Rect.MinMaxRect(-32.5f, 409f, 100f, 483.5f);                // KINGSLEY PLAZA
        static readonly Rect NbMallDriveway = Rect.MinMaxRect(40f, 406f, 47f, 409f);                 // entrada desde el bulevar
        static readonly Rect NbMallAvenueDriveway = Rect.MinMaxRect(100f, 436f, 103f, 443f);         // entrada desde la avenida
        static readonly Rect NbMallBackDriveway = Rect.MinMaxRect(-35f, 478.5f, -32.5f, 483.5f);     // patio de carga, desde Webley
        static readonly Rect NbTowerLot = Rect.MinMaxRect(225f, 302f, 290f, 320f);                   // aparcamiento de la torre suelta
        static readonly Rect NbTowerDriveway = Rect.MinMaxRect(217f, 307f, 225f, 314f);

        static Rect BoulevardLaneRect(int span, bool north) => north
            ? Rect.MinMaxRect(NorthSpans[span].x0, BoulevardZ + BoulevardMedian / 2f, NorthSpans[span].x1, BoulevardZ + BoulevardHalf)
            : Rect.MinMaxRect(NorthSpans[span].x0, BoulevardZ - BoulevardHalf, NorthSpans[span].x1, BoulevardZ - BoulevardMedian / 2f);
        static Rect BoulevardRect(int span) => Rect.MinMaxRect(NorthSpans[span].x0, BoulevardZ - BoulevardHalf, NorthSpans[span].x1, BoulevardZ + BoulevardHalf);
        static Rect PrescottRect(int span) => Rect.MinMaxRect(NorthSpans[span].x0, PrescottZ0, NorthSpans[span].x1, PrescottZ1);

        // Cortes de las aceras de las calles grandes (BuildStreets): la del este de la avenida y la del oeste de x = −110.
        static Rect[] NorthBandAvenueCutsEast => new[] { BoulevardRect(2), PrescottRect(2) };
        static Rect[] NorthBandAvenueCutsWest => new[] { BoulevardRect(0), PrescottRect(0) };

        /// <summary>Calles con nombre del barrio norte (para NamedStreets): rectángulo, nombre y ancho de sus aceras.</summary>
        static IEnumerable<(Rect rect, string name, float sidewalk)> NorthBandStreets()
        {
            yield return (NbJ, "S JUDSON AVE", HousesSidewalk);
            yield return (NbGSouth, "S TENNEY AVE", HousesSidewalk);
            yield return (NbGNorth, "S TENNEY AVE", HousesSidewalk);
            yield return (NbWebleySouth, "S WEBLEY AVE", HousesSidewalk);
            yield return (NbWebleyNorth, "S WEBLEY AVE", HousesSidewalk);
            yield return (NbCourt, "S MERRIN CT", HousesSidewalk);
            for (int i = 0; i < NorthSpans.Length; i++)
            {
                yield return (PrescottRect(i), "W PRESCOTT ST", HousesSidewalk);
                yield return (BoulevardLaneRect(i, false), BoulevardName, SidewalkWidth);
                yield return (BoulevardLaneRect(i, true), BoulevardName, SidewalkWidth);
            }
        }

        /// <summary>Bocas del barrio norte en las calles grandes (MainRoadMouths): sin sumideros ni nieve apartada.</summary>
        static IEnumerable<Rect> NorthBandMouths()
        {
            for (int i = 0; i < NorthSpans.Length; i++)
            {
                yield return BoulevardRect(i);
                yield return PrescottRect(i);
            }
            yield return NbMallAvenueDriveway;
            yield return Rect.MinMaxRect(100f, DiagLine(-DiagHalf) - 103f, 103f, DiagLine(DiagHalf) - 100f); // boca de la diagonal
        }

        static Vector2 V(float x, float z) => new(x, z);

        // ---------- Construcción ----------

        static void BuildNorthBand(Transform world)
        {
            var group = new GameObject("Barrio_Norte").transform;
            group.SetParent(world, false);

            BuildHousesPatch(group, "Oeste", 3001, NorthWestBandRoads(), NorthWestBandAlleys(), NorthWestBandSidewalks(), NorthWestBandLawns(),
                NorthWestBandRows, NorthWestBandLife);
            BuildHousesPatch(group, "Centro", 3003, NorthCenterBandRoads(), NorthCenterBandAlleys(), NorthCenterBandSidewalks(), NorthCenterBandLawns(),
                NorthCenterBandRows, NorthCenterBandLife);
            BuildHousesPatch(group, "Este", 3005, NorthEastBandRoads(), NorthEastBandAlleys(), NorthEastBandSidewalks(), NorthEastBandLawns(),
                NorthEastBandRows, NorthEastBandLife);

            var rng = new System.Random(3007);
            BuildBoulevard(group, rng);
            BuildEmbankment(group, rng);
            BuildNorthSkyline(world);
            ExportHousesFootprints(); // vuelve a escribir mapa/edificios_casas.json con las casas del norte
        }

        // ---------- Oeste (de J a la calle x = −110) ----------

        static Rect[] NorthWestBandRoads() => new[] { NbJ, PrescottRect(0) };

        static Rect[] NorthWestBandAlleys() => new[]
        {
            Rect.MinMaxRect(-252f, 338f, -120f, 343f), // entre H y el bulevar
            Rect.MinMaxRect(-252f, 444f, -120f, 449f), // entre el bulevar y Prescott
        };

        static Rect[] NorthWestBandSidewalks() => new[]
        {
            Rect.MinMaxRect(-249.5f, 298f, -120f, 300.5f),   // H norte
            Rect.MinMaxRect(-264.5f, 298f, -262f, 498.5f),   // J oeste
            Rect.MinMaxRect(-252f, 298f, -249.5f, 384f),     // J este (se corta en el bulevar)
            Rect.MinMaxRect(-252f, 406f, -249.5f, PrescottZ0),
            Rect.MinMaxRect(-249.5f, 381f, -120f, 384f),     // bulevar sur
            Rect.MinMaxRect(-249.5f, 406f, -120f, 409f),     // bulevar norte
            Rect.MinMaxRect(-249.5f, 483.5f, -120f, PrescottZ0), // Prescott sur
            Rect.MinMaxRect(-264.5f, PrescottZ1, -120f, 498.5f), // Prescott norte (pasa por la cabecera de J)
        };

        static Rect[] NorthWestBandLawns() => new[]
        {
            Rect.MinMaxRect(-300f, 298f, -264.5f, NorthWallZ), // al oeste de J, hasta el borde
            Rect.MinMaxRect(-249.5f, 300.5f, -120f, 381f),
            Rect.MinMaxRect(-249.5f, 409f, -120f, 483.5f),
        };

        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] NorthWestBandRows =
        {
            // Al oeste de J, mirando a J (yaw 270: la parcela hacia −X, el largo por Z), hasta el borde del mundo.
            ("J_O_1", new Vector3(-264.5f, 0f, 326f), 270f, 52f, 34f, RowKind.Houses),         // z 300–352
            ("J_O_Solar", new Vector3(-264.5f, 0f, 359f), 270f, 14f, 34f, RowKind.Overgrown),  // z 352–366
            ("J_O_2", new Vector3(-264.5f, 0f, 402f), 270f, 72f, 34f, RowKind.Houses),         // z 366–438
            ("J_O_Apart", new Vector3(-264.5f, 0f, 456f), 270f, 32f, 34f, RowKind.Apartments), // z 440–472
            ("J_O_3", new Vector3(-264.5f, 0f, 485f), 270f, 24f, 34f, RowKind.Houses),         // z 473–497
            // Norte de H, de espaldas al callejón (la esquina con x = −110 es la tienda de licores).
            ("H_Norte_O", new Vector3(-224.75f, 0f, 300.5f), 0f, 49.5f, 37.5f, RowKind.Houses),       // x −249.5…−200
            ("H_Norte_Solar", new Vector3(-193f, 0f, 300.5f), 0f, 14f, 37.5f, RowKind.Overgrown),     // x −200…−186
            ("H_Norte_Apart", new Vector3(-168f, 0f, 300.5f), 0f, 36f, 37.5f, RowKind.Apartments),    // x −186…−150
            ("H_Norte_E", new Vector3(-143.5f, 0f, 300.5f), 0f, 13f, 37.5f, RowKind.Houses),          // x −150…−137
            // Sur del bulevar (la esquina con x = −110 es el bloque con patio).
            ("Bulevar_S_O", new Vector3(-227.25f, 0f, 381f), 180f, 44.5f, 38f, RowKind.Houses),       // x −249.5…−205
            ("Bulevar_S_Solar", new Vector3(-197.5f, 0f, 381f), 180f, 15f, 38f, RowKind.Vacant),      // x −205…−190
            ("Bulevar_S_E", new Vector3(-180f, 0f, 381f), 180f, 20f, 38f, RowKind.Houses),            // x −190…−170
            // Norte del bulevar (la esquina con J es la tienda de belleza).
            ("Bulevar_N_O", new Vector3(-210f, 0f, 409f), 0f, 50f, 35f, RowKind.Houses),              // x −235…−185
            ("Bulevar_N_Solar", new Vector3(-178.5f, 0f, 409f), 0f, 13f, 35f, RowKind.Vacant),        // x −185…−172
            ("Bulevar_N_Apart", new Vector3(-146f, 0f, 409f), 0f, 52f, 35f, RowKind.Apartments),      // x −172…−120
            // Sur de Prescott (la esquina con x = −110 es el cambio de cheques).
            ("Prescott_S_O", new Vector3(-219.75f, 0f, 483.5f), 180f, 59.5f, 34.5f, RowKind.Houses),     // x −249.5…−190
            ("Prescott_S_Apart", new Vector3(-170f, 0f, 483.5f), 180f, 40f, 34.5f, RowKind.Apartments),  // x −190…−150
            ("Prescott_S_E", new Vector3(-143.5f, 0f, 483.5f), 180f, 13f, 34.5f, RowKind.Houses),        // x −150…−137
        };

        static void NorthWestBandLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { -230f, -180f, -140f }) StreetLight(group, new Vector3(x, 0f, 299.6f), Vector3.back, pole, lamp);
            foreach (float z in new[] { 330f, 440f }) StreetLight(group, new Vector3(-250.4f, 0f, z), Vector3.left, pole, lamp);
            foreach (float z in new[] { 360f, 470f }) StreetLight(group, new Vector3(-263.6f, 0f, z), Vector3.right, pole, lamp);
            foreach (float x in new[] { -230f, -175f }) StreetLight(group, new Vector3(x, 0f, 383.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { -205f, -150f }) StreetLight(group, new Vector3(x, 0f, 406.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { -225f, -165f }) StreetLight(group, new Vector3(x, 0f, 484.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { -195f, -135f }) StreetLight(group, new Vector3(x, 0f, 497.6f), Vector3.back, pole, lamp);

            // Locales sueltos en las esquinas.
            NorthShop(group, ShopKind.Market, V(-127.5f, 301.5f), Vector2.down, 13f, 1, rng);  // H con x = −110
            NorthShop(group, ShopKind.Beauty, V(-243f, 410f), Vector2.down, 10f, 1, rng);      // bulevar con J
            NorthShop(group, ShopKind.Checks, V(-128.5f, 482.5f), Vector2.up, 13f, 2, rng);    // Prescott con x = −110

            CourtyardBlock(group, rng);

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(-248f, AlleyTop, 338.6f), new Vector3(-122f, AlleyTop, 338.6f), 26f), wood, cable);
            PoleLine(group, Points(new Vector3(-248f, AlleyTop, 448.4f), new Vector3(-122f, AlleyTop, 448.4f), 26f), wood, cable);

            ParkedCar(group, new Vector3(-230f, RoadTop, PrescottZ0 + 1.2f), 0f, rng);
            ParkedCar(group, new Vector3(-180f, RoadTop, PrescottZ1 - 1.2f), 180f, rng);
            ParkedCar(group, new Vector3(-260.8f, RoadTop, 350f), 90f, rng);
            ParkedCar(group, new Vector3(-253.2f, RoadTop, 455f), 270f, rng);
            ParkedCar(group, new Vector3(-200f, RoadTop, 385.2f), 0f, rng);
            ParkedCar(group, new Vector3(-160f, RoadTop, 404.8f), 180f, rng);

            // Valla de madera en el borde oeste del mundo (sigue la del oeste lejano).
            WoodFenceLine(group, new Vector3(-299.8f, LawnTop, 298f), new Vector3(-299.8f, LawnTop, NorthWallZ - 0.2f));
        }

        // ---------- Centro (entre las calles grandes) ----------

        static Rect[] NorthCenterBandRoads() => new[] { NbWebleySouth, NbWebleyNorth, PrescottRect(1), NbMallDriveway, NbMallBackDriveway };

        static Rect[] NorthCenterBandAlleys() => new[]
        {
            Rect.MinMaxRect(-103f, 338f, -45f, 343f), // de x = −110 a Webley
            Rect.MinMaxRect(-35f, 338f, 22f, 343f),   // de Webley hacia la diagonal (sin salida)
        };

        static Rect[] NorthCenterBandSidewalks()
        {
            float l1 = DiagLine(-DiagHalf), l2 = DiagLine(DiagHalf);
            return new[]
            {
                Rect.MinMaxRect(-103f, 298f, -100.5f, 384f),    // lado este de x = −110 (se corta en el bulevar y Prescott)
                Rect.MinMaxRect(-103f, 406f, -100.5f, PrescottZ0),
                Rect.MinMaxRect(-103f, PrescottZ1, -100.5f, NorthWallZ),
                Rect.MinMaxRect(-100.5f, 298f, 100f, 300.5f),   // H norte
                Rect.MinMaxRect(-47.5f, 300.5f, -45f, 381f),    // Webley
                Rect.MinMaxRect(-35f, 300.5f, -32.5f, 381f),
                Rect.MinMaxRect(-47.5f, 409f, -45f, 483.5f),
                Rect.MinMaxRect(-35f, 409f, -32.5f, 483.5f),
                Rect.MinMaxRect(-100.5f, 381f, l1 - 384f, 384f), // bulevar sur, cortada en la boca de la diagonal
                Rect.MinMaxRect(l2 - 381f, 381f, 100f, 384f),
                Rect.MinMaxRect(-100.5f, 406f, 100f, 409f),     // bulevar norte (la corta la entrada del centro comercial)
                Rect.MinMaxRect(-100.5f, 483.5f, 100f, PrescottZ0),
                Rect.MinMaxRect(-100.5f, PrescottZ1, 100f, 498.5f),
            };
        }

        static Rect[] NorthCenterBandLawns() => new[]
        {
            Rect.MinMaxRect(-100.5f, 300.5f, -47.5f, 381f),
            Rect.MinMaxRect(-32.5f, 300.5f, DiagLine(-DiagHalf - HousesSidewalk) - 381f, 381f), // hasta la acera de la diagonal
            Rect.MinMaxRect(-100.5f, 409f, -47.5f, 483.5f), // parque
        };

        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] NorthCenterBandRows =
        {
            ("H_Norte_C_O", new Vector3(-74f, 0f, 300.5f), 0f, 53f, 37.5f, RowKind.Houses),          // x −100.5…−47.5
            ("H_Norte_C", new Vector3(-13.75f, 0f, 300.5f), 0f, 37.5f, 37.5f, RowKind.Houses),       // x −32.5…5
            ("H_Norte_C_Apart", new Vector3(17.5f, 0f, 300.5f), 0f, 25f, 37.5f, RowKind.Apartments), // x 5–30
            ("H_Norte_C_Solar", new Vector3(66f, 0f, 300.5f), 0f, 12f, 8.5f, RowKind.Vacant),        // entre los locales
            ("Bulevar_S_C_O", new Vector3(-74f, 0f, 381f), 180f, 53f, 38f, RowKind.Apartments),      // x −100.5…−47.5
            ("Bulevar_S_C", new Vector3(-6.25f, 0f, 381f), 180f, 52.5f, 38f, RowKind.Houses),        // x −32.5…20
            // Casas giradas 45° mirando a la diagonal (yaw 225: la parcela hacia el SO, el largo hacia el NO).
            ("Diagonal_SO", new Vector3(60f, 0f, 350.39f), 225f, 56.57f, 18f, RowKind.Houses),
        };

        static void NorthCenterBandLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { -80f, -20f, 45f, 80f }) StreetLight(group, new Vector3(x, 0f, 299.6f), Vector3.back, pole, lamp);
            foreach (float z in new[] { 320f, 362f, 430f, 470f }) StreetLight(group, new Vector3(-46.4f, 0f, z), Vector3.right, pole, lamp);
            foreach (float x in new[] { -80f, -10f, 70f }) StreetLight(group, new Vector3(x, 0f, 383.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { -85f, -60f, 0f, 80f }) StreetLight(group, new Vector3(x, 0f, 406.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { -80f, -15f, 60f }) StreetLight(group, new Vector3(x, 0f, 484.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { -50f, 30f, 90f }) StreetLight(group, new Vector3(x, 0f, 497.6f), Vector3.back, pole, lamp);
            // En la diagonal, sobre la acera SO (el brazo apunta a la calzada lo más recto que deja la farola).
            float lightLine = DiagLine(-DiagHalf - 0.9f);
            foreach (float x in new[] { 85f, 62f, 40f }) StreetLight(group, new Vector3(x, 0f, lightLine - x), Vector3.right, pole, lamp);

            // Locales sueltos al norte de H, con huecos entre ellos; el de la esquina da a la avenida y a la diagonal.
            NorthShop(group, ShopKind.Pizza, V(38f, 301.5f), Vector2.down, 10f, 2, rng);
            NorthShop(group, ShopKind.Phones, V(54.5f, 301.5f), Vector2.down, 9f, 1, rng);
            NorthShop(group, ShopKind.Wings, V(92.5f, 301.5f), Vector2.down, 13f, 1, rng);

            BuildDiagonal(group);
            BuildFlatiron(group, rng);
            BuildNorthPark(group, rng);
            BuildStripMall(group, rng);

            // Acera oeste de la avenida (sigue la de Shops.cs), cortada en el bulevar, la diagonal, la entrada y Prescott.
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var cuts = new List<Rect>
            {
                BoulevardRect(1), PrescottRect(1), NbMallAvenueDriveway,
                Rect.MinMaxRect(100f, DiagLine(-DiagHalf) - 103f, 103f, DiagLine(DiagHalf) - 100f),
            };
            foreach (var piece in StripMinus(Rect.MinMaxRect(100f, 300f, 103f, NorthWallZ), cuts))
                Box("AceraAvenida_O", group, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height), concrete);

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(-99f, AlleyTop, 338.6f), new Vector3(-48f, AlleyTop, 338.6f), 25f), wood, cable);
            PoleLine(group, Points(new Vector3(-33f, AlleyTop, 338.6f), new Vector3(20f, AlleyTop, 338.6f), 26f), wood, cable);

            ParkedCar(group, new Vector3(-43.8f, RoadTop, 330f), 90f, rng);
            ParkedCar(group, new Vector3(-36.2f, RoadTop, 455f), 270f, rng);
            ParkedCar(group, new Vector3(-70f, RoadTop, PrescottZ0 + 1.2f), 0f, rng);
            ParkedCar(group, new Vector3(20f, RoadTop, PrescottZ1 - 1.2f), 180f, rng);
            ParkedCar(group, new Vector3(-60f, RoadTop, 385.2f), 0f, rng);
            ParkedCar(group, new Vector3(-20f, RoadTop, 404.8f), 180f, rng);
        }

        // ---------- Este (de la avenida al borde) ----------

        static Rect[] NorthEastBandRoads() => new[] { NbGSouth, NbGNorth, PrescottRect(2), NbCourt, NbTowerDriveway };

        static Rect[] NorthEastBandAlleys() => new[] { Rect.MinMaxRect(120f, 338f, 207f, 343f) };

        static Rect[] NorthEastBandSidewalks() => new[]
        {
            Rect.MinMaxRect(120f, 298f, 204.5f, 300.5f),     // H norte
            Rect.MinMaxRect(204.5f, 298f, 207f, 384f),       // G
            Rect.MinMaxRect(217f, 298f, 219.5f, 384f),
            Rect.MinMaxRect(204.5f, 406f, 207f, PrescottZ0),
            Rect.MinMaxRect(217f, 406f, 219.5f, PrescottZ0),
            Rect.MinMaxRect(120f, 381f, 299.5f, 384f),       // bulevar
            Rect.MinMaxRect(120f, 406f, 299.5f, 409f),
            Rect.MinMaxRect(120f, 483.5f, 299.5f, PrescottZ0), // Prescott
            Rect.MinMaxRect(120f, PrescottZ1, 299.5f, 498.5f),
            Rect.MinMaxRect(152.5f, 452f, 155f, 483.5f),     // Merrin Ct (la acera de la rotonda la pone BuildCulDeSac)
            Rect.MinMaxRect(165f, 452f, 167.5f, 483.5f),
        };

        static Rect NbBulbSquare => Rect.MinMaxRect(NbBulb.x - 14.5f, NbBulb.y - 14.5f, NbBulb.x + 14.5f, NbBulb.y + 14.5f);

        static Rect[] NorthEastBandLawns()
        {
            var lawns = new List<Rect> { Rect.MinMaxRect(120f, 300.5f, 204.5f, 381f), Rect.MinMaxRect(219.5f, 409f, 299.5f, 483.5f) };
            lawns.AddRange(RectMinus(Rect.MinMaxRect(219.5f, 298f, 299.5f, 381f), new List<Rect> { NbTowerLot, NbTowerDriveway }));
            lawns.AddRange(RectMinus(Rect.MinMaxRect(120f, 409f, 204.5f, 483.5f), new List<Rect> { NbBulbSquare, Rect.MinMaxRect(152.5f, 446f, 167.5f, 483.5f) }));
            return lawns.ToArray();
        }

        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] NorthEastBandRows =
        {
            ("H_Norte_E_1", new Vector3(140f, 0f, 300.5f), 0f, 40f, 37.5f, RowKind.Houses),             // x 120–160
            ("H_Norte_E_Apart", new Vector3(182.25f, 0f, 300.5f), 0f, 44.5f, 37.5f, RowKind.Apartments), // x 160–204.5
            ("Bulevar_S_E", new Vector3(170.75f, 0f, 381f), 180f, 67.5f, 38f, RowKind.Houses),           // x 137–204.5
            ("Bulevar_N_E_1", new Vector3(156.5f, 0f, 409f), 0f, 43f, 16f, RowKind.Houses),              // x 135–178
            ("Bulevar_N_E_2", new Vector3(191.25f, 0f, 409f), 0f, 26.5f, 16f, RowKind.Houses),           // x 178–204.5
            // Calle sin salida: a los lados de la calle y alrededor de la rotonda.
            ("Merrin_O", new Vector3(152.5f, 0f, 468.25f), 270f, 30.5f, 32.5f, RowKind.Houses),          // z 453–483.5
            ("Merrin_O_Rotonda", new Vector3(145f, 0f, 440f), 270f, 26f, 25f, RowKind.Houses),           // z 427–453
            ("Merrin_E", new Vector3(167.5f, 0f, 468.25f), 90f, 30.5f, 37f, RowKind.Houses),
            ("Merrin_E_Rotonda", new Vector3(175f, 0f, 440f), 90f, 26f, 29.5f, RowKind.Apartments),
            // Al este de G, delante de la iglesia, y el solar de las vallas publicitarias.
            ("G_E_N", new Vector3(219.5f, 0f, 423.5f), 90f, 27f, 30f, RowKind.Houses),                   // z 410–437
            ("Solar_Vallas", new Vector3(285.75f, 0f, 409f), 0f, 27.5f, 74.5f, RowKind.Overgrown),       // x 272–299.5
        };

        static void NorthEastBandLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { 135f, 175f }) StreetLight(group, new Vector3(x, 0f, 299.6f), Vector3.back, pole, lamp);
            foreach (float z in new[] { 330f, 370f, 430f, 470f }) StreetLight(group, new Vector3(205.4f, 0f, z), Vector3.right, pole, lamp);
            foreach (float x in new[] { 150f, 190f, 240f, 285f }) StreetLight(group, new Vector3(x, 0f, 383.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { 140f, 230f, 280f }) StreetLight(group, new Vector3(x, 0f, 406.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { 140f, 195f, 255f }) StreetLight(group, new Vector3(x, 0f, 484.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { 135f, 200f, 270f }) StreetLight(group, new Vector3(x, 0f, 497.6f), Vector3.back, pole, lamp);
            StreetLight(group, new Vector3(153.4f, 0f, 470f), Vector3.right, pole, lamp); // Merrin Ct

            NorthShop(group, ShopKind.Barber, V(128f, 380f), Vector2.up, 14f, 2, rng);    // esquina del bulevar con la avenida, al sur
            NorthShop(group, ShopKind.Laundry, V(126.5f, 410f), Vector2.down, 11f, 1, rng); // y al norte
            NorthShop(group, ShopKind.Tires, V(263f, 410f), Vector2.down, 14f, 1, rng);    // taller junto al solar de las vallas

            BuildCulDeSac(group, rng);
            NorthTowerBlock(group, rng);
            NorthChurch(group, rng);
            var steel = Mat("AceroValla", new Color(0.2f, 0.21f, 0.22f));
            Billboard(group, new Vector3(281f, LawnTop, 416f), 0f, steel, 0);
            Billboard(group, new Vector3(292f, LawnTop, 452f), -35f, steel, 1);

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(122f, AlleyTop, 338.6f), new Vector3(204f, AlleyTop, 338.6f), 27f), wood, cable);

            ParkedCar(group, new Vector3(208.2f, RoadTop, 350f), 90f, rng);
            ParkedCar(group, new Vector3(215.8f, RoadTop, 455f), 270f, rng);
            ParkedCar(group, new Vector3(156.2f, RoadTop, 470f), 90f, rng);
            ParkedCar(group, new Vector3(180f, RoadTop, PrescottZ0 + 1.2f), 0f, rng);
            ParkedCar(group, new Vector3(240f, RoadTop, PrescottZ1 - 1.2f), 180f, rng);
            ParkedCar(group, new Vector3(170f, RoadTop, 385.2f), 0f, rng);
            ParkedCar(group, new Vector3(250f, RoadTop, 404.8f), 180f, rng);

            // Borde este provisional: valla de malla y barreras al final del bulevar y de Prescott (seguirán en la ampliación este).
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            FenceSegment(group, new Vector3(NorthEdgeX - 0.2f, 0f, 298f), new Vector3(NorthEdgeX - 0.2f, 0f, NorthWallZ - 0.2f), 2.2f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            RoadClosed(group, NorthEdgeX - 2.5f, BoulevardZ - BoulevardHalf, BoulevardZ - BoulevardMedian / 2f);
            RoadClosed(group, NorthEdgeX - 2.5f, BoulevardZ + BoulevardMedian / 2f, BoulevardZ + BoulevardHalf);
            RoadClosed(group, NorthEdgeX - 2.5f, PrescottZ0, PrescottZ1);
        }
    }
}
