using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Piezas del polígono industrial del sur (South.cs): cocheras de autobuses escolares, taller de chapa, desguace,
    /// fábrica abandonada con chimenea y apartadero, almacén con muelles de carga, parque de tubos, frigorífico y
    /// montón de sal con cinta al muelle; la curva de S WHARF RD (ArcStrip), la vía de mercancías con sus pasos a
    /// nivel, el canal con los puentes y la otra orilla (horizonte). Los remolques de camión salen de <see cref="Semi"/>.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // ---------- Utilidades ----------

        /// <summary>
        /// Sector de corona circular (centro <paramref name="c"/> en XZ, radios r0…r1, ángulos a0…a1 en grados, medidos
        /// desde +X hacia +Z) de y0 a y1, con malla propia. Arriba, U va a lo ancho (desde r0) y V a lo largo del arco
        /// medido en el radio medio, en metros divididos por <paramref name="tile"/>. Si tiene alto, lleva bordillos
        /// curvos dentro y fuera. El collider es un MeshCollider (no convexo).
        /// </summary>
        static GameObject ArcStrip(string name, Transform parent, Vector2 c, float r0, float r1, float a0, float a1, float y0, float y1,
            Material material, Vector2 tile, bool collider, int segments = 24)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3, Vector3 outward)
            {
                int b = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                uvs.Add(u0); uvs.Add(u1); uvs.Add(u2); uvs.Add(u3);
                // Unity: la cara de delante es la que mira hacia Cross(p1 − p0, p2 − p0).
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), outward) >= 0f) tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                else tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            Vector3 P(float r, float t, float y) => new(c.x + r * Mathf.Cos(t), y, c.y + r * Mathf.Sin(t));
            float rMid = (r0 + r1) / 2f, start = a0 * Mathf.Deg2Rad;
            for (int i = 0; i < segments; i++)
            {
                float t0 = Mathf.Lerp(a0, a1, (float)i / segments) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (float)(i + 1) / segments) * Mathf.Deg2Rad;
                float s0 = Mathf.Abs(t0 - start) * rMid / tile.y, s1 = Mathf.Abs(t1 - start) * rMid / tile.y;
                float w = (r1 - r0) / tile.x;
                Quad(P(r0, t0, y1), P(r1, t0, y1), P(r1, t1, y1), P(r0, t1, y1), new Vector2(0f, s0), new Vector2(w, s0), new Vector2(w, s1), new Vector2(0f, s1), Vector3.up);
                if (y1 - y0 < 0.05f) continue;
                var mid = new Vector3(Mathf.Cos((t0 + t1) / 2f), 0f, Mathf.Sin((t0 + t1) / 2f));
                float h = (y1 - y0) / tile.y;
                foreach (var (r, outward) in new[] { (r0, -mid), (r1, mid) })
                {
                    float u0 = Mathf.Abs(t0 - start) * r / tile.x, u1 = Mathf.Abs(t1 - start) * r / tile.x;
                    Quad(P(r, t0, y0), P(r, t1, y0), P(r, t1, y1), P(r, t0, y1), new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, h), new Vector2(u0, h), outward);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>Nave de caja: paredes con textura, tejado plano y huella para el plano.</summary>
        static Transform Shed(Transform parent, string name, Rect r, float height, Material wall, Vector2 wallTile, Material roof, string kind)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            TexturedBox("Muros", root, new Vector3(r.center.x, height / 2f, r.center.y), new Vector3(r.width, height, r.height), wall, wallTile, sidesOnly: true);
            Box("Tejado", root, new Vector3(r.center.x, height + 0.1f, r.center.y), new Vector3(r.width + 0.3f, 0.2f, r.height + 0.3f), roof);
            RecordPolygon(kind, RectCorners(r), height);
            return root;
        }

        /// <summary>Cartel de texto (una o varias líneas) pegado a una pared, mirando a <paramref name="normal"/>.</summary>
        static void WallSign(Transform parent, string matName, Vector3 center, Vector3 normal, string[] lines, Color background, Color ink, int seed, float width)
        {
            var mat = TexturedMat(matName, ProceduralTextures.Placard(matName, lines, background, ink, seed));
            float aspect = (float)mat.mainTexture.width / mat.mainTexture.height;
            SignQuad(parent, center + normal * 0.03f, normal, width, width / aspect, mat);
        }

        static void ChainFence(Transform parent, Vector3 a, Vector3 b, float height = 2.2f)
        {
            if (chainLink) FenceSegment(parent, a, b, height, chainLink, Vector2.one * ProceduralTextures.ChainLinkTile, ironMat, 3f, 0.05f, topRail: true);
        }

        static Material SouthSheet(string name, Color tint, int seed) => TexturedMat(name, ProceduralTextures.Corrugated(name, seed), tint);
        static Vector2 SheetTile => new(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);

        /// <summary>
        /// Camión: remolque de 13 m (con el frontal hacia +Z local girado <paramref name="yaw"/>) y, si
        /// <paramref name="tractor"/>, la cabeza tractora delante; sin ella, el remolque descansa en sus patas.
        /// </summary>
        static void Semi(Transform parent, Vector3 ground, float yaw, bool tractor, System.Random rng)
        {
            var root = new GameObject(tractor ? "Camion" : "Remolque").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var shells = new[] { new Color(0.86f, 0.86f, 0.84f), new Color(0.78f, 0.78f, 0.76f), new Color(0.55f, 0.2f, 0.16f), new Color(0.25f, 0.33f, 0.45f) };
            int pick = rng.Next(shells.Length);
            var shell = SouthSheet($"ChapaRemolque_{pick}", shells[pick], 410 + pick);
            var dark = Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f));
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            float tz = tractor ? -4.5f : 0f;
            TexturedBox("Caja", root, new Vector3(0f, 2.65f, tz), new Vector3(2.55f, 2.7f, 13f), shell, SheetTile);
            Box("Bastidor", root, new Vector3(0f, 1.15f, tz), new Vector3(1.2f, 0.3f, 12.6f), dark, collider: false);
            if (pick < 2 && rng.NextDouble() < 0.5) // remolque frigorífico: equipo de frío en el frontal
                Box("EquipoFrio", root, new Vector3(0f, 3.2f, tz + 6.75f), new Vector3(2f, 1.2f, 0.5f), Mat("EquipoFrio", new Color(0.82f, 0.82f, 0.8f)), collider: false);
            foreach (float az in new[] { tz - 5.2f, tz - 3.9f })
            foreach (float x in new[] { -0.95f, 0.95f })
                Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(x, 0.5f, az), new Vector3(1f, 0.25f, 1f), tire, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (!tractor)
            {
                foreach (float x in new[] { -0.9f, 0.9f })
                    Box("Pata", root, new Vector3(x, 0.6f, tz + 4.5f), new Vector3(0.15f, 1.2f, 0.15f), dark, collider: false);
                return;
            }
            var paints = new[] { new Color(0.6f, 0.1f, 0.08f), new Color(0.12f, 0.2f, 0.4f), new Color(0.85f, 0.85f, 0.82f), new Color(0.15f, 0.15f, 0.16f) };
            var cab = Mat($"CabinaCamion_{pick}", paints[rng.Next(paints.Length)]);
            Box("Chasis", root, new Vector3(0f, 0.95f, 3.2f), new Vector3(1.2f, 0.4f, 6.4f), dark, collider: false);
            Box("Cabina", root, new Vector3(0f, 2.35f, 4.2f), new Vector3(2.45f, 2.3f, 2.2f), cab);
            Box("Capo", root, new Vector3(0f, 1.6f, 6f), new Vector3(2.3f, 1.3f, 1.5f), cab);
            Box("Parabrisas", root, new Vector3(0f, 2.85f, 5.31f), new Vector3(2.2f, 0.9f, 0.02f), darkGlass ? darkGlass : dark, collider: false);
            Box("Parachoques", root, new Vector3(0f, 0.85f, 6.8f), new Vector3(2.45f, 0.35f, 0.2f), Mat("CromoCamion", new Color(0.7f, 0.7f, 0.72f)), collider: false);
            foreach (float x in new[] { -1.05f, 1.05f })
                Primitive(PrimitiveType.Cylinder, "Escape", root, new Vector3(x, 3.4f, 3.05f), new Vector3(0.14f, 1.1f, 0.14f), Mat("CromoCamion", new Color(0.7f, 0.7f, 0.72f)), keepCollider: false);
            foreach (float az in new[] { 5.7f, 1.6f, 0.4f })
            foreach (float x in new[] { -0.95f, 0.95f })
                Primitive(PrimitiveType.Cylinder, "Rueda", root, new Vector3(x, 0.5f, az), new Vector3(1f, 0.25f, 1f), tire, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static Vector3 G(float x, float z, float y = 0f) => new(x, y, z);

        // ---------- Suroeste: cocheras de autobuses, taller y solar ----------

        /// <summary>Cocheras de autobuses escolares: nave de chapa con portones, patio vallado con autobuses, surtidor y focos.</summary>
        static void BusDepot(Transform parent, System.Random rng)
        {
            var group = new GameObject("CocherasAutobuses").transform;
            group.SetParent(parent, false);
            var r = SouthDepot;
            // Valla con la entrada a J (z −200…−188).
            ChainFence(group, G(r.xMin, r.yMin), G(r.xMin, -200f));
            ChainFence(group, G(r.xMin, -188f), G(r.xMin, r.yMax));
            ChainFence(group, G(r.xMin, r.yMax), G(r.xMax, r.yMax));
            ChainFence(group, G(r.xMax, r.yMax), G(r.xMax, r.yMin));
            ChainFence(group, G(r.xMax, r.yMin), G(r.xMin, r.yMin));

            var sheet = SouthSheet("ChapaCocheras", new Color(0.62f, 0.64f, 0.6f), 421);
            var roof = Mat("TejadoNaveSur", new Color(0.3f, 0.31f, 0.32f));
            var shedRect = Rect.MinMaxRect(-240f, -170f, -185f, -125f);
            var shed = Shed(group, "NaveAutobuses", shedRect, 9f, sheet, SheetTile, roof, "nave");
            var door = TexturedMat("PortonCocheras", ProceduralTextures.Shutter("PortonCocheras", 422));
            var black = Mat("InteriorOscuro", new Color(0.03f, 0.03f, 0.03f));
            int i = 0;
            foreach (float x in new[] { -232f, -222f, -212f, -202f, -192f })
            {
                // Uno abierto, con un autobús asomando.
                bool open = i++ == 2;
                Box(open ? "PortonAbierto" : "Porton", shed, G(x, shedRect.yMin - 0.03f, 2.75f), new Vector3(6f, 5.5f, 0.06f), open ? black : door, collider: false);
                if (open) SchoolBus(group, G(x, shedRect.yMin - 3f), 180f);
            }
            WallSign(shed, "Letrero_Cocheras", G(-212.5f, shedRect.yMin - 0.05f, 7.3f), Vector3.back, new[] { "SOUTHSIDE STUDENT TRANSPORT" },
                new Color(0.85f, 0.65f, 0.1f), new Color(0.08f, 0.08f, 0.08f), 423, 16f);

            // Autobuses aparcados en dos filas, morro al norte, con huecos.
            foreach (float z in new[] { -215f, -262f })
                for (float x = -240f; x < -176f; x += 4.4f)
                    if (rng.NextDouble() < 0.8) SchoolBus(group, G(x, z), 0f);

            // Isla de gasoil con marquesina.
            var steel = Mat("AceroPoligono", new Color(0.4f, 0.42f, 0.42f));
            var red = Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f));
            var canopy = G(-177.5f, -150f);
            foreach (float dx in new[] { -3f, 3f })
            foreach (float dz in new[] { -4f, 4f })
                Box("Pilar", group, canopy + new Vector3(dx, 2.5f, dz), new Vector3(0.3f, 5f, 0.3f), steel);
            Box("Marquesina", group, canopy + Vector3.up * 5.2f, new Vector3(8f, 0.5f, 10f), Mat("MarquesinaGasoil", new Color(0.85f, 0.85f, 0.82f)), collider: false);
            Box("Isla", group, canopy + Vector3.up * 0.1f, new Vector3(1.6f, 0.2f, 7f), concreteMat);
            foreach (float dz in new[] { -2f, 2f })
                Box("Surtidor", group, canopy + new Vector3(0f, 1f, dz), new Vector3(0.8f, 1.6f, 0.6f), red);

            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lamp = EmissiveMat("Proyector", new Color(1f, 0.95f, 0.85f), 1.5f);
            FloodTower(group, G(-244f, -180f), G(-210f, -240f), pole, lamp);
            FloodTower(group, G(-173f, -296f), G(-210f, -240f), pole, lamp);

            // Barro y aceite en el patio.
            var oil = LoadMat("Aceite");
            for (int k = 0; k < 30; k++)
                if (oil) Decal(group, G(Range(rng, r.xMin + 2f, r.xMax - 2f), Range(rng, r.yMin + 2f, -175f), 0.004f + k * 0.0002f), Range(rng, 0f, 360f), Vector2.one * Range(rng, 0.8f, 2f), oil);
        }

        /// <summary>Taller de chapa y neumáticos en la esquina de Gantry con S CALDER AVE.</summary>
        static void AutoBodyShop(Transform parent, System.Random rng)
        {
            var group = new GameObject("TallerChapa").transform;
            group.SetParent(parent, false);
            var r = SouthAutoBody;
            var wall = SouthSheet("ChapaTaller", new Color(0.35f, 0.42f, 0.5f), 431);
            var shop = Shed(group, "Taller", r, 5.5f, wall, SheetTile, tarRoof, "nave");
            var door = TexturedMat("PortonCocheras", ProceduralTextures.Shutter("PortonCocheras", 422));
            var black = Mat("InteriorOscuro", new Color(0.03f, 0.03f, 0.03f));
            Box("Porton", shop, G(-145f, r.yMin - 0.03f, 2f), new Vector3(4.5f, 4f, 0.06f), door, collider: false);
            Box("PortonAbierto", shop, G(-138f, r.yMin - 0.03f, 2f), new Vector3(4.5f, 4f, 0.06f), black, collider: false);
            Box("Puerta", shop, G(-130f, r.yMin - 0.03f, 1.1f), new Vector3(1f, 2.2f, 0.06f), darkGlass, collider: false);
            WallSign(shop, "Letrero_Taller", G(-138f, r.yMin - 0.05f, 4.7f), Vector3.back, new[] { "AUTO BODY  TIRES" },
                new Color(0.9f, 0.9f, 0.86f), new Color(0.65f, 0.08f, 0.06f), 432, 13f);
            WallSign(shop, "Letrero_TallerLateral", G(r.xMax + 0.05f, -297f, 3.5f), Vector3.right, new[] { "USED TIRES", "$20 & UP" },
                new Color(0.95f, 0.8f, 0.15f), new Color(0.08f, 0.08f, 0.08f), 433, 5f);
            // Pilas de neumáticos y un coche a medio arreglar.
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            foreach (var p in new[] { G(-150f, -308f), G(-149f, -309.6f), G(-127f, -308.5f), G(-122.8f, -300f), G(-122.8f, -295f) })
            {
                int n = 3 + rng.Next(5);
                for (int k = 0; k < n; k++)
                    Primitive(PrimitiveType.Cylinder, "Neumatico", group, p + new Vector3(Range(rng, -0.05f, 0.05f), 0.13f + k * 0.26f, 0f), new Vector3(0.75f, 0.13f, 0.75f), tire, keepCollider: k == 0);
            }
            Wreck(group, G(-160f, -298f), 90f, rng);
            ParkedCar(group, G(-133f, -308.6f), 0f, rng);
        }

        static void SouthVacantLot(Transform parent, System.Random rng)
        {
            var group = new GameObject("SolarSur").transform;
            group.SetParent(parent, false);
            var r = SouthVacant;
            var steel = Mat("AceroPoligono", new Color(0.4f, 0.42f, 0.42f));
            Billboard(group, G(-128f, -230f, LawnTop), -90f, steel, 1);
            Billboard(group, G(-150f, -284f, LawnTop), 0f, steel, 0);
            // Valla de malla medio caída hacia la calle grande.
            ChainFence(group, G(-122.3f, -278f, LawnTop), G(-122.3f, -240f, LawnTop), 1.8f);
            ChainFence(group, G(-122.3f, -225f, LawnTop), G(-122.3f, -160f, LawnTop), 1.8f);
            var bark = LoadMat("Corteza");
            for (int i = 0; i < 70; i++)
                Clump(group, rng, G(Range(rng, r.xMin + 1f, r.xMax - 1f), Range(rng, r.yMin + 1f, r.yMax - 1f), LawnTop), 0.4f, 1.5f);
            for (int i = 0; i < 4; i++)
                BareTree(group, G(Range(rng, r.xMin + 4f, r.xMax - 4f), Range(rng, r.yMin + 4f, r.yMax - 10f), LawnTop), bark, rng, Range(rng, 0.8f, 1.2f));
            Box("Colchon", group, G(-140f, -150f, LawnTop + 0.12f), new Vector3(1.4f, 0.22f, 2f), Mat("Colchon", new Color(0.62f, 0.58f, 0.5f)), collider: false)
                .transform.localRotation = Quaternion.Euler(0f, 30f, 4f);
            for (int i = 0; i < 6; i++) GarbageBag(group, G(Range(rng, -160f, -125f), Range(rng, -275f, -115f), LawnTop), rng);
        }

        // ---------- Desguace ----------

        static void Scrapyard(Transform parent, System.Random rng)
        {
            var group = new GameObject("Desguace").transform;
            group.SetParent(parent, false);
            var r = SouthScrap;
            // Muro de chapa de 2.6 m, por paños de colores; entrada desde Gantry (x −200…−188).
            var sheets = new[]
            {
                SouthSheet("ChapaDesguace_Oxido", new Color(0.5f, 0.3f, 0.2f), 441),
                SouthSheet("ChapaDesguace_Gris", new Color(0.55f, 0.56f, 0.55f), 442),
                SouthSheet("ChapaDesguace_Azul", new Color(0.3f, 0.38f, 0.48f), 443),
            };
            void Wall(Vector2 a, Vector2 b)
            {
                var d = b - a;
                float len = d.magnitude;
                for (float s = 0f; s < len - 0.1f; s += 6f)
                {
                    float l = Mathf.Min(6f, len - s);
                    var m = a + d.normalized * (s + l / 2f);
                    var panel = TexturedBox("MuroChapa", group, G(m.x, m.y, 1.3f), new Vector3(l, 2.6f, 0.08f), sheets[rng.Next(sheets.Length)], SheetTile);
                    panel.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
                }
            }
            Wall(V(r.xMin, r.yMax), V(-200f, r.yMax));
            Wall(V(-188f, r.yMax), V(r.xMax, r.yMax));
            Wall(V(r.xMax, r.yMax), V(r.xMax, r.yMin));
            Wall(V(r.xMax, r.yMin), V(r.xMin, r.yMin));
            Wall(V(r.xMin, r.yMin), V(r.xMin, r.yMax));
            WallSign(group, "Letrero_Desguace", G(-214f, r.yMax + 0.05f, 1.6f), Vector3.forward, new[] { "WE BUY", "JUNK CARS", "CASH PAID" },
                new Color(0.95f, 0.85f, 0.2f), new Color(0.75f, 0.08f, 0.06f), 444, 5.5f);
            WallSign(group, "Letrero_DesguaceNombre", G(-170f, r.yMax + 0.05f, 1.6f), Vector3.forward, new[] { "ACE AUTO SALVAGE" },
                new Color(0.12f, 0.12f, 0.13f), new Color(0.95f, 0.95f, 0.9f), 445, 8f);

            // Oficina en una caseta de obra junto a la entrada.
            var office = Shed(group, "OficinaDesguace", Rect.MinMaxRect(-186f, -342f, -176f, -337f), 2.9f, SouthSheet("ChapaCaseta", new Color(0.85f, 0.85f, 0.8f), 446), SheetTile, tarRoof, "oficina");
            Box("Ventana", office, G(-181f, -342.03f, 1.7f), new Vector3(2f, 0.9f, 0.04f), darkGlass, collider: false);
            Box("Escalones", group, G(-179f, -343f, 0.25f), new Vector3(1.2f, 0.5f, 1f), concreteMat);

            // Pilas de chatarra prensada (balas) y coches enteros sin ruedas, en calles.
            var paints = carPaints ?? carBodyPaints;
            var rust = Mat("PinturaOxido", new Color(0.38f, 0.2f, 0.12f));
            for (float x = -240f; x < -135f; x += 9f)
            for (float z = -440f; z < -350f; z += 14f)
            {
                if (x > -175f && z < -385f) continue; // sitio de la grúa y la prensa
                if (x < -225f && z < -425f) continue; // montaña de neumáticos
                if (rng.NextDouble() < 0.35)
                {
                    Wreck(group, G(x + Range(rng, -1f, 1f), z), Range(rng, -20f, 20f) + (rng.Next(2) == 0 ? 0f : 180f), rng);
                    continue;
                }
                int n = 2 + rng.Next(4);
                for (int k = 0; k < n; k++)
                {
                    var bale = Box("Chatarra", group, G(x + Range(rng, -0.3f, 0.3f), z + Range(rng, -0.3f, 0.3f), 0.38f + k * 0.76f), new Vector3(1.9f, 0.75f, 4.2f),
                        rng.NextDouble() < 0.35 ? rust : paints[rng.Next(paints.Length)]);
                    bale.transform.localRotation = Quaternion.Euler(Range(rng, -3f, 3f), 90f + Range(rng, -12f, 12f), Range(rng, -3f, 3f));
                }
            }

            // Grúa de electroimán: base de orugas, cabina, pluma de celosía y el imán colgando sobre una pila.
            var yellow = Mat("AmarilloMaquina", new Color(0.85f, 0.6f, 0.1f));
            var dark = Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f));
            var basePos = G(-165f, -410f);
            Box("Orugas", group, basePos + Vector3.up * 0.6f, new Vector3(4.2f, 1.2f, 5f), dark);
            Box("Cabina", group, basePos + new Vector3(0f, 2.2f, 0f), new Vector3(3.4f, 2f, 3.6f), yellow);
            Box("Contrapeso", group, basePos + new Vector3(0f, 2f, -2.4f), new Vector3(3f, 1.6f, 1.4f), dark, collider: false);
            var tip = basePos + new Vector3(12f, 17f, 14f);
            foreach (float o in new[] { -0.6f, 0.6f })
                Strut("Pluma", group, basePos + new Vector3(o, 3f, 1.5f), tip + new Vector3(o * 0.3f, 0f, 0f), 0.22f, yellow);
            for (int k = 1; k < 8; k++)
                Strut("Celosia", group, Vector3.Lerp(basePos + new Vector3(-0.6f, 3f, 1.5f), tip, k / 8f), Vector3.Lerp(basePos + new Vector3(0.6f, 3f, 1.5f), tip, (k + 0.5f) / 8f), 0.06f, yellow);
            var magnet = new Vector3(tip.x, 5.5f, tip.z);
            Strut("Cable", group, tip, magnet + Vector3.up * 0.5f, 0.05f, dark);
            Primitive(PrimitiveType.Cylinder, "Iman", group, magnet, new Vector3(2.2f, 0.3f, 2.2f), dark, keepCollider: false);
            for (int k = 0; k < 4; k++)
                Box("Chatarra", group, new Vector3(tip.x + Range(rng, -1f, 1f), 0.38f + k * 0.76f, tip.z + Range(rng, -1f, 1f)), new Vector3(1.9f, 0.75f, 4.2f), rust)
                    .transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f);

            // Prensa: caja de máquina con tolva y una bala recién salida.
            var press = G(-138f, -432f);
            Box("Prensa", group, press + Vector3.up * 1.6f, new Vector3(4f, 3.2f, 10f), Mat("VerdeMaquina", new Color(0.22f, 0.36f, 0.24f)));
            Box("Tolva", group, press + new Vector3(0f, 3.8f, 2f), new Vector3(4.6f, 1.2f, 4f), dark, collider: false);
            Box("Motor", group, press + new Vector3(2.6f, 1.2f, -3f), new Vector3(1.2f, 2.4f, 3f), yellow);
            RecordPolygon("nave", RectCorners(Rect.MinMaxRect(press.x - 2f, press.z - 5f, press.x + 2f, press.z + 5f)), 3.2f);

            // Montaña de neumáticos, barro y focos.
            var tireMat = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            for (int k = 0; k < 60; k++)
            {
                float a = Range(rng, 0f, Mathf.PI * 2f), d = Mathf.Sqrt((float)rng.NextDouble()) * 5f;
                float y = 0.15f + (5f - d) * 0.35f * (float)rng.NextDouble();
                Primitive(PrimitiveType.Cylinder, "Neumatico", group, G(-232f + Mathf.Cos(a) * d, -440f + Mathf.Sin(a) * d, y), new Vector3(0.75f, 0.13f, 0.75f), tireMat, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(Range(rng, -40f, 40f), 0f, Range(rng, -40f, 40f));
            }
            ColliderBox(group, "PilaNeumaticos", G(-232f, -440f, 0.8f), new Vector3(7f, 1.6f, 7f));
            // Sin manchas de barro: el material es para césped y sobre el asfalto oscuro se ve claro.
            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lamp = EmissiveMat("Proyector", new Color(1f, 0.95f, 0.85f), 1.5f);
            FloodTower(group, G(r.xMin + 2f, r.yMax - 2f), G(-190f, -390f), pole, lamp);
            FloodTower(group, G(r.xMax - 2f, r.yMin + 2f), G(-180f, -400f), pole, lamp);
            // Perro guardián: caseta.
            Box("CasetaPerro", group, G(-196f, -347f, 0.5f), new Vector3(1.2f, 1f, 1.4f), Mat("MaderaCaseta", new Color(0.4f, 0.28f, 0.18f)));
            WallSign(group, "Letrero_Perro", G(-194f, r.yMax + 0.05f, 2.1f), Vector3.forward, new[] { "BEWARE", "OF DOG" },
                new Color(0.95f, 0.95f, 0.92f), new Color(0.7f, 0.06f, 0.05f), 447, 1.6f);
        }

        /// <summary>Franja de maleza entre la autopista y J, con vallas publicitarias de cara a la autopista.</summary>
        static void WestOfJudson(Transform parent, System.Random rng)
        {
            var group = new GameObject("FranjaOesteJ").transform;
            group.SetParent(parent, false);
            var steel = Mat("AceroPoligono", new Color(0.4f, 0.42f, 0.42f));
            Billboard(group, G(-270f, -150f, LawnTop), 90f, steel, 0);
            Billboard(group, G(-270f, -270f, LawnTop), 90f, steel, 1);
            Billboard(group, G(-270f, -395f, LawnTop), 90f, steel, 0);
            var bark = LoadMat("Corteza");
            for (int i = 0; i < 90; i++)
            {
                float z = Range(rng, -480f, -95f);
                if (Mathf.Abs(z - SouthFreightZ) < FreightBedHalf + 1f) continue;
                Clump(group, rng, G(Range(rng, -274f, -265f), z, LawnTop), 0.4f, 1.4f);
            }
            for (int i = 0; i < 9; i++)
            {
                float z = Range(rng, -440f, -100f);
                if (Mathf.Abs(z - SouthFreightZ) < FreightBedHalf + 3f) continue;
                BareTree(group, G(Range(rng, -273f, -267f), z, LawnTop), bark, rng, Range(rng, 0.8f, 1.2f));
            }
        }

        // ---------- Fábrica abandonada con chimenea ----------

        static void AbandonedFactory(Transform parent, System.Random rng)
        {
            var group = new GameObject("FabricaAbandonada").transform;
            group.SetParent(parent, false);
            var lot = SouthFactoryLot;
            var r = SouthFactory;
            const float H = 8f, Rise = 3f, Tooth = 8f;
            var brick = TexturedMat("LadrilloFabrica", ProceduralTextures.PlainBrick("LadrilloFabrica", new Color(0.44f, 0.25f, 0.19f), 451));
            var brickTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight);
            var broken = Mat("CristalRoto", new Color(0.03f, 0.03f, 0.035f));
            var dirty = Mat("CristalSucio", new Color(0.32f, 0.36f, 0.33f));
            var steel = Mat("AceroOxidado", new Color(0.33f, 0.24f, 0.18f));

            TexturedBox("Muros", group, G(r.center.x, r.center.y, H / 2f), new Vector3(r.width, H, r.height), brick, brickTile, sidesOnly: true);
            Box("Forjado", group, G(r.center.x, r.center.y, H - 0.1f), new Vector3(r.width, 0.2f, r.height), tarRoof);
            Box("Cornisa", group, G(r.center.x, r.center.y, H - 0.4f), new Vector3(r.width + 0.4f, 0.5f, r.height + 0.4f), stoneTrim, collider: false);
            RecordPolygon("fabrica", RectCorners(r), H + Rise);

            // Ventanales de fábrica cada 4 m en las cuatro caras: unos rotos (negros), otros sucios.
            var corners = new[] { V(r.xMin, r.yMin), V(r.xMax, r.yMin), V(r.xMax, r.yMax), V(r.xMin, r.yMax) };
            for (int e = 0; e < 4; e++)
            {
                Vector2 a = corners[e], b = corners[(e + 1) % 4], d = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                for (float s = 3f; s + 2.6f < len - 1f; s += 4f)
                {
                    var w0 = a + d * s;
                    FacePanel("Ventanal", group, w0, w0 + d * 2.6f, 2.2f, 6.6f, 0f, 0.06f, rng.NextDouble() < 0.45 ? broken : dirty);
                    FacePanel("Dintel", group, w0 - d * 0.15f, w0 + d * 2.75f, 6.6f, 6.85f, 0f, 0.1f, stoneTrim);
                }
            }

            // Tejado en diente de sierra: faldón inclinado que sube hacia el norte y lucernario vertical.
            float slope = Mathf.Sqrt(Tooth * Tooth + Rise * Rise), angle = Mathf.Atan2(Rise, Tooth) * Mathf.Rad2Deg;
            var roofMat = Mat("TejadoFabrica", new Color(0.24f, 0.24f, 0.25f));
            for (float z = r.yMin; z + Tooth <= r.yMax + 0.01f; z += Tooth)
            {
                Box("Faldon", group, G(r.center.x, z + Tooth / 2f, H + Rise / 2f), new Vector3(r.width - 0.8f, 0.2f, slope), roofMat, collider: false)
                    .transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                Box("Lucernario", group, G(r.center.x, z + Tooth - 0.05f, H + Rise / 2f), new Vector3(r.width - 0.8f, Rise, 0.08f), rng.NextDouble() < 0.5 ? broken : dirty, collider: false);
            }
            foreach (float x in new[] { r.xMin + 0.2f, r.xMax - 0.2f })
                TexturedBox("Peto", group, G(x, r.center.y, H + Rise / 2f), new Vector3(0.4f, Rise, r.height), brick, brickTile);
            Box("PetoSur", group, G(r.center.x, r.yMin + 0.2f, H + 0.5f), new Vector3(r.width, 1f, 0.4f), brick);

            // Rótulo pintado y desvaído en la fachada norte (a Gantry), tapiados y grafitis.
            WallSign(group, "Letrero_Fabrica", G(r.center.x, r.yMax + 0.05f, 7.2f), Vector3.forward, new[] { "MIDLAND STEEL CASTINGS CO." },
                new Color(0.42f, 0.27f, 0.22f), new Color(0.78f, 0.74f, 0.64f), 452, 26f);
            foreach (float z in new[] { -350f, -372f, -398f })
                Box("Tapiado", group, G(r.xMin - 0.08f, z, 1.6f), new Vector3(0.05f, 3.2f, 3.4f), plywood, collider: false);
            Box("PortonFabrica", group, G(-75f, r.yMax + 0.04f, 2.6f), new Vector3(6f, 5.2f, 0.06f), steel, collider: false);
            if (graffitiMats != null)
                for (int i = 0; i < 9; i++)
                {
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    if (!g) continue;
                    int e = rng.Next(4);
                    Vector2 a = corners[e], b = corners[(e + 1) % 4];
                    var p = Vector2.Lerp(a, b, Range(rng, 0.1f, 0.9f));
                    var n = new Vector3((b - a).y, 0f, -(b - a).x).normalized;
                    WallGraffiti(group, G(p.x, p.y, Range(rng, 1.2f, 1.9f)) + n * 0.02f, n, Range(rng, 2.5f, 5f), g);
                }

            // Muelle de carga a lo largo de la fachada sur, junto al apartadero.
            Box("MuelleCarga", group, G(-75f, -431.25f, 0.55f), new Vector3(40f, 1.1f, 2.5f), concreteMat);
            Box("Marquesina", group, G(-75f, -431.5f, 5f), new Vector3(40f, 0.2f, 3f), steel, collider: false);

            // Chimenea de ladrillo de 45 m (hito: se ve desde todo el sur).
            var stackPos = G(-60f, -445f);
            var stackBrick = Mat("ChimeneaFabrica", new Color(0.4f, 0.28f, 0.22f));
            Box("BaseChimenea", group, stackPos + Vector3.up * 2f, new Vector3(5.5f, 4f, 5.5f), stackBrick);
            Primitive(PrimitiveType.Cylinder, "Chimenea", group, stackPos + Vector3.up * 14f, new Vector3(4.4f, 10f, 4.4f), stackBrick, keepCollider: true);
            Primitive(PrimitiveType.Cylinder, "Chimenea", group, stackPos + Vector3.up * 34f, new Vector3(3.6f, 10f, 3.6f), stackBrick, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Corona", group, stackPos + Vector3.up * 44.4f, new Vector3(4.1f, 0.6f, 4.1f), Mat("SiluetaOscura", new Color(0.26f, 0.25f, 0.25f)), keepCollider: false);
            foreach (float y in new[] { 12f, 22f, 30f, 38f })
                Primitive(PrimitiveType.Cylinder, "Zuncho", group, stackPos + Vector3.up * y, new Vector3(y < 24f ? 4.5f : 3.7f, 0.12f, y < 24f ? 4.5f : 3.7f), steel, keepCollider: false);
            RecordPolygon("chimenea", RectCorners(Rect.MinMaxRect(stackPos.x - 2.75f, stackPos.z - 2.75f, stackPos.x + 2.75f, stackPos.z + 2.75f)), 45f);

            // Valla de malla con agujeros, cadena en la puerta y NO TRESPASSING.
            ChainFence(group, G(lot.xMin + 0.3f, lot.yMax - 0.3f), G(-80f, lot.yMax - 0.3f));
            ChainFence(group, G(-72f, lot.yMax - 0.3f), G(lot.xMax - 0.3f, lot.yMax - 0.3f));
            ChainFence(group, G(lot.xMax - 0.3f, lot.yMax - 0.3f), G(lot.xMax - 0.3f, -429f));
            ChainFence(group, G(lot.xMax - 0.3f, -440f), G(lot.xMax - 0.3f, lot.yMin + 0.3f));
            ChainFence(group, G(lot.xMin + 0.3f, lot.yMin + 0.3f), G(lot.xMin + 0.3f, -400f));
            ChainFence(group, G(lot.xMin + 0.3f, -394f), G(lot.xMin + 0.3f, lot.yMax - 0.3f));
            ChainFence(group, G(lot.xMin + 0.3f, lot.yMin + 0.3f), G(lot.xMax - 0.3f, lot.yMin + 0.3f));
            WallSign(group, "Letrero_NoTrespassing", G(-76f, lot.yMax - 0.25f, 1.3f), Vector3.forward, new[] { "NO", "TRESPASSING" },
                new Color(0.95f, 0.95f, 0.92f), new Color(0.7f, 0.06f, 0.05f), 453, 1.4f);

            // Escombros, bidones, maleza y un coche quemado.
            var rubble = Mat("Escombro", new Color(0.45f, 0.42f, 0.4f));
            for (int i = 0; i < 14; i++)
                Box("Escombro", group, G(Range(rng, -99f, -49f), rng.NextDouble() < 0.5 ? Range(rng, -333f, -328f) : Range(rng, -452f, -438f), 0.2f),
                    new Vector3(Range(rng, 0.6f, 2f), Range(rng, 0.3f, 0.7f), Range(rng, 0.6f, 2f)), rubble).transform.localRotation = Quaternion.Euler(0f, Range(rng, 0f, 90f), 0f);
            var drum = Mat("BidonOxido", new Color(0.4f, 0.22f, 0.12f));
            for (int i = 0; i < 8; i++)
                Primitive(PrimitiveType.Cylinder, "Bidon", group, G(Range(rng, -95f, -55f), Range(rng, -452f, -440f), 0.45f), new Vector3(0.6f, 0.45f, 0.6f), drum, keepCollider: true);
            for (int i = 0; i < 70; i++)
            {
                float x = Range(rng, lot.xMin + 1f, lot.xMax - 1f), z = Range(rng, lot.yMin + 1f, lot.yMax - 1f);
                if (r.Contains(V(x, z)) || Mathf.Abs(z - SidingZ) < 2.2f) continue;
                Clump(group, rng, G(x, z), 0.4f, 1.6f);
            }
            Wreck(group, G(-88f, -444f), 30f, rng);
        }

        // ---------- Almacén dentro de la curva ----------

        static void Warehouse(Transform parent, System.Random rng)
        {
            var group = new GameObject("Almacen").transform;
            group.SetParent(parent, false);
            var r = SouthWarehouse;
            var sheet = SouthSheet("ChapaAlmacen", new Color(0.78f, 0.79f, 0.77f), 461);
            var shed = Shed(group, "NaveAlmacen", r, 10f, sheet, SheetTile, tarRoof, "nave");
            var blue = Mat("FranjaAlmacen", new Color(0.12f, 0.25f, 0.5f));
            foreach (var (a, b) in new[] { (V(r.xMin, r.yMin), V(r.xMax, r.yMin)), (V(r.xMax, r.yMin), V(r.xMax, r.yMax)), (V(r.xMax, r.yMax), V(r.xMin, r.yMax)), (V(r.xMin, r.yMax), V(r.xMin, r.yMin)) })
                FacePanel("Franja", shed, a, b, 8.6f, 9.3f, 0f, 0.05f, blue);
            WallSign(shed, "Letrero_Almacen", G(r.center.x, r.yMax + 0.06f, 6.8f), Vector3.forward, new[] { "LAKESIDE DISTRIBUTION" },
                new Color(0.12f, 0.25f, 0.5f), new Color(0.95f, 0.95f, 0.92f), 462, 22f);

            // Muelles en la fachada oeste: portón, topes y remolques arrimados.
            var door = TexturedMat("PortonCocheras", ProceduralTextures.Shutter("PortonCocheras", 422));
            var bumper = Mat("TopeMuelle", new Color(0.08f, 0.08f, 0.08f));
            int k = 0;
            for (float z = -383f; z <= -348f; z += 5f, k++)
            {
                Box("PortonMuelle", shed, G(r.xMin - 0.03f, z, 2.8f), new Vector3(0.06f, 3f, 3f), door, collider: false);
                foreach (float o in new[] { -1.7f, 1.7f })
                    Box("Tope", shed, G(r.xMin - 0.2f, z + o, 1.1f), new Vector3(0.4f, 0.6f, 0.3f), bumper, collider: false);
                Box("Pescante", shed, G(r.xMin - 0.6f, z, 4.6f), new Vector3(1.2f, 0.2f, 3.4f), concreteMat, collider: false);
                if (k % 3 != 1) Semi(group, G(r.xMin - 7f, z), 270f, false, rng);
            }
            // Tractoras aparcadas y garita con barrera en la entrada desde Gantry.
            foreach (float x in new[] { -22f, -14f })
                Semi(group, G(x, -356f), 0f, true, rng);
            var shack = Shed(group, "Garita", Rect.MinMaxRect(-31f, -334f, -28f, -331f), 2.8f, SouthSheet("ChapaCaseta", new Color(0.85f, 0.85f, 0.8f), 446), SheetTile, tarRoof, "caseta");
            Box("VentanaGarita", shack, G(-27.97f, -332.5f, 1.7f), new Vector3(0.04f, 0.9f, 2f), darkGlass, collider: false);
            Box("PosteBarrera", group, G(-27f, -329f, 0.5f), new Vector3(0.3f, 1f, 0.3f), Mat("AmarilloMaquina", new Color(0.85f, 0.6f, 0.1f)));
            for (int s = 0; s < 8; s++)
                Box("Barrera", group, G(-26.2f + s, -329f, 1f), new Vector3(1f, 0.1f, 0.1f), s % 2 == 0 ? Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f)) : Mat("BlancoBarrera", new Color(0.9f, 0.9f, 0.88f)), collider: false);
            ChainFence(group, G(-32f, -326.8f), G(-29f, -326.8f));
            ChainFence(group, G(-17f, -326.8f), G(99.7f, -326.8f));
            ChainFence(group, G(99.7f, -326.8f), G(99.7f, -402.2f));
            // Aparatos en el tejado.
            for (int i = 0; i < 5; i++)
                Box("AparatoTejado", group, G(Range(rng, r.xMin + 5f, r.xMax - 5f), Range(rng, r.yMin + 5f, r.yMax - 5f), 10.8f), new Vector3(2.5f, 1.2f, 3.5f),
                    Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f)), collider: false);
            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lamp = EmissiveMat("Proyector", new Color(1f, 0.95f, 0.85f), 1.5f);
            FloodTower(group, G(-5f, -340f), G(10f, -370f), pole, lamp);
        }

        // ---------- Parque de tubos junto a la vía ----------

        static void PipeYard(Transform parent, System.Random rng)
        {
            var group = new GameObject("ParqueTubos").transform;
            group.SetParent(parent, false);
            var r = SouthPipeYard;
            var pipeMats = new[] { Mat("TuboOxido", new Color(0.42f, 0.24f, 0.14f)), Mat("TuboNegro", new Color(0.12f, 0.12f, 0.12f)), Mat("TuboGalvanizado", new Color(0.55f, 0.56f, 0.57f)) };
            // Pilas de tubos en pirámide (a lo largo de X), con calzos de madera.
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            foreach (var (x, z) in new[] { (35f, -425f), (52f, -425f), (35f, -436f), (52f, -436f), (35f, -447f) })
            {
                float radius = Range(rng, 0.3f, 0.6f);
                var mat = pipeMats[rng.Next(pipeMats.Length)];
                int rows = 3 + rng.Next(2);
                for (int row = 0; row < rows; row++)
                {
                    int count = 6 - row;
                    for (int i = 0; i < count; i++)
                    {
                        float off = (i - (count - 1) / 2f) * radius * 2f;
                        Primitive(PrimitiveType.Cylinder, "Tubo", group, G(x, z + off, 0.25f + radius + row * radius * 1.73f), new Vector3(radius * 2f, 6f, radius * 2f), mat, keepCollider: false)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                }
                foreach (float o in new[] { -4f, 0f, 4f })
                    Box("Calzo", group, G(x + o, z, 0.12f), new Vector3(0.25f, 0.25f, 6f * radius * 2f + 0.5f), wood, collider: false);
                ColliderBox(group, "PilaTubos", G(x, z, 0.25f + rows * radius * 0.9f), new Vector3(12f, rows * radius * 1.8f, 12f * radius));
            }
            // Perfiles de acero (vigas en I) apilados.
            var beam = Mat("PerfilAcero", new Color(0.36f, 0.3f, 0.26f));
            foreach (var (x, z) in new[] { (72f, -430f), (88f, -430f), (80f, -444f) })
                for (int layer = 0; layer < 4; layer++)
                for (int i = 0; i < 5; i++)
                    Box("Perfil", group, G(x, z - 2f + i * 1f, 0.2f + layer * 0.42f), new Vector3(12f, 0.4f, 0.3f), beam, collider: i == 0);
            // Puente grúa: dos pórticos y viga de rodadura.
            var yellow = Mat("AmarilloMaquina", new Color(0.85f, 0.6f, 0.1f));
            foreach (float x in new[] { 30f, 92f })
            {
                foreach (float z in new[] { r.yMin + 1f, r.yMax - 1f })
                    Box("PilarGrua", group, G(x, z, 4.5f), new Vector3(0.6f, 9f, 0.6f), yellow);
            }
            foreach (float z in new[] { r.yMin + 1f, r.yMax - 1f })
                Box("Rodadura", group, G((30f + 92f) / 2f, z, 9.2f), new Vector3(63f, 0.6f, 0.6f), yellow, collider: false);
            Box("VigaGrua", group, G(60f, r.center.y, 9.8f), new Vector3(1f, 0.8f, r.height - 1f), yellow, collider: false);
            Box("Carro", group, G(60f, -432f, 9.2f), new Vector3(1.6f, 0.8f, 1.6f), Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f)), collider: false);
            // Valla con entrada desde S WHARF RD.
            ChainFence(group, G(22f, -417.8f), G(58f, -417.8f));
            ChainFence(group, G(68f, -417.8f), G(99.7f, -417.8f));
            ChainFence(group, G(99.7f, -417.8f), G(99.7f, -452.8f));
            ChainFence(group, G(99.7f, -452.8f), G(32f, -452.8f));
            ChainFence(group, G(22f, -417.8f), G(22f, -449f));
            Semi(group, G(63f, -440f), 0f, true, rng);
        }

        // ---------- Frigorífico y montón de sal ----------

        static void ColdStorage(Transform parent, System.Random rng)
        {
            var group = new GameObject("Frigorifico").transform;
            group.SetParent(parent, false);
            var r = SouthColdStorage;
            var panel = SouthSheet("PanelFrigorifico", new Color(0.88f, 0.88f, 0.86f), 471);
            var shed = Shed(group, "NaveFrigorifico", r, 13f, panel, new Vector2(ProceduralTextures.CorrugatedTileU * 3f, ProceduralTextures.CorrugatedTileV * 2f), tarRoof, "nave");
            WallSign(shed, "Letrero_Frigorifico", G(r.center.x, r.yMax + 0.06f, 10.5f), Vector3.forward, new[] { "ARCTIC COLD STORAGE" },
                new Color(0.1f, 0.35f, 0.55f), new Color(0.95f, 0.95f, 0.92f), 472, 26f);
            // Oficinas de ladrillo en el extremo oeste, con aparcamiento hacia la avenida.
            var brick = TexturedMat("LadrilloOficina", ProceduralTextures.PlainBrick("LadrilloOficina", new Color(0.5f, 0.32f, 0.24f), 473));
            var office = Shed(group, "OficinasFrigorifico", Rect.MinMaxRect(126f, -385f, 140f, -360f), 7f, brick, new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight), tarRoof, "oficina");
            for (float z = -383f; z < -361f; z += 3f)
                foreach (float y in new[] { 1.2f, 4.2f })
                    Box("Ventana", office, G(125.97f, z + 1f, y + 0.8f), new Vector3(0.04f, 1.4f, 1.8f), darkGlass, collider: false);
            for (int i = 0; i < 4; i++) ParkedCar(group, G(130f, -392f - i * 2.8f), 0f, rng);

            // Muelles en la fachada sur con remolques frigoríficos y tractoras en la explanada.
            var door = TexturedMat("PortonCocheras", ProceduralTextures.Shutter("PortonCocheras", 422));
            int k = 0;
            for (float x = 150f; x <= 232f; x += 6f, k++)
            {
                Box("PortonMuelle", shed, G(x, r.yMin - 0.03f, 2.8f), new Vector3(3f, 3f, 0.06f), door, collider: false);
                Box("Abrigo", shed, G(x, r.yMin - 0.4f, 2.8f), new Vector3(3.6f, 3.6f, 0.8f), Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f)), collider: false);
                bool tractor = k % 3 == 0; // con la tractora delante, el remolque queda más atrás en el morro
                if (k % 4 != 2) Semi(group, G(x, r.yMin - (tractor ? 11.5f : 7f)), 180f, tractor, rng);
            }
            for (int i = 0; i < 6; i++)
                Box("EquipoFrioTejado", group, G(150f + i * 16f, r.center.y + Range(rng, -15f, 15f), 13.8f), new Vector3(3f, 1.4f, 6f), Mat("EquipoFrio", new Color(0.82f, 0.82f, 0.8f)), collider: false);
            ChainFence(group, G(122.5f, -326.8f), G(160f, -326.8f));
            ChainFence(group, G(172f, -326.8f), G(SouthEdgeX - 0.5f, -326.8f));
            ChainFence(group, G(122.5f, -452.8f), G(SouthEdgeX - 0.5f, -452.8f));
            var pole = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lamp = EmissiveMat("Proyector", new Color(1f, 0.95f, 0.85f), 1.5f);
            FloodTower(group, G(244f, -440f), G(200f, -430f), pole, lamp);

            // Montón de sal de las carreteras, contenido por bloques de hormigón, con la cinta que llega al muelle.
            var salt = Mat("SalCarretera", new Color(0.8f, 0.8f, 0.78f));
            var pile = Primitive(PrimitiveType.Sphere, "MontonSal", group, G(SaltPile.x, SaltPile.y), new Vector3(36f, 18f, 30f), salt, keepCollider: false);
            pile.AddComponent<MeshCollider>().sharedMesh = pile.GetComponent<MeshFilter>().sharedMesh;
            RecordPolygon("sal", RectCorners(Rect.MinMaxRect(SaltPile.x - 18f, SaltPile.y - 15f, SaltPile.x + 18f, SaltPile.y + 15f)), 9f, occupy: true);
            Box("Lona", group, G(SaltPile.x - 6f, SaltPile.y + 6f, 6.5f), new Vector3(12f, 0.05f, 10f), Mat("LonaAzul", new Color(0.15f, 0.3f, 0.55f)), collider: false)
                .transform.localRotation = Quaternion.Euler(-22f, 20f, 10f);
            for (float x = SaltPile.x - 19f; x <= SaltPile.x + 19f; x += 1.6f)
                Box("Bloque", group, G(x, SaltPile.y + 16.5f, 1.2f), new Vector3(1.5f, 2.4f, 0.8f), concreteMat);
            for (float z = SaltPile.y - 15f; z <= SaltPile.y + 16f; z += 1.6f)
                Box("Bloque", group, G(SaltPile.x - 19.5f, z, 1.2f), new Vector3(0.8f, 2.4f, 1.5f), concreteMat);
            var steel = Mat("AceroPoligono", new Color(0.4f, 0.42f, 0.42f));
            var start = G(SaltPile.x, SaltPile.y - 14f, 2f);
            var end = G(SaltPile.x, SouthQuayZ - 1.5f, 10f);
            Strut("CintaTransportadora", group, start, end, 1.4f, steel);
            Strut("CubiertaCinta", group, start + Vector3.up * 1f, end + Vector3.up * 1f, 1.2f, Mat("ChapaCinta", new Color(0.55f, 0.56f, 0.55f)));
            foreach (float z in new[] { -440f, -452.5f, -467f, -483.5f })
            {
                float t = (z - start.z) / (end.z - start.z);
                float y = Mathf.Lerp(start.y, end.y, t);
                foreach (float o in new[] { -1f, 1f })
                    Box("PataCinta", group, G(SaltPile.x + o, z, (y - 0.7f) / 2f), new Vector3(0.3f, y - 0.7f, 0.3f), steel);
            }
            Box("Tolva", group, end + new Vector3(0f, -1.2f, -0.5f), new Vector3(2.4f, 2f, 2.4f), steel, collider: false);
            // Pala cargadora junto al montón.
            var yellow = Mat("AmarilloMaquina", new Color(0.85f, 0.6f, 0.1f));
            var loader = G(250f, -440f);
            Box("Pala", group, loader + Vector3.up * 1.4f, new Vector3(2.6f, 1.6f, 5f), yellow);
            Box("CabinaPala", group, loader + new Vector3(0f, 3f, -0.8f), new Vector3(2f, 1.6f, 2f), yellow);
            Box("Cazo", group, loader + new Vector3(0f, 0.6f, 3.2f), new Vector3(3f, 1.2f, 1.2f), Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f)), collider: false);
        }

        /// <summary>Descampado del sureste (queda para la fase del este): maleza, árboles pelados y un cartel de venta.</summary>
        static void SouthEastLot(Transform parent, System.Random rng)
        {
            var group = new GameObject("DescampadoSureste").transform;
            group.SetParent(parent, false);
            var bark = LoadMat("Corteza");
            for (int i = 0; i < 160; i++)
                Clump(group, rng, G(Range(rng, 121f, SouthEdgeX - 1f), Range(rng, -310f, -109f), LawnTop), 0.4f, 1.6f);
            for (int i = 0; i < 12; i++)
                BareTree(group, G(Range(rng, 125f, SouthEdgeX - 4f), Range(rng, -305f, -115f), LawnTop), bark, rng, Range(rng, 0.8f, 1.3f));
            ChainFence(group, G(121f, -311.2f, LawnTop), G(200f, -311.2f, LawnTop), 1.8f);
            ChainFence(group, G(212f, -311.2f, LawnTop), G(SouthEdgeX - 1f, -311.2f, LawnTop), 1.8f);
            WallSign(group, "Letrero_SeVende", G(206f, -311f, 2.4f), Vector3.back, new[] { "FOR SALE", "INDUSTRIAL LAND", "12 ACRES" },
                new Color(0.95f, 0.95f, 0.92f), new Color(0.1f, 0.25f, 0.5f), 481, 3.2f);
            foreach (float dx in new[] { -1.4f, 1.4f })
                Box("PosteCartel", group, G(206f + dx, -310.9f, 1.4f), new Vector3(0.12f, 2.8f, 0.12f), Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f)), collider: false);
            for (int i = 0; i < 8; i++) GarbageBag(group, G(Range(rng, 125f, 290f), Range(rng, -305f, -115f), LawnTop), rng);
            for (int i = 0; i < 3; i++) Wreck(group, G(Range(rng, 140f, 280f), Range(rng, -290f, -130f), LawnTop), Range(rng, 0f, 360f), rng);
        }

        // ---------- S WHARF RD: la curva ----------

        static void BuildWharfCurve(Transform parent)
        {
            var group = new GameObject("CurvaWharf").transform;
            group.SetParent(parent, false);
            var road = LoadMat("CalleCasas") ?? TexturedMat("CalleCasas", ProceduralTextures.Road("CalleCasas", HousesRoadWidth, 8));
            float half = HousesRoadWidth / 2f;
            ArcStrip("CalleCurva", group, WharfCenter, WharfRadius - half, WharfRadius + half, 180f, 270f, 0f, RoadTop, road, Vector2.one * HousesRoadWidth, collider: false, segments: 32);
            ArcStrip("AceraCurvaDentro", group, WharfCenter, WharfRadius - half - HousesSidewalk, WharfRadius - half, 180f, 270f, 0f, SidewalkTop, sidewalkMat,
                Vector2.one * ProceduralTextures.SidewalkTile, collider: true, segments: 32);
            ArcStrip("AceraCurvaFuera", group, WharfCenter, WharfRadius + half, WharfRadius + half + HousesSidewalk, 180f, 270f, 0f, SidewalkTop, sidewalkMat,
                Vector2.one * ProceduralTextures.SidewalkTile, collider: true, segments: 32);
            // Farolas por fuera de la curva y una señal de curva peligrosa en cada entrada.
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            foreach (float a in new[] { 200f, 235f, 260f })
            {
                float t = a * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t));
                StreetLight(group, new Vector3(WharfCenter.x, 0f, WharfCenter.y) + dir * (WharfRadius + half + 0.6f), -dir, pole, lamp);
            }
            var curve = TexturedMat("SenalCurva", ProceduralTextures.Placard("SenalCurva", new[] { "CURVE", "15 MPH" }, new Color(0.95f, 0.75f, 0.1f), new Color(0.05f, 0.05f, 0.05f), 491));
            var steel = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            foreach (var (p, n) in new[] { (G(-46.3f, -353f, SidewalkTop), Vector3.forward), (G(20f, -403.7f, SidewalkTop), Vector3.right) })
            {
                Primitive(PrimitiveType.Cylinder, "Poste", group, p + Vector3.up * 1.2f, new Vector3(0.06f, 1.2f, 0.06f), steel, keepCollider: true);
                float aspect = (float)curve.mainTexture.width / curve.mainTexture.height;
                SignQuad(group, p + Vector3.up * 2.3f + n * 0.05f, n, 0.8f, 0.8f / aspect, curve);
            }
        }

        // ---------- Vía de mercancías ----------

        // Pasos a nivel: (x0, x1) del cruce con aceras, y la calzada (r0, r1) con su altura.
        static readonly (float x0, float x1, float r0, float r1, float top)[] FreightCrossings =
        {
            (-264.5f, -249.5f, -262f, -252f, RoadTop),
            (-120f, -100f, -117f, -103f, MainRoadTop),
            (100f, 120f, 103f, 117f, MainRoadTop),
        };

        static void BuildFreightLine(Transform parent, System.Random rng)
        {
            var group = new GameObject("ViaMercancias").transform;
            group.SetParent(parent, false);
            // Tramos entre pasos a nivel: lecho de balasto ancho y dos vías.
            var gaps = new List<(float, float)>();
            float x = -300f;
            foreach (var c in FreightCrossings) { gaps.Add((x, c.x0)); x = c.x1; }
            gaps.Add((x, 300f));
            var ballastTile = Vector2.one * ProceduralTextures.BallastTile;
            foreach (var (a, b) in gaps)
            {
                TexturedBox("LechoMercancias", group, G((a + b) / 2f, SouthFreightZ, 0.035f), new Vector3(b - a, 0.07f, FreightBedHalf * 2f), yardBallast, ballastTile, collider: false);
                foreach (float z in FreightTracksZ) YardTrack(group, V(a, z), V(b, z), false);
            }
            // Pasos a nivel: losas de hormigón con los carriles enrasados (sin colisión: se pisa la calle de debajo).
            var slab = Mat("LosaPasoNivel", new Color(0.5f, 0.49f, 0.46f));
            var rubber = Mat("GomaPasoNivel", new Color(0.12f, 0.12f, 0.12f));
            foreach (var c in FreightCrossings)
                foreach (float z in FreightTracksZ)
                {
                    foreach (var (a, b, top) in new[] { (c.x0, c.r0, SidewalkTop), (c.r0, c.r1, c.top), (c.r1, c.x1, SidewalkTop) })
                    {
                        Box("LosaPasoNivel", group, G((a + b) / 2f, z, top + 0.006f), new Vector3(b - a, 0.012f, 3f), slab, collider: false);
                        foreach (float o in new[] { -0.72f, 0.72f })
                            Box("CarrilEnrasado", group, G((a + b) / 2f, z + o, top + 0.014f), new Vector3(b - a, 0.012f, 0.1f), rubber, collider: false);
                    }
                }

            // Apartadero de la fábrica: sale de la vía norte con un desvío suave (≤ 21°) y acaba en un tope junto al muelle de carga.
            var siding = new[] { V(-97f, SidingZ), V(-45f, SidingZ), V(-30f, -436.5f), V(-15f, -441f), V(0f, -447f), V(15f, -452.5f), V(30f, -456.4f), V(45f, FreightTracksZ[0]) };
            for (int i = 0; i + 1 < siding.Length; i++) YardTrack(group, siding[i], siding[i + 1], false, overlap: 0.4f);
            Box("TopeApartadero", group, G(-98f, SidingZ, 0.55f), new Vector3(0.6f, 1.1f, 2.4f), concreteMat);
            Box("TopeViga", group, G(-97.6f, SidingZ, 1f), new Vector3(0.12f, 0.3f, 2.2f), Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f)), collider: false);
            // Un vagón olvidado en el apartadero.
            var shells = new[] { LoadMat("ChapaVagon_Oxido"), LoadMat("ChapaVagon_Amarillo"), LoadMat("ChapaVagon_Gris") };
            var chassis = Mat("BajosVagon", new Color(0.08f, 0.08f, 0.08f));
            void BoxCar(Vector3 at, float yaw)
            {
                var car = new GameObject("VagonMercancias").transform;
                car.SetParent(group, false);
                car.localPosition = at;
                car.localRotation = Quaternion.Euler(0f, yaw, 0f);
                var shell = shells[rng.Next(shells.Length)] ?? chassis;
                TexturedBox("Caja", car, new Vector3(0f, 3.15f, 0f), new Vector3(3.1f, 4.2f, 15f), shell, SheetTile);
                Box("Bastidor", car, new Vector3(0f, 0.85f, 0f), new Vector3(2.7f, 0.5f, 14.6f), chassis, collider: false);
                foreach (float o in new[] { -5.5f, 5.5f })
                    Box("Bogie", car, new Vector3(0f, 0.55f, o), new Vector3(2.5f, 0.6f, 2.6f), chassis, collider: false);
                if (graffitiMats != null && rng.NextDouble() < 0.6)
                {
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    float side = rng.Next(2) == 0 ? -1f : 1f;
                    if (g) WallGraffiti(car, new Vector3(side * 1.56f, 2.6f, Range(rng, -3f, 3f)), side > 0f ? Vector3.right : Vector3.left, Range(rng, 4f, 7f), g);
                }
            }
            BoxCar(G(-80f, SidingZ, 0.1f), 90f);
            // Tren de mercancías parado en la vía sur, entre J y S CALDER AVE, y cisternas también en la vía sur, al este de
            // la avenida: la vía norte queda libre para el tren que pasa (Freight.cs).
            for (int k = 0; k < 7; k++) BoxCar(G(-240f + k * 16.2f, FreightTracksZ[1], 0.1f), 90f);
            var tank = Mat("Cisterna", new Color(0.1f, 0.1f, 0.11f));
            for (int k = 0; k < 4; k++)
            {
                var at = G(140f + k * 16.5f, FreightTracksZ[1], 0.1f);
                Primitive(PrimitiveType.Cylinder, "Cisterna", group, at + Vector3.up * 2.6f, new Vector3(3f, 7f, 3f), tank, keepCollider: true)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Primitive(PrimitiveType.Cylinder, "Domo", group, at + Vector3.up * 4.2f, new Vector3(0.9f, 0.3f, 0.9f), tank, keepCollider: false);
                Box("Bastidor", group, at + Vector3.up * 0.85f, new Vector3(15f, 0.5f, 2.6f), chassis, collider: false);
                foreach (float o in new[] { -5.5f, 5.5f })
                    Box("Bogie", group, at + new Vector3(o, 0.55f, 0f), new Vector3(2.6f, 0.6f, 2.5f), chassis, collider: false);
            }
            // Armarios de señalización junto a la vía.
            var cabinet = Mat("ArmarioVia", new Color(0.55f, 0.57f, 0.55f));
            foreach (float cx in new[] { -268f, -123f, 97f, 123f })
                Box("ArmarioVia", group, G(cx, SouthFreightZ + FreightBedHalf + 0.6f, 0.8f), new Vector3(1.2f, 1.6f, 0.8f), cabinet);
        }

        // ---------- Canal, puentes y la otra orilla ----------

        static void BuildCanal(Transform parent, System.Random rng)
        {
            var group = new GameObject("Canal").transform;
            group.SetParent(parent, false);
            float midZ = (SouthQuayZ + CanalFarZ) / 2f, width = SouthQuayZ - CanalFarZ;
            var water = Mat("AguaCanal", new Color(0.13f, 0.16f, 0.15f));
            water.SetFloat("_Smoothness", 0.88f);
            Box("Agua", group, G(0f, midZ, CanalWater - 0.05f), new Vector3(600f, 0.1f, width), water, collider: false);
            Box("Fondo", group, G(0f, midZ, -4.6f), new Vector3(600f, 0.2f, width), Mat("FondoCanal", new Color(0.05f, 0.06f, 0.05f)), collider: false);
            var wallMat = LoadMat("MuroContencion") ?? concreteMat;
            var wallTile = new Vector2(ProceduralTextures.WallTileWidth, ProceduralTextures.WallTileHeight);
            TexturedBox("MuroMuelle", group, G(0f, SouthQuayZ - 0.25f, -2.25f), new Vector3(600f, 4.5f, 0.5f), wallMat, wallTile);
            TexturedBox("MuroOrilla", group, G(0f, CanalFarZ - 0.25f, -2.25f), new Vector3(600f, 4.5f, 0.5f), wallMat, wallTile, collider: false);

            // Barandilla, bolardos y defensas a lo largo del muelle; detrás, una pared invisible para no caer al agua
            // (salvo en el puente fijo de S MARLOW AVE, que tiene sus barandillas).
            var fenceTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.1f);
            foreach (var (a, b) in new[] { (-300f, -120f), (-100f, 100f), (120f, 300f) })
                FenceSegment(group, G(a, SouthQuayZ + 0.25f, SidewalkTop), G(b, SouthQuayZ + 0.25f, SidewalkTop), 1.1f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: true);
            foreach (var (a, b) in new[] { (-300f, 100f), (120f, 300f) })
                ColliderBox(group, "LimiteCanal", G((a + b) / 2f, SouthQuayZ - 0.1f, 1.5f), new Vector3(b - a, 5f, 0.2f));
            var black = Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f));
            var tire = Mat("Neumatico", new Color(0.06f, 0.06f, 0.06f));
            for (float bx = -290f; bx < 300f; bx += 15f)
            {
                if (Mathf.Abs(Mathf.Abs(bx) - MainRoadX) < 12f) continue;
                Primitive(PrimitiveType.Cylinder, "Bolardo", group, G(bx, SouthQuayZ + 0.8f, SidewalkTop + 0.3f), new Vector3(0.45f, 0.3f, 0.45f), black, keepCollider: true);
                Primitive(PrimitiveType.Cylinder, "Defensa", group, G(bx + 7f, SouthQuayZ - 0.65f, -0.9f), new Vector3(0.9f, 0.18f, 0.9f), tire, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // S CALDER AVE: puente basculante con las dos hojas levantadas (hito), barreras bajadas, semáforo rojo y caseta.
            var deck = Mat("TableroPuente", new Color(0.2f, 0.21f, 0.22f));
            var truss = Mat("AceroPuente", new Color(0.3f, 0.36f, 0.4f));
            foreach (var (hingeZ, angle, dir) in new[] { (SouthQuayZ, 72f, -1f), (CanalFarZ, -72f, 1f) })
            {
                var leaf = new GameObject("HojaPuente").transform;
                leaf.SetParent(group, false);
                leaf.localPosition = G(-MainRoadX, hingeZ, 0f);
                leaf.localRotation = Quaternion.Euler(angle, 0f, 0f);
                float len = width / 2f;
                Box("Tablero", leaf, new Vector3(0f, -0.4f, dir * len / 2f), new Vector3(20f, 0.8f, len), deck);
                foreach (float side in new[] { -1f, 1f })
                {
                    Box("Celosia", leaf, new Vector3(side * 9.8f, 0.6f, dir * len / 2f), new Vector3(0.35f, 1.2f, len), truss, collider: false);
                    for (float s = 1f; s < len; s += 2.2f)
                        Strut("Diagonal", leaf, new Vector3(side * 9.8f, 0f, dir * s), new Vector3(side * 9.8f, 1.2f, dir * (s + 1.1f)), 0.12f, truss);
                }
                Box("Contrapeso", leaf, new Vector3(0f, -1.5f, -dir * 2.5f), new Vector3(18f, 2.2f, 4f), concreteMat, collider: false);
            }
            ColliderBox(group, "LimitePuente", G(-MainRoadX, SouthQuayZ - 0.1f, 3f), new Vector3(20f, 6f, 0.2f));
            var red = EmissiveMat("LuzRojaPuente", new Color(0.9f, 0.08f, 0.05f), 3f);
            var stripeR = Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f));
            var stripeW = Mat("BlancoBarrera", new Color(0.9f, 0.9f, 0.88f));
            foreach (var (postX, dirX) in new[] { (-MainRoadX - RoadWidth / 2f - 1.4f, 1f), (-MainRoadX + RoadWidth / 2f + 1.4f, -1f) })
            {
                var post = G(postX, SouthQuayZ + 3.5f, SidewalkTop);
                Box("PosteBarrera", group, post + Vector3.up * 0.6f, new Vector3(0.5f, 1.2f, 0.5f), stripeW);
                for (int s = 0; s < 7; s++)
                    Box("Barrera", group, post + new Vector3(dirX * (0.8f + s), 1.05f, 0f), new Vector3(1f, 0.12f, 0.12f), s % 2 == 0 ? stripeR : stripeW, collider: false);
                Primitive(PrimitiveType.Cylinder, "Poste", group, post + new Vector3(-dirX * 0.6f, 1.6f, 0f), new Vector3(0.12f, 1.6f, 0.12f), black, keepCollider: true);
                Box("Semaforo", group, post + new Vector3(-dirX * 0.6f, 3.4f, 0.2f), new Vector3(0.45f, 0.9f, 0.3f), black, collider: false);
                Primitive(PrimitiveType.Sphere, "LuzRoja", group, post + new Vector3(-dirX * 0.6f, 3.6f, 0.37f), Vector3.one * 0.25f, red, keepCollider: false);
            }
            WallSign(group, "Letrero_PuenteLevantado", G(-MainRoadX, SouthQuayZ + 3.6f, 2.4f), Vector3.forward, new[] { "BRIDGE UP", "STOP" },
                new Color(0.95f, 0.75f, 0.1f), new Color(0.05f, 0.05f, 0.05f), 501, 1.8f);
            Primitive(PrimitiveType.Cylinder, "PosteCartel", group, G(-MainRoadX, SouthQuayZ + 3.5f, 1.05f), new Vector3(0.08f, 0.95f, 0.08f), black, keepCollider: false);
            // Caseta del puentero: base de ladrillo y cabina acristalada que vuela sobre el muelle.
            var house = G(-126f, SouthQuayZ + 2.5f);
            Box("CasetaBase", group, house + Vector3.up * 1.6f, new Vector3(3.6f, 3.2f, 3.6f), LoadMat("LadrilloFabrica") ?? concreteMat);
            Box("CasetaCabina", group, house + Vector3.up * 4.4f, new Vector3(4.6f, 2.4f, 4.6f), truss);
            foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                Box("CristalCaseta", group, house + Vector3.up * 4.5f + n * 2.32f, new Vector3(n.x != 0f ? 0.04f : 3.8f, 1.3f, n.z != 0f ? 0.04f : 3.8f), darkGlass, collider: false);
            Box("TejadoCaseta", group, house + Vector3.up * 5.75f, new Vector3(5.2f, 0.3f, 5.2f), tarRoof, collider: false);
            RecordPolygon("caseta", RectCorners(Rect.MinMaxRect(house.x - 1.8f, house.z - 1.8f, house.x + 1.8f, house.z + 1.8f)), 5.9f);

            // S MARLOW AVE: puente fijo de vigas, cortado al otro lado por obras.
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            Box("TableroMarlow", group, G(MainRoadX, midZ, -0.4f), new Vector3(20f, 0.8f, width + 1f), asphalt);
            foreach (float side in new[] { -1f, 1f })
            {
                float sx = MainRoadX + side * (RoadWidth / 2f + SidewalkWidth / 2f);
                TexturedBox("AceraPuente", group, G(sx, midZ, SidewalkTop / 2f), new Vector3(SidewalkWidth, SidewalkTop, width), sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);
                Box("VigaBorde", group, G(MainRoadX + side * 9.9f, midZ, -1.2f), new Vector3(0.4f, 1.6f, width), truss, collider: false);
                FenceSegment(group, G(MainRoadX + side * 9.85f, SouthQuayZ, SidewalkTop), G(MainRoadX + side * 9.85f, CanalFarZ, SidewalkTop), 1.1f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: true);
                ColliderBox(group, "LimitePuente", G(MainRoadX + side * 10.05f, midZ, 2.5f), new Vector3(0.2f, 5f, width));
            }
            float closeZ = CanalFarZ + 2f;
            for (float bx = MainRoadX - 9.5f; bx < MainRoadX + 9.5f; bx += 3.05f)
            {
                Box("Barrera", group, G(bx + 1.5f, closeZ, 0.2f), new Vector3(3f, 0.4f, 0.6f), concreteMat);
                Box("BarreraAlta", group, G(bx + 1.5f, closeZ, 0.6f), new Vector3(3f, 0.4f, 0.3f), concreteMat, collider: false);
            }
            ColliderBox(group, "LimitePuente", G(MainRoadX, closeZ - 0.5f, 2.5f), new Vector3(20f, 5f, 0.2f));
            WallSign(group, "Letrero_PuenteCerrado", G(MainRoadX, closeZ + 0.35f, 1.6f), Vector3.forward, new[] { "BRIDGE CLOSED", "DETOUR" },
                new Color(0.95f, 0.55f, 0.1f), new Color(0.08f, 0.08f, 0.08f), 502, 2.6f);
            var drum = Mat("BidonObra", new Color(0.95f, 0.45f, 0.1f));
            for (int i = 0; i < 6; i++)
                Primitive(PrimitiveType.Cylinder, "BidonObra", group, G(MainRoadX + Range(rng, -6f, 6f), closeZ + Range(rng, 2f, 8f), 0.5f), new Vector3(0.6f, 0.5f, 0.6f), drum, keepCollider: true);

            // Postes de amarre de madera en el agua junto a los puentes.
            var wood = Mat("MaderaPoste", new Color(0.3f, 0.22f, 0.15f));
            foreach (float px in new[] { -122f, -98f, 98f, 122f })
                for (int i = 0; i < 3; i++)
                    Primitive(PrimitiveType.Cylinder, "Pilote", group, G(px + i * 0.5f * Mathf.Sign(px), SouthQuayZ - 3f - i * 0.4f, -1.5f), new Vector3(0.4f, 2.5f, 0.4f), wood, keepCollider: false);
        }

        /// <summary>La otra orilla del canal, sin colisiones: silos, grúas, depósitos, chimeneas y bloques que borra la niebla.</summary>
        static void BuildSouthSkyline(Transform world)
        {
            var group = new GameObject("Horizonte_Sur").transform;
            group.SetParent(world, false);
            var rng = new System.Random(4051);
            Box("SueloLejano", group, G(0f, -700f, -0.1f), new Vector3(1400f, 0.2f, 380f), Mat("SueloLejano", new Color(0.5f, 0.5f, 0.51f)), collider: false);
            var concrete = Mat("SiluetaHormigon", new Color(0.62f, 0.61f, 0.58f));
            var far = new[] { Mat("SiluetaLadrillo", new Color(0.36f, 0.27f, 0.23f)), Mat("SiluetaGris", new Color(0.42f, 0.42f, 0.43f)), Mat("SiluetaOscura", new Color(0.26f, 0.25f, 0.25f)) };
            // Primera fila: naves bajas con chapa en la orilla.
            var sheet = SouthSheet("ChapaHorizonte", new Color(0.55f, 0.55f, 0.53f), 511);
            for (float x = -420f; x < 420f;)
            {
                float w = Range(rng, 20f, 50f);
                if (rng.NextDouble() < 0.25 || Mathf.Abs(x + w / 2f + 160f) < 35f) { x += w; continue; }
                float h = Range(rng, 6f, 13f), d = Range(rng, 15f, 30f);
                TexturedBox("NaveLejana", group, G(x + w / 2f, -518f - d / 2f, h / 2f), new Vector3(w, h, d), sheet, SheetTile, collider: false, sidesOnly: true);
                Box("Tejado", group, G(x + w / 2f, -518f - d / 2f, h + 0.05f), new Vector3(w, 0.1f, d), tarRoof, collider: false);
                x += w + Range(rng, 3f, 12f);
            }
            // Silos de grano con su torre de elevación.
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 2; j++)
                Primitive(PrimitiveType.Cylinder, "Silo", group, G(-180f + i * 11f, -530f - j * 11f, 19f), new Vector3(10.5f, 19f, 10.5f), concrete, keepCollider: false);
            Box("TorreSilos", group, G(-198f, -535f, 27f), new Vector3(10f, 54f, 12f), concrete, collider: false);
            // Grúas pórtico de contenedores.
            var crane = Mat("GruaPuerto", new Color(0.65f, 0.25f, 0.15f));
            foreach (float cx in new[] { 20f, 60f })
            {
                foreach (float dx in new[] { -8f, 8f })
                foreach (float dz in new[] { -6f, 6f })
                    Box("PataGrua", group, G(cx + dx, -528f + dz, 17f), new Vector3(1.2f, 34f, 1.2f), crane, collider: false);
                Box("VigaGrua", group, G(cx, -522f, 35f), new Vector3(3f, 3f, 60f), crane, collider: false);
                Box("CabinaGrua", group, G(cx, -515f, 32f), new Vector3(3f, 2.5f, 3f), crane, collider: false);
            }
            // Parque de depósitos.
            var tankMat = Mat("DepositoBlanco", new Color(0.78f, 0.78f, 0.76f));
            for (int i = 0; i < 9; i++)
            {
                float r = Range(rng, 9f, 14f), h = Range(rng, 9f, 15f);
                Primitive(PrimitiveType.Cylinder, "Deposito", group, G(150f + (i % 3) * 34f, -548f - (i / 3) * 32f, h / 2f), new Vector3(r * 2f, h / 2f, r * 2f), tankMat, keepCollider: false);
            }
            // Chimeneas y bloques lejanos.
            var stack = Mat("ChimeneaFabrica", new Color(0.4f, 0.28f, 0.22f));
            foreach (var (sx, sz, r, h) in new[] { (-70f, -600f, 3f, 62f), (110f, -640f, 2.6f, 55f), (260f, -590f, 3.2f, 70f) })
            {
                Primitive(PrimitiveType.Cylinder, "Chimenea", group, G(sx, sz, h / 2f), new Vector3(r * 2f, h / 2f, r * 2f), stack, keepCollider: false);
                Primitive(PrimitiveType.Cylinder, "BandaChimenea", group, G(sx, sz, h - 3f), new Vector3(r * 2f + 0.3f, 1f, r * 2f + 0.3f), far[2], keepCollider: false);
            }
            for (int i = 0; i < 40; i++)
            {
                float w = Range(rng, 18f, 45f), d = Range(rng, 14f, 30f), h = rng.NextDouble() < 0.1 ? Range(rng, 60f, 100f) : Range(rng, 14f, 40f);
                Box("SiluetaLejana", group, G(Range(rng, -520f, 520f), Range(rng, -800f, -610f), h / 2f), new Vector3(w, h, d), far[rng.Next(far.Length)], collider: false);
            }
        }

        // ---------- Detalle vial del sur (lo llama BuildStreetDetail) ----------

        /// <summary>Pasos a nivel: cruz de San Andrés con dos luces rojas, barrera levantada y línea de detención a cada lado.</summary>
        static void BuildSouthStreetDetail(Transform group)
        {
            var root = new GameObject("PasosNivel").transform;
            root.SetParent(group, false);
            var white = Mat("BlancoBarrera", new Color(0.9f, 0.9f, 0.88f));
            var stripeR = Mat("RojoSurtidor", new Color(0.6f, 0.1f, 0.08f));
            var black = Mat("BajosCamion", new Color(0.07f, 0.07f, 0.07f));
            var redOff = Mat("LuzPasoNivelApagada", new Color(0.25f, 0.04f, 0.03f)); // las encendidas y las barreras, en Freight.cs
            var word = TexturedMat("CruzSanAndres", ProceduralTextures.Placard("CruzSanAndres", new[] { "RAILROAD CROSSING" }, new Color(0.95f, 0.95f, 0.92f), new Color(0.05f, 0.05f, 0.05f), 521));
            float aspect = (float)word.mainTexture.width / word.mainTexture.height;
            foreach (var c in FreightCrossings)
            {
                float center = (c.r0 + c.r1) / 2f, hw = (c.r1 - c.r0) / 2f;
                // (lado de la vía, signo hacia donde mira el cartel en z, x del poste a la derecha de quien llega)
                foreach (var (z, facing, px) in new[] { (SouthFreightZ + FreightBedHalf + 1.4f, 1f, c.r0 - 0.9f), (SouthFreightZ - FreightBedHalf - 1.4f, -1f, c.r1 + 0.9f) })
                {
                    var n = new Vector3(0f, 0f, facing);
                    var post = G(px, z, SidewalkTop);
                    Primitive(PrimitiveType.Cylinder, "PostePasoNivel", root, post + Vector3.up * 2.1f, new Vector3(0.14f, 2.1f, 0.14f), sdGalvanized ? sdGalvanized : white, keepCollider: true);
                    foreach (float roll in new[] { 40f, -40f })
                    {
                        var q = Primitive(PrimitiveType.Quad, "Aspa", root, post + Vector3.up * 3.7f + n * 0.12f, new Vector3(2.2f, 2.2f / aspect, 1f), word, keepCollider: false);
                        q.transform.localRotation = Quaternion.LookRotation(-n) * Quaternion.Euler(0f, 0f, roll);
                    }
                    Box("Travesano", root, post + Vector3.up * 2.7f + n * 0.12f, new Vector3(1.6f, 0.12f, 0.1f), black, collider: false);
                    foreach (float o in new[] { -0.6f, 0.6f })
                    {
                        Primitive(PrimitiveType.Cylinder, "FondoLuz", root, post + new Vector3(o, 2.7f, 0f) + n * 0.2f, new Vector3(0.6f, 0.03f, 0.6f), black, keepCollider: false)
                            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        Primitive(PrimitiveType.Sphere, "LuzPasoNivel", root, post + new Vector3(o, 2.7f, 0f) + n * 0.25f, Vector3.one * 0.22f, redOff, keepCollider: false);
                    }
                    Box("Campana", root, post + Vector3.up * 4.35f, new Vector3(0.3f, 0.25f, 0.3f), black, collider: false);
                    // Mecanismo de la barrera (la barrera, que sube y baja, la pone Freight.cs fuera de "Mundo").
                    var pivot = post + new Vector3(Mathf.Sign(center - px) * 0.5f, 0f, 0f);
                    Box("Mecanismo", root, pivot + Vector3.up * 0.6f + n * 0.5f, new Vector3(0.5f, 1.2f, 0.4f), white, collider: true);
                    // Línea de detención en el carril que llega.
                    float lane0 = facing > 0f ? c.r0 : center, lane1 = facing > 0f ? center : c.r1;
                    Box("LineaStop", root, G((lane0 + lane1) / 2f, z + facing * 1.5f, c.top + PaintLift / 2f), new Vector3(lane1 - lane0 - 0.4f, PaintLift, 0.4f), sdPaint, collider: false);
                }
            }
        }
    }
}
