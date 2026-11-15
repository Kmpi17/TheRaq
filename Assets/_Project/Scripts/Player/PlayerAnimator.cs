using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Animación procedural del personaje (no hay Animator ni clips: el modelo es de piezas). Mueve las articulaciones
    /// que crea el generador dentro de <c>Modelo</c>: <c>Cuerpo</c> (pelvis), <c>Cadera/Rodilla/Tobillo</c> Izq/Der,
    /// <c>Columna</c>, <c>Craneo</c> y <c>Hombro/Codo</c> Izq/Der. Poses:
    /// <list type="bullet">
    /// <item>quieto (respira y se balancea un poco), andar, correr y esprintar con el ciclo de <see cref="AnimationMath.Gait"/>;</item>
    /// <item>en el aire, escalando, agachado tras una cobertura baja o pegado a una alta, y muerto;</item>
    /// <item>con IK de brazo (<see cref="AnimationMath.SolveTwoBone"/>): la mano derecha en la pistola (y la izquierda
    /// sujetándola al apuntar o recargando), puñetazos con guardia, y las dos manos en la puerta al forzar un coche.</item>
    /// </list>
    /// Corre en LateUpdate, después de que el combate coloque la pistola.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] float runSpeed = 4.4f, sprintSpeed = 7.2f;
        [Tooltip("Rapidez con la que las articulaciones van a su pose (más alto, más seco).")]
        [SerializeField] float sharpness = 14f;

        Transform model, pelvis, spine, head, pistol;
        readonly Transform[] hip = new Transform[2], knee = new Transform[2], ankle = new Transform[2];
        readonly Transform[] shoulder = new Transform[2], elbow = new Transform[2], hand = new Transform[2];
        Vector3 pelvisRest;

        PlayerMovement movement;
        CharacterController controller;
        PlayerClimb climb;
        PlayerCover cover;
        PlayerCombat combat;
        PlayerVitals vitals;
        VehicleEntry entry;

        float phase, airTime, moveBlend, runBlend;
        Vector3 moveDir = Vector3.forward; // dirección del movimiento en el espacio del cuerpo, suavizada
        Vector3 modelRest;
        float modelYaw, coverFacing = 1f;

        /// <summary>Fase del ciclo de paso (radianes), para tests y depurar.</summary>
        public float Phase => phase;

        void Awake()
        {
            model = transform.Find("Modelo");
            Transform Find(string name) => model ? FindDeep(model, name) : null;
            pelvis = Find("Cuerpo");
            spine = Find("Columna");
            head = Find("Craneo");
            pistol = Find("Pistola");
            string[] sides = { "Izq", "Der" };
            for (int i = 0; i < 2; i++)
            {
                hip[i] = Find("Cadera" + sides[i]);
                knee[i] = Find("Rodilla" + sides[i]);
                ankle[i] = Find("Tobillo" + sides[i]);
                shoulder[i] = Find("Hombro" + sides[i]);
                elbow[i] = Find("Codo" + sides[i]);
                hand[i] = Find("Mano" + sides[i]);
            }
            if (!pelvis || !spine || !hip[0] || !hip[1] || !shoulder[0] || !shoulder[1])
            {
                enabled = false; // modelo antiguo sin articulaciones
                return;
            }
            pelvisRest = pelvis.localPosition;
            modelRest = model.localPosition;
            movement = GetComponent<PlayerMovement>();
            controller = GetComponent<CharacterController>();
            climb = GetComponent<PlayerClimb>();
            cover = GetComponent<PlayerCover>();
            combat = GetComponent<PlayerCombat>();
            vitals = GetComponent<PlayerVitals>();
            entry = GetComponent<VehicleEntry>();
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        void LateUpdate()
        {
            if (controller && !controller.enabled && !(vitals && vitals.Dead) && !(climb && climb.Climbing)) return; // en el coche no se ve
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-sharpness * dt);

            bool dead = vitals && vitals.Dead;
            bool climbing = climb && climb.Climbing;
            bool inCover = cover && cover.InCover;
            bool crouched = cover && cover.Crouched;
            bool grounded = !controller || !controller.enabled || controller.isGrounded;
            airTime = grounded || climbing ? 0f : airTime + dt;
            bool inAir = airTime > 0.18f;

            // Velocidad: la del movimiento a pie, o la real del controlador en cobertura (se mueve de lado).
            var velocity = movement ? movement.PlanarVelocity : Vector3.zero;
            if (inCover && controller && controller.enabled) velocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            float speed = velocity.magnitude;
            bool still = dead || climbing || inAir;
            moveBlend = Mathf.MoveTowards(moveBlend, still ? 0f : Mathf.Clamp01(speed / 1.2f), 6f * dt);
            runBlend = Mathf.MoveTowards(runBlend, Mathf.Clamp01((speed - runSpeed * 0.5f) / (sprintSpeed - runSpeed * 0.5f)), 3f * dt);
            phase = AnimationMath.AdvancePhase(phase, still ? 0f : speed, runBlend, dt);
            var g = AnimationMath.Gait(phase, moveBlend, inCover ? 0f : runBlend);

            // Cobertura alta sin asomarse: de lado, con el hombro contra la pared y mirando a lo largo de ella hacia
            // donde se movió por última vez (no abrazado a la pared). Al apuntar se gira para asomarse.
            bool sideOn = inCover && !cover.LowCover && !cover.Peeking && !dead;
            if (inCover && speed > 0.3f)
            {
                var alongWall = transform.InverseTransformDirection(velocity);
                if (Mathf.Abs(alongWall.x) > 0.3f) coverFacing = Mathf.Sign(alongWall.x);
            }
            if (!dead)
            {
                modelYaw = Mathf.MoveTowards(modelYaw, sideOn ? coverFacing * 75f : 0f, 420f * dt);
                model.localRotation = Quaternion.Euler(0f, modelYaw, 0f);
                // Se separa un poco de la pared para que el hombro no se meta en ella.
                model.localPosition = modelRest + Vector3.back * (0.14f * Mathf.Abs(modelYaw) / 75f);
            }

            // Hacia dónde se mueve respecto al cuerpo (suavizado): apuntando o en cobertura se anda de lado y hacia
            // atrás, y las piernas tienen que dar el paso hacia allí, no hacia delante.
            if (speed > 0.3f)
            {
                var local = model.InverseTransformDirection(velocity);
                local.y = 0f;
                moveDir = Vector3.Slerp(moveDir, local.normalized, 1f - Mathf.Exp(-10f * dt));
            }
            else moveDir = Vector3.Slerp(moveDir, Vector3.forward, 1f - Mathf.Exp(-4f * dt));
            float ahead = moveDir.z;                  // 1 hacia delante, −1 hacia atrás, 0 de lado
            float forwardOnly = Mathf.Max(0f, ahead); // inclinarse y torcer el tronco solo al ir hacia delante
            var swingAxis = AnimationMath.SwingAxis(moveDir);

            float t = Time.time;
            float idle = 1f - moveBlend;
            float breathe = Mathf.Sin(t * 1.8f) * 1.4f * idle;
            bool aiming = combat && combat.WeaponRaised && !dead && !climbing;
            float aimPitch = aiming ? combat.AimPitch : 0f;

            // ---------- Piernas, pelvis, columna y cabeza ----------
            var hipA = new[] { g.HipL, g.HipR };
            var kneeA = new[] { g.KneeL, g.KneeR };
            var ankleA = new float[2];
            var spread = new float[2]; // abrir la pierna hacia fuera (grados)
            float drop = g.Bob, lean = g.Lean * forwardOnly + breathe;
            float pelvisYaw = g.PelvisYaw * Mathf.Abs(ahead), spineYaw = g.SpineYaw * Mathf.Abs(ahead);
            float headPitch = -g.Lean * 0.6f * forwardOnly;
            bool locomotion = false;
            if (crouched)
            {
                drop = -0.36f;
                for (int i = 0; i < 2; i++)
                {
                    hipA[i] = -78f + (i == 0 ? -6f : 6f);
                    kneeA[i] = 118f;
                    ankleA[i] = -38f;
                    spread[i] = 8f;
                }
                // Agachado y moviéndose de lado: pasitos cortos hacia donde va.
                if (moveBlend > 0.05f)
                    for (int i = 0; i < 2; i++)
                        spread[i] += Mathf.Sin(phase + i * Mathf.PI) * 10f * moveBlend * Mathf.Abs(moveDir.x);
                lean = 22f + breathe;
                headPitch = -16f;
            }
            else if (climbing)
            {
                var kind = climb.CurrentKind;
                float below = climb.EdgeY - transform.position.y; // los pies, por debajo del borde
                if (kind is ClimbKind.Vault or ClimbKind.SlideOver)
                {
                    // Por encima con una mano apoyada: piernas recogidas y el cuerpo echado hacia delante.
                    hipA = new[] { -85f, -55f };
                    kneeA = new[] { 115f, 105f };
                    ankleA = new[] { -20f, -20f };
                    spread = new[] { 6f, 14f };
                    lean = 26f;
                    headPitch = -12f;
                }
                else if (below > 0.4f)
                {
                    // Colgado del borde y trepando: los pies buscan apoyo en la pared, alternando.
                    float sc = Mathf.Sin(t * 10f);
                    hipA = new[] { -45f + 22f * sc, -45f - 22f * sc };
                    kneeA = new[] { 85f - 25f * sc, 85f + 25f * sc };
                    ankleA = new[] { -10f, -10f };
                    lean = 6f;
                    headPitch = -22f; // mira al borde
                }
                else if (below < -0.05f)
                {
                    // Ya arriba: se endereza y da el paso para pasar o bajar.
                    hipA = new[] { -22f, -4f };
                    kneeA = new[] { 35f, 12f };
                    ankleA = new[] { -8f, -4f };
                    lean = 8f;
                    headPitch = 0f;
                }
                else
                {
                    // Encaramándose: una rodilla sobre el borde y el pecho echado encima.
                    hipA = new[] { -100f, -12f };
                    kneeA = new[] { 125f, 25f };
                    ankleA = new[] { -20f, 10f };
                    lean = 34f;
                    headPitch = -8f;
                }
            }
            else if (inAir)
            {
                hipA = new[] { -28f, -8f };
                kneeA = new[] { 45f, 25f };
                ankleA = new[] { -10f, -10f };
                lean = 4f;
            }
            else if (dead)
            {
                hipA = new[] { -4f, 6f };
                kneeA = new[] { 12f, 4f };
                ankleA = new[] { 0f, 0f };
                lean = 0f;
                headPitch = 10f;
            }
            else
            {
                locomotion = true;
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    hipA[i] = AnimationMath.SideStepHip(hipA[i], side, moveDir);
                    spread[i] = 2f * idle;
                }
                // Apuntando parado: piernas abiertas, la izquierda algo adelantada, rodillas sueltas, y el tronco y la
                // cabeza siguiendo la mira arriba y abajo.
                if (aiming)
                {
                    hipA[0] -= 9f * idle;
                    hipA[1] += 6f * idle;
                    for (int i = 0; i < 2; i++)
                    {
                        kneeA[i] += 9f * idle;
                        spread[i] += 4f * idle;
                    }
                    drop -= 0.02f * idle;
                    lean += 3f + aimPitch * 0.35f;
                    headPitch += aimPitch * 0.4f;
                }
                // El pie, casi plano: compensa la cadera (en su eje) y la rodilla.
                for (int i = 0; i < 2; i++) ankleA[i] = -(hipA[i] * ahead + kneeA[i]) * 0.6f;
            }

            pelvis.localPosition = Vector3.Lerp(pelvis.localPosition, pelvisRest + Vector3.up * drop, k);
            Blend(pelvis, Quaternion.Euler(0f, pelvisYaw, 0f), k);
            Blend(spine, Quaternion.Euler(lean, spineYaw, 0f), k);
            if (head) Blend(head, Quaternion.Euler(headPitch, -spineYaw * 0.5f, 0f), k);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var axis = locomotion ? swingAxis : Vector3.right;
                Blend(hip[i], Quaternion.AngleAxis(hipA[i], axis) * Quaternion.Euler(0f, 0f, side * spread[i]), k);
                if (knee[i]) Blend(knee[i], Quaternion.Euler(kneeA[i], 0f, 0f), k);
                if (ankle[i]) Blend(ankle[i], Quaternion.Euler(ankleA[i], 0f, 0f), k);
            }

            // ---------- Brazos: primero la pose de movimiento (FK) y encima el IK de lo que haga con las manos ----------
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float sway = Mathf.Sin(t * 1.8f + i) * 2f * idle;
                Quaternion sh, el;
                if (dead)
                {
                    sh = Quaternion.Euler(-10f, 0f, side * 65f);
                    el = Quaternion.Euler(-10f, 0f, 0f);
                }
                else if (climbing)
                {
                    // La mano que no se apoya: abierta para equilibrarse al saltar, o bajando tras subir.
                    bool vault = climb.CurrentKind is ClimbKind.Vault or ClimbKind.SlideOver;
                    sh = vault ? Quaternion.Euler(-35f, 0f, side * 60f) : Quaternion.Euler(-25f, 0f, side * 18f);
                    el = Quaternion.Euler(vault ? -25f : -40f, 0f, 0f);
                }
                else if (inAir)
                {
                    sh = Quaternion.Euler(-35f, 0f, side * 35f);
                    el = Quaternion.Euler(-30f, 0f, 0f);
                }
                else if (crouched)
                {
                    sh = Quaternion.Euler(-35f, 0f, side * 10f);
                    el = Quaternion.Euler(-75f, 0f, 0f);
                }
                else if (sideOn)
                {
                    // De lado contra la pared: brazos recogidos delante, listos.
                    sh = Quaternion.Euler(-22f, 0f, side * 8f);
                    el = Quaternion.Euler(-60f, 0f, 0f);
                }
                else if (inCover)
                {
                    // Asomándose: de cara, con las manos cerca del pecho.
                    sh = Quaternion.Euler(-45f, 0f, side * 10f);
                    el = Quaternion.Euler(-70f, 0f, 0f);
                }
                else
                {
                    // Los brazos se balancean al ir hacia delante o atrás; de lado, casi quietos.
                    float swing = (i == 0 ? g.ShoulderL : g.ShoulderR) * Mathf.Max(0.25f, Mathf.Abs(ahead));
                    sh = Quaternion.Euler(swing + sway, 0f, side * 5f);
                    el = Quaternion.Euler((i == 0 ? g.ElbowL : g.ElbowR) - 8f * idle, 0f, 0f);
                }

                if (!dead && HandTarget(i, out var target)) SolveArm(i, target, out sh, out el);
                Blend(shoulder[i], sh, Mathf.Min(1f, k * 1.6f));
                if (elbow[i]) Blend(elbow[i], el, Mathf.Min(1f, k * 1.6f));
            }
        }

        /// <summary>Dónde va la mano <paramref name="i"/> (0 izquierda, 1 derecha) si está haciendo algo, en el mundo.</summary>
        bool HandTarget(int i, out Vector3 target)
        {
            target = default;
            float side = i == 0 ? -1f : 1f;
            float t = Time.time;

            // Escalando: las manos agarran el borde (las dos al trepar; una, la izquierda, al saltar por encima) hasta
            // que el cuerpo lo ha pasado. Si el borde queda lejos, el brazo se estira hacia él.
            if (climb && climb.Climbing)
            {
                var forward = climb.ClimbForward;
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                var edge = climb.EdgePoint + Vector3.up * 0.02f;
                float past = Vector3.Dot(transform.position - edge, forward);
                if (climb.CurrentKind is ClimbKind.Vault or ClimbKind.SlideOver)
                {
                    if (i != 0 || past > 0.35f) return false;
                    target = edge - right * 0.1f;
                    return true;
                }
                if (past > 0.45f || transform.position.y > climb.EdgeY + 0.1f) return false;
                target = edge + right * (side * 0.22f);
                return true;
            }

            // Forzar la puerta de un coche: las dos manos delante, a la altura del pecho, forcejeando.
            if (entry && entry.Forcing)
            {
                target = model.TransformPoint(new Vector3(side * 0.14f, 1.12f + Mathf.Sin(t * 22f + i) * 0.03f, 0.48f + Mathf.Sin(t * 17f) * 0.03f));
                return true;
            }
            if (!combat) return false;

            // Puñetazos: el puño sale de la guardia hacia delante; la otra mano protege la cara.
            if (combat.Punching)
            {
                int punchHand = combat.PunchSide > 0 ? 1 : 0;
                float e = combat.PunchExtend;
                target = i == punchHand
                    ? model.TransformPoint(new Vector3(side * Mathf.Lerp(0.2f, 0.06f, e), 1.35f + (combat.PunchIsHook ? 0.08f * e : 0f), 0.2f + 0.48f * e))
                    : model.TransformPoint(new Vector3(side * 0.14f, 1.42f, 0.24f));
                return true;
            }

            // Pistola: la derecha la empuña siempre; la izquierda la sujeta al apuntar o al recargar.
            if (pistol && pistol.gameObject.activeInHierarchy)
            {
                var grip = pistol.TransformPoint(new Vector3(0f, -0.07f, -0.04f));
                if (i == 1)
                {
                    target = grip;
                    return true;
                }
                if (combat.WeaponRaised || combat.Reloading)
                {
                    target = grip + model.TransformVector(new Vector3(-0.06f, -0.02f, -0.02f));
                    return true;
                }
            }
            return false;
        }

        void SolveArm(int i, Vector3 worldTarget, out Quaternion shoulderRot, out Quaternion elbowRot)
        {
            var parent = shoulder[i].parent;
            var root = shoulder[i].localPosition;
            var target = parent.InverseTransformPoint(worldTarget);
            float upper = elbow[i] ? elbow[i].localPosition.magnitude : 0.3f;
            float lower = hand[i] ? hand[i].localPosition.magnitude : 0.3f;
            float side = i == 0 ? -1f : 1f;
            AnimationMath.SolveTwoBone(root, target, upper, lower, new Vector3(side * 0.5f, -0.6f, -1f), out var up, out var low);
            shoulderRot = Quaternion.FromToRotation(Vector3.down, up);
            elbowRot = Quaternion.FromToRotation(Vector3.down, Quaternion.Inverse(shoulderRot) * low);
        }

        static void Blend(Transform t, Quaternion target, float k)
        {
            if (t) t.localRotation = Quaternion.Slerp(t.localRotation, target, k);
        }
    }
}
