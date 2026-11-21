using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Detalle vial de toda la ciudad (regla "ciudad con detalle", docs/estructuras/calles.md): nombres de calle en
    /// placas verdes, semáforos que funcionan en los cruces de las calles grandes, STOP con línea de detención y paso
    /// de cebra donde una calle de barrio llega a otra, flechas de carril en las llegadas a los cruces, postes de madera
    /// con cables a lo largo de las calles grandes, límites de velocidad, y alcantarillas, sumideros y bocas de
    /// incendio en las calles de barrio. Va después de todas las zonas: comprueba con la física que cada poste o señal
    /// cabe donde va (no se mete en farolas, árboles, vallas ni coches).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float PaintLift = 0.004f; // la pintura y las calcomanías, por encima de la calzada
        static readonly Color StreetSignGreen = new(0.03f, 0.36f, 0.2f);

        /// <summary>Calle con nombre: rectángulo de la calzada en el mundo.</summary>
        class Street
        {
            public Rect Rect;
            public string Name;
            public bool Main;
            public float Sidewalk; // ancho de sus aceras: lo que mide el paso de cebra de quien llega a ella
            public bool AlongX => Rect.width >= Rect.height;
            public float Width => AlongX ? Rect.height : Rect.width;
            public float Center => AlongX ? Rect.center.y : Rect.center.x;
            public float Start => AlongX ? Rect.xMin : Rect.yMin;
            public float End => AlongX ? Rect.xMax : Rect.yMax;
            public float Top => Main ? MainRoadTop : RoadTop;
            public Vector3 Point(float t, float across, float y) => AlongX ? new Vector3(t, y, Center + across) : new Vector3(Center + across, y, t);
        }

        /// <summary>
        /// Todas las calles con su nombre. Nombres inventados (no son calles reales de Chicago), con el formato de
        /// allí: W para las este–oeste, S para las norte–sur. Algunas dan nombre a su facción (Larkin St, Dunmore St,
        /// Kessler Ct) y la sur, a Ashgrove Homes. La tabla está en docs/estructuras/calles.md.
        /// </summary>
        static List<Street> NamedStreets()
        {
            var list = new List<Street>();
            void AddMain(Rect r, string n) => list.Add(new Street { Rect = r, Name = n, Main = true, Sidewalk = SidewalkWidth });
            void AddLocal(Rect r, string n, float sidewalk = HousesSidewalk) => list.Add(new Street { Rect = r, Name = n, Sidewalk = sidewalk });

            float h = RoadWidth / 2f, avenueEast = MainRoadX + h;
            AddMain(Rect.MinMaxRect(-MainRoadHalfLength, MainRoadZ - h, avenueEast, MainRoadZ + h), "W BRAXTON ST");    // norte, z = 80
            AddMain(Rect.MinMaxRect(-MainRoadHalfLength, -MainRoadZ - h, avenueEast, -MainRoadZ + h), "W ASHGROVE ST"); // sur, z = −80
            AddMain(Rect.MinMaxRect(MainRoadX - h, SouthQuayZ, MainRoadX + h, WorldNorthZ), "S MARLOW AVE");   // avenida
            AddMain(Rect.MinMaxRect(-MainRoadX - h, SouthQuayZ, -MainRoadX + h, WorldNorthZ), "S CALDER AVE"); // oeste, x = −110

            // Barrio de casas este (Larkin): A, B y C (C1 y C2; el quiebro Cq no cuenta).
            AddLocal(HousesRoads[0], "W LARKIN ST");
            AddLocal(HousesRoads[1], "S TENNEY AVE");
            AddLocal(HousesRoads[2], "W HOLLIS ST");
            AddLocal(HousesRoads[3], "W HOLLIS ST");
            // Copia oeste (Dunmore): A', B' y C'.
            AddLocal(HousesWestRect(HousesRoads[0]), "W DUNMORE ST");
            AddLocal(HousesWestRect(HousesRoads[1]), "S BRISCOE AVE");
            AddLocal(HousesWestRect(HousesRoads[2]), "W CROWELL ST");
            AddLocal(HousesWestRect(HousesRoads[3]), "W CROWELL ST");
            // Ampliaciones y oeste lejano: D, E, F (Fe al este), G, H y J.
            AddLocal(WestExtRoads[0], "W DORSEY ST");
            AddLocal(WestExtRoads[1], "W EMBRY ST");
            AddLocal(NorthEastRoads[0], "W FENWICK ST");
            AddLocal(NorthEastRoads[1], "S TENNEY AVE");
            AddLocal(NorthEastRoads[2], "W HARLAND ST");
            AddLocal(NorthWestRoads[0], "W FENWICK ST");
            AddLocal(NorthWestRoads[1], "S BRISCOE AVE");
            AddLocal(NorthWestRoads[2], "W HARLAND ST");
            AddLocal(FarWestRoads[0], "S JUDSON AVE");
            AddLocal(FarWestRoads[1], "W DORSEY ST");
            AddLocal(FarWestRoads[2], "W EMBRY ST");
            AddLocal(FarWestRoads[3], "W FENWICK ST");
            AddLocal(FarWestRoads[4], "W HARLAND ST");
            // Barrio de viviendas (Kessler Courts): la calle que sale de la avenida.
            AddLocal(Rect.MinMaxRect(DistrictMinX - SidewalkWidth, -DistrictRoadWidth / 2f, DistrictMaxX, DistrictRoadWidth / 2f), "W KESSLER CT", SidewalkWidth);
            // Barrio norte (North.cs): bulevar, calle del borde, J, G, la nueva norte–sur y la calle sin salida. La diagonal no
            // es un rectángulo: su detalle lo pone BuildNorthStreetDetail.
            foreach (var (r, n, sidewalk) in NorthBandStreets()) AddLocal(r, n, sidewalk);
            foreach (var (r, n, sidewalk) in SouthBandStreets()) AddLocal(r, n, sidewalk); // polígono industrial (South.cs)
            return list;
        }

        static Street StreetAt(List<Street> streets, Vector3 p, Street except)
        {
            foreach (var s in streets)
                if (s != except && s.Name != except?.Name && s.Rect.Contains(new Vector2(p.x, p.z))) return s;
            return null;
        }

        /// <summary>¿Cabe algo de <paramref name="radius"/> de radio y hasta 3.2 m de alto en ese punto del suelo?</summary>
        static bool SpotFree(Vector3 ground, float radius = 0.35f)
        {
            Physics.SyncTransforms();
            return !Physics.CheckCapsule(ground + Vector3.up * (0.55f + radius), ground + Vector3.up * 3.2f, radius, ~0, QueryTriggerInteraction.Ignore);
        }

        static float YawOf(Vector3 forward) => Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        // Materiales del detalle vial (los prepara BuildStreetDetail).
        static Material sdGalvanized, sdSignalBody, sdPaint, sdStop, sdStopBack, sdHydrant, sdWood, sdTransformer, sdCable, sdIron, sdGrate, sdDirt, sdSignBack;
        static Material[] sdLensDark, sdLensLit;
        static Material sdHandLit, sdWalkLit, sdPedDark;
        static readonly Dictionary<string, Material> sdNameSigns = new();

        static void BuildStreetDetail(Transform world)
        {
            var group = new GameObject("DetalleVial").transform;
            group.SetParent(world, false);
            var rng = new System.Random(1212);

            sdGalvanized = Mat("PosteSenal", new Color(0.52f, 0.54f, 0.55f));
            sdSignalBody = Mat("CuerpoSemaforo", new Color(0.07f, 0.09f, 0.08f));
            sdPaint = Mat("PinturaVial", new Color(0.8f, 0.8f, 0.76f));
            sdHydrant = Mat("BocaIncendio", new Color(0.78f, 0.62f, 0.12f));
            sdWood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            sdTransformer = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            sdCable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            sdIron = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            sdGrate = Mat("RejillaSumidero", new Color(0.05f, 0.05f, 0.055f));
            sdSignBack = Mat("TraseraSenal", new Color(0.5f, 0.51f, 0.52f));
            sdDirt = LoadMat("Suciedad");
            sdStop = CullBack(CutoutMat("SenalStop", ProceduralTextures.StopSign("SenalStop", false, 41)));
            sdStopBack = CullBack(CutoutMat("SenalStopTrasera", ProceduralTextures.StopSign("SenalStopTrasera", true, 42)));
            sdLensDark = new[]
            {
                Mat("LenteRojaApagada", new Color(0.22f, 0.04f, 0.03f)),
                Mat("LenteAmbarApagada", new Color(0.24f, 0.15f, 0.03f)),
                Mat("LenteVerdeApagada", new Color(0.03f, 0.16f, 0.1f)),
            };
            sdLensLit = new[]
            {
                EmissiveMat("LenteRoja", new Color(1f, 0.1f, 0.06f), 2.2f),
                EmissiveMat("LenteAmbar", new Color(1f, 0.55f, 0.05f), 2.2f),
                EmissiveMat("LenteVerde", new Color(0.05f, 0.85f, 0.45f), 1.6f),
            };
            sdHandLit = EmissiveMat("PeatonMano", new Color(1f, 0.45f, 0.1f), 3f);
            sdWalkLit = EmissiveMat("PeatonAnda", new Color(0.95f, 0.95f, 0.9f), 3f);
            sdPedDark = Mat("PeatonApagado", new Color(0.06f, 0.06f, 0.06f));
            sdNameSigns.Clear();

            var streets = NamedStreets();
            BuildStreetApproaches(group, streets, rng);
            BuildTrafficSignals(streets);
            BuildLaneArrows(group, streets);
            BuildMainRoadPoles(group, rng);
            BuildSpeedLimitSigns(group, streets, rng);
            BuildLocalSewers(group, streets, rng);
            BuildNorthStreetDetail(group); // la diagonal y los semáforos del bulevar (North.cs)
            BuildSouthStreetDetail(group); // pasos a nivel, la curva y los puentes levadizos (South.cs)
        }

        static Material CullBack(Material m)
        {
            m.SetFloat("_Cull", (float)CullMode.Back);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- Llegadas de calles de barrio: STOP, línea, paso de cebra, nombres y bocas de incendio ----------

        static void BuildStreetApproaches(Transform group, List<Street> streets, System.Random rng)
        {
            var crosswalks = new Dictionary<int, Material>();
            foreach (var s in streets)
            {
                if (s.Main) continue;
                float hw = s.Width / 2f;
                foreach (int dir in new[] { -1, 1 })
                {
                    float end = dir > 0 ? s.End : s.Start;
                    var cross = StreetAt(streets, s.Point(end + dir * 0.5f, 0f, 0f), s);
                    if (cross == null || cross.AlongX == s.AlongX) continue; // sin salida, o la misma calle que sigue
                    var d = s.AlongX ? new Vector3(dir, 0f, 0f) : new Vector3(0f, 0f, dir);
                    float rightSign = s.AlongX ? -dir : dir; // la derecha de quien llega, en la coordenada "across"
                    float sw = cross.Sidewalk;
                    float yaw = YawOf(d);
                    // Bulevar (North.cs): cada calzada es de un sentido (la sur al este, la norte al oeste) y tiene preferencia
                    // en las calles pequeñas; en las grandes manda el semáforo (BuildNorthStreetDetail): solo paso de cebra
                    // y línea de detención de lado a lado en el extremo al que se llega.
                    bool boulevard = s.Name == BoulevardName;
                    if (boulevard && !cross.Main) continue;

                    // Paso de cebra donde la acera de la calle grande cruza la boca.
                    if (cross.Main)
                    {
                        int key = Mathf.RoundToInt(s.Width);
                        if (!crosswalks.TryGetValue(key, out var cw))
                            crosswalks[key] = cw = TransparentMat($"PasoCebra{key}", ProceduralTextures.Crosswalk($"PasoCebra{key}", s.Width, 60 + key), new Color(1f, 1f, 1f, 0.95f), 0.1f);
                        Decal(group, s.Point(end - dir * sw / 2f, 0f, s.Top + PaintLift), yaw, new Vector2(s.Width, sw - 0.2f), cw);
                    }

                    if (boulevard)
                    {
                        if ((dir > 0) == (s.Rect.center.y < BoulevardZ))
                            Box("LineaStop", group, s.Point(end - dir * (sw + 0.35f), 0f, s.Top + PaintLift / 2f),
                                new Vector3(s.Width - 0.6f, PaintLift, 0.4f), sdPaint, collider: false).transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        continue;
                    }

                    // Línea de detención en el carril que llega (de la doble amarilla al bordillo).
                    Box("LineaStop", group, s.Point(end - dir * (sw + 0.35f), rightSign * (hw / 2f + 0.1f), s.Top + PaintLift / 2f),
                        new Vector3(hw - 0.4f, PaintLift, 0.4f), sdPaint, collider: false).transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                    // STOP en la esquina de la derecha, con las dos placas de nombre encima (como en Chicago).
                    for (float back = 0f; back <= 4.5f; back += 1.5f)
                    {
                        var p = s.Point(end - dir * (sw + 1.3f + back), rightSign * (hw + 0.7f), SidewalkTop);
                        if (!SpotFree(p, 0.25f)) continue;
                        StopPost(group, p, -d, s, cross);
                        break;
                    }

                    // Boca de incendio en la esquina de la izquierda, en la mitad de las llegadas.
                    if (rng.NextDouble() < 0.5)
                    {
                        var p = s.Point(end - dir * (sw + Range(rng, 3f, 7f)), -rightSign * (hw + 0.55f), SidewalkTop);
                        if (SpotFree(p, 0.3f)) Hydrant(group, p, yaw);
                    }
                }
            }
        }

        static void StopPost(Transform group, Vector3 ground, Vector3 facing, Street street, Street cross)
        {
            var root = new GameObject($"Stop_{street.Name}").transform;
            root.SetParent(group, false);
            root.localPosition = ground;
            Primitive(PrimitiveType.Cylinder, "Poste", root, new Vector3(0f, 1.73f, 0f), new Vector3(0.07f, 1.73f, 0.07f), sdGalvanized, keepCollider: true);
            SignQuad(root, Vector3.up * 2.15f + facing * 0.045f, facing, 0.76f, 0.76f, sdStop);
            SignQuad(root, Vector3.up * 2.15f + facing * 0.035f, -facing, 0.76f, 0.76f, sdStopBack);
            var right = new Vector3(facing.z, 0f, -facing.x);
            NameBlades(root, Vector3.up * 3.12f, right, street.Name);   // a lo largo de su calle
            NameBlades(root, Vector3.up * 3.36f, facing, cross.Name);   // a lo largo de la otra
            root.gameObject.AddComponent<BreakableProp>().Configure(BreakableProp.Kind.Metal, 30f);
        }

        /// <summary>Placa verde de nombre de calle a doble cara, a lo largo de la dirección perpendicular a <paramref name="normal"/>.</summary>
        static void NameBlades(Transform root, Vector3 center, Vector3 normal, string name)
        {
            if (!sdNameSigns.TryGetValue(name, out var mat))
                sdNameSigns[name] = mat = SignMat($"Calle_{name.Replace(' ', '_')}", name, StreetSignGreen, new Color(0.95f, 0.95f, 0.92f), 90 + sdNameSigns.Count, 0.15f);
            const float height = 0.22f;
            float width = height * (name.Length * 6f + 3f) / 11f;
            normal = normal.normalized;
            SignQuad(root, center + normal * 0.012f, normal, width, height, mat);
            SignQuad(root, center - normal * 0.012f, -normal, width, height, mat);
            var along = new Vector3(normal.z, 0f, -normal.x);
            Box("Canto", root, center, new Vector3(0.02f, height + 0.02f, 0.02f) + Abs(along) * (width - 0.02f), sdGalvanized, collider: false);
        }

        static void Hydrant(Transform group, Vector3 ground, float yaw)
        {
            var root = new GameObject("BocaIncendio").transform;
            root.SetParent(group, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Primitive(PrimitiveType.Cylinder, "Cuerpo", root, Vector3.up * 0.35f, new Vector3(0.25f, 0.35f, 0.25f), sdHydrant, keepCollider: true);
            Primitive(PrimitiveType.Sphere, "Tapa", root, Vector3.up * 0.72f, new Vector3(0.24f, 0.16f, 0.24f), sdHydrant, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Boca", root, Vector3.up * 0.45f, new Vector3(0.1f, 0.22f, 0.1f), sdHydrant, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (!sdSpray) sdSpray = ParticleMat("Vapor", ProceduralTextures.Puff());
            root.gameObject.AddComponent<BreakableProp>().Configure(BreakableProp.Kind.Hydrant, 90f, sdSpray);
        }

        /// <summary>Material del chorro de agua de las bocas de incendio rotas (el mismo que el vapor).</summary>
        static Material sdSpray;

        // ---------- Semáforos ----------

        /// <summary>
        /// Los cuatro cruces de las calles grandes (x = ±110 con z = ±80): un poste en cada esquina con un semáforo
        /// para cada dirección (mirando a los coches que llegan desde el otro lado), semáforos de peatones y, en dos
        /// esquinas, las placas con los nombres. Cada cruce lleva un <see cref="TrafficLight"/>; van fuera de "Mundo".
        /// </summary>
        /// <summary>Cruces con semáforo (centro y su <see cref="TrafficLight"/>), para el tráfico (Traffic.cs).</summary>
        static readonly List<(Vector2 center, TrafficLight light)> signalJunctions = new List<(Vector2, TrafficLight)>();

        static void BuildTrafficSignals(List<Street> streets)
        {
            signalJunctions.Clear();
            var all = new GameObject("Semaforos").transform;
            float h = RoadWidth / 2f;
            int index = 0;
            foreach (float x in new[] { -MainRoadX, MainRoadX })
            foreach (float z in new[] { -MainRoadZ, MainRoadZ })
            {
                var junction = new GameObject($"Cruce_{index}").transform;
                junction.SetParent(all, false);
                var xName = StreetAt(streets, new Vector3(x - h - 5f, 0f, z), null).Name; // la E–O (al oeste siempre sigue)
                var zName = StreetAt(streets, new Vector3(x, 0f, z - h - 5f), null).Name;
                foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    for (float o = h + 0.9f; o <= h + 2.5f; o += 0.8f)
                    {
                        var p = new Vector3(x + sx * o, SidewalkTop, z + sz * o);
                        if (!SpotFree(p, 0.3f)) continue;
                        SignalPost(junction, p, sx, sz, sx == sz ? (xName, zName) : default);
                        break;
                    }
                var light = junction.gameObject.AddComponent<TrafficLight>();
                light.Configure(index * 11.5f);
                signalJunctions.Add((new Vector2(x, z), light));
                index++;
            }
        }

        static void SignalPost(Transform junction, Vector3 ground, int sx, int sz, (string x, string z) names)
        {
            var root = new GameObject("PosteSemaforo").transform;
            root.SetParent(junction, false);
            root.localPosition = ground;
            root.gameObject.AddComponent<BreakableProp>().Configure(BreakableProp.Kind.Metal, 140f);
            Primitive(PrimitiveType.Cylinder, "Poste", root, new Vector3(0f, 2.3f, 0f), new Vector3(0.13f, 2.3f, 0.13f), sdSignalBody, keepCollider: true);
            Primitive(PrimitiveType.Cylinder, "Base", root, new Vector3(0f, 0.15f, 0f), new Vector3(0.3f, 0.15f, 0.3f), sdSignalBody, keepCollider: false);

            var fx = new Vector3(-sx, 0f, 0f); // mira a los coches que llegan a lo largo de X desde el otro lado
            var fz = new Vector3(0f, 0f, -sz);
            SignalHead(root, Vector3.up * 3.55f + fx * 0.25f, fx, "X");
            SignalHead(root, Vector3.up * 3.55f + fz * 0.25f, fz, "Z");
            PedestrianHead(root, Vector3.up * 2.45f + fx * 0.2f, fx, "X");
            PedestrianHead(root, Vector3.up * 2.45f + fz * 0.2f, fz, "Z");
            if (names.x != null)
            {
                NameBlades(root, Vector3.up * 4.45f, Vector3.forward, names.x); // placa a lo largo de X
                NameBlades(root, Vector3.up * 4.7f, Vector3.right, names.z);
            }
        }

        static void SignalHead(Transform root, Vector3 center, Vector3 facing, string axis)
        {
            var rot = Quaternion.LookRotation(facing);
            Box("Semaforo", root, center, new Vector3(0.36f, 1.02f, 0.28f), sdSignalBody, collider: false).transform.localRotation = rot;
            Box("Placa", root, center - facing * 0.15f, new Vector3(0.62f, 1.26f, 0.025f), sdSignalBody, collider: false).transform.localRotation = rot;
            string[] names = { "Luz_Roja", "Luz_Ambar", "Luz_Verde" };
            bool[] initial = { axis == "Z", false, axis == "X" }; // como en el primer segundo del ciclo (para las capturas)
            var lensRot = Quaternion.FromToRotation(Vector3.up, facing);
            for (int i = 0; i < 3; i++)
            {
                var p = center + Vector3.up * (0.32f - i * 0.32f) + facing * 0.145f;
                Primitive(PrimitiveType.Cylinder, "Lente", root, p, new Vector3(0.22f, 0.006f, 0.22f), sdLensDark[i], keepCollider: false).transform.localRotation = lensRot;
                var lit = Primitive(PrimitiveType.Cylinder, $"{names[i]}_{axis}", root, p + facing * 0.008f, new Vector3(0.22f, 0.004f, 0.22f), sdLensLit[i], keepCollider: false);
                lit.transform.localRotation = lensRot;
                lit.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                lit.SetActive(initial[i]);
                Box("Visera", root, p + Vector3.up * 0.13f + facing * 0.09f, new Vector3(0.26f, 0.02f, 0.18f), sdSignalBody, collider: false).transform.localRotation = rot;
            }
        }

        static void PedestrianHead(Transform root, Vector3 center, Vector3 facing, string axis)
        {
            var rot = Quaternion.LookRotation(facing);
            Box("SemaforoPeatones", root, center, new Vector3(0.4f, 0.42f, 0.22f), sdSignalBody, collider: false).transform.localRotation = rot;
            Box("Pantalla", root, center + facing * 0.112f, new Vector3(0.32f, 0.34f, 0.004f), sdPedDark, collider: false).transform.localRotation = rot;
            var hand = Box($"Peaton_Mano_{axis}", root, center + Vector3.up * 0.085f + facing * 0.116f, new Vector3(0.14f, 0.12f, 0.004f), sdHandLit, collider: false);
            hand.transform.localRotation = rot;
            hand.SetActive(axis == "Z");
            var walk = Box($"Peaton_Anda_{axis}", root, center - Vector3.up * 0.085f + facing * 0.116f, new Vector3(0.08f, 0.14f, 0.004f), sdWalkLit, collider: false);
            walk.transform.localRotation = rot;
            walk.SetActive(axis == "X");
            Box("Visera", root, center + Vector3.up * 0.22f + facing * 0.09f, new Vector3(0.4f, 0.02f, 0.2f), sdSignalBody, collider: false).transform.localRotation = rot;
        }

        // ---------- Flechas de carril ----------

        /// <summary>
        /// Flechas pintadas en las llegadas a los cruces de las calles grandes: en el carril de dentro, giro a la
        /// izquierda (si hay calle a la izquierda); en el de fuera, recto (o a la derecha en el cruce en T de la avenida).
        /// </summary>
        static void BuildLaneArrows(Transform group, List<Street> streets)
        {
            var mats = new Dictionary<int, Material>();
            Material Arrow(int turn)
            {
                if (!mats.TryGetValue(turn, out var m))
                {
                    string name = turn < 0 ? "FlechaIzquierda" : turn > 0 ? "FlechaDerecha" : "FlechaRecto";
                    mats[turn] = m = TransparentMat(name, ProceduralTextures.LaneArrow(name, turn, 70 + turn), new Color(1f, 1f, 1f, 0.95f), 0.1f);
                }
                return m;
            }
            bool IsRoad(Vector3 p)
            {
                foreach (var s in streets) if (s.Main && s.Rect.Contains(new Vector2(p.x, p.z))) return true;
                return false;
            }

            float h = RoadWidth / 2f;
            foreach (var (_, alongX, center, crossings, end) in MainRoadsList())
                foreach (float k in crossings)
                    foreach (int dir in new[] { 1, -1 })
                    {
                        if (dir < 0 && k + h >= end - 0.01f) continue; // cruce en T: no se llega desde el otro lado
                        var d = alongX ? new Vector3(dir, 0f, 0f) : new Vector3(0f, 0f, dir);
                        var right = new Vector3(d.z, 0f, -d.x);
                        float rightSign = alongX ? -dir : dir;
                        var junction = MainRoadPoint(alongX, center, k, 0f, 0f);
                        bool hasLeft = IsRoad(junction - right * (h + 5f));
                        bool hasStraight = IsRoad(junction + d * (h + 5f));
                        float t = k - dir * (h + 9.5f);
                        float y = MainRoadTop + PaintLift;
                        float yaw = YawOf(d);
                        int inner = hasLeft ? -1 : 0, outer = hasStraight ? 0 : 1;
                        Decal(group, MainRoadPoint(alongX, center, t, rightSign * 1.75f, y), yaw, inner == 0 ? new Vector2(0.9f, 3.6f) : new Vector2(1.6f, 2.8f), Arrow(inner));
                        Decal(group, MainRoadPoint(alongX, center, t, rightSign * 5.25f, y), yaw, outer == 0 ? new Vector2(0.9f, 3.6f) : new Vector2(1.6f, 2.8f), Arrow(outer));
                    }
        }

        // ---------- Postes de madera con cables en las calles grandes ----------

        /// <summary>
        /// Postes de madera cada ~34 m en la acera de fuera de la calle norte, la sur y la del oeste (en la avenida
        /// está el tren elevado), con cruceta, transformador cada tres y tres cables de poste a poste, también por
        /// encima de las bocas de las calles y de los cruces.
        /// </summary>
        static void BuildMainRoadPoles(Transform group, System.Random rng)
        {
            var poles = new GameObject("PostesCables").transform;
            poles.SetParent(group, false);
            var mouths = MainRoadMouths();
            float h = RoadWidth / 2f;
            var lines = new (bool alongX, float center, float side, float[] crossings, float t0, float t1)[]
            {
                (true, MainRoadZ, 1f, new[] { -MainRoadX }, -MainRoadHalfLength + 4f, MainRoadX - h - 12f),
                (true, -MainRoadZ, -1f, new[] { -MainRoadX }, -MainRoadHalfLength + 4f, MainRoadX - h - 12f),
                (false, -MainRoadX, -1f, new[] { SouthDockZ, SouthFreightZ, GantryZ, -MainRoadZ, MainRoadZ, BoulevardZ }, SouthQuayZ + 8f, NorthWallZ - 6f),
            };
            foreach (var (alongX, center, side, crossings, t0, t1) in lines)
            {
                var placed = new List<Vector3>();
                for (float t = t0; t <= t1; t += 34f)
                    foreach (float shift in new[] { 0f, 2f, -2f, 4f, -4f })
                    {
                        float tt = t + shift;
                        bool nearCrossing = false;
                        foreach (float k in crossings) if (Mathf.Abs(tt - k) < h + 3.5f) nearCrossing = true;
                        var p = MainRoadPoint(alongX, center, tt, side * (h + 0.45f), SidewalkTop);
                        if (nearCrossing || InsideAny(mouths, p) || !SpotFree(p, 0.3f)) continue;
                        UtilityPole(poles, p, alongX, placed.Count % 3 == 1);
                        placed.Add(p);
                        break;
                    }
                const float armY = 8.9f;
                var arm = alongX ? Vector3.forward : Vector3.right;
                for (int i = 0; i + 1 < placed.Count; i++)
                {
                    float span = Vector3.Distance(placed[i], placed[i + 1]);
                    if (span > 75f) continue;
                    foreach (float o in new[] { -0.95f, 0f, 0.95f })
                        Wire(poles, placed[i] + Vector3.up * (armY + 0.08f) + arm * o, placed[i + 1] + Vector3.up * (armY + 0.08f) + arm * o, 0.5f + span * 0.012f, sdCable);
                    // Cable bajo del teléfono, con más comba.
                    Wire(poles, placed[i] + Vector3.up * 7.2f + arm * 0.18f, placed[i + 1] + Vector3.up * 7.2f + arm * 0.18f, 0.9f + span * 0.015f, sdCable);
                }
            }
        }

        static void UtilityPole(Transform parent, Vector3 ground, bool armAcrossZ, bool transformer)
        {
            var root = new GameObject("PosteMadera").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            Primitive(PrimitiveType.Cylinder, "Poste", root, Vector3.up * 4.75f, new Vector3(0.28f, 4.75f, 0.28f), sdWood, keepCollider: true);
            Box("Cruceta", root, Vector3.up * 8.9f, armAcrossZ ? new Vector3(0.12f, 0.12f, 2.2f) : new Vector3(2.2f, 0.12f, 0.12f), sdWood, collider: false);
            var side = armAcrossZ ? Vector3.right : Vector3.forward;
            if (transformer)
                Primitive(PrimitiveType.Cylinder, "Transformador", root, Vector3.up * 7.6f + side * 0.4f, new Vector3(0.5f, 0.45f, 0.5f), sdTransformer, keepCollider: false);
            Box("Caja", root, Vector3.up * 7.2f + (armAcrossZ ? Vector3.forward : Vector3.right) * 0.18f, new Vector3(0.14f, 0.2f, 0.14f), sdTransformer, collider: false);
        }

        // ---------- Límites de velocidad ----------

        static void BuildSpeedLimitSigns(Transform group, List<Street> streets, System.Random rng)
        {
            var face = CullBack(TexturedMat("SenalVelocidad", ProceduralTextures.Placard("SenalVelocidad", new[] { "SPEED", "LIMIT", "30" }, new Color(0.92f, 0.92f, 0.9f), new Color(0.05f, 0.05f, 0.05f), 33)));
            var mouths = MainRoadMouths();
            float h = RoadWidth / 2f;
            foreach (var (_, alongX, center, crossings, end) in MainRoadsList())
                foreach (float side in new[] { 1f, -1f })
                    for (float t = MainRoadStart(alongX) + 30f + (side > 0 ? 0f : 60f); t < end - 20f; t += 120f)
                    {
                        bool near = false;
                        foreach (float k in crossings) if (Mathf.Abs(t - k) < h + 25f) near = true;
                        var p = MainRoadPoint(alongX, center, t, side * (h + 0.5f), SidewalkTop);
                        if (near || InsideAny(mouths, p) || !SpotFree(p, 0.25f)) continue;
                        // Mira a los coches que llevan esta acera a su derecha.
                        var normal = alongX ? new Vector3(side, 0f, 0f) : new Vector3(0f, 0f, -side);
                        var root = new GameObject("LimiteVelocidad").transform;
                        root.SetParent(group, false);
                        root.localPosition = p;
                        root.gameObject.AddComponent<BreakableProp>().Configure(BreakableProp.Kind.Metal, 20f);
                        Primitive(PrimitiveType.Cylinder, "Poste", root, Vector3.up * 1.3f, new Vector3(0.06f, 1.3f, 0.06f), sdGalvanized, keepCollider: true);
                        Box("Trasera", root, Vector3.up * 2.15f + normal * 0.04f, new Vector3(0.62f, 0.51f, 0.015f), sdSignBack, collider: false)
                            .transform.localRotation = Quaternion.LookRotation(normal);
                        SignQuad(root, Vector3.up * 2.15f + normal * 0.05f, normal, 0.62f, 0.51f, face);
                    }
        }

        // ---------- Alcantarillado de las calles de barrio ----------

        /// <summary>Tapas de alcantarilla en los carriles y sumideros junto a los dos bordillos de las calles de barrio.</summary>
        static void BuildLocalSewers(Transform group, List<Street> streets, System.Random rng)
        {
            var sewers = new GameObject("Alcantarillado").transform;
            sewers.SetParent(group, false);
            int layer = 0;
            foreach (var s in streets)
            {
                if (s.Main || s.Name == "W KESSLER CT") continue; // las calles grandes y la de Kessler ya lo tienen
                float hw = s.Width / 2f;
                float yaw = s.AlongX ? 90f : 0f;
                for (float t = s.Start + 10f + Range(rng, 0f, 10f); t < s.End - 9f; t += Range(rng, 35f, 55f))
                {
                    float across = (rng.Next(2) == 0 ? -1f : 1f) * hw / 2f;
                    Primitive(PrimitiveType.Cylinder, "Alcantarilla", sewers, s.Point(t, across, RoadTop + 0.005f), new Vector3(0.72f, 0.005f, 0.72f), sdIron, keepCollider: false);
                    if (sdDirt) Decal(sewers, s.Point(t, across, RoadTop + 0.003f + (layer++ % 20) * 0.0001f), Range(rng, 0f, 360f), Vector2.one * 1.4f, sdDirt);
                }
                foreach (float side in new[] { 1f, -1f })
                    for (float t = s.Start + 6f + Range(rng, 0f, 12f); t < s.End - 6f; t += Range(rng, 28f, 42f))
                        Box("Sumidero", sewers, s.Point(t, side * (hw - 0.3f), RoadTop + 0.006f), new Vector3(0.5f, 0.012f, 0.9f), sdGrate, collider: false)
                            .transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
