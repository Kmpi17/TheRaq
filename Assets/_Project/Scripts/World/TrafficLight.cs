using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Semáforos de un cruce. Enciende y apaga las luces de sus hijos según <see cref="TrafficSignalMath"/>. Las luces
    /// se buscan por nombre (las pone el generador, <c>PrototypeSceneBuilder.StreetDetail.cs</c>):
    /// <c>Luz_Roja_X</c>, <c>Luz_Ambar_X</c>, <c>Luz_Verde_X</c>, <c>Peaton_Anda_X</c>, <c>Peaton_Mano_X</c> y lo mismo con
    /// <c>_Z</c>. X son los coches que van a lo largo de X y los peatones que cruzan en paralelo a ellos.
    /// Va fuera de <c>Mundo</c> (que es estático), porque cambia.
    /// </summary>
    public class TrafficLight : MonoBehaviour
    {
        [SerializeField] SignalTiming timing = SignalTiming.Default;
        [SerializeField] float offset; // para que los cruces no cambien a la vez

        readonly List<GameObject>[,] vehicle = new List<GameObject>[2, 3]; // [eje, SignalColor]
        readonly List<GameObject>[] walk = new List<GameObject>[2], hand = new List<GameObject>[2];
        readonly int[] shown = { -1, -1, -1, -1 };

        public void Configure(float secondsOffset) => offset = secondsOffset;

        /// <summary>Color que ven ahora (o hace <c>secondsAgo</c> s) los coches que van a lo largo de X (o de Z). Lo usa el tráfico (<see cref="TrafficCar"/>).</summary>
        public SignalColor VehicleColor(bool alongX, float secondsAgo = 0f) => TrafficSignalMath.Vehicle(Time.time - secondsAgo + offset, alongX, timing);

        void Awake()
        {
            for (int a = 0; a < 2; a++)
            {
                for (int c = 0; c < 3; c++) vehicle[a, c] = new List<GameObject>();
                walk[a] = new List<GameObject>();
                hand[a] = new List<GameObject>();
            }
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                int axis = n.EndsWith("_X") ? 0 : n.EndsWith("_Z") ? 1 : -1;
                if (axis < 0) continue;
                if (n.StartsWith("Luz_Roja")) vehicle[axis, (int)SignalColor.Red].Add(t.gameObject);
                else if (n.StartsWith("Luz_Ambar")) vehicle[axis, (int)SignalColor.Amber].Add(t.gameObject);
                else if (n.StartsWith("Luz_Verde")) vehicle[axis, (int)SignalColor.Green].Add(t.gameObject);
                else if (n.StartsWith("Peaton_Anda")) walk[axis].Add(t.gameObject);
                else if (n.StartsWith("Peaton_Mano")) hand[axis].Add(t.gameObject);
            }
        }

        void Update()
        {
            float time = Time.time + offset;
            for (int a = 0; a < 2; a++)
            {
                bool alongX = a == 0;
                int color = (int)TrafficSignalMath.Vehicle(time, alongX, timing);
                if (shown[a] != color)
                {
                    shown[a] = color;
                    for (int c = 0; c < 3; c++) SetAll(vehicle[a, c], c == color);
                }

                var w = TrafficSignalMath.Pedestrian(time, alongX, timing);
                bool handOn = w == WalkSignal.DontWalk || (w == WalkSignal.Flashing && TrafficSignalMath.FlashOn(time));
                int state = (w == WalkSignal.Walk ? 1 : 0) | (handOn ? 2 : 0);
                if (shown[2 + a] != state)
                {
                    shown[2 + a] = state;
                    SetAll(walk[a], w == WalkSignal.Walk);
                    SetAll(hand[a], handOn);
                }
            }
        }

        static void SetAll(List<GameObject> list, bool on)
        {
            foreach (var go in list) if (go.activeSelf != on) go.SetActive(on);
        }
    }
}
