using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Algo con lo que el jugador hace algo pulsando F a pie (ascensor, armario, caja fuerte). <see cref="PlayerInteractor"/>
    /// elige el más cercano dentro de su <see cref="Radius"/>, enseña su <see cref="Prompt"/> en el HUD y llama a
    /// <see cref="Interact"/>. Mientras hay uno a mano, F no sube al coche.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected float radius = 1.4f;

        /// <summary>Todos los activos, para buscar el más cercano sin recorrer la escena.</summary>
        public static readonly List<Interactable> All = new();

        public float Radius => radius;
        /// <summary>Texto del aviso, por ejemplo "F: abrir el armario".</summary>
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerInteractor player);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        /// <summary>Más cercano a <paramref name="from"/> dentro de su radio (en horizontal y a menos de 2 m de altura), o null.</summary>
        public static Interactable FindNearest(Vector3 from, IEnumerable<Interactable> candidates)
        {
            Interactable best = null;
            float bestSqr = float.MaxValue;
            foreach (var c in candidates)
            {
                if (!c) continue;
                var d = c.transform.position - from;
                if (Mathf.Abs(d.y) > 2f) continue;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr <= c.radius * c.radius && sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = c;
                }
            }
            return best;
        }
    }
}
