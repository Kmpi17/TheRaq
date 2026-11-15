using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Escalar (Espacio): el personaje no salta, pero pasa por encima de vallas y muros bajos, trepa vallas altas y
    /// sube a bordes de hasta 2.4 m (ver <see cref="ClimbMath"/>). Mira delante (hacia donde se pide ir, o hacia
    /// donde mira) y solo cuenta lo que se ve: los colliders invisibles (límites, rampas) y lo que lleva
    /// <see cref="NoClimb"/> no se escalan. Al otro lado el suelo no puede estar más de 3 m por debajo del borde,
    /// así que no se salta a la trinchera de la autopista. Los coches se cruzan por encima (capó, maletero o techo
    /// bajo) de un lado al otro. Cuesta energía.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerClimb : MonoBehaviour
    {
        [SerializeField] float reach = 0.8f;
        [Tooltip("Por defecto ignora la capa 'Ignore Raycast' (el jugador). Los coches (capa Vehiculo) sí cuentan: se cruzan por encima.")]
        [SerializeField] LayerMask mask = ~(1 << 2);

        struct Plan
        {
            public ClimbKind Kind;
            public float Height, TopY;
            public Vector3 Start, Wall, Land, Forward;
            public Vector3 Near, Far; // coches: por dónde pasa por encima, al entrar y al salir
        }

        CharacterController controller;
        PlayerMovement movement;
        PlayerVitals vitals;
        Plan plan;

        public bool Climbing { get; private set; }
        /// <summary>Lo que se está haciendo ahora (None si no escala). Para la animación.</summary>
        public ClimbKind CurrentKind => Climbing ? plan.Kind : ClimbKind.None;
        /// <summary>Altura (Y del mundo) del borde que se agarra.</summary>
        public float EdgeY => plan.TopY;
        /// <summary>Punto del borde, en la cara de arriba, justo pasada la arista (donde van las manos), y hacia dónde se escala.</summary>
        public Vector3 EdgePoint => new Vector3(plan.Wall.x, plan.TopY, plan.Wall.z) + plan.Forward * ((controller ? controller.radius : 0.35f) + 0.13f);
        public Vector3 ClimbForward => plan.Forward;
        /// <summary>Avance de la escalada en curso (0–1).</summary>
        public float Progress { get; private set; }
        /// <summary>Lo que se puede hacer ahora mismo (para el aviso del HUD).</summary>
        public ClimbKind Available { get; private set; }
        public float NoStaminaTime { get; private set; } = -10f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            vitals = GetComponent<PlayerVitals>();
        }

        void Update()
        {
            Available = ClimbKind.None;
            if (Climbing || movement.Locked || !controller.enabled || !controller.isGrounded || GameUi.MenuOpen) return;
            var input = movement.InputDirection;
            var forward = input.sqrMagnitude > 0.1f ? input.normalized : transform.forward;
            if (!Probe(forward, out plan)) return;
            Available = plan.Kind;

            if (!GameInput.Actions.Climb.WasPressedThisFrame()) return;
            if (vitals && !vitals.Stamina.TrySpend(ClimbMath.StaminaCost(plan.Kind, plan.Height)))
            {
                NoStaminaTime = Time.time;
                return;
            }
            StartCoroutine(Run(plan));
        }

        /// <summary>Solo se escala lo que se ve y no lleva <see cref="NoClimb"/>.</summary>
        public static bool IsClimbable(Collider c) => SolidVisual.IsVisible(c) && !c.GetComponentInParent<NoClimb>();

        bool Probe(Vector3 forward, out Plan p)
        {
            p = default;
            var feet = transform.position;
            float r = controller.radius;

            // 1) Obstáculo delante, a la altura de las rodillas, con la cara casi vertical.
            if (!Physics.SphereCast(feet + Vector3.up * 0.55f, 0.15f, forward, out var hit, r + reach, mask, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(hit.normal.y) > 0.4f || !IsClimbable(hit.collider)) return false;
            var n = hit.normal;
            n.y = 0f;
            var dir = -n.normalized; // de frente a la pared

            // Coches: se cruzan de lado a lado por encima si no son muy altos.
            var car = hit.collider.GetComponentInParent<VehicleController>();
            if (car && ProbeVehicle(car, hit, dir, feet, out p)) return true;

            // 2) Parte de arriba, justo pasada la cara. Dos rayos: uno casi pegado a la cara, porque las vallas son
            //    tablas de 3 cm y el de 0.2 m caería al otro lado, y otro 0.2 m dentro. Vale el más alto.
            float probeY = feet.y + ClimbMath.MaxHeight + 0.4f;
            bool TopAt(float depth, out RaycastHit t)
            {
                var at = hit.point + dir * depth;
                return Physics.Raycast(new Vector3(at.x, probeY, at.z), Vector3.down, out t, ClimbMath.MaxHeight + 0.4f, mask, QueryTriggerInteraction.Ignore);
            }
            bool nearOk = TopAt(0.015f, out var nearTop), deepOk = TopAt(0.2f, out var deepTop);
            if (!nearOk && !deepOk) return false;
            var top = !deepOk || (nearOk && nearTop.point.y > deepTop.point.y) ? nearTop : deepTop;
            if (top.normal.y < 0.7f) return false;
            float h = top.point.y - feet.y;
            if (h < ClimbMath.MinHeight || h > ClimbMath.MaxHeight) return false;
            if (Physics.CheckSphere(top.point + Vector3.up * 0.45f, 0.3f, mask, QueryTriggerInteraction.Ignore)) return false; // hay algo encima

            // 3) ¿Grueso (se puede estar encima) o fino (valla, muro)?
            var far = hit.point + dir * 0.75f;
            bool thick = Physics.Raycast(new Vector3(far.x, probeY, far.z), Vector3.down, out var top2, ClimbMath.MaxHeight + 0.4f, mask, QueryTriggerInteraction.Ignore)
                         && Mathf.Abs(top2.point.y - top.point.y) < 0.25f;
            var kind = ClimbMath.Classify(h, thick);
            if (kind == ClimbKind.None) return false;

            Vector3 land;
            if (kind == ClimbKind.ClimbUp)
            {
                land = hit.point + dir * 0.6f;
                land.y = top.point.y;
            }
            else
            {
                var beyond = hit.point + dir * (0.9f + r);
                beyond.y = top.point.y + 0.3f;
                if (!Physics.Raycast(beyond, Vector3.down, out var ground, ClimbMath.MaxDrop + 0.3f, mask, QueryTriggerInteraction.Ignore)) return false;
                if (ground.normal.y < 0.7f || !IsFloor(ground.point)) return false;
                land = ground.point;
            }

            // 4) Sitio para el cuerpo al llegar y nada encima de la cabeza al subir.
            if (!RoomToLand(land, feet, h)) return false;

            var wall = hit.point - dir * (r + 0.05f);
            p = new Plan
            {
                Kind = kind, Height = h, TopY = top.point.y, Start = feet,
                Wall = new Vector3(wall.x, feet.y, wall.z), Land = land, Forward = dir,
            };
            return true;
        }

        /// <summary>
        /// Suelo de verdad donde caer: alrededor del punto (a un palmo en cruz) también hay suelo a esa altura. El
        /// pasamanos de 3 cm de otra valla o el borde de un muro no valen: antes, al saltar una valla junto a una
        /// esquina, el rayo de suelo daba en el pasamanos del tramo de al lado y el personaje acababa de pie encima.
        /// </summary>
        bool IsFloor(Vector3 point)
        {
            int solid = 0;
            foreach (var offset in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                var from = point + offset * 0.22f + Vector3.up * 0.3f;
                if (Physics.Raycast(from, Vector3.down, out var h, 0.55f, mask, QueryTriggerInteraction.Ignore) && h.normal.y > 0.7f) solid++;
            }
            return solid >= 3;
        }

        bool RoomToLand(Vector3 land, Vector3 feet, float height)
        {
            // Con el alto de pie aunque esté agachado: al escalar se levanta.
            float cr = controller.radius * 0.9f, standing = Mathf.Max(controller.height, movement.StandHeight);
            if (Physics.CheckCapsule(land + Vector3.up * (cr + 0.08f), land + Vector3.up * (standing - cr), cr, mask, QueryTriggerInteraction.Ignore)) return false;
            return !Physics.Raycast(feet + Vector3.up * (controller.height - 0.1f), Vector3.up, height + 0.3f, mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Coche: busca la cara de salida (un rayo de vuelta desde el otro lado contra los colliders del coche), mira lo
        /// más alto que hay en el camino y, si no pasa de <see cref="ClimbMath.SlideMax"/>, lo cruza por encima hasta el
        /// suelo del otro lado. Si es más alto (furgoneta, todoterreno por el techo) lo deja a la escalada normal, que sube encima.
        /// </summary>
        bool ProbeVehicle(VehicleController car, RaycastHit hit, Vector3 dir, Vector3 feet, out Plan p)
        {
            p = default;
            var colliders = car.GetComponentsInChildren<Collider>();
            const float span = 7f;
            var back = new Ray(new Vector3(hit.point.x, feet.y + 0.55f, hit.point.z) + dir * span, -dir);
            float nearest = float.MaxValue;
            foreach (var c in colliders)
                if (c is not WheelCollider && !c.isTrigger && c.Raycast(back, out var b, span) && b.distance < nearest) nearest = b.distance;
            if (nearest == float.MaxValue) return false;
            float width = span - nearest; // de la cara de entrada a la de salida
            if (width < 0.3f) return false;

            // Lo más alto del coche a lo largo del camino, cada 0.3 m.
            float topY = float.MinValue;
            for (float d = 0.05f; d <= width - 0.05f; d += 0.3f)
            {
                var at = hit.point + dir * d;
                var down = new Ray(new Vector3(at.x, feet.y + ClimbMath.MaxHeight + 0.5f, at.z), Vector3.down);
                foreach (var c in colliders)
                    if (c is not WheelCollider && !c.isTrigger && c.Raycast(down, out var t, ClimbMath.MaxHeight + 0.5f)) topY = Mathf.Max(topY, t.point.y);
            }
            if (topY == float.MinValue) return false;
            float h = topY - feet.y;
            if (ClimbMath.ClassifyVehicle(h) == ClimbKind.None) return false;
            if (Physics.CheckSphere(hit.point + dir * (width / 2f) + Vector3.up * (h + 1f), 0.3f, mask, QueryTriggerInteraction.Ignore))
                return false; // algo encima del coche (un techo, una rama)

            float r = controller.radius;
            var beyond = hit.point + dir * (width + r + 0.3f);
            beyond.y = topY + 0.3f;
            if (!Physics.Raycast(beyond, Vector3.down, out var ground, ClimbMath.MaxDrop + 0.3f, mask, QueryTriggerInteraction.Ignore)) return false;
            if (ground.normal.y < 0.7f || !IsFloor(ground.point)) return false;
            var land = ground.point;
            if (!RoomToLand(land, feet, h)) return false;

            var wall = hit.point - dir * (r + 0.05f);
            var near = hit.point + dir * 0.15f;
            var far = hit.point + dir * (width - 0.15f);
            p = new Plan
            {
                Kind = ClimbKind.SlideOver, Height = h, TopY = topY, Start = feet,
                Wall = new Vector3(wall.x, feet.y, wall.z), Land = land, Forward = dir,
                Near = new Vector3(near.x, topY + 0.1f, near.z), Far = new Vector3(far.x, topY + 0.1f, far.z),
            };
            return true;
        }

        IEnumerator Run(Plan p)
        {
            Climbing = true;
            movement.ForceStand(); // se levanta para escalar
            movement.Locked = true;
            controller.enabled = false;
            transform.rotation = Quaternion.LookRotation(p.Forward);

            // Recorrido: pegarse a la pared, subir hasta el borde y pasar (por encima o al otro lado).
            var points = new List<Vector3> { p.Start, p.Wall };
            var edge = new Vector3(p.Wall.x, p.TopY + 0.08f, p.Wall.z);
            if (p.Kind == ClimbKind.SlideOver)
            {
                // Por encima del coche, casi tumbado sobre el capó o el techo.
                points.Add(p.Near);
                points.Add(p.Far);
            }
            else if (p.Kind == ClimbKind.Vault)
            {
                var mid = Vector3.Lerp(p.Wall, p.Land, 0.5f);
                points.Add(new Vector3(mid.x, p.TopY + 0.15f, mid.z));
            }
            else
            {
                points.Add(edge);
                if (p.Kind == ClimbKind.ClimbOver)
                {
                    var mid = Vector3.Lerp(p.Wall, p.Land, 0.5f);
                    points.Add(new Vector3(mid.x, p.TopY + 0.2f, mid.z));
                }
            }
            points.Add(p.Land);

            float total = 0f;
            for (int i = 1; i < points.Count; i++) total += Vector3.Distance(points[i - 1], points[i]);
            float duration = ClimbMath.Duration(p.Kind, p.Height);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Progress = t / duration;
                transform.position = PointAlong(points, Mathf.SmoothStep(0f, 1f, t / duration) * total);
                yield return null;
            }
            transform.position = p.Land;

            controller.enabled = true;
            movement.ResetMotion();
            if (p.Kind is ClimbKind.Vault or ClimbKind.SlideOver) movement.SetPlanarVelocity(p.Forward * 3f); // sigue corriendo al caer
            movement.Locked = false;
            Progress = 1f;
            Climbing = false;
        }

        static Vector3 PointAlong(List<Vector3> points, float distance)
        {
            for (int i = 1; i < points.Count; i++)
            {
                float d = Vector3.Distance(points[i - 1], points[i]);
                if (distance <= d) return Vector3.Lerp(points[i - 1], points[i], d > 0f ? distance / d : 1f);
                distance -= d;
            }
            return points[^1];
        }
    }
}
