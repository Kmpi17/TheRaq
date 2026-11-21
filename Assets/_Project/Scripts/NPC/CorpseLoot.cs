using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Cuerpo de un NPC muerto que se puede registrar con F: abre un panel con lo que llevaba (<see cref="NpcLoot"/>) y su
    /// dinero, y se coge casilla a casilla o todo de golpe. Lo que no cabe en el inventario se queda en el cuerpo.
    /// Lo pone <see cref="NpcBrain"/> al morir; el cuerpo no desaparece hasta que el jugador se aleja (<see cref="NpcPopulation"/>).
    /// </summary>
    public sealed class CorpseLoot : MenuInteractable
    {
        static AudioClip rummageClip;

        PlayerInventory inventory;
        string note;

        public Stash Contents { get; private set; } = new Stash(NpcLoot.Slots);
        public bool Empty
        {
            get
            {
                if (Contents.Money > 0) return false;
                for (int i = 0; i < Contents.Items.SlotCount; i++) if (!Contents.Items[i].IsEmpty) return false;
                return true;
            }
        }

        public override string Prompt => Empty ? "F: registrar el cuerpo (no lleva nada)" : "F: registrar el cuerpo";

        /// <summary>Lo llama el NPC al morir.</summary>
        public void Fill(Stash contents)
        {
            Contents = contents ?? new Stash(NpcLoot.Slots);
            radius = 1.8f;
        }

        protected override void OnOpened()
        {
            inventory = User.GetComponent<PlayerInventory>();
            note = null;
            if (!rummageClip) rummageClip = ProceduralSfx.Rummage();
            AudioSource.PlayClipAtPoint(rummageClip, transform.position, 0.8f);
        }

        /// <summary>Pasa la casilla del cuerpo al inventario del jugador (lo que quepa).</summary>
        public int Take(PlayerInventory to, int slot)
        {
            if (!to || slot < 0 || slot >= Contents.Items.SlotCount || Contents.Items[slot].IsEmpty) return 0;
            var stack = Contents.Items[slot];
            int moved = to.Pickup(stack.Id, stack.Count);
            if (moved > 0) Contents.Items.RemoveAt(slot, moved);
            note = moved > 0 ? null : "No te cabe en el inventario";
            return moved;
        }

        /// <summary>Coge el dinero y todo lo que quepa.</summary>
        public void TakeAll(PlayerInventory to)
        {
            if (!to) return;
            to.AddMoney(Contents.TakeMoney(Contents.Money));
            bool full = false;
            for (int i = 0; i < Contents.Items.SlotCount; i++)
                if (!Contents.Items[i].IsEmpty && Take(to, i) == 0) full = true;
            note = full ? "No te cabe todo en el inventario" : null;
        }

        protected override void DrawPanel()
        {
            const int cols = 4, cell = 82, gap = 5;
            float w = cols * (cell + gap) + gap + 40f, h = 2 * (cell + gap) + 190f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 14, area.y + 8, w - 28, 30), "CUERPO", Title);
            if (!inventory) return;

            GUI.Label(new Rect(area.x + 20, area.y + 42, w - 40, 24), $"Dinero: ${Contents.Money}      Tú llevas ${inventory.Money}", Text);
            float top = area.y + 70f, left = area.x + 20f;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, alignment = TextAnchor.LowerLeft };
            for (int i = 0; i < Contents.Items.SlotCount; i++)
            {
                var r = new Rect(left + (i % cols) * (cell + gap), top + (i / cols) * (cell + gap), cell, cell);
                var stack = Contents.Items[i];
                if (GUI.Button(r, GUIContent.none) && !stack.IsEmpty) Take(inventory, i);
                if (stack.IsEmpty) continue;
                var def = stack.Def;
                Swatch(new Rect(r.x + 6, r.y + 6, 24, 24), def.Color);
                string count = def.Kind == ItemKind.Weapon ? "" : $" ×{stack.Count}";
                GUI.Label(new Rect(r.x + 4, r.y + 30, r.width - 8, r.height - 32), def.Name + count, small);
            }

            float y = top + 2 * (cell + gap) + 8f;
            if (GUI.Button(new Rect(left, y, 140, 30), "Coger todo")) TakeAll(inventory);
            if (Contents.Money > 0 && GUI.Button(new Rect(left + 150, y, 140, 30), $"Coger ${Contents.Money}"))
                inventory.AddMoney(Contents.TakeMoney(Contents.Money));
            string help = note ?? (Empty ? "No lleva nada más." : "Clic en una casilla para cogerla.") + "   F / Esc: cerrar";
            GUI.Label(new Rect(left, y + 38, w - 40, 40), help, Text);
        }
    }
}
