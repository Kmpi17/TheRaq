using UnityEngine;

namespace GTAChiraq.Tests
{
    /// <summary>Coches de prueba con el mismo montaje que <c>PrototypeSceneBuilder.MakeDrivable</c>, sin modelo.</summary>
    public static class TestCars
    {
        /// <summary>Un sedán como los del generador (sin modelo: una caja y las cuatro ruedas).</summary>
        public static VehicleController Sedan(Vector3 position, out GameObject root)
        {
            root = new GameObject("CochePrueba");
            root.transform.position = position;
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1400f;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.95f, 0f);
            box.size = new Vector3(1.9f, 0.9f, 4.6f);
            var vehicle = root.AddComponent<VehicleController>();

            var wheels = new VehicleController.Wheel[4];
            int i = 0;
            foreach (float z in new[] { 1.4f, -1.4f })
            foreach (float x in new[] { -0.82f, 0.82f })
            {
                var w = new GameObject("Rueda").AddComponent<WheelCollider>();
                w.transform.SetParent(root.transform, false);
                w.transform.localPosition = new Vector3(x, 0.4f, z);
                w.radius = 0.38f;
                w.mass = 20f;
                w.suspensionDistance = 0.25f;
                w.suspensionSpring = new JointSpring { spring = 35000f, damper = 4500f, targetPosition = 0.5f };
                bool front = z > 0f;
                wheels[i++] = new VehicleController.Wheel { collider = w, steer = front, drive = !front, handbrake = !front };
            }
            vehicle.Configure(wheels, null);
            vehicle.SetDriver(true);
            return vehicle;
        }

    }
}
