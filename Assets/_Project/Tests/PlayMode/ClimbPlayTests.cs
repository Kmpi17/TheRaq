using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    public class ClimbPlayTests : InputTestFixture
    {
        Keyboard keyboard;
        GameObject ground, wall, player;

        public override void Setup()
        {
            base.Setup();
            GameInput.Reset();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        /// <summary>Escena vacía: sin lo que dejen cargado otros tests (la escena prototipo con sus NPC).</summary>
        [UnitySetUp]
        public IEnumerator FreshScene() => TestScenes.Fresh();

        public override void TearDown()
        {
            foreach (var go in new[] { player, wall, ground, car })
                if (go) Object.Destroy(go);
            GameInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator SpaceInFrontOfLowWall_VaultsToTheOtherSide()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // muro de 1 m de alto y 0.3 de grueso, a 1 m delante
            wall.transform.position = new Vector3(0f, 0.5f, 1f);
            wall.transform.localScale = new Vector3(4f, 1f, 0.3f);

            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerMovement>();
            var climb = player.AddComponent<PlayerClimb>();
            player.layer = 2;

            for (int i = 0; i < 10; i++) yield return null; // que se asiente en el suelo
            Assert.That(climb.Available, Is.EqualTo(ClimbKind.Vault));

            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return new WaitForSeconds(1f);

            Assert.Greater(player.transform.position.z, 1.4f, "Debería haber pasado al otro lado del muro.");
            Assert.Less(player.transform.position.y, 0.3f, "Debería haber vuelto al suelo.");
        }

        GameObject car;

        PlayerClimb MakePlayer()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;
            player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerMovement>();
            player.layer = 2;
            return player.AddComponent<PlayerClimb>();
        }

        IEnumerator PressSpaceAndWait()
        {
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return new WaitForSeconds(1.2f);
        }

        [UnityTest]
        public IEnumerator SpaceInFrontOfThinFence_VaultsToTheOtherSide()
        {
            var climb = MakePlayer();
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // valla de 1.1 m y 3 cm, como las del generador
            wall.transform.position = new Vector3(0f, 0.55f, 0.9f);
            wall.transform.localScale = new Vector3(4f, 1.1f, 0.03f);

            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(climb.Available, Is.EqualTo(ClimbKind.Vault));
            yield return PressSpaceAndWait();

            Assert.Greater(player.transform.position.z, 1.1f, "Debería haber saltado la valla.");
        }

        [UnityTest]
        public IEnumerator SpaceInFrontOfCar_SlidesOverToTheOtherSide()
        {
            var climb = MakePlayer();
            // Coche de 2 m de ancho: caja baja hasta 1 m y habitáculo hasta 1.4 m, colliders en la raíz sin Renderer.
            car = new GameObject("Car") { layer = 6 };
            car.transform.position = new Vector3(0f, 0f, 1.6f);
            car.AddComponent<Rigidbody>().isKinematic = true;
            car.AddComponent<VehicleController>();
            car.AddComponent<SolidVisual>();
            var lower = car.AddComponent<BoxCollider>();
            lower.center = new Vector3(0f, 0.6f, 0f);
            lower.size = new Vector3(5f, 0.8f, 2f);
            var cabin = car.AddComponent<BoxCollider>();
            cabin.center = new Vector3(0f, 1.2f, 0f);
            cabin.size = new Vector3(2.5f, 0.4f, 1.8f);

            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(climb.Available, Is.EqualTo(ClimbKind.SlideOver));
            yield return PressSpaceAndWait();

            Assert.Greater(player.transform.position.z, 2.6f, "Debería haber pasado al otro lado del coche.");
            Assert.Less(player.transform.position.y, 0.3f, "Debería haber vuelto al suelo.");
            Object.Destroy(car);
        }
    }
}
