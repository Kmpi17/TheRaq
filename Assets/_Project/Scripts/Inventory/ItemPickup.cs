using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Objeto tirado en el suelo: gira y flota, y el jugador lo recoge al pasar por encima (a pie). Si no cabe entero,
    /// se queda lo que sobra. <see cref="Spawn"/> crea uno en tiempo de juego (al tirar algo del inventario).
    /// </summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] string itemId = ItemCatalog.Ammo9mm;
        [SerializeField] int count = 1;
        [SerializeField] float radius = 1.1f;

        PlayerInventory inventory;
        CharacterController playerController;
        Vector3 basePosition;
        float armedAt;

        public string ItemId => itemId;
        public int Count => count;

        public void Configure(string id, int amount)
        {
            itemId = id;
            count = amount;
        }

        void Start()
        {
            basePosition = transform.position;
            var player = GameObject.FindWithTag("Player");
            if (player)
            {
                inventory = player.GetComponent<PlayerInventory>();
                playerController = player.GetComponent<CharacterController>();
            }
        }

        void Update()
        {
            transform.position = basePosition + Vector3.up * (0.08f * Mathf.Sin(Time.time * 2.2f + basePosition.x));
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

            if (!inventory || Time.time < armedAt || (playerController && !playerController.enabled)) return;
            var d = inventory.transform.position - basePosition;
            if (Mathf.Abs(d.y) > 1.6f || d.x * d.x + d.z * d.z > radius * radius) return;

            int taken = inventory.Pickup(itemId, count);
            count -= taken;
            if (count <= 0) Destroy(gameObject);
            else armedAt = Time.time + 2f; // no cabe: no repetir el aviso cada fotograma
        }

        /// <summary>Crea un objeto en el suelo en <paramref name="position"/> (un cubo del color del objeto).</summary>
        public static ItemPickup Spawn(string id, int amount, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Objeto_{id}";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.3f;
            var def = ItemCatalog.Get(id);
            go.GetComponent<Renderer>().material.color = def != null ? def.Color : new Color(0.2f, 0.6f, 0.25f);
            var pickup = go.AddComponent<ItemPickup>();
            pickup.Configure(id, amount);
            pickup.armedAt = Time.time + 1.5f; // que no se recoja en cuanto se tira
            return pickup;
        }
    }
}
