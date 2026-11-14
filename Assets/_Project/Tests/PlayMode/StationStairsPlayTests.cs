using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Comprueba en la escena generada (Prototype) que el jugador puede subir a pie por las dos escaleras de la
    /// estación del tren elevado hasta el andén. Las coordenadas salen de PrototypeSceneBuilder.Train.cs
    /// (estación de z 4 a 68, andén a 9.6 m, escaleras de 1.4 m de ancho y 1.6 m de huella por metro de subida).
    /// </summary>
    public class StationStairsPlayTests
    {
        const float PlatformTop = 9.6f;
        const float StairTopZ = 10f;                          // StationZ0 + 6
        const float StairBottomZ = StairTopZ + 9.45f * 1.6f; // + (PlatformTop − SidewalkTop) · huella

        [UnityTest]
        public IEnumerator Player_CanClimbWestStairsToPlatform() => Climb(100.35f, 103f);

        [UnityTest]
        public IEnumerator Player_CanClimbEastStairsToPlatform() => Climb(119.65f, 117f);

        static IEnumerator Climb(float stairX, float platformX)
        {
            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;

            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "La escena no tiene jugador.");
            var movement = player.GetComponent<PlayerMovement>();
            if (movement) movement.enabled = false; // se mueve a mano, sin input
            var controller = player.GetComponent<CharacterController>();

            controller.enabled = false;
            player.transform.position = new Vector3(stairX, 0.3f, StairBottomZ + 1.5f);
            controller.enabled = true;

            // Sube hacia el sur por la escalera hasta el rellano y luego entra en el andén.
            yield return Walk(controller, Vector3.back, () => player.transform.position.z <= StairTopZ - 1.1f, 12f);
            var towardPlatform = new Vector3(Mathf.Sign(platformX - stairX), 0f, 0f);
            yield return Walk(controller, towardPlatform, () => Mathf.Abs(player.transform.position.x - platformX) < 0.3f, 4f);

            var p = player.transform.position;
            Assert.Greater(p.y, PlatformTop - 0.2f, $"No llegó a la altura del andén: {p}");
            Assert.Less(Mathf.Abs(p.x - platformX), 0.5f, $"No entró en el andén: {p}");
        }

        static IEnumerator Walk(CharacterController controller, Vector3 direction, System.Func<bool> done, float maxSeconds)
        {
            const float speed = 4f, gravity = -20f;
            float vertical = 0f, t = 0f;
            while (!done() && t < maxSeconds)
            {
                float dt = Time.deltaTime;
                vertical = controller.isGrounded ? -2f : vertical + gravity * dt;
                controller.Move((direction * speed + Vector3.up * vertical) * dt);
                t += dt;
                yield return null;
            }
        }
    }
}
