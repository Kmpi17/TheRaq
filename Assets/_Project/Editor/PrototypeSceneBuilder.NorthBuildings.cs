using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Piezas del barrio norte (North.cs): la diagonal y el edificio en cuña, el mini centro comercial, el parque, la
    /// rotonda de la calle sin salida, el bloque con patio, la torre suelta, la iglesia, el bulevar, el terraplén con sus
    /// pasos inferiores, las siluetas del horizonte y el detalle vial que no sale de NamedStreets. Incluye Prism, un
    /// prisma de base poligonal para lo que no va alineado con los ejes.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // ---------- Geometría ----------

        /// <summary>Copia del polígono en sentido antihorario visto desde arriba (x a la derecha, z hacia arriba).</summary>
        static List<Vector2> Ccw(IList<Vector2> polygon)
        {
            var p = new List<Vector2>(polygon);
            float area = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Count];
                area += a.x * b.y - b.x * a.y;
            }
            if (area < 0f) p.Reverse();
            return p;
        }

        /// <summary>Polígono convexo desplazado <paramref name="margin"/> metros hacia fuera (esquinas a inglete).</summary>
        static List<Vector2> OffsetPoly(IList<Vector2> polygon, float margin)
        {
            var p = Ccw(polygon);
            var result = new List<Vector2>();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 prev = p[(i + p.Count - 1) % p.Count], cur = p[i], next = p[(i + 1) % p.Count];
                Vector2 e1 = (cur - prev).normalized, e2 = (next - cur).normalized;
                Vector2 n1 = new(e1.y, -e1.x), n2 = new(e2.y, -e2.x);
                result.Add(cur + (n1 + n2) * (margin / Mathf.Max(1f + Vector2.Dot(n1, n2), 0.2f)));
            }
            return result;
        }

        /// <summary>
        /// Prisma recto de base poligonal convexa (en XZ del padre), de <paramref name="y0"/> a <paramref name="y1"/>, con malla
        /// propia. UV en metros: arriba, a lo largo de <paramref name="uAxis"/> y su perpendicular contando desde
        /// <paramref name="uvOrigin"/> (repetición <paramref name="topTile"/>); en los lados, U a lo largo del perímetro y V
        /// desde y0 (<paramref name="sideTile"/>). El collider es un MeshCollider convexo.
        /// </summary>
        static GameObject Prism(string name, Transform parent, IList<Vector2> polygon, float y0, float y1, Material material,
            Vector2 topTile, Vector2 sideTile, Vector2 uAxis = default, Vector2 uvOrigin = default, bool collider = true, bool top = true, bool sides = true)
        {
            var p = Ccw(polygon);
            if (uAxis == default) uAxis = Vector2.right;
            uAxis.Normalize();
            var vAxis = new Vector2(-uAxis.y, uAxis.x);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            void Tri(int a, int b, int c, Vector3 outward)
            {
                var n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (Vector3.Dot(n, outward) < 0f) { tris.Add(a); tris.Add(c); tris.Add(b); }
                else { tris.Add(a); tris.Add(b); tris.Add(c); }
            }
            if (top)
            {
                int start = verts.Count;
                foreach (var q in p)
                {
                    verts.Add(new Vector3(q.x, y1, q.y));
                    var d = q - uvOrigin;
                    uvs.Add(new Vector2(Vector2.Dot(d, uAxis) / topTile.x, Vector2.Dot(d, vAxis) / topTile.y));
                }
                for (int i = 1; i + 1 < p.Count; i++) Tri(start, start + i, start + i + 1, Vector3.up);
            }
            if (sides)
            {
                float run = 0f;
                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Count];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.001f) continue;
                    var e = (b - a) / len;
                    var outward = new Vector3(e.y, 0f, -e.x); // antihorario: el exterior queda a la derecha de cada lado
                    int s = verts.Count;
                    verts.Add(new Vector3(a.x, y0, a.y));
                    verts.Add(new Vector3(b.x, y0, b.y));
                    verts.Add(new Vector3(b.x, y1, b.y));
                    verts.Add(new Vector3(a.x, y1, a.y));
                    float u0 = run / sideTile.x, u1 = (run + len) / sideTile.x, v1 = (y1 - y0) / sideTile.y;
                    uvs.Add(new Vector2(u0, 0f));
                    uvs.Add(new Vector2(u1, 0f));
                    uvs.Add(new Vector2(u1, v1));
                    uvs.Add(new Vector2(u0, v1));
                    Tri(s, s + 1, s + 2, outward);
                    Tri(s, s + 2, s + 3, outward);
                    run += len;
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
            if (collider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                mc.convex = true;
            }
            return go;
        }

        /// <summary>Panel fino delante del lado a→b de un polígono antihorario (la cara de fuera queda a la derecha).</summary>
        static GameObject FacePanel(string name, Transform parent, Vector2 a, Vector2 b, float y0, float y1, float offset, float thickness, Material material)
        {
            var d = b - a;
            float len = d.magnitude;
            var n = new Vector3(d.y, 0f, -d.x) / len;
            var mid = (a + b) / 2f;
            var go = Box(name, parent, new Vector3(mid.x, (y0 + y1) / 2f, mid.y) + n * (offset + thickness / 2f), new Vector3(len, y1 - y0, thickness), material, collider: false);
            go.transform.localRotation = Quaternion.LookRotation(n);
            return go;
        }

        /// <summary>Pirámide de base cuadrada (chapitel, tejado de depósito).</summary>
        static GameObject Pyramid(string name, Transform parent, Vector3 baseCenter, float baseSize, float height, Material material)
        {
            float h = baseSize / 2f;
            var corners = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) };
            var apex = Vector3.up * height;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(apex);
                var outward = (a + b) / 2f;
                if (Vector3.Dot(Vector3.Cross(b - a, apex - a), outward) < 0f) { tris.Add(s); tris.Add(s + 2); tris.Add(s + 1); }
                else { tris.Add(s); tris.Add(s + 1); tris.Add(s + 2); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = baseCenter;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Huella para el plano (mapa/edificios_casas.json) y, si ocupa, rectángulo que no pisan la nieve ni los trastos.</summary>
        static void RecordPolygon(string kind, IList<Vector2> corners, float height, bool occupy = true)
        {
            housesFootprints.Add((kind, new List<Vector2>(corners).ToArray(), height));
            if (!occupy) return;
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var c in corners)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minZ = Mathf.Min(minZ, c.y); maxZ = Mathf.Max(maxZ, c.y);
            }
            housesOccupied.Add(Rect.MinMaxRect(minX, minZ, maxX, maxZ));
        }

        static Vector2[] RectCorners(Rect r) => new[] { V(r.xMin, r.yMin), V(r.xMax, r.yMin), V(r.xMax, r.yMax), V(r.xMin, r.yMax) };

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ---------- Locales ----------

        /// <summary>
        /// Local suelto (<see cref="Storefront"/>, con su horario y sitios de NPC) con la fachada centrada en
        /// <paramref name="front"/> y mirando hacia <paramref name="facing"/>; el fondo (7.5 m) va hacia atrás.
        /// </summary>
        static void NorthShop(Transform parent, ShopKind kind, Vector2 front, Vector2 facing, float width, int floors, System.Random rng)
        {
            var frame = new GameObject($"Marco_{kind}").transform;
            frame.SetParent(parent, false);
            var f3 = new Vector3(facing.x, 0f, facing.y).normalized;
            frame.localRotation = Quaternion.LookRotation(-f3); // la fachada de Storefront mira a −Z local
            frame.localPosition = new Vector3(front.x, 0f, front.y) - frame.localRotation * new Vector3(0f, 0f, ShopsFront);
            Storefront(frame, kind, -width / 2f, width / 2f, floors, rng);
            RecordPolygon("tienda", UnitCorners(front, f3, width), floors == 1 ? 4.6f : 7.4f);
        }

        static Vector2[] UnitCorners(Vector2 front, Vector3 facing, float width)
        {
            var right = new Vector2(-facing.z, facing.x) * (width / 2f);
            var back = new Vector2(-facing.x, -facing.z) * ShopsDepth;
            return new[] { front - right, front + right, front + right + back, front - right + back };
        }

        /// <summary>Local vacío del centro comercial: escaparate oscuro, tablones y cartel de FOR LEASE.</summary>
        static void VacantUnit(Transform parent, Vector2 front, Vector2 facing, float width)
        {
            var root = new GameObject("Local_Vacio").transform;
            root.SetParent(parent, false);
            var f3 = new Vector3(facing.x, 0f, facing.y).normalized;
            root.localPosition = new Vector3(front.x, 0f, front.y);
            root.localRotation = Quaternion.LookRotation(-f3);
            float w = width, d = ShopsDepth, h = 4.6f;
            TexturedBox("Muros", root, new Vector3(0f, h / 2f, d / 2f), new Vector3(w, h, d), aptFacadeB,
                new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor), sidesOnly: true);
            Box("Tejado", root, new Vector3(0f, h + 0.05f, d / 2f), new Vector3(w, 0.1f, d), tarRoof, collider: false);
            Box("Pretil", root, new Vector3(0f, h + 0.45f, 0.15f), new Vector3(w + 0.2f, 0.9f, 0.3f), stoneTrim, collider: false);
            Box("Frente", root, new Vector3(0f, 2.05f, -0.04f), new Vector3(w, 4.1f, 0.08f), Mat("FrenteVacio", new Color(0.42f, 0.41f, 0.39f)), collider: false);
            Box("Escaparate", root, new Vector3(0f, 1.75f, -0.09f), new Vector3(w - 1f, 2.3f, 0.04f), darkGlass, collider: false);
            Box("Tablon", root, new Vector3(-w / 4f, 1.4f, -0.13f), new Vector3(w / 2f - 0.6f, 1.8f, 0.04f), plywood, collider: false);
            var sign = SignMat("Letrero_SeAlquila", "FOR LEASE", new Color(0.95f, 0.95f, 0.92f), new Color(0.75f, 0.1f, 0.08f), 311, 0.15f);
            float aspect = (float)sign.mainTexture.width / sign.mainTexture.height;
            SignQuad(root, new Vector3(w / 4f, 2.1f, -0.12f), Vector3.back, 2.2f, 2.2f / aspect, sign);
            RecordPolygon("tienda", UnitCorners(front, f3, width), h);
        }

        static void ShopCart(Transform parent, Vector3 ground, float yaw, bool tipped)
        {
            var root = new GameObject("Carrito").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(tipped ? 75f : 0f, yaw, 0f);
            Box("Cesta", root, new Vector3(0f, 0.62f, 0f), new Vector3(0.55f, 0.45f, 0.85f), chainLink ? chainLink : ironMat, collider: false);
            Box("Base", root, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.04f, 0.75f), ironMat, collider: false);
            Strut("Asa", root, new Vector3(-0.28f, 0.95f, 0.45f), new Vector3(0.28f, 0.95f, 0.45f), 0.03f, ironMat, cylinder: true);
            foreach (float x in new[] { -0.22f, 0.22f })
            foreach (float z in new[] { -0.35f, 0.35f })
                Box("Rueda", root, new Vector3(x, 0.06f, z), new Vector3(0.04f, 0.12f, 0.12f), ironMat, collider: false);
        }

        /// <summary>Calle cortada: barreras de hormigón de lado a lado (de z0 a z1) en x y un cartel de ROAD CLOSED.</summary>
        static void RoadClosed(Transform parent, float x, float z0, float z1)
        {
            var root = new GameObject("CalleCortada").transform;
            root.SetParent(parent, false);
            for (float z = z0 + 0.2f; z < z1 - 0.5f; z += 3.05f)
            {
                float len = Mathf.Min(3f, z1 - 0.2f - z);
                Box("Barrera", root, new Vector3(x, RoadTop + 0.2f, z + len / 2f), new Vector3(0.6f, 0.4f, len), concreteMat);
                Box("BarreraAlta", root, new Vector3(x, RoadTop + 0.6f, z + len / 2f), new Vector3(0.3f, 0.4f, len), concreteMat, collider: false);
            }
            var sign = SignMat("Letrero_CalleCortada", "ROAD CLOSED", new Color(0.95f, 0.55f, 0.1f), new Color(0.08f, 0.08f, 0.08f), 321, 0.1f);
            float aspect = (float)sign.mainTexture.width / sign.mainTexture.height;
            float mid = (z0 + z1) / 2f;
            foreach (float s in new[] { -1.1f, 1.1f })
                Box("PosteCartel", root, new Vector3(x - 0.2f, 0.75f, mid + s), new Vector3(0.08f, 1.5f, 0.08f), ironMat, collider: false);
            SignQuad(root, new Vector3(x - 0.26f, 1.35f, mid), Vector3.left, 2.4f, 2.4f / aspect, sign);
        }

        /// <summary>Valla publicitaria en un mástil: la cara mira a −Z local (girada <paramref name="yaw"/>).</summary>
        static void Billboard(Transform parent, Vector3 ground, float yaw, Material steel, int variant)
        {
            var root = new GameObject("VallaPublicitaria").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var lines = variant == 0 ? new[] { "WE BUY HOUSES", "CASH FAST" } : new[] { "INJURED", "CALL NOW" };
            var background = variant == 0 ? new Color(0.95f, 0.82f, 0.2f) : new Color(0.12f, 0.2f, 0.45f);
            var ink = variant == 0 ? new Color(0.1f, 0.08f, 0.06f) : new Color(0.95f, 0.95f, 0.92f);
            var face = TexturedMat($"Valla_{variant}", ProceduralTextures.Placard($"Valla_{variant}", lines, background, ink, 330 + variant));
            float aspect = (float)face.mainTexture.width / face.mainTexture.height;
            float w = 10f, h = w / aspect;
            if (h > 4f) { h = 4f; w = h * aspect; }
            const float top = 10.5f;
            float cy = top - h / 2f, mast = cy - h / 2f - 0.2f;
            Primitive(PrimitiveType.Cylinder, "Mastil", root, new Vector3(0f, mast / 2f, 0.4f), new Vector3(0.7f, mast / 2f, 0.7f), steel, keepCollider: true);
            Box("Marco", root, new Vector3(0f, cy, 0.4f), new Vector3(w + 0.4f, h + 0.4f, 0.3f), steel, collider: false);
            SignQuad(root, new Vector3(0f, cy, 0.24f), Vector3.back, w, h, face);
            Box("Pasarela", root, new Vector3(0f, cy - h / 2f - 0.3f, -0.2f), new Vector3(w, 0.08f, 1f), steel, collider: false);
            for (float x = -w / 2f + 1.5f; x < w / 2f; x += 3f)
                Strut("BrazoFoco", root, new Vector3(x, cy - h / 2f - 0.25f, -0.6f), new Vector3(x, cy - h / 2f + 0.3f, -1.4f), 0.06f, steel);
        }

        static void Playground(Transform parent, Vector3 center, float yaw)
        {
            var root = new GameObject("Juegos").transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var rubber = Mat("SueloCaucho", new Color(0.33f, 0.17f, 0.13f));
            var steel = Mat("AceroJuegos", new Color(0.16f, 0.32f, 0.5f));
            var red = Mat("PlasticoRojo", new Color(0.62f, 0.14f, 0.1f));
            var seat = Mat("AsientoColumpio", new Color(0.08f, 0.08f, 0.08f));
            Box("Suelo", root, new Vector3(0f, 0.02f, 0f), new Vector3(10f, 0.04f, 8f), rubber, collider: false);
            // Columpios: pórtico en A con dos asientos.
            foreach (float sx in new[] { -2.6f, 2.6f })
            {
                Strut("Pata", root, new Vector3(sx, 0f, -2.3f), new Vector3(sx, 2.6f, -1f), 0.09f, steel, cylinder: true);
                Strut("Pata", root, new Vector3(sx, 0f, 0.3f), new Vector3(sx, 2.6f, -1f), 0.09f, steel, cylinder: true);
            }
            Strut("Travesano", root, new Vector3(-2.6f, 2.6f, -1f), new Vector3(2.6f, 2.6f, -1f), 0.1f, steel, cylinder: true);
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                foreach (float o in new[] { -0.22f, 0.22f })
                    Strut("Cadena", root, new Vector3(x + o, 2.6f, -1f), new Vector3(x + o, 0.55f, -1f), 0.02f, ironMat);
                Box("Asiento", root, new Vector3(x, 0.53f, -1f), new Vector3(0.55f, 0.05f, 0.25f), seat, collider: false);
            }
            // Tobogán: plataforma con escalera y rampa.
            var deck = new Vector3(2.8f, 1.6f, 2.3f);
            foreach (float ox in new[] { -0.55f, 0.55f })
            foreach (float oz in new[] { -0.55f, 0.55f })
                Strut("Poste", root, deck + new Vector3(ox, -1.6f, oz), deck + new Vector3(ox, 0.9f, oz), 0.08f, steel, cylinder: true);
            Box("Plataforma", root, deck, new Vector3(1.2f, 0.08f, 1.2f), red);
            for (int k = 1; k <= 4; k++)
                Box("Peldano", root, deck + new Vector3(0.75f + k * 0.18f, -k * 0.32f, 0f), new Vector3(0.12f, 0.04f, 0.8f), steel, collider: false);
            Vector3 a = deck + new Vector3(-0.6f, 0f, 0f), b = new Vector3(-0.4f, 0.25f, 2.3f);
            var slide = Box("Rampa", root, (a + b) / 2f, new Vector3(0.6f, 0.05f, (b - a).magnitude), red, collider: false);
            slide.transform.localRotation = Quaternion.LookRotation(b - a);
        }

        // ---------- Diagonal y edificio en cuña ----------

        static void BuildDiagonal(Transform parent)
        {
            var group = new GameObject("Diagonal").transform;
            group.SetParent(parent, false);
            float l1 = DiagLine(-DiagHalf), l2 = DiagLine(DiagHalf);
            float ls = DiagLine(-DiagHalf - HousesSidewalk), ln = DiagLine(DiagHalf + HousesSidewalk);
            const float ax = 103f, bz = 384f;     // bordillo de la avenida y del bulevar
            const float wx = 100f, wz = 381f;     // borde de dentro de sus aceras
            var across = V(1f, 1f).normalized;    // U de la textura: de la acera SO a la NE

            // Calzada: los extremos siguen el bordillo de la avenida (x = 103) y el del bulevar (z = 384).
            var road = LoadMat("CalleCasas") ?? Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            Prism("Calle", group, new[] { V(ax, l1 - ax), V(ax, l2 - ax), V(l2 - bz, bz), V(l1 - bz, bz) }, 0f, RoadTop, road,
                Vector2.one * HousesRoadWidth, Vector2.one, across, V(ax, l1 - ax), collider: false, sides: false);

            var tile = Vector2.one * ProceduralTextures.SidewalkTile;
            void Walk(params Vector2[] poly) => Prism("Acera", group, poly, 0f, SidewalkTop, sidewalkMat, tile, tile, across);
            Walk(V(wx, ls - wx), V(wx, l1 - wx), V(l1 - wz, wz), V(ls - wz, wz));   // acera SO
            Walk(V(wx, l2 - wx), V(wx, ln - wx), V(ln - wz, wz), V(l2 - wz, wz));   // acera NE
            // Esquinas: lo que queda entre el bordillo inclinado y las aceras rectas de la avenida y del bulevar.
            Walk(V(wx, l1 - ax), V(ax, l1 - ax), V(wx, l1 - wx));
            Walk(V(ax, l2 - ax), V(ax, l2 - wx), V(wx, l2 - wx));
            Walk(V(l1 - bz, wz), V(l1 - wz, wz), V(l1 - bz, bz));
            Walk(V(l2 - wz, wz), V(l2 - wz, bz), V(l2 - bz, bz));

            // Césped en cuña al SO (el rectángulo de al lado es del barrio): llega hasta la acera de la diagonal.
            var grass = Vector2.one * ProceduralTextures.GrassTile;
            Prism("Cesped", group, new[] { V(ls - wz, 300.5f), V(wx, 300.5f), V(wx, ls - wx), V(ls - wz, wz) }, 0f, LawnTop, lawnMat, grass, grass);
        }

        /// <summary>
        /// Edificio en cuña de 5 plantas (tipo flatiron) en la punta entre la avenida y la diagonal: piedra gris, cornisa,
        /// restaurante en la planta baja con la proa hacia el sur, escalera de incendios atrás, depósito de agua en el
        /// tejado y un anuncio pintado y desvaído en la medianera. Detrás, un aparcamiento de grava en el resto de la cuña.
        /// </summary>
        static void BuildFlatiron(Transform parent, System.Random rng)
        {
            var group = new GameObject("EdificioCuna").transform;
            group.SetParent(parent, false);
            float ln = DiagLine(DiagHalf + HousesSidewalk);
            const float prowZ = 334.6f, backZ = 362f, H = 15f;
            var foot = new[] { V(100f, prowZ), V(100f, backZ), V(ln - backZ, backZ), V(ln - prowZ, prowZ) }; // antihorario
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            Prism("Muros", group, foot, 0f, H, greystone, facadeTile, facadeTile, top: false);
            Prism("Tejado", group, foot, H, H + 0.05f, tarRoof, Vector2.one * 4f, Vector2.one, collider: false, sides: false);
            Prism("Cornisa", group, OffsetPoly(foot, 0.4f), H - 0.25f, H + 0.45f, stoneTrim, Vector2.one, Vector2.one, collider: false);
            Prism("Imposta", group, OffsetPoly(foot, 0.15f), 4.2f, 4.45f, stoneTrim, Vector2.one, Vector2.one, collider: false);

            // Planta baja: restaurante con frente pintado y lunas en la proa, la avenida y la diagonal.
            var paint = Mat("FrenteRestaurante", new Color(0.32f, 0.1f, 0.08f));
            Vector2 p0 = foot[0], p1 = foot[1], p2 = foot[2], p3 = foot[3];
            foreach (var (a, b) in new[] { (p0, p1), (p2, p3), (p3, p0) })
            {
                FacePanel("Frente", group, a, b, 0f, 4.1f, 0.02f, 0.06f, paint);
                // Lunas de 2.4 m separadas por machones (una luna corrida parece una tira de luz).
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                int bays = Mathf.FloorToInt((len - 1.2f) / 3.2f);
                if (bays < 1) continue;
                float start = (len - bays * 3.2f) / 2f + 0.4f;
                for (int k = 0; k < bays; k++)
                {
                    var g0 = a + dir * (start + k * 3.2f);
                    FacePanel("Escaparate", group, g0, g0 + dir * 2.4f, 0.8f, 3f, 0.08f, 0.04f, shopLitGlass);
                }
            }
            var sign = SignMat("Letrero_SoulFood", "SOUL FOOD", new Color(0.95f, 0.8f, 0.25f), new Color(0.4f, 0.08f, 0.05f), 341, 0.9f);
            float aspect = (float)sign.mainTexture.width / sign.mainTexture.height;
            SignQuad(group, new Vector3((p3.x + p0.x) / 2f, 3.6f, prowZ - 0.12f), Vector3.back, 2.8f, 2.8f / aspect, sign);
            var diagMid = (p2 + p3) / 2f;
            var diagOut = new Vector3(-1f, 0f, -1f).normalized;
            SignQuad(group, new Vector3(diagMid.x, 3.6f, diagMid.y) + diagOut * 0.12f, diagOut, 6f, 6f / aspect, sign);

            // Anuncio pintado en la medianera de atrás (mira al norte, al bulevar), muy desvaído.
            var ghost = SignMat("Anuncio_Hotel", "HOTEL ROOMS WEEKLY", new Color(0.42f, 0.25f, 0.2f), new Color(0.78f, 0.74f, 0.62f), 342, 0f);
            float ghostAspect = (float)ghost.mainTexture.width / ghost.mainTexture.height;
            SignQuad(group, new Vector3(88f, 11f, backZ + 0.03f), Vector3.forward, 20f, 20f / ghostAspect, ghost);

            // Escalera de incendios en la fachada de atrás: rellanos con barandilla y escalas entre ellos.
            for (int f = 1; f <= 4; f++)
            {
                float y = f * ProceduralTextures.FacadeFloor + 0.1f;
                Box("Rellano", group, new Vector3(80f, y, backZ + 0.65f), new Vector3(6f, 0.08f, 1.3f), ironMat, collider: false);
                Box("Baranda", group, new Vector3(80f, y + 0.9f, backZ + 1.28f), new Vector3(6f, 0.05f, 0.05f), ironMat, collider: false);
                for (float x = 77.1f; x <= 83f; x += 1.45f)
                    Box("Barrote", group, new Vector3(x, y + 0.45f, backZ + 1.28f), new Vector3(0.04f, 0.9f, 0.04f), ironMat, collider: false);
                if (f < 4)
                    Strut("Escala", group, new Vector3(f % 2 == 0 ? 78f : 82f, y, backZ + 0.65f), new Vector3(f % 2 == 0 ? 82f : 78f, y + 3f, backZ + 0.65f), 0.08f, ironMat);
            }

            // Depósito de agua de madera sobre patas, y aparatos de aire en el tejado.
            var tankWood = Mat("MaderaDeposito", new Color(0.36f, 0.27f, 0.19f));
            var tank = new Vector3(88f, H, 352f);
            foreach (float ox in new[] { -1.4f, 1.4f })
            foreach (float oz in new[] { -1.4f, 1.4f })
                Strut("PataDeposito", group, tank + new Vector3(ox, 0f, oz), tank + new Vector3(ox * 0.8f, 3f, oz * 0.8f), 0.18f, ironMat);
            Primitive(PrimitiveType.Cylinder, "Deposito", group, tank + Vector3.up * 5f, new Vector3(3.4f, 2f, 3.4f), tankWood, keepCollider: false);
            Pyramid("TejadoDeposito", group, tank + Vector3.up * 7f, 3.2f, 1.3f, tarRoof);
            var ac = Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f));
            for (int i = 0; i < 3; i++)
                Box("AireTejado", group, new Vector3(Range(rng, 85f, 97f), H + 0.5f, Range(rng, 340f, 358f)), new Vector3(1.4f, 1f, 1f), ac, collider: false);
            RecordPolygon("edificio", foot, H);

            // Resto de la cuña: aparcamiento de grava detrás y una placita en la proa.
            Prism("Aparcamiento", group, new[] { V(100f, backZ), V(100f, 381f), V(ln - 381f, 381f), V(ln - backZ, backZ) }, 0f, LotTop, alleyMat,
                Vector2.one * ProceduralTextures.AlleyTile, Vector2.one);
            var tile = Vector2.one * ProceduralTextures.SidewalkTile;
            Prism("Acera", group, new[] { V(100f, ln - 100f), V(100f, prowZ), V(ln - prowZ, prowZ) }, 0f, SidewalkTop, sidewalkMat, tile, tile);
            ParkedCar(group, new Vector3(92f, LotTop, 372f), 90f + Range(rng, -6f, 6f), rng);
            ParkedCar(group, new Vector3(84f, LotTop, 375f), 270f + Range(rng, -6f, 6f), rng);
            Dumpster(group, new Vector3(97f, LotTop, 366f), 90f, false);
        }

        // ---------- Mini centro comercial ----------

        /// <summary>
        /// KINGSLEY PLAZA: una fila en L de locales de una planta (Storefront) bajo una marquesina corrida, con el
        /// aparcamiento delante (entradas desde el bulevar y la avenida), un local suelto de alitas en una esquina del
        /// aparcamiento, el poste alto con los letreros de las tiendas y el patio de carga detrás.
        /// </summary>
        static void BuildStripMall(Transform parent, System.Random rng)
        {
            var group = new GameObject("CentroComercial").transform;
            group.SetParent(parent, false);
            var lot = NbMallLot;
            TexturedBox("Asfalto", group, new Vector3(lot.center.x, LotTop / 2f, lot.center.y), new Vector3(lot.width, LotTop, lot.height), alleyMat,
                Vector2.one * ProceduralTextures.AlleyTile);
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            Box("EntradaAvenida", group, new Vector3(NbMallAvenueDriveway.center.x, 0.01f, NbMallAvenueDriveway.center.y),
                new Vector3(NbMallAvenueDriveway.width, 0.02f, NbMallAvenueDriveway.height), asphalt, collider: false);

            // Locales: la fila norte mira al sur (fachada en z = 462) y la oeste mira al este (fachada en x = −10).
            const float northFront = 462f, westFront = -10f;
            var northUnits = new (ShopKind? kind, float x0, float x1)[]
            {
                (ShopKind.Dollar, -26f, -10f), (ShopKind.Phones, -10f, -2f), (ShopKind.Pizza, -2f, 8f), (ShopKind.Laundry, 8f, 19f),
                (ShopKind.Tax, 19f, 27f), (ShopKind.Beauty, 27f, 36f), (null, 36f, 45f), (ShopKind.Chicken, 45f, 55f), (ShopKind.Checks, 55f, 62f),
            };
            foreach (var (kind, x0, x1) in northUnits)
            {
                var front = V((x0 + x1) / 2f, northFront);
                if (kind.HasValue) NorthShop(group, kind.Value, front, Vector2.down, x1 - x0, 1, rng);
                else VacantUnit(group, front, Vector2.down, x1 - x0);
            }
            var westUnits = new (ShopKind kind, float z0, float z1)[]
            {
                (ShopKind.Barber, 418f, 426f), (ShopKind.Market, 426f, 438f), (ShopKind.Pawn, 438f, 445f), (ShopKind.Church, 445f, 452f),
            };
            foreach (var (kind, z0, z1) in westUnits)
                NorthShop(group, kind, V(westFront, (z0 + z1) / 2f), Vector2.right, z1 - z0, 1, rng);

            // Trastienda (los locales miden 7.5 m de fondo; el centro, 16).
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            var backRooms = new[] { Rect.MinMaxRect(-26f, northFront + ShopsDepth, 62f, 478f), Rect.MinMaxRect(-26f, 418f, westFront - ShopsDepth, 452f) };
            foreach (var r in backRooms)
            {
                TexturedBox("Trastienda", group, new Vector3(r.center.x, 2.3f, r.center.y), new Vector3(r.width, 4.6f, r.height), aptFacadeB, facadeTile, sidesOnly: true);
                Box("Tejado", group, new Vector3(r.center.x, 4.65f, r.center.y), new Vector3(r.width, 0.1f, r.height), tarRoof, collider: false);
                RecordPolygon("centro", RectCorners(r), 4.6f);
            }
            var ac = Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f));
            for (float x = -20f; x < 60f; x += Range(rng, 8f, 14f))
                Box("AireTejado", group, new Vector3(x, 5.2f, Range(rng, 466f, 475f)), new Vector3(2f, 1.1f, 1.4f), ac, collider: false);

            // Acera cubierta: marquesina corrida con franja de color, pilares y luces.
            var canopy = Mat("MarquesinaCentro", new Color(0.78f, 0.76f, 0.7f));
            var band = Mat("FranjaCentro", new Color(0.45f, 0.12f, 0.1f));
            var steel = Mat("AceroMarquesina", new Color(0.3f, 0.3f, 0.31f));
            var glow = EmissiveMat("LuzMarquesina", new Color(1f, 0.9f, 0.7f), 2f);
            Box("AceraTiendas", group, new Vector3(18f, 0.075f, northFront - 1.5f), new Vector3(88f, 0.15f, 3f), concreteMat);
            Box("AceraTiendas", group, new Vector3(westFront + 1.5f, 0.075f, 435f), new Vector3(3f, 0.15f, 34f), concreteMat);
            Box("Marquesina", group, new Vector3(18f, 4.55f, northFront - 1.6f), new Vector3(88f, 0.2f, 3.2f), canopy, collider: false);
            Box("Franja", group, new Vector3(18f, 4.35f, northFront - 3.2f), new Vector3(88f, 0.6f, 0.12f), band, collider: false);
            Box("Marquesina", group, new Vector3(westFront + 1.6f, 4.55f, 435f), new Vector3(3.2f, 0.2f, 34f), canopy, collider: false);
            Box("Franja", group, new Vector3(westFront + 3.2f, 4.35f, 435f), new Vector3(0.12f, 0.6f, 34f), band, collider: false);
            for (float x = -24f; x <= 60f; x += 7f)
            {
                Box("Pilar", group, new Vector3(x, 2.3f, northFront - 2.9f), new Vector3(0.2f, 4.1f, 0.2f), steel);
                Box("LuzTecho", group, new Vector3(x + 3.5f, 4.43f, northFront - 1.6f), new Vector3(0.8f, 0.04f, 0.3f), glow, collider: false);
            }
            for (float z = 420f; z <= 451f; z += 7f)
            {
                Box("Pilar", group, new Vector3(westFront + 2.9f, 2.3f, z), new Vector3(0.2f, 4.1f, 0.2f), steel);
                Box("LuzTecho", group, new Vector3(westFront + 1.6f, 4.43f, z + 3.5f), new Vector3(0.3f, 0.04f, 0.8f), glow, collider: false);
            }

            // Local suelto de alitas en la esquina del aparcamiento que da al bulevar y a la avenida.
            NorthShop(group, ShopKind.Wings, V(80f, 414f), Vector2.down, 16f, 1, rng);

            // Plazas: filas a lo largo de X (los coches por Z), pasillo de entrada en x 39–48 y dos isletas con farola.
            var paint = Mat("LineaAparcamiento", new Color(0.82f, 0.82f, 0.78f));
            var oil = LoadMat("Aceite");
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            const float stall = 2.7f, stallDepth = 5.5f, firstX = -5f;
            const int n = 36;
            float lineY = LotTop + 0.005f;
            var rows = new (float z0, float into)[] { (412f, -1f), (424.5f, 1f), (430f, -1f), (443f, 1f), (448.5f, -1f) };
            float[] lampX = { 20f, 75f };
            int decals = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                var (z0, into) = rows[r];
                float zc = z0 + stallDepth / 2f, backZ = into < 0f ? z0 : z0 + stallDepth;
                var used = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    float sx0 = firstX + i * stall, sx1 = sx0 + stall;
                    if (sx1 > 39f && sx0 < 48f) continue;   // pasillo desde la entrada del bulevar
                    if (r == 0 && sx1 > 66f) continue;      // delante de las alitas
                    bool island = false;
                    foreach (float lx in lampX) if (r > 0 && sx0 < lx + 0.7f && sx1 > lx - 0.7f) island = true;
                    if (island) continue;
                    used[i] = true;
                    float cx = (sx0 + sx1) / 2f;
                    Box("Tope", group, new Vector3(cx, LotTop + 0.06f, backZ - into * 0.7f), new Vector3(1.8f, 0.12f, 0.22f), concreteMat, collider: false);
                    double roll = rng.NextDouble();
                    if (roll < 0.4)
                        ParkedCar(group, new Vector3(cx, LotTop, zc), 90f + (rng.Next(2) == 0 ? 0f : 180f) + Range(rng, -4f, 4f), rng);
                    else if (roll < 0.45)
                        SnowMound(group, new Vector3(cx, LotTop, zc), new Vector3(2.2f, Range(rng, 0.5f, 0.9f), 3.8f), Range(rng, -10f, 10f));
                    if (oil && rng.NextDouble() < 0.35)
                        Decal(group, new Vector3(cx + Range(rng, -0.5f, 0.5f), LotTop + 0.002f + (decals++ % 10) * 0.0002f, zc + Range(rng, -1f, 1f)),
                            Range(rng, 0f, 360f), new Vector2(Range(rng, 0.8f, 1.5f), Range(rng, 0.8f, 1.8f)), oil);
                }
                for (int b = 0; b <= n; b++)
                {
                    bool left = b > 0 && used[b - 1], right = b < n && used[b];
                    if (left || right)
                        Box("Linea", group, new Vector3(firstX + b * stall, lineY, zc), new Vector3(0.12f, 0.01f, stallDepth), paint, collider: false);
                }
            }
            foreach (float lx in lampX)
            foreach (float lz in new[] { 430f, 448.5f })
            {
                Box("Isleta", group, new Vector3(lx, 0.3f, lz), new Vector3(1.1f, 0.6f, 1.1f), concreteMat);
                StreetLight(group, new Vector3(lx, 0.45f, lz), Vector3.left, pole, lamp);
            }
            for (int i = 0; i < 5; i++)
                ShopCart(group, new Vector3(Range(rng, 0f, 90f), LotTop, rows[rng.Next(rows.Length)].z0 + (rng.Next(2) == 0 ? -3f : 9f)), Range(rng, 0f, 360f), rng.NextDouble() < 0.3);

            // Poste alto con el nombre y los letreros de las tiendas, junto a la entrada de la avenida.
            var pylon = new Vector3(96.5f, 0f, 452f);
            var pylonMat = Mat("PostePilono", new Color(0.35f, 0.33f, 0.31f));
            foreach (float ox in new[] { -1.3f, 1.3f })
                Box("PostePilono", group, pylon + new Vector3(ox, 4.8f, 0f), new Vector3(0.4f, 9.6f, 0.4f), pylonMat);
            Box("CajaPilono", group, pylon + new Vector3(0f, 9.3f, 0f), new Vector3(4.6f, 2.2f, 0.5f), pylonMat, collider: false);
            Box("PanelPilono", group, pylon + new Vector3(0f, 6.6f, 0f), new Vector3(3.6f, 2.8f, 0.3f), pylonMat, collider: false);
            var title = new[] { ("KINGSLEY", 9.75f, 4.2f), ("PLAZA", 8.85f, 2.6f) };
            var tenants = new[] { "DOLLAR & MORE", "CELL PHONES", "PIZZA", "TAX SERVICE" };
            foreach (float side in new[] { -1f, 1f })
            {
                var normal = new Vector3(0f, 0f, side);
                foreach (var (text, y, w) in title)
                {
                    var m = SignMat($"Pilono_{text}", text, new Color(0.55f, 0.1f, 0.08f), new Color(1f, 0.95f, 0.85f), 300 + text.Length, 1.2f);
                    float asp = (float)m.mainTexture.width / m.mainTexture.height;
                    SignQuad(group, pylon + new Vector3(0f, y, side * 0.27f), normal, w, w / asp, m);
                }
                for (int i = 0; i < tenants.Length; i++)
                {
                    var m = SignMat($"Pilono_{tenants[i].Replace(' ', '_').Replace("&", "Y")}", tenants[i], new Color(0.95f, 0.95f, 0.92f),
                        new Color(0.12f, 0.12f, 0.15f), 350 + i, 0.8f);
                    float asp = (float)m.mainTexture.width / m.mainTexture.height;
                    SignQuad(group, pylon + new Vector3(0f, 7.7f - i * 0.68f, side * 0.17f), normal, Mathf.Min(3.4f, 0.55f * asp), Mathf.Min(3.4f, 0.55f * asp) / asp, m);
                }
            }

            // Patio de carga detrás: contenedores, bidones, palés y una valla de malla hacia Prescott y Webley.
            Dumpster(group, new Vector3(-6f, LotTop, 480.6f), 0f, false);
            Dumpster(group, new Vector3(24f, LotTop, 480.6f), 0f, true);
            Dumpster(group, new Vector3(52f, LotTop, 480.6f), 0f, false);
            var drum = Mat("BidonGrasa", new Color(0.2f, 0.22f, 0.25f));
            Primitive(PrimitiveType.Cylinder, "BidonGrasa", group, new Vector3(30f, LotTop + 0.45f, 481f), new Vector3(0.6f, 0.45f, 0.6f), drum, keepCollider: true);
            var pallet = Mat("Palet", new Color(0.55f, 0.43f, 0.28f));
            for (int k = 0; k < 4; k++)
                Box("Palet", group, new Vector3(40f, LotTop + 0.08f + k * 0.15f, 480.8f), new Vector3(1.2f, 0.14f, 1f), pallet);
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            FenceSegment(group, new Vector3(-32f, LotTop, 483.2f), new Vector3(99.5f, LotTop, 483.2f), 2f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            FenceSegment(group, new Vector3(-32.2f, LotTop, 409.5f), new Vector3(-32.2f, LotTop, 478f), 2f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            if (graffitiMats != null)
                for (int i = 0; i < 4; i++)
                {
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    if (g) WallGraffiti(group, new Vector3(Range(rng, -20f, 58f), Range(rng, 1.3f, 2.4f), 478f), Vector3.forward, Range(rng, 2.5f, 4f), g);
                }
        }

        // ---------- Parque ----------

        /// <summary>
        /// HOLLOWAY PARK: césped con un camino en diagonal de esquina a esquina, pabellón de ladrillo (FIELD HOUSE),
        /// cancha, columpios y tobogán, bancos, farolas de globo, árboles y una valla baja de hierro.
        /// </summary>
        static void BuildNorthPark(Transform parent, System.Random rng)
        {
            var group = new GameObject("Parque").transform;
            group.SetParent(parent, false);
            var park = Rect.MinMaxRect(-100.5f, 409f, -47.5f, 483.5f);
            Vector2 a = V(-99f, 410.5f), b = V(-49f, 482f);
            ParkPath(group, a, b, 2.4f);
            var houseDoor = V(-88f, 465.6f);
            var join = a + (b - a) * ((458f - a.y) / (b.y - a.y));
            ParkPath(group, houseDoor, join, 1.8f);

            // Pabellón de ladrillo.
            var hall = Rect.MinMaxRect(-97f, 466f, -83f, 475f);
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            TexturedBox("Pabellon", group, new Vector3(hall.center.x, 2.3f, hall.center.y), new Vector3(hall.width, 4.6f, hall.height), houseFacades[0], facadeTile, sidesOnly: true);
            Box("Tejado", group, new Vector3(hall.center.x, 4.7f, hall.center.y), new Vector3(hall.width + 0.6f, 0.2f, hall.height + 0.6f), tarRoof, collider: false);
            Box("Puerta", group, new Vector3(houseDoor.x, 1.2f, hall.yMin - 0.04f), new Vector3(1.4f, 2.4f, 0.06f), doorMat, collider: false);
            var sign = SignMat("Letrero_Pabellon", "FIELD HOUSE", new Color(0.12f, 0.3f, 0.18f), new Color(0.95f, 0.95f, 0.9f), 361, 0.1f);
            float aspect = (float)sign.mainTexture.width / sign.mainTexture.height;
            SignQuad(group, new Vector3(hall.center.x, 3.5f, hall.yMin - 0.05f), Vector3.back, 4f, 4f / aspect, sign);
            RecordPolygon("edificio", RectCorners(hall), 4.6f);

            Court(group, new Vector3(-62f, 0f, 426f), 1f);
            var courtRect = RectAround(-62f, 426f, CourtSize.x + 2f * CourtMargin, CourtSize.y + 2f * CourtMargin);
            Playground(group, new Vector3(-58f, LawnTop, 452f), 0f);
            var playRect = RectAround(-58f, 452f, 10f, 8f);

            // Bancos a los dos lados del camino, mirándolo, y farolas de globo.
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var dir = (b - a).normalized;
            var perp = new Vector2(-dir.y, dir.x);
            foreach (float t in new[] { 0.22f, 0.5f, 0.78f })
            foreach (float s in new[] { -1f, 1f })
            {
                var p = a + (b - a) * t + perp * (s * 2.3f) + dir * (s * 3f);
                var facing = new Vector3(-perp.x * s, 0f, -perp.y * s);
                Bench(group, new Vector3(p.x, LawnTop, p.y), YawOf(-facing), wood, concreteMat);
            }
            var lampPole = Mat("PosteParque", new Color(0.1f, 0.14f, 0.12f));
            var globe = EmissiveMat("GloboParque", new Color(1f, 0.85f, 0.6f), 1.8f);
            foreach (float t in new[] { 0.12f, 0.38f, 0.64f, 0.9f })
            {
                var p = a + (b - a) * t + perp * 1.9f;
                ParkLamp(group, new Vector3(p.x, LawnTop, p.y), lampPole, globe);
            }
            TrashCan(group, new Vector3(-70f, LawnTop, 447f), 30f, false);
            TrashCan(group, new Vector3(-92f, LawnTop, 420f), 0f, true);

            // Árboles sueltos lejos del camino, la cancha, los juegos, el pabellón y la valla.
            var bark = LoadMat("Corteza");
            int trees = 0;
            for (int tries = 0; tries < 300 && trees < 24; tries++)
            {
                var p = V(Range(rng, park.xMin + 2f, park.xMax - 2f), Range(rng, park.yMin + 2f, park.yMax - 2f));
                if (SegmentDistance(p, a, b) < 3.5f || SegmentDistance(p, houseDoor, join) < 3f) continue;
                if (Grow(courtRect, 2f).Contains(p) || Grow(playRect, 2f).Contains(p) || Grow(hall, 2.5f).Contains(p)) continue;
                DistrictTree(group, new Vector3(p.x, LawnTop, p.y), bark, rng, Range(rng, 0.9f, 1.35f), rng);
                trees++;
            }
            for (int i = 0; i < 4; i++)
                SnowMound(group, new Vector3(Range(rng, -98f, -50f), LawnTop, Range(rng, 412f, 480f)), new Vector3(Range(rng, 1.5f, 3f), Range(rng, 0.3f, 0.6f), Range(rng, 1.5f, 3f)), Range(rng, 0f, 180f));

            // Valla baja de hierro, abierta en las esquinas del camino.
            var fenceTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.2f);
            void Fence(Vector3 p, Vector3 q) => FenceSegment(group, p, q, 1.2f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            Fence(new Vector3(-95f, LawnTop, 409.4f), new Vector3(-47.9f, LawnTop, 409.4f));
            Fence(new Vector3(-100.1f, LawnTop, 414f), new Vector3(-100.1f, LawnTop, 483.1f));
            Fence(new Vector3(-100.1f, LawnTop, 483.1f), new Vector3(-54f, LawnTop, 483.1f));
            Fence(new Vector3(-47.9f, LawnTop, 409.4f), new Vector3(-47.9f, LawnTop, 477f));

            var parkSign = SignMat("Letrero_Parque", "HOLLOWAY PARK", new Color(0.1f, 0.28f, 0.16f), new Color(0.95f, 0.93f, 0.85f), 362, 0.05f);
            float parkAspect = (float)parkSign.mainTexture.width / parkSign.mainTexture.height;
            var signPos = new Vector3(-96f, LawnTop, 413.5f);
            var signNormal = new Vector3(-1f, 0f, -1f).normalized;
            foreach (float o in new[] { -1.3f, 1.3f })
                Box("PosteCartel", group, signPos + new Vector3(o, 0.6f, -o) * 0.7071f + Vector3.up * 0.6f, new Vector3(0.12f, 1.2f, 0.12f), wood, collider: false);
            SignQuad(group, signPos + Vector3.up * 1.05f + signNormal * 0.05f, signNormal, 3f, 3f / parkAspect, parkSign);
        }

        static void ParkPath(Transform parent, Vector2 a, Vector2 b, float width)
        {
            var mid = (a + b) / 2f;
            var d = new Vector3(b.x - a.x, 0f, b.y - a.y);
            var go = TexturedBox("Camino", parent, new Vector3(mid.x, 0.115f, mid.y), new Vector3(width, 0.03f, d.magnitude), sidewalkMat,
                Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
            go.transform.localRotation = Quaternion.LookRotation(d);
        }

        // ---------- Calle sin salida ----------

        /// <summary>Rotonda al fondo de S MERRIN CT: calzada redonda, acera en anillo, césped en las esquinas e isleta con un árbol.</summary>
        static void BuildCulDeSac(Transform parent, System.Random rng)
        {
            var group = new GameObject("Rotonda").transform;
            group.SetParent(parent, false);
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            var c = NbBulb;
            const int seg = 24;
            const float step = 360f / seg;
            Vector2 Dir(float deg) => new(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
            var disc = new List<Vector2>();
            for (int i = 0; i < seg; i++) disc.Add(c + Dir(i * step) * NbBulbRadius);
            Prism("Calzada", group, disc, 0f, RoadTop - 0.002f, asphalt, Vector2.one * 4f, Vector2.one, collider: false, sides: false);

            float rIn = NbBulbRadius, rOut = NbBulbRadius + HousesSidewalk;
            var walkTile = Vector2.one * ProceduralTextures.SidewalkTile;
            var grass = Vector2.one * ProceduralTextures.GrassTile;
            Vector2 Square(float deg)
            {
                var d = Dir(deg);
                return c + d * (rOut / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)));
            }
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * step, a1 = a0 + step, mid = a0 + step / 2f;
                if (mid > 60f && mid < 120f) continue; // boca de la calle
                Prism("Acera", group, new[] { c + Dir(a0) * rIn, c + Dir(a0) * rOut, c + Dir(a1) * rOut, c + Dir(a1) * rIn }, 0f, SidewalkTop, sidewalkMat, walkTile, walkTile);
                // Césped entre el anillo y el cuadrado que se quitó del césped del barrio (los cortes a 45° caen en las esquinas).
                Prism("Cesped", group, new[] { c + Dir(a0) * rOut, Square(a0), Square(a1), c + Dir(a1) * rOut }, 0f, LawnTop, lawnMat, grass, grass);
            }

            // Isleta central con bordillo, un árbol y nieve apartada.
            var island = new List<Vector2>();
            var islandGrass = new List<Vector2>();
            for (int i = 0; i < 12; i++)
            {
                island.Add(c + Dir(i * 30f) * 3.5f);
                islandGrass.Add(c + Dir(i * 30f) * 3.2f);
            }
            Prism("Isleta", group, island, 0f, SidewalkTop, concreteMat, Vector2.one, Vector2.one);
            Prism("IsletaCesped", group, islandGrass, SidewalkTop, SidewalkTop + 0.05f, lawnMat, grass, grass, collider: false);
            DistrictTree(group, new Vector3(c.x, SidewalkTop + 0.05f, c.y), LoadMat("Corteza"), rng, 1.1f, rng);
            SnowMound(group, new Vector3(c.x + 7f, RoadTop, c.y - 6f), new Vector3(2.5f, 0.6f, 1.6f), 40f);
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            StreetLight(group, new Vector3(c.x - 13.3f, 0f, c.y - 2f), Vector3.right, pole, Mat("LuzSodio", SodiumLight));
        }

        // ---------- Bloque con patio y torre suelta ----------

        /// <summary>Bloque de 4 plantas en U alrededor de un patio abierto al bulevar (esquina con x = −110), con portales en el patio.</summary>
        static void CourtyardBlock(Transform parent, System.Random rng)
        {
            var group = new GameObject("BloquePatio").transform;
            group.SetParent(parent, false);
            const float H = 12f;
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            var wings = new[] { Rect.MinMaxRect(-165f, 345f, -152f, 379f), Rect.MinMaxRect(-135f, 345f, -122f, 379f), Rect.MinMaxRect(-152f, 345f, -135f, 358f) };
            foreach (var r in wings)
            {
                TexturedBox("Muros", group, new Vector3(r.center.x, H / 2f, r.center.y), new Vector3(r.width, H, r.height), aptFacadeA, facadeTile, sidesOnly: true);
                Box("Tejado", group, new Vector3(r.center.x, H + 0.05f, r.center.y), new Vector3(r.width, 0.1f, r.height), tarRoof, collider: false);
                Box("Cornisa", group, new Vector3(r.center.x, H - 0.2f, r.center.y), new Vector3(r.width + 0.5f, 0.6f, r.height + 0.5f), stoneTrim, collider: false);
                Box("Zocalo", group, new Vector3(r.center.x, 0.45f, r.center.y), new Vector3(r.width + 0.12f, 0.9f, r.height + 0.12f), stoneTrim);
                RecordPolygon("apartamentos", RectCorners(r), H);
            }

            // Patio: camino desde la verja, tres portales, bancos y dos árboles.
            TexturedBox("Camino", group, new Vector3(-143.5f, 0.115f, 369.5f), new Vector3(3f, 0.03f, 23f), sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
            var fenceTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.5f);
            FenceSegment(group, new Vector3(-151.8f, LawnTop, 378.6f), new Vector3(-145.2f, LawnTop, 378.6f), 1.5f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            FenceSegment(group, new Vector3(-141.8f, LawnTop, 378.6f), new Vector3(-135.2f, LawnTop, 378.6f), 1.5f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            var lampGlow = EmissiveMat("LuzPortalTorre", new Color(1f, 0.86f, 0.62f), 2f);
            foreach (var (pos, normal) in new[] { (new Vector3(-152f, 0f, 370f), Vector3.right), (new Vector3(-135f, 0f, 370f), Vector3.left), (new Vector3(-143.5f, 0f, 358f), Vector3.forward) })
            {
                var along = new Vector3(normal.z, 0f, -normal.x);
                var frame = Box("MarcoPortal", group, pos + Vector3.up * 1.5f + normal * 0.04f, new Vector3(2f, 3f, 0.08f), stoneTrim, collider: false);
                frame.transform.localRotation = Quaternion.LookRotation(normal);
                var door = Box("Portal", group, pos + Vector3.up * 1.25f + normal * 0.09f, new Vector3(1.4f, 2.5f, 0.04f), darkGlass, collider: false);
                door.transform.localRotation = Quaternion.LookRotation(normal);
                var hood = Box("Tejadillo", group, pos + Vector3.up * 3.1f + normal * 0.6f, new Vector3(2.4f, 0.12f, 1.2f), concreteMat, collider: false);
                hood.transform.localRotation = Quaternion.LookRotation(normal);
                Box("LuzPortal", group, pos + Vector3.up * 3f + normal * 0.6f, new Vector3(0.3f, 0.06f, 0.3f), lampGlow, collider: false);
                Box("Escalon", group, pos + Vector3.up * 0.15f + normal * 0.6f, Abs(along) * 2f + Abs(normal) * 1.2f + Vector3.up * 0.3f, concreteMat);
                TrashCan(group, pos + normal * 1.5f + along * 1.6f + Vector3.up * LawnTop, Range(rng, 0f, 360f), rng.NextDouble() < 0.25);
            }
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            Bench(group, new Vector3(-149f, LawnTop, 365f), 90f, wood, concreteMat);
            Bench(group, new Vector3(-138f, LawnTop, 374f), 270f, wood, concreteMat);
            var bark = LoadMat("Corteza");
            DistrictTree(group, new Vector3(-148f, LawnTop, 374f), bark, rng, 1.1f, rng);
            DistrictTree(group, new Vector3(-139f, LawnTop, 363f), bark, rng, 1f, rng);

            // Aparatos de aire en las ventanas que dan al bulevar y a la calle grande.
            var ac = Mat("AireAcondicionado", new Color(0.7f, 0.69f, 0.64f));
            for (int f = 1; f < 4; f++)
            for (float z = 347f; z < 378f; z += ProceduralTextures.FacadeBay)
                if (rng.NextDouble() < 0.25)
                    Box("AireVentana", group, new Vector3(-121.7f, f * 3f + 1.2f, z), new Vector3(0.55f, 0.45f, 0.7f), ac, collider: false);
        }

        /// <summary>Torre suelta de 13 plantas al este de G, con aparcamiento, cancha, juegos, cámara y valla de hierro.</summary>
        static void NorthTowerBlock(Transform parent, System.Random rng)
        {
            var group = new GameObject("TorreSuelta").transform;
            group.SetParent(parent, false);
            const int floors = 13;
            HighRise(group, "Torre_Norte", new Vector3(262f, 0f, 340f), 90f, floors, aptFacadeB, rng);
            RecordPolygon("torre", RectCorners(Rect.MinMaxRect(262f - HighRiseDepth / 2f, 340f - HighRiseWidth / 2f, 262f + HighRiseDepth / 2f, 340f + HighRiseWidth / 2f)),
                floors * ProceduralTextures.FacadeFloor);
            ParkingLot(group, "TorreNorte", NbTowerLot, NbTowerDriveway, rng);
            TexturedBox("Camino", group, new Vector3(237f, 0.115f, 340f), new Vector3(35f, 0.03f, 3f), sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
            Court(group, new Vector3(237f, 0f, 362f), -1f);
            Playground(group, new Vector3(284f, LawnTop, 364f), 90f);

            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            Bench(group, new Vector3(248f, LawnTop, 336.5f), 0f, wood, concreteMat);
            Bench(group, new Vector3(244f, LawnTop, 343.5f), 180f, wood, concreteMat);
            Bench(group, new Vector3(284f, LawnTop, 357f), 0f, wood, concreteMat);
            SecurityCamera(group, new Vector3(251.5f, LawnTop, 336.6f), Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f)),
                EmissiveMat("LuzAzulCamara", new Color(0.15f, 0.35f, 1f), 3f));
            Dumpster(group, new Vector3(293f, LotTop, 305f), 90f, true);
            Dumpster(group, new Vector3(293f, LotTop, 309f), 90f, false);

            // Valla de hierro a lo largo de G, abierta en la entrada de coches y en el camino del portal.
            var fenceTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.5f);
            void Fence(float z0, float z1) => FenceSegment(group, new Vector3(220.2f, LawnTop, z0), new Vector3(220.2f, LawnTop, z1), 1.5f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            Fence(298.4f, 306.5f);
            Fence(314.5f, 338.2f);
            Fence(341.8f, 380.6f);
            var notice = TexturedMat("Cartel_NoLoitering", ProceduralTextures.Placard("Cartel_NoLoitering", new[] { "NO LOITERING", "NO TRESPASSING" },
                new Color(0.92f, 0.92f, 0.9f), new Color(0.65f, 0.08f, 0.06f), 371));
            float aspect = (float)notice.mainTexture.width / notice.mainTexture.height;
            SignQuad(group, new Vector3(220f, 1.1f, 344f), Vector3.left, 1.2f, 1.2f / aspect, notice);
        }

        // ---------- Iglesia ----------

        /// <summary>Iglesia de ladrillo con campanario y chapitel frente a G: un hito alto que se ve desde lejos.</summary>
        static void NorthChurch(Transform parent, System.Random rng)
        {
            var group = new GameObject("Iglesia").transform;
            group.SetParent(parent, false);
            var brick = TexturedMat("LadrilloIglesia", ProceduralTextures.PlainBrick("LadrilloIglesia", new Color(0.52f, 0.3f, 0.24f), 97));
            var brickTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.PlainBrickHeight);
            var nave = Rect.MinMaxRect(234f, 448f, 266f, 470f);
            const float wall = 9f;
            TexturedBox("Nave", group, new Vector3(nave.center.x, wall / 2f, nave.center.y), new Vector3(nave.width, wall, nave.height), brick, brickTile, sidesOnly: true);
            GableRoof(group, new Vector3(nave.center.x, wall, nave.center.y), nave.width, nave.height, 6.5f, 0.6f, shingles, brick, ridgeAlongX: true);
            RecordPolygon("iglesia", RectCorners(nave), wall);

            var tower = Rect.MinMaxRect(227f, 442.5f, 234f, 449.5f);
            const float towerH = 24f;
            var tc = new Vector3(tower.center.x, 0f, tower.center.y);
            TexturedBox("Campanario", group, tc + Vector3.up * (towerH / 2f), new Vector3(tower.width, towerH, tower.height), brick, brickTile, sidesOnly: true);
            Box("Cornisa", group, tc + Vector3.up * towerH, new Vector3(tower.width + 0.6f, 0.5f, tower.height + 0.6f), stoneTrim, collider: false);
            foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var louver = Box("Campanas", group, tc + Vector3.up * 20.5f + n * (tower.width / 2f + 0.02f), new Vector3(2f, 3.2f, 0.05f), darkGlass, collider: false);
                louver.transform.localRotation = Quaternion.LookRotation(n);
            }
            Pyramid("Chapitel", group, tc + Vector3.up * (towerH + 0.25f), tower.width + 0.2f, 9f, shingles);
            var white = Mat("CruzBlanca", new Color(0.92f, 0.92f, 0.88f));
            Box("Cruz", group, tc + Vector3.up * (towerH + 10f), new Vector3(0.18f, 1.8f, 0.18f), white, collider: false);
            Box("CruzBrazo", group, tc + Vector3.up * (towerH + 10.3f), new Vector3(0.18f, 0.18f, 1f), white, collider: false);
            RecordPolygon("iglesia", RectCorners(tower), towerH);

            // Fachada oeste (a G): puerta doble con arco de piedra, escalones y rosetón.
            float fx = nave.xMin;
            Box("ArcoPuerta", group, new Vector3(fx - 0.05f, 2.1f, nave.center.y), new Vector3(0.1f, 4.2f, 3.4f), stoneTrim, collider: false);
            Box("Puerta", group, new Vector3(fx - 0.1f, 1.6f, nave.center.y), new Vector3(0.06f, 3.2f, 2.4f), doorMat, collider: false);
            for (int k = 0; k < 3; k++)
                Box("Escalon", group, new Vector3(fx - 0.5f - k * 0.4f, 0.075f + (2 - k) * 0.15f, nave.center.y), new Vector3(0.8f + k * 0.8f, 0.15f + (2 - k) * 0.3f, 4.5f), concreteMat);
            var rose = Primitive(PrimitiveType.Cylinder, "Roseton", group, new Vector3(fx - 0.04f, 6.6f, nave.center.y), new Vector3(2.6f, 0.03f, 2.6f), darkGlass, keepCollider: false);
            rose.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var roseRing = Primitive(PrimitiveType.Cylinder, "MarcoRoseton", group, new Vector3(fx - 0.02f, 6.6f, nave.center.y), new Vector3(3.1f, 0.02f, 3.1f), stoneTrim, keepCollider: false);
            roseRing.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Vidrieras altas en los costados.
            var stained = Mat("Vidriera", new Color(0.24f, 0.12f, 0.3f));
            for (float x = nave.xMin + 4f; x < nave.xMax - 2f; x += 4.5f)
            foreach (float side in new[] { -1f, 1f })
            {
                float z = side < 0f ? nave.yMin - 0.04f : nave.yMax + 0.04f;
                Box("Vidriera", group, new Vector3(x, 4.4f, z), new Vector3(1.1f, 3.8f, 0.05f), stained, collider: false);
                Box("Alfeizar", group, new Vector3(x, 2.45f, z + side * 0.06f), new Vector3(1.4f, 0.12f, 0.16f), stoneTrim, collider: false);
            }

            // Atrio de hormigón hasta la acera de G, verja y cartel.
            TexturedBox("Atrio", group, new Vector3((219.5f + fx - 1.6f) / 2f, 0.075f, nave.center.y), new Vector3(fx - 1.6f - 219.5f, 0.15f, 12f), sidewalkMat,
                Vector2.one * ProceduralTextures.SidewalkTile);
            var fenceTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.5f);
            FenceSegment(group, new Vector3(220.2f, LawnTop, 438f), new Vector3(220.2f, LawnTop, 452.5f), 1.5f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            FenceSegment(group, new Vector3(220.2f, LawnTop, 465.5f), new Vector3(220.2f, LawnTop, 483f), 1.5f, ironFence, fenceTile, ironMat, 2.5f, 0.05f, topRail: false);
            var board = TexturedMat("Cartel_Iglesia", ProceduralTextures.Placard("Cartel_Iglesia", new[] { "TRUE LIGHT", "BAPTIST CHURCH", "ALL WELCOME" },
                new Color(0.92f, 0.9f, 0.84f), new Color(0.1f, 0.12f, 0.3f), 372));
            float aspect = (float)board.mainTexture.width / board.mainTexture.height;
            var boardPos = new Vector3(223f, 0f, 474f);
            foreach (float o in new[] { -1.3f, 1.3f })
                Box("PosteCartel", group, boardPos + new Vector3(0f, 0.8f, o), new Vector3(0.15f, 1.6f, 0.15f), stoneTrim, collider: false);
            SignQuad(group, boardPos + new Vector3(-0.05f, 1.55f, 0f), Vector3.left, 2.8f, 2.8f / aspect, board);
            Box("LuzPuerta", group, new Vector3(fx - 0.3f, 4.5f, nave.center.y), new Vector3(0.3f, 0.3f, 0.3f), EmissiveMat("LuzPortalTorre", new Color(1f, 0.86f, 0.62f), 2f), collider: false);
        }

        // ---------- Bulevar ----------

        /// <summary>
        /// Calzadas del bulevar (textura de un sentido) y mediana: bordillo, césped, árboles cada ~12 m y farolas dobles;
        /// se abre en las calles que la cruzan, con asfalto liso en los huecos.
        /// </summary>
        static void BuildBoulevard(Transform parent, System.Random rng)
        {
            var group = new GameObject("Bulevar").transform;
            group.SetParent(parent, false);
            var road = TexturedMat("CalleBulevar", ProceduralTextures.Road("CalleBulevar", BoulevardLane, 81, oneWay: true));
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));
            for (int i = 0; i < NorthSpans.Length; i++)
            foreach (bool north in new[] { false, true })
            {
                var r = BoulevardLaneRect(i, north);
                var go = TexturedBox("Calzada", group, new Vector3(r.center.x, RoadTop / 2f, r.center.y), new Vector3(r.height, RoadTop, r.width), road,
                    Vector2.one * BoulevardLane, collider: false);
                go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            var medians = new (float x0, float x1)[] { (-249.5f, -120f), (-100.5f, -47.5f), (-32.5f, 100f), (120f, 204.5f), (219.5f, 296f) };
            var gaps = new (float x0, float x1)[] { (-252f, -249.5f), (-120f, -117f), (-103f, -100.5f), (-47.5f, -32.5f), (100f, 103f), (117f, 120f), (204.5f, 219.5f), (296f, NorthEdgeX) };
            foreach (var (x0, x1) in gaps)
                Box("Cruce", group, new Vector3((x0 + x1) / 2f, RoadTop / 2f, BoulevardZ), new Vector3(x1 - x0, RoadTop, BoulevardMedian), asphalt, collider: false);

            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);
            var bark = LoadMat("Corteza");
            var grass = Vector2.one * ProceduralTextures.GrassTile;
            foreach (var (x0, x1) in medians)
            {
                float len = x1 - x0, cx = (x0 + x1) / 2f;
                Box("Mediana", group, new Vector3(cx, SidewalkTop / 2f, BoulevardZ), new Vector3(len, SidewalkTop, BoulevardMedian), concreteMat);
                TexturedBox("MedianaCesped", group, new Vector3(cx, SidewalkTop + 0.02f, BoulevardZ), new Vector3(len - 0.5f, 0.04f, BoulevardMedian - 0.5f), lawnMat, grass, collider: false);
                for (float x = x0 + 4f; x < x1 - 3f; x += Range(rng, 10f, 14f))
                {
                    if (rng.NextDouble() < 0.12) continue; // árbol que falta
                    DistrictTree(group, new Vector3(x, SidewalkTop + 0.04f, BoulevardZ + Range(rng, -0.6f, 0.6f)), bark, rng, Range(rng, 0.95f, 1.3f), rng);
                }
                for (float x = x0 + 9f; x < x1 - 6f; x += 40f)
                {
                    StreetLight(group, new Vector3(x + 5f, 0f, BoulevardZ + 0.3f), Vector3.forward, pole, lamp);
                    StreetLight(group, new Vector3(x + 5f, 0f, BoulevardZ - 0.3f), Vector3.back, pole, lamp);
                }
                if (len > 30f && rng.NextDouble() < 0.7)
                    SnowMound(group, new Vector3(Range(rng, x0 + 5f, x1 - 5f), SidewalkTop, BoulevardZ), new Vector3(Range(rng, 2f, 4f), 0.5f, 2.2f), Range(rng, -10f, 10f));
            }
        }

        // ---------- Terraplén del tren de mercancías ----------

        /// <summary>
        /// Borde norte del mundo: muro de contención de 5.8 m con grafitis, albardilla y malla encima; detrás, el terraplén
        /// con grava, dos vías, vagones de mercancías parados, árboles pelados y maleza. Las dos calles grandes pasan por
        /// debajo en pasos inferiores oscuros (viaductos).
        /// </summary>
        static void BuildEmbankment(Transform parent, System.Random rng)
        {
            var group = new GameObject("Terraplen").transform;
            group.SetParent(parent, false);
            const float back = 545f, top = EmbankmentTop;
            var wallMat = TexturedMat("MuroTerraplen", ProceduralTextures.RetainingWall("MuroTerraplen", 77));
            var wallTile = new Vector2(ProceduralTextures.WallTileWidth, ProceduralTextures.WallTileHeight);
            var earth = Mat("TierraTerraplen", new Color(0.33f, 0.3f, 0.26f));
            var gravel = TexturedMat("GravaTerraplen", ProceduralTextures.Ballast("GravaTerraplen", 88));
            var gravelTile = Vector2.one * ProceduralTextures.BallastTile;
            var openings = new[] { (-MainRoadX - 12f, -MainRoadX + 12f), (MainRoadX - 12f, MainRoadX + 12f) };
            var pieces = new[] { (-310f, openings[0].Item1), (openings[0].Item2, openings[1].Item1), (openings[1].Item2, 310f) };
            float bodyZ0 = NorthWallZ + 1f;
            var tile = Vector2.one * ProceduralTextures.ChainLinkTile;
            foreach (var (x0, x1) in pieces)
            {
                float cx = (x0 + x1) / 2f, len = x1 - x0;
                TexturedBox("Muro", group, new Vector3(cx, top / 2f, NorthWallZ + 0.5f), new Vector3(len, top, 1f), wallMat, wallTile);
                Box("Albardilla", group, new Vector3(cx, top + 0.15f, NorthWallZ + 0.55f), new Vector3(len, 0.3f, 1.3f), concreteMat, collider: false);
                Box("Terraplen", group, new Vector3(cx, top / 2f, (bodyZ0 + back) / 2f), new Vector3(len, top, back - bodyZ0), earth, collider: false);
                TexturedBox("Grava", group, new Vector3(cx, top + 0.02f, (bodyZ0 + back) / 2f), new Vector3(len, 0.04f, back - bodyZ0), gravel, gravelTile, collider: false);
                FenceSegment(group, new Vector3(x0 + 0.5f, top + 0.3f, NorthWallZ + 1f), new Vector3(x1 - 0.5f, top + 0.3f, NorthWallZ + 1f), 1.8f, chainLink, tile, ironMat, 3f, 0.05f, topRail: true);
            }
            foreach (var (x0, x1) in openings) Underpass(group, x0, x1, back, rng);

            // Grava y maleza al pie del muro.
            TexturedBox("GravaPie", group, new Vector3(0f, 0.02f, NorthWallZ - 0.5f), new Vector3(600f, 0.04f, 1f), gravel, gravelTile, collider: false);
            for (int i = 0; i < 70; i++)
            {
                float x = Range(rng, -298f, 298f);
                if (Mathf.Abs(Mathf.Abs(x) - MainRoadX) < 13f) continue;
                Clump(group, rng, new Vector3(x, 0.04f, Range(rng, NorthWallZ - 0.9f, NorthWallZ - 0.2f)), 0.3f, 1f);
            }

            // Grafitis en la cara del muro.
            if (graffitiMats != null)
                for (int i = 0; i < 18; i++)
                {
                    float x = Range(rng, -295f, 295f);
                    if (Mathf.Abs(Mathf.Abs(x) - MainRoadX) < 16f) continue;
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    if (g) WallGraffiti(group, new Vector3(x, Range(rng, 1.4f, 3.2f), NorthWallZ - 0.01f), Vector3.back, Range(rng, 2.5f, 6f), g);
                }

            // Arriba: dos vías, vagones de mercancías parados, árboles pelados y maleza (se ven por encima del muro).
            var rail = Mat("Rail", new Color(0.3f, 0.27f, 0.25f));
            foreach (float z in new[] { 512f, 518f })
            foreach (float o in new[] { -0.72f, 0.72f })
                Box("Rail", group, new Vector3(0f, top + 0.12f, z + o), new Vector3(640f, 0.15f, 0.08f), rail, collider: false);
            var shells = new[]
            {
                TexturedMat("ChapaVagon_Oxido", ProceduralTextures.Corrugated("ChapaVagon_Oxido", 64), new Color(0.55f, 0.3f, 0.2f)),
                TexturedMat("ChapaVagon_Amarillo", ProceduralTextures.Corrugated("ChapaVagon_Amarillo", 65), new Color(0.75f, 0.6f, 0.25f)),
                TexturedMat("ChapaVagon_Gris", ProceduralTextures.Corrugated("ChapaVagon_Gris", 66), new Color(0.5f, 0.5f, 0.5f)),
            };
            var chassis = Mat("BajosVagon", new Color(0.08f, 0.08f, 0.08f));
            var shellTile = new Vector2(ProceduralTextures.CorrugatedTileU, ProceduralTextures.CorrugatedTileV);
            foreach (var (z, start, cars) in new[] { (512f, -150f, 7), (518f, 60f, 5) })
                for (int k = 0; k < cars; k++)
                {
                    float x = start + k * 16.2f;
                    TexturedBox("Vagon", group, new Vector3(x, top + 3.15f, z), new Vector3(15f, 4.2f, 3.1f), shells[rng.Next(shells.Length)], shellTile, collider: false);
                    Box("Bastidor", group, new Vector3(x, top + 0.85f, z), new Vector3(14.6f, 0.5f, 2.7f), chassis, collider: false);
                    foreach (float o in new[] { -5.5f, 5.5f })
                        Box("Bogie", group, new Vector3(x + o, top + 0.45f, z), new Vector3(2.6f, 0.7f, 2.5f), chassis, collider: false);
                }
            var bark = LoadMat("Corteza");
            for (int i = 0; i < 26; i++)
            {
                float x = Range(rng, -300f, 300f);
                if (Mathf.Abs(Mathf.Abs(x) - MainRoadX) < 14f) continue;
                BareTree(group, new Vector3(x, top, Range(rng, 503f, 508f)), bark, rng, Range(rng, 0.9f, 1.4f));
            }
            for (int i = 0; i < 60; i++)
                Clump(group, rng, new Vector3(Range(rng, -300f, 300f), top + 0.04f, Range(rng, 501.5f, 509f)), 0.4f, 1.3f);
        }

        /// <summary>
        /// Paso inferior de una calle grande bajo el terraplén: viga de acero en la boca, tablero, pilares en el eje,
        /// la calzada y las aceras que siguen dentro, una luz amarilla siempre encendida y un fondo negro que corta el paso.
        /// </summary>
        static void Underpass(Transform parent, float x0, float x1, float back, System.Random rng)
        {
            var group = new GameObject("PasoInferior").transform;
            group.SetParent(parent, false);
            const float top = EmbankmentTop, clear = 4.8f, endZ = 528f;
            float cx = (x0 + x1) / 2f, width = x1 - x0, z0 = NorthWallZ, depth = endZ - z0;
            var steel = Mat("AceroViaducto", new Color(0.2f, 0.24f, 0.22f));
            var dark = Mat("HormigonOscuro", new Color(0.18f, 0.18f, 0.17f));
            var black = Mat("FondoTunel", new Color(0.015f, 0.015f, 0.015f));
            var asphalt = Mat("Asfalto", new Color(0.17f, 0.17f, 0.18f));

            Box("Viga", group, new Vector3(cx, (clear + top) / 2f, z0 + 0.4f), new Vector3(width + 1f, top - clear, 1.2f), steel);
            for (float x = x0 + 0.6f; x < x1; x += 1.2f)
                Box("Remache", group, new Vector3(x, clear + 0.25f, z0 - 0.22f), new Vector3(0.12f, 0.12f, 0.06f), steel, collider: false);
            Box("Tablero", group, new Vector3(cx, (clear + top) / 2f, (z0 + 1f + back) / 2f), new Vector3(width, top - clear, back - z0 - 1f), dark, collider: false);
            Box("TableroGrava", group, new Vector3(cx, top + 0.02f, (z0 + 1f + back) / 2f), new Vector3(width, 0.04f, back - z0 - 1f), LoadMat("GravaTerraplen") ?? dark, collider: false);
            foreach (float side in new[] { -1f, 1f })
                Box("MuroInterior", group, new Vector3(cx + side * (width / 2f + 0.5f), clear / 2f, z0 + depth / 2f), new Vector3(1f, clear, depth), dark);
            Box("SueloPaso", group, new Vector3(cx, -0.1f, (WorldNorthZ + endZ) / 2f), new Vector3(width, 0.2f, endZ - WorldNorthZ), asphalt);
            var lanes = LoadMat("CalleGrande1");
            if (lanes)
                TexturedBox("Calle", group, new Vector3(cx, MainRoadTop / 2f, (WorldNorthZ + endZ) / 2f), new Vector3(RoadWidth, MainRoadTop, endZ - WorldNorthZ), lanes,
                    Vector2.one * RoadWidth, collider: false);
            float walk = (width - RoadWidth) / 2f;
            foreach (float side in new[] { -1f, 1f })
                Box("AceraPaso", group, new Vector3(cx + side * (RoadWidth / 2f + walk / 2f), 0.075f, (WorldNorthZ + endZ) / 2f), new Vector3(walk, 0.15f, endZ - WorldNorthZ), concreteMat);
            for (float z = z0 + 4f; z < endZ - 2f; z += 6f)
                Box("Pilar", group, new Vector3(cx, clear / 2f, z), new Vector3(0.5f, clear, 0.5f), steel);
            Box("Fondo", group, new Vector3(cx, clear / 2f, endZ + 0.5f), new Vector3(width, clear, 1f), black);

            var light = new GameObject("LuzPaso").AddComponent<Light>();
            light.transform.SetParent(group, false);
            light.transform.localPosition = new Vector3(cx, clear - 0.4f, z0 + 7f);
            light.type = LightType.Point;
            light.color = SodiumLight;
            light.range = 15f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
            var fixture = EmissiveMat("LuzPasoInferior", new Color(1f, 0.7f, 0.35f), 2.2f);
            Box("Lampara", group, new Vector3(cx, clear - 0.05f, z0 + 7f), new Vector3(0.8f, 0.1f, 0.3f), fixture, collider: false);
            if (graffitiMats != null)
                foreach (float side in new[] { -1f, 1f })
                {
                    var g = graffitiMats[rng.Next(graffitiMats.Length)];
                    if (g) WallGraffiti(group, new Vector3(cx + side * width / 2f, 1.8f, z0 + Range(rng, 4f, 12f)), new Vector3(-side, 0f, 0f), 3f, g);
                }
        }

        // ---------- Horizonte ----------

        /// <summary>
        /// Siluetas de ciudad detrás del terraplén, sin colisiones: una primera fila de casas y naves con textura (z 548–600),
        /// bloques y torres más altos y lisos (z 620–800) que la niebla va borrando, dos chimeneas de fábrica y un depósito de
        /// agua. Dan sensación de ciudad grande sin nada jugable detrás.
        /// </summary>
        static void BuildNorthSkyline(Transform world)
        {
            var group = new GameObject("Horizonte_Norte").transform;
            group.SetParent(world, false);
            var rng = new System.Random(4041);
            Box("SueloLejano", group, new Vector3(0f, -0.1f, 720f), new Vector3(1400f, 0.2f, 360f), Mat("SueloLejano", new Color(0.5f, 0.5f, 0.51f)), collider: false);
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, ProceduralTextures.FacadeFloor);
            var near = new[] { houseFacades[0], houseFacades[1], aptFacadeA, aptFacadeB, greystone };
            for (float x = -440f; x < 440f;)
            {
                float w = Range(rng, 10f, 28f);
                if (rng.NextDouble() < 0.18) { x += w; continue; }
                float h = Range(rng, 7f, 17f), d = Range(rng, 10f, 20f), z = Range(rng, 548f, 590f) + d / 2f;
                TexturedBox("Silueta", group, new Vector3(x + w / 2f, h / 2f, z), new Vector3(w, h, d), near[rng.Next(near.Length)], facadeTile, collider: false, sidesOnly: true);
                Box("Tejado", group, new Vector3(x + w / 2f, h + 0.05f, z), new Vector3(w, 0.1f, d), tarRoof, collider: false);
                x += w + Range(rng, 1f, 7f);
            }
            var far = new[] { Mat("SiluetaLadrillo", new Color(0.36f, 0.27f, 0.23f)), Mat("SiluetaGris", new Color(0.42f, 0.42f, 0.43f)), Mat("SiluetaOscura", new Color(0.26f, 0.25f, 0.25f)) };
            for (int i = 0; i < 46; i++)
            {
                float w = Range(rng, 18f, 40f), d = Range(rng, 14f, 30f);
                float h = rng.NextDouble() < 0.12 ? Range(rng, 70f, 110f) : Range(rng, 18f, 55f);
                Box("SiluetaLejana", group, new Vector3(Range(rng, -520f, 520f), h / 2f, Range(rng, 620f, 800f)), new Vector3(w, h, d), far[rng.Next(far.Length)], collider: false);
            }
            var stack = Mat("ChimeneaFabrica", new Color(0.4f, 0.28f, 0.22f));
            foreach (var (x, z, r, h) in new[] { (-210f, 680f, 2.6f, 55f), (250f, 640f, 3f, 46f) })
            {
                Primitive(PrimitiveType.Cylinder, "Chimenea", group, new Vector3(x, h / 2f, z), new Vector3(r * 2f, h / 2f, r * 2f), stack, keepCollider: false);
                Primitive(PrimitiveType.Cylinder, "BandaChimenea", group, new Vector3(x, h - 3f, z), new Vector3(r * 2f + 0.3f, 1f, r * 2f + 0.3f), far[2], keepCollider: false);
            }
            var tower = new Vector3(40f, 0f, 600f);
            var legs = Mat("PataDeposito", new Color(0.3f, 0.32f, 0.33f));
            foreach (float ox in new[] { -3.5f, 3.5f })
            foreach (float oz in new[] { -3.5f, 3.5f })
                Strut("Pata", group, tower + new Vector3(ox, 0f, oz), tower + new Vector3(ox * 0.6f, 26f, oz * 0.6f), 0.5f, legs);
            Primitive(PrimitiveType.Cylinder, "DepositoAgua", group, tower + Vector3.up * 29f, new Vector3(9f, 3f, 9f), Mat("DepositoAgua", new Color(0.6f, 0.62f, 0.62f)), keepCollider: false);
            Pyramid("TejadoDepositoAgua", group, tower + Vector3.up * 32f, 7f, 3f, legs);
        }

        // ---------- Detalle vial que no sale de NamedStreets ----------

        /// <summary>Semáforos en los cruces del bulevar con las calles grandes y STOP con placas en los dos extremos de la diagonal.</summary>
        static void BuildNorthStreetDetail(Transform group)
        {
            var all = new GameObject("SemaforosNorte").transform;
            float h = RoadWidth / 2f;
            int index = 0;
            foreach (float x in new[] { -MainRoadX, MainRoadX })
            {
                var junction = new GameObject($"CruceBulevar_{index}").transform;
                junction.SetParent(all, false);
                string zName = x < 0f ? "S CALDER AVE" : "S MARLOW AVE";
                foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    for (float o = 0.9f; o <= 2.5f; o += 0.8f)
                    {
                        var p = new Vector3(x + sx * (h + o), SidewalkTop, BoulevardZ + sz * (BoulevardHalf + o));
                        if (!SpotFree(p, 0.3f)) continue;
                        SignalPost(junction, p, sx, sz, sx == sz ? (BoulevardName, zName) : default);
                        break;
                    }
                junction.gameObject.AddComponent<TrafficLight>().Configure(50f + index * 7f);
                index++;
            }

            // Diagonal: llegan a la avenida yendo al SE y al bulevar yendo al NO.
            var diag = new Street { Name = DiagName, Sidewalk = HousesSidewalk };
            var along = new Vector3(-1f, 0f, 1f).normalized; // hacia el bulevar
            float l1 = DiagLine(-DiagHalf), l2 = DiagLine(DiagHalf), ls = DiagLine(-DiagHalf - HousesSidewalk), ln = DiagLine(DiagHalf + HousesSidewalk);
            bool OnDiagSidewalk(Vector3 p)
            {
                float s = p.x + p.z;
                return p.x <= 99.6f && p.z <= 380.6f && ((s > ls + 0.3f && s < l1 - 0.3f) || (s > l2 + 0.3f && s < ln - 0.3f));
            }
            foreach (var (end, d, crossName) in new[]
            {
                (new Vector3(103f, 0f, DiagC - 103f), -along, "S MARLOW AVE"),
                (new Vector3(DiagC - 384f, 0f, 384f), along, BoulevardName),
            })
            {
                var cross = new Street { Name = crossName, Main = crossName != BoulevardName };
                var right = new Vector3(d.z, 0f, -d.x);
                var line = Box("LineaStop", group, end - d * 3.6f + right * (DiagHalf / 2f + 0.1f) + Vector3.up * (RoadTop + PaintLift / 2f),
                    new Vector3(DiagHalf - 0.4f, PaintLift, 0.4f), sdPaint, collider: false);
                line.transform.localRotation = Quaternion.Euler(0f, YawOf(d), 0f);
                for (float back = 4f; back <= 14f; back += 1f)
                {
                    var p = end - d * back + right * (DiagHalf + 0.7f) + Vector3.up * SidewalkTop;
                    if (!OnDiagSidewalk(p) || !SpotFree(p, 0.25f)) continue;
                    StopPost(group, p, -d, diag, cross);
                    break;
                }
            }
        }
    }
}
