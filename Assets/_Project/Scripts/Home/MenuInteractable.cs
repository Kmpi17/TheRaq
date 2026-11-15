using UnityEngine;
using UnityEngine.InputSystem;

namespace GTAChiraq
{
    /// <summary>
    /// Mueble que abre un panel (armario, caja fuerte). Abierto: <see cref="GameUi.MenuOpen"/> bloquea movimiento, cámara
    /// y combate, y el cursor queda libre. Se cierra con F, Esc o alejándose. El tiempo no se frena: en casa no hay peligro.
    /// </summary>
    public abstract class MenuInteractable : Interactable
    {
        int openedFrame = -1;

        /// <summary>Fotograma en que se cerró un panel con F: ese F no debe volver a abrir nada.</summary>
        public static int LastClosedFrame { get; private set; } = -1;
        protected PlayerInteractor User { get; private set; }
        public bool IsOpen { get; private set; }

        public override void Interact(PlayerInteractor player)
        {
            if (IsOpen || GameUi.MenuOpen) return;
            User = player;
            IsOpen = true;
            openedFrame = Time.frameCount;
            GameUi.MenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OnOpened();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            LastClosedFrame = Time.frameCount;
            GameUi.MenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnClosed();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
        /// <summary>Dibuja el panel con OnGUI.</summary>
        protected abstract void DrawPanel();

        protected override void OnDisable()
        {
            Close();
            base.OnDisable();
        }

        protected virtual void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            bool far = !User || (User.transform.position - transform.position).sqrMagnitude > (radius + 1f) * (radius + 1f);
            if (far || GameInput.Actions.Interact.WasPressedThisFrame() || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)) Close();
        }

        void OnGUI()
        {
            if (IsOpen) DrawPanel();
        }

        // ---------- Ayudas para los paneles ----------

        protected static GUIStyle Title => new(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        protected static GUIStyle Text => new(GUI.skin.label) { fontSize = 14, wordWrap = true };

        protected static void Swatch(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
