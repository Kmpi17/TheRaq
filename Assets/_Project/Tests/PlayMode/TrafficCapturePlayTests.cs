using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Capturas del tráfico y de lo que se mueve (solo existe en juego): un cruce con semáforo, la avenida bajo el tren
    /// con la parada del autobús, el autobús, una calle de barrio con STOP, el tren de mercancías en el paso a nivel con
    /// las barreras bajadas y el vapor de las alcantarillas de noche. Se sacan a mano (con la escena generada).
    /// Salida: &lt;proyecto&gt;/ingame/trafico/&lt;carpeta&gt;/&lt;vista&gt;.ppm (se pasan a PNG fuera), con la carpeta en
    /// la variable de entorno GTACHIRAQ_CAPTURA ("ultima" si no está).
    /// </summary>
    [Explicit("Capturas del tráfico: se lanzan a mano")]
    [Category("Capturas")]
    public class TrafficCapturePlayTests
    {
        const int Width = 1280, Height = 720;
        string outDir;
        Camera cam;
        WeatherSystem weather;
        GameObject player;

        [UnityTest]
        public IEnumerator CaptureTraffic()
        {
            string folder = System.Environment.GetEnvironmentVariable("GTACHIRAQ_CAPTURA");
            if (string.IsNullOrEmpty(folder)) folder = "ultima";
            outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ingame", "trafico", folder);
            Directory.CreateDirectory(outDir);

            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;
            player = GameObject.FindWithTag("Player");
            weather = Object.FindAnyObjectByType<WeatherSystem>();
            Assert.IsNotNull(Object.FindAnyObjectByType<TrafficPopulation>(), "La escena no tiene tráfico (regenera la escena).");
            cam = Camera.main;
            var follow = cam.GetComponent<ThirdPersonCamera>();
            if (follow) follow.enabled = false;
            if (weather) weather.ForceWeather(Weather.Nublado, 0f, instant: true);
            var movement = player.GetComponent<PlayerMovement>();
            if (movement) movement.enabled = false;
            if (weather) weather.SetHour(17.5f); // hora punta

            // 1. Cruce de S CALDER AVE con W ASHGROVE ST.
            MovePlayer(new Vector3(-96f, 0.3f, -66f));
            yield return Wait(30f);
            Shot("cruce_semaforo", new Vector3(-92f, 7f, -58f), new Vector3(-112f, 0f, -82f));

            // 2. La avenida bajo el tren elevado, con la parada del autobús.
            MovePlayer(new Vector3(120f, 0.3f, -30f));
            yield return Wait(25f);
            Shot("avenida_bajo_el_tren", new Vector3(121f, 2.6f, -50f), new Vector3(110f, 1.5f, -10f));

            // 3. El autobús, siguiéndolo un momento.
            TrafficCar bus = null;
            foreach (var c in Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None)) if (c.IsBus) bus = c;
            if (bus)
            {
                MovePlayer(bus.transform.position + bus.transform.right * 9f);
                yield return Wait(3f);
                var b = bus.transform;
                Shot("autobus", b.position + b.right * 7f + b.forward * 9f + Vector3.up * 2.5f, b.position + Vector3.up * 1.5f);
            }

            // 4. Calle de barrio: W LARKIN ST llegando a la avenida (STOP).
            MovePlayer(new Vector3(130f, 0.3f, 130f));
            yield return Wait(25f);
            Shot("barrio_stop", new Vector3(135f, 5f, 134f), new Vector3(112f, 0.5f, 120f));

            // 5. El tren de mercancías en el paso a nivel de S CALDER AVE, con las barreras bajadas y coches esperando.
            MovePlayer(new Vector3(-125f, 0.3f, -436f));
            LevelCrossing crossing = null;
            foreach (var c in Object.FindObjectsByType<LevelCrossing>(FindObjectsSortMode.None)) if (c.name == "PasoNivel_-110") crossing = c;
            var train = Object.FindAnyObjectByType<FreightTrain>();
            if (crossing && train)
            {
                Time.timeScale = 4f;
                float limit = Time.time + 400f;
                while (Time.time < limit && !(crossing.ArmLowered > 0.99f && Mathf.Abs(train.Center + 110f) < 60f)) yield return null;
                Time.timeScale = 1f;
                Shot("paso_nivel_tren", new Vector3(-124f, 4.5f, -428f), new Vector3(-108f, 2f, -460f));
            }

            // 6. Vapor de las alcantarillas, de noche.
            yield return Hour(21.5f);
            Transform steam = null;
            float best = float.MaxValue;
            var near = new Vector3(-10f, 0f, -80f);
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps.name != "VaporAlcantarilla") continue;
                float d = (ps.transform.position - near).sqrMagnitude;
                if (d < best) { best = d; steam = ps.transform; }
            }
            if (steam)
            {
                MovePlayer(steam.position + new Vector3(0f, 0.3f, 12f));
                yield return Wait(6f);
                Shot("vapor_noche", steam.position + new Vector3(-6f, 2.2f, 9f), steam.position + Vector3.up * 1.5f);
            }
        }

        IEnumerator Hour(float hour)
        {
            if (weather) weather.SetHour(hour);
            yield return null;
            yield return null;
        }

        void MovePlayer(Vector3 p)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = p;
            cc.enabled = true;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        void Shot(string view, Vector3 from, Vector3 lookAt)
        {
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(lookAt - from));
            var rt = RenderTexture.GetTemporary(Width, Height, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            cam.targetTexture = previous;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            var pixels = tex.GetPixels32();
            var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n");
            var data = new byte[header.Length + Width * Height * 3];
            header.CopyTo(data, 0);
            int o = header.Length;
            for (int y = Height - 1; y >= 0; y--)
                for (int x = 0; x < Width; x++)
                {
                    var c = pixels[y * Width + x];
                    data[o++] = c.r;
                    data[o++] = c.g;
                    data[o++] = c.b;
                }
            File.WriteAllBytes(Path.Combine(outDir, view + ".ppm"), data);
            Object.Destroy(tex);
        }
    }
}
