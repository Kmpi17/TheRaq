using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Vegetación de invierno del barrio de viviendas: árboles variados (pelados, con hoja verde, nevados o con la
    /// hoja seca de invierno), restos de nieve (montones en el bordillo, placas en el césped, nieve a la sombra de los
    /// edificios y junto a la valla) y maleza alta de zona abandonada (fondo del barrio, final de la calle, alrededor
    /// del coche abandonado, pie de las vallas y de las fachadas, grietas de la acera y un par de solares).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        enum TreeLook { Bare, Evergreen, Snowy, Withered }

        // Materiales de la vegetación; se preparan una vez por build en PrepareVegetation().
        static Material foliageGreen, foliageWithered, snowMat, snowPatch, tallGrass, weeds;
        static int snowDecalCount;

        static void PrepareVegetation()
        {
            foliageGreen = TexturedMat("CopaVerde", ProceduralTextures.Foliage("CopaVerde", new Color(0.11f, 0.19f, 0.1f), new Color(0.2f, 0.25f, 0.12f), 71));
            foliageWithered = TexturedMat("CopaInvierno", ProceduralTextures.Foliage("CopaInvierno", new Color(0.42f, 0.25f, 0.12f), new Color(0.55f, 0.43f, 0.22f), 72));
            snowMat = TexturedMat("NieveRestos", ProceduralTextures.Snow("NieveRestos", 73));
            snowMat.SetFloat("_Smoothness", 0.3f);
            snowPatch = TransparentMat("NieveCesped", ProceduralTextures.Grime("ManchaNieve", 74), new Color(0.88f, 0.9f, 0.94f, 0.95f), 0.3f);
            tallGrass = CutoutMat("HierbaAlta", ProceduralTextures.TallGrass("HierbaAlta", new Color(0.62f, 0.54f, 0.34f), new Color(0.38f, 0.4f, 0.22f), false, 75));
            weeds = CutoutMat("Maleza", ProceduralTextures.TallGrass("Maleza", new Color(0.4f, 0.32f, 0.2f), new Color(0.3f, 0.34f, 0.18f), true, 76));
            snowDecalCount = 0;
        }

        // ---------- Árboles ----------

        /// <summary>
        /// Árbol del barrio: tronco y ramas de <see cref="BareTree"/> y, según el aspecto que salga de
        /// <paramref name="looks"/>, una copa verde, una copa verde con nieve encima o una copa rala de hoja seca.
        /// </summary>
        static void DistrictTree(Transform parent, Vector3 basePosition, Material bark, System.Random rng, float scale, System.Random looks)
        {
            var root = BareTree(parent, basePosition, bark, rng, scale, out float height);
            double r = looks.NextDouble();
            var look = r < 0.35 ? TreeLook.Bare : r < 0.55 ? TreeLook.Evergreen : r < 0.75 ? TreeLook.Snowy : TreeLook.Withered;
            if (look == TreeLook.Bare) return;

            root.name = look switch
            {
                TreeLook.Evergreen => "ArbolHojas",
                TreeLook.Snowy => "ArbolNevado",
                _ => "ArbolHojaSeca",
            };
            bool withered = look == TreeLook.Withered;
            var material = withered ? foliageWithered : foliageGreen;
            int lumps = withered ? 5 : 6;
            var center = new Vector3(0f, height * 0.95f + 0.9f * scale, 0f);
            float spread = (withered ? 2f : 1.5f) * scale; // la hoja seca, más rala: se ven las ramas

            for (int i = 0; i < lumps; i++)
            {
                float s = Range(looks, 1.7f, 2.5f) * scale * (withered ? 0.7f : 1f);
                var p = center + new Vector3(Range(looks, -1f, 1f) * spread, Range(looks, -0.5f, 0.7f) * scale, Range(looks, -1f, 1f) * spread);
                Primitive(PrimitiveType.Sphere, "Copa", root, p, new Vector3(s, s * 0.8f, s), material, keepCollider: false);
                if (look == TreeLook.Snowy)
                    Primitive(PrimitiveType.Sphere, "NieveCopa", root, p + Vector3.up * (s * 0.28f), new Vector3(s * 0.85f, s * 0.32f, s * 0.85f), snowMat, keepCollider: false)
                        .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // Bajo el árbol nevado queda una placa de nieve.
            if (look == TreeLook.Snowy) SnowPatch(parent, basePosition, Range(looks, 0f, 360f), Vector2.one * Range(looks, 2.5f, 4f) * scale);
        }

        // ---------- Restos de nieve ----------

        static void BuildSnowRemnants(Transform parent, List<Rect> occupied)
        {
            var group = new GameObject("RestosNieve").transform;
            group.SetParent(parent, false);
            var rng = new System.Random(909);
            float halfRoad = DistrictRoadWidth / 2f;

            // Montones sucios en el bordillo, donde la quitanieves los dejó.
            for (int i = 0; i < 24; i++)
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                var p = new Vector3(Range(rng, DistrictMinX + 2f, DistrictMaxX - 3f), SidewalkTop, side * (halfRoad + Range(rng, 0.4f, 0.7f)));
                SnowMound(group, p, new Vector3(Range(rng, 1.4f, 3.4f), Range(rng, 0.25f, 0.45f), Range(rng, 0.7f, 1.1f)), Range(rng, -8f, 8f));
            }

            // Montón grande al final de la calle, empujado contra las pilonas.
            for (float z = -halfRoad + 1.5f; z <= halfRoad - 1.5f; z += 2.2f)
                SnowMound(group, new Vector3(DistrictMaxX - 2.6f + Range(rng, -0.4f, 0.4f), RoadTop, z),
                    new Vector3(Range(rng, 2f, 3f), Range(rng, 0.6f, 0.9f), Range(rng, 2.2f, 3f)), Range(rng, 0f, 360f));

            // Placas de nieve sueltas en el césped.
            for (int i = 0; i < 40; i++)
                if (RandomLawnPoint(rng, occupied, 1f, out var p))
                    SnowPatch(group, p, Range(rng, 0f, 360f), new Vector2(Range(rng, 1.5f, 4.5f), Range(rng, 1.2f, 3f)));

            // A la sombra, detrás de los edificios (la cara que no da a la calle), la nieve aguanta más.
            foreach (float side in DistrictSides)
            {
                foreach (float x in DistrictWalkupsX) SnowBehind(group, rng, x, side * DistrictWalkupZ, WalkupSize);
                foreach (float x in DistrictTowersX) SnowBehind(group, rng, x, side * DistrictTowerZ, TowerSize);
            }

            // Ventisqueros junto a la valla, del lado del césped.
            foreach (float side in DistrictSides)
                for (int i = 0; i < 7; i++)
                {
                    float x = Range(rng, DistrictMinX + 2f, DistrictMaxX - 2f);
                    if (NearPathGap(x, 2.5f)) continue;
                    SnowMound(group, new Vector3(x, LawnTop, side * (DistrictLawnInner + 0.8f)), new Vector3(Range(rng, 1.5f, 3f), Range(rng, 0.2f, 0.35f), 0.9f), 0f);
                }
        }

        static void SnowBehind(Transform parent, System.Random rng, float x, float z, Vector3 size)
        {
            float away = Mathf.Sign(z);
            float back = z + away * (size.z / 2f);
            for (float dx = -size.x / 2f + 1.5f; dx < size.x / 2f - 1f; dx += Range(rng, 2.5f, 4.5f))
            {
                SnowPatch(parent, new Vector3(x + dx, LawnTop, back + away * Range(rng, 1.2f, 2.4f)), Range(rng, 0f, 360f), new Vector2(Range(rng, 2.5f, 4f), Range(rng, 1.6f, 2.6f)));
                if (rng.NextDouble() < 0.6)
                    SnowMound(parent, new Vector3(x + dx, LawnTop, back + away * 0.5f), new Vector3(Range(rng, 1.5f, 2.8f), Range(rng, 0.25f, 0.5f), 0.9f), 0f);
            }
        }

        /// <summary>Montón de nieve: media esfera aplastada sobre el suelo, sin colisión.</summary>
        static void SnowMound(Transform parent, Vector3 ground, Vector3 size, float yaw)
        {
            var go = Primitive(PrimitiveType.Sphere, "MontonNieve", parent, ground, new Vector3(size.x, size.y * 2f, size.z), snowMat, keepCollider: false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Placa de nieve fina sobre el césped (calcomanía transparente, por encima de las manchas de barro).</summary>
        static void SnowPatch(Transform parent, Vector3 ground, float yaw, Vector2 size)
        {
            float y = LawnTop + 0.009f + (snowDecalCount++ % 60) * 0.0001f;
            Decal(parent, new Vector3(ground.x, y, ground.z), yaw, size, snowPatch);
        }

        /// <summary>¿Está x cerca del hueco de la valla por donde entra el camino de algún edificio?</summary>
        static bool NearPathGap(float x, float margin)
        {
            foreach (float gx in DistrictWalkupsX) if (Mathf.Abs(x - gx) < margin) return true;
            foreach (float gx in DistrictTowersX) if (Mathf.Abs(x - gx) < margin) return true;
            return false;
        }

        // ---------- Maleza de zona abandonada ----------

        static void BuildOvergrowth(Transform parent, List<Rect> occupied)
        {
            var group = new GameObject("Maleza").transform;
            group.SetParent(parent, false);
            var rng = new System.Random(808);

            // Fondo del barrio, detrás de las torres: franja que nadie siega.
            foreach (float side in DistrictSides)
                for (float x = DistrictMinX + 1f; x < DistrictMaxX - 1f; x += Range(rng, 1.2f, 2f))
                {
                    if (rng.NextDouble() < 0.2) continue;
                    float z = side * Range(rng, DistrictHalfZ - 5.5f, DistrictHalfZ - 0.6f);
                    if (IsFree(occupied, x, z)) Clump(group, rng, new Vector3(x, LawnTop, z), 0.7f, 1.4f);
                }

            // Final de la calle, más allá de la cancha y la plaza.
            foreach (float side in DistrictSides)
                for (int i = 0; i < 22; i++)
                {
                    float x = Range(rng, DistrictMaxX - 7f, DistrictMaxX - 0.6f);
                    float z = side * Range(rng, DistrictLawnInner + 0.8f, DistrictHalfZ - 1f);
                    if (IsFree(occupied, x, z)) Clump(group, rng, new Vector3(x, LawnTop, z), 0.6f, 1.3f);
                }

            // Alrededor del coche abandonado (sin meterse en la carrocería).
            for (int i = 0; i < 24; i++)
            {
                float angle = Range(rng, 0f, Mathf.PI * 2f), radius = Range(rng, 2.8f, 6f);
                Clump(group, rng, WreckPosition + new Vector3(Mathf.Cos(angle) * radius, LawnTop, Mathf.Sin(angle) * radius), 0.5f, 1.2f);
            }

            // Un par de solares descuidados en el césped.
            for (int lot = 0; lot < 3; lot++)
            {
                if (!RandomLawnPoint(rng, occupied, 12f, out var c)) continue;
                for (int i = 0; i < 20; i++)
                {
                    float angle = Range(rng, 0f, Mathf.PI * 2f), radius = Mathf.Sqrt((float)rng.NextDouble()) * 5f;
                    var p = c + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (IsFree(occupied, p.x, p.z)) Clump(group, rng, p, 0.5f, 1.3f);
                }
                occupied.Add(RectAround(c.x, c.z, 10f, 10f));
            }

            // Pie de la valla, del lado del césped.
            foreach (float side in DistrictSides)
                for (float x = DistrictMinX + 1f; x < DistrictMaxX - 1f; x += 1.8f)
                    if (!NearPathGap(x, 2.5f) && rng.NextDouble() < 0.35)
                        Clump(group, rng, new Vector3(x + Range(rng, -0.5f, 0.5f), LawnTop, side * (DistrictLawnInner + Range(rng, 0.55f, 0.95f))), 0.35f, 0.8f);

            // Pie de las fachadas traseras de los edificios.
            foreach (float side in DistrictSides)
            {
                foreach (float x in DistrictWalkupsX) WeedsAlongBack(group, rng, x, side * DistrictWalkupZ, WalkupSize);
                foreach (float x in DistrictTowersX) WeedsAlongBack(group, rng, x, side * DistrictTowerZ, TowerSize);
            }

            // Hierbajos en las grietas de la acera, junto a la valla.
            for (int i = 0; i < 28; i++)
            {
                float side = rng.Next(2) == 0 ? 1f : -1f;
                float x = Range(rng, DistrictMinX + 1f, DistrictMaxX - 1f);
                if (NearPathGap(x, 2f)) continue;
                Clump(group, rng, new Vector3(x, SidewalkTop, side * (DistrictLawnInner - Range(rng, 0.15f, 0.35f))), 0.2f, 0.45f);
            }
        }

        static void WeedsAlongBack(Transform parent, System.Random rng, float x, float z, Vector3 size)
        {
            float away = Mathf.Sign(z);
            float back = z + away * (size.z / 2f + 0.45f);
            for (float dx = -size.x / 2f + 0.6f; dx < size.x / 2f - 0.6f; dx += 1.6f)
                if (rng.NextDouble() < 0.45)
                    Clump(parent, rng, new Vector3(x + dx + Range(rng, -0.4f, 0.4f), LawnTop, back + away * Range(rng, 0f, 0.5f)), 0.4f, 0.9f);
        }

        /// <summary>
        /// Matojo: tres quads cruzados (a 60°) con la textura de hierba alta o de maleza con espigas.
        /// Sin colisión: se atraviesa andando o con el coche.
        /// </summary>
        static void Clump(Transform parent, System.Random rng, Vector3 ground, float minHeight, float maxHeight)
        {
            float height = Range(rng, minHeight, maxHeight);
            float width = height * Range(rng, 0.9f, 1.4f);
            var material = rng.NextDouble() < 0.6 ? tallGrass : weeds;
            float yaw = Range(rng, 0f, 60f);
            for (int i = 0; i < 3; i++)
            {
                var quad = Primitive(PrimitiveType.Quad, "Hierba", parent, ground + Vector3.up * (height / 2f - 0.02f), new Vector3(width, height, 1f), material, keepCollider: false);
                quad.transform.localRotation = Quaternion.Euler(0f, yaw + i * 60f, 0f);
                quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
