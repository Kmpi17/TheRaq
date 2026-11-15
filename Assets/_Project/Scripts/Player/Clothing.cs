using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Partes del cuerpo. Cabeza, cara (gafas) y cuello (cadena) pueden ir sin nada (<see cref="ClothingCatalog.IsOptional"/>).</summary>
    public enum ClothingSlot { Head, Torso, Legs, Feet, Face, Neck }

    /// <summary>Forma de la prenda: decide qué piezas del modelo se ven (<see cref="PlayerOutfit"/>).</summary>
    /// <remarks>Los nuevos van siempre al final: el generador indexa las texturas de tejido por este orden.</remarks>
    public enum ClothingStyle
    {
        Cap, Beanie, Hoodie, Puffer, Tee, Jeans, Sweatpants, Sneakers, Boots,
        BucketHat, Durag, Polo, Vest, Shirt, Khakis, Sunglasses, Chain, ChainPendant,
    }

    public sealed class ClothingDef
    {
        public string Id, Name;
        public ClothingSlot Slot;
        public ClothingStyle Style;
        public Color Color;
    }

    /// <summary>Toda la ropa del juego, en código. Prendas genéricas de la época, sin marcas.</summary>
    public static class ClothingCatalog
    {
        static readonly Dictionary<string, ClothingDef> items = new();
        static readonly List<ClothingDef> ordered = new();

        static ClothingCatalog()
        {
            void Add(string id, string name, ClothingSlot slot, ClothingStyle style, Color color)
            {
                var def = new ClothingDef { Id = id, Name = name, Slot = slot, Style = style, Color = color };
                items[id] = def;
                ordered.Add(def);
            }

            Add("gorra_roja", "Gorra roja", ClothingSlot.Head, ClothingStyle.Cap, new Color(0.7f, 0.1f, 0.1f));
            Add("gorra_negra", "Gorra negra", ClothingSlot.Head, ClothingStyle.Cap, new Color(0.06f, 0.06f, 0.07f));
            Add("gorro_gris", "Gorro de lana gris", ClothingSlot.Head, ClothingStyle.Beanie, new Color(0.4f, 0.41f, 0.43f));
            Add("gorro_negro", "Gorro de lana negro", ClothingSlot.Head, ClothingStyle.Beanie, new Color(0.07f, 0.07f, 0.08f));
            Add("pescador_blanco", "Gorro de pescador blanco", ClothingSlot.Head, ClothingStyle.BucketHat, new Color(0.88f, 0.87f, 0.83f));
            Add("pescador_beige", "Gorro de pescador beige", ClothingSlot.Head, ClothingStyle.BucketHat, new Color(0.62f, 0.55f, 0.42f));
            Add("panuelo_negro", "Pañuelo de cabeza negro", ClothingSlot.Head, ClothingStyle.Durag, new Color(0.06f, 0.06f, 0.07f));

            Add("sudadera_negra", "Sudadera negra", ClothingSlot.Torso, ClothingStyle.Hoodie, new Color(0.12f, 0.12f, 0.13f));
            Add("sudadera_gris", "Sudadera gris", ClothingSlot.Torso, ClothingStyle.Hoodie, new Color(0.45f, 0.46f, 0.48f));
            Add("plumifero_negro", "Plumífero negro", ClothingSlot.Torso, ClothingStyle.Puffer, new Color(0.05f, 0.05f, 0.06f));
            Add("plumifero_beige", "Plumífero beige", ClothingSlot.Torso, ClothingStyle.Puffer, new Color(0.6f, 0.52f, 0.38f));
            Add("camiseta_blanca", "Camiseta blanca XL", ClothingSlot.Torso, ClothingStyle.Tee, new Color(0.9f, 0.9f, 0.88f));
            Add("camiseta_gris", "Camiseta gris XL", ClothingSlot.Torso, ClothingStyle.Tee, new Color(0.55f, 0.56f, 0.57f));
            Add("camiseta_amarilla", "Camiseta amarilla XL", ClothingSlot.Torso, ClothingStyle.Tee, new Color(0.85f, 0.68f, 0.2f));
            Add("camiseta_verde", "Camiseta verde XL", ClothingSlot.Torso, ClothingStyle.Tee, new Color(0.17f, 0.27f, 0.18f));
            // El polo de rayas lleva los colores en la textura: el tinte es casi blanco.
            Add("polo_rayas", "Polo de rayas", ClothingSlot.Torso, ClothingStyle.Polo, new Color(0.95f, 0.95f, 0.95f));
            Add("chaleco_negro", "Chaleco negro y camiseta", ClothingSlot.Torso, ClothingStyle.Vest, new Color(0.07f, 0.07f, 0.08f));
            Add("camisa_vaquera", "Camisa vaquera", ClothingSlot.Torso, ClothingStyle.Shirt, new Color(0.45f, 0.55f, 0.7f));

            Add("vaqueros_oscuros", "Vaqueros oscuros", ClothingSlot.Legs, ClothingStyle.Jeans, new Color(0.12f, 0.16f, 0.28f));
            Add("vaqueros_negros", "Vaqueros negros", ClothingSlot.Legs, ClothingStyle.Jeans, new Color(0.07f, 0.07f, 0.08f));
            Add("chandal_gris", "Pantalón de chándal gris", ClothingSlot.Legs, ClothingStyle.Sweatpants, new Color(0.42f, 0.43f, 0.45f));
            Add("vaqueros_claros", "Vaqueros anchos claros", ClothingSlot.Legs, ClothingStyle.Jeans, new Color(0.5f, 0.6f, 0.74f));
            Add("pantalon_caqui", "Pantalón caqui de bolsillos", ClothingSlot.Legs, ClothingStyle.Khakis, new Color(0.56f, 0.51f, 0.39f));

            Add("zapatillas_blancas", "Zapatillas blancas", ClothingSlot.Feet, ClothingStyle.Sneakers, new Color(0.92f, 0.92f, 0.9f));
            Add("zapatillas_negras", "Zapatillas negras", ClothingSlot.Feet, ClothingStyle.Sneakers, new Color(0.05f, 0.05f, 0.06f));
            Add("botas_trabajo", "Botas de trabajo", ClothingSlot.Feet, ClothingStyle.Boots, new Color(0.62f, 0.45f, 0.22f));

            // Complementos: el color es el de la montura o el metal (los cristales siempre son oscuros).
            Add("gafas_negras", "Gafas de sol negras", ClothingSlot.Face, ClothingStyle.Sunglasses, new Color(0.04f, 0.04f, 0.05f));
            Add("gafas_doradas", "Gafas de sol doradas", ClothingSlot.Face, ClothingStyle.Sunglasses, new Color(0.8f, 0.64f, 0.26f));
            Add("gafas_blancas", "Gafas de sol blancas", ClothingSlot.Face, ClothingStyle.Sunglasses, new Color(0.9f, 0.9f, 0.88f));
            Add("cadena_oro", "Cadena de oro", ClothingSlot.Neck, ClothingStyle.Chain, new Color(0.85f, 0.68f, 0.25f));
            Add("cadena_plata", "Cadena de plata", ClothingSlot.Neck, ClothingStyle.Chain, new Color(0.76f, 0.77f, 0.79f));
            Add("cadena_colgante", "Cadena con colgante", ClothingSlot.Neck, ClothingStyle.ChainPendant, new Color(0.85f, 0.68f, 0.25f));
        }

        public static ClothingDef Get(string id) => id != null && items.TryGetValue(id, out var def) ? def : null;
        public static IEnumerable<ClothingDef> All => ordered;
        public static IEnumerable<ClothingDef> InSlot(ClothingSlot slot) => ordered.Where(d => d.Slot == slot);

        /// <summary>Armario básico con el que se empieza: todo el catálogo.</summary>
        public static IEnumerable<string> StarterWardrobe => ordered.Select(d => d.Id);

        /// <summary>Lo que lleva puesto al empezar.</summary>
        public static readonly string[] StarterOutfit = { "gorra_roja", "sudadera_negra", "vaqueros_oscuros", "zapatillas_blancas" };

        public static string SlotName(ClothingSlot slot) => slot switch
        {
            ClothingSlot.Head => "Cabeza",
            ClothingSlot.Torso => "Torso",
            ClothingSlot.Legs => "Piernas",
            ClothingSlot.Feet => "Pies",
            ClothingSlot.Face => "Cara",
            _ => "Cuello",
        };

        /// <summary>Partes que pueden ir sin nada: cabeza, cara y cuello.</summary>
        public static bool IsOptional(ClothingSlot slot) => slot is ClothingSlot.Head or ClothingSlot.Face or ClothingSlot.Neck;
    }

    /// <summary>
    /// Ropa del jugador (lógica pura): qué prendas tiene y cuál lleva en cada parte. Cabeza, cara y cuello pueden ir sin
    /// nada; el resto siempre lleva algo. Se guarda como texto (<see cref="Serialize"/>) en PlayerPrefs.
    /// </summary>
    public sealed class Outfit
    {
        readonly HashSet<string> owned = new();
        readonly Dictionary<ClothingSlot, string> equipped = new();
        public event Action Changed;

        public IEnumerable<string> Owned => owned;
        public bool Owns(string id) => owned.Contains(id);
        public string Equipped(ClothingSlot slot) => equipped.TryGetValue(slot, out var id) ? id : null;

        public static Outfit Starter()
        {
            var o = new Outfit();
            foreach (var id in ClothingCatalog.StarterWardrobe) o.owned.Add(id);
            foreach (var id in ClothingCatalog.StarterOutfit) o.equipped[ClothingCatalog.Get(id).Slot] = id;
            return o;
        }

        public bool Give(string id)
        {
            if (ClothingCatalog.Get(id) == null || !owned.Add(id)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Se pone una prenda que tiene. Devuelve false si no la tiene o no existe.</summary>
        public bool Equip(string id)
        {
            var def = ClothingCatalog.Get(id);
            if (def == null || !owned.Contains(id)) return false;
            equipped[def.Slot] = id;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Se quita lo de esa parte. Solo cabeza, cara y cuello pueden quedarse sin nada.</summary>
        public bool Unequip(ClothingSlot slot)
        {
            if (!ClothingCatalog.IsOptional(slot) || !equipped.Remove(slot)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>"tiene=a,b,c;lleva=Head:x,Torso:y".</summary>
        public string Serialize() =>
            "tiene=" + string.Join(",", owned) + ";lleva=" + string.Join(",", equipped.Select(kv => $"{kv.Key}:{kv.Value}"));

        /// <summary>Lee lo de <see cref="Serialize"/>. Si está vacío o roto, o le falta algo obligatorio, completa con lo de inicio.</summary>
        public static Outfit Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text)) return Starter();
            var o = new Outfit();
            foreach (var part in text.Split(';'))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "tiene")
                    foreach (var id in kv[1].Split(','))
                        if (ClothingCatalog.Get(id) != null) o.owned.Add(id);
                if (kv[0] == "lleva")
                    foreach (var pair in kv[1].Split(','))
                    {
                        var sv = pair.Split(':');
                        if (sv.Length == 2 && Enum.TryParse(sv[0], out ClothingSlot slot) && ClothingCatalog.Get(sv[1]) is { } def && def.Slot == slot)
                            o.equipped[slot] = sv[1];
                    }
            }
            if (o.owned.Count == 0) return Starter();
            // El armario básico se tiene siempre: así, las prendas nuevas del catálogo aparecen también en partidas viejas.
            foreach (var id in ClothingCatalog.StarterWardrobe) o.owned.Add(id);
            foreach (var id in ClothingCatalog.StarterOutfit)
            {
                var def = ClothingCatalog.Get(id);
                if (ClothingCatalog.IsOptional(def.Slot)) continue;
                if (o.Equipped(def.Slot) == null)
                {
                    o.owned.Add(id);
                    o.equipped[def.Slot] = id;
                }
            }
            foreach (var id in o.equipped.Values) o.owned.Add(id);
            return o;
        }
    }
}
