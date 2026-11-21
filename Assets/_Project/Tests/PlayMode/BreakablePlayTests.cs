using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Petición del usuario (2026-11-17): «si te chocas contra una valla o señal, que se destruya como en GTA». Un coche
    /// de prueba choca contra una valla como las del generador (panel, postes y barandilla) y contra una señal de STOP:
    /// rápido, las rompe y sigue sin frenar en seco; despacio, solo choca.
    /// </summary>
    public class BreakablePlayTests
    {
        GameObject ground, car;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return TestScenes.Fresh();
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.localScale = new Vector3(300f, 1f, 300f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(go);
        }

        /// <summary>Valla de hierro de 20 m a lo largo de X en z = <paramref name="z"/>, como la de FenceSegment.</summary>
        static BreakableFence Fence(float z, out GameObject root)
        {
            root = new GameObject("Vallas");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var size = new Vector3(20f, 1.8f, 0.03f);
            var wall = new GameObject("Valla");
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = new Vector3(0f, size.y / 2f, z);
            wall.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Box("Valla", size, new Vector2(1.8f, 1.8f), true);
            wall.AddComponent<MeshRenderer>().sharedMaterial = material;
            wall.AddComponent<BoxCollider>().size = size;
            var posts = new Transform[9];
            for (int i = 0; i < posts.Length; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(p.GetComponent<Collider>());
                p.name = "Poste";
                p.transform.SetParent(root.transform, false);
                p.transform.localPosition = new Vector3(-10f + i * 2.5f, 0.95f, z);
                p.transform.localScale = new Vector3(0.06f, 0.95f, 0.06f);
                posts[i] = p.transform;
            }
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(rail.GetComponent<Collider>());
            rail.name = "Barandilla";
            rail.transform.SetParent(root.transform, false);
            rail.transform.localPosition = new Vector3(0f, 1.8f, z);
            rail.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
            rail.transform.localScale = new Vector3(0.05f, 10f, 0.05f);
            var fence = wall.AddComponent<BreakableFence>();
            fence.Configure(new Vector2(1.8f, 1.8f), true, posts, rail.transform, BreakableFence.Kind.Iron);
            fence.SetStrength(5f, 0.8f);
            return fence;
        }

        /// <summary>Acelera hacia +Z hasta <paramref name="speed"/> y lo mantiene hasta pasar z = <paramref name="untilZ"/> (o rendirse).</summary>
        static IEnumerator DriveAt(VehicleController v, float speed, float untilZ, float seconds)
        {
            for (float t = 0f; t < seconds && v.transform.position.z < untilZ; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(0f, v.ForwardSpeed < speed ? 1f : 0f));
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator FastCar_BreaksThroughFenceAndKeepsGoing()
        {
            var fence = Fence(40f, out _);
            var v = TestCars.Sedan(new Vector3(2f, 0.3f, -20f), out car);
            float before = 0f;
            for (float t = 0f; t < 15f && car.transform.position.z < 36f; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(0f, v.ForwardSpeed < 16f ? 1f : 0f));
                yield return new WaitForFixedUpdate();
                before = v.ForwardSpeed;
            }
            yield return DriveAt(v, 0f, 46f, 3f);
            float after = v.ForwardSpeed;

            Assert.IsTrue(fence.Broken, "la valla se rompe");
            Assert.IsFalse(fence.GetComponent<MeshRenderer>().enabled, "el panel entero se esconde");
            Assert.Greater(car.transform.position.z, 44f, "el coche la atraviesa");
            Assert.Greater(after, before * 0.55f, $"no frena en seco ({before:F1} → {after:F1} m/s)");
            Assert.Greater(Debris.Count, 2, "salen trozos volando");
            int standing = 0;
            foreach (var f in Object.FindObjectsByType<BreakableFence>(FindObjectsSortMode.None))
                if (!f.Broken && f.GetComponent<MeshRenderer>().enabled) standing++;
            Assert.AreEqual(2, standing, "a los lados del hueco la valla sigue en pie");
            Debug.Log($"[Romper] valla: {before:F1} → {after:F1} m/s, {Debris.Count} trozos.");
        }

        [UnityTest]
        public IEnumerator SlowCar_OnlyBumpsTheFence()
        {
            var fence = Fence(15f, out _);
            var v = TestCars.Sedan(new Vector3(2f, 0.3f, 0f), out car);
            yield return DriveAt(v, 2.5f, 20f, 10f);
            Assert.IsFalse(fence.Broken, "despacio no se rompe");
            Assert.Less(car.transform.position.z, 15f, "y no se pasa");
        }

        [UnityTest]
        public IEnumerator FastCar_KnocksDownAStopSign()
        {
            var sign = new GameObject("Stop_PRUEBA");
            sign.transform.position = new Vector3(0.5f, 0f, 35f);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(sign.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.73f, 0f);
            pole.transform.localScale = new Vector3(0.07f, 1.73f, 0.07f);
            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(plate.GetComponent<Collider>());
            plate.transform.SetParent(sign.transform, false);
            plate.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            var prop = sign.AddComponent<BreakableProp>();
            prop.Configure(BreakableProp.Kind.Metal, 30f);

            var v = TestCars.Sedan(new Vector3(0f, 0.3f, -10f), out car);
            float before = 0f;
            for (float t = 0f; t < 15f && car.transform.position.z < 31f; t += Time.fixedDeltaTime)
            {
                v.SetAutoInput(new Vector2(0f, v.ForwardSpeed < 14f ? 1f : 0f));
                yield return new WaitForFixedUpdate();
                before = v.ForwardSpeed;
            }
            yield return DriveAt(v, 0f, 60f, 2f);

            Assert.IsTrue(prop.Broken, "la señal se rompe");
            Assert.IsNotNull(sign.GetComponent<Rigidbody>(), "y se vuelve un cuerpo físico");
            Assert.Less(sign.transform.up.y, 0.6f, "acaba tumbada");
            Assert.Greater(v.ForwardSpeed, before * 0.6f, "el coche sigue");
        }
    }
}
