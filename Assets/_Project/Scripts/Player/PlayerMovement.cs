using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Movimiento a pie en tercera persona, relativo a la cámara. Realista pero arcade: arranca y frena con inercia,
    /// gira más despacio cuanto más deprisa va, esprinta gastando energía (agotado, va jadeando más lento) y se hace
    /// daño al caer de alto. No salta en el sitio: para eso está <see cref="PlayerClimb"/>. Con C se agacha
    /// (<see cref="Crouching"/>): va más despacio, ocupa menos alto y apunta mejor; esprintar lo levanta.
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
        [Tooltip("Velocidad agachado (también apuntando agachado).")]
        [SerializeField] float crouchSpeed = 1.7f;
        [Tooltip("Alto del CharacterController agachado (el de pie es el que trae el componente).")]
        [SerializeField] float crouchHeight = 1.15f;
        [SerializeField] LayerMask standMask = ~(1 << 2);
        [Tooltip("Si se deja vacío se usa Camera.main. Sin cámara, el movimiento usa los ejes del mundo.")]
        [SerializeField] Transform cameraTransform;

        CharacterController controller;
        PlayerVitals vitals;
        Health health;
        Vector3 planar, impulse;
        float verticalVelocity;
        bool wasGrounded = true;
        float standHeight, lastGroundedTime = float.NegativeInfinity;
        Vector3 standCenter;

        /// <summary>Otra habilidad controla al personaje (escalar, cobertura, muerto): aquí no se mueve.</summary>
        public bool Locked { get; set; }
        /// <summary>Apuntando: va más despacio, no esprinta y mira hacia <see cref="AimYaw"/>.</summary>
        public bool Aiming { get; set; }
        public float AimYaw { get; set; }
        public bool IsSprinting { get; private set; }
        /// <summary>Agachado por su cuenta (C). La cobertura baja agacha aparte (<see cref="PlayerCover.Crouched"/>).</summary>
        public bool Crouching { get; private set; }
        /// <summary>Alto del CharacterController de pie (para quien mire si cabe, como escalar).</summary>
        public float StandHeight => standHeight;
        /// <summary>Última vez que quiso levantarse y no había sitio encima (para el aviso del HUD).</summary>
        public float StandBlockedTime { get; private set; } = float.NegativeInfinity;
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
            standHeight = controller.height;
            standCenter = controller.center;
        }

        void OnEnable()
        {
            ResetMotion();
            ForceStand(); // al bajar del coche, de pie
        }

        /// <summary>Agacharse o levantarse. Levantarse solo si hay sitio encima; devuelve si quedó como se pidió.</summary>
        public bool SetCrouching(bool crouch)
        {
            if (crouch == Crouching) return true;
            if (!crouch && !RoomToStand())
            {
                StandBlockedTime = Time.time;
                return false;
            }
            Crouching = crouch;
            ApplyHeight();
            return true;
        }

        /// <summary>De pie sin mirar si cabe (al escalar, morir, reaparecer o subir al coche).</summary>
        public void ForceStand()
        {
            Crouching = false;
            if (controller) ApplyHeight();
        }

        bool RoomToStand()
        {
            if (!controller.enabled) return true;
            float r = controller.radius;
            var feet = transform.position;
            // Del alto agachado al de pie: si toca algo (un techo, una mesa encima), no cabe.
            return !Physics.CheckCapsule(feet + Vector3.up * (crouchHeight - r + 0.02f), feet + Vector3.up * (standHeight - r),
                r * 0.95f, standMask, QueryTriggerInteraction.Ignore);
        }

        void ApplyHeight()
        {
            float height = Crouching ? crouchHeight : standHeight;
            if (Mathf.Approximately(controller.height, height)) return;
            controller.height = height;
            controller.center = Crouching ? new Vector3(standCenter.x, height / 2f, standCenter.z) : standCenter;
        }

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
            var actions = GameInput.Actions;
            bool grounded = controller.isGrounded;
            if (grounded) lastGroundedTime = Time.time;

            // Agacharse (C) solo en el suelo (isGrounded parpadea a veces: vale haberlo tocado hace un momento);
            // levantarse, donde quepa. Esprintar también lo levanta.
            bool onFloor = Time.time - lastGroundedTime < 0.2f;
            if (!GameUi.MenuOpen && actions.Crouch.WasPressedThisFrame() && (Crouching || onFloor)) SetCrouching(!Crouching);
            if (Crouching && !Aiming && actions.Sprint.WasPressedThisFrame() && direction.sqrMagnitude > 0.1f) SetCrouching(false);
            ApplyHeight(); // la cobertura cambia el alto por su cuenta y lo deja de pie al salir

            bool wantsSprint = actions.Sprint.IsPressed() && !Aiming && !Crouching && direction.sqrMagnitude > 0.1f;
            IsSprinting = wantsSprint && (stamina == null || stamina.CanSprint);
            if (IsSprinting) stamina?.Drain(sprintStaminaPerSecond * dt);

            float speed = Aiming ? walkSpeed : IsSprinting ? sprintSpeed : runSpeed;
            if (stamina != null && stamina.Exhausted && !Aiming) speed = runSpeed * 0.75f; // agotado: jadeando
            if (Crouching) speed = Mathf.Min(speed, crouchSpeed);
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
            if (wasGrounded) lastGroundedTime = Time.time;

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
