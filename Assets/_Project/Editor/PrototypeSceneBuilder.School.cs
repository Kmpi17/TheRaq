using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Instituto público entre la autopista hundida y la calle x = −110, y entre las calles z = ±80
    /// (x −275…−120, z −70…70). Lleva:
    /// - edificio principal de 3 plantas de ladrillo claro mirando a la calle norte, con portal de piedra, letrero
    ///   "HIGH SCHOOL" en un remate sobre la entrada, rejas en las ventanas de abajo, mástil y cartel de bienvenida;
    /// - un ala de 2 plantas hacia el patio y un gimnasio de ladrillo liso con bloque de vidrio arriba;
    /// - campo de fútbol americano con pista de 6 calles (una textura), porterías, gradas por las que se sube, banquillos,
    ///   torres de focos, marcador y malla alrededor;
    /// - aparcamiento de profesores con salida a la calle x = −110 y dos autobuses escolares;
    /// - patio trasero, zona de servicio con contenedores, valla de hierro, árboles, nieve y basura.
    /// Va después de BuildHighway (usa sus bordes) y de BuildTowersBlock (materiales de los barrios).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static readonly Rect SchoolArea = Rect.MinMaxRect(HighwayMaxX, -70f, -120f, 70f);
        static readonly Vector3 SchoolFieldCenter = new(-197f, 0f, -35.5f);
        static readonly Rect SchoolFieldFence = Rect.MinMaxRect(-265f, -68.5f, -129f, 6f);
        static readonly Rect SchoolBleachers = Rect.MinMaxRect(-222f, -2.6f, -172f, 3.4f);
        static readonly Rect SchoolMain = Rect.MinMaxRect(-252f, 38f, -176f, 56f);   // 3 plantas, fachada al norte
        static readonly Rect SchoolWing = Rect.MinMaxRect(-232f, 20f, -214f, 38f);   // 2 plantas, hacia el patio
        static readonly Rect SchoolGym = Rect.MinMaxRect(-176f, 22f, -152f, 56f);
        static readonly Rect SchoolLot = Rect.MinMaxRect(-141f, 4f, -124f, 66f);
        static readonly Rect SchoolBusBay = Rect.MinMaxRect(-150f, 4f, -141f, 22f);
        static readonly Rect SchoolServiceYard = Rect.MinMaxRect(-252f, 26f, -232f, 38f);
        // Salidas del aparcamiento a la calle x = −110 (cortan la acera de enfrente oeste en BuildStreets).
        static readonly Rect[] SchoolDriveways = { Rect.MinMaxRect(-124f, 58f, -117f, 66f), Rect.MinMaxRect(-124f, 4f, -117f, 12f) };
        static readonly (float x0, float x1)[] SchoolFieldGates = { (-228f, -224f), (-170f, -166f) };
        const float SchoolMainHeight = 9f, SchoolParapet = 1.5f;

        static readonly List<Rect> schoolOccupied = new();

        static void BuildSchool(Transform world)
        {
            var root = new GameObject("Instituto").transform;
            root.SetParent(world, false);
            var rng = new System.Random(2018);
            schoolOccupied.Clear();

            var brick = new Color(0.64f, 0.52f, 0.36f);
            var facade = TexturedMat("FachadaInstituto", ProceduralTextures.BrickFacade("FachadaInstituto", brick, false, 61));
            plainBrick[facade] = TexturedMat("FachadaInstitutoLiso", ProceduralTextures.PlainBrick("FachadaInstitutoLiso", brick, 61));

            BuildSchoolGround(root);
            float entranceX = SchoolMainBuilding(root, facade, rng);
            SchoolWingBuilding(root, facade, rng);
            SchoolGymBuilding(root, facade, rng);
            BuildSchoolField(root, rng);
            BuildSchoolParking(root, rng);
            BuildSchoolYard(root, entranceX, rng);
        }

        // ---------- Suelo, aceras, caminos y valla ----------

        static void BuildSchoolGround(Transform root)
        {
            var group = new GameObject("Suelo").transform;
            group.SetParent(root, false);

            // Aceras del lado del instituto en las calles z = ±80, también sobre los puentes de la autopista.
            foreach (float z in new[] { 71.5f, -71.5f })
                TexturedBox("Acera", group, new Vector3((HighwayMinX - 120f) / 2f, SidewalkTop / 2f, z), new Vector3(-120f - HighwayMinX, SidewalkTop, SidewalkWidth),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);

            // Asfalto: aparcamiento, bahía de autobuses, salidas (hasta la calzada) y zona de servicio.
            var paved = new List<Rect> { SchoolLot, SchoolBusBay, SchoolServiceYard };
            paved.AddRange(SchoolDriveways);
            foreach (var r in paved)
            {
                TexturedBox("Asfalto", group, new Vector3(r.center.x, LotTop / 2f, r.center.y), new Vector3(r.width, LotTop, r.height),
                    alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
                schoolOccupied.Add(r);
            }
            foreach (var piece in RectMinus(SchoolArea, paved))
                TexturedBox("Cesped", group, new Vector3(piece.center.x, LawnTop / 2f, piece.center.y), new Vector3(piece.width, LawnTop, piece.height),
                    lawnMat, Vector2.one * ProceduralTextures.GrassTile);

            // Caminos de losas (sin colisión, como los de la manzana central).
            var walks = new[]
            {
                Rect.MinMaxRect(-216f, 58.8f, -212f, 70f),   // de la acera a la escalinata
                Rect.MinMaxRect(-226f, 56.6f, -202f, 58.8f), // delante del portal
                Rect.MinMaxRect(-202f, 58.8f, -141f, 61.3f), // por delante del gimnasio hasta el aparcamiento
                Rect.MinMaxRect(-152f, 38f, -141f, 40.5f),   // puerta lateral del gimnasio
                Rect.MinMaxRect(-246f, 9f, -156f, 19.6f),    // patio trasero
                Rect.MinMaxRect(-166f, 19.6f, -162f, 22f),   // puerta sur del gimnasio
                Rect.MinMaxRect(-197.5f, 19.6f, -193.5f, 38f), // puerta trasera del edificio principal
                Rect.MinMaxRect(-228f, 6f, -224f, 9f),       // entradas al campo
                Rect.MinMaxRect(-170f, 6f, -166f, 9f),
            };
            foreach (var w in walks)
            {
                TexturedBox("Camino", group, new Vector3(w.center.x, WalkTop / 2f, w.center.y), new Vector3(w.width, WalkTop, w.height),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
                schoolOccupied.Add(Grow(w, 0.3f));
            }

            // Valla de hierro por la calle norte, la sur y la x = −110, abierta en el camino del portal y en las salidas.
            var tile = new Vector2(ProceduralTextures.IronFenceWidth, ProceduralTextures.IronFenceHeight);
            void Iron(Vector3 a, Vector3 b) => FenceSegment(group, a, b, 1.5f, ironFence, tile, ironMat, 2.5f, 0.06f, topRail: true);
            float north = 69.4f, south = -69.4f, east = -120.6f, west = HighwayMaxX + 0.4f;
            Iron(new Vector3(west, LawnTop, north), new Vector3(-216.2f, LawnTop, north));
            Iron(new Vector3(-211.8f, LawnTop, north), new Vector3(east, LawnTop, north));
            Iron(new Vector3(west, LawnTop, south), new Vector3(east, LawnTop, south));
            Iron(new Vector3(east, LawnTop, south), new Vector3(east, LawnTop, SchoolDriveways[1].yMin));
            Iron(new Vector3(east, LawnTop, SchoolDriveways[1].yMax), new Vector3(east, LawnTop, SchoolDriveways[0].yMin));
            Iron(new Vector3(east, LawnTop, SchoolDriveways[0].yMax), new Vector3(east, LawnTop, north));
        }

        // ---------- Edificios ----------

        /// <summary>
        /// Bloque de ladrillo con ventanas (las de la textura de fachada), antepecho de ladrillo liso, albardilla de piedra,
        /// cornisa, zócalo y tejado. El marco local tiene el origen en el centro de la planta, en el suelo.
        /// </summary>
        static Transform SchoolBlock(Transform parent, string name, Rect r, float wallH, float parapetH, Material facade)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(r.center.x, 0f, r.center.y);
            float w = r.width, d = r.height;
            TexturedBox("Muros", root, new Vector3(0f, wallH / 2f, 0f), new Vector3(w, wallH, d), facade,
                new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor), sidesOnly: true);
            SchoolParapetAndRoof(root, w, d, wallH, parapetH, plainBrick[facade]);
            Box("Cornisa", root, new Vector3(0f, wallH + 0.12f, 0f), new Vector3(w + 0.25f, 0.24f, d + 0.25f), stoneTrim, collider: false);
            Box("Zocalo", root, new Vector3(0f, 0.3f, 0f), new Vector3(w + 0.12f, 0.6f, d + 0.12f), stoneTrim);
            schoolOccupied.Add(Grow(r, 0.5f));
            return root;
        }

        static void SchoolParapetAndRoof(Transform root, float w, float d, float wallH, float parapetH, Material plain)
        {
            TexturedBox("Antepecho", root, new Vector3(0f, wallH + parapetH / 2f, 0f), new Vector3(w, parapetH, d), plain,
                new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight), sidesOnly: true);
            float top = wallH + parapetH;
            foreach (float s in new[] { -1f, 1f })
            {
                Box("Albardilla", root, new Vector3(0f, top + 0.08f, s * (d / 2f - 0.1f)), new Vector3(w + 0.2f, 0.16f, 0.45f), stoneTrim, collider: false);
                Box("Albardilla", root, new Vector3(s * (w / 2f - 0.1f), top + 0.08f, 0f), new Vector3(0.45f, 0.16f, d + 0.2f), stoneTrim, collider: false);
            }
            Box("Tejado", root, new Vector3(0f, wallH + 0.35f, 0f), new Vector3(w - 0.2f, 0.1f, d - 0.2f), tarRoof, collider: false);
        }

        /// <summary>Rejas sobre las ventanas de la planta baja de la cara z = <paramref name="faceZ"/> (±), menos en los vanos de <paramref name="skip"/>.</summary>
        static void SchoolGrilles(Transform root, float w, float faceZ, float outward, IEnumerable<float> skip)
        {
            if (!chainLink) return;
            float bayW = BayWidth(w);
            for (float x = -w / 2f + bayW / 2f; x < w / 2f; x += bayW)
            {
                bool skipped = false;
                foreach (float s in skip) skipped |= Mathf.Abs(x - s) < bayW * 0.6f;
                if (skipped) continue;
                SignQuad(root, new Vector3(x, 1.6f, faceZ + outward * 0.06f), new Vector3(0f, 0f, outward), bayW * 0.5f, 1.7f, chainLink);
            }
        }

        /// <summary>Edificio principal: 3 plantas mirando a la calle norte. Devuelve la x (mundo) de la entrada.</summary>
        static float SchoolMainBuilding(Transform parent, Material facade, System.Random rng)
        {
            var r = SchoolMain;
            var root = SchoolBlock(parent, "EdificioPrincipal", r, SchoolMainHeight, SchoolParapet, facade);
            float w = r.width, d = r.height, front = d / 2f, back = -d / 2f, bayW = BayWidth(w);
            float entrance = FacadeBayCenter(0f, w, -214f - r.center.x);
            float backDoor = FacadeBayCenter(0f, w, -195.5f - r.center.x);
            float dock = FacadeBayCenter(0f, w, -244f - r.center.x);

            // Portal de piedra: tapa entera la planta baja de tres vanos (no deja medias ventanas) y no llega a las del primero.
            float portalW = 3f * bayW, portalH = 3.6f;
            Box("Portal", root, new Vector3(entrance, portalH / 2f, front + 0.35f), new Vector3(portalW, portalH, 0.7f), stoneTrim);
            Box("Friso", root, new Vector3(entrance, portalH - 0.2f, front + 0.75f), new Vector3(portalW + 0.3f, 0.4f, 0.2f), stoneTrim, collider: false);
            var glass = Mat("CristalPortal", new Color(0.1f, 0.12f, 0.13f));
            glass.SetFloat("_Smoothness", 0.7f);
            for (int i = -1; i <= 1; i++)
            {
                float x = entrance + i * bayW;
                Box("MarcoPuerta", root, new Vector3(x, 1.6f, front + 0.71f), new Vector3(2.3f, 2.6f, 0.04f), ironMat, collider: false);
                foreach (float s in new[] { -0.5f, 0.5f })
                    Box("Hoja", root, new Vector3(x + s, 1.55f, front + 0.74f), new Vector3(0.9f, 2.4f, 0.04f), glass, collider: false);
            }
            var lampGlow = EmissiveMat("LuzPortalTorre", new Color(1f, 0.88f, 0.65f), 2f);
            foreach (float s in new[] { -1f, 1f })
                Box("Aplique", root, new Vector3(entrance + s * (portalW / 2f - 0.4f), 2.9f, front + 0.8f), new Vector3(0.3f, 0.45f, 0.2f), lampGlow, collider: false);
            var visitors = SignMat("Letrero_Visitas", "ALL VISITORS REPORT TO MAIN OFFICE", new Color(0.12f, 0.2f, 0.45f), new Color(0.95f, 0.95f, 0.95f), 95, 0.1f);
            // Encima de las puertas de la izquierda (en la fachada de al lado taparía media ventana).
            SignQuad(root, new Vector3(entrance - bayW, 3.08f, front + 0.72f), Vector3.forward, 2.6f,
                2.6f / ((float)visitors.mainTexture.width / visitors.mainTexture.height), visitors);

            // Escalinata ancha con barandillas (se sube andando: escalones de 15 cm).
            Box("Rellano", root, new Vector3(entrance, 0.15f, front + 1.5f), new Vector3(portalW + 1f, 0.3f, 1.6f), concreteMat);
            Box("Escalon", root, new Vector3(entrance, 0.075f, front + 2.55f), new Vector3(portalW + 1f, 0.15f, 0.5f), concreteMat);
            foreach (float s in new[] { -1f, 1f })
            {
                float x = entrance + s * (bayW * 0.5f);
                Strut("Barandilla", root, new Vector3(x, 1.2f, front + 0.9f), new Vector3(x, 1.05f, front + 2.8f), 0.05f, ironMat, cylinder: true);
                foreach (float z in new[] { front + 0.9f, front + 2.8f })
                    Strut("Poste", root, new Vector3(x, z > front + 2f ? 0.15f : 0.3f, z), new Vector3(x, z > front + 2f ? 1.05f : 1.2f, z), 0.05f, ironMat, cylinder: true);
            }

            // Remate de ladrillo sobre la entrada con el letrero "HIGH SCHOOL" (genérico, sin nombre real) en piedra.
            float roofTop = SchoolMainHeight + SchoolParapet;
            TexturedBox("Remate", root, new Vector3(entrance, (SchoolMainHeight + roofTop + 2.6f) / 2f, front - 0.3f),
                new Vector3(portalW, roofTop + 2.6f - SchoolMainHeight, 0.9f), plainBrick[facade], new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight));
            Box("AlbardillaRemate", root, new Vector3(entrance, roofTop + 2.68f, front - 0.3f), new Vector3(portalW + 0.3f, 0.16f, 1.1f), stoneTrim, collider: false);
            Box("Placa", root, new Vector3(entrance, roofTop + 1.1f, front + 0.17f), new Vector3(portalW - 0.6f, 1.7f, 0.06f), stoneTrim, collider: false);
            var name = SignMat("Letrero_HighSchool", "HIGH SCHOOL", new Color(0.66f, 0.64f, 0.58f), new Color(0.2f, 0.18f, 0.16f), 96, 0f);
            float aspect = (float)name.mainTexture.width / name.mainTexture.height;
            SignQuad(root, new Vector3(entrance, roofTop + 1.1f, front + 0.21f), Vector3.forward, portalW - 1f, (portalW - 1f) / aspect, name);
            var year = SignMat("Letrero_Ano", "EST 1961", new Color(0.62f, 0.6f, 0.55f), new Color(0.25f, 0.23f, 0.2f), 97, 0f);
            SignQuad(root, new Vector3(entrance, portalH - 0.2f, front + 0.86f), Vector3.forward, 1.8f, 1.8f / ((float)year.mainTexture.width / year.mainTexture.height), year);

            // Rejas en la planta baja de delante y de detrás; puerta trasera hacia el patio y muelle de carga en la zona de servicio.
            SchoolGrilles(root, w, front, 1f, new[] { entrance - bayW, entrance, entrance + bayW });
            SchoolGrilles(root, w, back, -1f, new[] { backDoor, dock });
            FacadeDoor(root, backDoor, 0.1f, back, -1f, bayW, 1.8f, 2.3f, Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f)), concreteMat);
            Box("Escalon", root, new Vector3(backDoor, 0.1f, back - 0.5f), new Vector3(2.4f, 0.2f, 1f), concreteMat);
            var shutter = LoadMat("PersianaMetalica");
            if (shutter)
            {
                TexturedBox("PersianaCarga", root, new Vector3(dock, 1.55f, back - 0.05f), new Vector3(bayW, 2.9f, 0.06f), shutter, Vector2.one, collider: false);
                Box("Muelle", root, new Vector3(dock, 0.5f, back - 1.2f), new Vector3(bayW + 0.6f, 1f, 2.4f), concreteMat);
            }

            // Algunas ventanas de arriba tapadas con contrachapado y máquinas en el tejado.
            for (int i = 0; i < 5; i++)
            {
                int bays = Mathf.RoundToInt(w / ProceduralTextures.FacadeBay);
                float x = -w / 2f + (rng.Next(bays) + 0.5f) * bayW;
                float y = (1 + rng.Next(2)) * ProceduralTextures.FacadeFloor + 1.59f;
                float face = rng.Next(2) == 0 ? front : back;
                Box("Tablon", root, new Vector3(x, y, face + Mathf.Sign(face) * 0.05f), new Vector3(bayW * 0.42f, 1.6f, 0.04f), plywood, collider: false);
            }
            var hvac = Mat("AparatoAire", new Color(0.6f, 0.61f, 0.6f));
            foreach (float x in new[] { -24f, -6f, 15f })
            {
                Box("Climatizador", root, new Vector3(x, SchoolMainHeight + 1.1f, -1f), new Vector3(3.2f, 1.5f, 2.2f), hvac, collider: false);
                Primitive(PrimitiveType.Cylinder, "Ventilador", root, new Vector3(x, SchoolMainHeight + 1.9f, -1f), new Vector3(1.4f, 0.05f, 1.4f), ironMat, keepCollider: false);
            }
            for (int i = 0; i < 3; i++)
                WallGraffiti(root, new Vector3(-w / 2f + 4f + i * 5f, 1.4f, back), Vector3.back, Range(rng, 2.4f, 3.6f), graffitiMats[rng.Next(graffitiMats.Length)]);

            return r.center.x + entrance;
        }

        static void SchoolWingBuilding(Transform parent, Material facade, System.Random rng)
        {
            var r = SchoolWing;
            var root = SchoolBlock(parent, "Ala", r, 6f, 0.75f, facade);
            float bayW = BayWidth(r.width), door = FacadeBayCenter(0f, r.width, 0f);
            FacadeDoor(root, door, 0.1f, -r.height / 2f, -1f, bayW, 1.8f, 2.3f, Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f)), concreteMat);
            Box("Escalon", root, new Vector3(door, 0.1f, -r.height / 2f - 0.5f), new Vector3(2.4f, 0.2f, 1f), concreteMat);
            Box("Tejadillo", root, new Vector3(door, 2.95f, -r.height / 2f - 0.6f), new Vector3(2.6f, 0.12f, 1.2f), concreteMat, collider: false);
            SchoolGrilles(root, r.width, -r.height / 2f, -1f, new[] { door });
            WallGraffiti(root, new Vector3(r.width / 2f, 1.5f, -2f), Vector3.right, 3f, graffitiMats[rng.Next(graffitiMats.Length)]);
        }

        /// <summary>Gimnasio: caja alta de ladrillo liso con una banda de bloque de vidrio arriba y letrero hacia la calle.</summary>
        static void SchoolGymBuilding(Transform parent, Material facade, System.Random rng)
        {
            var r = SchoolGym;
            var root = new GameObject("Gimnasio").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(r.center.x, 0f, r.center.y);
            float w = r.width, d = r.height, h = 10f;
            var plain = plainBrick[facade];
            TexturedBox("Muros", root, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), plain, new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight), sidesOnly: true);
            SchoolParapetAndRoof(root, w, d, h, 0.8f, plain);
            Box("Zocalo", root, new Vector3(0f, 0.3f, 0f), new Vector3(w + 0.12f, 0.6f, d + 0.12f), stoneTrim);
            schoolOccupied.Add(Grow(r, 0.5f));

            // Banda de bloque de vidrio en la cara este y la sur, partida por pilastras.
            foreach (var (normal, along, length) in new[] { (Vector3.right, Vector3.forward, d), (Vector3.back, Vector3.right, w) })
            {
                float half = normal == Vector3.right ? w / 2f : d / 2f;
                Box("BloqueVidrio", root, normal * (half + 0.03f) + Vector3.up * 7.5f, Abs(along) * (length - 1f) + Abs(normal) * 0.06f + Vector3.up * 2f, glassBlock, collider: false);
                for (float t = -length / 2f + 0.5f; t <= length / 2f - 0.4f; t += (length - 1f) / Mathf.Round((length - 1f) / 4f))
                    TexturedBox("Pilastra", root, normal * (half + 0.15f) + along * t + Vector3.up * 7.5f, Abs(along) * 0.5f + Abs(normal) * 0.3f + Vector3.up * 2.2f,
                        plain, new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight), collider: false);
            }

            // Letrero hacia la calle norte y puertas dobles de chapa: al este (camino del aparcamiento) y al sur (patio).
            var sign = SignMat("Letrero_Gimnasio", "GYMNASIUM", new Color(0.4f, 0.1f, 0.1f), new Color(0.92f, 0.85f, 0.55f), 98, 0.15f);
            SignQuad(root, new Vector3(0f, 7.2f, d / 2f + 0.03f), Vector3.forward, 12f, 12f / ((float)sign.mainTexture.width / sign.mainTexture.height), sign);
            var metal = Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f));
            var red = Mat("PuertaRoja", new Color(0.45f, 0.1f, 0.08f));
            void Doors(Vector3 face, Vector3 normal)
            {
                var across = Vector3.Cross(Vector3.up, normal);
                Box("Marco", root, face + normal * 0.03f + Vector3.up * 1.35f, Abs(across) * 2.8f + Abs(normal) * 0.06f + Vector3.up * 2.7f, metal, collider: false);
                foreach (float s in new[] { -0.65f, 0.65f })
                    Box("Hoja", root, face + normal * 0.07f + across * s + Vector3.up * 1.25f, Abs(across) * 1.25f + Abs(normal) * 0.05f + Vector3.up * 2.4f, red, collider: false);
                Box("Tejadillo", root, face + normal * 0.7f + Vector3.up * 2.95f, Abs(across) * 3.4f + Abs(normal) * 1.4f + Vector3.up * 0.12f, concreteMat, collider: false);
            }
            Doors(new Vector3(w / 2f, 0f, 39.25f - r.center.y), Vector3.right);
            Doors(new Vector3(-164f - r.center.x, 0f, -d / 2f), Vector3.back);

            // Grafitis en la cara sur (la del patio) y aparatos en el tejado.
            for (int i = 0; i < 3; i++)
                WallGraffiti(root, new Vector3(-w / 2f + 2f + i * 3.5f, Range(rng, 1.3f, 2f), -d / 2f), Vector3.back, Range(rng, 2.2f, 3f), graffitiMats[rng.Next(graffitiMats.Length)]);
            Box("Climatizador", root, new Vector3(4f, h + 1f, 6f), new Vector3(3f, 1.4f, 2.2f), Mat("AparatoAire", new Color(0.6f, 0.61f, 0.6f)), collider: false);
        }

        // ---------- Campo, pista, gradas ----------

        static void BuildSchoolField(Transform parent, System.Random rng)
        {
            var root = new GameObject("Campo").transform;
            root.SetParent(parent, false);
            var c = SchoolFieldCenter;
            var size = ProceduralTextures.FieldTextureSize;
            var field = TexturedMat("CampoFutbol", ProceduralTextures.FootballField("CampoFutbol", 62));
            TexturedBox("Campo", root, new Vector3(c.x, LawnTop + 0.006f, c.z), new Vector3(size.x, 0.012f, size.y), field, size, collider: false);
            schoolOccupied.Add(Rect.MinMaxRect(c.x - size.x / 2f, c.z - size.y / 2f, c.x + size.x / 2f, c.z + size.y / 2f));

            // Porterías amarillas de cuello de cisne: el poste va detrás de la línea de fondo.
            var yellow = Mat("PorteriaAmarilla", new Color(0.85f, 0.7f, 0.1f));
            var pad = Mat("ProtectorPorteria", new Color(0.4f, 0.1f, 0.1f));
            foreach (float s in new[] { -1f, 1f })
            {
                float endX = c.x + s * ProceduralTextures.FieldHalfLength, baseX = endX + s * 1.8f;
                var g = new Vector3(0f, LawnTop, c.z);
                Primitive(PrimitiveType.Cylinder, "Poste", root, g + new Vector3(baseX, 1.6f, 0f), new Vector3(0.2f, 1.6f, 0.2f), yellow, keepCollider: true);
                Primitive(PrimitiveType.Cylinder, "Protector", root, g + new Vector3(baseX, 1f, 0f), new Vector3(0.36f, 1f, 0.36f), pad, keepCollider: false);
                Strut("Cuello", root, g + new Vector3(baseX, 3.1f, 0f), g + new Vector3(endX, 3.1f, 0f), 0.16f, yellow, cylinder: true);
                Strut("Travesano", root, g + new Vector3(endX, 3.1f, -2.8f), g + new Vector3(endX, 3.1f, 2.8f), 0.14f, yellow, cylinder: true);
                foreach (float z in new[] { -2.8f, 2.8f })
                    Strut("Palo", root, g + new Vector3(endX, 3.1f, z), g + new Vector3(endX, 9f, z), 0.1f, yellow, cylinder: true);
            }

            // Malla de 2.4 m alrededor, abierta en las dos entradas desde el patio (al norte).
            if (chainLink)
            {
                var f = SchoolFieldFence;
                var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
                void Fence(Vector3 a, Vector3 b) => FenceSegment(root, a, b, 2.4f, chainLink, tile, ironMat, 3f, 0.06f, topRail: true);
                Fence(new Vector3(f.xMin, LawnTop, f.yMin), new Vector3(f.xMax, LawnTop, f.yMin));
                Fence(new Vector3(f.xMin, LawnTop, f.yMin), new Vector3(f.xMin, LawnTop, f.yMax));
                Fence(new Vector3(f.xMax, LawnTop, f.yMin), new Vector3(f.xMax, LawnTop, f.yMax));
                float x = f.xMin;
                foreach (var (g0, g1) in SchoolFieldGates)
                {
                    Fence(new Vector3(x, LawnTop, f.yMax), new Vector3(g0, LawnTop, f.yMax));
                    x = g1;
                }
                Fence(new Vector3(x, LawnTop, f.yMax), new Vector3(f.xMax, LawnTop, f.yMax));
            }

            SchoolBleachersBuild(root);

            // Banquillos en la banda norte, entre la línea y la pista.
            var alu = Mat("Aluminio", new Color(0.72f, 0.73f, 0.74f));
            alu.SetFloat("_Metallic", 0.6f);
            alu.SetFloat("_Smoothness", 0.45f);
            float benchZ = c.z + ProceduralTextures.FieldHalfWidth + 2f;
            foreach (float x0 in new[] { c.x - 13f, c.x + 3f })
            {
                Box("Banquillo", root, new Vector3(x0 + 5f, LawnTop + 0.45f, benchZ), new Vector3(10f, 0.06f, 0.4f), alu);
                for (float x = x0 + 0.5f; x <= x0 + 9.6f; x += 3f)
                    Box("PataBanquillo", root, new Vector3(x, LawnTop + 0.22f, benchZ), new Vector3(0.06f, 0.44f, 0.35f), alu, collider: false);
                TrashCan(root, new Vector3(x0 + 10.6f, LawnTop, benchZ), 0f, rng.NextDouble() < 0.3);
            }

            // Torres de focos en las cuatro esquinas, dentro de la malla.
            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lights = EmissiveMat("FocosEstadio", new Color(0.95f, 0.95f, 0.85f), 0.8f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    var p = new Vector3(c.x + sx * 52f, LawnTop, sz < 0f ? SchoolFieldFence.yMin + 0.8f : c.z + 32.4f);
                    Primitive(PrimitiveType.Cylinder, "TorreFocos", root, p + Vector3.up * 9f, new Vector3(0.4f, 9f, 0.4f), pole, keepCollider: true);
                    var head = Box("Focos", root, p + new Vector3(0f, 18.4f, 0f), new Vector3(3.2f, 1.6f, 0.3f), lights, collider: false);
                    head.transform.localRotation = Quaternion.LookRotation(new Vector3(c.x - p.x, 0f, c.z - p.z)) * Quaternion.Euler(20f, 0f, 0f);
                    Box("Pasarela", root, p + new Vector3(0f, 17.4f, 0f), new Vector3(3.4f, 0.1f, 1.2f), pole, collider: false)
                        .transform.localRotation = head.transform.localRotation;
                }

            // Marcador entre la malla y la autopista, mirando al campo.
            var board = new GameObject("Marcador").transform;
            board.SetParent(root, false);
            board.localPosition = new Vector3(SchoolFieldFence.xMin - 3.5f, LawnTop, c.z);
            board.localRotation = Quaternion.Euler(0f, 90f, 0f); // la cara +Z local mira al este, al campo
            var black = Mat("PanelNegro", new Color(0.04f, 0.04f, 0.05f));
            var maroon = Mat("MarcadorGranate", new Color(0.4f, 0.1f, 0.1f));
            foreach (float x in new[] { -3f, 3f })
                Box("Pata", board, new Vector3(x, 3f, 0f), new Vector3(0.35f, 6f, 0.35f), pole);
            Box("Tablero", board, new Vector3(0f, 7.6f, 0f), new Vector3(8.4f, 3.8f, 0.4f), black, collider: false);
            Box("Remate", board, new Vector3(0f, 9.9f, 0f), new Vector3(8.4f, 0.9f, 0.42f), maroon, collider: false);
            var home = SignMat("Letrero_Home", "HOME", new Color(0.04f, 0.04f, 0.05f), new Color(0.95f, 0.95f, 0.9f), 99, 0.4f);
            var guest = SignMat("Letrero_Guest", "GUEST", new Color(0.04f, 0.04f, 0.05f), new Color(0.95f, 0.95f, 0.9f), 100, 0.4f);
            float ha = (float)home.mainTexture.width / home.mainTexture.height, ga = (float)guest.mainTexture.width / guest.mainTexture.height;
            SignQuad(board, new Vector3(-2.2f, 8.9f, 0.22f), Vector3.forward, 0.6f * ha, 0.6f, home);
            SignQuad(board, new Vector3(2.2f, 8.9f, 0.22f), Vector3.forward, 0.6f * ga, 0.6f, guest);
            var led = EmissiveMat("PrecioLED", new Color(1f, 0.45f, 0.08f), 2.5f);
            foreach (float x in new[] { -2.2f, 2.2f })
                for (int k = 0; k < 2; k++)
                    Box("Digito", board, new Vector3(x - 0.45f + k * 0.9f, 7.5f, 0.22f), new Vector3(0.7f, 1.3f, 0.02f), led, collider: false);
            if (graffitiMats != null && graffitiMats[1])
                WallGraffiti(board, new Vector3(0f, 7.6f, -0.2f), Vector3.back, 5f, graffitiMats[1]);

            // Nieve apartada contra la malla y basura bajo las gradas.
            for (int i = 0; i < 18; i++)
            {
                bool south = rng.Next(2) == 0;
                var p = south ? new Vector3(Range(rng, SchoolFieldFence.xMin + 2f, SchoolFieldFence.xMax - 2f), LawnTop, SchoolFieldFence.yMin + 1f)
                              : new Vector3(rng.Next(2) == 0 ? SchoolFieldFence.xMin + 1f : SchoolFieldFence.xMax - 1f, LawnTop, Range(rng, -60f, -8f));
                SnowMound(root, p, new Vector3(Range(rng, 1.5f, 3.5f), Range(rng, 0.25f, 0.6f), Range(rng, 0.8f, 1.4f)), south ? 0f : 90f);
            }
            var cup = (name: "Vaso", material: Mat("BasuraVaso", new Color(0.75f, 0.15f, 0.12f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.09f, 0.06f, 0.09f));
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var b = SchoolBleachers;
            for (int i = 0; i < 40; i++)
                PlaceLitter(root, rng.Next(2) == 0 ? cup : paper, new Vector3(Range(rng, b.xMin, b.xMax), LawnTop, Range(rng, b.yMin - 1.2f, b.yMax + 1.5f)), rng);
        }

        /// <summary>
        /// Gradas de aluminio de 7 filas mirando al campo (al sur), con palco de prensa arriba. Se suben andando: una
        /// rampa invisible sigue la pendiente de los asientos (29°, menos que el límite del CharacterController).
        /// </summary>
        static void SchoolBleachersBuild(Transform parent)
        {
            var root = new GameObject("Gradas").transform;
            root.SetParent(parent, false);
            var b = SchoolBleachers;
            var alu = Mat("Aluminio", new Color(0.72f, 0.73f, 0.74f));
            var frame = Mat("AceroGalvanizado", new Color(0.45f, 0.47f, 0.48f));
            const int rows = 7;
            const float rise = 0.45f, run = 0.8f;
            float x0 = b.xMin, x1 = b.xMax, cx = b.center.x, len = b.width;
            for (int i = 0; i < rows; i++)
            {
                float z = b.yMin + 0.6f + i * run, y = LawnTop + 0.3f + i * rise;
                Box("Pisadera", root, new Vector3(cx, y, z + 0.2f), new Vector3(len, 0.04f, 0.45f), alu, collider: false);
                Box("Asiento", root, new Vector3(cx, y + rise, z + 0.6f), new Vector3(len, 0.05f, 0.3f), alu, collider: false);
            }
            float topY = LawnTop + 0.3f + rows * rise, backZ = b.yMin + 0.6f + rows * run;
            // Estructura: pórticos cada 5 m (pata trasera y diagonal), barandilla atrás y a los lados.
            for (float x = x0 + 0.5f; x <= x1 - 0.4f; x += 4.95f)
            {
                Strut("PataTrasera", root, new Vector3(x, LawnTop, backZ), new Vector3(x, topY + 1.1f, backZ), 0.08f, frame);
                Strut("Diagonal", root, new Vector3(x, LawnTop, b.yMin + 0.4f), new Vector3(x, topY, backZ), 0.08f, frame);
            }
            Strut("BarandillaTrasera", root, new Vector3(x0, topY + 1.1f, backZ), new Vector3(x1, topY + 1.1f, backZ), 0.05f, frame, cylinder: true);
            foreach (float x in new[] { x0, x1 })
                Strut("BarandillaLado", root, new Vector3(x, LawnTop + 1.1f, b.yMin + 0.4f), new Vector3(x, topY + 1.1f, backZ), 0.05f, frame, cylinder: true);
            ColliderBox(root, "BarandillaTraseraCol", new Vector3(cx, topY + 0.6f, backZ), new Vector3(len, 1.2f, 0.1f));

            // Rampa invisible de la primera pisadera a la última (se pisa sobre los asientos).
            var from = new Vector3(cx, LawnTop, b.yMin + 0.2f);
            var to = new Vector3(cx, topY + 0.05f, backZ - 0.2f);
            var ramp = ColliderBox(root, "RampaGradas", (from + to) / 2f - Vector3.up * 0.1f, new Vector3(len, 0.2f, Vector3.Distance(from, to)));
            ramp.transform.localRotation = Quaternion.LookRotation(to - from);

            // Palco de prensa encima, en el centro.
            var booth = Mat("PalcoPrensa", new Color(0.62f, 0.6f, 0.55f));
            Box("Palco", root, new Vector3(cx, topY + 1.4f, backZ + 0.9f), new Vector3(6f, 2.6f, 1.8f), booth, collider: false);
            Box("VentanaPalco", root, new Vector3(cx, topY + 1.7f, backZ - 0.02f), new Vector3(5f, 0.9f, 0.04f), Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f)), collider: false);
            Box("TejadoPalco", root, new Vector3(cx, topY + 2.8f, backZ + 0.8f), new Vector3(6.4f, 0.15f, 2.4f), tarRoof, collider: false);
            schoolOccupied.Add(Grow(b, 0.5f));
        }

        // ---------- Aparcamiento y autobuses ----------

        static void BuildSchoolParking(Transform parent, System.Random rng)
        {
            var root = new GameObject("Aparcamiento").transform;
            root.SetParent(parent, false);
            var paint = Mat("LineaAparcamiento", new Color(0.82f, 0.82f, 0.78f));
            var oil = LoadMat("Aceite");
            var lot = SchoolLot;
            const float stall = 2.6f, depth = 5f;
            float z0 = 12.9f, z1 = 58f, lineY = LotTop + 0.005f;
            int n = Mathf.FloorToInt((z1 - z0) / stall);
            // Dos filas de plazas mirando al pasillo central (x −136…−129): la oeste con el morro al oeste y la este al este.
            foreach (var (xBack, into) in new[] { (lot.xMin, 1f), (lot.xMax, -1f) })
            {
                float xc = xBack + into * depth / 2f;
                for (int i = 0; i <= n; i++)
                    Box("Linea", root, new Vector3(xc, lineY, z0 + i * stall), new Vector3(depth, 0.01f, 0.12f), paint, collider: false);
                for (int i = 0; i < n; i++)
                {
                    float z = z0 + (i + 0.5f) * stall;
                    Box("Tope", root, new Vector3(xBack + into * 0.7f, LotTop + 0.06f, z), new Vector3(0.22f, 0.12f, 1.8f), concreteMat, collider: false);
                    double roll = rng.NextDouble();
                    if (roll < 0.5) ParkedCar(root, new Vector3(xc, LotTop, z), (into > 0f ? 180f : 0f) + Range(rng, -4f, 4f), rng);
                    else if (roll < 0.58) SnowMound(root, new Vector3(xc, LotTop, z), new Vector3(3.8f, Range(rng, 0.5f, 0.9f), 2.2f), Range(rng, -10f, 10f));
                    if (oil && rng.NextDouble() < 0.4)
                        Decal(root, new Vector3(xc + Range(rng, -1f, 1f), LotTop + 0.002f + (i % 10) * 0.0002f, z), Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.8f, 1.6f), oil);
                }
            }
            // Flechas de sentido único en el pasillo (entra por el norte, sale por el sur) y montones de nieve en las esquinas.
            for (float z = 52f; z > 16f; z -= 12f)
            {
                Box("Flecha", root, new Vector3(-132.5f, lineY, z), new Vector3(0.25f, 0.01f, 2f), paint, collider: false);
                foreach (float s in new[] { -1f, 1f })
                    Box("Punta", root, new Vector3(-132.5f + s * 0.3f, lineY, z - 1.05f), new Vector3(0.18f, 0.01f, 0.8f), paint, collider: false)
                        .transform.localRotation = Quaternion.Euler(0f, s * 35f, 0f);
            }
            foreach (var p in new[] { new Vector3(-140f, LotTop, 64.5f), new Vector3(-125f, LotTop, 5.5f), new Vector3(-140f, LotTop, 5.5f) })
                SnowMound(root, p, new Vector3(Range(rng, 3f, 4.5f), Range(rng, 0.9f, 1.4f), Range(rng, 2.5f, 3.5f)), Range(rng, 0f, 90f));
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float z in new[] { 30f, 50f })
                StreetLight(root, new Vector3(lot.xMin - 0.8f, LawnTop - SidewalkTop, z), Vector3.right, pole, lamp);
            var reserved = SignMat("Letrero_Staff", "STAFF PARKING ONLY", new Color(0.92f, 0.92f, 0.9f), new Color(0.1f, 0.2f, 0.5f), 101, 0.1f);
            var signPos = new Vector3(-123f, LawnTop, 67.2f);
            Strut("PosteCartel", root, signPos, signPos + Vector3.up * 2.3f, 0.06f, ironMat, cylinder: true);
            SignQuad(root, signPos + new Vector3(0f, 2f, 0.05f), Vector3.forward, 1.4f, 1.4f / ((float)reserved.mainTexture.width / reserved.mainTexture.height), reserved);

            // Bahía de autobuses: dos autobuses escolares amarillos aparcados en paralelo (morro al sur, hacia la salida).
            var bay = SchoolBusBay;
            for (int i = 0; i < 2; i++)
                SchoolBus(root, new Vector3(bay.xMin + 2.3f + i * 3.6f, LotTop, bay.center.y + 0.5f), 180f);
            Box("LineaBus", root, new Vector3(bay.xMin + 4.1f, lineY, bay.center.y), new Vector3(0.12f, 0.01f, bay.height - 1f), paint, collider: false);
        }

        /// <summary>Autobús escolar genérico: caja amarilla con capó, franjas negras, ventanillas, letreros "SCHOOL BUS" y brazo de stop.</summary>
        static void SchoolBus(Transform parent, Vector3 ground, float yaw)
        {
            var root = new GameObject("AutobusEscolar").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f); // morro hacia +Z local
            var yellow = Mat("AmarilloAutobus", new Color(0.88f, 0.62f, 0.08f));
            var black = Mat("PlasticoNegro", new Color(0.05f, 0.05f, 0.05f));
            var windows = Mat("CristalCoche", new Color(0.12f, 0.14f, 0.16f));
            var red = Mat("PilotoTrasero", new Color(0.6f, 0.05f, 0.04f));
            const float w = 2.45f, bodyLen = 9.6f, hoodLen = 1.6f, floor = 0.55f, roofY = 3f;
            float bodyZ = -hoodLen / 2f;
            Box("Carroceria", root, new Vector3(0f, (floor + roofY) / 2f, bodyZ), new Vector3(w, roofY - floor, bodyLen), yellow);
            Box("Techo", root, new Vector3(0f, roofY + 0.05f, bodyZ), new Vector3(w - 0.2f, 0.12f, bodyLen - 0.2f), yellow, collider: false);
            Box("Capo", root, new Vector3(0f, 1.2f, bodyZ + bodyLen / 2f + hoodLen / 2f), new Vector3(w - 0.25f, 1.3f, hoodLen), yellow);
            Box("Parachoques", root, new Vector3(0f, 0.6f, bodyZ + bodyLen / 2f + hoodLen + 0.1f), new Vector3(w, 0.3f, 0.2f), black, collider: false);
            Box("ParachoquesTrasero", root, new Vector3(0f, 0.6f, bodyZ - bodyLen / 2f - 0.1f), new Vector3(w, 0.3f, 0.2f), black, collider: false);
            foreach (float s in new[] { -1f, 1f })
            {
                Box("Ventanillas", root, new Vector3(s * (w / 2f + 0.01f), 2.2f, bodyZ - 0.3f), new Vector3(0.02f, 0.75f, bodyLen - 1.4f), windows, collider: false);
                foreach (float y in new[] { 1.05f, 1.55f })
                    Box("Franja", root, new Vector3(s * (w / 2f + 0.015f), y, bodyZ), new Vector3(0.02f, 0.1f, bodyLen), black, collider: false);
                foreach (float z in new[] { bodyZ + bodyLen / 2f - 0.2f, bodyZ - bodyLen / 2f + 2f })
                    Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(s * (w / 2f - 0.25f), 0.5f, z), new Vector3(1f, 0.15f, 1f), black, keepCollider: false)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Box("Parabrisas", root, new Vector3(0f, 2.2f, bodyZ + bodyLen / 2f + 0.01f), new Vector3(w - 0.3f, 0.95f, 0.02f), windows, collider: false);
            Box("LunaTrasera", root, new Vector3(0f, 2.2f, bodyZ - bodyLen / 2f - 0.01f), new Vector3(w - 0.6f, 0.8f, 0.02f), windows, collider: false);
            Box("BrazoStop", root, new Vector3(-w / 2f - 0.3f, 1.9f, bodyZ + bodyLen / 2f - 1.5f), new Vector3(0.6f, 0.6f, 0.03f), red, collider: false);
            var sign = SignMat("Letrero_SchoolBus", "SCHOOL BUS", new Color(0.88f, 0.62f, 0.08f), new Color(0.05f, 0.05f, 0.05f), 102, 0.05f);
            float h = 1.7f / ((float)sign.mainTexture.width / sign.mainTexture.height);
            SignQuad(root, new Vector3(0f, roofY - 0.25f, bodyZ + bodyLen / 2f + 0.02f), Vector3.forward, 1.7f, h, sign);
            SignQuad(root, new Vector3(0f, roofY - 0.25f, bodyZ - bodyLen / 2f - 0.02f), Vector3.back, 1.7f, h, sign);
        }

        // ---------- Patio, jardín delantero y vida alrededor ----------

        static void BuildSchoolYard(Transform parent, float entranceX, System.Random rng)
        {
            var root = new GameObject("Patio").transform;
            root.SetParent(parent, false);
            var bark = LoadMat("Corteza");
            var wood = Mat("MaderaBanco", new Color(0.4f, 0.28f, 0.18f));
            var galvanized = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));

            // Mástil con bandera de franjas (sin escudo ni texto) delante del portal.
            var flagPos = new Vector3(entranceX + 11f, LawnTop, 65f);
            Box("BaseMastil", root, flagPos + Vector3.up * 0.2f, new Vector3(1.2f, 0.4f, 1.2f), concreteMat);
            Primitive(PrimitiveType.Cylinder, "Mastil", root, flagPos + Vector3.up * 5.6f, new Vector3(0.14f, 5.4f, 0.14f), galvanized, keepCollider: true);
            var red = Mat("BanderaRoja", new Color(0.6f, 0.08f, 0.1f));
            var white = Mat("BanderaBlanca", new Color(0.85f, 0.85f, 0.82f));
            var blue = Mat("BanderaAzul", new Color(0.1f, 0.14f, 0.35f));
            for (int i = 0; i < 7; i++)
                Box("Franja", root, flagPos + new Vector3(1.25f, 9.7f + i * 0.2f, 0f), new Vector3(2.3f, 0.2f, 0.02f), i % 2 == 0 ? red : white, collider: false);
            Box("Canton", root, flagPos + new Vector3(0.7f, 10.4f, 0.015f), new Vector3(1.05f, 0.8f, 0.02f), blue, collider: false);

            // Cartel de bienvenida junto a la acera norte (base de ladrillo y panel con dos caras).
            var marquee = new Vector3(entranceX - 18f, LawnTop, 66f);
            Box("BaseCartel", root, marquee + Vector3.up * 0.5f, new Vector3(4.6f, 1f, 0.7f), Mat("LadrilloInstituto", new Color(0.55f, 0.43f, 0.3f)));
            Box("PanelCartel", root, marquee + Vector3.up * 1.95f, new Vector3(4.4f, 1.9f, 0.4f), Mat("PanelBlanco", new Color(0.82f, 0.82f, 0.8f)), collider: false);
            var title = SignMat("Letrero_Instituto", "HIGH SCHOOL", new Color(0.4f, 0.1f, 0.1f), new Color(0.95f, 0.9f, 0.7f), 103, 0.3f);
            var welcome = SignMat("Letrero_Bienvenida", "WELCOME BACK", new Color(0.9f, 0.9f, 0.88f), new Color(0.08f, 0.08f, 0.1f), 104, 0.3f);
            var game = SignMat("Letrero_Partido", "GAME FRIDAY", new Color(0.9f, 0.9f, 0.88f), new Color(0.08f, 0.08f, 0.1f), 105, 0.3f);
            foreach (float s in new[] { 1f, -1f })
            {
                var n = new Vector3(0f, 0f, s);
                SignQuad(root, marquee + new Vector3(0f, 2.55f, s * 0.21f), n, 4f, 4f / ((float)title.mainTexture.width / title.mainTexture.height), title);
                SignQuad(root, marquee + new Vector3(0f, 1.9f, s * 0.21f), n, 3.4f, 3.4f / ((float)welcome.mainTexture.width / welcome.mainTexture.height), welcome);
                SignQuad(root, marquee + new Vector3(0f, 1.4f, s * 0.21f), n, 3.1f, 3.1f / ((float)game.mainTexture.width / game.mainTexture.height), game);
            }

            // Aparcabicis, papeleras, cámara de vigilancia y bancos delante; bancos y jardineras en el patio trasero.
            BikeRack(root, new Vector3(entranceX + 6f, WalkTop, 57.7f), galvanized);
            TrashCan(root, new Vector3(entranceX - 6.5f, WalkTop, 57.8f), 0f, false);
            TrashCan(root, new Vector3(-196f, WalkTop, 61.9f), 0f, rng.NextDouble() < 0.3);
            SecurityCamera(root, new Vector3(entranceX - 9f, LawnTop, 59.5f), Mat("Poste", new Color(0.25f, 0.27f, 0.28f)), EmissiveMat("LuzAzulCamara", new Color(0.15f, 0.35f, 1f), 3f));
            for (float x = -240f; x < -160f; x += 14f)
            {
                if (x > -233f && x < -213f) continue; // delante del ala
                Bench(root, new Vector3(x, WalkTop, 18.4f), 0f, wood, concreteMat); // mira al campo
            }
            for (float x = -236f; x < -160f; x += 19f)
                TrashCan(root, new Vector3(x + 6f, WalkTop, 10f), 0f, rng.NextDouble() < 0.25);
            Box("MesaPicnic", root, new Vector3(-205f, WalkTop + 0.75f, 13f), new Vector3(2f, 0.08f, 0.9f), wood);
            foreach (float s in new[] { -0.75f, 0.75f })
                Box("BancoPicnic", root, new Vector3(-205f, WalkTop + 0.45f, 13f + s), new Vector3(2f, 0.06f, 0.3f), wood, collider: false);

            // Zona de servicio: contenedores, palés y bolsas.
            var yard = SchoolServiceYard;
            for (int i = 0; i < 3; i++)
                Dumpster(root, new Vector3(yard.xMin + 3f + i * 3.2f, LotTop, yard.yMin + 2f), 0f, rng.NextDouble() < 0.5);
            for (int i = 0; i < 6; i++)
                GarbageBag(root, new Vector3(Range(rng, yard.xMin + 1f, yard.xMax - 1f), LotTop, Range(rng, yard.yMin + 3.5f, yard.yMin + 5f)), rng);
            var pallet = Mat("Pale", new Color(0.55f, 0.45f, 0.32f));
            for (int k = 0; k < 3; k++)
                Box("Pale", root, new Vector3(yard.xMax - 2f, LotTop + 0.07f + k * 0.14f, yard.yMax - 4.5f), new Vector3(1.2f, 0.13f, 1f), pallet, collider: false);

            // Árboles: fila en el jardín delantero, alrededor del patio y junto a la autopista.
            var trees = new List<Vector3>
            {
                new(-262f, LawnTop, 64f), new(-248f, LawnTop, 64.5f), new(-236f, LawnTop, 62f), new(-192f, LawnTop, 65f), new(-178f, LawnTop, 64f),
                new(-163f, LawnTop, 65f), new(-150f, LawnTop, 66f), new(-268f, LawnTop, 48f), new(-268f, LawnTop, 30f), new(-268f, LawnTop, 14f),
                new(-258f, LawnTop, 12f), new(-240f, LawnTop, 22f), new(-205f, LawnTop, 30f), new(-185f, LawnTop, 26f), new(-158f, LawnTop, 8f),
                new(-146f, LawnTop, 34f), new(-146f, LawnTop, 52f),
            };
            foreach (var p in trees) DistrictTree(root, p, bark, rng, Range(rng, 1f, 1.4f), rng);

            // Farolas de sodio en las aceras del instituto.
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            for (float x = -262f; x < -125f; x += 32f)
            {
                StreetLight(root, new Vector3(x, 0f, 72.4f), Vector3.forward, pole, lamp);
                StreetLight(root, new Vector3(x + 16f, 0f, -72.4f), Vector3.back, pole, lamp);
            }
            foreach (float z in new[] { -55f, -25f, 30f, 48f })
                StreetLight(root, new Vector3(-117.6f, 0f, z), Vector3.right, pole, lamp);

            // Nieve en el césped, montones en el bordillo, basura y barro donde los chavales atajan.
            for (int i = 0; i < 45; i++)
            {
                var p = new Vector3(Range(rng, SchoolArea.xMin + 1f, SchoolArea.xMax - 1f), LawnTop, Range(rng, SchoolArea.yMin + 1f, SchoolArea.yMax - 1f));
                if (InsideAny(schoolOccupied, p)) continue;
                SnowPatch(root, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 4f), Range(rng, 1f, 3f)));
            }
            for (int i = 0; i < 16; i++)
            {
                bool north = rng.Next(2) == 0;
                SnowMound(root, new Vector3(Range(rng, -270f, -125f), SidewalkTop, north ? 72.4f : -72.4f), new Vector3(Range(rng, 1.4f, 3f), Range(rng, 0.25f, 0.5f), 0.8f), 0f);
            }
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            for (int i = 0; i < 70; i++)
            {
                var p = i < 30
                    ? new Vector3(Range(rng, -272f, -122f), SidewalkTop, (rng.Next(2) == 0 ? 1f : -1f) * Range(rng, 70.3f, 72.8f))
                    : new Vector3(Range(rng, SchoolArea.xMin + 1f, SchoolArea.xMax - 1f), LawnTop, Range(rng, 6f, 69f));
                if (i >= 30 && InsideAny(schoolOccupied, p)) continue;
                PlaceLitter(root, rng.Next(2) == 0 ? paper : can, p, rng);
            }
            var mud = LoadMat("Barro");
            if (mud)
            {
                DesirePath(root, new Vector3(-170f, LawnTop, 69f), new Vector3(-160f, LawnTop, 61.6f), mud, rng); // atajo de la acera norte al camino del gimnasio
                DesirePath(root, new Vector3(-252f, LawnTop, 20f), new Vector3(-262f, LawnTop, 6f), mud, rng);   // por detrás, hacia el campo
            }
        }
    }
}
