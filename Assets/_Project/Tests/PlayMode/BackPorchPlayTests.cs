using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Comprueba en la escena generada (Prototype) que se puede subir a pie por la escalera en zigzag de un porche
    /// trasero de los apartamentos de 3 pisos, de terraza en terraza, hasta la puerta del tercer piso.
    /// Las medidas salen de PrototypeSceneBuilder.Houses.cs (BackPorch): marco local con la fachada en z = 0,
    /// banda A en z ≈ 2.55, banda B en z ≈ 3.65, rellanos en x ±1.7, terrazas a 1.07, 4.07 y 7.07 m y puerta en x −0.6.
    /// </summary>
    public class BackPorchPlayTests
    {
        const float BandA = 2.55f, BandB = 3.65f, LandingX = 1.7f, DoorX = -0.6f;
        static readonly float[] DeckTops = { 1.07f, 4.07f, 7.07f };

        [UnityTest]
        public IEnumerator Player_CanClimbBackPorchToEveryDoor()
        {
            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;

            var porch = GameObject.Find("PorcheTrasero");
            Assert.IsNotNull(porch, "La escena no tiene porches traseros.");
            var player = GameObject.FindWithTag("Player");
            var movement = player.GetComponent<PlayerMovement>();
            if (movement) movement.enabled = false;
            var controller = player.GetComponent<CharacterController>();
            var frame = porch.transform;

            Vector3 World(float x, float z) => frame.TransformPoint(new Vector3(x, 0f, z));

            // Al pie, por fuera de la torre de escalera, en el lado del rellano izquierdo.
            controller.enabled = false;
            player.transform.position = World(-LandingX, 5.2f) + Vector3.up * 0.4f;
            controller.enabled = true;

            var route = new List<(float x, float z)> { (-LandingX, BandA), (LandingX, BandA) }; // suelo → terraza 1
            for (int floor = 1; floor < DeckTops.Length; floor++)
                route.AddRange(new[] { (LandingX, BandB), (-LandingX, BandB), (-LandingX, BandA), (LandingX, BandA) });
            foreach (var (x, z) in route)
                yield return WalkTo(controller, World(x, z), 6f);

            // Entra en la terraza del tercer piso hasta su puerta.
            yield return WalkTo(controller, World(LandingX, 1f), 3f);
            yield return WalkTo(controller, World(DoorX, 0.6f), 4f);

            var local = frame.InverseTransformPoint(player.transform.position);
            Assert.Greater(local.y, DeckTops[2] - 0.2f, $"No llegó a la terraza del tercer piso (local {local}).");
            Assert.Less(Vector2.Distance(new Vector2(local.x, local.z), new Vector2(DoorX, 0.6f)), 0.5f, $"No llegó a la puerta (local {local}).");
        }

        /// <summary>Anda en línea recta (en planta) hacia el punto, con gravedad, hasta llegar o agotar el tiempo.</summary>
        static IEnumerator WalkTo(CharacterController controller, Vector3 target, float maxSeconds)
        {
            const float speed = 3f, gravity = -20f;
            float vertical = 0f, t = 0f;
            while (t < maxSeconds)
            {
                var flat = target - controller.transform.position;
                flat.y = 0f;
                if (flat.magnitude < 0.12f) break;
                float dt = Time.deltaTime;
                vertical = controller.isGrounded ? -2f : vertical + gravity * dt;
                controller.Move((flat.normalized * Mathf.Min(speed, flat.magnitude / dt) + Vector3.up * vertical) * dt);
                t += dt;
                yield return null;
            }
        }
    }
}
