using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GTAChiraq
{
    /// <summary>
    /// Cabeza de un NPC (civil o pandillero). Piensa cada 0.2 s y anda con un NavMeshAgent. Reglas (NPCS.md):
    /// <list type="bullet">
    /// <item>Vida normal: elige un sitio (<see cref="NpcSpot"/>) por puntuación (<see cref="NpcMath.SpotScore"/>): su
    /// esquina habitual, un banco, una tienda abierta, algo a cubierto si llueve; va, se queda un rato y cambia.</item>
    /// <item>Civiles: huyen de los disparos y de quien les apunta, se agachan un rato y se van.</item>
    /// <item>Enemigos (Dunmore, Larkin): si ven al jugador en su territorio, sospechan; primero miran, luego le encaran
    /// y al final sacan el arma (<see cref="NpcMath.SuspicionRate"/>). Fuera de su barrio solo atacan en incursiones o
    /// si les provocas.</item>
    /// <item>Aliados: no te atacan (salvo que les dispares: entonces te guardan rencor) y defienden su zona: disparan a
    /// los enemigos que entren en su territorio o que estén peleando cerca.</item>
    /// <item>Incursión (<see cref="Raider"/>): van armados hacia un punto del bloque y atacan a lo que encuentren del
    /// otro bando; luego se van.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcBrain : MonoBehaviour
    {
        public enum State { Idle, Walking, AtSpot, Flee, Cower, Stare, Confront, Combat, Raid, Leave, Dead }

        [SerializeField] Transform muzzle;
        [SerializeField] GameObject pistolModel;
        [SerializeField] float walkSpeed = 1.35f, runSpeed = 4.6f;
        [SerializeField] float bulletDamage = 9f;
        [SerializeField] int magazine = 12;

        public Faction Faction { get; private set; }
        public NpcRole Role { get; private set; }
        public bool Armed { get; private set; }
        public bool Raider { get; private set; }
        public State Current { get; private set; } = State.Idle;
        public float Suspicion { get; private set; }
        public Health Target { get; private set; }
        public float DeadSince { get; private set; }
        public bool Dead => Current == State.Dead;
        /// <summary>Le guarda rencor al jugador (le ha disparado a él o a alguien de su bando).</summary>
        public bool Grudge { get; private set; }

        // Para el animador.
        public bool Aiming { get; private set; }
        public float AimPitch { get; private set; }
        public float PunchExtend { get; private set; }
        public bool PistolOut => Armed && pistolModel && pistolModel.activeSelf;
        public NpcActivity Activity => spot ? spot.Activity : NpcActivity.Hangout;
        public bool AtSpot => Current == State.AtSpot;
        public bool Cowering => Current == State.Cower;
        public Vector3 Velocity => agent && agent.enabled ? agent.velocity : Vector3.zero;
        public Vector3 LookAt { get; private set; }
        public bool Looking { get; private set; }

        public NpcPopulation Owner { get; set; }
        public Health Health => health;

        NavMeshAgent agent;
        Health health;
        NpcSpot spot;
        Vector3 slot, raidGoal, lastSeenPos;
        float stateUntil, nextThink, lastSeenTime, nextShot, reloadUntil, punchStart = -10f, nextPunch, confrontSaid, walkingSince;
        int rounds, burst;
        float skill;
        AudioSource audioSource;
        Light flash;
        float flashUntil;
        static AudioClip shotClip, punchClip;
        static readonly List<NpcBrain> scratch = new();

        // El jugador (se busca una vez).
        static Transform player;
        static Health playerHealth;
        static PlayerCombat playerCombat;

        const float ThinkInterval = 0.2f;
        static readonly int SeeMask = ~((1 << GameLayers.IgnoreRaycast) | (1 << GameLayers.Npc));

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            if (health)
            {
                health.Damaged += OnDamaged;
                health.Died += Die;
            }
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 90f;
            shotClip ??= ProceduralSfx.Gunshot();
            punchClip ??= ProceduralSfx.Punch();
            if (muzzle)
            {
                flash = new GameObject("FogonazoLuz").AddComponent<Light>();
                flash.transform.SetParent(muzzle, false);
                flash.type = LightType.Point;
                flash.color = new Color(1f, 0.75f, 0.4f);
                flash.range = 6f;
                flash.intensity = 4f;
                flash.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (health)
            {
                health.Damaged -= OnDamaged;
                health.Died -= Die;
            }
            ReleaseSpot();
        }

        /// <summary>Lo llama la población al sacarlo.</summary>
        public void Init(Faction faction, NpcRole role, bool armed, bool raider = false, Vector3 raidGoal = default)
        {
            Faction = faction;
            Role = role;
            Armed = armed && role == NpcRole.Gang;
            Raider = raider;
            this.raidGoal = raidGoal;
            rounds = magazine;
            skill = Random.Range(0f, 1f);
            nextThink = Time.time + Random.Range(0f, ThinkInterval);
            agent.speed = Role == NpcRole.Gang ? walkSpeed * 0.9f : walkSpeed * Random.Range(0.9f, 1.15f);
            if (pistolModel) pistolModel.SetActive(false);
            if (raider)
            {
                Current = State.Raid;
                agent.speed = 3.2f;
                SetDestination(raidGoal);
                if (pistolModel) pistolModel.SetActive(Armed);
            }
        }

        public bool HostileToPlayer => Role == NpcRole.Gang && (Grudge || FactionCatalog.Hostile(Faction, FactionCatalog.PlayerFaction));

        static void FindPlayer()
        {
            if (player) return;
            var go = GameObject.FindWithTag("Player");
            if (!go) return;
            player = go.transform;
            playerHealth = go.GetComponent<Health>();
            playerCombat = go.GetComponent<PlayerCombat>();
        }

        /// <summary>Para los tests: olvida el jugador guardado.</summary>
        public static void ResetPlayerCache() => player = null;

        // ---------- Bucle ----------

        void Update()
        {
            if (Current == State.Dead) return;
            float now = Time.time;
            if (now >= nextThink)
            {
                nextThink = now + ThinkInterval;
                Think(now);
            }
            if (Current == State.Combat) CombatTick(now);
            else
            {
                Aiming = false;
                if (agent.enabled) agent.updateRotation = true;
                if (Current is State.Stare or State.Confront && player) Face(player.position, 360f);
                else if (Current == State.AtSpot && spot) Face(transform.position + spot.FacingFrom(slot), 180f);
            }
            PunchExtend = Mathf.Sin(Mathf.Clamp01((now - punchStart) / 0.35f) * Mathf.PI);
            if (flash) flash.enabled = now < flashUntil;
        }

        void Think(float now)
        {
            FindPlayer();
            if (NpcAlerts.Heard(transform.position, 0.5f, gameObject, out var alert)) React(alert);
            if (Role == NpcRole.Gang) GangPerception(now);
            else CivilianPerception();

            switch (Current)
            {
                case State.Idle:
                    ChooseSomething();
                    break;
                case State.Walking:
                    if (Arrived())
                    {
                        Current = State.AtSpot;
                        stateUntil = now + StayTime();
                        agent.ResetPath();
                    }
                    else if (now - walkingSince > 60f) GoIdle();
                    break;
                case State.AtSpot:
                    if (now > stateUntil || (spot && !spot.Open))
                    {
                        ReleaseSpot();
                        if (Role == NpcRole.Civilian && Random.value < 0.35f) Leave();
                        else GoIdle();
                    }
                    break;
                case State.Flee:
                    if (Arrived() || now > stateUntil)
                    {
                        Current = State.Cower;
                        agent.ResetPath();
                        stateUntil = now + Random.Range(4f, 9f);
                    }
                    break;
                case State.Cower:
                    if (now > stateUntil)
                    {
                        if (Role == NpcRole.Civilian) Leave();
                        else GoIdle();
                    }
                    break;
                case State.Combat:
                    CombatThink(now);
                    break;
                case State.Raid:
                    if (Arrived())
                    {
                        if (stateUntil <= 0f) stateUntil = now + 25f;
                        else if (now > stateUntil) Leave();
                    }
                    break;
                case State.Leave:
                    if (Arrived()) Leave();
                    break;
            }
        }

        // ---------- Percepción ----------

        void React(NpcAlert a)
        {
            if (Current == State.Dead) return;
            if (Role == NpcRole.Civilian)
            {
                if (Current != State.Flee && Current != State.Cower) Flee(a.Position);
                return;
            }
            var sourceFaction = FactionOf(a.Source, out bool sourceIsPlayer);
            var targetBrain = a.Target ? a.Target.GetComponentInParent<NpcBrain>() : null;
            bool targetIsPlayer = a.Target && player && a.Target.transform == player;

            // Le han disparado a alguien de mi bando el jugador: rencor.
            if (sourceIsPlayer && targetBrain && targetBrain != this && FactionCatalog.Relation(Faction, targetBrain.Faction) == FactionRelation.Ally
                && (transform.position - a.Position).sqrMagnitude < 35f * 35f)
                Grudge = true;

            bool sourceHostile = sourceIsPlayer ? HostileToPlayer : FactionCatalog.Hostile(Faction, sourceFaction);
            if (sourceHostile)
            {
                if (Armed) Engage(a.Source ? a.Source.GetComponentInParent<Health>() : null);
                else if (Current != State.Combat && Random.value < 0.5f) Flee(a.Position);
                return;
            }
            // Un aliado (o el jugador, si no hay rencor) pelea con alguien: le ayudo si es enemigo mío y está cerca.
            if (Armed && Current != State.Combat && (transform.position - a.Position).sqrMagnitude < 35f * 35f)
            {
                if (targetBrain && targetBrain.Role == NpcRole.Gang && FactionCatalog.Hostile(Faction, targetBrain.Faction)) Engage(targetBrain.Health);
                else if (targetIsPlayer && sourceFaction != Faction.None && FactionCatalog.Hostile(FactionCatalog.PlayerFaction, sourceFaction) && !HostileToPlayer)
                    Engage(a.Source ? a.Source.GetComponentInParent<Health>() : null);
            }
        }

        Faction FactionOf(GameObject go, out bool isPlayer)
        {
            isPlayer = go && player && go.transform == player;
            if (isPlayer) return FactionCatalog.PlayerFaction;
            var b = go ? go.GetComponentInParent<NpcBrain>() : null;
            return b ? (b.Role == NpcRole.Gang ? b.Faction : Faction.None) : Faction.None;
        }

        void CivilianPerception()
        {
            if (Current is State.Flee or State.Cower or State.Leave || !player || !playerCombat) return;
            // Le apuntan con una pistola de cerca: se asusta.
            if (!playerCombat.IsAiming) return;
            var toMe = transform.position - player.position;
            if (toMe.sqrMagnitude > 15f * 15f) return;
            if (Vector3.Angle(player.forward, toMe) < 18f) Flee(player.position);
        }

        void GangPerception(float now)
        {
            if (Current is State.Combat or State.Flee or State.Dead) return;
            float daylight = WeatherSystem.Instance ? WeatherSystem.Instance.LastLook.Daylight : 1f;

            // El jugador.
            bool sawPlayer = false;
            if (player && playerHealth && !playerHealth.IsDead && HostileToPlayer && PlayerOnFoot())
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist < NpcMath.SightRange(daylight) && CanSee(player.position + Vector3.up * 1.3f))
                {
                    sawPlayer = true;
                    if (Raider || Grudge) { Engage(playerHealth); return; }
                    bool inOurBlock = FactionCatalog.TerritoryAt(player.position) == Faction || (dist < 12f && FactionCatalog.TerritoryAt(transform.position) == Faction);
                    if (inOurBlock) Suspicion += NpcMath.SuspicionRate(dist, daylight) * ThinkInterval;
                    else sawPlayer = false;
                }
            }
            if (!sawPlayer) Suspicion = Mathf.Max(0f, Suspicion - 0.35f * ThinkInterval);

            if (Suspicion >= NpcMath.AttackAt) { Engage(playerHealth); return; }
            if (Suspicion >= NpcMath.ConfrontAt)
            {
                if (Current != State.Confront)
                {
                    ReleaseSpot();
                    Current = State.Confront;
                    agent.speed = walkSpeed * 1.1f;
                    if (now - confrontSaid > 30f && Owner)
                    {
                        confrontSaid = now;
                        Owner.Toast($"Los de {FactionCatalog.Name(Faction)} te han reconocido: «tú no eres de aquí»");
                    }
                }
                if (player && Vector3.Distance(transform.position, player.position) > 6f) SetDestination(player.position);
                else agent.ResetPath();
                return;
            }
            if (Suspicion >= NpcMath.StareAt)
            {
                if (Current != State.Stare && Current != State.Confront)
                {
                    Current = State.Stare;
                    agent.ResetPath();
                }
                return;
            }
            if (Current is State.Stare or State.Confront && Suspicion < 0.5f) GoIdle();

            // Enemigos del otro bando: los que entran en mi zona, los de una incursión o los que ya pelean.
            if (!Armed || Owner == null) return;
            Health best = null;
            float bestSqr = 25f * 25f;
            foreach (var other in Owner.Active)
            {
                if (!other || other == this || other.Dead || other.Role != NpcRole.Gang || !FactionCatalog.Hostile(Faction, other.Faction)) continue;
                float sqr = (other.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bool intrudes = FactionCatalog.SideOf(FactionCatalog.TerritoryAt(other.transform.position)) == FactionCatalog.SideOf(Faction);
                if (!(intrudes || other.Raider || Raider || other.Current == State.Combat)) continue;
                if (!CanSee(other.transform.position + Vector3.up * 1.3f)) continue;
                best = other.Health;
                bestSqr = sqr;
            }
            if (best) Engage(best);
        }

        bool PlayerOnFoot()
        {
            var cc = player ? player.GetComponent<CharacterController>() : null;
            return !cc || cc.enabled; // en el coche no se le reconoce (por ahora)
        }

        bool CanSee(Vector3 point)
        {
            var eye = transform.position + Vector3.up * 1.6f;
            return !Physics.Linecast(eye, point, SeeMask, QueryTriggerInteraction.Ignore);
        }

        void OnDamaged(DamageInfo info)
        {
            if (Current == State.Dead || health.IsDead) return;
            FindPlayer(); // puede llegar antes de que haya pensado por primera vez
            var src = info.Source;
            if (src && player && src.transform == player && Role == NpcRole.Gang && !HostileToPlayer) Grudge = true;
            if (Role == NpcRole.Civilian)
            {
                Flee(src ? src.transform.position : transform.position - info.Direction);
                return;
            }
            var h = src ? src.GetComponentInParent<Health>() : null;
            if (Armed || (h && (h.transform.position - transform.position).sqrMagnitude < 4f)) Engage(h);
            else Flee(src ? src.transform.position : transform.position);
        }

        // ---------- Vida normal ----------

        void GoIdle()
        {
            ReleaseSpot();
            Current = State.Idle;
            Suspicion = Mathf.Min(Suspicion, 0.4f);
            if (agent.enabled)
            {
                agent.speed = Role == NpcRole.Gang ? walkSpeed * 0.9f : walkSpeed;
                agent.ResetPath();
            }
            if (pistolModel && !Raider) pistolModel.SetActive(false);
        }

        void ChooseSomething()
        {
            var w = WeatherSystem.Instance;
            float precip = w ? Mathf.Max(w.Rain, w.Snow) : 0f;
            NpcSpot best = null;
            float bestScore = 0f;
            var pos = transform.position;
            foreach (var s in NpcSpot.All)
            {
                if (!s.Serves(Role)) continue;
                float d = Vector3.Distance(pos, s.transform.position);
                if (d > 90f) continue;
                float score = NpcMath.SpotScore(Role, Faction, s.Faction, s.Habitual, s.Covered, s.Open, precip, d, s.Occupants, s.Capacity, Random.Range(0.6f, 1.4f));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }
            if (best && Random.value < 0.85f)
            {
                spot = best;
                slot = best.Claim(this);
                SetDestination(slot);
                Current = State.Walking;
                walkingSince = Time.time;
                return;
            }
            // Pasear: un punto cualquiera a 15–40 m.
            var dir = Random.insideUnitCircle.normalized * Random.Range(15f, 40f);
            if (NavMesh.SamplePosition(pos + new Vector3(dir.x, 0f, dir.y), out var hit, 6f, NavMesh.AllAreas) && hit.position.y < 2f)
            {
                spot = null;
                SetDestination(hit.position);
                Current = State.Walking;
                walkingSince = Time.time;
            }
        }

        float StayTime()
        {
            if (!spot) return Random.Range(3f, 10f);
            return spot.Activity switch
            {
                NpcActivity.Errand => Random.Range(6f, 14f),
                NpcActivity.Mourn => Random.Range(15f, 40f),
                _ => Role == NpcRole.Gang ? Random.Range(60f, 200f) : Random.Range(15f, 60f),
            };
        }

        void ReleaseSpot()
        {
            if (spot) spot.Release(this);
            spot = null;
        }

        void Flee(Vector3 from)
        {
            ReleaseSpot();
            Current = State.Flee;
            stateUntil = Time.time + 12f;
            agent.speed = runSpeed + Random.Range(-0.3f, 0.4f);
            var away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f;
            var goal = transform.position + away.normalized * Random.Range(22f, 35f);
            if (NavMesh.SamplePosition(goal, out var hit, 10f, NavMesh.AllAreas)) SetDestination(hit.position);
        }

        /// <summary>Se va lejos del jugador (la población lo quita cuando no se ve).</summary>
        void Leave()
        {
            ReleaseSpot();
            Current = State.Leave;
            if (agent.enabled) agent.speed = walkSpeed;
            var from = player ? player.position : transform.position;
            var away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 1f) away = Random.insideUnitSphere;
            away.y = 0f;
            var goal = transform.position + away.normalized * 70f;
            if (NavMesh.SamplePosition(goal, out var hit, 15f, NavMesh.AllAreas)) SetDestination(hit.position);
        }

        bool Arrived() => agent.enabled && !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.35f);

        void SetDestination(Vector3 p)
        {
            if (agent.enabled && agent.isOnNavMesh) agent.SetDestination(p);
        }

        void Face(Vector3 point, float degPerSec)
        {
            var d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f || agent.velocity.sqrMagnitude > 0.2f) return;
            if (agent.enabled) agent.updateRotation = false;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), degPerSec * Time.deltaTime);
            LookAt = point;
            Looking = true;
        }

        // ---------- Combate ----------

        public void Engage(Health target)
        {
            if (!target || target.IsDead || target == health || Current == State.Dead) return;
            ReleaseSpot();
            Target = target;
            Current = State.Combat;
            lastSeenTime = Time.time;
            lastSeenPos = target.transform.position;
            agent.speed = Armed ? 3.2f : runSpeed;
            if (pistolModel) pistolModel.SetActive(Armed);
        }

        void CombatThink(float now)
        {
            if (!Target || Target.IsDead)
            {
                Target = null;
                Suspicion = 0f;
                if (Raider) Current = State.Raid;
                else GoIdle();
                return;
            }
            var tp = Target.transform.position;
            bool see = CanSee(tp + Vector3.up * 1.3f);
            if (see)
            {
                lastSeenPos = tp;
                lastSeenTime = now;
            }
            if (now - lastSeenTime > 10f)
            {
                Target = null;
                GoIdle();
                return;
            }
            float dist = Vector3.Distance(transform.position, tp);
            if (Armed)
            {
                // Se para a disparar si lo ve a tiro; si no, va hacia donde lo vio.
                if (!see || dist > 20f) SetDestination(lastSeenPos);
                else if (agent.hasPath) agent.ResetPath();
                // Herido de muerte: a veces sale corriendo.
                if (health && health.Pool.Current < health.Pool.Max * 0.25f && Random.value < 0.02f) Flee(tp);
            }
            else
            {
                if (dist > 1.3f) SetDestination(tp);
                else agent.ResetPath();
            }
        }

        void CombatTick(float now)
        {
            if (!Target) return;
            var targetChest = Target.transform.position + Vector3.up * 1.3f;
            bool recentlySeen = now - lastSeenTime < 1.2f;
            Aiming = Armed && recentlySeen;
            if (agent.enabled) agent.updateRotation = !Aiming && agent.velocity.sqrMagnitude > 0.5f;
            var d = Target.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f && (Aiming || agent.velocity.sqrMagnitude < 0.5f))
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 420f * Time.deltaTime);
            LookAt = targetChest;
            Looking = true;
            var chest = transform.position + Vector3.up * 1.4f;
            AimPitch = Mathf.Lerp(AimPitch, Mathf.Clamp(-Mathf.Atan2(targetChest.y - chest.y, d.magnitude) * Mathf.Rad2Deg, -50f, 50f), 1f - Mathf.Exp(-10f * Time.deltaTime));

            if (Armed)
            {
                if (now < reloadUntil || !Aiming || now < nextShot) return;
                if (Vector3.Angle(transform.forward, d) > 14f) return;
                Shoot(targetChest, d.magnitude);
            }
            else if (d.magnitude < 1.6f && now >= nextPunch)
            {
                nextPunch = now + Random.Range(0.8f, 1.2f);
                punchStart = now;
                Target.TakeDamage(new DamageInfo(7f, DamageKind.Melee, targetChest, d.normalized, gameObject));
                audioSource.PlayOneShot(punchClip, 0.8f);
                NpcAlerts.Hit(transform.position, gameObject, Target.gameObject);
            }
        }

        void Shoot(Vector3 aimAt, float distance)
        {
            float now = Time.time;
            var origin = muzzle ? muzzle.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            bool moving = agent.enabled && agent.velocity.sqrMagnitude > 0.3f;
            var dir = CombatMath.Scatter((aimAt - origin).normalized, NpcMath.ShotSpread(distance, moving, skill), Random.insideUnitCircle);
            if (Physics.Raycast(origin, dir, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = hit.collider.GetComponentInParent<Health>();
                if (h && h != health && !h.IsDead)
                {
                    float dmg = bulletDamage * (hit.collider.name == "Cabeza" ? 2f : 1f);
                    h.TakeDamage(new DamageInfo(dmg, DamageKind.Bullet, hit.point, dir, gameObject));
                }
                if (hit.rigidbody && !hit.rigidbody.isKinematic) hit.rigidbody.AddForceAtPosition(dir * 3f, hit.point, ForceMode.Impulse);
            }
            audioSource.PlayOneShot(shotClip, 0.9f);
            flashUntil = now + 0.05f;
            NpcAlerts.Gunshot(transform.position, gameObject, Target ? Target.gameObject : null);

            rounds--;
            burst++;
            // Ráfagas de 2–4 tiros con pausa; recarga al vaciar el cargador.
            nextShot = now + (burst >= Random.Range(2, 5) ? Random.Range(0.9f, 1.6f) : Random.Range(0.28f, 0.5f));
            if (nextShot - now > 0.8f) burst = 0;
            if (rounds <= 0)
            {
                rounds = magazine;
                reloadUntil = now + 2.2f;
            }
        }

        public bool Reloading => Time.time < reloadUntil;

        // ---------- Muerte ----------

        void Die()
        {
            if (Current == State.Dead) return;
            ReleaseSpot();
            Current = State.Dead;
            DeadSince = Time.time;
            Aiming = false;
            Target = null;
            if (agent.enabled) agent.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (pistolModel) pistolModel.SetActive(false);
            if (flash) flash.enabled = false;
            // A veces deja balas en el suelo.
            if (Armed && Random.value < 0.5f) ItemPickup.Spawn(ItemCatalog.Ammo9mm, Random.Range(6, 13), transform.position + transform.right * 0.6f + Vector3.up * 0.3f);
            if (Owner) Owner.OnNpcDied(this);
        }
    }
}
