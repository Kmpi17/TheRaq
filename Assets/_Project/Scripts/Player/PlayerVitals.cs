using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Salud y energía del jugador. La energía (<see cref="StaminaPool"/>) se gasta al esprintar, escalar y pegar.
    /// La vida baja se recupera sola hasta el 25 % tras 8 s sin recibir daño. Al morir, el personaje cae, pierde
    /// el control y a los pocos segundos reaparece en su punto de salida ("hospitalizado"), pagando la factura.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float respawnSeconds = 5f;
        [SerializeField] float regenDelay = 8f;
        [SerializeField] float regenUpTo = 0.25f;
        [SerializeField] float regenPerSecond = 4f;
        [SerializeField] int hospitalBill = 100;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        PlayerMovement movement;
        PlayerCover cover;
        CharacterController controller;
        Transform model;

        public Health Health { get; private set; }
        public StaminaPool Stamina { get; private set; }
        public bool Dead => Health.IsDead;
        public float DeathTime { get; private set; }
        public float RespawnSeconds => respawnSeconds;

        void Awake()
        {
            Health = GetComponent<Health>();
            Stamina = new StaminaPool(maxStamina);
            movement = GetComponent<PlayerMovement>();
            cover = GetComponent<PlayerCover>();
            controller = GetComponent<CharacterController>();
            model = transform.Find("Modelo");
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            Health.Died += OnDied;
        }

        void Update()
        {
            if (Dead)
            {
                if (Time.time - DeathTime > respawnSeconds) Respawn();
                return;
            }
            Stamina.Tick(Time.deltaTime);
            var pool = Health.Pool;
            if (Time.time - Health.LastDamageTime > regenDelay && pool.Current < pool.Max * regenUpTo)
                Health.Heal(Mathf.Min(regenPerSecond * Time.deltaTime, pool.Max * regenUpTo - pool.Current));
        }

        void OnDied()
        {
            DeathTime = Time.time;
            if (cover) cover.Exit();
            if (movement) movement.Locked = true;
            if (model) model.localRotation = Quaternion.Euler(-88f, 0f, 0f); // tumbado boca arriba
        }

        public void Respawn()
        {
            if (controller) controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (controller) controller.enabled = true;
            Health.Revive();
            Stamina.Restore(Stamina.Max);
            if (model) model.localRotation = Quaternion.identity;
            if (movement)
            {
                movement.ResetMotion();
                movement.Locked = false;
            }
            var inventory = GetComponent<PlayerInventory>();
            if (inventory) inventory.SpendMoney(Mathf.Min(hospitalBill, inventory.Money));
        }
    }
}
