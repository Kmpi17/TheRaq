using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Oeste lejano: barrio de casas al otro lado de la calle x = −110, de x −300 a −120 y de la calle norte (z 90)
    /// al borde norte (z 298). Las calles siguen rectas las de la ampliación oeste y la norte (D, E, F y H cruzan la
    /// calle x = −110) y acaban en la calle J (x −262…−252), que va de la calle norte a H. Lleva:
    /// - una gasolinera en la esquina de la calle norte con la calle x = −110 (marquesina, surtidores, tienda y poste
    ///   de precios), con entradas desde las dos calles;
    /// - negocios sueltos frente a la calle norte (alitas, empeños, belleza, neumáticos, lavandería) y un solar;
    /// - una cancha de baloncesto al final del callejón entre D y E;
    /// - un descampado grande entre E y F, junto a J (barro, escombros, malla caída, coche abandonado, bidón quemado);
    /// - filas de casas y apartamentos, solares con vegetación y vacíos.
    /// Todo en coordenadas del mundo (BuildHousesPatch).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float FarWestMinX = -300f, FarWestMaxX = -120f; // de borde del mundo a la acera oeste de la calle x = −110

        // Calle J (N–S) y las calles E–O que siguen las de la ampliación oeste y la norte al otro lado de la calle x = −110.
        static readonly Rect FarWestJ = Rect.MinMaxRect(-262f, 87f, -252f, 298f);

        static readonly Rect[] FarWestRoads =
        {
            FarWestJ,
            Rect.MinMaxRect(-252f, 126.5f, -117f, 136.5f), // D (sigue a la D de la ampliación oeste)
            Rect.MinMaxRect(-252f, 180.5f, -117f, 190.5f), // E
            Rect.MinMaxRect(-252f, 248f, -117f, 258f),     // F
            Rect.MinMaxRect(-252f, 288f, -117f, 298f),     // H
        };

        static readonly Rect[] FarWestAlleys =
        {
            Rect.MinMaxRect(-226f, 156.5f, -120f, 161.5f),   // sigue al callejón n2 de la ampliación oeste; acaba en la cancha
            Rect.MinMaxRect(-205f, 219f, -120f, 224f),       // sigue al n3; acaba en el descampado
        };

        static readonly Rect[] FarWestSidewalks =
        {
            Rect.MinMaxRect(-264.5f, 90f, -262f, 298f),      // J oeste
            Rect.MinMaxRect(-252f, 90f, -249.5f, 298f),      // J este (se corta en D, E, F y H)
            Rect.MinMaxRect(-249.5f, 124f, -120f, 126.5f),   // D sur
            Rect.MinMaxRect(-249.5f, 136.5f, -120f, 139f),   // D norte
            Rect.MinMaxRect(-249.5f, 178f, -120f, 180.5f),   // E sur
            Rect.MinMaxRect(-249.5f, 190.5f, -120f, 193f),   // E norte
            Rect.MinMaxRect(-249.5f, 245.5f, -120f, 248f),   // F sur
            Rect.MinMaxRect(-249.5f, 258f, -120f, 260.5f),   // F norte
            Rect.MinMaxRect(-249.5f, 285.5f, -120f, 288f),   // H sur
        };

        // Negocios (z 90–102) y gasolinera (x −160…−120, z 90–124) ponen su propio suelo.
        static readonly Rect FarWestShopsArea = Rect.MinMaxRect(-249.5f, 90f, -160f, 102f);
        static readonly Rect GasLot = Rect.MinMaxRect(-160f, 90f, -120f, 124f);
        static readonly Rect[] GasDriveways =
        {
            Rect.MinMaxRect(-156f, 87f, -146f, 90f),   // desde la calle norte
            Rect.MinMaxRect(-134f, 87f, -124f, 90f),
            Rect.MinMaxRect(-120f, 104f, -117f, 114f), // desde la calle x = −110
        };

        static readonly Rect[] FarWestLawns =
        {
            Rect.MinMaxRect(-249.5f, 102f, -160f, 124f),     // detrás de los negocios
            Rect.MinMaxRect(-249.5f, 139f, -120f, 178f),     // entre D y E (cancha al oeste)
            Rect.MinMaxRect(-249.5f, 193f, -120f, 245.5f),   // entre E y F (descampado al oeste)
            Rect.MinMaxRect(-249.5f, 260.5f, -120f, 285.5f), // entre F y H
            Rect.MinMaxRect(FarWestMinX, 90f, -264.5f, 298f),// al oeste de J
        };

        static readonly Vector3 FarWestCourtCenter = new(-237.75f, 0f, 158.5f);
        static readonly Rect FarWestCourtLot = Rect.MinMaxRect(-249.5f, 139f, -226f, 178f);
        static readonly Rect FarWestWasteland = Rect.MinMaxRect(-249.5f, 193f, -205f, 245.5f);

        static readonly (ShopKind kind, float x0, float x1, int floors)[] FarWestShops =
        {
            (ShopKind.Wings, -247f, -235f, 1),
            (ShopKind.Pawn, -229f, -219f, 2),
            (ShopKind.Beauty, -215f, -202f, 1),
            (ShopKind.Tires, -190f, -176f, 1),
            (ShopKind.Laundry, -173f, -163f, 1),
        };
        static readonly Rect FarWestShopsVacant = Rect.MinMaxRect(-201f, 93f, -191f, 102f);

        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] FarWestRows =
        {
            // Detrás de los negocios, frente a D (hasta los patios de servicio, z 102).
            ("D_Sur_O", new Vector3(-232.75f, 0f, 124f), 180f, 33.5f, 22f, RowKind.Houses),     // x −249.5…−216
            ("D_Sur_Solar", new Vector3(-208f, 0f, 124f), 180f, 16f, 22f, RowKind.Overgrown),   // x −216…−200
            ("D_Sur_E", new Vector3(-180f, 0f, 124f), 180f, 40f, 22f, RowKind.Houses),          // x −200…−160
            // Entre D y E, espalda con espalda contra el callejón (la cancha ocupa x −249.5…−226).
            ("D_Norte_O", new Vector3(-205f, 0f, 139f), 0f, 42f, 17.5f, RowKind.Houses),        // x −226…−184
            ("D_Norte_E", new Vector3(-166f, 0f, 139f), 0f, 36f, 17.5f, RowKind.Houses),        // x −184…−148
            ("D_Norte_Solar", new Vector3(-141f, 0f, 139f), 0f, 14f, 17.5f, RowKind.Vacant),    // x −148…−134
            ("D_Norte_Esq", new Vector3(-127f, 0f, 139f), 0f, 14f, 17.5f, RowKind.Houses),      // x −134…−120
            ("E_Sur_O", new Vector3(-208f, 0f, 178f), 180f, 36f, 16.5f, RowKind.Houses),        // x −226…−190
            ("E_Sur_Solar", new Vector3(-182f, 0f, 178f), 180f, 16f, 16.5f, RowKind.Overgrown), // x −190…−174
            ("E_Sur_E", new Vector3(-147f, 0f, 178f), 180f, 54f, 16.5f, RowKind.Houses),        // x −174…−120
            // Entre E y F (el descampado ocupa x −249.5…−205).
            ("E_Norte_Apart", new Vector3(-186.5f, 0f, 193f), 0f, 37f, 26f, RowKind.Apartments),// x −205…−168
            ("E_Norte", new Vector3(-144f, 0f, 193f), 0f, 48f, 26f, RowKind.Houses),            // x −168…−120
            ("F_Sur_O", new Vector3(-183.5f, 0f, 245.5f), 180f, 43f, 21.5f, RowKind.Houses),    // x −205…−162
            ("F_Sur_Solar", new Vector3(-155f, 0f, 245.5f), 180f, 14f, 21.5f, RowKind.Overgrown),// x −162…−148
            ("F_Sur_E", new Vector3(-134f, 0f, 245.5f), 180f, 28f, 21.5f, RowKind.Houses),      // x −148…−120
            // Entre F y H, dando la espalda a la acera de H.
            ("F_Norte_O", new Vector3(-221.75f, 0f, 260.5f), 0f, 55.5f, 25f, RowKind.Houses),   // x −249.5…−194
            ("F_Norte_Apart", new Vector3(-175.5f, 0f, 260.5f), 0f, 37f, 25f, RowKind.Apartments),// x −194…−157
            ("F_Norte_Solar", new Vector3(-150f, 0f, 260.5f), 0f, 14f, 25f, RowKind.Vacant),    // x −157…−143
            ("F_Norte_E", new Vector3(-131.5f, 0f, 260.5f), 0f, 23f, 25f, RowKind.Houses),      // x −143…−120
            // Al oeste de J, mirando a J, hasta el borde del mundo (yaw 270: la parcela hacia −X; el largo va por Z).
            ("J_Oeste_1", new Vector3(-264.5f, 0f, 113f), 270f, 44f, 35f, RowKind.Houses),      // z 91–135
            ("J_Oeste_Apart", new Vector3(-264.5f, 0f, 153.5f), 270f, 37f, 35f, RowKind.Apartments), // z 135–172
            ("J_Oeste_Solar", new Vector3(-264.5f, 0f, 180f), 270f, 16f, 35f, RowKind.Overgrown),// z 172–188
            ("J_Oeste_2", new Vector3(-264.5f, 0f, 214f), 270f, 52f, 35f, RowKind.Houses),      // z 188–240
            ("J_Oeste_Solar2", new Vector3(-264.5f, 0f, 247f), 270f, 14f, 35f, RowKind.Vacant), // z 240–254
            ("J_Oeste_3", new Vector3(-264.5f, 0f, 275.5f), 270f, 43f, 35f, RowKind.Houses),    // z 254–297
        };

        /// <summary>Huecos en las aceras de enfrente de las calles grandes (BuildStreets): la calle J y las entradas de la gasolinera a la calle norte.</summary>
        static readonly Rect[] FarWestNorthSidewalkCuts = { FarWestJ, GasDriveways[0], GasDriveways[1] };
        /// <summary>Huecos en la acera de enfrente oeste: D, E, F, H y la entrada de la gasolinera desde la calle x = −110.</summary>
        static readonly Rect[] FarWestWestSidewalkCuts = { FarWestRoads[1], FarWestRoads[2], FarWestRoads[3], FarWestRoads[4], GasDriveways[2] };

        static void BuildFarWest(Transform world)
        {
            var group = new GameObject("Barrio_Oeste_Lejano").transform;
            group.SetParent(world, false);
            BuildHousesPatch(group, "Casas", 2011, FarWestRoads, FarWestAlleys, FarWestSidewalks, FarWestLawns, FarWestRows, FarWestStreetLife);
        }

        static void FarWestStreetLife(Transform parent, System.Random rng)
        {
            FarWestShopsStrip(parent, rng);
            GasStation(parent, rng);

            var court = new GameObject("Cancha").transform;
            court.SetParent(parent, false);
            Court(court, FarWestCourtCenter, -1f); // puerta hacia D
            var bark = LoadMat("Corteza");
            foreach (var p in new[] { new Vector3(-247.5f, LawnTop, 143f), new Vector3(-228f, LawnTop, 174f), new Vector3(-247.5f, LawnTop, 175f) })
                DistrictTree(court, p, bark, rng, Range(rng, 1f, 1.3f), rng);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            StreetLight(court, new Vector3(-228f, 0f, 145f), Vector3.left, pole, lamp);
            RecordFootprint(court, "cancha", FarWestCourtCenter.x - 8f, FarWestCourtCenter.z - 12f, FarWestCourtCenter.x + 8f, FarWestCourtCenter.z + 12f, 0f);

            Wasteland(parent, rng);

            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            foreach (float x in new[] { -235f, -200f, -165f, -135f })
            {
                StreetLight(group, new Vector3(x, 0f, 137.6f), Vector3.back, pole, lamp);   // D
                StreetLight(group, new Vector3(x + 12f, 0f, 191.6f), Vector3.back, pole, lamp); // E
                StreetLight(group, new Vector3(x, 0f, 246.4f), Vector3.forward, pole, lamp);  // F
                StreetLight(group, new Vector3(x + 12f, 0f, 286.4f), Vector3.forward, pole, lamp); // H
            }
            for (float z = 105f; z < 290f; z += 36f) StreetLight(group, new Vector3(-250.6f, 0f, z), Vector3.left, pole, lamp); // J

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(-224f, AlleyTop, 161f), new Vector3(-123f, AlleyTop, 161f), 22f), wood, cable);
            PoleLine(group, Points(new Vector3(-203f, AlleyTop, 223.6f), new Vector3(-123f, AlleyTop, 223.6f), 22f), wood, cable);
            PoleLine(group, Points(new Vector3(-263.5f, SidewalkTop, 100f), new Vector3(-263.5f, SidewalkTop, 292f), 32f), wood, cable);

            // Coches aparcados en el sentido de la marcha (por la derecha): en las E–O, yaw 0 (hacia +X) en el lado sur y
            // 180 en el norte; en J, yaw 90 (hacia −Z) en el lado oeste y 270 en el este.
            ParkedCar(group, new Vector3(-215f, RoadTop, 127.7f), 0f, rng);
            ParkedCar(group, new Vector3(-150f, RoadTop, 135.3f), 180f, rng);
            ParkedCar(group, new Vector3(-190f, RoadTop, 181.7f), 0f, rng);
            ParkedCar(group, new Vector3(-130f, RoadTop, 189.3f), 180f, rng);
            ParkedCar(group, new Vector3(-170f, RoadTop, 249.2f), 0f, rng);
            ParkedCar(group, new Vector3(-225f, RoadTop, 256.8f), 180f, rng);
            ParkedCar(group, new Vector3(-200f, RoadTop, 289.2f), 0f, rng);
            ParkedCar(group, new Vector3(-260.8f, RoadTop, 150f), 90f, rng);
            ParkedCar(group, new Vector3(-253.2f, RoadTop, 230f), 270f, rng);
            ParkedCar(group, new Vector3(-260.8f, RoadTop, 270f), 90f, rng);

            // Valla de madera en el borde del mundo (x −300) y en la cabecera norte de J.
            WoodFenceLine(group, new Vector3(FarWestMinX + 0.2f, LawnTop, 90f), new Vector3(FarWestMinX + 0.2f, LawnTop, 298f));
            WoodFenceLine(group, new Vector3(FarWestMinX, LawnTop, 297.8f), new Vector3(-264.5f, LawnTop, 297.8f));
        }

        // ---------- Negocios frente a la calle norte ----------

        static void FarWestShopsStrip(Transform parent, System.Random rng)
        {
            var group = new GameObject("Negocios").transform;
            group.SetParent(parent, false);
            var a = FarWestShopsArea;

            TexturedBox("AceraNegocios", group, new Vector3(a.center.x, SidewalkTop / 2f, (a.yMin + ShopsFront) / 2f), new Vector3(a.width, SidewalkTop, ShopsFront - a.yMin),
                sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);
            TexturedBox("Cesped", group, new Vector3(a.center.x, LawnTop / 2f, (ShopsFront + a.yMax) / 2f), new Vector3(a.width, LawnTop, a.yMax - ShopsFront),
                lawnMat, Vector2.one * ProceduralTextures.GrassTile);
            foreach (var (kind, x0, x1, floors) in FarWestShops)
            {
                float z0 = ShopsFront + ShopsDepth;
                TexturedBox("Patio", group, new Vector3((x0 + x1) / 2f, LawnTop + 0.01f, (z0 + a.yMax) / 2f), new Vector3(x1 - x0, 0.02f, a.yMax - z0),
                    alleyMat, Vector2.one * ProceduralTextures.AlleyTile, collider: false);
                Storefront(group, kind, x0, x1, floors, rng);
            }

            var vacant = new GameObject("Solar").transform;
            vacant.SetParent(group, false);
            vacant.localPosition = new Vector3(0f, 0f, FarWestShopsVacant.yMin);
            VacantLot(vacant, rng, FarWestShopsVacant.xMin, FarWestShopsVacant.xMax, FarWestShopsVacant.height);

            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { -240f, -205f, -170f }) StreetLight(group, new Vector3(x, 0f, 87.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { -232f, -196f, -174f }) TrashCan(group, new Vector3(x, SidewalkTop, 90.4f), 0f, rng.NextDouble() < 0.2);
            foreach (float x in new[] { -238f, -184f })
                ParkedCar(group, new Vector3(x, RoadTop, BlockHalfZ + SidewalkWidth + RoadWidth - 1.5f), 180f, rng);
            for (float x = a.xMin + 2f; x < a.xMax - 2f; x += Range(rng, 6f, 12f))
                if (rng.NextDouble() < 0.6)
                    SnowMound(group, new Vector3(x, SidewalkTop, 87.6f), new Vector3(Range(rng, 1.4f, 3f), Range(rng, 0.25f, 0.45f), 0.8f), 0f);
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            for (int i = 0; i < 40; i++)
                PlaceLitter(group, rng.Next(2) == 0 ? paper : can, new Vector3(Range(rng, a.xMin + 0.5f, a.xMax - 0.5f), SidewalkTop, Range(rng, 87.3f, ShopsFront - 0.6f)), rng);
            // Huecos entre locales: maleza y nieve.
            float prev = a.xMin;
            foreach (var (_, x0, x1, _) in FarWestShops)
            {
                if (x0 - prev > 2f && !(prev >= FarWestShopsVacant.xMin - 1f && x0 <= FarWestShopsVacant.xMax + 1f))
                    for (int i = 0; i < 3; i++)
                    {
                        var p = new Vector3(Range(rng, prev + 0.6f, x0 - 0.6f), LawnTop, Range(rng, ShopsFront + 0.6f, a.yMax - 0.6f));
                        if (FarWestShopsVacant.Contains(new Vector2(p.x, p.z))) continue;
                        if (i == 0) SnowPatch(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.2f, 2.2f), Range(rng, 1f, 2f)));
                        else Clump(group, rng, p, 0.3f, 0.9f);
                    }
                prev = x1;
            }
        }

        // ---------- Gasolinera ----------

        /// <summary>
        /// Gasolinera de esquina (calle norte y calle x = −110): explanada de asfalto con tres entradas, marquesina de
        /// 22 × 12 m con dos islas de dos surtidores, tienda "FOOD MART" al fondo, poste de precios en la esquina,
        /// máquina de aire, cubos, manchas y nieve apartada.
        /// </summary>
        static void GasStation(Transform parent, System.Random rng)
        {
            var root = new GameObject("Gasolinera").transform;
            root.SetParent(parent, false);
            var lot = GasLot;

            // Suelo: asfalto de la explanada (y de las entradas, que cortan las aceras en BuildStreets), losa bajo la marquesina.
            TexturedBox("Explanada", root, new Vector3(lot.center.x, LotTop / 2f, lot.center.y), new Vector3(lot.width, LotTop, lot.height),
                alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
            foreach (var r in GasDriveways)
                TexturedBox("Entrada", root, new Vector3(r.center.x, LotTop / 2f, r.center.y), new Vector3(r.width, LotTop, r.height),
                    alleyMat, Vector2.one * ProceduralTextures.AlleyTile, collider: false);
            var canopyCenter = new Vector3(-139f, 0f, 104.5f);
            const float canopyW = 22f, canopyD = 12f, canopyH = 5.2f;
            var slab = Mat("LosaGasolinera", new Color(0.62f, 0.61f, 0.58f));
            Box("Losa", root, new Vector3(canopyCenter.x, LotTop + 0.005f, canopyCenter.z), new Vector3(canopyW + 2f, 0.02f, canopyD + 3f), slab, collider: false);

            // Marquesina: tablero blanco con franja roja, luces en el techo y letreros "GAS" hacia las dos calles.
            var white = Mat("MarquesinaBlanca", new Color(0.86f, 0.86f, 0.84f));
            var red = Mat("FranjaRoja", new Color(0.65f, 0.08f, 0.06f));
            var light = EmissiveMat("LuzMarquesina", new Color(0.95f, 0.95f, 0.88f), 2.2f);
            Box("Marquesina", root, new Vector3(canopyCenter.x, canopyH + 0.4f, canopyCenter.z), new Vector3(canopyW, 0.8f, canopyD), white, collider: false);
            Box("FranjaS", root, new Vector3(canopyCenter.x, canopyH + 0.35f, canopyCenter.z - canopyD / 2f - 0.02f), new Vector3(canopyW, 0.3f, 0.04f), red, collider: false);
            Box("FranjaN", root, new Vector3(canopyCenter.x, canopyH + 0.35f, canopyCenter.z + canopyD / 2f + 0.02f), new Vector3(canopyW, 0.3f, 0.04f), red, collider: false);
            Box("FranjaE", root, new Vector3(canopyCenter.x + canopyW / 2f + 0.02f, canopyH + 0.35f, canopyCenter.z), new Vector3(0.04f, 0.3f, canopyD), red, collider: false);
            Box("FranjaO", root, new Vector3(canopyCenter.x - canopyW / 2f - 0.02f, canopyH + 0.35f, canopyCenter.z), new Vector3(0.04f, 0.3f, canopyD), red, collider: false);
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 2; j++)
                Box("LuzTecho", root, new Vector3(canopyCenter.x - 7.5f + i * 5f, canopyH - 0.02f, canopyCenter.z - 2.8f + j * 5.6f), new Vector3(1.2f, 0.04f, 1.2f), light, collider: false);
            var gas = SignMat("Letrero_Gas", "GAS", new Color(0.65f, 0.08f, 0.06f), new Color(0.98f, 0.96f, 0.9f), 84, 1.2f);
            float gasAspect = (float)gas.mainTexture.width / gas.mainTexture.height;
            SignQuad(root, new Vector3(canopyCenter.x, canopyH + 0.62f, canopyCenter.z - canopyD / 2f - 0.05f), Vector3.back, 0.4f * gasAspect, 0.4f, gas);
            SignQuad(root, new Vector3(canopyCenter.x + canopyW / 2f + 0.05f, canopyH + 0.62f, canopyCenter.z), Vector3.right, 0.4f * gasAspect, 0.4f, gas);
            var deck = new GameObject("MarquesinaColision").AddComponent<BoxCollider>();
            deck.transform.SetParent(root, false);
            deck.center = new Vector3(canopyCenter.x, canopyH + 0.4f, canopyCenter.z);
            deck.size = new Vector3(canopyW, 0.8f, canopyD);

            // Dos islas a lo largo de X, cada una con dos surtidores y un pilar en cada punta.
            var island = Mat("IslaHormigon", new Color(0.66f, 0.65f, 0.6f));
            var pillar = Mat("PilarMarquesina", new Color(0.8f, 0.8f, 0.78f));
            var bollard = Mat("PilonaAmarilla", new Color(0.85f, 0.7f, 0.1f));
            var pumpBody = Mat("Surtidor", new Color(0.85f, 0.85f, 0.82f));
            var hose = Mat("Manguera", new Color(0.05f, 0.05f, 0.05f));
            var screen = EmissiveMat("PantallaSurtidor", new Color(0.3f, 0.75f, 0.45f), 0.8f);
            var oil = LoadMat("Aceite");
            foreach (float dz in new[] { -3.2f, 3.2f })
            {
                float z = canopyCenter.z + dz;
                Box("Isla", root, new Vector3(canopyCenter.x, LotTop + 0.1f, z), new Vector3(16f, 0.2f, 1.3f), island);
                foreach (float end in new[] { -1f, 1f })
                {
                    float px = canopyCenter.x + end * 7.4f;
                    Box("Pilar", root, new Vector3(px, (LotTop + 0.2f + canopyH) / 2f, z), new Vector3(0.45f, canopyH - LotTop - 0.2f, 0.45f), pillar);
                    Primitive(PrimitiveType.Cylinder, "Pilona", root, new Vector3(canopyCenter.x + end * 8.4f, LotTop + 0.5f, z), new Vector3(0.18f, 0.5f, 0.18f), bollard, keepCollider: true);
                }
                foreach (float dx in new[] { -3.5f, 3.5f })
                {
                    float px = canopyCenter.x + dx;
                    Box("Surtidor", root, new Vector3(px, LotTop + 0.2f + 0.9f, z), new Vector3(1.1f, 1.8f, 0.6f), pumpBody);
                    Box("CabezaSurtidor", root, new Vector3(px, LotTop + 0.2f + 1.95f, z), new Vector3(1.15f, 0.32f, 0.65f), red, collider: false);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Box("Pantalla", root, new Vector3(px, LotTop + 1.55f, z + side * 0.31f), new Vector3(0.5f, 0.3f, 0.02f), screen, collider: false);
                        Box("Boquerel", root, new Vector3(px + 0.4f, LotTop + 1.05f, z + side * 0.34f), new Vector3(0.12f, 0.3f, 0.08f), hose, collider: false);
                        Strut("Manguera", root, new Vector3(px + 0.4f, LotTop + 0.95f, z + side * 0.36f), new Vector3(px + 0.15f, LotTop + 0.35f, z + side * 0.5f), 0.04f, hose, cylinder: true);
                        if (oil) Decal(root, new Vector3(px + Range(rng, -0.8f, 0.8f), LotTop + 0.012f, z + side * Range(rng, 1.4f, 2.4f)), Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.7f, 1.4f), oil);
                    }
                }
                TrashCan(root, new Vector3(canopyCenter.x, LotTop + 0.2f, z), 0f, false);
                Primitive(PrimitiveType.Cylinder, "CuboEscobilla", root, new Vector3(canopyCenter.x + 1f, LotTop + 0.45f, z), new Vector3(0.35f, 0.25f, 0.35f), Mat("CuboAzul", new Color(0.12f, 0.25f, 0.55f)), keepCollider: false);
            }
            // Un coche repostando y otro aparcado junto a la tienda.
            ParkedCar(root, new Vector3(canopyCenter.x - 3.5f, LotTop, canopyCenter.z - 5.6f), 0f, rng);
            ParkedCar(root, new Vector3(-125f, LotTop, 118f), 0f, rng);
            RecordFootprint(root, "gasolinera", canopyCenter.x - canopyW / 2f, canopyCenter.z - canopyD / 2f, canopyCenter.x + canopyW / 2f, canopyCenter.z + canopyD / 2f, canopyH + 0.8f);

            // Tienda al fondo, con la fachada hacia la marquesina (−Z): un Storefront en un marco desplazado.
            var martFrame = new GameObject("Tienda").transform;
            martFrame.SetParent(root, false);
            martFrame.localPosition = new Vector3(0f, LotTop, 113f - ShopsFront);
            Storefront(martFrame, ShopKind.GasMart, -158f, -141f, 1, rng);
            TexturedBox("AceraTienda", martFrame, new Vector3(-149.5f, SidewalkTop / 2f, ShopsFront - 1.2f), new Vector3(17f, SidewalkTop, 2.4f),
                sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);

            // Poste de precios en la esquina, en diagonal hacia el cruce: "GAS" arriba y tres filas de precio con números de LED.
            var pylon = new GameObject("PostePrecios").transform;
            pylon.SetParent(root, false);
            pylon.localPosition = new Vector3(-123.5f, LotTop, 93.5f);
            pylon.localRotation = Quaternion.Euler(0f, -45f, 0f); // la cara −Z local mira hacia el cruce (+X, −Z del mundo)
            var steel = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            foreach (float x in new[] { -0.9f, 0.9f })
                Box("Pata", pylon, new Vector3(x, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), steel);
            Box("Cartel", pylon, new Vector3(0f, 5.6f, 0f), new Vector3(2.6f, 3.4f, 0.4f), white, collider: false);
            Box("CartelBase", pylon, new Vector3(0f, 3.95f, 0f), new Vector3(2.7f, 0.15f, 0.45f), red, collider: false);
            var led = EmissiveMat("PrecioLED", new Color(1f, 0.45f, 0.08f), 2.5f);
            string[] grades = { "REGULAR", "PLUS", "DIESEL" };
            foreach (float face in new[] { -1f, 1f })
            {
                var normal = new Vector3(0f, 0f, face);
                SignQuad(pylon, new Vector3(0f, 6.85f, face * 0.21f), normal, 0.55f * gasAspect, 0.55f, gas);
                for (int i = 0; i < grades.Length; i++)
                {
                    float y = 6.1f - i * 0.65f;
                    var grade = SignMat($"Letrero_{grades[i]}", grades[i], new Color(0.92f, 0.92f, 0.9f), new Color(0.1f, 0.1f, 0.12f), 85 + i, 0.2f);
                    SignQuad(pylon, new Vector3(-0.6f, y, face * 0.21f), normal, 1.1f, 1.1f / ((float)grade.mainTexture.width / grade.mainTexture.height), grade);
                    Box("PanelPrecio", pylon, new Vector3(0.6f, y, face * 0.205f), new Vector3(1.1f, 0.48f, 0.02f), Mat("PanelNegro", new Color(0.04f, 0.04f, 0.05f)), collider: false);
                    for (int k = 0; k < 4; k++)
                        Box("Digito", pylon, new Vector3(0.2f + k * 0.26f, y, face * 0.218f), new Vector3(0.16f, 0.32f, 0.01f), led, collider: false);
                }
            }

            // Máquina de aire, contenedor detrás de la tienda, luces del borde, valla con el barrio y nieve apartada en las esquinas.
            var air = SignMat("Letrero_Aire", "AIR", new Color(0.12f, 0.3f, 0.65f), new Color(0.95f, 0.95f, 0.95f), 88, 0.4f);
            Box("MaquinaAire", root, new Vector3(-158.6f, LotTop + 0.75f, 100f), new Vector3(0.7f, 1.5f, 0.6f), Mat("MaquinaAzul", new Color(0.12f, 0.3f, 0.65f)));
            SignQuad(root, new Vector3(-158.24f, LotTop + 1.25f, 100f), Vector3.right, 0.5f, 0.5f / ((float)air.mainTexture.width / air.mainTexture.height), air);
            Dumpster(root, new Vector3(-138f, LotTop, 122.2f), 0f, rng.NextDouble() < 0.5);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            StreetLight(root, new Vector3(-158.8f, LotTop, 92f), Vector3.right, pole, lamp);
            StreetLight(root, new Vector3(-121f, LotTop, 121f), Vector3.left, pole, lamp);
            if (chainLink)
                FenceSegment(root, new Vector3(lot.xMin + 0.15f, LotTop, ShopsFront + 1f), new Vector3(lot.xMin + 0.15f, LotTop, lot.yMax - 0.3f), 1.8f, chainLink,
                    Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 3f, 0.05f, topRail: true);
            foreach (var p in new[] { new Vector3(-157f, LotTop, 121f), new Vector3(-122.5f, LotTop, 98f), new Vector3(-158f, LotTop, 108f) })
                SnowMound(root, p, new Vector3(Range(rng, 2f, 3.5f), Range(rng, 0.6f, 1f), Range(rng, 1.6f, 2.6f)), Range(rng, 0f, 90f));
        }

        // ---------- Descampado ----------

        /// <summary>
        /// Descampado grande (FarWestWasteland) junto a la calle J: tierra y barro con charcos, hierba a rodales,
        /// montones de escombros y ladrillos, malla con tramos caídos, un coche abandonado, palés, neumáticos, un sofá,
        /// un bidón quemado con brasas, cartel de "FOR SALE", árboles pelados y nieve. Es la cancha de la gente que no
        /// tiene cancha: senderos de barro lo cruzan en diagonal.
        /// </summary>
        static void Wasteland(Transform parent, System.Random rng)
        {
            var root = new GameObject("Descampado").transform;
            root.SetParent(parent, false);
            var r = FarWestWasteland;
            Vector3 P(float x, float z, float y = LawnTop) => new(x, y, z);
            Vector3 Any(float margin) => P(Range(rng, r.xMin + margin, r.xMax - margin), Range(rng, r.yMin + margin, r.yMax - margin));

            var mud = LoadMat("Barro");
            var puddle = LoadMat("Charco");
            var dirt = LoadMat("Suciedad");
            int layer = 0;
            float Y() => LawnTop + 0.003f + (layer++ % 30) * 0.0002f;

            // Tierra pelada: manchas de barro grandes que tapan casi todo el césped, y charcos.
            if (mud)
                for (int i = 0; i < 26; i++)
                {
                    var p = Any(3f);
                    Decal(root, new Vector3(p.x, Y(), p.z), Range(rng, 0f, 360f), new Vector2(Range(rng, 5f, 10f), Range(rng, 4f, 8f)), mud);
                }
            if (puddle)
                for (int i = 0; i < 6; i++)
                {
                    var p = Any(4f);
                    Decal(root, new Vector3(p.x, Y(), p.z), Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 3.5f), Range(rng, 1f, 2.2f)), puddle);
                }
            if (mud)
            {
                DesirePath(root, P(r.xMax - 1f, r.yMin + 1f), P(r.xMin + 1f, r.yMax - 1f), mud, rng);   // de la esquina E–sur a J–F
                DesirePath(root, P(r.xMin + 1f, r.yMin + 8f), P(r.xMax - 2f, r.yMax - 3f), mud, rng);
            }

            // Hierba alta a rodales (no por todo, como en los solares).
            for (int patch = 0; patch < 9; patch++)
            {
                var c = Any(3f);
                for (int i = 0; i < 18; i++)
                    Clump(root, rng, c + new Vector3(Range(rng, -3f, 3f), 0f, Range(rng, -3f, 3f)), 0.4f, 1.3f);
            }
            var bark = LoadMat("Corteza");
            for (int i = 0; i < 3; i++) BareTree(root, P(Range(rng, r.xMin + 1.5f, r.xMin + 6f), Range(rng, r.yMin + 5f, r.yMax - 5f)), bark, rng, Range(rng, 0.9f, 1.3f));
            DistrictTree(root, P(r.xMax - 3f, r.yMax - 4f), bark, rng, 1.4f, rng);

            // Escombros: montones de cascotes y ladrillos.
            var rubble = new[] { Mat("Cascote", new Color(0.55f, 0.54f, 0.5f)), Mat("LadrilloSuelto", new Color(0.45f, 0.24f, 0.18f)), Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f)) };
            for (int pile = 0; pile < 5; pile++)
            {
                var c = Any(5f);
                int n = 10 + rng.Next(12);
                for (int i = 0; i < n; i++)
                {
                    float s = Range(rng, 0.25f, 0.9f), off = Range(rng, 0f, 1.8f), a = Range(rng, 0f, Mathf.PI * 2f);
                    var piece = Box("Escombro", root, c + new Vector3(Mathf.Cos(a) * off, s * 0.3f + (1.8f - off) * 0.25f, Mathf.Sin(a) * off),
                        new Vector3(s, s * 0.6f, s * Range(rng, 0.6f, 1.2f)), rubble[rng.Next(rubble.Length)], collider: false);
                    piece.transform.localRotation = Quaternion.Euler(Range(rng, -25f, 25f), Range(rng, 0f, 360f), Range(rng, -25f, 25f));
                }
                Box("MontonCol", root, c + Vector3.up * 0.4f, new Vector3(2.6f, 0.8f, 2.6f), rubble[0], collider: true).GetComponent<Renderer>().enabled = false;
            }

            // Trastos: coche abandonado, palés, neumáticos, sofá, colchón y un carro de la compra volcado.
            Wreck(root, P(r.xMin + 14f, r.yMin + 30f), Range(rng, 0f, 360f), rng);
            var pallet = Mat("Pale", new Color(0.55f, 0.45f, 0.32f));
            for (int i = 0; i < 4; i++)
            {
                var p = Any(4f);
                for (int k = 0; k < 1 + rng.Next(3); k++)
                    Box("Pale", root, p + Vector3.up * (0.07f + k * 0.14f), new Vector3(1.2f, 0.13f, 1f), pallet, collider: false).transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 90f), 0f);
            }
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            for (int i = 0; i < 9; i++)
                Primitive(PrimitiveType.Cylinder, "Neumatico", root, Any(2f) + Vector3.up * 0.1f, new Vector3(0.7f, 0.1f, 0.7f), tire, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(Range(rng, -10f, 10f), 0f, Range(rng, -10f, 10f));
            var sofa = Mat("SofaNaranja", new Color(0.62f, 0.32f, 0.12f));
            var sofaRoot = new GameObject("Sofa").transform;
            sofaRoot.SetParent(root, false);
            sofaRoot.localPosition = Any(6f);
            sofaRoot.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 360f), 4f);
            Box("Asiento", sofaRoot, new Vector3(0f, 0.25f, 0f), new Vector3(2f, 0.4f, 0.85f), sofa);
            Box("Respaldo", sofaRoot, new Vector3(0f, 0.6f, 0.35f), new Vector3(2f, 0.6f, 0.2f), sofa, collider: false);
            foreach (float x in new[] { -0.95f, 0.95f }) Box("Brazo", sofaRoot, new Vector3(x, 0.45f, 0f), new Vector3(0.2f, 0.5f, 0.85f), sofa, collider: false);
            Box("Colchon", root, Any(5f) + Vector3.up * 0.1f, new Vector3(1.4f, 0.2f, 1.9f), Mat("Colchon", new Color(0.66f, 0.62f, 0.52f)), collider: false)
                .transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 3f);
            var cart = new GameObject("CarroCompra").transform;
            cart.SetParent(root, false);
            cart.localPosition = Any(5f) + Vector3.up * 0.3f;
            cart.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 360f), 80f); // volcado de lado
            var wire = Mat("Cromado", new Color(0.8f, 0.8f, 0.82f));
            Box("Cesta", cart, new Vector3(0f, 0.55f, 0f), new Vector3(0.55f, 0.45f, 0.85f), chainLink ? chainLink : wire, collider: false);
            Strut("Asa", cart, new Vector3(-0.28f, 0.85f, -0.45f), new Vector3(0.28f, 0.85f, -0.45f), 0.03f, wire, cylinder: true);

            // Bidón quemado con brasas: la luz naranja se ve de lejos en la niebla.
            var drum = Mat("BidonOxido", new Color(0.32f, 0.17f, 0.1f));
            var drumPos = P(r.xMin + 10f, r.yMin + 12f);
            Primitive(PrimitiveType.Cylinder, "Bidon", root, drumPos + Vector3.up * 0.45f, new Vector3(0.6f, 0.45f, 0.6f), drum, keepCollider: true);
            Primitive(PrimitiveType.Cylinder, "Brasas", root, drumPos + Vector3.up * 0.86f, new Vector3(0.5f, 0.02f, 0.5f), EmissiveMat("Brasas", new Color(1f, 0.42f, 0.1f), 3f), keepCollider: false);
            var fire = new GameObject("LuzFuego").AddComponent<Light>();
            fire.transform.SetParent(root, false);
            fire.transform.localPosition = drumPos + Vector3.up * 1.3f;
            fire.type = LightType.Point;
            fire.color = new Color(1f, 0.5f, 0.2f);
            fire.range = 7f;
            fire.intensity = 2.5f;
            fire.shadows = LightShadows.None;
            var crate = Mat("CajaLeche", new Color(0.15f, 0.3f, 0.6f));
            foreach (float a in new[] { 0.5f, 2.4f, 4.2f }) // cajas de leche para sentarse alrededor
                Box("CajaLeche", root, drumPos + new Vector3(Mathf.Cos(a) * 1.4f, 0.17f, Mathf.Sin(a) * 1.4f), new Vector3(0.34f, 0.3f, 0.34f), crate, collider: false);

            // Malla con tramos caídos a lo largo de J y de E; cartel de "FOR SALE" torcido en la esquina.
            if (chainLink)
            {
                var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
                FenceSegment(root, P(r.xMin + 0.3f, r.yMin + 0.5f), P(r.xMin + 0.3f, r.yMin + 18f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                FenceSegment(root, P(r.xMin + 0.3f, r.yMin + 24f), P(r.xMin + 0.3f, r.yMax - 0.5f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                FenceSegment(root, P(r.xMin + 0.5f, r.yMin + 0.3f), P(r.xMin + 15f, r.yMin + 0.3f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                FenceSegment(root, P(r.xMin + 26f, r.yMin + 0.3f), P(r.xMax - 0.5f, r.yMin + 0.3f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                // El tramo caído, tumbado en el suelo.
                var fallen = TexturedBox("VallaCaida", root, P(r.xMin + 20.5f, r.yMin + 1.2f, LawnTop + 0.05f), new Vector3(11f, 1.8f, 0.03f), chainLink, tile, sidesOnly: true);
                fallen.transform.localRotation = Quaternion.Euler(84f, 0f, 0f);
            }
            var forSale = SignMat("Letrero_Venta", "FOR SALE", new Color(0.92f, 0.92f, 0.9f), new Color(0.7f, 0.08f, 0.06f), 89, 0.15f);
            var signPos = P(r.xMax - 2f, r.yMin + 1f);
            Strut("PosteCartel", root, signPos, signPos + new Vector3(0.15f, 2.2f, 0f), 0.07f, ironMat, cylinder: true);
            SignQuad(root, signPos + new Vector3(0.15f, 2.1f, -0.06f), Vector3.back, 1.4f, 1.4f / ((float)forSale.mainTexture.width / forSale.mainTexture.height), forSale);

            // Nieve en las zonas de sombra, bolsas y suciedad.
            for (int i = 0; i < 8; i++)
                SnowPatch(root, Any(2f), Range(rng, 0f, 360f), new Vector2(Range(rng, 2f, 5f), Range(rng, 1.5f, 3f)));
            for (int i = 0; i < 10; i++) GarbageBag(root, Any(2f), rng);
            if (dirt)
                for (int i = 0; i < 6; i++)
                {
                    var p = Any(2f);
                    Decal(root, new Vector3(p.x, Y(), p.z), Range(rng, 0f, 360f), Vector2.one * Range(rng, 1.5f, 3f), dirt);
                }

            RecordFootprint(root, "solar", r.xMin, r.yMin, r.xMax, r.yMax, 0f);
        }
    }
}
