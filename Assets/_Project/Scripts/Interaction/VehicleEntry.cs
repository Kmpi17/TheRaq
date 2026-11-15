using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Va en el jugador. Con Interact (F) entra al coche más cercano o sale del actual.
    /// Si el coche no es suyo (<see cref="VehicleLock"/> cerrado) hay que forzar la puerta manteniendo F; al soltar se
    /// pierde lo forzado. Al conducir, el jugador se emparenta al coche y se desactivan su control y sus renderers.
    /// </summary>
    public sealed class VehicleEntry : MonoBehaviour
    {
        [SerializeField] float enterRange = 3.5f;
        [SerializeField] ThirdPersonCamera followCamera;

        PlayerMovement movement;
        PlayerVitals vitals;
        PlayerInteractor interactor;
        VehicleLock forcing;

        public VehicleController CurrentVehicle { get; private set; }
        public bool IsDriving => CurrentVehicle != null;
        /// <summary>Coche libre al alcance (para el aviso del HUD), o null.</summary>
        public VehicleController Nearby { get; private set; }
        /// <summary>Forzando la puerta de un coche ajeno.</summary>
        public bool Forcing => forcing != null;
        public float ForceProgress => forcing ? forcing.Progress : 0f;

        void Awake()
        {
            if (!followCamera) followCamera = FindAnyObjectByType<ThirdPersonCamera>();
            movement = GetComponent<PlayerMovement>();
            vitals = GetComponent<PlayerVitals>();
            interactor = GetComponent<PlayerInteractor>();
        }

        void Update()
        {
            var actions = GameInput.Actions;
            if (forcing)
            {
                TickForcing();
                return;
            }

            Nearby = null;
            if (GameUi.MenuOpen) return;
            if (IsDriving)
            {
                if (actions.Interact.WasPressedThisFrame()) ExitVehicle();
                return;
            }
            // Escalando, en cobertura o muerto no se sube al coche.
            if (movement && movement.Locked) return;
            // Con un ascensor, armario o caja fuerte a mano, F es para eso.
            if (interactor && (interactor.Focused || interactor.Teleporting || interactor.UsedFrame == Time.frameCount)) return;
            if (MenuInteractable.LastClosedFrame == Time.frameCount) return;

            Nearby = FindNearest(transform.position, VehicleController.All, enterRange);
            if (!Nearby || !actions.Interact.WasPressedThisFrame()) return;
            if (Nearby.TryGetComponent(out VehicleLock carLock) && carLock.Locked) StartForcing(carLock);
            else EnterVehicle(Nearby);
        }

        void StartForcing(VehicleLock carLock)
        {
            forcing = carLock;
            carLock.ResetForce();
            if (movement)
            {
                movement.Locked = true;
                movement.ResetMotion();
            }
            var toCar = carLock.transform.position - transform.position;
            toCar.y = 0f;
            if (toCar.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toCar);
        }

        void TickForcing()
        {
            // Morir, abrir el inventario, soltar F o que alguien se lleve el coche cortan el forcejeo.
            bool dead = vitals && vitals.Dead;
            var car = forcing.GetComponent<VehicleController>();
            if (dead || GameUi.MenuOpen || !GameInput.Actions.Interact.IsPressed() || !car || car.HasDriver)
            {
                StopForcing(unlockMovement: !dead);
                return;
            }
            if (!forcing.Force(Time.deltaTime)) return;
            StopForcing(unlockMovement: true);
            EnterVehicle(car);
        }

        void StopForcing(bool unlockMovement)
        {
            if (forcing) forcing.ResetForce();
            forcing = null;
            if (unlockMovement && movement) movement.Locked = false;
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
            Nearby = null;
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
            if (movement || TryGetComponent(out movement)) movement.enabled = onFoot;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = onFoot;
        }
    }
}
