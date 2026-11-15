using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GTAChiraq
{
    /// <summary>
    /// La gente del barrio alrededor del jugador (NPCS.md). Cada segundo compara cuántos civiles y pandilleros hay a
    /// menos de <see cref="despawnRadius"/> con los que tocan según la hora y el tiempo (<see cref="NpcMath"/>) y saca
    /// o quita. Los nuevos salen fuera de la vista, entre <see cref="spawnMin"/> y <see cref="spawnMax"/> m, en un
    /// sitio (<see cref="NpcSpot"/>): los pandilleros en grupos de 1–3 en un sitio de su barrio (el del territorio del
    /// sitio) y vestidos con su color; los civiles en cualquier sitio para civiles. Se quitan cuando quedan lejos y no
    /// se ven, y los muertos a los 40 s. Los NPC se clonan de una plantilla inactiva (la crea el generador).
    /// <para>Incursiones: cada minuto, si el jugador está en un barrio de su bando, tira <see cref="NpcMath.RaidChance"/>;
    /// si sale, 3–4 armados del grupo enemigo más cercano llegan desde fuera de la vista hacia él.</para>
    /// </summary>
    public sealed class NpcPopulation : MonoBehaviour
    {
        public static NpcPopulation Instance { get; private set; }

        [SerializeField] GameObject template;
        [SerializeField] Transform player;
        [SerializeField] float spawnMin = 35f, spawnMax = 110f, despawnRadius = 140f;
        [SerializeField] int maxNpcs = 40;
        [SerializeField] int seed = 2013;
        [Tooltip("Apagado, no saca a nadie (para tests y capturas que ponen los NPC a mano).")]
        [SerializeField] bool autoSpawn = true;
        [SerializeField] bool raids = true;

        readonly List<NpcBrain> active = new();
        System.Random rng;
        float nextTick, nextRaidRoll = 60f, lastRaid = -999f;
        string message;
        float messageUntil;
        Camera cam;

        public IReadOnlyList<NpcBrain> Active => active;
        public string Message => Time.time < messageUntil ? message : null;
        public bool AutoSpawn { get => autoSpawn; set => autoSpawn = value; }
        public bool Raids { get => raids; set => raids = value; }

        /// <summary>El barrio donde está el jugador, para el HUD.</summary>
        public string TerritoryLabel
        {
            get
            {
                if (!player) return "";
                var f = FactionCatalog.TerritoryAt(player.position);
                if (f == Faction.None) return "Zona neutral";
                string rel = FactionCatalog.Relation(FactionCatalog.PlayerFaction, f) switch
                {
                    FactionRelation.Hostile => " (enemigo)",
                    _ => f == FactionCatalog.PlayerFaction ? " (tu bloque)" : " (aliado)",
                };
                return FactionCatalog.Name(f) + rel;
            }
        }

        public Color TerritoryColor
        {
            get
            {
                if (!player) return Color.white;
                var f = FactionCatalog.TerritoryAt(player.position);
                return FactionCatalog.Hostile(FactionCatalog.PlayerFaction, f) ? new Color(1f, 0.45f, 0.4f)
                    : f == Faction.None ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.6f, 0.9f, 0.6f);
            }
        }

        /// <summary>Lo usa el generador.</summary>
        public void Configure(GameObject template, Transform player)
        {
            this.template = template;
            this.player = player;
        }

        void Awake()
        {
            Instance = this;
            rng = new System.Random(seed);
            if (!player)
            {
                var go = GameObject.FindWithTag("Player");
                if (go) player = go.transform;
            }
            if (template) template.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Toast(string text, float seconds = 4f)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + 1f;
            if (!player) return;
            if (!cam) cam = Camera.main;
            Despawn();
            if (autoSpawn && template) Populate();
            if (raids && template && Time.time >= nextRaidRoll)
            {
                nextRaidRoll = Time.time + 60f;
                TryRaid(false);
            }
        }

        // ---------- Quitar ----------

        void Despawn()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var b = active[i];
                if (!b)
                {
                    active.RemoveAt(i);
                    continue;
                }
                float d = Vector3.Distance(b.transform.position, player.position);
                bool gone = b.Dead ? Time.time - b.DeadSince > 40f && !Visible(b.transform.position)
                    : d > despawnRadius && !Visible(b.transform.position)
                      || b.Current == NpcBrain.State.Leave && d > spawnMin && !Visible(b.transform.position);
                if (gone) Recycle(b, i);
            }
        }

        void Recycle(NpcBrain b, int index)
        {
            active.RemoveAt(index);
            b.gameObject.SetActive(false);
            Destroy(b.gameObject); // el cuerpo cambia (ropa, colliders apagados): más simple que reutilizarlo
        }

        /// <summary>Lo llama el NPC al morir.</summary>
        public void OnNpcDied(NpcBrain b) { }

        // ---------- Sacar ----------

        void Populate()
        {
            var w = WeatherSystem.Instance;
            float daylight = w ? w.LastLook.Daylight : 1f;
            float rain = w ? w.Rain : 0f, snow = w ? w.Snow : 0f;
            int civTarget = NpcMath.Civilians(daylight, rain, snow);
            int gangTarget = NpcMath.GangMembers(daylight, rain, snow);
            int civ = 0, gang = 0;
            foreach (var b in active)
            {
                if (!b || b.Dead || b.Raider) continue;
                if (b.Role == NpcRole.Civilian) civ++;
                else gang++;
            }
            int spawned = 0;
            // Lo que más falta, primero; como mucho dos tandas por segundo para no dar tirones.
            while (spawned < 2 && active.Count < maxNpcs)
            {
                float civNeed = civTarget - civ, gangNeed = gangTarget - gang;
                if (civNeed <= 0 && gangNeed <= 0) break;
                if (gangNeed / Mathf.Max(1f, gangTarget) >= civNeed / Mathf.Max(1f, civTarget))
                {
                    int n = SpawnGangGroup(daylight, gangTarget - gang);
                    if (n == 0) break;
                    gang += n;
                }
                else
                {
                    if (!SpawnCivilian()) break;
                    civ++;
                }
                spawned++;
            }
        }

        bool SpawnCivilian()
        {
            var s = PickSpot(NpcRole.Civilian, Faction.None);
            if (!s) return false;
            var pos = Around(s.transform.position, 3f);
            if (pos == null) return false;
            Spawn(Faction.None, NpcRole.Civilian, false, pos.Value, false, default);
            return true;
        }

        int SpawnGangGroup(float daylight, int max)
        {
            var s = PickSpot(NpcRole.Gang, Faction.None);
            if (!s) return 0;
            var f = s.Faction;
            if (f == Faction.None) return 0;
            int n = Mathf.Clamp(rng.Next(1, 4), 1, Mathf.Max(1, max));
            int made = 0;
            for (int i = 0; i < n && active.Count < maxNpcs; i++)
            {
                var pos = Around(s.transform.position, 3f);
                if (pos == null) continue;
                Spawn(f, NpcRole.Gang, rng.NextDouble() < NpcMath.ArmedChance(daylight), pos.Value, false, default);
                made++;
            }
            return made;
        }

        /// <summary>Un sitio a la distancia de aparición y fuera de la vista (al azar, pesando los habituales).</summary>
        NpcSpot PickSpot(NpcRole role, Faction only)
        {
            NpcSpot best = null;
            double bestScore = 0;
            foreach (var s in NpcSpot.All)
            {
                if (!s.Serves(role) || (only != Faction.None && s.Faction != only)) continue;
                if (role == NpcRole.Gang && s.Faction == Faction.None) continue;
                float d = Vector3.Distance(s.transform.position, player.position);
                if (d < spawnMin || d > spawnMax || Visible(s.transform.position)) continue;
                double score = rng.NextDouble() * (s.Habitual ? 3 : 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }
            return best;
        }

        Vector3? Around(Vector3 center, float radius)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                var o = new Vector3((float)(rng.NextDouble() * 2 - 1) * radius, 0f, (float)(rng.NextDouble() * 2 - 1) * radius);
                // Solo el suelo: nada encima de mesas, coches o muros (la malla también los cubre).
                if (NavMesh.SamplePosition(center + o, out var hit, 3f, NavMesh.AllAreas) && hit.position.y < center.y + 0.6f) return hit.position;
            }
            return null;
        }

        bool Visible(Vector3 p)
        {
            if (!cam) return false;
            var v = cam.WorldToViewportPoint(p + Vector3.up);
            return v.z > 0f && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f && v.z < 160f;
        }

        /// <summary>Saca un NPC (también para tests y capturas).</summary>
        public NpcBrain Spawn(Faction faction, NpcRole role, bool armed, Vector3 position, bool raider, Vector3 raidGoal, float? yaw = null)
        {
            if (!template) return null;
            var go = Instantiate(template, position, Quaternion.Euler(0f, yaw ?? (float)(rng.NextDouble() * 360.0), 0f), transform);
            go.name = role == NpcRole.Civilian ? "Civil" : "Pandillero_" + faction;
            var outfit = go.GetComponent<PlayerOutfit>();
            var brain = go.GetComponent<NpcBrain>();
            go.SetActive(true);
            if (outfit) outfit.Wear(FactionCatalog.RandomOutfit(role == NpcRole.Gang ? faction : Faction.None, rng), 0.7f + (float)rng.NextDouble() * 0.75f);
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent && !agent.isOnNavMesh && NavMesh.SamplePosition(position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
            brain.Owner = this;
            brain.Init(faction, role, armed, raider, raidGoal);
            active.Add(brain);
            return brain;
        }

        // ---------- Incursiones ----------

        /// <summary>
        /// Tira la incursión (o la fuerza): solo si el jugador está en un barrio de su bando (el suyo o uno aliado) y
        /// ha pasado el respiro. Vienen del grupo enemigo cuyo barrio queda más cerca.
        /// </summary>
        public bool TryRaid(bool force)
        {
            if (!player || !template) return false;
            var here = FactionCatalog.TerritoryAt(player.position);
            if (FactionCatalog.SideOf(here) != FactionSide.Block) return false;
            if (!force)
            {
                if (Time.time - lastRaid < NpcMath.RaidCooldown) return false;
                var w = WeatherSystem.Instance;
                float chance = NpcMath.RaidChance(w ? w.LastLook.Daylight : 1f, w ? w.Rain : 0f, w ? w.Snow : 0f);
                if (rng.NextDouble() >= chance) return false;
            }
            Faction enemy = Faction.None;
            float best = float.MaxValue;
            foreach (var f in new[] { Faction.Dunmore, Faction.Larkin })
            {
                float d = Vector2.Distance(FactionCatalog.Center(f), new Vector2(player.position.x, player.position.z));
                if (d < best)
                {
                    best = d;
                    enemy = f;
                }
            }
            // Entran desde el lado de su barrio, a 70–90 m, por donde no se ve.
            var from = FactionCatalog.Center(enemy);
            var dir = new Vector3(from.x - player.position.x, 0f, from.y - player.position.z).normalized;
            Vector3? start = null;
            for (int tries = 0; tries < 12 && start == null; tries++)
            {
                var d = Quaternion.Euler(0f, (float)(rng.NextDouble() * 120 - 60), 0f) * dir;
                var p = player.position + d * (70f + (float)rng.NextDouble() * 20f);
                if (NavMesh.SamplePosition(p, out var hit, 8f, NavMesh.AllAreas) && hit.position.y < 2f && (force || !Visible(hit.position))) start = hit.position;
            }
            if (start == null) return false;
            int n = 3 + rng.Next(0, 2);
            for (int i = 0; i < n; i++)
            {
                var p = Around(start.Value, 2.5f) ?? start.Value;
                var goal = Around(player.position, 8f) ?? player.position;
                Spawn(enemy, NpcRole.Gang, true, p, true, goal);
            }
            lastRaid = Time.time;
            Toast($"¡Incursión! Los de {FactionCatalog.Name(enemy)} se han metido en el bloque", 6f);
            return true;
        }

        [ContextMenu("Forzar incursión")]
        void ForceRaid() => TryRaid(true);

        /// <summary>Quita a todos (tests).</summary>
        public void Clear()
        {
            foreach (var b in active)
                if (b) Destroy(b.gameObject);
            active.Clear();
        }
    }
}
