using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// La gente del barrio (NPCS.md, Scripts/NPC): la malla de navegación de todo el mundo, los sitios donde los NPC
    /// hacen algo (<see cref="NpcSpot"/>), el horario de los locales (<see cref="ShopHours"/>), la plantilla de NPC y
    /// la población (<see cref="NpcPopulation"/>).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const int NpcLayer = 7; // "Personaje" (GameLayers.Npc)
        const string NavMeshPath = Root + "/Scenes/Prototype_NavMesh.asset";

        /// <summary>
        /// Lo llama <see cref="Storefront"/>: horario del local (cerrado apaga el OPEN y baja una persiana entera; la
        /// tienda de licores, FOOD &amp; LIQUOR, nunca cierra), un sitio de recado en la puerta para los civiles y, en la
        /// de licores, una esquina de venta para el grupo del barrio (la de la franja de negocios, de Ash Block).
        /// </summary>
        static void ShopLife(Transform root, ShopKind kind, float width, float doorX, float front, GameObject openSign)
        {
            ShopHours hours = null;
            if (kind != ShopKind.Church)
            {
                var shutter = TexturedBox("PersianaCerrada", root, new Vector3(0f, 1.55f, front - 0.17f), new Vector3(width - 0.2f, 3.1f, 0.03f),
                    shopShutter, new Vector2(width, 3f), collider: false);
                shutter.SetActive(false);
                hours = root.gameObject.AddComponent<ShopHours>();
                hours.Configure(kind == ShopKind.Market, openSign, shutter);
            }
            var errand = Spot(root, "SitioRecado", new Vector3(doorX, 0f, front - 1.3f), 0f, NpcActivity.Errand, 2, 0.6f,
                civilians: true, gang: false);
            errand.Configure(NpcActivity.Errand, 2, 0.6f, false, true, false, shop: hours);

            if (kind == ShopKind.Market)
            {
                var pos = root.TransformPoint(new Vector3(doorX + (doorX > 0f ? -3.2f : 3.2f), 0f, -2.2f));
                var f = FactionCatalog.TerritoryAt(pos);
                if (f == Faction.None) f = Faction.AshBlock;
                Spot(root, "EsquinaLicores", new Vector3(doorX + (doorX > 0f ? -3.2f : 3.2f), 0f, -2.2f), 180f, NpcActivity.Sell, 3, 1.4f,
                    civilians: false, gang: true, habitual: true, faction: f);
            }
        }

        static NpcSpot Spot(Transform parent, string name, Vector3 localPosition, float yaw, NpcActivity activity, int capacity, float radius,
            bool civilians, bool gang, bool habitual = false, Faction faction = Faction.None, bool covered = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var s = go.AddComponent<NpcSpot>();
            s.Configure(activity, capacity, radius, covered, civilians, gang, habitual, faction);
            return s;
        }

        // Esquinas y sitios habituales de cada grupo (docs/npcs/facciones.md), en coordenadas del mundo (y = suelo).
        static readonly (string name, Vector3 pos, NpcActivity activity, int capacity, Faction faction, bool covered)[] HabitualSpots =
        {
            // Ash Block: zona de reunión, canasta, memorial, portales de las torres y el aparcamiento P2.
            ("Reunion", new Vector3(-26f, 0f, 20f), NpcActivity.Hangout, 5, Faction.AshBlock, false),
            ("Canasta", new Vector3(-26f, 0f, 31f), NpcActivity.Hangout, 4, Faction.AshBlock, false),
            ("PortalT2", new Vector3(0f, 0f, -12f), NpcActivity.Hangout, 4, Faction.AshBlock, true),
            ("PortalT3", new Vector3(57f, 0f, 6f), NpcActivity.Hangout, 3, Faction.AshBlock, false),
            ("AparcamientoP2", new Vector3(8f, 0f, -40f), NpcActivity.Sell, 3, Faction.AshBlock, false),
            // Kessler: cancha, plaza de los grafitis, la entrada junto a la parada y el pie de la escalera de la estación.
            ("CanchaKessler", new Vector3(232f, 0f, 46f), NpcActivity.Hangout, 6, Faction.Kessler, false),
            ("PlazaKessler", new Vector3(232f, 0f, -58f), NpcActivity.Hangout, 5, Faction.Kessler, false),
            ("EntradaKessler", new Vector3(125f, 0f, 22f), NpcActivity.Sell, 3, Faction.Kessler, false),
            ("EscaleraEste", new Vector3(119f, 0f, 36f), NpcActivity.Hangout, 3, Faction.Kessler, false),
            // Crestline: gasolinera, casa de empeños y cancha del oeste (el bidón del descampado lo pone ScanProps).
            ("Gasolinera", new Vector3(-139f, 0f, 100f), NpcActivity.Sell, 3, Faction.Crestline, true),
            ("Empenos", new Vector3(-224f, 0f, 90f), NpcActivity.Sell, 3, Faction.Crestline, false),
            ("CanchaOeste", new Vector3(-238f, 0f, 146f), NpcActivity.Hangout, 5, Faction.Crestline, false),
            // Dunmore: barbería, la B' sin salida, el solar de los negocios y sus calles.
            ("Barberia", new Vector3(32f, 0f, 90.5f), NpcActivity.Hangout, 4, Faction.Dunmore, false),
            ("BPrimaSinSalida", new Vector3(8f, 0f, 115f), NpcActivity.Sell, 3, Faction.Dunmore, false),
            ("SolarNegocios", new Vector3(59f, 0f, 97f), NpcActivity.Hangout, 4, Faction.Dunmore, false),
            ("CalleAPrima", new Vector3(70f, 0f, 190f), NpcActivity.Sell, 3, Faction.Dunmore, false),
            ("CalleCPrima", new Vector3(77f, 0f, 128f), NpcActivity.Hangout, 3, Faction.Dunmore, false),
            ("CalleE", new Vector3(-70f, 0f, 186f), NpcActivity.Sell, 3, Faction.Dunmore, false),
            // Larkin: tienda de esquina, quiebro de la calle C, el solar del este y la calle A.
            ("TiendaLarkin", new Vector3(182f, 0f, 190f), NpcActivity.Sell, 3, Faction.Larkin, false),
            ("QuiebroC", new Vector3(191f, 0f, 176f), NpcActivity.Hangout, 4, Faction.Larkin, false),
            ("SolarEste", new Vector3(230f, 0f, 187f), NpcActivity.Hangout, 4, Faction.Larkin, false),
            ("CalleA", new Vector3(148f, 0f, 122f), NpcActivity.Sell, 3, Faction.Larkin, false),
            ("CalleG", new Vector3(230f, 0f, 250f), NpcActivity.Hangout, 3, Faction.Larkin, false),
        };

        // Sitios de paso para civiles: aceras de las calles grandes, cruces y la entrada del instituto.
        static readonly Vector3[] CivilianSpots =
        {
            // Aceras: la de la calle sur (z −71.5 y −88.5), la de la calle norte (z 71.5 y 89), las de la avenida
            // (x 102 y 118) y las de la calle x = −110 (x −101.5 y −118.5).
            new(-60f, 0f, -71.5f), new(40f, 0f, -71.5f), new(-30f, 0f, -88.5f), new(150f, 0f, -88.5f), new(-180f, 0f, -88.5f),
            new(-60f, 0f, 71.5f), new(0f, 0f, 71.5f), new(60f, 0f, 89f), new(-200f, 0f, 89f), new(160f, 0f, 89f),
            new(102f, 0f, -40f), new(118f, 0f, 60f), new(102f, 0f, 150f), new(118f, 0f, 250f),
            new(-101.5f, 0f, -30f), new(-118.5f, 0f, 40f), new(-118.5f, 0f, 150f), new(-101.5f, 0f, 250f),
            new(-200f, 0f, 76f), new(-40f, 0f, 0f), new(30f, 0f, -20f), new(170f, 0f, 0f), new(200f, 0f, -30f),
            new(30f, 0f, 150f), new(-40f, 0f, 230f), new(170f, 0f, 140f), new(200f, 0f, 230f), new(-170f, 0f, 200f),
            // Barrio norte (North.cs): aceras del bulevar (z 382.5 y 407.5) y de las calles grandes, la acera cubierta del
            // centro comercial, el camino del parque, el patio del bloque, el atrio de la iglesia y el camino de la torre suelta.
            new(-200f, 0f, 382.5f), new(-70f, 0f, 407.5f), new(60f, 0f, 382.5f), new(160f, 0f, 407.5f), new(260f, 0f, 382.5f),
            new(102f, 0f, 350f), new(118f, 0f, 440f), new(-101.5f, 0f, 330f), new(-118.5f, 0f, 450f),
            new(5f, 0f, 460.3f), new(35f, 0f, 460.3f), new(-8.5f, 0f, 432f), new(-74f, 0f, 446f), new(-143.5f, 0f, 370f), new(225f, 0f, 459f), new(237f, 0f, 340f),
        };

        static void BuildNpcs(Transform world, GameObject player)
        {
            var spots = new GameObject("SitiosNPC").transform;
            spots.SetParent(world, false);
            foreach (var (name, pos, activity, capacity, faction, covered) in HabitualSpots)
                Spot(spots, name, pos, 0f, activity, capacity, activity == NpcActivity.Sell ? 1.4f : 1.8f,
                    civilians: false, gang: true, habitual: true, faction: faction, covered: covered);
            for (int i = 0; i < CivilianSpots.Length; i++)
                Spot(spots, "Paso" + i, CivilianSpots[i], i * 47f, NpcActivity.Hangout, 2, 1.1f, civilians: true, gang: false);
            // El memorial: de cara a él, para todos.
            Spot(spots, "Memorial", new Vector3(-17.5f, 0f, -7.2f), 0f, NpcActivity.Mourn, 3, 1.2f, civilians: true, gang: true);
            ScanProps(world);

            BuildNavMesh();
            var template = BuildNpcTemplate(player);
            var population = new GameObject("Poblacion").AddComponent<NpcPopulation>();
            template.transform.SetParent(population.transform, true);
            population.Configure(template, player.transform);
            EditorUtility.SetDirty(population);
        }

        /// <summary>Sitios en props que ya hay: bancos (sentarse; en la parada, esperar a cubierto) y el bidón con fuego.</summary>
        static void ScanProps(Transform world)
        {
            foreach (var t in world.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Banco")
                {
                    bool bus = t.parent && t.parent.name == "ParadaBus";
                    // El banco mira hacia −Z local: el sitio, en el asiento y de cara al frente.
                    Spot(t, "SitioBanco", new Vector3(0f, 0f, -0.05f), 180f, bus ? NpcActivity.Wait : NpcActivity.Sit, 2, 0.7f,
                        civilians: true, gang: !bus, covered: bus);
                }
                else if (t.name == "Bidon" && t.parent && t.parent.Find("Brasas"))
                {
                    var s = Spot(t.parent, "SitioBidon", t.localPosition - Vector3.up * t.localPosition.y, 0f, NpcActivity.Warm, 4, 1.05f,
                        civilians: false, gang: true, habitual: true);
                    s.Configure(NpcActivity.Warm, 4, 1.05f, false, false, true, true, Faction.Crestline);
                }
            }
        }

        /// <summary>
        /// Malla de navegación de todo el mundo con los colliders (sin el jugador ni los NPC), guardada junto a la
        /// escena (<see cref="NavMeshPath"/>, no se versiona: se regenera con la escena).
        /// </summary>
        static void BuildNavMesh()
        {
            var surface = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~((1 << IgnoreRaycastLayer) | (1 << NpcLayer));
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.2f;
            surface.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath)) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        /// <summary>
        /// La plantilla inactiva de la que salen todos los NPC: el cuerpo del jugador (con la pistola), una cápsula
        /// hasta los hombros y una esfera "Cabeza" para los tiros a la cabeza, NavMeshAgent, Health, la ropa
        /// (<see cref="PlayerOutfit"/>, sin guardar), <see cref="NpcAnimator"/> y <see cref="NpcBrain"/>; en la capa
        /// "Personaje".
        /// </summary>
        static GameObject BuildNpcTemplate(GameObject player)
        {
            var go = new GameObject("PlantillaNPC");
            go.transform.position = new Vector3(0f, -50f, 0f);
            var model = Object.Instantiate(player.transform.Find("Modelo").gameObject, go.transform, false);
            model.name = "Modelo";

            var body = go.AddComponent<CapsuleCollider>();
            body.height = 1.5f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.75f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var skull = FindChild(model.transform, "Craneo");
            var headGo = new GameObject("Cabeza");
            headGo.transform.SetParent(skull ? skull : go.transform, false);
            headGo.transform.localPosition = skull ? new Vector3(0f, 0.11f, 0.01f) : new Vector3(0f, 1.65f, 0f);
            headGo.AddComponent<SphereCollider>().radius = 0.14f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.speed = 1.35f;
            agent.angularSpeed = 420f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.15f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            go.AddComponent<Health>();
            var outfit = go.AddComponent<PlayerOutfit>();
            EditorUtility.CopySerialized(player.GetComponent<PlayerOutfit>(), outfit); // texturas y escalas de reposo
            var so = new SerializedObject(outfit);
            so.FindProperty("persist").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<NpcAnimator>();
            var brain = go.AddComponent<NpcBrain>();
            var pistol = FindChild(model.transform, "Pistola");
            SetReference(brain, "pistolModel", pistol ? pistol.gameObject : null);
            SetReference(brain, "muzzle", FindChild(model.transform, "BocaCanon"));

            SetLayerRecursively(go, NpcLayer);
            go.SetActive(false);
            return go;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
