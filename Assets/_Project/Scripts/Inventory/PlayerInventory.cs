using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Inventario del jugador: casillas (<see cref="Inventory"/>), dinero, arma equipada y balas en el cargador de cada
    /// arma. Tab abre el panel (cámara lenta y cursor libre), donde se usa, equipa o tira cada objeto con el ratón.
    /// La rueda del ratón (o la cruceta) cambia entre puños y las armas que se llevan.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] int slotCount = 12;
        [Tooltip("\"id:cantidad\" de cada objeto con el que se empieza.")]
        [SerializeField] string[] startingItems = { "pistola:1", "municion_9mm:36", "botiquin:1", "alitas:2" };
        [SerializeField] int startingMoney = 150;
        [SerializeField] float slowMotionScale = 0.2f;

        readonly Dictionary<string, int> magazines = new();
        Health health;
        PlayerVitals vitals;
        CharacterController controller;
        int selected = -1;
        float previousScroll;
        string message;
        float messageUntil;

        public Inventory Items { get; private set; }
        public int Money { get; private set; }
        public string EquippedId { get; private set; } // null: puños
        public bool IsOpen { get; private set; }

        public ItemDef EquippedWeapon => EquippedId != null && Items.Has(EquippedId) ? ItemCatalog.Get(EquippedId) : null;
        public string Message => Time.unscaledTime < messageUntil ? message : null;

        void Awake()
        {
            health = GetComponent<Health>();
            vitals = GetComponent<PlayerVitals>();
            controller = GetComponent<CharacterController>();
            Items = new Inventory(slotCount);
            foreach (var entry in startingItems)
            {
                var parts = entry.Split(':');
                int n = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : 1;
                Items.Add(parts[0], n);
            }
            Money = startingMoney;
            foreach (var w in Items.Weapons()) magazines[w.Id] = w.MagazineSize; // las armas iniciales vienen cargadas
        }

        void OnDisable()
        {
            if (IsOpen) SetOpen(false);
        }

        void Update()
        {
            bool onFoot = !controller || controller.enabled;
            // Con otro menú abierto (armario, caja fuerte) Tab no abre el inventario.
            if (GameInput.Actions.Inventory.WasPressedThisFrame() && (onFoot || IsOpen) && (IsOpen || !GameUi.MenuOpen)) SetOpen(!IsOpen);
            if (IsOpen && !onFoot) SetOpen(false);
            if (EquippedId != null && !Items.Has(EquippedId)) EquippedId = null;

            float scroll = GameInput.Actions.WeaponScroll.ReadValue<float>();
            if (!IsOpen && onFoot && Mathf.Abs(scroll) > 0.1f && Mathf.Abs(previousScroll) <= 0.1f) Cycle(scroll > 0f ? 1 : -1);
            previousScroll = scroll;
        }

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            GameUi.MenuOpen = open;
            Time.timeScale = open ? slowMotionScale : 1f;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        // ---------- Armas ----------

        /// <summary>Pasa al arma siguiente (o anterior) entre puños y las armas del inventario.</summary>
        public void Cycle(int direction)
        {
            var list = new List<string> { null };
            foreach (var w in Items.Weapons()) list.Add(w.Id);
            int i = Mathf.Max(0, list.IndexOf(EquippedId));
            EquippedId = list[((i + direction) % list.Count + list.Count) % list.Count];
        }

        public void Equip(string id)
        {
            var def = ItemCatalog.Get(id);
            EquippedId = def != null && def.Kind == ItemKind.Weapon && Items.Has(id) ? id : null;
        }

        public int Magazine(string weaponId) => weaponId != null && magazines.TryGetValue(weaponId, out var m) ? m : 0;
        public void SetMagazine(string weaponId, int rounds) => magazines[weaponId] = Mathf.Max(0, rounds);

        /// <summary>Llena el cargador con la munición del inventario. Devuelve cuántas balas ha metido.</summary>
        public int Reload(string weaponId)
        {
            var def = ItemCatalog.Get(weaponId);
            if (def == null || def.Kind != ItemKind.Weapon) return 0;
            int need = def.MagazineSize - Magazine(weaponId);
            int taken = need > 0 ? Items.Remove(def.AmmoId, need) : 0;
            SetMagazine(weaponId, Magazine(weaponId) + taken);
            return taken;
        }

        // ---------- Objetos ----------

        /// <summary>Recoge un objeto del suelo. Devuelve cuántos se han quedado (el dinero siempre entero).</summary>
        public int Pickup(string id, int count)
        {
            if (id == ItemCatalog.Money)
            {
                Money += count;
                Say($"+${count}");
                return count;
            }
            int added = Items.Add(id, count);
            if (added > 0)
            {
                Say($"+{added} {ItemCatalog.DisplayName(id)}");
                var def = ItemCatalog.Get(id);
                if (def.Kind == ItemKind.Weapon && !magazines.ContainsKey(id)) magazines[id] = 0;
            }
            else Say("Inventario lleno");
            return added;
        }

        public void AddMoney(int amount)
        {
            if (amount > 0) Money += amount;
        }

        /// <summary>
        /// Prepara la casilla para sacarla del inventario sin tirarla (guardarla en la caja fuerte): si es un arma y no
        /// queda otra igual, las balas del cargador vuelven al inventario y deja de estar en la mano.
        /// </summary>
        public void UnloadForStorage(int slot)
        {
            if (slot < 0 || slot >= Items.SlotCount || Items[slot].IsEmpty) return;
            var def = Items[slot].Def;
            if (def.Kind != ItemKind.Weapon || Items.Count(def.Id) > Items[slot].Count) return;
            int rounds = Magazine(def.Id);
            magazines.Remove(def.Id);
            if (EquippedId == def.Id) EquippedId = null;
            if (rounds > 0) Items.Add(def.AmmoId, rounds);
        }

        public bool SpendMoney(int amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount;
            return true;
        }

        /// <summary>Usa la casilla: equipa un arma, o consume un botiquín, comida, bebida o chaleco.</summary>
        public bool Use(int slot)
        {
            if (slot < 0 || slot >= Items.SlotCount || Items[slot].IsEmpty) return false;
            var def = Items[slot].Def;
            switch (def.Kind)
            {
                case ItemKind.Weapon:
                    Equip(EquippedId == def.Id ? null : def.Id);
                    return true;
                case ItemKind.Ammo:
                    Say("La munición se usa al recargar (R)");
                    return false;
            }

            var pool = health ? health.Pool : null;
            var stamina = vitals ? vitals.Stamina : null;
            bool useful = (def.Heal > 0f && pool != null && pool.Current < pool.Max)
                          || (def.Armor > 0f && pool != null && pool.Armor < pool.MaxArmor)
                          || (def.Stamina > 0f && stamina != null && (stamina.Current < stamina.Max || def.StaminaBoostSeconds > 0f));
            if (!useful)
            {
                Say("Ahora no te hace falta");
                return false;
            }
            if (health)
            {
                health.Heal(def.Heal);
                health.AddArmor(def.Armor);
            }
            if (stamina != null)
            {
                stamina.Restore(def.Stamina);
                if (def.StaminaBoostSeconds > 0f) stamina.Boost(def.StaminaBoostSeconds);
            }
            Items.RemoveAt(slot, 1);
            Say($"Usado: {def.Name}");
            return true;
        }

        /// <summary>Tira al suelo todo el montón de la casilla. Las balas del cargador de un arma vuelven al inventario.</summary>
        public void Drop(int slot)
        {
            if (slot < 0 || slot >= Items.SlotCount || Items[slot].IsEmpty) return;
            var stack = Items[slot];
            var def = stack.Def;
            Items.RemoveAt(slot, stack.Count);
            if (def.Kind == ItemKind.Weapon && !Items.Has(def.Id))
            {
                Items.Add(def.AmmoId, Magazine(def.Id));
                magazines.Remove(def.Id);
                if (EquippedId == def.Id) EquippedId = null;
            }
            ItemPickup.Spawn(stack.Id, stack.Count, transform.position + transform.forward * 1.3f + Vector3.up * 0.4f);
            Say($"Tirado: {def.Name}");
        }

        void Say(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 2.5f;
        }

        // ---------- Panel ----------

        void OnGUI()
        {
            if (!IsOpen) return;
            const int cols = 4, cell = 96, gap = 6;
            int rows = Mathf.CeilToInt(Items.SlotCount / (float)cols);
            float w = cols * (cell + gap) + gap + 20f, h = rows * (cell + gap) + 210f;
            var area = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(area, GUIContent.none);
            var title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(area.x + 14, area.y + 8, w - 28, 30), $"INVENTARIO      ${Money}", title);

            var small = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, alignment = TextAnchor.LowerLeft };
            for (int i = 0; i < Items.SlotCount; i++)
            {
                var r = new Rect(area.x + 10 + gap + (i % cols) * (cell + gap), area.y + 44 + (i / cols) * (cell + gap), cell, cell);
                if (GUI.Toggle(r, selected == i, GUIContent.none, GUI.skin.button)) selected = i;
                var stack = Items[i];
                if (stack.IsEmpty) continue;
                var def = stack.Def;
                var old = GUI.color;
                GUI.color = def.Color;
                GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, 30, 30), Texture2D.whiteTexture);
                GUI.color = old;
                string extra = def.Kind == ItemKind.Weapon ? (EquippedId == def.Id ? "\n[EQUIPADA]" : "") : $"  ×{stack.Count}";
                GUI.Label(new Rect(r.x + 6, r.y + 40, r.width - 10, r.height - 44), def.Name + extra, small);
            }

            float y = area.y + 50 + rows * (cell + gap);
            var info = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            if (selected >= 0 && selected < Items.SlotCount && !Items[selected].IsEmpty)
            {
                var def = Items[selected].Def;
                GUI.Label(new Rect(area.x + 14, y, w - 28, 50), $"{def.Name}: {def.Description}", info);
                string useLabel = def.Kind == ItemKind.Weapon ? (EquippedId == def.Id ? "Guardar" : "Equipar") : "Usar";
                if (GUI.Button(new Rect(area.x + 14, y + 56, 120, 30), useLabel)) Use(selected);
                if (GUI.Button(new Rect(area.x + 144, y + 56, 120, 30), "Tirar")) Drop(selected);
            }
            else GUI.Label(new Rect(area.x + 14, y, w - 28, 50), "Elige una casilla.", info);

            string equipped = EquippedWeapon != null ? EquippedWeapon.Name : "Puños";
            GUI.Label(new Rect(area.x + 14, y + 96, w - 28, 50), $"En la mano: {equipped}   ·   Tab: cerrar   ·   Rueda: cambiar de arma", info);
            if (Message != null) GUI.Label(new Rect(area.x + 14, y + 124, w - 28, 30), Message, info);
        }
    }
}
