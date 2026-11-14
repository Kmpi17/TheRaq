using System;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Coche con WheelColliders: motor, freno, dirección y freno de mano. Solo lee input cuando
    /// tiene conductor; sin conductor queda frenado.
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
        [SerializeField] float maxSteerAngle = 32f;
        [Tooltip("Ángulo máximo de giro a 30 m/s; se interpola desde maxSteerAngle según la velocidad.")]
        [SerializeField] float steerAngleAtSpeed = 10f;
        [SerializeField] Vector3 centerOfMass = new(0f, 0.4f, 0f);
        [SerializeField] Transform exitPoint;

        Rigidbody body;

        public bool HasDriver { get; private set; }
        public Transform ExitPoint => exitPoint ? exitPoint : transform;
        public float ForwardSpeed => body ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;

        /// <summary>Asigna ruedas y punto de salida (lo usa el generador de escena del editor).</summary>
        public void Configure(Wheel[] newWheels, Transform newExitPoint)
        {
            wheels = newWheels ?? Array.Empty<Wheel>();
            exitPoint = newExitPoint;
        }

        public void SetDriver(bool hasDriver) => HasDriver = hasDriver;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = centerOfMass;
        }

        void FixedUpdate()
        {
            Vector2 move = HasDriver ? GameInput.Actions.Move.ReadValue<Vector2>() : Vector2.zero;
            bool handbrake = HasDriver && GameInput.Actions.Handbrake.IsPressed();
            float speed = ForwardSpeed;
            float throttle = move.y;

            // Si el input va en contra del sentido de la marcha, frena en lugar de acelerar.
            bool braking = Mathf.Abs(speed) > 1f && Mathf.Abs(throttle) > 0.1f && Mathf.Sign(throttle) != Mathf.Sign(speed);
            float steerLimit = Mathf.Lerp(maxSteerAngle, steerAngleAtSpeed, Mathf.Abs(speed) / 30f);

            foreach (var wheel in wheels)
            {
                if (wheel.steer) wheel.collider.steerAngle = move.x * steerLimit;
                wheel.collider.motorTorque = wheel.drive && !braking ? throttle * motorTorque : 0f;

                float brake = !HasDriver || braking ? brakeTorque : 0f;
                if (handbrake && wheel.handbrake) brake = handbrakeTorque;
                wheel.collider.brakeTorque = brake;
            }
        }

        void Update()
        {
            foreach (var wheel in wheels)
            {
                if (!wheel.visual) continue;
                wheel.collider.GetWorldPose(out var position, out var rotation);
                wheel.visual.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
