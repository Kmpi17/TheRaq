using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Paso a nivel que funciona: cuando se acerca un <see cref="FreightTrain"/>, las luces rojas parpadean alternas, suena
    /// la campana y, a los pocos segundos, bajan las barreras; cuando la cola lo deja atrás, suben y se apaga todo. El
    /// tráfico mira <see cref="Closed"/> en los tramos que cruzan la vía (<c>TrafficLinkData.Rail</c>) y espera antes de la
    /// vía. Va fuera de "Mundo", porque las barreras se mueven.
    /// </summary>
    public class LevelCrossing : MonoBehaviour
    {
        [SerializeField] float crossingX;
        [SerializeField] FreightTrain[] trains = new FreightTrain[0];
        [SerializeField] Transform[] arms = new Transform[0];
        [Tooltip("Giro de cada barrera bajada (levantada: sin giro).")]
        [SerializeField] Quaternion[] armsDown = new Quaternion[0];
        [SerializeField] Renderer[] lightsA = new Renderer[0];
        [SerializeField] Renderer[] lightsB = new Renderer[0];
        [SerializeField] float warning = 85f;
        [SerializeField] float clearance = 6f;
        [SerializeField] float lowerDelay = 2.5f;
        [SerializeField] float armSeconds = 5f;

        static AudioClip bellClip;
        AudioSource bell;
        float closedFor, arm; // arm: 0 levantada, 1 bajada
        bool lit = true;

        public bool Closed { get; private set; }
        public float ArmLowered => arm;

        public void Configure(float x, FreightTrain[] freight, Transform[] gateArms, Quaternion[] down, Renderer[] a, Renderer[] b)
        {
            crossingX = x;
            trains = freight;
            arms = gateArms;
            armsDown = down;
            lightsA = a;
            lightsB = b;
        }

        void Awake()
        {
            if (!bellClip) bellClip = ProceduralSfx.CrossingBell();
            // La campana suena en el paso (el objeto está en el origen y sus piezas, en coordenadas del mundo).
            var at = Vector3.zero;
            foreach (var a in arms) if (a) at += a.position / arms.Length;
            var source = new GameObject("Campana");
            source.transform.SetParent(transform, false);
            source.transform.position = at + Vector3.up * 3f;
            bell = source.AddComponent<AudioSource>();
            bell.clip = bellClip;
            bell.loop = true;
            bell.spatialBlend = 1f;
            bell.minDistance = 8f;
            bell.maxDistance = 90f;
            bell.playOnAwake = false;
            Apply(false, 0f);
        }

        void Update()
        {
            bool closed = false;
            foreach (var t in trains) if (t && t.Approaching(crossingX, warning, clearance)) closed = true;
            Closed = closed;
            closedFor = closed ? closedFor + Time.deltaTime : 0f;
            float target = closed && closedFor > lowerDelay ? 1f : 0f;
            arm = Mathf.MoveTowards(arm, target, Time.deltaTime / armSeconds);
            bool warn = closed || arm > 0f;
            Apply(warn, arm);
            if (warn && !bell.isPlaying) bell.Play();
            else if (!warn && bell.isPlaying) bell.Stop();
        }

        void Apply(bool warn, float lowered)
        {
            float k = Mathf.SmoothStep(0f, 1f, lowered);
            for (int i = 0; i < arms.Length && i < armsDown.Length; i++)
                if (arms[i]) arms[i].localRotation = Quaternion.Slerp(Quaternion.identity, armsDown[i], k);
            bool phase = Mathf.Repeat(Time.time, 1f) < 0.5f;
            bool aOn = warn && phase, bOn = warn && !phase;
            if (lit != warn || warn)
            {
                foreach (var r in lightsA) if (r && r.enabled != aOn) r.enabled = aOn;
                foreach (var r in lightsB) if (r && r.enabled != bOn) r.enabled = bOn;
                lit = warn;
            }
        }
    }
}
