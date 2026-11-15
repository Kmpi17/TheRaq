using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Grupos del barrio (todos inventados; ver NPCS.md). <see cref="None"/>: civiles o zona de nadie.</summary>
    public enum Faction { None, AshBlock, Kessler, Crestline, Dunmore, Larkin }

    /// <summary>Bandos: el del jugador (Ash Block y sus sets aliados) y el rival.</summary>
    public enum FactionSide { Neutral, Block, Rival }

    public enum FactionRelation { Neutral, Ally, Hostile }

    /// <summary>
    /// Facciones, bandos, territorios y ropa (lógica pura, testeable en EditMode). Ash Block, Kessler Courts y
    /// Crestline son un bando; Dunmore y Larkin Street, el otro. Los territorios son rectángulos en metros (x, z) del
    /// plano; lo que no cae en ninguno es zona de nadie (franja de negocios, avenida, instituto, cocheras).
    /// </summary>
    public static class FactionCatalog
    {
        /// <summary>El jugador es de Ash Block.</summary>
        public const Faction PlayerFaction = Faction.AshBlock;

        public static string Name(Faction f) => f switch
        {
            Faction.AshBlock => "Ash Block",
            Faction.Kessler => "Kessler Courts",
            Faction.Crestline => "Crestline",
            Faction.Dunmore => "Dunmore",
            Faction.Larkin => "Larkin Street",
            _ => "Nadie",
        };

        public static FactionSide SideOf(Faction f) => f switch
        {
            Faction.AshBlock or Faction.Kessler or Faction.Crestline => FactionSide.Block,
            Faction.Dunmore or Faction.Larkin => FactionSide.Rival,
            _ => FactionSide.Neutral,
        };

        /// <summary>Mismo bando: aliados; bandos distintos: enemigos; con civiles o zona de nadie: neutral.</summary>
        public static FactionRelation Relation(Faction a, Faction b)
        {
            var sa = SideOf(a);
            var sb = SideOf(b);
            if (sa == FactionSide.Neutral || sb == FactionSide.Neutral) return FactionRelation.Neutral;
            return sa == sb ? FactionRelation.Ally : FactionRelation.Hostile;
        }

        public static bool Hostile(Faction a, Faction b) => Relation(a, b) == FactionRelation.Hostile;

        // Territorios (xMin, zMin, xMax, zMax), con las aceras de alrededor.
        static readonly (Faction faction, Rect area)[] territories =
        {
            (Faction.AshBlock, Rect.MinMaxRect(-103f, -78f, 103f, 78f)),
            (Faction.Kessler, Rect.MinMaxRect(112f, -88f, 260f, 88f)),
            (Faction.Larkin, Rect.MinMaxRect(112f, 88f, 260f, 300f)),
            (Faction.Dunmore, Rect.MinMaxRect(-106f, 88f, -30f, 300f)),
            (Faction.Dunmore, Rect.MinMaxRect(-30f, 102f, 103f, 300f)),
            (Faction.Crestline, Rect.MinMaxRect(-300f, 88f, -106f, 300f)),
        };

        public static IEnumerable<(Faction faction, Rect area)> Territories => territories;

        /// <summary>De quién es el punto (x, z) del plano; <see cref="Faction.None"/> si es zona de nadie.</summary>
        public static Faction TerritoryAt(Vector2 xz)
        {
            foreach (var (f, r) in territories)
                if (r.Contains(xz)) return f;
            return Faction.None;
        }

        public static Faction TerritoryAt(Vector3 world) => TerritoryAt(new Vector2(world.x, world.z));

        /// <summary>Centro aproximado del territorio (para saber de dónde vienen las incursiones).</summary>
        public static Vector2 Center(Faction f)
        {
            foreach (var (fa, r) in territories)
                if (fa == f) return r.center;
            return Vector2.zero;
        }

        // ---------- Ropa: un color de acento por grupo (ficticio) y ropa variada para los civiles ----------

        static readonly Dictionary<Faction, string[]> accentTops = new()
        {
            [Faction.AshBlock] = new[] { "sudadera_gris", "camiseta_blanca", "plumifero_negro", "sudadera_gris" },
            [Faction.Kessler] = new[] { "sudadera_granate", "camiseta_granate", "plumifero_negro" },
            [Faction.Crestline] = new[] { "plumifero_negro", "chaleco_negro", "sudadera_negra", "camisa_vaquera" },
            [Faction.Dunmore] = new[] { "sudadera_mostaza", "camiseta_amarilla", "plumifero_beige" },
            [Faction.Larkin] = new[] { "sudadera_verde", "camiseta_verde", "plumifero_negro" },
        };
        static readonly Dictionary<Faction, string[]> accentHeads = new()
        {
            [Faction.AshBlock] = new[] { "gorro_gris", "pescador_blanco", "gorra_blanca", null },
            [Faction.Kessler] = new[] { "gorra_granate", "gorro_negro", null },
            [Faction.Crestline] = new[] { "gorra_negra", "gorro_negro", "panuelo_negro", null },
            [Faction.Dunmore] = new[] { "gorro_mostaza", "gorra_negra", null },
            [Faction.Larkin] = new[] { "gorra_verde", "gorro_negro", null },
        };
        static readonly string[] gangLegs = { "vaqueros_oscuros", "vaqueros_negros", "chandal_gris", "vaqueros_claros" };
        static readonly string[] gangFeet = { "zapatillas_blancas", "zapatillas_negras", "botas_trabajo" };
        static readonly string[] civTops = { "plumifero_beige", "camisa_vaquera", "sudadera_azul", "plumifero_azul", "polo_rayas", "abrigo_marron", "sudadera_gris", "camiseta_blanca" };
        static readonly string[] civHeads = { null, null, "gorro_gris", "pescador_beige", "gorra_negra", null };
        static readonly string[] civLegs = { "vaqueros_oscuros", "pantalon_caqui", "chandal_gris", "vaqueros_claros" };
        static readonly string[] civFeet = { "zapatillas_blancas", "zapatillas_negras", "botas_trabajo" };

        static string Pick(string[] a, System.Random rng) => a[rng.Next(a.Length)];

        /// <summary>Prendas (ids de <see cref="ClothingCatalog"/>) para un NPC: con el acento de su grupo, o de civil.</summary>
        public static List<string> RandomOutfit(Faction f, System.Random rng)
        {
            var ids = new List<string>();
            bool gang = f != Faction.None && accentTops.ContainsKey(f);
            var head = gang ? Pick(accentHeads[f], rng) : Pick(civHeads, rng);
            if (head != null) ids.Add(head);
            ids.Add(gang ? Pick(accentTops[f], rng) : Pick(civTops, rng));
            ids.Add(gang ? Pick(gangLegs, rng) : Pick(civLegs, rng));
            ids.Add(gang ? Pick(gangFeet, rng) : Pick(civFeet, rng));
            if (gang && rng.NextDouble() < 0.25) ids.Add(rng.NextDouble() < 0.5 ? "cadena_oro" : "cadena_plata");
            if (rng.NextDouble() < 0.08) ids.Add("gafas_negras");
            return ids;
        }
    }
}
