using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GTAChiraq.Tests
{
    public class VehicleEntryTests
    {
        GameObject player;
        GameObject car;
        VehicleEntry entry;
        VehicleController vehicle;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("Player");
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerMovement>();
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(player.transform, false);
            entry = player.AddComponent<VehicleEntry>();

            car = new GameObject("Car");
            car.transform.position = new Vector3(10f, 0f, 0f);
            vehicle = car.AddComponent<VehicleController>();
            var exit = new GameObject("Exit").transform;
            exit.SetParent(car.transform, false);
            exit.localPosition = new Vector3(-2f, 0f, 0f);
            vehicle.Configure(Array.Empty<VehicleController.Wheel>(), exit);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(car);
        }

        [Test]
        public void EnterVehicle_DisablesOnFootControlAndTakesTheWheel()
        {
            entry.EnterVehicle(vehicle);

            Assert.IsTrue(entry.IsDriving);
            Assert.AreSame(vehicle, entry.CurrentVehicle);
            Assert.IsTrue(vehicle.HasDriver);
            Assert.IsFalse(player.GetComponent<CharacterController>().enabled);
            Assert.IsFalse(player.GetComponent<PlayerMovement>().enabled);
            Assert.IsFalse(player.GetComponentInChildren<Renderer>().enabled);
            Assert.AreSame(car.transform, player.transform.parent);
        }

        [Test]
        public void ExitVehicle_RestoresOnFootControlAtExitPoint()
        {
            entry.EnterVehicle(vehicle);
            entry.ExitVehicle();

            Assert.IsFalse(entry.IsDriving);
            Assert.IsFalse(vehicle.HasDriver);
            Assert.IsNull(player.transform.parent);
            Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
            Assert.IsTrue(player.GetComponent<PlayerMovement>().enabled);
            Assert.IsTrue(player.GetComponentInChildren<Renderer>().enabled);
            Assert.That(Vector3.Distance(player.transform.position, new Vector3(8f, 0f, 0f)), Is.LessThan(0.001f));
        }

        [Test]
        public void EnterVehicle_IgnoresVehicleThatAlreadyHasDriver()
        {
            vehicle.SetDriver(true);

            entry.EnterVehicle(vehicle);

            Assert.IsFalse(entry.IsDriving);
            Assert.IsTrue(player.GetComponent<PlayerMovement>().enabled);
        }

        [Test]
        public void FindNearest_ReturnsClosestFreeVehicleInRange()
        {
            var nearCar = new GameObject("Near").AddComponent<VehicleController>();
            nearCar.transform.position = new Vector3(2f, 0f, 0f);
            try
            {
                var candidates = new[] { vehicle, nearCar };

                Assert.AreSame(nearCar, VehicleEntry.FindNearest(Vector3.zero, candidates, 3.5f));
                Assert.IsNull(VehicleEntry.FindNearest(Vector3.zero, new[] { vehicle }, 3.5f));

                nearCar.SetDriver(true);
                Assert.IsNull(VehicleEntry.FindNearest(Vector3.zero, candidates, 3.5f));
            }
            finally
            {
                Object.DestroyImmediate(nearCar.gameObject);
            }
        }
    }
}
