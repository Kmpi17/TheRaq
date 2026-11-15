using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Caja fuerte del piso: F abre un panel con el inventario del jugador a la izquierda y la caja a la derecha. Un clic
    /// en una casilla la pasa entera al otro lado (lo que quepa). Abajo, botones para guardar o sacar dinero. Lo de la
    /// caja no se pierde al morir (la factura del hospital solo cobra lo que se lleva encima) y se guarda entre partidas
    /// en PlayerPrefs ("casa.caja").
    /// </summary>
    public sealed class SafeBox : MenuInteractable
    {
        const string PrefsKey = "casa.caja";

        [SerializeField] int slots = 16;
        [Tooltip("Guarda el contenido entre partidas. Los tests lo apagan.")]
        [SerializeField] bool persist = true;

        PlayerInventory inventory;
        string note;

        public Stash Contents { get; private set; }
        public override string Prompt => "F: abrir la caja fuerte";

        void Awake() => Contents = persist ? Stash.Deserialize(PlayerPrefs.GetString(PrefsKey, ""), slots) : new Stash(slots);

        protected override void OnOpened()
        {
            inventory = User.GetComponent<PlayerInventory>();
            note = null;
        }

        protected override void OnClosed() => Save();

        void Save()
        {
            if (!persist) return;
            PlayerPrefs.SetString(PrefsKey, Contents.Serialize());
            PlayerPrefs.Save();
        }

        /// <summary>Guarda la casilla del inventario del jugador en la caja. Las armas se guardan descargadas.</summary>
        public int Store(PlayerInventory from, int slot)
        {
            if (!from || slot < 0 || slot >= from.Items.SlotCount || from.Items[slot].IsEmpty) return 0;
            from.UnloadForStorage(slot);
            int moved = Stash.Transfer(from.Items, slot, Contents.Items);
            note = moved > 0 ? null : "La caja está llena";
            return moved;
        }

        /// <summary>Saca la casilla de la caja al inventario del jugador (lo que quepa).</summary>
        public int Withdraw(PlayerInventory to, int slot)
        {
            if (!to || slot < 0 || slot >= Contents.Items.SlotCount || Contents.Items[slot].IsEmpty) return 0;
            var stack = Contents.Items[slot];
            int moved = to.Pickup(stack.Id, stack.Count);
            if (moved > 0) Contents.Items.RemoveAt(slot, moved);
            note = moved > 0 ? null : "No te cabe en el inventario";
            return moved;
        }

        public void StoreMoney(PlayerInventory from, int amount)
        {
            if (!from) return;
            amount = Mathf.Min(amount, from.Money);
            if (amount > 0 && from.SpendMoney(amount)) Contents.AddMoney(amount);
        }

        public void WithdrawMoney(PlayerInventory to, int amount)
        {
            if (to) to.AddMoney(Contents.TakeMoney(amount));
        }

        protected override void DrawPanel()
        {
            const int cols = 4, cell = 82, gap = 5;
            float colW = cols * (cell + gap) + gap;
            float w = colW * 2f + 60f, h = 4 * (cell + gap) + 200f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 14, area.y + 8, w - 28, 30), "CAJA FUERTE", Title);
            if (!inventory) return;

            float top = area.y + 70f, leftX = area.x + 20f, rightX = area.x + 40f + colW;
            GUI.Label(new Rect(leftX, area.y + 42, colW, 24), $"Encima   ·   ${inventory.Money}", Text);
            GUI.Label(new Rect(rightX, area.y + 42, colW, 24), $"En la caja   ·   ${Contents.Money}", Text);
            int clicked;
            if ((clicked = Grid(inventory.Items, leftX, top, cols, cell, gap)) >= 0) Store(inventory, clicked);
            if ((clicked = Grid(Contents.Items, rightX, top, cols, cell, gap)) >= 0) Withdraw(inventory, clicked);

            float rows = Mathf.Max(Mathf.Ceil(inventory.Items.SlotCount / (float)cols), Mathf.Ceil(Contents.Items.SlotCount / (float)cols));
            float y = top + rows * (cell + gap) + 8f;
            float bx = leftX;
            foreach (int amount in new[] { 10, 100 })
            {
                if (GUI.Button(new Rect(bx, y, 100, 28), $"Guardar ${amount}")) StoreMoney(inventory, amount);
                bx += 106f;
            }
            if (GUI.Button(new Rect(bx, y, 120, 28), "Guardar todo")) StoreMoney(inventory, inventory.Money);
            bx = rightX;
            foreach (int amount in new[] { 10, 100 })
            {
                if (GUI.Button(new Rect(bx, y, 100, 28), $"Sacar ${amount}")) WithdrawMoney(inventory, amount);
                bx += 106f;
            }
            if (GUI.Button(new Rect(bx, y, 120, 28), "Sacar todo")) WithdrawMoney(inventory, Contents.Money);

            string help = note ?? "Clic en una casilla para pasarla al otro lado. Las armas se guardan descargadas.   F / Esc: cerrar";
            GUI.Label(new Rect(leftX, y + 36, w - 40, 40), help, Text);
        }

        /// <summary>Dibuja las casillas y devuelve la que se ha pulsado, o −1.</summary>
        static int Grid(Inventory items, float x0, float y0, int cols, int cell, int gap)
        {
            int clicked = -1;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, alignment = TextAnchor.LowerLeft };
            for (int i = 0; i < items.SlotCount; i++)
            {
                var r = new Rect(x0 + (i % cols) * (cell + gap), y0 + (i / cols) * (cell + gap), cell, cell);
                if (GUI.Button(r, GUIContent.none) && !items[i].IsEmpty) clicked = i;
                var stack = items[i];
                if (stack.IsEmpty) continue;
                var def = stack.Def;
                Swatch(new Rect(r.x + 6, r.y + 6, 24, 24), def.Color);
                string count = def.Kind == ItemKind.Weapon ? "" : $" ×{stack.Count}";
                GUI.Label(new Rect(r.x + 4, r.y + 30, r.width - 8, r.height - 32), def.Name + count, small);
            }
            return clicked;
        }
    }
}
