using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Manzana central: conjunto de vivienda pública al estilo de img/estructures/thetowers1-3. Tres torres de ladrillo
    /// de 11 a 14 plantas (cantos de forjado vistos, balcones cerrados con malla metálica en la fachada trasera,
    /// portal con marquesina) y filas de casas adosadas de 2 plantas con tejado a dos aguas alrededor de patios de
    /// césped. Usa las mismas texturas que el barrio de viviendas de al lado (ladrillo con ventanas, acera, césped
    /// seco, valla de hierro) para que case con él. En los huecos hay tres aparcamientos con entrada desde la calle sur.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const float LotTop = 0.03f, WalkTop = 0.12f;
        const float HighRiseWidth = 27f, HighRiseDepth = 15f, RowhouseDepth = 9f;

        // Aparcamientos (x0, z0, x1, z1) y sus entradas desde la calle sur (cortan la acera).
        static readonly Rect[] TowersLots =
        {
            Rect.MinMaxRect(-92f, -64f, -50f, -47f), // P1
            Rect.MinMaxRect(-22f, -64f, 22f, -30f),  // P2 (delante de T2, con fila doble en el centro)
            Rect.MinMaxRect(36f, -64f, 76f, -47f),   // P3
        };
        static readonly Rect[] TowersDriveways =
        {
            Rect.MinMaxRect(-75f, -73f, -67f, -64f),
            Rect.MinMaxRect(12f, -73f, 20f, -64f),
            Rect.MinMaxRect(64f, -73f, 72f, -64f),
        };
        // Paso de peatones que cruza P2 desde la acera sur hasta el portal de T2.
        static readonly Rect TowersCrossing = Rect.MinMaxRect(-1.2f, -64f, 1.2f, -30f);

        static readonly List<Rect> towersOccupied = new();
        static readonly List<Rect> towersWalks = new();
        static readonly List<(Vector3 point, float half)> towersGates = new(); // donde un camino llega al borde de la manzana

        static void BuildTowersBlock(Transform world)
        {
            var group = new GameObject("Manzana_Torres").transform;
            group.SetParent(world, false);
            towersOccupied.Clear();
            towersWalks.Clear();
            towersGates.Clear();
            var rng = new System.Random(2013);

            BuildTowersGround(group);

            // Caminos: primero el eje E–O que cruza toda la manzana; los demás se recortan donde lo tocan.
            var walks = new GameObject("Caminos").transform;
            walks.SetParent(group, false);
            TowersWalk(walks, Rect.MinMaxRect(-BlockHalfX, -8.2f, BlockHalfX, -5.8f));
            TowersWalk(walks, Rect.MinMaxRect(-1.2f, -30f, 1.2f, -8.2f));         // portal de T2 → P2
            TowersWalk(walks, Rect.MinMaxRect(-1.2f, -BlockHalfZ, 1.2f, -64f));   // P2 → acera sur
            TowersWalk(walks, Rect.MinMaxRect(-63.2f, 47.5f, -60.8f, BlockHalfZ)); // portal de T1 → acera norte
            TowersWalk(walks, Rect.MinMaxRect(72.5f, 8.8f, BlockHalfX, 11.2f));   // portal de T3 → acera este (escaleras de la estación)
            TowersWalk(walks, Rect.MinMaxRect(74.6f, -5.8f, 77f, 8.8f));          // T3 → eje
            foreach (var p in new[]
                     {
                         new Vector3(-BlockHalfX, 0f, -7f), new Vector3(BlockHalfX, 0f, -7f), new Vector3(0f, 0f, -BlockHalfZ),
                         new Vector3(-62f, 0f, BlockHalfZ), new Vector3(BlockHalfX, 0f, 10f),
                     })
                towersGates.Add((p, 1.2f));

            // Torres: el portal en la cara −Z local. yaw 180: mira al norte; 0: al sur; −90: al este.
            var buildings = new GameObject("Edificios").transform;
            buildings.SetParent(group, false);
            HighRise(buildings, "Torre_T1", new Vector3(-62f, 0f, 40f), 180f, 12, LoadMat("FachadaLadrillo"), rng);
            HighRise(buildings, "Torre_T2", T2Center, 0f, T2Floors, LoadMat("FachadaLadrilloAire"), rng, T2Openings()); // vestíbulo y piso (Apartment.cs)
            HighRise(buildings, "Torre_T3", new Vector3(65f, 0f, 10f), -90f, 11, aptFacadeA, rng);

            // Adosadas: origen en el centro de la fachada, +Z local hacia dentro. Las del oeste se miran a través del eje;
            // las del norte y la del este dan a la calle (detrás de la valla del perímetro).
            Rowhouses(buildings, "Adosadas_O_Norte", new Vector3(-65f, 0f, 8f), 0f, 46f, 13.8f, true, rng);
            Rowhouses(buildings, "Adosadas_O_Sur", new Vector3(-65f, 0f, -22f), 180f, 46f, 13.8f, true, rng);
            Rowhouses(buildings, "Adosadas_Norte_Centro", new Vector3(5f, 0f, 60f), 180f, 44f, 10f, false, rng);
            Rowhouses(buildings, "Adosadas_Norte_Este", new Vector3(62f, 0f, 60f), 180f, 40f, 10f, false, rng);
            Rowhouses(buildings, "Adosadas_Este", new Vector3(90f, 0f, -28f), -90f, 38f, 10f, false, rng);

            var lots = new GameObject("Aparcamientos").transform;
            lots.SetParent(group, false);
            for (int i = 0; i < TowersLots.Length; i++)
                ParkingLot(lots, $"P{i + 1}", TowersLots[i], TowersDriveways[i], rng);

            BuildTowersYard(group, rng);
            BuildTowersFence(group);
            BuildTowersWeather(group, rng);
        }

        // ---------- Suelo ----------

        static void BuildTowersGround(Transform parent)
        {
            var group = new GameObject("Suelo").transform;
            group.SetParent(parent, false);

            // Césped en toda la manzana menos los aparcamientos y sus entradas.
            var holes = new List<Rect>(TowersLots);
            holes.AddRange(TowersDriveways);
            holes.Add(StudioStairwell); // escalera del estudio del sótano (Studio.cs)
            foreach (var r in RectMinus(Rect.MinMaxRect(-BlockHalfX, -BlockHalfZ, BlockHalfX, BlockHalfZ), holes))
                TexturedBox("Cesped", group, new Vector3(r.center.x, LawnTop / 2f, r.center.y), new Vector3(r.width, LawnTop, r.height),
                    lawnMat, Vector2.one * ProceduralTextures.GrassTile);

            // Acera alrededor de la manzana, cortada en las entradas de los aparcamientos.
            float ox = BlockHalfX + SidewalkWidth, oz = BlockHalfZ + SidewalkWidth;
            var strips = new[]
            {
                Rect.MinMaxRect(-ox, BlockHalfZ, ox, oz), Rect.MinMaxRect(-ox, -oz, ox, -BlockHalfZ),
                Rect.MinMaxRect(BlockHalfX, -BlockHalfZ, ox, BlockHalfZ), Rect.MinMaxRect(-ox, -BlockHalfZ, -BlockHalfX, BlockHalfZ),
            };
            var driveways = new List<Rect>(TowersDriveways);
            foreach (var strip in strips)
            foreach (var piece in StripMinus(strip, driveways))
                TexturedBox("Acera", group, new Vector3(piece.center.x, SidewalkTop / 2f, piece.center.y), new Vector3(piece.width, SidewalkTop, piece.height),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile);
            foreach (var d in TowersDriveways)
                TexturedBox("Entrada", group, new Vector3(d.center.x, LotTop / 2f, d.center.y), new Vector3(d.width, LotTop, d.height),
                    alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
        }

        /// <summary>Divide <paramref name="area"/> en rectángulos que no tocan ningún hueco (rejilla con los bordes de los huecos, uniendo celdas por filas).</summary>
        static List<Rect> RectMinus(Rect area, List<Rect> holes)
        {
            var xs = new List<float> { area.xMin, area.xMax };
            var zs = new List<float> { area.yMin, area.yMax };
            foreach (var h in holes)
            {
                if (!h.Overlaps(area)) continue;
                xs.Add(Mathf.Clamp(h.xMin, area.xMin, area.xMax));
                xs.Add(Mathf.Clamp(h.xMax, area.xMin, area.xMax));
                zs.Add(Mathf.Clamp(h.yMin, area.yMin, area.yMax));
                zs.Add(Mathf.Clamp(h.yMax, area.yMin, area.yMax));
            }
            xs = Cuts(xs);
            zs = Cuts(zs);

            var result = new List<Rect>();
            for (int j = 0; j + 1 < zs.Count; j++)
            {
                float run = float.NaN;
                for (int i = 0; i + 1 < xs.Count; i++)
                {
                    var c = new Vector2((xs[i] + xs[i + 1]) / 2f, (zs[j] + zs[j + 1]) / 2f);
                    bool hole = false;
                    foreach (var h in holes)
                        if (h.Contains(c)) { hole = true; break; }
                    if (!hole && float.IsNaN(run)) run = xs[i];
                    if (hole && !float.IsNaN(run))
                    {
                        result.Add(Rect.MinMaxRect(run, zs[j], xs[i], zs[j + 1]));
                        run = float.NaN;
                    }
                }
                if (!float.IsNaN(run)) result.Add(Rect.MinMaxRect(run, zs[j], xs[^1], zs[j + 1]));
            }
            return result;

            static List<float> Cuts(List<float> values)
            {
                values.Sort();
                var unique = new List<float>();
                foreach (float v in values)
                    if (unique.Count == 0 || v - unique[^1] > 0.01f) unique.Add(v);
                return unique;
            }
        }

        static void TowersWalk(Transform parent, Rect rect)
        {
            foreach (var piece in StripMinus(rect, towersWalks))
            {
                TexturedBox("Camino", parent, new Vector3(piece.center.x, WalkTop / 2f, piece.center.y), new Vector3(piece.width, WalkTop, piece.height),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
                towersWalks.Add(piece);
                towersOccupied.Add(Grow(piece, 0.3f));
            }
        }

        // ---------- Torre ----------

        /// <summary>
        /// Huecos de una torre por dentro (la T2: vestíbulo y piso del jugador). <see cref="Holes"/>: celdas de fachada
        /// (cara, vano, planta) que se dejan abiertas; cara 0 = −Z (portal), 1 = +X, 2 = +Z, 3 = −X; el vano cuenta desde
        /// −X (caras 0 y 2) o desde −Z (caras 1 y 3). <see cref="Voids"/>: cajas locales sin collider (las salas).
        /// <see cref="OpenPortal"/>: el portal es un hueco de verdad (marco sin puerta), no una puerta pintada.
        /// </summary>
        sealed class TowerOpenings
        {
            public readonly HashSet<(int face, int bay, int floor)> Holes = new();
            public readonly List<Bounds> Voids = new();
            public bool OpenPortal;
        }

        /// <summary>Torre de ladrillo de W × D con el portal en la cara −Z local y los balcones en la +Z.</summary>
        static void HighRise(Transform parent, string name, Vector3 center, float yaw, int floors, Material facade, System.Random rng,
            TowerOpenings openings = null)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            const float W = HighRiseWidth, D = HighRiseDepth, F = ProceduralTextures.FacadeFloor;
            float H = floors * F, front = -D / 2f, back = D / 2f;
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, F);

            if (openings == null)
            {
                TexturedBox("Muros", root, new Vector3(0f, H / 2f, 0f), new Vector3(W, H, D), facade, facadeTile, sidesOnly: true);
                Box("Zocalo", root, new Vector3(0f, 0.45f, 0f), new Vector3(W + 0.15f, 0.9f, D + 0.15f), concreteMat);
            }
            else
            {
                // Fachada por piezas con las celdas abiertas, y el collider de la torre en trozos que dejan libres las salas.
                OpenFacade(root, W, D, floors, facade, facadeTile, openings);
                foreach (var piece in SubtractBounds(new Bounds(new Vector3(0f, H / 2f, 0f), new Vector3(W, H, D)), openings.Voids))
                    ColliderBox(root, "Volumen", piece.center, piece.size);
                root.gameObject.AddComponent<SolidVisual>(); // esos colliders cuentan como visibles (escalar, cubrirse)
                // Zócalo en anillo, abierto en el portal.
                const float zt = 0.4f, zo = 0.075f;
                float zx = W / 2f + zo, zz = D / 2f + zo;
                Box("Zocalo", root, new Vector3((-zx - 1.5f) / 2f, 0.45f, front - zo + zt / 2f), new Vector3(zx - 1.5f, 0.9f, zt), concreteMat);
                Box("Zocalo", root, new Vector3((zx + 1.5f) / 2f, 0.45f, front - zo + zt / 2f), new Vector3(zx - 1.5f, 0.9f, zt), concreteMat);
                Box("Zocalo", root, new Vector3(0f, 0.45f, back + zo - zt / 2f), new Vector3(2f * zx, 0.9f, zt), concreteMat);
                foreach (float sx in new[] { -1f, 1f })
                    Box("Zocalo", root, new Vector3(sx * (zx - zt / 2f), 0.45f, 0f), new Vector3(zt, 0.9f, 2f * zz), concreteMat);
            }
            // Cantos de forjado vistos en cada planta y pilastras de hormigón en las esquinas, como en las torres de la referencia.
            for (int f = 1; f < floors; f++)
                Box("Forjado", root, new Vector3(0f, f * F, 0f), new Vector3(W + 0.16f, 0.22f, D + 0.16f), concreteMat, collider: false);
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Box("Pilastra", root, new Vector3(sx * W / 2f, H / 2f, sz * D / 2f), new Vector3(0.7f, H, 0.7f), concreteMat, collider: false);

            // Remate: cornisa, tejado de alquitrán, caseta del ascensor, depósito y antena.
            Box("Cornisa", root, new Vector3(0f, H + 0.3f, 0f), new Vector3(W + 0.3f, 0.6f, D + 0.3f), concreteMat, collider: false);
            TexturedBox("Tejado", root, new Vector3(0f, H + 0.62f, 0f), new Vector3(W - 0.4f, 0.04f, D - 0.4f), tarRoof, Vector2.one * ProceduralTextures.RoofTile, collider: false);
            Box("CasetaAscensor", root, new Vector3(W * 0.22f, H + 2.2f, 0f), new Vector3(5f, 3.2f, 4f), concreteMat, collider: false);
            Primitive(PrimitiveType.Cylinder, "Deposito", root, new Vector3(-W * 0.25f, H + 2f, 1.5f), new Vector3(2.6f, 1.3f, 2.6f), garageWalls[0], keepCollider: false);
            Strut("Antena", root, new Vector3(-W * 0.38f, H + 0.6f, -4f), new Vector3(-W * 0.38f, H + 5f, -4f), 0.08f, ironMat, cylinder: true);

            // Portal con marquesina, luz y escalón. En una torre con vestíbulo, el marco rodea un hueco de 2.6 × 2.6 m con la
            // puerta abierta hacia dentro; en las demás, la puerta es un cristal oscuro pintado en la fachada.
            if (openings is { OpenPortal: true })
            {
                foreach (float sx in new[] { -1f, 1f })
                    Box("MarcoPortal", root, new Vector3(sx * 1.55f, 1.6f, front - 0.03f), new Vector3(0.5f, 3.2f, 0.06f), concreteMat, collider: false);
                Box("MarcoPortal", root, new Vector3(0f, 2.9f, front - 0.03f), new Vector3(3.6f, 0.6f, 0.06f), concreteMat, collider: false);
                var hinge = new GameObject("Bisagra").transform;
                hinge.SetParent(root, false);
                hinge.localPosition = new Vector3(1.25f, 0f, front + 0.32f);
                hinge.localRotation = Quaternion.Euler(0f, 100f, 0f); // abierta hacia dentro, contra la pared
                Box("PuertaAbierta", hinge, new Vector3(-0.62f, 1.5f, 0f), new Vector3(1.2f, 2.3f, 0.05f), darkGlass, collider: false);
                Box("MarcoPuerta", hinge, new Vector3(-0.62f, 1.5f, 0f), new Vector3(1.26f, 2.36f, 0.04f), ironMat, collider: false);
            }
            else
            {
                Box("MarcoPortal", root, new Vector3(0f, 1.6f, front - 0.03f), new Vector3(3.6f, 3.2f, 0.06f), concreteMat, collider: false);
                Box("Puerta", root, new Vector3(0f, 1.45f, front - 0.09f), new Vector3(2.6f, 2.7f, 0.06f), darkGlass, collider: false);
            }
            Box("Marquesina", root, new Vector3(0f, 3.15f, front - 1.1f), new Vector3(4.6f, 0.22f, 2.2f), concreteMat, collider: false);
            Box("LuzPortal", root, new Vector3(0f, 2.99f, front - 0.7f), new Vector3(0.6f, 0.1f, 0.3f),
                EmissiveMat("LuzPortalTorre", new Color(1f, 0.86f, 0.62f), 2f), collider: false);
            Box("Escalon", root, new Vector3(0f, 0.12f, front - 0.9f), new Vector3(3.6f, 0.24f, 1.6f), concreteMat);
            TrashCan(root, new Vector3(2.6f, LawnTop, front - 2.2f), 0f, false);

            // Balcones apilados en la fachada trasera, cerrados con malla metálica; en algunos hay ropa tendida.
            var mesh = chainLink ? chainLink : ironFence;
            var meshTile = Vector2.one * ProceduralTextures.ChainLinkTile;
            var clothes = TowersClothes();
            foreach (float bx in new[] { -6f, 6f })
                for (int f = 1; f < floors; f++)
                {
                    float y = f * F;
                    Box("Balcon", root, new Vector3(bx, y, back + 0.7f), new Vector3(4f, 0.2f, 1.4f), concreteMat, collider: false);
                    TexturedBox("Malla", root, new Vector3(bx, y + 0.65f, back + 1.38f), new Vector3(4f, 1.1f, 0.03f), mesh, meshTile, collider: false, sidesOnly: true);
                    foreach (float sx in new[] { -1.98f, 1.98f })
                        TexturedBox("Malla", root, new Vector3(bx + sx, y + 0.65f, back + 0.7f), new Vector3(0.03f, 1.1f, 1.4f), mesh, meshTile, collider: false, sidesOnly: true);
                    Box("Pasamanos", root, new Vector3(bx, y + 1.22f, back + 1.38f), new Vector3(4.04f, 0.06f, 0.06f), ironMat, collider: false);
                    if (rng.NextDouble() < 0.25)
                        Box("Ropa", root, new Vector3(bx + Range(rng, -1.4f, 1.4f), y + 0.95f, back + 1.42f),
                            new Vector3(Range(rng, 0.6f, 1.1f), Range(rng, 0.4f, 0.7f), 0.02f), clothes[rng.Next(clothes.Length)], collider: false);
                }

            // Ventanas tapiadas: muchas en la planta baja, pocas arriba.
            int bays = Mathf.RoundToInt(W / ProceduralTextures.FacadeBay);
            float bay = W / bays;
            for (int f = 0; f < floors; f++)
            for (int i = 0; i < bays; i++)
            foreach (float face in new[] { -1f, 1f })
            {
                if (rng.NextDouble() > (f == 0 ? 0.25 : 0.04)) continue;
                float x = -W / 2f + (i + 0.5f) * bay;
                if (face < 0f && f == 0 && Mathf.Abs(x) < 3f) continue;                                   // portal
                if (openings != null && openings.Holes.Contains((face < 0f ? 0 : 2, i, f))) continue;     // ventana abierta
                if (face > 0f && f > 0 && (Mathf.Abs(x - 6f) < 2.6f || Mathf.Abs(x + 6f) < 2.6f)) continue; // balcones
                var board = Box("Tablon", root, new Vector3(x, f * F + F * 0.53f, face * (D / 2f + 0.1f)), new Vector3(bay * 0.45f, F * 0.52f, 0.05f), plywood, collider: false);
                board.transform.localRotation = Quaternion.Euler(0f, 0f, Range(rng, -4f, 4f));
            }

            // Antenas parabólicas en la fachada del portal.
            var dish = Mat("AntenaParabolica", new Color(0.78f, 0.78f, 0.76f));
            for (int i = 0; i < 6; i++)
            {
                int dishBay = rng.Next(bays), dishFloor = rng.Next(1, floors);
                if (openings != null && openings.Holes.Contains((0, dishBay, dishFloor))) continue; // no tapa una ventana abierta
                float x = -W / 2f + (dishBay + 0.5f) * bay + 0.9f;
                SatelliteDish(root, new Vector3(x, dishFloor * F + 1f, front - 0.05f), Vector3.back, dish);
            }

            // Grafitis en la planta baja.
            if (graffitiMats[0])
            {
                foreach (float side in new[] { -1f, 1f })
                    if (rng.NextDouble() < 0.6)
                        WallGraffiti(root, new Vector3(side * (W / 2f + 0.08f), 1.7f, Range(rng, -D / 2f + 2.5f, D / 2f - 2.5f)), Vector3.right * side,
                            Range(rng, 2.6f, 3.6f), graffitiMats[rng.Next(graffitiMats.Length)]);
                if (rng.NextDouble() < 0.5)
                    WallGraffiti(root, new Vector3(-8f, 1.7f, front - 0.1f), Vector3.back, 3f, graffitiMats[rng.Next(graffitiMats.Length)]);
            }

            // Contenedores en una losa de hormigón junto a la fachada +X, con bolsas al lado.
            Box("LosaContenedores", root, new Vector3(W / 2f + 2.2f, 0.12f, back - 3.7f), new Vector3(3f, 0.06f, 5.2f), concreteMat);
            Dumpster(root, new Vector3(W / 2f + 2.2f, 0.15f, back - 2.4f), 90f, rng.NextDouble() < 0.5);
            Dumpster(root, new Vector3(W / 2f + 2.2f, 0.15f, back - 5f), 90f, rng.NextDouble() < 0.5);
            for (int i = 0; i < 3; i++)
                GarbageBag(root, new Vector3(W / 2f + 4.1f, LawnTop, back - Range(rng, 1f, 6f)), rng);

            // Maleza al pie de los muros y nieve en la sombra de la fachada trasera.
            for (int i = 0; i < 14; i++)
            {
                int side = rng.Next(4);
                var p = side switch
                {
                    0 => new Vector3(Range(rng, -W / 2f, W / 2f), LawnTop, front - 0.45f),
                    1 => new Vector3(Range(rng, -W / 2f, W / 2f), LawnTop, back + 0.45f),
                    2 => new Vector3(-W / 2f - 0.45f, LawnTop, Range(rng, -D / 2f, D / 2f)),
                    _ => new Vector3(W / 2f + 0.45f, LawnTop, Range(rng, -D / 2f, D / 2f)),
                };
                if (side == 0 && Mathf.Abs(p.x) < 2.5f) continue;
                Clump(root, rng, p, 0.3f, 0.9f);
            }
            for (int i = 0; i < 2; i++)
                SnowMound(root, new Vector3(Range(rng, -W / 2f + 3f, W / 2f - 3f), LawnTop, back + 2.6f), new Vector3(Range(rng, 3f, 5f), Range(rng, 0.3f, 0.5f), 1.4f), 0f);

            towersOccupied.Add(FrameRect(root, -W / 2f - 0.8f, front - 3f, W / 2f + 4.5f, back + 2f));
        }

        /// <summary>
        /// Fachada de una torre con huecos: cada cara es una lámina por tramos de vanos (los vanos enteros sin huecos van
        /// juntos, de arriba abajo; los que tienen alguna celda abierta, por tramos de plantas). Los tramos empiezan y
        /// acaban en vanos y plantas enteros, así que la textura (un vano × una planta) cuadra igual que en una caja.
        /// </summary>
        static void OpenFacade(Transform root, float W, float D, int floors, Material facade, Vector2 tile, TowerOpenings openings)
        {
            const float F = ProceduralTextures.FacadeFloor, skin = 0.02f;
            for (int face = 0; face < 4; face++)
            {
                bool alongX = face % 2 == 0;
                float len = alongX ? W : D;
                int bays = Mathf.RoundToInt(len / ProceduralTextures.FacadeBay);
                float bay = len / bays;
                float plane = face switch { 0 => -D / 2f, 2 => D / 2f, 1 => W / 2f, _ => -W / 2f };
                bool HasHole(int b)
                {
                    for (int f = 0; f < floors; f++) if (openings.Holes.Contains((face, b, f))) return true;
                    return false;
                }
                void Slab(int b0, int b1, int f0, int f1)
                {
                    float a0 = -len / 2f + b0 * bay, a1 = -len / 2f + b1 * bay, y0 = f0 * F, y1 = f1 * F;
                    float c = (a0 + a1) / 2f, along = a1 - a0, h = y1 - y0;
                    var pos = alongX ? new Vector3(c, (y0 + y1) / 2f, plane) : new Vector3(plane, (y0 + y1) / 2f, c);
                    var size = alongX ? new Vector3(along, h, skin) : new Vector3(skin, h, along);
                    TexturedBox("Muros", root, pos, size, facade, tile, collider: false, sidesOnly: true);
                }
                int i = 0;
                while (i < bays)
                {
                    if (!HasHole(i))
                    {
                        int j = i;
                        while (j < bays && !HasHole(j)) j++;
                        Slab(i, j, 0, floors);
                        i = j;
                        continue;
                    }
                    int f = 0;
                    while (f < floors)
                    {
                        if (openings.Holes.Contains((face, i, f))) { f++; continue; }
                        int g = f;
                        while (g < floors && !openings.Holes.Contains((face, i, g))) g++;
                        Slab(i, i + 1, f, g);
                        f = g;
                    }
                    i++;
                }
            }
        }

        /// <summary>Resta cajas alineadas con los ejes: devuelve trozos que cubren <paramref name="whole"/> menos los huecos.</summary>
        static List<Bounds> SubtractBounds(Bounds whole, IEnumerable<Bounds> holes)
        {
            static Bounds MinMax(Vector3 a, Vector3 b)
            {
                var bounds = new Bounds();
                bounds.SetMinMax(a, b);
                return bounds;
            }
            var result = new List<Bounds> { whole };
            foreach (var hole in holes)
            {
                var next = new List<Bounds>();
                foreach (var b in result)
                {
                    Vector3 min = b.min, max = b.max;
                    bool overlaps = hole.min.x < max.x && hole.max.x > min.x && hole.min.y < max.y && hole.max.y > min.y && hole.min.z < max.z && hole.max.z > min.z;
                    if (!overlaps)
                    {
                        next.Add(b);
                        continue;
                    }
                    Vector3 hmin = Vector3.Max(hole.min, min), hmax = Vector3.Min(hole.max, max);
                    var parts = new List<Bounds>
                    {
                        MinMax(min, new Vector3(hmin.x, max.y, max.z)),
                        MinMax(new Vector3(hmax.x, min.y, min.z), max),
                        MinMax(new Vector3(hmin.x, min.y, min.z), new Vector3(hmax.x, hmin.y, max.z)),
                        MinMax(new Vector3(hmin.x, hmax.y, min.z), new Vector3(hmax.x, max.y, max.z)),
                        MinMax(new Vector3(hmin.x, hmin.y, min.z), new Vector3(hmax.x, hmax.y, hmin.z)),
                        MinMax(new Vector3(hmin.x, hmin.y, hmax.z), new Vector3(hmax.x, hmax.y, max.z)),
                    };
                    foreach (var part in parts)
                        if (part.size.x > 0.01f && part.size.y > 0.01f && part.size.z > 0.01f) next.Add(part);
                }
                result = next;
            }
            return result;
        }

        /// <summary>Antena parabólica con su brazo, pegada a la pared en <paramref name="wallPoint"/> y mirando hacia fuera y un poco arriba.</summary>
        static void SatelliteDish(Transform parent, Vector3 wallPoint, Vector3 outward, Material material)
        {
            var center = wallPoint + outward * 0.45f;
            Strut("Soporte", parent, wallPoint, center + Vector3.down * 0.1f, 0.04f, material);
            var dish = Primitive(PrimitiveType.Cylinder, "Parabolica", parent, center, new Vector3(0.65f, 0.025f, 0.65f), material, keepCollider: false);
            dish.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (outward + Vector3.up * 0.4f).normalized);
        }

        // ---------- Casas adosadas ----------

        /// <summary>
        /// Fila de casas adosadas de 2 plantas (unas 6.5 m por casa), con el tono del ladrillo cambiando por parejas,
        /// tejado gris a dos aguas con la cumbrera a lo largo de la fila, portal con tejadillo y camino de
        /// <paramref name="yard"/> m hasta el borde del jardín. Detrás: patio de hormigón y cubos de basura.
        /// </summary>
        static void Rowhouses(Transform parent, string name, Vector3 frontCenter, float yaw, float length, float yard, bool frontFence, System.Random rng)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = frontCenter;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            const float D = RowhouseDepth, F = ProceduralTextures.FacadeFloor, H = 2f * F;
            var facadeTile = new Vector2(ProceduralTextures.FacadeBay, F);
            int n = Mathf.Max(2, Mathf.RoundToInt(length / 6.5f));
            float u = length / n, half = length / 2f;
            var tones = new[] { houseFacades[0], aptFacadeB, houseFacades[2], houseFacades[1] };
            int tone0 = rng.Next(tones.Length);
            var doors = TowersDoorColors();
            var dish = Mat("AntenaParabolica", new Color(0.78f, 0.78f, 0.76f));
            var acUnit = Mat("AparatoAire", new Color(0.7f, 0.7f, 0.66f));
            var bark = LoadMat("Corteza");
            var ironTile = new Vector2(ProceduralTextures.IronFenceWidth, 1.1f);

            for (int i = 0; i < n; i++)
            {
                float cx = -half + (i + 0.5f) * u;
                var facade = tones[(tone0 + i / 2) % tones.Length];
                TexturedBox("Muros", root, new Vector3(cx, H / 2f, D / 2f), new Vector3(u, H, D), facade, facadeTile, sidesOnly: true);

                // Portales por parejas, hacia la medianera, cada uno en un vano: su marco tapa la ventana de ese vano.
                bool boarded = rng.NextDouble() < 0.12;
                float sideSign = i % 2 == 0 ? 1f : -1f;
                float doorX = FacadeBayCenter(cx, u, cx + sideSign * u / 2f, 0.9f), bayU = BayWidth(u);
                Box("Rellano", root, new Vector3(doorX, 0.2f, -0.55f), new Vector3(1.6f, 0.3f, 1.1f), concreteMat);
                Box("Escalon", root, new Vector3(doorX, 0.14f, -1.3f), new Vector3(1.3f, 0.18f, 0.4f), concreteMat);
                FacadeDoor(root, doorX, 0.35f, 0f, -1f, bayU, 1f, 2.2f, boarded ? plywood : doors[rng.Next(doors.Length)], stoneTrim, 2.78f);
                Box("Tejadillo", root, new Vector3(doorX, 2.85f, -0.45f), new Vector3(1.6f, 0.1f, 0.9f), stoneTrim, collider: false);
                float walkLength = yard - 1.5f;
                TexturedBox("Camino", root, new Vector3(doorX, WalkTop / 2f, -1.5f - walkLength / 2f), new Vector3(1.2f, WalkTop, walkLength),
                    sidewalkMat, Vector2.one * ProceduralTextures.SidewalkTile, collider: false);
                towersGates.Add((root.TransformPoint(new Vector3(doorX, 0f, -yard)), 0.6f));

                if (boarded) BoardUpWindows(root, cx, u, H, 0f, rng, doorX);
                if (i + 1 < n) Box("Bajante", root, new Vector3(cx + u / 2f, H / 2f, -0.08f), new Vector3(0.1f, H, 0.1f), garageWalls[0], collider: false);
                if (!boarded && rng.NextDouble() < 0.3)
                    Box("AparatoAire", root, new Vector3(cx - sideSign * Range(rng, 0.5f, 1.8f), F + 1f, -0.3f), new Vector3(0.7f, 0.45f, 0.55f), acUnit, collider: false);
                if (rng.NextDouble() < 0.25)
                    SatelliteDish(root, new Vector3(cx - sideSign * 1.5f, H - 0.6f, 0f), Vector3.back, dish);

                // Jardín delantero: valla baja de hierro (si no la pone ya el perímetro), algún árbol y maleza si está tapiada.
                if (frontFence && !boarded && rng.NextDouble() < 0.7)
                {
                    float fz = -yard + 0.5f;
                    FenceSegment(root, new Vector3(cx - u / 2f + 0.1f, LawnTop, fz), new Vector3(doorX - 0.8f, LawnTop, fz), 1.1f, ironFence, ironTile, ironMat, 2.5f, 0.05f, topRail: false);
                    FenceSegment(root, new Vector3(doorX + 0.8f, LawnTop, fz), new Vector3(cx + u / 2f - 0.1f, LawnTop, fz), 1.1f, ironFence, ironTile, ironMat, 2.5f, 0.05f, topRail: false);
                }
                if (rng.NextDouble() < 0.22)
                    DistrictTree(root, new Vector3(cx - sideSign * (u / 2f - 1.6f), LawnTop, -yard * 0.45f), bark, rng, Range(rng, 0.8f, 1.1f), rng);
                if (boarded)
                    for (int k = 0; k < 4; k++)
                        Clump(root, rng, new Vector3(Range(rng, cx - u / 2f + 0.3f, cx + u / 2f - 0.3f), LawnTop, Range(rng, -yard + 1f, -1.6f)), 0.5f, 1.3f);

                // Detrás: puerta, patio de hormigón y un cubo de basura.
                FacadeDoor(root, doorX, 0.14f, D, 1f, bayU, 0.95f, 2.1f, doorMat, stoneTrim).name = "PuertaTrasera";
                Box("PatioTrasero", root, new Vector3(cx, 0.12f, D + 1.4f), new Vector3(u - 0.4f, 0.04f, 2.6f), concreteMat, collider: false);
                TrashCart(root, new Vector3(cx + Range(rng, -u / 3f, u / 3f), 0.14f, D + 2.2f), Range(rng, -20f, 20f), binColors[rng.Next(binColors.Length)], rng.NextDouble() < 0.1);
            }

            GableRoof(root, new Vector3(0f, H, D / 2f), length, D, 1.6f, 0.4f, shingles, tones[tone0], ridgeAlongX: true);

            if (graffitiMats[0] && rng.NextDouble() < 0.6)
            {
                float end = rng.Next(2) == 0 ? -1f : 1f;
                WallGraffiti(root, new Vector3(end * (half + 0.02f), 1.6f, D / 2f + Range(rng, -1.5f, 1.5f)), Vector3.right * end,
                    Range(rng, 2.6f, 3.4f), graffitiMats[rng.Next(graffitiMats.Length)]);
            }
            towersOccupied.Add(FrameRect(root, -half - 0.5f, -yard, half + 0.5f, D + 3f));
        }

        static Material[] TowersDoorColors() => new[]
        {
            doorMat,
            Mat("PuertaRoja", new Color(0.45f, 0.1f, 0.08f)),
            Mat("PuertaVerde", new Color(0.15f, 0.3f, 0.2f)),
            Mat("PuertaAzul", new Color(0.12f, 0.2f, 0.38f)),
        };

        static Material[] TowersClothes() => new[]
        {
            Mat("RopaRoja", new Color(0.55f, 0.1f, 0.1f)),
            Mat("RopaBlanca", new Color(0.82f, 0.82f, 0.8f)),
            Mat("RopaAzul", new Color(0.15f, 0.25f, 0.5f)),
            Mat("RopaAmarilla", new Color(0.75f, 0.62f, 0.2f)),
        };

        // ---------- Aparcamientos ----------

        /// <summary>
        /// Aparcamiento de asfalto con plazas de 2.6 × 5 m pintadas, topes de hormigón, coches aparcados, alguna plaza
        /// ocupada por la nieve que apartó la quitanieves, manchas de aceite y dos farolas en el borde norte.
        /// </summary>
        static void ParkingLot(Transform parent, string name, Rect lot, Rect driveway, System.Random rng)
        {
            var group = new GameObject($"Aparcamiento_{name}").transform;
            group.SetParent(parent, false);
            var paint = Mat("LineaAparcamiento", new Color(0.82f, 0.82f, 0.78f));
            var oil = LoadMat("Aceite");
            var pole = Mat("Poste", new Color(0.25f, 0.27f, 0.28f));
            var lamp = Mat("LuzSodio", SodiumLight);

            TexturedBox("Asfalto", group, new Vector3(lot.center.x, LotTop / 2f, lot.center.y), new Vector3(lot.width, LotTop, lot.height),
                alleyMat, Vector2.one * ProceduralTextures.AlleyTile);
            towersOccupied.Add(Grow(lot, 0.4f));
            towersOccupied.Add(driveway);

            // Filas: (z de inicio, hacia dónde se entra en la plaza desde el pasillo). Pasillos de 7 m.
            const float stall = 2.6f, stallDepth = 5f;
            var rows = new List<(float z0, float into)> { (lot.yMin, -1f) };
            if (lot.height >= 33f)
            {
                rows.Add((lot.yMin + 12f, 1f));
                rows.Add((lot.yMin + 17f, -1f));
            }
            rows.Add((lot.yMax - stallDepth, 1f));

            int n = Mathf.FloorToInt((lot.width - 1f) / stall);
            float x0 = lot.center.x - n * stall / 2f;
            float lineY = LotTop + 0.005f;
            int decals = 0;
            foreach (var (z0, into) in rows)
            {
                float zc = z0 + stallDepth / 2f, backZ = into < 0f ? z0 : z0 + stallDepth;
                var used = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    float sx0 = x0 + i * stall, sx1 = sx0 + stall;
                    bool blocked = (z0 < driveway.yMax + 0.1f && sx1 > driveway.xMin && sx0 < driveway.xMax)
                                   || TowersCrossing.Overlaps(Rect.MinMaxRect(sx0, z0, sx1, z0 + stallDepth));
                    if (blocked) continue;
                    used[i] = true;
                    float cx = (sx0 + sx1) / 2f;
                    Box("Tope", group, new Vector3(cx, LotTop + 0.06f, backZ - into * 0.7f), new Vector3(1.8f, 0.12f, 0.22f), concreteMat, collider: false);
                    double roll = rng.NextDouble();
                    if (roll < 0.55)
                        ParkedCar(group, new Vector3(cx, LotTop, zc), 90f + (rng.Next(2) == 0 ? 0f : 180f) + Range(rng, -4f, 4f), rng);
                    else if (roll < 0.63)
                        SnowMound(group, new Vector3(cx, LotTop, zc), new Vector3(2.2f, Range(rng, 0.5f, 0.9f), 3.8f), Range(rng, -10f, 10f));
                    if (oil && rng.NextDouble() < 0.4)
                        Decal(group, new Vector3(cx + Range(rng, -0.5f, 0.5f), LotTop + 0.002f + (decals++ % 10) * 0.0002f, zc + Range(rng, -1f, 1f)),
                            Range(rng, 0f, 360f), new Vector2(Range(rng, 0.8f, 1.5f), Range(rng, 0.8f, 1.8f)), oil);
                }
                for (int b = 0; b <= n; b++)
                {
                    bool left = b > 0 && used[b - 1], right = b < n && used[b];
                    if (left || right)
                        Box("Linea", group, new Vector3(x0 + b * stall, lineY, zc), new Vector3(0.12f, 0.01f, stallDepth), paint, collider: false);
                }
            }

            // Paso de peatones pintado por donde se cruza hacia el portal.
            if (TowersCrossing.Overlaps(lot))
                for (float z = lot.yMin + 0.6f; z < lot.yMax - 0.3f; z += 1f)
                    Box("PasoPeatones", group, new Vector3(TowersCrossing.center.x, lineY, z), new Vector3(TowersCrossing.width, 0.01f, 0.5f), paint, collider: false);

            // Farolas de sodio en el césped del borde norte (la base baja al césped: el poste empieza a la altura de acera).
            foreach (float t in new[] { 0.25f, 0.75f })
                StreetLight(group, new Vector3(Mathf.Lerp(lot.xMin, lot.xMax, t), LawnTop - SidewalkTop, lot.yMax + 0.8f), Vector3.back, pole, lamp);
        }

        // ---------- Patios: mobiliario, tendederos, canasta, sofá y caminos de barro ----------

        static void BuildTowersYard(Transform parent, System.Random rng)
        {
            var group = new GameObject("Patios").transform;
            group.SetParent(parent, false);
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var galvanized = Mat("PosteGalvanizado", new Color(0.5f, 0.52f, 0.53f));
            var lampGlow = EmissiveMat("LuzFarolaParque", new Color(1f, 0.78f, 0.5f), 2.5f);
            var blueGlow = EmissiveMat("LuzAzulCamara", new Color(0.15f, 0.35f, 1f), 3f);
            var cable = Mat("Cable", new Color(0.03f, 0.03f, 0.03f));
            var clothes = TowersClothes();

            // Cámara de vigilancia delante de T2.
            SecurityCamera(group, new Vector3(6.5f, LawnTop, -10.6f), galvanized, blueGlow);
            towersOccupied.Add(RectAround(6.5f, -10.6f, 1f, 1f));

            // Losa de hormigón con una canasta portátil, entre T1 y T2.
            var pad = new Vector3(-26f, 0f, 30f);
            Box("PistaHormigon", group, new Vector3(pad.x, 0.12f, pad.z), new Vector3(10f, 0.06f, 8f), concreteMat);
            PortableHoop(group, new Vector3(pad.x, 0.15f, pad.z + 3.3f), 0f, galvanized,
                Mat("TableroPortatil", new Color(0.85f, 0.85f, 0.82f)), Mat("AroCanastaPortatil", new Color(0.75f, 0.25f, 0.08f)));
            towersOccupied.Add(RectAround(pad.x, pad.z, 11f, 9f));

            // Vida del bloque (Studio.cs): reunión junto a la canasta, memorial del eje y estudio en el sótano de T1.
            // Van antes de las farolas y bancos del eje para que esos respeten su sitio. Semilla propia: no mueve el resto.
            var lifeRng = new System.Random(1017);
            BuildHangout(group, pad, lifeRng);
            BuildMemorial(group, lifeRng);
            BuildStudio(group);
            BuildApartment(group); // Apartment.cs: ascensor en el portal de T2 y el piso del jugador

            // Tendederos detrás de las adosadas y de T1.
            foreach (var (a, b) in new[]
                     {
                         (new Vector3(-82f, LawnTop, 23f), new Vector3(-70f, LawnTop, 23f)),
                         (new Vector3(-58f, LawnTop, 24.5f), new Vector3(-47f, LawnTop, 24.5f)),
                         (new Vector3(-12f, LawnTop, 44f), new Vector3(0f, LawnTop, 44f)),
                         (new Vector3(50f, LawnTop, 44.5f), new Vector3(62f, LawnTop, 44.5f)),
                         (new Vector3(74f, LawnTop, -40f), new Vector3(74f, LawnTop, -28f)),
                     })
            {
                Clothesline(group, a, b, galvanized, cable, clothes, rng);
                towersOccupied.Add(Grow(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)), 1f));
            }

            // Sofá viejo sacado al patio este.
            OldSofa(group, new Vector3(50f, LawnTop, -30f), 30f);
            towersOccupied.Add(RectAround(50f, -30f, 3f, 3f));

            // Caminos de barro donde la gente ataja por el césped.
            var mud = LoadMat("Barro");
            if (mud)
            {
                DesirePath(group, new Vector3(-38f, 0f, -9f), new Vector3(-39.5f, 0f, -32f), mud, rng);
                DesirePath(group, new Vector3(-39.5f, 0f, -32f), new Vector3(-49f, 0f, -46.5f), mud, rng);
                DesirePath(group, new Vector3(40f, 0f, -8.6f), new Vector3(52f, 0f, -46.5f), mud, rng);
                DesirePath(group, new Vector3(-8f, 0f, 47.5f), new Vector3(-21f, 0f, 34.5f), mud, rng);
            }

            // Eje E–O: farolas de parque al norte y bancos (con papelera) al sur, cada 10 m alternando, donde hay sitio.
            int k = 0;
            for (float x = -95f; x <= 95f; x += 10f, k++)
            {
                if (k % 2 == 0)
                {
                    var r = RectAround(x, -4.9f, 0.8f, 0.8f);
                    if (!TowersFree(r, 0f)) continue;
                    ParkLamp(group, new Vector3(x, LawnTop, -4.9f), ironMat, lampGlow);
                    towersOccupied.Add(r);
                }
                else
                {
                    var r = RectAround(x, -9.2f, 2.2f, 1f);
                    if (!TowersFree(r, 0f)) continue;
                    Bench(group, new Vector3(x, LawnTop, -9.2f), 180f, wood, concreteMat);
                    if (rng.NextDouble() < 0.5) TrashCan(group, new Vector3(x + 1.6f, LawnTop, -9.3f), 0f, rng.NextDouble() < 0.2);
                    towersOccupied.Add(Grow(r, 0.8f));
                }
            }
        }

        /// <summary>Tendedero: dos postes en T con dos cuerdas y ropa colgada.</summary>
        static void Clothesline(Transform parent, Vector3 a, Vector3 b, Material pole, Material cable, Material[] clothes, System.Random rng)
        {
            var dir = (b - a).normalized;
            var across = Vector3.Cross(Vector3.up, dir);
            foreach (var p in new[] { a, b })
            {
                Primitive(PrimitiveType.Cylinder, "PosteTendedero", parent, p + Vector3.up * 1.1f, new Vector3(0.08f, 1.1f, 0.08f), pole, keepCollider: true);
                var bar = Box("Travesano", parent, p + Vector3.up * 2.1f, new Vector3(0.06f, 0.06f, 1.2f), pole, collider: false);
                bar.transform.localRotation = Quaternion.LookRotation(across, Vector3.up);
            }
            const float sag = 0.12f;
            float length = Vector3.Distance(a, b);
            foreach (float o in new[] { -0.45f, 0.45f })
            {
                var p0 = a + Vector3.up * 2.08f + across * o;
                var p1 = b + Vector3.up * 2.08f + across * o;
                Wire(parent, p0, p1, sag, cable);
                for (float t = 0.8f; t < length - 0.8f; t += Range(rng, 0.9f, 1.8f))
                {
                    if (rng.NextDouble() < 0.35) continue;
                    float s = t / length, h = Range(rng, 0.4f, 0.8f);
                    var pos = Vector3.Lerp(p0, p1, s) + Vector3.down * (4f * sag * s * (1f - s) + 0.05f + h / 2f);
                    var cloth = Box("Ropa", parent, pos, new Vector3(Range(rng, 0.4f, 0.9f), h, 0.02f), clothes[rng.Next(clothes.Length)], collider: false);
                    cloth.transform.localRotation = Quaternion.LookRotation(across, Vector3.up);
                }
            }
        }

        /// <summary>Canasta portátil: base de plástico, poste inclinado, tablero y aro, mirando hacia −Z local.</summary>
        static void PortableHoop(Transform parent, Vector3 ground, float yaw, Material pole, Material board, Material rim)
        {
            var root = new GameObject("CanastaPortatil").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Base", root, new Vector3(0f, 0.25f, 0.5f), new Vector3(1.1f, 0.5f, 1.2f), Mat("BasePlastico", new Color(0.12f, 0.12f, 0.13f)));
            Strut("Poste", root, new Vector3(0f, 0.5f, 0.6f), new Vector3(0f, 3.3f, 0f), 0.1f, pole, cylinder: true);
            Box("Tablero", root, new Vector3(0f, 3.4f, -0.05f), new Vector3(1.4f, 0.9f, 0.05f), board, collider: false);
            var center = new Vector3(0f, 3.05f, -0.35f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var segment = Box("Aro", root, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.23f, new Vector3(0.025f, 0.025f, 0.13f), rim, collider: false);
                segment.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        /// <summary>Sofá naranja viejo, mirando hacia −Z local.</summary>
        static void OldSofa(Transform parent, Vector3 ground, float yaw)
        {
            var root = new GameObject("SofaViejo").transform;
            root.SetParent(parent, false);
            root.localPosition = ground;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var fabric = Mat("SofaNaranja", new Color(0.62f, 0.32f, 0.12f));
            Box("Asiento", root, new Vector3(0f, 0.25f, 0f), new Vector3(2f, 0.45f, 0.9f), fabric);
            Box("Respaldo", root, new Vector3(0f, 0.65f, 0.35f), new Vector3(2f, 0.6f, 0.25f), fabric, collider: false);
            foreach (float x in new[] { -0.95f, 0.95f })
                Box("Brazo", root, new Vector3(x, 0.5f, 0f), new Vector3(0.22f, 0.4f, 0.9f), fabric, collider: false);
            Box("Cojin", root, new Vector3(0.45f, 0.52f, -0.05f), new Vector3(0.8f, 0.12f, 0.7f), fabric, collider: false)
                .transform.localRotation = Quaternion.Euler(8f, 12f, 0f);
        }

        /// <summary>Camino de barro (manchas encadenadas) sobre el césped, de a a b.</summary>
        static void DesirePath(Transform parent, Vector3 a, Vector3 b, Material mud, System.Random rng)
        {
            var d = b - a;
            d.y = 0f;
            float length = d.magnitude, yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            var side = Vector3.Cross(Vector3.up, d.normalized);
            int i = 0;
            for (float t = 0.5f; t < length; t += 1.4f, i++)
            {
                var p = a + d * (t / length) + side * Range(rng, -0.25f, 0.25f);
                Decal(parent, new Vector3(p.x, LawnTop + 0.004f + (i % 3) * 0.0005f, p.z), yaw + Range(rng, -12f, 12f),
                    new Vector2(Range(rng, 0.9f, 1.3f), Range(rng, 1.6f, 2.2f)), mud);
            }
        }

        // ---------- Valla del perímetro ----------

        static void BuildTowersFence(Transform parent)
        {
            var group = new GameObject("VallaPerimetro").transform;
            group.SetParent(parent, false);
            const float inset = 0.6f;
            float fx = BlockHalfX - inset, fz = BlockHalfZ - inset;
            FenceWithGates(group, true, fz, -fx, fx);
            FenceWithGates(group, true, -fz, -fx, fx);
            FenceWithGates(group, false, fx, -fz, fz);
            FenceWithGates(group, false, -fx, -fz, fz);
        }

        /// <summary>Valla baja de hierro a lo largo de una línea, abierta donde llegan caminos (towersGates) y entradas de aparcamiento.</summary>
        static void FenceWithGates(Transform parent, bool alongX, float line, float start, float end)
        {
            var openings = new List<(float a, float b)>();
            foreach (var (p, half) in towersGates)
            {
                float across = alongX ? p.z : p.x, along = alongX ? p.x : p.z;
                if (Mathf.Abs(across - line) <= 1.5f) openings.Add((along - half - 0.5f, along + half + 0.5f));
            }
            if (alongX)
                foreach (var d in TowersDriveways)
                    if (Mathf.Abs(d.center.y - line) < 6f) openings.Add((d.xMin - 0.3f, d.xMax + 0.3f));
            openings.Sort((p, q) => p.a.CompareTo(q.a));

            var tile = new Vector2(ProceduralTextures.IronFenceWidth, 1.1f);
            void Segment(float s0, float s1)
            {
                if (s1 - s0 < 0.3f) return;
                var a = alongX ? new Vector3(s0, LawnTop, line) : new Vector3(line, LawnTop, s0);
                var b = alongX ? new Vector3(s1, LawnTop, line) : new Vector3(line, LawnTop, s1);
                FenceSegment(parent, a, b, 1.1f, ironFence, tile, ironMat, 2.5f, 0.05f, topRail: false);
            }
            float cursor = start;
            foreach (var (a, b) in openings)
            {
                Segment(cursor, Mathf.Min(a, end));
                cursor = Mathf.Max(cursor, b);
            }
            Segment(cursor, end);
        }

        // ---------- Árboles, nieve, maleza y basura ----------

        static void BuildTowersWeather(Transform parent, System.Random rng)
        {
            var group = new GameObject("ArbolesNieveBasura").transform;
            group.SetParent(parent, false);
            var bark = LoadMat("Corteza");

            for (int i = 0; i < 34; i++)
                if (TowersRandomPoint(rng, 2.5f, out var p))
                {
                    DistrictTree(group, p, bark, rng, Range(rng, 0.85f, 1.25f), rng);
                    towersOccupied.Add(RectAround(p.x, p.z, 1.5f, 1.5f));
                }

            for (int i = 0; i < 70; i++)
                if (TowersRandomPoint(rng, 0.3f, out var p))
                    SnowPatch(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.4f, 3.5f), Range(rng, 1f, 2.4f)));

            // Montones de nieve apartada a los lados del eje.
            for (int i = 0; i < 16; i++)
            {
                float x = Range(rng, -95f, 95f), z = rng.Next(2) == 0 ? -4.7f : -9.3f;
                if (!TowersFree(RectAround(x, z, 2f, 0.6f), 0f)) continue;
                SnowMound(group, new Vector3(x, LawnTop, z), new Vector3(Range(rng, 1.2f, 2.4f), Range(rng, 0.2f, 0.35f), 0.8f), 0f);
            }

            // Maleza junto a la valla del perímetro, fuera de caminos y entradas.
            for (int i = 0; i < 60; i++)
            {
                int side = rng.Next(4);
                float t = Range(rng, -0.97f, 0.97f);
                var p = side switch
                {
                    0 => new Vector3(t * BlockHalfX, LawnTop, BlockHalfZ - 1f),
                    1 => new Vector3(t * BlockHalfX, LawnTop, -BlockHalfZ + 1f),
                    2 => new Vector3(BlockHalfX - 1f, LawnTop, t * BlockHalfZ),
                    _ => new Vector3(-BlockHalfX + 1f, LawnTop, t * BlockHalfZ),
                };
                if (InsideAny(towersWalks, p) || InsideAny(TowersDriveways, p) || NearTowersGate(p, 1.5f)) continue;
                Clump(group, rng, p, 0.35f, 1.1f);
            }

            // Basura en caminos y aparcamientos, bolsas sueltas en el césped y charcos en el asfalto.
            var paper = (name: "Papel", material: Mat("BasuraPapel", new Color(0.82f, 0.82f, 0.78f)), shape: PrimitiveType.Cube, size: new Vector3(0.22f, 0.01f, 0.3f));
            var can = (name: "Lata", material: Mat("BasuraLata", new Color(0.62f, 0.62f, 0.65f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.07f, 0.06f, 0.07f));
            var bottle = (name: "Botella", material: Mat("BasuraBotella", new Color(0.22f, 0.32f, 0.14f)), shape: PrimitiveType.Cylinder, size: new Vector3(0.08f, 0.13f, 0.08f));
            var kinds = new[] { paper, can, bottle };
            for (int i = 0; i < 150; i++)
            {
                bool onLot = rng.NextDouble() < 0.4;
                var r = onLot ? TowersLots[rng.Next(TowersLots.Length)] : towersWalks[rng.Next(towersWalks.Count)];
                var p = new Vector3(Range(rng, r.xMin + 0.2f, r.xMax - 0.2f), onLot ? LotTop : WalkTop, Range(rng, r.yMin + 0.2f, r.yMax - 0.2f));
                PlaceLitter(group, kinds[rng.Next(kinds.Length)], p, rng);
            }
            for (int i = 0; i < 12; i++)
                if (TowersRandomPoint(rng, 0.2f, out var p))
                    GarbageBag(group, p, rng);
            var puddle = LoadMat("Charco");
            if (puddle)
                for (int i = 0; i < 14; i++)
                {
                    var r = TowersLots[rng.Next(TowersLots.Length)];
                    Decal(group, new Vector3(Range(rng, r.xMin + 1f, r.xMax - 1f), LotTop + 0.0045f + i * 0.0001f, Range(rng, r.yMin + 1f, r.yMax - 1f)),
                        Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 3.5f), Range(rng, 1.2f, 2.5f)), puddle);
                }
        }

        // ---------- Utilidades ----------

        static Rect Grow(Rect r, float margin) => Rect.MinMaxRect(r.xMin - margin, r.yMin - margin, r.xMax + margin, r.yMax + margin);

        /// <summary>Rectángulo del mundo que ocupa el rectángulo local (x0, z0)–(x1, z1) de <paramref name="frame"/> (giros de 90°).</summary>
        static Rect FrameRect(Transform frame, float x0, float z0, float x1, float z1)
        {
            var a = frame.TransformPoint(new Vector3(x0, 0f, z0));
            var b = frame.TransformPoint(new Vector3(x1, 0f, z1));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
        }

        static bool TowersFree(Rect r, float margin)
        {
            var e = Grow(r, margin);
            foreach (var o in towersOccupied)
                if (o.Overlaps(e)) return false;
            return true;
        }

        static bool TowersRandomPoint(System.Random rng, float margin, out Vector3 point)
        {
            for (int i = 0; i < 30; i++)
            {
                point = new Vector3(Range(rng, -97f, 97f), LawnTop, Range(rng, -67f, 67f));
                if (TowersFree(RectAround(point.x, point.z, 0.1f, 0.1f), margin)) return true;
            }
            point = default;
            return false;
        }

        static bool NearTowersGate(Vector3 p, float distance)
        {
            foreach (var (g, half) in towersGates)
                if (Mathf.Abs(g.x - p.x) < half + distance && Mathf.Abs(g.z - p.z) < half + distance) return true;
            return false;
        }
    }
}
