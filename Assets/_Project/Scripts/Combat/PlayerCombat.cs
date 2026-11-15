using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Pegar y disparar.
    /// - Puños (sin arma, clic izquierdo): combo de 3 golpes (el tercero, un gancho más fuerte). Se orienta solo hacia
    ///   el objetivo más cercano delante y da un paso. Cuesta energía; agotado, pega la mitad.
    /// - Pistola: clic derecho apunta (cámara al hombro, mira y paso lento) y el izquierdo dispara (semiautomática).
    ///   Desde la cadera también se puede, con más dispersión. Cada tiro abre la dispersión (bloom), que se cierra
    ///   sola, y da retroceso a la cámara. R recarga del inventario. El tiro sale de la cámara (lo que hay bajo la
    ///   mira) y se comprueba desde el arma, así que una pared delante del cañón lo para. Tiro en la cabeza ×2.5.
    /// Los golpes buscan <see cref="Health"/> en lo que tocan y empujan los Rigidbody.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] ThirdPersonCamera followCamera;
        [Tooltip("Punta del cañón (hija del modelo de la pistola).")]
        [SerializeField] Transform muzzle;
        [SerializeField] GameObject pistolModel;
        [SerializeField] Transform fist;
        [SerializeField] LayerMask mask = ~(1 << 2);
        [SerializeField] float[] punchDamage = { 12f, 12f, 24f };
        [SerializeField] float punchStamina = 7f;
        [SerializeField] float headshotMultiplier = 2.5f;

        PlayerMovement movement;
        PlayerInventory inventory;
        PlayerVitals vitals;
        PlayerCover cover;
        PlayerClimb climb;
        CharacterController controller;
        Health ownHealth;
        AudioSource audioSource;
        AudioClip shotClip, punchClip, clickClip;
        Light flash;
        float flashUntil, nextShot, bloom, hipFireUntil;
        string reloadingId;
        float reloadEnd;
        int combo;
        bool punching, punchLanded, queuedPunch, tiredPunch;
        float punchStart, punchEnd, punchHitAt, comboUntil;
        Vector3 fistRest;
        readonly List<GameObject> holes = new();
        static Material holeMaterial;

        public bool IsAiming { get; private set; }
        public bool Reloading => reloadingId != null;
        public float ReloadProgress => Reloading ? 1f - Mathf.Clamp01((reloadEnd - Time.time) / ItemCatalog.Get(reloadingId).ReloadSeconds) : 0f;
        public float CurrentSpread { get; private set; }
        public float LastHitTime { get; private set; } = -10f;
        public bool LastHitKilled { get; private set; }
        /// <summary>Se ve la mira: apuntando o justo después de disparar desde la cadera.</summary>
        public bool ShowCrosshair => IsAiming || Time.time < hipFireUntil;
        /// <summary>Con el arma levantada (apuntando o justo después de disparar desde la cadera). Para la animación.</summary>
        public bool WeaponRaised => IsAiming || Time.time < hipFireUntil;
        /// <summary>Puñetazo en curso: cuánto ha salido el puño (0–1, sale y vuelve), con qué mano (+1 derecha) y si es el gancho.</summary>
        public float PunchExtend { get; private set; }
        public int PunchSide { get; private set; } = 1;
        public bool PunchIsHook => combo == 2;
        public bool Punching => punching;
        /// <summary>Inclinación de la mira (grados, positivo hacia abajo), para subir o bajar los brazos y la pistola.</summary>
        public float AimPitch { get; private set; }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            inventory = GetComponent<PlayerInventory>();
            vitals = GetComponent<PlayerVitals>();
            cover = GetComponent<PlayerCover>();
            climb = GetComponent<PlayerClimb>();
            controller = GetComponent<CharacterController>();
            ownHealth = GetComponent<Health>();
            if (!followCamera) followCamera = FindAnyObjectByType<ThirdPersonCamera>();
            if (fist) fistRest = fist.localPosition;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            shotClip = ProceduralSfx.Gunshot();
            punchClip = ProceduralSfx.Punch();
            clickClip = ProceduralSfx.Click();

            flash = new GameObject("FogonazoLuz").AddComponent<Light>();
            flash.transform.SetParent(muzzle ? muzzle : transform, false);
            if (!muzzle) flash.transform.localPosition = new Vector3(0.3f, 1.4f, 0.6f);
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.75f, 0.4f);
            flash.range = 6f;
            flash.intensity = 4f;
            flash.enabled = false;
        }

        bool CanAct => controller.enabled && !GameUi.MenuOpen && !(vitals && vitals.Dead) && !(climb && climb.Climbing);

        void Update()
        {
            var weapon = inventory ? inventory.EquippedWeapon : null;
            bool can = CanAct;
            var actions = GameInput.Actions;

            IsAiming = can && weapon != null && actions.Aim.IsPressed();
            if (movement)
            {
                movement.Aiming = IsAiming || (can && Time.time < hipFireUntil);
                movement.AimYaw = followCamera ? followCamera.Yaw : transform.eulerAngles.y;
            }
            if (cover) cover.Peeking = IsAiming && cover.InCover;
            if (followCamera) followCamera.SetAiming(IsAiming);
            if (IsAiming && cover && cover.InCover) transform.rotation = Quaternion.Euler(0f, movement.AimYaw, 0f);

            bloom = Mathf.MoveTowards(bloom, 0f, 6f * Time.deltaTime);
            float speed = movement ? movement.PlanarVelocity.magnitude : 0f;
            CurrentSpread = weapon != null ? CombatMath.Spread(weapon.Spread, IsAiming, speed, bloom) : 0f;
            flash.enabled = Time.time < flashUntil;
            UpdateReload(weapon);
            UpdateVisuals(weapon);
            if (!can)
            {
                punching = queuedPunch = false;
                return;
            }

            if (actions.Reload.WasPressedThisFrame() && weapon != null) StartReload(weapon);
            if (actions.Attack.WasPressedThisFrame())
            {
                if (weapon == null) RequestPunch();
                else if (!(cover && cover.InCover && !IsAiming)) Fire(weapon);
            }
            TickPunch();
        }

        // ---------- Pistola ----------

        void Fire(ItemDef weapon)
        {
            if (Reloading || Time.time < nextShot) return;
            int rounds = inventory.Magazine(weapon.Id);
            if (rounds <= 0)
            {
                audioSource.PlayOneShot(clickClip);
                nextShot = Time.time + 0.25f;
                StartReload(weapon);
                return;
            }
            inventory.SetMagazine(weapon.Id, rounds - 1);
            nextShot = Time.time + weapon.FireInterval;

            float yaw = movement ? movement.AimYaw : transform.eulerAngles.y;
            if (!IsAiming)
            {
                hipFireUntil = Time.time + 0.6f;
                if (!(cover && cover.InCover)) transform.rotation = Quaternion.Euler(0f, yaw, 0f); // gira hacia la mira
            }

            var chest = transform.position + Vector3.up * 1.35f;
            var origin = muzzle ? muzzle.position : chest + transform.forward * 0.5f;
            var aimRay = followCamera ? followCamera.AimRay : new Ray(chest, transform.forward);
            var target = Physics.Raycast(aimRay, out var aimHit, weapon.Range, mask, QueryTriggerInteraction.Ignore) ? aimHit.point : aimRay.GetPoint(weapon.Range);
            var toTarget = target - origin;
            if (Vector3.Dot(toTarget, aimRay.direction) <= 0.1f) toTarget = aimRay.direction; // la mira apunta a algo detrás del arma
            var dir = CombatMath.Scatter(toTarget.normalized, CurrentSpread, Random.insideUnitCircle);
            bloom = Mathf.Min(bloom + weapon.Bloom, weapon.MaxBloom);

            // Del pecho al cañón: si el cañón está metido en una pared, el tiro se queda en ella.
            RaycastHit hit;
            bool blocked = Physics.Linecast(chest, origin, out hit, mask, QueryTriggerInteraction.Ignore);
            if (blocked || Physics.Raycast(origin, dir, out hit, weapon.Range, mask, QueryTriggerInteraction.Ignore))
            {
                float damage = weapon.Damage * (hit.collider.name == "Cabeza" ? headshotMultiplier : 1f);
                ApplyHit(hit.collider, hit.rigidbody, hit.point, dir, damage, DamageKind.Bullet, 4f);
                BulletHole(hit);
            }

            if (followCamera) followCamera.AddRecoil(weapon.Recoil, Random.Range(-0.4f, 0.4f));
            flashUntil = Time.time + 0.05f;
            audioSource.PlayOneShot(shotClip, 0.9f);
        }

        void StartReload(ItemDef weapon)
        {
            if (Reloading || inventory.Magazine(weapon.Id) >= weapon.MagazineSize || !inventory.Items.Has(weapon.AmmoId)) return;
            reloadingId = weapon.Id;
            reloadEnd = Time.time + weapon.ReloadSeconds;
            audioSource.PlayOneShot(clickClip, 0.5f);
        }

        void UpdateReload(ItemDef weapon)
        {
            if (!Reloading) return;
            if (weapon == null || weapon.Id != reloadingId || !CanAct) reloadingId = null; // cambió de arma: se cancela
            else if (Time.time >= reloadEnd)
            {
                inventory.Reload(reloadingId);
                reloadingId = null;
                audioSource.PlayOneShot(clickClip, 0.7f);
            }
        }

        void BulletHole(RaycastHit hit)
        {
            if (!holeMaterial)
            {
                holeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                holeMaterial.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f));
            }
            var hole = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hole.name = "Impacto";
            Destroy(hole.GetComponent<Collider>());
            hole.GetComponent<Renderer>().sharedMaterial = holeMaterial;
            hole.transform.SetPositionAndRotation(hit.point + hit.normal * 0.01f, Quaternion.LookRotation(-hit.normal));
            hole.transform.localScale = Vector3.one * 0.09f;
            if (hit.collider.GetComponentInParent<Health>() || hit.rigidbody) hole.transform.SetParent(hit.collider.transform, true);
            holes.Add(hole);
            if (holes.Count > 40)
            {
                if (holes[0]) Destroy(holes[0]);
                holes.RemoveAt(0);
            }
        }

        // ---------- Puños ----------

        void RequestPunch()
        {
            if (cover && cover.InCover) return;
            if (punching) queuedPunch = true; // se encadena al acabar este
            else StartPunch();
        }

        void StartPunch()
        {
            combo = Time.time < comboUntil ? (combo + 1) % 3 : 0;
            tiredPunch = vitals && !vitals.Stamina.TrySpend(punchStamina);
            float duration = combo == 2 ? 0.5f : 0.32f;
            punching = true;
            punchLanded = queuedPunch = false;
            punchStart = Time.time;
            punchEnd = Time.time + duration;
            punchHitAt = Time.time + duration * 0.45f;

            // Se orienta al objetivo más cercano delante (o hacia donde se pide ir) y da un paso.
            var target = FindMeleeTarget();
            var input = movement ? movement.InputDirection : Vector3.zero;
            Vector3 face = target ? target.transform.position - transform.position : input.sqrMagnitude > 0.1f ? input : transform.forward;
            face.y = 0f;
            if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(face);
            if (movement) movement.AddImpulse(transform.forward * (combo == 2 ? 3.5f : 2f));
        }

        void TickPunch()
        {
            if (!punching) return;
            if (!punchLanded && Time.time >= punchHitAt)
            {
                punchLanded = true;
                PunchHit();
            }
            if (Time.time < punchEnd) return;
            punching = false;
            comboUntil = Time.time + 0.45f;
            if (queuedPunch) StartPunch();
        }

        void PunchHit()
        {
            var center = transform.position + Vector3.up * 1.2f + transform.forward * 0.85f;
            float damage = punchDamage[Mathf.Clamp(combo, 0, punchDamage.Length - 1)] * (tiredPunch ? 0.5f : 1f);
            var done = new HashSet<Object>();
            bool any = false;
            foreach (var c in Physics.OverlapSphere(center, 0.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                var key = (Object)c.GetComponentInParent<Health>() ?? c.attachedRigidbody;
                if (key == null || !done.Add(key)) continue;
                ApplyHit(c, c.attachedRigidbody, c.ClosestPoint(center), transform.forward, damage, DamageKind.Melee, combo == 2 ? 6f : 3f);
                any = true;
            }
            if (any) audioSource.PlayOneShot(punchClip);
        }

        Health FindMeleeTarget()
        {
            Health best = null;
            float bestSqr = 2.5f * 2.5f;
            var forward = movement && movement.InputDirection.sqrMagnitude > 0.1f ? movement.InputDirection.normalized : transform.forward;
            foreach (var c in Physics.OverlapSphere(transform.position + Vector3.up, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = c.GetComponentInParent<Health>();
                if (!h || h == ownHealth || h.IsDead) continue;
                var d = h.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > bestSqr || Vector3.Angle(forward, d) > 70f) continue;
                bestSqr = d.sqrMagnitude;
                best = h;
            }
            return best;
        }

        // ---------- Común ----------

        void ApplyHit(Collider c, Rigidbody body, Vector3 point, Vector3 direction, float damage, DamageKind kind, float force)
        {
            var health = c.GetComponentInParent<Health>();
            if (health && health != ownHealth && !health.IsDead)
            {
                health.TakeDamage(new DamageInfo(damage, kind, point, direction, gameObject));
                LastHitTime = Time.time;
                LastHitKilled = health.IsDead;
            }
            if (body && !body.isKinematic) body.AddForceAtPosition(direction * force, point, ForceMode.Impulse);
        }

        void UpdateVisuals(ItemDef weapon)
        {
            // Escalando no se ve: las manos van al borde.
            bool showPistol = weapon != null && !(climb && climb.Climbing);
            if (pistolModel && pistolModel.activeSelf != showPistol) pistolModel.SetActive(showPistol);
            if (pistolModel && weapon != null)
            {
                // Se levanta al apuntar (a la altura de la barbilla, con los brazos casi estirados) y gira alrededor
                // del hombro siguiendo la mira arriba y abajo; baja al recargar.
                var t = pistolModel.transform;
                bool raised = IsAiming || Time.time < hipFireUntil;
                float pitch = 0f;
                if (raised && followCamera)
                    pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(followCamera.AimRay.direction.y, -1f, 1f)) * Mathf.Rad2Deg, -55f, 60f);
                AimPitch = Mathf.Lerp(AimPitch, pitch, 1f - Mathf.Exp(-14f * Time.deltaTime));
                var pivot = new Vector3(0.1f, 1.45f, 0f);
                var aimPose = pivot + Quaternion.Euler(AimPitch, 0f, 0f) * new Vector3(0.06f, 0.04f, 0.66f);
                var idle = new Vector3(0.3f, 0.98f, 0.16f);
                var pose = raised ? aimPose : idle;
                if (Reloading) pose += Vector3.down * 0.25f;
                float follow = 1f - Mathf.Exp(-18f * Time.deltaTime);
                t.localPosition = Vector3.Lerp(t.localPosition, pose, follow);
                // Apuntando, el cañón sigue la mira; en reposo, hacia el suelo delante, como se lleva de verdad.
                t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(raised ? AimPitch : 55f, 0f, 0f), follow);
            }
            PunchExtend = punching ? Mathf.Sin(Mathf.Clamp01((Time.time - punchStart) / Mathf.Max(0.01f, punchEnd - punchStart)) * Mathf.PI) : 0f;
            PunchSide = combo % 2 == 0 ? 1 : -1;
            if (!fist) return; // con brazos animados (PlayerAnimator) no hace falta un puño suelto
            bool show = punching;
            if (fist.gameObject.activeSelf != show) fist.gameObject.SetActive(show);
            if (!show) return;
            float k = Mathf.Clamp01((Time.time - punchStart) / Mathf.Max(0.01f, punchEnd - punchStart));
            float extend = Mathf.Sin(k * Mathf.PI); // sale y vuelve
            float side = combo % 2 == 0 ? 1f : -1f;
            fist.localPosition = new Vector3(fistRest.x * side, fistRest.y + (combo == 2 ? 0.1f * extend : 0f), fistRest.z + 0.6f * extend);
        }
    }
}
