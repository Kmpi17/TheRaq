using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GTAChiraq
{
    /// <summary>
    /// Cámara orbital en tercera persona con colisión. Tiene un perfil a pie y otro en vehículo;
    /// en vehículo se recoloca sola detrás del coche si no se mueve la cámara durante un rato.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public enum Mode { OnFoot, Vehicle }

        [Serializable]
        public struct Profile
        {
            public float distance;
            public float height;
            public float minPitch;
            public float maxPitch;
        }

        [SerializeField] Transform target;
        [SerializeField] Profile onFoot = new() { distance = 4f, height = 1.6f, minPitch = -30f, maxPitch = 70f };
        [SerializeField] Profile vehicle = new() { distance = 7.5f, height = 2f, minPitch = -10f, maxPitch = 60f };
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float stickSpeed = 180f;
        [SerializeField] float autoAlignDelay = 1.5f;
        [SerializeField] float autoAlignSpeed = 120f;
        [SerializeField] float collisionRadius = 0.25f;
        [Tooltip("Por defecto ignora la capa 'Ignore Raycast', donde están el jugador y los coches.")]
        [SerializeField] LayerMask collisionMask = ~(1 << 2);

        float yaw;
        float pitch = 15f;
        float lastLookTime = float.NegativeInfinity;
        Mode mode = Mode.OnFoot;

        public void SetTarget(Transform newTarget, Mode newMode)
        {
            target = newTarget;
            mode = newMode;
            lastLookTime = float.NegativeInfinity;
        }

        void Start()
        {
            if (target) yaw = target.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            HandleCursor();
            if (!target) return;

            var profile = mode == Mode.Vehicle ? vehicle : onFoot;
            var input = GameInput.Actions;
            Vector2 look = input.LookMouse.ReadValue<Vector2>() * mouseSensitivity
                         + input.LookStick.ReadValue<Vector2>() * (stickSpeed * Time.deltaTime);
            if (look.sqrMagnitude > 0.0001f) lastLookTime = Time.time;

            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, profile.minPitch, profile.maxPitch);

            if (mode == Mode.Vehicle && Time.time - lastLookTime > autoAlignDelay)
                yaw = Mathf.MoveTowardsAngle(yaw, target.eulerAngles.y, autoAlignSpeed * Time.deltaTime);

            Vector3 pivot = target.position + Vector3.up * profile.height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            float distance = profile.distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, profile.distance, collisionMask, QueryTriggerInteraction.Ignore))
                distance = hit.distance;

            transform.SetPositionAndRotation(pivot + back * distance, rotation);
        }

        static void HandleCursor()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
