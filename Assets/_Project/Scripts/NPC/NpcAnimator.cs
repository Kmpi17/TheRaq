using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Animación procedural de un NPC, con el mismo cuerpo de piezas que el jugador (<c>BuildPlayerBody</c>) y la misma
    /// matemática (<see cref="AnimationMath"/>): ciclo de paso según la velocidad del NavMeshAgent, peso en una pierna y
    /// mirar alrededor estando quieto, y poses de lo que hace en su sitio (<see cref="NpcActivity"/>): sentado, de
    /// corro con los brazos cruzados o gesticulando, vendiendo con las manos en el bolsillo de la sudadera, calentándose
    /// las manos en el bidón; agachado con las manos en la cabeza (asustado), apuntando con la pistola con las dos
    /// manos, puñetazo y muerto en el suelo. Más simple que <see cref="PlayerAnimator"/>: sin escalar ni cobertura.
    /// También coloca la pistola (al jugador se la coloca el combate).
    /// </summary>
    public sealed class NpcAnimator : MonoBehaviour
    {
        [SerializeField] float sharpness = 10f;

        Transform model, pelvis, spine, head, pistol;
        readonly Transform[] hip = new Transform[2], knee = new Transform[2], ankle = new Transform[2];
        readonly Transform[] shoulder = new Transform[2], elbow = new Transform[2], hand = new Transform[2];
        Vector3 pelvisRest, modelRest;
        NpcBrain brain;

        float phase, moveBlend, jogBlend, weight, weightTarget = 1f, nextWeightSwap, lookYaw, lookTarget, nextLook, fall, gestureSeed;

        void Awake()
        {
            model = transform.Find("Modelo");
            Transform Find(string name)
            {
                if (!model) return null;
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
                return null;
            }
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
            brain = GetComponent<NpcBrain>();
            if (!pelvis || !spine || !hip[0] || !hip[1] || !shoulder[0] || !shoulder[1] || !brain)
            {
                enabled = false;
                return;
            }
            pelvisRest = pelvis.localPosition;
            modelRest = model.localPosition;
            gestureSeed = Random.Range(0f, 100f);
            nextWeightSwap = Random.Range(1f, 6f);
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void OnEnable()
        {
            fall = 0f;
            if (model) model.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float k = 1f - Mathf.Exp(-sharpness * dt);
            float t = Time.time;
            bool dead = brain.Dead;
            bool aiming = brain.Aiming && brain.PistolOut;
            bool cower = brain.Cowering;
            var velocity = brain.Velocity;
            velocity.y = 0f;
            float speed = dead ? 0f : velocity.magnitude;
            bool atSpot = brain.AtSpot && speed < 0.3f;
            var activity = brain.Activity;
            bool sitting = atSpot && activity == NpcActivity.Sit;

            moveBlend = Mathf.MoveTowards(moveBlend, Mathf.Clamp01(speed / 1.1f), 5f * dt);
            jogBlend = Mathf.MoveTowards(jogBlend, aiming ? 0f : Mathf.Clamp01((speed - 2.4f) / 1.6f), 3f * dt);
            phase = AnimationMath.AdvancePhase(phase, speed, jogBlend, 0f, dt);
            var g = AnimationMath.Gait(phase, moveBlend, jogBlend, 0f);
            float idle = 1f - moveBlend;
            var local = speed > 0.2f ? transform.InverseTransformDirection(velocity).normalized : Vector3.forward;
            float ahead = Mathf.Clamp(local.z, -1f, 1f);

            // Muerto: cae de espaldas en medio segundo y se queda tumbado.
            fall = dead ? Mathf.MoveTowards(fall, 1f, dt / 0.55f) : 0f;
            float f = Mathf.SmoothStep(0f, 1f, fall);
            model.localRotation = Quaternion.Euler(-88f * f, 0f, 0f);
            model.localPosition = modelRest + new Vector3(0f, 0.18f * f, -0.15f * f);

            // Quieto: el peso en una pierna y mirar alrededor (o a quien tiene delante).
            bool relaxed = !dead && !aiming && !cower && !sitting;
            if (t > nextWeightSwap)
            {
                weightTarget = -weightTarget;
                nextWeightSwap = t + Random.Range(4f, 10f);
            }
            weight = Mathf.Lerp(weight, relaxed ? weightTarget * idle : 0f, 1f - Mathf.Exp(-2f * dt));
            AnimationMath.WeightShift(weight, out float wSway, out float wRoll, out float looseKnee);
            if (t > nextLook)
            {
                bool away = Mathf.Approximately(lookTarget, 0f);
                lookTarget = away ? Random.Range(15f, 40f) * (Random.value < 0.5f ? -1f : 1f) : 0f;
                nextLook = t + (away ? Random.Range(1f, 3f) : Random.Range(2.5f, 7f));
            }
            float headYawTarget = relaxed ? lookTarget : 0f;
            if (brain.Looking && !dead)
            {
                // Mira a quien le interesa (el jugador al que vigila, el blanco) girando el cuello hasta 60°.
                var to = transform.InverseTransformPoint(brain.LookAt);
                headYawTarget = Mathf.Clamp(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -60f, 60f);
            }
            lookYaw = Mathf.Lerp(lookYaw, headYawTarget, 1f - Mathf.Exp(-4f * dt));
            float breathe = Mathf.Sin(t * 1.6f + gestureSeed) * 1.2f * idle;

            // ---------- Piernas, pelvis, tronco ----------
            float[] hipA = { g.HipL * ahead, g.HipR * ahead };
            float[] kneeA = { g.KneeL, g.KneeR };
            float[] ankleA = new float[2];
            float[] spread = { 3f * idle, 3f * idle };
            float drop = g.Bob, swayX = g.SwayX, roll = g.PelvisRoll * 1.3f;
            float lean = g.Lean * Mathf.Max(0f, ahead) + breathe + 2f;
            float headPitch = -g.Lean * 0.7f * Mathf.Max(0f, ahead) - 2f;
            if (sitting)
            {
                swayX = roll = 0f;
                drop = -0.46f;
                hipA = new[] { -85f, -80f };
                kneeA = new[] { 88f, 84f };
                ankleA = new[] { -4f, -2f };
                spread = new[] { 9f, 11f };
                lean = 10f + breathe;
                headPitch = -4f;
            }
            else if (cower)
            {
                // Agachado de cuclillas con las manos en la cabeza.
                swayX = roll = 0f;
                drop = -0.5f;
                hipA = new[] { -100f, -92f };
                kneeA = new[] { 135f, 130f };
                ankleA = new[] { -35f, -38f };
                spread = new[] { 12f, 12f };
                lean = 38f;
                headPitch = 20f;
            }
            else if (!dead)
            {
                if (Mathf.Abs(weight) > 0.01f)
                {
                    int loose = weight < 0f ? 1 : 0;
                    hipA[loose] -= looseKnee * 0.45f;
                    kneeA[loose] += looseKnee;
                    spread[loose] += 3f * Mathf.Abs(weight);
                    swayX += wSway;
                    roll += wRoll;
                }
                if (aiming)
                {
                    hipA[0] -= 9f * idle;
                    hipA[1] += 6f * idle;
                    kneeA[0] += 8f * idle;
                    kneeA[1] += 8f * idle;
                    lean += 3f + brain.AimPitch * 0.35f;
                    headPitch = brain.AimPitch * 0.4f;
                }
                else if (atSpot && activity == NpcActivity.Warm) lean += 10f; // inclinado sobre el fuego
                else if (atSpot && activity == NpcActivity.Mourn) headPitch = 22f; // cabeza gacha
                for (int i = 0; i < 2; i++)
                {
                    float foot = (i == 0 ? g.FootL : g.FootR) * ahead;
                    ankleA[i] = foot - (hipA[i] + kneeA[i]);
                }
            }
            else
            {
                swayX = roll = 0f;
                hipA = new[] { -4f, 8f };
                kneeA = new[] { 14f, 5f };
                lean = 0f;
                headPitch = 12f;
            }

            pelvis.localPosition = Vector3.Lerp(pelvis.localPosition, pelvisRest + new Vector3(swayX, drop, 0f), k);
            Blend(pelvis, Quaternion.Euler(0f, g.PelvisYaw * Mathf.Abs(ahead), roll), k);
            Blend(spine, Quaternion.Euler(lean, g.SpineYaw * Mathf.Abs(ahead), -roll * 1.5f), k);
            if (head) Blend(head, Quaternion.Euler(headPitch, lookYaw - g.SpineYaw * Mathf.Abs(ahead), roll * 0.4f), k);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Blend(hip[i], Quaternion.Euler(hipA[i], 0f, side * spread[i] - roll), k);
                if (knee[i]) Blend(knee[i], Quaternion.Euler(kneeA[i], 0f, 0f), k);
                if (ankle[i]) Blend(ankle[i], Quaternion.Euler(ankleA[i], 0f, 0f), k);
            }

            // ---------- Pistola: levantada a la altura del hombro al apuntar; si no, en la mano, hacia el suelo ----------
            bool pistolOut = pistol && pistol.gameObject.activeInHierarchy && !dead;
            if (pistolOut)
            {
                float shoulderY = shoulder[1] ? model.InverseTransformPoint(shoulder[1].position).y + 0.03f : 1.45f;
                var pivot = new Vector3(0.1f, shoulderY, 0f);
                var pose = aiming ? pivot + Quaternion.Euler(brain.AimPitch, 0f, 0f) * new Vector3(0.06f, 0.04f, 0.66f) : new Vector3(0.3f, shoulderY - 0.47f, 0.16f);
                if (brain.Reloading) pose += Vector3.down * 0.25f;
                float follow = 1f - Mathf.Exp(-16f * dt);
                pistol.localPosition = Vector3.Lerp(pistol.localPosition, pose, follow);
                pistol.localRotation = Quaternion.Slerp(pistol.localRotation, Quaternion.Euler(aiming ? brain.AimPitch : 55f, 0f, 0f), follow);
            }

            // ---------- Brazos ----------
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float swing = (i == 0 ? g.ShoulderL : g.ShoulderR) * Mathf.Max(0.25f, Mathf.Abs(ahead));
                var sh = Quaternion.Euler(swing + Mathf.Sin(t * 1.6f + i) * 2f * idle, 0f, side * 7f);
                var el = Quaternion.Euler((i == 0 ? g.ElbowL : g.ElbowR) - 14f * idle, 0f, 0f);
                if (dead)
                {
                    sh = Quaternion.Euler(-10f, 0f, side * 65f);
                    el = Quaternion.Euler(-10f, 0f, 0f);
                }
                else if (HandTarget(i, aiming, cower, atSpot, sitting, activity, pistolOut, out var target)) SolveArm(i, target, out sh, out el);
                Blend(shoulder[i], sh, Mathf.Min(1f, k * 1.6f));
                if (elbow[i]) Blend(elbow[i], el, Mathf.Min(1f, k * 1.6f));
            }

            // Pistola bajada: en la mano derecha.
            if (pistolOut && !aiming && hand[1])
            {
                pistol.rotation = hand[1].rotation * Quaternion.Euler(28f, 0f, 0f);
                pistol.position = hand[1].TransformPoint(new Vector3(0f, -0.055f, 0.005f)) - pistol.TransformVector(new Vector3(0f, -0.07f, -0.04f));
            }
        }

        bool HandTarget(int i, bool aiming, bool cower, bool atSpot, bool sitting, NpcActivity activity, bool pistolOut, out Vector3 target)
        {
            target = default;
            float side = i == 0 ? -1f : 1f;
            float t = Time.time;
            if (aiming && pistolOut)
            {
                var grip = pistol.TransformPoint(new Vector3(0f, -0.07f, -0.04f));
                target = i == 1 ? grip : grip + model.TransformVector(new Vector3(-0.06f, -0.02f, -0.02f));
                return true;
            }
            if (brain.PunchExtend > 0.01f)
            {
                float e = brain.PunchExtend;
                target = i == 1
                    ? model.TransformPoint(new Vector3(Mathf.Lerp(0.2f, 0.06f, e), 1.35f, 0.2f + 0.48f * e))
                    : model.TransformPoint(new Vector3(-0.14f, 1.42f, 0.24f));
                return true;
            }
            if (cower)
            {
                // Las manos sobre la cabeza.
                target = head ? head.TransformPoint(new Vector3(side * 0.09f, 0.12f, -0.02f)) : model.TransformPoint(new Vector3(side * 0.1f, 1.2f, 0.2f));
                return true;
            }
            if (sitting)
            {
                // Los antebrazos sobre los muslos.
                target = model.TransformPoint(new Vector3(side * 0.16f, 0.6f, 0.38f));
                return true;
            }
            if (!atSpot || pistolOut && i == 1) return false;
            switch (activity)
            {
                case NpcActivity.Warm:
                    // Las palmas hacia el fuego, delante.
                    target = model.TransformPoint(new Vector3(side * 0.16f, 1.05f + Mathf.Sin(t * 2f + i) * 0.02f, 0.5f));
                    return true;
                case NpcActivity.Sell:
                case NpcActivity.Wait:
                    // Manos en el bolsillo de la sudadera (o cruzadas delante, esperando).
                    target = model.TransformPoint(new Vector3(side * 0.07f, 1.02f, 0.2f));
                    return true;
                case NpcActivity.Mourn:
                    target = model.TransformPoint(new Vector3(side * 0.05f, 1.0f, 0.22f));
                    return true;
                case NpcActivity.Hangout:
                    // En corro: unos con los brazos cruzados; otros gesticulan con una mano al hablar.
                    bool crossed = Mathf.Repeat(gestureSeed, 2f) < 1f;
                    if (crossed)
                    {
                        target = model.TransformPoint(new Vector3(-side * 0.12f, 1.22f, 0.2f));
                        return true;
                    }
                    if (i == 1)
                    {
                        float talk = Mathf.PerlinNoise(t * 0.7f, gestureSeed);
                        if (talk < 0.5f) return false;
                        target = model.TransformPoint(new Vector3(0.22f + Mathf.Sin(t * 3.1f) * 0.06f, 1.2f + Mathf.Sin(t * 4.3f) * 0.08f, 0.38f));
                        return true;
                    }
                    return false;
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
