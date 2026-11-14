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
    }
}
