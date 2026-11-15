using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Grabador de animaciones del personaje (no es un test de verdad: es explícito y no comprueba nada). Carga la
    /// escena generada, monta un plató despejado en el aire (suelo, muro alto, muro bajo y una valla), maneja al jugador
    /// con teclado y ratón simulados (quieto, andar apuntando, correr, esprintar, agachado quieto, andando y apuntando, apuntar y moverse de lado y hacia
    /// atrás, disparar, puñetazos, cubrirse en muro alto y bajo moviéndose de lado y asomándose, y escalar) y guarda
    /// fotogramas de cada acción desde un lado, desde 3/4 por delante y con la cámara del juego.
    /// Salida: &lt;proyecto&gt;/ingame/animaciones/&lt;carpeta&gt;/&lt;acción&gt;_&lt;vista&gt;_NN.ppm (se pasan a PNG fuera), con la carpeta en la
    /// variable de entorno GTACHIRAQ_CAPTURA (por defecto "ultima"). Se lanza con
    /// <c>-runTests -testPlatform PlayMode -testFilter GTAChiraq.Tests.AnimationCapturePlayTests</c>.
    /// </summary>
    [Explicit("Grabador de animaciones: se lanza a mano")]
    [Category("Capturas")]
    public class AnimationCapturePlayTests : InputTestFixture
    {
        const int Size = 360;
        static readonly Vector3 Stage = new(0f, 400f, 0f);

        Keyboard keyboard;
        Mouse mouse;
        GameObject player;
        CharacterController controller;
        PlayerInventory inventory;
        ThirdPersonCamera followCamera;
        Camera shotCamera;
        Texture2D readback;
        string outDir;

        public override void Setup()
        {
            base.Setup();
            GameInput.Reset();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.captureFramerate = 0;
            GameInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator RecordAnimations()
        {
            string folder = System.Environment.GetEnvironmentVariable("GTACHIRAQ_CAPTURA");
            if (string.IsNullOrEmpty(folder)) folder = "ultima";
            outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ingame", "animaciones", folder);
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 60; // pasos de tiempo fijos: misma animación en cada grabación

            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;
            player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "La escena no tiene jugador.");
            controller = player.GetComponent<CharacterController>();
            inventory = player.GetComponent<PlayerInventory>();
            followCamera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            BuildStage();
            shotCamera = new GameObject("CamaraGrabador").AddComponent<Camera>();
            shotCamera.enabled = false;
            shotCamera.fieldOfView = 38f;
            shotCamera.nearClipPlane = 0.05f;
            shotCamera.clearFlags = CameraClearFlags.SolidColor;
            shotCamera.backgroundColor = new Color(0.55f, 0.57f, 0.6f);
            readback = new Texture2D(Size, Size, TextureFormat.RGB24, false);

            // Quieto, andar apuntando (andar es la velocidad al apuntar), correr y esprintar, sin arma.
            Equip(null);
            yield return Place(new Vector3(-4f, 0f, -6f));
            yield return Record("quieto", 6, 0.25f);
            yield return Place(new Vector3(-4f, 0f, -12f));
            yield return Hold(new ButtonControl[] { keyboard.wKey }, 0.8f);
            yield return Record("correr", 10, 0.05f);
            Up(keyboard.wKey);
            yield return Place(new Vector3(-4f, 0f, -14f));
            yield return Hold(new ButtonControl[] { keyboard.wKey, keyboard.leftShiftKey }, 1.2f);
            yield return Record("esprintar", 10, 0.04f);
            Up(keyboard.leftShiftKey);
            yield return null;
            Up(keyboard.wKey);
            yield return Wait(0.6f);
            yield return Record("frenar", 4, 0.1f);

            // Quieto un buen rato: carga el peso en una pierna, lo cambia y mira alrededor.
            yield return Place(new Vector3(-4f, 0f, -6f));
            yield return Record("quieto_largo", 12, 0.7f);
            // Corriendo en curva (la cámara gira y el personaje la sigue): se inclina hacia dentro.
            yield return Place(new Vector3(6f, 0f, -12f));
            var spinner = new GameObject("GiraCamara").AddComponent<CameraSpinner>();
            spinner.Camera = followCamera;
            spinner.Rate = 110f;
            Down(keyboard.wKey);
            yield return Wait(0.8f);
            yield return Record("correr_curva", 8, 0.08f);
            Up(keyboard.wKey);
            Object.Destroy(spinner.gameObject);
            yield return Wait(0.6f);

            // Agachado (C): quieto, andando hacia delante y de lado, y apuntando agachado.
            yield return Place(new Vector3(-4f, 0f, -6f));
            yield return TapCrouch();
            yield return Wait(0.5f);
            yield return Record("agachado", 3, 0.25f);
            foreach (var (name, key) in new[] { ("agachado_andar", keyboard.wKey), ("agachado_lado", keyboard.dKey) })
            {
                yield return Place(new Vector3(-4f, 0f, -12f));
                Down(key);
                yield return Wait(0.6f);
                yield return Record(name, 10, 0.06f);
                Up(key);
            }
            Equip("pistola");
            yield return Place(new Vector3(-4f, 0f, -6f));
            Down(mouse.rightButton);
            yield return Wait(0.6f);
            yield return Record("agachado_apuntar", 3, 0.2f);
            Up(mouse.rightButton);
            Equip(null);
            yield return TapCrouch();
            yield return Wait(0.4f);

            // Puñetazos (tres seguidos para ver el combo).
            yield return Place(new Vector3(-4f, 0f, -6f));
            for (int i = 0; i < 3; i++)
            {
                Down(mouse.leftButton);
                yield return Wait(0.05f);
                Up(mouse.leftButton);
                yield return Record("punetazo" + (i + 1), 4, 0.06f);
            }

            // Pistola: en reposo, apuntando, apuntando y andando hacia delante, de lado y hacia atrás, y disparando.
            Equip("pistola");
            yield return Place(new Vector3(-4f, 0f, -6f));
            yield return Record("pistola_reposo", 2, 0.2f);
            Down(mouse.rightButton);
            yield return Wait(0.5f);
            yield return Record("apuntar", 3, 0.2f);
            foreach (var (name, key) in new[] { ("apuntar_adelante", keyboard.wKey), ("apuntar_derecha", keyboard.dKey), ("apuntar_atras", keyboard.sKey) })
            {
                yield return Place(new Vector3(-4f, 0f, -8f), keepAim: true);
                Down(key);
                yield return Wait(0.6f);
                yield return Record(name, 8, 0.06f);
                Up(key);
            }
            yield return Place(new Vector3(-4f, 0f, -6f), keepAim: true);
            Down(mouse.leftButton);
            yield return Wait(0.03f);
            Up(mouse.leftButton);
            yield return Record("disparar", 4, 0.05f);
            Up(mouse.rightButton);
            yield return Wait(0.8f);
            yield return Place(new Vector3(-4f, 0f, -8f));
            Down(keyboard.wKey);
            yield return Wait(0.8f);
            yield return Record("correr_pistola", 8, 0.05f);
            Up(keyboard.wKey);
            Equip(null);

            // Cobertura alta: entrar, moverse de lado, quedarse en la esquina y asomarse apuntando.
            yield return Place(new Vector3(0f, 0f, 4.6f));
            yield return TapCover();
            yield return Wait(0.5f);
            yield return Record("cobertura_alta", 3, 0.2f);
            Down(keyboard.dKey);
            yield return Wait(0.3f);
            yield return Record("cobertura_alta_lado", 8, 0.06f);
            yield return Wait(1.5f); // hasta la esquina
            Up(keyboard.dKey);
            yield return Record("cobertura_alta_esquina", 2, 0.2f);
            Equip("pistola");
            Down(mouse.rightButton);
            yield return Wait(0.6f);
            yield return Record("cobertura_alta_asomarse", 3, 0.2f);
            Up(mouse.rightButton);
            Equip(null);
            yield return Wait(0.3f);
            yield return TapCover();

            // Cobertura baja: agachado, de lado y asomándose por encima.
            yield return Place(new Vector3(-8f, 0f, 4.6f));
            yield return TapCover();
            yield return Wait(0.5f);
            yield return Record("cobertura_baja", 3, 0.2f);
            Down(keyboard.aKey);
            yield return Wait(0.3f);
            yield return Record("cobertura_baja_lado", 8, 0.06f);
            Up(keyboard.aKey);
            Equip("pistola");
            Down(mouse.rightButton);
            yield return Wait(0.6f);
            yield return Record("cobertura_baja_asomarse", 3, 0.2f);
            Up(mouse.rightButton);
            Equip(null);
            yield return TapCover();

            // Escalar la valla y el muro alto.
            yield return Place(new Vector3(8f, 0f, 5f));
            Down(keyboard.spaceKey);
            yield return Wait(0.05f);
            Up(keyboard.spaceKey);
            yield return Record("saltar_valla", 12, 0.07f);
            yield return Place(new Vector3(0f, 0f, 5f));
            Down(keyboard.spaceKey);
            yield return Wait(0.05f);
            Up(keyboard.spaceKey);
            yield return Record("escalar_muro", 14, 0.09f);

            Object.Destroy(readback);
            Debug.Log("Grabador de animaciones: fotogramas en " + outDir);
        }

        // ---------- Plató ----------

        void BuildStage()
        {
            GameObject Block(string name, Vector3 center, Vector3 size, Color color)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = Stage + center;
                go.transform.localScale = size;
                var r = go.GetComponent<Renderer>();
                r.material = new Material(r.sharedMaterial) { color = color };
                return go;
            }
            Block("PlatoSuelo", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), new Color(0.42f, 0.42f, 0.4f));
            Block("PlatoMuroAlto", new Vector3(0f, 1.15f, 6f), new Vector3(4f, 2.3f, 0.4f), new Color(0.55f, 0.32f, 0.24f));
            Block("PlatoMuroBajo", new Vector3(-8f, 0.5f, 6f), new Vector3(4f, 1f, 0.4f), new Color(0.6f, 0.6f, 0.58f));
            Block("PlatoValla", new Vector3(8f, 0.6f, 6f), new Vector3(3f, 1.2f, 0.25f), new Color(0.45f, 0.35f, 0.25f));
            var light = new GameObject("PlatoLuz").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        /// <summary>
        /// Pulsa o suelta avanzando antes el reloj de la entrada simulada: el sistema de entrada descarta los eventos con
        /// una marca de tiempo que no avanza, y sin esto las teclas se quedan pulsadas.
        /// </summary>
        void Down(ButtonControl button)
        {
            currentTime += 0.01;
            Press(button);
        }

        void Up(ButtonControl button)
        {
            if (!button.isPressed) return;
            currentTime += 0.01;
            Release(button);
        }

        /// <summary>Pulsa Q (cubrirse) y apunta en el registro si había cobertura delante.</summary>
        IEnumerator TapCover()
        {
            var cover = player.GetComponent<PlayerCover>();
            Debug.Log($"[Grabador] Q: disponible {(cover && cover.Available)} suelo {controller.isGrounded} en cobertura {(cover && cover.InCover)}");
            Down(keyboard.qKey);
            yield return Wait(0.05f);
            Up(keyboard.qKey);
            yield return null;
            Debug.Log($"[Grabador] Q después: en cobertura {(cover && cover.InCover)} baja {(cover && cover.LowCover)}");
        }

        /// <summary>Pulsa C (agacharse o levantarse) y apunta en el registro cómo queda.</summary>
        IEnumerator TapCrouch()
        {
            Down(keyboard.cKey);
            yield return Wait(0.05f);
            Up(keyboard.cKey);
            yield return null;
            var mv = player.GetComponent<PlayerMovement>();
            Debug.Log($"[Grabador] C: agachado {(mv && mv.Crouching)} alto {controller.height}");
        }

        /// <summary>Gira la cámara del juego a ritmo fijo (para correr en curva).</summary>
        sealed class CameraSpinner : MonoBehaviour
        {
            public ThirdPersonCamera Camera;
            public float Rate;
            void Update()
            {
                if (Camera) Camera.SetYaw(Camera.Yaw + Rate * Time.deltaTime);
            }
        }

        void Equip(string id)
        {
            if (inventory) inventory.Equip(id);
        }

        /// <summary>Pone al jugador en el plató mirando a +Z, con la cámara detrás, y espera a que se asiente.</summary>
        IEnumerator Place(Vector3 local, bool keepAim = false)
        {
            // Una tecla por fotograma: dos cambios del mismo dispositivo en un fotograma se pisan.
            foreach (var key in new ButtonControl[] { keyboard.wKey, keyboard.aKey, keyboard.sKey, keyboard.dKey, keyboard.leftShiftKey })
                if (key.isPressed)
                {
                    Up(key);
                    yield return null;
                }
            if (!keepAim && mouse.rightButton.isPressed)
            {
                Up(mouse.rightButton);
                yield return null;
            }
            controller.enabled = false;
            player.transform.SetPositionAndRotation(Stage + local + Vector3.up * 0.05f, Quaternion.identity);
            controller.enabled = true;
            var movement = player.GetComponent<PlayerMovement>();
            if (movement) movement.ResetMotion();
            if (followCamera) followCamera.SetYaw(0f);
            yield return Wait(0.6f);
        }

        IEnumerator Hold(ButtonControl[] keys, float seconds)
        {
            foreach (var key in keys)
            {
                Down(key);
                yield return null; // una tecla por fotograma: dos en el mismo se pisan
            }
            yield return Wait(seconds);
        }

        static IEnumerator Wait(float seconds)
        {
            int frames = Mathf.Max(1, Mathf.RoundToInt(seconds * 60f));
            for (int i = 0; i < frames; i++) yield return null;
        }

        /// <summary>Guarda <paramref name="count"/> fotogramas cada <paramref name="interval"/> s desde las tres vistas.</summary>
        IEnumerator Record(string action, int count, float interval)
        {
            var mv = player.GetComponent<PlayerMovement>();
            var combat = player.GetComponent<PlayerCombat>();
            Debug.Log($"[Grabador] {action}: pos {player.transform.position - Stage} vel {(mv ? mv.PlanarVelocity : Vector3.zero)} " +
                      $"move {GameInput.Actions.Move.ReadValue<Vector2>()} w {keyboard.wKey.isPressed} aim {GameInput.Actions.Aim.IsPressed()} " +
                      $"apuntando {(combat && combat.IsAiming)} arma {(inventory ? inventory.EquippedId : "-")} bloqueado {(mv && mv.Locked)} menu {GameUi.MenuOpen}");
            for (int n = 0; n < count; n++)
            {
                if (n > 0) yield return Wait(interval);
                yield return null;
                var root = player.transform;
                var focus = root.position + Vector3.up * 0.95f;
                Shot(action, "lado", n, focus + root.right * 3.6f + Vector3.up * 0.15f, focus);
                Shot(action, "frente", n, focus + (root.forward * 2.6f + root.right * 1.7f) + Vector3.up * 0.35f, focus);
                var main = Camera.main;
                if (main) Save(action, "juego", n, main);
            }
        }

        void Shot(string action, string view, int n, Vector3 from, Vector3 lookAt)
        {
            shotCamera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(lookAt - from));
            Save(action, view, n, shotCamera);
        }

        /// <summary>
        /// PPM binario (P6): el proyecto no tiene el módulo de conversión de imágenes en tiempo de juego, así que no hay
        /// EncodeToPNG; se pasan a PNG fuera de Unity (Pillow lee PPM).
        /// </summary>
        static byte[] ToPpm(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            int w = texture.width, h = texture.height;
            var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{w} {h}\n255\n");
            var data = new byte[header.Length + w * h * 3];
            header.CopyTo(data, 0);
            int o = header.Length;
            for (int y = h - 1; y >= 0; y--) // PPM va de arriba abajo; las texturas, de abajo arriba
                for (int x = 0; x < w; x++)
                {
                    var c = pixels[y * w + x];
                    data[o++] = c.r;
                    data[o++] = c.g;
                    data[o++] = c.b;
                }
            return data;
        }

        void Save(string action, string view, int n, Camera cam)
        {
            var rt = RenderTexture.GetTemporary(Size, Size, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            readback.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(outDir, $"{action}_{view}_{n:00}.ppm"), ToPpm(readback));
        }
    }
}
