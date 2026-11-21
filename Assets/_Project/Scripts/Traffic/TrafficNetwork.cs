using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Un tramo de calle entre dos nodos, en un sentido. Lo rellena el generador de la escena.</summary>
    [Serializable]
    public struct TrafficLinkData
    {
        public int From, To;
        [Tooltip("Puntos intermedios del eje de la calle (las curvas). Vacío si es recto.")]
        public Vector3[] Via;
        [Tooltip("Desplazamiento del carril a la derecha del eje.")]
        public float LaneOffset;
        public float SpeedLimit;
        [Tooltip("Qué hay al final: nada (calle con preferencia), semáforo o STOP.")]
        public TrafficGate Gate;
        [Tooltip("Paso a nivel que cruza el tramo (o ninguno), el centro de la vía y su medio ancho.")]
        public LevelCrossing Rail;
        public Vector3 RailPoint;
        public float RailHalf;
    }

    /// <summary>
    /// Red de calles por la que circula el tráfico de la ciudad (<see cref="TrafficCar"/>). Nodos en los cruces y en los
    /// extremos de las calles, tramos de un sentido entre ellos y, en algunos nodos, el <see cref="TrafficLight"/> que los
    /// regula. Los carriles (líneas desplazadas a la derecha y recortadas al borde de cada cruce) se calculan al empezar.
    /// Los nodos de salida están en el borde del mundo: un coche que llega a uno desaparece. Los tramos que llevan a un
    /// callejón sin salida no se eligen nunca (los coches no dan media vuelta).
    /// </summary>
    public class TrafficNetwork : MonoBehaviour
    {
        [SerializeField] Vector3[] nodes = new Vector3[0];
        [Tooltip("Medio ancho del cruce: los carriles acaban y empiezan a esa distancia del nodo.")]
        [SerializeField] float[] nodeRadius = new float[0];
        [SerializeField] bool[] nodeExit = new bool[0];
        [SerializeField] TrafficLight[] nodeSignal = new TrafficLight[0];
        [SerializeField] TrafficLinkData[] links = new TrafficLinkData[0];

        public sealed class Lane
        {
            public int Index;
            public TrafficLinkData Data;
            public Vector3[] Points;
            public float[] Cumulative;
            public float Length => Cumulative[Cumulative.Length - 1];
            public Vector3 EndDirection, StartDirection;
            public bool LeadsToDeadEnd;
            /// <summary>Distancia del carril a la que empieza la vía de un paso a nivel (−1: no hay).</summary>
            public float RailStart = -1f;

            /// <summary>¿Hay un paso a nivel cerrado en este carril, entre <paramref name="from"/> y el principio de la vía?</summary>
            public bool RailClosedAhead(float from) => RailStart >= 0f && from < RailStart && Data.Rail && Data.Rail.Closed;

            public Vector3 Evaluate(float s, out Vector3 tangent)
            {
                int i = 1;
                while (i < Cumulative.Length - 1 && Cumulative[i] < s) i++;
                float seg = Cumulative[i] - Cumulative[i - 1];
                float t = seg > 1e-4f ? Mathf.Clamp01((s - Cumulative[i - 1]) / seg) : 0f;
                tangent = (Points[i] - Points[i - 1]).normalized;
                return Vector3.Lerp(Points[i - 1], Points[i], t);
            }
        }

        Lane[] lanes;
        List<int>[] outgoing;
        int[] occupancy;

        public IReadOnlyList<Lane> Lanes { get { Prepare(); return lanes; } }
        public int NodeCount => nodes.Length;
        public Vector3 Node(int i) => nodes[i];
        public bool IsExit(int node) => nodeExit[node];
        public TrafficLight Signal(int node) => node < nodeSignal.Length ? nodeSignal[node] : null;

        /// <summary>Coches dentro del cruce (girando o cruzando). Para que un STOP espere a que esté libre.</summary>
        public int Occupancy(int node) { Prepare(); return occupancy[node]; }
        public void Enter(int node) { Prepare(); occupancy[node]++; }
        public void Leave(int node) { Prepare(); occupancy[node] = Mathf.Max(0, occupancy[node] - 1); }

        /// <summary>Lo usa el generador de la escena.</summary>
        public void Configure(Vector3[] nodePositions, float[] radius, bool[] exits, TrafficLight[] signals, TrafficLinkData[] linkData)
        {
            nodes = nodePositions;
            nodeRadius = radius;
            nodeExit = exits;
            nodeSignal = signals;
            links = linkData;
            lanes = null;
        }

        void Awake() => Prepare();

        void Prepare()
        {
            if (lanes != null) return;
            outgoing = new List<int>[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) outgoing[i] = new List<int>();
            occupancy = new int[nodes.Length];
            lanes = new Lane[links.Length];
            for (int i = 0; i < links.Length; i++)
            {
                lanes[i] = BuildLane(i, links[i]);
                outgoing[links[i].From].Add(i);
            }
            MarkDeadEnds();
        }

        Lane BuildLane(int index, TrafficLinkData data)
        {
            var axis = new List<Vector3> { nodes[data.From] };
            if (data.Via != null) axis.AddRange(data.Via);
            axis.Add(nodes[data.To]);

            // Recorta el eje al borde de los cruces (en las salidas sigue hasta el nodo, que ya está fuera de la vista).
            float trimStart = nodeExit[data.From] ? 0f : nodeRadius[data.From];
            float trimEnd = nodeExit[data.To] ? 0f : nodeRadius[data.To];
            axis[0] = Vector3.MoveTowards(axis[0], axis[1], trimStart);
            int last = axis.Count - 1;
            axis[last] = Vector3.MoveTowards(axis[last], axis[last - 1], trimEnd);

            var points = TrafficMath.OffsetRight(axis, data.LaneOffset);
            var cumulative = new float[points.Length];
            for (int i = 1; i < points.Length; i++) cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            var lane = new Lane
            {
                Index = index,
                Data = data,
                Points = points,
                Cumulative = cumulative,
                StartDirection = (points[1] - points[0]).normalized,
                EndDirection = (points[points.Length - 1] - points[points.Length - 2]).normalized,
            };
            if (data.Rail)
            {
                // Distancia a lo largo del carril hasta el centro de la vía, y de ahí al borde del lecho por donde se llega.
                float best = float.MaxValue, at = 0f;
                for (int i = 1; i < points.Length; i++)
                {
                    var a = points[i - 1];
                    var ab = points[i] - a;
                    float t = Mathf.Clamp01(Vector3.Dot(data.RailPoint - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                    float d = Vector3.Distance(a + ab * t, data.RailPoint);
                    if (d < best) { best = d; at = cumulative[i - 1] + ab.magnitude * t; }
                }
                lane.RailStart = Mathf.Max(0f, at - data.RailHalf);
            }
            return lane;
        }

        /// <summary>
        /// Poda los callejones: un tramo es malo si al llegar a su final (que no sea una salida del mundo) solo se puede
        /// seguir dando media vuelta o por tramos malos. Se repite hasta que no cambia nada.
        /// </summary>
        void MarkDeadEnds()
        {
            foreach (var lane in lanes) lane.LeadsToDeadEnd = false;
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var lane in lanes)
                {
                    if (lane.LeadsToDeadEnd || nodeExit[lane.Data.To]) continue;
                    bool ok = false;
                    foreach (int o in outgoing[lane.Data.To])
                        if (lanes[o].Data.To != lane.Data.From && !lanes[o].LeadsToDeadEnd) { ok = true; break; }
                    if (!ok) { lane.LeadsToDeadEnd = true; changed = true; }
                }
            }
        }

        static readonly List<Vector3> optionDirs = new List<Vector3>();
        static readonly List<bool> optionUsable = new List<bool>();
        static readonly List<int> optionIndex = new List<int>();

        /// <summary>
        /// El tramo que sigue a <paramref name="lane"/> al llegar a su nodo final, al azar (ver
        /// <see cref="TrafficMath.ChooseNext"/>). −1 si es una salida o no hay por dónde seguir.
        /// </summary>
        public int ChooseNext(Lane lane, float roll)
        {
            Prepare();
            int node = lane.Data.To;
            if (nodeExit[node]) return -1;
            optionDirs.Clear(); optionUsable.Clear(); optionIndex.Clear();
            foreach (int o in outgoing[node])
            {
                optionDirs.Add(lanes[o].StartDirection);
                optionUsable.Add(!lanes[o].LeadsToDeadEnd && lanes[o].Data.To != lane.Data.From);
                optionIndex.Add(o);
            }
            int pick = TrafficMath.ChooseNext(lane.EndDirection, optionDirs, optionUsable, roll);
            return pick < 0 ? -1 : optionIndex[pick];
        }

        /// <summary>El siguiente tramo de una ruta fija (el autobús): el que va de este nodo a <paramref name="nextNode"/>.</summary>
        public int LinkBetween(int from, int to)
        {
            Prepare();
            foreach (int o in outgoing[from]) if (lanes[o].Data.To == to) return o;
            return -1;
        }

        void OnDrawGizmosSelected()
        {
            Prepare();
            foreach (var lane in lanes)
            {
                Gizmos.color = lane.LeadsToDeadEnd ? Color.red : lane.Data.Gate == TrafficGate.Signal ? Color.green : lane.Data.Gate == TrafficGate.Stop ? Color.yellow : Color.cyan;
                for (int i = 1; i < lane.Points.Length; i++) Gizmos.DrawLine(lane.Points[i - 1] + Vector3.up * 0.3f, lane.Points[i] + Vector3.up * 0.3f);
            }
        }
    }
}
