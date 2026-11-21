using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Comprueba en la escena generada (Prototype) que el tráfico de la ciudad circula: salen coches alrededor del
    /// jugador, se mueven, no se meten unos dentro de otros, no se saltan los semáforos en rojo y el autobús da la vuelta
    /// a la manzana central. El tiempo va a ×4 para que dé tiempo a ver varios ciclos de semáforo.
    /// </summary>
    public class TrafficPlayTests
    {
        const float SimSeconds = 90f;
        static readonly Vector2[] SignalNodes = { new(-110f, -80f), new(-110f, 80f), new(110f, -80f), new(110f, 80f) };
        const float BoxRadius = 7.3f;

        [UnityTest]
        public IEnumerator Traffic_DrivesAroundAndObeysTheLights()
        {
            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;

            var population = Object.FindFirstObjectByType<TrafficPopulation>();
            Assert.IsNotNull(population, "La escena no tiene tráfico (TrafficPopulation).");
            var cars = Object.FindObjectsByType<TrafficCar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TrafficCar bus = null;
            foreach (var c in cars) if (c.IsBus) bus = c;
            Assert.IsNotNull(bus, "No hay autobús.");

            Time.timeScale = 4f;
            try
            {
                yield return new WaitForSeconds(1f);
                Assert.GreaterOrEqual(population.ActiveCount, 5, "Salen muy pocos coches.");

                var lights = new Dictionary<Vector2, TrafficLight>();
                foreach (var l in Object.FindObjectsByType<TrafficLight>(FindObjectsSortMode.None))
                {
                    // El centro del cruce: la media de sus postes.
                    var sum = Vector3.zero;
                    int n = 0;
                    foreach (Transform post in l.transform) { sum += post.position; n++; }
                    var c = new Vector2(sum.x / n, sum.z / n);
                    foreach (var node in SignalNodes) if (Vector2.Distance(node, c) < 4f) lights[node] = l;
                }
                Assert.AreEqual(4, lights.Count, "No se encuentran los cuatro semáforos.");

                var start = new Dictionary<TrafficCar, Vector3>();
                var travelled = new Dictionary<TrafficCar, float>();
                var last = new Dictionary<TrafficCar, Vector3>();
                var busStart = bus.transform.position;
                float busTravel = 0f;
                var busLast = busStart;
                int samples = 0, overlaps = 0, redRuns = 0, boxEntries = 0;
                var report = new List<string>();

                float end = Time.time + SimSeconds;
                while (Time.time < end)
                {
                    yield return new WaitForFixedUpdate();
                    samples++;
                    var active = new List<TrafficCar>();
                    foreach (var c in cars) if (c.gameObject.activeInHierarchy) active.Add(c);

                    foreach (var c in active)
                    {
                        var p = c.transform.position;
                        if (!last.TryGetValue(c, out var prev) || (p - prev).sqrMagnitude > 25f) { last[c] = p; continue; } // recién sacado
                        travelled[c] = (travelled.TryGetValue(c, out float t) ? t : 0f) + Vector3.Distance(prev, p);
                        if (!start.ContainsKey(c)) start[c] = p;

                        // ¿Entra en un cruce con semáforo con el rojo puesto desde hace más de 1.5 s?
                        foreach (var node in SignalNodes)
                        {
                            float before = Vector2.Distance(new Vector2(prev.x, prev.z), node);
                            float now = Vector2.Distance(new Vector2(p.x, p.z), node);
                            if (before >= BoxRadius && now < BoxRadius)
                            {
                                boxEntries++;
                                var dir = p - prev;
                                bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
                                var light = lights[node];
                                if (light.VehicleColor(alongX) == SignalColor.Red && light.VehicleColor(alongX, 1.5f) == SignalColor.Red)
                                {
                                    var car = c.GetComponent<TrafficCar>();
                                    if (car.Panicking) report.Add($"{c.name} huye de unos tiros y se salta el rojo en {node} (vale)");
                                    else
                                    {
                                        redRuns++;
                                        report.Add($"{c.name} se salta el rojo en {node} ({p}, {car.DebugState})");
                                    }
                                }
                            }
                        }
                        last[c] = p;
                    }
                    for (int i = 0; i < active.Count; i++)
                    for (int j = i + 1; j < active.Count; j++)
                    {
                        var a = active[i].transform.position;
                        var b = active[j].transform.position;
                        if ((a - b).sqrMagnitude < 2.2f * 2.2f)
                        {
                            overlaps++;
                            if (report.Count < 12) report.Add($"{active[i].name} y {active[j].name} chocan en {a}");
                        }
                    }

                    var bp = bus.transform.position;
                    busTravel += Vector3.Distance(busLast, bp);
                    busLast = bp;
                    Assert.That(Mathf.Abs(bp.x) < 120f && Mathf.Abs(bp.z) < 90f, $"El autobús se sale de su ruta: {bp}");
                }

                int moving = 0;
                foreach (var kv in travelled) if (kv.Value > 30f) moving++;
                string details = string.Join("\n", report);
                Debug.Log($"[Tráfico] {travelled.Count} coches vistos, {moving} se mueven, {boxEntries} entradas en cruces con semáforo, " +
                          $"{redRuns} en rojo, {overlaps} solapes en {samples} muestras; autobús {busTravel:F0} m.\n{details}");
                Assert.GreaterOrEqual(moving, travelled.Count / 2, "Más de la mitad de los coches no se mueve.");
                Assert.Greater(boxEntries, 3, "Casi nadie pasa por los cruces con semáforo.");
                Assert.AreEqual(0, redRuns, details);
                Assert.Less(overlaps, samples / 50 + 1, details);
                Assert.Greater(busTravel, 200f, "El autobús no da la vuelta.");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// El paso a nivel de S CALDER AVE (x = −110) se cierra al llegar el tren de mercancías: baja las barreras y ningún
        /// coche entra en la vía mientras el tren la ocupa; cuando pasa, las barreras suben. El jugador se pone al lado para
        /// que salga tráfico.
        /// </summary>
        [UnityTest]
        public IEnumerator LevelCrossing_ClosesForTheFreightTrain()
        {
            SceneManager.LoadScene("Prototype");
            yield return null;
            yield return null;

            var player = GameObject.FindWithTag("Player");
            var controller = player.GetComponent<CharacterController>();
            var movement = player.GetComponent<PlayerMovement>();
            if (movement) movement.enabled = false;
            controller.enabled = false;
            player.transform.position = new Vector3(-125f, 0.3f, -440f);
            controller.enabled = true;

            LevelCrossing crossing = null;
            foreach (var c in Object.FindObjectsByType<LevelCrossing>(FindObjectsSortMode.None))
                if (c.name == "PasoNivel_-110") crossing = c;
            Assert.IsNotNull(crossing, "No está el paso a nivel de S CALDER AVE.");
            var train = Object.FindFirstObjectByType<FreightTrain>();
            Assert.IsNotNull(train, "No hay tren de mercancías.");
            var cars = Object.FindObjectsByType<TrafficCar>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Time.timeScale = 4f;
            try
            {
                float limit = Time.time + 120f;
                while (!crossing.Closed && Time.time < limit) yield return null;
                Assert.IsTrue(crossing.Closed, "El paso no se cierra.");
                yield return new WaitForSeconds(9f);
                Assert.Greater(crossing.ArmLowered, 0.95f, "Las barreras no bajan.");

                int intrusions = 0;
                string where = "";
                limit = Time.time + 120f;
                while (crossing.Closed && Time.time < limit)
                {
                    bool trainOnCrossing = Mathf.Abs(train.Center - (-110f)) < train.Length / 2f + 2f;
                    if (trainOnCrossing)
                        foreach (var car in cars)
                        {
                            if (!car.gameObject.activeInHierarchy) continue;
                            var p = car.transform.position;
                            if (p.x > -117f && p.x < -103f && p.z > -466f && p.z < -454f) { intrusions++; where = $"{car.name} {p}"; }
                        }
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsFalse(crossing.Closed, "El paso no se vuelve a abrir.");
                Assert.AreEqual(0, intrusions, $"Un coche se mete en la vía con el tren: {where}");
                yield return new WaitForSeconds(6f);
                Assert.Less(crossing.ArmLowered, 0.05f, "Las barreras no suben.");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
