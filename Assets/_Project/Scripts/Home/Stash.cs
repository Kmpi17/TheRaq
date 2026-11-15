using System.Text;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Contenido de la caja fuerte (lógica pura): casillas de objetos (<see cref="Inventory"/>) y dinero. Se guarda
    /// como texto con <see cref="Serialize"/>.
    /// </summary>
    public sealed class Stash
    {
        public Inventory Items { get; }
        public int Money { get; private set; }

        public Stash(int slots = 16) => Items = new Inventory(slots);

        public void AddMoney(int amount)
        {
            if (amount > 0) Money += amount;
        }

        /// <summary>Saca hasta <paramref name="amount"/> y devuelve lo que ha sacado.</summary>
        public int TakeMoney(int amount)
        {
            int take = Mathf.Clamp(amount, 0, Money);
            Money -= take;
            return take;
        }

        /// <summary>
        /// Pasa el montón de la casilla <paramref name="slot"/> de <paramref name="from"/> a <paramref name="to"/>: lo que
        /// cabe. Lo que no cabe se queda donde estaba. Devuelve cuántos ha pasado.
        /// </summary>
        public static int Transfer(Inventory from, int slot, Inventory to)
        {
            if (slot < 0 || slot >= from.SlotCount || from[slot].IsEmpty) return 0;
            var stack = from[slot];
            int moved = to.Add(stack.Id, stack.Count);
            if (moved > 0) from.RemoveAt(slot, moved);
            return moved;
        }

        /// <summary>"dinero=120;pistola:1,municion_9mm:36".</summary>
        public string Serialize()
        {
            var sb = new StringBuilder("dinero=").Append(Money).Append(';');
            for (int i = 0; i < Items.SlotCount; i++)
            {
                if (Items[i].IsEmpty) continue;
                sb.Append(Items[i].Id).Append(':').Append(Items[i].Count).Append(',');
            }
            return sb.ToString();
        }

        public static Stash Deserialize(string text, int slots = 16)
        {
            var s = new Stash(slots);
            if (string.IsNullOrEmpty(text)) return s;
            var parts = text.Split(new[] { ';' }, 2);
            if (parts[0].StartsWith("dinero=") && int.TryParse(parts[0].Substring(7), out int money)) s.Money = Mathf.Max(0, money);
            if (parts.Length > 1)
                foreach (var entry in parts[1].Split(','))
                {
                    var kv = entry.Split(':');
                    if (kv.Length == 2 && int.TryParse(kv[1], out int n)) s.Items.Add(kv[0], n);
                }
            return s;
        }
    }
}
