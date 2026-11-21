using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Lo que se mueve en la vía de mercancías del sur (fuera de "Mundo", que es estático): el tren que pasa
    /// (<see cref="FreightTrain"/>, dos locomotoras y 12 vagones mezclados) y la parte viva de los tres pasos a nivel
    /// (<see cref="LevelCrossing"/>): luces encendidas que parpadean y barreras que bajan. Se construye antes del tráfico,
    /// que apunta en cada tramo que cruza la vía su paso a nivel (<see cref="levelCrossings"/>).
    /// Lo fijo de los pasos (postes, cruz de San Andrés, mecanismo, línea de detención) lo pone BuildSouthStreetDetail.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float FreightRailY = 0.1f, FreightSpeed = 9f, FreightCarPitch = 16.2f, LocoLength = 22f;
        const int FreightCars = 12;

        /// <summary>Los pasos a nivel (x del centro de la calle y su <see cref="LevelCrossing"/>), para el tráfico (Traffic.cs).</summary>
        static readonly List<(float x, LevelCrossing crossing)> levelCrossings = new List<(float, LevelCrossing)>();

        static void BuildFreightTraffic()
        {
            levelCrossings.Clear();
            var rng = new System.Random(4545);
            var train = BuildFreightConsist(rng, out float length);
            var crossingX = new float[FreightCrossings.Length];
            for (int i = 0; i < FreightCrossings.Length; i++) crossingX[i] = (FreightCrossings[i].r0 + FreightCrossings[i].r1) / 2f;
            train.Configure(-MainRoadHalfLength - 10f, SouthEdgeX + 10f, FreightTracksZ[0], FreightRailY, length, FreightSpeed, crossingX);

            var root = new GameObject("PasosNivel_Barreras").transform;
            var white = Mat("BlancoBarrera", new Color(0.9f, 0.9f, 0.88f));
            var red = Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f));
            var lit = EmissiveMat("LuzPasoNivel", new Color(1f, 0.08f, 0.05f), 3f);
            foreach (var c in FreightCrossings)
            {
                float center = (c.r0 + c.r1) / 2f, hw = (c.r1 - c.r0) / 2f;
                var crossing = new GameObject($"PasoNivel_{center:F0}").transform;
                crossing.SetParent(root, false);
                var arms = new List<Transform>();
                var down = new List<Quaternion>();
                var lightsA = new List<Renderer>();
                var lightsB = new List<Renderer>();
                foreach (var (z, facing, px) in new[] { (SouthFreightZ + FreightBedHalf + 1.4f, 1f, c.r0 - 0.9f), (SouthFreightZ - FreightBedHalf - 1.4f, -1f, c.r1 + 0.9f) })
                {
                    var n = new Vector3(0f, 0f, facing);
                    var post = G(px, z, SidewalkTop);
                    float toCenter = Mathf.Sign(center - px);

                    // Barrera: pivote en el mecanismo; levantada apunta arriba y bajada cruza el carril que llega.
                    var arm = new GameObject("Barrera").transform;
                    arm.SetParent(crossing, false);
                    arm.localPosition = post + new Vector3(toCenter * 0.5f, 1.1f, 0f) + n * 0.5f;
                    float armLength = hw + 0.1f;
                    int pieces = Mathf.CeilToInt(armLength / 0.6f);
                    for (int s = 0; s < pieces; s++)
                    {
                        float a = s * 0.6f, b = Mathf.Min(armLength, a + 0.6f);
                        Box("Tramo", arm, new Vector3(0f, (a + b) / 2f, 0f), new Vector3(0.1f, b - a, 0.1f), s % 2 == 0 ? red : white, collider: false);
                    }
                    Box("Contrapeso", arm, new Vector3(0f, -0.35f, 0f), new Vector3(0.25f, 0.5f, 0.25f), Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f)), collider: false);
                    arms.Add(arm);
                    down.Add(Quaternion.Euler(0f, 0f, -90f * toCenter));

                    // Luces encendidas (apagadas al empezar): delante de las fijas, que son rojo oscuro.
                    foreach (float o in new[] { -0.6f, 0.6f })
                    {
                        var light = Primitive(PrimitiveType.Sphere, "LuzEncendida", crossing, post + new Vector3(o, 2.7f, 0f) + n * 0.27f, Vector3.one * 0.24f, lit, keepCollider: false);
                        var r = light.GetComponent<Renderer>();
                        r.enabled = false;
                        (o < 0f ? lightsA : lightsB).Add(r);
                    }
                }
                var level = crossing.gameObject.AddComponent<LevelCrossing>();
                level.Configure(center, new[] { train }, arms.ToArray(), down.ToArray(), lightsA.ToArray(), lightsB.ToArray());
                levelCrossings.Add((center, level));
            }
        }

        /// <summary>
        /// El tren: una locomotora en cada punta (la de atrás mirando hacia atrás) y vagones cerrados, cisternas, tolvas y
        /// bateas con chatarra. El frente es +Z local; el centro del tren está en el origen.
        /// </summary>
        static FreightTrain BuildFreightConsist(System.Random rng, out float length)
        {
            length = LocoLength * 2f + FreightCars * FreightCarPitch + 1f;
            var root = new GameObject("TrenMercancias").transform;
            float front = length / 2f;

            Locomotive(root, new Vector3(0f, 0f, front - LocoLength / 2f), 0f, rng);
            Locomotive(root, new Vector3(0f, 0f, -front + LocoLength / 2f), 180f, rng);

            var shells = new[] { LoadMat("ChapaVagon_Oxido"), LoadMat("ChapaVagon_Amarillo"), LoadMat("ChapaVagon_Gris") };
            var chassis = Mat("BajosVagon", new Color(0.08f, 0.08f, 0.08f));
            var tank = Mat("Cisterna", new Color(0.1f, 0.1f, 0.11f));
            var hopper = Mat("TolvaGris", new Color(0.42f, 0.43f, 0.42f));
            var scrap = Mat("Chatarra", new Color(0.3f, 0.22f, 0.16f));
            float z = front - LocoLength - FreightCarPitch / 2f - 0.5f;
            for (int i = 0; i < FreightCars; i++, z -= FreightCarPitch)
            {
                var car = new GameObject("Vagon").transform;
                car.SetParent(root, false);
                car.localPosition = new Vector3(0f, 0f, z);
                Box("Bastidor", car, new Vector3(0f, 0.85f, 0f), new Vector3(2.7f, 0.5f, 14.6f), chassis, collider: false);
                foreach (float o in new[] { -5.5f, 5.5f })
                {
                    var bogie = new GameObject("Bogie").transform;
                    bogie.SetParent(car, false);
                    bogie.localPosition = new Vector3(0f, 0.55f, o);
                    Box("Bastidor", bogie, Vector3.zero, new Vector3(2.5f, 0.6f, 2.6f), chassis, collider: false);
                }
                Box("Enganche", car, new Vector3(0f, 0.9f, 7.7f), new Vector3(0.3f, 0.25f, 0.9f), chassis, collider: false);
                int kind = rng.Next(10);
                if (kind < 4)
                {
                    var shell = shells[rng.Next(shells.Length)] ?? hopper;
                    TexturedBox("Caja", car, new Vector3(0f, 3.15f, 0f), new Vector3(3.1f, 4.2f, 15f), shell, SheetTile, collider: false);
                    if (graffitiMats != null && rng.NextDouble() < 0.6)
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var g = graffitiMats[rng.Next(graffitiMats.Length)];
                            if (g) WallGraffiti(car, new Vector3(side * 1.56f, 2.6f, Range(rng, -3f, 3f)), side > 0f ? Vector3.right : Vector3.left, Range(rng, 4f, 7f), g);
                        }
                }
                else if (kind < 7)
                {
                    Primitive(PrimitiveType.Cylinder, "Cisterna", car, new Vector3(0f, 2.6f, 0f), new Vector3(3f, 7f, 3f), tank, keepCollider: false)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Primitive(PrimitiveType.Cylinder, "Domo", car, new Vector3(0f, 4.2f, 0f), new Vector3(0.9f, 0.3f, 0.9f), tank, keepCollider: false);
                }
                else if (kind < 9)
                {
                    // Tolva: caja alta con las tolvas inclinadas debajo.
                    Box("Tolva", car, new Vector3(0f, 2.9f, 0f), new Vector3(3.1f, 3.4f, 14.4f), hopper, collider: false);
                    foreach (float o in new[] { -4f, 0f, 4f })
                        Box("Boca", car, new Vector3(0f, 1.15f, o), new Vector3(2.2f, 0.5f, 2f), hopper, collider: false);
                }
                else
                {
                    // Batea con chatarra.
                    Box("Fondo", car, new Vector3(0f, 1.3f, 0f), new Vector3(3f, 0.3f, 15f), shells[0] ?? hopper, collider: false);
                    foreach (float side in new[] { -1f, 1f })
                        Box("Lateral", car, new Vector3(side * 1.45f, 2.1f, 0f), new Vector3(0.1f, 1.6f, 15f), shells[0] ?? hopper, collider: false);
                    foreach (float end in new[] { -1f, 1f })
                        Box("Testero", car, new Vector3(0f, 2.1f, end * 7.45f), new Vector3(3f, 1.6f, 0.1f), shells[0] ?? hopper, collider: false);
                    for (int k = 0; k < 9; k++)
                        Box("Chatarra", car, new Vector3(Range(rng, -0.9f, 0.9f), Range(rng, 1.9f, 2.7f), Range(rng, -6f, 6f)),
                            new Vector3(Range(rng, 0.5f, 1.6f), Range(rng, 0.3f, 0.9f), Range(rng, 0.8f, 2.4f)), scrap, collider: false)
                            .transform.localRotation = Quaternion.Euler(Range(rng, -20f, 20f), Range(rng, 0f, 180f), Range(rng, -20f, 20f));
                }
            }

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2.4f, 0f);
            box.size = new Vector3(3.1f, 4.4f, length);
            var body = root.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            SetLayerRecursively(root.gameObject, GameLayers.Vehicle);
            return root.gameObject.AddComponent<FreightTrain>();
        }

        /// <summary>Locomotora diésel genérica de 22 m (sin logotipos): morro corto, cabina, capó largo y franja amarilla.</summary>
        static void Locomotive(Transform parent, Vector3 center, float yaw, System.Random rng)
        {
            var loco = new GameObject("Locomotora").transform;
            loco.SetParent(parent, false);
            loco.localPosition = center;
            loco.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var paint = Mat("PinturaLocomotora", new Color(0.16f, 0.2f, 0.27f));
            var yellow = Mat("FranjaLocomotora", new Color(0.85f, 0.65f, 0.1f));
            var dark = Mat("BajosVagon", new Color(0.08f, 0.08f, 0.08f));
            var glass = Mat("CristalCoche", new Color(0.12f, 0.14f, 0.16f));
            var rail = Mat("PasamanosLocomotora", new Color(0.75f, 0.7f, 0.2f));
            var lamp = EmissiveMat("FaroTren", new Color(1f, 0.95f, 0.8f), 2f);
            float half = LocoLength / 2f;

            Box("Bastidor", loco, new Vector3(0f, 1.35f, 0f), new Vector3(3.1f, 0.5f, LocoLength), dark, collider: false);
            Box("Franja", loco, new Vector3(0f, 1.35f, 0f), new Vector3(3.14f, 0.18f, LocoLength - 0.4f), yellow, collider: false);
            foreach (float o in new[] { -half + 4f, half - 4f })
                Box("Bogie", loco, new Vector3(0f, 0.65f, o), new Vector3(2.8f, 0.8f, 4f), dark, collider: false);
            Box("Deposito", loco, new Vector3(0f, 0.75f, 0f), new Vector3(2.6f, 0.9f, 6f), dark, collider: false);
            // Capó largo detrás de la cabina, cabina y morro corto delante.
            Box("CapoLargo", loco, new Vector3(0f, 2.85f, -2.6f), new Vector3(2.5f, 2.5f, LocoLength - 7.2f), paint, collider: false);
            Box("Cabina", loco, new Vector3(0f, 3.05f, half - 4.6f), new Vector3(3.05f, 2.9f, 3.4f), paint, collider: false);
            Box("TechoCabina", loco, new Vector3(0f, 4.55f, half - 4.6f), new Vector3(3.1f, 0.12f, 3.6f), dark, collider: false);
            Box("Morro", loco, new Vector3(0f, 2.3f, half - 1.6f), new Vector3(2.4f, 1.4f, 2.6f), paint, collider: false);
            Box("Parabrisas", loco, new Vector3(0f, 3.75f, half - 2.88f), new Vector3(2.7f, 0.9f, 0.04f), glass, collider: false);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Ventanilla", loco, new Vector3(side * 1.535f, 3.75f, half - 4.6f), new Vector3(0.03f, 0.8f, 1.6f), glass, collider: false);
                Box("Pasamanos", loco, new Vector3(side * 1.5f, 2.35f, -1.5f), new Vector3(0.05f, 0.05f, LocoLength - 5f), rail, collider: false);
                for (float pz = -half + 2.5f; pz < half - 3f; pz += 3f)
                    Box("Montante", loco, new Vector3(side * 1.5f, 1.95f, pz), new Vector3(0.05f, 0.8f, 0.05f), rail, collider: false);
                Box("Faro", loco, new Vector3(side * 0.45f, 2.8f, half - 0.28f), new Vector3(0.3f, 0.22f, 0.04f), lamp, collider: false);
                Box("LuzCuneta", loco, new Vector3(side * 1.25f, 1.05f, half + 0.03f), new Vector3(0.22f, 0.22f, 0.04f), lamp, collider: false);
            }
            Box("Quitapiedras", loco, new Vector3(0f, 0.75f, half + 0.05f), new Vector3(2.9f, 0.6f, 0.15f), yellow, collider: false);
            Box("Escape", loco, new Vector3(0f, 4.2f, -1f), new Vector3(0.6f, 0.2f, 1.2f), dark, collider: false);
            for (int i = 0; i < 3; i++)
                Box("Ventilador", loco, new Vector3(0f, 4.12f, -5f - i * 2.2f), new Vector3(1.6f, 0.06f, 1.6f), dark, collider: false);
        }
    }
}
