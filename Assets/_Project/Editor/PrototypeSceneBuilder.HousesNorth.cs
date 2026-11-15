using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Ampliación hacia el norte (+Z) de los barrios de casas, a los dos lados de la avenida, hasta el borde del
    /// mundo. Las calles son rectas y siguen las que ya había:
    /// - F/Fe (z 248–258): una sola calle recta de la calle x = −110 a la calle G, que cruza la avenida.
    /// - H (z 288–298): calle del borde norte, también de la calle x = −110 a G, cruzando la avenida.
    /// - B' (x 3–13) sigue recta hacia el norte desde la copia oeste hasta F, y B (x 207–217) sigue recta desde el
    ///   barrio del este hasta H con el nombre de G (ya no son calles sin salida por ese lado).
    /// Las filas del sur dan la espalda a los callejones del antiguo borde norte (a4 en el este; a1' y n3 en el
    /// oeste), cuyas vallas se quitaron; las del norte de F/Fe dan la espalda a la acera de H. En coordenadas del mundo.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float HousesNorthMaxZ = 298f; // borde norte de la calle H (el suelo acaba en 300)

        // ---------- Este ----------

        static readonly Rect[] NorthEastRoads =
        {
            Rect.MinMaxRect(117f, 248f, 207f, 258f),   // Fe (sigue a F al otro lado de la avenida)
            Rect.MinMaxRect(207f, 202f, 217f, 298f),   // G: sigue a B desde z = 202 hasta H
            Rect.MinMaxRect(117f, 288f, 207f, 298f),   // H (tramo este)
        };

        static readonly Rect[] NorthEastAlleys =
        {
            Rect.MinMaxRect(241f, 213.5f, 246f, 285.5f), // sigue a a5 hacia el norte
        };

        // Las aceras de G se cortan en el callejón a4 (x 120–246, z 208.5–213.5), que es del barrio del este.
        static readonly Rect[] NorthEastSidewalks =
        {
            Rect.MinMaxRect(120f, 245.5f, 207f, 248f),     // Fe sur
            Rect.MinMaxRect(120f, 258f, 207f, 260.5f),     // Fe norte
            Rect.MinMaxRect(204.5f, 202f, 207f, 208.5f),   // G oeste (entre B y a4)
            Rect.MinMaxRect(204.5f, 213.5f, 207f, 285.5f), // G oeste
            Rect.MinMaxRect(217f, 202f, 219.5f, 208.5f),   // G este (entre B y a4)
            Rect.MinMaxRect(217f, 213.5f, 219.5f, 298f),   // G este
            Rect.MinMaxRect(120f, 285.5f, 207f, 288f),     // H sur
        };

        static readonly Rect[] NorthEastLawns =
        {
            Rect.MinMaxRect(120f, 214f, 207f, 248f),
            Rect.MinMaxRect(120f, 258f, 207f, 288f),
            Rect.MinMaxRect(217f, 214f, 250f, HousesNorthMaxZ),
        };

        // (nombre, centro de la línea de fachada, yaw, largo, fondo, tipo). Ver Row.
        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] NorthEastRows =
        {
            ("Fe_Sur_Apart", new Vector3(138.5f, 0f, 245.5f), 180f, 37f, 32f, RowKind.Apartments), // x 120–157, hasta a4
            ("Fe_Sur", new Vector3(180.75f, 0f, 245.5f), 180f, 47.5f, 32f, RowKind.Houses),        // x 157–204.5
            ("Fe_Norte_O", new Vector3(135f, 0f, 260.5f), 0f, 30f, 25f, RowKind.Houses),           // x 120–150, hasta la acera de H
            ("Fe_Norte_Solar", new Vector3(158f, 0f, 260.5f), 0f, 16f, 25f, RowKind.Overgrown),    // x 150–166
            ("Fe_Norte_E", new Vector3(185.25f, 0f, 260.5f), 0f, 38.5f, 25f, RowKind.Houses),      // x 166–204.5
            ("G_Este_1", new Vector3(219.5f, 0f, 228f), 90f, 26f, 21.5f, RowKind.Apartments),      // z 215–241
            ("G_Este_Solar", new Vector3(219.5f, 0f, 250f), 90f, 14f, 21.5f, RowKind.Overgrown),   // z 243–257
            ("G_Este_2", new Vector3(219.5f, 0f, 271f), 90f, 24f, 21.5f, RowKind.Apartments),      // z 259–283
        };

        // ---------- Oeste ----------

        static readonly Rect[] NorthWestRoads =
        {
            Rect.MinMaxRect(-103f, 248f, 103f, 258f),  // F (de la calle x = −110 a la avenida)
            Rect.MinMaxRect(3f, 224f, 13f, 248f),      // B' sigue recta desde la copia oeste hasta F
            Rect.MinMaxRect(-103f, 288f, 103f, 298f),  // H (tramo oeste)
        };

        static readonly Rect[] NorthWestAlleys =
        {
            Rect.MinMaxRect(-42f, 260.5f, -37f, 285.5f), // pasaje norte–sur entre F y H
        };

        static readonly Rect[] NorthWestSidewalks =
        {
            Rect.MinMaxRect(-103f, 226f, -100.5f, 288f), // lado este de la calle x = −110
            Rect.MinMaxRect(-100.5f, 245.5f, 100f, 248f), // F sur
            Rect.MinMaxRect(-100.5f, 258f, 100f, 260.5f), // F norte
            Rect.MinMaxRect(0.5f, 224f, 3f, 245.5f),      // B' oeste (sigue la acera de la copia)
            Rect.MinMaxRect(13f, 224f, 15.5f, 245.5f),    // B' este
            Rect.MinMaxRect(-100.5f, 285.5f, 100f, 288f), // H sur
        };

        static readonly Rect[] NorthWestLawns =
        {
            Rect.MinMaxRect(-100.5f, 226f, 3f, 248f),
            Rect.MinMaxRect(13f, 226f, 100f, 248f),
            Rect.MinMaxRect(-100.5f, 258f, 100f, 288f),
        };

        static readonly (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] NorthWestRows =
        {
            // Sur de F, hasta los callejones a1'/n3 (z 224).
            ("F_Sur_O", new Vector3(-81.25f, 0f, 245.5f), 180f, 38.5f, 21.5f, RowKind.Houses),       // x −100.5…−62
            ("F_Sur_Solar", new Vector3(-55f, 0f, 245.5f), 180f, 14f, 21.5f, RowKind.Overgrown),     // x −62…−48
            ("F_Sur_Apart", new Vector3(-29.5f, 0f, 245.5f), 180f, 37f, 21.5f, RowKind.Apartments),  // x −48…−11
            ("F_Sur_B", new Vector3(-5.25f, 0f, 245.5f), 180f, 11.5f, 21.5f, RowKind.Houses),        // x −11…0.5 (esquina con B')
            ("F_Sur_E", new Vector3(37.75f, 0f, 245.5f), 180f, 44.5f, 21.5f, RowKind.Houses),        // x 15.5–60
            ("F_Sur_Solar_E", new Vector3(67f, 0f, 245.5f), 180f, 14f, 21.5f, RowKind.Overgrown),    // x 60–74
            ("F_Sur_Avenida", new Vector3(87f, 0f, 245.5f), 180f, 26f, 21.5f, RowKind.Houses),       // x 74–100
            // Norte de F, hasta la acera de H.
            ("F_Norte_O", new Vector3(-71.25f, 0f, 260.5f), 0f, 58.5f, 25f, RowKind.Houses),        // x −100.5…−42
            ("F_Norte_Apart", new Vector3(-18.5f, 0f, 260.5f), 0f, 37f, 25f, RowKind.Apartments),   // x −37…0
            ("F_Norte_C", new Vector3(22.5f, 0f, 260.5f), 0f, 45f, 25f, RowKind.Houses),            // x 0–45
            ("F_Norte_Solar", new Vector3(52f, 0f, 260.5f), 0f, 14f, 25f, RowKind.Overgrown),       // x 45–59
            ("F_Norte_E", new Vector3(79.5f, 0f, 260.5f), 0f, 41f, 25f, RowKind.Houses),            // x 59–100
        };

        // Calles que salen de la avenida (para cortar sus aceras y no poner pórtico del tren ahí).
        static readonly Rect[] NorthAvenueRoadsEast = { NorthEastRoads[0], NorthEastRoads[2] };
        static readonly Rect[] NorthAvenueRoadsWest = { NorthWestRoads[0], NorthWestRoads[2] };

        static void BuildHousesNorth(Transform world)
        {
            var group = new GameObject("Barrio_Casas_Norte").transform;
            group.SetParent(world, false);
            BuildHousesPatch(group, "Este", 2005, NorthEastRoads, NorthEastAlleys, NorthEastSidewalks, NorthEastLawns, NorthEastRows, NorthEastStreetLife);
            BuildHousesPatch(group, "Oeste", 2007, NorthWestRoads, NorthWestAlleys, NorthWestSidewalks, NorthWestLawns, NorthWestRows, NorthWestStreetLife);
        }

        /// <summary>Trozo de barrio de casas en coordenadas del mundo: suelo, filas, vida de calle y tiempo.</summary>
        static void BuildHousesPatch(Transform parent, string name, int seed, Rect[] roads, Rect[] alleys, Rect[] sidewalks, Rect[] lawns,
            (string name, Vector3 front, float yaw, float length, float depth, RowKind kind)[] rowSpecs, System.Action<Transform, System.Random> streetLife)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            housesFrame = null;
            housesOccupied.Clear();
            var rng = new System.Random(seed);

            BuildHousesGround(group, roads, alleys, sidewalks, lawns);
            var rows = new GameObject("Parcelas").transform;
            rows.SetParent(group, false);
            foreach (var r in rowSpecs) Row(rows, r.name, r.front, r.yaw, r.length, r.depth, r.kind, rng);
            streetLife(group, rng);
            BuildHousesWeather(group, rng, roads, alleys, sidewalks, lawns);
        }

        static void NorthEastStreetLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { 135f, 170f, 198f }) StreetLight(group, new Vector3(x, 0f, 259.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { 140f, 185f }) StreetLight(group, new Vector3(x, 0f, 286.4f), Vector3.forward, pole, lamp);
            foreach (float z in new[] { 230f, 270f }) StreetLight(group, new Vector3(205.4f, 0f, z), Vector3.right, pole, lamp);

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(245.6f, AlleyTop, 218f), new Vector3(245.6f, AlleyTop, 282f), 21f), wood, cable);

            ParkedCar(group, new Vector3(150f, RoadTop, 249.2f), 0f, rng);
            ParkedCar(group, new Vector3(188f, RoadTop, 256.8f), 180f, rng);
            ParkedCar(group, new Vector3(215.8f, RoadTop, 240f), -90f, rng);
            ParkedCar(group, new Vector3(208.2f, RoadTop, 276f), 90f, rng);
            ParkedCar(group, new Vector3(160f, RoadTop, 289.2f), 0f, rng);

            WoodFenceLine(group, new Vector3(249.8f, LawnTop, 214f), new Vector3(249.8f, LawnTop, HousesNorthMaxZ));
            WoodFenceLine(group, new Vector3(219.5f, LawnTop, HousesNorthMaxZ - 0.2f), new Vector3(250f, LawnTop, HousesNorthMaxZ - 0.2f));
        }

        static void NorthWestStreetLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float x in new[] { -85f, -55f, -20f, 30f, 65f, 92f }) StreetLight(group, new Vector3(x, 0f, 246.4f), Vector3.forward, pole, lamp);
            foreach (float x in new[] { -70f, -10f, 50f }) StreetLight(group, new Vector3(x, 0f, 286.4f), Vector3.forward, pole, lamp);
            StreetLight(group, new Vector3(1.6f, 0f, 232f), Vector3.right, pole, lamp);  // B'
            StreetLight(group, new Vector3(14.4f, 0f, 240f), Vector3.left, pole, lamp);
            foreach (float z in new[] { 240f, 275f }) StreetLight(group, new Vector3(-101.6f, 0f, z), Vector3.left, pole, lamp);

            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(-37.4f, AlleyTop, 264f), new Vector3(-37.4f, AlleyTop, 282f), 18f), wood, cable);

            ParkedCar(group, new Vector3(-75f, RoadTop, 249.2f), 0f, rng);
            ParkedCar(group, new Vector3(-30f, RoadTop, 256.8f), 180f, rng);
            ParkedCar(group, new Vector3(40f, RoadTop, 249.2f), 0f, rng);
            ParkedCar(group, new Vector3(80f, RoadTop, 256.8f), 180f, rng);
            ParkedCar(group, new Vector3(4.2f, RoadTop, 236f), 90f, rng); // en B'
            ParkedCar(group, new Vector3(-50f, RoadTop, 289.2f), 0f, rng);
        }
    }
}
