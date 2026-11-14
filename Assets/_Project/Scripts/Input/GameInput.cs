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
        public readonly InputAction Jump;
        public readonly InputAction Sprint;
        public readonly InputAction Interact;
        public readonly InputAction Handbrake;

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

            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            Interact = Button("Interact", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            Handbrake = Button("Handbrake", "<Keyboard>/space", "<Gamepad>/rightShoulder");

            all = new[] { Move, LookMouse, LookStick, Jump, Sprint, Interact, Handbrake };
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
        static void ResetOnLoad() => Reset();
    }
}
