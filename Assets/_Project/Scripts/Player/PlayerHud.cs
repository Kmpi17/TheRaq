using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// HUD provisional (OnGUI): barras de vida, chaleco y energía; arma, balas y dinero; mira con la dispersión real y
    /// marca de impacto; avisos de escalar y cubrirse; destello rojo al recibir daño y pantalla de hospitalizado.
    /// </summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        Health health;
        PlayerVitals vitals;
        PlayerCombat combat;
        PlayerInventory inventory;
        PlayerClimb climb;
        PlayerCover cover;
        PlayerMovement movement;
        VehicleEntry entry;
        PlayerInteractor interactor;
        CharacterController controller;
        Camera cam;
        GUIStyle label, big, center;

        void Awake()
        {
            health = GetComponent<Health>();
            vitals = GetComponent<PlayerVitals>();
            combat = GetComponent<PlayerCombat>();
            inventory = GetComponent<PlayerInventory>();
            climb = GetComponent<PlayerClimb>();
            cover = GetComponent<PlayerCover>();
            movement = GetComponent<PlayerMovement>();
            entry = GetComponent<VehicleEntry>();
            interactor = GetComponent<PlayerInteractor>();
            controller = GetComponent<CharacterController>();
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Bar(Rect r, float fraction, Color c)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fraction), r.height - 4), c);
        }

        void OnGUI()
        {
            if (!health) return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            center ??= new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            big ??= new GUIStyle(center) { fontSize = 46 };
            if (!cam) cam = Camera.main;
            float sw = Screen.width, sh = Screen.height;

            // Daño recibido: destello rojo que se apaga.
            float since = Time.time - health.LastDamageTime;
            if (since < 0.4f) Fill(new Rect(0, 0, sw, sh), new Color(0.8f, 0f, 0f, 0.3f * (1f - since / 0.4f)));

            // Barras abajo a la izquierda.
            var pool = health.Pool;
            float y = sh - 40f;
            if (vitals)
            {
                var st = vitals.Stamina;
                var col = st.Exhausted ? new Color(0.9f, 0.45f, 0.1f) : st.BoostLeft > 0f ? new Color(0.4f, 1f, 0.5f) : new Color(0.85f, 0.85f, 0.25f);
                Bar(new Rect(20, y, 220, 12), st.Current / st.Max, col);
                y -= 18f;
            }
            Bar(new Rect(20, y, 220, 16), pool.Current / pool.Max, new Color(0.8f, 0.12f, 0.12f));
            if (pool.Armor > 0f) Bar(new Rect(20, y - 14f, 220, 10), pool.Armor / pool.MaxArmor, new Color(0.3f, 0.5f, 0.95f));

            // Arma, balas y dinero arriba a la derecha.
            if (inventory)
            {
                var w = inventory.EquippedWeapon;
                string weaponText = w == null ? "Puños"
                    : $"{w.Name}\n{inventory.Magazine(w.Id)} / {inventory.Items.Count(w.AmmoId)}" + (combat && combat.Reloading ? "  recargando…" : "");
                GUI.Label(new Rect(sw - 300, 16, 280, 50), weaponText, new GUIStyle(label) { alignment = TextAnchor.UpperRight });
                GUI.Label(new Rect(sw - 300, 62, 280, 24), $"${inventory.Money}", new GUIStyle(label) { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.5f, 0.95f, 0.5f) } });
                if (!inventory.IsOpen && inventory.Message != null) GUI.Label(new Rect(sw / 2 - 200, sh * 0.72f, 400, 24), inventory.Message, center);
            }

            // Hora y tiempo, bajo el dinero.
            var weather = WeatherSystem.Instance;
            if (weather) GUI.Label(new Rect(sw - 300, 86, 280, 24), weather.Describe(), new GUIStyle(label) { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.85f, 0.88f, 0.95f) } });

            // Barrio en el que está y avisos de los NPC (te han reconocido, incursión), arriba en el centro.
            var population = NpcPopulation.Instance;
            if (population)
            {
                GUI.Label(new Rect(sw - 300, 110, 280, 24), population.TerritoryLabel, new GUIStyle(label) { alignment = TextAnchor.UpperRight, normal = { textColor = population.TerritoryColor } });
                if (population.Message != null) GUI.Label(new Rect(sw / 2 - 350, sh * 0.16f, 700, 28), population.Message, new GUIStyle(center) { normal = { textColor = new Color(1f, 0.82f, 0.45f) } });
            }

            if (vitals && vitals.Dead)
            {
                GUI.Label(new Rect(0, sh / 2 - 60, sw, 70), "HOSPITALIZADO", big);
                float left = Mathf.Max(0f, vitals.RespawnSeconds - (Time.time - vitals.DeathTime));
                GUI.Label(new Rect(0, sh / 2 + 10, sw, 30), $"Sales en {left:0} s (la factura del hospital se paga sola)", center);
                return;
            }
            if (controller && !controller.enabled) return; // en el coche no hay mira ni avisos

            // Mira: el hueco muestra la dispersión real del arma.
            if (combat && combat.ShowCrosshair && cam)
            {
                float px = sh / 2f * Mathf.Tan(combat.CurrentSpread * Mathf.Deg2Rad) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float gap = 4f + px, len = 8f;
                var c = new Color(1f, 1f, 1f, 0.9f);
                float cx = sw / 2f, cy = sh / 2f;
                Fill(new Rect(cx - gap - len, cy - 1, len, 2), c);
                Fill(new Rect(cx + gap, cy - 1, len, 2), c);
                Fill(new Rect(cx - 1, cy - gap - len, 2, len), c);
                Fill(new Rect(cx - 1, cy + gap, 2, len), c);
                if (Time.time - combat.LastHitTime < 0.15f)
                    GUI.Label(new Rect(cx - 20, cy - 20, 40, 40), "×", new GUIStyle(big) { fontSize = 30, normal = { textColor = combat.LastHitKilled ? Color.red : Color.white } });
            }
            if (combat && combat.Reloading) Bar(new Rect(sw / 2 - 40, sh / 2 + 30, 80, 8), combat.ReloadProgress, Color.white);

            // Avisos.
            string hint = null;
            if (entry && entry.Forcing)
            {
                hint = "Forzando la puerta… (mantén F)";
                Bar(new Rect(sw / 2 - 60, sh - 172, 120, 8), entry.ForceProgress, new Color(0.95f, 0.75f, 0.2f));
            }
            else if (cover && cover.InCover) hint = cover.AtEdge || cover.LowCover ? "Q: salir · clic derecho: asomarse" : "Q: salir · muévete a la esquina para asomarte";
            else if (climb && climb.Available != ClimbKind.None)
                hint = "Espacio: " + climb.Available switch
                {
                    ClimbKind.Vault => "saltar por encima",
                    ClimbKind.SlideOver => "pasar por encima del coche",
                    ClimbKind.ClimbUp => "subir",
                    _ => "trepar",
                };
            else if (cover && cover.Available) hint = "Q: cubrirse";
            if (hint == null && interactor && interactor.Focused) hint = interactor.Focused.Prompt;
            else if (hint == null && entry && entry.Nearby)
                hint = entry.Nearby.TryGetComponent(out VehicleLock carLock) && carLock.Locked ? "Mantén F: forzar la puerta y robar el coche" : "F: subir al coche";
            else if (hint != null && entry && entry.Nearby && !entry.Forcing && !(cover && cover.InCover))
                hint += "  ·  F: " + (entry.Nearby.TryGetComponent(out VehicleLock nearLock) && nearLock.Locked ? "forzar la puerta (mantén)" : "subir");
            if (hint == null && movement && movement.Crouching && !(cover && cover.InCover)) hint = "Agachado · C: levantarse";
            if (movement && Time.time - movement.StandBlockedTime < 1.5f) hint = "No hay sitio para levantarse";
            if (climb && Time.time - climb.NoStaminaTime < 1.5f) hint = "Sin fuerzas para escalar";
            if (hint != null) GUI.Label(new Rect(sw / 2 - 250, sh - 200, 500, 24), hint, center);
        }
    }
}
