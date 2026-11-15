using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Animación procedural del personaje (no hay Animator ni clips: el modelo es de piezas). Mueve las articulaciones
    /// que crea el generador dentro de <c>Modelo</c>: <c>Cuerpo</c> (pelvis), <c>Cadera/Rodilla/Tobillo</c> Izq/Der,
    /// <c>Columna</c>, <c>Craneo</c> y <c>Hombro/Codo</c> Izq/Der. Capas:
    /// <list type="bullet">
    /// <item>ciclo de paso de <see cref="AnimationMath.Gait"/> (curvas de la marcha real: andar, trotar, esprintar), con
    /// la pelvis que bota, se va sobre la pierna de apoyo y cae del otro lado, y los hombros contra las caderas;</item>
    /// <item>peso: todo el cuerpo se inclina hacia dentro en las curvas, hacia delante al arrancar y hacia atrás al
    /// frenar, y se hunde al caer de alto;</item>
    /// <item>personalidad: quieto carga el peso en una pierna y lo cambia de vez en cuando, respira, mira alrededor; la
    /// cabeza se mantiene estable y mira al frente aunque el cuerpo se incline;</item>
    /// <item>agachado (C), en cobertura, escalando, en el aire y muerto;</item>
    /// <item>IK de brazo (<see cref="AnimationMath.SolveTwoBone"/>): las dos manos en la pistola al apuntar o recargar,
    /// puñetazos con guardia, las dos manos en la puerta al forzar un coche y en el borde al escalar. Con la pistola
    /// bajada, el brazo se balancea y la pistola va en la mano (la coloca este componente).</item>
    /// </list>
    /// Corre en LateUpdate, después de que el combate coloque la pistola.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] float runSpeed = 4.4f, sprintSpeed = 7.2f;
        [Tooltip("Rapidez con la que las articulaciones van a su pose (más alto, más seco).")]
        [SerializeField] float sharpness = 14f;
        [Tooltip("Cuánto se inclina hacia dentro en las curvas (grados por (°/s · m/s)).")]
        [SerializeField] float bankPerTurn = 0.008f;
        [Tooltip("Actitud (0–1): balanceo de hombros y caderas, cabeceo y brazos algo abiertos.")]
        [Range(0f, 1f)] [SerializeField] float swagger = 0.6f;

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

        float phase, airTime, moveBlend, jogBlend, sprintBlend;
        Vector3 moveDir = Vector3.forward; // dirección del movimiento en el espacio del cuerpo, suavizada
        Vector3 modelRest;
        float modelYaw, coverFacing = 1f;
        // Peso y personalidad.
        float lastYaw, lastForwardSpeed, accelSmooth, bank, tilt, landDip;
        float weight, weightTarget = 1f, nextWeightSwap = 3f;
        float lookYaw, lookTarget, nextLook = 4f, idleTime;
        bool wasInAir;

        /// <summary>Fase del ciclo de paso (radianes), para tests y depurar.</summary>
        public float Phase => phase;
        /// <summary>
        /// Altura (en el espacio de <c>Modelo</c>) del pivote de la pistola al apuntar: el hombro derecho. Baja al
        /// agacharse, así que <see cref="PlayerCombat"/> no pone la pistola a la altura de pie (antes, agachado, los
        /// brazos se iban por encima de la cabeza). 0 si no se ha calculado aún.
        /// </summary>
        public float AimPivotY { get; private set; }

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
            lastYaw = transform.eulerAngles.y;
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
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float k = 1f - Mathf.Exp(-sharpness * dt);
            float t = Time.time;

            bool dead = vitals && vitals.Dead;
            bool climbing = climb && climb.Climbing;
            bool inCover = cover && cover.InCover;
            bool grounded = !controller || !controller.enabled || controller.isGrounded;
            float airBefore = airTime;
            airTime = grounded || climbing ? 0f : airTime + dt;
            bool inAir = airTime > 0.18f;
            // Agachado por su cuenta (C), fuera de la cobertura; o tras una cobertura baja.
            bool crouchStance = movement && movement.Crouching && !inCover && !climbing && !dead && !inAir;
            bool crouched = (cover && cover.Crouched) || crouchStance;
            bool aiming = combat && combat.WeaponRaised && !dead && !climbing;
            float aimPitch = aiming ? combat.AimPitch : 0f;

            // Al tocar suelo después de una caída: se hunde (más cuanto más tiempo en el aire) y se recupera.
            if (wasInAir && !inAir && !climbing) landDip = Mathf.Max(landDip, Mathf.Clamp01((airBefore + 0.1f) / 0.9f));
            if (inAir) landDip = 0f;
            wasInAir = inAir;
            landDip = Mathf.MoveTowards(landDip, 0f, 2.8f * dt);

            // Velocidad: la del movimiento a pie, o la real del controlador en cobertura (se mueve de lado).
            var velocity = movement ? movement.PlanarVelocity : Vector3.zero;
            if (inCover && controller && controller.enabled) velocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            float speed = velocity.magnitude;
            bool still = dead || climbing || inAir;
            bool slowGait = inCover || crouchStance || aiming; // pasos de andar, nunca de correr
            moveBlend = Mathf.MoveTowards(moveBlend, still ? 0f : Mathf.Clamp01(speed / 1.2f), 6f * dt);
            jogBlend = Mathf.MoveTowards(jogBlend, slowGait ? 0f : Mathf.Clamp01((speed - 2.6f) / Mathf.Max(0.1f, runSpeed - 3f)), 3f * dt);
            sprintBlend = Mathf.MoveTowards(sprintBlend, slowGait ? 0f : Mathf.Clamp01((speed - runSpeed - 0.4f) / Mathf.Max(0.1f, sprintSpeed - runSpeed - 0.6f)), 2.5f * dt);
            // Agachado los pasos son cortos: la fase va más deprisa para que los pies no patinen.
            float phaseSpeed = still ? 0f : crouchStance ? speed * AnimationMath.Stride(0f, 0f) / AnimationMath.CrouchStride : speed;
            phase = AnimationMath.AdvancePhase(phase, phaseSpeed, jogBlend, sprintBlend, dt);
            var g = AnimationMath.Gait(phase, moveBlend, jogBlend, sprintBlend);
            float idle = 1f - moveBlend;

            // ---------- Peso: inclinarse en las curvas y al arrancar o frenar (todo el cuerpo, desde los pies) ----------
            float yaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(lastYaw, yaw) / dt;
            lastYaw = yaw;
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            accelSmooth = Mathf.Lerp(accelSmooth, (forwardSpeed - lastForwardSpeed) / dt, 1f - Mathf.Exp(-8f * dt));
            lastForwardSpeed = forwardSpeed;
            bool weighted = grounded && !climbing && !inCover && !dead && !inAir;
            float bankTarget = weighted ? Mathf.Clamp(-yawRate * speed * bankPerTurn, -13f, 13f) : 0f;
            float tiltTarget = weighted && !aiming ? Mathf.Clamp(accelSmooth * 0.45f, -8f, 6f) : 0f;
            bank = Mathf.Lerp(bank, bankTarget, 1f - Mathf.Exp(-6f * dt));
            tilt = Mathf.Lerp(tilt, tiltTarget, 1f - Mathf.Exp(-5f * dt));

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
                model.localRotation = Quaternion.Euler(tilt, 0f, bank) * Quaternion.Euler(0f, modelYaw, 0f);
                // Se separa un poco de la pared para que el hombro no se meta en ella.
                model.localPosition = modelRest + Vector3.back * (0.14f * Mathf.Abs(modelYaw) / 75f);
            }

            // Hacia dónde se mueve respecto al cuerpo (suavizado): apuntando o en cobertura se anda de lado y hacia
            // atrás, y las piernas tienen que dar el paso hacia allí, no hacia delante.
            if (speed > 0.3f)
            {
                var local = Quaternion.Euler(0f, -modelYaw, 0f) * transform.InverseTransformDirection(velocity);
                local.y = 0f;
                moveDir = Vector3.Slerp(moveDir, local.normalized, 1f - Mathf.Exp(-10f * dt));
            }
            else moveDir = Vector3.Slerp(moveDir, Vector3.forward, 1f - Mathf.Exp(-4f * dt));
            float ahead = moveDir.z;                  // 1 hacia delante, −1 hacia atrás, 0 de lado
            float forwardOnly = Mathf.Max(0f, ahead); // inclinarse y torcer el tronco solo al ir hacia delante
            var swingAxis = AnimationMath.SwingAxis(moveDir);

            // ---------- Personalidad quieto: peso en una pierna, respirar, mirar alrededor ----------
            bool relaxed = !dead && !climbing && !inAir && !inCover && !crouchStance && !aiming && !(combat && combat.Punching) && !(entry && entry.Forcing);
            idleTime = relaxed && moveBlend < 0.1f ? idleTime + dt : 0f;
            if (t > nextWeightSwap)
            {
                weightTarget = -weightTarget;
                nextWeightSwap = t + Random.Range(4.5f, 9f);
            }
            weight = Mathf.Lerp(weight, relaxed ? weightTarget * idle : 0f, 1f - Mathf.Exp(-2.2f * dt));
            AnimationMath.WeightShift(weight, out float weightSway, out float weightRoll, out float looseKnee);
            if (t > nextLook)
            {
                // Mira a un lado un rato y vuelve al frente.
                bool lookAway = Mathf.Approximately(lookTarget, 0f) && idleTime > 1.5f;
                lookTarget = lookAway ? Random.Range(18f, 35f) * (Random.value < 0.5f ? -1f : 1f) : 0f;
                nextLook = t + (lookAway ? Random.Range(1.2f, 2.6f) : Random.Range(3f, 6.5f));
            }
            if (!relaxed) lookTarget = 0f;
            lookYaw = Mathf.Lerp(lookYaw, lookTarget, 1f - Mathf.Exp(-4f * dt));
            float breathe = Mathf.Sin(t * 1.7f) * 1.3f * idle;

            // ---------- Piernas, pelvis, columna y cabeza ----------
            var hipA = new[] { g.HipL, g.HipR };
            var kneeA = new[] { g.KneeL, g.KneeR };
            var ankleA = new float[2];
            var spread = new float[2];   // abrir la pierna hacia fuera (grados)
            var hipSwing = new float[2]; // paso encima de la pose (en el eje del movimiento), agachado
            float drop = g.Bob, swayX = g.SwayX, roll = g.PelvisRoll * (1f + 0.6f * swagger);
            float lean = g.Lean * forwardOnly + breathe + 2f; // de base, algo cargado de hombros
            float pelvisYaw = g.PelvisYaw * Mathf.Abs(ahead), spineYaw = g.SpineYaw * Mathf.Abs(ahead) * (1f + 0.4f * swagger);
            // La cabeza compensa la inclinación del tronco: mira al frente (con un leve cabeceo al paso).
            float headPitch = -g.Lean * 0.75f * forwardOnly - 2f + Mathf.Sin(2f * phase) * 1.5f * swagger * moveBlend;
            bool locomotion = false;
            if (crouched)
            {
                swayX = 0f;
                roll = 0f;
                drop = -0.36f;
                for (int i = 0; i < 2; i++)
                {
                    hipA[i] = -78f + (i == 0 ? -6f : 6f);
                    kneeA[i] = 118f;
                    ankleA[i] = -38f;
                    spread[i] = 8f;
                }
                lean = 22f + breathe;
                headPitch = -16f;
                if (crouchStance)
                {
                    // Agachado en medio de la calle: algo menos hundido que tras un muro, un pie algo adelantado, y
                    // pasos cortos hacia donde vaya (delante, de lado o atrás) con el ciclo de andar encima de la pose.
                    drop = -0.32f + g.Bob;
                    for (int i = 0; i < 2; i++)
                    {
                        float side = i == 0 ? -1f : 1f;
                        hipSwing[i] = AnimationMath.SideStepHip((i == 0 ? g.HipL : g.HipR) * 0.8f, side, moveDir);
                        hipA[i] = -70f + (i == 0 ? -8f : 6f) * idle;
                        kneeA[i] = 108f + (i == 0 ? g.KneeL : g.KneeR) * 0.45f;
                        ankleA[i] = -(hipA[i] + hipSwing[i] * ahead + kneeA[i]) * 0.9f;
                        spread[i] = 7f;
                    }
                    lean = 16f + breathe;
                    headPitch = -10f;
                    if (aiming)
                    {
                        lean = 8f + aimPitch * 0.35f;
                        headPitch = aimPitch * 0.4f - 4f;
                    }
                }
                // Tras la cobertura y moviéndose de lado: pasitos cortos hacia donde va.
                else if (moveBlend > 0.05f)
                    for (int i = 0; i < 2; i++)
                        spread[i] += Mathf.Sin(phase + i * Mathf.PI) * 10f * moveBlend * Mathf.Abs(moveDir.x);
            }
            else if (climbing)
            {
                swayX = roll = 0f;
                var kind = climb.CurrentKind;
                float below = climb.EdgeY - transform.position.y; // los pies, por debajo del borde
                if (kind is ClimbKind.Vault or ClimbKind.SlideOver)
                {
                    // Por encima con una mano apoyada: piernas recogidas a un lado y el cuerpo echado hacia delante.
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
                // Cayendo: una pierna adelantada buscando el suelo y la otra recogida.
                swayX = roll = 0f;
                hipA = new[] { -32f, -6f };
                kneeA = new[] { 40f, 55f };
                ankleA = new[] { 0f, -15f };
                spread = new[] { 4f, 6f };
                lean = 6f;
            }
            else if (dead)
            {
                swayX = roll = 0f;
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
                    spread[i] = 3f * idle;
                }
                // Quieto: el peso en una pierna; la otra, con la rodilla suelta y el pie algo adelantado y abierto.
                if (Mathf.Abs(weight) > 0.01f)
                {
                    int loose = weight < 0f ? 1 : 0; // con el peso a la izquierda, la derecha va suelta
                    hipA[loose] -= looseKnee * 0.45f;
                    kneeA[loose] += looseKnee;
                    spread[loose] += 3f * Mathf.Abs(weight);
                    swayX += weightSway;
                    roll += weightRoll;
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
                    headPitch = aimPitch * 0.4f;
                }
                // El pie con la inclinación del ciclo respecto al suelo (al ir hacia atrás, al revés; de lado, plano).
                for (int i = 0; i < 2; i++)
                {
                    float foot = (i == 0 ? g.FootL : g.FootR) * ahead;
                    ankleA[i] = foot - (hipA[i] * ahead + kneeA[i]);
                }
            }

            // Al caer de alto: rodillas y caderas se doblan y el tronco se echa adelante para amortiguar.
            if (landDip > 0.01f && !climbing && !dead)
            {
                float d = Mathf.SmoothStep(0f, 1f, landDip);
                drop -= 0.16f * d;
                for (int i = 0; i < 2; i++)
                {
                    hipA[i] -= 30f * d;
                    kneeA[i] += 50f * d;
                    ankleA[i] -= 20f * d;
                }
                lean += 14f * d;
                headPitch -= 8f * d;
            }

            pelvis.localPosition = Vector3.Lerp(pelvis.localPosition, pelvisRest + new Vector3(swayX, drop, 0f), k);
            Blend(pelvis, Quaternion.Euler(0f, pelvisYaw, roll), k);
            // Los hombros, contra la caída de la cadera (y de lado al cargar el peso, al revés que la pelvis).
            float spineRoll = -roll * 1.6f;
            Blend(spine, Quaternion.Euler(lean, spineYaw, spineRoll), k);
            // La cabeza: casi nivelada y mirando al frente, contra el giro y la caída del tronco; quieto, mira alrededor.
            float headYaw = -(pelvisYaw + spineYaw) + lookYaw;
            if (head) Blend(head, Quaternion.Euler(headPitch, headYaw, roll * 0.4f), k);
            // Las piernas no se mueven de sitio con la pelvis: compensan su desplazamiento y su caída.
            float legComp = -Mathf.Asin(Mathf.Clamp(swayX / 0.86f, -0.5f, 0.5f)) * Mathf.Rad2Deg - roll;
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var axis = locomotion ? swingAxis : Vector3.right;
                Blend(hip[i], Quaternion.AngleAxis(hipSwing[i], swingAxis) * Quaternion.AngleAxis(hipA[i], axis)
                              * Quaternion.Euler(0f, 0f, side * spread[i] + legComp), k);
                if (knee[i]) Blend(knee[i], Quaternion.Euler(kneeA[i], 0f, 0f), k);
                if (ankle[i]) Blend(ankle[i], Quaternion.Euler(ankleA[i], 0f, 0f), k);
            }

            // ---------- Brazos: primero la pose de movimiento (FK) y encima el IK de lo que haga con las manos ----------
            bool carryPistol = CarryingLoweredPistol();
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float sway = Mathf.Sin(t * 1.7f + i * 1.3f) * 2f * idle;
                Quaternion sh, el;
                if (dead)
                {
                    sh = Quaternion.Euler(-10f, 0f, side * 65f);
                    el = Quaternion.Euler(-10f, 0f, 0f);
                }
                else if (climbing)
                {
                    // La mano que no se apoya: al saltar, adelantada y algo arriba para equilibrarse (no en cruz);
                    // subiendo, bajando tras pasar el borde.
                    bool vault = climb.CurrentKind is ClimbKind.Vault or ClimbKind.SlideOver;
                    sh = vault ? Quaternion.Euler(-65f, 0f, side * 22f) : Quaternion.Euler(-25f, 0f, side * 18f);
                    el = Quaternion.Euler(vault ? -35f : -40f, 0f, 0f);
                }
                else if (inAir)
                {
                    // Brazos algo abiertos y adelantados para equilibrarse.
                    sh = Quaternion.Euler(-30f, 0f, side * 28f);
                    el = Quaternion.Euler(-35f, 0f, 0f);
                }
                else if (crouched)
                {
                    // Tras la cobertura, brazos recogidos delante, listos. Agachado por su cuenta, más bajos, con las
                    // manos sobre las rodillas, y se balancean un poco al andar.
                    float swing = crouchStance ? (i == 0 ? g.ShoulderL : g.ShoulderR) * 0.4f * Mathf.Abs(ahead) : 0f;
                    sh = Quaternion.Euler((crouchStance ? -16f : -35f) + swing, 0f, side * (crouchStance ? 14f : 10f));
                    el = Quaternion.Euler(crouchStance ? -48f : -75f, 0f, 0f);
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
                    // Los brazos se balancean al ir hacia delante o atrás; de lado, casi quietos. Quietos, relajados:
                    // algo separados del cuerpo y con el codo un poco doblado. El brazo de la pistola, más corto.
                    float swing = (i == 0 ? g.ShoulderL : g.ShoulderR) * Mathf.Max(0.25f, Mathf.Abs(ahead));
                    float elbowBend = (i == 0 ? g.ElbowL : g.ElbowR) - 14f * idle;
                    if (i == 1 && carryPistol)
                    {
                        swing *= 0.6f;
                        elbowBend -= 18f;
                    }
                    float open = side * (6f + 4f * swagger + 3f * jogBlend);
                    sh = Quaternion.Euler(swing + sway - 2f * idle, 0f, open);
                    el = Quaternion.Euler(elbowBend, 0f, 0f);
                }

                if (!dead && HandTarget(i, out var target)) SolveArm(i, target, out sh, out el);
                Blend(shoulder[i], sh, Mathf.Min(1f, k * 1.6f));
                if (elbow[i]) Blend(elbow[i], el, Mathf.Min(1f, k * 1.6f));
            }

            // Con la pistola bajada, va en la mano derecha, con el cañón hacia delante y abajo.
            if (carryPistol && hand[1])
            {
                pistol.rotation = hand[1].rotation * Quaternion.Euler(28f, 0f, 0f);
                pistol.position = hand[1].TransformPoint(new Vector3(0f, -0.055f, 0.005f)) - pistol.TransformVector(new Vector3(0f, -0.07f, -0.04f));
            }

            // Pivote de la pistola al apuntar (para el combate): a la altura del hombro derecho, sea cual sea la pose.
            if (shoulder[1])
            {
                float y = model.InverseTransformPoint(shoulder[1].position).y + 0.03f;
                AimPivotY = AimPivotY <= 0f ? y : Mathf.Lerp(AimPivotY, y, 1f - Mathf.Exp(-12f * dt));
            }
        }

        /// <summary>Lleva la pistola bajada (no apunta, no recarga, no pega): va en la mano y el brazo se balancea.</summary>
        bool CarryingLoweredPistol()
            => pistol && pistol.gameObject.activeInHierarchy && combat && !combat.WeaponRaised && !combat.Reloading && !combat.Punching
               && !(entry && entry.Forcing) && !(vitals && vitals.Dead);

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

            // Pistola levantada (apuntando o recargando): la derecha la empuña y la izquierda la sujeta. Bajada, la
            // lleva la mano derecha (ver CarryingLoweredPistol) y no hay IK.
            if (pistol && pistol.gameObject.activeInHierarchy && (combat.WeaponRaised || combat.Reloading))
            {
                var grip = pistol.TransformPoint(new Vector3(0f, -0.07f, -0.04f));
                target = i == 1 ? grip : grip + model.TransformVector(new Vector3(-0.06f, -0.02f, -0.02f));
                return true;
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
