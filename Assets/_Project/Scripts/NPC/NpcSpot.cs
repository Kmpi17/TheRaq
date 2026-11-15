using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Sitio del barrio donde los NPC hacen algo (objeto inteligente): una esquina, un banco, la puerta de una tienda,
    /// la cancha, el memorial. Dice qué actividad ofrece, para quién, cuántas plazas tiene, si está a cubierto y de qué
    /// grupo es (si no se dice, el del territorio donde está). El NPC no sabe hacer cada cosa: va al sitio y el sitio
    /// le da una plaza (<see cref="Claim"/>) y la actividad. Los coloca el generador de la escena.
    /// </summary>
    public sealed class NpcSpot : MonoBehaviour
    {
        static readonly List<NpcSpot> all = new();
        public static IReadOnlyList<NpcSpot> All => all;

        [SerializeField] NpcActivity activity = NpcActivity.Hangout;
        [SerializeField] int capacity = 3;
        [Tooltip("Radio en el que se reparten las plazas (los sentados van justo en el sitio).")]
        [SerializeField] float radius = 1.6f;
        [SerializeField] bool covered;
        [SerializeField] bool forCivilians = true;
        [SerializeField] bool forGang = true;
        [Tooltip("Esquina o sitio habitual de su grupo: lo prefieren mucho.")]
        [SerializeField] bool habitual;
        [Tooltip("Grupo del sitio. None: el del territorio donde está.")]
        [SerializeField] Faction faction;
        [Tooltip("Tienda de la que depende (solo sirve si está abierta).")]
        [SerializeField] ShopHours shop;

        readonly List<Object> occupants = new();

        public NpcActivity Activity => activity;
        public int Capacity => capacity;
        public bool Covered => covered;
        public bool Habitual => habitual;
        public int Occupants => occupants.Count;
        public Faction Faction => faction != Faction.None ? faction : FactionCatalog.TerritoryAt(transform.position);
        public bool Open => !shop || shop.IsOpen;

        public bool Serves(NpcRole role) => role == NpcRole.Civilian ? forCivilians : forGang;

        /// <summary>Lo usa el generador.</summary>
        public void Configure(NpcActivity activity, int capacity, float radius, bool covered, bool forCivilians, bool forGang,
            bool habitual = false, Faction faction = Faction.None, ShopHours shop = null)
        {
            this.activity = activity;
            this.capacity = Mathf.Max(1, capacity);
            this.radius = radius;
            this.covered = covered;
            this.forCivilians = forCivilians;
            this.forGang = forGang;
            this.habitual = habitual;
            this.faction = faction;
            this.shop = shop;
        }

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            occupants.Clear();
        }

        /// <summary>Ocupa una plaza y devuelve dónde ponerse (en corro alrededor del sitio, o en él si es sentarse).</summary>
        public Vector3 Claim(Object who)
        {
            if (!occupants.Contains(who)) occupants.Add(who);
            int i = occupants.IndexOf(who);
            if (activity == NpcActivity.Sit || capacity == 1) return transform.position + transform.right * ((i - (capacity - 1) / 2f) * 0.7f);
            float a = (i + 0.5f) / capacity * Mathf.PI * 2f + transform.eulerAngles.y * Mathf.Deg2Rad;
            return transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
        }

        public void Release(Object who) => occupants.Remove(who);

        /// <summary>Hacia dónde mirar en la plaza: sentado o esperando, al frente del sitio; en corro, al centro.</summary>
        public Vector3 FacingFrom(Vector3 slot)
        {
            if (activity is NpcActivity.Sit or NpcActivity.Wait or NpcActivity.Mourn || capacity == 1) return transform.forward;
            var d = transform.position - slot;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = habitual ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawRay(transform.position, transform.forward);
        }
    }
}
