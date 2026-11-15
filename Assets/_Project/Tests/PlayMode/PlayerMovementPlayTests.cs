using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    public class PlayerMovementPlayTests : InputTestFixture
    {
        Keyboard keyboard;
        GameObject ground;
        GameObject player;

        public override void Setup()
        {
            base.Setup();
            GameInput.Reset(); // las acciones deben crearse sobre los dispositivos simulados
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        /// <summary>Escena vacía: sin lo que dejen cargado otros tests (la escena prototipo con sus NPC).</summary>
        [UnitySetUp]
        public IEnumerator FreshScene() => TestScenes.Fresh();

        public override void TearDown()
        {
            if (player) Object.Destroy(player);
            if (ground) Object.Destroy(ground);
            GameInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator HoldingW_MovesPlayerForward()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 10f;

            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.1f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerMovement>();

            yield return null;
            float startZ = player.transform.position.z;

            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(keyboard.wKey);

            Assert.Greater(player.transform.position.z - startZ, 1f, "El jugador debería avanzar ~2 m en 0,5 s andando.");
        }

        PlayerMovement SpawnPlayer()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 10f;
            player = new GameObject("Player") { layer = 2 };
            player.transform.position = new Vector3(0f, 0.1f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            return player.AddComponent<PlayerMovement>();
        }

        // Los eventos van a la cola y los procesa el bucle de Unity en el fotograma siguiente, antes de los Update,
        // como en el juego: así WasPressedThisFrame se ve en ese fotograma. Cada uno avanza el reloj de la entrada
        // simulada (si no, se descartan los que repiten marca de tiempo).
        void Down(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            currentTime += 0.01;
            Press(key, queueEventOnly: true);
        }

        void Up(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            currentTime += 0.01;
            Release(key, queueEventOnly: true);
        }

        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            Down(key);
            yield return null;
            yield return null;
            Up(key);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator C_CrouchesSlowerAndStandsBackUp()
        {
            var movement = SpawnPlayer();
            var controller = player.GetComponent<CharacterController>();
            yield return new WaitForSeconds(0.2f);

            yield return Tap(keyboard.cKey);
            Assert.IsTrue(movement.Crouching, $"C debería agacharlo (en el suelo: {controller.isGrounded}).");
            Assert.Less(controller.height, 1.4f, "Agachado, el CharacterController es más bajo.");

            Down(keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            float speed = movement.PlanarVelocity.magnitude;
            Up(keyboard.wKey);
            Assert.That(speed, Is.InRange(1f, 2f), "Agachado anda despacio (1.7 m/s; corriendo, 4.4).");
            yield return new WaitForSeconds(0.2f);

            yield return Tap(keyboard.cKey);
            Assert.IsFalse(movement.Crouching, "C otra vez lo levanta.");
            Assert.AreEqual(1.8f, controller.height, 0.001f);
        }

        [UnityTest]
        public IEnumerator UnderLowCeiling_CannotStandUp()
        {
            var movement = SpawnPlayer();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(movement.SetCrouching(true));

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = player.transform.position + Vector3.up * 1.5f;
            ceiling.transform.localScale = new Vector3(3f, 0.2f, 3f);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(movement.SetCrouching(false), "Con un techo a 1.4 m no cabe de pie.");
            Assert.IsTrue(movement.Crouching);
            Object.Destroy(ceiling);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(movement.SetCrouching(false), "Sin el techo, se levanta.");
        }
    }
}
