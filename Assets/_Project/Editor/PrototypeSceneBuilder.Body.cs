using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Cuerpo del personaje (<see cref="BuildPlayerBody"/>) y la malla por anillos con la que se hace (<see cref="LoftMesh"/>).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static Texture2D[] clothTextures;
        static Texture2D skinTexture;

        /// <summary>Anillo de una malla por anillos: altura, semiancho (X), semifondo (Z) y desplazamiento del centro en Z.</summary>
        readonly struct Ring
        {
            public readonly float Y, Rx, Rz, Cz;
            public Ring(float y, float rx, float rz, float cz = 0f) { Y = y; Rx = rx; Rz = rz; Cz = cz; }
        }

        /// <summary>
        /// Malla low-poly por anillos (como las de PS2): cada anillo es una superelipse de <paramref name="sides"/> lados
        /// (<paramref name="squareness"/> 2 = elipse, más = más cuadrada), unidos en orden de altura, con sombreado suave
        /// y tapas opcionales. UV: U da la vuelta empezando por detrás (U = 0.5 es el frente, +Z) y V va de abajo (0) a
        /// arriba (1), lo que esperan las texturas de ropa y la de la cara.
        /// </summary>
        static Mesh LoftMesh(string name, Ring[] rings, int sides, float squareness, bool capBottom, bool capTop)
        {
            var sorted = (Ring[])rings.Clone();
            System.Array.Sort(sorted, (a, b) => a.Y.CompareTo(b.Y));
            float minY = sorted[0].Y, maxY = sorted[^1].Y;
            float e = 2f / squareness;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int stride = sides + 1;

            Vector3 Point(Ring r, int k)
            {
                float a = 2f * Mathf.PI * k / sides;
                float s = Mathf.Sin(a), c = -Mathf.Cos(a);
                return new Vector3(Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e) * r.Rx, r.Y, r.Cz + Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e) * r.Rz);
            }

            foreach (var r in sorted)
                for (int k = 0; k <= sides; k++)
                {
                    verts.Add(Point(r, k));
                    uvs.Add(new Vector2((float)k / sides, Mathf.InverseLerp(minY, maxY, r.Y)));
                }
            for (int i = 0; i < sorted.Length - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = i * stride + k, b = a + 1, c = a + stride, d = c + 1;
                    tris.AddRange(new[] { a, c, d, a, d, b });
                }

            void Cap(Ring r, bool top)
            {
                int center = verts.Count;
                verts.Add(new Vector3(0f, r.Y, r.Cz));
                uvs.Add(new Vector2(0.5f, top ? 1f : 0f));
                for (int k = 0; k <= sides; k++)
                {
                    verts.Add(Point(r, k));
                    uvs.Add(new Vector2((float)k / sides, top ? 1f : 0f));
                }
                for (int k = 0; k < sides; k++)
                    tris.AddRange(top ? new[] { center, center + 2 + k, center + 1 + k } : new[] { center, center + 1 + k, center + 2 + k });
            }
            if (capBottom) Cap(sorted[0], false);
            if (capTop) Cap(sorted[^1], true);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // Costura: los dos vértices de U = 0 y U = 1 comparten normal para que no se vea la raja.
            var normals = mesh.normals;
            for (int i = 0; i < sorted.Length; i++)
            {
                int a = i * stride, b = a + sides;
                normals[a] = normals[b] = (normals[a] + normals[b]).normalized;
            }
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        static GameObject Loft(string name, Transform parent, Vector3 localPosition, Material material, int sides, float squareness,
            bool capBottom, bool capTop, params Ring[] rings)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = LoftMesh(name, rings, sides, squareness, capBottom, capTop);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Z del frente de una malla por anillos a la altura <paramref name="y"/> (interpolando entre anillos).</summary>
        static float FrontZ(Ring[] rings, float y)
        {
            for (int i = 0; i < rings.Length - 1; i++)
            {
                var a = rings[i];
                var b = rings[i + 1];
                if (y >= Mathf.Min(a.Y, b.Y) && y <= Mathf.Max(a.Y, b.Y))
                {
                    float t = Mathf.InverseLerp(a.Y, b.Y, y);
                    return Mathf.Lerp(a.Cz + a.Rz, b.Cz + b.Rz, t);
                }
            }
            return rings[^1].Cz + rings[^1].Rz;
        }

        /// <summary>
        /// Cuerpo low-poly del personaje con articulaciones para animarlo (<see cref="PlayerAnimator"/>) y piezas para
        /// vestirlo (<see cref="PlayerOutfit"/>), con la forma y la pinta de la referencia img/gang.png: mallas facetadas
        /// por anillos (<see cref="LoftMesh"/>) en vez de cápsulas y esferas, de proporciones de PS2: torso cuadrado y
        /// estrecho de cintura, camiseta XL que cae recta hasta medio muslo con mangas anchas hasta el codo, brazos largos
        /// y delgados, piernas largas con vaqueros anchos y rectos amontonados en el tobillo, botas grandes y cabeza
        /// ovalada con mandíbula y la cara pintada en la textura (ojos, cejas, labios, bigote, perilla y pelo rapado,
        /// <see cref="ProceduralTextures.Face"/>), con nariz y orejas de malla. Jerarquía (las piezas cuelgan hacia −Y de
        /// su articulación): Cuerpo (pelvis, a 0.92 m, con cinturón) → Cadera → Muslo, bolsillo de caqui, Rodilla →
        /// Espinilla, Bajo del pantalón, Caña de bota, Tobillo → Pie, Suela; Cuerpo → Columna → Torso, Faldón y cuello de
        /// camiseta, Capucha, Cordones, CuelloAlto, Chaleco, CuelloCamisa, Botonera, Cuello, Cadena, Craneo (cabeza,
        /// nariz, orejas, gorra, gorro, pescador, pañuelo, gafas) y Hombro → Deltoide, Brazo, Manga, Codo → Antebrazo,
        /// Mano (palma, dedos, pulgar). La ropa lleva textura de tejido (<see cref="ProceduralTextures.Cloth"/>) y los
        /// materiales "Ropa_&lt;prenda&gt;" de la ropa de inicio, para que salga vestido también en el editor y en las
        /// capturas; en juego PlayerOutfit la tiñe con la que se lleve.
        /// </summary>
        static void BuildPlayerBody(Transform model)
        {
            // Texturas: una por estilo de prenda (en el orden de ClothingStyle), piel y pelo.
            var patternOf = new[]
            {
                ProceduralTextures.ClothPattern.Twill,   // Cap
                ProceduralTextures.ClothPattern.Knit,    // Beanie
                ProceduralTextures.ClothPattern.Knit,    // Hoodie
                ProceduralTextures.ClothPattern.Puffer,  // Puffer
                ProceduralTextures.ClothPattern.Cotton,  // Tee
                ProceduralTextures.ClothPattern.Denim,   // Jeans
                ProceduralTextures.ClothPattern.Knit,    // Sweatpants
                ProceduralTextures.ClothPattern.Sneaker, // Sneakers
                ProceduralTextures.ClothPattern.Suede,   // Boots
                ProceduralTextures.ClothPattern.Twill,   // BucketHat
                ProceduralTextures.ClothPattern.Satin,   // Durag
                ProceduralTextures.ClothPattern.Stripes, // Polo
                ProceduralTextures.ClothPattern.Puffer,  // Vest
                ProceduralTextures.ClothPattern.Denim,   // Shirt
                ProceduralTextures.ClothPattern.Twill,   // Khakis
                ProceduralTextures.ClothPattern.Satin,   // Sunglasses
                ProceduralTextures.ClothPattern.Satin,   // Chain
                ProceduralTextures.ClothPattern.Satin,   // ChainPendant
            };
            clothTextures = new Texture2D[patternOf.Length];
            for (int i = 0; i < patternOf.Length; i++)
                clothTextures[i] = ProceduralTextures.Cloth("Tejido_" + (ClothingStyle)i, patternOf[i], 70 + i);
            skinTexture = ProceduralTextures.Skin("PielPersonaje", 81);
            var skinColor = new Color(0.36f, 0.24f, 0.17f);
            var hairColor = new Color(0.09f, 0.07f, 0.06f);

            Material Cloth(string id)
            {
                var def = ClothingCatalog.Get(id);
                return TexturedMat("Ropa_" + id, clothTextures[(int)def.Style], def.Color);
            }
            Material Starter(ClothingSlot slot)
            {
                foreach (var id in ClothingCatalog.StarterOutfit)
                    if (ClothingCatalog.Get(id).Slot == slot) return Cloth(id);
                return null;
            }
            var headMat = Starter(ClothingSlot.Head);
            var torsoMat = Starter(ClothingSlot.Torso);
            var legsMat = Starter(ClothingSlot.Legs);
            var feetMat = Starter(ClothingSlot.Feet);
            var beanieMat = Cloth("gorro_gris");
            var bucketMat = Cloth("pescador_blanco");
            var duragMat = Cloth("panuelo_negro");
            var vestMat = Cloth("chaleco_negro");
            var teeMat = Cloth("camiseta_blanca");
            var shirtMat = Cloth("camisa_vaquera");
            var frameMat = Cloth("gafas_negras");
            var goldMat = Cloth("cadena_oro");
            var skin = TexturedMat("Piel", skinTexture, skinColor);
            // La cara va pintada a todo color (piel, ojos, labios, barba y pelo): el material no la tiñe.
            var face = TexturedMat("CaraPersonaje", ProceduralTextures.Face("CaraPersonaje", skinColor, hairColor, 84), Color.white);
            var lens = Mat("CristalGafas", new Color(0.03f, 0.03f, 0.04f));
            var sole = Mat("SuelaZapatilla", new Color(0.9f, 0.9f, 0.88f));
            var cord = Mat("CordonSudadera", new Color(0.85f, 0.85f, 0.83f));
            var belt = Mat("Cinturon", new Color(0.1f, 0.07f, 0.05f));
            var buckle = Mat("Hebilla", new Color(0.7f, 0.7f, 0.72f));

            GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
                => Primitive(type, name, parent, pos, scale, mat, keepCollider: false);
            Transform Joint(string name, Transform parent, Vector3 pos)
            {
                var t = new GameObject(name).transform;
                t.SetParent(parent, false);
                t.localPosition = pos;
                return t;
            }
            Transform Hidden(string name, Transform parent)
            {
                var t = Joint(name, parent, Vector3.zero);
                t.gameObject.SetActive(false);
                return t;
            }

            // Pelvis, con cinturón (lo tapa el faldón de la camiseta XL).
            var pelvis = Joint("Cuerpo", model, new Vector3(0f, 0.92f, 0f));
            Loft("Pelvis", pelvis, Vector3.zero, legsMat, 10, 2.4f, true, true,
                new Ring(-0.13f, 0.17f, 0.12f), new Ring(-0.03f, 0.2f, 0.13f), new Ring(0.07f, 0.19f, 0.125f));
            Loft("Cinturon", pelvis, Vector3.zero, belt, 10, 2.4f, false, false,
                new Ring(0.03f, 0.197f, 0.132f), new Ring(0.07f, 0.197f, 0.132f));
            Part(PrimitiveType.Cube, "Hebilla", pelvis, new Vector3(0f, 0.05f, 0.135f), new Vector3(0.06f, 0.04f, 0.012f), buckle);

            // Piernas: muslo y espinilla de 0.44 + 0.40 m, tobillo a 6 cm del suelo. Vaqueros anchos y rectos (casi sin
            // cintura de rodilla) con el bajo amontonado en ondas sobre el zapato; el caqui lleva bolsillos a los lados.
            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0f ? "Izq" : "Der";
                var hip = Joint("Cadera" + s, pelvis, new Vector3(side * 0.1f, -0.02f, 0f));
                Loft("Muslo" + s, hip, Vector3.zero, legsMat, 8, 2.1f, false, false,
                    new Ring(0.04f, 0.112f, 0.12f), new Ring(-0.2f, 0.108f, 0.112f, 0.005f), new Ring(-0.47f, 0.096f, 0.1f, 0.008f));
                Part(PrimitiveType.Cube, "BolsilloCargo" + s, hip, new Vector3(side * 0.118f, -0.27f, 0f), new Vector3(0.035f, 0.13f, 0.12f), legsMat)
                    .SetActive(false);
                var knee = Joint("Rodilla" + s, hip, new Vector3(0f, -0.44f, 0f));
                Loft("Espinilla" + s, knee, Vector3.zero, legsMat, 8, 2.1f, false, false,
                    new Ring(0.03f, 0.096f, 0.1f, 0.008f), new Ring(-0.16f, 0.092f, 0.096f, 0.005f), new Ring(-0.3f, 0.096f, 0.1f, 0.008f));
                Loft("Bajo" + s, knee, Vector3.zero, legsMat, 8, 2.1f, false, true,
                    new Ring(-0.28f, 0.098f, 0.102f, 0.008f), new Ring(-0.315f, 0.114f, 0.12f, 0.012f), new Ring(-0.345f, 0.104f, 0.112f, 0.012f),
                    new Ring(-0.37f, 0.116f, 0.128f, 0.016f), new Ring(-0.395f, 0.112f, 0.125f, 0.018f));
                Part(PrimitiveType.Cylinder, "Cana" + s, knee, new Vector3(0f, -0.33f, 0.01f), new Vector3(0.2f, 0.07f, 0.21f), feetMat).SetActive(false);
                var ankle = Joint("Tobillo" + s, knee, new Vector3(0f, -0.4f, 0f));
                // Pie grande y cuadrado, con la puntera algo levantada.
                Part(PrimitiveType.Cube, "Pie" + s, ankle, new Vector3(0f, 0.01f, 0.065f), new Vector3(0.15f, 0.12f, 0.31f), feetMat);
                Part(PrimitiveType.Cube, "Suela" + s, ankle, new Vector3(0f, -0.045f, 0.065f), new Vector3(0.165f, 0.04f, 0.32f), sole);
                Part(PrimitiveType.Cube, "Puntera" + s, ankle, new Vector3(0f, -0.005f, 0.21f), new Vector3(0.145f, 0.07f, 0.05f), sole)
                    .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            }

            // Columna: torso cuadrado, ancho de pecho y estrecho de cintura, con los hombros caídos hacia el cuello (no
            // de hombreras), y todo lo que cambia con la prenda de arriba.
            var spine = Joint("Columna", pelvis, new Vector3(0f, 0.05f, 0f));
            var torsoRings = new[]
            {
                new Ring(-0.02f, 0.18f, 0.122f), new Ring(0.12f, 0.188f, 0.13f, 0.005f), new Ring(0.3f, 0.205f, 0.145f, 0.012f),
                new Ring(0.41f, 0.21f, 0.135f, 0.006f), new Ring(0.49f, 0.18f, 0.112f), new Ring(0.555f, 0.09f, 0.07f),
            };
            Loft("Torso", spine, Vector3.zero, torsoMat, 10, 2.6f, true, true, torsoRings);
            // Camiseta XL: faldón que cae recto y se abre un poco hasta medio muslo, y cuello redondo.
            Loft("Faldon", spine, Vector3.zero, teeMat, 10, 2.6f, false, false,
                new Ring(0.04f, 0.195f, 0.135f, 0.005f), new Ring(-0.12f, 0.215f, 0.145f, 0.005f), new Ring(-0.28f, 0.235f, 0.16f, 0.008f))
                .SetActive(false);
            Loft("CuelloCamiseta", spine, Vector3.zero, teeMat, 10, 2f, false, true,
                new Ring(0.56f, 0.08f, 0.075f, 0.005f), new Ring(0.585f, 0.075f, 0.07f, 0.005f)).SetActive(false);
            // Sudadera: capucha caída, bolsillo canguro y cordones.
            var hood = Joint("Capucha", spine, Vector3.zero);
            Loft("CapuchaCaida", hood, new Vector3(0f, 0f, -0.1f), torsoMat, 10, 2.2f, true, true,
                new Ring(0.38f, 0.1f, 0.05f, -0.02f), new Ring(0.48f, 0.16f, 0.09f, -0.04f), new Ring(0.56f, 0.13f, 0.08f, -0.04f), new Ring(0.6f, 0.08f, 0.05f, -0.04f));
            Part(PrimitiveType.Cube, "Bolsillo", hood, new Vector3(0f, 0.12f, FrontZ(torsoRings, 0.12f) + 0.012f), new Vector3(0.28f, 0.14f, 0.03f), torsoMat);
            var hoodCords = Joint("Cordones", spine, Vector3.zero);
            foreach (float side in new[] { -0.045f, 0.045f })
                Part(PrimitiveType.Cube, "Cordon", hoodCords, new Vector3(side, 0.45f, FrontZ(torsoRings, 0.45f) + 0.012f), new Vector3(0.012f, 0.16f, 0.012f), cord);
            // Plumífero: cuello alto.
            Loft("CuelloAlto", spine, Vector3.zero, torsoMat, 10, 2.2f, false, true,
                new Ring(0.5f, 0.15f, 0.1f), new Ring(0.64f, 0.12f, 0.105f, 0.005f)).SetActive(false);
            // Chaleco acolchado, abierto por delante (se ve la camiseta de debajo). Va por encima de la camiseta XL, que
            // ensancha el torso un 7 %.
            var vest = Hidden("Chaleco", spine);
            var vestRings = new Ring[torsoRings.Length - 1];
            for (int i = 0; i < vestRings.Length; i++)
            {
                var r = torsoRings[i];
                vestRings[i] = new Ring(r.Y, r.Rx * 1.13f, r.Rz * 1.15f, r.Cz);
            }
            vestRings[^1] = new Ring(0.56f, 0.15f, 0.11f, -0.01f);
            Loft("ChalecoCuerpo", vest, Vector3.zero, vestMat, 10, 2.6f, false, false, vestRings);
            Part(PrimitiveType.Cube, "AberturaChaleco", spine, new Vector3(0f, 0.27f, FrontZ(vestRings, 0.27f) + 0.004f), new Vector3(0.1f, 0.5f, 0.012f), teeMat)
                .SetActive(false);
            // Camisa y polo: cuello de solapas alrededor del cuello; la camisa, botonera.
            var collar = Hidden("CuelloCamisa", spine);
            foreach (float side in new[] { -1f, 1f })
                Part(PrimitiveType.Cube, "Solapa", collar, new Vector3(side * 0.05f, 0.575f, 0.065f), new Vector3(0.08f, 0.025f, 0.07f), shirtMat)
                    .transform.localRotation = Quaternion.Euler(-25f, side * 30f, side * 10f);
            Part(PrimitiveType.Cube, "Botonera", spine, new Vector3(0f, 0.28f, FrontZ(torsoRings, 0.28f) + 0.004f), new Vector3(0.035f, 0.5f, 0.012f), shirtMat)
                .SetActive(false);
            Loft("Cuello", spine, Vector3.zero, skin, 8, 2f, false, false, new Ring(0.5f, 0.06f, 0.058f), new Ring(0.65f, 0.055f, 0.055f, 0.005f));

            // Cadena: eslabones en U alrededor del cuello, apoyados sobre el pecho, y colgante opcional.
            var chain = Hidden("Cadena", spine);
            const int links = 13;
            var linkPos = new Vector3[links];
            for (int k = 0; k < links; k++)
            {
                float a = Mathf.Lerp(-1f, 1f, k / (links - 1f));
                float dip = 1f - a * a;
                float ly = 0.58f - dip * 0.2f;
                float lz = ly > 0.55f ? 0.05f : Mathf.Max(0.05f, FrontZ(torsoRings, ly) * 1.07f + 0.012f);
                linkPos[k] = new Vector3(a * 0.085f, ly, lz);
            }
            for (int k = 0; k < links; k++)
            {
                var tangent = linkPos[Mathf.Min(k + 1, links - 1)] - linkPos[Mathf.Max(k - 1, 0)];
                Part(PrimitiveType.Cube, "Eslabon", chain, linkPos[k], new Vector3(0.018f, 0.012f, 0.032f), goldMat)
                    .transform.localRotation = Quaternion.LookRotation(tangent);
            }
            var pendant = Joint("Colgante", chain, new Vector3(0f, 0.375f, FrontZ(torsoRings, 0.35f) * 1.07f + 0.015f));
            Part(PrimitiveType.Cube, "Medalla", pendant, new Vector3(0f, -0.03f, 0f), new Vector3(0.055f, 0.065f, 0.012f), goldMat)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Cabeza ovalada de 12 lados con mandíbula y barbilla adelantadas, con la cara pintada (U = 0.5 es el frente,
            // V de la barbilla, −0.075, a la coronilla, 0.215); nariz y orejas de malla. Encima, lo que se pone en la
            // cabeza (gorra, gorro, gorro de pescador, pañuelo, gafas).
            var head = Joint("Craneo", spine, new Vector3(0f, 0.61f, 0f));
            Loft("Cabeza", head, Vector3.zero, face, 12, 2.2f, true, true,
                new Ring(-0.075f, 0.035f, 0.03f, 0.085f), new Ring(-0.06f, 0.07f, 0.065f, 0.055f), new Ring(-0.03f, 0.098f, 0.1f, 0.025f),
                new Ring(0.02f, 0.112f, 0.122f, 0.004f), new Ring(0.08f, 0.118f, 0.13f, -0.005f), new Ring(0.14f, 0.114f, 0.125f, -0.01f),
                new Ring(0.19f, 0.09f, 0.1f, -0.015f), new Ring(0.215f, 0.04f, 0.05f, -0.015f));
            Part(PrimitiveType.Cube, "Nariz", head, new Vector3(0f, 0.045f, 0.128f), new Vector3(0.032f, 0.065f, 0.035f), skin)
                .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Part(PrimitiveType.Cube, "PuntaNariz", head, new Vector3(0f, 0.016f, 0.138f), new Vector3(0.055f, 0.026f, 0.03f), skin);
            foreach (float side in new[] { -1f, 1f })
                Loft("Oreja", head, new Vector3(side * 0.118f, 0f, -0.005f), skin, 6, 2f, true, true,
                    new Ring(0.025f, 0.018f, 0.022f), new Ring(0.06f, 0.024f, 0.032f), new Ring(0.095f, 0.016f, 0.026f, -0.005f));

            var cap = Joint("Gorra", head, Vector3.zero);
            // Gorra de béisbol: la copa asienta en la frente y por encima de las orejas (antes bajaba hasta ellas y,
            // en negro, parecía un casco), con el frente algo más alto y la visera larga y un poco caída.
            Part(PrimitiveType.Sphere, "Copa", cap, new Vector3(0f, 0.148f, -0.012f), new Vector3(0.272f, 0.175f, 0.29f), headMat)
                .transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
            Part(PrimitiveType.Cube, "Frente", cap, new Vector3(0f, 0.15f, 0.105f), new Vector3(0.2f, 0.09f, 0.03f), headMat)
                .transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Part(PrimitiveType.Cube, "Visera", cap, new Vector3(0f, 0.112f, 0.205f), new Vector3(0.215f, 0.02f, 0.19f), headMat)
                .transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            foreach (float side in new[] { -1f, 1f })
                Part(PrimitiveType.Cube, "ViseraLado", cap, new Vector3(side * 0.095f, 0.106f, 0.2f), new Vector3(0.04f, 0.02f, 0.17f), headMat)
                    .transform.localRotation = Quaternion.Euler(9f, 0f, side * 14f);
            Part(PrimitiveType.Sphere, "Boton", cap, new Vector3(0f, 0.234f, -0.02f), new Vector3(0.03f, 0.015f, 0.03f), headMat);

            var beanie = Hidden("Gorro", head);
            Part(PrimitiveType.Sphere, "Copa", beanie, new Vector3(0f, 0.14f, 0f), new Vector3(0.3f, 0.27f, 0.31f), beanieMat);
            Part(PrimitiveType.Cylinder, "Vuelta", beanie, new Vector3(0f, 0.09f, 0f), new Vector3(0.31f, 0.04f, 0.32f), beanieMat);

            // Gorro de pescador: copa baja y ala ancha caída.
            var bucket = Hidden("Pescador", head);
            Part(PrimitiveType.Cylinder, "Copa", bucket, new Vector3(0f, 0.17f, 0f), new Vector3(0.27f, 0.07f, 0.28f), bucketMat);
            Part(PrimitiveType.Sphere, "Techo", bucket, new Vector3(0f, 0.235f, 0f), new Vector3(0.27f, 0.05f, 0.28f), bucketMat);
            Part(PrimitiveType.Cylinder, "Ala", bucket, new Vector3(0f, 0.11f, 0f), new Vector3(0.42f, 0.012f, 0.43f), bucketMat);
            Part(PrimitiveType.Cylinder, "AlaCaida", bucket, new Vector3(0f, 0.095f, 0f), new Vector3(0.45f, 0.01f, 0.46f), bucketMat);

            // Pañuelo de cabeza: ceñido, con las puntas atadas cayendo por detrás.
            var durag = Hidden("Panuelo", head);
            Part(PrimitiveType.Sphere, "Tela", durag, new Vector3(0f, 0.125f, -0.02f), new Vector3(0.275f, 0.21f, 0.295f), duragMat);
            Part(PrimitiveType.Cube, "Cola", durag, new Vector3(0f, -0.02f, -0.15f), new Vector3(0.07f, 0.22f, 0.015f), duragMat)
                .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Part(PrimitiveType.Sphere, "Nudo", durag, new Vector3(0f, 0.09f, -0.145f), new Vector3(0.06f, 0.04f, 0.04f), duragMat);

            // Gafas de sol: cristales oscuros y montura (la montura se tiñe con la prenda).
            var glasses = Hidden("Gafas", head);
            foreach (float side in new[] { -1f, 1f })
                Part(PrimitiveType.Cube, "Cristal", glasses, new Vector3(side * 0.05f, 0.08f, 0.138f), new Vector3(0.062f, 0.036f, 0.008f), lens);
            var frame = Joint("Montura", glasses, Vector3.zero);
            Part(PrimitiveType.Cube, "Barra", frame, new Vector3(0f, 0.1f, 0.139f), new Vector3(0.17f, 0.012f, 0.012f), frameMat);
            foreach (float side in new[] { -1f, 1f })
                Part(PrimitiveType.Cube, "Patilla", frame, new Vector3(side * 0.12f, 0.093f, 0.07f), new Vector3(0.008f, 0.012f, 0.14f), frameMat);

            // Brazos largos: brazo y antebrazo de 0.3 m, con la manga de la sudadera holgada, y manos grandes (palma, dedos y pulgar). El deltoide
            // redondea el hombro; la manga ancha de la camiseta llega casi al codo.
            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0f ? "Izq" : "Der";
                var shoulder = Joint("Hombro" + s, spine, new Vector3(side * 0.235f, 0.45f, 0f));
                Loft("Deltoide" + s, shoulder, new Vector3(-side * 0.01f, 0f, 0f), torsoMat, 8, 2f, true, true,
                    new Ring(-0.1f, 0.062f, 0.067f), new Ring(-0.03f, 0.075f, 0.082f), new Ring(0.025f, 0.064f, 0.073f), new Ring(0.055f, 0.03f, 0.038f));
                Loft("Brazo" + s, shoulder, Vector3.zero, torsoMat, 8, 2f, false, false,
                    new Ring(0.02f, 0.058f, 0.062f), new Ring(-0.15f, 0.056f, 0.06f), new Ring(-0.31f, 0.048f, 0.051f));
                Loft("Manga" + s, shoulder, Vector3.zero, torsoMat, 8, 2f, false, false,
                    new Ring(0.05f, 0.08f, 0.085f), new Ring(-0.1f, 0.088f, 0.092f), new Ring(-0.24f, 0.092f, 0.096f, 0.005f)).SetActive(false);
                var elbow = Joint("Codo" + s, shoulder, new Vector3(0f, -0.3f, 0f));
                // Manga de sudadera holgada que se recoge en el puño.
                Loft("Antebrazo" + s, elbow, Vector3.zero, torsoMat, 8, 2f, false, false,
                    new Ring(0.02f, 0.05f, 0.053f), new Ring(-0.12f, 0.05f, 0.052f), new Ring(-0.25f, 0.044f, 0.045f), new Ring(-0.29f, 0.036f, 0.034f));
                // Mano: la palma mira al cuerpo con el brazo colgando; el pulgar, hacia delante.
                var hand = Joint("Mano" + s, elbow, new Vector3(0f, -0.3f, 0f));
                Part(PrimitiveType.Cube, "Palma", hand, new Vector3(0f, -0.042f, 0f), new Vector3(0.048f, 0.1f, 0.098f), skin);
                Part(PrimitiveType.Cube, "Dedos", hand, new Vector3(-side * 0.01f, -0.122f, -0.003f), new Vector3(0.04f, 0.08f, 0.092f), skin)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -side * 16f);
                Part(PrimitiveType.Cube, "Pulgar", hand, new Vector3(-side * 0.014f, -0.058f, 0.058f), new Vector3(0.03f, 0.064f, 0.03f), skin)
                    .transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            }
        }
    }
}
