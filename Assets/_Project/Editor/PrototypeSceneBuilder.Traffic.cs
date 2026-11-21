using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Tráfico de la ciudad (objeto <c>Trafico_Ciudad</c>, fuera de "Mundo" porque se mueve): la red de calles
    /// (<see cref="TrafficNetwork"/>), una reserva de coches (<see cref="TrafficCar"/>) que saca
    /// <see cref="TrafficPopulation"/> y el autobús urbano con sus paradas. La red sale sola de los rectángulos de
    /// <see cref="NamedStreets"/>: un nodo en cada cruce y en cada extremo, y tramos de ida y vuelta entre nodos seguidos.
    /// Se construye al final, después de la malla de navegación.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const int TrafficPoolSize = 34, TrafficMaxCars = 30;
        const float MainLaneOffset = 1.9f, LocalLaneOffset = 1.7f;
        const float MainSpeed = 13f, LocalSpeed = 10f, BoulevardSpeed = 12f, CurveSpeed = 7f;
        const float TrafficY = 0.02f;
        const float NorthExitZ = 520f; // dentro de los pasos inferiores del terraplén, a oscuras

        /// <summary>Una calle para el tráfico: rectángulo, si es grande y en qué sentido se circula (0: los dos).</summary>
        sealed class TrafficStreet
        {
            public Rect Rect;
            public bool Main;
            public int OneWay;    // +1: hacia +X/+Z; −1: hacia −X/−Z
            public float Speed;
            public bool AlongX => Rect.width >= Rect.height;
            public float Half => (AlongX ? Rect.height : Rect.width) / 2f;
            public float Center => AlongX ? Rect.center.y : Rect.center.x;
            public float Start => AlongX ? Rect.xMin : Rect.yMin;
            public float End => AlongX ? Rect.xMax : Rect.yMax;
            public Vector3 Point(float t) => AlongX ? new Vector3(t, TrafficY, Center) : new Vector3(Center, TrafficY, t);
            public List<(float t, int node, bool crossing)> Stations = new List<(float, int, bool)>();
        }

        static void BuildCityTraffic()
        {
            var root = new GameObject("Trafico_Ciudad").transform;
            var network = root.gameObject.AddComponent<TrafficNetwork>();

            // ---------- Calles ----------
            var streets = new List<TrafficStreet>();
            foreach (var s in NamedStreets())
            {
                float width = Mathf.Min(s.Rect.width, s.Rect.height);
                if (width < 7f) continue;
                var ts = new TrafficStreet { Rect = s.Rect, Main = s.Main, Speed = s.Main ? MainSpeed : LocalSpeed };
                if (s.Name == BoulevardName)
                {
                    // Bulevar con mediana: cada calzada en un sentido (por la derecha: al norte hacia el oeste).
                    ts.OneWay = s.Rect.center.y > BoulevardZ ? -1 : 1;
                    ts.Speed = BoulevardSpeed;
                }
                streets.Add(ts);
            }
            // Los quiebros de W HOLLIS ST y W CROWELL ST: un tramo norte–sur que une las dos mitades.
            var jog = Rect.MinMaxRect(163f, 176f, 173f, 188f);
            streets.Add(new TrafficStreet { Rect = jog, Speed = LocalSpeed });
            streets.Add(new TrafficStreet { Rect = HousesWestRect(jog), Speed = LocalSpeed });

            // ---------- Nodos ----------
            var nodes = new List<Vector3>();
            var radius = new List<float>();
            var exits = new List<bool>();
            var nodeKeys = new Dictionary<Vector2Int, int>();
            int NodeAt(Vector3 p, float r)
            {
                var key = new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.z * 2f));
                if (nodeKeys.TryGetValue(key, out int i)) { radius[i] = Mathf.Max(radius[i], r); return i; }
                nodes.Add(p); radius.Add(r); exits.Add(false);
                nodeKeys[key] = nodes.Count - 1;
                return nodes.Count - 1;
            }

            const float tol = 0.6f;
            foreach (var a in streets)
            foreach (var b in streets)
            {
                if (a == b || a.AlongX == b.AlongX) continue;
                bool bOnA = b.Center >= a.Start - b.Half - tol && b.Center <= a.End + b.Half + tol;
                bool aOnB = a.Center >= b.Start - a.Half - tol && a.Center <= b.End + a.Half + tol;
                if (!bOnA || !aOnB) continue;
                int n = NodeAt(a.Point(b.Center), Mathf.Max(a.Half, b.Half) + 0.3f);
                a.Stations.Add((b.Center, n, true));
            }
            foreach (var s in streets)
            {
                foreach (float t in new[] { s.Start, s.End })
                {
                    bool nearCrossing = false;
                    foreach (var st in s.Stations) if (Mathf.Abs(st.t - t) < 7.5f) nearCrossing = true;
                    if (nearCrossing) continue;
                    var p = s.Point(t);
                    bool exit = p.x <= -MainRoadHalfLength + 1f || p.z >= WorldNorthZ - 1f;
                    if (exit && p.z >= WorldNorthZ - 1f) p.z = NorthExitZ;
                    int n = NodeAt(p, 0f);
                    if (exit) exits[n] = true;
                    s.Stations.Add((exit && !s.AlongX ? NorthExitZ : t, n, false));
                }
                s.Stations.Sort((x, y) => x.t.CompareTo(y.t));
            }

            // ---------- Tramos ----------
            var links = new List<TrafficLinkData>();
            var streetsAtNode = new Dictionary<int, List<TrafficStreet>>();
            foreach (var s in streets)
                foreach (var st in s.Stations)
                {
                    if (!streetsAtNode.TryGetValue(st.node, out var list)) streetsAtNode[st.node] = list = new List<TrafficStreet>();
                    if (!list.Contains(s)) list.Add(s);
                }
            var signals = new TrafficLight[nodes.Count];
            foreach (var (center, light) in signalJunctions)
                for (int i = 0; i < nodes.Count; i++)
                    if (Vector2.Distance(new Vector2(nodes[i].x, nodes[i].z), center) < 1f) signals[i] = light;

            bool EndsAt(TrafficStreet s, int node) => s.Stations[0].node == node || s.Stations[s.Stations.Count - 1].node == node;
            TrafficGate GateFor(TrafficStreet s, int node)
            {
                if (signals[node]) return TrafficGate.Signal;
                if (s.Main) return TrafficGate.Free;
                var others = streetsAtNode[node];
                bool anyMain = false, othersPass = false;
                foreach (var o in others)
                {
                    if (o == s || o.AlongX == s.AlongX) continue;
                    anyMain |= o.Main;
                    othersPass |= !EndsAt(o, node);
                }
                if (anyMain) return TrafficGate.Stop;
                if (EndsAt(s, node)) return others.Count > 1 ? TrafficGate.Stop : TrafficGate.Free; // T: para quien llega por el pie
                return othersPass ? TrafficGate.Stop : TrafficGate.Free;                            // cruce de dos calles de barrio: STOP en todas
            }
            void AddLink(TrafficStreet s, int from, int to, Vector3[] via = null, float? speed = null)
            {
                links.Add(new TrafficLinkData
                {
                    From = from, To = to, Via = via ?? new Vector3[0],
                    LaneOffset = s == null ? LocalLaneOffset : s.OneWay != 0 ? 0f : s.Main ? MainLaneOffset : LocalLaneOffset,
                    SpeedLimit = speed ?? s.Speed,
                    Gate = s == null ? TrafficGate.Free : GateFor(s, to),
                });
            }
            foreach (var s in streets)
                for (int i = 1; i < s.Stations.Count; i++)
                {
                    int a = s.Stations[i - 1].node, b = s.Stations[i].node;
                    if (a == b) continue;
                    if (s.OneWay >= 0) AddLink(s, a, b);
                    if (s.OneWay <= 0) AddLink(s, b, a);
                }

            // La curva de S WHARF RD: une el final del tramo norte–sur con el principio del este–oeste por el arco.
            int wharfA = ClosestNode(nodes, new Vector3(SsWharfWest.center.x, 0f, SsWharfWest.yMin));
            int wharfB = ClosestNode(nodes, new Vector3(SsWharfEast.xMin, 0f, SsWharfEast.center.y));
            if (wharfA >= 0 && wharfB >= 0)
            {
                var arc = new List<Vector3>();
                for (int k = 1; k < 12; k++)
                {
                    float angle = Mathf.Lerp(180f, 270f, k / 12f) * Mathf.Deg2Rad;
                    arc.Add(new Vector3(WharfCenter.x + Mathf.Cos(angle) * WharfRadius, TrafficY, WharfCenter.y + Mathf.Sin(angle) * WharfRadius));
                }
                AddLink(null, wharfA, wharfB, arc.ToArray(), CurveSpeed);
                arc.Reverse();
                AddLink(null, wharfB, wharfA, arc.ToArray(), CurveSpeed);
            }

            // Los tramos que cruzan la vía de mercancías llevan su paso a nivel.
            for (int i = 0; i < links.Count; i++)
            {
                var l = links[i];
                Vector3 a = nodes[l.From], b = nodes[l.To];
                if (Mathf.Abs(a.x - b.x) > 0.5f || (a.z - SouthFreightZ) * (b.z - SouthFreightZ) > 0f) continue;
                foreach (var (x, crossing) in levelCrossings)
                    if (Mathf.Abs(a.x - x) < 1f)
                    {
                        l.Rail = crossing;
                        l.RailPoint = new Vector3(x, TrafficY, SouthFreightZ);
                        l.RailHalf = FreightBedHalf + 0.5f;
                        links[i] = l;
                    }
            }

            network.Configure(nodes.ToArray(), radius.ToArray(), exits.ToArray(), signals, links.ToArray());

            // ---------- Coches ----------
            var rng = new System.Random(3131);
            var styles = new[] { CarStyle.Sedan, CarStyle.Sedan, CarStyle.Sedan, CarStyle.Coupe, CarStyle.Suv, CarStyle.Suv, CarStyle.Van };
            var hazardMat = EmissiveMat("Intermitente", new Color(1f, 0.55f, 0.05f), 3f);
            var pool = new TrafficCar[TrafficPoolSize];
            var carsRoot = new GameObject("Coches").transform;
            carsRoot.SetParent(root, false);
            for (int i = 0; i < pool.Length; i++)
            {
                var style = styles[rng.Next(styles.Length)];
                var spec = Spec(style);
                var model = CarModel(carsRoot, $"CocheTrafico_{i}", Vector3.zero, 0f, style, carBodyPaints[rng.Next(carBodyPaints.Length)], rng,
                    weathered: rng.NextDouble() < 0.5, lightsOn: true, collider: false);
                pool[i] = MakeTrafficVehicle(model, network, spec.Length, spec.Width, spec.Roof, spec.WheelRadius, Range(rng, 0.85f, 1.1f), hazardMat);
                model.gameObject.SetActive(false);
            }
            var population = root.gameObject.AddComponent<TrafficPopulation>();
            population.Configure(network, pool, TrafficMaxCars);

            BuildCityBus(root, network, nodes, rng);
            Debug.Log($"[GTAChiraq] Tráfico: {nodes.Count} nodos, {links.Count} tramos, {signalJunctions.Count} cruces con semáforo.");
        }

        static int ClosestNode(List<Vector3> nodes, Vector3 p, float max = 3f)
        {
            int best = -1;
            float bestD = max;
            for (int i = 0; i < nodes.Count; i++)
            {
                float d = Vector2.Distance(new Vector2(nodes[i].x, nodes[i].z), new Vector2(p.x, p.z));
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Convierte un modelo en coche del tráfico: capa de vehículos, caja de colisión, cuerpo cinemático, ruedas que
        /// giran e intermitentes (apagados) en las cuatro esquinas.
        /// </summary>
        static TrafficCar MakeTrafficVehicle(Transform model, TrafficNetwork network, float length, float width, float height, float wheelRadius,
            float temper, Material hazardMat, int[] route = null, Vector3[] stops = null)
        {
            var hazards = new List<Renderer>();
            foreach (float z in new[] { -1f, 1f })
            foreach (float x in new[] { -1f, 1f })
            {
                var h = Box("Intermitente", model, new Vector3(x * (width / 2f - 0.15f), 0.85f, z * (length / 2f + 0.02f)), new Vector3(0.2f, 0.1f, 0.05f), hazardMat, collider: false);
                var r = h.GetComponent<Renderer>();
                r.enabled = false;
                hazards.Add(r);
            }
            var wheels = new List<Transform>();
            foreach (Transform child in model) if (child.name == "Rueda") wheels.Add(child);

            var box = model.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, height / 2f + 0.1f, 0f);
            box.size = new Vector3(width, height - 0.2f, length);
            var body = model.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            SetLayerRecursively(model.gameObject, GameLayers.Vehicle);
            var car = model.gameObject.AddComponent<TrafficCar>();
            car.Configure(network, length, temper, wheels.ToArray(), wheelRadius, hazards.ToArray(), route, stops);
            return car;
        }

        // ---------- Autobús urbano ----------

        /// <summary>
        /// Autobús urbano de la línea 52 (inventada): da la vuelta a la manzana central por las cuatro calles grandes, en
        /// sentido contrario a las agujas del reloj, y para en cuatro paradas (poste con la placa y banco).
        /// </summary>
        static void BuildCityBus(Transform root, TrafficNetwork network, List<Vector3> nodes, System.Random rng)
        {
            // Ruta: todos los nodos de las cuatro calles entre las esquinas, en orden.
            float x0 = -MainRoadX, x1 = MainRoadX, z0 = -MainRoadZ, z1 = MainRoadZ;
            var route = new List<int>();
            void Side(System.Func<Vector3, bool> on, System.Func<Vector3, float> order)
            {
                var side = new List<int>();
                for (int i = 0; i < nodes.Count; i++) if (on(nodes[i])) side.Add(i);
                side.Sort((a, b) => order(nodes[a]).CompareTo(order(nodes[b])));
                foreach (int n in side) if (route.Count == 0 || route[route.Count - 1] != n) route.Add(n);
            }
            bool Near(float a, float b) => Mathf.Abs(a - b) < 0.6f;
            Side(p => Near(p.z, z0) && p.x >= x0 - 0.5f && p.x <= x1 + 0.5f, p => p.x);   // sur, hacia el este
            Side(p => Near(p.x, x1) && p.z >= z0 - 0.5f && p.z <= z1 + 0.5f, p => p.z);   // avenida, hacia el norte
            Side(p => Near(p.z, z1) && p.x >= x0 - 0.5f && p.x <= x1 + 0.5f, p => -p.x);  // norte, hacia el oeste
            Side(p => Near(p.x, x0) && p.z >= z0 - 0.5f && p.z <= z1 + 0.5f, p => -p.z);  // oeste, hacia el sur
            if (route.Count > 1 && route[route.Count - 1] == route[0]) route.RemoveAt(route.Count - 1);
            for (int i = 0; i < route.Count; i++)
                if (network.LinkBetween(route[i], route[(i + 1) % route.Count]) < 0)
                {
                    Debug.LogWarning($"[GTAChiraq] Autobús: la ruta no está unida entre {nodes[route[i]]} y {nodes[route[(i + 1) % route.Count]]}.");
                    return;
                }

            // Paradas: a mitad de cada lado, en el carril (derecha) y con el poste en la acera.
            var stopsRoot = new GameObject("ParadasAutobus").transform;
            stopsRoot.SetParent(root, false);
            var sign = SignMat("Letrero_Parada52", "BUS 52", new Color(0.12f, 0.3f, 0.6f), new Color(0.95f, 0.95f, 0.95f), 152, 0.1f);
            var pole = Mat("PosteSenal", new Color(0.52f, 0.54f, 0.55f));
            var wood = Mat("MaderaBanco", new Color(0.4f, 0.28f, 0.18f));
            var stops = new List<Vector3>();
            var sides = new[]
            {
                (new Vector3(-30f, 0f, z0), Vector3.right),   // W ASHGROVE ST, hacia el este
                (new Vector3(x1, 0f, -20f), Vector3.forward), // S MARLOW AVE, hacia el norte (antes de la estación)
                (new Vector3(30f, 0f, z1), Vector3.left),     // W BRAXTON ST, hacia el oeste
                (new Vector3(x0, 0f, 20f), Vector3.back),     // S CALDER AVE, hacia el sur
            };
            foreach (var (axis, dir) in sides)
            {
                var right = new Vector3(dir.z, 0f, -dir.x);
                for (float shift = 0f; shift < 40f; shift += 2f)
                {
                    var center = axis + dir * shift;
                    var ground = center + right * (RoadWidth / 2f + 1.3f);
                    ground.y = SidewalkTop;
                    if (!SpotFree(ground, 0.4f) || !SpotFree(ground - dir * 2.2f, 0.5f)) continue;
                    var stop = new GameObject("Parada").transform;
                    stop.gameObject.AddComponent<BreakableProp>().Configure(BreakableProp.Kind.Metal, 70f);
                    stop.SetParent(stopsRoot, false);
                    stop.localPosition = ground;
                    stop.localRotation = Quaternion.LookRotation(-right);
                    Primitive(PrimitiveType.Cylinder, "Poste", stop, new Vector3(0f, 1.4f, 0f), new Vector3(0.08f, 1.4f, 0.08f), pole, keepCollider: true);
                    float h = 0.6f / ((float)sign.mainTexture.width / sign.mainTexture.height);
                    foreach (float side in new[] { -1f, 1f })
                        SignQuad(stop, new Vector3(0f, 2.55f, side * 0.03f), new Vector3(0f, 0f, side), 0.6f, h, sign);
                    // Banco de cara a la calle, un poco antes del poste.
                    var bench = new GameObject("Banco").transform;
                    bench.SetParent(stop, false);
                    bench.localPosition = new Vector3(2.2f, 0f, 0.4f);
                    Box("Asiento", bench, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.08f, 0.45f), wood);
                    Box("Respaldo", bench, new Vector3(0f, 0.8f, -0.22f), new Vector3(1.8f, 0.5f, 0.06f), wood, collider: false);
                    foreach (float bx in new[] { -0.8f, 0.8f })
                        Box("Pata", bench, new Vector3(bx, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.4f), pole, collider: false);
                    var laneStop = center + right * MainLaneOffset;
                    laneStop.y = TrafficY;
                    stops.Add(laneStop);
                    break;
                }
            }

            var bus = CityBus(root);
            MakeTrafficVehicle(bus.root, network, 12f, 2.55f, 3.2f, 0.5f, 0.85f, EmissiveMat("Intermitente", new Color(1f, 0.55f, 0.05f), 3f),
                route.ToArray(), stops.ToArray());
        }

        /// <summary>Autobús urbano genérico de 12 m: blanco con franja azul, ventanas oscuras y letrero de destino encendido.</summary>
        static (Transform root, Transform[] wheels) CityBus(Transform parent)
        {
            var root = new GameObject("AutobusUrbano").transform;
            root.SetParent(parent, false);
            var white = Mat("BlancoAutobus", new Color(0.82f, 0.83f, 0.82f));
            var blue = Mat("AzulAutobus", new Color(0.12f, 0.28f, 0.55f));
            var black = Mat("PlasticoNegro", new Color(0.05f, 0.05f, 0.05f));
            var glass = Mat("CristalCoche", new Color(0.12f, 0.14f, 0.16f));
            var grey = Mat("GrisBajos", new Color(0.25f, 0.25f, 0.26f));
            const float w = 2.55f, len = 12f, floor = 0.45f, roof = 3.05f;
            Box("Carroceria", root, new Vector3(0f, (floor + roof) / 2f, 0f), new Vector3(w, roof - floor, len), white, collider: false);
            Box("Techo", root, new Vector3(0f, roof + 0.12f, -1f), new Vector3(w - 0.5f, 0.25f, 3.5f), white, collider: false); // equipo del aire
            Box("Bajos", root, new Vector3(0f, floor + 0.15f, 0f), new Vector3(w + 0.02f, 0.3f, len - 0.2f), grey, collider: false);
            foreach (float s in new[] { -1f, 1f })
            {
                Box("Ventanas", root, new Vector3(s * (w / 2f + 0.01f), 2.15f, -0.3f), new Vector3(0.02f, 1.1f, len - 2.4f), glass, collider: false);
                Box("Franja", root, new Vector3(s * (w / 2f + 0.015f), 1.3f, 0f), new Vector3(0.02f, 0.28f, len), blue, collider: false);
            }
            // Puertas en el lado derecho (x+): delante y en medio.
            foreach (float z in new[] { len / 2f - 1.1f, -0.8f })
                Box("Puerta", root, new Vector3(w / 2f + 0.02f, 1.6f, z), new Vector3(0.02f, 2.3f, 1.2f), glass, collider: false);
            Box("Parabrisas", root, new Vector3(0f, 1.9f, len / 2f + 0.01f), new Vector3(w - 0.2f, 1.6f, 0.02f), glass, collider: false);
            Box("LunaTrasera", root, new Vector3(0f, 2.3f, -len / 2f - 0.01f), new Vector3(w - 0.8f, 0.7f, 0.02f), glass, collider: false);
            foreach (float end in new[] { -1f, 1f })
                Box("Paragolpes", root, new Vector3(0f, 0.55f, end * (len / 2f + 0.08f)), new Vector3(w, 0.3f, 0.16f), black, collider: false);
            var lamp = EmissiveMat("FaroTren", new Color(1f, 0.95f, 0.8f), 2f);
            var tail = EmissiveMat("PilotoAutobus", new Color(0.9f, 0.08f, 0.05f), 1.5f);
            foreach (float s in new[] { -1f, 1f })
            {
                Box("Faro", root, new Vector3(s * 0.95f, 0.85f, len / 2f + 0.02f), new Vector3(0.35f, 0.2f, 0.04f), lamp, collider: false);
                Box("Piloto", root, new Vector3(s * 1.1f, 1.1f, -len / 2f - 0.02f), new Vector3(0.18f, 0.45f, 0.04f), tail, collider: false);
            }
            var destination = SignMat("Letrero_Linea52", "52 ASHGROVE", new Color(0.04f, 0.04f, 0.04f), new Color(1f, 0.72f, 0.15f), 153, 1.2f);
            float dh = 1.8f / ((float)destination.mainTexture.width / destination.mainTexture.height);
            SignQuad(root, new Vector3(0f, roof - 0.28f, len / 2f + 0.025f), Vector3.forward, 1.8f, Mathf.Min(dh, 0.35f), destination);
            SignQuad(root, new Vector3(w / 2f + 0.03f, roof - 0.3f, len / 2f - 2.5f), Vector3.right, 1.6f, Mathf.Min(dh, 0.3f), destination);

            var wheels = new List<Transform>();
            var rim = Mat("LlantaAutobus", new Color(0.6f, 0.6f, 0.6f));
            foreach (float z in new[] { len / 2f - 2.3f, -len / 2f + 3f })
            foreach (float s in new[] { -1f, 1f })
                wheels.Add(CarWheel(root, "Rueda", new Vector3(s * (w / 2f - 0.2f), 0.5f, z), 0.5f, s, rim));
            return (root, wheels.ToArray());
        }
    }
}
