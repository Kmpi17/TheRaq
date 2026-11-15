using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GTAChiraq
{
    /// <summary>
    /// Cámara orbital en tercera persona con colisión. Tiene un perfil a pie y otro en vehículo;
    /// en vehículo se recoloca sola detrás del coche si no se mueve la cámara durante un rato. Al apuntar a pie pasa
    /// suavemente a una vista al hombro (más cerca, desplazada a la derecha y con menos campo de visión); el combate
    /// le da retroceso con <see cref="AddRecoil"/> y dispara por <see cref="AimRay"/> (el centro de la pantalla).
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
        [SerializeField] Profile aim = new() { distance = 1.9f, height = 1.55f, minPitch = -50f, maxPitch = 60f };
        [SerializeField] float aimShoulder = 0.6f;
        [SerializeField] float aimFieldOfView = 46f;
        [SerializeField] float aimBlendSpeed = 7f;
        [SerializeField] float aimSensitivity = 0.6f;
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float stickSpeed = 180f;
        [SerializeField] float autoAlignDelay = 1.5f;
        [SerializeField] float autoAlignSpeed = 120f;
        [SerializeField] float collisionRadius = 0.25f;
        [Tooltip("Por defecto ignora al jugador (Ignore Raycast) y a los coches (Vehiculo).")]
        [SerializeField] LayerMask collisionMask = ~((1 << GameLayers.IgnoreRaycast) | (1 << GameLayers.Vehicle));

        float yaw;
        float pitch = 15f;
        float lastLookTime = float.NegativeInfinity;
        Mode mode = Mode.OnFoot;
        bool aiming;
        float aimBlend;
        float baseFieldOfView = -1f;
        Camera cameraComponent;

        public float Yaw => yaw;
        public float AimBlend => aimBlend;
        /// <summary>Rayo por el centro de la pantalla (donde está la mira).</summary>
        public Ray AimRay => new(transform.position, transform.forward);

        public void SetAiming(bool on) => aiming = on && mode == Mode.OnFoot;

        /// <summary>Sube la vista (grados) y la desvía un poco de lado: el retroceso de un disparo.</summary>
        public void AddRecoil(float up, float side)
        {
            pitch -= up;
            yaw += side;
        }

        /// <summary>Coloca la cámara detrás de ese yaw (al salir de un ascensor, por ejemplo).</summary>
        public void SetYaw(float newYaw) => yaw = newYaw;

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

            aimBlend = Mathf.MoveTowards(aimBlend, aiming && mode == Mode.OnFoot ? 1f : 0f, aimBlendSpeed * Time.deltaTime);
            var profile = mode == Mode.Vehicle ? vehicle : Blend(onFoot, aim, Mathf.SmoothStep(0f, 1f, aimBlend));
            var input = GameInput.Actions;
            Vector2 look = GameUi.MenuOpen ? Vector2.zero
                : (input.LookMouse.ReadValue<Vector2>() * mouseSensitivity + input.LookStick.ReadValue<Vector2>() * (stickSpeed * Time.deltaTime))
                  * Mathf.Lerp(1f, aimSensitivity, aimBlend);
            if (look.sqrMagnitude > 0.0001f) lastLookTime = Time.time;

            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, profile.minPitch, profile.maxPitch);

            if (mode == Mode.Vehicle && Time.time - lastLookTime > autoAlignDelay)
                yaw = Mathf.MoveTowardsAngle(yaw, target.eulerAngles.y, autoAlignSpeed * Time.deltaTime);

            Vector3 pivot = target.position + Vector3.up * profile.height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Al hombro: el pivote se aparta a la derecha, sin meterse en una pared.
            float shoulder = aimShoulder * Mathf.SmoothStep(0f, 1f, aimBlend);
            if (shoulder > 0.001f)
            {
                var right = rotation * Vector3.right;
                if (Physics.SphereCast(pivot, collisionRadius, right, out var sideHit, shoulder, collisionMask, QueryTriggerInteraction.Ignore))
                    shoulder = sideHit.distance;
                pivot += right * shoulder;
            }
            if (!cameraComponent) cameraComponent = GetComponent<Camera>();
            if (cameraComponent)
            {
                if (baseFieldOfView < 0f) baseFieldOfView = cameraComponent.fieldOfView;
                cameraComponent.fieldOfView = Mathf.Lerp(baseFieldOfView, aimFieldOfView, Mathf.SmoothStep(0f, 1f, aimBlend));
            }

            float distance = profile.distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, profile.distance, collisionMask, QueryTriggerInteraction.Ignore))
                distance = hit.distance;

            transform.SetPositionAndRotation(pivot + back * distance, rotation);
        }

        static Profile Blend(Profile a, Profile b, float t) => new()
        {
            distance = Mathf.Lerp(a.distance, b.distance, t),
            height = Mathf.Lerp(a.height, b.height, t),
            minPitch = Mathf.Lerp(a.minPitch, b.minPitch, t),
            maxPitch = Mathf.Lerp(a.maxPitch, b.maxPitch, t),
        };

        static void HandleCursor()
        {
            if (GameUi.MenuOpen) return; // el inventario abierto libera el cursor
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
