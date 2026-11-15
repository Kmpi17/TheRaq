using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GTAChiraq.Tests
{
    public class VehicleLockTests
    {
        GameObject car;
        VehicleLock carLock;

        [SetUp]
        public void SetUp()
        {
            car = new GameObject("Car");
            car.AddComponent<VehicleController>();
            carLock = car.AddComponent<VehicleLock>();
            carLock.Configure(isLocked: true, seconds: 2f, chanceOfAlarm: 0f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(car);

        [Test]
        public void Force_OpensOnlyAfterHoldingLongEnough()
        {
            Assert.IsFalse(carLock.Force(1f));
            Assert.That(carLock.Progress, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.IsTrue(carLock.Locked);

            Assert.IsTrue(carLock.Force(1.1f));
            Assert.IsFalse(carLock.Locked);
            Assert.IsTrue(carLock.Force(0f), "Ya abierto, no hay que volver a forzarlo.");
        }

        [Test]
        public void ResetForce_LosesProgress()
        {
            carLock.Force(1.5f);
            carLock.ResetForce();
            Assert.That(carLock.Progress, Is.EqualTo(0f));
            Assert.IsFalse(carLock.Force(1f));
            Assert.IsTrue(carLock.Locked);
        }

        [Test]
        public void SolidVisual_MakesRootCollidersVisibleAndNoCoverMarksSeeThrough()
        {
            var box = car.AddComponent<BoxCollider>(); // collider en la raíz, sin Renderer (como los coches)
            Assert.IsFalse(SolidVisual.IsVisible(box));
            car.AddComponent<SolidVisual>();
            Assert.IsTrue(SolidVisual.IsVisible(box));

            Assert.IsFalse(SolidVisual.IsSeeThrough(box));
            car.AddComponent<NoCover>();
            Assert.IsTrue(SolidVisual.IsSeeThrough(box));
        }
    }
}
