using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Saca y quita coches del tráfico alrededor del jugador, como <c>NpcPopulation</c> con la gente. Tiene una reserva
    /// de coches ya construidos (los pone el generador, desactivados) y mantiene activos tantos como pida la hora
    /// (<see cref="TrafficMath.Density"/>; con nieve, menos). Los nuevos salen en carriles lejos del jugador y fuera de la
    /// vista; los que se alejan demasiado (o se quedan atascados o embestidos sin que se vea) se guardan otra vez.
    /// </summary>
    public class TrafficPopulation : MonoBehaviour
    {
        [SerializeField] TrafficNetwork network;
        [SerializeField] TrafficCar[] pool = new TrafficCar[0];
        [SerializeField] int maxCars = 30;
        [SerializeField] float spawnMin = 70f;
        [SerializeField] float spawnMax = 190f;
        [SerializeField] float despawnDistance = 235f;
        [Tooltip("Coches atascados o embestidos que se quitan si nadie los ve, pasado este tiempo.")]
        [SerializeField] float stuckSeconds = 25f;

        Transform player;
        float nextTick;
        bool started;
        readonly System.Collections.Generic.Dictionary<TrafficCar, float> stuckSince = new System.Collections.Generic.Dictionary<TrafficCar, float>();

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var car in pool) if (car && car.gameObject.activeSelf) n++;
                return n;
            }
        }

        public void Configure(TrafficNetwork net, TrafficCar[] cars, int max)
        {
            network = net;
            pool = cars;
            maxCars = max;
        }

        void Update()
        {
            if (Time.time < nextTick || !network) return;
            nextTick = Time.time + 0.4f;
            if (!player)
            {
                var go = GameObject.FindWithTag("Player");
                if (!go) return;
                player = go.transform;
            }

            var center = player.position;
            float hour = WeatherSystem.Instance ? WeatherSystem.Instance.Hour : 12f;
            int target = Mathf.RoundToInt(maxCars * TrafficMath.Density(hour));

            // Quita los lejanos y los atascados que no se ven.
            int active = 0;
            foreach (var car in pool)
            {
                if (!car || !car.gameObject.activeSelf) continue;
                var p = car.transform.position;
                float d = Vector3.Distance(p, center);
                bool seen = Seen(p, d);
                bool stuck = car.Wrecked || car.Speed < 0.2f;
                if (stuck) { if (!stuckSince.ContainsKey(car)) stuckSince[car] = Time.time; }
                else stuckSince.Remove(car);
                bool stale = stuck && Time.time - stuckSince[car] > stuckSeconds && !seen && d > 40f;
                if ((d > despawnDistance && !seen) || d > despawnDistance + 80f || stale || active >= target + 4)
                {
                    stuckSince.Remove(car);
                    car.gameObject.SetActive(false);
                    continue;
                }
                active++;
            }

            // Saca coches nuevos: al principio, de golpe y más cerca; luego, de dos en dos, lejos y sin que se vea.
            int budget = started ? 2 : target;
            float min = started ? spawnMin : 20f;
            for (int tries = 0, maxTries = started ? 40 : 800; active < target && budget > 0 && tries < maxTries; tries++)
            {
                if (!TryPick(center, min, out int laneIndex, out float s, out Vector3 point)) continue;
                if (started && Seen(point, Vector3.Distance(point, center)) && Vector3.Distance(point, center) < 150f) continue;
                if (Crowded(point)) continue;
                var car = FreeCar();
                if (!car) break;
                car.gameObject.SetActive(true);
                car.Place(laneIndex, s, network.Lanes[laneIndex].Data.SpeedLimit * 0.8f);
                active++;
                budget--;
            }
            if (!started) Debug.Log($"[GTAChiraq] Tráfico: {active} coches al empezar (objetivo {target} a las {hour:F1} h).");
            started = true;
        }

        bool TryPick(Vector3 center, float min, out int laneIndex, out float s, out Vector3 point)
        {
            var lanes = network.Lanes;
            laneIndex = Random.Range(0, lanes.Count);
            var lane = lanes[laneIndex];
            s = Random.Range(0f, lane.Length);
            point = lane.Evaluate(s, out _);
            // Ni en callejones ni en el último tramo antes de un cruce (no le daría tiempo a frenar si está en rojo).
            if (lane.LeadsToDeadEnd || lane.Length < 40f || s > lane.Length - 30f) return false;
            float d = Vector3.Distance(point, center);
            return d >= min && d <= spawnMax;
        }

        bool Crowded(Vector3 point)
        {
            foreach (var car in pool)
                if (car && car.gameObject.activeSelf && (car.transform.position - point).sqrMagnitude < 14f * 14f) return true;
            return false;
        }

        TrafficCar FreeCar()
        {
            int start = Random.Range(0, pool.Length);
            for (int i = 0; i < pool.Length; i++)
            {
                var car = pool[(start + i) % pool.Length];
                if (car && !car.gameObject.activeSelf) return car;
            }
            return null;
        }

        static bool Seen(Vector3 point, float distance)
        {
            var cam = Camera.main;
            if (!cam || distance > 220f) return false;
            var v = cam.WorldToViewportPoint(point + Vector3.up);
            return v.z > 0f && v.x > -0.15f && v.x < 1.15f && v.y > -0.15f && v.y < 1.15f;
        }
    }
}
