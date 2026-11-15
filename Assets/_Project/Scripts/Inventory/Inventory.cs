using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    public enum ItemKind { Weapon, Ammo, Consumable }

    /// <summary>Definición de un objeto (lo que es, no cuántos hay). Las armas llevan también sus datos de tiro.</summary>
    public sealed class ItemDef
    {
        public string Id, Name, Description;
        public ItemKind Kind;
        public int MaxStack = 1;
        public Color Color = Color.gray; // icono provisional: un recuadro de color

        // Consumibles
        public float Heal, Stamina, Armor, StaminaBoostSeconds;

        // Armas
        public string AmmoId;
        public int MagazineSize;
        public float Damage, FireInterval, Range, Spread, Bloom, MaxBloom, Recoil, ReloadSeconds;
    }

    /// <summary>Catálogo de objetos, definido en código (como <see cref="GameInput"/>). Todo es genérico, sin marcas.</summary>
    public static class ItemCatalog
    {
        public const string Pistol = "pistola", Ammo9mm = "municion_9mm", Medkit = "botiquin", Wings = "alitas",
            EnergyDrink = "bebida_energetica", Vest = "chaleco", Money = "dinero";

        static readonly Dictionary<string, ItemDef> items = new();

        static ItemCatalog()
        {
            Add(new ItemDef
            {
                Id = Pistol, Name = "Pistola 9 mm (prueba)", Kind = ItemKind.Weapon, Color = new Color(0.25f, 0.25f, 0.27f),
                Description = "Pistola semiautomática de pruebas. Cargador de 12.",
                AmmoId = Ammo9mm, MagazineSize = 12, Damage = 25f, FireInterval = 0.22f, Range = 70f,
                Spread = 0.6f, Bloom = 1.2f, MaxBloom = 5f, Recoil = 1.6f, ReloadSeconds = 1.4f,
            });
            Add(new ItemDef { Id = Ammo9mm, Name = "Munición 9 mm", Kind = ItemKind.Ammo, MaxStack = 120, Color = new Color(0.75f, 0.6f, 0.2f),
                Description = "Balas para la pistola. Se usan al recargar (R)." });
            Add(new ItemDef { Id = Medkit, Name = "Botiquín", Kind = ItemKind.Consumable, MaxStack = 3, Heal = 50f, Color = new Color(0.85f, 0.15f, 0.15f),
                Description = "Cura 50 de vida." });
            Add(new ItemDef { Id = Wings, Name = "Alitas fritas", Kind = ItemKind.Consumable, MaxStack = 5, Heal = 15f, Stamina = 40f,
                Color = new Color(0.8f, 0.45f, 0.15f), Description = "Cura 15 de vida y recupera energía." });
            Add(new ItemDef { Id = EnergyDrink, Name = "Bebida energética", Kind = ItemKind.Consumable, MaxStack = 5, Stamina = 100f,
                StaminaBoostSeconds = 20f, Color = new Color(0.3f, 0.85f, 0.3f), Description = "Llena la energía y la recupera más rápido durante 20 s." });
            Add(new ItemDef { Id = Vest, Name = "Chaleco", Kind = ItemKind.Consumable, MaxStack = 2, Armor = 50f, Color = new Color(0.2f, 0.3f, 0.55f),
                Description = "Añade 50 de chaleco: para el 70 % del daño hasta gastarse." });
        }

        static void Add(ItemDef def) => items[def.Id] = def;

        public static ItemDef Get(string id) => id != null && items.TryGetValue(id, out var def) ? def : null;
        public static IEnumerable<ItemDef> All => items.Values;

        /// <summary>Nombre para mostrar (también para el dinero, que no es un objeto del inventario).</summary>
        public static string DisplayName(string id) => id == Money ? "Dinero" : Get(id)?.Name ?? id;
    }

    public struct ItemStack
    {
        public string Id;
        public int Count;
        public bool IsEmpty => Id == null || Count <= 0;
        public ItemDef Def => ItemCatalog.Get(Id);
    }

    /// <summary>
    /// Inventario por casillas (lógica pura). Cada casilla guarda un objeto con un máximo por montón; lo que no cabe
    /// en los montones abiertos va a casillas vacías.
    /// </summary>
    public sealed class Inventory
    {
        readonly ItemStack[] slots;
        public event Action Changed;

        public Inventory(int slotCount = 12) => slots = new ItemStack[Mathf.Max(1, slotCount)];

        public int SlotCount => slots.Length;
        public ItemStack this[int index] => slots[index];

        /// <summary>Añade hasta <paramref name="count"/> y devuelve cuántos han cabido.</summary>
        public int Add(string id, int count)
        {
            var def = ItemCatalog.Get(id);
            if (def == null || count <= 0) return 0;
            int left = count;
            for (int i = 0; i < slots.Length && left > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].Id != id) continue;
                int put = Mathf.Min(left, def.MaxStack - slots[i].Count);
                slots[i].Count += put;
                left -= put;
            }
            for (int i = 0; i < slots.Length && left > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                int put = Mathf.Min(left, def.MaxStack);
                slots[i] = new ItemStack { Id = id, Count = put };
                left -= put;
            }
            if (left != count) Changed?.Invoke();
            return count - left;
        }

        /// <summary>Quita hasta <paramref name="count"/> (de las últimas casillas primero) y devuelve cuántos ha quitado.</summary>
        public int Remove(string id, int count)
        {
            int left = count;
            for (int i = slots.Length - 1; i >= 0 && left > 0; i--)
            {
                if (slots[i].IsEmpty || slots[i].Id != id) continue;
                left -= TakeFrom(i, left);
            }
            if (left != count) Changed?.Invoke();
            return count - left;
        }

        public int RemoveAt(int index, int count)
        {
            if (index < 0 || index >= slots.Length || slots[index].IsEmpty || count <= 0) return 0;
            int taken = TakeFrom(index, count);
            Changed?.Invoke();
            return taken;
        }

        int TakeFrom(int i, int count)
        {
            int take = Mathf.Min(count, slots[i].Count);
            slots[i].Count -= take;
            if (slots[i].Count <= 0) slots[i] = default;
            return take;
        }

        public int Count(string id)
        {
            int total = 0;
            foreach (var s in slots)
                if (!s.IsEmpty && s.Id == id) total += s.Count;
            return total;
        }

        public bool Has(string id) => Count(id) > 0;

        /// <summary>Cuántos más de <paramref name="id"/> caben.</summary>
        public int SpaceFor(string id)
        {
            var def = ItemCatalog.Get(id);
            if (def == null) return 0;
            int space = 0;
            foreach (var s in slots)
                space += s.IsEmpty ? def.MaxStack : s.Id == id ? def.MaxStack - s.Count : 0;
            return space;
        }

        /// <summary>Armas que hay, en el orden de las casillas y sin repetir.</summary>
        public List<ItemDef> Weapons()
        {
            var list = new List<ItemDef>();
            foreach (var s in slots)
            {
                var def = s.IsEmpty ? null : s.Def;
                if (def != null && def.Kind == ItemKind.Weapon && !list.Contains(def)) list.Add(def);
            }
            return list;
        }
    }
}
