using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Coche del tráfico de la ciudad. Sigue los carriles de la <see cref="TrafficNetwork"/>: frena en rojo (y en ámbar si
    /// le da tiempo), para en los STOP y espera a que el cruce esté libre, gira por una curva en los cruces y elige al
    /// azar por dónde sigue. Frena ante lo que tiene delante (otros coches, el coche del jugador, el jugador y los NPC),
    /// pita si el jugador le corta el paso y, si oye tiros cerca (<see cref="NpcAlerts"/>), acelera y se salta los
    /// semáforos un rato. Si el coche del jugador lo embiste, se queda parado con los intermitentes. En los pasos a nivel
    /// cerrados (<see cref="LevelCrossing"/>) para antes de la vía.
    /// Es cinemático: no lo mueve la física. Lo saca y lo quita <see cref="TrafficPopulation"/>; el autobús lleva una
    /// ruta fija (<see cref="route"/>) y para en sus paradas.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrafficCar : MonoBehaviour
    {
        [SerializeField] float length = 5f;
        [SerializeField] float acceleration = 2.2f;
        [SerializeField] float braking = 4.5f;
        [Tooltip("Factor sobre el límite de cada calle: cada conductor va un poco más rápido o más lento.")]
        [SerializeField] float temper = 1f;
        [Tooltip("Ruta fija (nodos de la red, en bucle). Vacía: elige al azar en cada cruce.")]
        [SerializeField] int[] route = new int[0];
        [Tooltip("Paradas de la ruta fija (en el carril): para ahí unos segundos.")]
        [SerializeField] Vector3[] stops = new Vector3[0];
        [SerializeField] float stopSeconds = 8f;
        [SerializeField] TrafficNetwork network;

        const float TurnSpeed = 5f, StopLine = 0.8f, MinGap = 2.2f, ProbeRadius = 0.85f;
        static readonly int ObstacleMask = (1 << GameLayers.Vehicle) | (1 << GameLayers.Npc) | (1 << GameLayers.IgnoreRaycast);
        static readonly RaycastHit[] hits = new RaycastHit[8];
        static AudioClip hornClip, engineClip;

        Rigidbody body;
        AudioSource horn, engine;
        [SerializeField] Transform[] wheels = new Transform[0];
        [SerializeField] float wheelRadius = 0.38f;
        [Tooltip("Intermitentes: se encienden si el coche del jugador lo embiste.")]
        [SerializeField] Renderer[] hazards = new Renderer[0];

        TrafficNetwork.Lane lane, next;
        float s, speed, turnLength;
        bool turning, committed, stopDone, amberGo, holding;
        Vector3 turnFrom, turnControl, turnTo;
        float stopTimer, blockedTimer, hornCooldown, panicUntil, alertCheck, busStopS = -1f, dwell;
        int routeIndex;
        bool wrecked;

        public bool IsBus => route.Length > 1;
        public bool Wrecked => wrecked;
        /// <summary>Ha oído tiros y huye (se salta semáforos y STOP a propósito).</summary>
        public bool Panicking => Time.time < panicUntil;
        /// <summary>Estado para depurar: girando, comprometido a cruzar, sujeto en la línea.</summary>
        public string DebugState => $"giro={turning} comprometido={committed} sujeto={holding} ámbar={amberGo} v={speed:F1}";
        public float Speed => speed;
        public float BlockedSeconds => blockedTimer;

        public void Configure(TrafficNetwork net, float carLength, float driverTemper, Transform[] wheelTransforms, float radius,
            Renderer[] hazardLights = null, int[] fixedRoute = null, Vector3[] routeStops = null)
        {
            network = net;
            length = carLength;
            temper = driverTemper;
            wheels = wheelTransforms;
            wheelRadius = radius;
            hazards = hazardLights ?? new Renderer[0];
            route = fixedRoute ?? new int[0];
            stops = routeStops ?? new Vector3[0];
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (!hornClip) hornClip = ProceduralSfx.Horn();
            horn = gameObject.AddComponent<AudioSource>();
            horn.clip = hornClip;
            horn.spatialBlend = 1f;
            horn.minDistance = 6f;
            horn.maxDistance = 70f;
            horn.playOnAwake = false;

            // Motor: el mismo bucle que el coche del jugador, bajo y solo de cerca (el autobús, más grave).
            if (!engineClip) engineClip = ProceduralSfx.EngineLoop();
            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = engineClip;
            engine.loop = true;
            engine.spatialBlend = 1f;
            engine.minDistance = 3f;
            engine.maxDistance = 32f;
            engine.rolloffMode = AudioRolloffMode.Linear;
            engine.dopplerLevel = 0.4f;
            engine.volume = IsBus ? 0.3f : 0.18f;
            engine.playOnAwake = false;
        }

        void UpdateEngine()
        {
            if (!engine) return;
            bool on = !wrecked;
            if (on && !engine.isPlaying)
            {
                engine.time = Random.Range(0f, engineClip.length * 0.9f); // que no suenen todos a la vez
                engine.Play();
            }
            else if (!on && engine.isPlaying) engine.Stop();
            float rpm = VehicleMath.EngineRpm(speed, speed < 1f ? 0f : 0.5f, 4, 8f, out _);
            engine.pitch = (IsBus ? 0.55f : 0.7f) * Mathf.Lerp(0.8f, 1.9f, rpm);
        }

        /// <summary>Pone el coche en el carril <paramref name="laneIndex"/>, a <paramref name="distance"/> metros del principio.</summary>
        public void Place(int laneIndex, float distance, float initialSpeed)
        {
            lane = network.Lanes[laneIndex];
            s = Mathf.Clamp(distance, 0f, lane.Length - 0.5f);
            speed = initialSpeed;
            turning = false;
            wrecked = false;
            SetHazards(false);
            if (IsBus)
            {
                routeIndex = System.Array.IndexOf(route, lane.Data.To);
                if (routeIndex < 0) routeIndex = 0;
            }
            BeginLane();
            var p = lane.Evaluate(s, out var tangent);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(tangent));
            if (body) { body.position = p; body.rotation = transform.rotation; }
        }

        /// <summary>Pone el coche al principio de su ruta fija (el autobús).</summary>
        public void PlaceOnRoute()
        {
            if (!IsBus) return;
            int link = network.LinkBetween(route[0], route[1]);
            if (link >= 0) Place(link, 2f, 0f);
        }

        void OnEnable()
        {
            if (lane == null && IsBus && network) PlaceOnRoute();
        }

        void OnDisable()
        {
            if (turning && lane != null) network.Leave(lane.Data.To);
            turning = false;
        }

        void BeginLane()
        {
            committed = false;
            stopDone = false;
            amberGo = false;
            stopTimer = 0f;
            next = null;
            if (IsBus)
            {
                routeIndex = (routeIndex + 1) % route.Length;
                int link = network.LinkBetween(lane.Data.To, route[routeIndex]);
                if (link >= 0) next = network.Lanes[link];
                busStopS = StopOnLane();
                dwell = 0f;
            }
            else
            {
                int link = network.ChooseNext(lane, Random.value);
                if (link >= 0) next = network.Lanes[link];
            }
        }

        float StopOnLane()
        {
            float best = -1f, bestDistance = 4f;
            foreach (var stop in stops)
            {
                for (int i = 1; i < lane.Points.Length; i++)
                {
                    var a = lane.Points[i - 1];
                    var ab = lane.Points[i] - a;
                    float t = Mathf.Clamp01(Vector3.Dot(stop - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                    float d = Vector3.Distance(a + ab * t, stop);
                    if (d < bestDistance) { bestDistance = d; best = lane.Cumulative[i - 1] + ab.magnitude * t; }
                }
            }
            return best;
        }

        void FixedUpdate()
        {
            if (lane == null) return;
            float dt = Time.fixedDeltaTime;
            float now = Time.time;
            bool panic = now < panicUntil;
            if (now >= alertCheck)
            {
                alertCheck = now + 0.5f;
                if (!IsBus && NpcAlerts.Heard(transform.position, 1f, null, out _)) panicUntil = now + 12f;
            }

            float cruise = (turning ? TurnSpeed : lane.Data.SpeedLimit) * temper * (panic ? 1.45f : 1f);
            float target = wrecked ? 0f : cruise;

            holding = false;
            if (!turning && !wrecked)
            {
                float toLine = lane.Length - s - length / 2f; // del morro a la línea
                // Parada del autobús.
                if (busStopS >= 0f && s <= busStopS + 0.5f)
                {
                    target = Mathf.Min(target, TrafficMath.StoppingSpeed(busStopS - s, braking));
                    if (busStopS - s < 0.6f && speed < 0.3f)
                    {
                        dwell += dt;
                        if (dwell >= stopSeconds) busStopS = -1f;
                    }
                }
                if (!committed)
                {
                    bool mustStop = next == null && !network.IsExit(lane.Data.To);
                    switch (lane.Data.Gate)
                    {
                        case TrafficGate.Signal:
                            var signal = network.Signal(lane.Data.To);
                            if (signal && !panic)
                            {
                                bool alongX = Mathf.Abs(lane.EndDirection.x) > Mathf.Abs(lane.EndDirection.z);
                                var color = signal.VehicleColor(alongX);
                                if (color == SignalColor.Red) { mustStop = true; amberGo = false; }
                                else if (color == SignalColor.Amber)
                                {
                                    if (!amberGo && TrafficMath.StopsForAmber(toLine - StopLine, speed, braking)) mustStop = true;
                                    else amberGo = true;
                                }
                            }
                            break;
                        case TrafficGate.Stop:
                            if (!stopDone && !panic)
                            {
                                mustStop = true;
                                if (toLine < StopLine + 1.2f && speed < 0.4f) stopTimer += dt;
                                if (stopTimer > 1.2f && network.Occupancy(lane.Data.To) == 0) stopDone = true;
                            }
                            break;
                    }
                    // Paso a nivel cerrado justo al principio del siguiente tramo (una calle pegada a la vía): espera aquí.
                    if (next != null && next.RailStart >= 0f && next.RailStart < 14f && next.RailClosedAhead(0f)) mustStop = true;
                    holding = mustStop;
                    if (mustStop) target = Mathf.Min(target, TrafficMath.StoppingSpeed(toLine - StopLine, braking));
                    // Solo se compromete a cruzar quien va lanzado: el que avanza a paso de cola puede parar en seco si
                    // cambia a rojo (si no, se colaría en el cruce en rojo detrás del de delante).
                    else if (toLine < 1.5f && speed > 2f) committed = true;
                }
                // Paso a nivel cerrado en este tramo: para antes de la vía (si ya está encima, sigue para dejarla libre).
                if (lane.RailClosedAhead(s + length / 2f))
                    target = Mathf.Min(target, TrafficMath.StoppingSpeed(lane.RailStart - 1.5f - (s + length / 2f), braking));
                // Frena antes de girar.
                if (next != null && Vector3.Dot(lane.EndDirection, next.StartDirection) < 0.7f)
                    target = Mathf.Min(target, Mathf.Sqrt(TurnSpeed * TurnSpeed + 2f * braking * Mathf.Max(0f, toLine + length / 2f)));
            }

            // Lo que tiene delante.
            float look = speed * speed / (2f * braking) + 10f;
            bool blockedByPlayer = false;
            if (Probe(look, out float gap, out blockedByPlayer))
                target = Mathf.Min(target, TrafficMath.FollowSpeed(gap, MinGap, braking, cruise));

            float hardBrake = target < speed - 3f ? braking * 1.8f : braking;
            speed = TrafficMath.Approach(speed, target, acceleration * (panic ? 1.6f : 1f), hardBrake, dt);

            // Pita si el jugador le corta el paso.
            blockedTimer = speed < 0.3f && blockedByPlayer ? blockedTimer + dt : 0f;
            hornCooldown -= dt;
            if (blockedTimer > 2.5f && hornCooldown <= 0f && !wrecked)
            {
                horn.pitch = Random.Range(0.92f, 1.08f) * (IsBus ? 0.75f : 1f);
                horn.Play();
                hornCooldown = Random.Range(3f, 6f);
            }

            Advance(speed * dt);
            SpinWheels(speed * dt);
        }

        bool Probe(float distance, out float gap, out bool player)
        {
            gap = float.MaxValue;
            player = false;
            var forward = transform.forward;
            var origin = transform.position + Vector3.up * 1f + forward * (length / 2f + ProbeRadius + 0.05f);
            int count = Physics.SphereCastNonAlloc(origin, ProbeRadius, forward, hits, distance, ObstacleMask, QueryTriggerInteraction.Ignore);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.rigidbody == body || h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) { gap = 0f; found = true; continue; }
                float g = h.distance + ProbeRadius;
                if (g < gap)
                {
                    gap = g;
                    found = true;
                    var vehicle = h.collider.GetComponentInParent<VehicleController>();
                    player = h.collider.gameObject.layer == GameLayers.IgnoreRaycast || (vehicle && vehicle.HasDriver);
                }
            }
            return found;
        }

        void Advance(float delta)
        {
            s += delta;
            Vector3 position, tangent;
            if (!turning)
            {
                // Con la línea cerrada (rojo, STOP, paso a nivel) no se pasa aunque no haya podido frenar a tiempo.
                float hold = Mathf.Max(0f, lane.Length - length / 2f - StopLine);
                if (holding && s > hold)
                {
                    s = Mathf.Max(s - delta, hold);
                    speed = 0f;
                }
                if (s >= lane.Length)
                {
                    if (network.IsExit(lane.Data.To)) { gameObject.SetActive(false); return; }
                    if (next == null) { s = lane.Length; speed = 0f; }
                    else
                    {
                        s -= lane.Length;
                        turnFrom = lane.Points[lane.Points.Length - 1];
                        turnTo = next.Points[0];
                        turnControl = TrafficMath.TurnControl(turnFrom, lane.EndDirection, turnTo, next.StartDirection);
                        turnLength = 0f;
                        var prev = turnFrom;
                        for (int i = 1; i <= 8; i++)
                        {
                            var p = TrafficMath.Bezier(turnFrom, turnControl, turnTo, i / 8f);
                            turnLength += Vector3.Distance(prev, p);
                            prev = p;
                        }
                        turnLength = Mathf.Max(turnLength, 0.1f);
                        turning = true;
                        network.Enter(lane.Data.To);
                    }
                }
            }
            if (turning && s >= turnLength)
            {
                network.Leave(lane.Data.To);
                turning = false;
                s -= turnLength;
                lane = next;
                BeginLane();
            }

            if (turning)
            {
                float t = s / turnLength;
                position = TrafficMath.Bezier(turnFrom, turnControl, turnTo, t);
                tangent = TrafficMath.Bezier(turnFrom, turnControl, turnTo, Mathf.Min(1f, t + 0.05f)) - TrafficMath.Bezier(turnFrom, turnControl, turnTo, Mathf.Max(0f, t - 0.05f));
            }
            else position = lane.Evaluate(s, out tangent);
            tangent.y = 0f;
            if (tangent.sqrMagnitude < 1e-6f) tangent = transform.forward;
            body.MovePosition(position);
            body.MoveRotation(Quaternion.LookRotation(tangent.normalized));
        }

        void SpinWheels(float distance)
        {
            if (wheels == null || distance <= 0f) return;
            float degrees = distance / wheelRadius * Mathf.Rad2Deg;
            var axis = transform.right;
            foreach (var w in wheels) if (w) w.RotateAround(w.position, axis, degrees);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (wrecked || IsBus) return;
            var other = collision.rigidbody;
            if (other && !other.isKinematic && collision.relativeVelocity.magnitude > 5f)
            {
                wrecked = true;
                speed = 0f;
                SetHazards(true);
                NpcAlerts.Hit(transform.position, other.gameObject, gameObject);
            }
        }

        void SetHazards(bool on)
        {
            if (hazards == null) return;
            foreach (var r in hazards) if (r) r.enabled = on;
        }

        void Update()
        {
            UpdateEngine();
            // Intermitentes de emergencia: medio segundo encendidos y medio apagados.
            if (wrecked && hazards != null)
            {
                bool on = Mathf.Repeat(Time.time, 1f) < 0.5f;
                foreach (var r in hazards) if (r && r.enabled != on) r.enabled = on;
            }
        }
    }
}
