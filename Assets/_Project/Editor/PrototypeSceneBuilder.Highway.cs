using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Autopista hundida (tipo las del Southside: en trinchera, con muros de contención de hormigón) pegada al borde
    /// oeste del mundo, x −300…−275. Va de norte a sur por debajo de las calles z = ±80, que la cruzan en puente; al
    /// norte de la calle z = 80 entra en un túnel (encima sigue el oeste lejano) y al sur se pierde en la niebla.
    /// Lleva dos calzadas de 3 carriles con mediana de hormigón, farolas altas en la mediana, un pórtico con carteles
    /// verdes sin marcas, grafitis en los muros, basura y nieve en los arcenes, y tráfico decorativo (HighwayCar).
    /// Se cae dentro, pero no se puede entrar: muro, pretil y malla en los dos bordes.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float HighwayMinX = -300f, HighwayMaxX = -275f; // caras exteriores de los muros (y borde del "Suelo")
        const float HighwayWall = 1f;                          // grosor de los muros de contención
        const float HighwayDepth = 7f;                         // la calzada queda a y = −7
        const float HighwayDeck = 1.2f;                        // grosor del tablero de los puentes y del techo del túnel
        const float HighwayMedianX = -287.5f, HighwayMedianWidth = 0.8f;
        const float HighwaySouthZ = -480f;                     // fondo oscuro del túnel sur (bajo el canal)
        const float HighwaySouthTunnelZ = -440f;               // boca del túnel sur: pasa bajo la vía de mercancías, Dock y el canal
        const float HighwayTunnelZ = 90f;                      // boca del túnel, en el borde norte del puente de z = 80
        const float HighwayTunnelEndZ = 130f;                  // fondo oscuro del túnel
        static float HighwayInnerMinX => HighwayMinX + HighwayWall;  // −299
        static float HighwayInnerMaxX => HighwayMaxX - HighwayWall;  // −276
        static float HighwayCarriageway => HighwayMedianX - HighwayMedianWidth / 2f - HighwayInnerMinX; // 11.1 m por sentido

        // Puentes de las calles z = ±80: calzada y las dos aceras (el de z = 80 sigue como techo del túnel).
        static readonly Rect HighwayBridgeNorth = Rect.MinMaxRect(HighwayMinX, 70f, HighwayMaxX, 90f);
        static readonly Rect HighwayBridgeSouth = Rect.MinMaxRect(HighwayMinX, -90f, HighwayMaxX, -70f);
        // Tramos al aire libre (entre puentes y al sur), donde van pretiles y malla.
        static readonly (float z0, float z1)[] HighwayOpenStretches = { (HighwaySouthTunnelZ, -90f), (-70f, 70f) };

        static void BuildHighway(Transform world)
        {
            var root = new GameObject("Autopista").transform;
            root.SetParent(world, false);
            var rng = new System.Random(2016);

            var wallMat = TexturedMat("MuroContencion", ProceduralTextures.RetainingWall("MuroContencion", 41));
            var roadMat = TexturedMat("Autopista", ProceduralTextures.Highway("Autopista", HighwayCarriageway, 42));
            var concrete = Mat("HormigonPuente", new Color(0.58f, 0.57f, 0.54f));
            var barrier = Mat("BarreraMediana", new Color(0.66f, 0.65f, 0.61f));
            float floorY = -HighwayDepth;
            float length = HighwayTunnelEndZ - HighwaySouthZ, midZ = (HighwayTunnelEndZ + HighwaySouthZ) / 2f;

            // Calzadas (con colisión): la del oeste va hacia el sur y está girada 180° para que la línea amarilla quede
            // junto a la mediana en las dos.
            float cw = HighwayCarriageway;
            var roadTile = new Vector2(cw, ProceduralTextures.HighwayTile);
            TexturedBox("Calzada_Sur", root, new Vector3(HighwayInnerMinX + cw / 2f, floorY - 0.15f, midZ), new Vector3(cw, 0.3f, length), roadMat, roadTile)
                .transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TexturedBox("Calzada_Norte", root, new Vector3(HighwayInnerMaxX - cw / 2f, floorY - 0.15f, midZ), new Vector3(cw, 0.3f, length), roadMat, roadTile);
            Box("BajoMediana", root, new Vector3(HighwayMedianX, floorY - 0.15f, midZ), new Vector3(HighwayMedianWidth, 0.3f, length), concrete);

            // Mediana de hormigón (tipo New Jersey: base ancha y cuerpo estrecho).
            Box("Mediana", root, new Vector3(HighwayMedianX, floorY + 0.2f, midZ), new Vector3(HighwayMedianWidth, 0.4f, length), barrier);
            Box("MedianaCuerpo", root, new Vector3(HighwayMedianX, floorY + 0.7f, midZ), new Vector3(0.35f, 0.6f, length), barrier);

            // Muros de contención, de la calzada al nivel de la calle, todo el largo (también dentro del túnel).
            var wallTile = new Vector2(ProceduralTextures.WallTileWidth, ProceduralTextures.WallTileHeight);
            foreach (float x in new[] { HighwayMinX + HighwayWall / 2f, HighwayMaxX - HighwayWall / 2f })
                TexturedBox("MuroContencion", root, new Vector3(x, floorY / 2f, midZ), new Vector3(HighwayWall, HighwayDepth, length), wallMat, wallTile);

            BuildHighwayBridges(root, concrete);
            BuildHighwayTunnel(root, concrete);
            BuildHighwayEdges(root, concrete);
            BuildHighwayFurniture(root, rng);
            BuildHighwayTraffic();
        }

        /// <summary>Tableros de los puentes (con la calle encima), vigas, pila en la mediana y pretiles con malla.</summary>
        static void BuildHighwayBridges(Transform root, Material concrete)
        {
            var steel = Mat("AceroEstructura", new Color(0.32f, 0.36f, 0.33f));
            float span = HighwayInnerMaxX - HighwayInnerMinX, cx = (HighwayInnerMinX + HighwayInnerMaxX) / 2f;
            foreach (var bridge in new[] { HighwayBridgeNorth, HighwayBridgeSouth })
            {
                var group = new GameObject($"Puente_{(bridge.center.y > 0f ? "Norte" : "Sur")}").transform;
                group.SetParent(root, false);
                // El tablero del norte lo pone el techo del túnel, que empieza en el mismo sitio.
                if (bridge.center.y < 0f)
                    Box("Tablero", group, new Vector3(bridge.center.x, -HighwayDeck / 2f, bridge.center.y), new Vector3(bridge.width, HighwayDeck, bridge.height), concrete);

                // Vigas de canto de un muro al otro, cada 2.5 m, y la pila de la mediana (viga cabecera y tres pilares).
                for (float z = bridge.yMin + 1.25f; z < bridge.yMax; z += 2.5f)
                    Box("Viga", group, new Vector3(cx, -HighwayDeck - 0.4f, z), new Vector3(span, 0.8f, 0.45f), steel, collider: false);
                Box("Cabecera", group, new Vector3(HighwayMedianX, -HighwayDeck - 1.1f, bridge.center.y), new Vector3(1.2f, 0.6f, bridge.height - 1f), concrete, collider: false);
                foreach (float t in new[] { -0.35f, 0f, 0.35f })
                {
                    float pierTop = -HighwayDeck - 1.4f;
                    Primitive(PrimitiveType.Cylinder, "Pilar", group, new Vector3(HighwayMedianX, (pierTop - HighwayDepth) / 2f, bridge.center.y + t * bridge.height),
                        new Vector3(0.75f, (pierTop + HighwayDepth) / 2f, 0.75f), concrete, keepCollider: false);
                }

                // Pretiles con malla en los bordes que dan a la trinchera (el del norte del puente norte es la boca del túnel).
                var edges = bridge.center.y > 0f ? new[] { bridge.yMin } : new[] { bridge.yMin, bridge.yMax };
                foreach (float z in edges)
                {
                    float inward = z == bridge.yMin ? 0.2f : -0.2f;
                    Box("Pretil", group, new Vector3(cx, 0.45f, z + inward), new Vector3(span + 2f, 0.9f, 0.4f), concrete);
                    Box("Cara", group, new Vector3(cx, -HighwayDeck / 2f - 0.2f, z), new Vector3(span, HighwayDeck + 0.4f, 0.1f), concrete, collider: false);
                    if (chainLink)
                        FenceSegment(group, new Vector3(HighwayInnerMinX, 0.9f, z + inward), new Vector3(HighwayInnerMaxX, 0.9f, z + inward), 1.8f, chainLink,
                            Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 2.5f, 0.05f, topRail: true);
                }
            }
        }

        /// <summary>
        /// Túneles: al norte del puente de z = 80 (el techo es el suelo del oeste lejano) y al sur de z = −440 (bajo la vía
        /// de mercancías, W DOCK ST y el canal). Boca con dintel de hormigón, fluorescentes en el techo y un fondo negro
        /// donde desaparecen los coches. El del sur lleva pretil con malla encima de la boca y en el borde oeste del techo.
        /// </summary>
        static void BuildHighwayTunnel(Transform root, Material concrete)
        {
            BuildHighwayTunnel(root, concrete, "Tunel", HighwayTunnelZ, HighwayTunnelEndZ, HighwayBridgeNorth.yMin, WorldNorthZ);
            BuildHighwayTunnel(root, concrete, "Tunel_Sur", HighwaySouthTunnelZ, HighwaySouthZ, SouthQuayZ, HighwaySouthTunnelZ);
            // Encima de la boca sur: pretil con malla (no se cae a la trinchera) y valla en el borde oeste del mundo.
            float span = HighwayInnerMaxX - HighwayInnerMinX, cx = (HighwayInnerMinX + HighwayInnerMaxX) / 2f;
            Box("PretilBoca", root, new Vector3(cx, 0.45f, HighwaySouthTunnelZ - 0.2f), new Vector3(span + 2f, 0.9f, 0.4f), concrete);
            if (chainLink)
            {
                FenceSegment(root, new Vector3(HighwayInnerMinX, 0.9f, HighwaySouthTunnelZ - 0.2f), new Vector3(HighwayInnerMaxX, 0.9f, HighwaySouthTunnelZ - 0.2f), 1.8f, chainLink,
                    Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 2.5f, 0.05f, topRail: true);
                FenceSegment(root, new Vector3(HighwayMinX + 0.3f, 0f, HighwaySouthTunnelZ), new Vector3(HighwayMinX + 0.3f, 0f, SouthQuayZ), 2.2f, chainLink,
                    Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 3f, 0.05f, topRail: true);
            }
            ColliderBox(root, "LimiteMundo", new Vector3(HighwayMinX + 0.1f, 2.5f, (HighwaySouthTunnelZ + SouthQuayZ) / 2f), new Vector3(0.2f, 5f, HighwaySouthTunnelZ - SouthQuayZ));
        }

        /// <summary>Un túnel: de la boca (<paramref name="mouthZ"/>) al fondo (<paramref name="endZ"/>); techo de roofZ0 a roofZ1.</summary>
        static void BuildHighwayTunnel(Transform root, Material concrete, string name, float mouthZ, float endZ, float roofZ0, float roofZ1)
        {
            var group = new GameObject(name).transform;
            group.SetParent(root, false);
            float inward = Mathf.Sign(endZ - mouthZ);
            Box("Techo", group, new Vector3((HighwayMinX + HighwayMaxX) / 2f, -HighwayDeck / 2f, (roofZ0 + roofZ1) / 2f),
                new Vector3(HighwayMaxX - HighwayMinX, HighwayDeck, roofZ1 - roofZ0), concrete);

            float span = HighwayInnerMaxX - HighwayInnerMinX, cx = (HighwayInnerMinX + HighwayInnerMaxX) / 2f;
            var dark = Mat("HormigonOscuro", new Color(0.3f, 0.3f, 0.29f));
            Box("Dintel", group, new Vector3(cx, -HighwayDeck - 0.6f, mouthZ + inward * 0.5f), new Vector3(span, 1.2f, 1f), concrete, collider: false);
            Box("TechoInterior", group, new Vector3(cx, -HighwayDeck - 0.02f, (mouthZ + endZ) / 2f), new Vector3(span, 0.04f, Mathf.Abs(endZ - mouthZ)), dark, collider: false);
            Box("Fondo", group, new Vector3(cx, -(HighwayDeck + HighwayDepth) / 2f, endZ - inward * 0.5f), new Vector3(span, HighwayDepth - HighwayDeck, 1f),
                Mat("FondoTunel", new Color(0.015f, 0.015f, 0.018f)));
            var lamp = EmissiveMat("LuzTunel", new Color(1f, 0.85f, 0.6f), 2.2f);
            float length = Mathf.Abs(endZ - mouthZ);
            foreach (float x in new[] { HighwayInnerMinX + HighwayCarriageway / 2f, HighwayInnerMaxX - HighwayCarriageway / 2f })
                for (float d = 3f; d < length - 2f; d += 6f)
                    Box("Fluorescente", group, new Vector3(x, -HighwayDeck - 0.08f, mouthZ + inward * d), new Vector3(0.35f, 0.1f, 2.4f), lamp, collider: false);
            var light = new GameObject("LuzTunel").AddComponent<Light>();
            light.transform.SetParent(group, false);
            light.transform.localPosition = new Vector3(cx, -HighwayDeck - 1.5f, mouthZ + inward * 12f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.55f);
            light.range = 18f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
        }

        /// <summary>
        /// Bordes de la trinchera en los tramos al aire libre: pretil de hormigón sobre cada muro y malla encima, para
        /// que no se pueda saltar a la autopista. La del oeste es también el borde del mundo.
        /// </summary>
        static void BuildHighwayEdges(Transform root, Material concrete)
        {
            var group = new GameObject("Bordes").transform;
            group.SetParent(root, false);
            foreach (float x in new[] { HighwayMinX + HighwayWall / 2f, HighwayMaxX - HighwayWall / 2f })
                foreach (var (z0, z1) in HighwayOpenStretches)
                {
                    Box("Pretil", group, new Vector3(x, 0.45f, (z0 + z1) / 2f), new Vector3(HighwayWall, 0.9f, z1 - z0), concrete);
                    if (chainLink)
                        FenceSegment(group, new Vector3(x, 0.9f, z0), new Vector3(x, 0.9f, z1), 1.8f, chainLink,
                            Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 3f, 0.05f, topRail: true);
                }
        }

        /// <summary>Farolas altas en la mediana, pórtico con carteles, grafitis, basura, nieve y un coche averiado en el arcén.</summary>
        static void BuildHighwayFurniture(Transform root, System.Random rng)
        {
            var group = new GameObject("Detalle").transform;
            group.SetParent(root, false);
            float floorY = -HighwayDepth;
            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lamp = Mat("LuzSodio", SodiumLight);

            // Farolas de doble brazo sobre la mediana, cada 40 m al aire libre.
            for (float z = HighwaySouthTunnelZ + 20f; z < 70f; z += 40f)
            {
                if (HighwayBridgeSouth.yMin - 4f < z && z < HighwayBridgeSouth.yMax + 4f) continue;
                var p = new Vector3(HighwayMedianX, floorY + 1f, z);
                Primitive(PrimitiveType.Cylinder, "PosteAutopista", group, p + Vector3.up * 5.5f, new Vector3(0.25f, 5.5f, 0.25f), pole, keepCollider: false);
                foreach (float side in new[] { -1f, 1f })
                {
                    Box("Brazo", group, p + new Vector3(side * 1.6f, 11f, 0f), new Vector3(3.2f, 0.15f, 0.15f), pole, collider: false);
                    Box("Lampara", group, p + new Vector3(side * 3.1f, 10.85f, 0f), new Vector3(0.9f, 0.22f, 0.5f), lamp, collider: false);
                }
                var light = new GameObject("Luz").AddComponent<Light>();
                light.transform.SetParent(group, false);
                light.transform.localPosition = p + Vector3.up * 10f;
                light.type = LightType.Point;
                light.color = SodiumLight;
                light.range = 22f;
                light.intensity = 3f;
                light.shadows = LightShadows.None;
                light.gameObject.AddComponent<NightLight>(); // apagada de día
            }

            // Pórtico de acero con carteles verdes (texto genérico, sin nombres de autopistas reales) en z = −30: se ve
            // desde el instituto. Cada cartel mira a los coches de su sentido.
            float gantryZ = -30f, gantryY = floorY + 6f;
            var steel = Mat("AceroEstructura", new Color(0.32f, 0.36f, 0.33f));
            foreach (float x in new[] { HighwayInnerMinX + 0.6f, HighwayInnerMaxX - 0.6f })
                Box("PieGantry", group, new Vector3(x, floorY + 3.5f, gantryZ), new Vector3(0.5f, 7f, 0.5f), steel, collider: false);
            Box("VigaGantry", group, new Vector3(HighwayMedianX, gantryY + 0.8f, gantryZ), new Vector3(HighwayInnerMaxX - HighwayInnerMinX, 0.5f, 0.6f), steel, collider: false);
            var green = new Color(0.05f, 0.33f, 0.18f);
            var ink = new Color(0.95f, 0.95f, 0.92f);
            var south = SignMat("Letrero_ExpresswaySur", "EXPRESSWAY SOUTH", green, ink, 91, 0.25f);
            var north = SignMat("Letrero_ExpresswayNorte", "EXPRESSWAY NORTH", green, ink, 92, 0.25f);
            var exit = SignMat("Letrero_Exit", "EXIT ONLY", green, ink, 93, 0.25f);
            float Aspect(Material m) => (float)m.mainTexture.width / m.mainTexture.height;
            float southX = HighwayInnerMinX + HighwayCarriageway / 2f, northX = HighwayInnerMaxX - HighwayCarriageway / 2f;
            SignQuad(group, new Vector3(southX, gantryY, gantryZ + 0.35f), Vector3.forward, 7.5f, 7.5f / Aspect(south), south);
            SignQuad(group, new Vector3(northX, gantryY, gantryZ - 0.35f), Vector3.back, 7.5f, 7.5f / Aspect(north), north);
            SignQuad(group, new Vector3(northX + 3f, gantryY - 1.2f, gantryZ - 0.36f), Vector3.back, 3.2f, 3.2f / Aspect(exit), exit);

            // Grafitis en los muros, sobre todo donde se ven desde los puentes.
            if (graffitiMats != null)
                for (int i = 0; i < 14; i++)
                {
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    if (!g) continue;
                    bool westWall = rng.Next(2) == 0;
                    float z = Range(rng, -140f, 66f);
                    float x = westWall ? HighwayInnerMinX : HighwayInnerMaxX;
                    WallGraffiti(group, new Vector3(x, floorY + Range(rng, 1.6f, 3.5f), z), westWall ? Vector3.right : Vector3.left, Range(rng, 3f, 6f), g);
                }

            // Basura, nieve sucia y barro en los arcenes (junto a los muros y a la mediana).
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            float[] shoulderX =
            {
                HighwayInnerMinX + 0.5f, HighwayMedianX - HighwayMedianWidth / 2f - 0.4f,
                HighwayMedianX + HighwayMedianWidth / 2f + 0.4f, HighwayInnerMaxX - 0.5f,
            };
            for (int i = 0; i < 90; i++)
            {
                float x = shoulderX[rng.Next(shoulderX.Length)] + Range(rng, -0.3f, 0.3f);
                var p = new Vector3(x, floorY, Range(rng, HighwaySouthTunnelZ + 10f, HighwayTunnelZ - 2f));
                if (i < 40) PlaceLitter(group, rng.Next(2) == 0 ? paper : can, p, rng);
                else if (i < 70) SnowMound(group, p, new Vector3(Range(rng, 0.4f, 0.8f), Range(rng, 0.1f, 0.3f), Range(rng, 2f, 7f)), Range(rng, -2f, 2f));
                else GarbageBag(group, p, rng);
            }
            var oil = LoadMat("Aceite");
            var dirt = LoadMat("Suciedad");
            for (int i = 0; i < 40; i++)
            {
                float x = Range(rng, HighwayInnerMinX + 1f, HighwayInnerMaxX - 1f);
                if (Mathf.Abs(x - HighwayMedianX) < 1f) continue;
                Decal(group, new Vector3(x, floorY + 0.003f + (i % 20) * 0.0002f, Range(rng, HighwaySouthTunnelZ + 10f, HighwayTunnelZ)), Range(rng, 0f, 360f),
                    Vector2.one * Range(rng, 0.8f, 2.2f), i % 2 == 0 ? oil : dirt);
            }

            // Un coche averiado en el arcén de la calzada sur, con el triángulo de emergencia detrás.
            CarModel(group, "CocheAveriado", new Vector3(HighwayInnerMinX + 1.6f, floorY, -120f), 180f, CarStyle.Sedan, carBodyPaints[rng.Next(carBodyPaints.Length)], rng);
            var triangle = Mat("TrianguloAveria", new Color(0.85f, 0.25f, 0.1f));
            Box("Triangulo", group, new Vector3(HighwayInnerMinX + 2.5f, floorY + 0.25f, -112f), new Vector3(0.5f, 0.45f, 0.05f), triangle, collider: false);
        }

        /// <summary>
        /// Tráfico decorativo: coches con <see cref="HighwayCar"/> que recorren cada carril en bucle, del sur (en la
        /// niebla) a dentro del túnel o al revés. Se crean fuera de "Mundo", que es estático, y sin colisiones.
        /// </summary>
        static void BuildHighwayTraffic()
        {
            var root = new GameObject("Trafico_Autopista").transform;
            var rng = new System.Random(2017);
            float y = -HighwayDepth;
            float routeNorth = HighwayTunnelEndZ - 6f, routeSouth = HighwaySouthZ + 6f; // de fondo a fondo de los dos túneles
            var styles = new[] { CarStyle.Sedan, CarStyle.Sedan, CarStyle.Coupe, CarStyle.Suv, CarStyle.Suv, CarStyle.Van };
            for (int lane = 0; lane < 3; lane++)
            {
                float offset = ProceduralTextures.HighwayShoulderLeft + (lane + 0.5f) * ProceduralTextures.HighwayLane;
                float northX = HighwayMedianX + HighwayMedianWidth / 2f + offset;   // hacia el norte: carriles al este de la mediana
                float southX = HighwayMedianX - HighwayMedianWidth / 2f - offset;   // hacia el sur: al oeste
                float speed = 25f - lane * 3f;                                       // el carril 0, junto a la mediana, es el rápido
                foreach (var (x, from, to, tag) in new[] { (northX, routeSouth, routeNorth, "N"), (southX, routeNorth, routeSouth, "S") })
                {
                    float length = Mathf.Abs(to - from);
                    int cars = 3 + rng.Next(2);
                    for (int i = 0; i < cars; i++)
                    {
                        var car = CarModel(root, $"Coche_{tag}{lane}_{i}", Vector3.zero, 0f, styles[rng.Next(styles.Length)],
                            carBodyPaints[rng.Next(carBodyPaints.Length)], rng, weathered: true, lightsOn: true, collider: false);
                        float start = (i + (float)rng.NextDouble() * 0.6f) * length / cars;
                        car.gameObject.AddComponent<HighwayCar>().Configure(new Vector3(x, y, from), new Vector3(x, y, to), speed + Range(rng, -1.5f, 1.5f), start);
                    }
                }
            }
        }
    }
}
