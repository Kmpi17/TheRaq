using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Cobertura (Q): el personaje se pega a una pared, un muro, un coche o cualquier obstáculo visible y opaco de al
    /// menos 0.85 m (las verjas y mallas no cubren: se ve a través). Si el obstáculo es bajo (menos de 1.45 m) se agacha. Se mueve de lado a lo largo de la pared y se
    /// para en las esquinas. Al apuntar (<see cref="Peeking"/>) se asoma: se levanta en coberturas bajas y, en las
    /// altas, sale de lado si está en una esquina. Se sale con Q, esprintando o tirando hacia atrás.
    /// Las balas son rayos, así que la cobertura protege de verdad.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerCover : MonoBehaviour
    {
        [SerializeField] float searchDistance = 1.2f;
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float crouchHeight = 1.15f;
        [SerializeField] float peekStep = 0.65f;
        [SerializeField] LayerMask mask = ~(1 << 2);

        const float LowTest = 0.85f, HighTest = 1.45f, WallProbe = 0.55f;

        CharacterController controller;
        PlayerMovement movement;
        Transform model;
        bool animated;
        Vector3 normal;
        float defaultHeight;
        Vector3 defaultCenter;
        float pullAway, peekOffset;
        int edgeSide;

        public bool InCover { get; private set; }
        public bool LowCover { get; private set; }
        public bool AtEdge { get; private set; }
        /// <summary>Agachado tras una cobertura baja (sin asomarse). Para la animación.</summary>
        public bool Crouched => InCover && LowCover && !Peeking;
        public bool Available { get; private set; }
        /// <summary>Lo pone el combate mientras se apunta.</summary>
        public bool Peeking { get; set; }
        public Vector3 WallNormal => normal;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            model = transform.Find("Modelo");
            animated = GetComponent<PlayerAnimator>();
            defaultHeight = controller.height;
            defaultCenter = controller.center;
        }

        void Update()
        {
            Available = false;
            if (!controller.enabled || GameUi.MenuOpen)
            {
                if (InCover && !controller.enabled) Exit();
                return;
            }
            if (InCover)
            {
                Tick();
                return;
            }
            if (movement.Locked || !controller.isGrounded) return;
            Available = Find(out var n, out bool low);
            if (Available && GameInput.Actions.Cover.WasPressedThisFrame()) Enter(n, low);
        }

        bool Find(out Vector3 wallNormal, out bool low)
        {
            wallNormal = Vector3.zero;
            low = false;
            var input = movement.InputDirection;
            var forward = input.sqrMagnitude > 0.1f ? input.normalized : transform.forward;
            var feet = transform.position;
            if (!Physics.SphereCast(feet + Vector3.up * LowTest, 0.2f, forward, out var hit, controller.radius + searchDistance, mask, QueryTriggerInteraction.Ignore)) return false;
            // Solo lo que se ve (no los límites invisibles) y no deja ver a través: detrás de una verja de barrotes o
            // de una malla no hay cobertura; en una esquina de casa, un muro, una valla de tablones o un coche, sí.
            // Lo que lleva NoClimb sí sirve de cobertura.
            if (Mathf.Abs(hit.normal.y) > 0.35f || !SolidVisual.IsVisible(hit.collider) || SolidVisual.IsSeeThrough(hit.collider)) return false;
            wallNormal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            float reachWall = hit.distance + 0.5f;
            low = !Physics.Raycast(feet + Vector3.up * HighTest, -wallNormal, reachWall + controller.radius, mask, QueryTriggerInteraction.Ignore);
            return true;
        }

        void Enter(Vector3 wallNormal, bool low)
        {
            InCover = true;
            LowCover = low;
            normal = wallNormal;
            pullAway = peekOffset = 0f;
            edgeSide = 0;
            movement.Locked = true;
            movement.ResetMotion();
            transform.rotation = Quaternion.LookRotation(-normal);
        }

        public void Exit()
        {
            if (!InCover) return;
            InCover = false;
            Peeking = false;
            AtEdge = false;
            SetCrouch(false);
            if (controller.enabled && peekOffset != 0f) controller.Move(-Tangent() * (edgeSide * peekOffset));
            peekOffset = 0f;
            movement.ResetMotion();
            movement.Locked = false;
        }

        Vector3 Tangent() => Vector3.Cross(Vector3.up, normal); // a la derecha mirando a la pared

        void Tick()
        {
            float dt = Time.deltaTime;
            var input = movement.InputDirection;
            var actions = GameInput.Actions;
            if (actions.Cover.WasPressedThisFrame() || actions.Sprint.WasPressedThisFrame())
            {
                Exit();
                return;
            }
            pullAway = Vector3.Dot(input, normal) > 0.6f ? pullAway + dt : 0f;
            if (pullAway > 0.3f)
            {
                Exit();
                return;
            }

            // La pared sigue ahí (si no, se sale) y se toma su normal de nuevo, por si es curva o un coche. Se mira tan
            // lejos como al buscarla: al entrar se puede estar a más de un metro, y el tirón de abajo lo pega a ella.
            var feet = transform.position;
            var probe = feet + Vector3.up * WallProbe;
            if (!Physics.Raycast(probe - Tangent() * (edgeSide * peekOffset), -normal, out var hit, controller.radius + searchDistance + 0.3f, mask, QueryTriggerInteraction.Ignore))
            {
                Exit();
                return;
            }
            normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            var tangent = Tangent();
            float wallReach = hit.distance + 0.3f; // para buscar la esquina: algo más allá de donde está la pared

            // De lado a lo largo de la pared, sin pasar de la esquina.
            var move = Vector3.zero;
            float side = Peeking ? 0f : Vector3.Dot(input, tangent);
            AtEdge = false;
            if (Mathf.Abs(side) > 0.15f)
            {
                int s = side > 0f ? 1 : -1;
                var ahead = probe + tangent * (s * (controller.radius + 0.12f));
                if (Physics.Raycast(ahead, -normal, wallReach, mask, QueryTriggerInteraction.Ignore)) move = tangent * (side * moveSpeed);
                else
                {
                    AtEdge = true;
                    edgeSide = s;
                }
            }
            else if (edgeSide != 0)
            {
                var ahead = probe + tangent * (edgeSide * (controller.radius + 0.12f)) - tangent * (edgeSide * peekOffset);
                AtEdge = !Physics.Raycast(ahead, -normal, wallReach, mask, QueryTriggerInteraction.Ignore);
                if (!AtEdge && peekOffset <= 0f) edgeSide = 0;
            }

            // Asomarse por la esquina en coberturas altas.
            float targetPeek = Peeking && !LowCover && edgeSide != 0 && (AtEdge || peekOffset > 0f) ? peekStep : 0f;
            float newPeek = Mathf.MoveTowards(peekOffset, targetPeek, 4f * dt);
            move += tangent * (edgeSide * (newPeek - peekOffset) / Mathf.Max(dt, 1e-5f));
            peekOffset = newPeek;

            // Pegado a la pared.
            float gap = hit.distance - controller.radius - 0.05f;
            if (peekOffset <= 0.01f) move += -normal * Mathf.Clamp(gap * 8f, -4f, 4f);
            controller.Move((move + Vector3.down * 2f) * dt);

            SetCrouch(LowCover && !Peeking);
            if (!Peeking) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(-normal), 720f * dt);
        }

        void SetCrouch(bool crouch)
        {
            float height = crouch ? crouchHeight : defaultHeight;
            if (Mathf.Approximately(controller.height, height)) return;
            controller.height = height;
            controller.center = crouch ? new Vector3(defaultCenter.x, height / 2f, defaultCenter.z) : defaultCenter;
            // Con PlayerAnimator, el agachado es una pose (rodillas dobladas); si no, se encoge el modelo.
            if (model && !animated) model.localScale = new Vector3(1f, crouch ? height / defaultHeight : 1f, 1f);
        }
    }
}
