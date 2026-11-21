using System;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Coche con WheelColliders: motor, freno, dirección y freno de mano, con ayudas de conducción tipo arcade (volante
    /// suavizado, más agarre, ayuda al giro, barras estabilizadoras y apoyo aerodinámico) y sonido (motor, derrape,
    /// golpes y claxon). Solo lee input cuando tiene conductor; sin conductor queda frenado. Rompe lo rompible
    /// (<see cref="Breakable"/>: vallas, señales, farolas) al chocar sin apenas frenar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [Serializable]
        public sealed class Wheel
        {
            public WheelCollider collider;
            [Tooltip("Pivote visual; su eje X debe ser el eje de giro de la rueda.")]
            public Transform visual;
            public bool steer;
            public bool drive;
            public bool handbrake;
        }

        [SerializeField] Wheel[] wheels = Array.Empty<Wheel>();
        [SerializeField] float motorTorque = 1800f;
        [SerializeField] float brakeTorque = 3500f;
        [SerializeField] float handbrakeTorque = 6000f;
        [SerializeField] Vector3 centerOfMass = new(0f, 0.4f, 0f);
        [SerializeField] Transform exitPoint;

        [Header("Dirección")]
        [SerializeField] float maxSteerAngle = 36f;
        [Tooltip("Ángulo máximo a steerFadeSpeed m/s; se interpola desde maxSteerAngle según la velocidad.")]
        [SerializeField] float steerAngleAtSpeed = 15f;
        [SerializeField] float steerFadeSpeed = 32f;
        [Tooltip("Lo que gira el volante por segundo (1 = a tope) y lo que vuelve al centro.")]
        [SerializeField] float steerRate = 3.5f;
        [SerializeField] float steerReturnRate = 6f;

        [Header("Agarre y ayudas")]
        [SerializeField] float forwardGrip = 1.6f;
        [SerializeField] float frontGrip = 2.1f;
        [SerializeField] float rearGrip = 1.9f;
        [Tooltip("Agarre lateral de las traseras con el freno de mano (para derrapar).")]
        [SerializeField] float handbrakeRearGrip = 0.7f;
        [Tooltip("Cuánto empuja hacia el giro que corresponde al volante (modelo de bicicleta).")]
        [SerializeField] float yawAssist = 5f;
        [SerializeField] float maxYawRate = 2.3f;
        [Tooltip("Aceleración lateral máxima que permite la ayuda (m/s²; 14 ≈ 1.4 g, de arcade; un coche de calle real da ~8–9).")]
        [SerializeField] float maxLateralAccel = 14f;
        [Tooltip("Frena el deslizamiento lateral (1/s).")]
        [SerializeField] float lateralGrip = 2.2f;
        [SerializeField] float downforce = 0.005f;
        [SerializeField] float antiRoll = 7000f;

        Rigidbody body;
        float steer, wheelbase = 2.8f, throttleNow;
        Vector3 lastVelocity, lastAngular;
        Wheel[] frontPair, rearPair;
        bool handbrakeNow;

        AudioSource engine, skid, impacts, horn;
        static AudioClip engineClip, skidClip, crashClip, hornClip, doorClip;
        float engineLevel, lastImpact;

        /// <summary>Todos los coches activos, para buscar el más cercano sin recorrer la escena.</summary>
        public static readonly System.Collections.Generic.List<VehicleController> All = new();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool HasDriver { get; private set; }
        public Transform ExitPoint => exitPoint ? exitPoint : transform;
        public float ForwardSpeed => body ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
        /// <summary>Posición del volante, de −1 a 1 (suavizada).</summary>
        public float Steer => steer;
        /// <summary>Lo que más patina una rueda ahora (0 = nada), para el sonido y los tests.</summary>
        public float Slip { get; private set; }

        /// <summary>Asigna ruedas y punto de salida (lo usa el generador de escena del editor).</summary>
        public void Configure(Wheel[] newWheels, Transform newExitPoint)
        {
            wheels = newWheels ?? Array.Empty<Wheel>();
            exitPoint = newExitPoint;
            if (body) SetupWheels();
        }

        public void SetDriver(bool hasDriver)
        {
            if (hasDriver != HasDriver && Application.isPlaying) PlayDoor();
            HasDriver = hasDriver;
        }

        bool autoInput, autoHandbrake;
        Vector2 autoMove;

        /// <summary>Conduce sin teclado (tests y, más adelante, conductores NPC): acelerador en y, dirección en x.</summary>
        public void SetAutoInput(Vector2 move, bool handbrake = false)
        {
            autoInput = true;
            autoMove = move;
            autoHandbrake = handbrake;
        }

        public void ClearAutoInput() => autoInput = false;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = centerOfMass;
            SetupWheels();
            SetupAudio();
        }

        void SetupWheels()
        {
            // Batalla y parejas para las barras estabilizadoras.
            float front = float.MinValue, rear = float.MaxValue;
            var f = new System.Collections.Generic.List<Wheel>();
            var r = new System.Collections.Generic.List<Wheel>();
            foreach (var w in wheels)
            {
                if (!w?.collider) continue;
                float z = transform.InverseTransformPoint(w.collider.transform.position).z;
                front = Mathf.Max(front, z);
                rear = Mathf.Min(rear, z);
                (w.steer ? f : r).Add(w);
                SetGrip(w, w.steer ? frontGrip : rearGrip);
            }
            if (front > rear) wheelbase = front - rear;
            frontPair = f.Count == 2 ? f.ToArray() : null;
            rearPair = r.Count == 2 ? r.ToArray() : null;
        }

        void SetGrip(Wheel w, float side)
        {
            var fw = w.collider.forwardFriction;
            fw.stiffness = forwardGrip;
            w.collider.forwardFriction = fw;
            var sw = w.collider.sidewaysFriction;
            sw.stiffness = side;
            w.collider.sidewaysFriction = sw;
        }

        void FixedUpdate()
        {
            // La velocidad de antes de los choques de este paso: si se rompe algo, se recupera (ver OnCollisionEnter).
            lastVelocity = body.linearVelocity;
            lastAngular = body.angularVelocity;

            bool driven = autoInput || HasDriver;
            Vector2 move = autoInput ? autoMove : HasDriver ? GameInput.Actions.Move.ReadValue<Vector2>() : Vector2.zero;
            bool handbrake = autoInput ? autoHandbrake : HasDriver && GameInput.Actions.Handbrake.IsPressed();
            float speed = ForwardSpeed;
            float throttle = move.y;
            throttleNow = throttle;
            float dt = Time.fixedDeltaTime;

            // Si el input va en contra del sentido de la marcha, frena en lugar de acelerar.
            bool braking = Mathf.Abs(speed) > 1f && Mathf.Abs(throttle) > 0.1f && Mathf.Sign(throttle) != Mathf.Sign(speed);
            steer = VehicleMath.SmoothSteer(steer, Mathf.Clamp(move.x, -1f, 1f), dt, steerRate, steerReturnRate);
            float angle = steer * VehicleMath.SteerLimit(speed, maxSteerAngle, steerAngleAtSpeed, steerFadeSpeed);

            if (handbrake != handbrakeNow)
            {
                handbrakeNow = handbrake;
                foreach (var w in wheels) if (w.handbrake) SetGrip(w, handbrake ? handbrakeRearGrip : rearGrip);
            }

            bool grounded = false;
            float slip = 0f;
            foreach (var wheel in wheels)
            {
                if (wheel.steer) wheel.collider.steerAngle = angle;
                wheel.collider.motorTorque = wheel.drive && !braking ? throttle * motorTorque : 0f;

                float brake = !driven || braking ? brakeTorque : 0f;
                if (handbrake && wheel.handbrake) brake = handbrakeTorque;
                wheel.collider.brakeTorque = brake;

                if (wheel.collider.GetGroundHit(out var hit))
                {
                    grounded = true;
                    slip = Mathf.Max(slip, Mathf.Abs(hit.sidewaysSlip), Mathf.Abs(hit.forwardSlip) * 0.6f);
                }
            }
            Slip = slip;

            if (!grounded) return;
            var up = transform.up;

            // Ayuda al giro: lleva la rotación hacia la que pide el volante (sin ella, el coche se va de morro).
            if (driven && !handbrake)
            {
                float target = VehicleMath.TargetYawRate(speed, angle, wheelbase, maxYawRate, maxLateralAccel);
                float current = Vector3.Dot(body.angularVelocity, up);
                float fade = Mathf.Clamp01(Mathf.Abs(speed) / 3f);
                body.AddTorque(up * ((target - current) * yawAssist * fade), ForceMode.Acceleration);
            }

            // Agarre lateral: frena el deslizamiento de lado (con el freno de mano, poco, para derrapar).
            float lateral = Vector3.Dot(body.linearVelocity, transform.right);
            body.AddForce(-transform.right * (lateral * (handbrake ? lateralGrip * 0.15f : lateralGrip)), ForceMode.Acceleration);

            // Apoyo aerodinámico: más pegado al suelo cuanto más rápido.
            body.AddForce(-up * (downforce * speed * speed), ForceMode.Acceleration);

            // Barras estabilizadoras: menos balanceo en las curvas (y menos vuelcos).
            if (frontPair != null) AntiRoll(frontPair[0].collider, frontPair[1].collider);
            if (rearPair != null) AntiRoll(rearPair[0].collider, rearPair[1].collider);
        }

        void AntiRoll(WheelCollider a, WheelCollider b)
        {
            float ta = 1f, tb = 1f;
            bool ga = a.GetGroundHit(out var ha), gb = b.GetGroundHit(out var hb);
            if (ga) ta = (-a.transform.InverseTransformPoint(ha.point).y - a.radius) / a.suspensionDistance;
            if (gb) tb = (-b.transform.InverseTransformPoint(hb.point).y - b.radius) / b.suspensionDistance;
            float force = (ta - tb) * antiRoll;
            if (ga) body.AddForceAtPosition(a.transform.up * -force, a.transform.position);
            if (gb) body.AddForceAtPosition(b.transform.up * force, b.transform.position);
        }

        void Update()
        {
            foreach (var wheel in wheels)
            {
                if (!wheel.visual) continue;
                wheel.collider.GetWorldPose(out var position, out var rotation);
                wheel.visual.SetPositionAndRotation(position, rotation);
            }
            UpdateAudio();
        }

        // ---------- Choques y cosas que se rompen ----------

        void OnCollisionEnter(Collision collision)
        {
            var breakable = collision.collider.GetComponentInParent<Breakable>();
            if (breakable && breakable.TryBreak(lastVelocity, collision.GetContact(0).point, GetComponentsInChildren<Collider>()))
            {
                // Lo atraviesa casi sin frenar, como en las películas.
                body.linearVelocity = lastVelocity * breakable.SpeedKept;
                body.angularVelocity = lastAngular;
                Impact(collision.relativeVelocity.magnitude * 0.35f);
                return;
            }
            Impact(collision.relativeVelocity.magnitude);
        }

        void Impact(float speed)
        {
            if (speed < 2.5f || !impacts || Time.time - lastImpact < 0.15f) return;
            lastImpact = Time.time;
            impacts.pitch = UnityEngine.Random.Range(0.85f, 1.1f);
            impacts.PlayOneShot(crashClip, Mathf.Clamp01(speed / 14f));
        }

        // ---------- Sonido ----------

        void SetupAudio()
        {
            if (!engineClip)
            {
                engineClip = ProceduralSfx.EngineLoop();
                skidClip = ProceduralSfx.SkidLoop();
                crashClip = ProceduralSfx.Crash();
                hornClip = ProceduralSfx.HornLoop();
                doorClip = ProceduralSfx.CarDoor();
            }
            engine = Source(engineClip, loop: true, minDistance: 4f, maxDistance: 60f);
            skid = Source(skidClip, loop: true, minDistance: 4f, maxDistance: 50f);
            impacts = Source(null, loop: false, minDistance: 5f, maxDistance: 70f);
            horn = Source(hornClip, loop: true, minDistance: 6f, maxDistance: 90f);
        }

        AudioSource Source(AudioClip clip, bool loop, float minDistance, float maxDistance)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.dopplerLevel = 0f;
            s.volume = 0f;
            return s;
        }

        void PlayDoor()
        {
            if (impacts && doorClip) impacts.PlayOneShot(doorClip, 0.8f);
        }

        void UpdateAudio()
        {
            if (!engine) return;
            bool on = HasDriver || autoInput;
            float dt = Time.deltaTime;

            // Motor: arranca con el conductor y se apaga al bajar; el tono sigue el régimen de una caja automática.
            engineLevel = Mathf.MoveTowards(engineLevel, on ? 1f : 0f, dt * (on ? 3f : 1.5f));
            if (engineLevel > 0.01f)
            {
                if (!engine.isPlaying) engine.Play();
                float rpm = VehicleMath.EngineRpm(ForwardSpeed, throttleNow, 4, 9f, out _);
                engine.pitch = Mathf.Lerp(0.55f, 2.1f, rpm);
                engine.volume = engineLevel * (0.22f + 0.3f * Mathf.Abs(throttleNow) + 0.1f * rpm);
            }
            else if (engine.isPlaying) engine.Stop();

            // Derrape: según lo que patinan las ruedas (y si se mueve).
            float skidTarget = Mathf.Clamp01((Slip - 0.3f) / 0.6f) * Mathf.Clamp01(body.linearVelocity.magnitude / 4f);
            skid.volume = Mathf.MoveTowards(skid.volume, skidTarget * 0.55f, dt * 4f);
            if (skid.volume > 0.01f) { if (!skid.isPlaying) skid.Play(); }
            else if (skid.isPlaying) skid.Stop();

            // Claxon (H).
            bool honk = HasDriver && GameInput.Actions.Horn.IsPressed();
            horn.volume = honk ? 0.7f : 0f;
            if (honk && !horn.isPlaying) horn.Play();
            else if (!honk && horn.isPlaying) horn.Stop();
        }
    }
}
