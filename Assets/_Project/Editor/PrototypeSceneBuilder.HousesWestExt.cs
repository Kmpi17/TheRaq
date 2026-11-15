using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Ampliación del barrio de casas oeste hacia el oeste, hasta la calle x = −110: dos calles sin salida (D y E) que
    /// entran desde esa calle, una fila de casas frente a la calle norte (z = 90) y tres callejones traseros que siguen
    /// los de la copia oeste (el de detrás de los negocios, uno entre D y E, y el del borde norte). Mezcla casas,
    /// apartamentos de 3 pisos y solares abandonados con vegetación (img/estructures/abandonedParcelVegetacion.png).
    /// Todo está en coordenadas del mundo (sin giro).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Zona: x ∈ [−103, −30], z ∈ [90, 226]. Al este (x = −30) empieza la copia oeste.
        const float WestExtMinX = -103f, WestExtMaxX = -30f, WestExtMinZ = 90f, WestExtMaxZ = 226f;
        const float WestExtStreetEnd = -36f; // fondo de las calles sin salida D y E

        static readonly Rect[] WestExtRoads =
        {
            Rect.MinMaxRect(WestExtMinX, 126.5f, WestExtStreetEnd, 136.5f), // D
            Rect.MinMaxRect(WestExtMinX, 180.5f, WestExtStreetEnd, 190.5f), // E
        };

        // Llegan hasta x = −26, el callejón a5 de la copia (la valla de la copia se abre ahí).
        static readonly Rect[] WestExtAlleys =
        {
            Rect.MinMaxRect(-100.5f, 102.5f, -26f, 107.5f), // n1: sigue el callejón de detrás de los negocios (a4')
            Rect.MinMaxRect(-100.5f, 156.5f, -26f, 161.5f), // n2: entre las calles D y E
            Rect.MinMaxRect(-100.5f, 219f, -26f, 224f),     // n3: borde norte, sigue a a1'
        };

        static readonly Rect[] WestExtSidewalks =
        {
            Rect.MinMaxRect(WestExtMinX, WestExtMinZ, -100.5f, WestExtMaxZ), // lado este de la calle x = −110
            Rect.MinMaxRect(-100.5f, 124f, WestExtStreetEnd, 126.5f),  // D sur
            Rect.MinMaxRect(-100.5f, 136.5f, WestExtStreetEnd, 139f),  // D norte
            Rect.MinMaxRect(WestExtStreetEnd, 124f, -33.5f, 139f),     // D fondo
            Rect.MinMaxRect(-100.5f, 178f, WestExtStreetEnd, 180.5f),  // E sur
            Rect.MinMaxRect(-100.5f, 190.5f, WestExtStreetEnd, 193f),  // E norte
            Rect.MinMaxRect(WestExtStreetEnd, 178f, -33.5f, 193f),     // E fondo
        };

        static readonly Rect[] WestExtLawns =
        {
            Rect.MinMaxRect(-100.5f, WestExtMinZ, WestExtMaxX, 126.5f),
            Rect.MinMaxRect(WestExtStreetEnd, 126.5f, WestExtMaxX, 136.5f),
            Rect.MinMaxRect(-100.5f, 136.5f, WestExtMaxX, 180.5f),
            Rect.MinMaxRect(WestExtStreetEnd, 180.5f, WestExtMaxX, 190.5f),
            Rect.MinMaxRect(-100.5f, 190.5f, WestExtMaxX, WestExtMaxZ),
        };

        // Huecos de la valla este de la copia oeste (en z LOCAL de la copia: local = 316 − mundo) donde entran n1–n3.
        static readonly (float z0, float z1)[] WestExtFenceGapsLocal = { (92f, 97f), (154.5f, 159.5f), (208.5f, 213.5f) };

        static void BuildHousesWestExtension(Transform world)
        {
            var group = new GameObject("Barrio_Casas_Oeste_Ampliacion").transform;
            group.SetParent(world, false);
            housesFrame = null; // huellas y zonas ocupadas en coordenadas del mundo
            housesOccupied.Clear();
            var rng = new System.Random(2003);

            BuildHousesGround(group, WestExtRoads, WestExtAlleys, WestExtSidewalks, WestExtLawns);

            // Filas (ver Row: yaw 0 → la parcela queda al norte de la línea de fachada; 180 → al sur).
            var rows = new GameObject("Parcelas").transform;
            rows.SetParent(group, false);
            Row(rows, "CalleNorte", new Vector3(-65.75f, 0f, 90f), 0f, 69.5f, 12.5f, RowKind.Houses, rng);       // x −100.5…−31
            Row(rows, "D_Sur", new Vector3(-68.25f, 0f, 124f), 180f, 64.5f, 16.5f, RowKind.Houses, rng);         // x −100.5…−36
            Row(rows, "D_Norte_Solar", new Vector3(-89.5f, 0f, 139f), 0f, 22f, 17.5f, RowKind.Overgrown, rng);   // esquina, como la foto
            Row(rows, "D_Norte", new Vector3(-57.25f, 0f, 139f), 0f, 42.5f, 17.5f, RowKind.Houses, rng);
            Row(rows, "E_Sur_O", new Vector3(-85.5f, 0f, 178f), 180f, 30f, 16.5f, RowKind.Houses, rng);
            Row(rows, "E_Sur_Solar", new Vector3(-63f, 0f, 178f), 180f, 15f, 16.5f, RowKind.Overgrown, rng);
            Row(rows, "E_Sur_E", new Vector3(-45.75f, 0f, 178f), 180f, 19.5f, 16.5f, RowKind.Houses, rng);
            Row(rows, "E_Norte_Apart", new Vector3(-82f, 0f, 193f), 0f, 37f, 26f, RowKind.Apartments, rng);     // x −100.5…−63.5
            Row(rows, "E_Norte", new Vector3(-49.75f, 0f, 193f), 0f, 27.5f, 26f, RowKind.Houses, rng);

            BuildWestExtStreetLife(group, rng);
            BuildHousesWeather(group, rng, WestExtRoads, WestExtAlleys, WestExtSidewalks, WestExtLawns);
        }

        static void BuildWestExtStreetLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);

            // Farolas de sodio en D, E y la acera de la calle x = −110.
            foreach (float x in new[] { -88f, -58f }) StreetLight(group, new Vector3(x, 0f, 137.6f), Vector3.back, pole, lamp);
            foreach (float x in new[] { -80f, -50f }) StreetLight(group, new Vector3(x, 0f, 179.4f), Vector3.forward, pole, lamp);
            foreach (float z in new[] { 112f, 150f, 205f }) StreetLight(group, new Vector3(-101.6f, 0f, z), Vector3.left, pole, lamp);

            // Postes de madera con cables por los callejones.
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            foreach (var r in WestExtAlleys)
                PoleLine(group, Points(new Vector3(r.xMin + 3f, AlleyTop, r.yMax - 0.4f), new Vector3(r.xMax - 4f, AlleyTop, r.yMax - 0.4f), 24f), wood, cable);

            // Coches aparcados junto a la acera.
            ParkedCar(group, new Vector3(-80f, RoadTop, 127.7f), 0f, rng);
            ParkedCar(group, new Vector3(-52f, RoadTop, 135.3f), 180f, rng);
            ParkedCar(group, new Vector3(-90f, RoadTop, 181.7f), 0f, rng);
            ParkedCar(group, new Vector3(-66f, RoadTop, 189.3f), 180f, rng);
            ParkedCar(group, new Vector3(-60f, AlleyTop, 159f), 90f, rng); // en el callejón n2

            // D y E sin salida: barrera de hormigón, pilonas y nieve apilada al fondo.
            var bollard = Mat("PilonaRoja", new Color(0.55f, 0.12f, 0.1f));
            foreach (var road in WestExtRoads)
            {
                float x = road.xMax - 0.6f, z = road.center.y;
                Box("Barrera", group, new Vector3(x, 0.42f, z), new Vector3(0.6f, 0.8f, 9.6f), concreteMat);
                for (float pz = road.yMin + 1f; pz <= road.yMax - 1f; pz += 2f)
                    Primitive(PrimitiveType.Cylinder, "Pilona", group, new Vector3(x - 0.8f, 0.47f, pz), new Vector3(0.18f, 0.45f, 0.18f), bollard, keepCollider: true);
                for (int i = 0; i < 3; i++)
                    SnowMound(group, new Vector3(x - 2f, RoadTop, road.yMin + 2f + i * 3f), new Vector3(2.6f, 0.6f, 2f), Range(rng, 0f, 360f));
            }

            // El borde norte no lleva valla: al otro lado del callejón n3 sigue la ampliación norte (HousesNorth.cs).
        }
    }
}
