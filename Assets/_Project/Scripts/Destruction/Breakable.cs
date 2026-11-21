using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Algo que un coche rompe al chocar (como en GTA): vallas (<see cref="BreakableFence"/>) y mobiliario urbano
    /// (<see cref="BreakableProp"/>: señales, farolas, semáforos, bocas de incendio, bancos). Lo llama
    /// <see cref="VehicleController"/> en <c>OnCollisionEnter</c> con la velocidad que llevaba antes del golpe; si se
    /// rompe, el coche recupera <see cref="SpeedKept"/> de esa velocidad y sigue.
    /// </summary>
    public abstract class Breakable : MonoBehaviour
    {
        [Tooltip("Velocidad mínima del coche (m/s) para romperlo; más despacio, solo choca.")]
        [SerializeField] protected float minSpeed = 4f;
        [Tooltip("Parte de la velocidad que conserva el coche al romperlo.")]
        [SerializeField] protected float speedKept = 0.85f;

        public float SpeedKept => speedKept;
        public bool Broken { get; private set; }

        /// <summary>Cuántas cosas se han roto desde que empezó la partida (para los tests).</summary>
        public static int BrokenCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCount() => BrokenCount = 0;

        public void SetStrength(float minimumSpeed, float kept)
        {
            minSpeed = minimumSpeed;
            speedKept = kept;
        }

        /// <summary>Lo rompe si <paramref name="velocity"/> llega a <see cref="minSpeed"/>. Los trozos no chocan con <paramref name="ignore"/> (el coche).</summary>
        public bool TryBreak(Vector3 velocity, Vector3 point, Collider[] ignore)
        {
            if (Broken || velocity.magnitude < minSpeed) return false;
            Broken = true;
            BrokenCount++;
            Break(velocity, point, ignore ?? new Collider[0]);
            NpcAlerts.Hit(point, null, null);
            return true;
        }

        protected abstract void Break(Vector3 velocity, Vector3 point, Collider[] ignore);

        protected static void Play(AudioClip clip, Vector3 at, float volume)
        {
            if (!clip) return;
            var go = new GameObject("SonidoRotura");
            go.transform.position = at;
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.spatialBlend = 1f;
            s.minDistance = 5f;
            s.maxDistance = 70f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.volume = volume;
            s.pitch = Random.Range(0.9f, 1.1f);
            s.Play();
            Destroy(go, clip.length / s.pitch + 0.1f);
        }
    }

    /// <summary>
    /// Trozo suelto de algo roto: lleva <c>Rigidbody</c>, no choca con el coche que lo ha roto y va en la capa
    /// <c>Ignore Raycast</c> (la cámara y las balas no lo ven). Hay como mucho <see cref="Max"/>; al pasar, se borran los más viejos.
    /// </summary>
    public sealed class Debris : MonoBehaviour
    {
        public const int Max = 160;
        static readonly Queue<GameObject> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => all.Clear();

        public static int Count
        {
            get
            {
                while (all.Count > 0 && !all.Peek()) all.Dequeue();
                return all.Count;
            }
        }

        public static Rigidbody Launch(GameObject go, float mass, Vector3 velocity, Vector3 spin, Collider[] ignore)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2; // Ignore Raycast
            var body = go.GetComponent<Rigidbody>();
            if (!body) body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = velocity;
            body.angularVelocity = spin;
            foreach (var mine in go.GetComponentsInChildren<Collider>())
                foreach (var other in ignore)
                    if (other) Physics.IgnoreCollision(mine, other);
            if (!go.GetComponent<Debris>()) go.AddComponent<Debris>();
            all.Enqueue(go);
            while (all.Count > Max)
            {
                var old = all.Dequeue();
                if (old) Destroy(old);
            }
            return body;
        }
    }
}
