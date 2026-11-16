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
            ("detalle_vial", "01_semaforos_cruce_suroeste", new Vector3(-96f, 1.9f, -89f), new Vector3(-112f, 3f, -78f)),
            ("detalle_vial", "02_cruce_desde_arriba", new Vector3(-92f, 22f, -62f), new Vector3(-110f, 0f, -80f)),
            ("detalle_vial", "03_stop_calle_larkin", new Vector3(136f, 1.9f, 120f), new Vector3(117f, 2.2f, 126f)),
            ("detalle_vial", "04_postes_y_cables_calle_sur", new Vector3(-30f, 1.9f, -76f), new Vector3(-110f, 6f, -88f)),
            ("detalle_vial", "05_calle_judson_y_braxton", new Vector3(-257f, 1.9f, 108f), new Vector3(-258f, 2f, 82f)),
            ("detalle_vial", "06_cruce_avenida_norte", new Vector3(98f, 2f, 66f), new Vector3(114f, 3.5f, 84f)),
            ("autopista", "01_desde_puente_sur", new Vector3(-287.5f, 3.4f, -71.2f), new Vector3(-287.5f, -6f, 10f)),
            ("autopista", "02_trinchera_hacia_el_tunel", new Vector3(-281f, -5f, -40f), new Vector3(-283f, -4f, 60f)),
            ("autopista", "03_boca_del_tunel", new Vector3(-292f, -5f, 40f), new Vector3(-290f, -4f, 100f)),
            ("autopista", "04_desde_el_instituto", new Vector3(-262f, 2f, 20f), new Vector3(-290f, -3f, -20f)),
            ("instituto", "01_fachada", new Vector3(-200f, 2f, 76f), new Vector3(-214f, 6f, 52f)),
            ("instituto", "02_campo_desde_gradas", new Vector3(-197f, 5.6f, 2.5f), new Vector3(-197f, 0f, -40f)),
            ("instituto", "03_aparcamiento_autobuses", new Vector3(-118.5f, 2f, 2f), new Vector3(-140f, 2f, 25f)),
            ("instituto", "04_patio", new Vector3(-160f, 2f, 14f), new Vector3(-215f, 4f, 25f)),
            ("instituto", "05_aerea", new Vector3(-150f, 60f, -110f), new Vector3(-215f, 0f, 0f)),
            ("estudio", "01_bajada_escalera", new Vector3(-79f, 2.2f, 52f), new Vector3(-79f, -2.5f, 43f)),
            ("estudio", "02_sala_hacia_cabina", new Vector3(-76.5f, -1.5f, 39f), new Vector3(-65f, -2f, 47.5f)),
            ("estudio", "03_mesa_de_mezclas", new Vector3(-70.5f, -1.4f, 43f), new Vector3(-70.5f, -2.2f, 46.5f)),
            ("cocheras", "01_entrada_desde_el_inicio", new Vector3(-10f, 1.7f, -74f), new Vector3(4f, 2.5f, -112f)),
            ("cocheras", "02_playa_de_vias", new Vector3(95f, 30f, -100f), new Vector3(42.7f, 0.5f, -183.4f)),
            ("cocheras", "03_pasillo_contenedores", new Vector3(-86.85f, 1.7f, -110f), new Vector3(-86.85f, 2f, -180f)),
            ("cocheras", "04_nave_por_dentro", new Vector3(15.8f, 2.2f, -152.4f), new Vector3(19.6f, 2.5f, -115.1f)),
            ("cocheras", "05_rampa_del_elevado", new Vector3(40f, 2.5f, -270.6f), new Vector3(91.7f, 6f, -155.1f)),
            ("cocheras", "06_aerea", new Vector3(70f, 110f, -340f), new Vector3(10f, 0f, -185f)),
            ("cocheras", "07_escalera_de_agujas", new Vector3(-4.4f, 6f, -243.2f), new Vector3(37.5f, 0f, -202.7f)),
            ("cocheras", "08_valla_con_lona_desde_la_calle", new Vector3(-106f, 1.7f, -135f), new Vector3(-99f, 1.2f, -175f)),
            ("personaje", "01_de_frente", new Vector3(-10f, 1.5f, -69.6f), new Vector3(-10f, 1.1f, -71.5f)),
            ("personaje", "02_de_lado", new Vector3(-7.8f, 1.3f, -71.5f), new Vector3(-10f, 0.9f, -71.5f)),
            ("piso", "01_portal_T2", new Vector3(4f, 1.8f, -14f), new Vector3(0f, 1.4f, -5.5f)),
            ("piso", "02_vestibulo", new Vector3(-4.2f, 1.9f, -4.6f), new Vector3(3f, 1.2f, 1.4f)),
            ("piso", "03_salon", new Vector3(5.6f, 40.9f, 1.9f), new Vector3(12f, 40f, -4.5f)),
            ("piso", "04_armario_y_caja", new Vector3(11.5f, 40.8f, -3.6f), new Vector3(6f, 40f, 2.3f)),
            ("piso", "05_vista_ventana_sur", new Vector3(9.3f, 40.9f, -3.2f), new Vector3(2f, 28f, -70f)),
            ("piso", "06_vista_ventana_este", new Vector3(11f, 40.9f, -1f), new Vector3(60f, 30f, 2f)),
            ("piso", "07_ventanas_desde_fuera", new Vector3(30f, 25f, -45f), new Vector3(9f, 40f, -6f)),
            ("memorial", "01_desde_el_eje", new Vector3(-17.5f, 1.7f, -9.5f), new Vector3(-17.5f, 0.8f, -3.5f)),
            ("practica", "01_dianas_y_objetos", new Vector3(-12f, 2f, -75f), new Vector3(4f, 1f, -71.6f)),
            ("reunion", "01_barbacoa_y_canasta", new Vector3(-26f, 2.2f, 17f), new Vector3(-26f, 0.8f, 27f)),
            ("aereas", "01_manzana_central", new Vector3(-60f, 70f, -150f), new Vector3(10f, 0f, 0f)),
            ("aereas", "02_barrios_norte", new Vector3(110f, 85f, 20f), new Vector3(110f, 0f, 150f)),
            // Barrio norte (North.cs).
            ("norte", "01_aerea_desde_el_sur", new Vector3(10f, 110f, 250f), new Vector3(10f, 0f, 420f)),
            ("norte", "02_bulevar", new Vector3(-30f, 2f, 389f), new Vector3(60f, 4f, 396f)),
            ("norte", "03_centro_comercial", new Vector3(70f, 3f, 418f), new Vector3(10f, 3f, 455f)),
            ("norte", "04_centro_comercial_aereo", new Vector3(110f, 35f, 400f), new Vector3(25f, 0f, 450f)),
            ("norte", "05_edificio_en_cuna", new Vector3(80f, 2f, 318f), new Vector3(95f, 8f, 345f)),
            ("norte", "06_diagonal", new Vector3(55f, 2f, 362f), new Vector3(98f, 5f, 323f)),
            ("norte", "07_calle_sin_salida", new Vector3(160f, 2.2f, 475f), new Vector3(160f, 1f, 440f)),
            ("norte", "08_torre_suelta", new Vector3(215f, 2f, 305f), new Vector3(262f, 20f, 345f)),
            ("norte", "09_iglesia", new Vector3(212f, 2f, 430f), new Vector3(245f, 10f, 458f)),
            ("norte", "10_terraplen_y_paso", new Vector3(118f, 2f, 470f), new Vector3(110f, 4f, 510f)),
            ("norte", "11_parque", new Vector3(-98f, 2f, 412f), new Vector3(-60f, 2f, 450f)),
            ("norte", "12_bloque_con_patio", new Vector3(-143.5f, 2f, 392f), new Vector3(-143.5f, 6f, 360f)),
            ("norte", "13_horizonte", new Vector3(-40f, 45f, 380f), new Vector3(0f, 20f, 650f)),
            // Hora y tiempo (WeatherSystem.Preview, ver Weathers): la misma calle a varias horas y con lluvia o nieve.
            ("tiempo", "calle", new Vector3(-10f, 2.4f, -78f), new Vector3(30f, 5f, -60f)),
            ("tiempo", "avenida", new Vector3(104.5f, 1.9f, -45f), new Vector3(110f, 6f, 30f)),
        };

        // Para el elemento "tiempo": cada vista se captura con cada uno de estos (sufijo, hora, lluvia, nieve).
        static readonly (string suffix, float hour, float rain, float snow)[] Weathers =
        {
            ("1_mediodia", 12f, 0f, 0f),
            ("2_atardecer", 19.4f, 0f, 0f),
            ("3_noche", 23f, 0f, 0f),
            ("4_lluvia_dia", 12f, 1f, 0f),
            ("5_nieve_dia", 12f, 0f, 1f),
            ("6_lluvia_noche", 23f, 1f, 0f),
            ("7_nieve_noche", 23f, 0f, 1f),
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
            camera.clearFlags = main ? main.clearFlags : CameraClearFlags.SolidColor; // el cielo gris
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
                camera.Render(); // el primer fotograma tras abrir la escena sale quemado: se descarta
                foreach (var element in selected)
                {
                    string dir = NextVersionDir(element, change);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "cambio.txt"),
                        $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}\nCambio: {(string.IsNullOrWhiteSpace(change) ? "(sin descripción)" : change)}\n");
                    var weather = Object.FindAnyObjectByType<WeatherSystem>();
                    var states = element == "tiempo" && weather ? Weathers : new[] { ("", 12f, 0f, 0f) };
                    foreach (var (_, name, from, to) in Views.Where(v => v.element == element))
                    foreach (var (suffix, hour, rain, snow) in states)
                    {
                        go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from, Vector3.up));
                        if (weather) weather.Preview(hour, rain, snow, camera);
                        camera.Render();
                        RenderTexture.active = target;
                        readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        readback.Apply();
                        File.WriteAllBytes(Path.Combine(dir, name + (suffix.Length > 0 ? "_" + suffix : "") + ".png"), readback.EncodeToPNG());
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
