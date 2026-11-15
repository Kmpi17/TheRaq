using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Armario del piso: F abre un panel con la ropa que tiene el jugador, por partes (cabeza, torso, piernas, pies, cara y cuello).
    /// Al elegir una prenda se la pone al momento (<see cref="PlayerOutfit"/>). Al abrir, el personaje se gira hacia la
    /// cámara para verse de frente.
    /// </summary>
    public sealed class Wardrobe : MenuInteractable
    {
        PlayerOutfit outfit;
        Quaternion previousRotation;

        public override string Prompt => "F: abrir el armario (cambiarse de ropa)";

        protected override void OnOpened()
        {
            outfit = User.GetComponent<PlayerOutfit>();
            previousRotation = User.transform.rotation;
            var cam = Camera.main;
            if (cam)
            {
                var toCam = cam.transform.position - User.transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f) User.transform.rotation = Quaternion.LookRotation(toCam);
            }
        }

        protected override void OnClosed()
        {
            if (User) User.transform.rotation = previousRotation;
        }

        protected override void DrawPanel()
        {
            const float w = 700f, rowH = 56f, cellW = 104f, labelW = 82f;
            int perLine = Mathf.FloorToInt((w - labelW - 24f) / cellW);
            var o = outfit ? outfit.Outfit : null;

            // Alto: cada parte ocupa tantas líneas como haga falta para sus prendas (más el "Nada" de las opcionales).
            float h = 96f;
            if (o != null)
                foreach (ClothingSlot slot in System.Enum.GetValues(typeof(ClothingSlot)))
                    h += Lines(o, slot, perLine) * rowH + 6f;
            var area = new Rect(Screen.width - w - 30f, Mathf.Max(10f, (Screen.height - h) / 2f), w, h);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 14, area.y + 8, w - 28, 30), "ARMARIO", Title);
            if (o == null)
            {
                GUI.Label(new Rect(area.x + 14, area.y + 44, w - 28, 40), "Este personaje no tiene ropa que cambiar.", Text);
                return;
            }

            var small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            float y = area.y + 46f;
            foreach (ClothingSlot slot in System.Enum.GetValues(typeof(ClothingSlot)))
            {
                GUI.Label(new Rect(area.x + 14, y + 4, labelW, 24), ClothingCatalog.SlotName(slot), Text);
                string current = o.Equipped(slot);
                int n = 0;
                Rect Cell()
                {
                    var r = new Rect(area.x + labelW + 14f + (n % perLine) * cellW, y + (n / perLine) * rowH, cellW - 6f, rowH - 6f);
                    n++;
                    return r;
                }
                if (ClothingCatalog.IsOptional(slot) && GUI.Toggle(Cell(), current == null, "Nada", GUI.skin.button) && current != null)
                    o.Unequip(slot);
                foreach (var def in ClothingCatalog.InSlot(slot))
                {
                    if (!o.Owns(def.Id)) continue;
                    var r = Cell();
                    if (GUI.Toggle(r, current == def.Id, GUIContent.none, GUI.skin.button) && current != def.Id) o.Equip(def.Id);
                    Swatch(new Rect(r.x + 6, r.y + 6, 16, 16), def.Color);
                    GUI.Label(new Rect(r.x + 4, r.y + 22, r.width - 8, r.height - 24), def.Name, small);
                }
                y += Lines(o, slot, perLine) * rowH + 6f;
            }
            GUI.Label(new Rect(area.x + 14, y + 4, w - 28, 40), "Haz clic en una prenda para ponértela.   F / Esc: cerrar", Text);
        }

        static int Lines(Outfit o, ClothingSlot slot, int perLine)
        {
            int count = ClothingCatalog.IsOptional(slot) ? 1 : 0;
            foreach (var def in ClothingCatalog.InSlot(slot))
                if (o.Owns(def.Id)) count++;
            return Mathf.Max(1, (count + perLine - 1) / perLine);
        }
    }
}
