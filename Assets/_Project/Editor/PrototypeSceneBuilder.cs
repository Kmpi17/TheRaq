using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Genera la escena prototipo: una manzana en bloques grises a escala real (estilo Southside de
    /// Chicago en invierno), el jugador, un coche conducible y la cámara. Menú: GTAChiraq > Construir escena prototipo.
    /// Se puede volver a ejecutar cuando se quiera: sobrescribe la escena y reutiliza los materiales.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Prototype.unity";
        const string MaterialsDir = Root + "/Materials";
        const string SettingsDir = Root + "/Settings";
        const int IgnoreRaycastLayer = 2;

        // Manzana interior: x ∈ [-BlockHalfX, BlockHalfX], z ∈ [-BlockHalfZ, BlockHalfZ].
        const float BlockHalfX = 100f;
        const float BlockHalfZ = 70f;
        const float SidewalkWidth = 3f;
        const float RoadWidth = 14f;

        // Barrio de viviendas texturizado, al este de la avenida: x ∈ [DistrictMinX, DistrictMaxX].
        const float DistrictMinX = BlockHalfX + SidewalkWidth * 2f + RoadWidth; // 120
        const float DistrictMaxX = 250f;
        const float DistrictHalfZ = 88f;
        const float DistrictRoadWidth = 12f;
        const float DistrictLawnInner = DistrictRoadWidth / 2f + SidewalkWidth; // donde acaba la acera y empieza el césped
        const float DistrictWalkupZ = DistrictLawnInner + 14f;
        const float DistrictTowerZ = 62f;
        static readonly float[] DistrictWalkupsX = { 136f, 176f, 216f };
        static readonly float[] DistrictTowersX = { 156f, 196f }; // en los huecos entre bloques bajos, para que se vea el camino
        static readonly Vector3 WalkupSize = new(24f, 9f, 12f);
        static readonly Vector3 TowerSize = new(30f, 24f, 15f);
        static readonly float[] DistrictSides = { 1f, -1f };

        static readonly Color FogColor = new(0.56f, 0.58f, 0.62f);
        static readonly Color SodiumLight = new(1f, 0.62f, 0.28f);

        [MenuItem("GTAChiraq/Construir escena prototipo")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MaterialsDir);
            EnsureUrp();
            PrepareCars(); // PrototypeSceneBuilder.Cars.cs: los coches salen en todas las zonas

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();

            var world = new GameObject("Mundo").transform;
            BuildStreets(world);
            BuildSurroundings(world);
            BuildProjectsDistrict(world);
            BuildHousesDistrict(world); // PrototypeSceneBuilder.Houses.cs (después del barrio: usa sus materiales)
            BuildTowersBlock(world);    // PrototypeSceneBuilder.Towers.cs: la manzana central (usa los materiales de los dos barrios)
            BuildHighway(world);        // PrototypeSceneBuilder.Highway.cs: autopista hundida en el borde oeste (el tráfico va fuera de "Mundo")
            BuildSchool(world);         // PrototypeSceneBuilder.School.cs: instituto entre la autopista y la calle x = −110
            BuildElevatedTrain(world);  // PrototypeSceneBuilder.Train.cs (los trenes quedan fuera de "Mundo": se mueven)
            BuildRailYard(world);       // PrototypeSceneBuilder.RailYard.cs: cocheras al sur de la calle z = −80 (usa los materiales del tren)
            BuildStreetLights(world);
            BuildSnow(world);
            BuildMainRoadDetails(world); // PrototypeSceneBuilder.MainRoads.cs: alcantarillas, sumideros, nieve y manchas
            BuildDrivableParkedCars();   // PrototypeSceneBuilder.Cars.cs: los aparcados salen de "Mundo" y se pueden robar
            MarkStatic(world);

            var player = BuildPlayer(new Vector3(-10f, 0.2f, -BlockHalfZ - 1.5f));
            BuildCar(new Vector3(-16f, 0.4f, -BlockHalfZ - SidewalkWidth - 3f), Quaternion.Euler(0f, -90f, 0f));
            BuildCamera(player);
            BuildTrainingArea(); // PrototypeSceneBuilder.Training.cs: dianas y objetos junto al inicio (fuera de "Mundo")

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Debug.Log($"[GTAChiraq] Escena prototipo generada en {ScenePath}. Pulsa Play: WASD, ratón, Shift, Espacio, F para el coche.");
        }

        // ---------- Render pipeline e iluminación ----------

        static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;

            EnsureFolder(SettingsDir);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, SettingsDir + "/URP_Renderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, SettingsDir + "/URP_Pipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("[GTAChiraq] URP configurado como render pipeline por defecto.");
        }

        static void SetupLighting()
        {
            var sun = new GameObject("Sol nublado").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.8f, 0.84f, 0.92f);
            sun.intensity = 0.7f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 260f;
        }

        // ---------- Mundo ----------

        static void BuildStreets(Transform world)
        {
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));

            // El suelo acaba en la autopista hundida (x −300…−275): ahí ponen el suelo los puentes y el techo del túnel (Highway.cs).
            // Lleva un hueco para la escalera del estudio del sótano de T1 (Studio.cs).
            foreach (var r in RectMinus(Rect.MinMaxRect(HighwayMaxX, -300f, 300f, 300f), new List<Rect> { StudioStairwell }))
                Box("Suelo", world, new Vector3(r.center.x, -0.1f, r.center.y), new Vector3(r.width, 0.2f, r.height), asphalt);

            // El césped y la acera de la manzana central los pone BuildTowersBlock (PrototypeSceneBuilder.Towers.cs).
            float outerX = BlockHalfX + SidewalkWidth, outerZ = BlockHalfZ + SidewalkWidth;

            // Aceras de enfrente, al otro lado de la calzada.
            float farZ = outerZ + RoadWidth + SidewalkWidth / 2f, farX = outerX + RoadWidth + SidewalkWidth / 2f;
            // Las de enfrente se cortan donde las cruza la otra calle grande (la avenida y la calle x = −110 para las del
            // norte y sur; las calles z = ±80 para la del oeste): si no, quedaría un bordillo atravesando la calzada.
            var avenueCuts = new List<Rect> { Rect.MinMaxRect(outerX, -300f, outerX + RoadWidth, 300f), Rect.MinMaxRect(-outerX - RoadWidth, -300f, -outerX, 300f) };
            // La del norte se corta también en la entrada del aparcamiento de los negocios y en el pasaje al callejón (Shops.cs).
            var northStrip = Rect.MinMaxRect(-300f, farZ - SidewalkWidth / 2f, 300f, farZ + SidewalkWidth / 2f);
            var northCuts = new List<Rect>(avenueCuts) { ShopsLotDriveway, ShopsAlleyLink };
            northCuts.AddRange(FarWestNorthSidewalkCuts); // calle J y entradas de la gasolinera (FarWest.cs)
            foreach (var piece in StripMinus(northStrip, northCuts))
                Box("AceraEnfrente_N", world, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height), concrete);
            var southStrip = Rect.MinMaxRect(-300f, -farZ - SidewalkWidth / 2f, 300f, -farZ + SidewalkWidth / 2f);
            foreach (var piece in StripMinus(southStrip, avenueCuts))
                Box("AceraEnfrente_S", world, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height), concrete);
            // La acera de enfrente este se corta donde entra la calle del barrio de viviendas.
            float cut = DistrictRoadWidth / 2f, half = (300f - cut) / 2f;
            // Al norte, también se corta donde entran las calles A y C1 del barrio de casas y la Fe de la ampliación norte.
            float north = cut;
            var avenueRoads = new List<Rect>(HousesRoads);
            avenueRoads.AddRange(NorthAvenueRoadsEast);
            avenueRoads.Sort((p, q) => p.yMin.CompareTo(q.yMin));
            foreach (var r in avenueRoads)
            {
                if (r.xMin > farX) continue; // solo las que salen de la avenida
                Box("AceraEnfrente_E_N", world, new Vector3(farX, 0.075f, (north + r.yMin) / 2f), new Vector3(SidewalkWidth, 0.15f, r.yMin - north), concrete);
                north = r.yMax;
            }
            Box("AceraEnfrente_E_N", world, new Vector3(farX, 0.075f, (north + 300f) / 2f), new Vector3(SidewalkWidth, 0.15f, 300f - north), concrete);
            Box("AceraEnfrente_E_S", world, new Vector3(farX, 0.075f, -cut - half), new Vector3(SidewalkWidth, 0.15f, half * 2f), concrete);
            var westStrip = Rect.MinMaxRect(-farX - SidewalkWidth / 2f, -300f, -farX + SidewalkWidth / 2f, 300f);
            var crossStreets = new List<Rect> { Rect.MinMaxRect(-300f, outerZ, 300f, outerZ + RoadWidth), Rect.MinMaxRect(-300f, -outerZ - RoadWidth, 300f, -outerZ) };
            crossStreets.AddRange(FarWestWestSidewalkCuts); // calles D, E, F, H del oeste lejano y entrada de la gasolinera
            crossStreets.AddRange(SchoolDriveways);         // salidas del aparcamiento del instituto (School.cs)
            foreach (var piece in StripMinus(westStrip, crossStreets))
                Box("AceraEnfrente_O", world, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height), concrete);

            // Calzadas de las cuatro calles grandes con textura, cruces y pasos de cebra (PrototypeSceneBuilder.MainRoads.cs).
            BuildMainRoads(world);
        }

        static void BuildSurroundings(Transform world)
        {
            var group = new GameObject("Alrededores").transform;
            group.SetParent(world, false);

            var brick = Mat("Ladrillo", new Color(0.45f, 0.24f, 0.18f));
            var brickDark = Mat("LadrilloOscuro", new Color(0.36f, 0.2f, 0.16f));
            var siding = Mat("FachadaGris", new Color(0.52f, 0.52f, 0.5f));
            var materials = new[] { brick, brickDark, siding };

            // Filas de "two-flats" y casas al otro lado de las calles norte y sur.
            float rowZ = BlockHalfZ + SidewalkWidth * 2f + RoadWidth + 9f;
            float avenueX = BlockHalfX + SidewalkWidth + RoadWidth / 2f;
            var rng = new System.Random(42);
            int index = 0;
            for (float x = -200f; x <= 200f; x += 13f)
            {
                if (Mathf.Abs(Mathf.Abs(x) - avenueX) < 14f) continue; // deja libre el cruce
                foreach (float side in new[] { 1f, -1f })
                {
                    if (side > 0f) continue; // al norte: el oeste lejano, la ampliación oeste, los negocios y los barrios de casas
                    if (x > YardMinX - 5f && x < BlockHalfX + SidewalkWidth) continue; // al sur, las cocheras (RailYard.cs); quedan los de la esquina oeste

                    float height = rng.NextDouble() < 0.7 ? 8f : 5f;
                    var size = new Vector3(10f, height, 14f);
                    Building(group, $"Casa_{index++}", new Vector3(x, 0f, side * rowZ), size, materials[rng.Next(materials.Length)]);
                }
            }

            // Tienda de esquina.
            var sign = Mat("Letrero", new Color(0.1f, 0.25f, 0.6f));
            var store = Building(group, "TiendaEsquina", new Vector3(-avenueX - 20f, 0f, -rowZ), new Vector3(16f, 4.5f, 14f), siding);
            Box("Letrero", store, new Vector3(0f, 3.8f, 7.05f), new Vector3(12f, 1f, 0.2f), sign, collider: false);

            // Coches aparcados (estáticos) junto a la acera de enfrente, en el sentido de la marcha (circulación por la derecha).
            var carRng = new System.Random(4242);
            float parkedZ = BlockHalfZ + SidewalkWidth + RoadWidth - 1.5f;
            for (int i = 0; i < 5; i++)
            {
                float x = -80f + i * 37f;
                float side = i % 2 == 0 ? 1f : -1f;
                ParkedCar(group, new Vector3(x, 0f, side * parkedZ), side > 0f ? 180f : 0f, carRng);
            }
        }

        /// <summary>
        /// Barrio de viviendas sociales al este de la avenida, con texturas generadas (ProceduralTextures):
        /// calle con doble línea amarilla, aceras de losas, césped seco, bloques bajos de 3 plantas
        /// junto a la calle y torres de 8 plantas detrás, todo en ladrillo rojo con ventanas.
        /// </summary>
        static void BuildProjectsDistrict(Transform world)
        {
            var group = new GameObject("Barrio_Viviendas").transform;
            group.SetParent(world, false);

            var facade = TexturedMat("FachadaLadrillo", ProceduralTextures.BrickFacade("FachadaLadrillo", new Color(0.56f, 0.3f, 0.22f), false, 1));
            var facadeAc = TexturedMat("FachadaLadrilloAire", ProceduralTextures.BrickFacade("FachadaLadrilloAire", new Color(0.5f, 0.27f, 0.21f), true, 2));
            var road = TexturedMat("CalleBarrio", ProceduralTextures.Road("CalleBarrio", DistrictRoadWidth, 3));
            var sidewalk = TexturedMat("AceraBarrio", ProceduralTextures.Sidewalk("AceraBarrio", 4));
            var grass = TexturedMat("CespedSeco", ProceduralTextures.DeadGrass("CespedSeco", 5));
            var roof = TexturedMat("TejadoAlquitran", ProceduralTextures.Roof("TejadoAlquitran", 6));
            var trim = Mat("Cornisa", new Color(0.55f, 0.53f, 0.5f));
            var door = Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f));

            float halfRoad = DistrictRoadWidth / 2f;
            float lawnInner = halfRoad + SidewalkWidth;
            float length = DistrictMaxX - DistrictMinX, centerX = (DistrictMinX + DistrictMaxX) / 2f;

            // Calzada E–O por el centro. Girada 90° para que la U de la textura cruce la calle.
            // Empieza en la avenida para tapar el hueco de la acera de enfrente.
            float roadStart = DistrictMinX - SidewalkWidth, roadLength = DistrictMaxX - roadStart;
            var street = TexturedBox("Calle", group, new Vector3(roadStart + roadLength / 2f, 0.01f, 0f),
                new Vector3(DistrictRoadWidth, 0.02f, roadLength), road, Vector2.one * DistrictRoadWidth, collider: false);
            street.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            int index = 0;
            foreach (float side in new[] { 1f, -1f })
            {
                TexturedBox("Acera", group, new Vector3(centerX, 0.075f, side * (halfRoad + SidewalkWidth / 2f)),
                    new Vector3(length, 0.15f, SidewalkWidth), sidewalk, Vector2.one * ProceduralTextures.SidewalkTile);
                float lawnDepth = DistrictHalfZ - lawnInner;
                TexturedBox("Cesped", group, new Vector3(centerX, 0.05f, side * (lawnInner + lawnDepth / 2f)),
                    new Vector3(length, 0.1f, lawnDepth), grass, Vector2.one * ProceduralTextures.GrassTile);

                foreach (float x in DistrictWalkupsX)
                    ProjectBuilding(group, $"BloqueBajo_{index}", new Vector3(x, 0f, side * DistrictWalkupZ), WalkupSize,
                        index++ % 2 == 0 ? facade : facadeAc, roof, trim, door, sidewalk, lowRise: true);
                foreach (float x in DistrictTowersX)
                    ProjectBuilding(group, $"Torre_{index}", new Vector3(x, 0f, side * DistrictTowerZ), TowerSize,
                        index++ % 2 == 0 ? facadeAc : facade, roof, trim, door, sidewalk, lowRise: false);
            }

            // Árboles en el césped junto a la acera: pelados, con hojas verdes, nevados o con hoja seca de invierno
            // (PrototypeSceneBuilder.Vegetation.cs). El aspecto sale de otra semilla para no mover nada de sitio.
            PrepareVegetation();
            var bark = Mat("Corteza", new Color(0.25f, 0.2f, 0.16f));
            var rng = new System.Random(123);
            var looks = new System.Random(314);
            for (float x = DistrictMinX + 8f; x < DistrictMaxX - 4f; x += 18f)
                foreach (float side in new[] { 1f, -1f })
                    DistrictTree(group, new Vector3(x + Range(rng, -2f, 2f), 0.1f, side * (lawnInner + Range(rng, 1.5f, 3.5f))), bark, rng, 1f, looks);

            // Farolas de sodio en el bordillo.
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            for (float x = DistrictMinX + 15f; x < DistrictMaxX; x += 30f)
            {
                StreetLight(group, new Vector3(x, 0f, lawnInner - 0.5f), Vector3.back, pole, lamp);
                StreetLight(group, new Vector3(x, 0f, -lawnInner + 0.5f), Vector3.forward, pole, lamp);
            }

            // Un par de coches aparcados (estáticos).
            var carRng = new System.Random(4343);
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                ParkedCar(group, new Vector3(150f + i * 33f, RoadTop, side * (halfRoad - 1.3f)), side > 0f ? 180f : 0f, carRng);
            }

            BuildDistrictProps(group); // vallas, cancha, bancos, basura, grafitis... (PrototypeSceneBuilder.StreetProps.cs)
        }

        /// <summary>
        /// Edificio de ladrillo con la fachada texturizada (una ventana cada 3 m y por planta), portal con
        /// marquesina en la cara que mira a la calle (z = 0) y camino de hormigón hasta la acera.
        /// </summary>
        static void ProjectBuilding(Transform parent, string name, Vector3 basePosition, Vector3 size,
            Material wall, Material roof, Material trim, Material door, Material path, bool lowRise)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;

            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            TexturedBox("Muros", root, new Vector3(0f, size.y / 2f, 0f), size, wall, facadeTile, sidesOnly: true);

            var roofTile = Vector2.one * ProceduralTextures.RoofTile;
            if (lowRise)
            {
                // Tejado plano con alero, como los bloques bajos de la referencia.
                TexturedBox("Tejado", root, new Vector3(0f, size.y + 0.2f, 0f), new Vector3(size.x + 1.2f, 0.4f, size.z + 1.2f), roof, roofTile, collider: false);
            }
            else
            {
                Box("Cornisa", root, new Vector3(0f, size.y + 0.3f, 0f), new Vector3(size.x + 0.3f, 0.6f, size.z + 0.3f), trim, collider: false);
                TexturedBox("Tejado", root, new Vector3(0f, size.y + 0.62f, 0f), new Vector3(size.x - 0.4f, 0.04f, size.z - 0.4f), roof, roofTile, collider: false);
                Box("CasetaAscensor", root, new Vector3(size.x * 0.2f, size.y + 2.2f, 0f), new Vector3(5f, 3.2f, 4f), trim, collider: false);
            }

            // Portal: con un número par de vanos, x = 0 cae entre dos ventanas.
            float toward = -Mathf.Sign(basePosition.z);
            float faceZ = toward * size.z / 2f;
            Box("Puerta", root, new Vector3(0f, 1.25f, faceZ + toward * 0.06f), new Vector3(1.8f, 2.5f, 0.12f), door, collider: false);
            Box("Marquesina", root, new Vector3(0f, 2.75f, faceZ + toward * 0.6f), new Vector3(2.8f, 0.18f, 1.2f), trim, collider: false);
            Box("Escalon", root, new Vector3(0f, 0.15f, faceZ + toward * 0.7f), new Vector3(2.8f, 0.3f, 1.4f), trim);

            float sidewalkEdge = DistrictRoadWidth / 2f + SidewalkWidth;
            float pathLength = Mathf.Abs(basePosition.z) - size.z / 2f - 1.4f - sidewalkEdge;
            TexturedBox("Camino", root, new Vector3(0f, 0.06f, faceZ + toward * (1.4f + pathLength / 2f)), new Vector3(2.4f, 0.12f, pathLength),
                path, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
        }

        static Transform BareTree(Transform parent, Vector3 basePosition, Material bark, System.Random rng, float scale = 1f) =>
            BareTree(parent, basePosition, bark, rng, scale, out _);

        /// <summary>Árbol pelado: tronco y 5 ramas. Devuelve la raíz y, en <paramref name="height"/>, la altura del tronco.</summary>
        static Transform BareTree(Transform parent, Vector3 basePosition, Material bark, System.Random rng, float scale, out float height)
        {
            var root = new GameObject("ArbolPelado").transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;

            height = Range(rng, 4.5f, 6.5f) * scale;
            float thickness = Mathf.Sqrt(scale);
            Primitive(PrimitiveType.Cylinder, "Tronco", root, new Vector3(0f, height / 2f, 0f), new Vector3(0.35f * thickness, height / 2f, 0.35f * thickness), bark, keepCollider: true);
            for (int i = 0; i < 5; i++)
            {
                var rotation = Quaternion.Euler(0f, i * 72f + Range(rng, -20f, 20f), 0f) * Quaternion.Euler(Range(rng, 30f, 55f), 0f, 0f);
                float branchLength = Range(rng, 2f, 3.5f) * scale;
                var start = new Vector3(0f, height * Range(rng, 0.6f, 0.95f), 0f);
                var branch = Primitive(PrimitiveType.Cylinder, "Rama", root, start + rotation * Vector3.up * (branchLength / 2f),
                    new Vector3(0.12f * thickness, branchLength / 2f, 0.12f * thickness), bark, keepCollider: false);
                branch.transform.localRotation = rotation;
            }
            return root;
        }

        static void BuildStreetLights(Transform world)
        {
            var group = new GameObject("Farolas").transform;
            group.SetParent(world, false);

            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            lamp.EnableKeyword("_EMISSION");
            lamp.SetColor("_EmissionColor", SodiumLight * 2f);
            EditorUtility.SetDirty(lamp);

            float edgeZ = BlockHalfZ + SidewalkWidth - 0.5f;
            float edgeX = BlockHalfX + SidewalkWidth - 0.5f;
            for (float x = -90f; x <= 90f; x += 30f)
            {
                StreetLight(group, new Vector3(x, 0f, edgeZ), Vector3.forward, pole, lamp);
                StreetLight(group, new Vector3(x, 0f, -edgeZ), Vector3.back, pole, lamp);
            }
            for (float z = -60f; z <= 60f; z += 30f)
            {
                StreetLight(group, new Vector3(edgeX, 0f, z), Vector3.right, pole, lamp);
                StreetLight(group, new Vector3(-edgeX, 0f, z), Vector3.left, pole, lamp);
            }
        }

        static void StreetLight(Transform parent, Vector3 basePosition, Vector3 towardRoad, Material pole, Material lamp)
        {
            var root = new GameObject("Farola").transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;

            Primitive(PrimitiveType.Cylinder, "Poste", root, new Vector3(0f, 3.15f, 0f), new Vector3(0.18f, 3f, 0.18f), pole, keepCollider: true);
            Vector3 headPosition = new Vector3(0f, 6.1f, 0f) + towardRoad * 1.2f;
            Box("Brazo", root, new Vector3(0f, 6.1f, 0f) + towardRoad * 0.6f, Abs(towardRoad) * 1.2f + new Vector3(0.12f, 0.12f, 0.12f), pole, collider: false);
            Box("Lampara", root, headPosition, new Vector3(0.6f, 0.2f, 0.6f), lamp, collider: false);

            var light = new GameObject("Luz").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = headPosition - Vector3.up * 0.3f;
            light.type = LightType.Point;
            light.color = SodiumLight;
            light.range = 16f;
            light.intensity = 4f;
            light.shadows = LightShadows.None;
        }

        static void BuildSnow(Transform world)
        {
            var group = new GameObject("Nieve").transform;
            group.SetParent(world, false);
            var snow = Mat("NieveSucia", new Color(0.84f, 0.86f, 0.89f));
            var rng = new System.Random(99);

            float curbZ = BlockHalfZ + SidewalkWidth - 0.6f;
            for (int i = 0; i < 30; i++)
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                var size = new Vector3(Range(rng, 1.5f, 4f), Range(rng, 0.25f, 0.6f), Range(rng, 0.7f, 1.2f));
                var position = new Vector3(Range(rng, -95f, 95f), 0.15f + size.y / 2f, side * curbZ);
                if (InsideAny(TowersDriveways, position)) continue; // no tapar las entradas de los aparcamientos
                Box("Monton", group, position, size, snow, collider: false);
            }
        }

        // ---------- Jugador, coche y cámara ----------

        static GameObject BuildPlayer(Vector3 position)
        {
            var player = new GameObject("Jugador") { tag = "Player" };
            player.transform.position = position;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.4f;

            // Sistemas del personaje (Scripts/Player, Combat, Inventory). El orden importa poco: cada uno busca a los demás.
            player.AddComponent<Health>();
            player.AddComponent<PlayerVitals>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerClimb>();
            player.AddComponent<PlayerCover>();
            player.AddComponent<PlayerInventory>();
            var combat = player.AddComponent<PlayerCombat>();
            player.AddComponent<PlayerHud>();
            player.AddComponent<VehicleEntry>();
            player.AddComponent<PlayerInteractor>(); // ascensor, armario y caja fuerte (Scripts/Home)

            // Modelo (hijo propio: la cobertura lo agacha y al morir se tumba).
            var model = new GameObject("Modelo").transform;
            model.SetParent(player.transform, false);
            BuildPlayerBody(model);

            // Pistola de prueba (genérica) con la boca del cañón. La mano derecha la empuña (IK de PlayerAnimator), así
            // que ya no hay un puño suelto para pegar: los puñetazos los anima el brazo.
            var gunMetal = Mat("PistolaMetal", new Color(0.08f, 0.08f, 0.09f));
            var pistol = new GameObject("Pistola").transform;
            pistol.SetParent(model, false);
            pistol.localPosition = new Vector3(0.32f, 1.0f, 0.18f);
            Box("Corredera", pistol, new Vector3(0f, 0.05f, 0.04f), new Vector3(0.05f, 0.05f, 0.22f), gunMetal, collider: false);
            Box("Empunadura", pistol, new Vector3(0f, -0.04f, -0.04f), new Vector3(0.045f, 0.13f, 0.06f), gunMetal, collider: false)
                .transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            var muzzle = new GameObject("BocaCanon").transform;
            muzzle.SetParent(pistol, false);
            muzzle.localPosition = new Vector3(0f, 0.05f, 0.17f);
            pistol.gameObject.SetActive(false);
            SetReference(combat, "muzzle", muzzle);
            SetReference(combat, "pistolModel", pistol.gameObject);

            // La ropa se pinta al arrancar (PlayerOutfit busca las piezas de BuildPlayerBody por nombre) y las
            // articulaciones las mueve PlayerAnimator (andar, correr, cobertura, escalar, pistola, puñetazos…).
            var outfit = player.AddComponent<PlayerOutfit>();
            outfit.SetTextures(clothTextures, skinTexture);
            outfit.PreviewInEditor(); // vestido con la ropa de inicio también en el editor y las capturas
            EditorUtility.SetDirty(outfit);
            player.AddComponent<PlayerAnimator>();

            SetLayerRecursively(player, IgnoreRaycastLayer);
            return player;
        }

        static void BuildCar(Vector3 position, Quaternion rotation)
        {
            // Carrocería: el sedán grande de Cars.cs, con llantas cromadas y faros encendidos. Su suelo queda donde
            // apoyan las ruedas en reposo (CarGroundY). Es del jugador: no lleva VehicleLock.
            var spec = Spec(CarStyle.Sedan);
            float groundY = CarGroundY(spec), radius = spec.WheelRadius;
            var model = CarModel(null, "Modelo", position + Vector3.up * groundY, rotation.eulerAngles.y, CarStyle.Sedan, carPlayerPaint, new System.Random(5),
                wheels: false, weathered: false, lightsOn: true);
            float halfTrack = spec.Width / 2f - 0.12f, halfBase = spec.Wheelbase / 2f;
            foreach (float z in new[] { halfBase, -halfBase })
            foreach (float side in new[] { -1f, 1f })
                CarWheel(model, "Rueda", new Vector3(side * halfTrack, radius, z), radius, side, carChrome);
            var car = MakeDrivable(model, CarStyle.Sedan, "Coche_Sedan", null);

            foreach (float side in new[] { -1f, 1f })
            {
                var beam = new GameObject("LuzFaro").AddComponent<Light>();
                beam.transform.SetParent(car.transform, false);
                beam.transform.localPosition = new Vector3(side * 0.65f, groundY + 0.85f, spec.Length / 2f + 0.1f);
                beam.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
                beam.type = LightType.Spot;
                beam.color = new Color(1f, 0.95f, 0.82f);
                beam.range = 22f;
                beam.spotAngle = 55f;
                beam.intensity = 3f;
                beam.shadows = LightShadows.None;
                beam.gameObject.layer = GameLayers.Vehicle;
            }
        }

        static void BuildCamera(GameObject player)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(player.transform.position + new Vector3(0f, 2.5f, -4f), Quaternion.Euler(15f, 0f, 0f));

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FogColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            cameraObject.AddComponent<AudioListener>();

            var follow = cameraObject.AddComponent<ThirdPersonCamera>();
            SetReference(follow, "target", player.transform);
            SetReference(player.GetComponent<PlayerMovement>(), "cameraTransform", cameraObject.transform);
            SetReference(player.GetComponent<VehicleEntry>(), "followCamera", follow);
            SetReference(player.GetComponent<PlayerCombat>(), "followCamera", follow);
        }

        // ---------- Utilidades ----------

        static Transform Building(Transform parent, string name, Vector3 basePosition, Vector3 size, Material wall)
        {
            var window = Mat("FranjaVentanas", new Color(0.09f, 0.1f, 0.12f));
            var trim = Mat("Cornisa", new Color(0.55f, 0.53f, 0.5f));

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;

            Box("Muros", root, new Vector3(0f, size.y / 2f, 0f), size, wall);
            Box("Cornisa", root, new Vector3(0f, size.y + 0.2f, 0f), new Vector3(size.x + 0.4f, 0.4f, size.z + 0.4f), trim, collider: false);

            // Una franja oscura por planta simula las filas de ventanas.
            int floors = Mathf.Max(1, Mathf.FloorToInt(size.y / 3.2f));
            for (int i = 0; i < floors; i++)
            {
                float y = i * 3.2f + 1.7f;
                Box($"Ventanas_{i}", root, new Vector3(0f, y, 0f), new Vector3(size.x + 0.08f, 1.1f, size.z + 0.08f), window, collider: false);
            }
            return root;
        }

        static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool collider = true)
            => Primitive(PrimitiveType.Cube, name, parent, localPosition, size, material, collider);

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) throw new System.InvalidOperationException("No se encuentra el shader URP/Lit. ¿Está instalado el paquete URP?");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material TexturedMat(string name, Texture2D texture, Color? tint = null)
        {
            var material = Mat(name, tint ?? Color.white);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Caja con malla propia y UVs en metros: cada cara repite la textura cada <paramref name="tile"/> metros
        /// (U, V), redondeado a un número entero de repeticiones para que ventanas y juntas cuadren en las esquinas.
        /// En las caras laterales V empieza en el suelo; en la de arriba U va por X y V por Z (locales).
        /// </summary>
        static GameObject TexturedBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, Vector2 tile,
            bool collider = true, bool sidesOnly = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(name, size, tile, sidesOnly);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        static Mesh BoxMesh(string name, Vector3 size, Vector2 tile, bool sidesOnly)
        {
            var faces = new List<(Vector3 normal, Vector3 up)>
            {
                (Vector3.forward, Vector3.up), (Vector3.back, Vector3.up),
                (Vector3.right, Vector3.up), (Vector3.left, Vector3.up),
            };
            if (!sidesOnly)
            {
                faces.Add((Vector3.up, Vector3.forward));
                faces.Add((Vector3.down, Vector3.back));
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3 half = size / 2f;
            foreach (var (normal, up) in faces)
            {
                // "right" es la derecha de quien mira la cara desde fuera, para que la textura no salga en espejo.
                Vector3 right = Vector3.Cross(up, -normal);
                float hn = Mathf.Abs(Vector3.Dot(half, normal));
                float hr = Mathf.Abs(Vector3.Dot(half, right));
                float hu = Mathf.Abs(Vector3.Dot(half, up));
                float tilesU = Mathf.Max(1f, Mathf.Round(2f * hr / tile.x));
                float tilesV = Mathf.Max(1f, Mathf.Round(2f * hu / tile.y));

                int start = vertices.Count;
                for (int i = 0; i < 4; i++) // abajo-izq, abajo-der, arriba-der, arriba-izq
                {
                    float su = i == 1 || i == 2 ? 1f : 0f;
                    float sv = i >= 2 ? 1f : 0f;
                    vertices.Add(normal * hn + right * ((su * 2f - 1f) * hr) + up * ((sv * 2f - 1f) * hu));
                    normals.Add(normal);
                    uvs.Add(new Vector2(su * tilesU, sv * tilesV));
                }
                // Sentido horario visto desde fuera (cara frontal en Unity).
                triangles.AddRange(new[] { start, start + 3, start + 2, start, start + 2, start + 1 });
            }

            var mesh = new Mesh { name = $"{name}_{size.x:0.#}x{size.y:0.#}x{size.z:0.#}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static void SetReference(Object component, string property, Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static void MarkStatic(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
