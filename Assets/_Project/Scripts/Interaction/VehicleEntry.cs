using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Va en el jugador. Con Interact (F) entra al coche más cercano o sale del actual.
    /// Al conducir, el jugador se emparenta al coche y se desactivan su control y sus renderers.
    /// </summary>
    public sealed class VehicleEntry : MonoBehaviour
    {
        [SerializeField] float enterRange = 3.5f;
        [SerializeField] ThirdPersonCamera followCamera;

        public VehicleController CurrentVehicle { get; private set; }
        public bool IsDriving => CurrentVehicle != null;

        void Awake()
        {
            if (!followCamera) followCamera = FindAnyObjectByType<ThirdPersonCamera>();
        }

        void Update()
        {
            if (!GameInput.Actions.Interact.WasPressedThisFrame()) return;

            if (IsDriving)
            {
                ExitVehicle();
                return;
            }

            var vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            var nearest = FindNearest(transform.position, vehicles, enterRange);
            if (nearest) EnterVehicle(nearest);
        }

        /// <summary>Vehículo libre más cercano dentro de <paramref name="range"/>, o null.</summary>
        public static VehicleController FindNearest(Vector3 from, IEnumerable<VehicleController> candidates, float range)
        {
            VehicleController best = null;
            float bestSqr = range * range;
            foreach (var vehicle in candidates)
            {
                if (!vehicle || vehicle.HasDriver) continue;
                float sqr = (vehicle.transform.position - from).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = vehicle;
                }
            }
            return best;
        }

        public void EnterVehicle(VehicleController vehicle)
        {
            if (IsDriving || !vehicle || vehicle.HasDriver) return;

            CurrentVehicle = vehicle;
            vehicle.SetDriver(true);
            SetOnFoot(false);
            transform.SetParent(vehicle.transform, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (followCamera) followCamera.SetTarget(vehicle.transform, ThirdPersonCamera.Mode.Vehicle);
        }

        public void ExitVehicle()
        {
            if (!IsDriving) return;

            var vehicle = CurrentVehicle;
            CurrentVehicle = null;
            vehicle.SetDriver(false);

            transform.SetParent(null);
            var exit = vehicle.ExitPoint;
            transform.SetPositionAndRotation(exit.position, Quaternion.Euler(0f, exit.eulerAngles.y, 0f));
            SetOnFoot(true);

            if (followCamera) followCamera.SetTarget(transform, ThirdPersonCamera.Mode.OnFoot);
        }

        void SetOnFoot(bool onFoot)
        {
            if (TryGetComponent(out CharacterController controller)) controller.enabled = onFoot;
            if (TryGetComponent(out PlayerMovement movement)) movement.enabled = onFoot;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = onFoot;
        }
    }
}
