using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Coches low-poly genéricos (sin marcas ni logotipos), de finales de los 90 y los 2000: sedán grande, coupé,
    /// todoterreno y furgoneta. La silueta sale de una malla extruida desde el perfil lateral (carrocería y
    /// habitáculo con los cristales inclinados); encima van paragolpes, calandra, faros, pilotos, matrículas en blanco,
    /// retrovisores, juntas de puertas, tiradores, montantes y ruedas con llanta. Los aparcados pueden llevar nieve,
    /// óxido, una puerta en imprimación o una ventana tapada con plástico. Referencias: coches de
    /// img/estructures/apartaments.jfif y trainavenue.avif.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        enum CarStyle { Sedan, Coupe, Suv, Van }

        /// <summary>Medidas de un modelo. Z hacia delante, Y arriba, suelo en y = 0.</summary>
        sealed class CarSpec
        {
            public float Length, Width, Clearance, WheelRadius, Wheelbase;
            public float Belt, Roof, Deck, HoodFront;                                     // línea de cintura, techo, maletero, morro
            public float CabinRearBase, CabinRearTop, CabinFrontTop, CabinFrontBase;      // habitáculo (z)
            public float FrontSeam = float.NaN;                                           // junta delantera de la puerta (NaN: justo detrás del paso de rueda)
            public float[] Pillars = new float[0], DoorSeams = new float[0];
            public bool Chrome;                                                            // paragolpes y calandra cromados
        }

        static CarSpec Spec(CarStyle style) => style switch
        {
            CarStyle.Coupe => new CarSpec
            {
                Length = 4.95f, Width = 1.95f, Clearance = 0.2f, WheelRadius = 0.38f, Wheelbase = 2.8f,
                Belt = 0.98f, Roof = 1.38f, Deck = 0.98f, HoodFront = 0.92f,
                CabinRearBase = -1.7f, CabinRearTop = -0.85f, CabinFrontTop = 0.2f, CabinFrontBase = 0.85f,
                DoorSeams = new[] { -1f }, Chrome = true,
            },
            CarStyle.Suv => new CarSpec
            {
                Length = 4.9f, Width = 2f, Clearance = 0.36f, WheelRadius = 0.42f, Wheelbase = 2.9f,
                Belt = 1.22f, Roof = 1.86f, Deck = 1.2f, HoodFront = 1.12f,
                CabinRearBase = -2.3f, CabinRearTop = -2.22f, CabinFrontTop = 0.6f, CabinFrontBase = 1.15f,
                Pillars = new[] { -0.4f, -1.5f }, DoorSeams = new[] { -0.4f, -0.93f },
            },
            CarStyle.Van => new CarSpec
            {
                Length = 5.4f, Width = 2f, Clearance = 0.28f, WheelRadius = 0.4f, Wheelbase = 3.3f,
                Belt = 1.15f, Roof = 2.05f, Deck = 1.15f, HoodFront = 1.05f,
                CabinRearBase = -2.62f, CabinRearTop = -2.62f, CabinFrontTop = 1.25f, CabinFrontBase = 1.85f,
                FrontSeam = 1.8f, Pillars = new[] { 0.95f, -0.6f, -1.9f }, DoorSeams = new[] { 0.95f, -0.6f },
            },
            _ => new CarSpec // sedán grande de tracción trasera
            {
                Length = 5.1f, Width = 1.95f, Clearance = 0.22f, WheelRadius = 0.38f, Wheelbase = 2.9f,
                Belt = 1.02f, Roof = 1.45f, Deck = 1f, HoodFront = 0.95f,
                CabinRearBase = -1.55f, CabinRearTop = -1.05f, CabinFrontTop = 0.45f, CabinFrontBase = 1.05f,
                Pillars = new[] { -0.3f }, DoorSeams = new[] { -0.3f, -0.93f }, Chrome = true,
            },
        };

        static Material[] carBodyPaints;
        static Material carPlayerPaint, carGlass, carChrome, carPlastic, carTire, carWell, carRimGrey, carHeadlight, carHeadlightOn,
            carTaillight, carPlate, carPrimer, carRust, carSnow, carBagPlastic;
        static readonly Dictionary<CarStyle, (Mesh body, Mesh cabin)> carMeshes = new();

        /// <summary>Materiales de los coches. Se llama al principio de Build(): los usan todas las zonas.</summary>
        static void PrepareCars()
        {
            Material Glossy(string name, Color color, float smoothness, float metallic = 0f)
            {
                var m = Mat(name, color);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", metallic);
                EditorUtility.SetDirty(m);
                return m;
            }

            carBodyPaints = new[]
            {
                Glossy("CarroceriaNegra", new Color(0.04f, 0.04f, 0.05f), 0.6f, 0.2f),
                Glossy("CarroceriaBlanca", new Color(0.78f, 0.78f, 0.76f), 0.55f, 0.1f),
                Glossy("CarroceriaGranate", new Color(0.33f, 0.05f, 0.07f), 0.6f, 0.3f),
                Glossy("CarroceriaAzul", new Color(0.08f, 0.12f, 0.26f), 0.6f, 0.3f),
                Glossy("CarroceriaPlata", new Color(0.55f, 0.56f, 0.58f), 0.6f, 0.6f),
                Glossy("CarroceriaVerde", new Color(0.07f, 0.17f, 0.12f), 0.55f, 0.3f),
                Glossy("CarroceriaBeige", new Color(0.6f, 0.54f, 0.43f), 0.45f, 0.1f),
                Glossy("CarroceriaGris", new Color(0.32f, 0.33f, 0.35f), 0.5f, 0.4f),
            };
            carPlayerPaint = Glossy("CarroceriaJugador", new Color(0.1f, 0.13f, 0.24f), 0.65f, 0.35f);
            carGlass = Glossy("CristalCoche", new Color(0.04f, 0.05f, 0.06f), 0.85f, 0.2f);
            carChrome = Glossy("CromoCoche", new Color(0.75f, 0.75f, 0.78f), 0.8f, 1f);
            carPlastic = Glossy("PlasticoNegro", new Color(0.07f, 0.07f, 0.08f), 0.2f);
            carTire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            carWell = Mat("PasoRueda", new Color(0.03f, 0.03f, 0.03f));
            carRimGrey = Glossy("LlantaGris", new Color(0.45f, 0.46f, 0.48f), 0.5f, 0.6f);
            carHeadlight = Glossy("FaroCoche", new Color(0.82f, 0.82f, 0.76f), 0.9f);
            carHeadlightOn = EmissiveMat("FaroCocheEncendido", new Color(1f, 0.95f, 0.8f), 2f);
            carTaillight = Glossy("PilotoTrasero", new Color(0.55f, 0.04f, 0.04f), 0.8f);
            carPlate = Mat("Matricula", new Color(0.85f, 0.84f, 0.78f));
            carPrimer = Mat("Imprimacion", new Color(0.45f, 0.46f, 0.44f));
            carRust = Mat("PinturaOxido", new Color(0.38f, 0.2f, 0.12f));
            carSnow = Glossy("NieveCoche", new Color(0.88f, 0.9f, 0.93f), 0.3f);
            carBagPlastic = Glossy("PlasticoVentana", new Color(0.15f, 0.15f, 0.16f), 0.6f);
            carMeshes.Clear();
        }

        /// <summary>Coche aparcado estático: modelo al azar, con el largo a lo largo de X local antes de girar (como antes).</summary>
        static void ParkedCar(Transform parent, Vector3 ground, float yaw, System.Random rng)
        {
            double roll = rng.NextDouble();
            var style = roll < 0.4 ? CarStyle.Sedan : roll < 0.55 ? CarStyle.Coupe : roll < 0.85 ? CarStyle.Suv : CarStyle.Van;
            CarModel(parent, "CocheAparcado", ground, yaw + 90f, style, carBodyPaints[rng.Next(carBodyPaints.Length)], rng);
        }

        /// <summary>
        /// Crea un coche con el suelo en <paramref name="position"/> y el morro hacia +Z local girado <paramref name="yaw"/>.
        /// Sin <paramref name="wheels"/> no pone ruedas (el coche que se conduce pone las suyas, que mueve su WheelCollider).
        /// </summary>
        static Transform CarModel(Transform parent, string name, Vector3 position, float yaw, CarStyle style, Material paint, System.Random rng,
            bool wheels = true, bool weathered = true, bool lightsOn = false, bool collider = true)
        {
            var s = Spec(style);
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float hl = s.Length / 2f, hw = s.Width / 2f, r = s.WheelRadius, clr = s.Clearance;
            float archTop = 2f * r + 0.02f, archHalf = r + 0.1f;
            float wf = s.Wheelbase / 2f, wr = -s.Wheelbase / 2f;
            float CabinHalfWidth(float y) => Mathf.Lerp(hw - 0.06f, hw - 0.22f, Mathf.InverseLerp(s.Belt, s.Roof, y));
            bool InWell(float z) => Mathf.Abs(z - wf) < archHalf || Mathf.Abs(z - wr) < archHalf;
            var trim = s.Chrome ? carChrome : carPlastic;

            // Carrocería (malla con el perfil del capó y el maletero) y habitáculo (cristales; el techo en el color de la pintura).
            if (!carMeshes.TryGetValue(style, out var meshes))
            {
                meshes = BuildCarMeshes(style, s, archTop, CabinHalfWidth);
                carMeshes[style] = meshes;
            }
            MeshPart("Carroceria", root, meshes.body, paint);
            MeshPart("Habitaculo", root, meshes.cabin, carGlass, paint);

            // Faldones entre los pasos de rueda y paso de rueda oscuro (para no ver a través).
            void Skirt(float z0, float z1)
            {
                if (z1 - z0 > 0.05f)
                    Box("Faldon", root, new Vector3(0f, (clr + archTop) / 2f, (z0 + z1) / 2f), new Vector3(s.Width, archTop - clr, z1 - z0), paint, collider: false);
            }
            Skirt(-hl, wr - archHalf);
            Skirt(wr + archHalf, wf - archHalf);
            Skirt(wf + archHalf, hl);
            foreach (float wz in new[] { wf, wr })
                Box("PasoRueda", root, new Vector3(0f, (clr + archTop) / 2f + 0.02f, wz), new Vector3(s.Width - 0.06f, archTop - clr - 0.04f, archHalf * 2f - 0.02f), carWell, collider: false);
            if (wheels)
                foreach (float wz in new[] { wf, wr })
                foreach (float side in new[] { -1f, 1f })
                    CarWheel(root, "Rueda", new Vector3(side * (hw - 0.12f), r, wz), r, side, PickRim(style, rng));

            // Montantes del habitáculo y moldura de cintura.
            void Pillar(float zBottom, float zTop)
            {
                foreach (float side in new[] { -1f, 1f })
                    Strut("Montante", root, new Vector3(side * (CabinHalfWidth(s.Belt) + 0.012f), s.Belt, zBottom),
                        new Vector3(side * (CabinHalfWidth(s.Roof) + 0.012f), s.Roof, zTop), 0.08f, paint);
            }
            Pillar(s.CabinFrontBase - 0.03f, s.CabinFrontTop + 0.02f);
            Pillar(s.CabinRearBase + 0.03f, s.CabinRearTop - 0.02f);
            foreach (float z in s.Pillars) Pillar(z, z - 0.04f);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("MolduraCintura", root, new Vector3(side * (CabinHalfWidth(s.Belt) + 0.012f), s.Belt + 0.01f, (s.CabinFrontBase + s.CabinRearBase) / 2f),
                    new Vector3(0.02f, 0.03f, s.CabinFrontBase - s.CabinRearBase), trim, collider: false);
                Box("MolduraLateral", root, new Vector3(side * (hw + 0.01f), archTop + 0.06f, 0f), new Vector3(0.02f, 0.05f, s.Length - 0.6f), carPlastic, collider: false);
                Box("Retrovisor", root, new Vector3(side * (hw + 0.1f), s.Belt + 0.08f, s.CabinFrontBase - 0.15f), new Vector3(0.18f, 0.12f, 0.1f), paint, collider: false);
            }

            // Juntas de las puertas y tiradores.
            var seams = new List<float> { float.IsNaN(s.FrontSeam) ? wf - archHalf - 0.04f : s.FrontSeam };
            seams.AddRange(s.DoorSeams);
            seams.Sort((a, b) => b.CompareTo(a));
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float z in seams)
                {
                    float y0 = InWell(z) ? archTop + 0.02f : clr + 0.06f, y1 = s.Belt - 0.04f;
                    Box("Junta", root, new Vector3(side * (hw + 0.004f), (y0 + y1) / 2f, z), new Vector3(0.012f, y1 - y0, 0.025f), carWell, collider: false);
                }
                for (int i = 0; i + 1 < seams.Count; i++)
                    Box("Tirador", root, new Vector3(side * (hw + 0.01f), s.Belt - 0.12f, seams[i + 1] + 0.22f), new Vector3(0.02f, 0.04f, 0.2f), trim, collider: false);
            }

            // Delante: paragolpes, calandra, faros y matrícula (en blanco).
            bool boxy = style == CarStyle.Suv || style == CarStyle.Van;
            foreach (float end in new[] { -1f, 1f })
                Box("Paragolpes", root, new Vector3(0f, clr + 0.2f, end * hl), new Vector3(s.Width + 0.04f, 0.22f, 0.2f), trim, collider: false);
            float lightY = archTop + 0.04f;
            Box("Calandra", root, new Vector3(0f, lightY, hl + 0.012f), new Vector3(s.Width - 1.25f, boxy ? 0.2f : 0.16f, 0.03f), trim, collider: false);
            foreach (float side in new[] { -1f, 1f })
                Box("Faro", root, new Vector3(side * (hw - 0.32f), lightY, hl + 0.015f), new Vector3(0.42f, 0.16f, 0.03f), lightsOn ? carHeadlightOn : carHeadlight, collider: false);
            Box("Matricula", root, new Vector3(0f, clr + 0.2f, hl + 0.11f), new Vector3(0.5f, 0.14f, 0.02f), carPlate, collider: false);

            // Detrás: pilotos (verticales en el todoterreno y la furgoneta) y matrícula.
            foreach (float side in new[] { -1f, 1f })
            {
                var size = boxy ? new Vector3(0.18f, 0.4f, 0.03f) : new Vector3(0.42f, 0.18f, 0.03f);
                float y = boxy ? s.Belt - 0.22f : archTop + 0.02f;
                Box("Piloto", root, new Vector3(side * (hw - (boxy ? 0.1f : 0.3f)), y, -hl - 0.015f), size, carTaillight, collider: false);
            }
            Box("Matricula", root, new Vector3(0f, archTop - 0.12f, -hl - 0.012f), new Vector3(0.5f, 0.14f, 0.02f), carPlate, collider: false);
            if (style == CarStyle.Sedan)
                Strut("Antena", root, new Vector3(hw - 0.25f, s.Deck, -hl + 0.5f), new Vector3(hw - 0.25f, s.Deck + 0.75f, -hl + 0.42f), 0.012f, carPlastic);
            if (style == CarStyle.Suv)
                foreach (float side in new[] { -1f, 1f })
                    Box("BarraTecho", root, new Vector3(side * (CabinHalfWidth(s.Roof) - 0.08f), s.Roof + 0.06f, (s.CabinFrontTop + s.CabinRearTop) / 2f),
                        new Vector3(0.05f, 0.06f, (s.CabinFrontTop - s.CabinRearTop) * 0.85f), carPlastic, collider: false);

            // Desgaste de invierno (solo aparcados): nieve, óxido, puerta en imprimación, ventana con plástico.
            if (weathered)
            {
                if (rng.NextDouble() < 0.35)
                {
                    Box("NieveTecho", root, new Vector3(0f, s.Roof + 0.04f, (s.CabinFrontTop + s.CabinRearTop) / 2f),
                        new Vector3(2f * CabinHalfWidth(s.Roof) - 0.1f, 0.08f, s.CabinFrontTop - s.CabinRearTop - 0.15f), carSnow, collider: false);
                    if (rng.NextDouble() < 0.5)
                        Slab(root, new Vector3(0f, s.Belt + 0.03f, s.CabinFrontBase - 0.1f), new Vector3(0f, s.HoodFront + 0.03f, hl - 0.4f), s.Width - 0.25f, 0.05f, carSnow);
                }
                if (rng.NextDouble() < 0.15)
                    for (int i = 0; i < 1 + rng.Next(3); i++)
                    {
                        float side = rng.Next(2) == 0 ? -1f : 1f, wz = rng.Next(2) == 0 ? wf : wr;
                        Box("Oxido", root, new Vector3(side * (hw + 0.005f), Range(rng, clr + 0.12f, archTop - 0.1f), wz + (rng.Next(2) == 0 ? -1f : 1f) * (archHalf + Range(rng, 0.15f, 0.35f))),
                            new Vector3(0.012f, Range(rng, 0.12f, 0.26f), Range(rng, 0.25f, 0.5f)), carRust, collider: false);
                    }
                if (seams.Count >= 2 && rng.NextDouble() < 0.1)
                {
                    float side = rng.Next(2) == 0 ? -1f : 1f, z0 = seams[0], z1 = seams[1];
                    Box("PuertaImprimacion", root, new Vector3(side * (hw + 0.006f), (clr + s.Belt) / 2f, (z0 + z1) / 2f),
                        new Vector3(0.01f, s.Belt - clr - 0.18f, z0 - z1 - 0.03f), carPrimer, collider: false);
                }
                if (rng.NextDouble() < 0.06)
                {
                    float side = rng.Next(2) == 0 ? -1f : 1f, y = (s.Belt + s.Roof) / 2f;
                    Box("PlasticoVentana", root, new Vector3(side * (CabinHalfWidth(y) + 0.015f), y, Mathf.Lerp(s.CabinRearBase, s.CabinFrontBase, 0.3f)),
                        new Vector3(0.01f, (s.Roof - s.Belt) * 0.7f, 0.6f), carBagPlastic, collider: false);
                }
            }

            if (collider)
            {
                var lower = root.gameObject.AddComponent<BoxCollider>();
                lower.center = new Vector3(0f, (clr + s.Belt) / 2f, 0f);
                lower.size = new Vector3(s.Width, s.Belt - clr, s.Length);
                var cabin = root.gameObject.AddComponent<BoxCollider>();
                cabin.center = new Vector3(0f, (s.Belt + s.Roof) / 2f, (s.CabinFrontBase + s.CabinRearBase) / 2f);
                cabin.size = new Vector3(s.Width - 0.2f, s.Roof - s.Belt, s.CabinFrontBase - s.CabinRearBase);
            }
            return root;
        }

        static Material PickRim(CarStyle style, System.Random rng)
        {
            double chrome = style == CarStyle.Van ? 0.15 : 0.45; // llantas grandes cromadas, muy de la época
            double roll = rng.NextDouble();
            return roll < chrome ? carChrome : roll < chrome + 0.3 ? carRimGrey : carPlastic;
        }

        /// <summary>
        /// Rueda: neumático, llanta con borde y fondo oscuro, 5 radios y buje. El eje de giro es el X del pivote
        /// (lo que pide VehicleController); <paramref name="side"/> dice hacia qué lado queda la cara exterior.
        /// </summary>
        static Transform CarWheel(Transform parent, string name, Vector3 position, float radius, float side, Material rim)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            var axis = Quaternion.Euler(0f, 0f, 90f);
            Primitive(PrimitiveType.Cylinder, "Neumatico", pivot, Vector3.zero, new Vector3(radius * 2f, 0.12f, radius * 2f), carTire, keepCollider: false).transform.localRotation = axis;
            Primitive(PrimitiveType.Cylinder, "Llanta", pivot, new Vector3(side * 0.105f, 0f, 0f), new Vector3(radius * 1.3f, 0.025f, radius * 1.3f), rim, keepCollider: false).transform.localRotation = axis;
            Primitive(PrimitiveType.Cylinder, "FondoLlanta", pivot, new Vector3(side * 0.12f, 0f, 0f), new Vector3(radius * 1.05f, 0.02f, radius * 1.05f), carWell, keepCollider: false).transform.localRotation = axis;
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f * Mathf.Deg2Rad;
                var spoke = Box("Radio", pivot, new Vector3(side * 0.135f, Mathf.Sin(a) * radius * 0.33f, Mathf.Cos(a) * radius * 0.33f),
                    new Vector3(0.015f, 0.07f, radius * 0.6f), rim, collider: false);
                spoke.transform.localRotation = Quaternion.Euler(-k * 72f, 0f, 0f);
            }
            Primitive(PrimitiveType.Cylinder, "Buje", pivot, new Vector3(side * 0.14f, 0f, 0f), new Vector3(radius * 0.4f, 0.02f, radius * 0.4f), rim, keepCollider: false).transform.localRotation = axis;
            return pivot;
        }

        /// <summary>Mallas de un modelo: la carrocería (perfil de capó y maletero) y el habitáculo (cristales + techo).</summary>
        static (Mesh body, Mesh cabin) BuildCarMeshes(CarStyle style, CarSpec s, float archTop, System.Func<float, float> cabinHalfWidth)
        {
            float hl = s.Length / 2f, hw = s.Width / 2f;
            var body = new List<Vector2> { new(-hl, archTop) };
            if (s.CabinRearBase > -hl + 0.4f)
            {
                // Maletero: de la trasera sube a la tapa y luego a la base de la luna trasera.
                body.Add(new Vector2(-hl, s.Deck - 0.08f));
                body.Add(new Vector2(-hl + 0.25f, s.Deck));
                body.Add(new Vector2(s.CabinRearBase, s.Belt));
            }
            else body.Add(new Vector2(-hl, s.Belt)); // portón: trasera recta
            body.Add(new Vector2(s.CabinFrontBase, s.Belt));
            body.Add(new Vector2(hl - 0.3f, s.HoodFront));
            body.Add(new Vector2(hl, s.HoodFront - 0.06f));
            body.Add(new Vector2(hl, archTop));
            var bodyMesh = ProfileMesh($"Carroceria_{style}", body, _ => hw, new int[body.Count], 0, 1);

            // Habitáculo: luna trasera, techo, parabrisas y base (oculta). Submalla 0 = cristal, 1 = pintura.
            var cabin = new List<Vector2>
            {
                new(s.CabinRearBase, s.Belt), new(s.CabinRearTop, s.Roof), new(s.CabinFrontTop, s.Roof), new(s.CabinFrontBase, s.Belt),
            };
            var cabinMesh = ProfileMesh($"Habitaculo_{style}", cabin, cabinHalfWidth, new[] { 0, 1, 0, 1 }, 0, 2);
            return (bodyMesh, cabinMesh);
        }

        /// <summary>
        /// Extruye un perfil lateral convexo (z, y) a lo ancho: el semiancho en cada punto lo da <paramref name="halfWidth"/>(y).
        /// Cada tramo del perfil es una banda con su submalla; los dos laterales usan <paramref name="capSub"/>.
        /// Vértices sin compartir (sombreado plano, aire PS2) y caras orientadas hacia fuera desde el centro.
        /// </summary>
        static Mesh ProfileMesh(string name, List<Vector2> profile, System.Func<float, float> halfWidth, int[] bandSub, int capSub, int subCount)
        {
            var vertices = new List<Vector3>();
            var subs = new List<int>[subCount];
            for (int k = 0; k < subCount; k++) subs[k] = new List<int>();
            var c = Vector2.zero;
            foreach (var p in profile) c += p;
            c /= profile.Count;
            var center = new Vector3(0f, c.y, c.x);

            void Tri(int sub, Vector3 a, Vector3 b, Vector3 d)
            {
                var normal = Vector3.Cross(b - a, d - a);
                if (normal.sqrMagnitude < 1e-10f) return;
                if (Vector3.Dot(normal, (a + b + d) / 3f - center) < 0f) (b, d) = (d, b);
                int i = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(d);
                subs[sub].Add(i);
                subs[sub].Add(i + 1);
                subs[sub].Add(i + 2);
            }
            Vector3 At(Vector2 p, float side) => new(side * halfWidth(p.y), p.y, p.x);

            int n = profile.Count;
            for (int i = 0; i < n; i++)
            {
                var a = profile[i];
                var b = profile[(i + 1) % n];
                if ((a - b).sqrMagnitude < 1e-6f) continue;
                Tri(bandSub[i], At(a, -1f), At(a, 1f), At(b, 1f));
                Tri(bandSub[i], At(a, -1f), At(b, 1f), At(b, -1f));
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var cap = new Vector3(side * halfWidth(c.y), c.y, c.x);
                for (int i = 0; i < n; i++)
                    Tri(capSub, cap, At(profile[i], side), At(profile[(i + 1) % n], side));
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = subCount;
            for (int k = 0; k < subCount; k++) mesh.SetTriangles(subs[k], k);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static GameObject MeshPart(string name, Transform parent, Mesh mesh, params Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }
    }
}
