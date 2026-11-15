using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Capturas de los NPC en la escena de verdad (solo existen en juego): el corro de Ash Block de día, civiles por la
    /// calle, la esquina de la tienda de licores de noche con el resto de locales cerrados, la cancha de Kessler, un
    /// enemigo que te reconoce en su barrio y una incursión. Se sacan a mano (con la escena generada).
    /// Salida: &lt;proyecto&gt;/ingame/npc/&lt;carpeta&gt;/&lt;vista&gt;.ppm (se pasan a PNG fuera), con la carpeta en la variable
    /// de entorno GTACHIRAQ_CAPTURA ("ultima" si no está).
    /// </summary>
    [Explicit("Capturas de NPC: se lanzan a mano")]
    [Category("Capturas")]
    public class NpcCapturePlayTests
    {
        const int Width = 1280, Height = 720;
        string outDir;
        Camera cam;
        NpcPopulation population;
        WeatherSystem weather;
        GameObject player;

        [UnityTest]
        public IEnumerator CaptureNpcs()
        {
            string folder = System.Environment.GetEnvironmentVariable("GTACHIRAQ_CAPTURA");
            if (string.IsNullOrEmpty(folder)) folder = "ultima";
            outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ingame", "npc", folder);
            Directory.CreateDirectory(outDir);

            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;
            player = GameObject.FindWithTag("Player");
            population = Object.FindAnyObjectByType<NpcPopulation>();
            weather = Object.FindAnyObjectByType<WeatherSystem>();
            Assert.IsNotNull(population, "La escena no tiene población de NPC (regenera la escena).");
            population.AutoSpawn = false;
            population.Raids = false;
            population.Clear(); // los que haya sacado en el primer fotograma
            cam = Camera.main;
            var follow = cam.GetComponent<ThirdPersonCamera>();
            if (follow) follow.enabled = false;
            if (weather) weather.ForceWeather(Weather.Nublado, 0f, instant: true);

            // 1. De día: el corro de Ash Block en la zona de reunión y gente pasando.
            yield return Hour(13f);
            MovePlayer(new Vector3(-10f, 0.2f, -71.5f));
            for (int i = 0; i < 4; i++) population.Spawn(Faction.AshBlock, NpcRole.Gang, i == 0, new Vector3(-26f + i * 0.8f, 0f, 19f), false, default);
            for (int i = 0; i < 3; i++) population.Spawn(Faction.None, NpcRole.Civilian, false, new Vector3(-34f + i * 3f, 0f, 6f), false, default);
            yield return Wait(9f);
            Shot("dia_reunion", new Vector3(-14f, 3.2f, 6f), new Vector3(-26f, 1f, 20f));

            // 2. De día por la calle sur: civiles yendo a sus cosas.
            population.Clear();
            for (int i = 0; i < 7; i++) population.Spawn(Faction.None, NpcRole.Civilian, false, new Vector3(-50f + i * 6f, 0.15f, -71.5f), false, default);
            yield return Wait(6f);
            Shot("dia_calle", new Vector3(-14f, 2.2f, -78f), new Vector3(-38f, 1f, -71f));

            // 3. La cancha de Kessler (con el jugador cerca: a más de 140 m la población los quita).
            population.Clear();
            MovePlayer(new Vector3(242f, 0.2f, 30f));
            for (int i = 0; i < 5; i++) population.Spawn(Faction.Kessler, NpcRole.Gang, i < 2, new Vector3(230f + i, 0f, 45f), false, default);
            yield return Wait(10f);
            Shot("kessler_cancha", new Vector3(240f, 3f, 34f), new Vector3(232f, 1f, 47f));

            // 4. De noche: solo la tienda de licores abierta, con los de la esquina; el resto, con la persiana bajada.
            population.Clear();
            MovePlayer(new Vector3(60f, 0.2f, 71.5f));
            yield return Hour(23f);
            for (int i = 0; i < 3; i++) population.Spawn(Faction.AshBlock, NpcRole.Gang, true, new Vector3(84f + i, 0.15f, 90.5f), false, default);
            yield return Wait(10f);
            Shot("noche_licores", new Vector3(70f, 3f, 78f), new Vector3(82f, 1.5f, 92f));
            Shot("noche_negocios", new Vector3(20f, 4f, 76f), new Vector3(40f, 2f, 93f));

            // 5. En el barrio enemigo: el de Dunmore te reconoce, se queda mirando y te encara.
            population.Clear();
            yield return Hour(15f);
            MovePlayer(new Vector3(40f, 0.2f, 194f)); // calle A', en pleno barrio de Dunmore
            var watcher = population.Spawn(Faction.Dunmore, NpcRole.Gang, true, new Vector3(52f, 0f, 194f), false, default, -90f);
            for (float t = 0f; t < 12f && watcher && watcher.Current != NpcBrain.State.Confront; t += 0.1f) yield return Wait(0.1f);
            Shot("dunmore_te_reconoce", new Vector3(35f, 2.4f, 191f), new Vector3(48f, 1.2f, 194f));

            // 6. Incursión en el bloque: los de Dunmore contra los de Ash Block.
            population.Clear();
            yield return Hour(22f);
            MovePlayer(new Vector3(-10f, 0.2f, -71.5f));
            for (int i = 0; i < 3; i++) population.Spawn(Faction.AshBlock, NpcRole.Gang, true, new Vector3(-26f + i, 0f, 19f), false, default);
            for (int i = 0; i < 3; i++) population.Spawn(Faction.Dunmore, NpcRole.Gang, true, new Vector3(-26f + i * 1.2f, 0f, 42f), true, new Vector3(-26f, 0f, 22f));
            yield return Wait(4f);
            Shot("incursion", new Vector3(-12f, 4f, 24f), new Vector3(-26f, 1f, 30f));
        }

        IEnumerator Hour(float hour)
        {
            if (weather) weather.SetHour(hour);
            yield return null;
            yield return null;
            foreach (var shop in Object.FindObjectsByType<ShopHours>(FindObjectsSortMode.None)) shop.Refresh();
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
            var log = new System.Text.StringBuilder();
            foreach (var b in population.Active)
            {
                if (!b) continue;
                var agent = b.GetComponent<UnityEngine.AI.NavMeshAgent>();
                log.AppendLine($"{b.name} {b.transform.position} {b.Current} {b.Activity} navmesh={(agent && agent.isOnNavMesh)} activo={b.gameObject.activeInHierarchy}");
            }
            File.WriteAllText(Path.Combine(outDir, view + ".txt"), log.ToString());
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
