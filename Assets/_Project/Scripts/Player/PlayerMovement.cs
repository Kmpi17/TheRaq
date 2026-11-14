using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Movimiento a pie en tercera persona: andar, correr, saltar y gravedad, relativo a la cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7.5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float turnSpeed = 720f;
        [Tooltip("Si se deja vacío se usa Camera.main. Sin cámara, el movimiento usa los ejes del mundo.")]
        [SerializeField] Transform cameraTransform;

        CharacterController controller;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        }

        void OnEnable() => verticalVelocity = 0f;

        void Update()
        {
            var input = GameInput.Actions;
            float cameraYaw = cameraTransform ? cameraTransform.eulerAngles.y : 0f;
            Vector3 direction = MovementMath.CameraRelative(input.Move.ReadValue<Vector2>(), cameraYaw);
            float speed = input.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

            if (controller.isGrounded)
            {
                // Pequeña fuerza hacia abajo para que isGrounded no parpadee en rampas.
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (input.Jump.WasPressedThisFrame()) verticalVelocity = MovementMath.JumpVelocity(jumpHeight, gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }
}
