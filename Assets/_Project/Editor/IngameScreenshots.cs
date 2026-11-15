using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Capturas de las distintas zonas del mundo, vistas como en el juego (cámara a la altura de la cámara del jugador,
    /// misma niebla y luces). Se agrupan por elemento y por versión: cada vez que cambia un elemento se capturan sus
    /// vistas en una carpeta nueva <c>ingame/&lt;elemento&gt;/vNN_&lt;etiqueta&gt;/</c>, junto a las versiones anteriores,
    /// para poder compararlas. Cada versión lleva un <c>cambio.txt</c> con la fecha y lo que cambió.
    ///
    /// Menú: <i>GTAChiraq &gt; Capturas in-game (todas)</i>. Desde la terminal (con el editor cerrado):
    /// <c>Unity.exe -batchmode -projectPath E:\gtachiraq -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture
    /// -capturar estacion,negocios -cambio "escaleras transitables" -quit</c>. Sin <c>-capturar</c> se capturan todos.
    /// </summary>
    public static class IngameScreenshots
    {
        const int Width = 1920, Height = 1080;
        static string RootDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "ingame");

        // (elemento, archivo, posición de la cámara, punto al que mira). Las vistas a pie van a ~2 m, como la cámara en
        // tercera persona. El elemento es la carpeta de ingame/; para un elemento nuevo basta con usar otro nombre.
        static readonly (string element, string name, Vector3 from, Vector3 to)[] Views =
        {
            ("manzana_central", "01_inicio_jugador", new Vector3(-10f, 2.4f, -78f), new Vector3(-6f, 4f, -40f)),
            ("manzana_central", "02_torres_eje_peatonal", new Vector3(-40f, 1.9f, -7f), new Vector3(0f, 8f, 2f)),
            ("manzana_central", "03_torres_aparcamiento", new Vector3(-24f, 2.2f, -72f), new Vector3(0f, 6f, -30f)),
            ("manzana_central", "04_adosadas_patio", new Vector3(-88f, 1.9f, 22f), new Vector3(-50f, 5f, 30f)),
            ("avenida_tren", "01_avenida_bajo_el_tren", new Vector3(104.5f, 1.9f, -45f), new Vector3(110f, 6f, 30f)),
            ("estacion", "01_estacion_desde_la_acera", new Vector3(96f, 2.2f, -5f), new Vector3(112f, 9f, 40f)),
            ("estacion", "02_escalera", new Vector3(102.2f, 1.9f, 31f), new Vector3(100.4f, 5.5f, 15f)),
            ("estacion", "03_anden", new Vector3(103.2f, 11.4f, 62f), new Vector3(103.5f, 10.2f, 8f)),
            ("barrio_viviendas", "01_calle", new Vector3(124f, 2f, -3f), new Vector3(220f, 4f, 2f)),
            ("barrio_viviendas", "02_torre", new Vector3(148f, 2f, 9f), new Vector3(156f, 12f, 62f)),
            ("barrio_casas", "01_calle_a", new Vector3(124f, 2f, 121f), new Vector3(200f, 4f, 124f)),
            ("barrio_casas", "02_callejon", new Vector3(124f, 2f, 149.5f), new Vector3(200f, 3f, 149.5f)),
            ("barrio_casas", "04_porches_traseros", new Vector3(244.5f, 2f, 99f), new Vector3(228f, 5f, 112f)),
            ("barrio_casas", "03_calle_b", new Vector3(212f, 2f, 196f), new Vector3(212f, 3f, 110f)),
            ("negocios", "01_calle_norte", new Vector3(-28f, 2.2f, 78f), new Vector3(40f, 3f, 95f)),
            ("negocios", "02_barberia", new Vector3(22f, 1.9f, 84f), new Vector3(36f, 3f, 93f)),
            ("negocios", "03_licores_esquina", new Vector3(108f, 2.2f, 80f), new Vector3(86f, 3f, 95f)),
            ("barrio_casas_oeste", "01_calle_a", new Vector3(98f, 2f, 194f), new Vector3(20f, 4f, 192f)),
            ("barrio_casas_oeste_ampliacion", "01_calle_d_solar_esquina", new Vector3(-101.8f, 2f, 130f), new Vector3(-60f, 4f, 145f)),
            ("barrio_casas_oeste_ampliacion", "02_solar_calle_e", new Vector3(-72f, 2f, 187f), new Vector3(-62f, 3f, 168f)),
            ("barrio_casas_oeste_ampliacion", "03_calle_norte", new Vector3(-100f, 2.2f, 84f), new Vector3(-40f, 4f, 96f)),
            ("barrio_casas_norte", "01_calle_fe", new Vector3(119f, 2f, 253f), new Vector3(200f, 4f, 252f)),
            ("barrio_casas_norte", "02_calle_b_g", new Vector3(212f, 2f, 160f), new Vector3(212f, 3f, 290f)),
            ("barrio_casas_norte", "03_calle_f", new Vector3(-98f, 2f, 253f), new Vector3(40f, 4f, 253f)),
            ("barrio_casas_norte", "04_calle_b_prima", new Vector3(8f, 2f, 130f), new Vector3(8f, 3f, 250f)),
            ("barrio_casas_norte", "05_calle_h", new Vector3(-98f, 2f, 293f), new Vector3(100f, 4f, 293f)),
            ("oeste_lejano", "01_gasolinera", new Vector3(-108f, 3f, 84f), new Vector3(-140f, 2f, 108f)),
            ("oeste_lejano", "02_negocios", new Vector3(-150f, 2f, 84f), new Vector3(-235f, 2.5f, 95f)),
            ("oeste_lejano", "03_cancha", new Vector3(-215f, 4f, 150f), new Vector3(-238f, 1f, 158f)),
            ("oeste_lejano", "04_descampado", new Vector3(-200f, 5f, 186f), new Vector3(-230f, 0f, 220f)),
            ("oeste_lejano", "05_calle_d", new Vector3(-112f, 2f, 131.5f), new Vector3(-250f, 3f, 131.5f)),
            ("oeste_lejano", "06_calle_j", new Vector3(-257f, 2f, 95f), new Vector3(-257f, 3f, 290f)),
            ("calles_grandes", "01_cruce_suroeste", new Vector3(-95f, 3.5f, -88.5f), new Vector3(-112f, 0f, -78f)),
            ("calles_grandes", "02_calle_sur", new Vector3(-60f, 1.6f, -76f), new Vector3(40f, 1f, -82f)),
            ("calles_grandes", "03_cruce_noroeste_alto", new Vector3(-124f, 18f, 66f), new Vector3(-110f, 0f, 80f)),
            ("calles_grandes", "04_calle_oeste", new Vector3(-113f, 1.6f, -40f), new Vector3(-108f, 1f, 60f)),
            ("aereas", "01_manzana_central", new Vector3(-60f, 70f, -150f), new Vector3(10f, 0f, 0f)),
            ("aereas", "02_barrios_norte", new Vector3(110f, 85f, 20f), new Vector3(110f, 0f, 150f)),
        };

        public static IEnumerable<string> Elements => Views.Select(v => v.element).Distinct();

        [MenuItem("GTAChiraq/Capturas in-game (todas)")]
        public static void CaptureMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Capture(null, "desde el editor");
        }

        /// <summary>Batchmode: regenera la escena con el generador actual y captura (lee -capturar y -cambio).</summary>
        public static void BuildAndCapture()
        {
            PrototypeSceneBuilder.Build();
            CaptureFromCommandLine();
        }

        /// <summary>Batchmode: solo captura, con la escena tal como está guardada (lee -capturar y -cambio).</summary>
        public static void CaptureFromCommandLine()
        {
            string elements = Argument("-capturar"), change = Argument("-cambio");
            Capture(string.IsNullOrEmpty(elements) ? null : elements.Split(',').Select(e => e.Trim()).ToArray(), change ?? "");
        }

        /// <summary>
        /// Captura las vistas de <paramref name="elements"/> (null: todos) en una versión nueva de cada uno.
        /// <paramref name="change"/> describe el cambio: va en el nombre de la carpeta y en cambio.txt.
        /// </summary>
        public static void Capture(string[] elements, string change)
        {
            var selected = elements == null ? Elements.ToList() : elements.ToList();
            foreach (var e in selected)
                if (!Elements.Contains(e)) throw new ArgumentException($"Elemento desconocido '{e}'. Hay: {string.Join(", ", Elements)}");

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Prototype.unity", OpenSceneMode.Single);

            var go = new GameObject("CamaraCapturas") { hideFlags = HideFlags.HideAndDontSave };
            var camera = go.AddComponent<Camera>();
            var main = Camera.main;
            if (main) camera.CopyFrom(main);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.enabled = false;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            try
            {
                foreach (var element in selected)
                {
                    string dir = NextVersionDir(element, change);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "cambio.txt"),
                        $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}\nCambio: {(string.IsNullOrWhiteSpace(change) ? "(sin descripción)" : change)}\n");
                    foreach (var (_, name, from, to) in Views.Where(v => v.element == element))
                    {
                        go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from, Vector3.up));
                        camera.Render();
                        RenderTexture.active = target;
                        readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        readback.Apply();
                        File.WriteAllBytes(Path.Combine(dir, name + ".png"), readback.EncodeToPNG());
                    }
                    Debug.Log($"[GTAChiraq] Capturas de '{element}' en {dir}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>ingame/&lt;elemento&gt;/vNN_&lt;etiqueta&gt;, con NN = la última versión de ese elemento + 1.</summary>
        static string NextVersionDir(string element, string change)
        {
            string elementDir = Path.Combine(RootDir, element);
            int last = 0;
            if (Directory.Exists(elementDir))
                foreach (var d in Directory.GetDirectories(elementDir))
                {
                    var m = Regex.Match(Path.GetFileName(d), @"^v(\d+)");
                    if (m.Success) last = Math.Max(last, int.Parse(m.Groups[1].Value));
                }
            string slug = Regex.Replace((change ?? "").ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD), @"\p{Mn}", "");
            slug = Regex.Replace(slug, @"[^a-z0-9]+", "_").Trim('_');
            if (slug.Length > 40) slug = slug.Substring(0, 40).TrimEnd('_');
            return Path.Combine(elementDir, $"v{last + 1:00}" + (slug.Length > 0 ? "_" + slug : ""));
        }

        static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
