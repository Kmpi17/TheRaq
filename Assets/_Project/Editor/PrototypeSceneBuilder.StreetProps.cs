using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Detalle callejero del barrio de viviendas: vallas de hierro, cancha de baloncesto con malla metálica,
    /// plaza con bancos, parada de bus, postes de madera con cables (y unas zapatillas colgadas), arbustos,
    /// árboles, contenedores, bolsas de basura, desperdicios, manchas, charcos, grafitis y un coche abandonado.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float LawnTop = 0.1f, SidewalkTop = 0.15f, RoadTop = 0.02f;

        // Cancha al norte y plaza al sur, en el extremo este del barrio.
        static readonly Vector3 CourtCenter = new(232f, 0f, 58f);
        static readonly Vector2 CourtSize = new(12f, 20f);
        const float CourtMargin = 1.5f;
        static readonly Vector3 PlazaCenter = new(232f, 0f, -58f);
        static readonly Vector3 WreckPosition = new(241f, 0f, -34f);

        static void BuildDistrictProps(Transform district)
        {
            var group = new GameObject("Detalle_Callejero").transform;
            group.SetParent(district, false);
            var rng = new System.Random(2012);
            var occupied = DistrictOccupiedAreas();

            BuildFences(group);
            BuildCourt(group);
            BuildPlaza(group);
            BuildWreck(group, rng);
            BuildBusStop(group);
            BuildUtilityPoles(group);
            BuildBushes(group, rng);
            BuildTowerApproaches(group, occupied); // antes de los árboles: reserva su espacio en `occupied`
            BuildLawnTrees(group, rng, occupied);
            BuildDumpsters(group, rng);
            BuildTrash(group, rng, occupied);
            BuildGrime(group, rng, occupied);
            BuildGraffiti(group, rng);
            BuildStreetFurniture(group);
            BuildSnowRemnants(group, occupied); // PrototypeSceneBuilder.Vegetation.cs
            BuildOvergrowth(group, occupied);
        }

        // ---------- Zonas ocupadas (para no poner árboles ni basura dentro de los edificios) ----------

        static List<Rect> DistrictOccupiedAreas()
        {
            var list = new List<Rect>();
            foreach (float side in DistrictSides)
            {
                foreach (float x in DistrictWalkupsX) AddBuildingArea(list, x, side * DistrictWalkupZ, WalkupSize);
                foreach (float x in DistrictTowersX) AddBuildingArea(list, x, side * DistrictTowerZ, TowerSize);
            }
            list.Add(RectAround(CourtCenter.x, CourtCenter.z, CourtSize.x + 2f * CourtMargin + 4f, CourtSize.y + 2f * CourtMargin + 4f));
            list.Add(RectAround(PlazaCenter.x, PlazaCenter.z, 18f, 20f));
            list.Add(RectAround(WreckPosition.x, WreckPosition.z, 8f, 8f));
            foreach (float side in DistrictSides)
                foreach (float x in DistrictTowersX)
                    list.Add(DumpsterArea(x, side));
            return list;
        }

        static void AddBuildingArea(List<Rect> list, float x, float z, Vector3 size)
        {
            list.Add(RectAround(x, z, size.x + 4f, size.z + 4f));
            // Camino desde el portal hasta la acera.
            float face = Mathf.Abs(z) - size.z / 2f;
            list.Add(RectAround(x, Mathf.Sign(z) * (DistrictLawnInner + face) / 2f, 4f, face - DistrictLawnInner));
        }

        // Fachada de las torres que da a la calle y comienzo de la explanada delante del portal (|z|).
        static float TowerFaceZ => DistrictTowerZ - TowerSize.z / 2f;
        static float ForecourtStartZ => TowerFaceZ - 7.4f;
        const float ApproachStartZ = DistrictLawnInner + 0.4f;

        static Vector3 DumpsterPad(float towerX, float side) => new(towerX + TowerSize.x / 2f + 3f, LawnTop, side * (DistrictTowerZ - 3f));

        static Rect DumpsterArea(float towerX, float side)
        {
            var pad = DumpsterPad(towerX, side);
            return RectAround(pad.x - 1f, pad.z, 8f, 12f);
        }

        static Rect RectAround(float cx, float cz, float width, float depth) => new(cx - width / 2f, cz - depth / 2f, width, depth);

        static bool IsFree(List<Rect> occupied, float x, float z)
        {
            foreach (var r in occupied) if (r.Contains(new Vector2(x, z))) return false;
            return true;
        }

        /// <summary>Punto libre al azar sobre el césped, a una distancia mínima de la acera.</summary>
        static bool RandomLawnPoint(System.Random rng, List<Rect> occupied, float minFromSidewalk, out Vector3 point)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                float x = Range(rng, DistrictMinX + 2f, DistrictMaxX - 2f);
                float z = side * Range(rng, DistrictLawnInner + minFromSidewalk, DistrictHalfZ - 2f);
                if (!IsFree(occupied, x, z)) continue;
                point = new Vector3(x, LawnTop, z);
                return true;
            }
            point = default;
            return false;
        }

        // ---------- Vallas ----------

        static void BuildFences(Transform parent)
        {
            var fence = CutoutMat("VallaHierro", ProceduralTextures.IronFence("VallaHierro"));
            var iron = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            var gaps = new List<float>(DistrictWalkupsX);
            gaps.AddRange(DistrictTowersX);
            gaps.Sort();

            var tile = new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight);
            foreach (float side in DistrictSides)
            {
                // Valla de barrotes entre la acera y el césped, abierta donde llegan los caminos.
                float z = side * (DistrictLawnInner + 0.25f);
                float start = DistrictMinX + 0.3f;
                foreach (float gap in gaps)
                {
                    FenceSegment(parent, new Vector3(start, LawnTop, z), new Vector3(gap - 1.7f, LawnTop, z),
                        ProceduralTextures.IronFenceHeight, fence, tile, iron, 6f, 0.09f, topRail: false);
                    start = gap + 1.7f;
                }
                FenceSegment(parent, new Vector3(start, LawnTop, z), new Vector3(DistrictMaxX - 0.3f, LawnTop, z),
                    ProceduralTextures.IronFenceHeight, fence, tile, iron, 6f, 0.09f, topRail: false);
            }
        }

        /// <summary>Tramo de valla recto de a a b (a ras de suelo): panel con textura recortada, postes y barandilla opcional.</summary>
        static void FenceSegment(Transform parent, Vector3 a, Vector3 b, float height, Material panel, Vector2 tile,
            Material post, float postSpacing, float postSize, bool topRail)
        {
            Vector3 d = b - a;
            float length = d.magnitude;
            if (length < 0.3f) return;

            var wall = TexturedBox("Valla", parent, (a + b) / 2f + Vector3.up * (height / 2f), new Vector3(length, height, 0.03f), panel, tile, sidesOnly: true);
            wall.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg, 0f);

            int posts = Mathf.Max(1, Mathf.CeilToInt(length / postSpacing));
            float postHeight = height + 0.1f;
            for (int i = 0; i <= posts; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / posts);
                Primitive(PrimitiveType.Cylinder, "Poste", parent, p + Vector3.up * (postHeight / 2f), new Vector3(postSize, postHeight / 2f, postSize), post, keepCollider: false);
            }
            if (topRail) Strut("Barandilla", parent, a + Vector3.up * height, b + Vector3.up * height, 0.05f, post, cylinder: true);
        }

        // ---------- Cancha de baloncesto ----------

        static void BuildCourt(Transform parent)
        {
            var root = new GameObject("CanchaBaloncesto").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(CourtCenter.x, LawnTop, CourtCenter.z);

            var size = CourtSize + Vector2.one * (2f * CourtMargin);
            var court = TexturedMat("PistaBaloncesto", ProceduralTextures.Court("PistaBaloncesto", CourtSize.x, CourtSize.y, CourtMargin, 31));
            TexturedBox("Pista", root, new Vector3(0f, 0.02f, 0f), new Vector3(size.x, 0.04f, size.y), court, size);

            var pole = Mat("PosteCanasta", new Color(0.16f, 0.22f, 0.32f));
            var board = Mat("Tablero", new Color(0.85f, 0.85f, 0.82f));
            var rim = Mat("Aro", new Color(0.85f, 0.35f, 0.08f));
            var chain = Mat("Cadena", new Color(0.55f, 0.56f, 0.58f));
            Hoop(root, 1f, pole, board, rim, chain);
            Hoop(root, -1f, pole, board, rim, chain);

            // Malla metálica alrededor, con la puerta hacia la calle.
            var mesh = CutoutMat("MallaMetalica", ProceduralTextures.ChainLink("MallaMetalica", 41));
            var galvanized = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            float hx = size.x / 2f + 0.5f, hz = size.y / 2f + 0.5f, h = 3.5f;
            float toStreet = -Mathf.Sign(CourtCenter.z);
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            Vector3 P(float x, float z) => new(x, 0f, z);
            FenceSegment(root, P(-hx, -hz), P(-hx, hz), h, mesh, tile, galvanized, 3f, 0.07f, true);
            FenceSegment(root, P(hx, -hz), P(hx, hz), h, mesh, tile, galvanized, 3f, 0.07f, true);
            FenceSegment(root, P(-hx, -toStreet * hz), P(hx, -toStreet * hz), h, mesh, tile, galvanized, 3f, 0.07f, true);
            FenceSegment(root, P(-hx, toStreet * hz), P(-hx + 2f, toStreet * hz), h, mesh, tile, galvanized, 3f, 0.07f, true);
            FenceSegment(root, P(-hx + 4f, toStreet * hz), P(hx, toStreet * hz), h, mesh, tile, galvanized, 3f, 0.07f, true);

            // Bancos fuera de la malla, mirando a la pista.
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            Bench(root, new Vector3(-hx - 1.2f, 0f, -4f), -90f, wood, concrete);
            Bench(root, new Vector3(-hx - 1.2f, 0f, 4f), -90f, wood, concrete);
            TrashCan(root, new Vector3(-hx - 1.2f, 0f, 0f), 0f, tipped: false);
        }

        /// <summary>Canasta en el fondo <paramref name="end"/> (+1 norte, −1 sur): poste, tablero, aro y red de cadenas.</summary>
        static void Hoop(Transform court, float end, Material pole, Material board, Material rim, Material chain)
        {
            float baseline = end * CourtSize.y / 2f;
            float poleZ = baseline + end * 0.6f, boardZ = baseline - end * 1.2f;
            Primitive(PrimitiveType.Cylinder, "PosteCanasta", court, new Vector3(0f, 1.95f, poleZ), new Vector3(0.18f, 1.95f, 0.18f), pole, keepCollider: true);
            Strut("Brazo", court, new Vector3(0f, 3.4f, poleZ), new Vector3(0f, 3.4f, boardZ), 0.12f, pole);
            Box("Tablero", court, new Vector3(0f, 3.45f, boardZ), new Vector3(1.8f, 1.05f, 0.05f), board, collider: false);

            var center = new Vector3(0f, 3.05f, boardZ - end * 0.38f);
            Box("SoporteAro", court, new Vector3(0f, 3.05f, boardZ - end * 0.08f), new Vector3(0.12f, 0.03f, 0.16f), rim, collider: false);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.23f;
                var segment = Box("Aro", court, p, new Vector3(0.025f, 0.025f, 0.13f), rim, collider: false);
                segment.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            // Red de cadenas, típica de las canchas de barrio.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Strut("Cadena", court, center + dir * 0.22f, center + dir * 0.13f + Vector3.down * 0.42f, 0.015f, chain);
            }
        }

        // ---------- Plaza, coche abandonado y parada de bus ----------

        static void BuildPlaza(Transform parent)
        {
            var root = new GameObject("Plaza").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(PlazaCenter.x, LawnTop, PlazaCenter.z);

            var sidewalk = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/AceraBarrio.mat"); // creado en BuildProjectsDistrict
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            TexturedBox("Suelo", root, new Vector3(0f, 0.03f, 0f), new Vector3(14f, 0.06f, 16f), sidewalk, Vector2.one * ProceduralTextures.SidewalkTile);

            // Muro bajo al fondo (lado contrario a la calle), lleno de grafitis.
            float back = Mathf.Sign(PlazaCenter.z) * 7.6f;
            Box("Muro", root, new Vector3(0f, 0.9f, back), new Vector3(10f, 1.8f, 0.3f), concrete);

            Bench(root, new Vector3(-4.5f, 0.06f, 2f), -90f, wood, concrete);
            Bench(root, new Vector3(-4.5f, 0.06f, -3f), -90f, wood, concrete);
            Bench(root, new Vector3(4.5f, 0.06f, 2f), 90f, wood, concrete);
            Bench(root, new Vector3(4.5f, 0.06f, -3f), 90f, wood, concrete);

            // Mesa de picnic.
            var table = new GameObject("MesaPicnic").transform;
            table.SetParent(root, false);
            table.localPosition = new Vector3(0f, 0.06f, -0.5f);
            Box("Tablero", table, new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 0.06f, 0.8f), wood);
            Box("AsientoA", table, new Vector3(0f, 0.45f, 0.65f), new Vector3(1.9f, 0.05f, 0.3f), wood);
            Box("AsientoB", table, new Vector3(0f, 0.45f, -0.65f), new Vector3(1.9f, 0.05f, 0.3f), wood);
            foreach (float x in new[] { -0.75f, 0.75f })
                Box("Pata", table, new Vector3(x, 0.37f, 0f), new Vector3(0.08f, 0.75f, 1.5f), wood, collider: false);

            TrashCan(root, new Vector3(6f, 0.06f, 6f), 0f, tipped: false);
            TrashCan(root, new Vector3(-6f, 0.06f, -6f), 30f, tipped: true);
        }

        static void BuildWreck(Transform parent, System.Random rng)
        {
            // Coche abandonado sin ruedas, sobre bloques de hormigón.
            var root = new GameObject("CocheAbandonado").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(WreckPosition.x, LawnTop, WreckPosition.z);
            root.localRotation = Quaternion.Euler(0f, 25f, 0f);

            var rust = Mat("PinturaOxido", new Color(0.38f, 0.2f, 0.12f));
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));

            // El sedán de Cars.cs, oxidado y sin ruedas, con los bajos apoyados en los bloques (0.4 m) y algo torcido.
            var spec = Spec(CarStyle.Sedan);
            var body = CarModel(root, "Carroceria", new Vector3(0f, 0.4f - spec.Clearance, 0f), 0f, CarStyle.Sedan, rust, rng, wheels: false, weathered: false);
            body.localRotation = Quaternion.Euler(2f, 0f, -3f);
            // Capó abierto: bisagra en la base del parabrisas, levantado 35°.
            Box("Capo", body, new Vector3(0f, spec.Belt + 0.34f, spec.CabinFrontBase + 0.49f), new Vector3(spec.Width - 0.2f, 0.05f, 1.2f), rust, collider: false)
                .transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            foreach (var p in new[] { new Vector3(-0.8f, 0.2f, 1.45f), new Vector3(0.8f, 0.2f, 1.45f), new Vector3(-0.8f, 0.2f, -1.45f), new Vector3(0.8f, 0.2f, -1.45f) })
                Box("Bloque", root, p, new Vector3(0.4f, 0.4f, 0.2f), concrete, collider: false);

            // Neumáticos tirados alrededor.
            for (int i = 0; i < 4; i++)
            {
                var p = i switch
                {
                    0 => new Vector3(2.2f, 0.07f, 0.3f),
                    3 => new Vector3(2.2f, 0.21f, 0.3f), // encima del primero
                    _ => new Vector3(-Range(rng, 1.6f, 3f), 0.07f, Range(rng, -2.5f, 2.5f)),
                };
                Primitive(PrimitiveType.Cylinder, "Neumatico", root, p, new Vector3(0.7f, 0.07f, 0.7f), tire, keepCollider: false);
            }
        }

        static void BuildBusStop(Transform parent)
        {
            var root = new GameObject("ParadaBus").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(DistrictMinX + 9f, SidewalkTop, DistrictLawnInner - 1.1f);

            var frame = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var glass = Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            var sign = Mat("Letrero", new Color(0.1f, 0.25f, 0.6f));
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));

            foreach (float x in new[] { -1.7f, 1.7f })
            foreach (float z in new[] { -0.65f, 0.65f })
                Box("Pilar", root, new Vector3(x, 1.2f, z), new Vector3(0.07f, 2.4f, 0.07f), frame, collider: false);
            Box("Techo", root, new Vector3(0f, 2.43f, 0f), new Vector3(3.7f, 0.08f, 1.6f), frame, collider: false);
            Box("Fondo", root, new Vector3(0f, 1.25f, 0.65f), new Vector3(3.4f, 1.9f, 0.04f), glass);
            Box("Anuncio", root, new Vector3(1.7f, 1.25f, 0f), new Vector3(0.06f, 1.7f, 1.2f), sign);
            Bench(root, new Vector3(-0.4f, 0f, 0.25f), 0f, wood, concrete);

            // Señal de parada junto al bordillo.
            Primitive(PrimitiveType.Cylinder, "PosteSenal", root, new Vector3(-3.5f, 1.3f, -1.5f), new Vector3(0.06f, 1.3f, 0.06f), frame, keepCollider: true);
            Box("Senal", root, new Vector3(-3.5f, 2.35f, -1.5f), new Vector3(0.45f, 0.6f, 0.03f), sign, collider: false);
        }

        // ---------- Postes de madera y cables ----------

        static void BuildUtilityPoles(Transform parent)
        {
            var group = new GameObject("PostesLuz").transform;
            group.SetParent(parent, false);
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var metal = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            var shoe = Mat("Zapatilla", new Color(0.88f, 0.88f, 0.86f));

            const float height = 9.5f, armY = 8.9f;
            float z = -(DistrictRoadWidth / 2f + 0.5f); // acera sur, junto al bordillo
            var poles = new List<Vector3>();
            for (float x = DistrictMinX + 7f; x < DistrictMaxX; x += 32f) poles.Add(new Vector3(x, SidewalkTop, z));

            for (int i = 0; i < poles.Count; i++)
            {
                var p = poles[i];
                Primitive(PrimitiveType.Cylinder, "Poste", group, p + Vector3.up * (height / 2f), new Vector3(0.28f, height / 2f, 0.28f), wood, keepCollider: true);
                Box("Cruceta", group, p + Vector3.up * armY, new Vector3(0.12f, 0.12f, 2.2f), wood, collider: false);
                if (i % 2 == 1) // transformador
                    Primitive(PrimitiveType.Cylinder, "Transformador", group, p + new Vector3(0f, 7.4f, 0.4f), new Vector3(0.5f, 0.45f, 0.5f), metal, keepCollider: false);
            }

            float[] offsets = { -0.95f, 0f, 0.95f };
            for (int i = 0; i + 1 < poles.Count; i++)
                foreach (float o in offsets)
                    Wire(group, poles[i] + new Vector3(0f, armY + 0.08f, o), poles[i + 1] + new Vector3(0f, armY + 0.08f, o), 0.7f, cable);

            // Unas zapatillas colgadas del cable central, en mitad de un vano.
            if (poles.Count >= 3)
            {
                var mid = (poles[1] + poles[2]) / 2f + new Vector3(0f, armY + 0.08f - 0.7f, 0f);
                foreach (float dx in new[] { -0.12f, 0.12f })
                {
                    var bottom = mid + new Vector3(dx, -0.55f, 0f);
                    Strut("Cordon", group, mid, bottom, 0.012f, cable);
                    Box("Zapatilla", group, bottom + Vector3.down * 0.06f, new Vector3(0.11f, 0.12f, 0.28f), shoe, collider: false)
                        .transform.localRotation = Quaternion.Euler(70f, dx * 100f, 0f);
                }
            }
        }

        /// <summary>Cable colgante entre dos puntos (LineRenderer con catenaria aproximada por una parábola).</summary>
        static void Wire(Transform parent, Vector3 a, Vector3 b, float sag, Material material)
        {
            var go = new GameObject("Cable");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            const int points = 12;
            line.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float t = i / (points - 1f);
                line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t)));
            }
            line.widthMultiplier = 0.035f;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Accesos a las torres ----------

        /// <summary>
        /// Camino largo desde la acera hasta cada torre: pilonas y tablón de anuncios al empezar, barandilla baja,
        /// farolas de parque y bancos a lo largo, y delante del portal una explanada con jardineras, bancos,
        /// aparcabicis, papelera y una cámara de vigilancia con luz azul.
        /// </summary>
        static void BuildTowerApproaches(Transform parent, List<Rect> occupied)
        {
            var group = new GameObject("AccesosTorres").transform;
            group.SetParent(parent, false);
            var rng = new System.Random(77); // propia, para no mover el resto del barrio

            var paving = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/AceraBarrio.mat");
            var bush = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Arbusto.mat");
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var soil = Mat("Tierra", new Color(0.24f, 0.18f, 0.13f));
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var iron = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            var galvanized = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var cork = Mat("TablonCorcho", new Color(0.55f, 0.42f, 0.28f));
            var paper = Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f));
            var lampGlow = EmissiveMat("LuzFarolaParque", new Color(1f, 0.78f, 0.5f), 2.5f);
            var blueGlow = EmissiveMat("LuzAzulCamara", new Color(0.15f, 0.35f, 1f), 3f);

            float pathEnd = TowerFaceZ - 1.4f, forecourt = ForecourtStartZ;
            foreach (float side in DistrictSides)
            foreach (float tx in DistrictTowersX)
            {
                Vector3 At(float dx, float absZ, float y = LawnTop) => new(tx + dx, y, side * absZ);
                float FacingPath(float dx) => dx < 0f ? -90f : 90f; // los bancos miran hacia el eje del camino

                // Explanada delante del portal.
                float forecourtMid = (forecourt + pathEnd + 1.2f) / 2f, forecourtDepth = pathEnd + 1.2f - forecourt;
                TexturedBox("Explanada", group, At(0f, forecourtMid, LawnTop + 0.025f), new Vector3(11f, 0.05f, forecourtDepth), paving,
                    Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
                foreach (float dx in new[] { -4.3f, 4.3f })
                    Planter(group, At(dx, pathEnd - 0.6f, LawnTop + 0.05f), concrete, soil, bush, rng);
                Bench(group, At(-3.8f, forecourt + 1.6f, LawnTop + 0.05f), FacingPath(-1f), wood, concrete);
                Bench(group, At(3.8f, forecourt + 1.6f, LawnTop + 0.05f), FacingPath(1f), wood, concrete);
                BikeRack(group, At(4.4f, forecourt + 3.1f, LawnTop + 0.05f), galvanized);
                TrashCan(group, At(-5f, forecourt + 3.4f, LawnTop + 0.05f), 0f, tipped: false);
                SecurityCamera(group, At(5.2f, forecourt + 0.4f), galvanized, blueGlow);

                // Al empezar el camino: pilonas de hormigón (que no entren coches) y tablón de anuncios.
                foreach (float dx in new[] { -0.8f, 0f, 0.8f })
                    Primitive(PrimitiveType.Cylinder, "PilonaHormigon", group, At(dx, ApproachStartZ + 0.6f, LawnTop + 0.4f), new Vector3(0.28f, 0.4f, 0.28f), concrete, keepCollider: true);
                NoticeBoard(group, At(-2.4f, ApproachStartZ + 2.6f), FacingPath(-1f), wood, cork, paper, rng);

                // Barandilla baja a los dos lados, con huecos donde están los bancos.
                const float benchZ = 30f, gap = 1.6f;
                foreach (float dx in new[] { -1.45f, 1.45f })
                {
                    LowRail(group, At(dx, ApproachStartZ + 1.4f), At(dx, benchZ - gap), iron);
                    LowRail(group, At(dx, benchZ + gap), At(dx, forecourt - 0.2f), iron);
                    TexturedBox("LosaBanco", group, At(dx * 1.8f, benchZ, LawnTop + 0.02f), new Vector3(1.4f, 0.04f, 2.4f), paving,
                        Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
                    Bench(group, At(dx * 1.8f, benchZ, LawnTop + 0.04f), FacingPath(dx), wood, concrete);
                }
                TrashCan(group, At(2.6f, benchZ + 1.9f), 0f, tipped: false);

                // Farolas de parque alternando lados.
                int n = 0;
                for (float z = ApproachStartZ + 7f; z < forecourt - 2f; z += 11f)
                    ParkLamp(group, At(n++ % 2 == 0 ? 2.1f : -2.1f, z), iron, lampGlow);

                occupied.Add(RectAround(tx, side * (ApproachStartZ + forecourt) / 2f, 8f, forecourt - ApproachStartZ));
                occupied.Add(RectAround(tx, side * forecourtMid, 14f, forecourtDepth + 2f));
            }
        }

        static void Planter(Transform parent, Vector3 ground, Material concrete, Material soil, Material bush, System.Random rng)
        {
            Box("Jardinera", parent, ground + Vector3.up * 0.3f, new Vector3(2f, 0.6f, 2f), concrete);
            Box("Tierra", parent, ground + Vector3.up * 0.58f, new Vector3(1.7f, 0.06f, 1.7f), soil, collider: false);
            int lumps = 1 + rng.Next(3);
            for (int i = 0; i < lumps; i++)
            {
                float s = Range(rng, 0.8f, 1.2f);
                Primitive(PrimitiveType.Sphere, "Arbusto", parent, ground + new Vector3(Range(rng, -0.4f, 0.4f), 0.6f + s * 0.3f, Range(rng, -0.4f, 0.4f)),
                    new Vector3(s, s * 0.8f, s), bush, keepCollider: false);
            }
        }

        /// <summary>Cuatro arcos en U invertida, alineados en X.</summary>
        static void BikeRack(Transform parent, Vector3 ground, Material metal)
        {
            for (int i = 0; i < 4; i++)
            {
                var x = Vector3.right * ((i - 1.5f) * 0.8f);
                Vector3 a = ground + x + new Vector3(0f, 0f, -0.35f), b = a + Vector3.up * 0.8f;
                Vector3 d = ground + x + new Vector3(0f, 0f, 0.35f), c = d + Vector3.up * 0.8f;
                Strut("Aparcabicis", parent, a, b, 0.05f, metal, cylinder: true);
                Strut("Aparcabicis", parent, b, c, 0.05f, metal, cylinder: true);
                Strut("Aparcabicis", parent, d, c, 0.05f, metal, cylinder: true);
            }
        }

        /// <summary>Barandilla baja (0.5 m) de a a b, con postes cada 2 m y dos barras.</summary>
        static void LowRail(Transform parent, Vector3 a, Vector3 b, Material metal)
        {
            float length = Vector3.Distance(a, b);
            if (length < 0.5f) return;
            int posts = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            for (int i = 0; i <= posts; i++)
                Primitive(PrimitiveType.Cylinder, "PosteBarandilla", parent, Vector3.Lerp(a, b, (float)i / posts) + Vector3.up * 0.26f,
                    new Vector3(0.05f, 0.26f, 0.05f), metal, keepCollider: false);
            Strut("Barandilla", parent, a + Vector3.up * 0.5f, b + Vector3.up * 0.5f, 0.04f, metal, cylinder: true);
            Strut("Barandilla", parent, a + Vector3.up * 0.25f, b + Vector3.up * 0.25f, 0.03f, metal, cylinder: true);
        }

        static void ParkLamp(Transform parent, Vector3 ground, Material pole, Material glow)
        {
            Primitive(PrimitiveType.Cylinder, "FarolaParque", parent, ground + Vector3.up * 1.7f, new Vector3(0.1f, 1.7f, 0.1f), pole, keepCollider: true);
            Primitive(PrimitiveType.Sphere, "Globo", parent, ground + Vector3.up * 3.6f, Vector3.one * 0.45f, glow, keepCollider: false);
            var light = new GameObject("Luz").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = ground + Vector3.up * 3.4f;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.range = 9f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
        }

        /// <summary>Poste con cámara de vigilancia y luz azul encima (ficticio, sin logotipos).</summary>
        static void SecurityCamera(Transform parent, Vector3 ground, Material pole, Material blue)
        {
            var housing = Mat("PinturaBlanca", new Color(0.75f, 0.75f, 0.73f));
            Primitive(PrimitiveType.Cylinder, "PosteCamara", parent, ground + Vector3.up * 2.2f, new Vector3(0.14f, 2.2f, 0.14f), pole, keepCollider: true);
            Box("Camara", parent, ground + Vector3.up * 3.9f, new Vector3(0.5f, 0.6f, 0.5f), housing, collider: false);
            Primitive(PrimitiveType.Sphere, "Objetivo", parent, ground + Vector3.up * 3.5f, Vector3.one * 0.3f, Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f)), keepCollider: false);
            Box("LuzAzul", parent, ground + Vector3.up * 4.35f, new Vector3(0.25f, 0.3f, 0.25f), blue, collider: false);
        }

        static void NoticeBoard(Transform parent, Vector3 ground, float yaw, Material wood, Material cork, Material paper, System.Random rng)
        {
            var root = new GameObject("TablonAnuncios").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (float x in new[] { -0.75f, 0.75f })
                Box("Pata", root, new Vector3(x, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), wood);
            Box("Tablon", root, new Vector3(0f, 1.35f, 0f), new Vector3(1.6f, 0.9f, 0.05f), cork, collider: false);
            Box("Tejadillo", root, new Vector3(0f, 1.86f, 0f), new Vector3(1.8f, 0.06f, 0.3f), wood, collider: false);
            // Papeles clavados, por la cara que mira al camino (−Z local).
            for (int i = 0; i < 5; i++)
                Box("Papel", root, new Vector3(Range(rng, -0.6f, 0.6f), Range(rng, 1.05f, 1.65f), -0.035f), new Vector3(Range(rng, 0.18f, 0.3f), Range(rng, 0.22f, 0.32f), 0.01f), paper, collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, Range(rng, -8f, 8f));
        }

        static Material EmissiveMat(string name, Color color, float intensity)
        {
            var material = Mat(name, color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Vegetación ----------

        static void BuildBushes(Transform parent, System.Random rng)
        {
            var group = new GameObject("Arbustos").transform;
            group.SetParent(parent, false);
            var bushTexture = ProceduralTextures.Bush("Arbusto", 51);
            var green = TexturedMat("Arbusto", bushTexture);
            var dry = TexturedMat("ArbustoSeco", bushTexture, new Color(1.25f, 1.05f, 0.8f));

            foreach (float side in DistrictSides)
            {
                foreach (float x in DistrictWalkupsX) BushRow(group, new Vector3(x, LawnTop, side * DistrictWalkupZ), WalkupSize, green, dry, rng);
                foreach (float x in DistrictTowersX) BushRow(group, new Vector3(x, LawnTop, side * DistrictTowerZ), TowerSize, green, dry, rng);
            }
        }

        /// <summary>Fila de arbustos pegada a la fachada que da a la calle, con huecos y alguno seco.</summary>
        static void BushRow(Transform parent, Vector3 building, Vector3 size, Material green, Material dry, System.Random rng)
        {
            float toward = -Mathf.Sign(building.z);
            float z = building.z + toward * (size.z / 2f + 0.9f);
            for (float x = -size.x / 2f + 1f; x <= size.x / 2f - 1f; x += 2.1f)
            {
                if (Mathf.Abs(x) < 2.6f || rng.NextDouble() < 0.2) continue; // portal y huecos
                var material = rng.NextDouble() < 0.3 ? dry : green;
                int lumps = 2 + rng.Next(2);
                for (int i = 0; i < lumps; i++)
                {
                    float s = Range(rng, 0.9f, 1.5f);
                    var p = new Vector3(building.x + x + Range(rng, -0.5f, 0.5f), LawnTop + s * 0.35f, z + Range(rng, -0.3f, 0.3f));
                    Primitive(PrimitiveType.Sphere, "Arbusto", parent, p, new Vector3(s, s * 0.8f, s * 0.9f), material, keepCollider: false);
                }
            }
        }

        static void BuildLawnTrees(Transform parent, System.Random rng, List<Rect> occupied)
        {
            var group = new GameObject("ArbolesCesped").transform;
            group.SetParent(parent, false);
            var bark = Mat("Corteza", new Color(0.25f, 0.2f, 0.16f));
            var looks = new System.Random(315); // aspecto de la copa, aparte para no cambiar las posiciones
            for (int i = 0; i < 22; i++)
                if (RandomLawnPoint(rng, occupied, 6f, out var p))
                {
                    DistrictTree(group, p, bark, rng, Range(rng, 1.2f, 1.8f), looks);
                    occupied.Add(RectAround(p.x, p.z, 3f, 3f));
                }
        }

        // ---------- Basura ----------

        static void BuildDumpsters(Transform parent, System.Random rng)
        {
            var group = new GameObject("Contenedores").transform;
            group.SetParent(parent, false);
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var couch = Mat("TelaSofa", new Color(0.35f, 0.27f, 0.2f));
            var mattress = Mat("Colchon", new Color(0.66f, 0.62f, 0.52f));

            int index = 0;
            foreach (float side in DistrictSides)
            foreach (float x in DistrictTowersX)
            {
                // Junto al lateral este de cada torre, sobre una losa de hormigón.
                var pad = DumpsterPad(x, side);
                Box("Losa", group, pad + Vector3.up * 0.04f, new Vector3(3.2f, 0.08f, 5f), concrete);
                Dumpster(group, pad + new Vector3(0f, 0.08f, -1.2f), 90f, lidOpen: index % 2 == 0);
                Dumpster(group, pad + new Vector3(0f, 0.08f, 1.2f), 90f + Range(rng, -8f, 8f), lidOpen: index % 2 == 1);
                for (int b = 0; b < 5; b++)
                    GarbageBag(group, pad + new Vector3(Range(rng, -2.4f, -1.4f), 0f, Range(rng, -2.4f, 2.4f)), rng);

                if (index == 0)
                {
                    // Sofá viejo tirado al lado.
                    var sofa = new GameObject("SofaViejo").transform;
                    sofa.SetParent(group, false);
                    sofa.localPosition = pad + new Vector3(-2.5f, 0f, side * 5f);
                    sofa.localRotation = Quaternion.Euler(0f, 160f, 4f);
                    Box("Asiento", sofa, new Vector3(0f, 0.25f, 0f), new Vector3(2f, 0.45f, 0.85f), couch);
                    Box("Respaldo", sofa, new Vector3(0f, 0.7f, 0.33f), new Vector3(2f, 0.5f, 0.2f), couch, collider: false);
                    Box("BrazoA", sofa, new Vector3(-0.9f, 0.6f, 0f), new Vector3(0.2f, 0.35f, 0.85f), couch, collider: false);
                    Box("BrazoB", sofa, new Vector3(0.9f, 0.6f, 0f), new Vector3(0.2f, 0.35f, 0.85f), couch, collider: false);
                }
                if (index == 2)
                {
                    // Colchón apoyado en el contenedor.
                    Box("Colchon", group, pad + new Vector3(-1.1f, 0.95f, 0f), new Vector3(0.2f, 1.9f, 1.4f), mattress, collider: false)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
                }
                index++;
            }
        }

        static void Dumpster(Transform parent, Vector3 position, float yaw, bool lidOpen)
        {
            var root = new GameObject("Contenedor").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = Mat("ContenedorVerde", new Color(0.12f, 0.3f, 0.18f));
            var lid = Mat("PinturaNegra", new Color(0.05f, 0.05f, 0.06f));
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));

            Box("Cuerpo", root, new Vector3(0f, 0.75f, 0f), new Vector3(2f, 1.2f, 1.2f), body);
            if (lidOpen) Box("Tapa", root, new Vector3(0f, 1.95f, 0.63f), new Vector3(2.05f, 1.2f, 0.05f), lid, collider: false);
            else Box("Tapa", root, new Vector3(0f, 1.38f, 0f), new Vector3(2.05f, 0.05f, 1.25f), lid, collider: false);
            foreach (float x in new[] { -0.8f, 0.8f })
            foreach (float z in new[] { -0.45f, 0.45f })
                Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(x, 0.08f, z), new Vector3(0.15f, 0.04f, 0.15f), tire, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static void GarbageBag(Transform parent, Vector3 groundPosition, System.Random rng)
        {
            var bag = Mat("BolsaBasura", new Color(0.04f, 0.04f, 0.05f));
            bag.SetFloat("_Smoothness", 0.65f);
            float s = Range(rng, 0.45f, 0.7f);
            var go = Primitive(PrimitiveType.Sphere, "BolsaBasura", parent, groundPosition + Vector3.up * (s * 0.38f),
                new Vector3(s, s * 0.8f, s * 0.9f), bag, keepCollider: false);
            go.transform.localRotation = Quaternion.Euler(Range(rng, -15f, 15f), Range(rng, 0f, 360f), 0f);
        }

        static void TrashCan(Transform parent, Vector3 groundPosition, float yaw, bool tipped)
        {
            var body = Mat("PapeleraVerde", new Color(0.15f, 0.25f, 0.18f));
            var top = Mat("PinturaNegra", new Color(0.05f, 0.05f, 0.06f));
            var root = new GameObject("Papelera").transform;
            root.SetParent(parent, false);
            root.localPosition = groundPosition + (tipped ? Vector3.up * 0.3f : Vector3.zero);
            root.localRotation = Quaternion.Euler(tipped ? 90f : 0f, yaw, 0f);
            Primitive(PrimitiveType.Cylinder, "Cubo", root, new Vector3(0f, tipped ? 0f : 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), body, keepCollider: true);
            if (!tipped) Primitive(PrimitiveType.Cylinder, "Borde", root, new Vector3(0f, 0.91f, 0f), new Vector3(0.64f, 0.02f, 0.64f), top, keepCollider: false);
        }

        static void BuildTrash(Transform parent, System.Random rng, List<Rect> occupied)
        {
            var group = new GameObject("Basura").transform;
            group.SetParent(parent, false);

            // Papeleras en la acera, alguna volcada con bolsas al lado.
            int n = 0;
            for (float x = DistrictMinX + 22f; x < DistrictMaxX - 5f; x += 34f)
                foreach (float side in DistrictSides)
                {
                    var p = new Vector3(x, SidewalkTop, side * (DistrictLawnInner - 0.6f));
                    bool tipped = n++ % 3 == 1;
                    TrashCan(group, p, Range(rng, 0f, 360f), tipped);
                    if (tipped || rng.NextDouble() < 0.5)
                        for (int b = 0; b < 2; b++) GarbageBag(group, p + new Vector3(Range(rng, 0.6f, 1.4f), 0f, Range(rng, -0.6f, 0.2f) * side), rng);
                }

            // Desperdicios pequeños: papeles, vasos, latas, bolsas de patatas y botellas.
            var kinds = new (string name, Material material, PrimitiveType shape, Vector3 size)[]
            {
                ("Papel", Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), PrimitiveType.Cube, new Vector3(0.22f, 0.01f, 0.3f)),
                ("Vaso", Mat("BasuraRoja", new Color(0.7f, 0.1f, 0.1f)), PrimitiveType.Cylinder, new Vector3(0.09f, 0.06f, 0.09f)),
                ("Lata", Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), PrimitiveType.Cylinder, new Vector3(0.07f, 0.06f, 0.07f)),
                ("BolsaPatatas", Mat("BasuraBolsaColor", new Color(0.85f, 0.65f, 0.1f)), PrimitiveType.Cube, new Vector3(0.2f, 0.02f, 0.28f)),
                ("Botella", Mat("BasuraBotella", new Color(0.22f, 0.32f, 0.14f)), PrimitiveType.Cylinder, new Vector3(0.08f, 0.13f, 0.08f)),
            };
            float halfRoad = DistrictRoadWidth / 2f;
            for (int i = 0; i < 240; i++)
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                float x = Range(rng, DistrictMinX + 1f, DistrictMaxX - 1f);
                float z, y;
                double where = rng.NextDouble();
                if (where < 0.4) { z = side * Range(rng, halfRoad - 0.6f, halfRoad - 0.1f); y = RoadTop; }          // cuneta
                else if (where < 0.7) { z = side * Range(rng, halfRoad + 0.1f, DistrictLawnInner - 0.1f); y = SidewalkTop; } // acera
                else { z = side * Range(rng, DistrictLawnInner + 0.5f, DistrictHalfZ - 2f); y = LawnTop; }          // césped
                if (!IsFree(occupied, x, z)) continue;
                PlaceLitter(group, kinds[rng.Next(kinds.Length)], new Vector3(x, y, z), rng);
            }

            // Más basura en los caminos a las torres y en su explanada.
            foreach (float side in DistrictSides)
            foreach (float tx in DistrictTowersX)
                for (int i = 0; i < 12; i++)
                {
                    float absZ = Range(rng, ApproachStartZ + 1f, TowerFaceZ - 1.6f);
                    bool onForecourt = absZ > ForecourtStartZ;
                    float half = onForecourt ? 5f : 1.2f;
                    PlaceLitter(group, kinds[rng.Next(kinds.Length)], new Vector3(tx + Range(rng, -half, half), onForecourt ? 0.15f : 0.12f, side * absZ), rng);
                }
        }

        static void PlaceLitter(Transform parent, (string name, Material material, PrimitiveType shape, Vector3 size) kind, Vector3 ground, System.Random rng)
        {
            bool lying = kind.shape == PrimitiveType.Cylinder && rng.NextDouble() < 0.7;
            float lift = lying ? kind.size.x / 2f : kind.size.y * (kind.shape == PrimitiveType.Cylinder ? 1f : 0.5f);
            var go = Primitive(kind.shape, kind.name, parent, ground + Vector3.up * lift, kind.size, kind.material, keepCollider: false);
            go.transform.localRotation = Quaternion.Euler(lying ? 90f : 0f, Range(rng, 0f, 360f), 0f);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Suciedad, charcos y alcantarillas ----------

        static void BuildGrime(Transform parent, System.Random rng, List<Rect> occupied)
        {
            var group = new GameObject("Suciedad").transform;
            group.SetParent(parent, false);
            var texture = ProceduralTextures.Grime("Mancha", 61);
            var dirt = TransparentMat("Suciedad", texture, new Color(0.12f, 0.1f, 0.08f, 0.8f), 0.05f);
            var mud = TransparentMat("Barro", texture, new Color(0.3f, 0.23f, 0.15f, 0.9f), 0.1f);
            var oil = TransparentMat("Aceite", texture, new Color(0.04f, 0.04f, 0.05f, 0.7f), 0.5f);
            var puddle = TransparentMat("Charco", texture, new Color(0.08f, 0.09f, 0.1f, 0.85f), 0.92f);
            float halfRoad = DistrictRoadWidth / 2f;

            for (int i = 0; i < 40; i++) // acera
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                Decal(group, new Vector3(Range(rng, DistrictMinX + 1f, DistrictMaxX - 1f), SidewalkTop + 0.004f + i * 0.0001f, side * Range(rng, halfRoad + 0.6f, DistrictLawnInner - 0.6f)),
                    Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.8f, 2.4f), dirt);
            }
            for (int i = 0; i < 25; i++) // calzada: aceite y charcos
            {
                bool isPuddle = i < 10;
                float z = isPuddle ? (rng.Next(2) == 0 ? 1f : -1f) * Range(rng, halfRoad - 1.4f, halfRoad - 0.4f) : Range(rng, -halfRoad + 1f, halfRoad - 1f);
                var size = isPuddle ? new Vector2(Range(rng, 1.5f, 3.5f), Range(rng, 1f, 2f)) : Vector2.one * Range(rng, 0.8f, 1.8f);
                Decal(group, new Vector3(Range(rng, DistrictMinX - 2f, DistrictMaxX - 2f), RoadTop + 0.004f + i * 0.0001f, z), Range(rng, 0f, 360f), size, isPuddle ? puddle : oil);
            }
            for (int i = 0; i < 30; i++) // calvas de barro en el césped
                if (RandomLawnPoint(rng, occupied, 0.8f, out var p))
                    Decal(group, p + Vector3.up * (0.004f + i * 0.0001f), Range(rng, 0f, 360f), new Vector2(Range(rng, 2f, 5f), Range(rng, 1.5f, 3.5f)), mud);

            // Atajos de barro: donde la gente cruza el césped desde la acera hasta las torres.
            foreach (float side in DistrictSides)
            foreach (float x in DistrictTowersX)
                for (int k = 0; k < 4; k++)
                    Decal(group, new Vector3(x + 6f + k * 1.8f, LawnTop + 0.005f, side * (DistrictLawnInner + 2f + k * 2.2f)), 30f * side, new Vector2(1.6f, 2.6f), mud);

            // Manchas y un charco en cada camino a las torres (el camino está a 0.12 m y la explanada a 0.15 m).
            foreach (float side in DistrictSides)
            foreach (float tx in DistrictTowersX)
            {
                for (int k = 0; k < 4; k++)
                    Decal(group, new Vector3(tx + Range(rng, -0.6f, 0.6f), 0.124f + k * 0.0002f, side * Range(rng, ApproachStartZ + 2f, ForecourtStartZ - 1f)),
                        Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.8f, 1.6f), dirt);
                Decal(group, new Vector3(tx + Range(rng, -0.4f, 0.4f), 0.126f, side * Range(rng, 15f, 40f)), Range(rng, 0f, 360f), new Vector2(1.8f, 1.2f), puddle);
                for (int k = 0; k < 3; k++)
                    Decal(group, new Vector3(tx + Range(rng, -4.5f, 4.5f), 0.154f + k * 0.0002f, side * Range(rng, ForecourtStartZ + 0.5f, TowerFaceZ - 1.6f)),
                        Range(rng, 0f, 360f), Vector2.one * Range(rng, 1f, 2f), dirt);
            }

            // Tapas de alcantarilla.
            var iron = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            for (float x = DistrictMinX + 30f; x < DistrictMaxX; x += 45f)
                Primitive(PrimitiveType.Cylinder, "Alcantarilla", group, new Vector3(x, RoadTop + 0.005f, Range(rng, -2f, 2f)), new Vector3(0.7f, 0.005f, 0.7f), iron, keepCollider: false);
        }

        /// <summary>Calcomanía horizontal (quad mirando arriba) sin colisión ni sombras.</summary>
        static void Decal(Transform parent, Vector3 position, float yaw, Vector2 size, Material material)
        {
            var quad = Primitive(PrimitiveType.Quad, "Mancha", parent, position, new Vector3(size.x, size.y, 1f), material, keepCollider: false);
            quad.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Grafitis ----------

        static void BuildGraffiti(Transform parent, System.Random rng)
        {
            var group = new GameObject("Grafitis").transform;
            group.SetParent(parent, false);
            var materials = new Material[4];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = CutoutMat($"Grafiti_{i}", ProceduralTextures.Graffiti($"Grafiti_{i}", 700 + i * 13));

            // En las paredes laterales de los bloques bajos y en la planta baja de las torres.
            foreach (float side in DistrictSides)
            {
                foreach (float x in DistrictWalkupsX)
                    if (rng.NextDouble() < 0.75)
                    {
                        float end = rng.Next(2) == 0 ? 1f : -1f;
                        WallGraffiti(group, new Vector3(x + end * WalkupSize.x / 2f, Range(rng, 1.3f, 1.8f), side * DistrictWalkupZ + Range(rng, -3f, 3f)),
                            Vector3.right * end, Range(rng, 2.6f, 3.6f), materials[rng.Next(materials.Length)]);
                    }
                foreach (float x in DistrictTowersX)
                    WallGraffiti(group, new Vector3(x - TowerSize.x / 2f, Range(rng, 1.3f, 1.8f), side * DistrictTowerZ + Range(rng, -4f, 4f)),
                        Vector3.left, Range(rng, 2.6f, 3.6f), materials[rng.Next(materials.Length)]);
            }

            // Muro de la plaza: lleno.
            float back = PlazaCenter.z + Mathf.Sign(PlazaCenter.z) * 7.6f;
            var wallNormal = Vector3.back * Mathf.Sign(PlazaCenter.z);
            for (int i = 0; i < 3; i++)
                WallGraffiti(group, new Vector3(PlazaCenter.x - 3.2f + i * 3.2f, LawnTop + 0.95f, back + wallNormal.z * 0.15f), wallNormal, 3f, materials[i % materials.Length]);
        }

        /// <summary>Pega un grafiti (ancho × ancho/2) centrado en <paramref name="center"/>, sobre la pared cuya cara exterior mira hacia <paramref name="normal"/>.</summary>
        static void WallGraffiti(Transform parent, Vector3 center, Vector3 normal, float width, Material material)
        {
            var position = center + normal * 0.02f;
            var quad = Primitive(PrimitiveType.Quad, "Grafiti", parent, position, new Vector3(width, width / 2f, 1f), material, keepCollider: false);
            quad.transform.localRotation = Quaternion.LookRotation(-normal); // el quad se ve desde -forward
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Mobiliario urbano ----------

        static void BuildStreetFurniture(Transform parent)
        {
            var group = new GameObject("MobiliarioUrbano").transform;
            group.SetParent(parent, false);

            // Bocas de incendio junto al bordillo.
            var hydrant = Mat("BocaIncendio", new Color(0.78f, 0.62f, 0.12f));
            float curb = DistrictRoadWidth / 2f + 0.45f;
            foreach (var (x, side) in new[] { (148f, 1f), (205f, 1f), (170f, -1f), (238f, -1f) })
            {
                var p = new Vector3(x, SidewalkTop, side * curb);
                Primitive(PrimitiveType.Cylinder, "BocaIncendio", group, p + Vector3.up * 0.35f, new Vector3(0.25f, 0.35f, 0.25f), hydrant, keepCollider: true);
                Primitive(PrimitiveType.Sphere, "Tapa", group, p + Vector3.up * 0.72f, new Vector3(0.24f, 0.16f, 0.24f), hydrant, keepCollider: false);
                Primitive(PrimitiveType.Cylinder, "Boca", group, p + Vector3.up * 0.45f, new Vector3(0.1f, 0.22f, 0.1f), hydrant, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Pilonas rojas cerrando el final de la calle.
            var bollard = Mat("PilonaRoja", new Color(0.55f, 0.12f, 0.1f));
            for (float z = -DistrictRoadWidth / 2f + 0.8f; z <= DistrictRoadWidth / 2f - 0.8f; z += 1.5f)
                Primitive(PrimitiveType.Cylinder, "Pilona", group, new Vector3(DistrictMaxX - 0.8f, RoadTop + 0.45f, z), new Vector3(0.18f, 0.45f, 0.18f), bollard, keepCollider: true);
        }

        // ---------- Utilidades de props ----------

        static void Bench(Transform parent, Vector3 position, float yaw, Material wood, Material concrete)
        {
            // Mira hacia −Z local: el respaldo queda en +Z.
            var root = new GameObject("Banco").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (float x in new[] { -0.75f, 0.75f })
            {
                Box("Pata", root, new Vector3(x, 0.22f, 0f), new Vector3(0.14f, 0.44f, 0.5f), concrete, collider: false);
                Box("Soporte", root, new Vector3(x, 0.68f, 0.24f), new Vector3(0.06f, 0.5f, 0.06f), concrete, collider: false);
            }
            for (int i = 0; i < 3; i++)
                Box("Liston", root, new Vector3(0f, 0.47f, -0.15f + i * 0.15f), new Vector3(1.9f, 0.05f, 0.12f), wood, collider: false);
            for (int i = 0; i < 2; i++)
                Box("Respaldo", root, new Vector3(0f, 0.68f + i * 0.17f, 0.27f), new Vector3(1.9f, 0.1f, 0.04f), wood, collider: false)
                    .transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.45f, 0.05f);
            collider.size = new Vector3(1.9f, 0.9f, 0.6f);
        }

        /// <summary>Barra (caja o cilindro) que va del punto a al b.</summary>
        static GameObject Strut(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material material, bool cylinder = false)
        {
            float length = Vector3.Distance(a, b);
            var scale = cylinder ? new Vector3(thickness, length / 2f, thickness) : new Vector3(thickness, length, thickness);
            var go = Primitive(cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube, name, parent, (a + b) / 2f, scale, material, keepCollider: false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return go;
        }

        /// <summary>Material con recorte por alfa y sin culling (vallas, mallas, grafitis).</summary>
        static Material CutoutMat(string name, Texture2D texture)
        {
            var material = TexturedMat(name, texture);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Material transparente (manchas y charcos). El color del tinte lleva la opacidad en alfa.</summary>
        static Material TransparentMat(string name, Texture2D texture, Color tint, float smoothness)
        {
            var material = TexturedMat(name, texture, tint);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
