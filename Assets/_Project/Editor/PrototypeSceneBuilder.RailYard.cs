using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Cocheras del tren elevado al sur de la calle z = −80, delante del inicio del jugador (donde había una fila de
    /// two-flats lisos). Una rampa baja desde la vía oeste del elevado de la avenida hasta una playa de vías de grava:
    /// nave de mantenimiento de ladrillo y chapa con tres vías, diez vías de estacionamiento con trenes aparcados (algunos
    /// con grafitis), escalera de agujas al fondo, y al oeste un patio de contenedores apilados. Valla de malla con
    /// concertina (con agujeros), entrada con barrera y caseta de vigilante, oficina prefabricada, torres de focos,
    /// señales, traviesas y ruedas apiladas, maleza y nieve. Todo ficticio, sin marcas.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Recinto (valla) y suelo de grava.
        const float YardMinX = -74f, YardMaxX = 100f, YardNorthZ = -91.5f, YardSouthZ = -222f;
        const float YardGravelTop = 0.04f, YardRailTop = 0.36f;
        // Entrada (hueco de la valla norte) y camino de servicio, frente al inicio del jugador.
        const float YardGateX0 = -1f, YardGateX1 = 7f;
        // Nave de mantenimiento.
        const float ShedX0 = -34f, ShedX1 = -6f, ShedZSouth = -160f, ShedZNorth = -104f, ShedWallH = 8f, ShedRidge = 3f;
        static readonly float[] ShedTracksX = { -28f, -20f, -12f };
        // Vías de estacionamiento (de norte a la escalera de agujas).
        static readonly float[] StorageTracksX = { 8f, 13.5f, 19f, 24.5f, 30f, 35.5f, 41f, 46.5f, 52f, 57.5f };
        const float StorageNorthZ = -100f;
        // Escalera de agujas: recta del pie de la rampa (este) al oeste, algo inclinada.
        static readonly Vector2 LadderEast = new(78f, -206f), LadderWest = new(-30f, -214.2f);
        // Rampa desde la vía oeste del elevado (x 106.5) hasta el suelo, en el pie de la escalera.
        static readonly Vector2[] YardLead = { new(105.5f, -92f), new(90f, -130f), new(84f, -178f), LadderEast };
        // Patio de contenedores.
        static readonly float[] ContainerColumnsX = { -70.6f, -68.1f, -65.6f, -58.1f, -55.6f, -53.1f, -45.6f, -43.1f };
        const float ContainerFirstZ = -104.1f, ContainerRowStep = 15.2f;
        const int ContainerRows = 7;
        const float ContainerLong = 12.2f, ContainerShort = 6.1f, ContainerW = 2.44f, ContainerH = 2.59f;

        static float LadderZ(float x) => Mathf.Lerp(LadderWest.y, LadderEast.y, Mathf.InverseLerp(LadderWest.x, LadderEast.x, x));

        static Material yardTies, yardRail, yardThirdRail, yardCover, yardBallast, yardConcrete, yardSteel;

        static void BuildRailYard(Transform world)
        {
            var group = new GameObject("Cocheras").transform;
            group.SetParent(world, false);
            var rng = new System.Random(1902);

            yardBallast = TexturedMat("Balasto", ProceduralTextures.Ballast("Balasto", 91));
            yardTies = LoadMat("Traviesas") ?? CutoutMat("Traviesas", ProceduralTextures.Ties("Traviesas", 31));
            yardRail = LoadMat("Rail") ?? Mat("Rail", new Color(0.38f, 0.36f, 0.34f));
            yardThirdRail = Mat("TercerCarril", new Color(0.3f, 0.29f, 0.28f));
            yardCover = Mat("CubiertaTercerCarril", new Color(0.62f, 0.52f, 0.25f));
            yardConcrete = concreteMat ? concreteMat : Mat("Hormigon", new Color(0.6f, 0.59f, 0.56f));
            yardSteel = LoadMat("AceroEstructura") ?? Mat("AceroEstructura", new Color(0.36f, 0.4f, 0.37f));

            // Suelo de grava de todo el recinto, aceras a los lados (las calles x = ±110 siguen al sur) y camino de servicio.
            TexturedBox("GravaCocheras", group, new Vector3((YardMinX + YardMaxX) / 2f, YardGravelTop / 2f, (YardNorthZ + YardSouthZ) / 2f),
                new Vector3(YardMaxX - YardMinX, YardGravelTop, YardNorthZ - YardSouthZ), yardBallast, Vector2.one * ProceduralTextures.BallastTile);
            // La del oeste empieza pasados los two-flats de la esquina.
            Box("AceraCocheras", group, new Vector3(101.5f, 0.075f, -195f), new Vector3(SidewalkWidth, 0.15f, 210f), yardConcrete);
            Box("AceraCocheras", group, new Vector3(-101.5f, 0.075f, -205f), new Vector3(SidewalkWidth, 0.15f, 190f), yardConcrete);
            var asphalt = LoadMat("Asfalto") ?? Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            Box("CaminoServicio", group, new Vector3(-0.25f, 0.03f, (YardNorthZ - 200f) / 2f), new Vector3(10.5f, 0.06f, YardNorthZ + 200f), asphalt);

            BuildYardTracks(group);
            BuildYardLead(group);
            BuildYardShed(group, rng);
            BuildYardTrains(group, rng);
            BuildYardContainers(group, rng);
            BuildYardFence(group, rng);
            BuildYardEntrance(group);
            BuildYardProps(group, rng);
        }

        // ---------- Vías ----------

        static void BuildYardTracks(Transform parent)
        {
            var group = new GameObject("Vias").transform;
            group.SetParent(parent, false);
            var bumper = Mat("TopeVia", new Color(0.62f, 0.12f, 0.1f));

            foreach (float x in StorageTracksX)
            {
                YardTrack(group, new Vector2(x, StorageNorthZ), new Vector2(x, LadderZ(x)), thirdRail: true);
                Bumper(group, new Vector3(x, 0f, StorageNorthZ - 0.4f), bumper);
            }
            foreach (float x in ShedTracksX)
            {
                YardTrack(group, new Vector2(x, ShedZNorth - 1f), new Vector2(x, LadderZ(x)), thirdRail: false); // en la nave, sin tercer carril
                Bumper(group, new Vector3(x, 0f, ShedZNorth - 1.4f), bumper);
            }
            YardTrack(group, LadderWest, LadderEast, thirdRail: false);

            // Agujas: palanca de cambio junto a cada vía, en la escalera.
            var lever = Mat("PalancaAguja", new Color(0.75f, 0.62f, 0.12f));
            foreach (float x in StorageTracksX)
            {
                var p = new Vector3(x - 2.2f, YardGravelTop, LadderZ(x) + 1.5f);
                Box("CajaAguja", group, p + Vector3.up * 0.2f, new Vector3(0.5f, 0.4f, 0.5f), yardSteel);
                Strut("Palanca", group, p + Vector3.up * 0.4f, p + new Vector3(0.25f, 1.1f, 0f), 0.05f, lever, cylinder: true);
            }
        }

        /// <summary>Vía recta a ras de suelo de a a b (x, z): lecho de balasto, traviesas, raíles y, si toca, tercer carril con cubierta.</summary>
        static void YardTrack(Transform parent, Vector2 a, Vector2 b, bool thirdRail)
        {
            var d = b - a;
            float len = d.magnitude;
            var root = new GameObject("Via").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3((a.x + b.x) / 2f, 0f, (a.y + b.y) / 2f);
            root.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0f);
            TexturedBox("Lecho", root, new Vector3(0f, 0.07f, 0f), new Vector3(3.4f, 0.06f, len), yardBallast, Vector2.one * ProceduralTextures.BallastTile, collider: false);
            var ties = TexturedBox("Traviesas", root, new Vector3(0f, 0.12f, 0f), new Vector3(2.6f, 0.16f, len), yardTies,
                new Vector2(2.6f, ProceduralTextures.TiesTile), collider: false);
            ties.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            foreach (float dx in new[] { -0.72f, 0.72f })
                Box("Rail", root, new Vector3(dx, YardRailTop - 0.08f, 0f), new Vector3(0.08f, 0.16f, len), yardRail, collider: false);
            if (!thirdRail) return;
            Box("TercerCarril", root, new Vector3(1.4f, 0.25f, 0f), new Vector3(0.08f, 0.1f, len), yardThirdRail, collider: false);
            Box("Cubierta", root, new Vector3(1.4f, 0.34f, 0f), new Vector3(0.32f, 0.03f, len), yardCover, collider: false);
        }

        static void Bumper(Transform parent, Vector3 ground, Material paint)
        {
            Box("TopeVia", parent, ground + new Vector3(0f, 0.55f, -0.3f), new Vector3(2.4f, 1.1f, 0.6f), yardConcrete);
            Box("TopeViga", parent, ground + new Vector3(0f, 1.0f, -0.65f), new Vector3(2.2f, 0.3f, 0.12f), paint, collider: false);
        }

        // ---------- Rampa desde el elevado ----------

        /// <summary>
        /// Vía de acceso: sale de la vía oeste del elevado a 8.66 m y baja por tramos de 3 m hasta el pie de la escalera de
        /// agujas. Arriba va en viaducto (viga y pórticos de acero cada 4 tramos); por debajo de 2.6 m, sobre un terraplén
        /// con muros de contención (con colisión: se puede subir).
        /// </summary>
        static void BuildYardLead(Transform parent)
        {
            var group = new GameObject("RampaAcceso").transform;
            group.SetParent(parent, false);
            var wall = LoadMat("MuroContencion") ?? yardConcrete;
            var wallTile = new Vector2(ProceduralTextures.WallTileWidth, ProceduralTextures.WallTileHeight);

            float total = 0f;
            for (int i = 1; i < YardLead.Length; i++) total += Vector2.Distance(YardLead[i - 1], YardLead[i]);
            const float piece = 3f;
            int chunks = Mathf.CeilToInt(total / piece);
            float HeightAt(float s) => Mathf.Lerp(TrainRailTop, YardRailTop, s / total);
            Vector2 PointAt(float s, out Vector2 dir)
            {
                for (int i = 1; i < YardLead.Length; i++)
                {
                    float seg = Vector2.Distance(YardLead[i - 1], YardLead[i]);
                    dir = (YardLead[i] - YardLead[i - 1]).normalized;
                    if (s <= seg || i == YardLead.Length - 1) return YardLead[i - 1] + dir * Mathf.Min(s, seg);
                    s -= seg;
                }
                dir = Vector2.down;
                return YardLead[0];
            }

            float pitch = Mathf.Atan2(TrainRailTop - YardRailTop, total) * Mathf.Rad2Deg;
            for (int c = 0; c < chunks; c++)
            {
                float s = (c + 0.5f) * piece;
                var p = PointAt(s, out var dir);
                float h = HeightAt(s);
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                var root = new GameObject("Tramo").transform;
                root.SetParent(group, false);
                root.localPosition = new Vector3(p.x, h, p.y);
                root.localRotation = Quaternion.Euler(pitch, yaw, 0f);
                TexturedBox("Traviesas", root, new Vector3(0f, -0.24f, 0f), new Vector3(2.6f, 0.16f, piece + 0.1f), yardTies,
                    new Vector2(2.6f, ProceduralTextures.TiesTile), collider: false);
                foreach (float dx in new[] { -0.72f, 0.72f })
                    Box("Rail", root, new Vector3(dx, -0.08f, 0f), new Vector3(0.08f, 0.16f, piece + 0.1f), yardRail, collider: false);

                var flat = Quaternion.Euler(0f, yaw, 0f);
                var right = flat * Vector3.right;
                if (h > 2.6f)
                {
                    foreach (float dx in new[] { -1f, 1f })
                        Box("VigaRampa", root, new Vector3(dx, -0.85f, 0f), new Vector3(0.35f, 1f, piece + 0.1f), yardSteel, collider: false);
                    if (c % 4 == 0)
                    {
                        float top = h - 1.35f;
                        foreach (float side in new[] { -1.7f, 1.7f })
                        {
                            var g = new Vector3(p.x, 0f, p.y) + right * side;
                            Box("ZapataRampa", group, g + Vector3.up * 0.2f, new Vector3(0.9f, 0.4f, 0.9f), yardConcrete);
                            Box("PilarRampa", group, g + Vector3.up * (top / 2f), new Vector3(0.5f, top, 0.5f), yardSteel)
                                .transform.localRotation = flat;
                        }
                        Box("TravesanoRampa", group, new Vector3(p.x, top + 0.2f, p.y), new Vector3(4.2f, 0.4f, 0.6f), yardSteel, collider: false)
                            .transform.localRotation = flat;
                    }
                }
                else if (h - 0.3f > 0.1f)
                {
                    float fill = h - 0.3f;
                    TexturedBox("Terraplen", group, new Vector3(p.x, fill / 2f, p.y), new Vector3(4.4f, fill, piece + 0.05f), wall, wallTile)
                        .transform.localRotation = flat;
                }
            }
        }

        // ---------- Nave de mantenimiento ----------

        static void BuildYardShed(Transform parent, System.Random rng)
        {
            var root = new GameObject("NaveMantenimiento").transform;
            root.SetParent(parent, false);
            var brick = TexturedMat("LadrilloNave", ProceduralTextures.PlainBrick("LadrilloNave", new Color(0.5f, 0.27f, 0.2f), 92));
            var brickTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight);
            var corrugated = ProceduralTextures.Corrugated("ChapaOndulada", 93);
            var sheet = TexturedMat("ChapaNave", corrugated, new Color(0.52f, 0.56f, 0.52f));
            var roof = TexturedMat("TejadoNave", corrugated, new Color(0.3f, 0.31f, 0.32f));
            var sheetTile = new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            const float t = 0.4f, brickH = 3f;
            float len = ShedZNorth - ShedZSouth, midZ = (ShedZNorth + ShedZSouth) / 2f, width = ShedX1 - ShedX0, midX = (ShedX0 + ShedX1) / 2f;

            // Tramo de pared: ladrillo abajo y chapa arriba. a y b en planta (x, z); de y0 a y1.
            void Wall(Vector2 a, Vector2 b, float y0, float y1)
            {
                var d = b - a;
                float l = d.magnitude;
                var rot = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
                var c = (a + b) / 2f;
                if (y0 < brickH)
                {
                    float top = Mathf.Min(brickH, y1);
                    TexturedBox("MuroLadrillo", root, new Vector3(c.x, (y0 + top) / 2f, c.y), new Vector3(l, top - y0, t), brick, brickTile, sidesOnly: false)
                        .transform.localRotation = rot;
                }
                if (y1 > brickH)
                {
                    float bottom = Mathf.Max(brickH, y0);
                    TexturedBox("MuroChapa", root, new Vector3(c.x, (bottom + y1) / 2f, c.y), new Vector3(l, y1 - bottom, t), sheet, sheetTile)
                        .transform.localRotation = rot;
                }
            }

            // Oeste, entera. Este, con una puerta de personal hacia el camino de servicio (z −118…−116.6).
            Wall(new Vector2(ShedX0 + t / 2f, ShedZSouth), new Vector2(ShedX0 + t / 2f, ShedZNorth), 0f, ShedWallH);
            float ex = ShedX1 - t / 2f;
            Wall(new Vector2(ex, ShedZSouth), new Vector2(ex, -118f), 0f, ShedWallH);
            Wall(new Vector2(ex, -116.6f), new Vector2(ex, ShedZNorth), 0f, ShedWallH);
            Wall(new Vector2(ex, -118f), new Vector2(ex, -116.6f), 2.3f, ShedWallH);
            // Norte, entera (con una puerta de chapa cerrada, de adorno).
            Wall(new Vector2(ShedX0, ShedZNorth - t / 2f), new Vector2(ShedX1, ShedZNorth - t / 2f), 0f, ShedWallH);
            var doorMetal = LoadMat("PuertaMetal") ?? Mat("PuertaMetal", new Color(0.22f, 0.2f, 0.18f));
            Box("PuertaNorte", root, new Vector3(-20f, 1.1f, ShedZNorth + 0.03f), new Vector3(1.2f, 2.2f, 0.08f), doorMetal, collider: false);
            // Sur: tres portones de 5 × 6.5 m por donde entran las vías; uno con la persiana a medio bajar.
            float sz = ShedZSouth + t / 2f;
            float prev = ShedX0;
            foreach (float x in ShedTracksX)
            {
                Wall(new Vector2(prev, sz), new Vector2(x - 2.5f, sz), 0f, ShedWallH);
                Wall(new Vector2(x - 2.5f, sz), new Vector2(x + 2.5f, sz), 6.5f, ShedWallH);
                prev = x + 2.5f;
            }
            Wall(new Vector2(prev, sz), new Vector2(ShedX1, sz), 0f, ShedWallH);
            var shutter = Mat("PersianaNave", new Color(0.55f, 0.55f, 0.53f));
            Box("Persiana", root, new Vector3(ShedTracksX[2], 5f, sz), new Vector3(5f, 3f, 0.12f), shutter, collider: false);

            // Tejado a dos aguas (cumbrera a lo largo de Z) y hastiales.
            float half = width / 2f, slope = Mathf.Atan2(ShedRidge, half) * Mathf.Rad2Deg;
            float panel = Mathf.Sqrt(half * half + ShedRidge * ShedRidge) + 0.6f;
            foreach (float side in new[] { -1f, 1f })
                TexturedBox("Tejado", root, new Vector3(midX + side * half / 2f, ShedWallH + ShedRidge / 2f + 0.1f, midZ), new Vector3(panel, 0.2f, len + 0.8f), roof, sheetTile)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -side * slope);
            foreach (float z in new[] { ShedZNorth - t / 2f, ShedZSouth + t / 2f })
            {
                var gable = new GameObject("Hastial");
                gable.transform.SetParent(root, false);
                gable.transform.localPosition = new Vector3(midX, ShedWallH, z);
                gable.AddComponent<MeshFilter>().sharedMesh = GableMesh(width, ShedRidge, t - 0.02f);
                gable.AddComponent<MeshRenderer>().sharedMaterial = sheet;
            }
            // Ventanas altas corridas en los laterales.
            for (float z = ShedZSouth + 4f; z < ShedZNorth - 3f; z += 6f)
                foreach (float x in new[] { ShedX0 + t / 2f, ShedX1 - t / 2f })
                    Box("VentanaAlta", root, new Vector3(x, 5.8f, z), new Vector3(t + 0.06f, 1.4f, 3.2f), glass, collider: false);

            // Dentro: solera de hormigón, pasarela de trabajo, banco, bidones, ruedas sueltas y luces.
            Box("Solera", root, new Vector3(midX, 0.045f, midZ), new Vector3(width - 2f * t, 0.03f, len - 2f * t), yardConcrete);
            var yellow = Mat("PinturaAmarilla", new Color(0.8f, 0.64f, 0.12f));
            Box("PasarelaTrabajo", root, new Vector3(-15.6f, 0.55f, -130f), new Vector3(1.6f, 1.1f, 30f), yardSteel);
            Box("BordePasarela", root, new Vector3(-14.82f, 1.12f, -130f), new Vector3(0.06f, 0.04f, 30f), yellow, collider: false);
            Box("BancoTrabajo", root, new Vector3(ShedX0 + 1f, 0.45f, -122f), new Vector3(1f, 0.9f, 5f), Mat("MaderaBanco", new Color(0.4f, 0.3f, 0.2f)));
            for (int i = 0; i < 5; i++)
                Primitive(PrimitiveType.Cylinder, "Bidon", root, new Vector3(ShedX0 + 1f + Range(rng, 0f, 1.2f), 0.45f, -140f - i * 0.75f),
                    new Vector3(0.6f, 0.45f, 0.6f), Mat("BidonAzul", new Color(0.12f, 0.24f, 0.45f)), keepCollider: true);
            Wheelset(root, new Vector3(-24f, 0f, -150f), 0f);
            Wheelset(root, new Vector3(-24f, 0f, -153f), 0f);
            var lamp = EmissiveMat("LuzNave", new Color(1f, 0.95f, 0.85f), 2f);
            foreach (float z in new[] { -118f, -143f })
            {
                Box("Lampara", root, new Vector3(midX, ShedWallH - 0.3f, z), new Vector3(1.6f, 0.15f, 0.4f), lamp, collider: false);
                var light = new GameObject("LuzNave").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.localPosition = new Vector3(midX, ShedWallH - 0.8f, z);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.93f, 0.8f);
                light.range = 22f;
                light.intensity = 2f;
                light.shadows = LightShadows.None;
            }

            // Grafitis en la fachada sur y en la oeste.
            if (graffitiMats != null)
            {
                for (int i = 0; i < 3; i++)
                    if (graffitiMats[i % graffitiMats.Length])
                        WallGraffiti(root, new Vector3(ShedX0, Range(rng, 1.3f, 2f), ShedZSouth + 6f + i * 16f), Vector3.left, Range(rng, 3f, 4.5f), graffitiMats[i % graffitiMats.Length]);
                if (graffitiMats[3]) WallGraffiti(root, new Vector3(ShedX0 + 1.8f, 1.6f, ShedZSouth), Vector3.back, 3f, graffitiMats[3]);
            }
        }

        /// <summary>Prisma triangular (hastial): base de <paramref name="width"/> en X centrada, alto <paramref name="height"/>, grosor en Z.</summary>
        static Mesh GableMesh(float width, float height, float thickness)
        {
            float hw = width / 2f, hz = thickness / 2f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            void Face(Vector3[] corners, Vector3 normal)
            {
                int s = v.Count;
                foreach (var c in corners)
                {
                    v.Add(c);
                    n.Add(normal);
                    uv.Add(new Vector2((c.x + hw) / ProceduralTextures.CorrugatedTileU, c.y / ProceduralTextures.CorrugatedTileV));
                }
                for (int i = 1; i < corners.Length - 1; i++) tri.AddRange(new[] { s, s + i, s + i + 1 });
            }
            // Caras delantera y trasera (sentido horario visto desde fuera).
            Face(new[] { new Vector3(-hw, 0f, -hz), new Vector3(0f, height, -hz), new Vector3(hw, 0f, -hz) }, Vector3.back);
            Face(new[] { new Vector3(hw, 0f, hz), new Vector3(0f, height, hz), new Vector3(-hw, 0f, hz) }, Vector3.forward);
            var mesh = new Mesh { name = $"Hastial_{width:0.#}x{height:0.#}" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Wheelset(Transform parent, Vector3 ground, float yaw)
        {
            var root = new GameObject("Eje").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var dark = LoadMat("BajosTren") ?? Mat("BajosTren", new Color(0.08f, 0.08f, 0.09f));
            foreach (float x in new[] { -0.72f, 0.72f })
                Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(x, 0.45f, 0f), new Vector3(0.9f, 0.06f, 0.9f), dark, keepCollider: true)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Primitive(PrimitiveType.Cylinder, "Eje", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.8f, 0.14f), dark, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // ---------- Trenes aparcados ----------

        static void BuildYardTrains(Transform parent, System.Random rng)
        {
            var group = new GameObject("TrenesAparcados").transform;
            group.SetParent(parent, false);
            var body = LoadMat("AceroTren");
            var dark = LoadMat("BajosTren");
            var doors = LoadMat("PuertasTren");
            var stripe = LoadMat("FranjaTren");
            // Aparcados con las luces apagadas: ventanas y faros oscuros.
            var windows = Mat("VentanasTrenApagado", new Color(0.12f, 0.14f, 0.15f));
            var headlight = Mat("FaroTrenApagado", new Color(0.5f, 0.48f, 0.42f));
            if (!body || !dark || !doors || !stripe) return; // BuildTrains crea esos materiales antes

            // (vía, centro z, vagones). Dejan pasillos entre unos y otros para cruzar la playa a pie.
            var parked = new List<(float x, float z, int cars)>
            {
                (StorageTracksX[0], -140f, 4), (StorageTracksX[1], -168f, 4), (StorageTracksX[3], -132f, 4), (StorageTracksX[4], -176f, 2),
                (StorageTracksX[6], -128f, 4), (StorageTracksX[7], -158f, 4), (StorageTracksX[9], -170f, 2), (ShedTracksX[1], -128f, 2),
            };
            foreach (var (x, z, cars) in parked)
            {
                var train = Train("TrenAparcado", body, dark, windows, doors, stripe, headlight, cars);
                train.transform.SetParent(group, false);
                train.transform.localPosition = new Vector3(x, YardRailTop, z);
                train.AddComponent<SolidVisual>(); // los colliders de los vagones cuentan como visibles (escalar, cubrirse)
                foreach (Transform car in train.transform)
                {
                    var col = car.gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 2.15f, 0f);
                    col.size = new Vector3(2.9f, 3.7f, 14.6f);
                    // Grafitis en los costados de los de fuera de la nave.
                    if (x == ShedTracksX[1] || graffitiMats == null) continue;
                    foreach (float side in new[] { -1f, 1f })
                        if (rng.NextDouble() < 0.45)
                        {
                            var mat = graffitiMats[rng.Next(graffitiMats.Length)];
                            if (mat) WallGraffiti(car, new Vector3(side * 1.46f, Range(rng, 1.7f, 2.4f), Range(rng, -3.5f, 3.5f)), side > 0f ? Vector3.right : Vector3.left,
                                Range(rng, 4f, 7f), mat);
                        }
                }
            }
        }

        // ---------- Contenedores ----------

        static void BuildYardContainers(Transform parent, System.Random rng)
        {
            var group = new GameObject("Contenedores").transform;
            group.SetParent(parent, false);
            var corrugated = ProceduralTextures.Corrugated("ChapaOndulada", 93);
            var colors = new[]
            {
                new Color(0.55f, 0.16f, 0.12f), new Color(0.14f, 0.28f, 0.5f), new Color(0.2f, 0.36f, 0.25f),
                new Color(0.5f, 0.5f, 0.48f), new Color(0.75f, 0.73f, 0.68f), new Color(0.72f, 0.38f, 0.12f),
            };
            var shells = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++) shells[i] = TexturedMat($"Contenedor_{i}", corrugated, colors[i]);
            var rods = Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            var pallet = Mat("MaderaPale", new Color(0.55f, 0.44f, 0.3f));

            for (int row = 0; row < ContainerRows; row++)
            {
                float z = ContainerFirstZ - row * ContainerRowStep;
                foreach (float x in ContainerColumnsX)
                {
                    double r = rng.NextDouble();
                    int levels = r < 0.18 ? 0 : r < 0.5 ? 1 : r < 0.82 ? 2 : 3;
                    bool hideout = row == 2 && x == ContainerColumnsX[5];
                    if (hideout) levels = Mathf.Max(levels, 1);
                    for (int level = 0; level < levels; level++)
                    {
                        bool shortBox = level > 0 && rng.NextDouble() < 0.3;
                        float length = shortBox ? ContainerShort : ContainerLong;
                        float dz = shortBox ? Range(rng, -3f, 3f) : Range(rng, -0.15f, 0.15f);
                        float yaw = Range(rng, -1.2f, 1.2f) + (rng.NextDouble() < 0.5 ? 180f : 0f);
                        Container(group, new Vector3(x + Range(rng, -0.08f, 0.08f), YardGravelTop + level * ContainerH, z + dz), yaw, length,
                            shells[rng.Next(shells.Length)], rods, open: hideout && level == 0, rng);
                    }
                    // Palés apilados al pie de algunas pilas: escalón para subir encima de los contenedores.
                    if (levels > 0 && rng.NextDouble() < 0.3)
                    {
                        float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                        float pz = z + Range(rng, -4f, 4f);
                        int stack = 4 + rng.Next(3);
                        for (int k = 0; k < stack; k++)
                            Box("Pale", group, new Vector3(x + side * 1.9f, YardGravelTop + 0.075f + k * 0.16f, pz + (k % 2) * 0.03f),
                                new Vector3(1.2f, 0.15f, 1.2f), pallet);
                    }
                }
            }

            // Carretilla elevadora parada en el pasillo central.
            var forklift = new GameObject("Carretilla").transform;
            forklift.SetParent(group, false);
            forklift.localPosition = new Vector3(-61.8f, YardGravelTop, -150f);
            forklift.localRotation = Quaternion.Euler(0f, 8f, 0f);
            var yellow = Mat("PinturaAmarilla", new Color(0.8f, 0.64f, 0.12f));
            var black = LoadMat("BajosTren") ?? rods;
            Box("Cuerpo", forklift, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 1f, 2.2f), yellow);
            Box("Contrapeso", forklift, new Vector3(0f, 0.9f, -1.2f), new Vector3(1.15f, 0.9f, 0.4f), black, collider: false);
            Box("Mastil", forklift, new Vector3(0f, 1.6f, 1.2f), new Vector3(0.9f, 3f, 0.15f), black, collider: false);
            foreach (float x in new[] { -0.3f, 0.3f })
                Box("Horquilla", forklift, new Vector3(x, 0.12f, 1.85f), new Vector3(0.12f, 0.05f, 1.1f), black, collider: false);
            Box("Techo", forklift, new Vector3(0f, 2.2f, -0.1f), new Vector3(1.2f, 0.06f, 1.4f), black, collider: false);
            foreach (float x in new[] { -0.55f, 0.55f })
            foreach (float z in new[] { -0.7f, 0.7f })
                Primitive(PrimitiveType.Cylinder, "Rueda", forklift, new Vector3(x, 0.3f, z), new Vector3(0.6f, 0.12f, 0.6f), black, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        /// <summary>
        /// Contenedor de <paramref name="length"/> m (a lo largo de Z) apoyado en <paramref name="ground"/>, con las puertas
        /// en +Z y sus barras de cierre. Si va abierto (escondite), es hueco: se entra por las puertas, abiertas de par en par.
        /// </summary>
        static void Container(Transform parent, Vector3 ground, float yaw, float length, Material shell, Material rods, bool open, System.Random rng)
        {
            var root = new GameObject(open ? "ContenedorAbierto" : "Contenedor").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tile = new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);
            float hw = ContainerW / 2f, hl = length / 2f;
            if (!open)
                TexturedBox("Caja", root, new Vector3(0f, ContainerH / 2f, 0f), new Vector3(ContainerW, ContainerH, length), shell, tile);
            else
            {
                const float t = 0.06f;
                TexturedBox("Suelo", root, new Vector3(0f, t / 2f, 0f), new Vector3(ContainerW, t, length), shell, tile);
                TexturedBox("Techo", root, new Vector3(0f, ContainerH - t / 2f, 0f), new Vector3(ContainerW, t, length), shell, tile);
                foreach (float side in new[] { -1f, 1f })
                    TexturedBox("Lateral", root, new Vector3(side * (hw - t / 2f), ContainerH / 2f, 0f), new Vector3(t, ContainerH, length), shell, tile);
                TexturedBox("Fondo", root, new Vector3(0f, ContainerH / 2f, -hl + t / 2f), new Vector3(ContainerW, ContainerH, t), shell, tile);
                // Puertas abiertas contra los costados y, dentro, un colchón y una caja.
                foreach (float side in new[] { -1f, 1f })
                    TexturedBox("Puerta", root, new Vector3(side * (hw + 0.05f), ContainerH / 2f, hl + hw / 2f + 0.05f), new Vector3(0.05f, ContainerH - 0.1f, hw), shell, tile);
                Box("Colchon", root, new Vector3(0f, 0.18f, -hl + 1.4f), new Vector3(1.4f, 0.22f, 2f), Mat("Colchon", new Color(0.62f, 0.58f, 0.5f)));
                Box("Caja", root, new Vector3(0.7f, 0.3f, -hl + 3.2f), new Vector3(0.5f, 0.5f, 0.5f), Mat("CajaCarton", new Color(0.5f, 0.38f, 0.24f)));
                return;
            }
            // Barras de cierre en las puertas y esquineros.
            for (int i = 0; i < 4; i++)
                Box("BarraCierre", root, new Vector3(-0.9f + i * 0.6f, ContainerH / 2f, hl + 0.03f), new Vector3(0.04f, ContainerH - 0.2f, 0.04f), rods, collider: false);
            Box("JuntaPuertas", root, new Vector3(0f, ContainerH / 2f, hl + 0.015f), new Vector3(0.03f, ContainerH - 0.1f, 0.02f), rods, collider: false);
            // Algún grafiti en el costado.
            if (graffitiMats != null && rng.NextDouble() < 0.18 && ground.y < 0.5f)
            {
                var mat = graffitiMats[rng.Next(graffitiMats.Length)];
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                if (mat) WallGraffiti(root, new Vector3(side * (hw + 0.005f), 1.4f, Range(rng, -hl + 2.5f, hl - 2.5f)), side > 0f ? Vector3.right : Vector3.left, Mathf.Min(length - 2f, 4.5f), mat);
            }
        }

        // ---------- Valla ----------

        /// <summary>Malla de 2.2 m con concertina encima (se puede trepar, como todas las vallas), con un par de agujeros.</summary>
        static void BuildYardFence(Transform parent, System.Random rng)
        {
            var group = new GameObject("VallaCocheras").transform;
            group.SetParent(parent, false);
            var mesh = chainLink ? chainLink : LoadMat("MallaMetalica");
            var post = ironMat ? ironMat : Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            if (!mesh) return;
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            const float h = 2.2f;

            void Run(Vector3 a, Vector3 b)
            {
                FenceSegment(group, a, b, h, mesh, tile, post, 3f, 0.06f, topRail: true);
                Strut("Concertina", group, a + Vector3.up * (h + 0.25f), b + Vector3.up * (h + 0.25f), 0.45f, mesh, cylinder: true);
            }
            float g = YardGravelTop;
            // Norte: abierta en la entrada.
            Run(new Vector3(YardMinX, g, YardNorthZ), new Vector3(YardGateX0, g, YardNorthZ));
            Run(new Vector3(YardGateX1, g, YardNorthZ), new Vector3(YardMaxX, g, YardNorthZ));
            // Oeste, con un agujero hacia los patios de los two-flats que quedan en la esquina.
            Run(new Vector3(YardMinX, g, YardNorthZ), new Vector3(YardMinX, g, -112f));
            Run(new Vector3(YardMinX, g, -114f), new Vector3(YardMinX, g, YardSouthZ));
            // Sur, con otro agujero, y este (la rampa pasa por encima).
            Run(new Vector3(YardMinX, g, YardSouthZ), new Vector3(28f, g, YardSouthZ));
            Run(new Vector3(30.2f, g, YardSouthZ), new Vector3(YardMaxX, g, YardSouthZ));
            Run(new Vector3(YardMaxX, g, YardSouthZ), new Vector3(YardMaxX, g, YardNorthZ));

            // Malla arrancada colgando en los agujeros.
            var torn = TexturedBox("MallaRota", group, new Vector3(YardMinX + 0.5f, 0.9f, -113.6f), new Vector3(1.4f, 1.6f, 0.03f), mesh, tile, collider: false, sidesOnly: true);
            torn.transform.localRotation = Quaternion.Euler(8f, 70f, 12f);
            var torn2 = TexturedBox("MallaRota", group, new Vector3(29.6f, 0.8f, YardSouthZ + 0.6f), new Vector3(1.6f, 1.4f, 0.03f), mesh, tile, collider: false, sidesOnly: true);
            torn2.transform.localRotation = Quaternion.Euler(-10f, 25f, -8f);

            // Maleza al pie de la valla, por dentro y por fuera.
            if (tallGrass && weeds)
            {
                for (float x = YardMinX + 1f; x < YardMaxX - 1f; x += Range(rng, 1.4f, 2.6f))
                {
                    if (x > YardGateX0 - 1f && x < YardGateX1 + 1f) continue;
                    if (rng.NextDouble() < 0.5) Clump(group, rng, new Vector3(x, g, YardNorthZ - Range(rng, 0.4f, 1.2f)), 0.4f, 1.1f);
                    if (rng.NextDouble() < 0.7) Clump(group, rng, new Vector3(x, g, YardSouthZ + Range(rng, 0.4f, 2.5f)), 0.5f, 1.4f);
                }
                for (float z = YardSouthZ + 1f; z < YardNorthZ - 1f; z += Range(rng, 1.4f, 2.6f))
                {
                    if (rng.NextDouble() < 0.55) Clump(group, rng, new Vector3(YardMinX + Range(rng, 0.4f, 1.5f), g, z), 0.4f, 1.2f);
                    if (rng.NextDouble() < 0.55) Clump(group, rng, new Vector3(YardMaxX - Range(rng, 0.4f, 1.5f), g, z), 0.4f, 1.2f);
                }
            }
        }

        // ---------- Entrada ----------

        static void BuildYardEntrance(Transform parent)
        {
            var root = new GameObject("EntradaCocheras").transform;
            root.SetParent(parent, false);
            var post = ironMat ? ironMat : Mat("Hierro", new Color(0.07f, 0.07f, 0.08f));
            float g = YardGravelTop;

            // Pórtico con el letrero, puerta corredera abierta y barrera medio levantada.
            foreach (float x in new[] { YardGateX0, YardGateX1 })
                Box("PostePortico", root, new Vector3(x, 2.5f, YardNorthZ), new Vector3(0.3f, 5f, 0.3f), post);
            Box("Dintel", root, new Vector3((YardGateX0 + YardGateX1) / 2f, 5f, YardNorthZ), new Vector3(YardGateX1 - YardGateX0 + 0.3f, 0.3f, 0.3f), post, collider: false);
            var sign = SignMat("Letrero_Cocheras", "RAIL YARD", new Color(0.1f, 0.2f, 0.4f), new Color(0.92f, 0.92f, 0.88f), 94, 0.3f);
            SignQuad(root, new Vector3((YardGateX0 + YardGateX1) / 2f, 4.4f, YardNorthZ + 0.18f), Vector3.forward, 4f, 4f * 11f / (9 * 6 + 3), sign);
            var warning = SignMat("Letrero_Prohibido", "NO TRESPASSING", new Color(0.85f, 0.85f, 0.82f), new Color(0.7f, 0.1f, 0.08f), 95, 0f);
            foreach (float x in new[] { -12f, 22f, 60f, -50f })
                SignQuad(root, new Vector3(x, 1.5f, YardNorthZ + 0.04f), Vector3.forward, 2.6f, 2.6f * 11f / (14 * 6 + 3), warning);
            if (chainLink)
                FenceSegment(root, new Vector3(YardGateX1, g, YardNorthZ - 0.5f), new Vector3(YardGateX1 + 6f, g, YardNorthZ - 0.5f), 2.2f, chainLink,
                    Vector2.one * ProceduralTextures.ChainLinkTile, post, 3f, 0.06f, topRail: true);
            var red = Mat("BarreraRoja", new Color(0.7f, 0.1f, 0.08f));
            var white = LoadMat("PinturaBlanca") ?? Mat("PinturaBlanca", new Color(0.75f, 0.75f, 0.73f));
            var pivot = new Vector3(YardGateX1 - 0.6f, 1f, YardNorthZ - 2f);
            Box("PosteBarrera", root, pivot + Vector3.down * 0.5f, new Vector3(0.35f, 1f, 0.35f), white);
            var tip = pivot + new Vector3(-6.5f * Mathf.Cos(55f * Mathf.Deg2Rad), 6.5f * Mathf.Sin(55f * Mathf.Deg2Rad), 0f);
            Strut("Barrera", root, pivot, tip, 0.1f, red);
            Strut("BarreraFranja", root, Vector3.Lerp(pivot, tip, 0.33f), Vector3.Lerp(pivot, tip, 0.5f), 0.105f, white);

            // Caseta del vigilante, junto a la entrada, con luz.
            var booth = new GameObject("CasetaVigilante").transform;
            booth.SetParent(root, false);
            booth.localPosition = new Vector3(YardGateX1 + 2.8f, g, YardNorthZ - 4f);
            var wall = Mat("CasetaBlanca", new Color(0.7f, 0.69f, 0.64f));
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            Box("Muros", booth, new Vector3(0f, 1.25f, 0f), new Vector3(2.2f, 2.5f, 2.2f), wall);
            Box("Ventanas", booth, new Vector3(0f, 1.6f, 0f), new Vector3(2.24f, 0.8f, 2.24f), glass, collider: false);
            Box("Puerta", booth, new Vector3(1.12f, 1f, 0.3f), new Vector3(0.05f, 2f, 0.85f), LoadMat("PuertaMetal") ?? wall, collider: false);
            Box("Tejado", booth, new Vector3(0f, 2.6f, 0f), new Vector3(2.7f, 0.15f, 2.7f), post, collider: false);
            var lamp = EmissiveMat("LuzCaseta", new Color(1f, 0.8f, 0.55f), 2.5f);
            Box("Foco", booth, new Vector3(-0.8f, 2.4f, 1.15f), new Vector3(0.3f, 0.15f, 0.2f), lamp, collider: false);
            var light = new GameObject("LuzCaseta").AddComponent<Light>();
            light.transform.SetParent(booth, false);
            light.transform.localPosition = new Vector3(-0.8f, 2.2f, 1.6f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.45f);
            light.range = 9f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
        }

        // ---------- Resto ----------

        static void BuildYardProps(Transform parent, System.Random rng)
        {
            var group = new GameObject("Detalles").transform;
            group.SetParent(parent, false);
            float g = YardGravelTop;

            // Oficina prefabricada al norte de la nave, sobre bloques, con escalones, aparato de aire y un coche al lado.
            var office = new GameObject("Oficina").transform;
            office.SetParent(group, false);
            office.localPosition = new Vector3(-20f, g, -97f);
            var white = TexturedMat("ChapaOficina", ProceduralTextures.Corrugated("ChapaOndulada", 93), new Color(0.78f, 0.77f, 0.72f));
            var glass = LoadMat("CristalTintado") ?? Mat("CristalTintado", new Color(0.05f, 0.06f, 0.07f));
            foreach (float x in new[] { -4.2f, 0f, 4.2f })
                Box("Bloque", office, new Vector3(x, 0.25f, 0f), new Vector3(0.6f, 0.5f, 2.6f), yardConcrete);
            TexturedBox("Modulo", office, new Vector3(0f, 1.8f, 0f), new Vector3(10f, 2.6f, 3f), white,
                new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV));
            for (int i = 0; i < 3; i++)
                Box("Ventana", office, new Vector3(-3.5f + i * 2.4f, 2.1f, 1.52f), new Vector3(1.2f, 0.8f, 0.04f), glass, collider: false);
            Box("Puerta", office, new Vector3(3.6f, 1.55f, 1.52f), new Vector3(0.9f, 2.0f, 0.04f), LoadMat("PuertaMetal") ?? glass, collider: false);
            for (int k = 0; k < 3; k++)
                Box("Escalon", office, new Vector3(3.6f, 0.17f + k * 0.17f, 2.4f - k * 0.3f), new Vector3(1.2f, 0.17f, 0.3f), yardSteel);
            Box("AparatoAire", office, new Vector3(-4.6f, 1.7f, 1.7f), new Vector3(0.7f, 0.5f, 0.45f), LoadMat("AparatoAire") ?? white, collider: false);
            ParkedCar(group, new Vector3(-30f, 0f, -97.5f), 90f, rng);
            Dumpster(group, new Vector3(-11.5f, g, -99.5f), 0f, rng.NextDouble() < 0.5);

            // Torres de focos: dos junto a la nave y dos en la playa de vías.
            var pole = LoadMat("Poste") ?? Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lampMat = EmissiveMat("FocoCocheras", new Color(1f, 0.92f, 0.75f), 3f);
            foreach (var (pos, target) in new[]
                     {
                         (new Vector3(-37.5f, g, -112f), new Vector3(-20f, 0f, -170f)), (new Vector3(-37.5f, g, -190f), new Vector3(-10f, 0f, -200f)),
                         (new Vector3(66f, g, -112f), new Vector3(30f, 0f, -140f)), (new Vector3(66f, g, -195f), new Vector3(30f, 0f, -190f)),
                     })
                FloodTower(group, pos, target, pole, lampMat);

            // Señales: una alta al pie de la rampa y enanas al principio de las vías del fondo.
            var redLens = EmissiveMat("SenalRoja", new Color(1f, 0.12f, 0.08f), 3f);
            var greenLens = EmissiveMat("SenalVerde", new Color(0.2f, 1f, 0.4f), 2f);
            var black = LoadMat("BajosTren") ?? pole;
            var mast = new Vector3(LadderEast.x - 3f, g, LadderEast.y + 6f);
            Primitive(PrimitiveType.Cylinder, "MastilSenal", group, mast + Vector3.up * 2.5f, new Vector3(0.18f, 2.5f, 0.18f), pole, keepCollider: true);
            Box("CabezaSenal", group, mast + new Vector3(0f, 5.4f, 0f), new Vector3(0.5f, 1.2f, 0.35f), black, collider: false);
            Box("LuzRoja", group, mast + new Vector3(0f, 5.75f, 0.18f), new Vector3(0.22f, 0.22f, 0.04f), redLens, collider: false);
            Box("LuzVerde", group, mast + new Vector3(0f, 5.1f, 0.18f), new Vector3(0.22f, 0.22f, 0.04f), black, collider: false);
            for (int i = 0; i < StorageTracksX.Length; i += 2)
            {
                float x = StorageTracksX[i];
                var p = new Vector3(x + 2f, g, LadderZ(x) + 4f);
                Box("SenalEnana", group, p + Vector3.up * 0.45f, new Vector3(0.35f, 0.9f, 0.3f), black);
                Box("Luz", group, p + new Vector3(0f, 0.7f, -0.16f), new Vector3(0.15f, 0.15f, 0.03f), i % 4 == 0 ? redLens : greenLens, collider: false);
            }

            // Traviesas y carriles viejos apilados, ruedas sueltas y montones de balasto, en la franja libre del este.
            var oldWood = Mat("TraviesaVieja", new Color(0.24f, 0.19f, 0.15f));
            for (int pile = 0; pile < 3; pile++)
            {
                // Una pila por objeto, con un solo collider que la envuelve (SolidVisual: cuenta como visible).
                var c = new GameObject("PilaTraviesas").transform;
                c.SetParent(group, false);
                c.localPosition = new Vector3(68f, g, -125f - pile * 18f);
                c.gameObject.AddComponent<SolidVisual>();
                int layers = 3 + rng.Next(4);
                for (int k = 0; k < layers; k++)
                    for (int j = 0; j < 5; j++)
                        Box("TraviesaApilada", c, new Vector3(j * 0.3f - 0.6f + (k % 2) * 0.1f, 0.1f + k * 0.2f, 0f), new Vector3(0.25f, 0.18f, 2.6f), oldWood, collider: false);
                ColliderBox(c, "LimitePila", new Vector3(0f, layers * 0.1f, 0f), new Vector3(1.6f, layers * 0.2f, 2.6f));
            }
            for (int k = 0; k < 6; k++)
                Box("CarrilViejo", group, new Vector3(72.5f, g + 0.08f + (k / 3) * 0.16f, -168f + (k % 3) * 0.35f), new Vector3(12f, 0.15f, 0.12f), yardRail);
            for (int k = 0; k < 4; k++)
                Wheelset(group, new Vector3(70f + k * 1.5f, g, -180f), 90f);
            var ballastMat = yardBallast;
            foreach (var p in new[] { new Vector3(-36f, g, -205f), new Vector3(70f, g, -100f) })
            {
                var mound = Primitive(PrimitiveType.Sphere, "MontonBalasto", group, p, new Vector3(6f, 2.4f, 4.5f), ballastMat, keepCollider: true);
                mound.transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f);
            }

            // Nieve apartada a los lados del camino de servicio y en las esquinas, charcos de aceite y algún bidón tirado.
            for (int i = 0; i < 9; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                SnowMound(group, new Vector3(-0.25f + side * 5.2f, g, Range(rng, -100f, -195f)), new Vector3(Range(rng, 1.6f, 3f), Range(rng, 0.4f, 0.8f), Range(rng, 1.2f, 2.2f)), Range(rng, 0f, 90f));
            }
            foreach (var p in new[] { new Vector3(YardMinX + 3f, g, YardSouthZ + 3f), new Vector3(YardMaxX - 3f, g, YardSouthZ + 3f), new Vector3(YardMaxX - 4f, g, YardNorthZ - 4f) })
                SnowMound(group, p, new Vector3(Range(rng, 2.5f, 4f), Range(rng, 0.6f, 1f), Range(rng, 2f, 3f)), Range(rng, 0f, 90f));
            var drum = Mat("BidonOxido", new Color(0.38f, 0.22f, 0.14f));
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(Range(rng, 60.5f, 64.5f), g, Range(rng, -110f, -200f));
                bool tipped = rng.NextDouble() < 0.4;
                var d = Primitive(PrimitiveType.Cylinder, "Bidon", group, p + Vector3.up * (tipped ? 0.3f : 0.45f), new Vector3(0.6f, 0.45f, 0.6f), drum, keepCollider: true);
                if (tipped) d.transform.localRotation = Quaternion.Euler(90f, Range(rng, 0f, 180f), 0f);
            }
            // Hierbajos entre las vías.
            if (tallGrass && weeds)
                for (int i = 0; i < 90; i++)
                {
                    float x = Range(rng, StorageTracksX[0] - 2f, 76f);
                    float z = Range(rng, StorageNorthZ - 2f, LadderZ(x) + 2f);
                    Clump(group, rng, new Vector3(x, g, z), 0.25f, 0.7f);
                }
        }

        /// <summary>Torre de focos de 16 m con cuatro proyectores y un foco real (sin sombras) apuntando a la playa.</summary>
        static void FloodTower(Transform parent, Vector3 ground, Vector3 target, Material pole, Material lampMat)
        {
            const float height = 16f;
            Primitive(PrimitiveType.Cylinder, "TorreFocos", parent, ground + Vector3.up * (height / 2f), new Vector3(0.4f, height / 2f, 0.4f), pole, keepCollider: true);
            var head = new GameObject("Proyectores").transform;
            head.SetParent(parent, false);
            head.localPosition = ground + Vector3.up * height;
            var flat = target - ground;
            flat.y = 0f;
            head.localRotation = Quaternion.LookRotation(flat.sqrMagnitude > 0.01f ? flat : Vector3.forward);
            Box("Bastidor", head, Vector3.zero, new Vector3(2.6f, 0.15f, 0.2f), pole, collider: false);
            for (int i = 0; i < 4; i++)
                Box("Proyector", head, new Vector3(-0.95f + i * 0.63f, 0.35f, 0.15f), new Vector3(0.5f, 0.4f, 0.12f), lampMat, collider: false)
                    .transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            var light = new GameObject("LuzFocos").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = ground + Vector3.up * (height - 0.5f);
            light.transform.localRotation = Quaternion.LookRotation(target - light.transform.localPosition);
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.92f, 0.78f);
            light.range = 60f;
            light.spotAngle = 95f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            light.gameObject.AddComponent<NightLight>(); // apagada de día, encendida al anochecer
        }
    }
}
