using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Se pone en un objeto (o en un padre) que no sirve de cobertura aunque se vea. Lo que tiene un material con
    /// recorte por alfa (verjas, mallas, barandillas caladas) ya no cubre sin necesidad de esto.
    /// </summary>
    public sealed class NoCover : MonoBehaviour { }
}
