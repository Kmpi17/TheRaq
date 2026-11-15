using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Vida del bloque en la manzana central: el estudio casero en el sótano de T1 (centro de la progresión musical,
    /// con <see cref="RecordingStudio"/> en el micro), el memorial del eje E–O y la zona de reunión junto a la canasta.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Hueco de la escalera del sótano, al oeste de T1 (T1 ocupa x −75.5…−48.5, z 32.5…47.5) y al norte de su recinto de
        // contenedores (x −79.2…−76.2, z 33.6…38.8). Se recorta del
        // "Suelo" (BuildStreets) y del césped (BuildTowersGround).
        static readonly Rect StudioStairwell = Rect.MinMaxRect(-80.3f, 40f, -77.7f, 48.5f);
        const float StudioFloor = -3.2f, StudioCeiling = -0.2f;
        static readonly Rect StudioRoom = Rect.MinMaxRect(-77.5f, 38f, -63f, 49.5f);

        // ---------- Estudio en el sótano ----------

        static void BuildStudio(Transform parent)
        {
            var root = new GameObject("Estudio_Sotano_T1").transform;
            root.SetParent(parent, false);
            towersOccupied.Add(Grow(StudioStairwell, 1.5f));

            var concrete = concreteMat ? concreteMat : Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            var paint = Mat("ParedSotano", new Color(0.16f, 0.15f, 0.17f));
            var carpet = Mat("MoquetaEstudio", new Color(0.12f, 0.1f, 0.13f));
            var foam = Mat("EspumaAcustica", new Color(0.08f, 0.08f, 0.09f));
            var black = Mat("PlasticoNegro", new Color(0.03f, 0.03f, 0.035f));
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var purple = EmissiveMat("LedMorado", new Color(0.55f, 0.15f, 1f), 3f);
            var screen = EmissiveMat("PantallaDaw", new Color(0.25f, 0.45f, 0.75f), 1.6f);
            var red = EmissiveMat("LuzOnAir", new Color(1f, 0.1f, 0.08f), 3f);
            float h = StudioCeiling - StudioFloor;

            // --- Escalera: baja hacia el sur desde el césped hasta el rellano del sótano ---
            var s = StudioStairwell;
            float cx = s.center.x, w = s.width;
            var top = new Vector3(cx, LawnTop, s.yMax);
            float rise = LawnTop - StudioFloor;
            var bottom = new Vector3(cx, StudioFloor, s.yMax - rise * StairRunPerRise);
            var along = (bottom - top).normalized;
            var normal = Vector3.Cross(along, Vector3.Cross(Vector3.up, along)).normalized;
            if (normal.y < 0f) normal = -normal;
            var tilt = Quaternion.LookRotation(along, normal);
            float length = Vector3.Distance(top, bottom);
            ColliderBox(root, "RampaEscalera", (top + bottom) / 2f - normal * 0.15f - Vector3.up * 0.1f, new Vector3(w, 0.3f, length + 0.1f)).transform.localRotation = tilt;
            int steps = Mathf.RoundToInt(rise / 0.2f);
            float stepRise = rise / steps, stepRun = stepRise * StairRunPerRise;
            for (int k = 0; k < steps; k++)
            {
                float y = LawnTop - (k + 1) * stepRise;
                Box("Peldano", root, new Vector3(cx, (y + StudioFloor - 0.1f) / 2f + stepRise / 2f, top.z - k * stepRun - stepRun / 2f),
                    new Vector3(w, y - StudioFloor + 0.1f + stepRise - 0.02f, stepRun), concrete, collider: false);
            }
            // Rellano y paredes del hueco (llegan a ras del césped: bordillo de hormigón).
            Box("Rellano", root, new Vector3(cx, StudioFloor - 0.1f, (s.yMin + bottom.z) / 2f), new Vector3(w, 0.2f, bottom.z - s.yMin), concrete);
            float wallH = LawnTop + 0.05f - StudioFloor, wallY = StudioFloor + wallH / 2f;
            Box("ParedHueco_O", root, new Vector3(s.xMin - 0.1f, wallY, s.center.y), new Vector3(0.2f, wallH, s.height + 0.4f), concrete);
            Box("ParedHueco_S", root, new Vector3(cx, wallY, s.yMin - 0.1f), new Vector3(w + 0.4f, wallH, 0.2f), concrete);
            Box("ParedHueco_N", root, new Vector3(cx, StudioFloor + (StudioCeiling - StudioFloor + 0.2f) / 2f, s.yMax + 0.1f), new Vector3(w + 0.4f, h + 0.2f, 0.2f), concrete);
            Strut("Pasamanos", root, top + new Vector3(s.xMin - cx + 0.08f, 0.95f, 0f), bottom + new Vector3(s.xMin - cx + 0.08f, 0.95f, 0f), 0.04f, ironMat, cylinder: true);

            // Barandilla arriba, alrededor del hueco menos por el norte (por donde se baja).
            var rail = ironMat ? ironMat : black;
            foreach (var (a, b) in new[]
                     {
                         (new Vector3(s.xMin - 0.1f, LawnTop, s.yMin - 0.1f), new Vector3(s.xMin - 0.1f, LawnTop, s.yMax)),
                         (new Vector3(s.xMax + 0.1f, LawnTop, s.yMin - 0.1f), new Vector3(s.xMax + 0.1f, LawnTop, s.yMax)),
                         (new Vector3(s.xMin - 0.1f, LawnTop, s.yMin - 0.1f), new Vector3(s.xMax + 0.1f, LawnTop, s.yMin - 0.1f)),
                     })
            {
                Strut("BarandillaHueco", root, a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, 0.05f, rail, cylinder: true);
                Strut("BarandillaHueco", root, a + Vector3.up * 0.55f, b + Vector3.up * 0.55f, 0.03f, rail, cylinder: true);
                var c = (a + b) / 2f;
                ColliderBox(root, "LimiteHueco", c + Vector3.up * 0.6f, new Vector3(Mathf.Abs(b.x - a.x) + 0.1f, 1.2f, Mathf.Abs(b.z - a.z) + 0.1f));
                int posts = Mathf.CeilToInt(Vector3.Distance(a, b) / 1.5f);
                for (int i = 0; i <= posts; i++)
                    Primitive(PrimitiveType.Cylinder, "PosteBarandilla", root, Vector3.Lerp(a, b, (float)i / posts) + Vector3.up * 0.53f,
                        new Vector3(0.05f, 0.53f, 0.05f), rail, keepCollider: false);
            }
            if (graffitiMats != null && graffitiMats.Length > 0 && graffitiMats[0])
                WallGraffiti(root, new Vector3(s.xMin + 0.005f, StudioFloor + 1.6f, s.yMin + 1.6f), Vector3.right, 1.8f, graffitiMats[1 % graffitiMats.Length]);

            // --- Sala: paredes, suelo y techo acústico ---
            var r = StudioRoom;
            Box("SueloEstudio", root, new Vector3(r.center.x, StudioFloor - 0.1f, r.center.y), new Vector3(r.width, 0.2f, r.height), concrete);
            Box("Moqueta", root, new Vector3(r.center.x + 1f, StudioFloor + 0.01f, r.center.y), new Vector3(r.width - 4f, 0.02f, r.height - 2f), carpet, collider: false);
            Box("Techo", root, new Vector3(r.center.x, StudioCeiling - 0.03f, r.center.y), new Vector3(r.width, 0.06f, r.height), foam, collider: false);
            float yWall = StudioFloor + h / 2f;
            Box("Pared_E", root, new Vector3(r.xMax + 0.1f, yWall, r.center.y), new Vector3(0.2f, h, r.height + 0.4f), paint);
            Box("Pared_N", root, new Vector3(r.center.x, yWall, r.yMax + 0.1f), new Vector3(r.width + 0.4f, h, 0.2f), paint);
            Box("Pared_S", root, new Vector3(r.center.x, yWall, r.yMin - 0.1f), new Vector3(r.width + 0.4f, h, 0.2f), paint);
            // Pared oeste compartida con el hueco, con la puerta (abierta) al rellano.
            const float doorZ0 = 40.3f, doorZ1 = 42.4f, doorH = 2.2f;
            float wx = r.xMin - 0.1f;
            Box("Pared_O", root, new Vector3(wx, yWall, (r.yMin + doorZ0) / 2f), new Vector3(0.2f, h, doorZ0 - r.yMin), paint);
            Box("Pared_O", root, new Vector3(wx, yWall, (doorZ1 + r.yMax) / 2f), new Vector3(0.2f, h, r.yMax - doorZ1), paint);
            Box("Dintel", root, new Vector3(wx, (StudioFloor + doorH + StudioCeiling) / 2f, (doorZ0 + doorZ1) / 2f), new Vector3(0.2f, StudioCeiling - StudioFloor - doorH, doorZ1 - doorZ0), paint);
            Box("PuertaAbierta", root, new Vector3(wx + 0.5f, StudioFloor + doorH / 2f, doorZ0 + 0.05f), new Vector3(0.95f, doorH - 0.05f, 0.05f), Mat("PuertaMetal", new Color(0.3f, 0.31f, 0.32f)), collider: false);
            Box("LuzOnAir", root, new Vector3(wx - 0.12f, StudioFloor + doorH + 0.25f, (doorZ0 + doorZ1) / 2f), new Vector3(0.05f, 0.18f, 0.5f), red, collider: false);

            // Paneles de espuma en las paredes y tira de LED morado arriba.
            for (float z = r.yMin + 1f; z < r.yMax - 0.5f; z += 1.4f)
                Box("PanelEspuma", root, new Vector3(r.xMax - 0.04f, StudioFloor + 1.6f, z), new Vector3(0.08f, 0.9f, 0.9f), foam, collider: false);
            for (float x = r.xMin + 3.5f; x < r.xMax - 0.5f; x += 1.4f)
                Box("PanelEspuma", root, new Vector3(x, StudioFloor + 1.6f, r.yMax - 0.04f), new Vector3(0.9f, 0.9f, 0.08f), foam, collider: false);
            Box("TiraLed", root, new Vector3(r.center.x, StudioCeiling - 0.1f, r.yMax - 0.05f), new Vector3(r.width, 0.04f, 0.03f), purple, collider: false);
            Box("TiraLed", root, new Vector3(r.center.x, StudioCeiling - 0.1f, r.yMin + 0.05f), new Vector3(r.width, 0.04f, 0.03f), purple, collider: false);
            foreach (var (pos, col, range) in new[]
                     {
                         (new Vector3(-73f, StudioCeiling - 0.4f, 43.5f), new Color(0.6f, 0.25f, 1f), 9f),
                         (new Vector3(cx, LawnTop - 1f, 42f), new Color(1f, 0.75f, 0.45f), 5f),
                     })
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.localPosition = pos;
                l.type = LightType.Point;
                l.color = col;
                l.range = range;
                l.intensity = 1.8f;
                l.shadows = LightShadows.None;
            }

            // Cabina de voz en la esquina noreste: tabiques con cristal, micro con antipop y foco que late con el beat.
            const float bx0 = -67f;
            float bz0 = r.yMax - 4.5f;
            var glass = darkGlass ? darkGlass : Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            Box("TabiqueCabina", root, new Vector3(bx0, yWall, bz0 + 3f), new Vector3(0.15f, h, 3f), foam);
            Box("CristalCabina", root, new Vector3(bx0, StudioFloor + 1.5f, bz0 + 0.75f), new Vector3(0.05f, 1.2f, 1.5f), glass, collider: false);
            Box("TabiqueCabina", root, new Vector3(bx0, StudioFloor + 0.45f, bz0 + 0.75f), new Vector3(0.15f, 0.9f, 1.5f), foam);
            Box("TabiqueCabina", root, new Vector3((bx0 + r.xMax) / 2f + 0.9f, yWall, bz0), new Vector3(r.xMax - bx0 - 1.8f, h, 0.15f), foam);
            var mic = new Vector3(-64.6f, StudioFloor, r.yMax - 2f);
            Strut("PieMicro", root, mic, mic + Vector3.up * 1.55f, 0.03f, black, cylinder: true);
            Primitive(PrimitiveType.Cylinder, "BaseMicro", root, mic + Vector3.up * 0.02f, new Vector3(0.45f, 0.02f, 0.45f), black, keepCollider: false);
            Primitive(PrimitiveType.Capsule, "Micro", root, mic + new Vector3(-0.1f, 1.62f, 0f), new Vector3(0.07f, 0.1f, 0.07f), Mat("MetalMicro", new Color(0.6f, 0.6f, 0.62f)), keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Antipop", root, mic + new Vector3(-0.3f, 1.62f, 0f), new Vector3(0.22f, 0.005f, 0.22f), black, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var spot = new GameObject("FocoCabina").AddComponent<Light>();
            spot.transform.SetParent(root, false);
            spot.transform.localPosition = mic + new Vector3(0f, 2.6f, 0f);
            spot.type = LightType.Point;
            spot.color = new Color(1f, 0.3f, 0.45f);
            spot.range = 4f;
            spot.intensity = 1.2f;
            spot.shadows = LightShadows.None;
            var studio = new GameObject("Micro_Grabacion").AddComponent<RecordingStudio>();
            studio.transform.SetParent(root, false);
            studio.transform.localPosition = mic + new Vector3(-0.8f, 0f, 0f);
            var so = new UnityEditor.SerializedObject(studio);
            so.FindProperty("pulseLight").objectReferenceValue = spot;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Mesa de mezclas mirando a la cabina: monitores, pantallas, teclado MIDI, silla de oficina.
            var desk = new Vector3(-70.5f, StudioFloor, 46f);
            Box("Mesa", root, desk + new Vector3(0f, 0.75f, 0f), new Vector3(2.8f, 0.06f, 0.9f), wood);
            foreach (float dx in new[] { -1.3f, 1.3f })
                Box("PataMesa", root, desk + new Vector3(dx, 0.37f, 0f), new Vector3(0.08f, 0.74f, 0.8f), black);
            ColliderBox(root, "LimiteMesa", desk + new Vector3(0f, 0.4f, 0f), new Vector3(2.8f, 0.8f, 0.9f));
            Box("Mezclador", root, desk + new Vector3(0f, 0.82f, -0.1f), new Vector3(1f, 0.08f, 0.45f), black, collider: false)
                .transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            Box("TecladoMidi", root, desk + new Vector3(0f, 0.8f, -0.35f), new Vector3(1.2f, 0.04f, 0.18f), Mat("TeclasBlancas", new Color(0.85f, 0.85f, 0.83f)), collider: false);
            foreach (float dx in new[] { -0.45f, 0.45f })
            {
                Box("Pantalla", root, desk + new Vector3(dx, 1.12f, 0.25f), new Vector3(0.8f, 0.48f, 0.05f), black, collider: false);
                Box("ImagenPantalla", root, desk + new Vector3(dx, 1.12f, 0.22f), new Vector3(0.74f, 0.42f, 0.01f), screen, collider: false);
            }
            foreach (float dx in new[] { -1.15f, 1.15f })
            {
                Box("Monitor", root, desk + new Vector3(dx, 1.0f, 0.15f), new Vector3(0.3f, 0.45f, 0.3f), black, collider: false);
                Primitive(PrimitiveType.Cylinder, "Cono", root, desk + new Vector3(dx, 0.95f, -0.005f), new Vector3(0.2f, 0.01f, 0.2f), Mat("ConoAltavoz", new Color(0.2f, 0.2f, 0.2f)), keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var chair = desk + new Vector3(0.2f, 0f, -1f);
            Box("SillaAsiento", root, chair + new Vector3(0f, 0.48f, 0f), new Vector3(0.55f, 0.1f, 0.55f), black);
            Box("SillaRespaldo", root, chair + new Vector3(0f, 0.85f, -0.27f), new Vector3(0.5f, 0.6f, 0.08f), black, collider: false);
            Strut("SillaPie", root, chair, chair + Vector3.up * 0.45f, 0.06f, black, cylinder: true);

            // Sofá para los colegas, nevera pequeña, caja de discos y altavoz grande.
            OldSofa(root, new Vector3(-75.8f, StudioFloor, 44.5f), -90f);
            Box("Nevera", root, new Vector3(-76.9f, StudioFloor + 0.45f, 48.3f), new Vector3(0.6f, 0.9f, 0.6f), Mat("NeveraBlanca", new Color(0.8f, 0.8f, 0.78f)));
            Box("CajaDiscos", root, new Vector3(-74f, StudioFloor + 0.2f, 48.9f), new Vector3(0.8f, 0.4f, 0.4f), wood);
            Box("AltavozGrande", root, new Vector3(-63.6f, StudioFloor + 0.6f, 39.5f), new Vector3(0.6f, 1.2f, 0.6f), black);
            Box("Mesita", root, new Vector3(-74f, StudioFloor + 0.22f, 44.5f), new Vector3(0.9f, 0.44f, 0.6f), wood);
            Box("Ceniceros", root, new Vector3(-74f, StudioFloor + 0.46f, 44.5f), new Vector3(0.18f, 0.04f, 0.18f), Mat("Cenicero", new Color(0.4f, 0.4f, 0.42f)), collider: false);
            for (int i = 0; i < 3; i++)
                Primitive(PrimitiveType.Cylinder, "Vaso", root, new Vector3(-74.2f + i * 0.18f, StudioFloor + 0.5f, 44.35f + (i % 2) * 0.2f), new Vector3(0.08f, 0.06f, 0.08f),
                    Mat("VasoRojo", new Color(0.7f, 0.08f, 0.07f)), keepCollider: false);
        }

        // ---------- Memorial ----------

        /// <summary>
        /// Memorial improvisado en el eje E–O, al oeste de T2: tramo de malla con la foto, velas, peluches, flores,
        /// globos y un cartel "RIP". No lleva nombre ni cara: la foto es un rectángulo con marco.
        /// </summary>
        static void BuildMemorial(Transform parent, System.Random rng)
        {
            var root = new GameObject("Memorial").transform;
            root.SetParent(parent, false);
            var c = new Vector3(-17.5f, LawnTop, -4.6f);
            towersOccupied.Add(RectAround(c.x, c.z, 4f, 2.6f));

            // Tramo de malla de 3 m sobre postes (la foto y el cartel van pegados).
            var fenceA = new Vector3(c.x - 1.6f, LawnTop, -3.4f);
            var fenceB = new Vector3(c.x + 1.6f, LawnTop, -3.4f);
            if (chainLink)
                FenceSegment(root, fenceA, fenceB, 1.5f, chainLink, Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 3.2f, 0.05f, topRail: true);
            else
                Box("Valla", root, new Vector3(c.x, LawnTop + 0.75f, -3.4f), new Vector3(3.2f, 1.5f, 0.04f), ironMat);

            var photo = SignMat("FotoMemorial", "RIP", new Color(0.12f, 0.1f, 0.1f), new Color(0.95f, 0.92f, 0.85f), 7, 0f);
            Box("MarcoFoto", root, new Vector3(c.x, LawnTop + 1.05f, -3.44f), new Vector3(0.62f, 0.78f, 0.03f), Mat("MarcoDorado", new Color(0.65f, 0.5f, 0.2f)), collider: false);
            Box("Foto", root, new Vector3(c.x, LawnTop + 1.05f, -3.46f), new Vector3(0.5f, 0.64f, 0.01f), Mat("FotoSilueta", new Color(0.35f, 0.3f, 0.28f)), collider: false);
            SignQuad(root, new Vector3(c.x + 1f, LawnTop + 0.9f, -3.47f), Vector3.back, 0.9f, 0.5f, photo);
            SignQuad(root, new Vector3(c.x - 1.05f, LawnTop + 1.1f, -3.47f), Vector3.back, 0.8f, 0.45f,
                SignMat("CartelLlLong", "LONG LIVE", new Color(0.85f, 0.82f, 0.75f), new Color(0.1f, 0.1f, 0.4f), 8, 0f));

            // Velas de oración (cristal con la llama encendida) y una luz cálida baja.
            var candleGlass = Mat("VelaCristal", new Color(0.85f, 0.8f, 0.6f));
            var flame = EmissiveMat("LlamaVela", new Color(1f, 0.6f, 0.2f), 4f);
            for (int i = 0; i < 16; i++)
            {
                var p = c + new Vector3(Range(rng, -1.4f, 1.4f), 0f, Range(rng, 0.2f, 1.0f));
                Primitive(PrimitiveType.Cylinder, "Vela", root, p + Vector3.up * 0.1f, new Vector3(0.07f, 0.1f, 0.07f), candleGlass, keepCollider: false);
                Primitive(PrimitiveType.Sphere, "Llama", root, p + Vector3.up * 0.21f, Vector3.one * 0.035f, flame, keepCollider: false);
            }
            var glow = new GameObject("LuzVelas").AddComponent<Light>();
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = c + new Vector3(0f, 0.5f, 0.5f);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.6f, 0.3f);
            glow.range = 3.5f;
            glow.intensity = 1.5f;
            glow.shadows = LightShadows.None;

            // Peluches (cuerpo, cabeza y orejas) apoyados en la valla.
            var plush = new[] { Mat("PeluchMarron", new Color(0.45f, 0.3f, 0.18f)), Mat("PeluchBlanco", new Color(0.85f, 0.83f, 0.8f)), Mat("PeluchRosa", new Color(0.85f, 0.5f, 0.6f)) };
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(c.x - 1.3f + i * 0.62f, LawnTop, -3.6f - Range(rng, 0f, 0.15f));
                var m = plush[i % plush.Length];
                float sc = Range(rng, 0.8f, 1.2f);
                Primitive(PrimitiveType.Sphere, "Peluche", root, p + Vector3.up * 0.16f * sc, new Vector3(0.28f, 0.3f, 0.24f) * sc, m, keepCollider: false);
                Primitive(PrimitiveType.Sphere, "CabezaPeluche", root, p + Vector3.up * 0.38f * sc, Vector3.one * 0.2f * sc, m, keepCollider: false);
                foreach (float ex in new[] { -0.07f, 0.07f })
                    Primitive(PrimitiveType.Sphere, "Oreja", root, p + new Vector3(ex * sc, 0.47f * sc, 0f), Vector3.one * 0.07f * sc, m, keepCollider: false);
            }

            // Ramos de flores envueltos en celofán.
            var stems = Mat("Tallo", new Color(0.2f, 0.35f, 0.15f));
            var petals = new[] { Mat("FlorRoja", new Color(0.75f, 0.08f, 0.1f)), Mat("FlorBlanca", new Color(0.9f, 0.9f, 0.88f)), Mat("FlorAmarilla", new Color(0.9f, 0.75f, 0.15f)) };
            for (int i = 0; i < 4; i++)
            {
                var p = c + new Vector3(Range(rng, -1.4f, 1.4f), 0.05f, Range(rng, 0.7f, 1.0f));
                Primitive(PrimitiveType.Cylinder, "Ramo", root, p, new Vector3(0.12f, 0.25f, 0.12f), stems, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(80f, Range(rng, -40f, 40f), 0f);
                Primitive(PrimitiveType.Sphere, "Flores", root, p + new Vector3(0f, 0.05f, -0.25f), new Vector3(0.22f, 0.15f, 0.22f), petals[i % petals.Length], keepCollider: false);
            }
            Primitive(PrimitiveType.Cylinder, "Botella", root, c + new Vector3(0.6f, 0.15f, 0.5f), new Vector3(0.08f, 0.15f, 0.08f), Mat("BotellaAmbar", new Color(0.4f, 0.22f, 0.05f)), keepCollider: false);

            // Globos atados a la malla (cuerda y globo), de colores y alguno con la cara plateada.
            var balloons = new[]
            {
                Mat("GloboRojo", new Color(0.8f, 0.1f, 0.12f)), Mat("GloboAzul", new Color(0.12f, 0.3f, 0.8f)),
                Mat("GloboBlanco", new Color(0.92f, 0.92f, 0.9f)), Mat("GloboPlata", new Color(0.75f, 0.76f, 0.8f)),
            };
            var cord = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            for (int i = 0; i < 7; i++)
            {
                var knot = new Vector3(c.x - 1.4f + i * 0.45f, LawnTop + 1.5f, -3.42f);
                var ball = knot + new Vector3(Range(rng, -0.3f, 0.3f), Range(rng, 0.8f, 1.4f), Range(rng, -0.3f, 0.1f));
                Strut("Cuerda", root, knot, ball, 0.006f, cord);
                Primitive(PrimitiveType.Sphere, "Globo", root, ball + Vector3.up * 0.18f, new Vector3(0.3f, 0.36f, 0.3f), balloons[i % balloons.Length], keepCollider: false);
            }
        }

        // ---------- Zona de reunión ----------

        /// <summary>
        /// Al sur de la losa de la canasta (−26, 30): sillas de plástico en corro, barbacoa de bidón con brasas y humo,
        /// altavoz con LED, nevera portátil y vasos rojos.
        /// </summary>
        static void BuildHangout(Transform parent, Vector3 pad, System.Random rng)
        {
            var root = new GameObject("ZonaReunion").transform;
            root.SetParent(parent, false);
            var c = new Vector3(pad.x, LawnTop, pad.z - 6.3f); // (−26, 23.7): entre la losa y el tendedero de T1
            towersOccupied.Add(RectAround(c.x, c.z, 9f, 3.6f));

            // Sillas de plástico blancas y verdes en corro, mirando al centro.
            var plastic = new[] { Mat("PlasticoBlanco", new Color(0.88f, 0.88f, 0.85f)), Mat("PlasticoVerde", new Color(0.15f, 0.4f, 0.2f)) };
            for (int i = 0; i < 6; i++)
            {
                float a = -150f + i * 60f + Range(rng, -12f, 12f);
                var p = c + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 1.6f);
                p.x = c.x + (p.x - c.x) * 2.2f; // corro alargado este-oeste
                var chair = new GameObject("SillaPlastico").transform;
                chair.SetParent(root, false);
                chair.localPosition = p;
                var toCenter = c - p;
                chair.localRotation = Quaternion.Euler(0f, Mathf.Atan2(-toCenter.x, -toCenter.z) * Mathf.Rad2Deg + Range(rng, -15f, 15f), 0f);
                var m = plastic[i % 2];
                Box("Asiento", chair, new Vector3(0f, 0.42f, 0f), new Vector3(0.48f, 0.05f, 0.46f), m);
                Box("Respaldo", chair, new Vector3(0f, 0.72f, 0.22f), new Vector3(0.48f, 0.55f, 0.04f), m, collider: false)
                    .transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                foreach (var (lx, lz) in new[] { (-0.21f, -0.19f), (0.21f, -0.19f), (-0.21f, 0.19f), (0.21f, 0.19f) })
                    Box("Pata", chair, new Vector3(lx, 0.2f, lz), new Vector3(0.04f, 0.4f, 0.04f), m, collider: false);
            }

            // Barbacoa de bidón partido sobre patas, con brasas, rejilla y humo.
            var grill = c + new Vector3(3.2f, 0f, -0.2f);
            var steel = Mat("BidonNegro", new Color(0.08f, 0.08f, 0.08f));
            foreach (var (lx, lz) in new[] { (-0.4f, -0.2f), (0.4f, -0.2f), (-0.4f, 0.2f), (0.4f, 0.2f) })
                Strut("PataBarbacoa", root, grill + new Vector3(lx, 0f, lz), grill + new Vector3(lx * 0.8f, 0.75f, lz * 0.8f), 0.04f, steel);
            Primitive(PrimitiveType.Cylinder, "Bidon", root, grill + Vector3.up * 0.85f, new Vector3(0.6f, 0.5f, 0.6f), steel, keepCollider: true)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Box("Brasas", root, grill + Vector3.up * 1.12f, new Vector3(0.9f, 0.04f, 0.45f), EmissiveMat("BrasasBarbacoa", new Color(1f, 0.32f, 0.05f), 3f), collider: false);
            Box("Rejilla", root, grill + Vector3.up * 1.16f, new Vector3(0.95f, 0.01f, 0.5f), ironMat, collider: false);
            var meat = Mat("CarneAsada", new Color(0.35f, 0.15f, 0.08f));
            for (int i = 0; i < 4; i++)
                Box("Carne", root, grill + new Vector3(-0.3f + i * 0.2f, 1.19f, Range(rng, -0.1f, 0.1f)), new Vector3(0.14f, 0.04f, 0.1f), meat, collider: false);
            var smoke = Mat("Humo", new Color(0.6f, 0.6f, 0.62f));
            for (int i = 0; i < 4; i++)
                Primitive(PrimitiveType.Sphere, "Humo", root, grill + new Vector3(Range(rng, -0.1f, 0.3f), 1.5f + i * 0.45f, Range(rng, -0.1f, 0.2f)),
                    Vector3.one * (0.35f + i * 0.2f), smoke, keepCollider: false);
            var fire = new GameObject("LuzBrasas").AddComponent<Light>();
            fire.transform.SetParent(root, false);
            fire.transform.localPosition = grill + Vector3.up * 1.5f;
            fire.type = LightType.Point;
            fire.color = new Color(1f, 0.45f, 0.15f);
            fire.range = 4f;
            fire.intensity = 1.5f;
            fire.shadows = LightShadows.None;

            // Altavoz portátil grande con aro LED, nevera portátil y vasos.
            var speaker = c + new Vector3(-3.4f, 0f, 0.2f);
            var black = Mat("PlasticoNegro", new Color(0.03f, 0.03f, 0.035f));
            Box("Altavoz", root, speaker + Vector3.up * 0.45f, new Vector3(0.55f, 0.9f, 0.45f), black);
            foreach (float y in new[] { 0.25f, 0.65f })
            {
                Primitive(PrimitiveType.Cylinder, "AroLed", root, speaker + new Vector3(0f, y, -0.23f), new Vector3(0.36f, 0.005f, 0.36f),
                    EmissiveMat("LedAltavoz", new Color(0.1f, 0.6f, 1f), 3f), keepCollider: false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Primitive(PrimitiveType.Cylinder, "Cono", root, speaker + new Vector3(0f, y, -0.235f), new Vector3(0.3f, 0.005f, 0.3f), black, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Box("Nevera", root, c + new Vector3(1.2f, 0.22f, -1.3f), new Vector3(0.7f, 0.44f, 0.42f), Mat("NeveraAzul", new Color(0.15f, 0.3f, 0.65f)));
            Box("TapaNevera", root, c + new Vector3(1.2f, 0.46f, -1.3f), new Vector3(0.72f, 0.05f, 0.44f), Mat("PlasticoBlanco", new Color(0.88f, 0.88f, 0.85f)), collider: false);
            var cup = Mat("VasoRojo", new Color(0.7f, 0.08f, 0.07f));
            for (int i = 0; i < 6; i++)
            {
                var p = c + new Vector3(Range(rng, -2.5f, 2.5f), 0.06f, Range(rng, -1.2f, 1.2f));
                var cupGo = Primitive(PrimitiveType.Cylinder, "Vaso", root, p, new Vector3(0.09f, 0.06f, 0.09f), cup, keepCollider: false);
                if (i % 2 == 0) cupGo.transform.localRotation = Quaternion.Euler(90f, Range(rng, 0f, 360f), 0f);
            }
            Box("Bolsa", root, c + new Vector3(2.2f, 0.15f, 1.2f), new Vector3(0.35f, 0.3f, 0.25f), Mat("BolsaCarbon", new Color(0.55f, 0.45f, 0.3f)), collider: false);
        }
    }
}
