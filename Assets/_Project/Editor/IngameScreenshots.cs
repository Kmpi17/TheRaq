using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Capturas de las distintas zonas del mundo, vistas como en el juego (cámara a la altura de la cámara del jugador,
    /// misma niebla y luces), guardadas en <c>ingame/</c> en la raíz del proyecto.
    /// Menú: <i>GTAChiraq &gt; Capturas in-game</i>. Desde la terminal (con el editor cerrado):
    /// <c>Unity.exe -batchmode -projectPath E:\gtachiraq -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -quit</c>.
    /// </summary>
    public static class IngameScreenshots
    {
        const int Width = 1920, Height = 1080;

        // (archivo, posición de la cámara, punto al que mira). Las vistas a pie van a ~2 m, como la cámara en tercera persona.
        static readonly (string name, Vector3 from, Vector3 to)[] Views =
        {
            ("01_inicio_jugador", new Vector3(-10f, 2.4f, -78f), new Vector3(-6f, 4f, -40f)),
            ("02_torres_eje_peatonal", new Vector3(-40f, 1.9f, -7f), new Vector3(0f, 8f, 2f)),
            ("03_torres_aparcamiento", new Vector3(-24f, 2.2f, -72f), new Vector3(0f, 6f, -30f)),
            ("04_adosadas_patio", new Vector3(-88f, 1.9f, 22f), new Vector3(-50f, 5f, 30f)),
            ("05_avenida_tren", new Vector3(104.5f, 1.9f, -45f), new Vector3(110f, 6f, 30f)),
            ("06_estacion", new Vector3(96f, 2.2f, -5f), new Vector3(112f, 9f, 40f)),
            ("07_barrio_viviendas_calle", new Vector3(124f, 2f, -3f), new Vector3(220f, 4f, 2f)),
            ("08_barrio_viviendas_torre", new Vector3(148f, 2f, 9f), new Vector3(156f, 12f, 62f)),
            ("09_casas_calle_a", new Vector3(124f, 2f, 121f), new Vector3(200f, 4f, 124f)),
            ("10_casas_callejon", new Vector3(124f, 2f, 149.5f), new Vector3(200f, 3f, 149.5f)),
            ("11_casas_calle_b", new Vector3(212f, 2f, 196f), new Vector3(212f, 3f, 110f)),
            ("12_negocios_calle_norte", new Vector3(-28f, 2.2f, 78f), new Vector3(40f, 3f, 95f)),
            ("13_negocios_barberia", new Vector3(22f, 1.9f, 84f), new Vector3(36f, 3f, 93f)),
            ("14_negocios_licores_esquina", new Vector3(108f, 2.2f, 80f), new Vector3(86f, 3f, 95f)),
            ("15_casas_oeste_calle_a", new Vector3(98f, 2f, 194f), new Vector3(20f, 4f, 192f)),
            ("16_aerea_manzana_central", new Vector3(-60f, 70f, -150f), new Vector3(10f, 0f, 0f)),
            ("17_aerea_barrios_norte", new Vector3(110f, 85f, 20f), new Vector3(110f, 0f, 150f)),
        };

        [MenuItem("GTAChiraq/Capturas in-game")]
        public static void CaptureMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Capture();
        }

        /// <summary>Para batchmode: regenera la escena con el generador actual y saca las capturas.</summary>
        public static void BuildAndCapture()
        {
            PrototypeSceneBuilder.Build();
            Capture();
        }

        /// <summary>Para batchmode: solo las capturas, con la escena tal como está guardada.</summary>
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Prototype.unity", OpenSceneMode.Single);
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ingame");
            Directory.CreateDirectory(dir);

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
                foreach (var (name, from, to) in Views)
                {
                    go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from, Vector3.up));
                    camera.Render();
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    readback.Apply();
                    File.WriteAllBytes(Path.Combine(dir, name + ".png"), readback.EncodeToPNG());
                }
                Debug.Log($"[GTAChiraq] {Views.Length} capturas guardadas en {dir}");
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
    }
}
