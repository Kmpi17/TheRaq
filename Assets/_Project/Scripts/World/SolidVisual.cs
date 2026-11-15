using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Va en un objeto cuyos colliders están en él pero el modelo en los hijos (los coches): hace que esos colliders
    /// cuenten como visibles para escalar y cubrirse. También tiene la regla común de qué obstáculo es "de verdad".
    /// </summary>
    public sealed class SolidVisual : MonoBehaviour
    {
        /// <summary>
        /// Obstáculo que se ve: el collider tiene Renderer en su objeto o un <see cref="SolidVisual"/> en él o en un
        /// padre. Los colliders invisibles (límites, rampas), los triggers y los WheelCollider no cuentan.
        /// </summary>
        public static bool IsVisible(Collider c) =>
            c && !c.isTrigger && c is not WheelCollider && (c.GetComponent<Renderer>() || c.GetComponentInParent<SolidVisual>());

        /// <summary>
        /// Se ve a través: lleva <see cref="NoCover"/> o su material tiene recorte por alfa (verjas de barrotes, mallas,
        /// barandillas caladas). Detrás de eso no tiene sentido cubrirse.
        /// </summary>
        public static bool IsSeeThrough(Collider c)
        {
            if (!c) return false;
            if (c.GetComponentInParent<NoCover>()) return true;
            var r = c.GetComponent<Renderer>();
            if (!r) return false;
            var m = r.sharedMaterial;
            return m && m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f;
        }
    }
}
