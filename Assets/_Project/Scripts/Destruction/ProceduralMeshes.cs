using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Mallas hechas en código. La caja con la textura repetida por metros la usan el generador de la escena
    /// (<c>PrototypeSceneBuilder.TexturedBox</c>) y, en juego, las vallas rotas (<see cref="BreakableFence"/>) para
    /// rehacer los trozos que quedan en pie con la misma textura.
    /// </summary>
    public static class ProceduralMeshes
    {
        /// <summary>
        /// Caja de <paramref name="size"/> centrada en el origen con la textura repetida cada <paramref name="tile"/> metros
        /// (un número entero de veces por cara). Con <paramref name="sidesOnly"/>, sin tapas arriba y abajo.
        /// </summary>
        public static Mesh Box(string name, Vector3 size, Vector2 tile, bool sidesOnly)
        {
            var faces = new List<(Vector3 normal, Vector3 up)>
            {
                (Vector3.forward, Vector3.up), (Vector3.back, Vector3.up),
                (Vector3.right, Vector3.up), (Vector3.left, Vector3.up),
            };
            if (!sidesOnly)
            {
                faces.Add((Vector3.up, Vector3.forward));
                faces.Add((Vector3.down, Vector3.back));
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3 half = size / 2f;
            foreach (var (normal, up) in faces)
            {
                // "right" es la derecha de quien mira la cara desde fuera, para que la textura no salga en espejo.
                Vector3 right = Vector3.Cross(up, -normal);
                float hn = Mathf.Abs(Vector3.Dot(half, normal));
                float hr = Mathf.Abs(Vector3.Dot(half, right));
                float hu = Mathf.Abs(Vector3.Dot(half, up));
                float tilesU = Mathf.Max(1f, Mathf.Round(2f * hr / tile.x));
                float tilesV = Mathf.Max(1f, Mathf.Round(2f * hu / tile.y));

                int start = vertices.Count;
                for (int i = 0; i < 4; i++) // abajo-izq, abajo-der, arriba-der, arriba-izq
                {
                    float su = i == 1 || i == 2 ? 1f : 0f;
                    float sv = i >= 2 ? 1f : 0f;
                    vertices.Add(normal * hn + right * ((su * 2f - 1f) * hr) + up * ((sv * 2f - 1f) * hu));
                    normals.Add(normal);
                    uvs.Add(new Vector2(su * tilesU, sv * tilesV));
                }
                // Sentido horario visto desde fuera (cara frontal en Unity).
                triangles.AddRange(new[] { start, start + 3, start + 2, start, start + 2, start + 1 });
            }

            var mesh = new Mesh { name = $"{name}_{size.x:0.#}x{size.y:0.#}x{size.z:0.#}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static Mesh cylinder;

        /// <summary>El cilindro de Unity (2 m de alto y 1 de diámetro), para rehacer postes y barandillas.</summary>
        public static Mesh Cylinder
        {
            get
            {
                if (cylinder) return cylinder;
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder = temp.GetComponent<MeshFilter>().sharedMesh;
                if (Application.isPlaying) Object.Destroy(temp);
                else Object.DestroyImmediate(temp);
                return cylinder;
            }
        }
    }
}
