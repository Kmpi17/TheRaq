using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GTAChiraq
{
    /// <summary>
    /// Acciones de input del juego, definidas en código (sin asset .inputactions) para que sean
    /// fáciles de leer y de testear. Se accede siempre a través de <see cref="Actions"/>.
    /// </summary>
    public sealed class GameInput : IDisposable
    {
        static GameInput instance;

        /// <summary>Instancia compartida; se crea y activa la primera vez que se pide.</summary>
        public static GameInput Actions => instance ??= new GameInput();

        public readonly InputAction Move;
        public readonly InputAction LookMouse;
        public readonly InputAction LookStick;
        /// <summary>Escalar o saltar por encima de un obstáculo (el personaje no salta en el sitio).</summary>
        public readonly InputAction Climb;
        public readonly InputAction Sprint;
        public readonly InputAction Interact;
        public readonly InputAction Handbrake;
        public readonly InputAction Attack;
        public readonly InputAction Aim;
        public readonly InputAction Reload;
        public readonly InputAction Cover;
        public readonly InputAction Inventory;
        /// <summary>Eje: positivo, arma siguiente; negativo, la anterior (rueda del ratón o cruceta).</summary>
        public readonly InputAction WeaponScroll;

        readonly InputAction[] all;

        GameInput()
        {
            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");

            LookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            LookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");

            Climb = Button("Climb", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            Interact = Button("Interact", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            Handbrake = Button("Handbrake", "<Keyboard>/space", "<Gamepad>/rightShoulder");
            Attack = Button("Attack", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Aim = Button("Aim", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonWest");
            Cover = Button("Cover", "<Keyboard>/q", "<Gamepad>/rightShoulder");
            Inventory = Button("Inventory", "<Keyboard>/tab", "<Gamepad>/select");
            WeaponScroll = new InputAction("WeaponScroll", InputActionType.Value, expectedControlType: "Axis");
            WeaponScroll.AddBinding("<Mouse>/scroll/y");
            WeaponScroll.AddCompositeBinding("1DAxis").With("Positive", "<Gamepad>/dpad/right").With("Negative", "<Gamepad>/dpad/left");

            all = new[] { Move, LookMouse, LookStick, Climb, Sprint, Interact, Handbrake, Attack, Aim, Reload, Cover, Inventory, WeaponScroll };
            foreach (var action in all) action.Enable();
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var path in bindings) action.AddBinding(path);
            return action;
        }

        public void Dispose()
        {
            foreach (var action in all) action.Dispose();
        }

        /// <summary>Destruye la instancia compartida (la siguiente llamada a <see cref="Actions"/> crea otra).</summary>
        public static void Reset()
        {
            instance?.Dispose();
            instance = null;
        }

        // Necesario si "Enter Play Mode Options" desactiva el domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            Reset();
            GameUi.MenuOpen = false;
        }
    }

    /// <summary>Estado de la interfaz que bloquea el control (por ejemplo, el inventario abierto).</summary>
    public static class GameUi
    {
        public static bool MenuOpen;
    }
}
