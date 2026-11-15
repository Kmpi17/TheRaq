using System.Collections;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Va en el jugador. A pie, busca el <see cref="Interactable"/> más cercano (<see cref="Focused"/>, para el aviso del
    /// HUD) y lo usa con F. También hace los viajes en ascensor (<see cref="Teleport"/>): fundido a negro, mueve al
    /// jugador y vuelve la imagen.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float fadeSeconds = 0.35f;
        [SerializeField] float blackSeconds = 0.5f;

        CharacterController controller;
        PlayerMovement movement;
        PlayerVitals vitals;
        ThirdPersonCamera followCamera;
        float fade;

        public Interactable Focused { get; private set; }
        public bool Teleporting { get; private set; }
        /// <summary>Fotograma en que se usó F aquí: ese F ya no sirve para el coche.</summary>
        public int UsedFrame { get; private set; } = -1;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            vitals = GetComponent<PlayerVitals>();
        }

        void Update()
        {
            Focused = null;
            if (Teleporting || GameUi.MenuOpen || MenuInteractable.LastClosedFrame == Time.frameCount) return;
            if ((controller && !controller.enabled) || (movement && movement.Locked)) return;
            Focused = Interactable.FindNearest(transform.position, Interactable.All);
            if (!Focused || !GameInput.Actions.Interact.WasPressedThisFrame()) return;
            UsedFrame = Time.frameCount;
            Focused.Interact(this);
        }

        /// <summary>Lleva al jugador a <paramref name="destination"/> (posición y giro) con un fundido a negro.</summary>
        public void Teleport(Transform destination)
        {
            if (!Teleporting && destination) StartCoroutine(RunTeleport(destination.position, Quaternion.Euler(0f, destination.eulerAngles.y, 0f)));
        }

        IEnumerator RunTeleport(Vector3 position, Quaternion rotation)
        {
            Teleporting = true;
            if (movement)
            {
                movement.Locked = true;
                movement.ResetMotion();
            }
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                fade = t / fadeSeconds;
                yield return null;
            }
            fade = 1f;

            if (controller) controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            if (controller) controller.enabled = true;
            if (!followCamera) followCamera = FindAnyObjectByType<ThirdPersonCamera>();
            if (followCamera) followCamera.SetYaw(rotation.eulerAngles.y);
            yield return new WaitForSecondsRealtime(blackSeconds);

            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                fade = 1f - t / fadeSeconds;
                yield return null;
            }
            fade = 0f;
            if (movement && !(vitals && vitals.Dead)) movement.Locked = false;
            Teleporting = false;
        }

        void OnGUI()
        {
            if (fade <= 0f) return;
            GUI.depth = -100;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
