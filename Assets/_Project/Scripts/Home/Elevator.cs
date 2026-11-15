using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Puerta de ascensor: con F lleva al jugador a <see cref="Destination"/> con un fundido a negro
    /// (<see cref="PlayerInteractor.Teleport"/>). Hay uno en el portal de T2 (sube al piso) y otro en el piso (baja a la calle).
    /// </summary>
    public sealed class Elevator : Interactable
    {
        [SerializeField] Transform destination;
        [SerializeField] string prompt = "F: coger el ascensor";

        public Transform Destination => destination;
        public override string Prompt => prompt;

        /// <summary>Lo usa el generador de escena.</summary>
        public void Configure(Transform to, string promptText, float range)
        {
            destination = to;
            prompt = promptText;
            radius = range;
        }

        public override void Interact(PlayerInteractor player) => player.Teleport(destination);
    }
}
