using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Franja de negocios al oeste de la avenida, en el lado del barrio de casas oeste que da a la manzana central
    /// (calle norte). Locales sueltos de una o dos plantas con huecos entre ellos: iglesia de local, lavandería,
    /// aparcamiento de clientes, barbería, pollo y pescado frito, solar vallado, cambio de cheques y la tienda de
    /// comida y licores de la esquina. Letreros con palabras genéricas (ProceduralTextures.Sign), sin marcas.
    /// También pone la acera oeste de la avenida al norte de la calle norte.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Franja: x ∈ [ShopsMinX, ShopsMaxX], z ∈ [ShopsMinZ, ShopsMaxZ]. Detrás (z ≥ 102.5) va el callejón a4 de la copia oeste.
        const float ShopsMinX = -30f, ShopsMaxX = 100f, ShopsMinZ = 90f, ShopsMaxZ = 102f;
        const float ShopsFront = 93f;   // línea de fachada; entre la acera de la calle (z 90) y aquí, acera ancha
        const float ShopsDepth = 7.5f;  // fondo de los locales (hasta z 100.5); detrás, un patio de servicio

        // Pasaje que une el callejón a5 de la copia oeste con la calle norte (corta la acera).
        static readonly Rect ShopsAlleyLink = Rect.MinMaxRect(-26f, 87f, -21f, 102.5f);
        // Aparcamiento de clientes y su entrada desde la calle norte.
        static readonly Rect ShopsLot = Rect.MinMaxRect(10f, 90f, 26f, 101.5f);
        static readonly Rect ShopsLotDriveway = Rect.MinMaxRect(12f, 87f, 24f, 90f);
        // Solar vallado entre el pollo frito y el cambio de cheques.
        static readonly Rect ShopsVacant = Rect.MinMaxRect(54f, 93f, 64f, 102f);
        // Acera oeste de la avenida, al norte de la calle norte (se corta donde entran las calles de la copia oeste).
        static readonly Rect AvenueWestSidewalk = Rect.MinMaxRect(100f, 90f, 103f, 300f);

        enum ShopKind { Church, Laundry, Barber, Chicken, Checks, Market }

        // De oeste a este. floors: 1 o 2 plantas.
        static readonly (ShopKind kind, float x0, float x1, int floors)[] Shops =
        {
            (ShopKind.Church, -18f, -6f, 1),
            (ShopKind.Laundry, -2f, 8f, 1),
            (ShopKind.Barber, 28f, 37f, 2),
            (ShopKind.Chicken, 42f, 54f, 1),
            (ShopKind.Checks, 64f, 74f, 2),
            (ShopKind.Market, 79f, 99f, 1),
        };

        /// <summary>Rectángulo local de la copia oeste del barrio de casas → rectángulo del mundo (giro de 180°).</summary>
        static Rect HousesWestRect(Rect r) =>
            Rect.MinMaxRect(HousesWestOrigin.x - r.xMax, HousesWestOrigin.z - r.yMax, HousesWestOrigin.x - r.xMin, HousesWestOrigin.z - r.yMin);

        static Material shopLitGlass, shopShutter;

        static void BuildShopsStrip(Transform world)
        {
            var group = new GameObject("Negocios_Oeste").transform;
            group.SetParent(world, false);
            housesFrame = null; // las huellas de los locales se guardan en coordenadas del mundo
            var rng = new System.Random(2010);

            shopLitGlass = EmissiveMat("EscaparateLuz", new Color(0.85f, 0.78f, 0.55f), 0.9f);
            shopShutter = TexturedMat("PersianaMetalica", ProceduralTextures.Shutter("PersianaMetalica", 61));

            BuildShopsGround(group);
            foreach (var (kind, x0, x1, floors) in Shops) Storefront(group, kind, x0, x1, floors, rng);
            BuildShopsLot(group, rng);

            // Solar entre locales: el mismo de las filas de casas, en un marco con la parcela al norte.
            var vacant = new GameObject("Solar").transform;
            vacant.SetParent(group, false);
            vacant.localPosition = new Vector3(0f, 0f, ShopsVacant.yMin);
            VacantLot(vacant, rng, ShopsVacant.xMin, ShopsVacant.xMax, ShopsVacant.height);

            BuildShopsStreetLife(group, rng);
        }

        // ---------- Suelo ----------

        static void BuildShopsGround(Transform parent)
        {
            var group = new GameObject("Suelo").transform;
            group.SetParent(parent, false);
            var holes = new List<Rect> { ShopsAlleyLink, ShopsLot };

            // Acera ancha delante de los locales (la de la calle llega a z 90; esta sigue hasta la fachada).
            foreach (var piece in StripMinus(Rect.MinMaxRect(ShopsMinX, ShopsMinZ, ShopsMaxX, ShopsFront), holes))
                TexturedBox("AceraNegocios", group, new Vector3(piece.center.x, SidewalkTop / 2f, piece.center.y), new Vector3(piece.width, SidewalkTop, piece.height),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);

            // Detrás: césped seco (bajo los locales, en los huecos y en el solar) y patio de servicio de hormigón sucio.
            foreach (var piece in StripMinus(Rect.MinMaxRect(ShopsMinX, ShopsFront, ShopsMaxX, ShopsMaxZ), holes))
                TexturedBox("Cesped", group, new Vector3(piece.center.x, LawnTop / 2f, piece.center.y), new Vector3(piece.width, LawnTop, piece.height),
                    lawnMat, Vector2.one * ProceduralTextures.GrassTile);
            foreach (var (_, x0, x1, _) in Shops)
            {
                float z0 = ShopsFront + ShopsDepth;
                TexturedBox("Patio", group, new Vector3((x0 + x1) / 2f, AlleyTop / 2f, (z0 + ShopsMaxZ) / 2f), new Vector3(x1 - x0, AlleyTop, ShopsMaxZ - z0),
                    alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
            }

            // Pasaje al callejón (cruza la acera de la calle norte, que se corta ahí en BuildStreets).
            var link = TexturedBox("Pasaje", group, new Vector3(ShopsAlleyLink.center.x, AlleyTop / 2f, ShopsAlleyLink.center.y),
                new Vector3(ShopsAlleyLink.width, AlleyTop, ShopsAlleyLink.height), alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
            link.name = "Pasaje_Callejon";

            // Acera oeste de la avenida, con huecos donde entran las calles A y C1 de la copia oeste.
            var cutters = new List<Rect>();
            foreach (var r in HousesRoads)
                if (r.xMin < 118f) cutters.Add(HousesWestRect(r)); // las que salen de la avenida
            var concrete = Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            foreach (var piece in StripMinus(AvenueWestSidewalk, cutters))
                Box("AceraAvenida_O", group, new Vector3(piece.center.x, 0.075f, piece.center.y), new Vector3(piece.width, 0.15f, piece.height), concrete);
        }

        // ---------- Locales ----------

        /// <summary>
        /// Local de una o dos plantas con la fachada en z = ShopsFront mirando a la calle (−Z). Planta baja: frente
        /// pintado, escaparates (iluminados si está abierto), rejas o persiana, letrero luminoso y toldo según el tipo.
        /// </summary>
        static void Storefront(Transform parent, ShopKind kind, float x0, float x1, int floors, System.Random rng)
        {
            var root = new GameObject($"Local_{kind}").transform;
            root.SetParent(parent, false);
            float w = x1 - x0, d = ShopsDepth, h = floors == 1 ? 4.6f : 7.4f;
            root.localPosition = new Vector3((x0 + x1) / 2f, 0f, ShopsFront);
            float hw = w / 2f;
            const float f = -0.06f; // delante de la fachada

            // Muros, tejado y pretil.
            var facade = kind switch
            {
                ShopKind.Barber => greystone,
                ShopKind.Checks => aptFacadeB,
                ShopKind.Market => houseFacades[2],
                _ => houseFacades[rng.Next(2)],
            };
            TexturedBox("Muros", root, new Vector3(0f, h / 2f, d / 2f), new Vector3(w, h, d), facade, new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor), sidesOnly: true);
            Box("Tejado", root, new Vector3(0f, h + 0.05f, d / 2f), new Vector3(w, 0.1f, d), tarRoof, collider: false);
            Box("Pretil", root, new Vector3(0f, h + 0.45f, 0.15f), new Vector3(w + 0.2f, 0.9f, 0.3f), stoneTrim, collider: false);
            Box("PretilAtras", root, new Vector3(0f, h + 0.25f, d - 0.12f), new Vector3(w, 0.5f, 0.24f), stoneTrim, collider: false);

            // Frente de la planta baja: panel pintado que tapa las ventanas de la textura de ladrillo.
            var (paint, signText, signBg, signInk) = kind switch
            {
                ShopKind.Church => (Mat("FrenteIglesia", new Color(0.78f, 0.76f, 0.7f)), "NEW HOPE CHURCH", new Color(0.92f, 0.9f, 0.84f), new Color(0.1f, 0.12f, 0.3f)),
                ShopKind.Laundry => (Mat("FrenteLavanderia", new Color(0.18f, 0.35f, 0.55f)), "COIN LAUNDRY", new Color(0.15f, 0.35f, 0.7f), new Color(0.95f, 0.95f, 0.9f)),
                ShopKind.Barber => (Mat("FrenteBarberia", new Color(0.1f, 0.1f, 0.12f)), "BARBER SHOP", new Color(0.08f, 0.08f, 0.1f), new Color(0.95f, 0.85f, 0.35f)),
                ShopKind.Chicken => (Mat("FrentePollo", new Color(0.55f, 0.12f, 0.08f)), "CHICKEN & FISH", new Color(0.95f, 0.8f, 0.2f), new Color(0.7f, 0.08f, 0.05f)),
                ShopKind.Checks => (Mat("FrenteCheques", new Color(0.12f, 0.2f, 0.4f)), "CHECKS CASHED", new Color(0.95f, 0.85f, 0.15f), new Color(0.1f, 0.15f, 0.45f)),
                _ => (Mat("FrenteTienda", new Color(0.15f, 0.3f, 0.18f)), "FOOD & LIQUOR", new Color(0.95f, 0.93f, 0.85f), new Color(0.72f, 0.08f, 0.06f)),
            };
            Box("Frente", root, new Vector3(0f, 2.05f, -0.04f), new Vector3(w, 4.1f, 0.08f), paint, collider: false);

            // Puerta a un lado y escaparate en el resto. La iglesia tiene las lunas tapadas con cortinas blancas.
            bool doorLeft = rng.Next(2) == 0;
            float doorX = doorLeft ? -hw + 1.4f : hw - 1.4f;
            float winX0 = doorLeft ? doorX + 1.1f : -hw + 0.5f, winX1 = doorLeft ? hw - 0.5f : doorX - 1.1f;
            float winW = winX1 - winX0, winC = (winX0 + winX1) / 2f;
            var glass = kind == ShopKind.Church ? Mat("CortinaBlanca", new Color(0.86f, 0.85f, 0.8f)) : shopLitGlass;
            Box("Escaparate", root, new Vector3(winC, 1.75f, f - 0.02f), new Vector3(winW, 2.3f, 0.04f), glass, collider: false);
            Box("Zocalo", root, new Vector3(winC, 0.3f, f - 0.05f), new Vector3(winW + 0.2f, 0.6f, 0.1f), stoneTrim, collider: false);
            Box("Puerta", root, new Vector3(doorX, 1.2f, f - 0.02f), new Vector3(1.1f, 2.4f, 0.04f), kind == ShopKind.Church ? doorMat : darkGlass, collider: false);
            Box("MarcoPuerta", root, new Vector3(doorX, 2.45f, f - 0.05f), new Vector3(1.3f, 0.1f, 0.1f), ironMat, collider: false);

            // Rejas delante del cristal (cambio de cheques, tienda, pollo) o persiana medio bajada (barbería, lavandería).
            if (kind is ShopKind.Checks or ShopKind.Market or ShopKind.Chicken)
            {
                for (float x = winX0 + 0.15f; x < winX1; x += 0.3f)
                    Box("Reja", root, new Vector3(x, 1.75f, f - 0.12f), new Vector3(0.03f, 2.3f, 0.03f), ironMat, collider: false);
                foreach (float y in new[] { 0.65f, 2.85f })
                    Box("RejaTravesano", root, new Vector3(winC, y, f - 0.12f), new Vector3(winW, 0.05f, 0.04f), ironMat, collider: false);
            }
            // Caja de la persiana encima del escaparate.
            Box("CajaPersiana", root, new Vector3(winC, 3.05f, f - 0.12f), new Vector3(winW + 0.3f, 0.32f, 0.26f), shopShutter, collider: false);
            if ((kind is ShopKind.Barber or ShopKind.Laundry) && rng.NextDouble() < 0.7)
            {
                float down = Range(rng, 0.3f, 0.7f);
                var shutter = TexturedBox("Persiana", root, new Vector3(winC, 2.9f - down / 2f, f - 0.1f), new Vector3(winW, down, 0.03f), shopShutter, new Vector2(winW, 1f), collider: false);
                shutter.name = "PersianaBajada";
            }

            // Letrero luminoso encima de la puerta y el escaparate.
            var sign = SignMat($"Letrero_{kind}", signText, signBg, signInk, 70 + (int)kind, 0.9f);
            float aspect = (float)sign.mainTexture.width / sign.mainTexture.height;
            float signH = Mathf.Min(0.95f, (w - 1f) / aspect), signW = signH * aspect;
            float signY = 3.78f;
            Box("CajaLetrero", root, new Vector3(0f, signY, f - 0.12f), new Vector3(signW + 0.2f, signH + 0.16f, 0.22f), ironMat, collider: false);
            SignQuad(root, new Vector3(0f, signY, f - 0.235f), Vector3.back, signW, signH, sign);

            // Cartel de OPEN de neón en el escaparate (no en la iglesia).
            if (kind != ShopKind.Church)
            {
                var open = SignMat("Neon_Open", "OPEN", new Color(0.05f, 0.03f, 0.04f), new Color(1f, 0.15f, 0.2f), 77, 2.2f);
                float ox = doorLeft ? winX0 + 0.8f : winX1 - 0.8f;
                SignQuad(root, new Vector3(ox, 2.3f, f - 0.05f), Vector3.back, 1f, 1f / ((float)open.mainTexture.width / open.mainTexture.height), open);
            }

            // Toldo inclinado.
            if (kind is ShopKind.Laundry or ShopKind.Barber or ShopKind.Chicken)
            {
                var awningMat = kind switch
                {
                    ShopKind.Laundry => Mat("ToldoAzul", new Color(0.12f, 0.25f, 0.5f)),
                    ShopKind.Barber => Mat("ToldoNegro", new Color(0.08f, 0.08f, 0.09f)),
                    _ => Mat("ToldoRojo", new Color(0.5f, 0.08f, 0.06f)),
                };
                var awning = Box("Toldo", root, new Vector3(winC, 3.08f, f - 0.65f), new Vector3(winW + 0.4f, 0.06f, 1.3f), awningMat, collider: false);
                awning.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
                Box("ToldoFaldon", root, new Vector3(winC, 2.7f, f - 1.25f), new Vector3(winW + 0.4f, 0.28f, 0.03f), awningMat, collider: false);
            }

            // Segunda planta: la textura de ladrillo ya trae ventanas; aire acondicionado de ventana en alguna.
            if (floors == 2)
            {
                Box("Imposta", root, new Vector3(0f, 4.2f, -0.08f), new Vector3(w + 0.1f, 0.18f, 0.16f), stoneTrim, collider: false);
                var ac = Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f));
                for (float x = -hw + 1.5f; x < hw - 1f; x += ProceduralTextures.FacadeBay)
                    if (rng.NextDouble() < 0.4)
                        Box("AireVentana", root, new Vector3(x, 5.3f, -0.3f), new Vector3(0.7f, 0.45f, 0.55f), ac, collider: false);
            }

            // Detalles de cada negocio.
            switch (kind)
            {
                case ShopKind.Church:
                    // Cruz blanca encima del letrero y dos lunas con cartel de horario.
                    var white = Mat("CruzBlanca", new Color(0.92f, 0.92f, 0.88f));
                    Box("Cruz", root, new Vector3(0f, h + 1.4f, 0.15f), new Vector3(0.18f, 1.6f, 0.12f), white, collider: false);
                    Box("CruzBrazo", root, new Vector3(0f, h + 1.7f, 0.15f), new Vector3(0.9f, 0.18f, 0.12f), white, collider: false);
                    var hours = SignMat("Letrero_Domingo", "SUNDAY SERVICE", new Color(0.92f, 0.9f, 0.84f), new Color(0.1f, 0.1f, 0.12f), 79, 0.3f);
                    SignQuad(root, new Vector3(winC, 2.2f, f - 0.05f), Vector3.back, 2.2f, 2.2f / ((float)hours.mainTexture.width / hours.mainTexture.height), hours);
                    break;

                case ShopKind.Barber:
                    // Poste de barbería en un soporte junto a la puerta, con remates blancos.
                    var poleMat = TexturedMat("PosteBarberia", ProceduralTextures.BarberPole("PosteBarberia"));
                    poleMat.EnableKeyword("_EMISSION");
                    poleMat.SetTexture("_EmissionMap", poleMat.mainTexture);
                    poleMat.SetColor("_EmissionColor", Color.white * 0.6f);
                    EditorUtility.SetDirty(poleMat);
                    float px = doorLeft ? doorX - 0.85f : doorX + 0.85f;
                    Primitive(PrimitiveType.Cylinder, "PosteBarberia", root, new Vector3(px, 2.3f, f - 0.3f), new Vector3(0.2f, 0.42f, 0.2f), poleMat, keepCollider: false);
                    var cap = Mat("Cromado", new Color(0.8f, 0.8f, 0.82f));
                    Primitive(PrimitiveType.Sphere, "Remate", root, new Vector3(px, 2.78f, f - 0.3f), Vector3.one * 0.24f, cap, keepCollider: false);
                    Box("RemateBase", root, new Vector3(px, 1.84f, f - 0.3f), new Vector3(0.24f, 0.08f, 0.24f), cap, collider: false);
                    Box("Soporte", root, new Vector3(px, 2.3f, f - 0.15f), new Vector3(0.06f, 0.06f, 0.3f), cap, collider: false);
                    break;

                case ShopKind.Chicken:
                    // Extractor en el tejado y manchas de grasa en la acera.
                    var steel = Mat("ChapaGalvanizada", new Color(0.55f, 0.56f, 0.57f));
                    Primitive(PrimitiveType.Cylinder, "Extractor", root, new Vector3(hw - 2f, h + 0.9f, d - 2f), new Vector3(0.6f, 0.8f, 0.6f), steel, keepCollider: false);
                    Box("ExtractorTapa", root, new Vector3(hw - 2f, h + 1.75f, d - 2f), new Vector3(0.9f, 0.08f, 0.9f), steel, collider: false);
                    var oil = LoadMat("Aceite");
                    if (oil)
                        for (int i = 0; i < 3; i++)
                            Decal(root, new Vector3(Range(rng, -hw + 1f, hw - 1f), SidewalkTop + 0.003f + i * 0.0003f, Range(rng, -2.5f, -0.8f)), Range(rng, 0f, 360f),
                                new Vector2(Range(rng, 0.6f, 1.2f), Range(rng, 0.6f, 1.2f)), oil);
                    // Mural en la pared que da al solar.
                    if (graffitiMats[2]) WallGraffiti(root, new Vector3(hw, 2.2f, d / 2f), Vector3.right, 5.5f, graffitiMats[2]);
                    break;

                case ShopKind.Checks:
                    // Cartel pequeño de móviles en el escaparate.
                    var phones = SignMat("Letrero_Moviles", "CELL PHONES", new Color(0.1f, 0.1f, 0.12f), new Color(0.3f, 0.9f, 0.4f), 78, 1.6f);
                    float pw = Mathf.Min(2.4f, winW - 1.8f);
                    SignQuad(root, new Vector3(winC, 1.3f, f - 0.05f), Vector3.back, pw, pw / ((float)phones.mainTexture.width / phones.mainTexture.height), phones);
                    break;

                case ShopKind.Market:
                    // Segundo letrero hacia la avenida, carteles de ofertas en el cristal y arcón de hielo en la acera.
                    var grocery = SignMat("Letrero_Grocery", "GROCERY", new Color(0.95f, 0.93f, 0.85f), new Color(0.72f, 0.08f, 0.06f), 76, 0.9f);
                    float gAspect = (float)grocery.mainTexture.width / grocery.mainTexture.height;
                    Box("CajaLetreroLado", root, new Vector3(hw + 0.12f, 3.6f, d / 2f), new Vector3(0.22f, 1.06f, 0.9f * gAspect + 0.2f), ironMat, collider: false);
                    SignQuad(root, new Vector3(hw + 0.235f, 3.6f, d / 2f), Vector3.right, 0.9f * gAspect, 0.9f, grocery);
                    var posters = new[] { Mat("CartelAmarillo", new Color(0.9f, 0.8f, 0.2f)), Mat("CartelRojo", new Color(0.75f, 0.12f, 0.1f)), Mat("CartelBlanco", new Color(0.9f, 0.9f, 0.86f)) };
                    for (float x = winX0 + 0.8f; x < winX1 - 0.8f; x += Range(rng, 1.4f, 2.4f))
                        Box("Cartel", root, new Vector3(x, Range(rng, 1.3f, 2.3f), f - 0.05f), new Vector3(Range(rng, 0.5f, 0.9f), Range(rng, 0.6f, 0.9f), 0.01f), posters[rng.Next(posters.Length)], collider: false);
                    var ice = SignMat("Letrero_Ice", "ICE", new Color(0.15f, 0.4f, 0.8f), new Color(0.95f, 0.97f, 1f), 75, 0.4f);
                    float iceX = doorLeft ? hw - 2f : -hw + 2f;
                    Box("ArconHielo", root, new Vector3(iceX, SidewalkTop + 0.65f, -0.6f), new Vector3(1.8f, 1.3f, 0.8f), Mat("ArconBlanco", new Color(0.85f, 0.86f, 0.86f)));
                    SignQuad(root, new Vector3(iceX, SidewalkTop + 0.8f, -1.01f), Vector3.back, 1.2f, 1.2f / ((float)ice.mainTexture.width / ice.mainTexture.height), ice);
                    break;
            }

            // Detrás: puerta de chapa, cubos y contenedor en el patio de servicio.
            Box("PuertaTrasera", root, new Vector3(Range(rng, -hw + 1f, hw - 1f), 1.1f, d + 0.04f), new Vector3(1f, 2.2f, 0.06f), Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f)), collider: false);
            if (w >= 11f) Dumpster(root, new Vector3(hw - 1.6f, AlleyTop, d + 0.8f), 0f, rng.NextDouble() < 0.4);
            else TrashCart(root, new Vector3(-hw + 0.8f, AlleyTop, d + 0.8f), Range(rng, -15f, 15f), binColors[rng.Next(binColors.Length)], false);
            for (int i = 0; i < rng.Next(1, 4); i++) GarbageBag(root, new Vector3(Range(rng, -hw + 0.5f, hw - 0.5f), AlleyTop, d + Range(rng, 0.5f, 1.3f)), rng);

            // Unidad de aire en el tejado.
            if (rng.NextDouble() < 0.7)
                Box("AireTejado", root, new Vector3(Range(rng, -hw + 1.5f, hw - 1.5f), h + 0.55f, d * 0.6f), new Vector3(1.4f, 0.9f, 1.1f), Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f)));

            RecordFootprint(root, "tienda", -hw, 0f, hw, d, h + 0.9f);
        }

        /// <summary>Material de letrero: la textura también es el mapa de emisión, así brilla de noche y en la niebla.</summary>
        static Material SignMat(string name, string text, Color background, Color ink, int seed, float glow)
        {
            var texture = ProceduralTextures.Sign(name, text, background, ink, seed);
            var material = TexturedMat(name, texture);
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", Color.white * glow);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Quad con un letrero, visible desde el lado de <paramref name="normal"/> (el texto se lee bien desde ahí).</summary>
        static void SignQuad(Transform parent, Vector3 center, Vector3 normal, float width, float height, Material material)
        {
            var quad = Primitive(PrimitiveType.Quad, "Letrero", parent, center, new Vector3(width, height, 1f), material, keepCollider: false);
            quad.transform.localRotation = Quaternion.LookRotation(-normal);
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Aparcamiento de clientes ----------

        static void BuildShopsLot(Transform parent, System.Random rng)
        {
            var group = new GameObject("Aparcamiento_Clientes").transform;
            group.SetParent(parent, false);
            var paint = Mat("LineaAparcamiento", new Color(0.82f, 0.82f, 0.78f));

            // Asfalto desde el bordillo de la calle (la acera se corta en la entrada) hasta el fondo.
            var area = Rect.MinMaxRect(ShopsLot.xMin, ShopsLotDriveway.yMin, ShopsLot.xMax, ShopsLot.yMax);
            TexturedBox("Asfalto", group, new Vector3(area.center.x, LotTop / 2f, area.center.y), new Vector3(area.width, LotTop, area.height),
                alleyMat, Vector2.one * ProceduralTextures.AlleyTile);

            // Una fila de 6 plazas al fondo, de morro contra la valla; el pasillo es lo que queda hasta la calle.
            const float stall = 2.6f, stallDepth = 5f;
            int n = Mathf.FloorToInt((ShopsLot.width - 0.4f) / stall);
            float x0 = ShopsLot.center.x - n * stall / 2f, z0 = ShopsLot.yMax - stallDepth, lineY = LotTop + 0.005f;
            for (int i = 0; i <= n; i++)
                Box("Linea", group, new Vector3(x0 + i * stall, lineY, z0 + stallDepth / 2f), new Vector3(0.12f, 0.01f, stallDepth), paint, collider: false);
            for (int i = 0; i < n; i++)
            {
                float cx = x0 + (i + 0.5f) * stall;
                Box("Tope", group, new Vector3(cx, LotTop + 0.06f, ShopsLot.yMax - 0.7f), new Vector3(1.8f, 0.12f, 0.22f), concreteMat, collider: false);
                double roll = rng.NextDouble();
                if (roll < 0.5) ParkedCar(group, new Vector3(cx, LotTop, z0 + stallDepth / 2f), 90f + (rng.Next(2) == 0 ? 0f : 180f) + Range(rng, -4f, 4f), rng);
                else if (roll < 0.6) SnowMound(group, new Vector3(cx, LotTop, z0 + stallDepth / 2f), new Vector3(2.2f, Range(rng, 0.5f, 0.8f), 3.6f), Range(rng, -10f, 10f));
            }

            // Malla al fondo y en el lado oeste; cartel de solo clientes.
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            if (chainLink)
            {
                FenceSegment(group, new Vector3(ShopsLot.xMin + 0.2f, LotTop, ShopsLot.yMax - 0.15f), new Vector3(ShopsLot.xMax - 0.2f, LotTop, ShopsLot.yMax - 0.15f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
                FenceSegment(group, new Vector3(ShopsLot.xMin + 0.15f, LotTop, ShopsMinZ + 0.6f), new Vector3(ShopsLot.xMin + 0.15f, LotTop, ShopsLot.yMax - 0.2f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            }
            var only = SignMat("Letrero_Clientes", "CUSTOMER PARKING ONLY", new Color(0.92f, 0.92f, 0.9f), new Color(0.7f, 0.08f, 0.06f), 74, 0.2f);
            Primitive(PrimitiveType.Cylinder, "PosteCartel", group, new Vector3(ShopsLot.xMax - 0.5f, 1.2f, ShopsMinZ + 0.4f), new Vector3(0.07f, 1.2f, 0.07f), ironMat, keepCollider: true);
            SignQuad(group, new Vector3(ShopsLot.xMax - 0.5f, 2.2f, ShopsMinZ + 0.36f), Vector3.back, 1.6f, 1.6f / ((float)only.mainTexture.width / only.mainTexture.height), only);
            StreetLight(group, new Vector3(ShopsLot.xMax - 0.6f, LotTop - SidewalkTop, ShopsLot.yMax - 0.8f), Vector3.back, Mat("Poste", new Color(0.25f, 0.27f, 0.28f)), Mat("LuzSodio", SodiumLight));
            housesFootprints.Add(("aparcamiento", new[] { new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin), new Vector2(area.xMax, area.yMax), new Vector2(area.xMin, area.yMax) }, 0f));
        }

        // ---------- Calle: farolas, coches, nieve y basura ----------

        static void BuildShopsStreetLife(Transform parent, System.Random rng)
        {
            var group = new GameObject("Calle").transform;
            group.SetParent(parent, false);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);

            // Farolas de sodio en el bordillo de la calle norte y en la acera oeste de la avenida (entre los pilares del tren).
            foreach (float x in new[] { -16f, 35f, 62f, 92f }) StreetLight(group, new Vector3(x, 0f, 87.6f), Vector3.back, pole, lamp);
            foreach (float z in new[] { 110f, 140f, 170f, 215f, 245f }) StreetLight(group, new Vector3(100.5f, 0f, z), Vector3.right, pole, lamp);

            // Coches aparcados delante de los negocios, en el sentido de la marcha (hacia el oeste en este lado).
            foreach (float x in new[] { -24f, 40f, 84f })
                ParkedCar(group, new Vector3(x + Range(rng, -1f, 1f), RoadTop, BlockHalfZ + SidewalkWidth + RoadWidth - 1.5f), 180f, rng);

            // Bancos de nieve en el bordillo (sin tapar la entrada del aparcamiento ni el pasaje), papeleras y basura.
            for (float x = ShopsMinX + 2f; x < ShopsMaxX - 2f; x += Range(rng, 5f, 11f))
            {
                var p = new Vector3(x, SidewalkTop, 87.6f);
                if (InsideAny(new[] { ShopsLotDriveway, ShopsAlleyLink }, p) || rng.NextDouble() < 0.3) continue;
                SnowMound(group, p, new Vector3(Range(rng, 1.4f, 3f), Range(rng, 0.25f, 0.45f), 0.8f), 0f);
            }
            foreach (float x in new[] { -9f, 27f, 56f, 77f }) TrashCan(group, new Vector3(x, SidewalkTop, 90.4f), 0f, rng.NextDouble() < 0.15);
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            var cup = (name: "Vaso", material: Mat("BasuraVaso", new Color(0.85f, 0.2f, 0.15f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.09f, 0.07f, 0.09f));
            var kinds = new[] { paper, can, cup };
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(Range(rng, ShopsMinX + 0.5f, ShopsMaxX - 0.5f), SidewalkTop, Range(rng, 87.3f, ShopsFront - 0.6f));
                if (InsideAny(new[] { ShopsLot, ShopsLotDriveway, ShopsAlleyLink }, p)) continue;
                PlaceLitter(group, kinds[rng.Next(kinds.Length)], p, rng);
            }

            // Huecos entre locales: maleza, nieve a la sombra y alguna bolsa.
            float prev = ShopsMinX;
            foreach (var (_, x0, x1, _) in Shops)
            {
                if (x0 - prev > 2f)
                    for (int i = 0; i < 3; i++)
                    {
                        var p = new Vector3(Range(rng, prev + 0.6f, x0 - 0.6f), LawnTop, Range(rng, ShopsFront + 0.6f, ShopsMaxZ - 0.6f));
                        if (InsideAny(new[] { ShopsLot, ShopsAlleyLink, ShopsVacant }, p)) continue;
                        if (i == 0) SnowPatch(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.2f, 2.2f), Range(rng, 1f, 2f)));
                        else Clump(group, rng, p, 0.3f, 0.9f);
                    }
                prev = x1;
            }
        }
    }
}
