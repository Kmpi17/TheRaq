using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Movimiento a pie en tercera persona, relativo a la cámara. Realista pero arcade: arranca y frena con inercia,
    /// gira más despacio cuanto más deprisa va, esprinta gastando energía (agotado, va jadeando más lento) y se hace
    /// daño al caer de alto. No salta en el sitio: para eso está <see cref="PlayerClimb"/>.
    /// Otras habilidades (escalar, cobertura, muerte) toman el control con <see cref="Locked"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Tooltip("Velocidad apuntando.")]
        [SerializeField] float walkSpeed = 2.2f;
        [SerializeField] float runSpeed = 4.4f;
        [SerializeField] float sprintSpeed = 7.2f;
        [SerializeField] float acceleration = 16f;
        [SerializeField] float deceleration = 24f;
        [Tooltip("Aceleración por encima de la carrera normal (el esprint tarda en coger velocidad).")]
        [SerializeField] float sprintAcceleration = 6f;
        [SerializeField] float airControl = 0.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float fastTurn = 900f;
        [SerializeField] float slowTurn = 280f;
        [SerializeField] float sprintStaminaPerSecond = 12f;
        [Tooltip("Si se deja vacío se usa Camera.main. Sin cámara, el movimiento usa los ejes del mundo.")]
        [SerializeField] Transform cameraTransform;

        CharacterController controller;
        PlayerVitals vitals;
        Health health;
        Vector3 planar, impulse;
        float verticalVelocity;
        bool wasGrounded = true;

        /// <summary>Otra habilidad controla al personaje (escalar, cobertura, muerto): aquí no se mueve.</summary>
        public bool Locked { get; set; }
        /// <summary>Apuntando: va más despacio, no esprinta y mira hacia <see cref="AimYaw"/>.</summary>
        public bool Aiming { get; set; }
        public float AimYaw { get; set; }
        public bool IsSprinting { get; private set; }
        public Vector3 PlanarVelocity => planar;
        public float CameraYaw => cameraTransform ? cameraTransform.eulerAngles.y : 0f;

        /// <summary>Dirección pedida por el jugador (horizontal, relativa a la cámara, longitud ≤ 1).</summary>
        public Vector3 InputDirection => GameUi.MenuOpen ? Vector3.zero
            : MovementMath.CameraRelative(GameInput.Actions.Move.ReadValue<Vector2>(), CameraYaw);

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            vitals = GetComponent<PlayerVitals>();
            health = GetComponent<Health>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        }

        void OnEnable() => ResetMotion();

        /// <summary>Empujón (un puñetazo, un golpe recibido): se suma a la velocidad y se apaga solo.</summary>
        public void AddImpulse(Vector3 velocity) => impulse += new Vector3(velocity.x, 0f, velocity.z);

        public void SetPlanarVelocity(Vector3 velocity) => planar = new Vector3(velocity.x, 0f, velocity.z);

        public void ResetMotion()
        {
            planar = impulse = Vector3.zero;
            verticalVelocity = 0f;
            wasGrounded = true;
        }

        void Update()
        {
            if (Locked || !controller.enabled)
            {
                IsSprinting = false;
                return;
            }

            float dt = Time.deltaTime;
            var direction = InputDirection;
            var stamina = vitals ? vitals.Stamina : null;
            bool wantsSprint = GameInput.Actions.Sprint.IsPressed() && !Aiming && direction.sqrMagnitude > 0.1f;
            IsSprinting = wantsSprint && (stamina == null || stamina.CanSprint);
            if (IsSprinting) stamina?.Drain(sprintStaminaPerSecond * dt);

            float speed = Aiming ? walkSpeed : IsSprinting ? sprintSpeed : runSpeed;
            if (stamina != null && stamina.Exhausted && !Aiming) speed = runSpeed * 0.75f; // agotado: jadeando
            bool grounded = controller.isGrounded;
            float control = grounded ? 1f : airControl;
            float accel = planar.magnitude > runSpeed ? sprintAcceleration : acceleration;
            planar = MovementMath.Accelerate(planar, direction * speed, accel * control, deceleration * control, dt);
            impulse *= Mathf.Exp(-8f * dt);

            float vy = verticalVelocity + gravity * dt;
            controller.Move((planar + impulse + Vector3.up * vy) * dt);
            if (controller.isGrounded)
            {
                if (!wasGrounded && vy < 0f) Land(-vy);
                if (vy < -2f) vy = -2f; // pequeña fuerza hacia abajo para que isGrounded no parpadee en rampas
            }
            verticalVelocity = vy;
            wasGrounded = controller.isGrounded;

            if (Aiming)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, AimYaw, 0f), 1080f * dt);
            else if (direction.sqrMagnitude > 0.001f)
            {
                float turn = MovementMath.TurnSpeed(planar.magnitude, sprintSpeed, slowTurn, fastTurn);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turn * dt);
            }
        }

        void Land(float impactSpeed)
        {
            float damage = MovementMath.FallDamage(impactSpeed);
            if (damage <= 0f) return;
            planar *= 0.3f; // aterrizaje duro: se para en seco
            if (health) health.TakeDamage(new DamageInfo(damage, DamageKind.Fall, transform.position, Vector3.down, null));
        }
    }
}
