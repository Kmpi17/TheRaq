using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Tren de mercancías que cruza el polígono del sur por la vía norte, de un borde del mundo al otro, cada pocos minutos
    /// y en sentidos alternos (lleva una locomotora en cada punta). Pita al acercarse a cada paso a nivel (largo, largo,
    /// corto, largo). Es cinemático y tiene colisión en la capa de vehículos: lo ven el tráfico y el coche del jugador.
    /// Los pasos a nivel (<see cref="LevelCrossing"/>) preguntan con <see cref="Approaching"/> si tienen que cerrarse.
    /// La lógica del recorrido está en <see cref="FreightRun"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FreightTrain : MonoBehaviour
    {
        [SerializeField] float fromX = -340f;
        [SerializeField] float toX = 340f;
        [SerializeField] float trackZ;
        [SerializeField] float railY;
        [SerializeField] float length = 200f;
        [SerializeField] float speed = 9f;
        [SerializeField] float waitMin = 45f, waitMax = 120f;
        [SerializeField] float firstWait = 15f;
        [Tooltip("x de los pasos a nivel, para pitar antes de cada uno.")]
        [SerializeField] float[] crossings = new float[0];

        Rigidbody body;
        AudioSource horn;
        FreightRun run;
        bool[] honked;

        public bool Running => run.Running;
        public float Center => run.Center;
        public float Length => length;
        public int Direction => run.Direction;

        public void Configure(float from, float to, float z, float y, float consistLength, float metersPerSecond, float[] crossingX)
        {
            fromX = from;
            toX = to;
            trackZ = z;
            railY = y;
            length = consistLength;
            speed = metersPerSecond;
            crossings = crossingX;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            horn = gameObject.AddComponent<AudioSource>();
            horn.clip = ProceduralSfx.TrainHorn();
            horn.spatialBlend = 1f;
            horn.minDistance = 25f;
            horn.maxDistance = 400f;
            horn.rolloffMode = AudioRolloffMode.Linear;
            horn.playOnAwake = false;
            honked = new bool[crossings.Length];
            run = FreightRun.Waiting(firstWait, fromX, toX, length);
            Place();
        }

        void FixedUpdate()
        {
            bool wasRunning = run.Running;
            run = FreightRun.Step(run, Time.fixedDeltaTime, speed, fromX, toX, length, Random.Range(waitMin, waitMax));
            if (run.Running && !wasRunning) for (int i = 0; i < honked.Length; i++) honked[i] = false;
            if (run.Running)
            {
                float head = run.Center + run.Direction * length / 2f;
                for (int i = 0; i < crossings.Length; i++)
                {
                    float ahead = (crossings[i] - head) * run.Direction;
                    if (!honked[i] && ahead > 0f && ahead < 90f)
                    {
                        honked[i] = true;
                        horn.Play();
                    }
                }
            }
            Place();
        }

        void Place()
        {
            var p = new Vector3(run.Center, railY, trackZ);
            var r = Quaternion.LookRotation(new Vector3(run.Direction, 0f, 0f));
            if (body) { body.MovePosition(p); body.MoveRotation(r); }
            else transform.SetPositionAndRotation(p, r);
        }

        /// <summary>
        /// ¿Tiene que estar cerrado el paso a nivel de <paramref name="x"/>? Sí si el tren lo está cruzando o su cabeza
        /// está a menos de <paramref name="warning"/> metros, viniendo hacia él.
        /// </summary>
        public bool Approaching(float x, float warning, float clearance) => run.Running && FreightRun.Blocks(run, length, x, warning, clearance);
    }

    /// <summary>Estado y avance del tren de mercancías, sin escena, para testearlo en EditMode.</summary>
    public struct FreightRun
    {
        public bool Running;
        public float Center;     // centro del tren (x)
        public int Direction;    // +1 hacia el este, −1 hacia el oeste
        public float Wait;       // segundos que quedan de espera (parado fuera del mundo)

        /// <summary>Esperando fuera del mundo; luego sale hacia el este.</summary>
        public static FreightRun Waiting(float seconds, float fromX, float toX, float length) =>
            new FreightRun { Running = false, Center = fromX - length / 2f, Direction = -1, Wait = seconds };

        /// <summary>
        /// Avanza <paramref name="dt"/> segundos. Al acabar la espera sale desde el extremo contrario al que llegó; al
        /// salir entero por el otro lado (con la cola más allá del borde) se para y espera <paramref name="nextWait"/>.
        /// </summary>
        public static FreightRun Step(FreightRun s, float dt, float speed, float fromX, float toX, float length, float nextWait)
        {
            float half = length / 2f;
            if (!s.Running)
            {
                s.Wait -= dt;
                if (s.Wait > 0f) return s;
                s.Running = true;
                s.Direction = -s.Direction;
                s.Center = s.Direction > 0 ? fromX - half : toX + half;
                return s;
            }
            s.Center += s.Direction * speed * dt;
            bool gone = s.Direction > 0 ? s.Center - half > toX : s.Center + half < fromX;
            if (gone)
            {
                s.Running = false;
                s.Wait = nextWait;
            }
            return s;
        }

        /// <summary>
        /// ¿Ocupa el tren el paso de <paramref name="x"/>, o llega en menos de <paramref name="warning"/> m? La cola deja
        /// el paso libre cuando ya está <paramref name="clearance"/> m más allá.
        /// </summary>
        public static bool Blocks(FreightRun s, float length, float x, float warning, float clearance)
        {
            float half = length / 2f;
            float head = s.Center + s.Direction * half, tail = s.Center - s.Direction * half;
            float ahead = (x - head) * s.Direction;     // > 0: el paso está delante de la cabeza
            float behindTail = (tail - x) * s.Direction; // > 0: la cola ya lo ha dejado atrás
            return ahead < warning && behindTail < clearance;
        }
    }
}
