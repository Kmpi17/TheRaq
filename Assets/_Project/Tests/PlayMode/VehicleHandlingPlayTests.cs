using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Mide cómo gira el coche del jugador en un suelo plano, con el mismo montaje que <c>PrototypeSceneBuilder.MakeDrivable</c>
    /// (sedán de 1400 kg, tracción trasera, ruedas de 0.38 m): a poca velocidad tiene que dar media vuelta en pocos
    /// segundos, a velocidad de avenida tiene que girar sin volcar y en recta no se tiene que ir de lado.
    /// Petición del usuario (2026-11-17): «cuesta mucho girar». Con el control de antes (sin ayudas) giraba 92° en los
    /// 2.5 s a poca velocidad (las ruedas pedían unos 250°: se iba de morro) y 57° a 20 m/s.
    /// </summary>
    public class VehicleHandlingPlayTests
    {
        GameObject ground, car;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return TestScenes.Fresh();
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.localScale = new Vector3(600f, 1f, 600f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (car) Object.Destroy(car);
            if (ground) Object.Destroy(ground);
        }

        static float Heading(Transform t) => Mathf.Atan2(t.forward.x, t.forward.z) * Mathf.Rad2Deg;

        IEnumerator Drive(VehicleController v, Vector2 input, float seconds)
        {
            v.SetAutoInput(input);
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Car_TurnsAroundEasilyAndStaysUpright()
        {
            var v = TestCars.Sedan(new Vector3(0f, 0.3f, -200f), out car);
            yield return Drive(v, Vector2.zero, 1f); // que se asiente

            // En recta, 4 s a fondo: no se va de lado.
            float h0 = Heading(car.transform);
            yield return Drive(v, new Vector2(0f, 1f), 4f);
            float straightDrift = Mathf.Abs(Mathf.DeltaAngle(h0, Heading(car.transform)));
            float speedStraight = v.ForwardSpeed;

            // Girando a tope 2.5 s con medio acelerador (como al doblar una esquina).
            float h1 = Heading(car.transform);
            float turned = 0f, last = h1;
            for (float t = 0f; t < 2.5f; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(1f, 0.5f));
                yield return new WaitForFixedUpdate();
                float h = Heading(car.transform);
                turned += Mathf.DeltaAngle(last, h);
                last = h;
            }
            float uprightLow = car.transform.up.y;

            // A velocidad de avenida (unos 20 m/s), 2 s girando a tope.
            Object.Destroy(car);
            v = TestCars.Sedan(new Vector3(0f, 0.3f, -250f), out car);
            yield return Drive(v, Vector2.zero, 1f);
            for (float t = 0f; t < 12f && v.ForwardSpeed < 20f; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(0f, 1f));
                yield return new WaitForFixedUpdate();
            }
            float fast = v.ForwardSpeed;
            float turnedFast = 0f;
            last = Heading(car.transform);
            float minUp = 1f;
            for (float t = 0f; t < 2f; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(1f, 0.3f));
                yield return new WaitForFixedUpdate();
                float h = Heading(car.transform);
                turnedFast += Mathf.DeltaAngle(last, h);
                last = h;
                minUp = Mathf.Min(minUp, car.transform.up.y);
            }

            Debug.Log($"[Coche] recta 4 s: {speedStraight:F1} m/s, se desvía {straightDrift:F1}°; girando 2.5 s a poca velocidad: {turned:F0}° " +
                      $"(de pie {uprightLow:F2}); a {fast:F1} m/s, 2 s girando: {turnedFast:F0}° (inclinación mínima {minUp:F2}).");

            Assert.Less(straightDrift, 3f, "En recta se va de lado.");
            Assert.Greater(speedStraight, 10f, "Acelera muy poco.");
            Assert.Greater(turned, 130f, "A poca velocidad gira muy poco (antes, 92°).");
            Assert.Greater(turnedFast, 65f, "A velocidad de avenida gira muy poco (antes, 57°).");
            Assert.Less(turnedFast, 140f, "A velocidad de avenida gira demasiado (peonza).");
            Assert.Greater(minUp, 0.75f, "Vuelca al girar.");
        }
    }
}
