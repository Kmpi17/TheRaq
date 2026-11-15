using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Casa del personaje, dentro de la torre T2 (que por eso se construye hueca, ver <see cref="T2Openings"/>):
    /// <list type="bullet">
    /// <item><b>Vestíbulo</b> en la planta baja, detrás del portal (abierto de verdad): buzones, puerta de la escalera,
    /// fluorescentes, cámara, grafitis y dos ascensores. El <see cref="Elevator"/> sube, con un fundido a negro, al piso.</item>
    /// <item><b>Piso</b> en la esquina sureste de la última planta (14ª, a 39 m): dos ventanas al sur (el inicio, el
    /// aparcamiento P2, la calle y las cocheras) y una al este (el tren elevado y la estación), con cristal y vistas
    /// reales de la ciudad. Salón con sofá y tele, sillón mirando por la ventana, cocina, cama, el armario para
    /// cambiarse (<see cref="Wardrobe"/>) y la caja fuerte (<see cref="SafeBox"/>). El ascensor del piso baja al vestíbulo.</item>
    /// </list>
    /// Coordenadas: el vestíbulo, en las locales de la torre (origen en el centro de su planta, −Z = fachada del
    /// portal). El piso, en las suyas: suelo en y = 0, +Z al norte, sala de x −4.2…4.2 y z −3.85…3.85, techo a 2.7 m;
    /// la fachada sur está en z −4.15 y la este en x 4.5.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static readonly Vector3 T2Center = new(0f, 0f, 2f);
        const int T2Floors = 14;

        // Vestíbulo (locales de la torre): sala de x −5…5 y z −7.2…−0.5, suelo a 0.25 m y techo a 2.75 m.
        const float LobbyFloor = 0.25f, LobbyCeiling = 2.75f, LobbyTop = 2.88f;
        static readonly Vector3 LobbyLiftCall = new(2.7f, LobbyFloor, -1.4f);

        // Piso (locales de la torre): centro de la sala y suelo, en la planta 14 (forjado a 39 m).
        static readonly Vector3 ApartmentCenter = new(9f, 39.12f, -3.35f);
        const float AptHalfX = 4.2f, AptHalfZ = 3.85f, AptHeight = 2.7f, AptSill = 0.75f, AptHead = 2.45f;

        /// <summary>Huecos de T2: el portal, dos ventanas al sur y una al este en la última planta, y las dos salas.</summary>
        static TowerOpenings T2Openings()
        {
            var o = new TowerOpenings { OpenPortal = true };
            o.Holes.Add((0, 4, 0));             // portal: vano central de la planta baja
            o.Holes.Add((0, 6, T2Floors - 1));  // ventanas sur del piso (x 4.5…7.5 y 7.5…10.5)
            o.Holes.Add((0, 7, T2Floors - 1));
            o.Holes.Add((1, 1, T2Floors - 1));  // ventana este (z −4.5…−1.5)
            var lobby = new Bounds();
            lobby.SetMinMax(new Vector3(-5.3f, 0f, -HighRiseDepth / 2f), new Vector3(5.3f, LobbyTop, -0.2f));
            var flat = new Bounds();
            flat.SetMinMax(new Vector3(4.5f, ApartmentCenter.y - 0.22f, -HighRiseDepth / 2f), new Vector3(HighRiseWidth / 2f, T2Floors * ProceduralTextures.FacadeFloor, 0.8f));
            o.Voids.Add(lobby);
            o.Voids.Add(flat);
            return o;
        }

        static void BuildApartment(Transform parent)
        {
            var steel = Mat("AceroAscensor", new Color(0.55f, 0.56f, 0.58f));
            var buttonLit = EmissiveMat("BotonAscensor", new Color(1f, 0.55f, 0.15f), 2.5f);

            // --- Vestíbulo en la planta baja de T2 ---
            var lobby = new GameObject("Vestibulo_T2").transform;
            lobby.SetParent(parent, false);
            lobby.localPosition = T2Center;
            var lobbyArrival = new GameObject("LlegadaVestibulo").transform;
            lobbyArrival.SetParent(lobby, false);
            lobbyArrival.localPosition = new Vector3(LobbyLiftCall.x, LobbyFloor + 0.05f, LobbyLiftCall.z - 0.2f);
            lobbyArrival.localRotation = Quaternion.Euler(0f, 180f, 0f); // sale del ascensor mirando al portal

            // --- Piso en la última planta ---
            var flat = new GameObject("Piso_Jugador").transform;
            flat.SetParent(parent, false);
            flat.localPosition = T2Center + ApartmentCenter;
            var flatArrival = new GameObject("LlegadaPiso").transform;
            flatArrival.SetParent(flat, false);
            flatArrival.localPosition = new Vector3(-AptHalfX + 0.8f, 0.05f, 2.3f);
            flatArrival.localRotation = Quaternion.Euler(0f, 90f, 0f); // sale del ascensor mirando al salón (este)

            BuildLobby(lobby, steel, buttonLit, flatArrival);
            BuildApartmentRoom(flat, steel, buttonLit, lobbyArrival);
        }

        // ---------- Vestíbulo ----------

        static void BuildLobby(Transform root, Material steel, Material buttonLit, Transform flatArrival)
        {
            var rng = new System.Random(4141);
            float front = -HighRiseDepth / 2f;
            var tile = Mat("BaldosaVestibulo", new Color(0.46f, 0.47f, 0.42f));
            var wall = Mat("ParedVestibulo", new Color(0.62f, 0.6f, 0.52f));
            var wainscot = Mat("ZocaloVestibulo", new Color(0.3f, 0.34f, 0.3f));
            var ceiling = Mat("TechoVestibulo", new Color(0.68f, 0.68f, 0.64f));
            var darkSteel = Mat("AceroOscuro", new Color(0.32f, 0.33f, 0.35f));
            var black = Mat("NegroAparatos", new Color(0.04f, 0.04f, 0.045f));
            var paper = Mat("PapelAviso", new Color(0.85f, 0.84f, 0.78f));
            var tube = EmissiveMat("Fluorescente", new Color(0.92f, 0.97f, 1f), 2.2f);
            var tubeDim = EmissiveMat("FluorescenteFundido", new Color(0.6f, 0.65f, 0.62f), 0.4f);
            var exitGreen = EmissiveMat("LuzSalida", new Color(0.2f, 0.9f, 0.4f), 2f);

            // Caja: suelo, techo y paredes (con colisión). La pared sur deja el hueco del portal (2.6 × 2.35 m).
            Box("Suelo", root, new Vector3(0f, LobbyFloor / 2f, (front - 0.2f) / 2f), new Vector3(10.6f, LobbyFloor, -0.2f - front), tile);
            Box("Techo", root, new Vector3(0f, (LobbyCeiling + LobbyTop) / 2f, (front - 0.2f) / 2f), new Vector3(10.6f, LobbyTop - LobbyCeiling, -0.2f - front), ceiling);
            float sz = front + 0.15f;
            foreach (float sx in new[] { -1f, 1f })
                Box("Pared_S", root, new Vector3(sx * 3.3f, LobbyTop / 2f, sz), new Vector3(4f, LobbyTop, 0.3f), wall);
            Box("Dintel", root, new Vector3(0f, (2.6f + LobbyTop) / 2f, sz), new Vector3(2.6f, LobbyTop - 2.6f, 0.3f), wall);
            Box("Pared_N", root, new Vector3(0f, LobbyTop / 2f, -0.35f), new Vector3(10.6f, LobbyTop, 0.3f), wall);
            foreach (float sx in new[] { -1f, 1f })
                Box(sx < 0f ? "Pared_O" : "Pared_E", root, new Vector3(sx * 5.15f, LobbyTop / 2f, (front - 0.2f) / 2f), new Vector3(0.3f, LobbyTop, -0.2f - front), wall);
            // Zócalo pintado de verde oscuro, a media altura, en las cuatro paredes.
            Box("Zocalo", root, new Vector3(0f, LobbyFloor + 0.55f, -0.51f), new Vector3(10f, 1.1f, 0.02f), wainscot, collider: false);
            foreach (float sx in new[] { -1f, 1f })
                Box("Zocalo", root, new Vector3(sx * 4.99f, LobbyFloor + 0.55f, -3.85f), new Vector3(0.02f, 1.1f, 6.7f), wainscot, collider: false);

            // Ascensores en la pared norte, con la botonera entre los dos: el de la derecha es el que funciona.
            foreach (float x in new[] { 1.8f, 3.6f })
            {
                Box("MarcoAscensor", root, new Vector3(x, LobbyFloor + 1.15f, -0.52f), new Vector3(1.4f, 2.3f, 0.04f), darkSteel, collider: false);
                foreach (float s in new[] { -0.28f, 0.28f })
                    Box("PuertaAscensor", root, new Vector3(x + s, LobbyFloor + 1.05f, -0.55f), new Vector3(0.55f, 2.1f, 0.03f), steel, collider: false);
                Box("Indicador", root, new Vector3(x, LobbyFloor + 2.3f, -0.53f), new Vector3(0.3f, 0.1f, 0.02f), x > 2f ? buttonLit : black, collider: false);
            }
            Box("Botonera", root, new Vector3(2.7f, LobbyFloor + 1.2f, -0.53f), new Vector3(0.12f, 0.26f, 0.03f), steel, collider: false);
            Box("BotonSubir", root, new Vector3(2.7f, LobbyFloor + 1.25f, -0.555f), new Vector3(0.05f, 0.05f, 0.02f), buttonLit, collider: false);
            Box("AvisoAveriado", root, new Vector3(1.8f, LobbyFloor + 1.4f, -0.575f), new Vector3(0.3f, 0.4f, 0.01f), paper, collider: false); // "no funciona"
            var up = new GameObject("Ascensor_Vestibulo").AddComponent<Elevator>();
            up.transform.SetParent(root, false);
            up.transform.localPosition = LobbyLiftCall;
            up.Configure(flatArrival, "F: subir a casa (ascensor, planta 14)", 1.7f);
            EditorUtility.SetDirty(up);

            // Puerta de la escalera (cerrada) con ventanuco de malla y luz de salida.
            Box("PuertaEscalera", root, new Vector3(-3.4f, LobbyFloor + 1.05f, -0.53f), new Vector3(1f, 2.1f, 0.05f), Mat("PuertaEscaleraVerde", new Color(0.2f, 0.28f, 0.24f)), collider: false);
            Box("Ventanuco", root, new Vector3(-3.4f, LobbyFloor + 1.55f, -0.56f), new Vector3(0.25f, 0.4f, 0.01f), Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f)), collider: false);
            Box("BarraAntipanico", root, new Vector3(-3.4f, LobbyFloor + 1f, -0.58f), new Vector3(0.8f, 0.05f, 0.05f), steel, collider: false);
            Box("LuzSalida", root, new Vector3(-3.4f, LobbyFloor + 2.25f, -0.55f), new Vector3(0.35f, 0.14f, 0.05f), exitGreen, collider: false);

            // Buzones en la pared oeste: rejilla de 6 × 4 portezuelas; dos forzadas, abiertas.
            float bx = -4.98f;
            Box("Buzones", root, new Vector3(bx + 0.04f, LobbyFloor + 1.5f, -4f), new Vector3(0.08f, 1.1f, 2.6f), steel);
            for (int r = 0; r <= 4; r++)
                Box("JuntaBuzon", root, new Vector3(bx + 0.085f, LobbyFloor + 0.95f + r * 0.275f, -4f), new Vector3(0.01f, 0.015f, 2.6f), darkSteel, collider: false);
            for (int c = 0; c <= 6; c++)
                Box("JuntaBuzon", root, new Vector3(bx + 0.085f, LobbyFloor + 1.5f, -5.3f + c * 0.433f), new Vector3(0.01f, 1.1f, 0.015f), darkSteel, collider: false);
            foreach (var (c, r) in new[] { (1, 2), (4, 0) })
            {
                var lid = Box("PortezuelaAbierta", root, new Vector3(bx + 0.2f, LobbyFloor + 0.95f + (r + 0.5f) * 0.275f, -5.3f + (c + 0.5f) * 0.433f), new Vector3(0.01f, 0.25f, 0.4f), steel, collider: false);
                lid.transform.localRotation = Quaternion.Euler(0f, 55f, 0f);
            }
            for (int i = 0; i < 6; i++) // propaganda tirada debajo
                Box("Folleto", root, new Vector3(Range(rng, -4.8f, -3.6f), LobbyFloor + 0.005f, Range(rng, -5.4f, -2.6f)), new Vector3(0.2f, 0.005f, 0.28f), paper, collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f);

            // Avisos en la pared norte, radiador en la este, silla rota, basura y cámara en el techo.
            for (int i = 0; i < 3; i++)
                Box("Aviso", root, new Vector3(-1.4f + i * 0.4f, LobbyFloor + 1.5f + Range(rng, -0.1f, 0.1f), -0.515f), new Vector3(0.28f, 0.36f, 0.01f), paper, collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, Range(rng, -6f, 6f));
            Box("Radiador", root, new Vector3(4.92f, LobbyFloor + 0.45f, -3.5f), new Vector3(0.12f, 0.6f, 1.6f), Mat("MarcoVentanaPiso", new Color(0.88f, 0.87f, 0.83f)));
            var chair = new GameObject("SillaRota").transform;
            chair.SetParent(root, false);
            chair.localPosition = new Vector3(4.3f, LobbyFloor, -5.6f);
            chair.localRotation = Quaternion.Euler(0f, -30f, 0f);
            var plastic = Mat("SillaPlastico", new Color(0.82f, 0.8f, 0.74f));
            Box("Asiento", chair, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.05f, 0.45f), plastic);
            Box("Respaldo", chair, new Vector3(0f, 0.72f, -0.21f), new Vector3(0.45f, 0.5f, 0.04f), plastic, collider: false);
            foreach (var (lx, lz) in new[] { (-0.2f, -0.2f), (0.2f, -0.2f), (-0.2f, 0.2f) }) // le falta una pata
                Box("Pata", chair, new Vector3(lx, 0.22f, lz), new Vector3(0.04f, 0.44f, 0.04f), plastic, collider: false);
            GarbageBag(root, new Vector3(-4.5f, LobbyFloor, -6.6f), rng);
            Primitive(PrimitiveType.Cylinder, "Vaso", root, new Vector3(3.2f, LobbyFloor + 0.06f, -4.8f), new Vector3(0.08f, 0.06f, 0.08f), Mat("VasoRojoPiso", new Color(0.75f, 0.08f, 0.08f)), keepCollider: false)
                .transform.localRotation = Quaternion.Euler(90f, 30f, 0f);
            Primitive(PrimitiveType.Sphere, "CamaraTecho", root, new Vector3(-4.7f, LobbyCeiling - 0.05f, front + 0.6f), new Vector3(0.3f, 0.2f, 0.3f), black, keepCollider: false);
            if (graffitiMats != null && graffitiMats[2])
                WallGraffiti(root, new Vector3(5f, LobbyFloor + 1.6f, -5.7f), Vector3.left, 2.2f, graffitiMats[2]);
            if (graffitiMats != null && graffitiMats[0])
                WallGraffiti(root, new Vector3(-1.6f, LobbyFloor + 2.1f, -0.5f), Vector3.back, 1.4f, graffitiMats[0]);

            // Telefonillo junto al portal, por fuera.
            Box("Telefonillo", root, new Vector3(2.15f, 1.3f, front - 0.04f), new Vector3(0.2f, 0.32f, 0.05f), steel, collider: false);
            Box("BotonTelefonillo", root, new Vector3(2.15f, 1.22f, front - 0.07f), new Vector3(0.05f, 0.05f, 0.02f), buttonLit, collider: false);

            // Dos fluorescentes (uno casi fundido) con su luz.
            foreach (var (z, lit) in new[] { (-5.2f, true), (-2.2f, false) })
            {
                Box("Fluorescente", root, new Vector3(0f, LobbyCeiling - 0.04f, z), new Vector3(1.3f, 0.06f, 0.18f), lit ? tube : tubeDim, collider: false);
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.localPosition = new Vector3(0f, LobbyCeiling - 0.3f, z);
                l.type = LightType.Point;
                l.color = new Color(0.9f, 0.96f, 1f);
                l.range = 7f;
                l.intensity = lit ? 1.6f : 0.4f;
                l.shadows = LightShadows.None;
            }
        }

        // ---------- Piso ----------

        static void BuildApartmentRoom(Transform root, Material steel, Material buttonLit, Transform lobbyArrival)
        {
            var rng = new System.Random(4242);
            float hx = AptHalfX, hz = AptHalfZ, h = AptHeight;
            var floor = Mat("TarimaPiso", new Color(0.36f, 0.26f, 0.18f));
            var wall = Mat("ParedPiso", new Color(0.66f, 0.62f, 0.55f));
            var ceiling = Mat("TechoPiso", new Color(0.75f, 0.74f, 0.7f));
            var darkWood = Mat("MaderaOscuraPiso", new Color(0.24f, 0.16f, 0.1f));
            var leather = Mat("SofaPiso", new Color(0.2f, 0.17f, 0.15f));
            var cushion = Mat("CojinSofa", new Color(0.27f, 0.23f, 0.2f));
            var black = Mat("NegroAparatos", new Color(0.04f, 0.04f, 0.045f));
            var white = Mat("Electrodomestico", new Color(0.85f, 0.85f, 0.82f));
            var counter = Mat("Encimera", new Color(0.5f, 0.48f, 0.44f));
            var rug = Mat("AlfombraPiso", new Color(0.35f, 0.12f, 0.12f));
            var duvet = Mat("Edredon", new Color(0.2f, 0.24f, 0.32f));
            var sheet = Mat("Sabana", new Color(0.8f, 0.8f, 0.78f));
            var cardboard = Mat("Carton", new Color(0.62f, 0.48f, 0.3f));
            var safeMat = Mat("CajaFuerte", new Color(0.17f, 0.18f, 0.19f));
            var chrome = Mat("CromoPiso", new Color(0.72f, 0.72f, 0.7f));
            var frameWhite = Mat("MarcoVentanaPiso", new Color(0.88f, 0.87f, 0.83f));
            var mirror = Mat("EspejoArmario", new Color(0.7f, 0.74f, 0.78f));
            mirror.SetFloat("_Smoothness", 0.9f);
            mirror.SetFloat("_Metallic", 0.8f);
            var redCup = Mat("VasoRojoPiso", new Color(0.75f, 0.08f, 0.08f));
            var screen = EmissiveMat("PantallaTele", new Color(0.22f, 0.32f, 0.48f), 1.3f);
            var lamp = EmissiveMat("LuzTechoPiso", new Color(1f, 0.88f, 0.7f), 2f);
            var brick = TexturedMat("FachadaLadrilloAireLiso", ProceduralTextures.PlainBrick("FachadaLadrilloAireLiso", new Color(0.5f, 0.27f, 0.21f), 2));
            var glass = GlassMat("CristalVentanaPiso", new Color(0.7f, 0.8f, 0.85f, 0.14f));

            // --- Caja: suelo, techo, paredes interiores (oeste y norte) y las dos fachadas con ventanas (sur y este) ---
            Box("Suelo", root, new Vector3(0f, -0.1f, 0f), new Vector3(2f * hx + 0.6f, 0.2f, 2f * hz + 0.6f), floor);
            Box("Techo", root, new Vector3(0f, h + 0.075f, 0f), new Vector3(2f * hx + 0.6f, 0.15f, 2f * hz + 0.6f), ceiling);
            Box("Pared_O", root, new Vector3(-hx - 0.15f, (h + 0.15f) / 2f, 0f), new Vector3(0.3f, h + 0.15f, 2f * hz + 0.6f), wall);
            Box("Pared_N", root, new Vector3(0f, (h + 0.15f) / 2f, hz + 0.15f), new Vector3(2f * hx + 0.6f, h + 0.15f, 0.3f), wall);
            // Fachada sur (z −4.15): celdas de los vanos x −4.5…−1.5 y −1.5…1.5 con ventana de 2 m; el resto, pared.
            FacadeWall(root, alongX: true, plane: -hz - 0.3f, inward: 1f, from: -hx - 0.3f, to: hx + 0.3f,
                new[] { (-4.5f, -1.5f), (-1.5f, 1.5f) }, wall, brick, glass, frameWhite);
            // Fachada este (x 4.5): celda z −1.15…1.85 (vano z −4.5…−1.5 de la torre).
            FacadeWall(root, alongX: false, plane: hx + 0.3f, inward: -1f, from: -hz - 0.3f, to: hz + 0.3f,
                new[] { (-1.15f, 1.85f) }, wall, brick, glass, frameWhite);
            Box("Rodapie", root, new Vector3(0f, 0.05f, hz - 0.01f), new Vector3(2f * hx, 0.1f, 0.02f), darkWood, collider: false);
            Box("Rodapie", root, new Vector3(-hx + 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.1f, 2f * hz), darkWood, collider: false);

            // --- Ascensor en la pared oeste: baja al vestíbulo ---
            const float liftZ = 2.3f;
            Box("MarcoAscensor", root, new Vector3(-hx + 0.02f, 1.17f, liftZ), new Vector3(0.04f, 2.34f, 1.5f), Mat("AceroOscuro", new Color(0.32f, 0.33f, 0.35f)), collider: false);
            foreach (float s in new[] { -0.3f, 0.3f })
                Box("PuertaAscensor", root, new Vector3(-hx + 0.05f, 1.05f, liftZ + s), new Vector3(0.03f, 2.1f, 0.58f), steel, collider: false);
            Box("Indicador", root, new Vector3(-hx + 0.03f, 2.45f, liftZ), new Vector3(0.02f, 0.12f, 0.3f), buttonLit, collider: false);
            Box("Botonera", root, new Vector3(-hx + 0.03f, 1.2f, liftZ - 1.05f), new Vector3(0.03f, 0.26f, 0.12f), steel, collider: false);
            Box("BotonBajar", root, new Vector3(-hx + 0.05f, 1.16f, liftZ - 1.05f), new Vector3(0.02f, 0.05f, 0.05f), buttonLit, collider: false);
            Box("Felpudo", root, new Vector3(-hx + 0.8f, 0.01f, liftZ), new Vector3(0.7f, 0.02f, 1.2f), Mat("Felpudo", new Color(0.3f, 0.25f, 0.18f)), collider: false);
            foreach (float s in new[] { -0.12f, 0.12f })
                Box("Zapatilla", root, new Vector3(-hx + 0.45f, 0.05f, liftZ - 1.35f + s), new Vector3(0.3f, 0.1f, 0.12f), white, collider: false);
            var down = new GameObject("Ascensor_Piso").AddComponent<Elevator>();
            down.transform.SetParent(root, false);
            down.transform.localPosition = new Vector3(-hx + 0.8f, 0f, liftZ);
            down.Configure(lobbyArrival, "F: bajar al portal (ascensor)", 1.3f);
            EditorUtility.SetDirty(down);

            // --- Salón: sofá mirando a la tele de la pared oeste, alfombra y mesa baja; sillón mirando por la ventana sur ---
            Box("Alfombra", root, new Vector3(-2.4f, 0.01f, -1.3f), new Vector3(2.6f, 0.02f, 3f), rug, collider: false);
            var sofa = new GameObject("Sofa").transform;
            sofa.SetParent(root, false);
            sofa.localPosition = new Vector3(-0.9f, 0f, -1.3f);
            sofa.localRotation = Quaternion.Euler(0f, -90f, 0f); // mira al oeste
            Box("SofaBase", sofa, new Vector3(0f, 0.21f, 0f), new Vector3(2.4f, 0.42f, 0.9f), leather);
            Box("SofaRespaldo", sofa, new Vector3(0f, 0.68f, -0.34f), new Vector3(2.4f, 0.55f, 0.22f), leather);
            foreach (float s in new[] { -1.12f, 1.12f })
                Box("SofaBrazo", sofa, new Vector3(s, 0.33f, 0f), new Vector3(0.2f, 0.62f, 0.9f), leather);
            foreach (float s in new[] { -0.5f, 0.5f })
                Box("Cojin", sofa, new Vector3(s, 0.47f, 0.08f), new Vector3(0.95f, 0.1f, 0.7f), cushion, collider: false);
            Box("MesaBaja", root, new Vector3(-2.4f, 0.21f, -1.3f), new Vector3(0.6f, 0.42f, 1.2f), darkWood);
            Box("CajaPizza", root, new Vector3(-2.4f, 0.44f, -1.6f), new Vector3(0.4f, 0.04f, 0.4f), cardboard, collider: false).transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            foreach (var z in new[] { -1.0f, -0.85f })
                Primitive(PrimitiveType.Cylinder, "Vaso", root, new Vector3(-2.25f, 0.48f, z), new Vector3(0.08f, 0.06f, 0.08f), redCup, keepCollider: false);
            Box("Mando", root, new Vector3(-2.55f, 0.435f, -1.05f), new Vector3(0.09f, 0.03f, 0.14f), black, collider: false);
            Box("MuebleTele", root, new Vector3(-hx + 0.25f, 0.25f, -1.3f), new Vector3(0.45f, 0.5f, 1.8f), darkWood);
            Box("Tele", root, new Vector3(-hx + 0.14f, 1.0f, -1.3f), new Vector3(0.06f, 0.78f, 1.3f), black, collider: false);
            Box("PantallaTele", root, new Vector3(-hx + 0.175f, 1.0f, -1.3f), new Vector3(0.01f, 0.68f, 1.2f), screen, collider: false);
            Box("Consola", root, new Vector3(-hx + 0.25f, 0.54f, -0.7f), new Vector3(0.3f, 0.08f, 0.35f), black, collider: false);
            foreach (float s in new[] { -1.05f, 1.05f })
                Box("Altavoz", root, new Vector3(-hx + 0.25f, 0.7f, -1.3f + s), new Vector3(0.22f, 0.4f, 0.2f), black, collider: false);
            // Sillón de cara a la ventana sur del centro, con una mesita: el sitio para mirar la ciudad.
            var armchair = new GameObject("Sillon").transform;
            armchair.SetParent(root, false);
            armchair.localPosition = new Vector3(0.6f, 0f, -2.7f);
            armchair.localRotation = Quaternion.Euler(0f, 165f, 0f);
            Box("SillonBase", armchair, new Vector3(0f, 0.22f, 0f), new Vector3(0.9f, 0.44f, 0.85f), leather);
            Box("SillonRespaldo", armchair, new Vector3(0f, 0.7f, -0.36f), new Vector3(0.9f, 0.6f, 0.18f), leather);
            foreach (float s in new[] { -0.4f, 0.4f })
                Box("SillonBrazo", armchair, new Vector3(s, 0.34f, 0f), new Vector3(0.14f, 0.5f, 0.85f), leather, collider: false);
            Primitive(PrimitiveType.Cylinder, "Mesita", root, new Vector3(1.5f, 0.3f, -2.9f), new Vector3(0.45f, 0.3f, 0.45f), darkWood, keepCollider: true);
            Primitive(PrimitiveType.Cylinder, "Vaso", root, new Vector3(1.45f, 0.66f, -2.9f), new Vector3(0.08f, 0.06f, 0.08f), redCup, keepCollider: false);
            // Carteles genéricos en la pared oeste y en la norte (bloques de color, sin nada real).
            Box("Cartel", root, new Vector3(-hx + 0.01f, 1.65f, -3.1f), new Vector3(0.01f, 0.95f, 0.68f), Mat("CartelPisoA", new Color(0.55f, 0.1f, 0.12f)), collider: false);
            Box("Cartel", root, new Vector3(-0.2f, 1.7f, hz - 0.01f), new Vector3(0.68f, 0.95f, 0.01f), Mat("CartelPisoB", new Color(0.12f, 0.14f, 0.2f)), collider: false);

            // --- Cocina americana en la pared norte (este) ---
            Box("Nevera", root, new Vector3(hx - 0.36f, 0.9f, hz - 0.36f), new Vector3(0.7f, 1.8f, 0.7f), white);
            Box("TiradorNevera", root, new Vector3(hx - 0.6f, 1.1f, hz - 0.73f), new Vector3(0.04f, 0.5f, 0.03f), chrome, collider: false);
            Box("Muebles", root, new Vector3(1.75f, 0.45f, hz - 0.33f), new Vector3(2.4f, 0.9f, 0.65f), darkWood);
            Box("Encimera", root, new Vector3(1.75f, 0.92f, hz - 0.35f), new Vector3(2.45f, 0.04f, 0.7f), counter, collider: false);
            Box("Fregadero", root, new Vector3(1.2f, 0.935f, hz - 0.35f), new Vector3(0.5f, 0.02f, 0.45f), chrome, collider: false);
            foreach (var (dx, dz) in new[] { (2.25f, -0.15f), (2.25f, 0.15f), (2.55f, -0.15f), (2.55f, 0.15f) })
                Primitive(PrimitiveType.Cylinder, "Fuego", root, new Vector3(dx, 0.945f, hz - 0.35f + dz), new Vector3(0.18f, 0.01f, 0.18f), black, keepCollider: false);
            Box("Altillos", root, new Vector3(1.75f, 1.9f, hz - 0.18f), new Vector3(2.4f, 0.6f, 0.35f), darkWood, collider: false);
            GarbageBag(root, new Vector3(hx - 0.5f, 0f, hz - 1.1f), rng);

            // --- Dormitorio (rincón sureste): cama contra la fachada este, junto a la ventana ---
            Box("CamaBase", root, new Vector3(hx - 1.05f, 0.15f, -2.75f), new Vector3(2.1f, 0.3f, 1.6f), darkWood);
            Box("Colchon", root, new Vector3(hx - 1.05f, 0.41f, -2.75f), new Vector3(2.0f, 0.22f, 1.5f), sheet, collider: false);
            Box("Edredon", root, new Vector3(hx - 1.4f, 0.55f, -2.75f), new Vector3(1.35f, 0.07f, 1.54f), duvet, collider: false);
            Box("Almohada", root, new Vector3(hx - 0.3f, 0.57f, -2.75f), new Vector3(0.4f, 0.12f, 1.1f), sheet, collider: false);
            Box("Cabecero", root, new Vector3(hx - 0.03f, 0.6f, -2.75f), new Vector3(0.06f, 0.9f, 1.6f), darkWood);
            Box("SudaderaTirada", root, new Vector3(1.6f, 0.015f, -1.6f), new Vector3(0.5f, 0.03f, 0.42f), Mat("SudaderaTirada", new Color(0.4f, 0.41f, 0.43f)), collider: false)
                .transform.localRotation = Quaternion.Euler(0f, 28f, 0f);

            // --- Armario con espejo en la pared norte (oeste) y la caja fuerte en el rincón noroeste ---
            const float wardX = -1.8f;
            Box("Armario", root, new Vector3(wardX, 1.1f, hz - 0.31f), new Vector3(1.8f, 2.2f, 0.62f), darkWood);
            Box("JuntaPuertas", root, new Vector3(wardX, 1.1f, hz - 0.625f), new Vector3(0.02f, 2.1f, 0.01f), black, collider: false);
            Box("Espejo", root, new Vector3(wardX - 0.45f, 1.2f, hz - 0.625f), new Vector3(0.6f, 1.6f, 0.01f), mirror, collider: false);
            foreach (float s in new[] { -0.08f, 0.08f })
                Box("Tirador", root, new Vector3(wardX + s, 1.1f, hz - 0.645f), new Vector3(0.03f, 0.25f, 0.03f), chrome, collider: false);
            var shoeBox = new[] { Mat("CajaZapatillasNaranja", new Color(0.85f, 0.42f, 0.1f)), black, Mat("CajaZapatillasRoja", new Color(0.6f, 0.08f, 0.08f)) };
            for (int i = 0; i < 3; i++)
                Box("CajaZapatillas", root, new Vector3(wardX + 1.2f, 0.065f + i * 0.13f, hz - 0.25f), new Vector3(0.34f, 0.13f, 0.22f), shoeBox[i], collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, (i - 1) * 6f, 0f);
            var wardrobe = new GameObject("Armario_Ropa").AddComponent<Wardrobe>();
            wardrobe.transform.SetParent(root, false);
            wardrobe.transform.localPosition = new Vector3(wardX, 0f, hz - 1.3f);

            var safePos = new Vector3(-hx + 0.3f, 0.3f, hz - 0.32f);
            Box("CajaFuerte", root, safePos, new Vector3(0.5f, 0.6f, 0.55f), safeMat);
            Box("PuertaCaja", root, safePos + new Vector3(0.02f, 0.02f, -0.28f), new Vector3(0.42f, 0.5f, 0.02f), Mat("PuertaCajaFuerte", new Color(0.24f, 0.25f, 0.26f)), collider: false);
            Primitive(PrimitiveType.Cylinder, "Dial", root, safePos + new Vector3(0.05f, 0.08f, -0.3f), new Vector3(0.12f, 0.015f, 0.12f), chrome, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box("Maneta", root, safePos + new Vector3(0.05f, -0.1f, -0.31f), new Vector3(0.16f, 0.03f, 0.03f), chrome, collider: false);
            var safe = new GameObject("CajaFuerte_Piso").AddComponent<SafeBox>();
            safe.transform.SetParent(root, false);
            safe.transform.localPosition = new Vector3(-hx + 0.5f, 0f, hz - 1.1f);

            // --- Luces: dos plafones cálidos y una en la entrada (de día entra la luz por las ventanas) ---
            foreach (var (pos, col, range, intensity) in new[]
                     {
                         (new Vector3(-2f, h - 0.3f, -1.2f), new Color(1f, 0.85f, 0.65f), 7f, 1.2f),
                         (new Vector3(2f, h - 0.3f, 1.6f), new Color(1f, 0.85f, 0.65f), 7f, 1.1f),
                         (new Vector3(-hx + 0.9f, h - 0.3f, liftZ), new Color(1f, 0.9f, 0.75f), 4f, 0.7f),
                     })
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.localPosition = pos;
                l.type = LightType.Point;
                l.color = col;
                l.range = range;
                l.intensity = intensity;
                l.shadows = LightShadows.None;
            }
            foreach (var p in new[] { new Vector3(-2f, h - 0.01f, -1.2f), new Vector3(2f, h - 0.01f, 1.6f) })
                Primitive(PrimitiveType.Cylinder, "Plafon", root, p, new Vector3(0.45f, 0.02f, 0.45f), lamp, keepCollider: false);
        }

        /// <summary>
        /// Pared de fachada del piso, de <paramref name="from"/> a <paramref name="to"/> a lo largo de X (o de Z), con su
        /// cara exterior en <paramref name="plane"/> y 0.3 m de grueso hacia <paramref name="inward"/>. En cada celda
        /// (un vano de la torre, que en la fachada está abierto) va una ventana de 2 m centrada: por fuera, ladrillo liso
        /// alrededor; por dentro, pared pintada, alféizar, marco blanco y cristal (con colisión: no se cae nadie). Fuera de
        /// las celdas, solo la capa pintada (por fuera ya está la fachada de la torre).
        /// </summary>
        static void FacadeWall(Transform root, bool alongX, float plane, float inward, float from, float to, (float c0, float c1)[] cells,
            Material wall, Material brick, Material glass, Material frame)
        {
            const float outer = 0.1f, inner = 0.2f, half = 1f;
            const float cellBottom = -0.12f, cellTop = 2.88f, top = AptHeight + 0.15f;
            Vector3 P(float along, float y, float across) => alongX ? new Vector3(along, y, across) : new Vector3(across, y, along);
            Vector3 S(float along, float y, float across) => alongX ? new Vector3(along, y, across) : new Vector3(across, y, along);
            float outerC = plane + inward * outer / 2f, innerC = plane + inward * (outer + inner / 2f);
            void Piece(string name, float a0, float a1, float y0, float y1, float across, float thick, Material m)
            {
                if (a1 - a0 < 0.01f || y1 - y0 < 0.01f) return;
                Box(name, root, P((a0 + a1) / 2f, (y0 + y1) / 2f, across), S(a1 - a0, y1 - y0, thick), m);
            }

            float cursor = from;
            foreach (var (c0, c1) in cells)
            {
                Piece("Pared", cursor, c0, 0f, top, innerC, inner, wall); // tramo sin ventana
                float o0 = (c0 + c1) / 2f - half, o1 = (c0 + c1) / 2f + half;
                foreach (var (across, thick, m, y0, y1) in new[] { (outerC, outer, brick, cellBottom, cellTop), (innerC, inner, wall, 0f, top) })
                {
                    Piece("Antepecho", c0, c1, y0, AptSill, across, thick, m);
                    Piece("Dintel", c0, c1, AptHead, y1, across, thick, m);
                    Piece("Jamba", c0, o0, AptSill, AptHead, across, thick, m);
                    Piece("Jamba", o1, c1, AptSill, AptHead, across, thick, m);
                }
                // Cristal con colisión, marco blanco con montante en medio y alféizar por dentro.
                float glassAcross = plane + inward * (outer + 0.04f);
                var pane = Box("Cristal", root, P((o0 + o1) / 2f, (AptSill + AptHead) / 2f, glassAcross), S(o1 - o0, AptHead - AptSill, 0.02f), glass);
                pane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                pane.AddComponent<NoClimb>();
                pane.AddComponent<NoCover>();
                float frameAcross = plane + inward * (outer + 0.06f);
                foreach (float a in new[] { o0 + 0.03f, (o0 + o1) / 2f, o1 - 0.03f })
                    Box("Montante", root, P(a, (AptSill + AptHead) / 2f, frameAcross), S(0.06f, AptHead - AptSill, 0.06f), frame, collider: false);
                foreach (float y in new[] { AptSill + 0.03f, AptHead - 0.03f })
                    Box("Travesano", root, P((o0 + o1) / 2f, y, frameAcross), S(o1 - o0, 0.06f, 0.06f), frame, collider: false);
                Box("Alfeizar", root, P((o0 + o1) / 2f, AptSill + 0.02f, plane + inward * (outer + inner + 0.08f)), S(o1 - o0 + 0.2f, 0.05f, 0.2f), frame, collider: false);
                cursor = c1;
            }
            Piece("Pared", cursor, to, 0f, top, innerC, inner, wall);
        }

        /// <summary>Cristal transparente (URP Lit en modo transparente, alfa del color).</summary>
        static Material GlassMat(string name, Color color)
        {
            var m = Mat(name, color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetFloat("_Smoothness", 0.9f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
