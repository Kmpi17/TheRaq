using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Barrio de casas, al norte del barrio de viviendas: casas bajas (de ladrillo con tejado a dos aguas y
    /// greystones de piedra gris) y apartamentos de 3 pisos con porches traseros de madera. Las calles no forman una
    /// cuadrícula: una calle sin salida, otra con un quiebro y una red de callejones traseros con vallas de madera,
    /// garajes, cubos de basura, maleza y postes con cables. Referencias: img/estructures/houses.png, apartaments2.jpg,
    /// backparts.png, block2.png, apartaments.jfif.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Zona: x ∈ [HousesMinX, HousesMaxX], z ∈ [HousesMinZ, HousesMaxZ]. Se entra desde la avenida (x = 110).
        const float HousesMinX = 120f, HousesMaxX = 250f, HousesMinZ = 90f, HousesMaxZ = 214f;
        const float HousesRoadWidth = 10f, HousesSidewalk = 2.5f, AlleyTop = 0.12f;

        // Calzadas (x0, z0, x1, z1). A: entrada desde la avenida. B: norte–sur; sin salida por el sur y, por el norte,
        // sigue en la ampliación norte (G en el este, B' hasta F en la copia oeste).
        // C1 y C2: la misma calle con un quiebro de 10 m (Cq une las dos).
        static readonly Rect[] HousesRoads =
        {
            Rect.MinMaxRect(117f, 117f, 207f, 127f),   // A
            Rect.MinMaxRect(207f, 92f, 217f, 202f),    // B
            Rect.MinMaxRect(117f, 183f, 173f, 193f),   // C1
            Rect.MinMaxRect(163f, 171f, 207f, 181f),   // C2
            Rect.MinMaxRect(163f, 181f, 173f, 183f),   // Cq (quiebro)
        };

        // Callejones traseros (5 m): a1 sur, a2 centro, a3 pasaje norte–sur, a4 norte, a5 este, a6 pasaje entre bloques.
        static readonly Rect[] HousesAlleys =
        {
            Rect.MinMaxRect(120f, 92f, 241f, 97f),     // a1 (llega al callejón este)
            Rect.MinMaxRect(120f, 147f, 204.5f, 152f), // a2
            Rect.MinMaxRect(185f, 152f, 190f, 168.5f), // a3
            Rect.MinMaxRect(120f, 208.5f, 246f, 213.5f), // a4
            Rect.MinMaxRect(241f, 92f, 246f, 213.5f),  // a5
            Rect.MinMaxRect(219.5f, 126f, 241f, 131f), // a6
        };

        // Aceras (en orden: cada una se corta donde la pisan calzadas, callejones y aceras anteriores).
        static readonly Rect[] HousesSidewalks =
        {
            Rect.MinMaxRect(120f, 114.5f, 207f, 117f),    // A sur
            Rect.MinMaxRect(120f, 127f, 207f, 129.5f),    // A norte
            Rect.MinMaxRect(204.5f, 92f, 207f, 202f),     // B oeste
            Rect.MinMaxRect(217f, 92f, 219.5f, 202f),     // B este
            Rect.MinMaxRect(120f, 180.5f, 163f, 183f),    // C1 sur
            Rect.MinMaxRect(120f, 193f, 175.5f, 195.5f),  // C1 norte
            Rect.MinMaxRect(173f, 181f, 175.5f, 193f),    // quiebro este
            Rect.MinMaxRect(175.5f, 181f, 207f, 183.5f),  // C2 norte
            Rect.MinMaxRect(163f, 168.5f, 207f, 171f),    // C2 sur
            Rect.MinMaxRect(160.5f, 168.5f, 163f, 180.5f), // quiebro oeste
        };

        // Césped al final de B (local z 202–214). En la copia oeste se queda; en el este, B sigue por ahí hasta G.
        static readonly Rect HousesLawnBNorth = Rect.MinMaxRect(207f, 202f, 217f, 214f);

        // Manzanas con césped (lo que no es calzada).
        static readonly Rect[] HousesLawns =
        {
            Rect.MinMaxRect(120f, 90f, 207f, 117f),
            Rect.MinMaxRect(120f, 127f, 207f, 171f),
            Rect.MinMaxRect(120f, 171f, 163f, 183f),
            Rect.MinMaxRect(120f, 193f, 173f, 214f),
            Rect.MinMaxRect(173f, 181f, 207f, 214f),
            HousesLawnBNorth,
            Rect.MinMaxRect(217f, 90f, 250f, 214f),
        };

        enum RowKind { Houses, Apartments, Vacant, Overgrown }

        // Materiales del barrio (PrepareHouses).
        static Material[] houseFacades;
        static Material greystone, aptFacadeA, aptFacadeB, shingles, tarRoof, stoneTrim, woodFence, alleyMat, sidewalkMat, lawnMat,
            doorMat, plywood, porchWood, ironFence, chainLink, ironMat, concreteMat, garageDoor, glassBlock, darkGlass;
        static Material[] garageWalls, binColors, carPaints, graffitiMats;
        static Material porchRail, backDoor;
        // Ladrillo liso (sin ventanas) de cada fachada, para hastiales y antepechos (lo rellena Facade en PrepareHouses).
        static readonly Dictionary<Material, Material> plainBrick = new();

        static readonly List<Rect> housesOccupied = new();
        static readonly List<(string kind, Vector2[] corners, float height)> housesFootprints = new();

        // Copia del barrio al oeste de la avenida: el mismo trazado girado 180° (mundo = HousesWestOrigin − local), así la
        // entrada desde la avenida queda en x = 103 y el callejón norte (a4) pasa a ser el de atrás de la franja de negocios.
        static readonly Vector3 HousesWestOrigin = new(220f, 0f, 316f);

        // Marco del barrio que se está construyendo: housesOccupied se guarda en sus coordenadas locales, que son las
        // de HousesRoads, HousesLawns, etc.
        static Transform housesFrame;

        static void BuildHousesDistrict(Transform world)
        {
            PrepareHouses();
            housesFootprints.Clear();
            BuildHousesVariant(world, "Barrio_Casas", Vector3.zero, 0f, 1999, west: false);
            BuildHousesVariant(world, "Barrio_Casas_Oeste", HousesWestOrigin, 180f, 2001, west: true);
            BuildHousesWestExtension(world); // PrototypeSceneBuilder.HousesWestExt.cs: la ampliación hasta la calle x = −110
            BuildHousesNorth(world);         // PrototypeSceneBuilder.HousesNorth.cs: la ampliación hacia el norte (los dos lados)
            BuildShopsStrip(world); // PrototypeSceneBuilder.Shops.cs: los negocios del lado que da a la manzana central
            BuildFarWest(world);    // PrototypeSceneBuilder.FarWest.cs: al otro lado de la calle x = −110 (después de Shops: usa Storefront)
            ExportHousesFootprints();
        }

        static void BuildHousesVariant(Transform world, string name, Vector3 origin, float yaw, int seed, bool west)
        {
            var group = new GameObject(name).transform;
            group.SetParent(world, false);
            group.localPosition = origin;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
            housesFrame = group;
            housesOccupied.Clear();
            var rng = new System.Random(seed);

            // En el este, B sigue hacia el norte como calle G (HousesNorth.cs): ahí no va el césped del final de B.
            var lawns = west ? HousesLawns : System.Array.FindAll(HousesLawns, r => r != HousesLawnBNorth);
            BuildHousesGround(group, HousesRoads, HousesAlleys, HousesSidewalks, lawns);

            // Filas de solares. El origen de cada fila está en el centro de su línea de fachada (junto a la acera);
            // su +Z local apunta hacia dentro de la parcela. yaw 0: la parcela queda al norte; 180: al sur; 90: al este.
            var rows = new GameObject("Parcelas").transform;
            rows.SetParent(group, false);
            Row(rows, "A_Sur", new Vector3(162.5f, 0f, 114.5f), 180f, 81f, 17.5f, RowKind.Houses, rng);
            Row(rows, "A_Norte", new Vector3(162.5f, 0f, 129.5f), 0f, 81f, 17.5f, RowKind.Houses, rng);
            Row(rows, "C2_Sur_O", new Vector3(174.5f, 0f, 168.5f), 180f, 19f, 16.5f, RowKind.Houses, rng);
            Row(rows, "C2_Sur_E", new Vector3(197f, 0f, 168.5f), 180f, 13f, 16.5f, RowKind.Houses, rng);
            Row(rows, "C1_Sur", new Vector3(140.5f, 0f, 180.5f), 180f, 37f, 28.5f, RowKind.Apartments, rng);
            Row(rows, "C1_Norte", new Vector3(147f, 0f, 195.5f), 0f, 50f, 13f, RowKind.Houses, rng);
            Row(rows, "C2_Norte", new Vector3(197.5f, 0f, 183.5f), 0f, 13f, 25f, RowKind.Houses, rng);
            Row(rows, "B_Este_1", new Vector3(219.5f, 0f, 111.5f), 90f, 27f, 21.5f, RowKind.Apartments, rng);
            Row(rows, "B_Este_2", new Vector3(219.5f, 0f, 145f), 90f, 26f, 21.5f, RowKind.Apartments, rng);
            Row(rows, "B_Este_Solar", new Vector3(219.5f, 0f, 167.5f), 90f, 17f, 21.5f, RowKind.Vacant, rng);
            Row(rows, "B_Este_3", new Vector3(219.5f, 0f, 189f), 90f, 24f, 21.5f, RowKind.Apartments, rng);

            CornerStore(group, rng);
            BuildHousesStreetLife(group, rng, west);
            BuildHousesWeather(group, rng, HousesRoads, HousesAlleys, HousesSidewalks, lawns);
        }

        static void PrepareHouses()
        {
            plainBrick.Clear();
            Material Facade(string name, Color brick, bool ac, int seed)
            {
                var facadeMat = TexturedMat(name, ProceduralTextures.BrickFacade(name, brick, ac, seed));
                plainBrick[facadeMat] = TexturedMat(name + "Liso", ProceduralTextures.PlainBrick(name + "Liso", brick, seed));
                return facadeMat;
            }

            houseFacades = new[]
            {
                Facade("CasaLadrilloRojo", new Color(0.55f, 0.29f, 0.21f), false, 11),
                Facade("CasaLadrilloOscuro", new Color(0.4f, 0.21f, 0.17f), false, 12),
                Facade("CasaLadrilloAmarillo", new Color(0.66f, 0.55f, 0.38f), true, 13),
            };
            greystone = Facade("CasaPiedraGris", new Color(0.56f, 0.55f, 0.52f), false, 14);
            aptFacadeA = Facade("ApartamentoLadrillo", new Color(0.45f, 0.22f, 0.17f), true, 15);
            aptFacadeB = Facade("ApartamentoLadrilloClaro", new Color(0.62f, 0.45f, 0.36f), false, 16);
            shingles = TexturedMat("TejasCasa", ProceduralTextures.Shingles("TejasCasa", new Color(0.3f, 0.3f, 0.32f), 17));
            tarRoof = LoadMat("TejadoAlquitran") ?? Mat("TejadoAlquitran", new Color(0.3f, 0.3f, 0.31f));
            stoneTrim = Mat("PiedraCornisa", new Color(0.62f, 0.6f, 0.55f));
            woodFence = TexturedMat("VallaMadera", ProceduralTextures.WoodFence("VallaMadera", 18));
            alleyMat = TexturedMat("Callejon", ProceduralTextures.Alley("Callejon", 19));
            sidewalkMat = LoadMat("AceraBarrio");
            lawnMat = LoadMat("CespedSeco");
            doorMat = Mat("PuertaCasa", new Color(0.25f, 0.17f, 0.12f));
            plywood = Mat("Contrachapado", new Color(0.58f, 0.48f, 0.33f));
            porchWood = Mat("MaderaPorche", new Color(0.4f, 0.33f, 0.25f));
            porchRail = CutoutMat("BarandaPorche", ProceduralTextures.PorchRail("BarandaPorche", new Color(0.42f, 0.35f, 0.27f), 20));
            backDoor = Mat("PuertaTrasera", new Color(0.3f, 0.22f, 0.15f));
            ironFence = LoadMat("VallaHierro");
            chainLink = LoadMat("MallaMetalica");
            ironMat = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            concreteMat = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            garageDoor = Mat("PuertaGaraje", new Color(0.7f, 0.69f, 0.64f));
            glassBlock = Mat("BloqueVidrio", new Color(0.68f, 0.74f, 0.74f));
            darkGlass = Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            garageWalls = new[]
            {
                Mat("GarajeGris", new Color(0.52f, 0.52f, 0.5f)),
                Mat("GarajeBlanco", new Color(0.72f, 0.7f, 0.66f)),
                Mat("GarajeVerde", new Color(0.32f, 0.38f, 0.3f)),
                Mat("GarajeMarron", new Color(0.4f, 0.3f, 0.22f)),
            };
            binColors = new[] { Mat("CuboAzul", new Color(0.12f, 0.25f, 0.55f)), Mat("CuboNegro", new Color(0.06f, 0.06f, 0.07f)) };
            carPaints = new[]
            {
                Mat("PinturaGranate", new Color(0.35f, 0.06f, 0.08f)), Mat("PinturaNegra", new Color(0.05f, 0.05f, 0.06f)),
                Mat("PinturaBlanca", new Color(0.75f, 0.75f, 0.73f)), Mat("PinturaAzulOscuro", new Color(0.08f, 0.12f, 0.25f)),
            };
            graffitiMats = new Material[4];
            for (int i = 0; i < graffitiMats.Length; i++) graffitiMats[i] = LoadMat($"Grafiti_{i}");
        }

        static Material LoadMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/{name}.mat");

        // ---------- Suelo: césped, calzadas, aceras y callejones ----------

        /// <summary>Césped, calzadas, callejones y aceras de un barrio de casas, a partir de sus tablas de rectángulos.</summary>
        static void BuildHousesGround(Transform parent, Rect[] roads, Rect[] alleys, Rect[] sidewalks, Rect[] lawns)
        {
            var group = new GameObject("Suelo").transform;
            group.SetParent(parent, false);
            var road = TexturedMat("CalleCasas", ProceduralTextures.Road("CalleCasas", HousesRoadWidth, 8));
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));

            foreach (var r in lawns)
                TexturedBox("Cesped", group, new Vector3(r.center.x, 0.05f, r.center.y), new Vector3(r.width, 0.1f, r.height), lawnMat, Vector2.one * ProceduralTextures.GrassTile);

            foreach (var r in roads)
            {
                bool eastWest = r.width > r.height;
                if (Mathf.Min(r.width, r.height) < HousesRoadWidth - 0.1f)
                {
                    Box("Cruce", group, new Vector3(r.center.x, 0.01f, r.center.y), new Vector3(r.width, 0.02f, r.height), asphalt, collider: false);
                    continue;
                }
                // La U de la textura cruza la calle: las E–O se giran 90°.
                var size = eastWest ? new Vector3(r.height, 0.02f, r.width) : new Vector3(r.width, 0.02f, r.height);
                var go = TexturedBox("Calle", group, new Vector3(r.center.x, 0.01f, r.center.y), size, road, Vector2.one * HousesRoadWidth, collider: false);
                if (eastWest) go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            foreach (var r in alleys)
            {
                bool eastWest = r.width > r.height;
                var size = eastWest ? new Vector3(r.height, AlleyTop, r.width) : new Vector3(r.width, AlleyTop, r.height);
                var go = TexturedBox("Callejon", group, new Vector3(r.center.x, AlleyTop / 2f, r.center.y), size, alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
                if (eastWest) go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Aceras: cada franja se trocea donde la cruzan calzadas, callejones o aceras ya puestas (sin solapes).
            var placed = new List<Rect>();
            foreach (var strip in sidewalks)
            {
                var cutters = new List<Rect>(roads);
                cutters.AddRange(alleys);
                cutters.AddRange(placed);
                foreach (var piece in StripMinus(strip, cutters))
                {
                    TexturedBox("Acera", group, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height),
                        sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);
                    placed.Add(piece);
                }
            }
        }

        /// <summary>Trocea una franja a lo largo de su eje largo, quitando los tramos que tocan algún rectángulo.</summary>
        static List<Rect> StripMinus(Rect strip, List<Rect> cutters)
        {
            bool alongX = strip.width >= strip.height;
            float a0 = alongX ? strip.xMin : strip.yMin, a1 = alongX ? strip.xMax : strip.yMax;
            var holes = new List<(float, float)>();
            foreach (var c in cutters)
            {
                if (!c.Overlaps(strip)) continue;
                holes.Add(alongX ? (c.xMin, c.xMax) : (c.yMin, c.yMax));
            }
            holes.Sort((p, q) => p.Item1.CompareTo(q.Item1));
            var result = new List<Rect>();
            float cursor = a0;
            foreach (var (h0, h1) in holes)
            {
                if (h0 > cursor + 0.3f) result.Add(alongX ? Rect.MinMaxRect(cursor, strip.yMin, Mathf.Min(h0, a1), strip.yMax)
                                                          : Rect.MinMaxRect(strip.xMin, cursor, strip.xMax, Mathf.Min(h0, a1)));
                cursor = Mathf.Max(cursor, h1);
            }
            if (a1 > cursor + 0.3f) result.Add(alongX ? Rect.MinMaxRect(cursor, strip.yMin, a1, strip.yMax) : Rect.MinMaxRect(strip.xMin, cursor, strip.xMax, a1));
            return result;
        }

        // ---------- Filas de parcelas ----------

        static void Row(Transform parent, string name, Vector3 frontCenter, float yaw, float length, float depth, RowKind kind, System.Random rng)
        {
            var row = new GameObject($"Fila_{name}").transform;
            row.SetParent(parent, false);
            row.localPosition = frontCenter;
            row.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float half = length / 2f;

            if (kind == RowKind.Vacant)
            {
                VacantLot(row, rng, -half, half, depth);
                return;
            }
            if (kind == RowKind.Overgrown)
            {
                OvergrownLot(row, rng, -half, half, depth);
                return;
            }

            if (kind == RowKind.Apartments)
            {
                // Uno o dos bloques de 3 pisos, con un pasillo o un solar entre ellos.
                if (length < 32f) Apartment(row, rng, 0f, Mathf.Min(length - 3f, 24f), depth);
                else
                {
                    float w1 = Range(rng, 14f, 17f), w2 = length - w1 - Range(rng, 3f, 5f);
                    Apartment(row, rng, -half + w1 / 2f + 0.5f, w1, depth);
                    Apartment(row, rng, half - w2 / 2f - 0.5f, w2 - 1f, depth);
                }
                return;
            }

            // Casas: anchos y retranqueos distintos, pasillos entre ellas y algún solar vacío.
            float x = -half + Range(rng, 0.3f, 1.5f);
            while (x < half - 6f)
            {
                if (rng.NextDouble() < 0.12)
                {
                    float lotW = Mathf.Min(Range(rng, 7f, 11f), half - x);
                    // La mitad, solar vallado con trastos; la otra mitad, comido por la vegetación.
                    if (rng.NextDouble() < 0.5) OvergrownLot(row, rng, x, x + lotW, depth);
                    else VacantLot(row, rng, x, x + lotW, depth);
                    x += lotW + 0.5f;
                    continue;
                }
                float w = Range(rng, 6.5f, 8.5f);
                float gap = Range(rng, 1.4f, 3.4f);
                if (x + w > half - 0.3f) break;
                House(row, rng, x + w / 2f, w, x - 0.3f, Mathf.Min(x + w + gap - 0.3f, half), depth);
                x += w + gap;
            }
        }

        // ---------- Casa baja ----------

        static void House(Transform row, System.Random rng, float cx, float w, float lotX0, float lotX1, float depth)
        {
            var root = new GameObject("Casa").transform;
            root.SetParent(row, false);

            bool gable = rng.NextDouble() < 0.55;
            bool abandoned = rng.NextDouble() < 0.12;
            float s = Range(rng, 2.4f, 4.2f);
            bool garage = depth - s >= 14f && rng.NextDouble() < 0.7;
            float hd = Mathf.Clamp(Range(rng, 8f, 10.5f), 6f, Mathf.Max(6f, depth - s - (garage ? 6.8f : 1.5f)));
            int floors = gable ? 2 : (rng.NextDouble() < 0.3 ? 3 : 2);
            float h = floors * ProceduralTextures.FacadeFloor;
            var facade = gable ? houseFacades[rng.Next(houseFacades.Length)] : greystone;
            float back = s + hd;
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);

            root.name = gable ? "CasaLadrillo" : "CasaPiedraGris";
            if (abandoned) root.name += "_Tapiada";
            TexturedBox("Muros", root, new Vector3(cx, h / 2f, s + hd / 2f), new Vector3(w, h, hd), facade, facadeTile, sidesOnly: true);
            if (gable)
            {
                float rise = w * 0.42f;
                GableRoof(root, new Vector3(cx, h, s + hd / 2f), w, hd, rise, 0.45f, shingles, facade);
                // Ventana del desván en el hastial (el hastial es de ladrillo liso: la textura de ventanas salía cortada por el tejado).
                float atticY = h + Mathf.Min(1.1f, rise * 0.4f);
                Box("MarcoAtico", root, new Vector3(cx, atticY, s - 0.03f), new Vector3(0.95f, 1.15f, 0.06f), stoneTrim, collider: false);
                Box("VentanaAtico", root, new Vector3(cx, atticY, s - 0.07f), new Vector3(0.75f, 0.95f, 0.04f), abandoned ? plywood : darkGlass, collider: false);
                Box("AlfeizarAtico", root, new Vector3(cx, atticY - 0.62f, s - 0.08f), new Vector3(1.1f, 0.08f, 0.16f), stoneTrim, collider: false);
            }
            else
            {
                Box("Tejado", root, new Vector3(cx, h + 0.1f, s + hd / 2f), new Vector3(w - 0.1f, 0.2f, hd - 0.1f), tarRoof, collider: false);
                Box("Cornisa", root, new Vector3(cx, h + 0.35f, s + 0.2f), new Vector3(w + 0.4f, 0.55f, 0.7f), stoneTrim, collider: false);
                Box("Imposta", root, new Vector3(cx, ProceduralTextures.FacadeFloor - 0.1f, s - 0.04f), new Vector3(w + 0.1f, 0.2f, 0.12f), stoneTrim, collider: false);
            }

            // Escalinata y puerta: la piedra gris tiene la entrada más alta, con barandilla de hierro. La puerta va en el
            // vano de una esquina y su marco tapa entera la ventana de ese vano; por debajo de la imposta (2.8 m).
            float doorX = FacadeBayCenter(cx, w, cx + (rng.Next(2) == 0 ? -1f : 1f) * w / 2f, 0.9f);
            float landing = gable ? 0.45f : 0.6f;
            Box("Rellano", root, new Vector3(doorX, landing / 2f + 0.05f, s - 0.55f), new Vector3(1.8f, landing, 1.1f), concreteMat);
            int steps = Mathf.RoundToInt(landing / 0.15f) - 1;
            for (int k = 1; k <= steps; k++)
                Box("Escalon", root, new Vector3(doorX, (landing - 0.15f * k) / 2f + 0.05f, s - 1.1f - 0.3f * (k - 0.5f)), new Vector3(1.4f, landing - 0.15f * k, 0.3f), concreteMat);
            float stairsEnd = s - 1.1f - 0.3f * steps;
            if (!gable)
                foreach (float side in new[] { -0.66f, 0.66f })
                {
                    // Pasamanos sobre el borde de los escalones: tramo plano sobre el rellano y tramo inclinado, con postes.
                    float x = doorX + side, railTop = landing + 0.9f, footZ = stairsEnd + 0.15f;
                    Strut("Barandilla", root, new Vector3(x, railTop, s - 0.15f), new Vector3(x, railTop, s - 1.1f), 0.05f, ironMat, cylinder: true);
                    Strut("Barandilla", root, new Vector3(x, railTop, s - 1.1f), new Vector3(x, 0.95f, footZ), 0.05f, ironMat, cylinder: true);
                    Strut("PosteBarandilla", root, new Vector3(x, landing, s - 1.1f), new Vector3(x, railTop, s - 1.1f), 0.06f, ironMat, cylinder: true);
                    Strut("PosteBarandilla", root, new Vector3(x, LawnTop, footZ), new Vector3(x, 0.95f, footZ), 0.06f, ironMat, cylinder: true);
                }
            FacadeDoor(root, doorX, landing, s, -1f, BayWidth(w), 1.05f, 2.15f, abandoned ? plywood : doorMat, stoneTrim, gable ? 2.9f : 2.78f);
            if (stairsEnd > 0.3f)
                TexturedBox("Camino", root, new Vector3(doorX, 0.06f, stairsEnd / 2f), new Vector3(1.2f, 0.12f, stairsEnd), sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);

            if (abandoned) BoardUpWindows(root, cx, w, h, s, rng, doorX);

            // Valla baja de hierro en el jardín delantero, abierta en el camino.
            if (!abandoned && rng.NextDouble() < 0.55)
            {
                var tile = new Vector2(ProceduralTextures.IronFenceWidth, 1.1f);
                FenceSegment(root, new Vector3(lotX0 + 0.3f, LawnTop, 0.45f), new Vector3(doorX - 0.8f, LawnTop, 0.45f), 1.1f, ironFence, tile, ironMat, 2.5f, 0.05f, topRail: false);
                FenceSegment(root, new Vector3(doorX + 0.8f, LawnTop, 0.45f), new Vector3(lotX1 - 0.3f, LawnTop, 0.45f), 1.1f, ironFence, tile, ironMat, 2.5f, 0.05f, topRail: false);
            }
            if (rng.NextDouble() < 0.3) DistrictTree(root, new Vector3(lotX0 + 0.9f, LawnTop, 1.3f), LoadMat("Corteza"), rng, Range(rng, 0.9f, 1.2f), rng);

            // Patio trasero: valla de madera hacia el callejón (y entre parcelas), garaje y cubos.
            float fenceZ = depth - 0.25f;
            float gw = 5.6f, gx = Mathf.Clamp(cx + Range(rng, -0.8f, 0.8f), lotX0 + gw / 2f + 0.2f, lotX1 - gw / 2f - 0.2f);
            bool garageFits = garage && lotX1 - lotX0 >= gw + 0.4f;
            if (garageFits)
            {
                Garage(root, new Vector3(gx, 0f, depth - 3.25f), gw, rng);
                WoodFenceLine(root, new Vector3(lotX0, LawnTop, fenceZ), new Vector3(gx - gw / 2f, LawnTop, fenceZ));
                WoodFenceLine(root, new Vector3(gx + gw / 2f, LawnTop, fenceZ), new Vector3(lotX1, LawnTop, fenceZ));
            }
            else
            {
                // Puerta de la valla: un hueco de 1 m.
                float gateX = Range(rng, lotX0 + 1f, lotX1 - 2f);
                WoodFenceLine(root, new Vector3(lotX0, LawnTop, fenceZ), new Vector3(gateX, LawnTop, fenceZ));
                WoodFenceLine(root, new Vector3(gateX + 1f, LawnTop, fenceZ), new Vector3(lotX1, LawnTop, fenceZ));
            }
            if (rng.NextDouble() < 0.75) WoodFenceLine(root, new Vector3(lotX1, LawnTop, back + 0.3f), new Vector3(lotX1, LawnTop, fenceZ));

            // Cubos de basura en el borde del callejón.
            int bins = 1 + rng.Next(3);
            for (int i = 0; i < bins; i++)
            {
                float bx = garageFits ? gx + gw / 2f + 0.5f + i * 0.75f : cx - 1f + i * 0.75f;
                if (bx > lotX1 - 0.3f) bx = lotX0 + 0.5f + i * 0.75f;
                TrashCart(root, new Vector3(bx, AlleyTop, depth + 0.5f), 180f + Range(rng, -15f, 15f), binColors[rng.Next(binColors.Length)], rng.NextDouble() < 0.12);
            }

            // Patio: árbol, maleza junto a la valla y algún trasto.
            float yard = fenceZ - (garageFits ? 6.5f : 0f) - back;
            if (yard > 3f && rng.NextDouble() < 0.4)
                DistrictTree(root, new Vector3(Range(rng, lotX0 + 1.2f, lotX1 - 1.2f), LawnTop, back + yard / 2f), LoadMat("Corteza"), rng, Range(rng, 0.8f, 1.1f), rng);
            int weedsCount = abandoned ? 10 : 2 + rng.Next(3);
            for (int i = 0; i < weedsCount; i++)
                Clump(root, rng, new Vector3(Range(rng, lotX0 + 0.3f, lotX1 - 0.3f), LawnTop, abandoned ? Range(rng, 0.5f, s - 0.3f) : fenceZ - Range(rng, 0.3f, 0.8f)),
                    abandoned ? 0.5f : 0.3f, abandoned ? 1.3f : 0.8f);

            RecordFootprint(row, "casa", cx - w / 2f, s, cx + w / 2f, back, h + (gable ? w * 0.42f : 0.5f));
            if (garageFits) RecordFootprint(row, "garaje", gx - gw / 2f, depth - 6.25f, gx + gw / 2f, depth - 0.25f, 2.8f);
        }

        /// <summary>
        /// Tablones de contrachapado sobre las ventanas de la fachada (casa abandonada). En la planta baja se salta el vano
        /// de <paramref name="doorX"/>, cuya ventana ya tapa la puerta.
        /// </summary>
        static void BoardUpWindows(Transform root, float cx, float w, float h, float s, System.Random rng, float doorX = float.NaN)
        {
            int bays = Mathf.Max(1, Mathf.RoundToInt(w / ProceduralTextures.FacadeBay));
            int floors = Mathf.Max(1, Mathf.RoundToInt(h / ProceduralTextures.FacadeFloor));
            float bay = w / bays, floor = h / floors;
            for (int f = 0; f < floors; f++)
            for (int i = 0; i < bays; i++)
            {
                if (rng.NextDouble() < 0.2) continue; // alguna ventana rota sin tapar
                if (f == 0 && Mathf.Abs(cx - w / 2f + (i + 0.5f) * bay - doorX) < 0.2f) continue;
                var board = Box("Tablon", root, new Vector3(cx - w / 2f + (i + 0.5f) * bay, f * floor + floor * 0.53f, s - 0.04f),
                    new Vector3(bay * 0.45f, floor * 0.52f, 0.05f), plywood, collider: false);
                board.transform.localRotation = Quaternion.Euler(0f, 0f, Range(rng, -4f, 4f));
            }
        }

        /// <summary>Garaje de una plaza con la puerta al callejón (+Z local) y tejado plano o a dos aguas.</summary>
        static void Garage(Transform parent, Vector3 center, float width, System.Random rng)
        {
            var root = new GameObject("Garaje").transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            var wall = garageWalls[rng.Next(garageWalls.Length)];
            Box("Muros", root, new Vector3(0f, 1.4f, 0f), new Vector3(width, 2.8f, 6f), wall);
            Box("Puerta", root, new Vector3(0f, 1.15f, 3.03f), new Vector3(width - 1.2f, 2.2f, 0.08f), garageDoor, collider: false);
            if (rng.NextDouble() < 0.5)
                GableRoof(root, new Vector3(0f, 2.8f, 0f), width, 6f, 1.2f, 0.3f, shingles, wall, ridgeAlongX: true);
            else
                Box("Tejado", root, new Vector3(0f, 2.9f, 0f), new Vector3(width + 0.3f, 0.2f, 6.3f), tarRoof, collider: false);
            if (rng.NextDouble() < 0.4 && graffitiMats[0])
                WallGraffiti(root, new Vector3(Range(rng, -0.8f, 0.8f), 1.3f, 3.08f), Vector3.forward, Range(rng, 2f, 3f), graffitiMats[rng.Next(graffitiMats.Length)]);
        }

        static void WoodFenceLine(Transform parent, Vector3 a, Vector3 b) =>
            FenceSegment(parent, a, b, ProceduralTextures.WoodFenceHeight, woodFence,
                new Vector2(ProceduralTextures.WoodFenceWidth, ProceduralTextures.WoodFenceHeight), porchWood, 2.4f, 0.1f, topRail: false);

        /// <summary>Cubo de basura con ruedas (azul o negro), con la tapa hacia −Z local.</summary>
        static void TrashCart(Transform parent, Vector3 ground, float yaw, Material body, bool tipped)
        {
            var root = new GameObject("CuboRuedas").transform;
            root.SetParent(parent, false);
            root.localPosition = ground + (tipped ? Vector3.up * 0.35f : Vector3.zero);
            root.localRotation = Quaternion.Euler(tipped ? 90f : 0f, yaw, 0f);
            Box("Cuerpo", root, new Vector3(0f, 0.53f, 0f), new Vector3(0.62f, 0.95f, 0.72f), body);
            Box("Tapa", root, new Vector3(0f, 1.03f, -0.02f), new Vector3(0.66f, 0.06f, 0.78f), body, collider: false);
            Primitive(PrimitiveType.Cylinder, "Ruedas", root, new Vector3(0f, 0.1f, 0.3f), new Vector3(0.2f, 0.36f, 0.2f), carPaints[1], keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // ---------- Apartamentos de 3 pisos ----------

        static void Apartment(Transform row, System.Random rng, float cx, float w, float depth)
        {
            var root = new GameObject("Apartamentos3Pisos").transform;
            root.SetParent(row, false);
            const float h = 9f;                  // 3 pisos de la textura de fachada
            const float top = h + AptParapetHeight; // con el antepecho, para que quepa la puerta trasera del 3.er piso
            float s = Range(rng, 3f, 4.5f);
            float d = Mathf.Min(13f, depth - s - 6.5f); // detrás: porche con escalera (4.2 m) y patio
            float back = s + d;
            var facade = rng.NextDouble() < 0.6 ? aptFacadeA : aptFacadeB;
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);

            TexturedBox("Muros", root, new Vector3(cx, h / 2f, s + d / 2f), new Vector3(w, h, d), facade, facadeTile, sidesOnly: true);
            // Antepecho de ladrillo liso sobre el último piso: sin él, la puerta trasera del 3.er piso asomaba por encima del muro.
            TexturedBox("Antepecho", root, new Vector3(cx, h + AptParapetHeight / 2f, s + d / 2f), new Vector3(w, AptParapetHeight, d),
                plainBrick[facade], new Vector2(ProceduralTextures.FacadeBay, AptParapetHeight), sidesOnly: true);
            Box("Tejado", root, new Vector3(cx, top + 0.05f, s + d / 2f), new Vector3(w - 0.1f, 0.1f, d - 0.1f), tarRoof, collider: false);
            Box("Pretil", root, new Vector3(cx, top + 0.3f, s + 0.15f), new Vector3(w + 0.3f, 0.6f, 0.5f), stoneTrim, collider: false);
            Box("Zocalo", root, new Vector3(cx, 0.4f, s + d / 2f), new Vector3(w + 0.12f, 0.8f, d + 0.12f), stoneTrim);

            // Miradores (ventanas salientes de arriba abajo), como en apartaments2.jpg. Cada uno tapa un vano entero de
            // la textura; si no, asomaban medias ventanas a sus lados.
            var bayWindows = new List<float>();
            foreach (float k in w >= 16f ? new[] { -0.3f, 0.3f } : new[] { 0.28f })
            {
                float bx = FacadeBayCenter(cx, w, cx + k * w, 1.6f);
                bayWindows.Add(bx);
                TexturedBox("Mirador", root, new Vector3(bx, h / 2f + 0.4f, s - 0.45f), new Vector3(3f, h - 0.8f, 0.9f), facade, facadeTile);
                Box("CornisaMirador", root, new Vector3(bx, h - 0.1f, s - 0.5f), new Vector3(3.4f, 0.3f, 1.1f), stoneTrim, collider: false);
            }
            // Ventanas de bloque de vidrio del sótano.
            int bays = Mathf.Max(1, Mathf.RoundToInt(w / ProceduralTextures.FacadeBay));
            for (int i = 0; i < bays; i++)
                Box("BloqueVidrio", root, new Vector3(cx - w / 2f + (i + 0.5f) * (w / bays), 0.42f, s - 0.07f), new Vector3(0.9f, 0.45f, 0.05f), glassBlock, collider: false);

            // Portal de piedra con escalones, centrado en el vano más cercano al centro que no choque con un mirador: la
            // puerta tapa la ventana de ese vano y las pilastras caen entre ventanas. El dintel queda por debajo del
            // alféizar del primer piso (3.75 m).
            float portalX = cx, portalDist = float.MaxValue, bayW = BayWidth(w), wallX0 = cx - w / 2f;
            for (int bay = 0; bay < bays; bay++)
            {
                float c = wallX0 + (bay + 0.5f) * bayW;
                bool clear = c - 1.6f >= wallX0 && c + 1.6f <= wallX0 + w;
                foreach (float bx in bayWindows) clear &= Mathf.Abs(c - bx) >= 3.3f;
                if (clear && Mathf.Abs(c - cx) < portalDist) { portalDist = Mathf.Abs(c - cx); portalX = c; }
            }
            foreach (float px in new[] { -1.2f, 1.2f })
                Box("Pilastra", root, new Vector3(portalX + px, 1.55f, s - 0.25f), new Vector3(0.5f, 3.1f, 0.5f), stoneTrim);
            Box("Dintel", root, new Vector3(portalX, 3.3f, s - 0.3f), new Vector3(3f, 0.45f, 0.6f), stoneTrim, collider: false);
            Box("Puerta", root, new Vector3(portalX, 1.6f, s - 0.04f), new Vector3(1.7f, 2.6f, 0.08f), darkGlass, collider: false);
            Box("Escalon", root, new Vector3(portalX, 0.15f, s - 0.6f), new Vector3(2.4f, 0.3f, 1.2f), concreteMat);
            TexturedBox("Camino", root, new Vector3(portalX, 0.06f, (s - 1.2f) / 2f), new Vector3(1.8f, 0.12f, s - 1.2f), sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);

            // Valla baja de hierro delante, con hueco en el portal.
            var tile = new Vector2(ProceduralTextures.IronFenceWidth, 1.1f);
            FenceSegment(root, new Vector3(cx - w / 2f, LawnTop, 0.5f), new Vector3(portalX - 1.1f, LawnTop, 0.5f), 1.1f, ironFence, tile, ironMat, 2.5f, 0.05f, topRail: false);
            FenceSegment(root, new Vector3(portalX + 1.1f, LawnTop, 0.5f), new Vector3(cx + w / 2f, LawnTop, 0.5f), 1.1f, ironFence, tile, ironMat, 2.5f, 0.05f, topRail: false);

            // Porches traseros de madera con escalera en zigzag (apartaments.jfif). Cada porche se coloca para que su
            // puerta caiga justo en el centro de un vano de la textura de fachada: así la puerta tapa la ventana de ese
            // vano en cada piso y la escalera nunca da a una ventana.
            int facadeBays = bays;
            foreach (float k in w >= 18f ? new[] { -0.25f, 0.25f } : new[] { 0f })
            {
                // Vanos cuya puerta deja el porche entero dentro de la fachada; el más cercano al sitio buscado.
                float want = cx + k * w + PorchDoorX, best = float.MaxValue, doorAt = want;
                for (int bay = 0; bay < facadeBays; bay++)
                {
                    float c = wallX0 + (bay + 0.5f) * bayW, porchX = c - PorchDoorX;
                    if (porchX - PorchHalfWidth < wallX0 - 0.01f || porchX + PorchHalfWidth > wallX0 + w + 0.01f) continue;
                    if (Mathf.Abs(c - want) < best) { best = Mathf.Abs(c - want); doorAt = c; }
                }
                BackPorch(root, new Vector3(doorAt - PorchDoorX, 0f, back), rng);
            }

            // Patio trasero de hormigón detrás de la escalera: contenedor, algún coche y cubos junto al callejón.
            float yardStart = back + PorchDepth + 0.2f, yard = depth - yardStart;
            if (yard > 1.5f)
            {
                Box("Patio", root, new Vector3(cx, 0.11f, yardStart + yard / 2f), new Vector3(w, 0.04f, yard), concreteMat, collider: false);
                Dumpster(root, new Vector3(cx - w / 2f + 1.6f, 0.13f, depth - 0.8f), 0f, lidOpen: rng.NextDouble() < 0.5);
                for (int i = 0; i < 2; i++) GarbageBag(root, new Vector3(cx - w / 2f + 3.2f + i * 0.5f, 0.13f, depth - 0.6f), rng);
                if (yard > 5f && rng.NextDouble() < 0.6)
                    ParkedCar(root, new Vector3(cx + Range(rng, 0f, w / 3f), 0.13f, yardStart + yard / 2f), 90f + Range(rng, -10f, 10f), rng);
            }
            for (int i = 0; i < 3; i++)
                TrashCart(root, new Vector3(cx + w / 2f - 1f - i * 0.8f, AlleyTop, depth + 0.5f), 180f, binColors[rng.Next(binColors.Length)], false);

            RecordFootprint(row, "apartamentos", cx - w / 2f, s - 0.9f, cx + w / 2f, back, top + 0.6f);
            RecordFootprint(row, "porche", cx - w / 2f, back, cx + w / 2f, back + PorchDepth, 10.4f);
        }

        // Porche trasero (marco local: la fachada en z = 0 y el porche hacia +Z). Tres terrazas de 5.4 × 2 m, una por piso
        // y cada una con su puerta trasera, y delante una torre de escalera en zigzag de dos bandas (A: z 2.0–3.1,
        // B: z 3.1–4.2). Cada tramo sube medio piso (1.5 m en 2.4 m, 32°) entre dos rellanos (x −2.2…−1.2 y 1.2…2.2);
        // el rellano derecho a la altura de cada terraza es la entrada a ella. Sobre cada tramo quedan ~3 m libres, así
        // que se puede subir a pie: se pisan rampas invisibles bajo los peldaños, y los lados de los tramos y las
        // barandillas tienen colisión.
        const float PorchHalfWidth = 2.7f, PorchDeckDepth = 2f, PorchDepth = 4.2f, PorchBandSplit = 3.1f;
        const float PorchStairX = 1.2f, PorchLandingX = 2.2f; // tramos en |x| < 1.2; rellanos de 1.2 a 2.2
        const float AptParapetHeight = 0.75f;                   // antepecho de los apartamentos (64 px de PlainBrick)
        const float PorchDoorX = -0.6f;                         // puerta de cada piso, en el centro de un vano de la fachada
        static readonly float[] PorchDeckTops = { 1.07f, 4.07f, 7.07f }; // los pisos de la textura de fachada son de 3 m

        /// <summary>Porche trasero de 3 alturas con escalera transitable y una puerta por piso, pegado a la fachada en +Z local.</summary>
        static void BackPorch(Transform parent, Vector3 basePoint, System.Random rng)
        {
            var root = new GameObject("PorcheTrasero").transform;
            root.SetParent(parent, false);
            root.localPosition = basePoint;
            const float hw = PorchHalfWidth, dd = PorchDeckDepth, pd = PorchDepth, railH = 1f;
            float bandA = (dd + PorchBandSplit) / 2f, bandB = (PorchBandSplit + pd) / 2f, bandW = PorchBandSplit - dd;

            // Terrazas: puerta (tapa la ventana de ese vano de la textura) y barandilla, abierta solo hacia el rellano derecho.
            foreach (float top in PorchDeckTops)
            {
                Box("Suelo", root, new Vector3(0f, top - 0.07f, (0.07f + dd) / 2f), new Vector3(2f * hw, 0.14f, dd - 0.07f), porchWood);
                Box("PuertaTrasera", root, new Vector3(PorchDoorX, top + 0.9f, 0.05f), new Vector3(1.05f, 2.5f, 0.06f), backDoor, collider: false);
                Box("MarcoPuerta", root, new Vector3(PorchDoorX, top + 2.2f, 0.07f), new Vector3(1.4f, 0.14f, 0.08f), porchWood, collider: false);
                foreach (float side in new[] { -1f, 1f })
                    Box("MarcoPuerta", root, new Vector3(PorchDoorX + side * 0.62f, top + 1.05f, 0.07f), new Vector3(0.14f, 2.3f, 0.08f), porchWood, collider: false);
                Box("Pomo", root, new Vector3(PorchDoorX + 0.38f, top + 1f, 0.1f), new Vector3(0.06f, 0.06f, 0.06f), ironMat, collider: false);
                PorchRail(root, new Vector3(-hw, top, dd), new Vector3(PorchStairX, top, dd), railH);
                PorchRail(root, new Vector3(PorchLandingX, top, dd), new Vector3(hw, top, dd), railH);
                PorchRail(root, new Vector3(-hw, top, 0.07f), new Vector3(-hw, top, dd), railH);
                PorchRail(root, new Vector3(hw, top, 0.07f), new Vector3(hw, top, dd), railH);
            }

            // Alturas de los rellanos: suelo → terraza 1 → medio piso → terraza 2 → medio piso → terraza 3.
            var heights = new[]
            {
                LawnTop, PorchDeckTops[0], (PorchDeckTops[0] + PorchDeckTops[1]) / 2f,
                PorchDeckTops[1], (PorchDeckTops[1] + PorchDeckTops[2]) / 2f, PorchDeckTops[2],
            };
            for (int i = 0; i + 1 < heights.Length; i++)
            {
                bool rightward = i % 2 == 0; // los pares suben hacia +X por la banda A; los impares vuelven por la B
                float z = rightward ? bandA : bandB;
                float x0 = rightward ? -PorchStairX : PorchStairX;
                PorchFlight(root, new Vector3(x0, heights[i], z), new Vector3(-x0, heights[i + 1], z), bandW);

                // Rellano al final del tramo, ocupando las dos bandas.
                float h = heights[i + 1], side = rightward ? 1f : -1f;
                float innerX = side * PorchStairX, outerX = side * PorchLandingX;
                Box("Rellano", root, new Vector3((innerX + outerX) / 2f, h - 0.07f, (dd + pd) / 2f), new Vector3(PorchLandingX - PorchStairX, 0.14f, pd - dd), porchWood);
                PorchRail(root, new Vector3(innerX, h, pd), new Vector3(outerX, h, pd), railH);
                PorchRail(root, new Vector3(outerX, h, dd), new Vector3(outerX, h, pd), railH);
                if (!rightward) PorchRail(root, new Vector3(outerX, h, dd), new Vector3(innerX, h, dd), railH); // rellano de medio piso
                // En el último rellano no sale ningún tramo por la banda B: se cierra el hueco que da a la escalera de abajo.
                if (i + 2 == heights.Length) PorchRail(root, new Vector3(innerX, h, PorchBandSplit), new Vector3(innerX, h, pd), railH);
            }
            // Por debajo del primer tramo de vuelta no se pasa (hay poca altura).
            ColliderBox(root, "LimiteBajoEscalera", new Vector3(-PorchStairX, 1f, bandB), new Vector3(0.1f, 2f, bandW));

            // Postes, tejadillo y algún trasto.
            Box("Techo", root, new Vector3(0f, 10.2f, pd / 2f + 0.1f), new Vector3(2f * hw + 0.3f, 0.12f, pd + 0.5f), porchWood, collider: false);
            foreach (var (x, z) in new[] { (-hw, 0.12f), (hw, 0.12f), (-hw, dd), (hw, dd), (-PorchLandingX, pd), (PorchLandingX, pd) })
                Box("Poste", root, new Vector3(x, 5.1f, z), new Vector3(0.14f, 10.2f, 0.14f), porchWood);
            if (rng.NextDouble() < 0.5)
                Box("Silla", root, new Vector3(Range(rng, 0.4f, 2.2f), PorchDeckTops[1] + 0.3f, 1f), new Vector3(0.5f, 0.6f, 0.5f), garageWalls[rng.Next(garageWalls.Length)], collider: false);
            if (rng.NextDouble() < 0.4)
                GarbageBag(root, new Vector3(Range(rng, -2.3f, -1.4f), PorchDeckTops[0], Range(rng, 0.5f, 1.5f)), rng);
        }

        /// <summary>
        /// Tramo de escalera de madera de <paramref name="from"/> a <paramref name="to"/> (a lo largo de X), con peldaños
        /// abiertos, zancas, pasamanos y balaustres. Se pisa una rampa invisible que pasa por las aristas de los
        /// peldaños, y los dos lados llevan pared invisible.
        /// </summary>
        static void PorchFlight(Transform root, Vector3 from, Vector3 to, float width)
        {
            var along = (to - from).normalized;
            var across = Vector3.Cross(Vector3.up, along).normalized;
            var normal = Vector3.Cross(along, across).normalized;
            if (normal.y < 0f) normal = -normal;
            var tilt = Quaternion.LookRotation(along, normal);
            var mid = (from + to) / 2f;
            float length = Vector3.Distance(from, to), rise = to.y - from.y, run = Mathf.Abs(to.x - from.x);

            ColliderBox(root, "RampaEscalera", mid - normal * 0.15f, new Vector3(width, 0.3f, length + 0.02f)).transform.localRotation = tilt;

            // Peldaños: el k sube hasta from.y + (k + 1)·subida; el último es el propio rellano.
            int steps = Mathf.Max(3, Mathf.RoundToInt(rise / 0.19f));
            float stepRise = rise / steps, stepRun = run / steps, dir = Mathf.Sign(to.x - from.x);
            for (int k = 0; k < steps - 1; k++)
            {
                float x = from.x + dir * (k + 0.5f) * stepRun, y = from.y + (k + 1) * stepRise;
                Box("Peldano", root, new Vector3(x, y - 0.025f, from.z), new Vector3(stepRun + 0.04f, 0.05f, width - 0.12f), porchWood, collider: false);
            }

            foreach (float side in new[] { -1f, 1f })
            {
                var offset = new Vector3(0f, 0f, side * (width / 2f - 0.03f));
                Box("Zanca", root, mid + offset - normal * 0.12f, new Vector3(0.06f, 0.28f, length), porchWood, collider: false).transform.localRotation = tilt;
                Strut("Pasamanos", root, from + offset + Vector3.up * 0.95f, to + offset + Vector3.up * 0.95f, 0.06f, porchWood);
                StairSideWall(root, from + offset, to + offset, 1.4f);
                for (float t = 0.12f; t < 0.95f; t += 0.16f)
                {
                    var p = Vector3.Lerp(from, to, t) + offset;
                    Box("Balaustre", root, p + Vector3.up * 0.48f, new Vector3(0.04f, 0.94f, 0.04f), porchWood, collider: false);
                }
            }
        }

        /// <summary>Barandilla de balaustres (textura con alfa) de a a b, de <paramref name="height"/> de alto desde a.y, con colisión.</summary>
        static void PorchRail(Transform root, Vector3 a, Vector3 b, float height)
        {
            var d = b - a;
            float length = d.magnitude;
            if (length < 0.05f) return;
            var rail = TexturedBox("Baranda", root, (a + b) / 2f + Vector3.up * (height / 2f), new Vector3(length, height, 0.04f), porchRail, new Vector2(1f, height), sidesOnly: true);
            rail.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg, 0f);
        }

        /// <summary>Losa (tramo de escalera, tablón...) de a a b con ancho y grosor dados.</summary>
        static void Slab(Transform parent, Vector3 a, Vector3 b, float width, float thickness, Material material)
        {
            var go = Box("Tramo", parent, (a + b) / 2f, new Vector3(width, thickness, Vector3.Distance(a, b)), material, collider: false);
            go.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
        }

        // ---------- Solar vacío ----------

        static void VacantLot(Transform row, System.Random rng, float x0, float x1, float depth)
        {
            var root = new GameObject("SolarVacio").transform;
            root.SetParent(row, false);
            float w = x1 - x0;

            // Valla de malla delante, a veces con un tramo caído.
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            if (chainLink)
            {
                float gapAt = rng.NextDouble() < 0.6 ? Range(rng, x0 + 1f, x1 - 3f) : x1;
                FenceSegment(root, new Vector3(x0 + 0.3f, LawnTop, 0.5f), new Vector3(gapAt, LawnTop, 0.5f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                if (gapAt < x1) FenceSegment(root, new Vector3(gapAt + 2.2f, LawnTop, 0.5f), new Vector3(x1 - 0.3f, LawnTop, 0.5f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            }
            int clumps = Mathf.RoundToInt(w * depth * 0.18f);
            for (int i = 0; i < clumps; i++)
                Clump(root, rng, new Vector3(Range(rng, x0 + 0.4f, x1 - 0.4f), LawnTop, Range(rng, 0.9f, depth - 0.4f)), 0.4f, 1.4f);
            if (rng.NextDouble() < 0.5)
                Box("Colchon", root, new Vector3(Range(rng, x0 + 2f, x1 - 2f), LawnTop + 0.1f, Range(rng, 2f, depth - 2f)), new Vector3(1.4f, 0.2f, 1.9f), Mat("Colchon", new Color(0.66f, 0.62f, 0.52f)), collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), Range(rng, -3f, 3f));
            for (int i = 0; i < 3; i++)
                Primitive(PrimitiveType.Cylinder, "Neumatico", root, new Vector3(Range(rng, x0 + 1f, x1 - 1f), LawnTop + 0.1f, Range(rng, 2f, depth - 1f)),
                    new Vector3(0.7f, 0.1f, 0.7f), Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f)), keepCollider: false);
            for (int i = 0; i < 3; i++)
                SnowPatch(root, new Vector3(Range(rng, x0 + 1f, x1 - 1f), LawnTop, Range(rng, 1.5f, depth - 1.5f)), Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 3f), Range(rng, 1f, 2f)));
            RecordFootprint(row, "solar", x0, 0f, x1, depth, 0f);
        }

        /// <summary>
        /// Solar abandonado comido por la vegetación (img/estructures/abandonedParcelVegetacion.png): abierto a la acera,
        /// hierba alta por todo (algo más baja junto a la acera), árboles grandes y matorral al fondo, algún árbol seco,
        /// los restos del camino de hormigón de la casa que hubo, un atajo de barro en diagonal y un poste de cartel
        /// torcido. Marco de la fila: acera en z = 0 y la parcela hacia +Z.
        /// </summary>
        static void OvergrownLot(Transform row, System.Random rng, float x0, float x1, float depth)
        {
            var root = new GameObject("SolarVegetacion").transform;
            root.SetParent(row, false);
            float w = x1 - x0;
            var bark = LoadMat("Corteza");
            var bushes = new[] { LoadMat("Arbusto"), LoadMat("ArbustoSeco") };

            // Hierba alta muy densa; los primeros 2.5 m, más baja.
            int clumps = Mathf.RoundToInt(w * depth * 0.3f);
            for (int i = 0; i < clumps; i++)
            {
                float z = Range(rng, 0.5f, depth - 0.3f);
                bool front = z < 2.5f;
                Clump(root, rng, new Vector3(Range(rng, x0 + 0.3f, x1 - 0.3f), LawnTop, z), front ? 0.3f : 0.5f, front ? 0.8f : 1.5f);
            }

            // Árboles grandes en la mitad del fondo (uno cada ~6 m de frente) y un árbol seco.
            int trees = Mathf.Clamp(Mathf.RoundToInt(w / 6f), 1, 4);
            for (int i = 0; i < trees; i++)
            {
                float tx = x0 + (i + 0.5f) * w / trees + Range(rng, -1f, 1f);
                DistrictTree(root, new Vector3(Mathf.Clamp(tx, x0 + 1.5f, x1 - 1.5f), LawnTop, Range(rng, depth * 0.55f, depth - 1.2f)), bark, rng, Range(rng, 1.1f, 1.5f), rng);
            }
            if (rng.NextDouble() < 0.7)
                BareTree(root, new Vector3(Range(rng, x0 + 1.5f, x1 - 1.5f), LawnTop, Range(rng, depth * 0.3f, depth * 0.6f)), bark, rng, Range(rng, 0.8f, 1.1f));

            // Matorral a lo largo del fondo.
            for (float x = x0 + 0.8f; x < x1 - 0.8f; x += Range(rng, 1.2f, 2.4f))
            {
                if (rng.NextDouble() < 0.3) continue;
                float s = Range(rng, 1f, 2f);
                var material = bushes[rng.NextDouble() < 0.6 ? 0 : 1];
                if (material)
                    Primitive(PrimitiveType.Sphere, "Matorral", root, new Vector3(x + Range(rng, -0.3f, 0.3f), LawnTop + s * 0.3f, depth - Range(rng, 0.6f, 1.6f)),
                        new Vector3(s, s * 0.75f, s * 0.9f), material, keepCollider: false);
            }

            // Restos del camino de hormigón de la casa que hubo: losas rotas y alguna que falta.
            float walkX = Range(rng, x0 + 2f, x1 - 2f), walkEnd = Range(rng, 3f, 5f);
            for (float z = 0.1f; z < walkEnd; z += 1.05f)
            {
                if (rng.NextDouble() < 0.2) continue;
                Box("CaminoRoto", root, new Vector3(walkX + Range(rng, -0.05f, 0.05f), LawnTop + 0.01f, z + 0.5f), new Vector3(1.1f, 0.03f, 0.95f), concreteMat, collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, Range(rng, -4f, 4f), 0f);
            }

            // Atajo de barro en diagonal: la gente cruza el solar.
            var mud = LoadMat("Barro");
            if (mud && w > 8f)
            {
                bool flip = rng.NextDouble() < 0.5;
                DesirePath(root, new Vector3(flip ? x1 - 0.5f : x0 + 0.5f, 0f, 0.6f), new Vector3(flip ? x0 + 1f : x1 - 1f, 0f, depth - 1f), mud, rng);
            }

            // Poste de cartel torcido junto a la acera, basura y nieve.
            float postX = rng.NextDouble() < 0.5 ? x0 + 0.6f : x1 - 0.6f;
            Strut("PosteCartel", root, new Vector3(postX, LawnTop, 0.5f), new Vector3(postX + Range(rng, -0.25f, 0.25f), 2.4f, 0.5f + Range(rng, -0.15f, 0.15f)), 0.06f, ironMat, cylinder: true);
            for (int i = 0; i < 1 + rng.Next(3); i++)
                GarbageBag(root, new Vector3(Range(rng, x0 + 1f, x1 - 1f), LawnTop, Range(rng, 1f, depth - 1f)), rng);
            for (int i = 0; i < 2; i++)
                SnowPatch(root, new Vector3(Range(rng, x0 + 1f, x1 - 1f), LawnTop, Range(rng, depth * 0.5f, depth - 1f)), Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 3f), Range(rng, 1f, 2f)));
            RecordFootprint(row, "solar", x0, 0f, x1, depth, 0f);
        }

        // ---------- Tienda de esquina ----------

        static void CornerStore(Transform parent, System.Random rng)
        {
            var root = new GameObject("TiendaEsquina_Casas").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(182.5f, 0f, 190f);
            const float w = 12f, d = 11f, h = 4.6f;
            var facade = houseFacades[2];
            TexturedBox("Muros", root, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), facade, new Vector2(ProceduralTextures.FacadeBay, h), sidesOnly: true);
            Box("Tejado", root, new Vector3(0f, h + 0.05f, 0f), new Vector3(w, 0.1f, d), tarRoof, collider: false);
            Box("Pretil", root, new Vector3(0f, h + 0.4f, -d / 2f + 0.15f), new Vector3(w + 0.2f, 0.8f, 0.3f), stoneTrim, collider: false);
            // Escaparate con rejas hacia C2 (−Z) y letrero sin texto, sobre un frente pintado que tapa las ventanas de la
            // textura de ladrillo hasta el letrero (si no, asomaban trozos de ventana entre el escaparate y el letrero).
            Box("Frente", root, new Vector3(0f, 1.6f, -d / 2f - 0.02f), new Vector3(w, 3.2f, 0.04f), Mat("FrenteTienda", new Color(0.15f, 0.3f, 0.18f)), collider: false);
            Box("Escaparate", root, new Vector3(-1f, 1.5f, -d / 2f - 0.05f), new Vector3(8f, 2.2f, 0.04f), darkGlass, collider: false);
            for (float x = -4.8f; x <= 2.8f; x += 0.4f)
                Box("Reja", root, new Vector3(x, 1.5f, -d / 2f - 0.1f), new Vector3(0.04f, 2.2f, 0.04f), ironMat, collider: false);
            Box("Puerta", root, new Vector3(4.2f, 1.2f, -d / 2f - 0.05f), new Vector3(1.2f, 2.3f, 0.04f), darkGlass, collider: false);
            Box("Letrero", root, new Vector3(0f, 3.55f, -d / 2f - 0.15f), new Vector3(w - 1f, 0.9f, 0.2f), Mat("LetreroRojo", new Color(0.6f, 0.1f, 0.08f)), collider: false);
            if (graffitiMats[1]) WallGraffiti(root, new Vector3(-w / 2f, 1.6f, 1f), Vector3.left, 3.2f, graffitiMats[1]);
            TrashCan(root, new Vector3(5.6f, SidewalkTop - 0.0f, -d / 2f - 1.3f), 0f, false);
            for (int i = 0; i < 2; i++) GarbageBag(root, new Vector3(-w / 2f + 0.6f, LawnTop, 3f + i * 0.6f), rng);
            RecordFootprint(root, "tienda", -w / 2f, -d / 2f, w / 2f, d / 2f, h + 0.8f);
        }

        // ---------- Calle: farolas, postes con cables, coches, finales sin salida ----------

        static void BuildHousesStreetLife(Transform parent, System.Random rng, bool west)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);

            // Farolas de sodio.
            foreach (float x in new[] { 135f, 170f, 200f }) StreetLight(group, new Vector3(x, 0f, 128.6f), Vector3.back, pole, lamp);
            foreach (float z in new[] { 104f, 145f, 190f }) StreetLight(group, new Vector3(205.4f, 0f, z), Vector3.right, pole, lamp);
            foreach (float x in new[] { 132f, 156f }) StreetLight(group, new Vector3(x, 0f, 181.6f), Vector3.forward, pole, lamp);
            StreetLight(group, new Vector3(192f, 0f, 182.6f), Vector3.back, pole, lamp);

            // Postes de madera con cables a lo largo de los callejones (como en backparts.png).
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            PoleLine(group, Points(new Vector3(124f, AlleyTop, 96.6f), new Vector3(238f, AlleyTop, 96.6f), 24f), wood, cable);
            PoleLine(group, Points(new Vector3(124f, AlleyTop, 151.6f), new Vector3(202f, AlleyTop, 151.6f), 26f), wood, cable);
            PoleLine(group, Points(new Vector3(124f, AlleyTop, 213.1f), new Vector3(244f, AlleyTop, 213.1f), 24f), wood, cable);
            PoleLine(group, Points(new Vector3(245.6f, AlleyTop, 100f), new Vector3(245.6f, AlleyTop, 208f), 27f), wood, cable);

            // Coches aparcados junto a la acera.
            ParkedCar(group, new Vector3(141f, RoadTop, 118.2f), 0f, rng);
            ParkedCar(group, new Vector3(181f, RoadTop, 125.8f), 180f, rng);
            ParkedCar(group, new Vector3(215.8f, RoadTop, 108f), -90f, rng); // morro al norte (lado este de B)
            ParkedCar(group, new Vector3(215.8f, RoadTop, 164f), -90f, rng);
            ParkedCar(group, new Vector3(136f, RoadTop, 191.8f), 180f, rng);
            ParkedCar(group, new Vector3(223f, AlleyTop, 128.5f), 0f, rng); // en el pasaje a6

            // Barrera de hormigón, pilonas y nieve apilada en el extremo de B que no sigue: el norte (local 201.4) sigue
            // como G en el este, y el sur (local 92.6, el norte en el mundo) sigue como B' hasta F en la copia oeste
            // (HousesNorth.cs). Así B es sin salida solo por el lado del barrio de viviendas y de los negocios.
            var bollard = Mat("PilonaRoja", new Color(0.55f, 0.12f, 0.1f));
            foreach (float z in new[] { west ? 201.4f : 92.6f })
            {
                Box("Barrera", group, new Vector3(212f, 0.42f, z), new Vector3(9.6f, 0.8f, 0.6f), concreteMat);
                for (float x = 208f; x <= 216f; x += 2f)
                    Primitive(PrimitiveType.Cylinder, "Pilona", group, new Vector3(x, 0.47f, z + (z < 150f ? 0.8f : -0.8f)), new Vector3(0.18f, 0.45f, 0.18f), bollard, keepCollider: true);
                for (int i = 0; i < 3; i++)
                    SnowMound(group, new Vector3(209f + i * 3f, RoadTop, z + (z < 150f ? 2f : -2f)), new Vector3(2.6f, 0.6f, 2f), Range(rng, 0f, 360f));
            }

            // Vallas de madera que cierran el barrio por el norte, el este y el lado del barrio de viviendas.
            // En la copia oeste la valla norte (local) es la que separa el callejón de los negocios: se abre donde el
            // callejón a5 sigue hasta la calle (PrototypeSceneBuilder.Shops.cs, ShopsAlleyLink).
            float fenceZ = HousesMaxZ - 0.2f;
            if (west)
            {
                WoodFenceLine(group, new Vector3(HousesMinX, LawnTop, fenceZ), new Vector3(240.5f, LawnTop, fenceZ));
                WoodFenceLine(group, new Vector3(246.5f, LawnTop, fenceZ), new Vector3(HousesMaxX, LawnTop, fenceZ));
            }
            // En el este, el borde norte ya no lleva valla: al otro lado del callejón a4 siguen las casas de la ampliación
            // norte (HousesNorth.cs), cuyas parcelas tienen su propia valla trasera.
            // En la copia oeste esta valla separa de la ampliación: se abre donde entran sus callejones (HousesWestExt.cs).
            float fenceZ0 = HousesMinZ;
            foreach (var (g0, g1) in west ? WestExtFenceGapsLocal : new (float, float)[0])
            {
                WoodFenceLine(group, new Vector3(HousesMaxX - 0.2f, LawnTop, fenceZ0), new Vector3(HousesMaxX - 0.2f, LawnTop, g0));
                fenceZ0 = g1;
            }
            WoodFenceLine(group, new Vector3(HousesMaxX - 0.2f, LawnTop, fenceZ0), new Vector3(HousesMaxX - 0.2f, LawnTop, HousesMaxZ));
            // Lado sur (local): en el este da al barrio de viviendas y tiene dos huecos de paso. En el oeste es el borde
            // norte del mundo y no lleva valla: al otro lado del callejón a1' siguen las casas de la ampliación norte.
            if (!west)
            {
                float x0 = HousesMinX + 2f;
                foreach (float gap in new[] { 150f, 186f })
                {
                    WoodFenceLine(group, new Vector3(x0, LawnTop, 91.2f), new Vector3(gap, LawnTop, 91.2f));
                    x0 = gap + 2.5f;
                }
                WoodFenceLine(group, new Vector3(x0, LawnTop, 91.2f), new Vector3(240f, LawnTop, 91.2f));
            }
        }

        static List<Vector3> Points(Vector3 a, Vector3 b, float spacing)
        {
            var list = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / spacing));
            for (int i = 0; i <= n; i++) list.Add(Vector3.Lerp(a, b, (float)i / n));
            return list;
        }

        /// <summary>Línea de postes de madera con cruceta y tres cables entre cada dos.</summary>
        static void PoleLine(Transform parent, List<Vector3> poles, Material wood, Material cable)
        {
            const float height = 9f, armY = 8.4f;
            var along = (poles[^1] - poles[0]).normalized;
            var across = Vector3.Cross(Vector3.up, along);
            foreach (var p in poles)
            {
                Primitive(PrimitiveType.Cylinder, "PosteMadera", parent, p + Vector3.up * (height / 2f), new Vector3(0.26f, height / 2f, 0.26f), wood, keepCollider: true);
                var arm = Box("Cruceta", parent, p + Vector3.up * armY, new Vector3(0.12f, 0.12f, 2f), wood, collider: false);
                arm.transform.localRotation = Quaternion.LookRotation(across, Vector3.up);
            }
            for (int i = 0; i + 1 < poles.Count; i++)
                foreach (float o in new[] { -0.85f, 0f, 0.85f })
                    Wire(parent, poles[i] + Vector3.up * (armY + 0.08f) + across * o, poles[i + 1] + Vector3.up * (armY + 0.08f) + across * o, 0.6f, cable);
        }

        // ---------- Nieve, maleza, basura y suciedad ----------

        static void BuildHousesWeather(Transform parent, System.Random rng, Rect[] roads, Rect[] alleys, Rect[] sidewalks, Rect[] lawns)
        {
            var group = new GameObject("NieveMalezaBasura").transform;
            group.SetParent(parent, false);
            var grime = new[] { LoadMat("Barro"), LoadMat("Charco"), LoadMat("Aceite"), LoadMat("Suciedad") };

            // Callejones: maleza en los bordes, charcos y barro, bolsas y algún montón de nieve contra la valla.
            int decal = 0;
            foreach (var r in alleys)
            {
                bool eastWest = r.width > r.height;
                float length = eastWest ? r.width : r.height;
                for (float t = 1f; t < length - 1f; t += 1.7f)
                    foreach (float edge in new[] { 0.35f, 4.65f })
                    {
                        if (rng.NextDouble() > 0.4) continue;
                        var p = eastWest ? new Vector3(r.xMin + t, AlleyTop, r.yMin + edge) : new Vector3(r.xMin + edge, AlleyTop, r.yMin + t);
                        Clump(group, rng, p, 0.4f, 1.1f);
                    }
                for (int i = 0; i < length / 9f; i++)
                {
                    var p = new Vector3(Range(rng, r.xMin + 0.8f, r.xMax - 0.8f), AlleyTop + 0.004f + (decal++ % 40) * 0.0002f, Range(rng, r.yMin + 0.8f, r.yMax - 0.8f));
                    var m = grime[rng.Next(3)];
                    if (m) Decal(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.2f, 3f), Range(rng, 1f, 2.2f)), m);
                }
                for (int i = 0; i < length / 14f; i++)
                {
                    var p = eastWest ? new Vector3(Range(rng, r.xMin + 1f, r.xMax - 1f), AlleyTop, r.yMin + (rng.Next(2) == 0 ? 0.7f : 4.3f))
                                     : new Vector3(r.xMin + (rng.Next(2) == 0 ? 0.7f : 4.3f), AlleyTop, Range(rng, r.yMin + 1f, r.yMax - 1f));
                    if (rng.NextDouble() < 0.5) GarbageBag(group, p, rng);
                    else SnowMound(group, p, new Vector3(Range(rng, 1.2f, 2.4f), Range(rng, 0.2f, 0.4f), 0.9f), eastWest ? 0f : 90f);
                }
            }

            // Montones de nieve sucia en el bordillo de las aceras.
            foreach (var strip in sidewalks)
            {
                bool alongX = strip.width >= strip.height;
                float length = alongX ? strip.width : strip.height;
                for (float t = 2f; t < length - 2f; t += Range(rng, 6f, 13f))
                {
                    if (rng.NextDouble() < 0.35) continue;
                    var p = alongX ? new Vector3(strip.xMin + t, SidewalkTop, strip.center.y) : new Vector3(strip.center.x, SidewalkTop, strip.yMin + t);
                    if (!InsideAny(roads, p) && !InsideAny(alleys, p))
                        SnowMound(group, p, new Vector3(Range(rng, 1.2f, 2.8f), Range(rng, 0.2f, 0.4f), 0.8f), alongX ? 0f : 90f);
                }
            }

            // Placas de nieve en el césped y basura en las aceras, fuera de los edificios.
            for (int i = 0; i < 70; i++)
            {
                var lawn = lawns[rng.Next(lawns.Length)];
                var p = new Vector3(Range(rng, lawn.xMin + 1f, lawn.xMax - 1f), LawnTop, Range(rng, lawn.yMin + 1f, lawn.yMax - 1f));
                if (InsideAny(housesOccupied, p) || InsideAny(alleys, p)) continue;
                SnowPatch(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.4f, 3.5f), Range(rng, 1f, 2.4f)));
            }
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            var bottle = (name: "Botella", material: Mat("BasuraBotella", new Color(0.22f, 0.32f, 0.14f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.08f, 0.13f, 0.08f));
            var kinds = new[] { paper, can, bottle };
            for (int i = 0; i < 160; i++)
            {
                var strip = sidewalks[rng.Next(sidewalks.Length)];
                var p = new Vector3(Range(rng, strip.xMin + 0.2f, strip.xMax - 0.2f), SidewalkTop, Range(rng, strip.yMin + 0.2f, strip.yMax - 0.2f));
                if (InsideAny(roads, p) || InsideAny(alleys, p)) continue;
                PlaceLitter(group, kinds[rng.Next(kinds.Length)], p, rng);
            }
        }

        static bool InsideAny(IEnumerable<Rect> rects, Vector3 p)
        {
            foreach (var r in rects) if (r.Contains(new Vector2(p.x, p.z))) return true;
            return false;
        }

        // ---------- Tejado a dos aguas ----------

        /// <summary>
        /// Tejado a dos aguas sobre una caja de width × depth (en el plano local) cuyo borde superior está en
        /// <paramref name="wallTop"/>. Por defecto la cumbrera va a lo largo de Z (el hastial mira a la calle).
        /// Los faldones llevan cara por abajo; los hastiales (triángulos) usan el material del muro.
        /// </summary>
        static void GableRoof(Transform parent, Vector3 wallTop, float width, float depth, float rise, float overhang,
            Material roof, Material wall, bool ridgeAlongX = false)
        {
            float hw = (ridgeAlongX ? depth : width) / 2f, hd = (ridgeAlongX ? width : depth) / 2f;
            float W = hw + overhang, D = hd + overhang, drop = rise * overhang / hw;

            // Faldones: (a, b, c, e) por lado, en el marco con la cumbrera en Z.
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            float slope = Mathf.Sqrt(W * W + (rise + drop) * (rise + drop));
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, bool flip)
            {
                int i = v.Count;
                v.AddRange(new[] { a, b, c, e });
                float t = ProceduralTextures.ShingleTile;
                uv.AddRange(new[] { new Vector2(0f, 0f), new Vector2(2f * D / t, 0f), new Vector2(2f * D / t, slope / t), new Vector2(0f, slope / t) });
                tris.AddRange(flip ? new[] { i, i + 2, i + 1, i, i + 3, i + 2 } : new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            foreach (bool flip in new[] { false, true })
            {
                Quad(new Vector3(-W, -drop, -D), new Vector3(-W, -drop, D), new Vector3(0f, rise, D), new Vector3(0f, rise, -D), flip);
                Quad(new Vector3(W, -drop, D), new Vector3(W, -drop, -D), new Vector3(0f, rise, -D), new Vector3(0f, rise, D), flip);
            }
            var roofGo = MeshObject("Tejado", parent, wallTop, ridgeAlongX, v, uv, tris, roof);
            roofGo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;

            // Hastiales. Con una fachada de ladrillo se usa su versión lisa: la textura con ventanas salía cortada por los faldones.
            float tu = ProceduralTextures.FacadeBay, tv = ProceduralTextures.FacadeFloor;
            if (plainBrick.TryGetValue(wall, out var plainWall)) { wall = plainWall; tv = ProceduralTextures.PlainBrickHeight; }
            var gv = new List<Vector3>();
            var guv = new List<Vector2>();
            var gt = new List<int>();
            foreach (float z in new[] { hd, -hd })
            {
                int i = gv.Count;
                gv.AddRange(new[] { new Vector3(-hw, 0f, z), new Vector3(hw, 0f, z), new Vector3(0f, rise, z) });
                guv.AddRange(new[] { new Vector2(0f, 0f), new Vector2(2f * hw / tu, 0f), new Vector2(hw / tu, rise / tv) });
                gt.AddRange(z > 0f ? new[] { i, i + 1, i + 2 } : new[] { i, i + 2, i + 1 });
            }
            MeshObject("Hastiales", parent, wallTop, ridgeAlongX, gv, guv, gt, wall);
        }

        // ---------- Puertas sobre la textura de fachada ----------

        // La ventana de BrickFacade ocupa en cada vano 116/256 del ancho (con el alféizar) y, en la planta baja, de
        // 0.75 m (alféizar) a 2.46 m (dintel). Una puerta encima de un vano debe tapar todo eso.
        const float WindowCoverFraction = 0.47f, WindowLintelTop = 2.48f;

        /// <summary>Ancho de un vano de la textura de fachada en una pared de <paramref name="width"/> (TexturedBox redondea los vanos).</summary>
        static float BayWidth(float width) => width / Mathf.Max(1, Mathf.RoundToInt(width / ProceduralTextures.FacadeBay));

        /// <summary>
        /// Centro del vano de fachada más cercano a <paramref name="want"/> en una pared de ancho <paramref name="width"/>
        /// centrada en <paramref name="wallCx"/>, dejando al menos <paramref name="margin"/> hasta cada esquina.
        /// </summary>
        static float FacadeBayCenter(float wallCx, float width, float want, float margin = 0f)
        {
            float bayW = BayWidth(width), x0 = wallCx - width / 2f, best = wallCx, dist = float.MaxValue;
            for (float c = x0 + bayW / 2f; c < x0 + width; c += bayW)
            {
                if (c - x0 < margin - 0.01f || x0 + width - c < margin - 0.01f) continue;
                if (Mathf.Abs(c - want) < dist) { dist = Mathf.Abs(c - want); best = c; }
            }
            return best;
        }

        /// <summary>
        /// Puerta de planta baja sobre un vano de la textura de fachada (pared en z = <paramref name="faceZ"/>, mirando a
        /// <paramref name="outward"/> = ±1): un marco que tapa entera la ventana del vano, alféizar incluido, y la hoja
        /// delante. <paramref name="bottom"/> no debe pasar del alféizar (0.75 m). El marco no sube de <paramref name="maxTop"/>.
        /// </summary>
        static GameObject FacadeDoor(Transform root, float x, float bottom, float faceZ, float outward, float bayW,
            float doorW, float doorH, Material leaf, Material frame, float maxTop = 2.9f)
        {
            float frameW = Mathf.Max(doorW + 0.3f, bayW * WindowCoverFraction);
            float frameTop = Mathf.Min(maxTop, Mathf.Max(bottom + doorH + 0.12f, WindowLintelTop));
            doorH = Mathf.Min(doorH, frameTop - 0.1f - bottom);
            Box("MarcoPuerta", root, new Vector3(x, (bottom + frameTop) / 2f, faceZ + outward * 0.03f), new Vector3(frameW, frameTop - bottom, 0.06f), frame, collider: false);
            return Box("Puerta", root, new Vector3(x, bottom + doorH / 2f, faceZ + outward * 0.08f), new Vector3(doorW, doorH, 0.06f), leaf, collider: false);
        }

        static GameObject MeshObject(string name, Transform parent, Vector3 position, bool rotate90, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Material material)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (rotate90) go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // ---------- Huellas para el plano (mapa/edificios_casas.json) ----------

        /// <summary>Guarda la huella de un edificio (rectángulo local de la fila) en coordenadas del mundo, para el plano y para no poner cosas encima.</summary>
        static void RecordFootprint(Transform frame, string kind, float x0, float z0, float x1, float z1, float height)
        {
            var corners = new Vector2[4];
            var local = new[] { new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), new Vector3(x0, 0f, z1) };
            // Esquinas en el mundo (para el plano); el rectángulo ocupado, en el marco del barrio (housesFrame).
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var w = frame.TransformPoint(local[i]);
                corners[i] = new Vector2(w.x, w.z);
                var d = housesFrame ? housesFrame.InverseTransformPoint(w) : w;
                minX = Mathf.Min(minX, d.x); maxX = Mathf.Max(maxX, d.x);
                minZ = Mathf.Min(minZ, d.z); maxZ = Mathf.Max(maxZ, d.z);
            }
            housesFootprints.Add((kind, corners, height));
            if (kind != "solar") housesOccupied.Add(Rect.MinMaxRect(minX, minZ, maxX, maxZ));
        }

        /// <summary>Escribe mapa/edificios_casas.json (lo lee mapa/generar_mapa.py para dibujar las casas).</summary>
        static void ExportHousesFootprints()
        {
            var sb = new StringBuilder("{\n  \"edificios\": [\n");
            var inv = CultureInfo.InvariantCulture;
            for (int i = 0; i < housesFootprints.Count; i++)
            {
                var (kind, corners, height) = housesFootprints[i];
                sb.Append("    {\"tipo\": \"").Append(kind).Append("\", \"altura\": ").Append(height.ToString("0.##", inv)).Append(", \"esquinas\": [");
                for (int c = 0; c < corners.Length; c++)
                    sb.Append(c > 0 ? ", " : "").Append('[').Append(corners[c].x.ToString("0.##", inv)).Append(", ").Append(corners[c].y.ToString("0.##", inv)).Append(']');
                sb.Append("]}").Append(i + 1 < housesFootprints.Count ? ",\n" : "\n");
            }
            sb.Append("  ]\n}\n");
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "mapa");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "edificios_casas.json"), sb.ToString());
        }
    }
}
